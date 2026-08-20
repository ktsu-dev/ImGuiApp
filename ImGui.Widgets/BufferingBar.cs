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
	public static void BufferingBar(float value, Vector2 size, Srgb background, Srgb foreground) =>
		HexaProgressBar.ProgressBar(value, size, background.ToImGuiU32(), foreground.ToImGuiU32());
}
