// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiWidgets;

using System.Drawing;
using System.Numerics;

using Hexa.NET.ImGui;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Gets or sets a value indicating whether to enable grid debug drawing.
	/// </summary>
	public static bool EnableGridDebugDraw { get; set; }

	/// <summary>
	/// Specifies the order in which grid items are displayed.
	/// </summary>
	/// <remarks>
	/// <see cref="RowMajor"/> displays items left to right before moving to the next row.
	/// <see cref="ColumnMajor"/> displays items top to bottom before moving to the next column.
	/// </remarks>
	public enum GridOrder
	{
		/// <summary>
		/// Items are displayed in order left to right before dropping to the next row.
		/// Recommended for when displaying grids of icons.
		/// Example:
		/// [ [1] [2] [3] ]
		/// [ [4] [5] [6] ]
		/// OR
		/// [ [1] [2] [3] [4] [5] ]
		/// [ [6]                 ]
		/// </summary>
		RowMajor,
		/// <summary>
		/// Items are displayed top to bottom before moving to the next column.
		/// Recommended when displaying tables of data.
		/// Example:
		/// [ [1] [4] ]
		/// [ [2] [5] ]
		/// [ [3] [6] ]
		/// OR
		/// [ [1] [5] ]
		/// [ [2] [6] ]
		/// [ [3]     ]
		/// [ [4]     ]
		/// </summary>
		ColumnMajor,
	}

	/// <summary>
	/// Options for changing how the grid is laid out.
	/// </summary>
	public class GridOptions
	{
		/// <summary>
		/// Size of the grid. Setting any axis to 0 will use the available space.
		/// </summary>
		public Vector2 GridSize { get; set; } = new(0, 0);

		/// <summary>
		/// Size the content region to cover all the items.
		/// </summary>
		public bool FitToContents { get; init; }
	}

	/// <summary>
	/// Delegate to measure the size of a grid cell.
	/// </summary>
	/// <typeparam name="T">The type of the item.</typeparam>
	/// <param name="item">The item to measure.</param>
	/// <returns>The size of the item.</returns>
	public delegate Vector2 MeasureGridCell<T>(T item);

	/// <summary>
	/// Delegate to draw a grid cell.
	/// </summary>
	/// <typeparam name="T">The type of the item.</typeparam>
	/// <param name="item">The item to draw.</param>
	/// <param name="cellSize">The calculated size of the grid cell.</param>
	/// <param name="itemSize">The calculated size of the item.</param>
	public delegate void DrawGridCell<T>(T item, Vector2 cellSize, Vector2 itemSize);

	/// <summary>
	/// Renders a grid with the specified items and delegates.
	/// </summary>
	/// <typeparam name="T">The type of the items.</typeparam>
	/// <param name="id">Id for the grid.</param>
	/// <param name="items">The items to be displayed in the grid.</param>
	/// <param name="measureDelegate">The delegate to measure the size of each item.</param>
	/// <param name="drawDelegate">The delegate to draw each item.</param>
	public static void RowMajorGrid<T>(string id, IEnumerable<T> items, MeasureGridCell<T> measureDelegate, DrawGridCell<T> drawDelegate)
	{
		ArgumentNullException.ThrowIfNull(items);
		ArgumentNullException.ThrowIfNull(measureDelegate);
		ArgumentNullException.ThrowIfNull(drawDelegate);

		GridImpl.ShowRowMajor(id, items, measureDelegate, drawDelegate, new());
	}

	/// <summary>
	/// Renders a grid with the specified items and delegates.
	/// </summary>
	/// <typeparam name="T">The type of the items.</typeparam>
	/// <param name="id">Id for the grid.</param>
	/// <param name="items">The items to be displayed in the grid.</param>
	/// <param name="measureDelegate">The delegate to measure the size of each item.</param>
	/// <param name="drawDelegate">The delegate to draw each item.</param>
	/// <param name="gridOptions">Additional options to modify the grid behaviour</param>
	public static void RowMajorGrid<T>(string id, IEnumerable<T> items, MeasureGridCell<T> measureDelegate, DrawGridCell<T> drawDelegate, GridOptions gridOptions)
	{
		ArgumentNullException.ThrowIfNull(items);
		ArgumentNullException.ThrowIfNull(measureDelegate);
		ArgumentNullException.ThrowIfNull(drawDelegate);
		ArgumentNullException.ThrowIfNull(gridOptions);

		GridImpl.ShowRowMajor(id, items, measureDelegate, drawDelegate, gridOptions);
	}

	/// <summary>
	/// Renders a grid with the specified items and delegates.
	/// </summary>
	/// <typeparam name="T">The type of the items.</typeparam>
	/// <param name="id">Id for the grid.</param>
	/// <param name="items">The items to be displayed in the grid.</param>
	/// <param name="measureDelegate">The delegate to measure the size of each item.</param>
	/// <param name="drawDelegate">The delegate to draw each item.</param>
	public static void ColumnMajorGrid<T>(string id, IEnumerable<T> items, MeasureGridCell<T> measureDelegate, DrawGridCell<T> drawDelegate)
	{
		ArgumentNullException.ThrowIfNull(items);
		ArgumentNullException.ThrowIfNull(measureDelegate);
		ArgumentNullException.ThrowIfNull(drawDelegate);

		GridImpl.ShowColumnMajor(id, items, measureDelegate, drawDelegate, new());
	}

	/// <summary>
	/// Renders a grid with the specified items and delegates.
	/// </summary>
	/// <typeparam name="T">The type of the items.</typeparam>
	/// <param name="id">Id for the grid.</param>
	/// <param name="items">The items to be displayed in the grid.</param>
	/// <param name="measureDelegate">The delegate to measure the size of each item.</param>
	/// <param name="drawDelegate">The delegate to draw each item.</param>
	/// <param name="gridOptions">Additional options to modify the grid behaviour</param>
	public static void ColumnMajorGrid<T>(string id, IEnumerable<T> items, MeasureGridCell<T> measureDelegate, DrawGridCell<T> drawDelegate, GridOptions gridOptions)
	{
		ArgumentNullException.ThrowIfNull(items);
		ArgumentNullException.ThrowIfNull(measureDelegate);
		ArgumentNullException.ThrowIfNull(drawDelegate);
		ArgumentNullException.ThrowIfNull(gridOptions);

		GridImpl.ShowColumnMajor(id, items, measureDelegate, drawDelegate, gridOptions);
	}

	/// <summary>
	/// Contains the implementation details for rendering grids.
	/// </summary>
	internal static class GridImpl
	{
		internal class GridLayout()
		{
			internal Point Dimensions { private get; init; }
			internal float[] ColumnWidths { get; init; } = [];
			internal float[] RowHeights { get; init; } = [];
			internal int ColumnCount => Dimensions.X;
			internal int RowCount => Dimensions.Y;
		}

		#region RowMajor
		private static Point CalculateRowMajorCellLocation(int itemIndex, int columnCount)
		{
			int columnIndex = itemIndex % columnCount;
			int rowIndex = itemIndex / columnCount;
			return new(columnIndex, rowIndex);
		}

		private static GridLayout CalculateRowMajorGridLayout(Vector2[] itemSizes, float containerWidth)
		{
			float widestElementHeight = itemSizes.Max(itemSize => itemSize.X);
			GridLayout currentGridLayout = new()
			{
				Dimensions = new(1, itemSizes.Length),
				ColumnWidths = [widestElementHeight],
				RowHeights = [.. itemSizes.Select(itemSize => itemSize.Y)]
			};

			GridLayout previousGridLayout = currentGridLayout;
			while (currentGridLayout.ColumnCount < itemSizes.Length)
			{
				int newColumnCount = currentGridLayout.ColumnCount + 1;
				int newRowCount = (int)MathF.Ceiling(itemSizes.Length / (float)newColumnCount);
				currentGridLayout = new()
				{
					Dimensions = new(newColumnCount, newRowCount),
					ColumnWidths = new float[newColumnCount],
					RowHeights = new float[newRowCount],
				};

				for (int i = 0; i < itemSizes.Length; i++)
				{
					Vector2 itemSize = itemSizes[i];
					Point cellLocation = CalculateRowMajorCellLocation(i, newColumnCount);

					float maxColumnWidth = currentGridLayout.ColumnWidths[cellLocation.X];
					maxColumnWidth = Math.Max(maxColumnWidth, itemSize.X);
					currentGridLayout.ColumnWidths[cellLocation.X] = maxColumnWidth;

					float maxRowHeight = currentGridLayout.RowHeights[cellLocation.Y];
					maxRowHeight = Math.Max(maxRowHeight, itemSize.Y);
					currentGridLayout.RowHeights[cellLocation.Y] = maxRowHeight;
				}

				if (currentGridLayout.ColumnWidths.Sum() > containerWidth)
				{
					currentGridLayout = previousGridLayout;
					break;
				}

				previousGridLayout = currentGridLayout;
			}

			return currentGridLayout;
		}

		internal static void ShowRowMajor<T>(string id, IEnumerable<T> items, MeasureGridCell<T> measureDelegate, DrawGridCell<T> drawDelegate, GridOptions gridOptions)
		{
			if (gridOptions.GridSize.X <= 0)
			{
				gridOptions.GridSize = new(ImGui.GetContentRegionAvail().X, gridOptions.GridSize.Y);
			}

			if (gridOptions.GridSize.Y <= 0)
			{
				gridOptions.GridSize = new(gridOptions.GridSize.X, ImGui.GetContentRegionAvail().Y);
			}

			Vector2 itemSpacing = ImGui.GetStyle().ItemSpacing;
			T[] itemList = [.. items];
			int itemListCount = itemList.Length;
			Vector2[] itemDimensions = [.. itemList.Select(i => measureDelegate(i))];
			Vector2[] itemDimensionsWithSpacing = [.. itemDimensions.Select(d => d + itemSpacing)];

			if (itemList.Length == 0)
			{
				// No items to display
				if (!gridOptions.FitToContents)
				{
					ImGui.Dummy(gridOptions.GridSize);
				}

				return;
			}

			GridLayout gridLayout = CalculateRowMajorGridLayout(itemDimensionsWithSpacing, gridOptions.GridSize.X);

			if (gridOptions.FitToContents)
			{
				float width = gridLayout.ColumnWidths.Sum();
				float height = gridLayout.RowHeights.Sum();
				gridOptions.GridSize = new(width, height);
			}

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			uint borderColor = ImGui.GetColorU32(ImGui.GetStyle().Colors[(int)ImGuiCol.Border]);
			if (ImGui.BeginChild($"rowMajorGrid_{id}", gridOptions.GridSize, ImGuiChildFlags.None))
			{
				Vector2 gridTopLeft = ImGui.GetCursorScreenPos();
				if (EnableGridDebugDraw)
				{
					drawList.AddRect(gridTopLeft, gridTopLeft + gridOptions.GridSize, borderColor);
				}

				Vector2 rowTopleft = gridTopLeft;
				for (int rowIndex = 0; rowIndex < gridLayout.RowCount; rowIndex++)
				{
					bool isFirstRow = rowIndex == 0;
					float previousRowHeight = isFirstRow ? 0f : gridLayout.RowHeights[rowIndex - 1];

					float columnCursorX = rowTopleft.X;
					float columnCursorY = rowTopleft.Y + previousRowHeight;
					rowTopleft = new(columnCursorX, columnCursorY);
					ImGui.SetCursorScreenPos(rowTopleft);

					Vector2 cellTopLeft = ImGui.GetCursorScreenPos();
					int itemBeginIndex = rowIndex * gridLayout.ColumnCount;
					int itemEndIndex = Math.Min(itemBeginIndex + gridLayout.ColumnCount, itemListCount);
					for (int itemIndex = itemBeginIndex; itemIndex < itemEndIndex; itemIndex++)
					{
						int columnIndex = itemIndex - itemBeginIndex;
						bool isFirstColumn = itemIndex == itemBeginIndex;
						float previousCellWidth = isFirstColumn ? 0f : gridLayout.ColumnWidths[columnIndex - 1];

						float cellCursorX = cellTopLeft.X + previousCellWidth;
						float cellCursorY = cellTopLeft.Y;
						cellTopLeft = new(cellCursorX, cellCursorY);
						ImGui.SetCursorScreenPos(cellTopLeft);

						float cellWidth = gridLayout.ColumnWidths[columnIndex];
						float cellHeight = gridLayout.RowHeights[rowIndex];
						Vector2 cellSize = new(cellWidth, cellHeight);

						if (EnableGridDebugDraw)
						{
							drawList.AddRect(cellTopLeft, cellTopLeft + cellSize, borderColor);
						}

						drawDelegate(itemList[itemIndex], cellSize, itemDimensions[itemIndex]);
					}
				}
			}

			ImGui.EndChild();
		}
		#endregion

		#region ColumnMajor
		private static Point CalculateColumnMajorCellLocation(int itemIndex, int rowCount)
		{
			int columnIndex = itemIndex / rowCount;
			int rowIndex = itemIndex % rowCount;
			return new(columnIndex, rowIndex);
		}

		private static GridLayout CalculateColumnMajorGridLayout(Vector2[] itemSizes, float containerHeight)
		{
			float tallestElementHeight = itemSizes.Max(itemSize => itemSize.Y);
			GridLayout currentGridLayout = new()
			{
				Dimensions = new(itemSizes.Length, 1),
				ColumnWidths = [.. itemSizes.Select(itemSize => itemSize.X)],
				RowHeights = [tallestElementHeight],
			};

			GridLayout previousGridLayout = currentGridLayout;
			while (currentGridLayout.RowCount < itemSizes.Length)
			{
				int newRowCount = currentGridLayout.RowCount + 1;
				int newColumnCount = (int)MathF.Ceiling(itemSizes.Length / (float)newRowCount);
				currentGridLayout = new()
				{
					Dimensions = new(newColumnCount, newRowCount),
					ColumnWidths = new float[newColumnCount],
					RowHeights = new float[newRowCount],
				};

				for (int i = 0; i < itemSizes.Length; i++)
				{
					Vector2 itemSize = itemSizes[i];
					Point cellLocation = CalculateColumnMajorCellLocation(i, newRowCount);

					float maxColumnWidth = currentGridLayout.ColumnWidths[cellLocation.X];
					maxColumnWidth = Math.Max(maxColumnWidth, itemSize.X);
					currentGridLayout.ColumnWidths[cellLocation.X] = maxColumnWidth;

					float maxRowHeight = currentGridLayout.RowHeights[cellLocation.Y];
					maxRowHeight = Math.Max(maxRowHeight, itemSize.Y);
					currentGridLayout.RowHeights[cellLocation.Y] = maxRowHeight;
				}

				if (currentGridLayout.RowHeights.Sum() > containerHeight)
				{
					currentGridLayout = previousGridLayout;
					break;
				}

				previousGridLayout = currentGridLayout;
			}

			return currentGridLayout;
		}

		internal static void ShowColumnMajor<T>(string id, IEnumerable<T> items, MeasureGridCell<T> measureDelegate, DrawGridCell<T> drawDelegate, GridOptions gridOptions)
		{
			if (gridOptions.GridSize.X <= 0)
			{
				gridOptions.GridSize = new(ImGui.GetContentRegionAvail().X, gridOptions.GridSize.Y);
			}

			if (gridOptions.GridSize.Y <= 0)
			{
				gridOptions.GridSize = new(gridOptions.GridSize.X, ImGui.GetContentRegionAvail().Y);
			}

			Vector2 itemSpacing = ImGui.GetStyle().ItemSpacing;
			T[] itemList = [.. items];
			int itemListCount = itemList.Length;
			Vector2[] itemDimensions = [.. itemList.Select(i => measureDelegate(i))];
			Vector2[] itemDimensionsWithSpacing = [.. itemDimensions.Select(d => d + itemSpacing)];

			if (itemList.Length == 0)
			{
				// No items to display
				if (!gridOptions.FitToContents)
				{
					ImGui.Dummy(gridOptions.GridSize);
				}

				return;
			}

			GridLayout gridLayout = CalculateColumnMajorGridLayout(itemDimensionsWithSpacing, gridOptions.GridSize.Y);

			if (gridOptions.FitToContents)
			{
				float width = gridLayout.ColumnWidths.Sum();
				float height = gridLayout.RowHeights.Sum();
				gridOptions.GridSize = new(width, height);
			}

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			uint borderColor = ImGui.GetColorU32(ImGui.GetStyle().Colors[(int)ImGuiCol.Border]);
			if (ImGui.BeginChild($"columnMajorGrid_{id}", gridOptions.GridSize, ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar))
			{
				Vector2 gridTopLeft = ImGui.GetCursorScreenPos();
				if (EnableGridDebugDraw)
				{
					drawList.AddRect(gridTopLeft, gridTopLeft + gridOptions.GridSize, borderColor);
				}

				Vector2 columnTopLeft = gridTopLeft;
				for (int columnIndex = 0; columnIndex < gridLayout.ColumnCount; columnIndex++)
				{
					bool isFirstColumn = columnIndex == 0;
					float previousColumnWidth = isFirstColumn ? 0f : gridLayout.ColumnWidths[columnIndex - 1];

					float columnCursorX = columnTopLeft.X + previousColumnWidth;
					float columnCursorY = columnTopLeft.Y;
					columnTopLeft = new(columnCursorX, columnCursorY);
					ImGui.SetCursorScreenPos(columnTopLeft);

					Vector2 cellTopLeft = ImGui.GetCursorScreenPos();
					int itemBeginIndex = columnIndex * gridLayout.RowCount;
					int itemEndIndex = Math.Min(itemBeginIndex + gridLayout.RowCount, itemListCount);
					for (int itemIndex = itemBeginIndex; itemIndex < itemEndIndex; itemIndex++)
					{
						int rowIndex = itemIndex - itemBeginIndex;
						bool isFirstRow = itemIndex == itemBeginIndex;
						float previousCellHeight = isFirstRow ? 0f : itemDimensionsWithSpacing[rowIndex - 1].Y;

						float cellCursorX = cellTopLeft.X;
						float cellCursorY = cellTopLeft.Y + previousCellHeight;
						cellTopLeft = new(cellCursorX, cellCursorY);
						ImGui.SetCursorScreenPos(cellTopLeft);

						float cellWidth = gridLayout.ColumnWidths[columnIndex];
						float cellHeight = gridLayout.RowHeights[rowIndex];
						Vector2 cellSize = new(cellWidth, cellHeight);

						if (EnableGridDebugDraw)
						{
							drawList.AddRect(cellTopLeft, cellTopLeft + cellSize, borderColor);
						}

						drawDelegate(itemList[itemIndex], cellSize, itemDimensions[itemIndex]);
					}
				}
			}

			ImGui.EndChild();
		}
		#endregion
	}
}
