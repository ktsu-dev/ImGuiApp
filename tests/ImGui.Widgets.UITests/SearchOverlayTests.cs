// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using ktsu.ImGui.Widgets.Overlays;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.SearchOverlay"/> on its own.</summary>
[TestClass]
public sealed class SearchOverlayTests : WidgetTest
{
	private const string Id = "search";

	private static readonly string[] Fruit = ["Apple", "Apricot", "Banana", "Blueberry", "Cherry"];

	private readonly OverlayHost host = new();
	private ImGuiWidgets.SearchOverlay search = null!;
	private string? chosen;
	private int choices;

	private void Draw() => host.Render();

	private void OpenSearch()
	{
		search = new ImGuiWidgets.SearchOverlay(host, Id);
		Start(Draw);
		search.Open(Fruit, item =>
		{
			chosen = item;
			choices++;
		});
		Step(30);
	}

	[TestMethod]
	public void Open_ListsEverything()
	{
		OpenSearch();

		foreach (string fruit in Fruit)
		{
			Assert.IsTrue(IsVisible($"{Id}/{fruit}"), $"{fruit} was not listed.");
		}
	}

	[TestMethod]
	public void Typing_FiltersTheList()
	{
		OpenSearch();

		Harness.Keyboard.Type("ap");
		Step(2);

		Assert.AreEqual("ap", search.Query);
		Assert.IsTrue(IsVisible($"{Id}/Apple"));
		Assert.IsTrue(IsVisible($"{Id}/Apricot"));
		Assert.IsFalse(IsVisible($"{Id}/Cherry"));
	}

	[TestMethod]
	public void Enter_ChoosesTheBestMatch()
	{
		OpenSearch();

		Harness.Keyboard.Type("cher");
		Step(2);
		Harness.Keyboard.Press(ImGuiKey.Enter);
		Step();

		Assert.AreEqual("Cherry", chosen);
		Assert.IsFalse(search.IsOpen);
	}

	[TestMethod]
	public void ArrowKeys_MoveTheHighlight()
	{
		OpenSearch();

		Harness.Keyboard.Press(ImGuiKey.DownArrow);
		Harness.Keyboard.Press(ImGuiKey.DownArrow);
		Step();
		Harness.Keyboard.Press(ImGuiKey.Enter);
		Step();

		Assert.AreEqual("Apricot", chosen);
	}

	[TestMethod]
	public void ClickingAResult_ChoosesIt()
	{
		OpenSearch();

		Click($"{Id}/Banana");

		Assert.AreEqual("Banana", chosen);
		Assert.AreEqual(1, choices);
	}

	[TestMethod]
	public void BackButton_ClosesWithoutAnswering()
	{
		OpenSearch();

		Click($"{Id}/back");
		Step(30);

		Assert.IsFalse(search.IsOpen);
		Assert.AreEqual(0, choices);
		Assert.IsFalse(host.HasOverlays);
	}

	[TestMethod]
	public void Escape_ClosesWithoutAnswering()
	{
		OpenSearch();

		Harness.Keyboard.Press(ImGuiKey.Escape);
		Step();

		Assert.IsFalse(search.IsOpen);
		Assert.AreEqual(0, choices);
	}

	[TestMethod]
	public void Overlay_CoversTheWindow()
	{
		OpenSearch();

		Rectangle rect = RectOf(Id);
		Assert.AreEqual(640f, rect.Width, 1f);
		Assert.AreEqual(480f, rect.Height, 1f);
	}
}
