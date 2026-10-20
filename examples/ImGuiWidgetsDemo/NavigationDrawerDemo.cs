// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using Hexa.NET.ImGui;
using ktsu.ImGui.Probes;
using ktsu.ImGui.Widgets;
using ktsu.ImGui.Widgets.Overlays;

/// <summary>
/// The navigation drawer: a panel that slides in from the left edge over everything else.
/// </summary>
internal static class NavigationDrawerDemo
{
	private static readonly string[] Destinations = ["Inbox", "Starred", "Sent", "Drafts", "Trash"];
	private static readonly OverlayHost Host = new();

	/// <summary>Gets the destination picked in the Drawer.</summary>
	internal static string Destination { get; private set; } = Destinations[0];

	/// <summary>Gets the drawer, so a test can read whether it is open.</summary>
	internal static ImGuiWidgets.NavigationDrawer Drawer { get; private set; } = Create();

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		Host.Clear();
		Drawer = Create();
		Destination = Destinations[0];
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Mobile - Navigation Drawer"))
		{
			ImGui.TextWrapped("Open the drawer with the button, or drag in from the window's left edge while this section is open. Tap outside it, drag it back or press Escape to close it.");

			if (DemoProbe.Button("Open drawer##drawerOpen"))
			{
				Drawer.Open();
			}

			ImGui.SameLine();
			ImGui.TextUnformatted($"Showing: {Destination}");
			Drawer.TrackEdgeSwipe();
			Host.Render();
		}
	}

	private static ImGuiWidgets.NavigationDrawer Create() => new(Host, "demoDrawer", DrawContent);

	private static void DrawContent()
	{
		ImGui.TextUnformatted("Mail");
		ImGui.Separator();
		foreach (string destination in Destinations)
		{
			if (ImGui.Selectable($"{destination}##drawer{destination}", destination == Destination))
			{
				Destination = destination;
				Drawer.Close();
			}

			ImGuiProbes.MarkItem("demoDrawer", destination);
		}
	}
}
