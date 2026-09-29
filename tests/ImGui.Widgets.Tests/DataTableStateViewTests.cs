// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests how a data table's state builds its view of the rows. No ImGui context is required.
/// </summary>
[TestClass]
public class DataTableStateViewTests
{
	private readonly DataTableFixture fixture = new();

	private static void AssertView(ImGuiWidgets.DataTableState<DataTablePerson> state, params int[] expected) =>
		Assert.AreSequenceEqual(expected, state.View);

	[TestMethod]
	public void NewState_ShowsEveryRowInSourceOrder() =>
		AssertView(fixture.CreateState(), 0, 1, 2, 3);

	[TestMethod]
	public void ColumnFilter_KeepsOnlyMatchingRows()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();

		state.SetFilter(DataTableFixture.NameColumn, "ob");

		AssertView(state, 1);
	}

	/// <summary>
	/// The default is fuzzy: the typed characters in order, any case. "al" is in Alice and, spread out,
	/// in Carol, and in neither Bob nor Dave.
	/// </summary>
	[TestMethod]
	public void ColumnFilter_MatchesTypedCharactersInOrderIgnoringCase()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();

		state.SetFilter(DataTableFixture.NameColumn, "AL");

		AssertView(state, 0, 2);
	}

	[TestMethod]
	public void FiltersOnSeveralColumns_MustAllMatch()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();

		state.SetFilter(DataTableFixture.NameColumn, "a");
		state.SetFilter(DataTableFixture.AgeColumn, "25");

		AssertView(state, 3);
	}

	[TestMethod]
	public void ClearingAFilter_RestoresTheRows()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.SetFilter(DataTableFixture.NameColumn, "ob");

		state.SetFilter(DataTableFixture.NameColumn, string.Empty);

		AssertView(state, 0, 1, 2, 3);
	}

	[TestMethod]
	public void RowFilter_IsAppliedAsSoonAsItIsSet()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();

		state.RowFilter = person => person.IsActive;

		AssertView(state, 0, 2, 3);
	}

	[TestMethod]
	public void SortAscending_BreaksTiesBySourceOrder()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();

		state.SetSort(DataTableFixture.AgeColumn, isAscending: true);

		AssertView(state, 1, 3, 0, 2);
	}

	/// <summary>
	/// Descending reverses the values but not the tie-break, so Bob still comes before Dave. Reversing
	/// the whole ascending list would put Dave first and make equal rows swap on every click.
	/// </summary>
	[TestMethod]
	public void SortDescending_StillBreaksTiesBySourceOrder()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();

		state.SetSort(DataTableFixture.AgeColumn, isAscending: false);

		AssertView(state, 2, 0, 1, 3);
	}

	[TestMethod]
	public void SortOnNoColumn_RestoresSourceOrder()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.SetSort(DataTableFixture.AgeColumn, isAscending: true);

		state.SetSort(-1, isAscending: true);

		AssertView(state, 0, 1, 2, 3);
	}

	[TestMethod]
	public void SortOnAColumnThatCannotSort_IsIgnored()
	{
		ImGuiWidgets.DataTableColumn<DataTablePerson, int> age = new() { Label = "Age", Value = person => person.Age, CanSort = false };
		ImGuiWidgets.DataTableState<DataTablePerson> state = new([fixture.Name, age]);
		state.Sync(fixture.People);

		state.SetSort(1, isAscending: true);

		AssertView(state, 0, 1, 2, 3);
	}

	/// <summary>
	/// The rule that keeps an edited row in place: a value changing is not a trigger, because the table
	/// can't see it happen and because a row jumping away mid-edit is disorienting.
	/// </summary>
	[TestMethod]
	public void AValueChangingInPlace_DoesNotMoveItsRowUntilRefresh()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.SetSort(DataTableFixture.AgeColumn, isAscending: true);

		fixture.People[1].Age = 99;
		state.Sync(fixture.People);

		AssertView(state, 1, 3, 0, 2);

		state.Refresh();

		AssertView(state, 3, 0, 2, 1);
	}

	[TestMethod]
	public void ARowCountChange_RebuildsTheView()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.SetSort(DataTableFixture.AgeColumn, isAscending: true);

		fixture.People.Add(new("Eve", 20, true, DataTableMood.Calm));
		state.Sync(fixture.People);

		AssertView(state, 4, 1, 3, 0, 2);
	}

	[TestMethod]
	public void NoRows_GiveAnEmptyView()
	{
		fixture.People.Clear();

		AssertView(fixture.CreateState());
	}

	[TestMethod]
	public void ViewPositionOf_ReportsWhereASourceRowIsShownOrMinusOne()
	{
		ImGuiWidgets.DataTableState<DataTablePerson> state = fixture.CreateState();
		state.SetFilter(DataTableFixture.NameColumn, "ob");

		Assert.AreEqual(0, state.ViewPositionOf(1));
		Assert.AreEqual(-1, state.ViewPositionOf(0), "A filtered out row reported a position.");
		Assert.AreEqual(-1, state.ViewPositionOf(99), "A row past the end reported a position.");
	}

	[TestMethod]
	public void Constructor_WithNoColumns_Throws() =>
		Assert.ThrowsExactly<ArgumentException>(() => new ImGuiWidgets.DataTableState<DataTablePerson>([]));

	[TestMethod]
	public void Constructor_WithTwoColumnsOfOneLabel_Throws()
	{
		ImGuiWidgets.DataTableColumn<DataTablePerson, int> secondName = new() { Label = "Name", Value = person => person.Age };

		Assert.ThrowsExactly<ArgumentException>(() => new ImGuiWidgets.DataTableState<DataTablePerson>([fixture.Name, secondName]));
	}

	[TestMethod]
	public void Constructor_ValidatesEachColumn()
	{
		ImGuiWidgets.DataTableColumn<DataTablePerson, Version> version = new() { Label = "Version", Value = _ => new Version(1, 0), OnEdit = _ => { } };

		Assert.ThrowsExactly<ArgumentException>(() => new ImGuiWidgets.DataTableState<DataTablePerson>([version]));
	}
}
