// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Sortie;

using System;
using System.Numerics;
using Hexa.NET.ImGui;

/// <summary>
/// Drawing primitives shared by the widgets: packed colours, hazard stripes, chamfered plates,
/// hexagons, seven-segment digits and the heavy condensed lettering of a command display.
/// </summary>
internal static class SortieDraw
{
	/// <summary>How far heavy lettering is squeezed horizontally, as a fraction of its natural width.</summary>
	internal const float CondensedWidth = 0.62f;

	/// <summary>Square root of three, which every hexagon measurement goes through.</summary>
	internal static readonly float Sqrt3 = MathF.Sqrt(3f);

	/// <summary>Packs a colour for a draw list, with the current style alpha applied.</summary>
	/// <param name="color">The colour.</param>
	/// <returns>The packed colour.</returns>
	internal static uint U32(Vector4 color) =>
		ImGui.ColorConvertFloat4ToU32(color with { W = color.W * ImGui.GetStyle().Alpha });

	/// <summary>Packs a colour at a given alpha.</summary>
	/// <param name="color">The colour.</param>
	/// <param name="alpha">Its alpha, from 0 to 1.</param>
	/// <returns>The packed colour.</returns>
	internal static uint U32(Vector4 color, float alpha) => U32(color with { W = Math.Clamp(alpha, 0f, 1f) });

	/// <summary>Whether a blinking element is in its lit half at the current time.</summary>
	/// <param name="hertz">Blinks per second; zero or less holds it lit.</param>
	/// <returns>True for the lit half of each period.</returns>
	internal static bool BlinkPhase(float hertz) => hertz <= 0f || ImGui.GetTime() * hertz % 1.0 < 0.5;

	/// <summary>
	/// Resolves a requested size: a width of zero or less fills the available width, a negative height fills the
	/// available height, and a height of zero takes <paramref name="defaultHeight"/>.
	/// </summary>
	/// <param name="size">The requested size.</param>
	/// <param name="defaultHeight">Height to use when none was asked for.</param>
	/// <returns>The size to lay out.</returns>
	internal static Vector2 Resolve(Vector2 size, float defaultHeight)
	{
		Vector2 available = ImGui.GetContentRegionAvail();
		float width = size.X > 0f ? size.X : MathF.Max(available.X, 1f);
		float height = size.Y > 0f ? size.Y : size.Y < 0f ? MathF.Max(available.Y, 1f) : defaultHeight;
		return new Vector2(width, height);
	}

	/// <summary>Fills a rectangle with diagonal warning stripes in two colours.</summary>
	/// <param name="drawList">Where to draw.</param>
	/// <param name="min">Top left.</param>
	/// <param name="max">Bottom right.</param>
	/// <param name="stripe">The stripe colour.</param>
	/// <param name="ground">The colour between the stripes.</param>
	/// <param name="offset">How far the stripes have scrolled, in pixels.</param>
	internal static void HazardStripes(ImDrawListPtr drawList, Vector2 min, Vector2 max, Vector4 stripe, Vector4 ground, float offset = 0f)
	{
		float height = max.Y - min.Y;
		if (height <= 0f || max.X <= min.X)
		{
			return;
		}

		float width = MathF.Max(height * 0.75f, 4f);
		drawList.AddRectFilled(min, max, U32(ground));
		drawList.PushClipRect(min, max, true);
		uint color = U32(stripe);
		float start = min.X - height - (2f * width) + (offset % (2f * width));
		for (float x = start; x < max.X; x += 2f * width)
		{
			drawList.AddQuadFilled(
				new Vector2(x + height, min.Y),
				new Vector2(x + height + width, min.Y),
				new Vector2(x + width, max.Y),
				new Vector2(x, max.Y),
				color);
		}

		drawList.PopClipRect();
	}

