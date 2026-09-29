// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <c>ImGuiWidgets.DataTable</c> on its own.</summary>
[TestClass]
public sealed class DataTableTests : WidgetTest
{
	private const string Label = "people";

	private readonly List<DataTablePerson> people = [];
	private readonly List<ImGuiWidgets.DataTableEdit<DataTablePerson, string>> nameEdits = [];
	private readonly List<ImGuiWidgets.DataTableEdit<DataTablePerson, bool>> activeEdits = [];
	private readonly List<ImGuiWidgets.DataTableContextMenu> menus = [];

	private ImGuiWidgets.DataTableState<DataTablePerson> state = null!;
	private ImGuiWidgets.DataTableOptions options = null!;

	[TestInitialize]
	public void CreateTable()
	{
		people.Clear();
		people.AddRange(
		[
			new("Alice", 30, true),
			new("Bob", 25, false),
			new("Carol", 35, true),
			new("Dave", 25, true),
		]);

		state = new(
		[
			new ImGuiWidgets.DataTableColumn<DataTablePerson, string> { Label = "Name", Value = person => person.Name, OnEdit = nameEdits.Add },
			new ImGuiWidgets.DataTableColumn<DataTablePerson, int> { Label = "Age", Value = person => person.Age },
			new ImGuiWidgets.DataTableColumn<DataTablePerson, bool> { Label = "Active", Value = person => person.IsActive, OnEdit = activeEdits.Add },
		]);

		// A fixed height, so there's window below the table to click away into.
		options = new() { RowHeight = 22f, OuterSize = new Vector2(0f, 300f), OnContextMenu = menus.Add };
	}

	private void Draw() => ImGuiWidgets.DataTable(Label, people, state, options);

	private static string Cell(int sourceIndex, string column) =>
		string.Create(CultureInfo.InvariantCulture, $"{Label}/[{sourceIndex}]/{column}");

	private static string Header(string column) => $"{Label}/{column}";

	private static string Filter(string column) => $"{Label}/filter/{column}";

	private void AddPeople(int count)
	{
		for (int index = people.Count; index < count; index++)
		{
			people.Add(new(string.Create(CultureInfo.InvariantCulture, $"Person {index}"), index % 90, index % 2 == 0));
		}
	}

	private void DoubleClick(string name)
	{
		Vector2 center = CenterOf(name);
		Harness.Mouse.Click(center.X, center.Y);
		Harness.Mouse.Click(center.X, center.Y);
		Step();
	}

	private void WithCtrlHeld(System.Action action)
	{
		HarnessKeyboard.KeyDown(ImGuiKey.ModCtrl);

		// Ctrl needs a settled frame of its own before the click begins. Queuing the key and moving
		// the mouse in the same processed frame trips Dear ImGui's AllowOverlap hover confirmation,
		// which requires an item to have been hovered on the previous frame too, and a same-frame
		// new key press suppresses hover for that frame.
		Step();

		action();
		HarnessKeyboard.KeyUp(ImGuiKey.ModCtrl);
		Step();
	}

	[TestMethod]
	public void DataTable_MarksTheTableAndItsHeaders()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Label), "The table was not marked.");
		Assert.IsTrue(IsVisible(Header("Age")), "The Age header was not marked.");
	}

	[TestMethod]
	public void DataTable_MarksCellsBySourceIndex()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Cell(2, "Name")), "Carol's name cell was not marked.");
	}

	[TestMethod]
	public void DataTable_DrawsOnlyTheRowsOnScreen()
	{
		AddPeople(10_000);
		Start(Draw);

		Assert.IsTrue(IsVisible(Cell(0, "Name")), "The first row was not drawn.");
		Assert.IsFalse(IsVisible(Cell(9_000, "Name")), "A row far off screen was drawn, so the table is not virtualizing.");
	}

	[TestMethod]
	public void DataTable_StartsInSourceOrder()
	{
		Start(Draw);

		Assert.IsLessThan(RectOf(Cell(1, "Name")).MinY, RectOf(Cell(0, "Name")).MinY, "The table sorted itself before anyone asked.");
	}

	[TestMethod]
	public void ClickingACell_MakesItActiveAndSelectsItsRow()
	{
		Start(Draw);

		Click(Cell(1, "Age"));

		Assert.AreEqual(new ImGuiWidgets.DataTableCell(1, 1), state.ActiveCell);
		Assert.AreSequenceEqual([1], state.SelectedRows.Order());
	}

	[TestMethod]
	public void CtrlClickingACell_AddsItsRow()
	{
		Start(Draw);
		Click(Cell(0, "Name"));

		WithCtrlHeld(() => Click(Cell(2, "Name")));

		Assert.AreSequenceEqual([0, 2], state.SelectedRows.Order());
	}

	[TestMethod]
	public void ClickingAHeader_SortsTheRows()
	{
		Start(Draw);

		Click(Header("Age"));

		// Ascending by age puts Bob, who is 25, above Alice, who is 30.
		Assert.IsLessThan(RectOf(Cell(0, "Name")).MinY, RectOf(Cell(1, "Name")).MinY, "Clicking Age did not sort by age.");
	}

	[TestMethod]
	public void ClickingAHeaderTwice_ReversesTheSort()
	{
		Start(Draw);

		Click(Header("Age"));
		Click(Header("Age"));

		// Descending puts Carol, who is 35, above Alice, who is 30.
		Assert.IsLessThan(RectOf(Cell(0, "Name")).MinY, RectOf(Cell(2, "Name")).MinY, "A second click did not reverse the sort.");
	}

	[TestMethod]
	public void DoubleClickingAnEditableCell_BeginsEditing()
	{
		Start(Draw);

		DoubleClick(Cell(1, "Name"));

		Assert.IsTrue(state.IsEditing, "A double-click did not begin editing.");
	}

	[TestMethod]
	public void TypingInAFilter_NarrowsTheRows()
	{
		Start(Draw);

		Click(Filter("Name"));
		Harness.Keyboard.Type("ob");
		Step();

		Assert.IsTrue(IsVisible(Cell(1, "Name")), "Bob was filtered out by 'ob'.");
		Assert.IsFalse(IsVisible(Cell(0, "Name")), "Alice survived a filter of 'ob'.");
	}

	[TestMethod]
	public void WithoutAFilterRow_NoFilterBoxesAreDrawn()
	{
		options = new() { RowHeight = 22f, HasFilterRow = false };
		Start(Draw);

		Assert.IsFalse(IsVisible(Filter("Name")), "A filter box was drawn with the filter row turned off.");
	}
}
