// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;

/// <summary>
/// The knob, in every variant, all driving one value.
/// </summary>
internal static class KnobDemo
{
	private static float value = 0.5f;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState() => value = 0.5f;

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Knobs"))
		{
			ImGui.TextUnformatted("All knob variants with interactive controls:");
			ImGui.Separator();

			// Show all knob variants
			ImGui.Columns(3, "KnobColumns");

			ImGuiWidgets.Knob("Wiper", ref value, 0, 1, 0, null, ImGuiKnobVariant.Wiper);
			ImGui.NextColumn();
			ImGuiWidgets.Knob("Wiper Only", ref value, 0, 1, 0, null, ImGuiKnobVariant.WiperOnly);
			ImGui.NextColumn();
			ImGuiWidgets.Knob("Wiper Dot", ref value, 0, 1, 0, null, ImGuiKnobVariant.WiperDot);
			ImGui.NextColumn();

			ImGuiWidgets.Knob("Tick", ref value, 0, 1, 0, null, ImGuiKnobVariant.Tick);
			ImGui.NextColumn();
			ImGuiWidgets.Knob("Stepped", ref value, 0, 1, 0, null, ImGuiKnobVariant.Stepped);
			ImGui.NextColumn();
			ImGuiWidgets.Knob("Space", ref value, 0, 1, 0, null, ImGuiKnobVariant.Space);

			ImGui.Columns(1);

			ImGui.Separator();
			ImGui.TextUnformatted($"Current Value: {value:F3}");

			if (DemoProbe.Button("Reset to 0.5"))
			{
				value = 0.5f;
			}
		}
	}
}
