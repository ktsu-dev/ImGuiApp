// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// One column of a <see cref="VirtualTable(string, int, ReadOnlySpan{VirtualTableColumn}, Action{int}, VirtualTableOptions?)"/>.
	/// </summary>
	/// <param name="Label">The heading text, which is also the column's ImGui identifier.</param>
	/// <param name="Flags">Sizing and behaviour flags passed through to <c>TableSetupColumn</c>.</param>
	/// <param name="Width">
	/// The initial width or weight, interpreted according to <paramref name="Flags"/>. Zero leaves
	/// the choice to ImGui.
	/// </param>
	public readonly record struct VirtualTableColumn(
		string Label,
		ImGuiTableColumnFlags Flags = ImGuiTableColumnFlags.None,
		float Width = 0f);

	/// <summary>
	/// Options for <see cref="VirtualTable(string, int, ReadOnlySpan{VirtualTableColumn}, Action{int}, VirtualTableOptions?)"/>.
	/// </summary>
	public sealed class VirtualTableOptions
	{
		/// <summary>
		/// Gets the table flags. Defaults to a scrolling, banded, resizable table.
		/// </summary>
		/// <remarks>
		/// <see cref="ImGuiTableFlags.ScrollY"/> is added whatever this is set to, because a table
		/// that does not scroll has nothing to virtualize: the clipper measures visibility against
		/// the scrolling region, and without one every row is visible and every row is drawn.
		/// <see cref="ImGuiTableFlags.Sortable"/> is added when <see cref="OnSortChanged"/> is set,
		/// so asking for the callback is enough to get the clickable headers that raise it.
		/// </remarks>
		public ImGuiTableFlags Flags { get; init; } =
			ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable;

		/// <summary>
		/// Gets the size of the table. Either axis may be zero to take the available space.
		/// </summary>
		public Vector2 OuterSize { get; init; }

		/// <summary>
		/// Gets the height of a row in pixels, or zero to use one line of text plus item spacing.
		/// </summary>
		/// <remarks>
		/// <para>
		/// <b>Every row must be this tall.</b> The clipper skips to a row by multiplying its index
		/// by one height, so a row that draws taller than this overlaps the row below it and every
		/// recorded position after it is wrong. A row that needs more than one line should raise
		/// this for the whole table rather than growing on its own.
		/// </para>
		/// <para>
		/// Variable-height rows are deliberately out of scope for this version. They need
		/// <c>ImGuiListClipper.IncludeItemsByIndex</c> and a height cache the widget would have to
		/// own, which is a different widget rather than a flag on this one.
		/// </para>
		/// </remarks>
		public float RowHeight { get; init; }

		/// <summary>
		/// Gets a value indicating whether the header row is drawn. Defaults to <see langword="true"/>.
		/// </summary>
		public bool ShowHeaders { get; init; } = true;

		/// <summary>
		/// Gets the callback raised when the user changes the sort column or direction, reporting the
		/// column index and whether the sort is ascending.
		/// </summary>
		/// <remarks>
		/// <para>
		/// The widget does not sort anything. It does not own the data, and whether thirty thousand
		/// rows can be reordered cheaply depends on an index the caller either has or does not — so
		/// the caller is told the sort changed and decides what that costs.
		/// </para>
		/// <para>
		/// Only the primary sort column is reported. <see cref="ImGuiTableFlags.SortMulti"/> is not
		/// supported by this version, and the callback would silently report only the first column
		/// if it were set.
		/// </para>
		/// </remarks>
		public Action<int, bool>? OnSortChanged { get; init; }

		/// <summary>
		/// Gets or sets a row to bring into view on the next frame the table is drawn, or -1 for none.
		/// </summary>
		/// <remarks>
		/// Reset to -1 by the widget once the scroll has been issued, so setting it is a one-shot
		/// request rather than a position the caller has to keep clearing. The row is also forced
		/// into the drawn set for that frame, so it is probe-addressable immediately rather than
		/// only after the scroll settles.
		/// </remarks>
		public int ScrollToRow { get; set; } = -1;

		/// <summary>
		/// Gets or sets the selected row, or -1 for none.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Read and written in place: the widget assigns to it when a row is clicked, and reads it
		/// to draw the selection highlight. Selection lives here rather than in a <c>ref int</c>
		/// parameter because the widget's signature takes options by reference already, which gives
		/// the same read-write behaviour without a second out-parameter at every call site.
		/// </para>
		/// <para>
		/// Single selection only. Multi-selection wants a caller-owned set rather than an index,
		/// and can be added without disturbing this.
		/// </para>
		/// </remarks>
		public int SelectedRow { get; set; } = -1;
	}

	/// <summary>
	/// Draws a table whose rows are produced only for the rows on screen, so a row count in the tens
	/// of thousands costs the same per frame as a screenful.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <paramref name="drawRow"/> is called only for visible rows, in ascending index order, and is
	/// given the <b>absolute</b> row index — the index into the caller's data, not the position on
	/// screen. It is entered with the row begun and the cursor in column 0, so it draws the first
	/// cell directly and calls <see cref="ImGui.TableNextColumn"/> before each subsequent one:
	/// </para>
	/// <code>
	/// ImGuiWidgets.VirtualTable("catalogue", objects.Count, columns, row =&gt;
	/// {
	///     ImGui.TextUnformatted(objects[row].Name);
	///     ImGui.TableNextColumn();
	///     ImGui.TextUnformatted(objects[row].Epoch);
	/// });
	/// </code>
	/// <para>
	/// <b>Addressing a row in a test requires scrolling it into view first.</b> A virtual table can
	/// only mark the rows it drew; the rest are not ImGui items at all, so no probe name exists for
	/// them. That is what virtualization means rather than a gap in the marking, and
	/// <see cref="VirtualTableOptions.ScrollToRow"/> is how a test reaches one deterministically.
	/// The table itself is marked under <paramref name="label"/> whether or not any row is visible,
	/// each column heading under <c>&lt;label&gt;/&lt;columnLabel&gt;</c>, and each drawn row under
	/// <c>&lt;label&gt;/[&lt;absoluteIndex&gt;]</c> — an absolute index, so a row's probe name does
	/// not change when the table is scrolled.
	/// </para>
	/// <para>
	/// All rows must be the same height; see <see cref="VirtualTableOptions.RowHeight"/>.
	/// </para>
	/// </remarks>
	/// <param name="label">A unique label used for the table's ImGui ID and its probe name; not drawn.</param>
	/// <param name="rowCount">The total number of rows, including those off screen.</param>
	/// <param name="columns">The columns, left to right. Must not be empty.</param>
	/// <param name="drawRow">Draws one row, given its absolute index.</param>
	/// <param name="options">Sizing, sorting, scrolling and selection. Defaults are used when null.</param>
	/// <exception cref="ArgumentNullException"><paramref name="label"/> or <paramref name="drawRow"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="rowCount"/> is negative.</exception>
	/// <exception cref="ArgumentException"><paramref name="columns"/> is empty.</exception>
	public static void VirtualTable(
		string label,
		int rowCount,
		ReadOnlySpan<VirtualTableColumn> columns,
		Action<int> drawRow,
		VirtualTableOptions? options = null)
	{
		Ensure.NotNull(label);
		Ensure.NotNull(drawRow);
		ArgumentOutOfRangeException.ThrowIfNegative(rowCount);

		if (columns.IsEmpty)
		{
			throw new ArgumentException("A virtual table needs at least one column.", nameof(columns));
		}

		VirtualTableImpl.Draw(label, rowCount, columns, drawRow, options ?? new VirtualTableOptions());
	}

	internal static class VirtualTableImpl
	{
		public static void Draw(
			string label,
			int rowCount,
			ReadOnlySpan<VirtualTableColumn> columns,
			Action<int> drawRow,
			VirtualTableOptions options)
		{
			ImGuiTableFlags flags = options.Flags | ImGuiTableFlags.ScrollY;

			if (options.OnSortChanged is not null)
			{
				flags |= ImGuiTableFlags.Sortable;
			}

			// Captured before the table so the mark covers the whole widget. The table's own rect is
			// not available after EndTable when the table scrolls, because the last submitted item
			// is the inner child rather than the table.
			Vector2 origin = ImGui.GetCursorScreenPos();

			if (!ImGui.BeginTable(label, columns.Length, flags, options.OuterSize))
			{
				return;
			}

			try
			{
				// Frozen so the headings survive the clipper scrolling the body underneath them.
				ImGui.TableSetupScrollFreeze(0, options.ShowHeaders ? 1 : 0);

				for (int column = 0; column < columns.Length; column++)
				{
					ImGui.TableSetupColumn(columns[column].Label, columns[column].Flags, columns[column].Width);
				}

				if (options.ShowHeaders)
				{
					DrawHeaders(label, columns);
				}

				ReportSortChange(options);

				DrawRows(label, rowCount, drawRow, options, ScrollTarget(options, rowCount));
			}
			finally
			{
				ImGui.EndTable();
			}

			ImGuiProbes.MarkRegion(label, origin, ImGui.GetItemRectMax());
		}

		/// <summary>
		/// Submits the header row a column at a time, rather than through <c>TableHeadersRow</c>.
		/// </summary>
		/// <remarks>
		/// <c>TableHeadersRow</c> submits every heading itself and returns, leaving no moment at
		/// which any one of them is the current item — so nothing can be marked. On a sortable table
		/// the headings are the only control there is, and a heading a test cannot click is a table
		/// a test cannot sort. Submitting them individually is ImGui's own supported alternative and
		/// costs one loop.
		/// </remarks>
		private static void DrawHeaders(string label, ReadOnlySpan<VirtualTableColumn> columns)
		{
			ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

			for (int column = 0; column < columns.Length; column++)
			{
				ImGui.TableSetColumnIndex(column);
				ImGui.TableHeader(columns[column].Label);
				ImGuiProbes.MarkItem(label, columns[column].Label);
			}
		}

		/// <summary>
		/// Resolves the requested scroll target and clears the request, so a caller sets it once
		/// rather than having to clear it on the frame after.
		/// </summary>
		private static int ScrollTarget(VirtualTableOptions options, int rowCount)
		{
			int requested = options.ScrollToRow;
			options.ScrollToRow = -1;

			return requested >= 0 && requested < rowCount ? requested : -1;
		}

		private static void DrawRows(
			string label,
			int rowCount,
			Action<int> drawRow,
			VirtualTableOptions options,
			int forcedRow)
		{
			ImGuiListClipper clipper = default;

			// An explicit height lets the clipper skip its measuring pass. Left to ImGui when the
			// caller did not set one, rather than guessed at from the font: a guess that disagreed
			// with the real row pitch would put every skipped row at the wrong offset.
			if (options.RowHeight > 0f)
			{
				clipper.Begin(rowCount, options.RowHeight);
			}
			else
			{
				clipper.Begin(rowCount);
			}

			// The scroll is anchored on the row itself further down, which can only happen if the
			// row is drawn — and a row far outside the visible range would not be. This is what
			// makes ScrollToRow land on the first frame instead of the one after it.
			if (forcedRow >= 0)
			{
				clipper.IncludeItemByIndex(forcedRow);
			}

			while (clipper.Step())
			{
				for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
				{
					DrawRow(label, row, drawRow, options, row == forcedRow);
				}
			}

			clipper.End();
		}

		private static void DrawRow(
			string label,
			int row,
			Action<int> drawRow,
			VirtualTableOptions options,
			bool scrollHere)
		{
			ImGui.TableNextRow(ImGuiTableRowFlags.None, options.RowHeight);
			ImGui.TableSetColumnIndex(0);

			Vector2 rowStart = ImGui.GetCursorScreenPos();

			// A selectable spanning every column, submitted before the caller's content and then
			// drawn over. It is doing three jobs at once, which is why it is worth the trick: it is
			// the row's selection highlight, its hit target, and — being one real ImGui item as wide
			// as the row — the only thing here that knows the row's true rectangle. Marking the row
			// from a rectangle the widget computed instead would be marking an assumption.
			bool clicked = ImGui.Selectable(
				RowId(row),
				options.SelectedRow == row,
				ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap,
				new Vector2(0f, options.RowHeight));

			ImGuiProbes.MarkItem(RowName(label, row));

			if (clicked)
			{
				options.SelectedRow = row;
			}

			if (scrollHere)
			{
				// Anchored on the row rather than computed as index times height, so it stays
				// correct whether or not the caller set RowHeight, and whatever cell padding the
				// current style adds to a row.
				ImGui.SetScrollHereY(0.5f);
			}

			ImGui.SetCursorScreenPos(rowStart);

			drawRow(row);
		}

		/// <summary>
		/// Builds a row's ImGui identifier. Invisible: the label is empty so the selectable draws no
		/// text of its own under the caller's cells, and the index after <c>##</c> keeps the rows
		/// apart.
		/// </summary>
		private static string RowId(int row) =>
			string.Create(CultureInfo.InvariantCulture, $"##row{row}");

		/// <summary>
		/// Builds a row's probe name. Follows the <c>Tags/[0]</c> shape the property grid's lists
		/// already use, so a bracketed index means the same thing across the library.
		/// </summary>
		internal static string RowName(string label, int row) =>
			string.Create(CultureInfo.InvariantCulture, $"{label}/[{row}]");

		private static void ReportSortChange(VirtualTableOptions options)
		{
			if (options.OnSortChanged is null)
			{
				return;
			}

			ImGuiTableSortSpecsPtr sortSpecs = ImGui.TableGetSortSpecs();

			if (sortSpecs.IsNull || !sortSpecs.SpecsDirty)
			{
				return;
			}

			if (sortSpecs.SpecsCount > 0)
			{
				ImGuiTableColumnSortSpecsPtr primary = sortSpecs.Specs;

				options.OnSortChanged(
					primary.ColumnIndex,
					primary.SortDirection == ImGuiSortDirection.Ascending);
			}

			sortSpecs.SpecsDirty = false;
		}
	}
}
