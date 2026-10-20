// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using ktsu.TextFilter;

/// <summary>Severity of a <see cref="LogEntry"/>, ordered least to most severe.</summary>
public enum LogViewLevel
{
	/// <summary>Detail only useful when tracing a problem step by step.</summary>
	Trace,

	/// <summary>Diagnostic detail.</summary>
	Debug,

	/// <summary>Ordinary progress.</summary>
	Information,

	/// <summary>Something unexpected that the program recovered from.</summary>
	Warning,

	/// <summary>An operation failed.</summary>
	Error,

	/// <summary>The program cannot carry on.</summary>
	Critical,
}

/// <summary>One line of a log. <paramref name="Message"/> may contain newlines; only the first line is shown in the row.</summary>
/// <param name="Level">The entry's severity.</param>
/// <param name="Message">The text logged. A null message is treated as empty.</param>
/// <param name="Timestamp">When it was logged, printed as given.</param>
/// <param name="Category">The subsystem that logged it, or null.</param>
public readonly record struct LogEntry(LogViewLevel Level, string Message, DateTime Timestamp, string? Category = null);

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Caller-owned view state for <see cref="LogView"/>: the filter, the shown levels, repeat collapsing,
	/// auto-follow, the selection, and an incremental index of which entries are shown.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Makes no ImGui calls. The index is incremental: <see cref="Update"/> considers only entries appended
	/// since the last call, so a log that grows by N lines costs N filter evaluations rather than a rescan.
	/// It rebuilds from scratch when the log shrinks, when a filter input changes, or after
	/// <see cref="Invalidate"/>.
	/// </para>
	/// <para>
	/// The index cannot see a log that changes without changing length. A ring buffer that drops old
	/// lines as it adds new ones must call <see cref="EntriesRemovedFromStart"/>, and a log edited in place
	/// must call <see cref="Invalidate"/>.
	/// </para>
	/// <para>One state serves one view. Two views of one log each need their own.</para>
	/// </remarks>
	public sealed class LogViewState
	{
		private const int LevelCount = 6;

		private readonly bool[] levelEnabled = [true, true, true, true, true, true];
		private readonly List<int> rowStart = [];
		private readonly List<int> rowCount = [];
		private readonly SortedSet<int> selected = [];

		private int indexedCount;
		private bool invalidated = true;
		private string snapshotFilterText = string.Empty;
		private TextFilterType snapshotFilterType;
		private TextFilterMatchOptions snapshotMatchOptions;
		private TextFilterCaseSensitivity snapshotCaseSensitivity;
		private int snapshotLevelMask;
		private bool snapshotCollapse;
		private int anchor = -1;
		private int active = -1;
		private float lastScrollMaxY;

		/// <summary>Gets or sets the filter text, edited by the toolbar's search box. Matched against <see cref="LogEntry.Message"/> only.</summary>
		public string FilterText { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the filter type and match options; the search box's context menu replaces it. Defaults
		/// to Glob, ByWholeString, with <see cref="SearchBoxOptions.ReturnAllWhenEmpty"/> set.
		/// </summary>
		public SearchBoxOptions FilterOptions { get; set; } = new(Label: "filter", ReturnAllWhenEmpty: true, FullWidth: false);

		/// <summary>Gets or sets the case sensitivity of the filter. Defaults to case-insensitive.</summary>
		public TextFilterCaseSensitivity CaseSensitivity { get; set; } = TextFilterCaseSensitivity.CaseInsensitive;

		/// <summary>
		/// Gets or sets a value indicating whether consecutive entries with the same level and message are
		/// shown as one row with a count. Defaults to <see langword="false"/>.
		/// </summary>
		public bool CollapseRepeats { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the list scrolls to keep the newest row visible. Defaults to
		/// <see langword="true"/>; scrolling up turns it off and scrolling back to the bottom turns it on.
		/// </summary>
		public bool IsFollowing { get; set; } = true;

		/// <summary>Gets the number of rows currently shown.</summary>
		public int VisibleCount => rowStart.Count;

		/// <summary>Gets the selected source indices, ascending.</summary>
		public IReadOnlyList<int> SelectedSourceIndices => [.. selected];

		/// <summary>
		/// Gets what the widget keeps between frames for the view this state belongs to. Held here rather than
		/// in a table keyed by ImGui id, because the state is already one per view and outlives no context it
		/// was not given to.
		/// </summary>
		internal ViewData View { get; } = new();

		/// <summary>Gets how many rows are selected, without copying the selection.</summary>
		internal int SelectedCount => selected.Count;

		/// <summary>Gets a number that changes whenever the selection does, so a frame can tell whether it changed it.</summary>
		internal int SelectionVersion { get; private set; }

		/// <summary>Gets a value indicating whether a level is shown. All levels start enabled.</summary>
		/// <param name="level">The level.</param>
		/// <returns><see langword="true"/> when entries of that level are shown.</returns>
		public bool IsLevelEnabled(LogViewLevel level)
		{
			int index = LevelIndex(level);
			return index >= 0 && levelEnabled[index];
		}

		/// <summary>Shows or hides a level.</summary>
		/// <param name="level">The level.</param>
		/// <param name="enabled">Whether to show it.</param>
		public void SetLevelEnabled(LogViewLevel level, bool enabled)
		{
			int index = LevelIndex(level);

			if (index >= 0)
			{
				levelEnabled[index] = enabled;
			}
		}

		/// <summary>Gets the source index of the first entry a visible row represents.</summary>
		/// <param name="visibleRow">The row, 0 at the top.</param>
		/// <returns>The source index.</returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="visibleRow"/> is not a row.</exception>
		public int SourceIndexOf(int visibleRow)
		{
			EnsureRow(visibleRow);
			return rowStart[visibleRow];
		}

		/// <summary>Gets how many consecutive source entries a visible row represents: 1 unless collapsing.</summary>
		/// <param name="visibleRow">The row, 0 at the top.</param>
		/// <returns>The count.</returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="visibleRow"/> is not a row.</exception>
		public int RepeatCountOf(int visibleRow)
		{
			EnsureRow(visibleRow);
			return rowCount[visibleRow];
		}

		/// <summary>
		/// Brings the index up to date with the entries. The widget calls it each frame; call it yourself to
		/// read <see cref="VisibleCount"/> outside a frame.
		/// </summary>
		/// <param name="entries">The log, oldest first.</param>
		/// <exception cref="ArgumentNullException"><paramref name="entries"/> is <see langword="null"/>.</exception>
		public void Update(IReadOnlyList<LogEntry> entries)
		{
			Ensure.NotNull(entries);

			bool rebuild = invalidated || entries.Count < indexedCount || FilterInputsChanged();

			if (rebuild)
			{
				TakeSnapshot();
				rowStart.Clear();
				rowCount.Clear();
				indexedCount = 0;
				invalidated = false;
			}

			for (int source = indexedCount; source < entries.Count; source++)
			{
				Consider(entries, source);
			}

			indexedCount = entries.Count;

			if (rebuild)
			{
				DropSelectionThatStartsNoRow();
			}
		}

		/// <summary>Tells the state the host removed <paramref name="count"/> entries from the start of the log, as a ring buffer does.</summary>
		/// <param name="count">How many entries were removed. Zero or less does nothing.</param>
		public void EntriesRemovedFromStart(int count)
		{
			if (count <= 0)
			{
				return;
			}

			indexedCount = Math.Max(0, indexedCount - count);

			int write = 0;

			for (int row = 0; row < rowStart.Count; row++)
			{
				int start = rowStart[row] - count;
				int span = rowCount[row];

				if (start < 0)
				{
					// A collapsed row keeps the tail that survived; a row with nothing left goes.
					span += start;
					start = 0;
				}

				if (span <= 0)
				{
					continue;
				}

				rowStart[write] = start;
				rowCount[write] = span;
				write++;
			}

			rowStart.RemoveRange(write, rowStart.Count - write);
			rowCount.RemoveRange(write, rowCount.Count - write);

			int[] shifted = [.. selected];
			selected.Clear();

			foreach (int source in shifted)
			{
				if (source - count >= 0)
				{
					selected.Add(source - count);
				}
			}

			anchor = anchor - count >= 0 ? anchor - count : -1;
			active = active - count >= 0 ? active - count : -1;
		}

		/// <summary>Forces a full rebuild on the next <see cref="Update"/>. Call it after replacing or editing entries in place.</summary>
		public void Invalidate() => invalidated = true;

		/// <summary>Gets a value indicating whether a visible row is selected.</summary>
		/// <param name="visibleRow">The row, 0 at the top.</param>
		/// <returns><see langword="true"/> when it is selected; <see langword="false"/> for a row that does not exist.</returns>
		public bool IsRowSelected(int visibleRow) =>
			visibleRow >= 0 && visibleRow < rowStart.Count && selected.Contains(rowStart[visibleRow]);

		/// <summary>Applies a click on a visible row with the modifier keys held.</summary>
		/// <remarks>
		/// A plain click selects the row alone. Ctrl toggles it. Shift replaces the selection with every row
		/// between the last plain or Ctrl click and this one, inclusive, and behaves as a plain click when
		/// there was none. A row that does not exist does nothing.
		/// </remarks>
		/// <param name="visibleRow">The row clicked.</param>
		/// <param name="shift">Whether Shift was held.</param>
		/// <param name="ctrl">Whether Ctrl was held.</param>
		public void Click(int visibleRow, bool shift, bool ctrl)
		{
			if (visibleRow < 0 || visibleRow >= rowStart.Count)
			{
				return;
			}

			int[] before = [.. selected];
			ApplyClick(visibleRow, shift, ctrl);

			if (!selected.SetEquals(before))
			{
				SelectionVersion++;
			}
		}

		/// <summary>Selects every visible row.</summary>
		public void SelectAll()
		{
			int before = selected.Count;
			selected.Clear();

			foreach (int start in rowStart)
			{
				selected.Add(start);
			}

			// Every selected index starts a row, so the selection only changed if it grew.
			if (selected.Count != before)
			{
				SelectionVersion++;
			}
		}

		/// <summary>Clears the selection.</summary>
		public void ClearSelection()
		{
			if (selected.Count > 0)
			{
				SelectionVersion++;
			}

			selected.Clear();
			anchor = -1;
			active = -1;
		}

		private void ApplyClick(int visibleRow, bool shift, bool ctrl)
		{
			int source = rowStart[visibleRow];
			int anchorRow = anchor >= 0 ? rowStart.BinarySearch(anchor) : -1;

			if (shift && anchorRow >= 0)
			{
				selected.Clear();

				for (int row = Math.Min(anchorRow, visibleRow); row <= Math.Max(anchorRow, visibleRow); row++)
				{
					selected.Add(rowStart[row]);
				}

				active = source;
				return;
			}

			if (ctrl)
			{
				if (!selected.Remove(source))
				{
					selected.Add(source);
				}
			}
			else
			{
				selected.Clear();
				selected.Add(source);
			}

			anchor = source;
			active = source;
		}

		/// <summary>Applies one frame's scroll reading to <see cref="IsFollowing"/>.</summary>
		/// <remarks>
		/// Following stops when the list is scrolled more than half a row above the bottom it had on the
		/// previous frame, and resumes when it is scrolled back to the bottom. Comparing against the previous
		/// frame's bottom is what keeps new rows, which grow the bottom, from looking like a scroll up.
		/// </remarks>
		/// <param name="scrollY">The list's scroll position this frame.</param>
		/// <param name="scrollMaxY">The list's maximum scroll this frame.</param>
		/// <param name="rowHeight">The height of a row.</param>
		/// <returns>The resulting <see cref="IsFollowing"/>.</returns>
		public bool UpdateFollow(float scrollY, float scrollMaxY, float rowHeight)
		{
			float tolerance = MathF.Max(rowHeight * 0.5f, 1f);

			if (IsFollowing && scrollY < lastScrollMaxY - tolerance)
			{
				IsFollowing = false;
			}
			else if (!IsFollowing && scrollY >= scrollMaxY - tolerance)
			{
				IsFollowing = true;
			}

			lastScrollMaxY = scrollMaxY;
			return IsFollowing;
		}

		/// <summary>Formats rows as text to copy, one line per row in visible order.</summary>
		/// <remarks>
		/// Each row reads <c>HH:mm:ss.fff [Level] Category: Message</c>, without the category part when the
		/// entry has none and with <c> (×count)</c> after a collapsed row. Rows are joined by <c>\n</c>, and a
		/// message keeps its own newlines.
		/// </remarks>
		/// <param name="entries">The log the state indexes.</param>
		/// <param name="selectionOnly">Whether to copy only the selected rows. Every visible row is copied when this is false or nothing is selected.</param>
		/// <returns>The text.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="entries"/> is <see langword="null"/>.</exception>
		public string BuildCopyText(IReadOnlyList<LogEntry> entries, bool selectionOnly)
		{
			Ensure.NotNull(entries);

			bool onlySelected = selectionOnly && selected.Count > 0;
			StringBuilder text = new();

			for (int row = 0; row < rowStart.Count; row++)
			{
				int source = rowStart[row];

				if ((onlySelected && !selected.Contains(source)) || source >= entries.Count)
				{
					continue;
				}

				if (text.Length > 0)
				{
					text.Append('\n');
				}

				LogEntry entry = entries[source];
				text.Append(entry.Timestamp.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture))
					.Append(" [")
					.Append(entry.Level.ToString())
					.Append("] ");

				if (!string.IsNullOrEmpty(entry.Category))
				{
					text.Append(entry.Category).Append(": ");
				}

				text.Append(entry.Message ?? string.Empty);

				if (rowCount[row] > 1)
				{
					text.Append(" (×").Append(rowCount[row].ToString(CultureInfo.InvariantCulture)).Append(')');
				}
			}

			return text.ToString();
		}

		/// <summary>The widget's per-view bookkeeping: the table's options and what the previous frame saw.</summary>
		internal sealed class ViewData
		{
			public VirtualTableOptions Table { get; set; } = new();

			public VirtualTableColumn[] Columns { get; set; } = [];

			public int PreviousVisibleCount { get; set; }

			public float PreviousScrollY { get; set; }

			public bool ListHovered { get; set; }

			public bool ListFocused { get; set; }
		}

		private static int LevelIndex(LogViewLevel level)
		{
			int index = (int)level;
			return index is >= 0 and < LevelCount ? index : -1;
		}

		private void EnsureRow(int visibleRow)
		{
			if (visibleRow < 0 || visibleRow >= rowStart.Count)
			{
				throw new ArgumentOutOfRangeException(nameof(visibleRow), visibleRow, "Not a visible row.");
			}
		}

		private int LevelMask()
		{
			int mask = 0;

			for (int index = 0; index < LevelCount; index++)
			{
				if (levelEnabled[index])
				{
					mask |= 1 << index;
				}
			}

			return mask;
		}

		private bool FilterInputsChanged() =>
			!string.Equals(FilterText ?? string.Empty, snapshotFilterText, StringComparison.Ordinal)
			|| FilterOptions.FilterType != snapshotFilterType
			|| FilterOptions.MatchOptions != snapshotMatchOptions
			|| CaseSensitivity != snapshotCaseSensitivity
			|| LevelMask() != snapshotLevelMask
			|| CollapseRepeats != snapshotCollapse;

		private void TakeSnapshot()
		{
			snapshotFilterText = FilterText ?? string.Empty;
			snapshotFilterType = FilterOptions.FilterType;
			snapshotMatchOptions = FilterOptions.MatchOptions;
			snapshotCaseSensitivity = CaseSensitivity;
			snapshotLevelMask = LevelMask();
			snapshotCollapse = CollapseRepeats;
		}

		private bool IsShown(LogEntry entry)
		{
			if (!IsLevelEnabled(entry.Level))
			{
				return false;
			}

			return snapshotFilterText.Length == 0
				|| TextFilter.IsMatch(entry.Message ?? string.Empty, snapshotFilterText, snapshotFilterType, snapshotMatchOptions, snapshotCaseSensitivity);
		}

		private void Consider(IReadOnlyList<LogEntry> entries, int source)
		{
			LogEntry entry = entries[source];

			if (!IsShown(entry))
			{
				return;
			}

			int last = rowStart.Count - 1;

			// Only a run with nothing hidden in between collapses, so a collapsed row never hides that
			// something else happened in the middle of it.
			if (snapshotCollapse && last >= 0 && rowStart[last] + rowCount[last] == source)
			{
				LogEntry first = entries[rowStart[last]];

				if (first.Level == entry.Level && string.Equals(first.Message ?? string.Empty, entry.Message ?? string.Empty, StringComparison.Ordinal))
				{
					rowCount[last]++;
					return;
				}
			}

			rowStart.Add(source);
			rowCount.Add(1);
		}

		private void DropSelectionThatStartsNoRow()
		{
			selected.RemoveWhere(source => rowStart.BinarySearch(source) < 0);

			if (anchor >= 0 && rowStart.BinarySearch(anchor) < 0)
			{
				anchor = -1;
			}

			if (active >= 0 && rowStart.BinarySearch(active) < 0)
			{
				active = -1;
			}
		}
	}
}
