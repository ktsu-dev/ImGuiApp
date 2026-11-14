// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Rendering;

using System.Numerics;

/// <summary>How a draw interacts with the render target's depth attachment.</summary>
public enum DepthMode
{
	/// <summary>No depth test and no depth write. Draws in submission order.</summary>
	None,

	/// <summary>Tested against the depth buffer, but does not write to it. For transparent geometry.</summary>
	Test,

	/// <summary>Tested and written. The usual mode for opaque geometry.</summary>
	TestAndWrite,
}

/// <summary>Which triangle facings are discarded before rasterization.</summary>
public enum CullMode
{
	/// <summary>Both facings are drawn.</summary>
	None,

	/// <summary>Back faces are discarded. Front faces are counter-clockwise in target space.</summary>
	Back,

	/// <summary>Front faces are discarded.</summary>
	Front,
}

/// <summary>How a draw's output is combined with what is already in the target.</summary>
public enum BlendMode
{
	/// <summary>The source replaces the destination, alpha included.</summary>
	Opaque,

	/// <summary>Straight-alpha source-over, the same compositing ImGui's own draws use.</summary>
	Alpha,

	/// <summary>
	/// The source, scaled by its alpha, is added to the destination. Order-independent, so
	/// overlapping glows, particles or point sprites accumulate the way light does. On an
	/// <see cref="RenderTargetFormat.Rgba8"/> target the sum saturates at 1; draw into an
	/// <see cref="RenderTargetFormat.Rgba16F"/> target and tone-map it with
	/// <see cref="IRenderer3D.ToneMap"/> to keep the range.
	/// </summary>
	Additive,
}

/// <summary>The storage a render target's colour attachment uses.</summary>
public enum RenderTargetFormat
{
	/// <summary>Eight bits per channel, values in [0, 1]. What every renderer supports, and what ImGui shows directly.</summary>
	Rgba8,

	/// <summary>
	/// Sixteen-bit floating point per channel. Holds values above 1 and keeps faint contributions
	/// that eight bits would round away, which is what many overlapping, mostly transparent draws
	/// need. Not meant to be shown as it is: tone-map it into an <see cref="Rgba8"/> target with
	/// <see cref="IRenderer3D.ToneMap"/> first.
	/// </summary>
	Rgba16F,
}

/// <summary>The curve <see cref="ToneMap3D"/> compresses exposed values into [0, 1] with.</summary>
public enum ToneCurve
{
	/// <summary>Values above 1 are cut off. Exact below 1, so an image with no highlights passes through unchanged.</summary>
	Clamp,

	/// <summary><c>v / (1 + v)</c>. Never saturates; highlights compress gently and mid-tones darken a little.</summary>
	Reinhard,

	/// <summary><c>1 - exp(-v)</c>. Linear near black and saturates smoothly, like film or a photographic exposure.</summary>
	Exponential,
}

