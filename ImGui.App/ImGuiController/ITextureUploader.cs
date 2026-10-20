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
	/// <param name="width">Width of the whole texture in pixels, which is the source buffer's row stride.</param>
	/// <param name="height">Height of the whole texture in pixels.</param>
	/// <param name="rect">The rectangle that changed.</param>
	/// <param name="pixels">
	/// The whole texture's RGBA32 pixel buffer, tightly packed. A renderer that can write a
	/// rectangle reads it out of the middle of this; one that stores whole images re-uploads it all.
	/// </param>
	public void Update(nint textureId, int width, int height, ImTextureRect rect, nint pixels);

	/// <summary>Releases a texture.</summary>
	/// <param name="textureId">The texture to release.</param>
	public void Destroy(nint textureId);
}
