// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using Hexa.NET.ImGui;
using ktsu.ImGui.Probes;
using ktsu.ImGui.Widgets;

/// <summary>
/// The scoped helpers, such as scoped disabling and identifiers.
/// </summary>
internal static class ScopedUtilitiesDemo
{
	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		// Nothing to reset: the section keeps no state.
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Scoped Utilities"))
		{
			ImGui.TextUnformatted("Scoped helpers for ImGui state management:");
			ImGui.Separator();

			// ScopedDisable demo
			ImGui.TextUnformatted("ScopedDisable - disables widgets within scope:");
			using (new ScopedDisable(true))
			{
				bool dummyBool = true;
				int dummyInt = 0;
				string[] items = ["Item 1", "Item 2", "Item 3"];

				DemoProbe.Checkbox("Disabled Checkbox", ref dummyBool);
				ImGui.Combo("Disabled Combo", ref dummyInt, items, items.Length);
				ImGuiProbes.MarkItem("Disabled Combo");
				DemoProbe.Button("Disabled Button");
			}

			ImGui.Separator();

			// ScopedId demo
			ImGui.TextUnformatted("ScopedId - manages ImGui ID stack automatically:");
			for (int i = 0; i < 3; i++)
			{
				using (new ImGuiWidgets.ScopedId(i))
				{
					bool state = false;
					DemoProbe.Checkbox("Same Label", ref state);
				}
			}
			ImGui.TextUnformatted("↑ Three checkboxes with same label using ScopedId");
		}
	}
}
