// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using Hexa.NET.ImGui;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.ExpandableListItem"/> and <see cref="ImGuiWidgets.Accordion"/> on their own.</summary>
[TestClass]
public sealed class ExpandableListItemTests : WidgetTest
{
	private bool open;
	private int toggles;

	private void DrawItem()
	{
		if (ImGuiWidgets.ExpandableListItem("Details", ref open, Body))
		{
			toggles++;
		}

		ImGui.Button("After");
		Mark("After");
	}

	private static void Body()
	{
		for (int i = 0; i < 6; i++)
		{
			ImGui.TextUnformatted($"Line {i}");
		}

		ImGui.Button("Inside");
		Mark("Inside");
	}

	[TestMethod]
	public void ClickingTheRow_OpensIt_AndShowsTheBody()
	{
		Start(DrawItem);
		Assert.IsFalse(IsVisible("Inside"));

		Click("Details");
		Step(20);

		Assert.IsTrue(open);
		Assert.AreEqual(1, toggles);
		Assert.IsTrue(IsVisible("Inside"));
	}

	[TestMethod]
	public void Opening_PushesWhatFollowsDownGradually()
	{
		Start(DrawItem);
		int closedTop = RectOf("After").MinY;

		Click("Details");
		int earlyTop = RectOf("After").MinY;
		Step(20);
		int openTop = RectOf("After").MinY;

		Assert.IsTrue(earlyTop > closedTop, "The body did not start opening.");
		Assert.IsTrue(openTop > earlyTop, "The body did not keep growing.");
	}

	[TestMethod]
	public void ClickingAgain_ClosesIt_AndTheBodyGoes()
	{
		open = true;
		Start(DrawItem);
		Assert.IsTrue(IsVisible("Inside"));
		int openTop = RectOf("After").MinY;

		Click("Details");
		Step(20);

		Assert.IsFalse(open);
		Assert.IsFalse(IsVisible("Inside"));
		Assert.IsTrue(RectOf("After").MinY < openTop);
	}

	[TestMethod]
	public void Accordion_OpensOneAtATime()
	{
		ImGuiWidgets.Accordion accordion = new();
		Start(() =>
		{
			accordion.Item("Shipping", () => ImGui.TextUnformatted("Two days"));
			accordion.Item("Returns", () => ImGui.TextUnformatted("Thirty days"));
		});

		Click("Shipping");
		Step(20);
		Click("Returns");
		Step(20);

		Assert.IsFalse(accordion.IsOpen("Shipping"));
		Assert.IsTrue(accordion.IsOpen("Returns"));
	}

	[TestMethod]
	public void Accordion_AllowingSeveral_KeepsEachOpen()
	{
		ImGuiWidgets.Accordion accordion = new() { AllowMultiple = true };
		Start(() =>
		{
			accordion.Item("Shipping", () => ImGui.TextUnformatted("Two days"));
			accordion.Item("Returns", () => ImGui.TextUnformatted("Thirty days"));
		});

		Click("Shipping");
		Step(20);
		Click("Returns");
		Step(20);

		Assert.IsTrue(accordion.IsOpen("Shipping"));
		Assert.IsTrue(accordion.IsOpen("Returns"));
	}

	[TestMethod]
	public void Row_ShowsTheHandCursor()
	{
		Start(DrawItem);

		Hover("Details");

		Assert.AreEqual(ImGuiMouseCursor.Hand, Harness.MouseCursor);
	}
}
