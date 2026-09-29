// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the data table's editing state machine. No ImGui context is required, because a session's
/// working value can be set directly instead of typed.
/// </summary>
[TestClass]
public class DataTableStateEditingTests
{
	private const int Name = DataTableFixture.NameColumn;
	private const int Age = DataTableFixture.AgeColumn;

	private readonly DataTableFixture fixture = new();

	private static ImGuiWidgets.DataTableCell Cell(int sourceIndex, int column) => new(sourceIndex, column);

	private ImGuiWidgets.DataTableState<DataTablePerson> StateWithActive(int sourceIndex, int column)
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.Click(Cell(sourceIndex, column), isCtrlHeld: false, isShiftHeld: false);
		return state;
	}

	/// <summary>Stands in for typing: sets the open text session's working text directly.</summary>
	private static void Type(ImGuiWidgets.DataTableState<DataTablePerson> state, string text)
	{
		switch (state.EditSession)
		{
			case ImGuiWidgets.DataTableTextSession<DataTablePerson, string> name:
				name.Text = text;
				break;

			case ImGuiWidgets.DataTableTextSession<DataTablePerson, int> age:
				age.Text = text;
				break;

			default:
				Assert.Fail("There is no open text session to type into.");
				break;
		}
	}

	[TestMethod]
	public void BeginEdit_WithNoActiveCell_DoesNothing()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();

		Assert.IsFalse(state.BeginEdit());
		Assert.IsFalse(state.IsEditing);
	}

	[TestMethod]
	public void BeginEdit_OnAReadOnlyColumn_DoesNothing()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(0, DataTableFixture.LengthColumn);

		Assert.IsFalse(state.BeginEdit());
	}

	[TestMethod]
	public void BeginEdit_OnAToggle_DoesNothing()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(0, DataTableFixture.ActiveColumn);

		Assert.IsFalse(state.BeginEdit(), "A bool cell opened an editor instead of toggling.");
	}

	[TestMethod]
	public void BeginEdit_OnTheActiveCell_OpensASessionThere()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Name);

		Assert.IsTrue(state.BeginEdit());
		Assert.IsTrue(state.IsEditing);
		Assert.AreEqual(1, state.EditSession!.SourceIndex);
		Assert.AreEqual(Name, state.EditSession.Column);
	}

	[TestMethod]
	public void CommitEdit_WithAChangedValue_RaisesOneEditAndStopsEditing()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Name);
		state.BeginEdit();
		Type(state, "Robert");

		state.CommitEdit();

		Assert.IsFalse(state.IsEditing);
		Assert.AreEqual("Robert", fixture.NameEdits.Single().NewValue);
	}

	[TestMethod]
	public void CommitEdit_WithAnUnchangedValue_RaisesNothing()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Name);
		state.BeginEdit();

		state.CommitEdit();

		Assert.IsEmpty(fixture.NameEdits);
	}

	/// <summary>A null string opens as empty text, and committing that unchanged isn't an edit.</summary>
	[TestMethod]
	public void CommitEdit_OnANullStringLeftEmpty_RaisesNothing()
	{
		List<ImGuiWidgets.DataTableEdit<DataTablePerson, string?>> edits = [];
		ImGuiWidgets.DataTableColumn<DataTablePerson, string?> nickname = new() { Label = "Nickname", Value = _ => null, OnEdit = edits.Add };
		ImGuiWidgets.DataTableState<DataTablePerson> state = new([nickname]);
		state.Sync(fixture.People);
		state.Click(Cell(1, 0), isCtrlHeld: false, isShiftHeld: false);

		Assert.IsTrue(state.BeginEdit());
		state.CommitEdit();

		Assert.IsEmpty(edits);
	}

	[TestMethod]
	public void CancelEdit_RaisesNothing()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Name);
		state.BeginEdit();
		Type(state, "Robert");

		state.CancelEdit();

		Assert.IsFalse(state.IsEditing);
		Assert.IsEmpty(fixture.NameEdits);
	}

	[TestMethod]
	public void Move_WhileEditing_CommitsFirst()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Name);
		state.BeginEdit();
		Type(state, "Robert");

		state.Move(ImGuiWidgets.DataTableMove.Down);

		Assert.IsFalse(state.IsEditing);
		Assert.AreEqual("Robert", fixture.NameEdits.Single().NewValue);
		Assert.AreEqual(Cell(2, Name), state.ActiveCell);
	}

	[TestMethod]
	public void ClickingAnotherCell_CommitsFirst()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Name);
		state.BeginEdit();
		Type(state, "Robert");

		state.Click(Cell(3, Age), isCtrlHeld: false, isShiftHeld: false);

		Assert.IsFalse(state.IsEditing);
		Assert.HasCount(1, fixture.NameEdits);
	}

	[TestMethod]
	public void ClickingTheCellBeingEdited_KeepsEditing()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Name);
		state.BeginEdit();

		state.Click(Cell(1, Name), isCtrlHeld: false, isShiftHeld: false);

		Assert.IsTrue(state.IsEditing);
	}

	[TestMethod]
	public void ARowCountChange_CancelsTheEdit()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Name);
		state.BeginEdit();
		Type(state, "Robert");

		fixture.People.Add(new("Eve", 20, true, DataTableMood.Calm));
		state.Sync(fixture.People);

		Assert.IsFalse(state.IsEditing);
		Assert.IsEmpty(fixture.NameEdits, "An edit was committed to a row whose index may no longer name it.");
	}

	[TestMethod]
	public void AFilterHidingTheEditedRow_CommitsTheEdit()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Name);
		state.BeginEdit();
		Type(state, "Robert");

		state.SetFilter(Name, "Alice");

		Assert.IsFalse(state.IsEditing);
		Assert.HasCount(1, fixture.NameEdits);
	}

	[TestMethod]
	public void Toggle_OnABoolCell_RaisesTheOppositeValue()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, DataTableFixture.ActiveColumn);

		state.Toggle();

		Assert.IsTrue(fixture.ActiveEdits.Single().NewValue, "Bob is inactive, so a toggle should make him active.");
	}

	[TestMethod]
	public void Toggle_OnAnythingElse_DoesNothing()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(1, Name);

		state.Toggle();

		Assert.IsEmpty(fixture.ActiveEdits);
		Assert.IsEmpty(fixture.NameEdits);
	}

	/// <summary>
	/// The documented way to use the table is an edit callback that applies the edit and
	/// calls Refresh. The state must have left editing before the callback runs.
	/// </summary>
	[TestMethod]
	public void AnEditCallbackThatAppliesAndRefreshes_ReordersTheView()
	{
		ImGuiWidgets.DataTableState<DataTablePerson>? state = null;
		ImGuiWidgets.DataTableColumn<DataTablePerson, int> age = new()
		{
			Label = "Age",
			Value = person => person.Age,
			OnEdit = edit =>
			{
				Assert.IsFalse(state!.IsEditing, "The callback ran while the state was still editing.");
				edit.Row.Age = edit.NewValue;
				state!.Refresh();
			},
		};

		state = new([fixture.Name, age]);
		state.Sync(fixture.People);
		state.SetSort(1, isAscending: true);
		state.Click(Cell(1, 1), isCtrlHeld: false, isShiftHeld: false);
		state.BeginEdit();
		Type(state, "99");

		state.CommitEdit();

		Assert.AreSequenceEqual([3, 0, 2, 1], state.View);
	}

	/// <summary>A callback that throws must not leave the table stuck editing.</summary>
	[TestMethod]
	public void AnEditCallbackThatThrows_LeavesTheStateNotEditing()
	{
		ImGuiWidgets.DataTableColumn<DataTablePerson, string> name = new()
		{
			Label = "Name",
			Value = person => person.Name,
			OnEdit = _ => throw new InvalidOperationException("The caller refused the edit."),
		};

		ImGuiWidgets.DataTableState<DataTablePerson> state = new([name]);
		state.Sync(fixture.People);
		state.Click(Cell(0, 0), isCtrlHeld: false, isShiftHeld: false);
		state.BeginEdit();
		Type(state, "Alicia");

		Assert.ThrowsExactly<InvalidOperationException>(state.CommitEdit);
		Assert.IsFalse(state.IsEditing);
	}

	[TestMethod]
	public void OpenContextMenu_OnAnUnselectedRow_SelectsItAlone()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = StateWithActive(0, Name);

		state.OpenContextMenu(Cell(2, Age));

		Assert.AreEqual(Cell(2, Age), state.ActiveCell);
		Assert.AreSequenceEqual([2], state.ContextMenu!.Value.SelectedRows.Order());
	}

	[TestMethod]
	public void OpenContextMenu_OnASelectedRow_KeepsTheSelection()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.SelectAll();

		state.OpenContextMenu(Cell(2, Age));

		Assert.HasCount(4, state.ContextMenu!.Value.SelectedRows);
	}

	[TestMethod]
	public void ContextMenu_KeepsTheSelectionAsItWasWhenOpened()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.SelectAll();
		state.OpenContextMenu(Cell(2, Age));

		state.Click(Cell(0, Name), isCtrlHeld: false, isShiftHeld: false);

		Assert.HasCount(4, state.ContextMenu!.Value.SelectedRows);
	}

	[TestMethod]
	public void TakeContextMenuRequest_IsOneShot()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.OpenContextMenu(Cell(0, Name));

		Assert.IsTrue(state.TakeContextMenuRequest());
		Assert.IsFalse(state.TakeContextMenuRequest());
	}

	/// <summary>
	/// A caller holding an immutable list applies an edit by replacing the list, then calls
	/// <c>Refresh</c> before the table has seen the new one. The next sync must rebuild over it, or the
	/// view stays sorted by the old values with an unchanged count to tell it otherwise.
	/// </summary>
	[TestMethod]
	public void Refresh_FromOnEdit_WithAReplacedList_RebuildsOverTheNewList()
	{
		ImmutableList<DataTablePerson> people = [.. fixture.People];
		ImGuiWidgets.DataTableState<DataTablePerson> state = null!;

		ImGuiWidgets.DataTableColumn<DataTablePerson, int> age = new()
		{
			Label = "Age",
			Value = person => person.Age,
			OnEdit = edit =>
			{
				DataTablePerson changed = new(edit.Row.Name, edit.NewValue, edit.Row.IsActive, edit.Row.Mood);
				people = people.SetItem(edit.SourceIndex, changed);
				state.Refresh();
			},
		};

		state = new([fixture.Name, age]);
		state.Sync(people);
		state.SetSort(1, isAscending: true);

		// Alice is 30. At 40 she sorts after Carol, who is 35.
		state.Click(Cell(0, 1), isCtrlHeld: false, isShiftHeld: false);
		state.BeginEdit();
		Type(state, "40");
		state.CommitEdit();
		state.Sync(people);

		Assert.AreSequenceEqual([1, 3, 2, 0], state.View);
	}
}
