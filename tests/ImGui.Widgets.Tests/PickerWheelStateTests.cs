// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class PickerWheelStateTests
{
	[TestMethod]
	public void ResolveTarget_SlowRelease_TakesTheNearestRow()
	{
		Assert.AreEqual(3, PickerWheelState.ResolveTarget(2.6f, 0f, 10, wrap: false));
		Assert.AreEqual(2, PickerWheelState.ResolveTarget(2.4f, 0f, 10, wrap: false));
	}

	[TestMethod]
	public void ResolveTarget_Fling_CoastsOnByItsSpeed()
	{
		// Thirty rows a second coasts five rows at the wheel's friction.
		Assert.AreEqual(7, PickerWheelState.ResolveTarget(2f, 30f, 20, wrap: false));
		Assert.AreEqual(-3, PickerWheelState.ResolveTarget(2f, -30f, 20, wrap: true));
	}

	[TestMethod]
	public void ResolveTarget_WithoutWrap_StopsAtTheEnds()
	{
		Assert.AreEqual(9, PickerWheelState.ResolveTarget(8f, 60f, 10, wrap: false));
		Assert.AreEqual(0, PickerWheelState.ResolveTarget(1f, -60f, 10, wrap: false));
	}

	[TestMethod]
	public void WrapIndex_FoldsEveryRowOntoTheItems()
	{
		Assert.AreEqual(9, PickerWheelState.WrapIndex(-1, 10));
		Assert.AreEqual(0, PickerWheelState.WrapIndex(10, 10));
		Assert.AreEqual(3, PickerWheelState.WrapIndex(-17, 10));
	}

	[TestMethod]
	public void NearestEquivalent_TakesTheShorterWayRound()
	{
		// From 58 on a sixty-item wheel, item 1 is three rows on, not fifty-seven back.
		Assert.AreEqual(61, PickerWheelState.NearestEquivalent(58, 1, 60));
		Assert.AreEqual(-2, PickerWheelState.NearestEquivalent(1, 58, 60));
		Assert.AreEqual(125, PickerWheelState.NearestEquivalent(120, 5, 60));
	}

	[TestMethod]
	public void Drag_DownwardsBringsEarlierItemsToTheMiddle()
	{
		PickerWheelState state = new(5);
		state.BeginDrag();
		state.Drag(40f, 20f, 10, wrap: false);

		Assert.AreEqual(3f, state.Position, 1e-5f);
	}

	[TestMethod]
	public void Drag_WithoutWrap_StopsHalfARowPastTheEnd()
	{
		PickerWheelState state = new(0);
		state.BeginDrag();
		state.Drag(400f, 20f, 10, wrap: false);

		Assert.AreEqual(-PickerWheelState.Overscroll, state.Position, 1e-5f);
	}

	[TestMethod]
	public void Release_SettlesOnTheRow_AndWrapsItsIndex()
	{
		PickerWheelState state = new(0);
		state.BeginDrag();
		state.Drag(42f, 20f, 10, wrap: true);

		Assert.AreEqual(8, state.Release(0f, 20f, 10, wrap: true));
		Settle(state);

		Assert.AreEqual(-2f, state.Position, 1e-3f);
	}

	[TestMethod]
	public void Step_WithoutWrap_Clamps()
	{
		PickerWheelState state = new(9);

		Assert.AreEqual(9, state.Step(1, 10, wrap: false));
		Assert.AreEqual(8, state.Step(-1, 10, wrap: false));
	}

	[TestMethod]
	public void GoTo_OnAWrappingWheel_TurnsTheShortWay()
	{
		PickerWheelState state = new(58);
		state.GoTo(1, 60, wrap: true);
		Settle(state);

		Assert.AreEqual(61, state.Target);
		Assert.AreEqual(61f, state.Position, 1e-3f);
	}

	[TestMethod]
	public void RowAt_FindsTheRowUnderThePointer()
	{
		const int Rows = 5;
		const float Radius = 50f;
		float angle = ImGuiWidgets.PickerWheelImpl.RowAngle(Rows);

		Assert.AreEqual(0, ImGuiWidgets.PickerWheelImpl.RowAt(0f, Radius, Rows));
		Assert.AreEqual(1, ImGuiWidgets.PickerWheelImpl.RowAt(MathF.Sin(angle) * Radius, Radius, Rows));
		Assert.AreEqual(-2, ImGuiWidgets.PickerWheelImpl.RowAt(-MathF.Sin(2f * angle) * Radius, Radius, Rows));
		Assert.AreEqual(2, ImGuiWidgets.PickerWheelImpl.RowAt(Radius * 2f, Radius, Rows));
	}

	private static void Settle(PickerWheelState state)
	{
		for (int i = 0; i < 240; i++)
		{
			state.Update(1f / 60f);
		}
	}
}