	/// <summary>
	/// Gives the outline of a plate with its top right and bottom left corners cut away, in clockwise order.
	/// </summary>
	/// <param name="min">Top left.</param>
	/// <param name="max">Bottom right.</param>
	/// <param name="cut">How much of each cut corner is removed; clamped to half the shorter side.</param>
	/// <returns>The six corners of the plate.</returns>
	internal static Vector2[] ChamferedOutline(Vector2 min, Vector2 max, float cut)
	{
		float c = MathF.Max(0f, MathF.Min(cut, MathF.Min(max.X - min.X, max.Y - min.Y) / 2f));
		return
		[
			min,
			new Vector2(max.X - c, min.Y),
			new Vector2(max.X, min.Y + c),
			max,
			new Vector2(min.X + c, max.Y),
			new Vector2(min.X, max.Y - c),
		];
	}

	/// <summary>Fills a chamfered plate.</summary>
	/// <param name="drawList">Where to draw.</param>
	/// <param name="min">Top left.</param>
	/// <param name="max">Bottom right.</param>
	/// <param name="cut">How much of each cut corner is removed.</param>
	/// <param name="color">Fill colour.</param>
	internal static void ChamferedFill(ImDrawListPtr drawList, Vector2 min, Vector2 max, float cut, Vector4 color) =>
		FillConvex(drawList, ChamferedOutline(min, max, cut), color);

	/// <summary>Outlines a chamfered plate.</summary>
	/// <param name="drawList">Where to draw.</param>
	/// <param name="min">Top left.</param>
	/// <param name="max">Bottom right.</param>
	/// <param name="cut">How much of each cut corner is removed.</param>
	/// <param name="color">Line colour.</param>
	/// <param name="thickness">Line thickness.</param>
	internal static void ChamferedOutline(ImDrawListPtr drawList, Vector2 min, Vector2 max, float cut, Vector4 color, float thickness)
	{
		foreach (Vector2 point in ChamferedOutline(min, max, cut))
		{
			drawList.PathLineTo(point);
		}

		drawList.PathStroke(U32(color), ImDrawFlags.Closed, thickness);
	}

	/// <summary>Fills a convex polygon.</summary>
	/// <param name="drawList">Where to draw.</param>
	/// <param name="points">Its corners, in order.</param>
	/// <param name="color">Fill colour.</param>
	internal static void FillConvex(ImDrawListPtr drawList, ReadOnlySpan<Vector2> points, Vector4 color)
	{
		foreach (Vector2 point in points)
		{
			drawList.PathLineTo(point);
		}

		drawList.PathFillConvex(U32(color));
	}

	/// <summary>Gives the corners of a flat-topped hexagon.</summary>
	/// <param name="center">Its centre.</param>
	/// <param name="radius">Distance from the centre to each corner.</param>
	/// <returns>The six corners, starting at the right and going clockwise on screen.</returns>
	internal static Vector2[] Hexagon(Vector2 center, float radius)
	{
		Vector2[] points = new Vector2[6];
		for (int i = 0; i < 6; i++)
		{
			float angle = MathF.PI / 3f * i;
			points[i] = center + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius);
		}

