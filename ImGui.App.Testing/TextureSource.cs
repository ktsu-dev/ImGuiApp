// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;

/// <summary>
/// An RGBA8 texture the rasterizer can sample. Wraps a <see cref="Bitmap32"/> so the font atlas and
/// any application texture share one representation.
/// </summary>
public sealed class TextureSource
{
	/// <summary>Initializes a new instance of the <see cref="TextureSource"/> class.</summary>
	/// <param name="pixels">The texture pixels.</param>
	public TextureSource(Bitmap32 pixels)
	{
		Ensure.NotNull(pixels);
		Pixels = pixels;
	}

	/// <summary>Gets the underlying pixels.</summary>
	public Bitmap32 Pixels { get; }

	/// <summary>
	/// Samples the texture with nearest-neighbor filtering and clamped addressing. Nearest keeps
	/// output identical on every machine, because there are no filtering rules for two
	/// implementations to disagree about.
	/// </summary>
	/// <param name="u">Horizontal coordinate, normalized.</param>
	/// <param name="v">Vertical coordinate, normalized.</param>
	/// <returns>The sampled color.</returns>
	public Rgba32 Sample(float u, float v)
	{
		int x = (int)MathF.Floor(u * Pixels.Width);
		int y = (int)MathF.Floor(v * Pixels.Height);

		x = Math.Clamp(x, 0, Pixels.Width - 1);
		y = Math.Clamp(y, 0, Pixels.Height - 1);

		return Pixels.GetPixel(x, y);
	}
}
