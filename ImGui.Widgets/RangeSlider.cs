// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

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
		private static readonly Dictionary<uint, HandleTrackState> States = [];

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
			ImGuiProbes.MarkItem(label);

			float trackMinX = origin.X + grabRadius;
			float trackMaxX = origin.X + width - grabRadius;
			float trackY = origin.Y + (height * 0.5f);
			float span = MathF.Max(trackMaxX - trackMinX, 1.0f);

			Span<float> handles = [lower, upper];
			HandleTrackState.Normalize(handles, min, max, minGap);

			if (!States.TryGetValue(id, out HandleTrackState? state))
			{
				state = new HandleTrackState();
				States[id] = state;
			}

			float mouseValue = min + (Math.Clamp((ImGui.GetIO().MousePos.X - trackMinX) / span, 0.0f, 1.0f) * (max - min));

			if (ImGui.IsItemActivated())
			{
				state.Activate(handles, mouseValue);
			}

			bool changed = false;
			if (ImGui.IsItemActive())
			{
				changed = state.Drag(handles, mouseValue, min, max, minGap);
			}
			else
			{
				state.Release();
			}

			lower = handles[0];
			upper = handles[1];

			DrawSlider(label, lower, upper, min, max, trackMinX, trackMaxX, trackY, span, grabRadius, height);

			return changed;
		}

		[SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Private rendering helper extracted from Draw to reduce cognitive complexity; the parameters thread the slider geometry computed once by the caller and bundling them would not improve readability.")]
		private static void DrawSlider(string label, float lower, float upper, float min, float max, float trackMinX, float trackMaxX, float trackY, float span, float grabRadius, float height)
		{
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
		}

		private static float ValueToFraction(float value, float min, float max) =>
			max <= min ? 0.0f : Math.Clamp((value - min) / (max - min), 0.0f, 1.0f);
	}
}
