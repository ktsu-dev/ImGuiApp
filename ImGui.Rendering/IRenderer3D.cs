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
}
