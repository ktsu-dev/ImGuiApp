// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <c>ImGuiWidgets.SegmentedControl</c> on its own.</summary>
[TestClass]
public sealed class SegmentedControlTests : WidgetTest
{
	private const string Label = "View";

	private static readonly string[] Segments = ["Day", "Week", "Month"];

	private int selectedIndex;

	private void Draw() => ImGuiWidgets.SegmentedControl(Label, ref selectedIndex, Segments);

	private void ClickSegment(int index) =>
		ClickFraction(Label, (index + 0.5f) / Segments.Length);

	[TestMethod]
	public void SegmentedControl_IsDrawnAndMarksItself()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Label), "The segmented control marked no probe item.");
		AssertSomethingWasDrawn("the segmented control");
	}

	[TestMethod]
	public void SegmentedControl_ClickingASegmentSelectsIt()
	{
		Start(Draw);

		ClickSegment(2);

		Assert.AreEqual(2, selectedIndex, "Clicking the third segment did not select it.");
	}

	[TestMethod]
	public void SegmentedControl_ClickingBackSelectsTheEarlierSegment()
	{
		selectedIndex = 2;
		Start(Draw);

		ClickSegment(0);

		Assert.AreEqual(0, selectedIndex);
	}

	[TestMethod]
	public void SegmentedControl_ClickingTheSelectedSegmentKeepsIt()
	{
		selectedIndex = 1;
		Start(Draw);

		ClickSegment(1);

		Assert.AreEqual(1, selectedIndex);
	}

	[TestMethod]
	public void SegmentedControl_OutOfRangeSelection_IsClampedOnDraw()
	{
		selectedIndex = 99;
		Start(Draw);

		Assert.AreEqual(Segments.Length - 1, selectedIndex, "An out-of-range selection was not clamped.");
	}

	[TestMethod]
	public void SegmentedControl_AnimatesTheHighlightTowardTheSelection()
	{
		selectedIndex = 0;
		Start(Draw);
		Step(20);
		MoveAway();
		byte[] atFirst = Snapshot();

		selectedIndex = 2;
		Step(20);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(atFirst) > 0, "The highlight never moved to the new selection.");
	}
}
