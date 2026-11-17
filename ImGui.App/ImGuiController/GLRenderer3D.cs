// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using ktsu.ImGui.Rendering;
using Silk.NET.OpenGL;

/// <summary>
/// The OpenGL backend's <see cref="IRenderer3D"/>: offscreen framebuffers and a second shader
/// program beside ImGui's own.
/// </summary>
/// <remarks>
/// <para>
/// <strong>A target's handle is its colour texture's name.</strong> That is the one value that is
/// unique for as long as the target lives, because it comes from the namespace
/// <c>CreateTexture</c> draws from: a plain texture can never share it. A handle drawn from a counter,
/// or the framebuffer's own name, could collide with a live texture's number, and then
/// <see cref="DeleteRenderTarget"/> handed that texture would tear down a target instead of
/// refusing. <see cref="GetTargetTexture"/> therefore returns its argument, once it has checked it.
/// </para>
/// <para>
/// <strong>Clip space follows <see cref="Matrix4x4.CreatePerspectiveFieldOfView"/>, not OpenGL.</strong>
/// Depth runs 0 at the near plane to 1 at the far one, as the software renderer's does, so one matrix
/// draws the same picture on both. OpenGL clips z to [-w, w]; the vertex shader remaps [0, w] onto it,
/// which with the default depth range lands the window depth back on z / w exactly.
/// </para>
/// <para>
/// <strong>Rows are flipped, and so is the winding that counts as front.</strong> OpenGL writes
/// NDC y = -1 into a texture's first row, and <c>ImGui.Image</c> shows the first row at the top, so
/// an unflipped target would appear upside down beside the software renderer's. The shader negates
/// clip y; a triangle counter-clockwise in clip space is then clockwise in window space, so
/// front-facing is <c>GL_CW</c> while this renderer draws.
/// </para>
/// <para>
/// <strong>Every piece of state a call touches is put back.</strong> These calls are made from a
/// frame's render callback, in the middle of a context ImGui and possibly an embedding host are also
/// drawing with; a leaked depth test is enough to make ImGui itself render nothing. The bound
/// framebuffers in particular are restored to what they were, not to zero, since a host may be
/// rendering into its own.
/// </para>
/// </remarks>
/// <param name="gl">The calls to issue.</param>
internal sealed class GLRenderer3D(IGL3D gl) : IRenderer3D, IDisposable
{
	internal const string VertexSource =
		@"#version 330
		layout (location = 0) in vec3 Position;
		layout (location = 1) in vec2 UV;
		layout (location = 2) in vec4 Color;
		uniform mat4 ModelViewProjection;
		out vec2 Frag_UV;
		out vec4 Frag_Color;
		void main()
		{
			Frag_UV = UV;
			Frag_Color = Color;
			vec4 clip = ModelViewProjection * vec4(Position, 1.0);
			clip.z = (clip.z * 2.0) - clip.w;
			clip.y = -clip.y;
			gl_Position = clip;
		}";

	internal const string FragmentSource =
		@"#version 330
		in vec2 Frag_UV;
		in vec4 Frag_Color;
		uniform sampler2D Texture;
		uniform int UseTexture;
		layout (location = 0) out vec4 Out_Color;
		void main()
		{
			vec4 color = Frag_Color;
			if (UseTexture != 0)
			{
				color *= texture(Texture, Frag_UV);
			}
			Out_Color = color;
		}";

	/// <summary>
	/// A triangle covering the whole viewport, made from the vertex index alone so the tone-map pass
	/// needs no vertex buffer: indices 0, 1, 2 give (0,0), (2,0), (0,2) in texture space.
	/// </summary>
	/// <remarks>
	/// No row flip here, unlike <see cref="VertexSource"/>: both textures are stored the same way up,
	/// so mapping the destination's rows straight onto the source's keeps the picture as it was.
	/// </remarks>
	internal const string ToneMapVertexSource =
		@"#version 330
		out vec2 Frag_UV;
		void main()
		{
			vec2 corner = vec2((gl_VertexID << 1) & 2, gl_VertexID & 2);
			Frag_UV = corner;
			gl_Position = vec4((corner * 2.0) - 1.0, 0.0, 1.0);
		}";

	/// <summary>The tone map, matching <see cref="ToneMap3D.Apply"/> channel for channel.</summary>
	internal const string ToneMapFragmentSource =
		@"#version 330
		in vec2 Frag_UV;
		uniform sampler2D Source;
		uniform float Exposure;
		uniform float Power;
		uniform int Curve;
		uniform int PreserveHue;
		layout (location = 0) out vec4 Out_Color;
		float MapChannel(float raw)
		{
			float value = max(raw, 0.0) * Exposure;
			if (Power != 1.0 && value > 0.0)
			{
				value = pow(value, Power);
			}

			if (Curve == 1)
			{
				value = value / (1.0 + value);
			}
			else if (Curve == 2)
			{
				value = 1.0 - exp(-value);
			}

			return clamp(value, 0.0, 1.0);
		}

		void main()
		{
			vec4 color = texture(Source, Frag_UV);
			vec3 rgb = max(color.rgb, vec3(0.0));
			vec3 mapped;
			if (PreserveHue != 0)
			{
				float peak = max(rgb.r, max(rgb.g, rgb.b));
				mapped = peak > 0.0 ? min(rgb * (MapChannel(peak) / peak), vec3(1.0)) : vec3(0.0);
			}
			else
			{
				mapped = vec3(MapChannel(rgb.r), MapChannel(rgb.g), MapChannel(rgb.b));
			}

			Out_Color = vec4(mapped, clamp(color.a, 0.0, 1.0));
		}";

	/// <summary>
	/// The emissive volume pass, matching <see cref="VolumeRay"/> step for step: the same
	/// unprojection, clip, sample count, jitter and fade. Rows: the 3D pass negates clip y so a target's
	/// first row is its top, so a fragment in row r (from the bottom of the GL framebuffer, which is
	/// the target's first row) has normalized device y = 1 − 2(r + ½)/height.
	/// </summary>
	internal const string VolumeFragmentSource =
		@"#version 330
		uniform sampler3D Volume;
		uniform mat4 ClipToWorld;
		uniform mat4 WorldToVolume;
		uniform vec3 Eye;
		uniform vec4 Color;
		uniform vec4 Color1;
		uniform vec4 Color2;
		uniform vec4 Color3;
		uniform vec4 Extinction;
		uniform vec4 NoiseWeights;
		uniform sampler3D DetailMap;
		uniform int HasDetail;
		uniform float DetailFadeHeight;
		uniform float PlaneScale;
		uniform int Channels;
		uniform float FadeStart;
		uniform float FadeEnd;
		uniform float SamplesPerVoxel;
		uniform int MaxSamples;
		uniform vec3 VolumeSize;
		uniform vec4 TargetSize;
		uniform int Additive;
		uniform int Premultiplied;
		uniform float LowNoiseAmplitude;
		uniform float LowNoiseScale;
		uniform float HighNoiseAmplitude;
		uniform float HighNoiseScale;
		uniform vec3 NoiseOffset;
		uniform vec3 NoiseStretch;
		uniform float PixelAngle;
		layout (location = 0) out vec4 Out_Color;

