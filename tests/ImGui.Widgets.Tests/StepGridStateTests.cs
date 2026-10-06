// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Linq;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the geometry and the toggle-then-paint gesture behind StepGrid. All pure — no ImGui context required.
/// </summary>
[TestClass]
public class StepGridStateTests
{
	private const int Rows = 4;
	private const int Steps = 16;

	private static readonly ImGuiWidgets.StepGridLayout Layout = new(Rows, Steps, new Vector2(20f, 20f), 2f);

	private static Vector2 CentreOf(int row, int step) => Layout.CellMin(row, step) + new Vector2(10f, 10f);

	private static int IndexOf(int row, int step) => (row * Steps) + step;

	[TestMethod]
	public void Size_CountsCellsAndGaps() =>
		Assert.AreEqual(new Vector2(350f, 86f), Layout.Size);

	[TestMethod]
	public void Size_IsZeroForAnEmptyGrid() =>
		Assert.AreEqual(Vector2.Zero, (Layout with { Rows = 0 }).Size);

	[TestMethod]
	public void CellMin_StepsByCellPlusGap() =>
		Assert.AreEqual(new Vector2(66f, 44f), Layout.CellMin(2, 3));

	[TestMethod]
	public void HitTest_CentreOfACellIsItsIndex() =>
		Assert.AreEqual(35, Layout.HitTest(new Vector2(66f + 10f, 44f + 10f)));

	[TestMethod]
	public void HitTest_TopLeftCornerIsInside_RightEdgeIsNot()
	{
		Assert.AreEqual(35, Layout.HitTest(new Vector2(66f, 44f)), "A cell's top-left corner belongs to it.");
		Assert.AreEqual(-1, Layout.HitTest(new Vector2(86f, 44f)), "A cell's right edge is the start of the gap.");
	}

	[TestMethod]
	public void HitTest_GapIsNoCell() =>
		Assert.AreEqual(-1, Layout.HitTest(new Vector2(21f, 5f)));

	[TestMethod]
	public void HitTest_OutsideIsMinusOne()
	{
		Assert.AreEqual(-1, Layout.HitTest(new Vector2(-1f, 5f)), "Left of the grid.");
		Assert.AreEqual(-1, Layout.HitTest(new Vector2(350f, 5f)), "At the grid's right edge.");
		Assert.AreEqual(-1, Layout.HitTest(new Vector2(5f, 86f)), "At the grid's bottom edge.");
		Assert.AreEqual(-1, Layout.HitTest(new Vector2(float.NaN, 5f)), "Not a number.");
	}

	[TestMethod]
	public void IsBeatStart_EveryFourthStep()
	{
		foreach (int step in (int[])[0, 4, 8, 12])
		{
			Assert.IsTrue(ImGuiWidgets.StepGridLayout.IsBeatStart(step, 4), $"Step {step} should start a beat.");
		}

		Assert.IsFalse(ImGuiWidgets.StepGridLayout.IsBeatStart(1, 4));
		Assert.IsFalse(ImGuiWidgets.StepGridLayout.IsBeatStart(5, 4));
		Assert.IsFalse(ImGuiWidgets.StepGridLayout.IsBeatStart(0, 0), "No beats at all when stepsPerBeat is 0.");
		Assert.IsFalse(ImGuiWidgets.StepGridLayout.IsBeatStart(8, 0), "No beats at all when stepsPerBeat is 0.");
	}

	[TestMethod]
	public void Press_TogglesAnOffCellOnAndPaintsOn()
	{
		bool[] steps = new bool[Rows * Steps];
		ImGuiWidgets.StepGridState state = new();

		bool toggled = state.Press(steps, Layout, CentreOf(1, 2));

		Assert.IsTrue(toggled);
		Assert.IsTrue(steps[IndexOf(1, 2)]);
		Assert.IsTrue(state.PaintValue);
		Assert.IsTrue(state.IsPainting);
	}

	[TestMethod]
	public void Press_TogglesAnOnCellOffAndPaintsOff()
	{
		bool[] steps = new bool[Rows * Steps];
		steps[IndexOf(1, 2)] = true;
		ImGuiWidgets.StepGridState state = new();

		bool toggled = state.Press(steps, Layout, CentreOf(1, 2));

		Assert.IsTrue(toggled);
		Assert.IsFalse(steps[IndexOf(1, 2)]);
		Assert.IsFalse(state.PaintValue);
		Assert.IsTrue(state.IsPainting);
	}

	[TestMethod]
	public void Press_AlwaysToggles_EvenOnTheCellOfThePreviousGesture()
	{
		bool[] steps = new bool[Rows * Steps];
		ImGuiWidgets.StepGridState state = new();

		state.Press(steps, Layout, CentreOf(0, 0));
		state.Release();
		state.Press(steps, Layout, CentreOf(0, 0));

		Assert.IsFalse(steps[0], "The second press should have toggled the cell back off.");
		Assert.IsFalse(state.PaintValue);
	}

	[TestMethod]
	public void Press_OnAGapStartsNothing()
	{
		bool[] steps = new bool[Rows * Steps];
		ImGuiWidgets.StepGridState state = new();

		bool toggled = state.Press(steps, Layout, new Vector2(21f, 5f));

		Assert.IsFalse(toggled);
		Assert.IsFalse(state.IsPainting);
		Assert.AreEqual(0, CountOn(steps));

		Assert.IsFalse(state.PaintTo(steps, Layout, CentreOf(0, 5)), "A drag from a gap should paint nothing.");
		Assert.AreEqual(0, CountOn(steps));
	}

