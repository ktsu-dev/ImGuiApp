// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a tree whose rows can be dragged to reorder them and to move them between containers.
	/// </summary>
	/// <param name="label">A unique label, used for the ImGui ID and as the probe scope of the rows.</param>
	/// <param name="rows">The rows to draw, top first, as the caller has laid them out.</param>
	/// <param name="drawRow">
	/// Draws the content of one row, given its index in <paramref name="rows"/>. Called once per row per
	/// frame, already indented to the row's depth and inside the row's own ID scope, so two rows can
	/// submit identically labelled items.
	/// </param>
	/// <returns>The move the user dropped this frame, or <see langword="null"/>.</returns>
	/// <remarks>
	/// <para>
	/// <b>The tree draws no content and owns no data.</b> The caller flattens its own tree into rows,
	/// leaving out the children of anything collapsed, and draws each row's content itself: a
	/// disclosure arrow, a visibility toggle, a selectable name. The widget lays the rows out, turns a
	/// press and drag on one into a drag, draws where it would land, and on release reports the move.
	/// It never applies it, so a caller's move goes through its own model and its own undo history.
	/// </para>
	/// <para>
	/// The top quarter of a container row drops above it, the bottom quarter below it, and the middle
	/// half inside it. A leaf splits at the middle. Below the last row of a subtree, how far left the
	/// pointer is picks how far out the drop lands. A drop onto the dragged row or anywhere in its
	/// subtree is refused, and so is one that would leave it where it is.
	/// </para>
	/// <para>
	/// The reported index counts the new parent's children before the move; see
	/// <see cref="ReorderableTreeMove.Index"/> for the one case where that differs from the position the
	/// row ends up at.
	/// </para>
	/// <para>
	/// A drag starts from a press anywhere on a row, including on an item the row drew, once the mouse
	/// has moved ImGui's drag threshold. A click that never moves that far is left to the row's items,
	/// and so is a press on an item that uses the drag itself: a text field selecting text, or a slider,
	/// drag field or other control whose value the drag is changing.
	/// Each row is marked for probes as <c>row{index}</c> inside the tree's label.
	/// </para>
	/// </remarks>
	public static ReorderableTreeMove? ReorderableTree(string label, IReadOnlyList<ReorderableTreeRow> rows, Action<int> drawRow) =>
		ReorderableTreeImpl.Draw(label, rows, drawRow);

	internal static class ReorderableTreeImpl
	{
		private static readonly Dictionary<uint, ReorderableTreeState> States = [];

		public static ReorderableTreeMove? Draw(string label, IReadOnlyList<ReorderableTreeRow> rows, Action<int> drawRow)
		{
			Ensure.NotNull(label);
			Ensure.NotNull(rows);
			Ensure.NotNull(drawRow);

			// Checked before anything is drawn, so a list that is not a tree throws without leaving the
			// ID stack or the probe scopes unbalanced.
			ReorderableTreeState.Analyze(rows);

			uint id = ImGui.GetID(label);
			if (!States.TryGetValue(id, out ReorderableTreeState? state))
			{
				state = new ReorderableTreeState();
				States[id] = state;
			}

			float indent = ImGui.GetStyle().IndentSpacing;
			float left = ImGui.GetCursorScreenPos().X;
			float right = left + MathF.Max(ImGui.GetContentRegionAvail().X, 1f);

			ReorderableTreeRowSpan[] spans = new ReorderableTreeRowSpan[rows.Count];

			using (new ScopedId(label))
			{
				for (int i = 0; i < rows.Count; i++)
				{
					spans[i] = DrawRow(rows[i], i, drawRow, left, right, indent);
				}
			}

			return Interact(state, rows, spans, left, right, indent);
		}

		private static ReorderableTreeRowSpan DrawRow(
			ReorderableTreeRow row,
			int index,
			Action<int> drawRow,
			float left,
			float right,
			float indent)
		{
			float offset = row.Depth * indent;

			ImGui.PushID(index);
			try
			{
				if (offset > 0f)
				{
					ImGui.Indent(offset);
				}

				float top = ImGui.GetCursorScreenPos().Y;

				ImGui.BeginGroup();
				drawRow(index);
				ImGui.EndGroup();

				// A row the caller drew nothing into still occupies a line, so it can still be dragged
				// and dropped on rather than collapsing to a zero-height target nothing can hit.
				float bottom = MathF.Max(ImGui.GetItemRectMax().Y, top + ImGui.GetFrameHeight());

				if (offset > 0f)
				{
					ImGui.Unindent(offset);
				}

				// The whole width, not the content's: a short name is still a full row to drop on.
				ImGuiProbes.MarkRegion($"row{index}", new Vector2(left, top), new Vector2(right, bottom));
				return new ReorderableTreeRowSpan(top, bottom);
			}
			finally
			{
				ImGui.PopID();
			}
		}

		private static ReorderableTreeMove? Interact(
			ReorderableTreeState state,
			IReadOnlyList<ReorderableTreeRow> rows,
			ReorderableTreeRowSpan[] spans,
			float left,
			float right,
			float indent)
		{
			// An undo or a collapse can shorten the rows under a held press. The row index the press
			// remembers would then name a different row, or none, so the gesture is dropped.
			if (state.PressedRow >= rows.Count)
			{
				state.Release();
			}

			Vector2 mouse = ImGui.GetIO().MousePos;

			// Blocked-by-active-item is allowed because the press lands on the row's own items, which
			// become active on that same frame.
			if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)
				&& ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem))
			{
				int pressed = RowAt(spans, mouse, left, right);
				if (pressed >= 0)
				{
					state.Press(pressed);
				}
				else
				{
					state.Release();
				}
			}

			// A press on an item that takes the drag for itself — a text field selecting text, a slider
			// or drag field changing its value — is that item's gesture, not the row's. Checked every
			// frame the button is held rather than only when the drag would start, because an item can
			// show what it is late: a slider pressed exactly at its own value edits nothing until the
			// pointer moves along it, which may be after the pointer has already drifted far enough
			// vertically to have picked the row up.
			if (state.PressedRow >= 0 && ActiveItemOwnsTheDrag())
			{
				state.Release();
			}

			if (!state.IsDragging && state.PressedRow >= 0 && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
			{
				state.BeginDrag();
			}

			ReorderableTreeDrop? drop = state.IsDragging
				? ReorderableTreeState.Resolve(rows, spans, left, indent, state.DraggedRow, mouse)
				: null;

			if (drop is ReorderableTreeDrop shown)
			{
				DrawDrop(shown, spans, left, right, indent);
			}

			if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
			{
				int dragged = state.DraggedRow;
				state.Release();

				if (dragged >= 0 && drop is ReorderableTreeDrop landed)
				{
					return new ReorderableTreeMove(dragged, landed.ParentRow, landed.Index);
				}
			}

			return null;
		}

		/// <summary>Whether the item the press made active is one that uses a mouse drag itself.</summary>
		/// <remarks>
		/// The window's move handle, which a press on a row's background activates, is not an item.
		/// Otherwise three signs, any one of which is enough. A text field wants the keyboard. ImGui's sliders and
		/// drag fields claim the left and right arrow keys the moment they activate, which a button or
		/// selectable never does. And any item that has changed its value since it activated is being
		/// edited by this press, which covers controls drawn with <c>ButtonBehavior</c>, such as
		/// <see cref="ValueSlider(string, ref float, float, float, ValueSliderOptions?)"/> or a knob, that
		/// claim no keys. A selectable or button held and dragged does none of these, so a row can
		/// still be picked up by its name.
		/// </remarks>
		private static bool ActiveItemOwnsTheDrag()
		{
			if (ImGui.GetIO().WantTextInput)
			{
				return true;
			}

			if (ImGuiP.GetActiveID() == 0)
			{
				return false;
			}

			// A press on a row's background activates the window's own move handle, which claims every
			// key. That is the press a row drag is made from, not an item, so it does not count.
			ImGuiContextPtr context = ImGui.GetCurrentContext();
			if (!context.MovingWindow.IsNull && context.ActiveId == context.MovingWindow.MoveId)
			{
				return false;
			}

			return context.ActiveIdUsingNavDirMask != 0 || context.ActiveIdHasBeenEditedBefore;
		}

		private static int RowAt(ReorderableTreeRowSpan[] spans, Vector2 point, float left, float right)
		{
			if (point.X < left || point.X >= right)
			{
				return -1;
			}

			for (int i = 0; i < spans.Length; i++)
			{
				if (point.Y >= spans[i].Top && point.Y < spans[i].Bottom)
				{
					return i;
				}
			}

			return -1;
		}

		private static void DrawDrop(ReorderableTreeDrop drop, ReorderableTreeRowSpan[] spans, float left, float right, float indent)
		{
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			uint color = ImGui.GetColorU32(ImGuiCol.DragDropTarget);

			if (drop.Kind == ReorderableTreeDropKind.Inside)
			{
				ReorderableTreeRowSpan span = spans[drop.AnchorRow];
				drawList.AddRect(new Vector2(left, span.Top), new Vector2(right, span.Bottom), color, 0f, ImDrawFlags.None, 2f);
				return;
			}

			float x = left + (drop.LineDepth * indent);
			drawList.AddLine(new Vector2(x, drop.LineY), new Vector2(right, drop.LineY), color, 2f);
			drawList.AddCircleFilled(new Vector2(x, drop.LineY), 3f, color);
		}
	}
}