		float Lattice(ivec3 c)
		{
			uvec3 u = uvec3(c);
			uint h = (u.x * 0x8DA6B343u) ^ (u.y * 0xD8163841u) ^ (u.z * 0xCB1AB31Fu);
			h ^= h >> 16u;
			h *= 0x7FEB352Du;
			h ^= h >> 15u;
			h *= 0x846CA68Bu;
			h ^= h >> 16u;
			return float(h >> 8u) * (1.0 / 16777216.0);
		}

		float Noise(vec3 p)
		{
			vec3 f = floor(p);
			ivec3 i = ivec3(f);
			vec3 t = p - f;
			t = t * t * (3.0 - (2.0 * t));
			float x00 = mix(Lattice(i), Lattice(i + ivec3(1, 0, 0)), t.x);
			float x10 = mix(Lattice(i + ivec3(0, 1, 0)), Lattice(i + ivec3(1, 1, 0)), t.x);
			float x01 = mix(Lattice(i + ivec3(0, 0, 1)), Lattice(i + ivec3(1, 0, 1)), t.x);
			float x11 = mix(Lattice(i + ivec3(0, 1, 1)), Lattice(i + ivec3(1, 1, 1)), t.x);
			return mix(mix(x00, x10, t.y), mix(x01, x11, t.y), t.z);
		}

		float OctaveWeight(float cell, float footprint)
		{
			return PixelAngle <= 0.0 ? 1.0 : clamp(((cell / max(footprint, 1e-12)) - 1.0) / 2.0, 0.0, 1.0);
		}

		float Layer(vec3 p, float footprint, float amplitude, float scale, vec3 shift)
		{
			if (amplitude <= 0.0 || scale <= 0.0)
			{
				return 1.0;
			}

			float weight = OctaveWeight(scale, footprint);
			return weight <= 0.0 ? 1.0 : max(0.0, 1.0 + (amplitude * weight * ((2.0 * Noise((p / scale) + shift)) - 1.0)));
		}

		float Grain(vec3 world, float distanceToEye)
		{
			vec3 p = (world + NoiseOffset) / NoiseStretch;
			float footprint = distanceToEye * PixelAngle;
			return Layer(p, footprint, LowNoiseAmplitude, LowNoiseScale, vec3(0.0))
				* Layer(p, footprint, HighNoiseAmplitude, HighNoiseScale, vec3(17.3));
		}

		vec3 Unproject(vec4 clip)
		{
			vec4 world = ClipToWorld * clip;
			return world.xyz / world.w;
		}

		float Fade(float distance)
		{
			if (FadeEnd <= 0.0 && FadeStart <= 0.0)
			{
				return 1.0;
			}

			float t = clamp((distance - FadeStart) / max(FadeEnd - FadeStart, 1e-6), 0.0, 1.0);
			return t * t * (3.0 - (2.0 * t));
		}

		void main()
		{
			float ndcX = (gl_FragCoord.x / TargetSize.x * 2.0) - 1.0;
			float ndcY = 1.0 - (gl_FragCoord.y / TargetSize.y * 2.0);
			vec3 nearPoint = Unproject(vec4(ndcX, ndcY, 0.0, 1.0));
			vec3 farPoint = Unproject(vec4(ndcX, ndcY, 1.0, 1.0));

			vec3 origin = (WorldToVolume * vec4(nearPoint, 1.0)).xyz;
			vec3 direction = (WorldToVolume * vec4(farPoint, 1.0)).xyz - origin;

			float enter = 0.0;
			float exit = 1.0;
			for (int axis = 0; axis < 3; axis++)
			{
				float o = origin[axis];
				float d = direction[axis];
				if (abs(d) < 1e-12)
				{
					if (o < 0.0 || o > 1.0)
					{
						discard;
					}

					continue;
				}

				float t0 = -o / d;
				float t1 = (1.0 - o) / d;
				enter = max(enter, min(t0, t1));
				exit = min(exit, max(t0, t1));
			}

			if (exit <= enter)
			{
				discard;
			}

			vec3 voxels = direction * (exit - enter) * VolumeSize;
			int samples = clamp(int(ceil(length(voxels) * SamplesPerVoxel)), 1, MaxSamples);
			float stepSize = (exit - enter) / float(samples);
			float worldStep = stepSize * distance(nearPoint, farPoint);

			float a = 0.06711056 * (floor(gl_FragCoord.x) + 0.5);
			float b = 0.00583715 * (floor(gl_FragCoord.y) + 0.5);
			float jitter = mod(52.9829189 * mod(a + b, 1.0), 1.0);

			vec3 light = vec3(0.0);
			float transmittance = 1.0;
			float column = 0.0;
			for (int i = 0; i < MaxSamples; i++)
			{
				if (i >= samples)
				{
					break;
				}

				float t = enter + ((float(i) + jitter) * stepSize);
				vec3 uvw = origin + (direction * t);
				// An explicit LOD: implicit derivatives are undefined inside this divergent loop, and the
				// textures have no mipmaps to choose between anyway.
				vec4 value = textureLod(Volume, uvw, 0.0);
				if (Channels == 1)
				{
					value = vec4(value.r, 0.0, 0.0, 0.0);
				}

				if (value == vec4(0.0))
				{
					continue;
				}

				if (HasDetail != 0)
				{
					float weight = DetailFadeHeight > 0.0 ? exp(-abs(uvw.z - 0.5) * PlaneScale / DetailFadeHeight) : 1.0;
					value *= max(vec4(0.0), vec4(1.0) + ((textureLod(DetailMap, vec3(uvw.xy, 0.5), 0.0) - vec4(1.0)) * weight));
				}

				vec3 world = mix(nearPoint, farPoint, t);
				float distanceToEye = distance(world, Eye);
				value *= max(vec4(0.0), vec4(1.0) + (NoiseWeights * (Grain(world, distanceToEye) - 1.0)));

				float fade = Fade(distanceToEye);
				vec3 emission = (Color.rgb * value.x) + (Color1.rgb * value.y) + (Color2.rgb * value.z) + (Color3.rgb * value.w);
				light += emission * (fade * transmittance * worldStep);
				column += value.x * fade * worldStep;
				transmittance *= exp(-dot(Extinction, value) * worldStep);
			}

			float alpha = Additive != 0 ? 1.0 : Premultiplied != 0 ? 1.0 - transmittance : max(clamp(Color.a * column, 0.0, 1.0), 1.0 - transmittance);
			if (light == vec3(0.0) && (Additive != 0 || alpha <= 0.0))
			{
				discard;
			}

			Out_Color = vec4(light, alpha);
		}";

