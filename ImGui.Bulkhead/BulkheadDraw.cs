// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Bulkhead;

using System.Numerics;

using Hexa.NET.ImGui;

/// <summary>The shapes every Bulkhead widget is drawn from, with no ImGui item of their own.</summary>
internal static class BulkheadDraw
{
	/// <summary>Points per quarter circle, enough that an arc a few hundred pixels across stays smooth.</summary>
	private const int ArcSegments = 16;

	/// <summary>Fills a rectangle whose start and/or end are rounded into semicircles.</summary>
	internal static void Capsule(ImDrawListPtr drawList, Vector2 min, Vector2 max, uint color, BulkheadCaps caps)
	{
		Vector2 size = max - min;
		if (size.X <= 0 || size.Y <= 0)
		{
			return;
		}

		bool wide = size.X >= size.Y;
		float rounding = (wide ? size.Y : size.X) * 0.5f;
		ImDrawFlags flags = CornerFlags(caps, wide);
		if (flags == ImDrawFlags.RoundCornersNone)
		{
			drawList.AddRectFilled(min, max, color);
		}
		else
		{
			drawList.AddRectFilled(min, max, color, rounding, flags);
		}
	}

	/// <summary>The ImGui corner flags that round the requested ends of a shape.</summary>
	internal static ImDrawFlags CornerFlags(BulkheadCaps caps, bool wide)
	{
		bool start = caps.HasFlag(BulkheadCaps.Start);
		bool end = caps.HasFlag(BulkheadCaps.End);
		return (start, end, wide) switch
		{
			(true, true, _) => ImDrawFlags.RoundCornersAll,
			(true, false, true) => ImDrawFlags.RoundCornersLeft,
			(false, true, true) => ImDrawFlags.RoundCornersRight,
			(true, false, false) => ImDrawFlags.RoundCornersTop,
			(false, true, false) => ImDrawFlags.RoundCornersBottom,
			_ => ImDrawFlags.RoundCornersNone,
		};
	}

	/// <summary>
	/// Fills an elbow: a column of width <paramref name="columnWidth"/> along one side of the
	/// rectangle that turns through a rounded corner into a bar of thickness
	/// <paramref name="barThickness"/> along the top or bottom.
	/// </summary>
	internal static void Elbow(ImDrawListPtr drawList, Vector2 min, Vector2 size, BulkheadCorner corner, float columnWidth, float barThickness, uint color)
	{
		IReadOnlyList<Vector2> outline = ElbowOutline(size, corner, columnWidth, barThickness);
		if (outline.Count < 3)
		{
			return;
		}

		foreach (Vector2 point in outline)
		{
			drawList.PathLineTo(min + point);
		}

		drawList.PathFillConcave(color);
	}

	/// <summary>
	/// The elbow's outline in the rectangle's own coordinates, worked out for the top-left corner
	/// and mirrored into the others. It has no ImGui dependency, so its geometry is unit tested.
	/// </summary>
	internal static IReadOnlyList<Vector2> ElbowOutline(Vector2 size, BulkheadCorner corner, float columnWidth, float barThickness)
	{
		if (size.X <= 0 || size.Y <= 0)
		{
			return [];
		}

		float column = Math.Clamp(columnWidth, 1f, size.X);
		float bar = Math.Clamp(barThickness, 1f, size.Y);

		// The inner corner's radius matches the bar, and the outer one wraps around it, so the
		// curve keeps roughly the same thickness as it turns. Both shrink to fit a small rectangle.
		float inner = Math.Max(0f, Math.Min(bar, Math.Min(size.X - column, size.Y - bar)));
		float outer = Math.Min(column + inner, Math.Min(size.X, size.Y));

		List<Vector2> points = [new(0f, size.Y), new(0f, outer)];
		AddArc(points, new Vector2(outer, outer), outer, MathF.PI, MathF.PI * 1.5f);
		points.Add(new Vector2(size.X, 0f));
		points.Add(new Vector2(size.X, bar));
		if (inner > 0f)
		{
			AddArc(points, new Vector2(column + inner, bar + inner), inner, MathF.PI * 1.5f, MathF.PI);
		}
		else
		{
			points.Add(new Vector2(column, bar));
		}

		points.Add(new Vector2(column, size.Y));

		bool mirrorX = corner is BulkheadCorner.TopRight or BulkheadCorner.BottomRight;
		bool mirrorY = corner is BulkheadCorner.BottomLeft or BulkheadCorner.BottomRight;
		for (int i = 0; i < points.Count; i++)
		{
			Vector2 p = points[i];
			points[i] = new Vector2(mirrorX ? size.X - p.X : p.X, mirrorY ? size.Y - p.Y : p.Y);
		}

		// One mirror reverses the winding; keep it consistent for the triangulator.
		if (mirrorX ^ mirrorY)
		{
			points.Reverse();
		}

		return points;
	}

	private static void AddArc(List<Vector2> points, Vector2 center, float radius, float from, float to)
	{
		for (int i = 0; i <= ArcSegments; i++)
		{
			float angle = from + ((to - from) * i / ArcSegments);
			points.Add(center + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius));
		}
	}

	/// <summary>The part of a label ImGui would show: everything before a <c>##</c>, upper-cased.</summary>
	internal static string DisplayText(string label)
	{
		int hidden = label.IndexOf("##", StringComparison.Ordinal);
		string visible = hidden >= 0 ? label[..hidden] : label;
		return visible.ToUpperInvariant();
	}

	/// <summary>Resolves a requested width the way ImGui does: positive is literal, zero is natural, negative is "all but".</summary>
	internal static float ResolveWidth(float requested, float natural)
	{
		if (requested > 0f)
		{
			return requested;
		}

		if (requested < 0f)
		{
			return Math.Max(1f, ImGui.GetContentRegionAvail().X + requested);
		}

		return natural;
	}
}
