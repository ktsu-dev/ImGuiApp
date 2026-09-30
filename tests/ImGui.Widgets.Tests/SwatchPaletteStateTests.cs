// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Collections.Generic;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the layout, hit-testing, drop slot, move and drag tracking behind SwatchPalette. All pure —
/// no ImGui context required. Every case uses a 20 pixel swatch, so a cell's pitch is 24.
/// </summary>
[TestClass]
public class SwatchPaletteStateTests
{
	private const float Size = 20f;

	private static List<string> Letters() => ["a", "b", "c", "d"];

	[TestMethod]
	public void ClampSwatchSize_Clamps()
	{
		Assert.AreEqual(8f, ImGuiWidgets.SwatchPaletteState.ClampSwatchSize(4f));
		Assert.AreEqual(128f, ImGuiWidgets.SwatchPaletteState.ClampSwatchSize(200f));
		Assert.AreEqual(20f, ImGuiWidgets.SwatchPaletteState.ClampSwatchSize(float.NaN));
	}

	[TestMethod]
	public void ColumnsFor_UsesTheRequestedCount() =>
		Assert.AreEqual(3, ImGuiWidgets.SwatchPaletteState.ColumnsFor(3, 500f, Size));

	[TestMethod]
	public void ColumnsFor_FitsTheAvailableWidth() =>
		Assert.AreEqual(4, ImGuiWidgets.SwatchPaletteState.ColumnsFor(0, 100f, Size));

	[TestMethod]
	public void ColumnsFor_IsAtLeastOne()
	{
		Assert.AreEqual(1, ImGuiWidgets.SwatchPaletteState.ColumnsFor(0, 5f, Size));
		Assert.AreEqual(1, ImGuiWidgets.SwatchPaletteState.ColumnsFor(0, float.NaN, Size));
	}

	[TestMethod]
	public void GridSize_WrapsRows() =>
		Assert.AreEqual(new Vector2(92f, 68f), ImGuiWidgets.SwatchPaletteState.GridSize(10, 4, Size));

	[TestMethod]
	public void GridSize_EmptyIsOneSwatch() =>
		Assert.AreEqual(new Vector2(20f, 20f), ImGuiWidgets.SwatchPaletteState.GridSize(0, 4, Size));

	[TestMethod]
	public void CellMin_WrapsAtTheColumnCount() =>
		Assert.AreEqual(new Vector2(24f, 24f), ImGuiWidgets.SwatchPaletteState.CellMin(5, 4, Size));

	[TestMethod]
	public void IndexAt_FindsTheSwatch() =>
		Assert.AreEqual(5, ImGuiWidgets.SwatchPaletteState.IndexAt(new Vector2(30f, 30f), 10, 4, Size));

	[TestMethod]
	public void IndexAt_GapIsNothing() =>
		Assert.AreEqual(-1, ImGuiWidgets.SwatchPaletteState.IndexAt(new Vector2(22f, 5f), 10, 4, Size));

	[TestMethod]
	public void IndexAt_PastTheLastSwatchIsNothing() =>
		Assert.AreEqual(-1, ImGuiWidgets.SwatchPaletteState.IndexAt(new Vector2(80f, 60f), 10, 4, Size));

	[TestMethod]
	public void DropSlot_LeftHalfIsBefore() =>
		Assert.AreEqual(1, ImGuiWidgets.SwatchPaletteState.DropSlot(new Vector2(27f, 5f), 10, 4, Size));

	[TestMethod]
	public void DropSlot_RightHalfIsAfter() =>
		Assert.AreEqual(2, ImGuiWidgets.SwatchPaletteState.DropSlot(new Vector2(40f, 5f), 10, 4, Size));

	[TestMethod]
	public void DropSlot_ClampsToTheRowsEnd() =>
		Assert.AreEqual(4, ImGuiWidgets.SwatchPaletteState.DropSlot(new Vector2(500f, 5f), 10, 4, Size));