	private readonly IGL3D gl = gl;
	private readonly Dictionary<nint, Target> targets = [];
	private readonly Dictionary<nint, Volume> volumes = [];

	private uint volumeProgram;
	private int volumeSamplerLocation;
	private int volumeClipToWorldLocation;
	private int volumeWorldToVolumeLocation;
	private int volumeEyeLocation;
	private int volumeColorLocation;
	private int volumeColor1Location;
	private int volumeColor2Location;
	private int volumeColor3Location;
	private int volumeExtinctionLocation;
	private int volumeNoiseWeightsLocation;
	private int volumeDetailMapLocation;
	private int volumeHasDetailLocation;
	private int volumeDetailFadeHeightLocation;
	private int volumePlaneScaleLocation;
	private int volumeChannelsLocation;
	private int volumeFadeStartLocation;
	private int volumeFadeEndLocation;
	private int volumeSamplesPerVoxelLocation;
	private int volumeMaxSamplesLocation;
	private int volumeSizeLocation;
	private int volumeTargetSizeLocation;
	private int volumeAdditiveLocation;
	private int volumePremultipliedLocation;
	private int volumeLowNoiseAmplitudeLocation;
	private int volumeLowNoiseScaleLocation;
	private int volumeHighNoiseAmplitudeLocation;
	private int volumeHighNoiseScaleLocation;
	private int volumeNoiseOffsetLocation;
	private int volumeNoiseStretchLocation;
	private int volumePixelAngleLocation;

	private uint toneMapProgram;
	private int toneMapSourceLocation;
	private int toneMapExposureLocation;
	private int toneMapPowerLocation;
	private int toneMapCurveLocation;
	private int toneMapPreserveHueLocation;
	private uint toneMapVertexArray;
	private uint volumeVertexArray;

	private uint program;
	private int matrixLocation;
	private int textureLocation;
	private int useTextureLocation;
	private uint vertexArray;
	private uint vertexBuffer;
	private uint indexBuffer;
	private bool disposed;

	/// <summary>Whether an id is a render target rather than a texture <c>CreateTexture</c> issued.</summary>
	/// <param name="id">The id to test.</param>
	/// <returns><see langword="true"/> if it belongs to a live render target.</returns>
	public bool IsRenderTarget(nint id) => targets.ContainsKey(id);

	/// <inheritdoc />
	public nint CreateRenderTarget(int width, int height, bool depth) =>
		CreateRenderTarget(width, height, depth, RenderTargetFormat.Rgba8);

	/// <inheritdoc />
	public bool SupportsFormat(RenderTargetFormat format) => format is RenderTargetFormat.Rgba8 or RenderTargetFormat.Rgba16F;

	/// <inheritdoc />
	/// <exception cref="NotSupportedException">The format is not one this renderer knows.</exception>
	public nint CreateRenderTarget(int width, int height, bool depth, RenderTargetFormat format)
	{
		ObjectDisposedException.ThrowIf(disposed, this);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
		GLEnum internalFormat = InternalFormatOf(format);

		int previousTexture = gl.GetInteger(GLEnum.TextureBinding2D);
		int previousRenderbuffer = gl.GetInteger(GLEnum.RenderbufferBinding);
		int previousDraw = gl.GetInteger(GLEnum.DrawFramebufferBinding);
		int previousRead = gl.GetInteger(GLEnum.ReadFramebufferBinding);

		uint color = gl.GenTexture();
		gl.BindTexture(GLEnum.Texture2D, color);
		gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Linear);
		gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
		gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
		gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
		gl.TexImage2DEmpty(GLEnum.Texture2D, internalFormat, width, height);

		uint renderbuffer = 0;
		if (depth)
		{
			renderbuffer = gl.GenRenderbuffer();
			gl.BindRenderbuffer(renderbuffer);
			gl.RenderbufferStorage(GLEnum.DepthComponent24, width, height);
		}

		uint framebuffer = gl.GenFramebuffer();
		gl.BindFramebuffer(GLEnum.Framebuffer, framebuffer);
		gl.FramebufferTexture2D(GLEnum.Framebuffer, GLEnum.ColorAttachment0, GLEnum.Texture2D, color);
		if (depth)
		{
			gl.FramebufferRenderbuffer(GLEnum.Framebuffer, GLEnum.DepthAttachment, renderbuffer);
		}

		GLEnum status = gl.CheckFramebufferStatus(GLEnum.Framebuffer);

		gl.BindFramebuffer(GLEnum.DrawFramebuffer, (uint)previousDraw);
		gl.BindFramebuffer(GLEnum.ReadFramebuffer, (uint)previousRead);
		gl.BindRenderbuffer((uint)previousRenderbuffer);
		gl.BindTexture(GLEnum.Texture2D, (uint)previousTexture);

		Target created = new(framebuffer, color, renderbuffer, width, height, internalFormat);
		if (status != GLEnum.FramebufferComplete)
		{
			Release(created);
			throw new InvalidOperationException($"The render target's framebuffer is incomplete ({status}).");
		}

