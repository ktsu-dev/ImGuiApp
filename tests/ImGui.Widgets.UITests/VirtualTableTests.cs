// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Hexa.NET.ImGui;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <c>ImGuiWidgets.VirtualTable</c> on its own.</summary>
[TestClass]
public sealed class VirtualTableTests : WidgetTest
{
	private const string Label = "catalogue";

	/// <summary>
	/// Large enough that drawing every row would be visible as a stall rather than a number in an
	/// assertion. The satellite catalogue in ktsu-dev/ImGuiApp#411 is about this size.
	/// </summary>
	private const int RowCount = 30000;

	/// <summary>A row far enough down that no amount of default scrolling reaches it.</summary>
	private const int DistantRow = 15000;

	private static readonly ImGuiWidgets.VirtualTableColumn[] Columns =
	[
		new("Name", ImGuiTableColumnFlags.WidthStretch),
		new("Index", ImGuiTableColumnFlags.WidthFixed, 80f),
	];

	private readonly List<int> drawn = [];

	private readonly ImGuiWidgets.VirtualTableOptions options = new() { RowHeight = 20f };

	private void DrawRow(int row)
	{
		drawn.Add(row);
		ImGui.TextUnformatted(string.Create(CultureInfo.InvariantCulture, $"object {row}"));
		ImGui.TableNextColumn();
		ImGui.TextUnformatted(row.ToString(CultureInfo.InvariantCulture));
	}

	private void DrawTable() =>
		ImGuiWidgets.VirtualTable(Label, RowCount, Columns, DrawRow, options);

	private static string RowProbe(int row) =>
		string.Create(CultureInfo.InvariantCulture, $"{Label}/[{row}]");

	[TestInitialize]
	public void ClearDrawn() => drawn.Clear();

	/// <summary>
	/// The point of the widget. Thirty thousand rows have to cost a screenful, or none of the rest
	/// of this matters.
	/// </summary>
	[TestMethod]
	public void VirtualTable_DrawsABoundedNumberOfRowsWhateverTheRowCount()
	{
		Start(DrawTable);

		drawn.Clear();
		Step();

		Assert.AreNotEqual(0, drawn.Count, "The table drew no rows at all.");

		// A 480px viewport at 20px a row holds about 24. The clipper draws a little either side of
		// the visible range, so the bound is generous — but it is nowhere near 30,000, which is the
		// distinction being asserted.
		Assert.IsTrue(
			drawn.Count < 200,
			$"The table drew {drawn.Count} of {RowCount} rows, so it is not virtualizing.");
	}

	[TestMethod]
	public void VirtualTable_CallsDrawRowOncePerRowInAscendingOrder()
	{
		Start(DrawTable);

		drawn.Clear();
		Step();

		CollectionAssert.AllItemsAreUnique(drawn, "A row was drawn more than once in one frame.");
		CollectionAssert.AreEqual(
			drawn.OrderBy(row => row).ToList(),
			drawn,
			"Rows were not drawn in ascending index order.");
	}

	[TestMethod]
	public void VirtualTable_MarksTheTableItself()
	{
		Start(DrawTable);

		Assert.IsTrue(
			IsVisible(Label),
			"The table was not marked, so a test cannot locate the widget.");
	}

	/// <summary>
	/// The table has to be findable even with nothing in it, which is the case a mark taken from a
	/// row would miss.
	/// </summary>
	[TestMethod]
	public void VirtualTable_WithNoRows_StillMarksTheTableAndDrawsNothing()
	{
		Start(() => ImGuiWidgets.VirtualTable(Label, 0, Columns, DrawRow, options));

		Assert.AreEqual(0, drawn.Count, "An empty table called the draw delegate.");
		Assert.IsTrue(IsVisible(Label), "An empty table did not mark itself.");
	}

	/// <summary>
	/// The documented constraint, asserted rather than left to be discovered: a row that was never
	/// drawn is not an ImGui item and therefore has no probe name.
	/// </summary>
	[TestMethod]
	public void VirtualTable_OnlyDrawnRowsAreProbeAddressable()
	{
		Start(DrawTable);

		Assert.IsTrue(
			IsVisible(RowProbe(0)),
			"The first row was drawn but not marked.");

		Assert.IsFalse(
			IsVisible(RowProbe(DistantRow)),
			$"Row {DistantRow} is far off screen, so it should not have been drawn or marked.");
	}

	[TestMethod]
	public void VirtualTable_ScrollToRow_BringsADistantRowIntoView()
	{
		Start(DrawTable);

		options.ScrollToRow = DistantRow;
		Step();

		Assert.IsTrue(
			IsVisible(RowProbe(DistantRow)),
			$"ScrollToRow did not reach row {DistantRow}.");
	}