/// <summary>
/// How <see cref="IRenderer3D.ToneMap"/> turns a high-range target's values into displayable ones.
/// </summary>
/// <param name="Exposure">Multiplies every colour channel first.</param>
/// <param name="Power">Raises the exposed value to this power before the curve: below 1 lifts faint detail, above 1 deepens contrast. Must be positive.</param>
/// <param name="Curve">Compresses the result into [0, 1].</param>
/// <param name="PreserveHue">
/// When <see langword="true"/>, the mapping is applied to the brightest channel and the other two are
/// scaled with it, so a colour keeps its hue and saturation however bright it is. Per channel, a
/// bright warm colour desaturates towards white, because every channel saturates separately.
/// </param>
/// <remarks>
/// Applied in the order exposure, power, curve; alpha is only clamped. The arithmetic is
/// <see cref="Apply"/>, which every renderer's implementation must match, so a test can state the
/// expected pixel without a graphics context.
/// </remarks>
public readonly record struct ToneMap3D(float Exposure, float Power, ToneCurve Curve, bool PreserveHue = false)
{
	/// <summary>Gets the identity mapping for values already in range: exposure 1, power 1, clamped.</summary>
	public static ToneMap3D Default { get; } = new(1f, 1f, ToneCurve.Clamp);

	/// <summary>Maps one colour.</summary>
	/// <param name="color">The high-range colour, straight alpha.</param>
	/// <returns>The displayable colour, every channel in [0, 1].</returns>
	public Vector4 Apply(Vector4 color)
	{
		float alpha = Math.Clamp(color.W, 0f, 1f);
		if (!PreserveHue)
		{
			return new(Channel(color.X), Channel(color.Y), Channel(color.Z), alpha);
		}

		// The brightest channel is mapped and the others follow it, which keeps their ratios and so the hue.
		Vector3 rgb = Vector3.Max(new Vector3(color.X, color.Y, color.Z), Vector3.Zero);
		float peak = MathF.Max(rgb.X, MathF.Max(rgb.Y, rgb.Z));
		if (peak <= 0f)
		{
			return new(0f, 0f, 0f, alpha);
		}

		Vector3 mapped = Vector3.Min(rgb * (Channel(peak) / peak), Vector3.One);
		return new(mapped, alpha);
	}

	/// <summary>Maps one colour channel.</summary>
	/// <param name="value">The high-range value.</param>
	/// <returns>The displayable value in [0, 1].</returns>
	public float Channel(float value)
	{
		float exposed = MathF.Max(value, 0f) * Exposure;
		if (Power != 1f && exposed > 0f)
		{
			exposed = MathF.Pow(exposed, Power);
		}

		float curved = Curve switch
		{
			ToneCurve.Reinhard => exposed / (1f + exposed),
			ToneCurve.Exponential => 1f - MathF.Exp(-exposed),
			_ => exposed,
		};

		return Math.Clamp(curved, 0f, 1f);
	}
}

/// <summary>What the index buffer describes.</summary>
public enum PrimitiveTopology
{
	/// <summary>Every three indices are one triangle.</summary>
	TriangleList,

	/// <summary>Every two indices are one line segment.</summary>
	LineList,
}

/// <summary>
/// A vertex for <see cref="IRenderer3D"/>.
/// </summary>
/// <param name="Position">Position in model space. The draw's matrix takes it to clip space.</param>
/// <param name="Uv">Texture coordinate, normalized. Ignored when the draw has no texture.</param>
/// <param name="Color">Vertex color, straight alpha, packed as ImGui packs one: R in the low byte.</param>
/// <remarks>
/// <strong>There is no normal, because there is no lighting.</strong> The caller bakes shading into
/// <paramref name="Color"/>. A caller who wants real lighting wants shaders, and shaders are the
/// one thing that cannot be made backend-agnostic without inventing a shading language. Per-vertex
/// Lambert on the CPU is a few lines and costs nothing at these vertex counts. If that proves
/// insufficient, a single directional light on <see cref="DrawState3D"/> against a normal added
/// here is the smallest next step and does not break this surface.
/// </remarks>
public readonly record struct Vertex3D(Vector3 Position, Vector2 Uv, uint Color);

/// <summary>
/// Everything about a draw that is not its vertices.
/// </summary>
/// <remarks>
/// A record struct with init-only properties rather than a parameter list, so a caller sets the two
/// or three that matter and takes the defaults for the rest — which are chosen to be the ordinary
/// case: opaque, depth-tested, back-face-culled triangles with no texture.
/// </remarks>
public readonly record struct DrawState3D
{
	/// <summary>Gets the matrix taking <see cref="Vertex3D.Position"/> from model space to clip space.</summary>
	public Matrix4x4 ModelViewProjection { get; init; }

	/// <summary>
	/// Gets the texture to modulate the vertex color by, or zero for vertex color alone.
	/// </summary>
	/// <remarks>
	/// A handle from <c>IRendererBackend.CreateTexture</c>, not from
	/// <see cref="IRenderer3D.GetTargetTexture"/> — see the remarks on that member.
	/// </remarks>
	public nint TextureId { get; init; }

	/// <summary>Gets how the draw interacts with the depth attachment.</summary>
	public DepthMode Depth { get; init; }

	/// <summary>Gets which facings are discarded.</summary>
	public CullMode Cull { get; init; }

	/// <summary>Gets how the output is combined with the target.</summary>
	public BlendMode Blend { get; init; }

	/// <summary>Gets what the index buffer describes.</summary>
	public PrimitiveTopology Topology { get; init; }
}

