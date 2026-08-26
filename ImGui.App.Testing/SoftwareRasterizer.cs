// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;
using System.Numerics;

/// <summary>A rasterizer input vertex, matching ImGui's vertex layout.</summary>
/// <param name="Position">Position in target pixels.</param>
/// <param name="Uv">Texture coordinate, normalized.</param>
/// <param name="Color">Vertex color, straight alpha.</param>
public readonly record struct Vertex(Vector2 Position, Vector2 Uv, Rgba32 Color);

/// <summary>An integer rectangle used for scissor clipping and for reporting measured bounds.</summary>
/// <param name="MinX">Inclusive left edge.</param>
/// <param name="MinY">Inclusive top edge.</param>
/// <param name="MaxX">Exclusive right edge.</param>
/// <param name="MaxY">Exclusive bottom edge.</param>
public readonly record struct Rectangle(int MinX, int MinY, int MaxX, int MaxY)
{
	/// <summary>Gets the width in pixels, never negative.</summary>
	public int Width => Math.Max(0, MaxX - MinX);

	/// <summary>Gets the height in pixels, never negative.</summary>
	public int Height => Math.Max(0, MaxY - MinY);

	/// <summary>Builds a rectangle covering an entire bitmap.</summary>
	/// <param name="target">The bitmap to cover.</param>
	/// <returns>A rectangle spanning the whole target.</returns>
	public static Rectangle FullSize(Bitmap32 target)
	{
		Ensure.NotNull(target);
		return new Rectangle(0, 0, target.Width, target.Height);
	}
}

