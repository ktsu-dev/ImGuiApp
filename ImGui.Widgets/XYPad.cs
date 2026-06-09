// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws an interactive XY pad that edits two normalized parameters at once.
	/// </summary>
	/// <param name="label">A unique identifier for the pad.</param>
	/// <param name="x">The horizontal value in the range <c>[0, 1]</c>, updated while dragging.</param>
	/// <param name="y">The vertical value in the range <c>[0, 1]</c> (increasing upward), updated while dragging.</param>
	/// <param name="size">The pad size in pixels. Non-positive components fall back to a square default.</param>
	/// <returns><see langword="true"/> if the value changed this frame; otherwise <see langword="false"/>.</returns>
	/// <remarks>
	/// XY pads are the natural control for paired parameters such as a filter's cutoff and resonance or a
	/// panner's position. The handle tracks the pointer while the control is active.
	/// </remarks>
	public static bool XYPad(string label, ref float x, ref float y, Vector2 size)
	{
		float lineHeight = ImGui.GetTextLineHeight();
		float side = lineHeight * 8.0f;
		Vector2 padSize = new(
			size.X > 0 ? size.X : side,
			size.Y > 0 ? size.Y : side);

		Vector2 cursorPos = ImGui.GetCursorScreenPos();
		ImGui.InvisibleButton(label, padSize);

		bool changed = false;
		if (ImGui.IsItemActive() && padSize.X > 0 && padSize.Y > 0)
		{
			Vector2 mouse = ImGui.GetMousePos();
			// While the pad is being dragged, track the pointer and report an edit each frame, matching
			// how immediate-mode sliders behave. Screen Y grows downward, so invert it for an upward axis.
			x = Math.Clamp((mouse.X - cursorPos.X) / padSize.X, 0.0f, 1.0f);
			y = Math.Clamp(1.0f - ((mouse.Y - cursorPos.Y) / padSize.Y), 0.0f, 1.0f);
			changed = true;
		}

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		Span<Vector4> colors = ImGui.GetStyle().Colors;
		Vector2 min = cursorPos;
		Vector2 max = new(cursorPos.X + padSize.X, cursorPos.Y + padSize.Y);

		drawList.AddRectFilled(min, max, ImGui.GetColorU32(colors[(int)ImGuiCol.FrameBg]));

		// Crosshair at the current value.
		float clampedX = Math.Clamp(x, 0.0f, 1.0f);
		float clampedY = Math.Clamp(y, 0.0f, 1.0f);
		Vector2 handle = new(min.X + (clampedX * padSize.X), max.Y - (clampedY * padSize.Y));
		uint guideColor = ImGui.GetColorU32(colors[(int)ImGuiCol.Border]);
		drawList.AddLine(new Vector2(min.X, handle.Y), new Vector2(max.X, handle.Y), guideColor);
		drawList.AddLine(new Vector2(handle.X, min.Y), new Vector2(handle.X, max.Y), guideColor);
		drawList.AddCircleFilled(handle, MathF.Max(4.0f, lineHeight * 0.25f), ImGui.GetColorU32(colors[(int)ImGuiCol.SliderGrabActive]));

		drawList.AddRect(min, max, guideColor);
		return changed;
	}
}
