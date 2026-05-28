// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets.Tests;

using ktsu.ImGui.Widgets.Animation;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class EasingTests
{
	[TestMethod]
	public void All_Curves_HitZeroAndOneAtEndpoints()
	{
		System.Func<float, float>[] curves =
		[
			Easing.Linear,
			Easing.InQuad, Easing.OutQuad, Easing.InOutQuad,
			Easing.InCubic, Easing.OutCubic, Easing.InOutCubic,
		];

		foreach (System.Func<float, float> curve in curves)
		{
			Assert.AreEqual(0.0f, curve(0.0f), 1e-5f, "Curve must return 0 at t=0");
			Assert.AreEqual(1.0f, curve(1.0f), 1e-5f, "Curve must return 1 at t=1");
		}
	}

	[TestMethod]
	public void Linear_IsIdentity()
	{
		Assert.AreEqual(0.25f, Easing.Linear(0.25f), 1e-5f);
		Assert.AreEqual(0.5f, Easing.Linear(0.5f), 1e-5f);
	}

	[TestMethod]
	public void OutCubic_FastInitialProgress()
	{
		// OutCubic at 0.25 → 1 - (0.75)^3 = 1 - 0.421875 = 0.578125
		Assert.AreEqual(0.578125f, Easing.OutCubic(0.25f), 1e-5f);
	}

	[TestMethod]
	public void InCubic_SlowInitialProgress()
	{
		// InCubic at 0.25 → 0.015625
		Assert.AreEqual(0.015625f, Easing.InCubic(0.25f), 1e-5f);
	}

	[TestMethod]
	public void OutBack_Overshoots()
	{
		// OutBack peaks above 1 before settling. At t ≈ 0.65 it should exceed 1.
		float v = Easing.OutBack(0.65f);
		Assert.IsTrue(v > 1.0f, $"OutBack should overshoot 1 mid-curve. Got {v}");
	}

	[TestMethod]
	public void OutBack_LandsAtOne()
	{
		Assert.AreEqual(1.0f, Easing.OutBack(1.0f), 1e-5f);
	}
}