		return points;
	}

	/// <summary>Measures heavy condensed lettering without drawing it.</summary>
	/// <param name="text">The text.</param>
	/// <param name="pixelHeight">Height of the lettering in pixels.</param>
	/// <returns>The size the lettering occupies.</returns>
	internal static Vector2 MeasureHeavy(string text, float pixelHeight)
	{
		ImGui.PushFont(ImGui.GetFont(), pixelHeight);
		Vector2 natural = ImGui.CalcTextSize(text);
		ImGui.PopFont();
		return new Vector2((natural.X * CondensedWidth) + Weight(pixelHeight), natural.Y);
	}

	/// <summary>
	/// Draws heavy condensed lettering: the interface font at a large size, struck twice side by side for weight
	/// and then squeezed horizontally about its left edge, so no condensed font has to be shipped.
	/// </summary>
	/// <param name="drawList">Where to draw.</param>
	/// <param name="position">Top left of the lettering.</param>
	/// <param name="pixelHeight">Height of the lettering in pixels.</param>
	/// <param name="color">Colour of the lettering.</param>
	/// <param name="text">The text.</param>
	/// <returns>The size the lettering occupies.</returns>
	internal static Vector2 HeavyText(ImDrawListPtr drawList, Vector2 position, float pixelHeight, Vector4 color, string text)
	{
		ImGui.PushFont(ImGui.GetFont(), pixelHeight);
		Vector2 natural = ImGui.CalcTextSize(text);
		float weight = Weight(pixelHeight);
		uint u32 = U32(color);
		int first = drawList.VtxBuffer.Size;
		for (float dx = 0f; dx <= weight; dx += 1f)
		{
			drawList.AddText(position + new Vector2(dx / CondensedWidth, 0f), u32, text);
		}

		ImGui.PopFont();

		// ImVector is a view over native memory, so writing through a copy of it edits the list.
		ImVector<ImDrawVert> vertices = drawList.VtxBuffer;
		for (int i = first; i < vertices.Size; i++)
		{
			ImDrawVert vertex = vertices[i];
			vertex.Pos.X = position.X + ((vertex.Pos.X - position.X) * CondensedWidth);
			vertices[i] = vertex;
		}

		return new Vector2((natural.X * CondensedWidth) + weight, natural.Y);
	}

	/// <summary>How many extra pixels wide each stroke of heavy lettering is struck.</summary>
	/// <param name="pixelHeight">Height of the lettering.</param>
	/// <returns>The extra stroke width.</returns>
	private static float Weight(float pixelHeight) => MathF.Max(1f, MathF.Round(pixelHeight / 18f));

	/// <summary>Which of the seven segments each digit lights: a, b, c, d, e, f, g as bits 0 to 6.</summary>
	private static readonly byte[] SegmentMasks = [0x3F, 0x06, 0x5B, 0x4F, 0x66, 0x6D, 0x7D, 0x07, 0x7F, 0x6F];

	/// <summary>Draws one seven-segment digit, unlit segments ghosted.</summary>
	/// <param name="drawList">Where to draw.</param>
	/// <param name="min">Top left of the digit cell.</param>
	/// <param name="size">Size of the digit cell.</param>
	/// <param name="digit">The digit, 0 to 9; anything else lights only the middle bar.</param>
	/// <param name="lit">Colour of a lit segment.</param>
	/// <param name="ghost">Colour of an unlit segment.</param>
	internal static void SevenSegmentDigit(ImDrawListPtr drawList, Vector2 min, Vector2 size, int digit, Vector4 lit, Vector4 ghost)
	{
		int mask = digit is >= 0 and <= 9 ? SegmentMasks[digit] : 0x40;
		float t = MathF.Max(2f, size.X * 0.18f);
		float slant = size.X * 0.12f;
		float w = size.X - slant;
		float h = size.Y;
		float mid = h / 2f;

		Vector2 Skew(float x, float y) => new(min.X + x + (slant * (1f - (y / h))), min.Y + y);

		void Horizontal(int bit, float y)
		{
			float g = t * 0.15f;
			Vector2[] points =
			[
				Skew(g + (t / 2f), y),
				Skew(g + t, y - (t / 2f)),
				Skew(w - g - t, y - (t / 2f)),
				Skew(w - g - (t / 2f), y),
				Skew(w - g - t, y + (t / 2f)),
				Skew(g + t, y + (t / 2f)),
			];
			FillConvex(drawList, points, (mask & (1 << bit)) != 0 ? lit : ghost);
		}

		void Vertical(int bit, float x, float top, float bottom)
		{
			float g = t * 0.15f;
			Vector2[] points =
			[
				Skew(x, top + g + (t / 2f)),
				Skew(x + (t / 2f), top + g + t),
				Skew(x + (t / 2f), bottom - g - t),
				Skew(x, bottom - g - (t / 2f)),
				Skew(x - (t / 2f), bottom - g - t),
				Skew(x - (t / 2f), top + g + t),
			];
			FillConvex(drawList, points, (mask & (1 << bit)) != 0 ? lit : ghost);
		}

		Horizontal(0, t / 2f);
		Vertical(1, w - (t / 2f), t / 2f, mid);
		Vertical(2, w - (t / 2f), mid, h - (t / 2f));
		Horizontal(3, h - (t / 2f));
		Vertical(4, t / 2f, mid, h - (t / 2f));
		Vertical(5, t / 2f, t / 2f, mid);
		Horizontal(6, mid);
	}
}
