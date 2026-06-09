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
	/// Draws an oscilloscope-style waveform of a block of audio samples.
	/// </summary>
	/// <param name="label">A unique identifier for the scope (used to size and reserve layout space).</param>
	/// <param name="samples">The samples to plot, expected in the range <c>[-1, 1]</c>.</param>
	/// <param name="size">The scope size in pixels. Non-positive components fall back to sensible defaults.</param>
	/// <param name="amplitude">A vertical scale applied to the samples before plotting.</param>
	/// <remarks>
	/// Samples are drawn left to right with the zero line through the vertical centre; values are clamped
	/// to the plot area so out-of-range peaks do not draw outside the widget.
	/// </remarks>
	public static void Scope(string label, ReadOnlySpan<float> samples, Vector2 size, float amplitude = 1.0f)
	{
		ImGui.PushID(label);
		float lineHeight = ImGui.GetTextLineHeight();
		Vector2 scopeSize = new(
			size.X > 0 ? size.X : lineHeight * 12.0f,
			size.Y > 0 ? size.Y : lineHeight * 5.0f);

		Vector2 cursorPos = ImGui.GetCursorScreenPos();
		ImGui.Dummy(scopeSize);

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		Span<Vector4> colors = ImGui.GetStyle().Colors;
		Vector2 min = cursorPos;
		Vector2 max = new(cursorPos.X + scopeSize.X, cursorPos.Y + scopeSize.Y);
		float midY = cursorPos.Y + (scopeSize.Y * 0.5f);
		float halfHeight = scopeSize.Y * 0.5f;

		drawList.AddRectFilled(min, max, ImGui.GetColorU32(colors[(int)ImGuiCol.FrameBg]));

		// Zero line.
		drawList.AddLine(new Vector2(min.X, midY), new Vector2(max.X, midY), ImGui.GetColorU32(colors[(int)ImGuiCol.Border]));

		if (samples.Length >= 2)
		{
			uint waveColor = ImGui.GetColorU32(colors[(int)ImGuiCol.PlotLines]);
			float step = scopeSize.X / (samples.Length - 1);

			for (int i = 0; i < samples.Length - 1; i++)
			{
				float v0 = Math.Clamp(samples[i] * amplitude, -1.0f, 1.0f);
				float v1 = Math.Clamp(samples[i + 1] * amplitude, -1.0f, 1.0f);

				Vector2 p0 = new(min.X + (i * step), midY - (v0 * halfHeight));
				Vector2 p1 = new(min.X + ((i + 1) * step), midY - (v1 * halfHeight));
				drawList.AddLine(p0, p1, waveColor);
			}
		}

		drawList.AddRect(min, max, ImGui.GetColorU32(colors[(int)ImGuiCol.Border]));
		ImGui.PopID();
	}
}
