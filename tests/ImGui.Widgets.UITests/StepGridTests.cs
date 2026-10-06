// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.StepGrid"/> on its own.</summary>
[TestClass]
public sealed class StepGridTests : WidgetTest
{
	private const string Label = "grid";
	private const int Rows = 4;
	private const int Steps = 16;

	private static readonly Vector2 CellSize = new(20f, 20f);
	private static readonly string[] Voices = ["Kick", "Snare", "Hat", "Clap"];

	private bool[] steps = new bool[Rows * Steps];
	private bool changed;
	private int playingStep = -1;
	private string[] rowLabels = [];

	private void Draw() =>
		changed |= ImGuiWidgets.StepGrid(Label, steps, Rows, Steps, playingStep, cellSize: CellSize, rowLabels: rowLabels);

	private void Drag(string from, string to)
	{
		Vector2 start = CenterOf(from);
		Vector2 end = CenterOf(to);
		Harness.Mouse.Drag(start.X, start.Y, end.X, end.Y);
		Step();
	}

	[TestMethod]
	public void StepGrid_MarksItselfAndEveryCell()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Label), "The grid marked no probe item.");
		Assert.IsTrue(IsVisible("grid/r0s0"), "The first cell marked no probe region.");
		Assert.IsTrue(IsVisible("grid/r3s15"), "The last cell marked no probe region.");
	}

	[TestMethod]
	public void StepGrid_ReservesItsLayoutSize()
	{
		Start(Draw);

		Rectangle rect = RectOf(Label);

		Assert.AreEqual(350f, rect.Width, 2f, "The grid's width is not 16 cells and 15 gaps.");
		Assert.AreEqual(86f, rect.Height, 2f, "The grid's height is not 4 cells and 3 gaps.");
	}

	[TestMethod]
	public void StepGrid_ClickTogglesACellOn()
	{
		Start(Draw);

		Click("grid/r1s2");

		Assert.IsTrue(steps[18], "Clicking row 1, step 2 did not turn it on.");
		Assert.IsTrue(changed, "The click changed the pattern but the widget did not say so.");
	}

	[TestMethod]
	public void StepGrid_ClickTogglesAnOnCellOff()
	{
		steps[18] = true;
		Start(Draw);

		Click("grid/r1s2");

		Assert.IsFalse(steps[18], "Clicking an on cell did not turn it off.");
		Assert.IsTrue(changed);
	}

	[TestMethod]
	public void StepGrid_DragPaintsEveryCellCrossed()
	{
		Start(Draw);

		Drag("grid/r0s0", "grid/r0s7");

		for (int i = 0; i < steps.Length; i++)
		{
			Assert.AreEqual(i <= 7, steps[i], $"Cell {i} holds the wrong value after painting steps 0 to 7 of row 0.");
		}

		Assert.IsTrue(changed);
	}

	[TestMethod]
	public void StepGrid_DragFromAnOnCellErases()
	{
		for (int step = 0; step < Steps; step++)
		{
			steps[step] = true;
		}

		Start(Draw);

		Drag("grid/r0s0", "grid/r0s3");

		for (int step = 0; step < Steps; step++)
		{
			Assert.AreEqual(step > 3, steps[step], $"Step {step} of row 0 holds the wrong value after erasing steps 0 to 3.");
		}
	}

	[TestMethod]
	public void StepGrid_HoverAloneReportsNoChange()
	{
		Start(Draw);

		Hover("grid/r2s2");
		Step(3);

		Assert.IsFalse(changed, "Hovering reported a change.");
		Assert.IsFalse(Array.Exists(steps, on => on), "Hovering changed a cell.");
	}

	[TestMethod]
	public void StepGrid_PlayingColumnIsHighlighted()
	{
		Start(Draw);
		byte[] before = Snapshot();

		playingStep = 5;
		Step(2);

		Rectangle difference = BoundsOfDifference(before)
			?? throw new AssertFailedException("Highlighting step 5 changed no pixels.");
		Rectangle column = RectOf("grid/r0s5");
		Rectangle grid = RectOf(Label);

		Assert.IsGreaterThanOrEqualTo(column.MinX - 1f, difference.MinX, "The highlight starts left of column 5.");
		Assert.IsLessThanOrEqualTo(column.MaxX + 1f, difference.MaxX, "The highlight ends right of column 5.");
		Assert.AreEqual(grid.Height, difference.Height, 2f, "The highlight does not span the grid's height.");
	}

	[TestMethod]
	public void StepGrid_RowLabelsSitLeftOfTheGrid()
	{
		Start(Draw);
		float withoutLabels = RectOf(Label).MinX;
		DisposeHarness();

		rowLabels = Voices;
		float snareWidth = 0f;
		Start(() =>
		{
			snareWidth = ImGui.CalcTextSize("Snare").X;
			Draw();
		});
		float withLabels = RectOf(Label).MinX;

		Assert.IsGreaterThanOrEqualTo(
			snareWidth,
			withLabels - withoutLabels,
			$"The grid moved {withLabels - withoutLabels} px right for its labels, less than the widest label.");
	}

	[TestMethod]
	public void StepGrid_MismatchedLengthThrows()
	{
		steps = new bool[(Rows * Steps) - 1];

		HarnessFrameException error = Assert.ThrowsExactly<HarnessFrameException>(() => Start(Draw));

		Assert.IsInstanceOfType<ArgumentException>(error.InnerException);
	}

	[TestMethod]
	public void StepGrid_EmptyGridDrawsNothingAndDoesNotThrow()
	{
		bool result = true;
		Start(() => result = ImGuiWidgets.StepGrid(Label, [], 0, Steps));

		Assert.IsFalse(result);
		Assert.IsFalse(IsVisible(Label), "An empty grid submitted an item.");
	}
}
