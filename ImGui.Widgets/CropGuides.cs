// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

/// <summary>The composition guide drawn over what a crop keeps.</summary>
/// <remarks>The order is the order <see cref="CropGuides.Next"/> cycles through them in.</remarks>
public enum CropGuide
{
	/// <summary>The rule of thirds: two lines each way, a third in from each edge.</summary>
	Thirds,

	/// <summary>The phi grid: two lines each way, at the golden section from each edge.</summary>
	GoldenRatio,

	/// <summary>A golden spiral, with the sections it turns through. Asymmetric, so it can be flipped.</summary>
	GoldenSpiral,

	/// <summary>A 45 degree line in from each corner.</summary>
	Diagonals,

	/// <summary>One diagonal, with a line square to it from each of the other two corners. Asymmetric, so it can be flipped.</summary>
	Triangles,

	/// <summary>A fine grid of square cells, for checking that lines run true.</summary>
	Grid,
}

/// <summary>The lines each crop guide draws, worked out without a graphics context, and drawing them.</summary>
/// <remarks>
/// <para>
/// Every guide is a polyline in the crop window's own pixels, from its top-left corner. A straight
/// line is two points and the spiral's arcs are many, so the drawing has one shape to handle.
/// </para>
/// <para>
/// The golden spiral is drawn through golden sections of the window whatever its aspect, so in a
/// window that is not itself a golden rectangle its arcs are quarter ellipses rather than circles.
/// That keeps the sections it passes through on the phi grid, which is what a spiral is composed
/// against, rather than letting the spiral run off the window's edge.
/// </para>
/// </remarks>
public static class CropGuides
{
	/// <summary>The golden ratio.</summary>
	public static readonly float Phi = (1f + MathF.Sqrt(5f)) / 2f;

	/// <summary>How many quarter turns of spiral are drawn at most.</summary>
	private const int SpiralTurns = 12;

	/// <summary>The smallest section the spiral turns through, in pixels; anything finer would be a smudge.</summary>
	private const float SmallestSpiralSection = 2f;

	/// <summary>How many segments each quarter turn of spiral is drawn with.</summary>
	private const int SegmentsPerArc = 24;

	/// <summary>How many grid cells span the window's shorter side.</summary>
	private const int GridCellsAcrossShortSide = 12;

	/// <summary>How many ways round a guide can be drawn: one bit mirroring each axis.</summary>
	public const int Orientations = 4;

	/// <summary>Gets whether an orientation is one of the four there are.</summary>
	/// <param name="orientation">The orientation to check.</param>
	/// <returns>Whether it is.</returns>
	public static bool IsValidOrientation(int orientation) => orientation is >= 0 and < Orientations;

	/// <summary>Gets whether flipping a guide changes what it draws.</summary>
	/// <param name="guide">The guide.</param>
	/// <returns>Whether it is asymmetric.</returns>
	public static bool IsAsymmetric(CropGuide guide) => guide is CropGuide.GoldenSpiral or CropGuide.Triangles;

	/// <summary>The guide after <paramref name="guide"/>, wrapping round after the last.</summary>
	/// <param name="guide">The current guide.</param>
	/// <returns>The next one.</returns>
	public static CropGuide Next(CropGuide guide)
	{
		CropGuide[] all = Enum.GetValues<CropGuide>();
		return all[(Array.IndexOf(all, guide) + 1) % all.Length];
	}

	/// <summary>The orientation after <paramref name="orientation"/>, wrapping round after the last.</summary>
	/// <param name="orientation">The current orientation.</param>
	/// <returns>The next one.</returns>
	public static int NextOrientation(int orientation) => (Math.Clamp(orientation, 0, Orientations - 1) + 1) % Orientations;

	/// <summary>The lines a guide draws over a window of the given size.</summary>
	/// <param name="guide">Which guide.</param>
	/// <param name="orientation">Which way round, for an asymmetric one: bit 0 mirrors left to right, bit 1 top to bottom.</param>
	/// <param name="size">The window's size in pixels.</param>
	/// <returns>The guides, each a polyline in the window's pixels from its top-left corner.</returns>
	public static IReadOnlyList<Vector2[]> For(CropGuide guide, int orientation, Vector2 size)
	{
		if (size.X <= 0f || size.Y <= 0f)
		{
			return [];
		}

		List<Vector2[]> guides = guide switch
		{
			CropGuide.GoldenRatio => Sections(size, 1f / (Phi * Phi)),
			CropGuide.GoldenSpiral => Spiral(size),
			CropGuide.Diagonals => Diagonals(size),
			CropGuide.Triangles => Triangles(size),
			CropGuide.Grid => Grid(size),
			_ => Sections(size, 1f / 3f),
		};

		if (!IsAsymmetric(guide) || orientation == 0)
		{
			return guides;
		}

		bool mirrorX = (orientation & 1) != 0;
		bool mirrorY = (orientation & 2) != 0;
		return [.. guides.Select(line => line.Select(point => new Vector2(
			mirrorX ? size.X - point.X : point.X,
			mirrorY ? size.Y - point.Y : point.Y)).ToArray())];
	}

