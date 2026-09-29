// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using Hexa.NET.ImGui;
using ktsu.ImGui.Styler;
using ktsu.ImGui.Widgets;

/// <summary>
/// The color indicator.
/// </summary>
internal static class ColorIndicatorDemo
{
	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		// Nothing to reset: the section draws fixed colors and keeps no state.
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Color Indicators"))
		{
			ImGui.TextUnformatted("Color indicators show enabled/disabled states:");
			ImGui.Separator();

			ImGui.TextUnformatted("Status Lights:");
			ImGuiWidgets.ColorIndicator(Palette.Semantic.Success, true);
			ImGui.SameLine();
			ImGui.TextUnformatted("System OK");
			ImGuiWidgets.ColorIndicator(Palette.Semantic.Warning, true);
			ImGui.SameLine();
			ImGui.TextUnformatted("Warning");
			ImGuiWidgets.ColorIndicator(Palette.Semantic.Error, true);
			ImGui.SameLine();
			ImGui.TextUnformatted("Error");
			ImGuiWidgets.ColorIndicator(Palette.Semantic.Info, true);
			ImGui.SameLine();
			ImGui.TextUnformatted("Info");

			ImGui.Separator();
			ImGui.TextUnformatted("Enabled vs Disabled:");
			ImGuiWidgets.ColorIndicator(Palette.Semantic.Success, true);
			ImGui.SameLine();
			ImGui.TextUnformatted("Enabled");
			ImGuiWidgets.ColorIndicator(Palette.Semantic.Success, false);
			ImGui.SameLine();
			ImGui.TextUnformatted("Disabled");
		}
	}
}
