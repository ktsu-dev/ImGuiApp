# A data table widget, with sorting, filtering, and cell editing

Status: approved design, not yet implemented.

## Why this exists

`VirtualTable` draws rows by index and stops there. It reports a sort change and leaves the reordering
to the caller, it selects one row, and it has no notion of a cell. That's the right shape for a
read-only catalogue, and the wrong one for an application that edits records in bulk, where every
caller ends up writing the same sort index, the same filter, the same keyboard movement, and the same
edit bookkeeping on top of it.

`DataTable` is that layer, written once. It holds typed rows, sorts and filters them itself, lets a
person move between cells with the keyboard and edit them in place, and reports each edit to the
caller rather than writing it.

## Scope

In scope:

- Typed columns defined by a value getter, with display text, a comparer, and an optional editor.
- Sorting on one column, applied by the table.
- A filter row under the headers, one text filter per column, plus a caller predicate.
- An active cell moved by the keyboard, and spreadsheet-style editing of one cell at a time.
- Selecting multiple rows, and copying them as tab-separated text.
- A context menu per cell, drawn by the caller.
- Keyboard commands that follow the host's `ktsu.Keybinding` keymap when one is given.

Out of scope, deliberately:

- Writing to the caller's data. The table reports an edit and the caller applies it. This keeps
  immutable rows and undo stacks working without the table knowing about either.
- Column reordering. Movement between cells has to follow the displayed column order, and reading
  that order means reaching into ImGui's internal table structures. Worth adding once there is a
  public way to read it.
- Sorting on more than one column.
- Variable row heights. The same constraint as `VirtualTable`, for the same reason.
- Selecting cell ranges, and paste. Paste needs parsing and conversion per column type, which is a
  feature of its own.
- Persisting column layout anywhere but `imgui.ini`.

## Architecture

The widget is two pieces with a hard line between them.

- `DataTableState<TRow>` makes no ImGui calls. It owns the view of the rows, the active cell, the
  selection, the editing state, and keyboard movement, and it's driven by commands. The caller keeps
  one between frames.
- `ImGuiWidgets.DataTable(...)` draws one frame from that state. It turns ImGui input into commands
  and hosts the one editor that exists at a time. It makes no decisions the state should own.

This follows `Viewport3DState` and the diff view's model. The riskiest logic (movement at the edges
of the view, commit and cancel, selection after a re-sort) becomes plain unit tests that need no
window.

## Public API

```csharp
public abstract class DataTableColumn<TRow>
{
	public required string Label { get; init; }
	public ImGuiTableColumnFlags Flags { get; init; }
	public float Width { get; init; }
	public abstract string GetText(TRow row);
}

public sealed class DataTableColumn<TRow, TValue> : DataTableColumn<TRow>
{
	public required Func<TRow, TValue> Value { get; init; }
	public Func<TValue, string>? Format { get; init; }
	public IComparer<TValue>? Comparer { get; init; }
	public bool Sortable { get; init; } = true;
	public Action<DataTableEdit<TRow, TValue>>? OnEdit { get; init; }
	public DataTableEditor<TValue>? Editor { get; init; }
	public Action<TRow, TValue>? DrawCell { get; init; }
}

public readonly record struct DataTableEdit<TRow, TValue>(
	int SourceIndex, TRow Row, TValue OldValue, TValue NewValue);

public delegate bool DataTableEditor<TValue>(string id, ref TValue value);

public readonly record struct DataTableCell(int SourceIndex, int Column);

public sealed class DataTableState<TRow>
{
	public DataTableState(IReadOnlyList<DataTableColumn<TRow>> columns);

	public IReadOnlyList<DataTableColumn<TRow>> Columns { get; }
	public Func<TRow, bool>? RowFilter { get; set; }
	public DataTableCell? ActiveCell { get; }
	public IReadOnlySet<int> SelectedRows { get; }
	public bool IsEditing { get; }

	public void Refresh();
	public void ScrollToRow(int sourceIndex);
}

public sealed class DataTableOptions
{
	public ImGuiTableFlags Flags { get; init; }
	public Vector2 OuterSize { get; init; }
	public float RowHeight { get; init; }
	public bool ShowFilterRow { get; init; } = true;
	public int FrozenColumns { get; init; }
	public IKeybindingService? Keybindings { get; init; }
	public Action<DataTableContextMenu>? OnContextMenu { get; init; }
}

public static void DataTable<TRow>(
	string label,
	IReadOnlyList<TRow> rows,
	DataTableState<TRow> state,
	DataTableOptions? options = null);
```

`DataTableContextMenu` carries the right-clicked cell and the selection at the moment the menu
opened. The caller draws `MenuItem`s inside the callback.

Decisions behind the shape:

- **Edits are typed per column.** `OnEdit` receives the typed old and new values with no boxing. A
  column with no `OnEdit` is read-only, so there's no separate flag to keep in step with it.
- **Indices are always source indices.** `SourceIndex` is the row's position in the caller's list,
  never its position on screen, so selection and edits mean the same thing before and after a sort.