/// <summary>
/// Everything about an emissive volume pass that is not the volume itself.
/// </summary>
/// <remarks>
/// <para>
/// The pass casts a ray through every pixel of the target, from the near plane to the far plane of
/// <see cref="ViewProjection"/>, intersects it with the volume's box, and adds
/// <c>Color · ∫ value · ds</c> along it: the texture's value integrated over world distance, with the
/// value sampled trilinearly. Emission only, with no absorption, which is what a glowing medium such
/// as unresolved stars or gas lit from within looks like.
/// </para>
/// <para>
/// The number of samples is set by how many voxels the ray crosses, and each pixel starts its samples
/// at a hashed offset, so there is no banding from a fixed step and no step count to tune per scene.
/// </para>
/// </remarks>
public readonly record struct VolumeDrawState
{
	/// <summary>Gets the same world-to-clip matrix a <see cref="DrawState3D"/> would carry; its inverse turns pixels into rays.</summary>
	public Matrix4x4 ViewProjection { get; init; }

	/// <summary>Gets the matrix placing the volume's unit cube ([0, 1] on each axis, texture coordinates) in the world.</summary>
	public Matrix4x4 VolumeToWorld { get; init; }

	/// <summary>Gets the eye position in world space, which <see cref="FadeInStart"/> and <see cref="FadeInEnd"/> measure from.</summary>
	public Vector3 Eye { get; init; }

	/// <summary>
	/// Gets the colour channel 0 emits per unit of value per unit of world distance. Not clamped: the
	/// result is meant for a float target. A single-channel volume uses only this.
	/// </summary>
	public Vector4 Color { get; init; }

	/// <summary>Gets the colour channel 1 of a four-channel volume emits, as <see cref="Color"/>.</summary>
	public Vector4 Color1 { get; init; }

	/// <summary>Gets the colour channel 2 of a four-channel volume emits, as <see cref="Color"/>.</summary>
	public Vector4 Color2 { get; init; }

	/// <summary>Gets the colour channel 3 of a four-channel volume emits, as <see cref="Color"/>.</summary>
	public Vector4 Color3 { get; init; }

	/// <summary>
	/// Gets how strongly each channel absorbs light, per unit of value per unit of world distance:
	/// along the ray, the light from farther away is dimmed by exp(−∫ extinction · value · ds) in front
	/// of it, so a dust channel draws dark lanes over the glow behind it. Zero: emission only.
	/// </summary>
	public Vector4 Extinction { get; init; }

	/// <summary>
	/// Gets the distance from <see cref="Eye"/> within which the volume emits nothing. Emission rises
	/// smoothly to full at <see cref="FadeInEnd"/>, so whatever the caller draws near the eye (such as
	/// individual stars) can take over from the volume without counting its light twice. Both zero:
	/// no fade.
	/// </summary>
	public float FadeInStart { get; init; }

	/// <summary>Gets the distance from <see cref="Eye"/> at which the volume reaches full emission.</summary>
	public float FadeInEnd { get; init; }

	/// <summary>Gets how many samples are taken per voxel crossed; zero means 2.</summary>
	public float SamplesPerVoxel { get; init; }

	/// <summary>Gets the most samples one ray takes; zero means 1024.</summary>
	public int MaxSamples { get; init; }

	/// <summary>Gets how the result is combined with the target; normally <see cref="BlendMode.Additive"/>.</summary>
	public BlendMode Blend { get; init; }

	/// <summary>
	/// Gets how strongly the low-frequency noise layer modulates the emission: each sample is
	/// multiplied by 1 + amplitude · (2n − 1) for a noise value n in [0, 1), so 0 is none and 1 swings
	/// from nothing to double. Large clumps and lanes; the two layers multiply.
	/// </summary>
	public float LowNoiseAmplitude { get; init; }

	/// <summary>Gets the low-frequency layer's cell size in world units.</summary>
	public float LowNoiseScale { get; init; }

	/// <summary>Gets how strongly the high-frequency noise layer modulates the emission: fine grain, as <see cref="LowNoiseAmplitude"/>.</summary>
	public float HighNoiseAmplitude { get; init; }

	/// <summary>Gets the high-frequency layer's cell size in world units.</summary>
	public float HighNoiseScale { get; init; }

	/// <summary>
	/// Gets the offset added to world positions before the noise is evaluated, so a caller drawing in
	/// a moving, camera-relative space can keep the noise fixed to its own world.
	/// </summary>
	public Vector3 NoiseOffset { get; init; }

	/// <summary>
	/// Gets the world size of one pixel per unit of distance from the eye (2·tan(fov/2) / height). Each
	/// noise octave fades out where its cells shrink below a few pixels, so distant noise does not
	/// shimmer. Zero: no fade.
	/// </summary>
	public float PixelAngle { get; init; }

	/// <summary>
	/// Gets how strongly the noise modulates each channel: a channel's value is multiplied by
	/// max(0, 1 + weight · (grain − 1)), so 0 leaves it smooth, 1 applies the noise as it is and more
	/// exaggerates it, as for gas and dust that are clumpier than starlight. Zero in every channel
	/// means 1 in every channel.
	/// </summary>
	public Vector4 NoiseWeights { get; init; }

	/// <summary>
	/// Gets an optional detail map: a volume texture (normally one voxel deep) covering the volume's XY
	/// face, whose channels multiply the volume's, sampled at the same (u, v). It carries structure in a
	/// plane (a galaxy's disk, a cloud layer) at a far finer resolution than a full 3D grid could
	/// afford. Its effect fades with distance from the volume's mid-plane (w = ½), over
	/// <see cref="DetailFadeHeight"/>. Zero: no detail map.
	/// </summary>
	public nint DetailMap { get; init; }

	/// <summary>
	/// Gets the world distance from the volume's mid-plane over which the detail map's effect falls by
	/// a factor of e, so what lies far above or below the plane keeps the coarse volume's values.
	/// Zero: the detail applies at every height.
	/// </summary>
	public float DetailFadeHeight { get; init; }

	/// <summary>Gets <see cref="NoiseWeights"/> with its default applied.</summary>
	public Vector4 EffectiveNoiseWeights => NoiseWeights == Vector4.Zero ? Vector4.One : NoiseWeights;

	/// <summary>Gets <see cref="SamplesPerVoxel"/> with its default applied.</summary>
	public float EffectiveSamplesPerVoxel => SamplesPerVoxel > 0 ? SamplesPerVoxel : 2f;

	/// <summary>Gets <see cref="MaxSamples"/> with its default applied.</summary>
	public int EffectiveMaxSamples => MaxSamples > 0 ? MaxSamples : 1024;
}

