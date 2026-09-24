// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.ImGui.Styler;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>How a diff is laid out.</summary>
	public enum DiffViewMode
	{
		/// <summary>One column, in the order the lines appear, which is the shape a patch has.</summary>
		Unified,

		/// <summary>Two columns, the old side against the new, aligned with filler rows.</summary>
		SideBySide,
	}

	/// <summary>How a diff view is drawn.</summary>
	public sealed record DiffViewOptions
	{
		/// <summary>Gets the layout.</summary>
		public DiffViewMode Mode { get; init; } = DiffViewMode.Unified;

		/// <summary>Gets a value indicating whether the line number columns are drawn.</summary>
		public bool CanShowLineNumbers { get; init; } = true;

		/// <summary>Gets a value indicating whether clicking a hunk's header collapses it.</summary>
		public bool CanCollapseHunks { get; init; } = true;
	}

	/// <summary>
	/// Draws a diff with a checkbox on each hunk, and reports whether the selection changed.
	/// </summary>
	/// <remarks>
	/// The diff is drawn, never computed. Whoever produced it owns that, and mapping their result onto
	/// <see cref="DiffHunk"/> is what connects the two.
	/// <para>
	/// <paramref name="selection"/> is mutated in place and holds indices into <paramref name="hunks"/>.
	/// It is the caller's because it is the part that outlives the frame, which is what a caller hands
	/// on to stage what was ticked. An index no longer naming a hunk is ignored rather than tracked,
	/// so a caller re-reading a diff after staging clears the selection.
	/// </para>
	/// <para>
	/// Every glyph the widget draws of its own is ASCII, so it renders on the stock ImGui font. This
	/// package is consumable on its own, without the wider font coverage an application built on
	/// <c>ktsu.ImGui.App</c> configures for itself.
	/// </para>
	/// </remarks>
	/// <param name="label">The widget's ImGui id.</param>
	/// <param name="hunks">The hunks to draw.</param>
	/// <param name="selection">The selected hunk indices, mutated in place.</param>
	/// <param name="options">How to draw it, or <see langword="null"/> for the defaults.</param>
	/// <returns><see langword="true"/> when the selection changed this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="hunks"/> or <paramref name="selection"/> is <see langword="null"/>.</exception>
	public static bool DiffView(
		string label,
		IReadOnlyList<DiffHunk> hunks,
		ISet<int> selection,
		DiffViewOptions? options = null)
	{
		Ensure.NotNull(hunks);
		Ensure.NotNull(selection);

		return DiffViewImpl.Draw(label, hunks, selection, options ?? new DiffViewOptions());
	}

	/// <summary>Draws a diff with nothing to select.</summary>
	/// <remarks>
	/// The same drawing path as the selectable overload with no selection behind it: no checkbox is
	/// submitted, nothing can be ticked, and there is therefore no change to report, which is why this
	/// overload returns nothing.
	/// </remarks>
	/// <param name="label">The widget's ImGui id.</param>
	/// <param name="hunks">The hunks to draw.</param>
	/// <param name="options">How to draw it, or <see langword="null"/> for the defaults.</param>
	/// <exception cref="ArgumentNullException"><paramref name="hunks"/> is <see langword="null"/>.</exception>
	public static void DiffView(
		string label,
		IReadOnlyList<DiffHunk> hunks,
		DiffViewOptions? options = null)
	{
		Ensure.NotNull(hunks);

		_ = DiffViewImpl.Draw(label, hunks, selection: null, options ?? new DiffViewOptions());
	}

	/// <summary>The selected hunk indices that still name a hunk, ascending.</summary>
	/// <remarks>
	/// A selection outlives the diff it was made against. A caller that stages something and re-reads
	/// gets a different set of hunks, and an index that pointed at the third of five means nothing
	/// about the third of two. This is what a caller reads its selection back through, so a stale
	/// index is dropped rather than turned into a hunk that is not there.
	/// </remarks>
	/// <param name="selection">The selected hunk indices.</param>
	/// <param name="hunkCount">How many hunks there are.</param>
	/// <returns>The indices within range, ascending.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="selection"/> is <see langword="null"/>.</exception>
	public static IReadOnlyList<int> SelectedHunkIndices(ISet<int> selection, int hunkCount)
	{
		Ensure.NotNull(selection);

		return [.. selection.Where(index => index >= 0 && index < hunkCount).Order()];
	}

	/// <summary>Selects every hunk of a diff view's selection.</summary>
	/// <remarks>
	/// The widget owns a checkbox per hunk and nothing wider, so the "stage everything" a caller puts
	/// beside it works on the same set through this rather than reimplementing what those checkboxes
	/// mean.
	/// </remarks>
	/// <param name="selection">The selected hunk indices, mutated in place.</param>
	/// <param name="hunkCount">How many hunks there are.</param>
	/// <returns><see langword="true"/> when the selection changed.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="selection"/> is <see langword="null"/>.</exception>
	public static bool SelectAll(ISet<int> selection, int hunkCount)
	{
		Ensure.NotNull(selection);

		bool changed = false;

		for (int index = 0; index < hunkCount; index++)
		{
			changed |= selection.Add(index);
		}

		return changed;
	}

	/// <summary>Clears a diff view's selection.</summary>
	/// <param name="selection">The selected hunk indices, mutated in place.</param>
	/// <returns><see langword="true"/> when the selection changed.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="selection"/> is <see langword="null"/>.</exception>
	public static bool SelectNone(ISet<int> selection)
	{
		Ensure.NotNull(selection);

		if (selection.Count == 0)
		{
			return false;
		}

		selection.Clear();

		return true;
	}

	/// <summary>The row backgrounds a hunk's rows are drawn with.</summary>
	/// <remarks>
	/// Resolved once per widget and passed down. <c>Palette.Semantic</c> builds a theme instance and
	/// maps a dictionary of colors on every read, and a diff asks for a background on every row it
	/// draws, twice per row in the paired view.
	/// </remarks>
	/// <param name="Added">The background for an added line.</param>
	/// <param name="Removed">The background for a removed line.</param>
	/// <param name="Context">The background for a context line.</param>
	/// <param name="Filler">The background for a gap, where one side has no line at all.</param>
	internal readonly record struct DiffRowTints(uint Added, uint Removed, uint Context, uint Filler)
	{
		/// <summary>The background for a line of this kind.</summary>
		/// <param name="kind">What the line does.</param>
		/// <returns>The packed color.</returns>
		internal uint For(DiffLineKind kind) => kind switch
		{
			DiffLineKind.Added => Added,
			DiffLineKind.Removed => Removed,
			_ => Context,
		};
	}

	internal static class DiffViewImpl
	{
		private static readonly Dictionary<uint, DiffViewState> States = [];

		/// <summary>How opaque a changed line's row tint is.</summary>
		/// <remarks>
		/// Low enough that the text stays the text's color rather than becoming a shade of the tint,
		/// which is the failure mode of drawing a diff over a colored row.
		/// </remarks>
		private const float ChangedRowAlpha = 0.22f;

		/// <summary>How opaque a context row's tint is.</summary>
		private const float ContextRowAlpha = 0.10f;

		/// <summary>How opaque a filler row's tint is.</summary>
		/// <remarks>
		/// Deliberately far from <see cref="ContextRowAlpha"/> rather than a nudge away from it. Both
		/// tints come from the same <see cref="ImGuiCol.FrameBg"/>, so a factor of two on an alpha
		/// this low is not a difference anyone can see, and a gap would then be told apart from a
		/// context line only by having no text in it. At this distance the gap reads as a recess.
		/// </remarks>
		private const float FillerRowAlpha = 0.28f;

		/// <summary>How many rows a hunk's table shows before it scrolls.</summary>
		/// <remarks>
		/// A hunk's table needs a height of its own. It scrolls, which makes it a child window, and a
		/// child given no size takes the rest of the window: the first hunk would swallow everything
		/// and every hunk after it would get a sliver. Twenty-four rows draws an ordinary hunk whole,
		/// a change of a few lines inside git's three lines of context either side, and still leaves
		/// several hunks reachable in one window when a diff carries a generated file's worth of them.
		/// </remarks>
		private const int MaxVisibleRows = 24;

		/// <summary>
		/// The flags a hunk's table is opened with. <see cref="ImGuiTableFlags.ScrollY"/> sits beside
		/// <see cref="ImGuiTableFlags.ScrollX"/> for the reason <c>VirtualTable</c> adds it whatever
		/// the caller asks for: the clipper measures visibility against a scrolling region, and a
		/// table without a vertical one draws every row it has. ImGui also suppresses the vertical
		/// scrollbar when only <see cref="ImGuiTableFlags.ScrollX"/> is set, which would put
		/// everything past the box's height out of reach.
		/// </summary>
		private const ImGuiTableFlags HunkTableFlags =
			ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY;

		public static bool Draw(
			string label,
			IReadOnlyList<DiffHunk> hunks,
			ISet<int>? selection,
			DiffViewOptions options)
		{
			if (hunks.Count == 0)
			{
				return false;
			}

			// Captured before anything is submitted, so the marked region covers the whole widget
			// rather than whatever the last hunk left as the current item.
			Vector2 origin = ImGui.GetCursorScreenPos();

			ImGui.PushID(label);

			try
			{
				uint id = ImGui.GetID(label);

				if (!States.TryGetValue(id, out DiffViewState? state))
				{
					state = new DiffViewState();
					States[id] = state;
				}

				DiffRowTints tints = ResolveTints();
				bool changed = false;

				for (int index = 0; index < hunks.Count; index++)
				{
					changed |= DrawHunk(label, index, hunks[index], selection, options, state, tints);
				}

				ImGuiProbes.MarkRegion(label, origin, ImGui.GetItemRectMax());

				return changed;
			}
			finally
			{
				ImGui.PopID();
			}
		}

		private static bool DrawHunk(
			string label,
			int index,
			DiffHunk hunk,
			ISet<int>? selection,
			DiffViewOptions options,
			DiffViewState state,
			DiffRowTints tints)
		{
			ImGui.PushID(index);

			bool changed = false;

			try
			{
				bool hasPrecedingItem = false;

				if (selection is not null)
				{
					bool isSelected = selection.Contains(index);

					if (ImGui.Checkbox("##select", ref isSelected))
					{
						changed = DiffViewState.Toggle(selection, index);
					}

					ImGuiProbes.MarkItem(HunkName(label, index), "select");
					hasPrecedingItem = true;
				}

				(int removedCount, int addedCount) = state.ChangeCounts(index, hunk);
				(int removedBar, int addedBar) = DiffViewState.Summarize(removedCount, addedCount);

				if (removedBar > 0 || addedBar > 0)
				{
					if (hasPrecedingItem)
					{
						ImGui.SameLine();
					}

					DrawSummary(removedBar, addedBar);
					hasPrecedingItem = true;
				}

				// Only when something was submitted on this line. A context-only hunk drawn through
				// the overload with no selection submits neither a checkbox nor a summary, and an
				// unconditional call would put its heading beside whatever the caller drew last.
				if (hasPrecedingItem)
				{
					ImGui.SameLine();
				}

				if (DrawHeading(label, index, hunk, options, state))
				{
					state.ToggleCollapsed(index);
				}

				// A hunk that cannot be collapsed is always drawn. Consulting the collapse set anyway
				// would hide a hunk for good once a caller collapsed it and then turned the option off.
				if (!options.CanCollapseHunks || !state.IsCollapsed(index))
				{
					DrawLines(state, index, hunk, options, tints);
				}
			}
			finally
			{
				ImGui.PopID();
			}

			return changed;
		}

		/// <summary>Draws the hunk's heading, which collapses it when the options allow.</summary>
		/// <returns><see langword="true"/> when the heading was clicked.</returns>
		private static bool DrawHeading(
			string label,
			int index,
			DiffHunk hunk,
			DiffViewOptions options,
			DiffViewState state)
		{
			string heading = hunk.Heading.Length == 0
				? string.Create(CultureInfo.InvariantCulture, $"Hunk {index + 1}")
				: hunk.Heading;

			if (!options.CanCollapseHunks)
			{
				ImGui.TextUnformatted(heading);
				ImGuiProbes.MarkItem(HunkName(label, index), "heading");

				return false;
			}

			// ASCII rather than the geometric triangles a tree usually draws with. A caller on the
			// stock font has no glyph for those and would get a missing-glyph box in their place.
			string marker = state.IsCollapsed(index) ? ">" : "v";

			// Selectable rather than a tree node, because the checkbox above has already been
			// submitted on this line and a tree node would take the whole row's width from it. The id
			// is pinned past the `###`, so it survives the marker flipping and survives a heading
			// carrying `##` of its own, which a C or C++ diff does easily.
			bool clicked = ImGui.Selectable($"{marker} {heading}###heading");
			ImGuiProbes.MarkItem(HunkName(label, index), "heading");

			return clicked;
		}

		/// <summary>
		/// Builds a hunk's probe name, following the bracketed index the rest of the library uses.
		/// </summary>
		/// <param name="label">The widget's label.</param>
		/// <param name="index">The hunk's index.</param>
		/// <returns>The qualified name.</returns>
		internal static string HunkName(string label, int index) =>
			string.Create(CultureInfo.InvariantCulture, $"{label}/[{index}]");

		/// <summary>Draws the proportional bar of removals against additions.</summary>
		/// <param name="removed">How many removal characters the bar carries.</param>
		/// <param name="added">How many addition characters the bar carries.</param>
		private static void DrawSummary(int removed, int added)
		{
			ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0.0f, 0.0f));

			try
			{
				if (removed > 0)
				{
					using (new ScopedColor(ImGuiCol.Text, Palette.Semantic.Error))
					{
						ImGui.TextUnformatted(new string('-', removed));
					}
				}

				if (added > 0)
				{
					if (removed > 0)
					{
						ImGui.SameLine();
					}

					using (new ScopedColor(ImGuiCol.Text, Palette.Semantic.Success))
					{
						ImGui.TextUnformatted(new string('+', added));
					}
				}
			}
			finally
			{
				ImGui.PopStyleVar();
			}
		}

		/// <summary>Draws a hunk's lines in the layout the options ask for.</summary>
		/// <remarks>
		/// Clipped rather than drawn whole, because a hunk of a few thousand lines is ordinary on a
		/// generated file and the cost of a frame should follow what is on screen.
		/// </remarks>
		private static void DrawLines(
			DiffViewState state,
			int index,
			DiffHunk hunk,
			DiffViewOptions options,
			DiffRowTints tints)
		{
			if (options.Mode == DiffViewMode.SideBySide)
			{
				DrawPairedLines(state, index, hunk, options, tints);
				return;
			}

			DrawUnifiedLines(hunk, options, tints);
		}

		/// <summary>The box a hunk's table is drawn in.</summary>
		/// <remarks>
		/// The height follows the row count so a short hunk takes only what it needs, capped at
		/// <see cref="MaxVisibleRows"/> so a long one scrolls inside its own box rather than pushing
		/// every hunk below it out of reach. A width of zero takes the space available, which is what
		/// a table does when it is given no size at all.
		/// <para>
		/// The horizontal scrollbar's height is part of the box rather than on top of it, so the rows
		/// are given room beside it. A hunk of a single long line is otherwise handed a box the
		/// scrollbar alone fills, and clips the one row it exists to show.
		/// </para>
		/// </remarks>
		private static Vector2 OuterSize(int rowCount)
		{
			float rows = Math.Min(rowCount, MaxVisibleRows) * ImGui.GetTextLineHeightWithSpacing();

			return new Vector2(0.0f, rows + ImGui.GetStyle().ScrollbarSize);
		}

		private static void DrawUnifiedLines(DiffHunk hunk, DiffViewOptions options, DiffRowTints tints)
		{
			if (hunk.Lines.Count == 0)
			{
				return;
			}

			int columns = options.CanShowLineNumbers ? 4 : 2;

			if (!ImGui.BeginTable("lines", columns, HunkTableFlags, OuterSize(hunk.Lines.Count)))
			{
				return;
			}

			try
			{
				if (options.CanShowLineNumbers)
				{
					ImGui.TableSetupColumn("old", ImGuiTableColumnFlags.WidthFixed);
					ImGui.TableSetupColumn("new", ImGuiTableColumnFlags.WidthFixed);
				}

				ImGui.TableSetupColumn("marker", ImGuiTableColumnFlags.WidthFixed);
				ImGui.TableSetupColumn("text", ImGuiTableColumnFlags.WidthStretch);

				ImGuiListClipper clipper = default;
				clipper.Begin(hunk.Lines.Count);

				while (clipper.Step())
				{
					for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
					{
						DrawLine(hunk.Lines[row], options, tints);
					}
				}

				clipper.End();
			}
			finally
			{
				ImGui.EndTable();
			}
		}

		/// <summary>Draws a hunk as an old side against a new side, aligned by filler.</summary>
		/// <remarks>
		/// One table with both sides in it rather than two scrolling panes, so they stay level by
		/// construction. Two panes would need their scroll positions kept in step, which is a state
		/// machine that exists only to undo a layout choice.
		/// </remarks>
		private static void DrawPairedLines(
			DiffViewState state,
			int index,
			DiffHunk hunk,
			DiffViewOptions options,
			DiffRowTints tints)
		{
			IReadOnlyList<DiffRowPair> rows = state.RowsFor(index, hunk);

			if (rows.Count == 0)
			{
				return;
			}

			int perSide = options.CanShowLineNumbers ? 3 : 2;

			if (!ImGui.BeginTable("paired", perSide * 2, HunkTableFlags, OuterSize(rows.Count)))
			{
				return;
			}

			try
			{
				SetUpSideColumns(options, "old");
				SetUpSideColumns(options, "new");

				ImGuiListClipper clipper = default;
				clipper.Begin(rows.Count);

				while (clipper.Step())
				{
					for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
					{
						DrawPairedRow(rows[row], options, tints);
					}
				}

				clipper.End();
			}
			finally
			{
				ImGui.EndTable();
			}
		}

		private static void SetUpSideColumns(DiffViewOptions options, string side)
		{
			if (options.CanShowLineNumbers)
			{
				ImGui.TableSetupColumn($"{side}#", ImGuiTableColumnFlags.WidthFixed);
			}

			ImGui.TableSetupColumn($"{side}marker", ImGuiTableColumnFlags.WidthFixed);
			ImGui.TableSetupColumn($"{side}text", ImGuiTableColumnFlags.WidthStretch);
		}

		private static void DrawPairedRow(DiffRowPair pair, DiffViewOptions options, DiffRowTints tints)
		{
			ImGui.TableNextRow();

			DrawSide(pair.Left, isOldSide: true, options, tints);
			DrawSide(pair.Right, isOldSide: false, options, tints);
		}

		/// <summary>Draws one side of a paired row, or an empty tinted gap where it has no line.</summary>
		/// <remarks>
		/// Tinted a cell at a time, which is where this deliberately differs from the unified view: a
		/// paired row carries a removal on one side and an addition on the other, and a single row
		/// background cannot say two things at once.
		/// </remarks>
		private static void DrawSide(DiffLine? line, bool isOldSide, DiffViewOptions options, DiffRowTints tints)
		{
			uint tint = line is null ? tints.Filler : tints.For(line.Kind);

			if (options.CanShowLineNumbers)
			{
				_ = ImGui.TableNextColumn();
				ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, tint);
				DrawNumber(isOldSide ? line?.OldNumber : line?.NewNumber);
			}

			_ = ImGui.TableNextColumn();

			if (line is null)
			{
				// Every cell in the row's shape is tinted, the number cell included, so a filler row
				// reads as a gap across its whole width rather than only where the marker and text sit.
				ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, tint);
				_ = ImGui.TableNextColumn();
				ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, tint);
				return;
			}

			ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, tint);
			ImGui.TextUnformatted(MarkerFor(line.Kind));

			_ = ImGui.TableNextColumn();
			ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, tint);
			ImGui.TextUnformatted(line.Text);
		}

		/// <summary>Draws one line of the unified view.</summary>
		/// <remarks>
		/// One background across the whole row, where the paired view tints cell by cell. A unified
		/// row is one line of one kind, so the row is the unit that carries the color here and
		/// tinting each cell would repeat the same value four times.
		/// </remarks>
		private static void DrawLine(DiffLine line, DiffViewOptions options, DiffRowTints tints)
		{
			ImGui.TableNextRow();
			ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, tints.For(line.Kind));

			if (options.CanShowLineNumbers)
			{
				_ = ImGui.TableNextColumn();
				DrawNumber(line.OldNumber);

				_ = ImGui.TableNextColumn();
				DrawNumber(line.NewNumber);
			}

			_ = ImGui.TableNextColumn();
			ImGui.TextUnformatted(MarkerFor(line.Kind));

			_ = ImGui.TableNextColumn();
			ImGui.TextUnformatted(line.Text);
		}

		private static void DrawNumber(int? number)
		{
			if (number is int value)
			{
				ImGui.TextUnformatted(value.ToString(CultureInfo.InvariantCulture));
			}
		}

		internal static string MarkerFor(DiffLineKind kind) => kind switch
		{
			DiffLineKind.Added => "+",
			DiffLineKind.Removed => "-",
			_ => " ",
		};

		/// <summary>Resolves every row background from the current theme and style.</summary>
		/// <returns>The backgrounds this frame's rows are drawn with.</returns>
		internal static DiffRowTints ResolveTints()
		{
			Vector4 surface = ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg];

			return new DiffRowTints(
				Added: Tint(Palette.Semantic.Success.Value, ChangedRowAlpha),
				Removed: Tint(Palette.Semantic.Error.Value, ChangedRowAlpha),
				Context: Tint(surface, ContextRowAlpha),
				Filler: Tint(surface, FillerRowAlpha));
		}

		private static uint Tint(Vector4 color, float alpha)
		{
			color.W = alpha;

			return ImGui.ColorConvertFloat4ToU32(color);
		}
	}
}
