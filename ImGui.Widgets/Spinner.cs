// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using ktsu.ImGui.Color;
using ktsu.Semantics.Color;

using HexaSpinner = Hexa.NET.ImGui.Widgets.ImGuiSpinner;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws an indeterminate loading spinner that animates from the ImGui frame time.
	/// </summary>
	/// <param name="radius">Radius of the spinner in pixels.</param>
	/// <param name="thickness">Stroke thickness of the spinner arc in pixels.</param>
	/// <param name="color">Color of the spinner arc.</param>
	public static void Spinner(float radius, float thickness, Srgb color) =>
		HexaSpinner.Spinner(radius, thickness, color.ToImGuiU32());
}
