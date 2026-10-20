// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class NavigationDrawerStateTests
{
	private const float Width = 280f;

	private static void Settle(NavigationDrawerState state)
	{
		for (int i = 0; i < 600; i++)
		{
			state.Update(1f / 120f);
		}
	}

	[TestMethod]
	public void InEdgeZone_IsTheStripAlongTheEdge()
	{
		Assert.IsTrue(NavigationDrawerState.InEdgeZone(0f, 0f, 20f));
		Assert.IsTrue(NavigationDrawerState.InEdgeZone(20f, 0f, 20f));
		Assert.IsFalse(NavigationDrawerState.InEdgeZone(21f, 0f, 20f));
		Assert.IsFalse(NavigationDrawerState.InEdgeZone(5f, 10f, 20f));
	}

	[TestMethod]
	public void Open_SlidesFullyOut()
	{
		NavigationDrawerState state = new();
		state.Open();
		Settle(state);

		Assert.AreEqual(1f, state.Openness, 1e-3f);
	}

	[TestMethod]
	public void Drag_FollowsThePointer()
	{
		NavigationDrawerState state = new();
		state.BeginDrag();
		state.Drag(Width * 0.25f, Width);

		Assert.AreEqual(0.25f, state.Openness, 1e-5f);
		Assert.IsTrue(state.IsVisible);
	}

	[TestMethod]
	public void SlowRelease_PastHalfway_Opens()
	{
		NavigationDrawerState state = new();
		state.BeginDrag();
		state.Drag(Width * 0.6f, Width);
		state.Release(0f, Width);

		Assert.IsTrue(state.IsOpen);
	}

	[TestMethod]
	public void SlowRelease_ShortOfHalfway_Closes()
	{
		NavigationDrawerState state = new();
		state.BeginDrag();
		state.Drag(Width * 0.4f, Width);
		state.Release(0f, Width);

		Assert.IsFalse(state.IsOpen);
		Assert.IsTrue(state.Update(1f));
	}

	[TestMethod]
	public void Flick_OpensFromAShortDrag()
	{
		NavigationDrawerState state = new();
		state.BeginDrag();
		state.Drag(Width * 0.2f, Width);
		state.Release(1500f, Width);

		Assert.IsTrue(state.IsOpen);
	}

	[TestMethod]
	public void FlickBack_ClosesAnOpenDrawer()
	{
		NavigationDrawerState state = new();
		state.Open();
		Settle(state);

		state.BeginDrag();
		state.Drag(-Width * 0.1f, Width);
		state.Release(-1500f, Width);

		Assert.IsFalse(state.IsOpen);
	}
}
