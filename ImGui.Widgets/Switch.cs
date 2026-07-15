// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.ImGui.Widgets.Animation;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws an iOS-style toggle switch with an animated thumb. The track colour interpolates
	/// between the frame background (off) and the check-mark accent (on) as the thumb slides.
	/// </summary>
	/// <param name="label">A unique label; text after <c>##</c> is hidden but used for the ID. Visible text is drawn to the right of the switch.</param>
	/// <param name="value">The current on/off state. Toggled in place when the switch is clicked.</param>
	/// <returns><see langword="true"/> if the value changed this frame; otherwise <see langword="false"/>.</returns>
	public static bool Switch(string label, ref bool value) => SwitchImpl.Draw(label, ref value);

	internal static class SwitchImpl
	{
		// Per-ID thumb animation progress in [0, 1]; persists across frames in immediate mode.
		private static readonly Dictionary<uint, float> Progress = [];

		// Seconds for the thumb to travel end to end.
		private const float AnimationDuration = 0.12f;

		public static bool Draw(string label, ref bool value)
		{
			uint id = ImGui.GetID(label);

			float height = ImGui.GetFrameHeight();
			float width = height * 1.75f;
			float radius = height * 0.5f;

			Vector2 origin = ImGui.GetCursorScreenPos();
			ImGui.InvisibleButton(label, new Vector2(width, height));

			bool changed = false;
			if (ImGui.IsItemClicked())
			{
				value = !value;
				changed = true;
			}

			// Advance the thumb toward its target, easing for a natural feel.
			float target = value ? 1.0f : 0.0f;
			if (!Progress.TryGetValue(id, out float t))
			{
				t = target;
			}

			float delta = ImGui.GetIO().DeltaTime / AnimationDuration;
			t = target > t ? MathF.Min(target, t + delta) : MathF.Max(target, t - delta);
			Progress[id] = t;

			float eased = Easing.OutCubic(Math.Clamp(t, 0.0f, 1.0f));

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			Span<Vector4> colors = ImGui.GetStyle().Colors;
			Vector4 offColor = colors[(int)ImGuiCol.FrameBg];
			Vector4 onColor = colors[(int)ImGuiCol.CheckMark];
			Vector4 trackColor = Lerp(offColor, onColor, eased);

			bool hovered = ImGui.IsItemHovered();
			if (hovered)
			{
				Vector4 hoverTint = colors[(int)ImGuiCol.FrameBgHovered];
				trackColor = Lerp(trackColor, hoverTint, 0.25f);
			}

			drawList.AddRectFilled(origin, new Vector2(origin.X + width, origin.Y + height), ImGui.GetColorU32(trackColor), radius);

			float pad = height * 0.1f;
			float thumbRadius = radius - pad;
			float minX = origin.X + radius;
			float maxX = origin.X + width - radius;
			float thumbX = minX + ((maxX - minX) * eased);
			Vector2 thumbCenter = new(thumbX, origin.Y + radius);
			drawList.AddCircleFilled(thumbCenter, thumbRadius, ImColors.FromRgba(1f, 1f, 1f, 1f).ToImGuiU32(), 32);

			// Visible label to the right of the switch (text before ## only).
			string visible = VisibleLabel(label);
			if (visible.Length > 0)
			{
				ImGui.SameLine();
				ImGui.AlignTextToFramePadding();
				ImGui.TextUnformatted(visible);
			}

			return changed;
		}

		private static Vector4 Lerp(Vector4 a, Vector4 b, float t) => a + ((b - a) * Math.Clamp(t, 0.0f, 1.0f));
	}
}
