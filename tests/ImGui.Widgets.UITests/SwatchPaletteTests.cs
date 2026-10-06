// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Collections.Generic;
using System.Numerics;

using ktsu.ImGui.App.Testing;
using ktsu.Semantics.Color;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.SwatchPalette"/> on its own.</summary>
[TestClass]
public sealed class SwatchPaletteTests : WidgetTest
{
	private const string Label = "pal";

	private static readonly Color Red = Color.FromBytes(255, 0, 0, 255);
	private static readonly Color Green = Color.FromBytes(0, 255, 0, 255);
	private static readonly Color Blue = Color.FromBytes(0, 0, 255, 255);
	private static readonly Color White = Color.FromBytes(255, 255, 255, 255);
	private static readonly Color Black = Color.FromBytes(0, 0, 0, 255);

	private readonly List<Color> swatches = [Red, Green, Blue, White, Black];
	private int selected = -1;
	private int columns = 5;
	private bool changed;

	private void Draw() => changed |= ImGuiWidgets.SwatchPalette(Label, swatches, ref selected, 20f, columns);

	[TestMethod]
	public void SwatchPalette_MarksTheGridAndEachSwatch()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Label), "The grid was not marked.");
		for (int i = 0; i < swatches.Count; i++)
		{
			Assert.IsTrue(IsVisible($"{Label}/{i}"), $"Swatch {i} was not marked.");
		}

		Assert.IsTrue(Math.Abs(RectOf(Label).Width - 116) <= 1, $"The grid was {RectOf(Label).Width}px wide rather than 116.");
	}

	[TestMethod]
	public void SwatchPalette_ClickSelects()
	{
		Start(Draw);

		Click($"{Label}/2");

		Assert.AreEqual(2, selected);
		Assert.IsTrue(changed, "Selecting a swatch reported no change.");
	}

	[TestMethod]
	public void SwatchPalette_ClickingEmptySpaceKeepsTheSelection()
	{
		columns = 3;
		selected = 1;
		Start(Draw);

		// Five swatches in three columns leave the bottom row's third cell empty.
		Rectangle grid = RectOf(Label);
		Harness.Mouse.Click(grid.MinX + 48 + 10, grid.MinY + 24 + 10);

		Assert.AreEqual(1, selected);
	}

	[TestMethod]
	public void SwatchPalette_DrawsEachColour()
	{
		Start(Draw);

		CapturedFrame frame = Harness.Capture();
		Vector2 first = CenterOf($"{Label}/0");
		Vector2 third = CenterOf($"{Label}/2");
		Rgba32 red = frame.GetPixel((int)first.X, (int)first.Y);
		Rgba32 blue = frame.GetPixel((int)third.X, (int)third.Y);

		Assert.IsTrue(red.R > 200 && red.G < 60, $"The red swatch drew ({red.R}, {red.G}, {red.B}).");
		Assert.IsGreaterThan(200, blue.B, $"The blue swatch drew ({blue.R}, {blue.G}, {blue.B}).");
	}

	[TestMethod]
	public void SwatchPalette_DragReorders()
	{
		Start(Draw);

		DragFirstPastTheFourth();

		Assert.AreSequenceEqual([Green, Blue, White, Red, Black], swatches);
		Assert.IsTrue(changed, "Reordering reported no change.");
	}

	[TestMethod]
	public void SwatchPalette_SelectionFollowsTheDraggedSwatch()
	{
		selected = 0;
		Start(Draw);

		DragFirstPastTheFourth();

		Assert.AreEqual(3, selected);
	}

	[TestMethod]
	public void SwatchPalette_DragBackToTheSameSpotChangesNothing()
	{
		Start(Draw);

		Vector2 centre = CenterOf($"{Label}/1");
		Harness.Mouse.MoveTo(centre.X, centre.Y);
		Step();
		HarnessMouse.Down(0);
		Step();
		Harness.Mouse.MoveTo(centre.X + 20f, centre.Y);
		Step();
		Harness.Mouse.MoveTo(centre.X - 4f, centre.Y);
		Step();
		HarnessMouse.Up(0);
		Step();

		Assert.AreSequenceEqual([Red, Green, Blue, White, Black], swatches);
		Assert.AreEqual(-1, selected);
		Assert.IsFalse(changed, "Dropping a swatch where it started reported a change.");
	}

	[TestMethod]
	public void SwatchPalette_ShortDragIsAClick()
	{
		Start(Draw);

		Vector2 centre = CenterOf($"{Label}/4");
		Harness.Mouse.MoveTo(centre.X, centre.Y);
		Step();
		HarnessMouse.Down(0);
		Step();
		Harness.Mouse.MoveTo(centre.X + 2f, centre.Y);
		Step();
		HarnessMouse.Up(0);
		Step();

		Assert.AreEqual(4, selected);
		Assert.AreSequenceEqual([Red, Green, Blue, White, Black], swatches);
	}

	[TestMethod]
	public void SwatchPalette_WrapsIntoRows()
	{
		columns = 2;
		Start(Draw);

		Assert.IsGreaterThan(RectOf($"{Label}/0").MaxY, RectOf($"{Label}/2").MinY, "The third swatch did not wrap onto a second row.");
	}

	[TestMethod]
	public void SwatchPalette_EmptyListReservesOneSwatch()
	{
		swatches.Clear();
		Start(Draw);

		Assert.IsTrue(Math.Abs(RectOf(Label).Width - 20) <= 1, $"The empty grid was {RectOf(Label).Width}px wide rather than 20.");
		Assert.IsFalse(changed, "An empty palette reported a change.");
		Assert.IsFalse(IsVisible($"{Label}/0"), "An empty palette marked a swatch.");
	}

	[TestMethod]
	public void SwatchPalette_OutOfRangeSelectionIsCleared()
	{
		selected = 9;
		Start(Draw);

		Assert.AreEqual(-1, selected);
		Assert.IsTrue(changed, "Clearing an out-of-range selection reported no change.");
	}

	private void DragFirstPastTheFourth()
	{
		Vector2 from = CenterOf($"{Label}/0");
		Vector2 to = CenterOf($"{Label}/3");
		Harness.Mouse.Drag(from.X, from.Y, to.X + 6f, to.Y);
		Step();
	}
}
