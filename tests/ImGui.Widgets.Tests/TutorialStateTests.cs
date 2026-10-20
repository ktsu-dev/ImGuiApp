// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class TutorialStateTests
{
	[TestMethod]
	public void Start_ShowsTheFirstStep()
	{
		TutorialState state = new();

		Assert.IsTrue(state.Start(3));
		Assert.IsTrue(state.IsActive);
		Assert.AreEqual(0, state.Index);
	}

	[TestMethod]
	public void Start_WithNoSteps_DoesNothing()
	{
		TutorialState state = new();

		Assert.IsFalse(state.Start(0));
		Assert.IsFalse(state.IsActive);
	}

	[TestMethod]
	public void Next_StepsThrough_AndFinishesAfterTheLast()
	{
		TutorialState state = new();
		state.Start(3);

		Assert.IsFalse(state.Next());
		Assert.AreEqual(1, state.Index);
		Assert.IsFalse(state.Next());
		Assert.IsTrue(state.IsLastStep);
		Assert.IsTrue(state.Next());
		Assert.IsFalse(state.IsActive);
	}

	[TestMethod]
	public void Next_AfterFinishing_DoesNothing()
	{
		TutorialState state = new();
		state.Start(1);
		state.Next();

		Assert.IsFalse(state.Next());
	}

	[TestMethod]
	public void Skip_EndsAtOnce_AndOnlyOnce()
	{
		TutorialState state = new();
		state.Start(3);

		Assert.IsTrue(state.Skip());
		Assert.IsFalse(state.IsActive);
		Assert.IsFalse(state.Skip());
	}

	[TestMethod]
	public void Back_StopsAtTheFirstStep()
	{
		TutorialState state = new();
		state.Start(3, index: 1);

		Assert.IsTrue(state.Back());
		Assert.AreEqual(0, state.Index);
		Assert.IsFalse(state.Back());
	}

	[TestMethod]
	public void Update_ReportsTheFadeOutFinishing()
	{
		TutorialState state = new();
		state.Start(1);
		state.Update(1f);
		state.Skip();

		Assert.IsTrue(state.Update(1f));
	}

	[TestMethod]
	public void Spotlight_LightsTheFirstTargetWhereItIs()
	{
		TutorialState state = new();
		state.Start(2);

		state.TrackSpotlight((new Vector2(10f, 20f), new Vector2(50f, 40f)), 1f / 60f);

		Assert.IsTrue(state.HasSpotlight);
		Assert.AreEqual(new Vector2(10f, 20f), state.SpotlightMin);
	}

	[TestMethod]
	public void Spotlight_GlidesToTheNextTarget_AndArrives()
	{
		TutorialState state = new();
		state.Start(2);
		state.TrackSpotlight((new Vector2(10f, 20f), new Vector2(50f, 40f)), 1f / 60f);

		(Vector2, Vector2) next = (new Vector2(200f, 100f), new Vector2(260f, 130f));
		state.TrackSpotlight(next, 1f / 60f);
		Assert.IsTrue(state.SpotlightMin.X is > 10f and < 200f);

		for (int i = 0; i < 60; i++)
		{
			state.TrackSpotlight(next, 1f / 60f);
		}

		Assert.AreEqual(new Vector2(200f, 100f), state.SpotlightMin);
		Assert.AreEqual(new Vector2(260f, 130f), state.SpotlightMax);
	}

	[TestMethod]
	public void Spotlight_GoesOut_WhenTheTargetIsNotDrawn()
	{
		TutorialState state = new();
		state.Start(1);
		state.TrackSpotlight((Vector2.Zero, Vector2.One), 1f / 60f);

		state.TrackSpotlight(null, 1f / 60f);

		Assert.IsFalse(state.HasSpotlight);
	}
}
