// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Numerics;

using ktsu.ImGui.Color;
using ktsu.Semantics.Color;

using HexaProgressBar = Hexa.NET.ImGui.Widgets.ImGuiProgressBar;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a horizontal buffering bar filled from the left in proportion to <paramref name="value"/>.
	/// </summary>
	/// <param name="value">Fill fraction, clamped by the underlying implementation to the range 0 to 1.</param>
	/// <param name="size">Size of the bar in pixels.</param>
	/// <param name="background">Color of the unfilled portion.</param>
	/// <param name="foreground">Color of the filled portion.</param>
	/// <remarks>
	/// Marked for UI probes as <c>buffering-bar</c>; separate two in one window with
	/// <see cref="ScopedId"/>. Hexa paints the bar straight into the draw list without submitting an
	/// item, so the mark is the box around what it painted.
	/// </remarks>
	public static void BufferingBar(float value, Vector2 size, Srgb background, Srgb foreground) =>
		DrawListProbe.MarkDrawn("buffering-bar", DrawBufferingBar, (value, size, background.ToImGuiU32(), foreground.ToImGuiU32()));

	private static void DrawBufferingBar((float Value, Vector2 Size, uint Background, uint Foreground) bar) =>
		HexaProgressBar.ProgressBar(bar.Value, bar.Size, bar.Background, bar.Foreground);
}
