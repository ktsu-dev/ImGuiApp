// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Collections.Generic;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.TabPanel"/> on its own.</summary>
[TestClass]
public sealed class TabPanelTests : WidgetTest
{
	private const string PanelId = "demo-tabs";

	private static readonly string[] Labels = ["First", "Second", "Third"];

	private ImGuiWidgets.TabPanel panel = null!;
	private readonly List<int> changedToIndex = [];

	private void Draw() => panel.Draw();

	// Every tab header is marked under the panel's id, whether or not it is selected.
	private void ClickTab(int index) => Click($"{PanelId}/{Labels[index]}");

	private ImGuiWidgets.TabPanel BuildPanel(bool closable = false, bool reorderable = false)
	{
		ImGuiWidgets.TabPanel built = new(PanelId, closable, reorderable, changedToIndex.Add);

		foreach (string label in Labels)
		{
			// The loop variable is per-iteration in C#, so the closure captures this label rather
			// than whichever one the loop ends on; no copy of it is needed.
			built.AddTab(label, () => Mark($"content-{label}"));
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

	[TestMethod]
	public void TabPanel_MarksItsStripAndEveryTab()
	{
		panel = BuildPanel();
		Start(Draw);

		Assert.IsTrue(IsVisible(PanelId), "The tab strip did not mark itself.");
		Rectangle strip = RectOf(PanelId);

		foreach (string label in Labels)
		{
			Assert.IsTrue(IsVisible($"{PanelId}/{label}"), $"The '{label}' tab header was not marked, selected or not.");
			Rectangle tab = RectOf($"{PanelId}/{label}");
			Assert.IsTrue(tab.MinY >= strip.MinY - 1 && tab.MaxY <= strip.MaxY + 1, $"The '{label}' tab at {tab} is outside the strip {strip}.");
		}
	}
}
