// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class ExpandableStateTests
{
	private const float Content = 200f;

	[TestMethod]
	public void StartsAtItsOwnHeight_WhenOpen_AndAtNothing_WhenClosed()
	{
		ExpandableState open = new(open: true);
		open.Measure(Content);
		ExpandableState closed = new(open: false);
		closed.Measure(Content);

		Assert.AreEqual(Content, open.Height, 1e-4f);
		Assert.AreEqual(0f, closed.Height);
		Assert.IsFalse(closed.IsBodyVisible);
	}

	[TestMethod]
	public void Opening_GrowsTheBody_ThenStops()
	{
		ExpandableState state = new(open: false);
		state.Measure(Content);
		state.SetOpen(true);

		float previous = state.Height;
		for (int i = 0; i < 6; i++)
		{
			state.Update(1f / 60f);
			Assert.IsTrue(state.Height > previous, $"Frame {i} did not grow the body.");
			previous = state.Height;
		}

		state.Update(1f);
		Assert.AreEqual(Content, state.Height, 1e-4f);
		Assert.IsFalse(state.IsAnimating);
	}

	[TestMethod]
	public void Opening_TakesTheDuration()
	{
		ExpandableState state = new(open: false);
		state.Measure(Content);
		state.SetOpen(true);

		state.Update(ExpandableState.Duration * 0.5f);
		Assert.IsTrue(state.IsAnimating);
		Assert.IsTrue(state.Height is > 0f and < Content);

		state.Update(ExpandableState.Duration * 0.5f);
		Assert.IsFalse(state.IsAnimating);
	}

	[TestMethod]
	public void Closing_ShrinksTheBody_UntilItIsGone()
	{
		ExpandableState state = new(open: true);
		state.Measure(Content);
		state.SetOpen(false);

		state.Update(ExpandableState.Duration * 0.5f);
		Assert.IsTrue(state.IsBodyVisible);
		Assert.IsTrue(state.Height < Content);

		state.Update(ExpandableState.Duration);
		Assert.IsFalse(state.IsBodyVisible);
		Assert.AreEqual(0f, state.Height);
	}

	[TestMethod]
	public void Reversing_MidWay_TurnsBackFromWhereItIs()
	{
		ExpandableState state = new(open: false);
		state.Measure(Content);
		state.SetOpen(true);
		state.Update(ExpandableState.Duration * 0.5f);
		float midway = state.Height;

		state.SetOpen(false);
		state.Update(1f / 60f);

		Assert.IsTrue(state.Height < midway);
		Assert.IsTrue(state.Height > 0f);
	}

	[TestMethod]
	public void ContentThatGrows_IsFollowed()
	{
		ExpandableState state = new(open: true);
		state.Measure(Content);
		state.Measure(Content * 2f);

		Assert.AreEqual(Content * 2f, state.Height, 1e-4f);
	}
}
