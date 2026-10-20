// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using ktsu.ImGui.Widgets.Overlays;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class OverlayTransitionTests
{
	[TestMethod]
	public void Open_ComesInOverTheDuration()
	{
		OverlayTransition transition = new(0.2f);
		transition.Open();

		transition.Update(0.1f);
		Assert.AreEqual(0.5f, transition.Progress, 1e-5f);
		Assert.IsTrue(transition.IsAnimating);

		transition.Update(0.2f);
		Assert.AreEqual(1f, transition.Progress);
		Assert.IsFalse(transition.IsAnimating);
	}

	[TestMethod]
	public void Close_ReportsTheUpdateItFinishesOn_Once()
	{
		OverlayTransition transition = new(0.2f);
		transition.Snap(true);
		transition.Close();

		Assert.IsFalse(transition.Update(0.1f));
		Assert.IsTrue(transition.Update(0.1f));
		Assert.IsFalse(transition.IsVisible);
		Assert.IsFalse(transition.Update(0.1f));
	}

	[TestMethod]
	public void Reopen_WhileClosing_TurnsAroundWhereItIs()
	{
		OverlayTransition transition = new(0.2f);
		transition.Snap(true);
		transition.Close();
		transition.Update(0.05f);

		transition.Open();
		transition.Update(0f);

		Assert.AreEqual(0.75f, transition.Progress, 1e-5f);
		Assert.IsTrue(transition.IsOpen);
	}
}
