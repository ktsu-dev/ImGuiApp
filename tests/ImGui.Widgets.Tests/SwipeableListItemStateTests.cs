// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class SwipeableListItemStateTests
{
	// A 300-wide row with 80 of leading actions and 160 of trailing ones.
	private static readonly SwipeLayout Layout = new(80f, 160f, 300f, FullSwipe: true, FullSwipeFraction: 0.6f);

	[TestMethod]
	public void SlowRelease_PastHalfTheActions_Opens()
	{
		Assert.AreEqual(new SwipeRelease(80f, null), SwipeableListItemState.ResolveRelease(50f, 0f, Layout));
		Assert.AreEqual(new SwipeRelease(-160f, null), SwipeableListItemState.ResolveRelease(-90f, 0f, Layout));
	}

	[TestMethod]
	public void SlowRelease_ShortOfHalfTheActions_Closes()
	{
		Assert.AreEqual(new SwipeRelease(0f, null), SwipeableListItemState.ResolveRelease(30f, 0f, Layout));
		Assert.AreEqual(new SwipeRelease(0f, null), SwipeableListItemState.ResolveRelease(-70f, 0f, Layout));
	}

	[TestMethod]
	public void OutwardFlick_OpensEvenWhenShort()
	{
		Assert.AreEqual(80f, SwipeableListItemState.ResolveRelease(10f, 900f, Layout).Target);
		Assert.AreEqual(-160f, SwipeableListItemState.ResolveRelease(-10f, -900f, Layout).Target);
	}

	[TestMethod]
	public void InwardFlick_ClosesEvenWhenFullyOpen()
	{
		Assert.AreEqual(0f, SwipeableListItemState.ResolveRelease(80f, -900f, Layout).Target);
		Assert.AreEqual(0f, SwipeableListItemState.ResolveRelease(-160f, 900f, Layout).Target);
	}

	[TestMethod]
	public void LongDrag_CommitsTheOutermostAction_AndCloses()
	{
		Assert.AreEqual(new SwipeRelease(0f, SwipeEdge.Leading), SwipeableListItemState.ResolveRelease(190f, 0f, Layout));
		Assert.AreEqual(new SwipeRelease(0f, SwipeEdge.Trailing), SwipeableListItemState.ResolveRelease(-200f, 0f, Layout));
	}

	[TestMethod]
	public void LongDrag_WithFullSwipeOff_OnlyOpens()
	{
		SwipeLayout noFull = Layout with { FullSwipe = false };

		Assert.AreEqual(new SwipeRelease(80f, null), SwipeableListItemState.ResolveRelease(250f, 0f, noFull));
	}

	[TestMethod]
	public void ClampOffset_KeepsASideWithNoActionsShut()
	{
		SwipeLayout trailingOnly = Layout with { LeadingWidth = 0f };

		Assert.AreEqual(0f, SwipeableListItemState.ClampOffset(50f, trailingOnly));
		Assert.AreEqual(-50f, SwipeableListItemState.ClampOffset(-50f, trailingOnly));
		Assert.AreEqual(-300f, SwipeableListItemState.ClampOffset(-500f, trailingOnly));
	}

	[TestMethod]
	public void Drag_FollowsThePointer_FromWhereTheRowWas()
	{
		SwipeableListItemState state = new();
		state.BeginDrag();
		state.Drag(-120f, Layout);
		_ = state.Release(0f, Layout);
		Settle(state);
		Assert.AreEqual(-160f, state.Offset, 0.01f);

		// A second drag starts from the open position, not from zero.
		state.BeginDrag();
		state.Drag(40f, Layout);
		Assert.AreEqual(-120f, state.Offset, 0.01f);
	}

	[TestMethod]
	public void Close_SettlesAtZero()
	{
		SwipeableListItemState state = new();
		state.BeginDrag();
		state.Drag(70f, Layout);
		_ = state.Release(0f, Layout);
		Assert.IsTrue(state.IsOpen);

		state.Close();
		Settle(state);

		Assert.AreEqual(0f, state.Offset, 0.01f);
		Assert.IsFalse(state.IsOpen);
	}

	private static void Settle(SwipeableListItemState state)
	{
		for (int i = 0; i < 120; i++)
		{
			state.Update(1f / 60f);
		}
	}
}
