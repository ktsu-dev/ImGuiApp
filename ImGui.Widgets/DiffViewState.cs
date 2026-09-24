// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>One row of a side-by-side view, holding whichever sides exist.</summary>
	/// <param name="Left">The old side's line, or <see langword="null"/> when this row is filler there.</param>
	/// <param name="Right">The new side's line, or <see langword="null"/> when this row is filler there.</param>
	internal readonly record struct DiffRowPair(DiffLine? Left, DiffLine? Right);

	/// <summary>
	/// The rules behind the diff view widget: how a hunk lays out side by side, how its change
	/// summary is scaled, and what selecting does.
	/// </summary>
	/// <remarks>
	/// Deliberately free of ImGui, like <see cref="CurveTrackState"/>, which is what lets every rule
	/// here be tested without a graphics context. Only the collapse set is per widget. Everything
	/// else is static, because it depends on nothing but its arguments.
	/// </remarks>
	internal sealed class DiffViewState
	{
		/// <summary>The longest a change summary bar is allowed to be, in characters.</summary>
		internal const int SummaryWidth = 10;

		private readonly HashSet<int> collapsed = [];

		/// <summary>Whether a hunk is collapsed.</summary>
		/// <param name="index">The hunk's index.</param>
		/// <returns><see langword="true"/> when it is collapsed.</returns>
		internal bool IsCollapsed(int index) => collapsed.Contains(index);

		/// <summary>Collapses an expanded hunk, or expands a collapsed one.</summary>
		/// <param name="index">The hunk's index.</param>
		internal void ToggleCollapsed(int index)
		{
			if (!collapsed.Add(index))
			{
				_ = collapsed.Remove(index);
			}
		}

		/// <summary>
		/// Lays a hunk out as side-by-side rows, filling whichever side runs out first.
		/// </summary>
		/// <remarks>
		/// Removed and added lines are gathered as they are met and paired by index when the run ends,
		/// which is at a context line or at the end of the hunk. Pairing by index rather than by
		/// content is what keeps a change's two halves level with each other, and the filler rows are
		/// what stop the longer side from sliding the shorter one out of alignment.
		/// <para>
		/// A hunk carries both sides of its own change, so this needs nothing outside it. That is what
		/// makes a side-by-side view possible from a patch, which never carries the whole file.
		/// </para>
		/// </remarks>
		/// <param name="hunk">The hunk to lay out.</param>
		/// <returns>The rows, in order.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="hunk"/> is <see langword="null"/>.</exception>
		internal static IReadOnlyList<DiffRowPair> Pair(DiffHunk hunk)
		{
			Ensure.NotNull(hunk);

			List<DiffRowPair> rows = [];
			List<DiffLine> removed = [];
			List<DiffLine> added = [];

			foreach (DiffLine line in hunk.Lines)
			{
				switch (line.Kind)
				{
					case DiffLineKind.Removed:
						removed.Add(line);
						break;

					case DiffLineKind.Added:
						added.Add(line);
						break;

					case DiffLineKind.Context:
					default:
						FlushRun(rows, removed, added);
						rows.Add(new DiffRowPair(line, line));
						break;
				}
			}

			FlushRun(rows, removed, added);

			return rows;
		}

		/// <summary>Emits the gathered run as paired rows and clears it.</summary>
		/// <param name="rows">The rows built so far.</param>
		/// <param name="removed">The removed lines gathered since the last run ended.</param>
		/// <param name="added">The added lines gathered since the last run ended.</param>
		private static void FlushRun(List<DiffRowPair> rows, List<DiffLine> removed, List<DiffLine> added)
		{
			int height = Math.Max(removed.Count, added.Count);

			for (int index = 0; index < height; index++)
			{
				rows.Add(new DiffRowPair(
					index < removed.Count ? removed[index] : null,
					index < added.Count ? added[index] : null));
			}

			removed.Clear();
			added.Clear();
		}

		/// <summary>
		/// Scales a hunk's removed and added counts into the two halves of its summary bar.
		/// </summary>
		/// <remarks>
		/// The bar is proportional rather than absolute, so a hunk of four hundred changes and one of
		/// forty read the same shape at a glance, which is the whole point of showing a bar instead of
		/// two numbers. A side with any changes at all is never rounded away to nothing, because a bar
		/// that says a hunk only adds when it also removes is worse than no bar.
		/// </remarks>
		/// <param name="removedCount">How many lines the hunk removes.</param>
		/// <param name="addedCount">How many lines the hunk adds.</param>
		/// <returns>The two bar lengths, together no longer than <see cref="SummaryWidth"/>.</returns>
		internal static (int Removed, int Added) Summarize(int removedCount, int addedCount)
		{
			int total = removedCount + addedCount;

			if (total == 0)
			{
				return (0, 0);
			}

			int width = Math.Min(total, SummaryWidth);

			int removed = removedCount == 0
				? 0
				: Math.Max(1, (int)Math.Round((double)removedCount / total * width, MidpointRounding.AwayFromZero));

			int added = width - removed;

			// Both sides have changes but rounding gave the whole bar to one of them. Whichever side
			// lost is put back to a single character, which is the smallest honest thing the bar can
			// say about a change that exists.
			if (added == 0 && addedCount > 0)
			{
				added = 1;
				removed = width - 1;
			}

			return (removed, added);
		}

		/// <summary>Selects an unselected hunk, or deselects a selected one.</summary>
		/// <param name="selection">The selected hunk indices.</param>
		/// <param name="index">The hunk to toggle.</param>
		/// <returns><see langword="true"/> always, since a toggle is always a change.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="selection"/> is <see langword="null"/>.</exception>
		internal static bool Toggle(ISet<int> selection, int index)
		{
			Ensure.NotNull(selection);

			return selection.Add(index) || selection.Remove(index);
		}

		/// <summary>Selects every hunk.</summary>
		/// <param name="selection">The selected hunk indices.</param>
		/// <param name="hunkCount">How many hunks there are.</param>
		/// <returns><see langword="true"/> when the selection changed.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="selection"/> is <see langword="null"/>.</exception>
		internal static bool SelectAll(ISet<int> selection, int hunkCount)
		{
			Ensure.NotNull(selection);

			bool changed = false;

			for (int index = 0; index < hunkCount; index++)
			{
				changed |= selection.Add(index);
			}

			return changed;
		}

		/// <summary>Clears the selection.</summary>
		/// <param name="selection">The selected hunk indices.</param>
		/// <returns><see langword="true"/> when the selection changed.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="selection"/> is <see langword="null"/>.</exception>
		internal static bool SelectNone(ISet<int> selection)
		{
			Ensure.NotNull(selection);

			if (selection.Count == 0)
			{
				return false;
			}

			selection.Clear();

			return true;
		}
	}
}
