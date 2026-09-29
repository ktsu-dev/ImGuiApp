// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the active cell, the selection, keyboard movement and copying. No ImGui context is required.
/// </summary>
[TestClass]
public class DataTableStateNavigationTests
{
	private const int Name = DataTableFixture.NameColumn;
	private const int Age = DataTableFixture.AgeColumn;
	private const int Length = DataTableFixture.LengthColumn;

	private readonly DataTableFixture fixture = new();

	private static ImGuiWidgets.DataTableCell Cell(int sourceIndex, int column) => new(sourceIndex, column);

	private static void AssertSelected(ImGuiWidgets.DataTableState<DataTablePerson> state, params int[] expected) =>
		Assert.AreSequenceEqual(expected.Order(), state.SelectedRows.Order());

	private ImGuiWidgets.DataTableState<DataTablePerson> StateWithActive(int sourceIndex, int column)
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.Click(Cell(sourceIndex, column), isCtrlHeld: false, isShiftHeld: false);
		return state;
	}

	[TestMethod]
	public void Click_ActivatesTheCellAndSelectsOnlyItsRow()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(2, Age);

		Assert.AreEqual(Cell(2, Age), state.ActiveCell);
		AssertSelected(state, 2);
	}

	[TestMethod]
	public void CtrlClick_TogglesOneRow()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(0, Name);

		state.Click(Cell(2, Name), isCtrlHeld: true, isShiftHeld: false);
		AssertSelected(state, 0, 2);

		state.Click(Cell(0, Name), isCtrlHeld: true, isShiftHeld: false);
		AssertSelected(state, 2);
	}

	/// <summary>
	/// Sorted by age, the view is Bob, Dave, Alice, Carol. A range from Bob to Alice is those three as
	/// shown, where a range in source order would have been Alice and Bob.
	/// </summary>
	[TestMethod]
	public void ShiftClick_SelectsTheRangeAsShown()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.SetSort(Age, isAscending: true);

		state.Click(Cell(1, Name), isCtrlHeld: false, isShiftHeld: false);
		state.Click(Cell(0, Name), isCtrlHeld: false, isShiftHeld: true);

		AssertSelected(state, 0, 1, 3);
	}

	[TestMethod]
	public void Click_OnAFilteredOutRow_IsIgnored()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.SetFilter(Name, "ob");

		state.Click(Cell(0, Name), isCtrlHeld: false, isShiftHeld: false);

		Assert.IsNull(state.ActiveCell);
	}

	[TestMethod]
	public void Move_WithNoActiveCell_ActivatesTheFirstCell()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();

		state.Move(ImGuiWidgets.DataTableMove.Down);

		Assert.AreEqual(Cell(0, Name), state.ActiveCell);
	}

	// DataTableMove is internal, and a public test method can't take an internal type, so the rows
	// name the move and the test parses it.
	private static ImGuiWidgets.DataTableMove Parse(string move) => Enum.Parse<ImGuiWidgets.DataTableMove>(move);

	[TestMethod]
	[DataRow("Up", 0, Age)]
	[DataRow("Down", 2, Age)]
	[DataRow("Left", 1, Name)]
	[DataRow("Right", 1, DataTableFixture.ActiveColumn)]
	[DataRow("Home", 1, Name)]
	[DataRow("End", 1, Length)]
	[DataRow("First", 0, Name)]
	[DataRow("Last", 3, Length)]
	public void Move_FromTheMiddle_GoesWhereItSays(string move, int sourceIndex, int column)
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Age);

		state.Move(Parse(move));

		Assert.AreEqual(Cell(sourceIndex, column), state.ActiveCell);
	}

	[TestMethod]
	[DataRow("Up")]
	[DataRow("Left")]
	[DataRow("PageUp")]
	[DataRow("Previous")]
	public void Move_PastTheFirstCell_StaysPut(string move)
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(0, Name);

		state.Move(Parse(move));

		Assert.AreEqual(Cell(0, Name), state.ActiveCell);
	}

	[TestMethod]
	[DataRow("Down")]
	[DataRow("Right")]
	[DataRow("PageDown")]
	[DataRow("Next")]
	public void Move_PastTheLastCell_StaysPut(string move)
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(3, Length);

		state.Move(Parse(move));

		Assert.AreEqual(Cell(3, Length), state.ActiveCell);
	}

	[TestMethod]
	public void Next_AtTheEndOfARow_WrapsToTheStartOfTheNext()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Length);

		state.Move(ImGuiWidgets.DataTableMove.Next);

		Assert.AreEqual(Cell(2, Name), state.ActiveCell);
	}

	[TestMethod]
	public void Previous_AtTheStartOfARow_WrapsToTheEndOfThePrevious()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Name);

		state.Move(ImGuiWidgets.DataTableMove.Previous);

		Assert.AreEqual(Cell(0, Length), state.ActiveCell);
	}

	[TestMethod]
	public void Move_SkipsHiddenColumns()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(0, Name);
		state.SetColumnVisible(Age, isVisible: false);

		state.Move(ImGuiWidgets.DataTableMove.Right);

		Assert.AreEqual(Cell(0, DataTableFixture.ActiveColumn), state.ActiveCell);
	}

	[TestMethod]
	public void PageDown_MovesByThePageSizeAndClamps()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(0, Name);
		state.PageSize = 2;

		state.Move(ImGuiWidgets.DataTableMove.PageDown);
		Assert.AreEqual(Cell(2, Name), state.ActiveCell);

		state.Move(ImGuiWidgets.DataTableMove.PageDown);
		Assert.AreEqual(Cell(3, Name), state.ActiveCell);
	}

	[TestMethod]
	public void Move_CollapsesTheSelectionToTheNewRow()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(0, Name);
		state.Click(Cell(2, Name), isCtrlHeld: true, isShiftHeld: false);

		state.Move(ImGuiWidgets.DataTableMove.Down);

		AssertSelected(state, 3);
	}

	[TestMethod]
	public void MoveExtending_SelectsFromTheAnchor()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Name);

		state.Move(ImGuiWidgets.DataTableMove.Down, isExtending: true);
		state.Move(ImGuiWidgets.DataTableMove.Down, isExtending: true);

		AssertSelected(state, 1, 2, 3);
	}

	[TestMethod]
	public void SelectAll_SelectsOnlyTheRowsShown()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.RowFilter = person => person.IsActive;

		state.SelectAll();

		AssertSelected(state, 0, 2, 3);
	}

	[TestMethod]
	public void ActiveCell_FollowsItsRowThroughASort()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(0, Age);

		state.SetSort(Age, isAscending: true);

		Assert.AreEqual(Cell(0, Age), state.ActiveCell);
		Assert.AreEqual(2, state.ActiveViewRow, "Alice should be third when sorted by age.");
	}

	[TestMethod]
	public void ActiveCell_WhenFilteredOut_MovesToTheSamePositionClamped()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(3, Age);

		state.SetFilter(Name, "ob");

		Assert.AreEqual(Cell(1, Age), state.ActiveCell);
	}

	[TestMethod]
	public void ActiveCell_WhenEveryRowIsFilteredOut_IsCleared()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(0, Name);

		state.SetFilter(Name, "zzz");

		Assert.IsNull(state.ActiveCell);
	}

	[TestMethod]
	public void Selection_LosesRowsAFilterHides()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.SelectAll();

		state.SetFilter(Name, "ob");

		AssertSelected(state, 1);
	}

	[TestMethod]
	public void ARowCountChange_ClearsTheSelection()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.SelectAll();

		fixture.People.Add(new("Eve", 20, true, DataTableMood.Calm));
		state.Sync(fixture.People);

		Assert.IsEmpty(state.SelectedRows);
	}

	/// <summary>
	/// Review focus: the caller removes rows while one near the end is active. The active cell must end
	/// up on a row that still exists, or the next draw indexes past the end of the list.
	/// </summary>
	[TestMethod]
	public void RemovingRowsBelowTheActiveCell_KeepsItInRange()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(3, Name);

		fixture.People.RemoveRange(2, 2);
		state.Sync(fixture.People);

		Assert.IsNotNull(state.ActiveCell);
		Assert.IsLessThan(2, state.ActiveCell.Value.SourceIndex, "The active cell points past the end of the rows.");
	}

	[TestMethod]
	public void TakeScrollRequest_ReportsTheMovedToRowOnce()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(0, Name);

		state.Move(ImGuiWidgets.DataTableMove.Last);

		Assert.AreEqual(3, state.TakeScrollRequest());
		Assert.AreEqual(-1, state.TakeScrollRequest(), "A scroll request survived being taken.");
	}

	[TestMethod]
	public void ScrollToRow_OfAFilteredOutRow_IsDropped()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.SetFilter(Name, "ob");

		state.ScrollToRow(0);

		Assert.AreEqual(-1, state.TakeScrollRequest());
	}

	[TestMethod]
	public void BuildCopyText_JoinsSelectedRowsAsShownWithVisibleColumns()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.SetSort(Age, isAscending: true);
		state.SetColumnVisible(DataTableFixture.MoodColumn, isVisible: false);
		state.SetColumnVisible(Length, isVisible: false);
		state.Click(Cell(0, Name), isCtrlHeld: false, isShiftHeld: false);
		state.Click(Cell(1, Name), isCtrlHeld: true, isShiftHeld: false);

		Assert.AreEqual("Bob\t25\tFalse\nAlice\t30\tTrue", state.BuildCopyText());
	}

	[TestMethod]
	public void BuildCopyText_QuotesValuesThatWouldSplit()
	{
		fixture.People[0].Name = "Al\tice";
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(0, Name);
		for (int column = 1; column < state.Columns.Count; column++)
		{
			state.SetColumnVisible(column, isVisible: false);
		}

		Assert.AreEqual("\"Al\tice\"", state.BuildCopyText());
	}

	[TestMethod]
	public void BuildCopyText_WithNothingSelected_IsEmpty() =>
		Assert.AreEqual(string.Empty, fixture.CreateState().BuildCopyText());
}
