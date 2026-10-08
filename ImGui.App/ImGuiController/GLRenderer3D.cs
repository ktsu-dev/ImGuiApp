// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController;

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

	private readonly IGL3D gl = gl;
	private readonly Dictionary<nint, Target> targets = [];

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
	public nint CreateRenderTarget(int width, int height, bool depth)
	{
		ObjectDisposedException.ThrowIf(disposed, this);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

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
		gl.TexImage2DEmpty(GLEnum.Texture2D, GLEnum.Rgba8, width, height);

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

		if (status != GLEnum.FramebufferComplete)
		{
			Release(new Target(framebuffer, color, renderbuffer, width, height));
			throw new InvalidOperationException($"The render target's framebuffer is incomplete ({status}).");
		}

		targets[(nint)color] = new Target(framebuffer, color, renderbuffer, width, height);
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
		gl.TexImage2DEmpty(GLEnum.Texture2D, GLEnum.Rgba8, width, height);
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

	/// <summary>Releases every target and the program, buffers and vertex array behind them.</summary>
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

		if (program != 0)
		{
			gl.DeleteProgram(program);
			gl.DeleteVertexArray(vertexArray);
			gl.DeleteBuffer(vertexBuffer);
			gl.DeleteBuffer(indexBuffer);
		}

		disposed = true;
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

	/// <summary>A framebuffer, its colour texture and its optional depth renderbuffer (zero when absent).</summary>
	private sealed record Target(uint Framebuffer, uint Color, uint Depth, int Width, int Height);

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
		private readonly int sampler;
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

			// The texture and sampler bindings are per unit; this renderer only ever uses unit 0.
			gl.ActiveTexture(GLEnum.Texture0);
			texture = gl.GetInteger(GLEnum.TextureBinding2D);
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

			gl.ActiveTexture(GLEnum.Texture0);
			gl.BindTexture(GLEnum.Texture2D, (uint)texture);
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
