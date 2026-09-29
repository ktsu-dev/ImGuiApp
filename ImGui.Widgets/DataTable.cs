// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.Keybinding.Core.Contracts;

public static partial class ImGuiWidgets
{
	private static readonly DataTableOptions DefaultDataTableOptions = new();

	/// <summary>Options for <see cref="DataTable{TRow}"/>.</summary>
	public sealed class DataTableOptions
	{
		/// <summary>
		/// Gets the table flags. Defaults to banded, bordered, resizable columns a person can hide.
		/// </summary>
		/// <remarks>
		/// <see cref="ImGuiTableFlags.ScrollX"/> and <see cref="ImGuiTableFlags.ScrollY"/> are always added,
		/// because the rows are virtualized and the header and filter rows are frozen. Sorting flags are
		/// added when any column can sort.
		/// </remarks>
		public ImGuiTableFlags Flags { get; init; } =
			ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.Hideable;

		/// <summary>Gets the size of the table. Either axis may be zero to take the space available.</summary>
		public Vector2 OuterSize { get; init; }

		/// <summary>
		/// Gets the height of every row, or zero for one frame height, which fits an editor. Every row is
		/// this tall, for the same reason as <see cref="VirtualTableOptions.RowHeight"/>.
		/// </summary>
		public float RowHeight { get; init; }

		/// <summary>Gets a value indicating whether a row of filter boxes sits under the headers. Defaults to <see langword="true"/>.</summary>
		public bool HasFilterRow { get; init; } = true;

		/// <summary>Gets how many leading columns stay in place while the rest scroll sideways.</summary>
		public int FrozenColumns { get; init; }

		/// <summary>
		/// Gets the keymap the table reads its commands' chords from, or <see langword="null"/> for the
		/// defaults. Register the commands with <see cref="DataTableCommands.Register"/>.
		/// </summary>
		public IKeybindingService? Keybindings { get; init; }

		/// <summary>
		/// Gets the callback that draws the context menu's items, or <see langword="null"/> for no menu.
		/// Called every frame the menu is open, inside the popup, so it draws <c>MenuItem</c>s directly.
		/// </summary>
		public Action<DataTableContextMenu>? OnContextMenu { get; init; }
	}

	/// <summary>
	/// Draws a table of typed rows that sorts and filters itself, moves an active cell with the
	/// keyboard, and edits one cell at a time, reporting each edit through its column's
	/// <c>OnEdit</c> rather than writing it.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Only the rows on screen are drawn, so tens of thousands cost the same per frame as a screenful.
	/// Every row is the same height.
	/// </para>
	/// <para>
	/// Rows are addressed by their index in <paramref name="rows"/>, never by where they're shown, so
	/// selection and edits mean the same before and after a sort. Each cell is marked for tests as
	/// <c>&lt;label&gt;/[&lt;sourceIndex&gt;]/&lt;column&gt;</c>, each header as
	/// <c>&lt;label&gt;/&lt;column&gt;</c>, and each filter box as <c>&lt;label&gt;/filter/&lt;column&gt;</c>.
	/// A row must be on screen to be addressed, and <see cref="DataTableState{TRow}.ScrollToRow"/> is how
	/// a test puts it there.
	/// </para>
	/// <para>
	/// A host that turns on ImGui's keyboard navigation will find it competing with the table for the
	/// arrow keys. The table is designed for navigation to be off, which is ImGui's default.
	/// </para>
	/// </remarks>
	/// <typeparam name="TRow">The type of one row.</typeparam>
	/// <param name="label">A unique label for the table's ImGui id and probe name. Not drawn.</param>
	/// <param name="rows">The rows. Read every frame and never written.</param>
	/// <param name="state">The table's state, kept by the caller between frames.</param>
	/// <param name="options">Sizing, filtering, keymap and context menu. Defaults are used when null.</param>
	/// <exception cref="ArgumentNullException"><paramref name="label"/>, <paramref name="rows"/> or <paramref name="state"/> is <see langword="null"/>.</exception>
	public static void DataTable<TRow>(string label, IReadOnlyList<TRow> rows, DataTableState<TRow> state, DataTableOptions? options = null)
	{
		Ensure.NotNull(label);
		Ensure.NotNull(rows);
		Ensure.NotNull(state);

		DataTableImpl<TRow>.Draw(label, rows, state, options ?? DefaultDataTableOptions);
	}

