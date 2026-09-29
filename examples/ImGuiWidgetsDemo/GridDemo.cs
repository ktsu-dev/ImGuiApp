// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using Hexa.NET.ImGui;
using ktsu.ImGui.App;
using ktsu.ImGui.Styler;
using ktsu.ImGui.Widgets;

/// <summary>
/// The row-major and column-major grids.
/// </summary>
internal static class GridDemo
{
	/// <summary>Gets the number of grid items currently shown.</summary>
	internal static int GridItemCount => GridItemsToShow;

	/// <summary>Gets the generated strings the grid shows, which the search box section searches too.</summary>
	internal static IReadOnlyList<string> Strings => GridStrings;

	private static List<string> GridStrings { get; } = [];
	private static int InitialGridItemCount { get; } = 32;
	private static int GridItemsToShow { get; set; } = InitialGridItemCount;
	private static float GridHeight { get; set; } = 500f;
	private static ImGuiWidgets.GridOrder GridOrder { get; set; } = ImGuiWidgets.GridOrder.RowMajor;
	private static ImGuiWidgets.IconAlignment GridIconAlignment { get; set; } = ImGuiWidgets.IconAlignment.Vertical;
	private static bool GridIconSizeBig { get; set; } = true;
	private static bool GridFitToContents { get; set; }

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		GridItemsToShow = InitialGridItemCount;
		GridHeight = 500f;
		GridOrder = ImGuiWidgets.GridOrder.RowMajor;
		GridIconAlignment = ImGuiWidgets.IconAlignment.Vertical;
		GridIconSizeBig = true;
		GridFitToContents = false;
	}

#pragma warning disable CA5394 //Do not use insecure randomness - Random is used only for generating visual demo data; no security or cryptographic use.
	/// <summary>Generates the strings the grid shows.</summary>
	[SuppressMessage("Security Hotspot", "S2245:Make sure that using this pseudorandom number generator is safe here", Justification = "Random is used only for generating visual demo data; no security or cryptographic use.")]
	internal static void Initialize()
	{
		Random random = new();
		StringBuilder randomStringBuilder = new();
		for (int i = 0; i < InitialGridItemCount; i++)
		{
			randomStringBuilder.Clear();
			randomStringBuilder.Append(i);
			randomStringBuilder.Append(':');

			int lineCount = 1 + (i % 5);
			for (int j = 0; j < lineCount; j++)
			{
				int randomAmount = random.Next(2, 32);
				for (int k = 0; k < randomAmount; k++)
				{
					randomStringBuilder.Append((char)random.Next(32, 127));
				}

				if (j != lineCount - 1)
				{
					randomStringBuilder.Append('\n');
				}
			}

			GridStrings.Add(randomStringBuilder.ToString());
		}
	}
#pragma warning restore CA5394 //Do not use insecure randomness

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Grid Layout"))
		{
			ImGuiStylePtr style = ImGui.GetStyle();
			Vector2 itemSpacing = style.ItemSpacing;
			Vector2 framePadding = style.FramePadding;

			ImGui.TextUnformatted("Flexible grid layouts with automatic sizing:");
			ImGui.Separator();

			// Grid settings - inline controls
			ImGui.TextUnformatted("Grid Configuration:");

			bool showGridDebug = ImGuiWidgets.EnableGridDebugDraw;
			if (DemoProbe.Checkbox("Show Grid Debug Draw", ref showGridDebug))
			{
				ImGuiWidgets.EnableGridDebugDraw = showGridDebug;
			}
			ImGui.SameLine();

			bool showIconDebug = ImGuiWidgets.EnableIconDebugDraw;
			if (DemoProbe.Checkbox("Show Icon Debug Draw", ref showIconDebug))
			{
				ImGuiWidgets.EnableIconDebugDraw = showIconDebug;
			}

			ImGui.Columns(3, "GridSettings");

			bool gridIconSizeBig = GridIconSizeBig;
			if (DemoProbe.Checkbox("Big Icons", ref gridIconSizeBig))
			{
				GridIconSizeBig = gridIconSizeBig;
			}

			bool gridFitToContents = GridFitToContents;
			if (DemoProbe.Checkbox("Fit to Contents", ref gridFitToContents))
			{
				GridFitToContents = gridFitToContents;
			}

			ImGui.NextColumn();

			int gridItemsToShow = GridItemsToShow;
			if (DemoProbe.SliderInt("Items", ref gridItemsToShow, 0, GridStrings.Count))
			{
				GridItemsToShow = gridItemsToShow;
			}

			ImGuiWidgets.GridOrder gridOrder = GridOrder;
			if (ImGuiWidgets.Combo("Order", ref gridOrder))
			{
				GridOrder = gridOrder;
			}

			ImGui.NextColumn();

			ImGuiWidgets.IconAlignment gridIconAlignment = GridIconAlignment;
			if (ImGuiWidgets.Combo("Icon Layout", ref gridIconAlignment))
			{
				GridIconAlignment = gridIconAlignment;
			}

			float gridHeight = GridHeight;
			if (DemoProbe.SliderFloat("Height", ref gridHeight, 100f, 800f))
			{
				GridHeight = gridHeight;
			}

			ImGui.Columns(1);
			ImGui.Separator();

			// Grid display
			float iconSizePx = ImGuiApp.EmsToPx(2.5f);
			float bigIconSizePx = iconSizePx * 2;
			float gridIconSize = GridIconSizeBig ? bigIconSizePx : iconSizePx;

			Vector2 MeasureGridSize(string textBlock) => ImGuiWidgets.CalcIconSize(textBlock, gridIconSize, GridIconAlignment, itemSpacing, framePadding);
			void DrawGridCell(string textBlock, Vector2 cellSize, Vector2 itemSize)
			{
				float containerSizeX = GridIconAlignment == ImGuiWidgets.IconAlignment.Vertical ? cellSize.X : itemSize.X;
				float containerSizeY = GridIconAlignment == ImGuiWidgets.IconAlignment.Vertical ? itemSize.Y : cellSize.Y;
				using (new Alignment.CenterWithin(itemSize, new(containerSizeX, containerSizeY)))
				{
					ImGuiWidgets.Icon(textBlock, DemoContext.KtsuTexture.TextureId, gridIconSize, GridIconAlignment);
				}
			}

			ImGuiWidgets.GridOptions gridOptions = new()
			{
				GridSize = new Vector2(ImGui.GetContentRegionAvail().X, GridHeight),
				FitToContents = GridFitToContents,
			};

			ImGui.TextUnformatted($"Showing {GridItemsToShow} items in {GridOrder} order:");

			switch (GridOrder)
			{
				case ImGuiWidgets.GridOrder.RowMajor:
					ImGuiWidgets.RowMajorGrid("demoRowMajorGrid", GridStrings.Take(GridItemsToShow), MeasureGridSize, DrawGridCell, gridOptions);
					break;

				case ImGuiWidgets.GridOrder.ColumnMajor:
					ImGuiWidgets.ColumnMajorGrid("demoColumnMajorGrid", GridStrings.Take(GridItemsToShow), MeasureGridSize, DrawGridCell, gridOptions);
					break;

				default:
					throw new NotImplementedException();
			}
		}
	}
}
