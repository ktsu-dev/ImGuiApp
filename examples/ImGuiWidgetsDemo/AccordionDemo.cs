// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;

/// <summary>
/// Expandable rows: one on its own, and an accordion that keeps one of several open.
/// </summary>
internal static class AccordionDemo
{
	private static bool detailsOpen;

	/// <summary>Gets a value indicating whether the standalone row is open.</summary>
	internal static bool DetailsOpen => detailsOpen;

	/// <summary>Gets the accordion, for tests to read which item is open.</summary>
	internal static ImGuiWidgets.Accordion Faq { get; private set; } = new();

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		Faq = new ImGuiWidgets.Accordion();
		detailsOpen = false;
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Mobile - Accordion"))
		{
			ImGui.TextWrapped("A row opens the content beneath it, which grows into place. An accordion keeps one of its rows open at a time.");

			ImGuiWidgets.ExpandableListItem("Order details", ref detailsOpen, () =>
			{
				ImGui.TextUnformatted("2 x Coffee beans, 500 g");
				ImGui.TextUnformatted("1 x Grinder");
				ImGui.TextUnformatted("Delivery: Thursday");
			});

			ImGui.Spacing();
			ImGui.TextDisabled("Questions");
			Faq.Item("How long does shipping take?", () => ImGui.TextWrapped("Two to four working days within the country."));
			Faq.Item("Can I return an item?", () => ImGui.TextWrapped("Yes, within thirty days, in its original packaging."));
			Faq.Item("Do you ship abroad?", () => ImGui.TextWrapped("To most countries; the cost is shown at checkout."));
		}
	}
}
