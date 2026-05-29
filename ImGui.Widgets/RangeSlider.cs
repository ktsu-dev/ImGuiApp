// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a dual-handle range slider for selecting a <paramref name="lower"/>/<paramref name="upper"/>
	/// span within <paramref name="min"/>..<paramref name="max"/>. The handles cannot cross and are
	/// kept at least <paramref name="minGap"/> apart.
	/// </summary>
	/// <param name="label">A unique label; text after <c>##</c> is hidden but used for the ID. Visible text is drawn to the right.</param>
	/// <param name="lower">The lower bound of the selected range. Updated in place.</param>
	/// <param name="upper">The upper bound of the selected range. Updated in place.</param>
	/// <param name="min">The minimum selectable value.</param>
	/// <param name="max">The maximum selectable value.</param>
	/// <param name="minGap">The minimum distance kept between the two handles.</param>
	/// <returns><see langword="true"/> if either value changed this frame; otherwise <see langword="false"/>.</returns>
	public static bool RangeSlider(string label, ref float lower, ref float upper, float min, float max, float minGap = 0.0f) =>
		RangeSliderImpl.Draw(label, ref lower, ref upper, min, max, minGap);

	internal static class RangeSliderImpl
	{
		// Per-ID active handle: 0 = lower, 1 = upper, -1 = none.
		private static readonly Dictionary<uint, int> ActiveHandle = [];

		public static bool Draw(string label, ref float lower, ref float upper, float min, float max, float minGap)
		{
			if (min > max)
			{
				(min, max) = (max, min);
			}

			minGap = Math.Clamp(minGap, 0.0f, max - min);

			uint id = ImGui.GetID(label);
			float height = ImGui.GetFrameHeight();
			float width = MathF.Max(ImGui.CalcItemWidth(), height * 3.0f);
			float grabRadius = height * 0.35f;

			Vector2 origin = ImGui.GetCursorScreenPos();
			ImGui.InvisibleButton(label, new Vector2(width, height));

			float trackMinX = origin.X + grabRadius;
			float trackMaxX = origin.X + width - grabRadius;
			float trackY = origin.Y + (height * 0.5f);
			float span = MathF.Max(trackMaxX - trackMinX, 1.0f);

			// Clamp inputs to a valid, non-crossing, gap-respecting state before interaction.
			lower = Math.Clamp(lower, min, max);
			upper = Math.Clamp(upper, min, max);
			if (upper - lower < minGap)
			{
				upper = MathF.Min(max, lower + minGap);
				lower = MathF.Min(lower, upper - minGap);
			}

			bool changed = false;

			if (ImGui.IsItemActivated())
			{
				// Grab whichever handle is nearer the click.
				float mouseX = ImGui.GetIO().MousePos.X;
				float lowerX = trackMinX + (ValueToFraction(lower, min, max) * span);
				float upperX = trackMinX + (ValueToFraction(upper, min, max) * span);
				ActiveHandle[id] = MathF.Abs(mouseX - lowerX) <= MathF.Abs(mouseX - upperX) ? 0 : 1;
			}

			if (ImGui.IsItemActive())
			{
				int handle = ActiveHandle.GetValueOrDefault(id, -1);
				float t = Math.Clamp((ImGui.GetIO().MousePos.X - trackMinX) / span, 0.0f, 1.0f);
				float newValue = min + (t * (max - min));

				if (handle == 0)
				{
					float clamped = Math.Clamp(newValue, min, upper - minGap);
					if (clamped != lower)
					{
						lower = clamped;
						changed = true;
					}
				}
				else if (handle == 1)
				{
					float clamped = Math.Clamp(newValue, lower + minGap, max);
					if (clamped != upper)
					{
						upper = clamped;
						changed = true;
					}
				}
			}
			else
			{
				ActiveHandle.Remove(id);
			}

			// Draw track, filled selection, and handles.
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			Span<Vector4> colors = ImGui.GetStyle().Colors;
			float trackThickness = MathF.Max(height * 0.18f, 2.0f);

			drawList.AddLine(new Vector2(trackMinX, trackY), new Vector2(trackMaxX, trackY), ImGui.GetColorU32(colors[(int)ImGuiCol.FrameBg]), trackThickness);

			float lowerX2 = trackMinX + (ValueToFraction(lower, min, max) * span);
			float upperX2 = trackMinX + (ValueToFraction(upper, min, max) * span);
			drawList.AddLine(new Vector2(lowerX2, trackY), new Vector2(upperX2, trackY), ImGui.GetColorU32(colors[(int)ImGuiCol.SliderGrab]), trackThickness);

			bool hovered = ImGui.IsItemHovered() || ImGui.IsItemActive();
			uint grabColor = ImGui.GetColorU32(hovered ? colors[(int)ImGuiCol.SliderGrabActive] : colors[(int)ImGuiCol.SliderGrab]);
			drawList.AddCircleFilled(new Vector2(lowerX2, trackY), grabRadius, grabColor, 24);
			drawList.AddCircleFilled(new Vector2(upperX2, trackY), grabRadius, grabColor, 24);

			if (ImGui.IsItemHovered() || ImGui.IsItemActive())
			{
				ImGui.SetTooltip(string.Format(CultureInfo.CurrentCulture, "{0:0.###} – {1:0.###}", lower, upper));
			}

			string visible = VisibleLabel(label);
			if (visible.Length > 0)
			{
				ImGui.SameLine();
				ImGui.AlignTextToFramePadding();
				ImGui.TextUnformatted(visible);
			}

			return changed;
		}

		private static float ValueToFraction(float value, float min, float max) =>
			max <= min ? 0.0f : Math.Clamp((value - min) / (max - min), 0.0f, 1.0f);
	}
}
