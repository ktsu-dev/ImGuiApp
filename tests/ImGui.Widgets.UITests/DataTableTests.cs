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

	private void RightClick(string name)
	{
		Vector2 center = CenterOf(name);
		Harness.Mouse.Click(center.X, center.Y, button: 1);
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

	private void BeginEditing(int sourceIndex, string column)
	{
		Click(Cell(sourceIndex, column));
		Harness.Keyboard.Press(ImGuiKey.F2);
		Step(2);
	}

	[TestMethod]
	public void ArrowDown_MovesTheActiveCell()
	{
		Start(Draw);
		Click(Cell(0, "Name"));

		Harness.Keyboard.Press(ImGuiKey.DownArrow);

		Assert.AreEqual(new ImGuiWidgets.DataTableCell(1, 0), state.ActiveCell);
	}

	[TestMethod]
	public void ShiftArrowDown_ExtendsTheSelection()
	{
		Start(Draw);
		Click(Cell(0, "Name"));

		Harness.Keyboard.Press(ImGuiKey.DownArrow, shift: true);

		Assert.AreSequenceEqual([0, 1], state.SelectedRows.Order());
	}

	[TestMethod]
	public void CtrlEnd_ScrollsTheLastRowIntoView()
	{
		AddPeople(1_000);
		Start(Draw);
		Click(Cell(0, "Name"));

		Harness.Keyboard.Press(ImGuiKey.End, ctrl: true);
		Step(4);

		// Visible alone isn't enough, because the clipper draws a forced row even when it lands off
		// screen. The row has to sit inside the table's own rectangle.
		Assert.IsTrue(IsVisible(Cell(999, "Name")), "Ctrl+End did not draw the last row.");
		AssertInsideTable(Cell(999, "Name"));
	}

	private void AssertInsideTable(string name)
	{
		Rectangle table = RectOf(Label);
		Rectangle cell = RectOf(name);

		Assert.IsGreaterThanOrEqualTo(table.MinY, cell.MinY, $"{name} starts above the table.");
		Assert.IsLessThanOrEqualTo(table.MaxY, cell.MaxY, $"{name} ends below the table, at {cell.MaxY} against the table's {table.MaxY}.");
	}

	[TestMethod]
	public void F2ThenTypingThenEnter_ReportsTheEditAndMovesDown()
	{
		Start(Draw);
		BeginEditing(1, "Name");

		Harness.Keyboard.Press(ImGuiKey.A, ctrl: true);
		Harness.Keyboard.Type("Robert");
		Harness.Keyboard.Press(ImGuiKey.Enter);
		Step();

		Assert.AreEqual(new ImGuiWidgets.DataTableEdit<DataTablePerson, string>(1, people[1], "Bob", "Robert"), nameEdits.Single());
		Assert.AreEqual(new ImGuiWidgets.DataTableCell(2, 0), state.ActiveCell);
	}

	[TestMethod]
	public void Escape_CancelsTheEdit()
	{
		Start(Draw);
		BeginEditing(1, "Name");

		Harness.Keyboard.Type("zzz");
		Harness.Keyboard.Press(ImGuiKey.Escape);
		Step();

		Assert.IsFalse(state.IsEditing);
		Assert.IsEmpty(nameEdits);
	}

	[TestMethod]
	public void ClickingAway_CommitsTheEdit()
	{
		Start(Draw);
		BeginEditing(1, "Name");
		Harness.Keyboard.Press(ImGuiKey.A, ctrl: true);
		Harness.Keyboard.Type("Robert");

		// Below the table, inside the window, where nothing else is drawn.
		Harness.Mouse.Click(RectOf(Label).MinX + 20f, RectOf(Label).MaxY + 20f);
		Step();

		Assert.IsFalse(state.IsEditing);
		Assert.AreEqual("Robert", nameEdits.Single().NewValue);
	}

	/// <summary>
	/// Review focus: ImGui selects all of an input's text when it takes the keyboard, so without care the
	/// second typed character replaces the first.
	/// </summary>
	[TestMethod]
	public void TypingACharacter_BeginsAnEditThatKeepsTyping()
	{
		Start(Draw);
		Click(Cell(1, "Name"));

		Harness.Keyboard.Type("Q");
		Step(3);
		Harness.Keyboard.Type("ux");
		Harness.Keyboard.Press(ImGuiKey.Enter);
		Step();

		Assert.AreEqual("Qux", nameEdits.Single().NewValue);
	}

	/// <summary>
	/// Review focus: Tab must move the active cell and leave the keyboard with the table, rather than
	/// ImGui's own tabbing handing it to another item.
	/// </summary>
	[TestMethod]
	public void Tab_WhileEditing_MovesRightAndKeepsTheKeyboard()
	{
		Start(Draw);
		BeginEditing(1, "Name");

		Harness.Keyboard.Press(ImGuiKey.Tab);
		Step();

		Assert.IsFalse(state.IsEditing);
		Assert.AreEqual(new ImGuiWidgets.DataTableCell(1, 1), state.ActiveCell);

		Harness.Keyboard.Press(ImGuiKey.DownArrow);

		Assert.AreEqual(new ImGuiWidgets.DataTableCell(2, 1), state.ActiveCell, "After Tab, the arrow keys no longer reached the table.");
	}

	/// <summary>
	/// Review focus: keys typed into a filter box belong to the filter, not to the active cell.
	/// </summary>
	[TestMethod]
	public void KeysTypedIntoAFilter_DoNotMoveOrEditTheActiveCell()
	{
		Start(Draw);
		Click(Cell(0, "Name"));
		Click(Filter("Age"));

		Harness.Keyboard.Press(ImGuiKey.DownArrow);
		Harness.Keyboard.Type("3");
		Step();

		Assert.AreEqual(new ImGuiWidgets.DataTableCell(0, 0), state.ActiveCell);
		Assert.IsFalse(state.IsEditing, "Typing into a filter began editing the active cell.");
	}

	[TestMethod]
	public void Space_TogglesABoolCell()
	{
		Start(Draw);

		// Clicked near the right edge, clear of the checkbox, so the click only activates the cell.
		ClickFraction(Cell(1, "Active"), 0.9f);
		Harness.Keyboard.Press(ImGuiKey.Space);

		Assert.AreEqual(new ImGuiWidgets.DataTableEdit<DataTablePerson, bool>(1, people[1], false, true), activeEdits.Single());
	}

	[TestMethod]
	public void ClickingTheCheckbox_TogglesTheCell()
	{
		Start(Draw);

		ClickWithin(Cell(2, "Active"), 8f, RectOf(Cell(2, "Active")).Height / 2f);

		Assert.AreEqual(new ImGuiWidgets.DataTableEdit<DataTablePerson, bool>(2, people[2], true, false), activeEdits.Single());
	}

	[TestMethod]
	public void CtrlC_CopiesTheSelectedRows()
	{
		Start(Draw);

		// ImGuiApp installs no clipboard functions, so this reaches the real OS clipboard. Put back what
		// the person running the suite had there.
		string saved = ImGui.GetClipboardTextS() ?? string.Empty;
		try
		{
			Click(Cell(1, "Name"));

			Harness.Keyboard.Press(ImGuiKey.C, ctrl: true);

			Assert.AreEqual("Bob\t25\tFalse", ImGui.GetClipboardTextS());
		}
		finally
		{
			ImGui.SetClipboardText(saved);
		}
	}

	[TestMethod]
	public void TypingACharacterOutsideTheBasicPlane_DoesNotBeginAnEdit()
	{
		Start(Draw);
		Click(Cell(1, "Name"));

		// Queued as one codepoint, as a platform backend would, rather than as a surrogate pair.
		ImGui.GetIO().AddInputCharacter(0x1F600u);
		Step(2);

		Assert.IsFalse(state.IsEditing, "A character above U+FFFF began an edit.");
	}

	[TestMethod]
	public void RightClickingACell_OpensTheContextMenuForIt()
	{
		Start(Draw);

		RightClick(Cell(2, "Age"));

		Assert.IsNotEmpty(menus, "The context menu callback never ran.");
		Assert.AreEqual(new ImGuiWidgets.DataTableCell(2, 1), menus[^1].Cell);
		Assert.AreSequenceEqual([2], menus[^1].SelectedRows.Order());
	}

	[TestMethod]
	public void RightClickingASelectedRow_KeepsTheSelection()
	{
		Start(Draw);
		Click(Cell(0, "Name"));
		WithCtrlHeld(() => Click(Cell(2, "Name")));

		RightClick(Cell(2, "Age"));

		Assert.AreSequenceEqual([0, 2], menus[^1].SelectedRows.Order());
	}

	[TestMethod]
	public void RightClickingWithNoMenu_StillActivatesTheCell()
	{
		options = new() { RowHeight = 22f };
		Start(Draw);

		RightClick(Cell(3, "Name"));

		Assert.AreEqual(new ImGuiWidgets.DataTableCell(3, 0), state.ActiveCell);
	}
}
