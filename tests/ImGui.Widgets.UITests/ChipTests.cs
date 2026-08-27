// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <c>ImGuiWidgets.Chip</c> and <see cref="ImGuiWidgets.ChipGroup"/> on their own.</summary>
[TestClass]
public sealed class ChipTests : WidgetTest
{
	private const string Label = "Espresso";

	private bool selected;
	private bool clicked;
	private bool closeClicked;

	// Latched rather than assigned: a click is reported for the single frame it happens on, and the
	// frames drawn after it would immediately clear a plain assignment.
	private void DrawChip() => clicked |= ImGuiWidgets.Chip(Label, selected);

	private void DrawClosableChip()
	{
		clicked |= ImGuiWidgets.Chip(Label, selected, out bool close);
		closeClicked |= close;
	}

	[TestMethod]
	public void Chip_IsDrawnAndMarksItself()
	{
		Start(DrawChip);

		Assert.IsTrue(IsVisible(Label), "The chip marked no probe item.");
		AssertSomethingWasDrawn("the chip");
	}

	[TestMethod]
	public void Chip_ReportsAClick()
	{
		Start(DrawChip);

		Click(Label);

		Assert.IsTrue(clicked, "The chip did not report being clicked.");
	}

	[TestMethod]
	public void Chip_ReportsNoClickWhenLeftAlone()
	{
		Start(DrawChip);

		Step(3);

		Assert.IsFalse(clicked, "The chip reported a click nobody made.");
	}

	[TestMethod]
	public void Chip_SelectedDrawsDifferently()
	{
		selected = false;
		Start(DrawChip);
		MoveAway();
		byte[] unselected = Snapshot();

		selected = true;
		Step(2);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(unselected) > 0, "A selected chip drew the same as an unselected one.");
	}

	[TestMethod]
	public void ClosableChip_ReportsTheCloseButtonSeparately()
	{
		Start(DrawClosableChip);

		// The close affordance sits at the right-hand end of the chip; the body is everything left
		// of it, so a click near the left edge must not read as a close.
		ClickFraction(Label, 0.15f);

		Assert.IsFalse(closeClicked, "Clicking the chip body reported a close.");
	}

	[TestMethod]
	public void ClosableChip_IsWiderThanAPlainChip()
	{
		Start(DrawChip);
		int plainWidth = RectOf(Label).Width;
		DisposeHarness();

		Start(DrawClosableChip);
		int closableWidth = RectOf(Label).Width;

		Assert.IsTrue(
			closableWidth > plainWidth,
			$"A closable chip ({closableWidth}px) reserved no more room than a plain one ({plainWidth}px).");
	}

	// A chip inside a group is marked with the group's per-index suffix, so that two groups
	// offering the same options stay distinguishable.
	[TestMethod]
	public void ChipGroup_ClickingAChipSelectsIt()
	{
		int selectedIndex = 0;
		string[] options = ["One", "Two", "Three"];

		Start(() => ImGuiWidgets.ChipGroup("filters", options, ref selectedIndex));

		Click("Three##chip2");

		Assert.AreEqual(2, selectedIndex, "Clicking the third chip did not select it.");
	}

	[TestMethod]
	public void ChipGroup_WithoutDeselect_KeepsTheSelectionWhenReclicked()
	{
		int selectedIndex = 1;
		string[] options = ["One", "Two", "Three"];

		Start(() => ImGuiWidgets.ChipGroup("filters", options, ref selectedIndex));

		Click("Two##chip1");

		Assert.AreEqual(1, selectedIndex, "Re-clicking the selected chip cleared a selection that cannot be cleared.");
	}

	[TestMethod]
	public void ChipGroup_AllowingDeselect_ClearsTheSelectionWhenReclicked()
	{
		int selectedIndex = 1;
		string[] options = ["One", "Two", "Three"];

		Start(() => ImGuiWidgets.ChipGroup("filters", options, ref selectedIndex, allowDeselect: true));

		Click("Two##chip1");

		Assert.AreEqual(-1, selectedIndex, "Re-clicking the selected chip did not clear the selection.");
	}
}
