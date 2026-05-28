// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets.Tests;

using System;

using ktsu.ImGui.Widgets.Animation;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class SpringTests
{
	[TestMethod]
	public void New_RestsAtInitial()
	{
		Spring s = new(initial: 5.0f);

		Assert.AreEqual(5.0f, s.Value, 1e-5f);
		Assert.AreEqual(5.0f, s.Target, 1e-5f);
		Assert.AreEqual(0.0f, s.Velocity, 1e-5f);
		Assert.IsFalse(s.IsActive);
	}

	[TestMethod]
	public void Update_NewTarget_BecomesActive()
	{
		Spring s = new(initial: 0.0f) { Target = 10.0f };

		s.Update(0.016f);

		Assert.IsTrue(s.IsActive);
		Assert.IsTrue(s.Value > 0.0f, $"Spring should have moved toward target. Value={s.Value}");
	}

	[TestMethod]
	public void Update_SettlesAtTarget_GivenEnoughTime()
	{
		Spring s = new(initial: 0.0f, stiffness: 170.0f, damping: 26.0f) { Target = 100.0f };

		// Advance for two seconds in 16ms ticks; nearly-critically-damped should settle.
		for (int i = 0; i < 125; i++)
		{
			s.Update(0.016f);
		}

		Assert.IsFalse(s.IsActive, $"Spring should be at rest. Value={s.Value}, Vel={s.Velocity}");
		Assert.AreEqual(100.0f, s.Value, 1e-3f);
	}

	[TestMethod]
	public void Update_LargeDelta_StaysStable()
	{
		// A single huge dt would blow up a naive explicit Euler; sub-stepping should keep it bounded.
		Spring s = new(initial: 0.0f, stiffness: 500.0f, damping: 10.0f) { Target = 100.0f };

		float result = s.Update(1.0f);

		Assert.IsFalse(float.IsNaN(result));
		Assert.IsFalse(float.IsInfinity(result));
		Assert.IsTrue(MathF.Abs(result) < 1e4f, $"Spring exploded: {result}");
	}

	[TestMethod]
	public void Update_NegativeDelta_DoesNothing()
	{
		Spring s = new(initial: 0.0f) { Target = 10.0f };
		s.Update(0.016f);
		float before = s.Value;

		s.Update(-1.0f);

		Assert.AreEqual(before, s.Value, 1e-5f);
	}

	[TestMethod]
	public void SnapTo_ClearsVelocityAndRests()
	{
		Spring s = new(initial: 0.0f) { Target = 100.0f };
		s.Update(0.016f);
		Assert.IsTrue(s.IsActive);

		s.SnapTo(50.0f);

		Assert.AreEqual(50.0f, s.Value, 1e-5f);
		Assert.AreEqual(50.0f, s.Target, 1e-5f);
		Assert.AreEqual(0.0f, s.Velocity, 1e-5f);
		Assert.IsFalse(s.IsActive);
	}

	[TestMethod]
	public void Update_OverdampedSpring_DoesNotOvershoot()
	{
		// Heavily over-damped: damping > 2 sqrt(k) = 2 * sqrt(100) = 20
		Spring s = new(initial: 0.0f, stiffness: 100.0f, damping: 100.0f) { Target = 10.0f };

		float maxObserved = 0.0f;
		for (int i = 0; i < 300; i++)
		{
			s.Update(0.016f);
			maxObserved = MathF.Max(maxObserved, s.Value);
		}

		Assert.IsTrue(maxObserved <= 10.0f + 1e-3f, $"Over-damped spring should not overshoot. Max={maxObserved}");
	}
}