	/// <summary>Draws a guide over an upright rectangle on screen, such as a crop window.</summary>
	/// <param name="drawList">Where to draw.</param>
	/// <param name="guide">Which guide.</param>
	/// <param name="orientation">Which way round, for an asymmetric one; see <see cref="For"/>.</param>
	/// <param name="min">The rectangle's top-left corner on screen.</param>
	/// <param name="max">The rectangle's bottom-right corner on screen.</param>
	/// <param name="color">The lines' packed colour.</param>
	/// <param name="thickness">The lines' thickness in pixels.</param>
	public static void Draw(ImDrawListPtr drawList, CropGuide guide, int orientation, Vector2 min, Vector2 max, uint color, float thickness = 1f)
	{
		foreach (Vector2[] line in For(guide, orientation, max - min))
		{
			for (int point = 1; point < line.Length; point++)
			{
				drawList.AddLine(min + line[point - 1], min + line[point], color, thickness);
			}
		}
	}

	/// <summary>Draws a guide over a quadrilateral on screen, such as a turned crop.</summary>
	/// <param name="drawList">Where to draw.</param>
	/// <param name="guide">Which guide.</param>
	/// <param name="orientation">Which way round, for an asymmetric one; see <see cref="For"/>.</param>
	/// <param name="corners">The corners on screen, clockwise from what is the top-left before turning. Must be a parallelogram.</param>
	/// <param name="color">The lines' packed colour.</param>
	/// <param name="thickness">The lines' thickness in pixels.</param>
	/// <remarks>
	/// The guide is laid out over the parallelogram's own side lengths, so a grid's cells stay square
	/// and the spiral's smallest section is measured in screen pixels, then carried onto the corners.
	/// </remarks>
	public static void Draw(ImDrawListPtr drawList, CropGuide guide, int orientation, IReadOnlyList<Vector2> corners, uint color, float thickness = 1f)
	{
		Ensure.NotNull(corners);
		if (corners.Count != 4)
		{
			throw new ArgumentException("A crop has four corners.", nameof(corners));
		}

		Vector2 origin = corners[0];
		Vector2 across = corners[1] - origin;
		Vector2 down = corners[3] - origin;
		Vector2 size = new(across.Length(), down.Length());
		if (size.X <= 0f || size.Y <= 0f)
		{
			return;
		}

		Vector2 ToScreen(Vector2 point) => origin + (across * (point.X / size.X)) + (down * (point.Y / size.Y));

		foreach (Vector2[] line in For(guide, orientation, size))
		{
			for (int point = 1; point < line.Length; point++)
			{
				drawList.AddLine(ToScreen(line[point - 1]), ToScreen(line[point]), color, thickness);
			}
		}
	}

	/// <summary>Two lines each way, <paramref name="fraction"/> in from each edge.</summary>
	private static List<Vector2[]> Sections(Vector2 size, float fraction)
	{
		List<Vector2[]> lines = [];

		foreach (float at in (float[])[fraction, 1f - fraction])
		{
			lines.Add([new Vector2(size.X * at, 0f), new Vector2(size.X * at, size.Y)]);
			lines.Add([new Vector2(0f, size.Y * at), new Vector2(size.X, size.Y * at)]);
		}

		return lines;
	}

	/// <summary>A 45 degree line in from each corner, running until it meets the far side.</summary>
	private static List<Vector2[]> Diagonals(Vector2 size)
	{
		float reach = MathF.Min(size.X, size.Y);
		return
		[
			[Vector2.Zero, new Vector2(reach, reach)],
			[new Vector2(0f, size.Y), new Vector2(reach, size.Y - reach)],
			[new Vector2(size.X, 0f), new Vector2(size.X - reach, reach)],
			[size, new Vector2(size.X - reach, size.Y - reach)],
		];
	}

	/// <summary>The diagonal from the top-left corner, and a line square to it from each of the other two.</summary>
	private static List<Vector2[]> Triangles(Vector2 size)
	{
		Vector2 bottomLeft = new(0f, size.Y);
		Vector2 topRight = new(size.X, 0f);
		return
		[
			[Vector2.Zero, size],
			[bottomLeft, FootOnDiagonal(bottomLeft, size)],
			[topRight, FootOnDiagonal(topRight, size)],
		];
	}

	/// <summary>Where a line from <paramref name="point"/> square to the diagonal from the origin to <paramref name="size"/> meets it.</summary>
	private static Vector2 FootOnDiagonal(Vector2 point, Vector2 size) => size * (Vector2.Dot(point, size) / size.LengthSquared());

	/// <summary>Square cells, a fixed number across the shorter side, counted from the top-left corner.</summary>
	private static List<Vector2[]> Grid(Vector2 size)
	{
		float cell = MathF.Min(size.X, size.Y) / GridCellsAcrossShortSide;
		List<Vector2[]> lines = [];

		// Stopping a hair short of the far edge, so a line is never drawn over the box's own edge.
		for (float x = cell; x < size.X - (cell * 0.01f); x += cell)
		{
			lines.Add([new Vector2(x, 0f), new Vector2(x, size.Y)]);
		}

		for (float y = cell; y < size.Y - (cell * 0.01f); y += cell)
		{
			lines.Add([new Vector2(0f, y), new Vector2(size.X, y)]);
		}

		return lines;
	}