/// <summary>
/// An optional extension a renderer backend may implement to draw 3D geometry into an offscreen
/// target that can then be shown as an ImGui image.
/// </summary>
/// <remarks>
/// <para>
/// <strong>This is not a rendering engine.</strong> It is the draw path every backend already has,
/// with a matrix and a depth buffer. <c>IRendererBackend.RenderDrawData</c> already takes vertex
/// and index buffers with a texture id and a clip rect and draws them; every backend in this
/// repository is therefore already a textured-triangle rasterizer, and the 2D-ness lives in exactly
/// three places — the projection is orthographic, there is no depth buffer, and the position is a
/// <see cref="Vector2"/>. Making those three things parameters is the whole of it.
/// </para>
/// <para>
/// Separate from <c>IRendererBackend</c> rather than part of it, so that no existing backend
/// breaks and none is forced to implement it. A caller checks once with
/// <c>ImGuiApp.TryGetRenderer3D</c> and falls back to its own CPU path otherwise, which the
/// existing <c>CreateTexture</c>/<c>UpdateTexture</c> route already supports.
/// </para>
/// <para>
/// <strong>Deliberately not in v1:</strong> shaders, materials, lighting, instancing, compute,
/// MSAA, mipmaps, cubemaps, and target formats other than RGBA8 plus a depth attachment. That list
/// is where a rendering abstraction goes to die, because every item on it forces a backend-specific
/// escape hatch. Vertex color, one optional texture and a matrix cover what consumers have actually
/// asked for: a globe, a model preview, a node-editor background, a gizmo.
/// </para>
/// <para>
/// <strong>One exception to the RGBA8-only rule: <see cref="RenderTargetFormat.Rgba16F"/>.</strong>
/// Thousands of overlapping, mostly transparent draws (glows, particles, point sprites) cannot be
/// composited in eight bits: each faint contribution rounds per channel, which drifts the hue, and
/// additive light saturates. A float target, <see cref="BlendMode.Additive"/>, and one tone-mapping
/// pass into an RGBA8 target (<see cref="ToneMap"/>) cover that without shaders or materials. The
/// members carrying it have default implementations reporting no support, so an existing
/// implementation keeps compiling and a caller asks <see cref="SupportsFormat"/> before relying on it.
/// </para>
/// </remarks>
public interface IRenderer3D
{
	/// <summary>Creates an offscreen target.</summary>
	/// <param name="width">Width in pixels. Must be positive.</param>
	/// <param name="height">Height in pixels. Must be positive.</param>
	/// <param name="depth">Whether to attach a depth buffer. Without one, every <see cref="DepthMode"/> behaves as <see cref="DepthMode.None"/>.</param>
	/// <returns>An opaque handle, released with <see cref="DeleteRenderTarget"/>.</returns>
	public nint CreateRenderTarget(int width, int height, bool depth);

