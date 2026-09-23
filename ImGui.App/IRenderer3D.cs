// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App;

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
/// Separate from <see cref="IRendererBackend"/> rather than part of it, so that no existing backend
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
	/// <param name="target">A handle from <see cref="CreateRenderTarget"/>.</param>
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
	/// <param name="target">A handle from <see cref="CreateRenderTarget"/>.</param>
	public void DeleteRenderTarget(nint target);

	/// <summary>Gets the target's color attachment, usable as an ImGui texture id.</summary>
	/// <param name="target">A handle from <see cref="CreateRenderTarget"/>.</param>
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
	/// <param name="target">A handle from <see cref="CreateRenderTarget"/>.</param>
	/// <param name="color">The color to fill with, straight alpha, each channel in [0, 1].</param>
	/// <param name="depth">The depth to fill with. Far is 1.</param>
	public void Clear(nint target, Vector4 color, float depth);

	/// <summary>Draws indexed geometry into a target.</summary>
	/// <param name="target">A handle from <see cref="CreateRenderTarget"/>.</param>
	/// <param name="vertices">The vertices.</param>
	/// <param name="indices">Indices into <paramref name="vertices"/>, grouped by <see cref="DrawState3D.Topology"/>.</param>
	/// <param name="state">Everything about the draw that is not its vertices.</param>
	public void Draw(nint target, ReadOnlySpan<Vertex3D> vertices, ReadOnlySpan<uint> indices, in DrawState3D state);
}
