// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;

/// <summary>
/// The bottom navigation bar, switching the tabs of a tab panel drawn above it.
/// </summary>
internal static class BottomNavigationBarDemo
{
	private static readonly string[] Icons = ["H", "S", "L", "P"];

	private static bool showTabStrip;

	/// <summary>Gets the tab panel the bar drives.</summary>
	internal static ImGuiWidgets.TabPanel Panel { get; private set; } = Create();

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		Panel = Create();
		showTabStrip = false;
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Mobile - Bottom Navigation"))
		{
			ImGui.TextWrapped("The bar along the bottom switches the tab panel above it, and the panel's own tabs move the bar.");
			if (DemoProbe.Checkbox("Show the panel's own tabs##bottomNavTabs", ref showTabStrip))
			{
				Panel.ShowTabStrip = showTabStrip;
			}

			if (ImGui.BeginChild("##bottomNavPhone", new Vector2(360f, 240f), ImGuiChildFlags.Borders))
			{
				if (ImGui.BeginChild("##bottomNavPage", new Vector2(0f, -ImGuiWidgets.BottomNavigationBarHeight)))
				{
					Panel.Draw();
				}

				ImGui.EndChild();
				ImGuiWidgets.BottomNavigationBar("demoBottomNav", Panel, Icons);
			}

			ImGui.EndChild();
		}
	}

	private static ImGuiWidgets.TabPanel Create()
	{
		ImGuiWidgets.TabPanel created = new("demoBottomNavPanel") { ShowTabStrip = false };
		created.AddTab("home", "Home", () => ImGui.TextWrapped("Good morning. Three things are due today."));
		created.AddTab("search", "Search", () => ImGui.TextWrapped("Search everything you have saved."));
		created.AddTab("library", "Library", () => ImGui.TextWrapped("Twelve albums, four playlists."));
		created.AddTab("profile", "Profile", () => ImGui.TextWrapped("Signed in as Sam."));
		return created;
	}
}