	/// <summary>Resizes an existing target, discarding its contents.</summary>
	/// <param name="target">A handle from <see cref="CreateRenderTarget(int, int, bool)"/>.</param>
	/// <param name="width">New width in pixels. Must be positive.</param>
	/// <param name="height">New height in pixels. Must be positive.</param>
	/// <returns><see langword="true"/> if the target was resized; <see langword="false"/> if the handle is not one this renderer issued.</returns>
	/// <remarks>
	/// A caller sizing a target to <c>ImGui.GetContentRegionAvail()</c> will see it change every
	/// frame while a splitter is dragged, and reallocating storage per frame is the obvious way to
	/// make this feature look slow. Round the requested size up to a granularity and resize on a
	/// change of bucket rather than a change of pixel. That belongs in the widget, not here.
	/// </remarks>
	public bool ResizeRenderTarget(nint target, int width, int height);

	/// <summary>Releases a target and everything attached to it.</summary>
	/// <param name="target">A handle from <see cref="CreateRenderTarget(int, int, bool)"/>.</param>
	public void DeleteRenderTarget(nint target);

	/// <summary>Gets the target's color attachment, usable as an ImGui texture id.</summary>
	/// <param name="target">A handle from <see cref="CreateRenderTarget(int, int, bool)"/>.</param>
	/// <returns>A texture id for <c>ImGui.Image</c>.</returns>
	/// <remarks>
	/// <strong>The returned id must never be passed to <c>IRendererBackend.DeleteTexture</c>.</strong>
	/// On OpenGL it is a texture name from the same namespace <c>CreateTexture</c> draws from, so
	/// deleting it would free a live target's color attachment out from under it. A target is
	/// released through <see cref="DeleteRenderTarget"/> and nothing else, and an implementation
	/// should reject a handle it did not issue rather than corrupt state quietly. The same holds in
	/// reverse: a <c>CreateTexture</c> handle is not a target.
	/// </remarks>
	public nint GetTargetTexture(nint target);

	/// <summary>Clears a target's color and depth attachments.</summary>
	/// <param name="target">A handle from <see cref="CreateRenderTarget(int, int, bool)"/>.</param>
	/// <param name="color">The color to fill with, straight alpha, each channel in [0, 1].</param>
	/// <param name="depth">The depth to fill with. Far is 1.</param>
	public void Clear(nint target, Vector4 color, float depth);

	/// <summary>Draws indexed geometry into a target.</summary>
	/// <param name="target">A handle from <see cref="CreateRenderTarget(int, int, bool)"/>.</param>
	/// <param name="vertices">The vertices.</param>
	/// <param name="indices">Indices into <paramref name="vertices"/>, grouped by <see cref="DrawState3D.Topology"/>.</param>
	/// <param name="state">Everything about the draw that is not its vertices.</param>
	public void Draw(nint target, ReadOnlySpan<Vertex3D> vertices, ReadOnlySpan<uint> indices, in DrawState3D state);

	/// <summary>Whether this renderer can create targets in a format.</summary>
	/// <param name="format">The format.</param>
	/// <returns><see langword="true"/> if <see cref="CreateRenderTarget(int, int, bool, RenderTargetFormat)"/> accepts it.</returns>
	/// <remarks>The default answers only for <see cref="RenderTargetFormat.Rgba8"/>, which every renderer supports.</remarks>
	public bool SupportsFormat(RenderTargetFormat format) => format == RenderTargetFormat.Rgba8;

