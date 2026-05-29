// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a pill-shaped segmented control: a row of mutually-exclusive options with a sliding,
	/// animated highlight behind the selected segment (think iOS <c>UISegmentedControl</c>).
	/// </summary>
	/// <param name="label">A unique label used for the widget ID; not drawn.</param>
	/// <param name="selectedIndex">The index of the active segment. Updated in place when a segment is clicked.</param>
	/// <param name="segments">The segment captions, left to right.</param>
	/// <returns><see langword="true"/> if the selection changed this frame; otherwise <see langword="false"/>.</returns>
	public static bool SegmentedControl(string label, ref int selectedIndex, params string[] segments) =>
		SegmentedControlImpl.Draw(label, ref selectedIndex, segments);

	/// <summary>
	/// Draws a pill-shaped segmented control from any read-only list of captions.
	/// </summary>
	/// <param name="label">A unique label used for the widget ID; not drawn.</param>
	/// <param name="selectedIndex">The index of the active segment. Updated in place when a segment is clicked.</param>
	/// <param name="segments">The segment captions, left to right.</param>
	/// <returns><see langword="true"/> if the selection changed this frame; otherwise <see langword="false"/>.</returns>
	public static bool SegmentedControl(string label, ref int selectedIndex, IReadOnlyList<string> segments) =>
		SegmentedControlImpl.Draw(label, ref selectedIndex, segments);

	internal static class SegmentedControlImpl
	{
		// Per-ID animated highlight position (segment-index space); persists across frames.
		private static readonly Dictionary<uint, float> Highlight = [];

		private const float AnimationDuration = 0.15f;

		public static bool Draw(string label, ref int selectedIndex, IReadOnlyList<string> segments)
		{
			Ensure.NotNull(segments);
			if (segments.Count == 0)
			{
				return false;
			}

			uint id = ImGui.GetID(label);
			selectedIndex = Math.Clamp(selectedIndex, 0, segments.Count - 1);

			ImGuiStylePtr style = ImGui.GetStyle();
			float height = ImGui.GetFrameHeight();
			float padX = style.FramePadding.X * 2.0f;

			// Width: each segment sized to fit its text, all equal to the widest, with a sensible floor.
			float segmentWidth = 0.0f;
			for (int i = 0; i < segments.Count; i++)
			{
				segmentWidth = MathF.Max(segmentWidth, ImGui.CalcTextSize(segments[i]).X);
			}

			segmentWidth = MathF.Max(segmentWidth + padX, height * 1.5f);
			float totalWidth = segmentWidth * segments.Count;
			float rounding = height * 0.5f;

			Vector2 origin = ImGui.GetCursorScreenPos();
			ImGui.InvisibleButton(label, new Vector2(totalWidth, height));

			bool changed = false;
			if (ImGui.IsItemActive() && ImGui.IsItemHovered())
			{
				float localX = ImGui.GetIO().MousePos.X - origin.X;
				int hit = Math.Clamp((int)(localX / segmentWidth), 0, segments.Count - 1);
				if (hit != selectedIndex)
				{
					selectedIndex = hit;
					changed = true;
				}
			}

			// Slide the highlight toward the selected segment.
			if (!Highlight.TryGetValue(id, out float pos))
			{
				pos = selectedIndex;
			}

			float step = ImGui.GetIO().DeltaTime / AnimationDuration * MathF.Max(1.0f, MathF.Abs(selectedIndex - pos));
			pos = selectedIndex > pos ? MathF.Min(selectedIndex, pos + step) : MathF.Max(selectedIndex, pos - step);
			Highlight[id] = pos;

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			Span<Vector4> colors = ImGui.GetStyle().Colors;

			// Track background.
			drawList.AddRectFilled(origin, new Vector2(origin.X + totalWidth, origin.Y + height), ImGui.GetColorU32(colors[(int)ImGuiCol.FrameBg]), rounding);

			// Sliding selected-segment highlight.
			float highlightX = origin.X + (pos * segmentWidth);
			float pad = height * 0.08f;
			drawList.AddRectFilled(
				new Vector2(highlightX + pad, origin.Y + pad),
				new Vector2(highlightX + segmentWidth - pad, origin.Y + height - pad),
				ImGui.GetColorU32(colors[(int)ImGuiCol.ButtonActive]),
				rounding);

			// Captions.
			for (int i = 0; i < segments.Count; i++)
			{
				Vector2 textSize = ImGui.CalcTextSize(segments[i]);
				float cellX = origin.X + (i * segmentWidth);
				Vector2 textPos = new(
					cellX + ((segmentWidth - textSize.X) * 0.5f),
					origin.Y + ((height - textSize.Y) * 0.5f));
				uint textColor = ImGui.GetColorU32(i == selectedIndex ? colors[(int)ImGuiCol.Text] : colors[(int)ImGuiCol.TextDisabled]);
				drawList.AddText(textPos, textColor, segments[i]);
			}

			return changed;
		}
	}
}
