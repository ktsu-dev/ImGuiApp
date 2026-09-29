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
	/// Samples the texture with bilinear filtering and clamp-to-edge addressing, which is how every
	/// backend in <c>ktsu.ImGui.App</c> samples: the OpenGL controller and the Metal backend set
	/// linear filtering on the font atlas and on application textures alike.
	/// </summary>
	/// <remarks>
	/// Nearest-neighbour sampling is not a neutral simplification of that. Dear ImGui draws thick
	/// lines as a textured quad whose anti-aliased edges exist only as the blend between two
	/// texels, and it places those quads so pixel centres land on texel boundaries, where nearest
	/// picks a side by rounding error: a vertical divider came out as a staircase of three widths
	/// with no edge softening. The filter is still deterministic, since there is one
	/// implementation and it uses no hardware.
	/// </remarks>
	/// <param name="u">Horizontal coordinate, normalized.</param>
	/// <param name="v">Vertical coordinate, normalized.</param>
	/// <returns>The sampled color.</returns>
	public Rgba32 Sample(float u, float v)
	{
		int width = Pixels.Width;
		int height = Pixels.Height;

		// Texel centres sit at half-integer coordinates, so shift by half a texel before splitting
		// into a base texel and the fraction towards its neighbour.
		float fx = (u * width) - 0.5f;
		float fy = (v * height) - 0.5f;
		float floorX = MathF.Floor(fx);
		float floorY = MathF.Floor(fy);
		float tx = fx - floorX;
		float ty = fy - floorY;

		int x0 = Math.Clamp((int)floorX, 0, width - 1);
		int y0 = Math.Clamp((int)floorY, 0, height - 1);
		int x1 = Math.Clamp((int)floorX + 1, 0, width - 1);
		int y1 = Math.Clamp((int)floorY + 1, 0, height - 1);

		Rgba32 c00 = Pixels.GetPixel(x0, y0);

		// On a texel centre, or on a one-texel texture, there is nothing to blend. That covers the
		// white texel every solid ImGui shape samples, so the common case stays a single read.
		if ((tx <= NoBlend || x0 == x1) && (ty <= NoBlend || y0 == y1))
		{
			return c00;
		}

		Rgba32 c10 = Pixels.GetPixel(x1, y0);
		Rgba32 c01 = Pixels.GetPixel(x0, y1);
		Rgba32 c11 = Pixels.GetPixel(x1, y1);

		float w00 = (1f - tx) * (1f - ty);
		float w10 = tx * (1f - ty);
		float w01 = (1f - tx) * ty;
		float w11 = tx * ty;

		return new Rgba32(
			Blend(c00.R, c10.R, c01.R, c11.R, w00, w10, w01, w11),
			Blend(c00.G, c10.G, c01.G, c11.G, w00, w10, w01, w11),
			Blend(c00.B, c10.B, c01.B, c11.B, w00, w10, w01, w11),
			Blend(c00.A, c10.A, c01.A, c11.A, w00, w10, w01, w11));
	}

	/// <summary>A neighbour weight too small to move any channel by a unit.</summary>
	private const float NoBlend = 1e-6f;

	private static byte Blend(byte c00, byte c10, byte c01, byte c11, float w00, float w10, float w01, float w11) =>
		(byte)Math.Clamp(MathF.Round((c00 * w00) + (c10 * w10) + (c01 * w01) + (c11 * w11)), 0f, 255f);
}
