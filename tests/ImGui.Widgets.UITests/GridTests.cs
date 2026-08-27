// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <c>ImGuiWidgets.RowMajorGrid</c> and <c>ImGuiWidgets.ColumnMajorGrid</c> on their own.</summary>
[TestClass]
public sealed class GridTests : WidgetTest
{
	private static readonly string[] Items = ["one", "two", "three", "four", "five", "six"];
	private static readonly Vector2 CellSize = new(120f, 40f);

	private readonly List<string> drawn = [];

	private void DrawCell(string item, Vector2 cellSize, Vector2 itemSize)
	{
		drawn.Add(item);
		ImGui.Button(item, itemSize);
		Mark(item);
	}

	private void DrawRowMajor() =>
		ImGuiWidgets.RowMajorGrid("grid", Items, _ => CellSize, DrawCell);

	private void DrawColumnMajor() =>
		ImGuiWidgets.ColumnMajorGrid("grid", Items, _ => CellSize, DrawCell);

	[TestInitialize]
	public void ClearDrawn() => drawn.Clear();

	[TestMethod]
	public void Grid_DrawsEveryItem()
	{
		Start(DrawRowMajor);

		foreach (string item in Items)
		{
			Assert.IsTrue(IsVisible(item), $"The grid never drew '{item}'.");
		}
	}

	[TestMethod]
	public void Grid_PassesEachItemToTheDrawDelegateOncePerFrame()
	{
		Start(DrawRowMajor);

		drawn.Clear();
		Step();

		CollectionAssert.AreEqual(Items, drawn, "The draw delegate did not see every item exactly once, in order.");
	}

	[TestMethod]
	public void RowMajorGrid_FillsAcrossBeforeDown()
	{
		Start(DrawRowMajor);

		Rectangle first = RectOf(Items[0]);
		Rectangle second = RectOf(Items[1]);

		Assert.IsTrue(second.MinX > first.MinX, $"'{Items[1]}' was placed at x={second.MinX}, not right of '{Items[0]}' at x={first.MinX}.");
		Assert.AreEqual(first.MinY, second.MinY, "The first two items of a row-major grid were not on the same row.");
	}

	[TestMethod]
	public void ColumnMajorGrid_FillsDownBeforeAcross()
	{
		Start(DrawColumnMajor);

		Rectangle first = RectOf(Items[0]);
		Rectangle second = RectOf(Items[1]);

		Assert.IsTrue(second.MinY > first.MinY, $"'{Items[1]}' was placed at y={second.MinY}, not below '{Items[0]}' at y={first.MinY}.");
	}

	[TestMethod]
	public void Grid_WrapsWhenTheRowRunsOut()
	{
		Start(DrawRowMajor);

		// Six 120px cells cannot fit across a 640px window, so at least one item has to wrap onto
		// a later row.
		int firstRowTop = RectOf(Items[0]).MinY;
		bool wrapped = Items.Any(item => RectOf(item).MinY > firstRowTop);

		Assert.IsTrue(wrapped, "Every item stayed on one row in a window too narrow to hold them.");
	}

	[TestMethod]
	public void Grid_WithNoItems_DrawsNothing()
	{
		Start(() => ImGuiWidgets.RowMajorGrid("grid", [], (string _) => CellSize, DrawCell));

		Assert.AreEqual(0, drawn.Count, "An empty grid called the draw delegate.");
	}

	[TestMethod]
	public void Grid_FitToContents_SizesTheRegionToTheItems()
	{
		Start(() => ImGuiWidgets.RowMajorGrid(
			"grid",
			Items,
			_ => CellSize,
			DrawCell,
			new ImGuiWidgets.GridOptions { FitToContents = true }));

		foreach (string item in Items)
		{
			Assert.IsTrue(IsVisible(item), $"A fit-to-contents grid never drew '{item}'.");
		}
	}
}
