// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Numerics;

using Hexa.NET.ImGui;

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
	/// The fill is colored green below -6 dB, amber up to 0 dB, and red above 0 dB, the conventional
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

		Vector2 max = new(cursorPos.X + meterSize.X, cursorPos.Y + meterSize.Y);
		MeterScale.DrawVerticalMeter(ImGui.GetWindowDrawList(), cursorPos, max, db, minDb, maxDb, peakDb);
		ImGui.PopID();
	}
}
