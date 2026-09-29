// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Linq;

using ktsu.TextFilter;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// What a data table keeps between frames: the sorted and filtered view of the caller's rows, the
	/// active cell, the selected rows, and the cell being edited. Keep one per table, and pass the same
	/// one every frame.
	/// </summary>
	/// <remarks>
	/// Makes no ImGui calls. The renderer drives it with commands and reads it back, which is what lets
	/// every rule here be tested without a window.
	/// </remarks>
	/// <typeparam name="TRow">The type of one row.</typeparam>
	public sealed partial class DataTableState<TRow>
	{
		private readonly string[] filterTexts;
		private readonly SearchBoxOptions[] filterOptions;
		private readonly bool[] visibleColumns;
		private IReadOnlyList<TRow> rows = [];
		private int[] view = [];
		private int[] viewPositions = [];
		private int syncedCount = -1;
		private bool isRefreshPending;

		/// <summary>Creates the state for a table with the given columns.</summary>
		/// <param name="columns">The columns, left to right. Labels must be unique.</param>
		/// <exception cref="ArgumentNullException"><paramref name="columns"/> or one of its items is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">
		/// There are no columns, two share a label, or an editable column's value type has no built-in
		/// editor and no <c>Editor</c> was supplied.
		/// </exception>
		public DataTableState(IReadOnlyList<DataTableColumn<TRow>> columns)
		{
			Ensure.NotNull(columns);

			if (columns.Count == 0)
			{
				throw new ArgumentException("A data table needs at least one column.", nameof(columns));
			}

			HashSet<string> labels = new(StringComparer.Ordinal);
			foreach (DataTableColumn<TRow> column in columns)
			{
				Ensure.NotNull(column);
				column.Validate();

				// The label is the column's ImGui id and its probe name, so two alike would be one column
				// to ImGui and to a test.
				if (!labels.Add(column.Label))
				{
					throw new ArgumentException($"Two columns are labeled '{column.Label}'. Labels must be unique.", nameof(columns));
				}
			}

			Columns = [.. columns];
			filterTexts = [.. Enumerable.Repeat(string.Empty, columns.Count)];
			filterOptions = [.. Enumerable.Repeat(DataTableFilter.DefaultOptions, columns.Count)];
			visibleColumns = [.. Enumerable.Repeat(true, columns.Count)];
		}

		/// <summary>Gets the columns, left to right.</summary>
		public IReadOnlyList<DataTableColumn<TRow>> Columns { get; }

		/// <summary>
		/// Gets or sets a condition a row must meet to be shown, for filters the filter row can't
		/// express. Setting it rebuilds the view.
		/// </summary>
		public Func<TRow, bool>? RowFilter
		{
			get;
			set
			{
				field = value;
				Rebuild();
			}
		}

		internal IReadOnlyList<int> View => view;

		internal int SortColumn { get; private set; } = -1;

		internal bool IsSortAscending { get; private set; } = true;

		/// <summary>
		/// Rebuilds the view from the rows as they are now. Call it after applying an edit to a value the
		/// table sorts or filters on, to move the row to where it now belongs.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The table rebuilds by itself when the sort, a filter, or the row count changes. It doesn't
		/// rebuild when a value changes in place, because it can't see that happen, and because a row
		/// that jumps away while it's being edited is disorienting.
		/// </para>
		/// <para>
		/// A caller that applies an edit by replacing its list may call this before the table has seen the
		/// new list. The table rebuilds again when it's next given a different list, so the view follows
		/// the new values.
		/// </para>
		/// </remarks>
		public void Refresh()
		{
			isRefreshPending = true;
			Rebuild();
		}

		internal TRow RowAt(int sourceIndex) => rows[sourceIndex];

		internal int ViewPositionOf(int sourceIndex) =>
			sourceIndex >= 0 && sourceIndex < viewPositions.Length ? viewPositions[sourceIndex] : -1;

		internal bool IsColumnVisible(int column) => visibleColumns[column];

		internal void SetColumnVisible(int column, bool isVisible) => visibleColumns[column] = isVisible;

		internal string GetFilter(int column) => filterTexts[column];

		internal SearchBoxOptions GetFilterOptions(int column) => filterOptions[column];

		/// <summary>
		/// Takes the caller's rows for this frame, and rebuilds if their number changed, or if a refresh
		/// was asked for before the caller replaced the list.
		/// </summary>
		internal void Sync(IReadOnlyList<TRow> source)
		{
			Ensure.NotNull(source);
			bool isReplacedAfterRefresh = isRefreshPending && !ReferenceEquals(source, rows);
			isRefreshPending = false;
			rows = source;

			if (source.Count == syncedCount)
			{
				if (isReplacedAfterRefresh)
				{
					Rebuild();
				}

				return;
			}

			// Inserting or deleting rows shifts every source index after the change, so the selection
			// no longer names the rows it did. The first sync has nothing to forget.
			if (syncedCount >= 0)
			{
				ForgetRowIdentity();
			}

			syncedCount = source.Count;
			Rebuild();
		}

		internal void SetSort(int column, bool isAscending)
		{
			SortColumn = column;
			IsSortAscending = isAscending;
			Rebuild();
		}

		internal void SetFilter(int column, string text, SearchBoxOptions? options = null)
		{
			Ensure.NotNull(text);
			filterTexts[column] = text;

			if (options is not null)
			{
				filterOptions[column] = options;
			}

			Rebuild();
		}

		private void Rebuild()
		{
			int previousPosition = ActiveViewRow;

			int[] next = [.. Enumerable.Range(0, rows.Count).Where(Survives)];

			if (SortColumn >= 0 && SortColumn < Columns.Count && Columns[SortColumn].CanSort)
			{
				DataTableColumn<TRow> column = Columns[SortColumn];
				int direction = IsSortAscending ? 1 : -1;

				// Ties fall back to the source index in both directions, so rows that compare equal keep
				// their order and never trade places from one rebuild to the next.
				Array.Sort(next, (left, right) =>
				{
					int order = Math.Sign(column.Compare(rows[left], rows[right])) * direction;
					return order != 0 ? order : left.CompareTo(right);
				});
			}

			view = next;
			viewPositions = new int[rows.Count];
			Array.Fill(viewPositions, -1);

			for (int position = 0; position < view.Length; position++)
			{
				viewPositions[view[position]] = position;
			}

			AfterRebuild(previousPosition);
		}

		private bool Survives(int sourceIndex)
		{
			TRow row = rows[sourceIndex];

			if (RowFilter is not null && !RowFilter(row))
			{
				return false;
			}

			for (int column = 0; column < Columns.Count; column++)
			{
				string filter = filterTexts[column];
				if (filter.Length == 0)
				{
					continue;
				}

				SearchBoxOptions options = filterOptions[column];
				if (!TextFilter.IsMatch(Columns[column].GetText(row), filter, options.FilterType, options.MatchOptions))
				{
					return false;
				}
			}

			return true;
		}
	}

	/// <summary>Defaults shared by every data table's filter row.</summary>
	internal static class DataTableFilter
	{
		/// <summary>
		/// Gets the options each filter box starts with. Fuzzy, which matches the typed characters in
		/// order and ignores case, because that's what a person typing part of a value expects. Glob and
		/// regex are a right-click away through the search box's own menu.
		/// </summary>
		internal static SearchBoxOptions DefaultOptions { get; } = new(
			Label: "##filter",
			FilterType: TextFilterType.Fuzzy,
			MatchOptions: TextFilterMatchOptions.ByWholeString,
			FullWidth: true);
	}
}
