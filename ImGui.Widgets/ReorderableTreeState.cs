// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>Where a row was drawn, top and bottom, in screen pixels.</summary>
	/// <param name="Top">The row's top edge.</param>
	/// <param name="Bottom">The row's bottom edge.</param>
	internal readonly record struct ReorderableTreeRowSpan(float Top, float Bottom);

	/// <summary>How a resolved drop is shown.</summary>
	internal enum ReorderableTreeDropKind
	{
		/// <summary>An insertion line between rows.</summary>
		Line,

		/// <summary>A highlight around the container the row drops into.</summary>
		Inside,
	}

	/// <summary>A drop resolved from the pointer, with what the widget needs to draw it.</summary>
	/// <param name="ParentRow">The row that becomes the parent, or -1 for the top level.</param>
	/// <param name="Index">The gap among the parent's children, counted before the move.</param>
	/// <param name="Kind">Whether it is drawn as a line or a container highlight.</param>
	/// <param name="AnchorRow">The row the line is drawn against, or the container highlighted.</param>
	/// <param name="LineY">Where the insertion line is drawn, in screen pixels.</param>
	/// <param name="LineDepth">How far in the insertion line starts, in levels of indent.</param>
	internal readonly record struct ReorderableTreeDrop(
		int ParentRow,
		int Index,
		ReorderableTreeDropKind Kind,
		int AnchorRow,
		float LineY,
		int LineDepth);

	/// <summary>
	/// The interaction behind <see cref="ReorderableTree"/>: which row was pressed, whether it is being
	/// dragged, and where a drag would drop.
	/// </summary>
	/// <remarks>
	/// Deliberately free of ImGui, as <see cref="HandleTrackState"/> and <see cref="CurveTrackState"/>
	/// are. The widget hands in the rows, where it drew them, and the pointer, which is what lets every
	/// rule about where a drop lands be tested without a graphics context.
	/// </remarks>
	internal sealed class ReorderableTreeState
	{
		/// <summary>
		/// The fraction of a container row, from each edge, that drops beside it rather than inside it.
		/// A leaf has no inside, so it splits at the middle instead.
		/// </summary>
		internal const float ContainerEdgeFraction = 0.25f;

		/// <summary>Gets the row the mouse was pressed on, or -1 when it was not pressed on a row.</summary>
		public int PressedRow { get; private set; } = -1;

		/// <summary>Gets the row being dragged, or -1 when nothing is.</summary>
		public int DraggedRow { get; private set; } = -1;

		/// <summary>Gets whether a row is being dragged.</summary>
		public bool IsDragging => DraggedRow >= 0;

		/// <summary>Records a press on <paramref name="row"/>, which becomes a drag once the mouse moves far enough.</summary>
		public void Press(int row)
		{
			PressedRow = row;
			DraggedRow = -1;
		}

		/// <summary>Turns the pending press into a drag, if there is one.</summary>
		/// <returns><see langword="true"/> if a drag started.</returns>
		public bool BeginDrag()
		{
			if (PressedRow < 0 || IsDragging)
			{
				return false;
			}

			DraggedRow = PressedRow;
			return true;
		}

		/// <summary>Forgets the press and any drag.</summary>
		public void Release()
		{
			PressedRow = -1;
			DraggedRow = -1;
		}

		/// <summary>The tree the flat rows describe: each row's parent, its place among its siblings, and where its subtree ends.</summary>
		internal readonly record struct Structure(int[] Parents, int[] SiblingIndices, int[] SubtreeEnds);

		/// <summary>Recovers the tree from the rows' depths.</summary>
		/// <exception cref="ArgumentException">
		/// The first row is not at depth 0, a depth is negative, or a row is more than one deeper than
		/// the row above it. Each of those is a list no tree flattens to, so there is no parent to give
		/// the row rather than a parent to guess.
		/// </exception>
		internal static Structure Analyze(IReadOnlyList<ReorderableTreeRow> rows)
		{
			Ensure.NotNull(rows);

			int count = rows.Count;
			int[] parents = new int[count];
			int[] siblings = new int[count];
			int[] ends = new int[count];

			// The open ancestors of the current row, outermost first, and how many children each has
			// been given so far. The top level is the entry at depth 0 with no row of its own.
			List<int> ancestors = [];
			List<int> childCounts = [0];

			for (int i = 0; i < count; i++)
			{
				int depth = rows[i].Depth;
				if (depth < 0 || depth > ancestors.Count)
				{
					throw new ArgumentException(
						$"Row {i} is at depth {depth}, but the row above it allows at most {ancestors.Count}. The rows must be a tree flattened top to bottom.",
						nameof(rows));
				}

				// Leaving a subtree closes every ancestor deeper than this row, and each one's subtree
				// ends here.
				while (ancestors.Count > depth)
				{
					ends[ancestors[^1]] = i;
					ancestors.RemoveAt(ancestors.Count - 1);
					childCounts.RemoveAt(childCounts.Count - 1);
				}

				parents[i] = depth == 0 ? -1 : ancestors[^1];
				siblings[i] = childCounts[^1]++;

				ancestors.Add(i);
				childCounts.Add(0);
			}

			foreach (int open in ancestors)
			{
				ends[open] = count;
			}

			return new Structure(parents, siblings, ends);
		}

		/// <summary>Resolves where <paramref name="dragged"/> would drop if released at <paramref name="pointer"/>.</summary>
		/// <param name="rows">The rows, top first.</param>
		/// <param name="spans">Where each row was drawn.</param>
		/// <param name="left">The left edge of depth 0, where an unindented row starts.</param>
		/// <param name="indent">The width of one level of indent.</param>
		/// <param name="dragged">The row being dragged.</param>
		/// <param name="pointer">The pointer, in the same space as <paramref name="spans"/>.</param>
		/// <returns>The drop, or <see langword="null"/> where none is allowed or it would change nothing.</returns>
		/// <remarks>
		/// <para>
		/// The top quarter of a container row drops above it, the bottom quarter below it, and the middle
		/// half inside it. A leaf splits at the middle. Above the first row is above it, and below the
		/// last row is below it.
		/// </para>
		/// <para>
		/// Below a row whose children are showing is the top of those children, since that is the gap the
		/// line is drawn in. Below the last row of one or more subtrees, the pointer's horizontal position
		/// picks how far out the drop lands, from the row's own depth out to the depth of the row beneath.
		/// </para>
		/// <para>
		/// The pointer over the dragged row or anywhere in its subtree drops nowhere, since a row cannot
		/// become its own descendant. A drop that would leave the row exactly where it is drops nowhere
		/// either, so the widget never draws a line that means nothing.
		/// </para>
		/// </remarks>
		internal static ReorderableTreeDrop? Resolve(
			IReadOnlyList<ReorderableTreeRow> rows,
			IReadOnlyList<ReorderableTreeRowSpan> spans,
			float left,
			float indent,
			int dragged,
			Vector2 pointer)
		{
			Ensure.NotNull(rows);
			Ensure.NotNull(spans);

			if (rows.Count == 0 || spans.Count != rows.Count || dragged < 0 || dragged >= rows.Count)
			{
				return null;
			}

			Structure tree = Analyze(rows);
			(int row, Zone zone) = Locate(rows, spans, pointer.Y);

			if (row >= dragged && row < tree.SubtreeEnds[dragged])
			{
				return null;
			}

			ReorderableTreeDrop drop = zone switch
			{
				Zone.Before => new ReorderableTreeDrop(
					tree.Parents[row], tree.SiblingIndices[row], ReorderableTreeDropKind.Line, row, spans[row].Top, rows[row].Depth),
				Zone.Inside => new ReorderableTreeDrop(
					row, 0, ReorderableTreeDropKind.Inside, row, spans[row].Bottom, rows[row].Depth + 1),
				_ => After(rows, spans, tree, row, left, indent, pointer.X),
			};

			// Unreachable while the subtree test above holds, since every parent a drop can name is the
			// anchor row or one of its ancestors. Kept so a later zone cannot quietly break the rule.
			bool intoOwnSubtree = drop.ParentRow >= dragged && drop.ParentRow < tree.SubtreeEnds[dragged];

			bool inPlace = drop.ParentRow == tree.Parents[dragged]
				&& (drop.Index == tree.SiblingIndices[dragged] || drop.Index == tree.SiblingIndices[dragged] + 1);

			return intoOwnSubtree || inPlace ? null : drop;
		}

		/// <summary>Which part of a row the pointer is over.</summary>
		private enum Zone
		{
			Before,
			Inside,
			After,
		}

		/// <summary>Finds the row the pointer is over, and which part of it.</summary>
		/// <remarks>
		/// The space between two rows (ImGui's item spacing) belongs below the row above it, which is
		/// the same gap the line would be drawn in either way.
		/// </remarks>
		private static (int Row, Zone Zone) Locate(
			IReadOnlyList<ReorderableTreeRow> rows,
			IReadOnlyList<ReorderableTreeRowSpan> spans,
			float y)
		{
			for (int i = 0; i < spans.Count; i++)
			{
				ReorderableTreeRowSpan span = spans[i];
				if (y >= span.Bottom)
				{
					continue;
				}

				if (y < span.Top)
				{
					return i == 0 ? (0, Zone.Before) : (i - 1, Zone.After);
				}

				float height = span.Bottom - span.Top;
				float fraction = height > 0f ? (y - span.Top) / height : 0.5f;

				if (!rows[i].CanHaveChildren)
				{
					return (i, fraction < 0.5f ? Zone.Before : Zone.After);
				}

				return fraction < ContainerEdgeFraction
					? (i, Zone.Before)
					: fraction >= 1f - ContainerEdgeFraction ? (i, Zone.After) : (i, Zone.Inside);
			}

			return (spans.Count - 1, Zone.After);
		}

		private static ReorderableTreeDrop After(
			IReadOnlyList<ReorderableTreeRow> rows,
			IReadOnlyList<ReorderableTreeRowSpan> spans,
			Structure tree,
			int row,
			float left,
			float indent,
			float x)
		{
			int depth = rows[row].Depth;
			int next = row + 1;
			float lineY = spans[row].Bottom;

			if (next < rows.Count && rows[next].Depth > depth)
			{
				// The row's children are showing, so the gap below it is above the first of them.
				return new ReorderableTreeDrop(row, 0, ReorderableTreeDropKind.Line, row, lineY, depth + 1);
			}

			// Below the end of a subtree there is more than one gap in one place: after the row itself,
			// or after any ancestor it is the last descendant of, out to the depth of the row beneath.
			// The pointer's horizontal position says which. An indent of zero leaves nothing to measure
			// against, so the row's own depth stands.
			int outermost = next < rows.Count ? rows[next].Depth : 0;
			int pointed = indent > 0f ? (int)MathF.Floor((x - left) / indent) : depth;
			int chosen = Math.Clamp(pointed, outermost, depth);

			int anchor = row;
			while (rows[anchor].Depth > chosen)
			{
				anchor = tree.Parents[anchor];
			}

			return new ReorderableTreeDrop(
				tree.Parents[anchor], tree.SiblingIndices[anchor] + 1, ReorderableTreeDropKind.Line, row, lineY, chosen);
		}
	}
}
