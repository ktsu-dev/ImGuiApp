// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class AppBarStateTests
{
	private const float Height = 56f;

	[TestMethod]
	public void ScrollingDown_HidesTheBarByAsMuch()
	{
		AppBarState state = new();
		state.Track(0f, Height, 0.016f);

		Assert.AreEqual(Height - 20f, state.Track(20f, Height, 0.016f));
		Assert.AreEqual(0f, state.Track(200f, Height, 0.016f));
	}

	[TestMethod]
	public void ScrollingUp_BringsItBack_FromAnywhere()
	{
		AppBarState state = new();
		state.Track(0f, Height, 0.016f);
		state.Track(500f, Height, 0.016f);

		Assert.AreEqual(30f, state.Track(470f, Height, 0.016f));
	}

	[TestMethod]
	public void AtTheTop_TheBarIsFullyShown()
	{
		AppBarState state = new();
		state.Track(0f, Height, 0.016f);
		state.Track(30f, Height, 0.016f);

		Assert.AreEqual(Height, state.Track(0f, Height, 0.016f));
	}

	[TestMethod]
	public void AHalfHiddenBar_SettlesOnceTheScrollRests()
	{
		AppBarState state = new();
		state.Track(100f, Height, 0.016f);
		state.Track(140f, Height, 0.016f);
		Assert.AreEqual(40f, state.Hidden);

		float showing = Height;
		for (int i = 0; i < 60; i++)
		{
			showing = state.Track(140f, Height, 0.016f);
		}

		Assert.AreEqual(0f, showing);
	}

	[TestMethod]
	public void AMostlyShownBar_SettlesShown()
	{
		AppBarState state = new();
		state.Track(100f, Height, 0.016f);
		state.Track(110f, Height, 0.016f);

		float showing = 0f;
		for (int i = 0; i < 60; i++)
		{
			showing = state.Track(110f, Height, 0.016f);
		}

		Assert.AreEqual(Height, showing);
	}
}
