// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.BottomNavigationBar(string, ref int, System.Collections.Generic.IReadOnlyList{BottomNavigationItem})"/> on its own.</summary>
[TestClass]
public sealed class BottomNavigationBarTests : WidgetTest
{
	private const string Id = "nav";

	private static readonly BottomNavigationItem[] Items = [new("H", "Home"), new("S", "Search"), new("P", "Profile")];
	private static readonly string[] Icons = ["H", "S", "P"];

	private int selected;
	private int changes;
	private ImGuiWidgets.TabPanel panel = null!;

	private void Draw()
	{
		if (ImGuiWidgets.BottomNavigationBar(Id, ref selected, Items))
		{
			changes++;
		}
	}

	private void DrawPanel()
	{
		if (ImGui.BeginChild("##page", new System.Numerics.Vector2(0f, -ImGuiWidgets.BottomNavigationBarHeight)))
		{
			panel.Draw();
		}

		ImGui.EndChild();
		ImGuiWidgets.BottomNavigationBar(Id, panel, Icons);
	}

	private void StartPanel(bool showTabStrip)
	{
		panel = new ImGuiWidgets.TabPanel("pages") { ShowTabStrip = showTabStrip };
		panel.AddTab("home", "Home", () => Mark("home-page"));
		panel.AddTab("search", "Search", () => Mark("search-page"));
		panel.AddTab("profile", "Profile", () => Mark("profile-page"));
		Start(DrawPanel);
		Step(2);
	}

	[TestMethod]
	public void ClickingADestination_SelectsIt()
	{
		Start(Draw);

		Click($"{Id}/Profile");

		Assert.AreEqual(2, selected);
		Assert.AreEqual(1, changes);
	}

	[TestMethod]
	public void ClickingTheSelectedDestination_ChangesNothing()
	{
		Start(Draw);

		Click($"{Id}/Home");

		Assert.AreEqual(0, changes);
	}

	[TestMethod]
	public void Bar_SitsAtTheBottom_AcrossTheWindow()
	{
		Start(Draw);

		Rectangle bar = RectOf(Id);
		Assert.IsTrue(bar.MaxY is > 460 and <= 480, $"The bar ended at {bar.MaxY}.");
		Assert.IsTrue(bar.Width > 600f, $"The bar was {bar.Width} wide.");
		Assert.AreEqual(RectOf($"{Id}/Home").Width, RectOf($"{Id}/Profile").Width, 1f);
	}

	[TestMethod]
	public void Bar_SwitchesATabPanel()
	{
		StartPanel(showTabStrip: false);
		Assert.IsTrue(IsVisible("home-page"));

		Click($"{Id}/Search");
		Step();

		Assert.AreEqual(1, panel.ActiveTabIndex);
		Assert.IsTrue(IsVisible("search-page"));
		Assert.IsFalse(IsVisible("home-page"));
	}

	[TestMethod]
	public void Bar_SwitchesATabPanelWithItsOwnTabs_AndStays()
	{
		StartPanel(showTabStrip: true);

		Click($"{Id}/Profile");
		Step(5);

		Assert.AreEqual(2, panel.ActiveTabIndex);
		Assert.IsTrue(IsVisible("profile-page"));
	}

	[TestMethod]
	public void ClickingThePanelsOwnTab_MovesTheBar()
	{
		StartPanel(showTabStrip: true);

		Click("pages/Search");
		Step();

		Assert.AreEqual(1, panel.ActiveTabIndex);
		Assert.IsTrue(IsVisible("search-page"));
	}
}
