// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;

/// <summary>
/// The tab panel, with tabs added and removed at run time and a dirty marker.
/// </summary>
internal static class TabPanelDemo
{
	/// <summary>Gets the tab panel this section demonstrates.</summary>
	internal static ImGuiWidgets.TabPanel TabPanel => DemoTabPanel;

	private static float tab2Value = 0.5f;

	private static ImGuiWidgets.TabPanel DemoTabPanel { get; } = new("DemoTabPanel", true, true);
	private static Dictionary<string, string> TabIds { get; } = [];
	private static int NextDynamicTabId { get; set; } = 1;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState() => tab2Value = 0.5f;

	/// <summary>Adds the panel's three starting tabs.</summary>
	internal static void Initialize()
	{
		TabIds["tab1"] = DemoTabPanel.AddTab("tab1", "Tab 1", ShowTab1Content);
		TabIds["tab2"] = DemoTabPanel.AddTab("tab2", "Tab 2", ShowTab2Content);
		TabIds["tab3"] = DemoTabPanel.AddTab("tab3", "Tab 3", ShowTab3Content);
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("TabPanel"))
		{
			ImGui.TextUnformatted("Tabbed interface with dirty state tracking:");
			ImGui.Separator();

			// Tab Panel controls
			ImGui.TextUnformatted("Tab Management:");
			if (DemoProbe.Button("Mark Active Tab Dirty"))
			{
				DemoTabPanel.MarkActiveTabDirty();
			}
			ImGui.SameLine();
			if (DemoProbe.Button("Mark Active Tab Clean"))
			{
				DemoTabPanel.MarkActiveTabClean();
			}
			ImGui.SameLine();
			if (DemoProbe.Button("Add New Tab"))
			{
				int tabIndex = NextDynamicTabId++;
				string tabKey = $"dynamic{tabIndex}";
				string tabId = $"dyntab_{tabIndex}";
				TabIds[tabKey] = DemoTabPanel.AddTab(tabId, $"Extra Tab {tabIndex}", () => ShowDynamicTabContent(tabIndex));
			}

			ImGui.Separator();
			ImGui.TextUnformatted("Features demonstrated:");
			ImGui.BulletText("Closeable tabs (X button)");
			ImGui.BulletText("Dirty state indicators (*)");
			ImGui.BulletText("Dynamic tab addition");
			ImGui.BulletText("Per-tab state management");

			ImGui.Separator();

			// Display tab panel
			DemoTabPanel.Draw();
		}
	}

	private static void ShowTab1Content()
	{
		ImGui.TextUnformatted("This is the content of Tab 1");

		if (DemoProbe.Button("Edit Content"))
		{
			DemoTabPanel.MarkTabDirty(TabIds["tab1"]);
		}

		if (DemoProbe.Button("Save Content"))
		{
			DemoTabPanel.MarkTabClean(TabIds["tab1"]);
		}

		ImGui.TextUnformatted("Dirty State: " + (DemoTabPanel.IsTabDirty(TabIds["tab1"]) ? "Modified" : "Unchanged"));
	}

	private static void ShowTab2Content()
	{
		ImGui.TextUnformatted("This is the content of Tab 2");

		if (DemoProbe.SliderFloat("Value", ref tab2Value, 0.0f, 1.0f))
		{
			// Mark tab as dirty when slider value changes
			DemoTabPanel.MarkTabDirty(TabIds["tab2"]);
		}

		if (DemoProbe.Button("Reset"))
		{
			tab2Value = 0.5f;
			DemoTabPanel.MarkTabClean(TabIds["tab2"]);
		}
	}

	private static void ShowTab3Content()
	{
		ImGui.TextUnformatted("This is the content of Tab 3");
		ImGui.TextUnformatted("Try clicking 'Mark Active Tab Dirty' button above");
		ImGui.TextUnformatted("to see the dirty indicator (*) appear next to the tab name.");

		if (DemoProbe.Button("Toggle Dirty State"))
		{
			if (DemoTabPanel.IsTabDirty(TabIds["tab3"]))
			{
				DemoTabPanel.MarkTabClean(TabIds["tab3"]);
			}
			else
			{
				DemoTabPanel.MarkTabDirty(TabIds["tab3"]);
			}
		}
	}

	private static void ShowDynamicTabContent(int tabIndex)
	{
		string tabKey = $"dynamic{tabIndex}";
		ImGui.TextUnformatted($"This is a dynamically added tab ({tabIndex})");
		ImGui.TextUnformatted("The (*) indicator shows when content has been modified.");

		if (DemoProbe.Button("Toggle Dirty State"))
		{
			if (DemoTabPanel.IsTabDirty(TabIds[tabKey]))
			{
				DemoTabPanel.MarkTabClean(TabIds[tabKey]);
			}
			else
			{
				DemoTabPanel.MarkTabDirty(TabIds[tabKey]);
			}
		}
	}
}