		targets[(nint)color] = created;
		return (nint)color;
	}

	/// <inheritdoc />
	public bool ResizeRenderTarget(nint target, int width, int height)
	{
		ObjectDisposedException.ThrowIf(disposed, this);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

		if (!targets.TryGetValue(target, out Target? existing))
		{
			return false;
		}

		// Respecifying the attachments' storage keeps every name, so the texture id a caller is
		// already showing stays valid and the framebuffer stays attached to the same objects.
		int previousTexture = gl.GetInteger(GLEnum.TextureBinding2D);
		gl.BindTexture(GLEnum.Texture2D, existing.Color);
		gl.TexImage2DEmpty(GLEnum.Texture2D, existing.InternalFormat, width, height);
		gl.BindTexture(GLEnum.Texture2D, (uint)previousTexture);

		if (existing.Depth != 0)
		{
			int previousRenderbuffer = gl.GetInteger(GLEnum.RenderbufferBinding);
			gl.BindRenderbuffer(existing.Depth);
			gl.RenderbufferStorage(GLEnum.DepthComponent24, width, height);
			gl.BindRenderbuffer((uint)previousRenderbuffer);
		}

		targets[target] = existing with { Width = width, Height = height };
		return true;
	}

	/// <inheritdoc />
	/// <exception cref="ArgumentException">The handle is not a live render target, such as a texture from <c>CreateTexture</c>.</exception>
	public void DeleteRenderTarget(nint target)
	{
		if (!targets.Remove(target, out Target? existing))
		{
			throw new ArgumentException(
				$"{target} is not a render target. A texture from CreateTexture is released with DeleteTexture.", nameof(target));
		}

		Release(existing);
	}

	/// <inheritdoc />
	/// <exception cref="ArgumentException">The handle is not a live render target.</exception>
	public nint GetTargetTexture(nint target)
	{
		_ = Resolve(target);
		return target;
	}

	/// <inheritdoc />
	/// <exception cref="ArgumentException">The handle is not a live render target.</exception>
	public void Clear(nint target, Vector4 color, float depth)
	{
		ObjectDisposedException.ThrowIf(disposed, this);
		Target resolved = Resolve(target);

		SavedState saved = SavedState.Capture(gl);
		try
		{
			gl.BindFramebuffer(GLEnum.Framebuffer, resolved.Framebuffer);

			// A clear honours the scissor box and the write masks, either of which the frame may
			// have left set to something that would clear only part of the target or none of it.
			gl.Disable(GLEnum.ScissorTest);
			gl.ClearColor(color.X, color.Y, color.Z, color.W);

			ClearBufferMask mask = ClearBufferMask.ColorBufferBit;
			if (resolved.Depth != 0)
			{
				gl.DepthMask(true);
				gl.ClearDepth(depth);
				mask |= ClearBufferMask.DepthBufferBit;
			}

			gl.Clear(mask);
		}
		finally
		{
			saved.Restore(gl);
		}
	}

	/// <inheritdoc />
	/// <exception cref="ArgumentException">The handle is not a live render target, or the draw samples the target it draws into.</exception>
	/// <exception cref="ArgumentOutOfRangeException">An index is past the end of <paramref name="vertices"/>.</exception>
	public void Draw(nint target, ReadOnlySpan<Vertex3D> vertices, ReadOnlySpan<uint> indices, in DrawState3D state)
	{
		ObjectDisposedException.ThrowIf(disposed, this);
		Target resolved = Resolve(target);

		int stride = state.Topology == PrimitiveTopology.LineList ? 2 : 3;
		int count = indices.Length - (indices.Length % stride);
		if (count == 0)
		{
			return;
		}

		// One pass over the indices is cheap next to the upload that follows, and it turns what
		// would be a driver fault or a read of someone else's memory into a stack trace.
		uint highest = 0;
		foreach (uint index in indices[..count])
		{
			highest = Math.Max(highest, index);
		}

		if (highest >= (uint)vertices.Length)
		{
			throw new ArgumentOutOfRangeException(
				nameof(indices), highest, $"Index {highest} is past the end of a {vertices.Length}-vertex buffer.");
		}

		if (state.TextureId == target)
		{
			throw new ArgumentException("A draw cannot sample the render target it draws into.", nameof(state));
		}

		EnsureResources();

		SavedState saved = SavedState.Capture(gl);
		try
		{
			gl.BindFramebuffer(GLEnum.Framebuffer, resolved.Framebuffer);
			gl.Viewport(0, 0, resolved.Width, resolved.Height);
			gl.Disable(GLEnum.ScissorTest);
			ApplyDepth(state.Depth, resolved.Depth != 0);
			ApplyCull(state.Cull);
			ApplyBlend(state.Blend);

			gl.UseProgram(program);
			UploadMatrix(state.ModelViewProjection);
			gl.Uniform1(textureLocation, 0);
			gl.Uniform1(useTextureLocation, state.TextureId != 0 ? 1 : 0);

			gl.ActiveTexture(GLEnum.Texture0);
			gl.BindSampler(0, 0);
			gl.BindTexture(GLEnum.Texture2D, (uint)state.TextureId);

			gl.BindVertexArray(vertexArray);
			gl.BindBuffer(GLEnum.ArrayBuffer, vertexBuffer);
			gl.BufferData(GLEnum.ArrayBuffer, MemoryMarshal.AsBytes(vertices));
			gl.BindBuffer(GLEnum.ElementArrayBuffer, indexBuffer);
			gl.BufferData(GLEnum.ElementArrayBuffer, MemoryMarshal.AsBytes(indices[..count]));

			gl.DrawElements(state.Topology == PrimitiveTopology.LineList ? GLEnum.Lines : GLEnum.Triangles, count);
		}
		finally
		{
			saved.Restore(gl);
		}
	}

	/// <inheritdoc />
	/// <exception cref="ArgumentException">Either handle is not a live render target, or they are the same target.</exception>
	public void ToneMap(nint source, nint destination, in ToneMap3D toneMap)
	{
		ObjectDisposedException.ThrowIf(disposed, this);
		Target from = Resolve(source);
		Target to = Resolve(destination);
		if (source == destination)
		{
			throw new ArgumentException("A target cannot be tone-mapped into itself.", nameof(destination));
		}

		EnsureToneMapResources();

		SavedState saved = SavedState.Capture(gl);
		try
		{
			gl.BindFramebuffer(GLEnum.Framebuffer, to.Framebuffer);
			gl.Viewport(0, 0, to.Width, to.Height);
			gl.Disable(GLEnum.ScissorTest);
			gl.Disable(GLEnum.DepthTest);
			gl.Disable(GLEnum.CullFace);
			gl.Disable(GLEnum.Blend);

			gl.UseProgram(toneMapProgram);
			gl.Uniform1(toneMapSourceLocation, 0);
			gl.Uniform1(toneMapExposureLocation, toneMap.Exposure);
			gl.Uniform1(toneMapPowerLocation, toneMap.Power);
			gl.Uniform1(toneMapCurveLocation, (int)toneMap.Curve);
			gl.Uniform1(toneMapPreserveHueLocation, toneMap.PreserveHue ? 1 : 0);

			gl.ActiveTexture(GLEnum.Texture0);
			gl.BindSampler(0, 0);
			gl.BindTexture(GLEnum.Texture2D, from.Color);

			// A vertex array must be bound to draw in a core profile, even with no attributes.
			gl.BindVertexArray(toneMapVertexArray);
			gl.DrawArrays(GLEnum.Triangles, 0, 3);
		}
		finally
		{
			saved.Restore(gl);
		}
	}

	/// <summary>Gets whether this renderer supports volume textures: it does.</summary>
	public bool SupportsVolumes => true;

	/// <summary>Whether an id is a volume texture this renderer issued.</summary>
	/// <param name="id">The id to test.</param>
	/// <returns><see langword="true"/> if it belongs to a live volume.</returns>
	public bool IsVolume(nint id) => volumes.ContainsKey(id);

	/// <inheritdoc />
	/// <exception cref="ArgumentException">The span is the wrong length, or the channel count is not 1 or 4.</exception>
	public nint CreateVolumeTexture(int width, int height, int depth, int channels, ReadOnlySpan<Half> values)
	{
		ObjectDisposedException.ThrowIf(disposed, this);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(depth);
		if (channels is not (1 or 4))
		{
			throw new ArgumentException($"A volume has 1 or 4 channels, not {channels}.", nameof(channels));
		}

		if (values.Length != width * height * depth * channels)
		{
			throw new ArgumentException($"Expected {width * height * depth * channels} values, got {values.Length}.", nameof(values));
		}

		int previousTexture = gl.GetInteger(GLEnum.TextureBinding3D);
		int previousAlignment = gl.GetInteger(GLEnum.UnpackAlignment);

		uint texture = gl.GenTexture();
		gl.BindTexture(GLEnum.Texture3D, texture);
		gl.TexParameter(GLEnum.Texture3D, GLEnum.TextureMinFilter, (int)GLEnum.Linear);
		gl.TexParameter(GLEnum.Texture3D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
		gl.TexParameter(GLEnum.Texture3D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
		gl.TexParameter(GLEnum.Texture3D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
		gl.TexParameter(GLEnum.Texture3D, GLEnum.TextureWrapR, (int)GLEnum.ClampToEdge);

		// Rows of two-byte texels are not four-byte aligned when the width is odd.
		gl.PixelStore(GLEnum.UnpackAlignment, 1);
		gl.TexImage3D(
			GLEnum.Texture3D,
			channels == 1 ? GLEnum.R16f : GLEnum.Rgba16f,
			width,
			height,
			depth,
			channels == 1 ? GLEnum.Red : GLEnum.Rgba,
			GLEnum.HalfFloat,
			MemoryMarshal.AsBytes(values));
		gl.PixelStore(GLEnum.UnpackAlignment, previousAlignment);
		gl.BindTexture(GLEnum.Texture3D, (uint)previousTexture);

		volumes[(nint)texture] = new Volume(texture, width, height, depth, channels);
		return (nint)texture;
	}

	/// <inheritdoc />
	/// <exception cref="ArgumentException">The handle is not a live volume.</exception>
	public void DeleteVolumeTexture(nint volume)
	{
		if (!volumes.Remove(volume, out Volume? existing))
		{
			throw new ArgumentException($"{volume} is not a volume texture.", nameof(volume));
		}

		gl.DeleteTexture(existing.Texture);
	}

	/// <inheritdoc />
	/// <exception cref="ArgumentException">Either handle is unknown.</exception>
	public void DrawVolume(nint target, nint volume, in VolumeDrawState state)
	{
		ObjectDisposedException.ThrowIf(disposed, this);
		Target to = Resolve(target);
		if (!volumes.TryGetValue(volume, out Volume? from))
		{
			throw new ArgumentException($"No volume texture with handle {volume}.", nameof(volume));
		}

		Volume? detail = null;
		if (state.DetailMap != 0 && !volumes.TryGetValue(state.DetailMap, out detail))
		{
			throw new ArgumentException($"No volume texture with handle {state.DetailMap} for the detail map.", nameof(state));
		}

		if (!Matrix4x4.Invert(state.ViewProjection, out Matrix4x4 clipToWorld) || !Matrix4x4.Invert(state.VolumeToWorld, out Matrix4x4 worldToVolume))
		{
			return;
		}

		EnsureVolumeResources();

		SavedState saved = SavedState.Capture(gl);
		try
		{
			gl.BindFramebuffer(GLEnum.Framebuffer, to.Framebuffer);
			gl.Viewport(0, 0, to.Width, to.Height);
			gl.Disable(GLEnum.ScissorTest);
			gl.Disable(GLEnum.DepthTest);
			gl.Disable(GLEnum.CullFace);
			ApplyBlend(state.Blend);

			gl.UseProgram(volumeProgram);
			gl.Uniform1(volumeSamplerLocation, 0);
			gl.UniformMatrix4(volumeClipToWorldLocation, MemoryMarshal.CreateReadOnlySpan(ref clipToWorld.M11, 16));
			gl.UniformMatrix4(volumeWorldToVolumeLocation, MemoryMarshal.CreateReadOnlySpan(ref worldToVolume.M11, 16));
			gl.Uniform3(volumeEyeLocation, state.Eye.X, state.Eye.Y, state.Eye.Z);
			gl.Uniform4(volumeColorLocation, state.Color.X, state.Color.Y, state.Color.Z, state.Color.W);
			gl.Uniform4(volumeColor1Location, state.Color1.X, state.Color1.Y, state.Color1.Z, state.Color1.W);
			gl.Uniform4(volumeColor2Location, state.Color2.X, state.Color2.Y, state.Color2.Z, state.Color2.W);
			gl.Uniform4(volumeColor3Location, state.Color3.X, state.Color3.Y, state.Color3.Z, state.Color3.W);
			gl.Uniform4(volumeExtinctionLocation, state.Extinction.X, state.Extinction.Y, state.Extinction.Z, state.Extinction.W);
			Vector4 noiseWeights = state.EffectiveNoiseWeights;
			gl.Uniform4(volumeNoiseWeightsLocation, noiseWeights.X, noiseWeights.Y, noiseWeights.Z, noiseWeights.W);
			gl.Uniform1(volumeDetailMapLocation, 1);
			gl.Uniform1(volumeHasDetailLocation, detail is null ? 0 : 1);
			gl.Uniform1(volumeDetailFadeHeightLocation, state.DetailFadeHeight);
			gl.Uniform1(volumePlaneScaleLocation, new Vector3(state.VolumeToWorld.M31, state.VolumeToWorld.M32, state.VolumeToWorld.M33).Length());
			gl.Uniform1(volumeChannelsLocation, from.Channels);
			gl.Uniform1(volumeFadeStartLocation, state.FadeInStart);
			gl.Uniform1(volumeFadeEndLocation, state.FadeInEnd);
			gl.Uniform1(volumeSamplesPerVoxelLocation, state.EffectiveSamplesPerVoxel);
			gl.Uniform1(volumeMaxSamplesLocation, state.EffectiveMaxSamples);
			gl.Uniform3(volumeSizeLocation, from.Width, from.Height, from.Depth);
			gl.Uniform4(volumeTargetSizeLocation, to.Width, to.Height, 0f, 0f);
			gl.Uniform1(volumeAdditiveLocation, state.Blend == BlendMode.Additive ? 1 : 0);
			gl.Uniform1(volumePremultipliedLocation, state.Blend == BlendMode.Premultiplied ? 1 : 0);
			gl.Uniform1(volumeLowNoiseAmplitudeLocation, state.LowNoiseAmplitude);
			gl.Uniform1(volumeLowNoiseScaleLocation, state.LowNoiseScale);
			gl.Uniform1(volumeHighNoiseAmplitudeLocation, state.HighNoiseAmplitude);
			gl.Uniform1(volumeHighNoiseScaleLocation, state.HighNoiseScale);
			gl.Uniform3(volumeNoiseOffsetLocation, state.NoiseOffset.X, state.NoiseOffset.Y, state.NoiseOffset.Z);
			Vector3 noiseStretch = state.EffectiveNoiseStretch;
			gl.Uniform3(volumeNoiseStretchLocation, noiseStretch.X, noiseStretch.Y, noiseStretch.Z);
			gl.Uniform1(volumePixelAngleLocation, state.PixelAngle);

			gl.ActiveTexture(GLEnum.Texture0);
			gl.BindSampler(0, 0);
			gl.BindTexture(GLEnum.Texture3D, from.Texture);
			if (detail is not null)
			{
				gl.ActiveTexture(GLEnum.Texture1);
				gl.BindSampler(1, 0);
				gl.BindTexture(GLEnum.Texture3D, detail.Texture);
				gl.ActiveTexture(GLEnum.Texture0);
			}

			gl.BindVertexArray(volumeVertexArray);
			gl.DrawArrays(GLEnum.Triangles, 0, 3);
		}
		finally
		{
			saved.Restore(gl);
		}
	}

	/// <summary>Releases every target and the programs, buffers and vertex arrays behind them.</summary>
	public void Dispose()
	{
		if (disposed)
		{
			return;
		}

		foreach (Target target in targets.Values)
		{
			Release(target);
		}

		targets.Clear();

		foreach (Volume volume in volumes.Values)
		{
			gl.DeleteTexture(volume.Texture);
		}

		volumes.Clear();

		if (volumeProgram != 0)
		{
			gl.DeleteProgram(volumeProgram);
			gl.DeleteVertexArray(volumeVertexArray);
		}

		if (program != 0)
		{
			gl.DeleteProgram(program);
			gl.DeleteVertexArray(vertexArray);
			gl.DeleteBuffer(vertexBuffer);
			gl.DeleteBuffer(indexBuffer);
		}

		if (toneMapProgram != 0)
		{
			gl.DeleteProgram(toneMapProgram);
			gl.DeleteVertexArray(toneMapVertexArray);
		}

		disposed = true;
	}

	private static GLEnum InternalFormatOf(RenderTargetFormat format) => format switch
	{
		RenderTargetFormat.Rgba8 => GLEnum.Rgba8,
		RenderTargetFormat.Rgba16F => GLEnum.Rgba16f,
		_ => throw new NotSupportedException($"No OpenGL storage for {format}."),
	};

	/// <summary>Creates the volume program and its empty vertex array the first time a volume is drawn.</summary>
	private void EnsureVolumeResources()
	{
		if (volumeProgram != 0)
		{
			return;
		}

		volumeProgram = gl.CreateProgram(ToneMapVertexSource, VolumeFragmentSource);
		volumeSamplerLocation = gl.GetUniformLocation(volumeProgram, "Volume");
		volumeClipToWorldLocation = gl.GetUniformLocation(volumeProgram, "ClipToWorld");
		volumeWorldToVolumeLocation = gl.GetUniformLocation(volumeProgram, "WorldToVolume");
		volumeEyeLocation = gl.GetUniformLocation(volumeProgram, "Eye");
		volumeColorLocation = gl.GetUniformLocation(volumeProgram, "Color");
		volumeColor1Location = gl.GetUniformLocation(volumeProgram, "Color1");
		volumeColor2Location = gl.GetUniformLocation(volumeProgram, "Color2");
		volumeColor3Location = gl.GetUniformLocation(volumeProgram, "Color3");
		volumeExtinctionLocation = gl.GetUniformLocation(volumeProgram, "Extinction");
		volumeChannelsLocation = gl.GetUniformLocation(volumeProgram, "Channels");
		volumeNoiseWeightsLocation = gl.GetUniformLocation(volumeProgram, "NoiseWeights");
		volumeDetailMapLocation = gl.GetUniformLocation(volumeProgram, "DetailMap");
		volumeHasDetailLocation = gl.GetUniformLocation(volumeProgram, "HasDetail");
		volumeDetailFadeHeightLocation = gl.GetUniformLocation(volumeProgram, "DetailFadeHeight");
		volumePlaneScaleLocation = gl.GetUniformLocation(volumeProgram, "PlaneScale");
		volumeFadeStartLocation = gl.GetUniformLocation(volumeProgram, "FadeStart");
		volumeFadeEndLocation = gl.GetUniformLocation(volumeProgram, "FadeEnd");
		volumeSamplesPerVoxelLocation = gl.GetUniformLocation(volumeProgram, "SamplesPerVoxel");
		volumeMaxSamplesLocation = gl.GetUniformLocation(volumeProgram, "MaxSamples");
		volumeSizeLocation = gl.GetUniformLocation(volumeProgram, "VolumeSize");
		volumeTargetSizeLocation = gl.GetUniformLocation(volumeProgram, "TargetSize");
		volumeAdditiveLocation = gl.GetUniformLocation(volumeProgram, "Additive");
		volumePremultipliedLocation = gl.GetUniformLocation(volumeProgram, "Premultiplied");
		volumeLowNoiseAmplitudeLocation = gl.GetUniformLocation(volumeProgram, "LowNoiseAmplitude");
		volumeLowNoiseScaleLocation = gl.GetUniformLocation(volumeProgram, "LowNoiseScale");
		volumeHighNoiseAmplitudeLocation = gl.GetUniformLocation(volumeProgram, "HighNoiseAmplitude");
		volumeHighNoiseScaleLocation = gl.GetUniformLocation(volumeProgram, "HighNoiseScale");
		volumeNoiseOffsetLocation = gl.GetUniformLocation(volumeProgram, "NoiseOffset");
		volumeNoiseStretchLocation = gl.GetUniformLocation(volumeProgram, "NoiseStretch");
		volumePixelAngleLocation = gl.GetUniformLocation(volumeProgram, "PixelAngle");
		volumeVertexArray = gl.GenVertexArray();
	}

	/// <summary>Creates the tone-map program and its empty vertex array the first time a tone map is asked for.</summary>
	private void EnsureToneMapResources()
	{
		if (toneMapProgram != 0)
		{
			return;
		}

		toneMapProgram = gl.CreateProgram(ToneMapVertexSource, ToneMapFragmentSource);
		toneMapSourceLocation = gl.GetUniformLocation(toneMapProgram, "Source");
		toneMapExposureLocation = gl.GetUniformLocation(toneMapProgram, "Exposure");
		toneMapPowerLocation = gl.GetUniformLocation(toneMapProgram, "Power");
		toneMapCurveLocation = gl.GetUniformLocation(toneMapProgram, "Curve");
		toneMapPreserveHueLocation = gl.GetUniformLocation(toneMapProgram, "PreserveHue");
		toneMapVertexArray = gl.GenVertexArray();
	}

	/// <summary>
	/// Creates the program, the vertex array and its buffers the first time something is drawn,
	/// so an application that never draws in 3D never compiles a shader for it.
	/// </summary>
	private void EnsureResources()
	{
		if (program != 0)
		{
			return;
		}

		program = gl.CreateProgram(VertexSource, FragmentSource);
		matrixLocation = gl.GetUniformLocation(program, "ModelViewProjection");
		textureLocation = gl.GetUniformLocation(program, "Texture");
		useTextureLocation = gl.GetUniformLocation(program, "UseTexture");

		vertexBuffer = gl.GenBuffer();
		indexBuffer = gl.GenBuffer();
		vertexArray = gl.GenVertexArray();

		int previousVertexArray = gl.GetInteger(GLEnum.VertexArrayBinding);
		int previousArrayBuffer = gl.GetInteger(GLEnum.ArrayBufferBinding);

		// The layout is recorded into the vertex array once; every draw only rebinds it.
		int vertexSize = Unsafe.SizeOf<Vertex3D>();
		gl.BindVertexArray(vertexArray);
		gl.BindBuffer(GLEnum.ArrayBuffer, vertexBuffer);
		gl.EnableVertexAttribArray(0);
		gl.EnableVertexAttribArray(1);
		gl.EnableVertexAttribArray(2);
		gl.VertexAttribPointer(0, 3, GLEnum.Float, false, vertexSize, PositionOffset);
		gl.VertexAttribPointer(1, 2, GLEnum.Float, false, vertexSize, UvOffset);
		gl.VertexAttribPointer(2, 4, GLEnum.UnsignedByte, true, vertexSize, ColorOffset);

		gl.BindVertexArray((uint)previousVertexArray);
		gl.BindBuffer(GLEnum.ArrayBuffer, (uint)previousArrayBuffer);
	}

	/// <summary>Byte offset of <see cref="Vertex3D.Position"/>: a <see cref="Vector3"/> first.</summary>
	internal const int PositionOffset = 0;

	/// <summary>Byte offset of <see cref="Vertex3D.Uv"/>: after three floats.</summary>
	internal const int UvOffset = 12;

	/// <summary>Byte offset of <see cref="Vertex3D.Color"/>: after five floats, packed R in the low byte.</summary>
	internal const int ColorOffset = 20;

	private void ApplyDepth(DepthMode mode, bool hasDepth)
	{
		// Without a depth attachment every mode behaves as None, which is what the contract says.
		if (!hasDepth || mode == DepthMode.None)
		{
			gl.Disable(GLEnum.DepthTest);
			return;
		}

		gl.Enable(GLEnum.DepthTest);
		gl.DepthFunc(GLEnum.Less);
		gl.DepthMask(mode == DepthMode.TestAndWrite);
	}

	private void ApplyCull(CullMode mode)
	{
		if (mode == CullMode.None)
		{
			gl.Disable(GLEnum.CullFace);
			return;
		}

		gl.Enable(GLEnum.CullFace);
		gl.FrontFace(GLEnum.CW);
		gl.CullFace(mode == CullMode.Back ? GLEnum.Back : GLEnum.Front);
	}

	private void ApplyBlend(BlendMode mode)
	{
		if (mode == BlendMode.Opaque)
		{
			gl.Disable(GLEnum.Blend);
			return;
		}

		gl.Enable(GLEnum.Blend);
		gl.BlendEquationSeparate(GLEnum.FuncAdd, GLEnum.FuncAdd);
		if (mode == BlendMode.Additive)
		{
			// Colour accumulates scaled by its alpha; alpha accumulates as coverage.
			gl.BlendFuncSeparate(GLEnum.SrcAlpha, GLEnum.One, GLEnum.One, GLEnum.One);
			return;
		}

		if (mode == BlendMode.Premultiplied)
		{
			// The source colour is already weighted; the destination is dimmed by what the source covers.
			gl.BlendFuncSeparate(GLEnum.One, GLEnum.OneMinusSrcAlpha, GLEnum.One, GLEnum.OneMinusSrcAlpha);
			return;
		}

		gl.BlendFuncSeparate(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha, GLEnum.One, GLEnum.OneMinusSrcAlpha);
	}

	/// <summary>
	/// Uploads the matrix in memory order, untransposed.
	/// </summary>
	/// <remarks>
	/// <see cref="Matrix4x4"/> is row-vector (<c>v * M</c>) and stored row-major; OpenGL reads those
	/// sixteen floats column-major, which is <c>Mᵀ</c>, and <c>Mᵀ * v</c> in the shader is the same
	/// product. Transposing here would be the bug.
	/// </remarks>
	private void UploadMatrix(Matrix4x4 matrix)
	{
		ReadOnlySpan<float> values = MemoryMarshal.CreateReadOnlySpan(ref matrix.M11, 16);
		gl.UniformMatrix4(matrixLocation, values);
	}

	private Target Resolve(nint target) =>
		targets.TryGetValue(target, out Target? existing)
			? existing
			: throw new ArgumentException($"No render target with handle {target}.", nameof(target));

	private void Release(Target target)
	{
		gl.DeleteFramebuffer(target.Framebuffer);
		if (target.Depth != 0)
		{
			gl.DeleteRenderbuffer(target.Depth);
		}

		gl.DeleteTexture(target.Color);
	}

	/// <summary>
	/// A framebuffer, its colour texture and its optional depth renderbuffer (zero when absent), and
	/// the colour texture's storage format, which a resize respecifies it with.
	/// </summary>
	private sealed record Target(uint Framebuffer, uint Color, uint Depth, int Width, int Height, GLEnum InternalFormat);

	/// <summary>A volume texture and its size in voxels.</summary>
	private sealed record Volume(uint Texture, int Width, int Height, int Depth, int Channels);

	/// <summary>
	/// Everything <see cref="Clear"/> and <see cref="Draw"/> may change, captured beforehand and put
	/// back afterwards. Beyond the set ImGui's own renderer restores, that is the bound framebuffers,
	/// the depth test, depth mask and function, face culling and its mode and winding, and the clear
	/// values.
	/// </summary>
	private readonly struct SavedState
	{
		private readonly int drawFramebuffer;
		private readonly int readFramebuffer;
		private readonly int program;
		private readonly int activeTexture;
		private readonly int texture;
		private readonly int texture3D;
		private readonly int sampler;
		private readonly int texture3DUnit1;
		private readonly int samplerUnit1;
		private readonly int vertexArray;
		private readonly int arrayBuffer;
		private readonly int depthMask;
		private readonly int depthFunc;
		private readonly int cullFaceMode;
		private readonly int frontFace;
		private readonly int blendEquationRgb;
		private readonly int blendEquationAlpha;
		private readonly int blendSrcRgb;
		private readonly int blendDstRgb;
		private readonly int blendSrcAlpha;
		private readonly int blendDstAlpha;
		private readonly bool depthTest;
		private readonly bool cullFace;
		private readonly bool blend;
		private readonly bool scissorTest;
		private readonly int[] viewport;
		private readonly float[] clearColor;
		private readonly float[] clearDepth;

		private SavedState(IGL3D gl)
		{
			drawFramebuffer = gl.GetInteger(GLEnum.DrawFramebufferBinding);
			readFramebuffer = gl.GetInteger(GLEnum.ReadFramebufferBinding);
			program = gl.GetInteger(GLEnum.CurrentProgram);
			activeTexture = gl.GetInteger(GLEnum.ActiveTexture);

			// The texture and sampler bindings are per unit; this renderer uses unit 0, and unit 1 for a
			// volume's detail map.
			gl.ActiveTexture(GLEnum.Texture1);
			texture3DUnit1 = gl.GetInteger(GLEnum.TextureBinding3D);
			samplerUnit1 = gl.GetInteger(GLEnum.SamplerBinding);
			gl.ActiveTexture(GLEnum.Texture0);
			texture = gl.GetInteger(GLEnum.TextureBinding2D);
			texture3D = gl.GetInteger(GLEnum.TextureBinding3D);
			sampler = gl.GetInteger(GLEnum.SamplerBinding);

			vertexArray = gl.GetInteger(GLEnum.VertexArrayBinding);
			arrayBuffer = gl.GetInteger(GLEnum.ArrayBufferBinding);
			depthMask = gl.GetInteger(GLEnum.DepthWritemask);
			depthFunc = gl.GetInteger(GLEnum.DepthFunc);
			cullFaceMode = gl.GetInteger(GLEnum.CullFaceMode);
			frontFace = gl.GetInteger(GLEnum.FrontFace);
			blendEquationRgb = gl.GetInteger(GLEnum.BlendEquationRgb);
			blendEquationAlpha = gl.GetInteger(GLEnum.BlendEquationAlpha);
			blendSrcRgb = gl.GetInteger(GLEnum.BlendSrcRgb);
			blendDstRgb = gl.GetInteger(GLEnum.BlendDstRgb);
			blendSrcAlpha = gl.GetInteger(GLEnum.BlendSrcAlpha);
			blendDstAlpha = gl.GetInteger(GLEnum.BlendDstAlpha);
			depthTest = gl.IsEnabled(GLEnum.DepthTest);
			cullFace = gl.IsEnabled(GLEnum.CullFace);
			blend = gl.IsEnabled(GLEnum.Blend);
			scissorTest = gl.IsEnabled(GLEnum.ScissorTest);

			viewport = new int[4];
			gl.GetInteger(GLEnum.Viewport, viewport);
			clearColor = new float[4];
			gl.GetFloat(GLEnum.ColorClearValue, clearColor);
			clearDepth = new float[1];
			gl.GetFloat(GLEnum.DepthClearValue, clearDepth);
		}

		public static SavedState Capture(IGL3D gl) => new(gl);

		public void Restore(IGL3D gl)
		{
			gl.BindFramebuffer(GLEnum.DrawFramebuffer, (uint)drawFramebuffer);
			gl.BindFramebuffer(GLEnum.ReadFramebuffer, (uint)readFramebuffer);
			gl.UseProgram((uint)program);

			gl.ActiveTexture(GLEnum.Texture1);
			gl.BindTexture(GLEnum.Texture3D, (uint)texture3DUnit1);
			gl.BindSampler(1, (uint)samplerUnit1);
			gl.ActiveTexture(GLEnum.Texture0);
			gl.BindTexture(GLEnum.Texture2D, (uint)texture);
			gl.BindTexture(GLEnum.Texture3D, (uint)texture3D);
			gl.BindSampler(0, (uint)sampler);
			gl.ActiveTexture((GLEnum)activeTexture);

			gl.BindVertexArray((uint)vertexArray);
			gl.BindBuffer(GLEnum.ArrayBuffer, (uint)arrayBuffer);
			gl.DepthMask(depthMask != 0);
			gl.DepthFunc((GLEnum)depthFunc);
			gl.CullFace((GLEnum)cullFaceMode);
			gl.FrontFace((GLEnum)frontFace);
			gl.BlendEquationSeparate((GLEnum)blendEquationRgb, (GLEnum)blendEquationAlpha);
			gl.BlendFuncSeparate((GLEnum)blendSrcRgb, (GLEnum)blendDstRgb, (GLEnum)blendSrcAlpha, (GLEnum)blendDstAlpha);
			SetCapability(gl, GLEnum.DepthTest, depthTest);
			SetCapability(gl, GLEnum.CullFace, cullFace);
			SetCapability(gl, GLEnum.Blend, blend);
			SetCapability(gl, GLEnum.ScissorTest, scissorTest);
			gl.Viewport(viewport[0], viewport[1], viewport[2], viewport[3]);
			gl.ClearColor(clearColor[0], clearColor[1], clearColor[2], clearColor[3]);
			gl.ClearDepth(clearDepth[0]);
		}

		private static void SetCapability(IGL3D gl, GLEnum capability, bool enabled)
		{
			if (enabled)
			{
				gl.Enable(capability);
			}
			else
			{
				gl.Disable(capability);
			}
		}
	}
}
