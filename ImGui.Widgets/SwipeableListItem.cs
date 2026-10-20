// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.ImGui.Probes;
using ktsu.ImGui.Styler;
using ktsu.ImGui.Widgets.Gestures;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a list row that slides aside when dragged horizontally to reveal action buttons behind it,
	/// the way a mail or messages list does.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Dragging the row right reveals <paramref name="leading"/>, and dragging it left reveals
	/// <paramref name="trailing"/>. On release the row snaps open when more than half of a side's
	/// actions show, or when it was flicked open, and snaps closed otherwise. Dragging most of the way
	/// across commits that side's outermost action at once, unless
	/// <see cref="SwipeableListItemOptions.FullSwipe"/> is off. A tap on an open row closes it, and
	/// opening one row closes any other that was open.
	/// </para>
	/// <para>
	/// Both action lists run from the outer edge inward, so index 0 is the leftmost leading action and
	/// the rightmost trailing one. A vertical drag is left alone, so a list of these rows still scrolls.
	/// The row is probe-marked under <paramref name="id"/>, and each action under
	/// <c>id/Label</c> while it shows.
	/// </para>
	/// </remarks>
	/// <param name="id">A unique id for the row.</param>
	/// <param name="height">The row's height in pixels. The row takes the full available width.</param>
	/// <param name="drawContent">Draws the row's content, given its size. The cursor starts at the row's top left corner.</param>
	/// <param name="leading">The actions dragging right reveals. May be empty.</param>
	/// <param name="trailing">The actions dragging left reveals. May be empty.</param>
	/// <param name="options">How the row behaves, or null for the defaults.</param>
	/// <returns>The action the user chose this frame, or null.</returns>
	public static SwipeActionResult? SwipeableListItem(
		string id,
		float height,
		Action<Vector2> drawContent,
		IReadOnlyList<SwipeAction> leading,
		IReadOnlyList<SwipeAction> trailing,
		SwipeableListItemOptions? options = null) =>
		SwipeableListItemImpl.Draw(id, height, drawContent, leading, trailing, options ?? new SwipeableListItemOptions());

	internal static class SwipeableListItemImpl
	{
		private sealed class RowState
		{
			public SwipeableListItemState Swipe { get; } = new();

			public GestureMachine Gestures { get; } = new();

			public bool HorizontalPan { get; set; }

			public int LastFrame { get; set; } = int.MinValue;
		}

		private static readonly Dictionary<uint, RowState> Rows = [];

		// The row that was last opened, so opening another closes it, as only one row is ever open.
		private static uint openRow;

		internal static SwipeActionResult? Draw(
			string id,
			float height,
			Action<Vector2> drawContent,
			IReadOnlyList<SwipeAction> leading,
			IReadOnlyList<SwipeAction> trailing,
			SwipeableListItemOptions options)
		{
			Ensure.NotNull(id);
			Ensure.NotNull(drawContent);
			Ensure.NotNull(leading);
			Ensure.NotNull(trailing);

			uint key = ImGui.GetID(id);
			int frame = ImGui.GetFrameCount();

			// A row that was not drawn last frame starts closed: it was scrolled away or hidden, or the
			// state belongs to an earlier ImGui context whose frame count has started again.
			if (!Rows.TryGetValue(key, out RowState? row) || frame - row.LastFrame != 1)
			{
				row = new RowState();
				Rows[key] = row;
			}

			row.LastFrame = frame;

			Vector2 origin = ImGui.GetCursorScreenPos();
			Vector2 size = new(MathF.Max(ImGui.GetContentRegionAvail().X, 1f), height);
			float[] leadingWidths = ActionWidths(leading, options);
			float[] trailingWidths = ActionWidths(trailing, options);
			SwipeLayout layout = new(Sum(leadingWidths), Sum(trailingWidths), size.X, options.FullSwipe, options.FullSwipeFraction);

			if (openRow != key && row.Swipe.IsOpen && !row.Swipe.IsDragging)
			{
				row.Swipe.Close();
			}

			row.Swipe.Update(ImGui.GetIO().DeltaTime);
			float offset = row.Swipe.Offset;

			ImGui.PushID(id);
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();

			// Through ImGui rather than the draw list, so hovering is clipped to the row as well as drawing.
			ImGui.PushClipRect(origin, origin + size, true);

			SwipeActionResult? result = null;
			if (offset > 0f)
			{
				result = DrawActions(id, SwipeEdge.Leading, leading, leadingWidths, origin, size, offset, row);
			}
			else if (offset < 0f)
			{
				result = DrawActions(id, SwipeEdge.Trailing, trailing, trailingWidths, origin, size, offset, row);
			}

			// The row's own surface, so it covers the actions it slides over.
			Vector2 rowMin = origin + new Vector2(offset, 0f);
			drawList.AddRectFilled(rowMin, rowMin + size, ImGui.GetColorU32(ImGuiCol.WindowBg));

			ImGui.SetCursorScreenPos(rowMin);
			ImGui.SetNextItemAllowOverlap();
			ImGui.InvisibleButton("##row", size);
			ImGuiProbes.MarkItem(id);
			bool activated = ImGui.IsItemActivated();
			GestureResult gesture = row.Gestures.Update(ImGui.IsItemActive(), ImGui.GetMousePos(), ImGui.GetIO().DeltaTime);

			result ??= HandleGesture(key, row, gesture, activated, layout);

			ImGui.SetCursorScreenPos(rowMin);
			ImGui.BeginGroup();
			drawContent(size);
			ImGui.EndGroup();

			ImGui.PopClipRect();
			ImGui.PopID();

			ImGui.SetCursorScreenPos(origin);
			ImGui.Dummy(size);
			return result;
		}

		private static SwipeActionResult? HandleGesture(uint key, RowState row, GestureResult gesture, bool activated, SwipeLayout layout)
		{
			if (activated)
			{
				row.Swipe.BeginDrag();
				row.HorizontalPan = false;
			}

			if ((gesture.Gestures & GestureFlags.PanStart) != 0)
			{
				row.HorizontalPan = MathF.Abs(gesture.Delta.X) >= MathF.Abs(gesture.Delta.Y);
				if (row.HorizontalPan)
				{
					openRow = key;
				}
			}

			if (row.HorizontalPan && (gesture.Gestures & GestureFlags.Pan) != 0)
			{
				row.Swipe.Drag(gesture.Delta.X, layout);
			}

			if ((gesture.Gestures & GestureFlags.PanEnd) != 0)
			{
				// A vertical pan never moved the row, so it settles wherever it already was.
				SwipeEdge? committed = row.Swipe.Release(row.HorizontalPan ? gesture.Velocity.X : 0f, layout);
				row.HorizontalPan = false;
				if (committed is SwipeEdge edge)
				{
					return new SwipeActionResult(edge, 0);
				}
			}
			else if (!gesture.IsPressed && row.Swipe.IsDragging)
			{
				// A press that ended without becoming a pan: a tap closes an open row, and anything else
				// leaves it where it was.
				if (row.Swipe.Offset != 0f)
				{
					row.Swipe.Close();
				}
				else
				{
					_ = row.Swipe.Release(0f, layout);
				}
			}

			return null;
		}

		private static SwipeActionResult? DrawActions(
			string id,
			SwipeEdge edge,
			IReadOnlyList<SwipeAction> actions,
			float[] widths,
			Vector2 origin,
			Vector2 size,
			float offset,
			RowState row)
		{
			if (actions.Count == 0)
			{
				return null;
			}

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			float reach = MathF.Abs(offset);
			float total = Sum(widths);

			// Past its natural width the outermost action stretches to fill what the drag uncovered,
			// which is what tells the user a full swipe is about to commit it.
			float stretch = MathF.Max(0f, reach - total);
			float x = 0f;
			SwipeActionResult? chosen = null;

			for (int i = 0; i < actions.Count; i++)
			{
				float width = widths[i] + (i == 0 ? stretch : 0f);
				float left = edge == SwipeEdge.Leading ? origin.X + x : origin.X + size.X - x - width;
				Vector2 min = new(left, origin.Y);
				Vector2 max = new(left + width, origin.Y + size.Y);
				x += width;

				ImColor background = ActionColor(actions[i], i);
				drawList.AddRectFilled(min, max, background.ToImGuiU32());

				string label = actions[i].Label;
				Vector2 textSize = ImGui.CalcTextSize(label);
				Vector2 textPos = new(min.X + ((width - textSize.X) * 0.5f), min.Y + ((size.Y - textSize.Y) * 0.5f));
				drawList.AddText(textPos, background.MostReadableTextColor().ToImGuiU32(), label);

				ImGui.SetCursorScreenPos(min);
				bool clicked = ImGui.InvisibleButton($"{label}##{edge}{i}", max - min);
				ImGuiProbes.MarkItem($"{id}/{label}");
				WidgetCursor.OnLastItem(ImGuiMouseCursor.Hand);
				if (clicked && !row.Swipe.IsDragging)
				{
					chosen = new SwipeActionResult(edge, i);
					row.Swipe.Close();
				}
			}

			return chosen;
		}

		private static ImColor ActionColor(SwipeAction action, int index)
		{
			if (action.Destructive)
			{
				return Palette.Semantic.Error;
			}

			// Neighbouring actions alternate between the theme's two button shades so they read as
			// separate buttons without a gap between them.
			return new ImColor { Value = ImGui.GetStyle().Colors[(int)(index % 2 == 0 ? ImGuiCol.ButtonActive : ImGuiCol.Button)] };
		}

		private static float[] ActionWidths(IReadOnlyList<SwipeAction> actions, SwipeableListItemOptions options)
		{
			float[] widths = new float[actions.Count];
			float minimum = InterfaceScale.Pixels(options.ActionWidth);
			float padding = ImGui.GetStyle().FramePadding.X * 2f;
			for (int i = 0; i < actions.Count; i++)
			{
				widths[i] = MathF.Max(minimum, ImGui.CalcTextSize(actions[i].Label).X + padding);
			}

			return widths;
		}

		private static float Sum(float[] values)
		{
			float total = 0f;
			foreach (float value in values)
			{
				total += value;
			}

			return total;
		}
	}
}
