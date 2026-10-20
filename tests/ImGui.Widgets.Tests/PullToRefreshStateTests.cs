// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class PullToRefreshStateTests
{
	private const float Threshold = 50f;

	[TestMethod]
	public void Resist_OpensByHalfAtFirst_AndNeverPastTwiceTheThreshold()
	{
		Assert.AreEqual(0f, PullToRefreshState.Resist(0f, Threshold));
		Assert.AreEqual(0.5f, PullToRefreshState.Resist(1f, Threshold), 0.01f);
		Assert.AreEqual(Threshold, PullToRefreshState.Resist(4f * Threshold, Threshold), 1e-3f);
		Assert.IsTrue(PullToRefreshState.Resist(100_000f, Threshold) < 2f * Threshold);
	}

	[TestMethod]
	public void Pull_ArmsAtTheThreshold()
	{
		PullToRefreshState state = new(Threshold);

		state.Pull(3f * Threshold);
		Assert.AreEqual(PullPhase.Pulling, state.Phase);

		state.Pull(4f * Threshold);
		Assert.AreEqual(PullPhase.Armed, state.Phase);
	}

	[TestMethod]
	public void Release_ShortOfTheThreshold_SpringsBack()
	{
		PullToRefreshState state = new(Threshold);
		state.Pull(2f * Threshold);

		Assert.IsFalse(state.Release());
		Settle(state, isRefreshing: false);

		Assert.AreEqual(PullPhase.Idle, state.Phase);
		Assert.AreEqual(0f, state.Offset);
	}

	[TestMethod]
	public void Release_Armed_HoldsOpenWhileTheCallerRefreshes()
	{
		PullToRefreshState state = new(Threshold);
		state.Pull(6f * Threshold);

		Assert.IsTrue(state.Release());
		Settle(state, isRefreshing: true);

		Assert.AreEqual(PullPhase.Refreshing, state.Phase);
		Assert.AreEqual(Threshold, state.Offset, 0.5f);

		Settle(state, isRefreshing: false);
		Assert.AreEqual(PullPhase.Idle, state.Phase);
		Assert.AreEqual(0f, state.Offset);
	}

	[TestMethod]
	public void Release_Armed_ClosesAfterTheGraceWhenTheCallerNeverRefreshes()
	{
		PullToRefreshState state = new(Threshold);
		state.Pull(6f * Threshold);
		_ = state.Release();

		state.Update(PullToRefreshState.StartGrace * 0.5f, isRefreshing: false);
		Assert.AreEqual(PullPhase.Refreshing, state.Phase, "The region closed before the caller had a frame to start refreshing.");

		Settle(state, isRefreshing: false);
		Assert.AreEqual(PullPhase.Idle, state.Phase);
	}

	[TestMethod]
	public void Update_RefreshStartedByTheCaller_OpensTheRegion()
	{
		PullToRefreshState state = new(Threshold);

		Settle(state, isRefreshing: true);

		Assert.AreEqual(PullPhase.Refreshing, state.Phase);
		Assert.AreEqual(Threshold, state.Offset, 0.5f);
	}

	[TestMethod]
	public void Pull_WhileRefreshing_IsIgnored()
	{
		PullToRefreshState state = new(Threshold);
		Settle(state, isRefreshing: true);

		state.Pull(10f);

		Assert.AreEqual(PullPhase.Refreshing, state.Phase);
	}

	private static void Settle(PullToRefreshState state, bool isRefreshing)
	{
		for (int i = 0; i < 120; i++)
		{
			state.Update(1f / 60f, isRefreshing);
		}
	}
}