	/// <summary>Creates an offscreen target with a colour attachment in the given format.</summary>
	/// <param name="width">Width in pixels. Must be positive.</param>
	/// <param name="height">Height in pixels. Must be positive.</param>
	/// <param name="depth">Whether to attach a depth buffer.</param>
	/// <param name="format">The colour attachment's format. <see cref="ResizeRenderTarget"/> keeps it.</param>
	/// <returns>An opaque handle, released with <see cref="DeleteRenderTarget"/>.</returns>
	/// <exception cref="NotSupportedException">The renderer does not support <paramref name="format"/>; see <see cref="SupportsFormat"/>.</exception>
	public nint CreateRenderTarget(int width, int height, bool depth, RenderTargetFormat format) =>
		format == RenderTargetFormat.Rgba8
			? CreateRenderTarget(width, height, depth)
			: throw new NotSupportedException($"This renderer has no {format} render targets.");

	/// <summary>
	/// Draws <paramref name="source"/> into <paramref name="destination"/> through a tone map, replacing
	/// the destination's contents. The source normally holds <see cref="RenderTargetFormat.Rgba16F"/>
	/// values and the destination is the <see cref="RenderTargetFormat.Rgba8"/> target that is shown.
	/// </summary>
	/// <param name="source">The target to read. Its whole area is stretched over the destination's.</param>
	/// <param name="destination">The target to write. Must not be <paramref name="source"/>.</param>
	/// <param name="toneMap">The mapping, as <see cref="ToneMap3D.Apply"/> computes it.</param>
	/// <exception cref="NotSupportedException">The renderer cannot tone-map; true of every renderer whose <see cref="SupportsFormat"/> refuses <see cref="RenderTargetFormat.Rgba16F"/>.</exception>
	public void ToneMap(nint source, nint destination, in ToneMap3D toneMap) =>
		throw new NotSupportedException("This renderer cannot tone-map between render targets.");

	/// <summary>Gets whether this renderer supports volume textures and <see cref="DrawVolume"/>.</summary>
	/// <remarks>The default answers <see langword="false"/>.</remarks>
	public bool SupportsVolumes => false;

	/// <summary>
	/// Creates a volume texture holding one or four floating-point values per voxel, stored at half
	/// precision. Each channel has its own colour and extinction in <see cref="VolumeDrawState"/>.
	/// </summary>
	/// <param name="width">Voxels along X. Must be positive.</param>
	/// <param name="height">Voxels along Y. Must be positive.</param>
	/// <param name="depth">Voxels along Z. Must be positive.</param>
	/// <param name="channels">Values per voxel: 1 or 4.</param>
	/// <param name="values">The values, channels interleaved, then X fastest, then Y, then Z: <c>width · height · depth · channels</c> of them.</param>
	/// <returns>An opaque handle, released with <see cref="DeleteVolumeTexture"/>. Not an ImGui texture id.</returns>
	/// <exception cref="NotSupportedException">The renderer has no volumes; see <see cref="SupportsVolumes"/>.</exception>
	public nint CreateVolumeTexture(int width, int height, int depth, int channels, ReadOnlySpan<Half> values) =>
		throw new NotSupportedException("This renderer has no volume textures.");

	/// <summary>Releases a volume texture.</summary>
	/// <param name="volume">A handle from <see cref="CreateVolumeTexture"/>.</param>
	/// <exception cref="NotSupportedException">The renderer has no volumes.</exception>
	public void DeleteVolumeTexture(nint volume) =>
		throw new NotSupportedException("This renderer has no volume textures.");

	/// <summary>Ray-marches an emissive volume over every pixel of a target; see <see cref="VolumeDrawState"/>.</summary>
	/// <param name="target">A handle from <see cref="CreateRenderTarget(int, int, bool)"/>.</param>
	/// <param name="volume">A handle from <see cref="CreateVolumeTexture"/>.</param>
	/// <param name="state">The pass's matrices, colour, fade and sampling.</param>
	/// <exception cref="NotSupportedException">The renderer has no volumes.</exception>
	public void DrawVolume(nint target, nint volume, in VolumeDrawState state) =>
		throw new NotSupportedException("This renderer has no volume textures.");
}
