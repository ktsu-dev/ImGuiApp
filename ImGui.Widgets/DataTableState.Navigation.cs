// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Linq;

public static partial class ImGuiWidgets
{
	/// <summary>A movement of a data table's active cell.</summary>
	internal enum DataTableMove
	{
		Up,
		Down,
		Left,
		Right,
		PageUp,
		PageDown,
		Home,
		End,
		First,
		Last,
		Next,
		Previous,
	}

	public sealed partial class DataTableState<TRow>
	{
		private readonly HashSet<int> selectedRows = [];
		private int anchorSource = -1;
		private int scrollRequest = -1;

		/// <summary>Gets the cell keyboard input goes to, or <see langword="null"/> until one is chosen.</summary>
		public DataTableCell? ActiveCell { get; private set; }

		/// <summary>Gets the selected rows, as indices into the caller's list.</summary>
		public IReadOnlySet<int> SelectedRows => selectedRows;

		internal int PageSize
		{
			get;
			set => field = Math.Max(1, value);
		} = 10;

		internal int ActiveViewRow => ActiveCell is DataTableCell cell ? ViewPositionOf(cell.SourceIndex) : -1;

		/// <summary>Brings a row into view the next time the table is drawn.</summary>
		/// <param name="sourceIndex">The row's index in the caller's list. Ignored if that row isn't shown.</param>
		public void ScrollToRow(int sourceIndex) => scrollRequest = sourceIndex;

		/// <summary>Clears the selected rows. The active cell stays where it is.</summary>
		public void ClearSelection()
		{
			selectedRows.Clear();
			anchorSource = -1;
		}

		/// <summary>Replaces the selection with the given rows, for a caller restoring one after <see cref="Refresh"/>.</summary>
		/// <param name="sourceIndices">
		/// The rows to select, as indices into the caller's list. Rows that aren't shown are ignored. The
		/// first one shown becomes the anchor a later Shift-click selects from.
		/// </param>
		/// <exception cref="ArgumentNullException"><paramref name="sourceIndices"/> is <see langword="null"/>.</exception>
		public void SelectRows(IEnumerable<int> sourceIndices)
		{
			Ensure.NotNull(sourceIndices);

			selectedRows.Clear();
			anchorSource = -1;

			foreach (int sourceIndex in sourceIndices.Where(sourceIndex => ViewPositionOf(sourceIndex) >= 0))
			{
				selectedRows.Add(sourceIndex);
				if (anchorSource < 0)
				{
					anchorSource = sourceIndex;
				}
			}
		}

		/// <summary>
		/// Makes a cell active and brings it into view the next time the table is drawn, leaving the
		/// selection as it is.
		/// </summary>
		/// <param name="cell">The cell. Ignored if its row isn't shown or its column doesn't exist.</param>
		/// <remarks>An edit in progress is committed first, unless it's in this cell, as a click would.</remarks>
		public void ActivateCell(DataTableCell cell)
		{
			if (!IsShown(cell))
			{
				return;
			}

			FinishEditingUnless(cell);
			ActiveCell = cell;
			scrollRequest = cell.SourceIndex;
		}

		/// <summary>Takes the pending scroll request, so it's acted on once.</summary>
		/// <returns>The source index to scroll to, or -1 for none.</returns>
		internal int TakeScrollRequest()
		{
			int requested = scrollRequest;
			scrollRequest = -1;
			return ViewPositionOf(requested) >= 0 ? requested : -1;
		}

		internal void Click(DataTableCell cell, bool isCtrlHeld, bool isShiftHeld)
		{
			if (!IsShown(cell))
			{
				return;
			}

			FinishEditingUnless(cell);
			ActiveCell = cell;

			if (isShiftHeld && ViewPositionOf(anchorSource) >= 0)
			{
				SelectRange(anchorSource, cell.SourceIndex);
			}
			else if (isCtrlHeld)
			{
				if (!selectedRows.Remove(cell.SourceIndex))
				{
					selectedRows.Add(cell.SourceIndex);
				}

				anchorSource = cell.SourceIndex;
			}
			else
			{
				SelectOnly(cell.SourceIndex);
			}
		}

		internal void Move(DataTableMove move, bool isExtending = false)
		{
			FinishEditingUnless(null);

			if (view.Length == 0)
			{
				return;
			}

			DataTableCell destination = ActiveCell is DataTableCell current && ActiveViewRow >= 0
				? Step(move, ActiveViewRow, current.Column)
				: new DataTableCell(view[0], FirstVisibleColumn());

			ActiveCell = destination;
			scrollRequest = destination.SourceIndex;

			if (isExtending && ViewPositionOf(anchorSource) >= 0)
			{
				SelectRange(anchorSource, destination.SourceIndex);
			}
			else
			{
				SelectOnly(destination.SourceIndex);
			}
		}

		internal void SelectAll()
		{
			selectedRows.Clear();
			selectedRows.UnionWith(view);
		}

