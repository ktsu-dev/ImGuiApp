// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.ImGui.Probes;
using ktsu.Semantics.Color;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>Draws a wrapping grid of colour swatches; click selects, drag reorders the list in place.</summary>
	/// <param name="label">Unique id; also the probe name of the grid, and the prefix of each swatch's probe name.</param>
	/// <param name="swatches">The colours, in display order. Reordered in place by a drag. Owned by the caller.</param>
	/// <param name="selectedIndex">Index of the selected swatch, or -1 for none. Follows the selected swatch when the list reorders. Owned by the caller.</param>
	/// <param name="swatchSize">Side of one square swatch in pixels. Default 20, clamped to [8, 128].</param>
	/// <param name="columns">Swatches per row; 0 (default) fits as many as the available width holds, at least 1.</param>
	/// <returns>True if the order of <paramref name="swatches"/> or <paramref name="selectedIndex"/> changed this frame.</returns>
	/// <remarks>
	/// The whole grid is one invisible button drawn over with the draw list, so a drag is one
	/// gesture the widget owns rather than an ImGui drag-and-drop payload, and the list can reorder
	/// under the pointer without any per-swatch identity going stale. The widget never adds, removes
	/// or edits colours: the host does that from <paramref name="selectedIndex"/>. A drag reorders
	/// the list with <see cref="IList{T}.RemoveAt"/> and <see cref="IList{T}.Insert"/>, so a
	/// fixed-size list such as an array throws <see cref="NotSupportedException"/> when a
	/// drag lands; pass a <see cref="List{T}"/>.
	/// </remarks>
	public static bool SwatchPalette(string label, IList<Color> swatches, ref int selectedIndex, float swatchSize = 20f, int columns = 0) =>
		SwatchPaletteImpl.Draw(label, swatches, ref selectedIndex, swatchSize, columns);

	internal static class SwatchPaletteImpl
	{
		private const float DraggedSourceAlpha = 0.35f;
		private const float DragGhostAlpha = 0.7f;

		private static readonly Dictionary<uint, SwatchPaletteState> States = [];

		public static bool Draw(string label, IList<Color> swatches, ref int selectedIndex, float swatchSize, int columns)
		{
			Ensure.NotNull(label);
			Ensure.NotNull(swatches);

			uint id = ImGui.GetID(label);
			if (!States.TryGetValue(id, out SwatchPaletteState? state))
			{
				state = new SwatchPaletteState();
				States[id] = state;
			}

			int count = swatches.Count;
			float s = SwatchPaletteState.ClampSwatchSize(swatchSize);
			int cols = SwatchPaletteState.ColumnsFor(columns < 0 ? 0 : columns, ImGui.GetContentRegionAvail().X, s);
			Vector2 size = SwatchPaletteState.GridSize(count, cols, s);
			Vector2 origin = ImGui.GetCursorScreenPos();

			ImGui.InvisibleButton(label, size);
			ImGuiProbes.MarkItem(label);

			bool changed = false;
			if (selectedIndex < -1 || selectedIndex >= count)
			{
				selectedIndex = -1;
				changed = true;
			}

			Vector2 local = ImGui.GetIO().MousePos - origin;
			bool hovered = ImGui.IsItemHovered();

			if (ImGui.IsItemActivated())
			{
				state.Press(SwatchPaletteState.IndexAt(local, count, cols, s), local);
			}

			if (ImGui.IsItemActive())
			{
				state.Motion(local, ImGui.GetIO().MouseDragThreshold);
			}

			// Captured before the release clears it, so the frame that drops still draws the drag.
			bool dragging = state.Dragging;
			int dragSource = state.DragSource;

			if (ImGui.IsItemDeactivated())
			{
				changed |= Release(state, swatches, ref selectedIndex, local, cols, s);
				count = swatches.Count;
				dragging = false;
			}

			int hoveredIndex = hovered ? SwatchPaletteState.IndexAt(local, count, cols, s) : -1;
			UpdatePointer(swatches, hoveredIndex, dragging);

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			if (count == 0)
			{
				drawList.AddRect(origin, origin + size, ImGui.GetColorU32(ImGuiCol.Border));
				return changed;
			}

			DrawSwatches(drawList, label, swatches, selectedIndex, origin, cols, s, dragging ? dragSource : -1);

			if (dragging && dragSource >= 0 && dragSource < count)
			{
				int slot = SwatchPaletteState.DropSlot(local, count, cols, s);
				DrawInsertionMarker(drawList, origin, local, slot, count, cols, s);
				DrawGhost(swatches[dragSource], ImGui.GetIO().MousePos, s);
			}

			return changed;
		}

		private static bool Release(SwatchPaletteState state, IList<Color> swatches, ref int selectedIndex, Vector2 local, int cols, float s)
		{
			bool changed = false;
			if (state.Dragging)
			{
				int count = swatches.Count;
				int slot = SwatchPaletteState.DropSlot(local, count, cols, s);
				int source = state.DragSource;
				int previousSelection = selectedIndex;
				int newSelection = SwatchPaletteState.Move(swatches, source, slot, selectedIndex);
				bool moved = source >= 0 && source < count && slot != source && slot != source + 1;
				selectedIndex = newSelection;
				changed = moved || newSelection != previousSelection;
			}
			else if (state.DragSource >= 0 && state.DragSource < swatches.Count)
			{
				changed = selectedIndex != state.DragSource;
				selectedIndex = state.DragSource;
			}

			state.Release();
			return changed;
		}

		private static void UpdatePointer(IList<Color> swatches, int hoveredIndex, bool dragging)
		{
			if (dragging)
			{
				ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
				return;
			}

			if (hoveredIndex < 0)
			{
				return;
			}

			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			ImGui.BeginTooltip();
			ImGui.TextUnformatted(swatches[hoveredIndex].ToHex());
			ImGui.EndTooltip();
		}

		private static void DrawSwatches(ImDrawListPtr drawList, string label, IList<Color> swatches, int selectedIndex, Vector2 origin, int cols, float s, int draggedIndex)
		{
			uint border = ImGui.GetColorU32(ImGuiCol.Border);
			Vector2 extent = new(s, s);

			for (int i = 0; i < swatches.Count; i++)
			{
				Vector2 min = origin + SwatchPaletteState.CellMin(i, cols, s);
				Vector2 max = min + extent;

				Color colour = swatches[i];
				if (i == draggedIndex)
				{
					colour = colour.WithAlpha(colour.A * DraggedSourceAlpha);
				}

				DrawCheckerboard(drawList, min, extent);
				drawList.AddRectFilled(min, max, colour.ToImGuiU32());
				drawList.AddRect(min, max, border);

				if (i == selectedIndex)
				{
					drawList.AddRect(min - Vector2.One, max + Vector2.One, ImGui.GetColorU32(ImGuiCol.SliderGrabActive), 0f, ImDrawFlags.None, 2f);
				}

				ImGuiProbes.MarkRegion($"{label}/{i}", min, max);
			}
		}

		private static void DrawInsertionMarker(ImDrawListPtr drawList, Vector2 origin, Vector2 local, int slot, int count, int cols, float s)
		{
			// A slot at a row's end sits right of that row's last cell rather than left of the next
			// row's first, so the marker stays on the row the pointer is over. The row is worked out
			// the way DropSlot works it out, since the slot alone cannot tell the two apart.
			int lastRow = (count - 1) / cols;
			float rowPosition = MathF.Floor(local.Y / (s + SwatchPaletteState.Gap));
			int row = float.IsNaN(rowPosition) ? 0 : (int)Math.Clamp(rowPosition, 0f, lastRow);
			bool rowEnd = slot > row * cols && slot == Math.Min(count, (row + 1) * cols);
			float x;
			float y;
			if (rowEnd)
			{
				Vector2 last = SwatchPaletteState.CellMin(slot - 1, cols, s);
				x = last.X + s + (SwatchPaletteState.Gap / 2f);
				y = last.Y;
			}
			else
			{
				Vector2 cell = SwatchPaletteState.CellMin(slot, cols, s);
				x = cell.X - (SwatchPaletteState.Gap / 2f);
				y = cell.Y;
			}

			Vector2 top = origin + new Vector2(x, y);
			drawList.AddLine(top, top + new Vector2(0f, s), ImGui.GetColorU32(ImGuiCol.DragDropTarget), 2f);
		}

		private static void DrawGhost(Color colour, Vector2 pointer, float s)
		{
			ImDrawListPtr foreground = ImGui.GetForegroundDrawList();
			Vector2 half = new(s / 2f, s / 2f);
			Vector2 min = pointer - half;
			Vector2 max = pointer + half;
			foreground.AddRectFilled(min, max, colour.WithAlpha(colour.A * DragGhostAlpha).ToImGuiU32());
			foreground.AddRect(min, max, ImGui.GetColorU32(ImGuiCol.Border));
		}
	}
}
