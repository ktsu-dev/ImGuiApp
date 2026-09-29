// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Collections.Generic;

public static partial class ImGuiWidgets
{
	/// <summary>What a data table's context menu was opened on.</summary>
	/// <param name="Cell">The cell that was right-clicked.</param>
	/// <param name="SelectedRows">
	/// The selected rows at the moment the menu opened, as indices into the caller's list. A copy, so it
	/// doesn't change under the menu while it's open.
	/// </param>
	public readonly record struct DataTableContextMenu(DataTableCell Cell, IReadOnlySet<int> SelectedRows);

	public sealed partial class DataTableState<TRow>
	{
		private bool isContextMenuRequested;

		/// <summary>Gets a value indicating whether a cell is being edited.</summary>
		public bool IsEditing => EditSession is not null;

		internal DataTableEditSession? EditSession { get; private set; }

		internal DataTableContextMenu? ContextMenu { get; private set; }

		/// <summary>Opens an editor on the active cell.</summary>
		/// <param name="firstCharacter">
		/// The character typed to begin editing, which replaces the value, or <see langword="null"/> to
		/// begin from the current value.
		/// </param>
		/// <returns>True when an editor opened. False for a read-only cell, a toggle, or no active cell.</returns>
		internal bool BeginEdit(char? firstCharacter = null)
		{
			if (EditSession is not null || ActiveCell is not DataTableCell cell || ViewPositionOf(cell.SourceIndex) < 0)
			{
				return false;
			}

			EditSession = Columns[cell.Column].CreateSession(cell.Column, cell.SourceIndex, rows[cell.SourceIndex], firstCharacter);
			return EditSession is not null;
		}

		/// <summary>Closes the editor and reports the edit if the value changed.</summary>
		/// <remarks>
		/// The session is cleared before the caller hears about the edit, so a callback that applies the
		/// edit and calls <see cref="Refresh"/> sees a table that has finished editing, and one that throws
		/// doesn't leave the table stuck mid-edit.
		/// </remarks>
		internal void CommitEdit()
		{
			DataTableEditSession? session = EditSession;
			EditSession = null;
			session?.Commit();
		}

		internal void CancelEdit() => EditSession = null;

		/// <summary>Flips the active cell if it's an editable <see cref="bool"/>. Does nothing otherwise.</summary>
		internal void Toggle()
		{
			if (EditSession is null && ActiveCell is DataTableCell cell && ViewPositionOf(cell.SourceIndex) >= 0)
			{
				Columns[cell.Column].Toggle(cell.SourceIndex, rows[cell.SourceIndex]);
			}
		}

		/// <summary>
		/// Makes the right-clicked cell active and records what the menu is for. A row that wasn't
		/// selected becomes the only selected row, which is how file explorers behave.
		/// </summary>
		internal void OpenContextMenu(DataTableCell cell)
		{
			if (!IsShown(cell))
			{
				return;
			}

			if (selectedRows.Contains(cell.SourceIndex))
			{
				FinishEditingUnless(cell);
				ActiveCell = cell;
			}
			else
			{
				Click(cell, isCtrlHeld: false, isShiftHeld: false);
			}

			ContextMenu = new DataTableContextMenu(cell, new HashSet<int>(selectedRows));
			isContextMenuRequested = true;
		}

		/// <summary>Takes the request to open the context menu, so the popup is opened once.</summary>
		internal bool TakeContextMenuRequest()
		{
			bool isRequested = isContextMenuRequested;
			isContextMenuRequested = false;
			return isRequested;
		}

		partial void FinishEditingUnless(DataTableCell? cellKeptOpen)
		{
			if (EditSession is not DataTableEditSession session)
			{
				return;
			}

			bool isKeptOpen = cellKeptOpen is DataTableCell kept
				&& kept.SourceIndex == session.SourceIndex
				&& kept.Column == session.Column;

			if (!isKeptOpen)
			{
				CommitEdit();
			}
		}

		// An edit to a row whose index may now name a different row can't be committed safely, so it's
		// dropped rather than applied to the wrong row.
		partial void OnRowIdentityLost() => CancelEdit();

		// A filter that hides the row being edited takes the editor off screen, so the edit is kept by
		// committing it rather than left open where nobody can see it.
		partial void OnViewRebuilt()
		{
			if (EditSession is DataTableEditSession session && ViewPositionOf(session.SourceIndex) < 0)
			{
				CommitEdit();
			}
		}
	}
}