- **Built-in editors** cover `bool`, `int`, `long`, `float`, `double`, `string`, and enums, and follow
  `PropertyGrid`'s conventions (the same widgets, and `InvariantCulture` for parsing). Any other type
  supplies `Editor`. A column that has `OnEdit`, no `Editor`, and a type with no built-in editor
  throws `ArgumentException` from the `DataTableState` constructor, so the mistake surfaces when the
  table is built rather than when someone first edits that column.
- **`DrawCell`** draws a cell's content in place of its formatted text, for icons or colored values.
  It draws over the cell's hit target, so selection and editing work the same either way.
- `GetText` on the typed column returns `Format(Value(row))`, or the value's `ToString` with
  `InvariantCulture` when there is no `Format`. It's the text that's drawn, filtered, and copied.

## The state core

### The view

The view is an `int[]` of source indices, built in two passes:

1. **Filter.** A row survives when `RowFilter` accepts it and every non-empty column filter matches
   that column's `GetText(row)` through `ktsu.TextFilter`.
2. **Sort.** A stable sort on the sort column's `Comparer` (or `Comparer<TValue>.Default`), with ties
   broken by source index, so equal rows never trade places between rebuilds.

The view is rebuilt when the sort changes, when a filter changes, when the row count differs from the
last build, and when the caller calls `Refresh()`. Nothing else rebuilds it. An edited row stays
where it is, even if its new value sorts elsewhere or fails the filter, until one of those triggers.
That's how spreadsheets behave, because a row that jumps away while it's being edited is
disorienting, and it means nothing is sorted per frame.

### Identity across rebuilds

The active cell is stored as a source index and a column, so it follows its row through a re-sort.
If a rebuild filters out the active row, the active cell moves to the same view position, clamped to
the new view.

Selected rows that a filter hides are dropped from the selection, so an action on the selection can
never reach rows the user can't see.

A change in row count clears the selection and cancels any edit. Inserting or deleting rows shifts
source indices, so the table can't tell which rows the old indices meant, and clearing is the honest
answer. A caller that wants a selection after changing rows sets it after `Refresh()`.

### Editing

An editable column creates a typed edit session holding the original value and a working copy. The
state stays generic only over `TRow`, and nothing is boxed.

The state is either idle or editing one cell:

- `BeginEdit` starts the session from the current value. Beginning with a typed character starts
  string and numeric columns from that character, replacing the value.
- `Commit` calls `OnEdit` only when the working copy differs from the original by
  `EqualityComparer<TValue>.Default`, so opening and closing an editor never records an empty change.
- `Cancel` discards the working copy.
- Activating another cell while editing commits first.
- `bool` columns have no session. A toggle raises the edit directly.

### Commands

Movement commands are `Up`, `Down`, `Left`, `Right`, `PageUp`, `PageDown`, `Home`, `End`, `First`,
`Last`, `Next`, and `Previous`.

- Movement clamps at the edges of the view, except `Next` and `Previous`, which wrap to the following
  or preceding row.
- Hidden columns are skipped. The renderer tells the state which columns are visible.
- Movement while editing commits first.
- `Commit` from the keyboard commits and then moves down.
- The page size comes from the renderer, which is the only piece that knows how many rows fit.

Selection follows the conventions of file explorers:

- A click selects one row. Ctrl+click toggles one row. Shift+click selects from the anchor row to
  the clicked row, in view order.
- Movement without Shift moves the active cell and collapses the selection to its row. Movement with
  Shift extends the selection from the anchor.
- `SelectAll` selects every row in the view.

## The renderer

### Table setup

`BeginTable` with `ScrollX`, `ScrollY`, `Resizable`, `Hideable`, `RowBg`, and `Borders`, plus
`Sortable` when any column is sortable, merged with `DataTableOptions.Flags`. Columns that aren't
sortable get `NoSort`. `TableSetupScrollFreeze(FrozenColumns, headerRows)` freezes the header row, the
filter row when shown, and any leading columns.

Headers are submitted one at a time rather than through `TableHeadersRow`, as `VirtualTable` does, so
each can be marked for tests. The renderer reads `TableGetSortSpecs` and passes a dirty sort to the
state. Column visibility is read through `TableGetColumnFlags` and passed to the state for movement.

### The filter row

A frozen row under the headers, with a compact text box per column and a hint showing the filter
mode. Each change is passed to the state, which rebuilds the view. Implementation should reuse
`SearchBox` if it fits a narrow cell, and otherwise use `InputTextWithHint` feeding `ktsu.TextFilter`.

### Rows and cells

The list clipper runs over the view with a fixed row height. Each cell is a `Selectable` covering the
cell, which provides the click, double-click, and right-click targets, with the content drawn over it:

- The active cell gets a cell background and an outline, and selected rows get a row background,
  both through `TableSetBgColor` and style colors so they follow the current theme.
- The cell being edited draws its editor instead, with keyboard focus set on its first frame.
- Other cells draw `DrawCell`, or their `GetText`.