	internal static partial class DataTableImpl<TRow>
	{
		public static void Draw(string label, IReadOnlyList<TRow> rows, DataTableState<TRow> state, DataTableOptions options)
		{
			state.Sync(rows);
			IReadOnlyList<DataTableColumn<TRow>> columns = state.Columns;

			ImGuiTableFlags flags = options.Flags | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY;
			if (columns.Any(column => column.CanSort))
			{
				// Tristate so the table starts in the caller's order. Without it ImGui sorts on the first
				// column as soon as the table appears.
				flags |= ImGuiTableFlags.Sortable | ImGuiTableFlags.SortTristate;
			}

			// Captured before the table so the mark covers the whole widget, as VirtualTable does.
			Vector2 origin = ImGui.GetCursorScreenPos();

			if (!ImGui.BeginTable(label, columns.Count, flags, options.OuterSize))
			{
				return;
			}

			// No tab stops inside the table. Tab is the table's own command, and if ImGui's tabbing could
			// move the keyboard, Tab out of an editor would land in a filter box.
			ImGui.PushItemFlag(ImGuiItemFlags.NoTabStop, true);

			try
			{
				float rowHeight = options.RowHeight > 0f ? options.RowHeight : ImGui.GetFrameHeight();
				int frozenRows = options.HasFilterRow ? 2 : 1;

				ImGui.TableSetupScrollFreeze(Math.Clamp(options.FrozenColumns, 0, columns.Count), frozenRows);
				SetupColumns(columns);
				DrawHeaders(label, columns);
				ReadSort(state);
				ReadColumnVisibility(state);

				if (options.HasFilterRow)
				{
					DrawFilterRow(label, state);
				}

				// The table pads every row above and below its content, so rows sit this far apart. The
				// clipper and paging need the pitch, where the row's own items need only the content height.
				float rowPitch = rowHeight + (2f * ImGui.GetStyle().CellPadding.Y);

				CellRect? editorRect = DrawRows(label, state, rowHeight, rowPitch);
				state.PageSize = (int)(ImGui.GetWindowHeight() / rowPitch) - frozenRows;
				DataTableInput.Handle(state, options, editorRect);
				DataTableContextMenus.Draw(state, options);
			}
			finally
			{
				ImGui.PopItemFlag();
				ImGui.EndTable();
			}

			ImGuiProbes.MarkRegion(label, origin, ImGui.GetItemRectMax());
		}

		private static void SetupColumns(IReadOnlyList<DataTableColumn<TRow>> columns)
		{
			foreach (DataTableColumn<TRow> column in columns)
			{
				ImGuiTableColumnFlags flags = column.Flags;
				if (!column.CanSort)
				{
					flags |= ImGuiTableColumnFlags.NoSort;
				}

				ImGui.TableSetupColumn(column.Label, flags, column.Width);
			}
		}

		/// <summary>Submits the headers one at a time, so each can be marked for tests, as VirtualTable does.</summary>
		private static void DrawHeaders(string label, IReadOnlyList<DataTableColumn<TRow>> columns)
		{
			ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

			for (int column = 0; column < columns.Count; column++)
			{
				if (!ImGui.TableSetColumnIndex(column))
				{
					continue;
				}

				ImGui.TableHeader(columns[column].Label);
				ImGuiProbes.MarkItem(label, columns[column].Label);
			}
		}

		private static void ReadSort(DataTableState<TRow> state)
		{
			ImGuiTableSortSpecsPtr specs = ImGui.TableGetSortSpecs();
			if (specs.IsNull || !specs.SpecsDirty)
			{
				return;
			}

			if (specs.SpecsCount > 0)
			{
				ImGuiTableColumnSortSpecsPtr primary = specs.Specs;
				state.SetSort(primary.ColumnIndex, primary.SortDirection == ImGuiSortDirection.Ascending);
			}
			else
			{
				state.SetSort(-1, isAscending: true);
			}

			specs.SpecsDirty = false;
		}

		/// <summary>Tells the state which columns the person has hidden, so movement and copying skip them.</summary>
		private static void ReadColumnVisibility(DataTableState<TRow> state)
		{
			for (int column = 0; column < state.Columns.Count; column++)
			{
				bool isVisible = (ImGui.TableGetColumnFlags(column) & ImGuiTableColumnFlags.IsEnabled) != 0;
				state.SetColumnVisible(column, isVisible);
			}
		}

		/// <summary>
		/// Draws a search box under each header. The search box's own right-click menu switches a column
		/// between fuzzy, glob and regex, and a change of mode is passed on as a change of filter.
		/// </summary>
		private static void DrawFilterRow(string label, DataTableState<TRow> state)
		{
			ImGui.TableNextRow();
			string prefix = $"{label}/filter";

			for (int column = 0; column < state.Columns.Count; column++)
			{
				if (!ImGui.TableSetColumnIndex(column))
				{
					continue;
				}

				ImGui.PushID(column);
				try
				{
					SearchBoxOptions filterOptions = state.GetFilterOptions(column);
					string text = state.GetFilter(column);

					bool isChanged = SearchBox(ref filterOptions, ref text);
					ImGuiProbes.MarkItem(prefix, state.Columns[column].Label);

					if (isChanged || filterOptions != state.GetFilterOptions(column))
					{
						state.SetFilter(column, text, filterOptions);
					}
				}
				finally
				{
					ImGui.PopID();
				}
			}
		}

