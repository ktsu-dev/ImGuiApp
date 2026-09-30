// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// The selection, focus and grid arithmetic behind an
	/// <see cref="AssetBrowser(string, int, Func{int, string}, Func{int, nint}, AssetBrowserState, AssetBrowserOptions?)"/>,
	/// with no ImGui dependency.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Selection follows the file-manager convention. A plain click selects one item and makes it the
	/// anchor; a Ctrl+click toggles one and moves the anchor to it; a Shift+click selects the range from
	/// the anchor to the clicked item without moving the anchor, so a second Shift+click re-draws the
	/// range rather than growing it; and Ctrl+Shift adds that range to what was already selected.
	/// </para>
	/// <para>
	/// The caller owns this object, so a selection survives the widget being hidden and a host can read
	/// or change it between frames.
	/// </para>
	/// </remarks>
	public sealed class AssetBrowserState
	{
		private readonly SortedSet<int> selected = [];
		private int[]? selectedSnapshot;

		/// <summary>Gets the selected item indices, in ascending order.</summary>
		public IReadOnlyList<int> SelectedIndices => selectedSnapshot ??= [.. selected];

		/// <summary>
		/// Gets or sets the keyboard focus: the item the arrow keys move from and Enter activates, or -1
		/// when no item has focus.
		/// </summary>
		public int FocusIndex { get; set; } = -1;

		/// <summary>
		/// Gets or sets the tile a plain press landed on while it was already selected, which is resolved
		/// on release: a click if the press did not become a drag, nothing if it did.
		/// </summary>
		internal int PendingClick { get; set; } = -1;

		/// <summary>Gets or sets a value indicating whether a drag began from the pending press.</summary>
		internal bool DragStarted { get; set; }

		/// <summary>Gets the item a Shift+click or Shift+arrow range is drawn from, or -1 when there is none.</summary>
		public int AnchorIndex { get; private set; } = -1;

		/// <summary>Returns whether the item at <paramref name="index"/> is selected.</summary>
		/// <param name="index">The item index.</param>
		/// <returns><see langword="true"/> when the item is selected.</returns>
		public bool IsSelected(int index) => selected.Contains(index);

		/// <summary>Applies a click, with the modifier keys held at the time.</summary>
		/// <param name="index">The item clicked, or a negative value for a click on empty space.</param>
		/// <param name="ctrl">Whether Ctrl was held.</param>
		/// <param name="shift">Whether Shift was held.</param>
		/// <returns><see langword="true"/> when the selection changed.</returns>
		/// <remarks>A click on empty space clears the selection and leaves focus and anchor where they were.</remarks>
		public bool Click(int index, bool ctrl, bool shift)
		{
			if (index < 0)
			{
				return Clear();
			}

			if (shift && AnchorIndex >= 0)
			{
				SortedSet<int> before = [.. selected];
				if (!ctrl)
				{
					selected.Clear();
				}

				AddRange(AnchorIndex, index);
				FocusIndex = index;
				return Commit(!before.SetEquals(selected));
			}

			FocusIndex = index;
			AnchorIndex = index;

			if (ctrl)
			{
				if (!selected.Remove(index))
				{
					selected.Add(index);
				}

				return Commit(true);
			}

			return SelectOnly(index);
		}

		/// <summary>Selects every item.</summary>
		/// <param name="count">The number of items.</param>
		/// <returns><see langword="true"/> when the selection changed.</returns>
		public bool SelectAll(int count)
		{
			if (count <= 0)
			{
				return false;
			}

			bool changed = selected.Count != count || selected.Min != 0 || selected.Max != count - 1;
			selected.Clear();
			AddRange(0, count - 1);

			if (FocusIndex < 0)
			{
				FocusIndex = 0;
			}

			if (AnchorIndex < 0)
			{
				AnchorIndex = 0;
			}

			return Commit(changed);
		}

		/// <summary>Deselects everything, leaving focus and anchor where they were.</summary>
		/// <returns><see langword="true"/> when anything was selected.</returns>
		public bool Clear()
		{
			if (selected.Count == 0)
			{
				return false;
			}

			selected.Clear();
			return Commit(true);
		}

		/// <summary>Moves the focus by <paramref name="delta"/> items, as an arrow key does.</summary>
		/// <param name="delta">How far to move; one for a column, the column count for a row.</param>
		/// <param name="count">The number of items, which the new focus is clamped to.</param>
		/// <param name="shift">Whether Shift was held, extending the selection from the anchor.</param>
		/// <returns><see langword="true"/> when the focus or the selection changed.</returns>
		/// <remarks>
		/// With no focus the first move lands on the first item whichever way it went, because there is
		/// nowhere to move from. A move that the clamp cancels out changes nothing.
		/// </remarks>
		public bool Move(int delta, int count, bool shift)
		{
			if (count <= 0)
			{
				return false;
			}

			if (FocusIndex < 0)
			{
				FocusIndex = 0;
				AnchorIndex = 0;
				_ = SelectOnly(0);
				return true;
			}

			int newFocus = (int)Math.Clamp((long)FocusIndex + delta, 0L, count - 1L);
			if (newFocus == FocusIndex)
			{
				return false;
			}

			FocusIndex = newFocus;

			if (shift && AnchorIndex >= 0)
			{
				selected.Clear();
				AddRange(AnchorIndex, newFocus);
				return Commit(true);
			}

			AnchorIndex = newFocus;
			_ = SelectOnly(newFocus);
			return true;
		}

		/// <summary>
		/// Drops every index that no longer names an item, and pulls focus and anchor back inside the list.
		/// </summary>
		/// <param name="count">The new number of items.</param>
		/// <returns><see langword="true"/> when the selection changed.</returns>
		public bool ItemCountChanged(int count)
		{
			int last = Math.Max(count, 0) - 1;

			if (FocusIndex > last)
			{
				FocusIndex = last;
			}

			if (AnchorIndex > last)
			{
				AnchorIndex = last;
			}

			if (selected.Count == 0 || selected.Max <= last)
			{
				return false;
			}

			selected.RemoveWhere(index => index > last);
			return Commit(true);
		}

		/// <summary>Returns how many whole tiles fit across <paramref name="availableWidth"/>.</summary>
		/// <param name="availableWidth">The width to fill.</param>
		/// <param name="tileWidth">The width of one tile.</param>
		/// <param name="spacing">The gap between tiles, which is not needed after the last one.</param>
		/// <returns>The column count, never less than one.</returns>
		public static int ColumnCount(float availableWidth, float tileWidth, float spacing)
		{
			if (!float.IsFinite(availableWidth) || !float.IsFinite(tileWidth) || !float.IsFinite(spacing) || tileWidth <= 0f)
			{
				return 1;
			}

			float columns = (availableWidth + spacing) / (tileWidth + spacing);
			return columns >= int.MaxValue ? int.MaxValue : Math.Max(1, (int)columns);
		}

		/// <summary>Returns how many rows <paramref name="count"/> items take at <paramref name="columns"/> per row.</summary>
		/// <param name="count">The number of items.</param>
		/// <param name="columns">The column count.</param>
		/// <returns>The row count.</returns>
		public static int RowCount(int count, int columns) =>
			count <= 0 ? 0 : (int)(((long)count + Math.Max(columns, 1) - 1) / Math.Max(columns, 1));

		/// <summary>Returns the row the item at <paramref name="index"/> sits in.</summary>
		/// <param name="index">The item index.</param>
		/// <param name="columns">The column count.</param>
		/// <returns>The row.</returns>
		public static int RowOf(int index, int columns) => index / Math.Max(columns, 1);

		/// <summary>
		/// Returns the scroll offset that brings <paramref name="row"/> into view while moving as little as
		/// possible, or <paramref name="scrollY"/> when it is already visible.
		/// </summary>
		/// <param name="row">The row to reveal.</param>
		/// <param name="rowHeight">The pitch of one row.</param>
		/// <param name="scrollY">The current scroll offset.</param>
		/// <param name="viewHeight">The height of the visible area.</param>
		/// <returns>The new scroll offset.</returns>
		/// <remarks>A row taller than the view is aligned to its top, so the start of the row is what shows.</remarks>
		public static float ScrollToReveal(int row, float rowHeight, float scrollY, float viewHeight)
		{
			float top = row * rowHeight;
			float bottom = top + rowHeight;

			if (top < scrollY)
			{
				return top;
			}

			if (bottom > scrollY + viewHeight)
			{
				return Math.Min(bottom - viewHeight, top);
			}

			return scrollY;
		}

		private bool SelectOnly(int index)
		{
			if (selected.Count == 1 && selected.Min == index)
			{
				return false;
			}

			selected.Clear();
			selected.Add(index);
			return Commit(true);
		}

		private void AddRange(int from, int to)
		{
			int low = Math.Min(from, to);
			int high = Math.Max(from, to);
			for (int index = low; index <= high; index++)
			{
				selected.Add(index);
			}
		}

		private bool Commit(bool changed)
		{
			if (changed)
			{
				selectedSnapshot = null;
			}

			return changed;
		}
	}
}