	/// <summary>
	/// Scrolling has to actually move the viewport, not just force the one row to be drawn — a row
	/// included in the clipper's range but left off screen would pass the visibility check above
	/// while being useless to a test that then wants to click it.
	/// </summary>
	[TestMethod]
	public void VirtualTable_ScrollToRow_MovesTheViewportRatherThanJustDrawingTheRow()
	{
		Start(DrawTable);

		options.ScrollToRow = DistantRow;
		Step();

		drawn.Clear();
		Step();

		Assert.IsTrue(
			drawn.Contains(DistantRow),
			$"Row {DistantRow} was not drawn on the frame after the scroll, so the viewport did not move to it.");

		Assert.IsFalse(
			drawn.Contains(0),
			"Row 0 was still being drawn after scrolling fifteen thousand rows down.");
	}

	[TestMethod]
	public void VirtualTable_ScrollToRow_IsAOneShotRequest()
	{
		Start(DrawTable);

		options.ScrollToRow = DistantRow;
		Step();

		Assert.AreEqual(
			-1,
			options.ScrollToRow,
			"ScrollToRow was not cleared, so the table would re-scroll on every later frame.");
	}

	[TestMethod]
	public void VirtualTable_ScrollToRow_OutsideTheRowCountIsIgnored()
	{
		Start(DrawTable);

		options.ScrollToRow = RowCount + 1;
		Step();

		drawn.Clear();
		Step();

		Assert.IsTrue(
			drawn.Contains(0),
			"A scroll request past the last row moved the table away from the top.");
	}

	/// <summary>
	/// The naming decision the issue calls out as the one that cannot be changed later: a probe name
	/// that shifted with scroll position would not be an address.
	/// </summary>
	[TestMethod]
	public void VirtualTable_RowProbeNamesUseTheAbsoluteIndex()
	{
		Start(DrawTable);

		options.ScrollToRow = DistantRow;
		Step(2);

		Assert.IsTrue(
			IsVisible(RowProbe(DistantRow)),
			$"Row {DistantRow} was not addressable as '{RowProbe(DistantRow)}' after being scrolled to.");

		// The row is near the middle of the viewport, so if names were relative to the visible range
		// it would be addressed by a low index instead.
		Assert.IsFalse(
			IsVisible(RowProbe(0)),
			"A row was marked under index 0 while the table was scrolled to the middle, so names are relative to the view rather than absolute.");
	}

	[TestMethod]
	public void VirtualTable_ClickingARowSelectsIt()
	{
		Start(DrawTable);

		Click(RowProbe(2));

		Assert.AreEqual(2, options.SelectedRow, "Clicking row 2 did not select it.");
	}

	[TestMethod]
	public void VirtualTable_SelectsTheAbsoluteRowThatWasClicked()
	{
		Start(DrawTable);

		options.ScrollToRow = DistantRow;
		Step(2);

		Click(RowProbe(DistantRow));

		Assert.AreEqual(
			DistantRow,
			options.SelectedRow,
			"Clicking a scrolled-to row selected a different row, so selection is using the visible index.");
	}

	[TestMethod]
	public void VirtualTable_WithASortCallback_DrawsSortableHeaders()
	{
		List<(int Column, bool Ascending)> sorts = [];

		ImGuiWidgets.VirtualTableOptions sortable = new()
		{
			RowHeight = 20f,
			OnSortChanged = (column, ascending) => sorts.Add((column, ascending)),
		};

		Start(() => ImGuiWidgets.VirtualTable(Label, RowCount, Columns, DrawRow, sortable));

		// ImGui raises the specs as dirty once when the table first establishes its default sort,
		// which is the callback's first delivery and confirms the wiring end to end.
		Assert.AreNotEqual(0, sorts.Count, "The sort callback was never raised, so Sortable was not applied.");

		sorts.Clear();
		Click("Index");

		Assert.AreNotEqual(
			0,
			sorts.Count,
			"Clicking the 'Index' header did not raise the sort callback.");

		Assert.AreEqual(
			1,
			sorts[^1].Column,
			"The sort callback reported the wrong column for the 'Index' header.");
	}

	/// <summary>
	/// The widget explicitly does not sort. A test pins that, because "it sorts nothing" is a design
	/// decision rather than an omission and should fail loudly if someone adds sorting later.
	/// </summary>
	[TestMethod]
	public void VirtualTable_DoesNotReorderRowsItself()
	{
		ImGuiWidgets.VirtualTableOptions sortable = new()
		{
			RowHeight = 20f,
			OnSortChanged = (_, _) => { },
		};

		Start(() => ImGuiWidgets.VirtualTable(Label, RowCount, Columns, DrawRow, sortable));

		Click("Index");

		drawn.Clear();
		Step();

		CollectionAssert.AreEqual(
			drawn.OrderBy(row => row).ToList(),
			drawn,
			"The widget reordered the rows after a header click; sorting belongs to the caller.");
	}

	[TestMethod]
	public void VirtualTable_WithNoColumns_Throws() =>
		Assert.ThrowsExactly<ArgumentException>(
			() => ImGuiWidgets.VirtualTable(Label, 1, [], DrawRow));

	[TestMethod]
	public void VirtualTable_WithANegativeRowCount_Throws() =>
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(
			() => ImGuiWidgets.VirtualTable(Label, -1, Columns, DrawRow));
}
