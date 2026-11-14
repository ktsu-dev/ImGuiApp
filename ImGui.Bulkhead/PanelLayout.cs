// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Bulkhead;

using System.Numerics;

/// <summary>Where each part of a <see cref="ImGuiBulkhead.Panel"/> goes, with no ImGui dependency.</summary>
internal readonly record struct PanelLayout(
	float ColumnWidth,
	float BarThickness,
	Vector2 TopElbow,
	Vector2 BottomElbow,
	float ColumnHeight,
	Vector2 ContentOffset,
	Vector2 ContentSize)
{
	/// <summary>Lays a panel of <paramref name="size"/> out at a given frame height.</summary>
	internal static PanelLayout For(Vector2 size, float frameHeight, BulkheadPanelOptions options)
	{
		float column = options.ColumnWidth > 0f ? options.ColumnWidth : frameHeight * 4f;
		float bar = options.BarThickness > 0f ? options.BarThickness : frameHeight * (2f / 3f);
		column = Math.Min(column, size.X * 0.5f);
		float gap = Math.Max(0f, options.Gap);

		// Each elbow is as tall as its bar plus a turn as deep as the bar again, then two bars' room more
		// for the column to read as a column before the gap.
		float elbowHeight = Math.Min(bar * 3f, size.Y * 0.5f);
		Vector2 top = new(size.X, elbowHeight);
		Vector2 bottom = new(size.X, elbowHeight);
		float columnHeight = Math.Max(0f, size.Y - (elbowHeight * 2f) - (gap * 2f));

		float innerLeft = column + (bar * 1.5f);
		float innerTop = bar + (gap * 2f);
		Vector2 contentOffset = new(innerLeft, innerTop);
		Vector2 contentSize = new(Math.Max(0f, size.X - innerLeft), Math.Max(0f, size.Y - (innerTop * 2f)));
		return new PanelLayout(column, bar, top, bottom, columnHeight, contentOffset, contentSize);
	}
}
