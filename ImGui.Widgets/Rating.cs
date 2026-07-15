// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.ImGui.Styler;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws an interactive star rating. Hovering previews the value the user would set; clicking commits it.
	/// </summary>
	/// <param name="id">Unique widget identifier.</param>
	/// <param name="value">The current rating, in the range 0..<paramref name="starCount"/>. Updated in place when the user clicks.</param>
	/// <param name="starCount">The number of stars.</param>
	/// <param name="allowHalf">When true, the rating snaps to half-star increments instead of whole stars.</param>
	/// <param name="readOnly">When true, the rating is drawn but not interactive.</param>
	/// <param name="size">Per-star size in pixels. When 0, defaults to the frame height so it scales with DPI.</param>
	/// <returns>True if <paramref name="value"/> changed this frame.</returns>
	public static bool Rating(string id, ref float value, int starCount = 5, bool allowHalf = false, bool readOnly = false, float size = 0f) =>
		RatingImpl.Draw(id, ref value, starCount, allowHalf, readOnly, size);

	/// <summary>
	/// Maps a horizontal offset (relative to the left edge of the first star) to a rating value, snapping to
	/// whole stars or — when <paramref name="allowHalf"/> is true — half stars. The result is clamped to
	/// 0..<paramref name="starCount"/>.
	/// </summary>
	/// <param name="localX">Horizontal offset from the left edge of the rating, in pixels.</param>
	/// <param name="starPitch">Distance between the left edges of adjacent stars (star size plus spacing), in pixels.</param>
	/// <param name="starCount">The number of stars.</param>
	/// <param name="allowHalf">When true, snap to half-star increments.</param>
	/// <returns>The snapped rating value.</returns>
	public static float RatingValueFromOffset(float localX, float starPitch, int starCount, bool allowHalf)
	{
		if (starPitch <= 0f || starCount <= 0)
		{
			return 0f;
		}

		float raw = localX / starPitch;
		float snapped = allowHalf ? MathF.Ceiling(raw * 2.0f) * 0.5f : MathF.Ceiling(raw);
		return Math.Clamp(snapped, 0f, starCount);
	}

	/// <summary>
	/// Returns how much of the star at <paramref name="starIndex"/> (zero-based) should be filled for the
	/// given rating <paramref name="value"/>, as a fraction in the range 0..1.
	/// </summary>
	/// <param name="value">The current rating value.</param>
	/// <param name="starIndex">Zero-based index of the star.</param>
	/// <returns>The fill fraction for the star.</returns>
	public static float StarFillFraction(float value, int starIndex) => Math.Clamp(value - starIndex, 0f, 1f);

	internal static class RatingImpl
	{
		internal static bool Draw(string id, ref float value, int starCount, bool allowHalf, bool readOnly, float size)
		{
			starCount = Math.Max(1, starCount);
			float starSize = size > 0f ? size : ImGui.GetFrameHeight();
			float spacing = ImGui.GetStyle().ItemInnerSpacing.X;
			float pitch = starSize + spacing;
			float totalWidth = (starSize * starCount) + (spacing * (starCount - 1));

			Vector2 origin = ImGui.GetCursorScreenPos();
			bool clicked = ImGui.InvisibleButton(id, new Vector2(totalWidth, starSize));
			bool hovered = !readOnly && ImGui.IsItemHovered();

			float displayValue = value;
			bool changed = false;
			if (hovered)
			{
				float localX = ImGui.GetMousePos().X - origin.X;
				displayValue = RatingValueFromOffset(localX, pitch, starCount, allowHalf);
				if (clicked)
				{
					value = displayValue;
					changed = true;
				}
			}

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			uint emptyColor = ImGui.GetColorU32(ImGuiCol.FrameBg);
			uint filledColor = Palette.Basic.Yellow.ToImGuiU32();
			float radius = starSize * 0.5f;

			for (int i = 0; i < starCount; i++)
			{
				Vector2 center = new(origin.X + (i * pitch) + radius, origin.Y + radius);
				DrawStar(drawList, center, radius, emptyColor, 1f);

				float fill = StarFillFraction(displayValue, i);
				if (fill > 0f)
				{
					ImGui.PushClipRect(new Vector2(center.X - radius, center.Y - radius), new Vector2(center.X - radius + (starSize * fill), center.Y + radius), true);
					DrawStar(drawList, center, radius, filledColor, 1f);
					ImGui.PopClipRect();
				}
			}

			return changed;
		}

		private static void DrawStar(ImDrawListPtr drawList, Vector2 center, float radius, uint color, float scale)
		{
			const int points = 5;
			float outer = radius * 0.95f * scale;
			float inner = outer * 0.4f;

			Span<Vector2> verts = stackalloc Vector2[points * 2];
			// Start at the top point (-90°) and alternate outer/inner radii around the circle.
			float step = MathF.PI / points;
			for (int i = 0; i < verts.Length; i++)
			{
				float r = (i % 2) == 0 ? outer : inner;
				float angle = (-MathF.PI * 0.5f) + (i * step);
				verts[i] = new Vector2(center.X + (MathF.Cos(angle) * r), center.Y + (MathF.Sin(angle) * r));
			}

			// A star is star-shaped about its centre, so a triangle fan from the centre fills it correctly.
			for (int i = 0; i < verts.Length; i++)
			{
				drawList.AddTriangleFilled(center, verts[i], verts[(i + 1) % verts.Length], color);
			}
		}
	}
}
