// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.TabPanel"/> on its own.</summary>
[TestClass]
public sealed class TabPanelTests : WidgetTest
{
	private static readonly string[] Labels = ["First", "Second", "Third"];

	private ImGuiWidgets.TabPanel panel = null!;
	private readonly List<int> changedToIndex = [];

	// Tab items are ImGui's own, so they carry no probe mark. The test records where the tab bar
	// begins and how wide each tab is, using the same rules ImGui lays them out by: a tab is its
	// label plus the frame padding on both sides, and consecutive tabs are separated by the inner
	// item spacing.
	private Vector2 tabBarOrigin;
	private readonly List<float> tabWidths = [];
	private float tabHeight;
	private float tabSpacing;

	private void Draw()
	{
		tabBarOrigin = ImGui.GetCursorScreenPos();
		tabHeight = ImGui.GetFrameHeight();
		tabSpacing = ImGui.GetStyle().ItemInnerSpacing.X;

		tabWidths.Clear();
		foreach (ImGuiWidgets.Tab tab in panel.Tabs)
		{
			tabWidths.Add(ImGui.CalcTextSize(tab.Label).X + (ImGui.GetStyle().FramePadding.X * 2f));
		}

		panel.Draw();
	}

	private void ClickTab(int index)
	{
		float x = tabBarOrigin.X;

		for (int i = 0; i < index; i++)
		{
			x += tabWidths[i] + tabSpacing;
		}

		Harness.Mouse.Click(x + (tabWidths[index] / 2f), tabBarOrigin.Y + (tabHeight / 2f));
		Step();
	}

	private ImGuiWidgets.TabPanel BuildPanel(bool closable = false, bool reorderable = false)
	{
		ImGuiWidgets.TabPanel built = new("demo-tabs", closable, reorderable, changedToIndex.Add);

		foreach (string label in Labels)
		{
			string captured = label;
			built.AddTab(captured, () => Mark($"content-{captured}"));
		}

		return built;
	}

	[TestMethod]
	public void TabPanel_ShowsTheFirstTabsContent()
	{
		panel = BuildPanel();
		Start(Draw);

		Assert.IsTrue(IsVisible("content-First"), "The first tab's content was not drawn.");
		Assert.IsFalse(IsVisible("content-Second"), "An inactive tab's content was drawn.");
	}

	[TestMethod]
	public void TabPanel_ClickingATabShowsItsContent()
	{
		panel = BuildPanel();
		Start(Draw);

		ClickTab(1);

		Assert.IsTrue(IsVisible("content-Second"), "Clicking the second tab did not show its content.");
		Assert.IsFalse(IsVisible("content-First"), "The first tab's content stayed on screen after switching away.");
	}

	[TestMethod]
	public void TabPanel_ReportsTheTabItSwitchedTo()
	{
		panel = BuildPanel();
		Start(Draw);
		changedToIndex.Clear();

		ClickTab(2);

		CollectionAssert.Contains(changedToIndex, 2, "The panel did not report switching to the third tab.");
	}

	[TestMethod]
	public void TabPanel_TracksItsActiveTab()
	{
		panel = BuildPanel();
		Start(Draw);

		ClickTab(1);

		Assert.AreEqual(1, panel.ActiveTabIndex, "The panel's active index did not follow the click.");
		Assert.AreEqual("Second", panel.ActiveTab?.Label);
	}

	[TestMethod]
	public void TabPanel_MarksATabDirtyAndClean()
	{
		panel = BuildPanel();
		Start(Draw);
		MoveAway();
		byte[] clean = Snapshot();

		panel.MarkTabDirty(0);
		Step(2);
		MoveAway();

		Assert.IsTrue(panel.IsTabDirty(0), "The tab was not recorded as dirty.");
		Assert.IsTrue(PixelsChangedSince(clean) > 0, "A dirty tab drew no unsaved-document marker.");

		panel.MarkTabClean(0);
		Step(2);

		Assert.IsFalse(panel.IsTabDirty(0), "The tab stayed dirty after being marked clean.");
	}

	[TestMethod]
	public void TabPanel_RemovingATabDropsItsContent()
	{
		panel = BuildPanel();
		Start(Draw);

		panel.RemoveTab(0);
		Step(2);

		Assert.AreEqual(Labels.Length - 1, panel.Tabs.Count, "The tab was not removed.");
		Assert.IsFalse(IsVisible("content-First"), "The removed tab's content was still drawn.");
	}

	[TestMethod]
	public void TabPanel_WithNoTabs_DrawsNothing()
	{
		panel = new ImGuiWidgets.TabPanel("empty-tabs");
		Start(Draw);

		foreach (string label in Labels)
		{
			Assert.IsFalse(IsVisible($"content-{label}"), $"An empty panel drew content for '{label}'.");
		}
	}

	[TestMethod]
	public void TabPanel_LooksUpTabsByIdAndIndex()
	{
		panel = BuildPanel();
		Start(Draw);

		ImGuiWidgets.Tab first = panel.Tabs[0];

		Assert.AreSame(first, panel.GetTabById(first.Id), "A tab could not be found by its own id.");
		Assert.AreEqual(0, panel.GetTabIndex(first.Id), "A tab reported the wrong index.");
	}
}