	[TestMethod]
	public void PaintTo_WithoutAPressDoesNothing()
	{
		bool[] steps = new bool[Rows * Steps];
		ImGuiWidgets.StepGridState state = new();

		Assert.IsFalse(state.PaintTo(steps, Layout, CentreOf(0, 7)));
		Assert.AreEqual(0, CountOn(steps));
	}

	[TestMethod]
	public void PaintTo_PaintsEveryCellAlongTheSegment()
	{
		bool[] steps = new bool[Rows * Steps];
		ImGuiWidgets.StepGridState state = new();

		state.Press(steps, Layout, CentreOf(0, 0));
		bool changed = state.PaintTo(steps, Layout, CentreOf(0, 7));

		Assert.IsTrue(changed);
		for (int i = 0; i < steps.Length; i++)
		{
			Assert.AreEqual(i <= 7, steps[i], $"Cell {i} holds the wrong value.");
		}
	}

	[TestMethod]
	public void PaintTo_ErasesWhenTheGestureStartedOnAnOnCell()
	{
		bool[] steps = new bool[Rows * Steps];
		for (int step = 0; step < Steps; step++)
		{
			steps[step] = true;
		}

		ImGuiWidgets.StepGridState state = new();

		state.Press(steps, Layout, CentreOf(0, 0));
		state.PaintTo(steps, Layout, CentreOf(0, 3));

		for (int step = 0; step < Steps; step++)
		{
			Assert.AreEqual(step > 3, steps[step], $"Step {step} of row 0 holds the wrong value.");
		}
	}

	[TestMethod]
	public void PaintTo_ReturnsFalseWhenEveryCellAlreadyHoldsTheValue()
	{
		bool[] steps = new bool[Rows * Steps];
		ImGuiWidgets.StepGridState state = new();

		state.Press(steps, Layout, CentreOf(0, 0));
		state.PaintTo(steps, Layout, CentreOf(0, 7));

		Assert.IsFalse(state.PaintTo(steps, Layout, CentreOf(0, 2)), "Dragging back over painted cells is not a change.");
	}

	[TestMethod]
	public void PaintTo_DiagonalSegmentHitsBothEnds()
	{
		bool[] steps = new bool[Rows * Steps];
		ImGuiWidgets.StepGridState state = new();

		state.Press(steps, Layout, CentreOf(0, 0));
		state.PaintTo(steps, Layout, CentreOf(3, 3));

		Assert.IsTrue(steps[IndexOf(0, 0)]);
		Assert.IsTrue(steps[IndexOf(3, 3)]);
	}

	[TestMethod]
	public void PaintTo_ContinuesAfterLeavingTheGridAndComingBack()
	{
		bool[] steps = new bool[Rows * Steps];
		ImGuiWidgets.StepGridState state = new();

		state.Press(steps, Layout, CentreOf(0, 0));
		Assert.IsFalse(state.PaintTo(steps, Layout, new Vector2(10f, -500f)), "Leaving the grid straight upwards crosses no new cell.");
		Assert.IsTrue(state.PaintTo(steps, Layout, CentreOf(0, 5)));

		Assert.IsTrue(steps[IndexOf(0, 5)]);
	}

	[TestMethod]
	public void PaintTo_ToleratesAnUnavailableMouse()
	{
		bool[] steps = new bool[Rows * Steps];
		ImGuiWidgets.StepGridState state = new();

		state.Press(steps, Layout, CentreOf(0, 0));

		// Dear ImGui reports an unavailable mouse at -FLT_MAX; sampling that segment unclipped would
		// never finish.
		Assert.IsFalse(state.PaintTo(steps, Layout, new Vector2(-float.MaxValue, -float.MaxValue)));
		Assert.IsFalse(state.PaintTo(steps, Layout, new Vector2(float.NaN, float.NaN)));
		Assert.AreEqual(1, CountOn(steps));
	}

	[TestMethod]
	public void PaintTo_UsesTheCurrentLayoutWhenThePatternShrinks()
	{
		bool[] steps = new bool[Rows * Steps];
		ImGuiWidgets.StepGridState state = new();
		state.Press(steps, Layout, CentreOf(0, 0));

		ImGuiWidgets.StepGridLayout smaller = Layout with { Steps = 4 };
		bool[] shorter = new bool[Rows * 4];
		state.PaintTo(shorter, smaller, smaller.CellMin(3, 3) + new Vector2(10f, 10f));

		Assert.IsTrue(shorter[(3 * 4) + 3], "The index should come from the layout the pattern has now.");
	}

	[TestMethod]
	public void Release_StopsPainting()
	{
		bool[] steps = new bool[Rows * Steps];
		ImGuiWidgets.StepGridState state = new();

		state.Press(steps, Layout, CentreOf(0, 0));
		state.Release();

		Assert.IsFalse(state.IsPainting);
		Assert.IsFalse(state.PaintTo(steps, Layout, CentreOf(0, 7)));
		Assert.AreEqual(1, CountOn(steps));
	}

	private static int CountOn(bool[] steps) => steps.Count(static on => on);
}
