// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets.Tests;

using ktsu.ImGui.Widgets.Animation;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class TweenTests
{
	[TestMethod]
	public void New_LinearTween_StartsAtStartValue()
	{
		Tween t = new(0.0f, 100.0f, 1.0f);

		Assert.AreEqual(0.0f, t.Value, 1e-5f);
		Assert.IsTrue(t.IsActive);
	}

	[TestMethod]
	public void Update_LinearTween_AtMidpointReturnsHalfway()
	{
		Tween t = new(0.0f, 100.0f, 1.0f);

		float v = t.Update(0.5f);

		Assert.AreEqual(50.0f, v, 1e-4f);
		Assert.AreEqual(50.0f, t.Value, 1e-4f);
		Assert.IsTrue(t.IsActive);
	}

	[TestMethod]
	public void Update_LinearTween_PastDuration_Clamps()
	{
		Tween t = new(10.0f, 20.0f, 1.0f);

		float v = t.Update(2.0f);

		Assert.AreEqual(20.0f, v, 1e-5f);
		Assert.IsFalse(t.IsActive);
		Assert.IsTrue(t.IsComplete);
	}

	[TestMethod]
	public void Update_LinearTween_ExactlyAtDuration_Completes()
	{
		Tween t = new(0.0f, 1.0f, 1.0f);
		t.Update(1.0f);

		Assert.AreEqual(1.0f, t.Value, 1e-5f);
		Assert.IsFalse(t.IsActive);
	}

	[TestMethod]
	public void New_ZeroDuration_SnapsToEndImmediately()
	{
		Tween t = new(0.0f, 5.0f, 0.0f);

		Assert.AreEqual(5.0f, t.Value, 1e-5f);
		Assert.IsFalse(t.IsActive);
	}

	[TestMethod]
	public void Update_OutCubic_FastStartSlowEnd()
	{
		Tween t = new(0.0f, 100.0f, 1.0f, Easing.OutCubic);

		// At t=0.5, OutCubic = 1 - (0.5)^3 = 1 - 0.125 = 0.875 → 87.5
		t.Update(0.5f);
		Assert.AreEqual(87.5f, t.Value, 1e-3f);
	}

	[TestMethod]
	public void Update_NegativeDelta_DoesNotRewind()
	{
		Tween t = new(0.0f, 10.0f, 1.0f);
		t.Update(0.4f);
		float before = t.Value;
		t.Update(-1.0f);

		Assert.AreEqual(before, t.Value, 1e-5f);
	}

	[TestMethod]
	public void Restart_ResetsToStart()
	{
		Tween t = new(0.0f, 10.0f, 1.0f);
		t.Update(2.0f);
		Assert.IsFalse(t.IsActive);

		t.Restart();

		Assert.AreEqual(0.0f, t.Value, 1e-5f);
		Assert.IsTrue(t.IsActive);
	}

	[TestMethod]
	public void Complete_JumpsToEnd()
	{
		Tween t = new(0.0f, 10.0f, 5.0f);
		t.Complete();

		Assert.AreEqual(10.0f, t.Value, 1e-5f);
		Assert.IsFalse(t.IsActive);
	}

	[TestMethod]
	public void Repeat_WrapsAroundAfterDuration()
	{
		Tween t = new(0.0f, 10.0f, 1.0f) { Repeat = true };

		// After 1.5 cycles → fractional = 0.5 → value = 5
		t.Update(1.5f);

		Assert.AreEqual(5.0f, t.Value, 1e-4f);
		Assert.IsTrue(t.IsActive, "Repeating tween should stay active indefinitely.");
	}

	[TestMethod]
	public void PingPong_ReversesEachCycle()
	{
		Tween t = new(0.0f, 10.0f, 1.0f) { PingPong = true };

		// 0.5s in → forward half → 5.0
		t.Update(0.5f);
		Assert.AreEqual(5.0f, t.Value, 1e-4f);

		// total 1.5s → into second half, reversed at 0.5 → 5.0 again
		t.Update(1.0f);
		Assert.AreEqual(5.0f, t.Value, 1e-4f);

		// total 2.0s → completed reverse half → back at 0
		t.Update(0.5f);
		Assert.AreEqual(0.0f, t.Value, 1e-4f);
	}

	[TestMethod]
	public void SeekTo_JumpsToTimeOffset()
	{
		Tween t = new(0.0f, 100.0f, 2.0f);
		t.SeekTo(1.0f);

		Assert.AreEqual(50.0f, t.Value, 1e-4f);
		Assert.IsTrue(t.IsActive);
	}
}
