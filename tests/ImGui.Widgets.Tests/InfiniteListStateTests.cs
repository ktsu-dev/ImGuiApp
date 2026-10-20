// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class InfiniteListStateTests
{
	private const float Threshold = 100f;

	[TestMethod]
	public void FarFromTheEnd_AsksForNothing()
	{
		InfiniteListState state = new(Threshold);
		state.Update(500f, 20, isLoading: false, hasMore: true);

		Assert.IsFalse(state.Update(500f, 20, isLoading: false, hasMore: true));
	}

	[TestMethod]
	public void NearingTheEnd_AsksOnce()
	{
		InfiniteListState state = new(Threshold);
		state.Update(500f, 20, isLoading: false, hasMore: true);

		Assert.IsTrue(state.Update(80f, 20, isLoading: false, hasMore: true));
		Assert.IsFalse(state.Update(60f, 20, isLoading: false, hasMore: true));
		Assert.IsFalse(state.Update(0f, 20, isLoading: false, hasMore: true));
	}

	[TestMethod]
	public void TheFrameItemsArrive_AsksForNothing()
	{
		InfiniteListState state = new(Threshold);

		Assert.IsFalse(state.Update(0f, 20, isLoading: false, hasMore: true));
	}

	[TestMethod]
	public void WhileLoading_AsksForNothing()
	{
		InfiniteListState state = new(Threshold);
		state.Update(0f, 20, isLoading: true, hasMore: true);

		Assert.IsFalse(state.Update(0f, 20, isLoading: true, hasMore: true));
	}

	[TestMethod]
	public void WithNothingMore_AsksForNothing()
	{
		InfiniteListState state = new(Threshold);
		state.Update(0f, 20, isLoading: false, hasMore: false);

		Assert.IsFalse(state.Update(0f, 20, isLoading: false, hasMore: false));
	}

	[TestMethod]
	public void MovingAwayAndBack_AsksAgain()
	{
		InfiniteListState state = new(Threshold);
		state.Update(300f, 20, isLoading: false, hasMore: true);
		Assert.IsTrue(state.Update(50f, 20, isLoading: false, hasMore: true));

		state.Update(300f, 20, isLoading: false, hasMore: true);

		Assert.IsTrue(state.Update(50f, 20, isLoading: false, hasMore: true));
	}

	[TestMethod]
	public void NewItems_StillInsideTheView_AskAgain_OnceTheyAreLaidOut()
	{
		InfiniteListState state = new(Threshold);
		state.Update(0f, 5, isLoading: false, hasMore: true);
		Assert.IsTrue(state.Update(0f, 5, isLoading: false, hasMore: true));
		state.Update(0f, 5, isLoading: true, hasMore: true);

		Assert.IsFalse(state.Update(0f, 10, isLoading: false, hasMore: true), "The frame the items arrive reads a stale scroll range.");
		Assert.IsTrue(state.Update(0f, 10, isLoading: false, hasMore: true));
	}

	[TestMethod]
	public void ALoadThatBroughtNothing_IsNotRepeatedInALoop()
	{
		InfiniteListState state = new(Threshold);
		state.Update(0f, 5, isLoading: false, hasMore: true);
		Assert.IsTrue(state.Update(0f, 5, isLoading: false, hasMore: true));
		state.Update(0f, 5, isLoading: true, hasMore: true);

		Assert.IsFalse(state.Update(0f, 5, isLoading: false, hasMore: true));
	}

	[TestMethod]
	public void ALoadStillPending_WhenTheCallerIsSlowToSaySo_IsNotAskedTwice()
	{
		InfiniteListState state = new(Threshold);
		state.Update(0f, 5, isLoading: false, hasMore: true);
		Assert.IsTrue(state.Update(0f, 5, isLoading: false, hasMore: true));

		Assert.IsFalse(state.Update(0f, 5, isLoading: false, hasMore: true));
	}
}
