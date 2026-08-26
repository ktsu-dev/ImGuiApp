// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController;

using Hexa.NET.ImGui;

/// <summary>
/// The GPU-facing half of texture reconciliation: the few operations a renderer must provide for
/// <see cref="TextureReconciler"/> to satisfy ImGui's texture requests.
/// </summary>
/// <remarks>
/// This exists to keep the decisions in <see cref="TextureReconciler"/> separable from the graphics
/// calls that carry them out, so the decisions can be exercised without a graphics context.
/// </remarks>
internal interface ITextureUploader
{
	/// <summary>Creates a texture from a whole pixel buffer.</summary>
	/// <param name="width">Texture width in pixels.</param>
	/// <param name="height">Texture height in pixels.</param>
	/// <param name="pixels">The RGBA32 pixel buffer, tightly packed.</param>
	/// <returns>The renderer's identifier for the new texture.</returns>
	public nint Create(int width, int height, nint pixels);

	/// <summary>Uploads one rectangle into an existing texture.</summary>
	/// <param name="textureId">The texture to write into.</param>
	/// <param name="sourceRowPixels">
	/// Row stride of the source buffer in pixels. The rectangle is read out of the middle of a
	/// full-atlas buffer, so this is the atlas width rather than the rectangle width.
	/// </param>
	/// <param name="rect">The destination rectangle.</param>
	/// <param name="pixels">Pointer to the rectangle's top-left pixel within the source buffer.</param>
	public void Update(nint textureId, int sourceRowPixels, ImTextureRect rect, nint pixels);

	/// <summary>Releases a texture.</summary>
	/// <param name="textureId">The texture to release.</param>
	public void Destroy(nint textureId);
}