/// <summary>
/// A CPU rasterizer covering the subset of drawing ImGui emits: indexed triangle lists, one texture
/// per draw command, vertex color modulation, straight-alpha blending, and scissor rectangles.
/// Deliberately not general purpose.
/// </summary>
public static class SoftwareRasterizer
{
	/// <summary>Fills one triangle into the target, blending over what is already there.</summary>
	/// <param name="target">The bitmap to draw into.</param>
	/// <param name="a">First vertex.</param>
	/// <param name="b">Second vertex.</param>
	/// <param name="c">Third vertex.</param>
	/// <param name="texture">Texture to sample, or null to use vertex color alone.</param>
	/// <param name="scissor">Clip rectangle in target pixels.</param>
	public static void FillTriangle(Bitmap32 target, in Vertex a, in Vertex b, in Vertex c, TextureSource? texture, in Rectangle scissor)
	{
		Ensure.NotNull(target);

		float signedArea = Edge(a.Position, b.Position, c.Position);
		if (Math.Abs(signedArea) < 1e-6f)
		{
			// A zero-area triangle. ImGui emits these routinely for collapsed geometry.
			return;
		}

		// ImGui emits both windings, so swap two vertices rather than culling. Culling one winding
		// would silently drop geometry instead of failing visibly.
		Vertex v0 = a;
		Vertex v1 = signedArea < 0 ? c : b;
		Vertex v2 = signedArea < 0 ? b : c;
		float area = Math.Abs(signedArea);

		int minX = Math.Max(Math.Max(scissor.MinX, 0), (int)MathF.Floor(Min3(v0.Position.X, v1.Position.X, v2.Position.X)));
		int minY = Math.Max(Math.Max(scissor.MinY, 0), (int)MathF.Floor(Min3(v0.Position.Y, v1.Position.Y, v2.Position.Y)));
		int maxX = Math.Min(Math.Min(scissor.MaxX, target.Width), (int)MathF.Ceiling(Max3(v0.Position.X, v1.Position.X, v2.Position.X)));
		int maxY = Math.Min(Math.Min(scissor.MaxY, target.Height), (int)MathF.Ceiling(Max3(v0.Position.Y, v1.Position.Y, v2.Position.Y)));

		if (minX >= maxX || minY >= maxY)
		{
			return;
		}

		// Most of what ImGui emits is a solid rectangle: window backgrounds, frames, separators,
		// table rows, button fills. Every vertex of one carries the same color and the same texture
		// coordinate, the atlas's white texel, so the color it contributes is constant across the
		// triangle and can be resolved once instead of per pixel. Hoisting it matters because the
		// atlas is bound for essentially every draw command, which is why keying this off a null
		// texture alone would almost never fire.
		//
		// Both of the shortcuts below are exact rather than approximate:
		//   * three equal vertex colors: the barycentric weights sum to one, so interpolating
		//     between three copies of a value returns that value to well inside half a unit of a
		//     byte channel.
		//   * an opaque source: straight-alpha source-over with an alpha of one reduces
		//     algebraically to the source color, and rounding a byte-valued float returns it.
		// The arithmetic is otherwise untouched. In particular the divides are not turned into a
		// reciprocal multiply and the edge functions are not stepped incrementally, because both
		// change rounding and can move a pixel at a triangle's edge.
		bool uniformColor = v0.Color == v1.Color && v1.Color == v2.Color;
		bool uniformUv = v0.Uv == v1.Uv && v1.Uv == v2.Uv;
		bool constantSource = uniformColor && (texture is null || uniformUv);

		Rgba32 flat = default;
		if (constantSource)
		{
			flat = texture is null ? v0.Color : Modulate(v0.Color, texture.Sample(v0.Uv.X, v0.Uv.Y));
		}

		bool opaqueFlat = constantSource && flat.A == 255;

		Span<byte> pixels = target.Pixels;
		int width = target.Width;

		for (int y = minY; y < maxY; y++)
		{
			int row = y * width * 4;

			for (int x = minX; x < maxX; x++)
			{
				Vector2 p = new(x + 0.5f, y + 0.5f);

				// Tested one at a time so a pixel outside the triangle costs one edge function
				// rather than three. Most pixels in a triangle's bounding box are outside it.
				float w0 = Edge(v1.Position, v2.Position, p);
				if (w0 < 0)
				{
					continue;
				}

				float w1 = Edge(v2.Position, v0.Position, p);
				if (w1 < 0)
				{
					continue;
				}

				float w2 = Edge(v0.Position, v1.Position, p);
				if (w2 < 0)
				{
					continue;
				}

				int i = row + (x * 4);

				if (opaqueFlat)
				{
					pixels[i] = flat.R;
					pixels[i + 1] = flat.G;
					pixels[i + 2] = flat.B;
					pixels[i + 3] = 255;
					continue;
				}

				Rgba32 source;

				if (constantSource)
				{
					source = flat;
				}
				else
				{
					float l0 = w0 / area;
					float l1 = w1 / area;
					float l2 = w2 / area;

					source = uniformColor ? v0.Color : Interpolate(v0.Color, v1.Color, v2.Color, l0, l1, l2);

					if (texture is not null)
					{
						Vector2 uv = (v0.Uv * l0) + (v1.Uv * l1) + (v2.Uv * l2);
						source = Modulate(source, texture.Sample(uv.X, uv.Y));
					}
				}

				if (source.A == 255)
				{
					pixels[i] = source.R;
					pixels[i + 1] = source.G;
					pixels[i + 2] = source.B;
					pixels[i + 3] = 255;
					continue;
				}

				Rgba32 blended = BlendOver(source, new Rgba32(pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3]));
				pixels[i] = blended.R;
				pixels[i + 1] = blended.G;
				pixels[i + 2] = blended.B;
				pixels[i + 3] = blended.A;
			}
		}
	}

	/// <summary>Multiplies two colors channel by channel.</summary>
	/// <param name="a">First color.</param>
	/// <param name="b">Second color.</param>
	/// <returns>The modulated color.</returns>
	public static Rgba32 Modulate(Rgba32 a, Rgba32 b) => new(
		ToByte(a.R * b.R / 255f),
		ToByte(a.G * b.G / 255f),
		ToByte(a.B * b.B / 255f),
		ToByte(a.A * b.A / 255f));

	/// <summary>
	/// Composites a source color over a destination using straight-alpha source-over. Alpha is
	/// coverage and never passes through a transfer function, matching how the rest of this
	/// ecosystem treats it.
	/// </summary>
	/// <param name="source">The incoming color.</param>
	/// <param name="destination">The color already in the target.</param>
	/// <returns>The composited color.</returns>
	public static Rgba32 BlendOver(Rgba32 source, Rgba32 destination)
	{
		float sourceAlpha = source.A / 255f;
		float destinationAlpha = destination.A / 255f;
		float outAlpha = sourceAlpha + (destinationAlpha * (1f - sourceAlpha));

		if (outAlpha <= 0f)
		{
			return new Rgba32(0, 0, 0, 0);
		}

		byte Channel(byte s, byte d) => ToByte(
			((s / 255f * sourceAlpha) + (d / 255f * destinationAlpha * (1f - sourceAlpha))) / outAlpha * 255f);

		return new Rgba32(
			Channel(source.R, destination.R),
			Channel(source.G, destination.G),
			Channel(source.B, destination.B),
			ToByte(outAlpha * 255f));
	}

	private static float Edge(Vector2 a, Vector2 b, Vector2 p) =>
		((b.X - a.X) * (p.Y - a.Y)) - ((b.Y - a.Y) * (p.X - a.X));

	private static float Min3(float a, float b, float c) => MathF.Min(a, MathF.Min(b, c));

	private static float Max3(float a, float b, float c) => MathF.Max(a, MathF.Max(b, c));

	private static Rgba32 Interpolate(Rgba32 a, Rgba32 b, Rgba32 c, float l0, float l1, float l2) => new(
		ToByte((a.R * l0) + (b.R * l1) + (c.R * l2)),
		ToByte((a.G * l0) + (b.G * l1) + (c.G * l2)),
		ToByte((a.B * l0) + (b.B * l1) + (c.B * l2)),
		ToByte((a.A * l0) + (b.A * l1) + (c.A * l2)));

	private static byte ToByte(float value) => (byte)Math.Clamp(MathF.Round(value), 0f, 255f);
}
