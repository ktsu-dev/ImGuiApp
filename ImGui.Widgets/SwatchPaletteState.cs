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
	/// <summary>
	/// The layout and interaction behind <see cref="SwatchPalette"/>: where each swatch sits, which
	/// one a point is over, where a drag would drop, the move itself, and the drag in progress.
	/// </summary>
	/// <remarks>
	/// Deliberately free of ImGui, so every rule is tested without a context. Positions are relative
	/// to the grid's top-left corner. The widget keeps one of these per ID for the drag alone; the
	/// list and the selection belong to the caller.
	/// </remarks>
	internal sealed class SwatchPaletteState
	{
		/// <summary>Space between neighbouring swatches, in pixels.</summary>
		public const float Gap = 4f;

		/// <summary>The smallest swatch side the widget draws.</summary>
		public const float MinSwatchSize = 8f;

		/// <summary>The largest swatch side the widget draws.</summary>
		public const float MaxSwatchSize = 128f;

		private const float DefaultSwatchSize = 20f;

		private Vector2 pressPoint;

		/// <summary>Gets the index of the swatch pressed, or -1 when the press was not on one.</summary>
		public int DragSource { get; private set; } = -1;

		/// <summary>Gets a value indicating whether the press has moved far enough to be a drag.</summary>
		public bool Dragging { get; private set; }

		/// <summary>Clamps a swatch side to [<see cref="MinSwatchSize"/>, <see cref="MaxSwatchSize"/>].</summary>
		/// <param name="size">The requested side.</param>
		/// <returns>The side to draw; 20 for NaN.</returns>
		public static float ClampSwatchSize(float size) =>
			float.IsNaN(size) ? DefaultSwatchSize : Math.Clamp(size, MinSwatchSize, MaxSwatchSize);

		/// <summary>Works out how many swatches go in a row.</summary>
		/// <param name="requested">The caller's column count; zero or less fits the width.</param>
		/// <param name="availableWidth">The width available to the grid.</param>
		/// <param name="swatchSize">The swatch side.</param>
		/// <returns>The column count, at least 1.</returns>
		public static int ColumnsFor(int requested, float availableWidth, float swatchSize)
		{
			if (requested > 0)
			{
				return requested;
			}

			if (float.IsNaN(availableWidth) || availableWidth <= 0f)
			{
				return 1;
			}

			return Math.Max(1, (int)MathF.Floor((availableWidth + Gap) / (swatchSize + Gap)));
		}

		/// <summary>Measures the grid.</summary>
		/// <param name="count">The number of swatches.</param>
		/// <param name="columns">Swatches per row.</param>
		/// <param name="swatchSize">The swatch side.</param>
		/// <returns>The grid's size; one swatch's worth when the list is empty.</returns>
		public static Vector2 GridSize(int count, int columns, float swatchSize)
		{
			if (count <= 0)
			{
				return new Vector2(swatchSize, swatchSize);
			}

			int rows = (count + columns - 1) / columns;
			int cols = Math.Min(count, columns);
			return new Vector2(
				(cols * swatchSize) + ((cols - 1) * Gap),
				(rows * swatchSize) + ((rows - 1) * Gap));
		}

		/// <summary>Finds the top-left corner of a swatch's cell.</summary>
		/// <param name="index">The swatch.</param>
		/// <param name="columns">Swatches per row.</param>
		/// <param name="swatchSize">The swatch side.</param>
		/// <returns>The cell's top-left corner.</returns>
		public static Vector2 CellMin(int index, int columns, float swatchSize)
		{
			float pitch = swatchSize + Gap;
			return new Vector2(index % columns * pitch, index / columns * pitch);
		}

		/// <summary>Finds the swatch under a point.</summary>
		/// <param name="point">The point.</param>
		/// <param name="count">The number of swatches.</param>
		/// <param name="columns">Swatches per row.</param>
		/// <param name="swatchSize">The swatch side.</param>
		/// <returns>The swatch's index, or -1 for a gap, a point outside the grid, or a cell past the last swatch.</returns>
		public static int IndexAt(Vector2 point, int count, int columns, float swatchSize)
		{
			if (point.X < 0f || point.Y < 0f || float.IsNaN(point.X) || float.IsNaN(point.Y))
			{
				return -1;
			}

			float pitch = swatchSize + Gap;
			int col = (int)MathF.Floor(point.X / pitch);
			int row = (int)MathF.Floor(point.Y / pitch);
			if (col >= columns)
			{
				return -1;
			}

			int index = (row * columns) + col;
			if (index >= count)
			{
				return -1;
			}

			Vector2 min = CellMin(index, columns, swatchSize);
			bool inside = point.X < min.X + swatchSize && point.Y < min.Y + swatchSize;
			return inside ? index : -1;
		}

		/// <summary>Finds where a swatch dropped at a point would be inserted.</summary>
		/// <param name="point">The point.</param>
		/// <param name="count">The number of swatches.</param>
		/// <param name="columns">Swatches per row.</param>
		/// <param name="swatchSize">The swatch side.</param>
		/// <returns>
		/// A slot in [0, <paramref name="count"/>]: before the swatch at that index, or after the last
		/// for <paramref name="count"/>. A point outside the grid snaps to the nearest row and column.
		/// </returns>
		public static int DropSlot(Vector2 point, int count, int columns, float swatchSize)
		{
			if (count <= 0)
			{
				return 0;
			}

			float pitch = swatchSize + Gap;
			int lastRow = (count - 1) / columns;
			int row = Math.Clamp(FloorOrZero(point.Y / pitch), 0, lastRow);
			int col = Math.Clamp(FloorOrZero(point.X / pitch), 0, columns - 1);

			int slot = (row * columns) + col;
			float centre = (col * pitch) + (swatchSize / 2f);
			if (point.X > centre)
			{
				slot++;
			}

			slot = Math.Clamp(slot, row * columns, Math.Min(count, (row + 1) * columns));
			return Math.Clamp(slot, 0, count);
		}

		/// <summary>Moves an item to an insertion slot, and works out where the selection went.</summary>
		/// <typeparam name="T">The item type.</typeparam>
		/// <param name="list">The list, changed in place.</param>
		/// <param name="from">The index of the item to move.</param>
		/// <param name="slot">The insertion slot, as <see cref="DropSlot"/> returns it.</param>
		/// <param name="selectedIndex">The selection before the move.</param>
		/// <returns>The selection after the move, following the item it named.</returns>
		public static int Move<T>(IList<T> list, int from, int slot, int selectedIndex)
		{
			Ensure.NotNull(list);

			if (from < 0 || from >= list.Count || slot == from || slot == from + 1)
			{
				return selectedIndex;
			}

			int target = slot > from ? slot - 1 : slot;
			T item = list[from];
			list.RemoveAt(from);
			list.Insert(target, item);

			if (selectedIndex == from)
			{
				return target;
			}

			if (from < selectedIndex && selectedIndex <= target)
			{
				return selectedIndex - 1;
			}

			if (target <= selectedIndex && selectedIndex < from)
			{
				return selectedIndex + 1;
			}

			return selectedIndex;
		}

		/// <summary>Records a press.</summary>
		/// <param name="index">The swatch pressed, or -1.</param>
		/// <param name="point">Where the press landed.</param>
		public void Press(int index, Vector2 point)
		{
			DragSource = index < 0 ? -1 : index;
			pressPoint = point;
			Dragging = false;
		}

		/// <summary>Follows the pointer while the press is held, and starts a drag once it has moved far enough.</summary>
		/// <param name="point">Where the pointer is now.</param>
		/// <param name="threshold">How far it must move from the press before the press is a drag.</param>
		public void Motion(Vector2 point, float threshold)
		{
			if (!Dragging && DragSource >= 0 && Vector2.Distance(point, pressPoint) > threshold)
			{
				Dragging = true;
			}
		}

		/// <summary>Ends the press.</summary>
		public void Release()
		{
			DragSource = -1;
			Dragging = false;
		}

		/// <summary>How far either way a floored index is clamped, so the cast back to <see cref="int"/> cannot overflow.</summary>
		private const float IndexLimit = 1 << 30;

		private static int FloorOrZero(float value) => float.IsNaN(value) ? 0 : (int)Math.Clamp(MathF.Floor(value), -IndexLimit, IndexLimit);
	}
}