When the active cell moves, the renderer forces its row through the clipper with
`IncludeItemByIndex` and scrolls it into view, the same technique `VirtualTable.ScrollToRow` uses.

The clipper setup and the row naming move out of `VirtualTableImpl` into a shared internal helper, so
the two tables can't drift apart.

### Keyboard

Keys are read only while the table has focus. Each command has an id in `DataTableCommands`, which
follows `NodeEditorCommands`:

| Command | Default chord | Also, without a keymap |
| --- | --- | --- |
| `datatable.moveup`, `movedown`, `moveleft`, `moveright` | Arrow keys | |
| `datatable.pageup`, `pagedown` | Page Up, Page Down | |
| `datatable.home`, `end` | Home, End | |
| `datatable.first`, `last` | Ctrl+Home, Ctrl+End | |
| `datatable.next`, `previous` | Tab, Shift+Tab | |
| `datatable.beginedit` | F2 | Enter |
| `datatable.commit` | Enter | |
| `datatable.cancel` | Escape | |
| `datatable.toggle` | Space | |
| `datatable.copy` | Ctrl+C | |
| `datatable.selectall` | Ctrl+A | |

Shift with a movement chord extends the selection. `DataTableCommands.Register` registers the
commands and binds defaults without overwriting a chord the user has already bound, and is safe to
call on every start-up.

With `DataTableOptions.Keybindings` set, each command's chord comes from the active profile. Without
it, the defaults apply, along with alternates a profile can't express because it holds one chord per
command.

While a cell is being edited, only `commit`, `cancel`, `next`, and `previous` are handled, so the
arrow keys stay inside the editor. An editor that loses focus to a click elsewhere commits. Typing a
character to begin editing is text input rather than a chord, and isn't in the keymap.

`KeyChordMatcher` moves from `ImGui.NodeEditor` into `ImGui.Widgets`, which the node editor already
references, and gains a `repeat` parameter. It defaults to off, which keeps the node editor's
behavior, and the movement commands turn it on so held arrows keep moving.

### Context menu

Right-clicking a cell makes it active and, if its row isn't selected, selects that row alone. The
popup then calls `OnContextMenu` with the cell and the selection.

### Copy

`copy` writes the selected rows, in view order, with visible columns only, as tab-separated text
built from `GetText`. A value containing a tab, a newline, or a double quote is wrapped in double
quotes with inner quotes doubled, so it pastes into a spreadsheet as one cell.

### Probe names

| Element | Name |
| --- | --- |
| The table | `label` |
| A header | `label/<column>` |
| A filter box | `label/filter/<column>` |
| A cell | `label/[<sourceIndex>]/<column>` |

Source indices keep names stable across sorting and scrolling. As with `VirtualTable`, a row must be
scrolled into view before a test can address it, and `ScrollToRow` is how a test does that.

## Dependencies

`ImGui.Widgets` gains a reference to `ktsu.Keybinding`, which is already in the solution through the
node editor and targets the same frameworks. It brings `Microsoft.Extensions.DependencyInjection.Abstractions`
and `System.Text.Json` to every consumer of the widgets package. That cost is accepted, and recorded in
`CLAUDE.md` alongside the node editor's.

`ktsu.TextFilter` is already referenced.

## Testing

**Unit tests** in `tests/ImGui.Widgets.Tests` cover `DataTableState<TRow>` with no window:

- Filtering by column text and by `RowFilter`, sorting ascending and descending, and ties keeping
  source order.
- An edit leaving its row in place until `Refresh()`, and a change in row count rebuilding the view,
  clearing the selection, and cancelling an edit.
- The active cell following its row through a re-sort, and clamping when a filter hides it.
- Every movement command at every edge, wrapping on `Next` and `Previous`, and hidden columns being
  skipped.
- Commit with a changed value raising `OnEdit` once, commit with an unchanged value and cancel raising
  nothing, and movement during editing committing first.
- Ctrl toggling, Shift ranges in view order, and hidden rows leaving the selection.
- Copy text, including the quoting rules.
- The constructor rejecting an editable column with no usable editor.
- `DataTableCommands.Register` being idempotent and leaving user chords alone.
- The matcher's `repeat` parameter.

The node editor's existing tests must still pass after `KeyChordMatcher` moves.

**UI tests** in `tests/ImGui.Widgets.UITests`, through the headless harness and the probe names:
clicking a header sorts, typing in a filter narrows the rows, double-clicking a cell opens its editor,
typing and pressing Enter raises the edit and moves down, Escape cancels, the arrow keys move the
active cell and scroll it into view, and a right-click opens the context menu.

## Demo

`DataTableDemo` in `examples/ImGuiWidgetsDemo`, in its own class for the same reason as
`VirtualTableDemo`. A few thousand rows of mixed types (string, int, double, bool, and an enum), every
column editable, with edits applied through `ktsu.UndoRedo` so undo and redo show the edit request
design doing what it's for.

## Documentation

The `ImGui.Widgets` README gains a `DataTable` section, and `CLAUDE.md` records the new dependency.
