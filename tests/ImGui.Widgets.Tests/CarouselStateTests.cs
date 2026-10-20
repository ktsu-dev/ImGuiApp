// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class CarouselStateTests
{
	[TestMethod]
	public void ResolvePage_SlowRelease_TakesTheNearestPage()
	{
		Assert.AreEqual(1, CarouselState.ResolvePage(0.6f, 0f, 5, 0));
		Assert.AreEqual(0, CarouselState.ResolvePage(0.4f, 0f, 5, 0));
	}

	[TestMethod]
	public void ResolvePage_Fling_TurnsThePageFromAShortDrag()
	{
		// A tenth of a page dragged, but at three pages a second: half a page more of coasting.
		Assert.AreEqual(1, CarouselState.ResolvePage(0.1f, 3f, 5, 0));
		Assert.AreEqual(1, CarouselState.ResolvePage(1.9f, -3f, 5, 2));
	}

	[TestMethod]
	public void ResolvePage_HardFling_MovesOnePageAtMost()
	{
		Assert.AreEqual(3, CarouselState.ResolvePage(2.3f, 60f, 10, 2));
		Assert.AreEqual(1, CarouselState.ResolvePage(1.7f, -60f, 10, 2));
	}

	[TestMethod]
	public void ResolvePage_StaysWithinThePages()
	{
		Assert.AreEqual(0, CarouselState.ResolvePage(-0.2f, -5f, 3, 0));
		Assert.AreEqual(2, CarouselState.ResolvePage(2.2f, 5f, 3, 2));
	}

	[TestMethod]
	public void Drag_FollowsThePointer_InPages()
	{
		CarouselState state = new(1);
		state.BeginDrag();
		state.Drag(-50f, 200f, 4);

		Assert.AreEqual(1.25f, state.Position, 1e-5f);
	}

	[TestMethod]
	public void Drag_PastTheEnd_StopsAtTheOverscroll()
	{
		CarouselState state = new(0);
		state.BeginDrag();
		state.Drag(500f, 200f, 4);

		Assert.AreEqual(-CarouselState.Overscroll, state.Position, 1e-5f);
	}

	[TestMethod]
	public void Release_SettlesOnTheChosenPage()
	{
		CarouselState state = new(0);
		state.BeginDrag();
		state.Drag(-130f, 200f, 4);

		Assert.AreEqual(1, state.Release(0f, 200f, 4));
		Settle(state);

		Assert.AreEqual(1f, state.Position, 1e-3f);
	}

	[TestMethod]
	public void GoTo_SlidesToThePage_AndClamps()
	{
		CarouselState state = new(0);
		state.GoTo(9, 4);
		Settle(state);

		Assert.AreEqual(3, state.TargetPage);
		Assert.AreEqual(3f, state.Position, 1e-3f);
	}

	private static void Settle(CarouselState state)
	{
		for (int i = 0; i < 180; i++)
		{
			state.Update(1f / 60f);
		}
	}
}
