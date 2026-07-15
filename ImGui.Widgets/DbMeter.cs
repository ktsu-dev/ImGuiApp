// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a vertical audio level meter calibrated in decibels, with an optional peak-hold marker.
	/// </summary>
	/// <param name="label">A unique identifier for the meter (used to size and reserve layout space).</param>
	/// <param name="db">The current level, in decibels (for example dBFS).</param>
	/// <param name="size">The meter size in pixels. Non-positive components fall back to sensible defaults.</param>
	/// <param name="minDb">The level mapped to the bottom of the meter.</param>
	/// <param name="maxDb">The level mapped to the top of the meter.</param>
	/// <param name="peakDb">An optional peak-hold level to mark; pass <see cref="float.NegativeInfinity"/> to hide it.</param>
	/// <remarks>
	/// The fill is coloured green below -6 dB, amber up to 0 dB, and red above 0 dB, the conventional
	/// nominal/peak zones of a digital meter.
	/// </remarks>
	public static void DbMeter(string label, float db, Vector2 size, float minDb = -60f, float maxDb = 6f, float peakDb = float.NegativeInfinity)
	{
		ImGui.PushID(label);
		float lineHeight = ImGui.GetTextLineHeight();
		Vector2 meterSize = new(
			size.X > 0 ? size.X : lineHeight * 1.5f,
			size.Y > 0 ? size.Y : lineHeight * 8.0f);

		Vector2 cursorPos = ImGui.GetCursorScreenPos();
		ImGui.Dummy(meterSize);

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		Span<Vector4> colors = ImGui.GetStyle().Colors;
		Vector2 min = cursorPos;
		Vector2 max = new(cursorPos.X + meterSize.X, cursorPos.Y + meterSize.Y);

		// Background and border.
		drawList.AddRectFilled(min, max, ImGui.GetColorU32(colors[(int)ImGuiCol.FrameBg]));

		float range = maxDb - minDb;
		float fill = range > 0 ? Math.Clamp((db - minDb) / range, 0.0f, 1.0f) : 0.0f;
		float fillTop = max.Y - (fill * meterSize.Y);

		if (fill > 0.0f)
		{
			drawList.AddRectFilled(new Vector2(min.X, fillTop), max, ImGui.GetColorU32(ZoneColor(db).Value));
		}

		// Peak-hold marker.
		if (float.IsFinite(peakDb) && range > 0)
		{
			float peak = Math.Clamp((peakDb - minDb) / range, 0.0f, 1.0f);
			float peakY = max.Y - (peak * meterSize.Y);
			drawList.AddLine(new Vector2(min.X, peakY), new Vector2(max.X, peakY), ImGui.GetColorU32(ZoneColor(peakDb).Value), 2.0f);
		}

		drawList.AddRect(min, max, ImGui.GetColorU32(colors[(int)ImGuiCol.Border]));
		ImGui.PopID();
	}

	/// <summary>
	/// Returns the conventional meter colour for a level in decibels.
	/// </summary>
	/// <param name="db">The level, in decibels.</param>
	/// <returns>Green below -6 dB, amber up to 0 dB, otherwise red.</returns>
	private static ImColor ZoneColor(float db) => db switch
	{
		> 0.0f => ImColors.FromRgba(0.90f, 0.20f, 0.20f, 1.0f),
		> -6.0f => ImColors.FromRgba(0.90f, 0.78f, 0.20f, 1.0f),
		_ => ImColors.FromRgba(0.25f, 0.80f, 0.35f, 1.0f),
	};
}