	[TestMethod]
	public void DropSlot_ClampsToTheListEnd() =>
		Assert.AreEqual(10, ImGuiWidgets.SwatchPaletteState.DropSlot(new Vector2(500f, 500f), 10, 4, Size));

	[TestMethod]
	public void DropSlot_BeforeEverythingIsZero() =>
		Assert.AreEqual(0, ImGuiWidgets.SwatchPaletteState.DropSlot(new Vector2(-50f, -50f), 10, 4, Size));

	[TestMethod]
	public void Move_MovesForward()
	{
		List<string> list = Letters();
		ImGuiWidgets.SwatchPaletteState.Move(list, 0, 3, -1);
		Assert.AreSequenceEqual(["b", "c", "a", "d"], list);
	}

	[TestMethod]
	public void Move_MovesBackward()
	{
		List<string> list = Letters();
		ImGuiWidgets.SwatchPaletteState.Move(list, 3, 1, -1);
		Assert.AreSequenceEqual(["a", "d", "b", "c"], list);
	}

	[TestMethod]
	public void Move_ToTheEnd()
	{
		List<string> list = Letters();
		ImGuiWidgets.SwatchPaletteState.Move(list, 1, 4, -1);
		Assert.AreSequenceEqual(["a", "c", "d", "b"], list);
	}

	[TestMethod]
	public void Move_AdjacentSlotsAreNoOps()
	{
		List<string> list = Letters();

		Assert.AreEqual(1, ImGuiWidgets.SwatchPaletteState.Move(list, 2, 2, 1));
		Assert.AreEqual(1, ImGuiWidgets.SwatchPaletteState.Move(list, 2, 3, 1));
		Assert.AreSequenceEqual(Letters(), list);
	}

	[TestMethod]
	public void Move_SelectionFollowsTheMovedSwatch() =>
		Assert.AreEqual(2, ImGuiWidgets.SwatchPaletteState.Move(Letters(), 0, 3, 0));

	[TestMethod]
	public void Move_SelectionShiftsWhenPassedOver()
	{
		Assert.AreEqual(1, ImGuiWidgets.SwatchPaletteState.Move(Letters(), 0, 3, 2));
		Assert.AreEqual(2, ImGuiWidgets.SwatchPaletteState.Move(Letters(), 3, 1, 1));
	}

	[TestMethod]
	public void Move_OutOfRangeSourceIsANoOp()
	{
		List<string> list = Letters();

		Assert.AreEqual(-1, ImGuiWidgets.SwatchPaletteState.Move(list, 7, 0, -1));
		Assert.AreSequenceEqual(Letters(), list);
	}

	[TestMethod]
	public void Motion_StartsDraggingPastTheThreshold()
	{
		ImGuiWidgets.SwatchPaletteState state = new();
		state.Press(2, Vector2.Zero);

		state.Motion(new Vector2(3f, 0f), 6f);
		Assert.IsFalse(state.Dragging, "A move inside the threshold started a drag.");

		state.Motion(new Vector2(7f, 0f), 6f);
		Assert.IsTrue(state.Dragging, "A move past the threshold did not start a drag.");

		state.Motion(Vector2.Zero, 6f);
		Assert.IsTrue(state.Dragging, "Moving back to the press point ended the drag.");
	}

	[TestMethod]
	public void Motion_WithoutASourceNeverDrags()
	{
		ImGuiWidgets.SwatchPaletteState state = new();
		state.Press(-1, Vector2.Zero);

		state.Motion(new Vector2(50f, 0f), 6f);

		Assert.IsFalse(state.Dragging);
	}

	[TestMethod]
	public void Release_Resets()
	{
		ImGuiWidgets.SwatchPaletteState state = new();
		state.Press(1, Vector2.Zero);
		state.Motion(new Vector2(50f, 0f), 6f);

		state.Release();

		Assert.AreEqual(-1, state.DragSource);
		Assert.IsFalse(state.Dragging);
	}
}
