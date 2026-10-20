// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class BottomSheetStateTests
{
	private static readonly BottomSheetHeights Heights = new(100f, 300f, 540f);

	private static void Settle(BottomSheetState state)
	{
		for (int i = 0; i < 600; i++)
		{
			state.Update(1f / 120f, Heights);
		}
	}

	[TestMethod]
	public void Resolve_PicksTheNearestDetent()
	{
		Assert.AreEqual(BottomSheetDetent.Peek, BottomSheetState.Resolve(150f, Heights));
		Assert.AreEqual(BottomSheetDetent.Half, BottomSheetState.Resolve(250f, Heights));
		Assert.AreEqual(BottomSheetDetent.Full, BottomSheetState.Resolve(500f, Heights));
		Assert.AreEqual(BottomSheetDetent.Full, BottomSheetState.Resolve(900f, Heights));
	}

	[TestMethod]
	public void Resolve_BelowHalfOfPeek_Dismisses()
	{
		Assert.IsNull(BottomSheetState.Resolve(49f, Heights));
		Assert.AreEqual(BottomSheetDetent.Peek, BottomSheetState.Resolve(51f, Heights));
	}

	[TestMethod]
	public void Open_RisesToItsDetent()
	{
		BottomSheetState state = new();
		state.Open(BottomSheetDetent.Half);
		Settle(state);

		Assert.AreEqual(Heights.Half, state.Height, 1f);
	}

	[TestMethod]
	public void SlowRelease_SettlesAtTheNearestDetent()
	{
		BottomSheetState state = new();
		state.Open(BottomSheetDetent.Half);
		Settle(state);

		state.BeginDrag();
		state.Drag(-200f, Heights);
		state.Release(0f, Heights);
		Settle(state);

		Assert.AreEqual(BottomSheetDetent.Full, state.Detent);
		Assert.AreEqual(Heights.Full, state.Height, 1f);
	}

	[TestMethod]
	public void Flick_CarriesItPastTheNearestDetent()
	{
		BottomSheetState state = new();
		state.Open(BottomSheetDetent.Peek);
		Settle(state);

		state.BeginDrag();
		state.Drag(-60f, Heights);
		state.Release(-800f, Heights);

		Assert.AreEqual(BottomSheetDetent.Half, state.Detent);
	}

	[TestMethod]
	public void FlickDownFromPeek_Dismisses()
	{
		BottomSheetState state = new();
		state.Open(BottomSheetDetent.Peek);
		Settle(state);

		state.BeginDrag();
		state.Drag(20f, Heights);
		state.Release(1500f, Heights);

		Assert.IsNull(state.Detent);
		Assert.IsFalse(state.IsOpen);
	}

	[TestMethod]
	public void Drag_StopsAtFull()
	{
		BottomSheetState state = new();
		state.Open(BottomSheetDetent.Full);
		Settle(state);

		state.BeginDrag();
		state.Drag(-500f, Heights);

		Assert.AreEqual(Heights.Full, state.Height);
	}

	[TestMethod]
	public void Close_ReportsWhenTheSheetIsGone()
	{
		BottomSheetState state = new();
		state.Open(BottomSheetDetent.Half);
		Settle(state);
		state.Close();

		bool finished = false;
		for (int i = 0; i < 600 && !finished; i++)
		{
			finished = state.Update(1f / 120f, Heights);
		}

		Assert.IsTrue(finished);
		Assert.AreEqual(0f, state.Height);
		Assert.IsFalse(state.IsVisible);
	}
}
