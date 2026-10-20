// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;
using ktsu.ImGui.Widgets.Overlays;

/// <summary>
/// The search overlay: a search that covers the window, with the matches listed beneath it.
/// </summary>
internal static class SearchOverlayDemo
{
	private static readonly string[] Fruit =
	[
		"Apple", "Apricot", "Avocado", "Banana", "Blackberry", "Blueberry", "Cherry", "Coconut", "Date", "Dragon fruit",
		"Fig", "Grape", "Guava", "Kiwi", "Lemon", "Lime", "Lychee", "Mango", "Nectarine", "Orange", "Papaya", "Passionfruit",
		"Peach", "Pear", "Pineapple", "Plum", "Pomegranate", "Raspberry", "Strawberry", "Watermelon",
	];

	private static readonly OverlayHost Host = new();
	private static ImGuiWidgets.SearchOverlay search = new(Host, "demoSearch");

	/// <summary>Gets the item last chosen.</summary>
	internal static string Chosen { get; private set; } = "(none)";

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		Host.Clear();
		search = new ImGuiWidgets.SearchOverlay(Host, "demoSearch");
		Chosen = "(none)";
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Mobile - Search Overlay"))
		{
			ImGui.TextWrapped("The search covers the whole window. Type to filter, use the arrow keys and Enter, or click a result. The back button or Escape closes it.");

			if (DemoProbe.Button("Search fruit##searchOpen"))
			{
				search.Open(Fruit, item => Chosen = item, "Search fruit");
			}

			ImGui.SameLine();
			ImGui.TextUnformatted($"Chosen: {Chosen}");
			Host.Render();
		}
	}
}