		private static CellRect? DrawRows(string label, DataTableState<TRow> state, float rowHeight, float rowPitch)
		{
			IReadOnlyList<int> view = state.View;
			int scrollSource = state.TakeScrollRequest();
			CellRect? editorRect = null;

			ImGuiListClipper clipper = default;
			TableClipping.Begin(ref clipper, view.Count, rowPitch, state.ViewPositionOf(scrollSource));

			// The edited row is drawn wherever it is. ImGui drops the keyboard from an editor that isn't
			// submitted, and a click away can only commit an editor that has a rectangle.
			int editedPosition = state.EditSession is DataTableEditSession session ? state.ViewPositionOf(session.SourceIndex) : -1;
			if (editedPosition >= 0)
			{
				clipper.IncludeItemByIndex(editedPosition);
			}

			while (clipper.Step())
			{
				for (int position = clipper.DisplayStart; position < clipper.DisplayEnd; position++)
				{
					int source = view[position];
					DrawRow(label, state, source, rowHeight, source == scrollSource, ref editorRect);
				}
			}

			clipper.End();
			return editorRect;
		}

		private static void DrawRow(string label, DataTableState<TRow> state, int source, float rowHeight, bool isScrollTarget, ref CellRect? editorRect)
		{
			ImGui.TableNextRow(ImGuiTableRowFlags.None, rowHeight);

			if (state.SelectedRows.Contains(source))
			{
				ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg1, ImGui.GetColorU32(ImGuiCol.Header));
			}

			// The column the scroll anchors on: the active cell's when it's in this row, otherwise the first.
			int scrollColumn = -1;
			if (isScrollTarget)
			{
				scrollColumn = state.ActiveCell is DataTableCell active && active.SourceIndex == source ? active.Column : 0;
			}

			ImGui.PushID(source);
			try
			{
				for (int column = 0; column < state.Columns.Count; column++)
				{
					bool isScrollCell = column == scrollColumn;

					// A cell scrolled sideways out of view reports itself hidden, but the scroll target is
					// drawn anyway, because a scroll can only anchor on something drawn.
					if (ImGui.TableSetColumnIndex(column) || isScrollCell)
					{
						DrawCell(label, state, new DataTableCell(source, column), rowHeight, isScrollCell, ref editorRect);
					}
				}
			}
			finally
			{
				ImGui.PopID();
			}
		}

		private static void DrawCell(string label, DataTableState<TRow> state, DataTableCell cell, float rowHeight, bool isScrollTarget, ref CellRect? editorRect)
		{
			DataTableColumn<TRow> column = state.Columns[cell.Column];
			bool isActive = state.ActiveCell == cell;

			ImGui.PushID(cell.Column);
			try
			{
				Vector2 start = ImGui.GetCursorScreenPos();

				if (isActive)
				{
					ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, ImGui.GetColorU32(ImGuiCol.HeaderActive));
				}

				// A selectable spanning the cell, drawn under the content: it's the hit target for clicks
				// and double-clicks, and the one item that knows the cell's true rectangle.
				bool isClicked = ImGui.Selectable(
					"##cell",
					false,
					ImGuiSelectableFlags.AllowOverlap | ImGuiSelectableFlags.AllowDoubleClick,
					new Vector2(0f, rowHeight));

				ImGuiProbes.MarkItem(TableClipping.RowName(label, cell.SourceIndex), column.Label);
				Vector2 end = ImGui.GetItemRectMax();

				if (isScrollTarget && !ImGui.IsItemVisible())
				{
					ImGui.SetScrollHereY(0.5f);
					ImGui.SetScrollHereX(0.5f);
				}

				HandleCellMouse(state, cell, isClicked);

				ImGui.SetCursorScreenPos(start);

				// Read after the mouse, so a double-click shows its editor on the next frame, and a click on
				// another cell that committed this one draws it as text again straight away.
				DataTableEditSession? session = state.EditSession;
				if (session is not null && session.SourceIndex == cell.SourceIndex && session.Column == cell.Column)
				{
					if (session.FramesDrawn == 0)
					{
						ImGui.SetKeyboardFocusHere();
					}

					session.Draw("##editor");
					editorRect = new CellRect(start, end);
				}
				else if (column.DrawContent(state.RowAt(cell.SourceIndex)))
				{
					state.Click(cell, isCtrlHeld: false, isShiftHeld: false);
					state.Toggle();
				}

				if (isActive)
				{
					ImGui.GetWindowDrawList().AddRect(start, end, ImGui.GetColorU32(ImGuiCol.SeparatorActive));
				}
			}
			finally
			{
				ImGui.PopID();
			}
		}

		private static void HandleCellMouse(DataTableState<TRow> state, DataTableCell cell, bool isClicked)
		{
			if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
			{
				state.OpenContextMenu(cell);
				return;
			}

			if (!isClicked)
			{
				return;
			}

			ImGuiIOPtr io = ImGui.GetIO();
			state.Click(cell, io.KeyCtrl, io.KeyShift);

			if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
			{
				state.BeginEdit();
			}
		}
	}
}
