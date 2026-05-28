// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets.Tests;

using System.Numerics;

using ktsu.ImGui.Widgets.Gestures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class GestureMachineTests
{
	private static GestureSettings DefaultSettings => new();

	private static GestureMachine NewMachine(GestureSettings? settings = null) => new(settings ?? DefaultSettings);

	[TestMethod]
	public void Update_NotPressed_ReportsNoGesture()
	{
		GestureMachine machine = NewMachine();
		GestureResult r = machine.Update(pressed: false, pos: Vector2.Zero, deltaTime: 0.016f);

		Assert.AreEqual(GestureFlags.None, r.Gestures);
		Assert.IsFalse(r.IsPressed);
	}

	[TestMethod]
	public void Update_QuickPressRelease_EmitsTap()
	{
		GestureMachine machine = NewMachine();
		Vector2 pos = new(100, 100);

		GestureResult press = machine.Update(pressed: true, pos, deltaTime: 0.016f);
		Assert.AreEqual(GestureFlags.None, press.Gestures);
		Assert.IsTrue(press.IsPressed);

		GestureResult release = machine.Update(pressed: false, pos, deltaTime: 0.016f);
		Assert.AreEqual(GestureFlags.Tap, release.Gestures);
		Assert.IsTrue(release.Tapped);
		Assert.IsFalse(release.IsPressed);
	}

	[TestMethod]
	public void Update_LongDuration_NoMovement_EmitsLongPress()
	{
		GestureSettings settings = DefaultSettings with { LongPressMinDuration = 0.5f };
		GestureMachine machine = NewMachine(settings);
		Vector2 pos = new(50, 50);

		machine.Update(pressed: true, pos, deltaTime: 0.016f);

		// Hold for 0.6s in 0.1s chunks. Long press should fire exactly once.
		int longPressFireCount = 0;
		for (int i = 0; i < 6; i++)
		{
			GestureResult r = machine.Update(pressed: true, pos, deltaTime: 0.1f);
			if (r.LongPressed)
			{
				longPressFireCount++;
			}
		}

		Assert.AreEqual(1, longPressFireCount, "LongPress must fire exactly once per press.");

		// Releasing after long-press should NOT also emit Tap.
		GestureResult release = machine.Update(pressed: false, pos, deltaTime: 0.016f);
		Assert.IsFalse(release.Tapped, "Releasing after a long-press must not also count as a tap.");
	}

	[TestMethod]
	public void Update_MoveBeyondPanThreshold_EmitsPanStartThenPan()
	{
		GestureSettings settings = DefaultSettings with { PanMinDistance = 5.0f };
		GestureMachine machine = NewMachine(settings);

		machine.Update(pressed: true, pos: new(0, 0), deltaTime: 0.016f);
		GestureResult mid = machine.Update(pressed: true, pos: new(20, 0), deltaTime: 0.016f);

		Assert.IsTrue((mid.Gestures & GestureFlags.PanStart) != 0, "PanStart should fire on the frame the threshold is crossed.");
		Assert.IsTrue((mid.Gestures & GestureFlags.Pan) != 0, "Pan should also be flagged on the start frame.");

		GestureResult next = machine.Update(pressed: true, pos: new(30, 0), deltaTime: 0.016f);
		Assert.IsTrue((next.Gestures & GestureFlags.Pan) != 0);
		Assert.IsTrue((next.Gestures & GestureFlags.PanStart) == 0, "PanStart should only fire once per press.");
	}

	[TestMethod]
	public void Update_ReleaseAfterPan_EmitsPanEnd()
	{
		GestureMachine machine = NewMachine();
		machine.Update(pressed: true, pos: new(0, 0), deltaTime: 0.016f);
		machine.Update(pressed: true, pos: new(50, 0), deltaTime: 0.016f);

		GestureResult release = machine.Update(pressed: false, pos: new(50, 0), deltaTime: 0.016f);
		Assert.IsTrue((release.Gestures & GestureFlags.PanEnd) != 0);
		Assert.IsFalse(release.Tapped, "Pan release should not also be a tap.");
	}

	[TestMethod]
	public void Update_FastRightMovement_EmitsSwipeRight()
	{
		// Cover ~200px in 5 frames at 60Hz = 2400 px/s, well above 600 px/s threshold.
		GestureMachine machine = NewMachine();
		machine.Update(pressed: true, pos: new(0, 0), deltaTime: 0.016f);
		for (int i = 1; i <= 5; i++)
		{
			machine.Update(pressed: true, pos: new(i * 40, 0), deltaTime: 0.016f);
		}

		GestureResult release = machine.Update(pressed: false, pos: new(200, 0), deltaTime: 0.016f);
		Assert.IsTrue((release.Gestures & GestureFlags.SwipeRight) != 0, $"Fast horizontal drag should produce SwipeRight. Velocity={release.Velocity}");
		Assert.IsTrue((release.Gestures & GestureFlags.PanEnd) != 0);
	}

	[TestMethod]
	public void Update_FastUpMovement_EmitsSwipeUp()
	{
		GestureMachine machine = NewMachine();
		machine.Update(pressed: true, pos: new(0, 0), deltaTime: 0.016f);
		for (int i = 1; i <= 5; i++)
		{
			machine.Update(pressed: true, pos: new(0, -i * 40), deltaTime: 0.016f);
		}

		GestureResult release = machine.Update(pressed: false, pos: new(0, -200), deltaTime: 0.016f);
		Assert.IsTrue((release.Gestures & GestureFlags.SwipeUp) != 0, $"Fast upward drag should produce SwipeUp. Velocity={release.Velocity}");
	}

	[TestMethod]
	public void Update_SlowPanRelease_NoSwipe()
	{
		// 60px over 60 frames = 60 px/s, way below the 600 px/s swipe threshold.
		GestureMachine machine = NewMachine();
		machine.Update(pressed: true, pos: new(0, 0), deltaTime: 0.016f);
		for (int i = 1; i <= 60; i++)
		{
			machine.Update(pressed: true, pos: new(i, 0), deltaTime: 0.016f);
		}

		GestureResult release = machine.Update(pressed: false, pos: new(60, 0), deltaTime: 0.016f);
		Assert.IsTrue((release.Gestures & GestureFlags.PanEnd) != 0);
		Assert.AreEqual(GestureFlags.None, release.Gestures & (GestureFlags.SwipeLeft | GestureFlags.SwipeRight | GestureFlags.SwipeUp | GestureFlags.SwipeDown));
	}

	[TestMethod]
	public void Update_TwoQuickTapsAtSamePoint_EmitsDoubleTap()
	{
		GestureMachine machine = NewMachine();
		Vector2 pos = new(40, 40);

		// First tap.
		machine.Update(pressed: true, pos, deltaTime: 0.016f);
		GestureResult first = machine.Update(pressed: false, pos, deltaTime: 0.016f);
		Assert.IsTrue(first.Tapped);

		// Short gap before second tap.
		machine.Update(pressed: false, pos, deltaTime: 0.1f);

		// Second tap at the same point.
		machine.Update(pressed: true, pos, deltaTime: 0.016f);
		GestureResult second = machine.Update(pressed: false, pos, deltaTime: 0.016f);

		Assert.IsTrue(second.DoubleTapped, "Second quick tap at the same point should produce DoubleTap.");
		Assert.IsFalse(second.Tapped, "DoubleTap should not also emit a Tap on the same frame.");
	}

	[TestMethod]
	public void Update_TwoTapsTooFarApart_NoDoubleTap()
	{
		GestureSettings settings = DefaultSettings with { DoubleTapMaxDistance = 10.0f };
		GestureMachine machine = NewMachine(settings);

		machine.Update(pressed: true, pos: new(0, 0), deltaTime: 0.016f);
		machine.Update(pressed: false, pos: new(0, 0), deltaTime: 0.016f);

		machine.Update(pressed: false, pos: new(100, 100), deltaTime: 0.05f);

		machine.Update(pressed: true, pos: new(100, 100), deltaTime: 0.016f);
		GestureResult second = machine.Update(pressed: false, pos: new(100, 100), deltaTime: 0.016f);

		Assert.IsTrue(second.Tapped, "Second tap far from the first should be a fresh Tap, not DoubleTap.");
		Assert.IsFalse(second.DoubleTapped);
	}

	[TestMethod]
	public void Update_TwoTapsWithLongGap_NoDoubleTap()
	{
		GestureSettings settings = DefaultSettings with { DoubleTapMaxInterval = 0.3f };
		GestureMachine machine = NewMachine(settings);

		machine.Update(pressed: true, pos: new(5, 5), deltaTime: 0.016f);
		machine.Update(pressed: false, pos: new(5, 5), deltaTime: 0.016f);

		// Sleep longer than the double-tap interval.
		machine.Update(pressed: false, pos: new(5, 5), deltaTime: 0.5f);

		machine.Update(pressed: true, pos: new(5, 5), deltaTime: 0.016f);
		GestureResult second = machine.Update(pressed: false, pos: new(5, 5), deltaTime: 0.016f);

		Assert.IsTrue(second.Tapped);
		Assert.IsFalse(second.DoubleTapped);
	}

	[TestMethod]
	public void Reset_ClearsActivePress()
	{
		GestureMachine machine = NewMachine();
		machine.Update(pressed: true, pos: new(10, 10), deltaTime: 0.016f);
		Assert.IsTrue(machine.IsPressed);

		machine.Reset();
		Assert.IsFalse(machine.IsPressed);

		// A subsequent release should NOT fire a tap because the press was cleared.
		GestureResult release = machine.Update(pressed: false, pos: new(10, 10), deltaTime: 0.016f);
		Assert.AreEqual(GestureFlags.None, release.Gestures);
	}

	[TestMethod]
	public void Update_NegativeDeltaTime_IsClampedToZero()
	{
		GestureMachine machine = NewMachine();
		// Negative delta should not crash or produce wild velocity.
		machine.Update(pressed: true, pos: new(0, 0), deltaTime: -1.0f);
		GestureResult r = machine.Update(pressed: true, pos: new(10, 0), deltaTime: -0.5f);

		Assert.IsTrue(r.IsPressed);
		// With clamped delta, velocity should be zero (deltaTime==0 branch).
		Assert.AreEqual(Vector2.Zero, r.Velocity);
	}
}