	/// <summary>A golden spiral, turning inwards clockwise from the bottom-left corner, with the section lines it turns at.</summary>
	/// <remarks>
	/// Laid out for a landscape window and transposed for a portrait one, so the first and largest
	/// section is always cut across the longer side.
	/// </remarks>
	private static List<Vector2[]> Spiral(Vector2 size)
	{
		bool portrait = size.Y > size.X;
		Vector2 landscape = portrait ? new Vector2(size.Y, size.X) : size;
		List<Vector2[]> guides = [];
		List<Vector2> path = [];

		// The rectangle the spiral has still to turn through, shrinking by a golden section each quarter turn.
		Vector2 min = Vector2.Zero;
		Vector2 extent = landscape;
		float cut = 1f / Phi;

		for (int turn = 0; turn < SpiralTurns && MathF.Min(extent.X, extent.Y) >= SmallestSpiralSection; turn++)
		{
			Vector2 max = min + extent;
			(Vector2 from, Vector2 to, Vector2 centre, Vector2[] section) = (turn % 4) switch
			{
				// A section off the left: the arc runs from its bottom-left to its top-right, about its bottom-right.
				0 => LeftSection(min, max, extent.X * cut),
				1 => TopSection(min, max, extent.Y * cut),
				2 => RightSection(min, max, extent.X * cut),
				_ => BottomSection(min, max, extent.Y * cut),
			};

			AddArc(path, from, to, centre);
			guides.Add(section);
			(min, extent) = Remainder(turn % 4, min, extent, cut);
		}

		guides.Insert(0, [.. path]);

		return portrait
			? [.. guides.Select(line => line.Select(static point => new Vector2(point.Y, point.X)).ToArray())]
			: guides;
	}

	private static (Vector2 From, Vector2 To, Vector2 Centre, Vector2[] Section) LeftSection(Vector2 min, Vector2 max, float width)
	{
		float x = min.X + width;
		return (new Vector2(min.X, max.Y), new Vector2(x, min.Y), new Vector2(x, max.Y), [new Vector2(x, min.Y), new Vector2(x, max.Y)]);
	}

	private static (Vector2 From, Vector2 To, Vector2 Centre, Vector2[] Section) TopSection(Vector2 min, Vector2 max, float height)
	{
		float y = min.Y + height;
		return (min, new Vector2(max.X, y), new Vector2(min.X, y), [new Vector2(min.X, y), new Vector2(max.X, y)]);
	}

	private static (Vector2 From, Vector2 To, Vector2 Centre, Vector2[] Section) RightSection(Vector2 min, Vector2 max, float width)
	{
		float x = max.X - width;
		return (new Vector2(max.X, min.Y), new Vector2(x, max.Y), new Vector2(x, min.Y), [new Vector2(x, min.Y), new Vector2(x, max.Y)]);
	}

	private static (Vector2 From, Vector2 To, Vector2 Centre, Vector2[] Section) BottomSection(Vector2 min, Vector2 max, float height)
	{
		float y = max.Y - height;
		return (max, new Vector2(min.X, y), new Vector2(max.X, y), [new Vector2(min.X, y), new Vector2(max.X, y)]);
	}

	/// <summary>What is left once a quarter turn's section is cut off.</summary>
	private static (Vector2 Min, Vector2 Extent) Remainder(int side, Vector2 min, Vector2 extent, float cut) => side switch
	{
		0 => (new Vector2(min.X + (extent.X * cut), min.Y), new Vector2(extent.X * (1f - cut), extent.Y)),
		1 => (new Vector2(min.X, min.Y + (extent.Y * cut)), new Vector2(extent.X, extent.Y * (1f - cut))),
		2 => (min, new Vector2(extent.X * (1f - cut), extent.Y)),
		_ => (min, new Vector2(extent.X, extent.Y * (1f - cut))),
	};

	/// <summary>Adds a quarter ellipse about <paramref name="centre"/> from <paramref name="from"/> to <paramref name="to"/>.</summary>
	/// <remarks>
	/// The two ends lie along perpendicular axes from the centre, so sweeping between them by cosine
	/// and sine traces the quarter ellipse with those two as its radii. The first point is skipped
	/// after the first arc, since it is the last arc's end.
	/// </remarks>
	private static void AddArc(List<Vector2> path, Vector2 from, Vector2 to, Vector2 centre)
	{
		Vector2 a = from - centre;
		Vector2 b = to - centre;

		for (int segment = path.Count == 0 ? 0 : 1; segment <= SegmentsPerArc; segment++)
		{
			float t = segment * MathF.PI / 2f / SegmentsPerArc;
			path.Add(centre + (a * MathF.Cos(t)) + (b * MathF.Sin(t)));
		}
	}
}