		/// <summary>
		/// Builds the text Ctrl+C puts on the clipboard: the selected rows as shown, visible columns only,
		/// tab separated, one row per line.
		/// </summary>
		internal string BuildCopyText()
		{
			int[] columns = [.. Enumerable.Range(0, Columns.Count).Where(IsColumnVisible)];
			IEnumerable<string> lines = view
				.Where(selectedRows.Contains)
				.Select(source => string.Join('\t', columns.Select(column => DataTableText.QuoteForCopy(Columns[column].GetText(rows[source])))));

			return string.Join('\n', lines);
		}

		/// <summary>Commits the edit in progress, unless it's in the given cell.</summary>
		partial void FinishEditingUnless(DataTableCell? cellKeptOpen);

		/// <summary>Called when source indices stop meaning the rows they did.</summary>
		partial void OnRowIdentityLost();

		/// <summary>Called after every rebuild, once the active cell and the selection are settled.</summary>
		partial void OnViewRebuilt();

		/// <summary>
		/// Forgets everything held by source index, for when rows were inserted or deleted and every
		/// index after the change now names a different row.
		/// </summary>
		private void ForgetRowIdentity()
		{
			selectedRows.Clear();
			anchorSource = -1;
			OnRowIdentityLost();
		}

		private void AfterRebuild(int previousPosition)
		{
			selectedRows.RemoveWhere(source => ViewPositionOf(source) < 0);

			if (ActiveCell is DataTableCell cell && ViewPositionOf(cell.SourceIndex) < 0)
			{
				ActiveCell = view.Length == 0
					? null
					: cell with { SourceIndex = view[Math.Clamp(previousPosition, 0, view.Length - 1)] };
			}

			OnViewRebuilt();
		}

		private bool IsShown(DataTableCell cell) =>
			ViewPositionOf(cell.SourceIndex) >= 0 && cell.Column >= 0 && cell.Column < Columns.Count;

		private void SelectOnly(int sourceIndex)
		{
			selectedRows.Clear();
			selectedRows.Add(sourceIndex);
			anchorSource = sourceIndex;
		}

		private void SelectRange(int fromSource, int toSource)
		{
			int from = ViewPositionOf(fromSource);
			int to = ViewPositionOf(toSource);

			selectedRows.Clear();
			for (int position = Math.Min(from, to); position <= Math.Max(from, to); position++)
			{
				selectedRows.Add(view[position]);
			}
		}

		private DataTableCell Step(DataTableMove move, int row, int column)
		{
			int lastRow = view.Length - 1;

			(int targetRow, int targetColumn) = move switch
			{
				DataTableMove.Up => (Math.Max(0, row - 1), column),
				DataTableMove.Down => (Math.Min(lastRow, row + 1), column),
				DataTableMove.PageUp => (Math.Max(0, row - PageSize), column),
				DataTableMove.PageDown => (Math.Min(lastRow, row + PageSize), column),
				DataTableMove.Left => (row, VisibleColumnBefore(column) ?? column),
				DataTableMove.Right => (row, VisibleColumnAfter(column) ?? column),
				DataTableMove.Home => (row, FirstVisibleColumn()),
				DataTableMove.End => (row, LastVisibleColumn()),
				DataTableMove.First => (0, FirstVisibleColumn()),
				DataTableMove.Last => (lastRow, LastVisibleColumn()),
				DataTableMove.Next => StepNext(row, column, lastRow),
				DataTableMove.Previous => StepPrevious(row, column),
				_ => (row, column),
			};

			return new DataTableCell(view[targetRow], targetColumn);
		}

		private (int Row, int Column) StepNext(int row, int column, int lastRow)
		{
			if (VisibleColumnAfter(column) is int next)
			{
				return (row, next);
			}

			if (row < lastRow)
			{
				return (row + 1, FirstVisibleColumn());
			}

			return (row, column);
		}

		private (int Row, int Column) StepPrevious(int row, int column)
		{
			if (VisibleColumnBefore(column) is int previous)
			{
				return (row, previous);
			}

			if (row > 0)
			{
				return (row - 1, LastVisibleColumn());
			}

			return (row, column);
		}

		private int? VisibleColumnAfter(int column)
		{
			for (int candidate = column + 1; candidate < Columns.Count; candidate++)
			{
				if (IsColumnVisible(candidate))
				{
					return candidate;
				}
			}

			return null;
		}

		private int? VisibleColumnBefore(int column)
		{
			for (int candidate = column - 1; candidate >= 0; candidate--)
			{
				if (IsColumnVisible(candidate))
				{
					return candidate;
				}
			}

			return null;
		}

		private int FirstVisibleColumn() => VisibleColumnAfter(-1) ?? 0;

		private int LastVisibleColumn() => VisibleColumnBefore(Columns.Count) ?? (Columns.Count - 1);
	}
}
