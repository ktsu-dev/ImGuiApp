// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App;

using System;

/// <summary>
/// The optional half-float textures an <see cref="IRendererBackend"/> may also make, holding values
/// encoded by <see cref="HdrEncoding"/> that can go above 1.
/// </summary>
/// <remarks>
/// A backend that doesn't implement it gets the same pixels converted to RGBA8 by
/// <see cref="HdrEncoding.ToRgba8"/>, which is what an SDR window shows of them anyway.
/// </remarks>
internal interface IHalfFloatTextures
{
	/// <summary>Creates a half-float texture.</summary>
	/// <param name="rgba">Tightly packed pixels, four values each.</param>
	/// <param name="width">The width in pixels.</param>
	/// <param name="height">The height in pixels.</param>
	/// <returns>The texture's id.</returns>
	public nint CreateTexture(ReadOnlySpan<Half> rgba, int width, int height);

	/// <summary>Replaces the whole of a half-float texture made by <see cref="CreateTexture"/>.</summary>
	/// <param name="id">The texture.</param>
	/// <param name="rgba">Tightly packed pixels, four values each.</param>
	/// <param name="width">The width in pixels, the texture's own.</param>
	/// <param name="height">The height in pixels, the texture's own.</param>
	/// <returns>Whether it was replaced in place.</returns>
	public bool UpdateTexture(nint id, ReadOnlySpan<Half> rgba, int width, int height);
}
