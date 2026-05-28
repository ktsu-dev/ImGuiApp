// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets.Tests;

using System;

using ktsu.ImGui.Widgets.Scroll;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class InertialScrollTests
{
	[TestMethod]
	public void New_DefaultsAtRest()
	{
		InertialScroll s = new(initialPosition: 50.0f);

		Assert.AreEqual(50.0f, s.Position, 1e-5f);
		Assert.AreEqual(0.0f, s.Velocity, 1e-5f);
		Assert.IsFalse(s.IsActive);
	}

	[TestMethod]
	public void Drag_AdvancesPosition_AndRecordsInstantaneousVelocity()
	{
		InertialScroll s = new();

		s.Drag(delta: 10.0f, deltaTime: 0.1f);

		Assert.AreEqual(10.0f, s.Position, 1e-5f);
		Assert.AreEqual(100.0f, s.Velocity, 1e-5f);
	}

	[TestMethod]
	public void Drag_ZeroDeltaTime_DoesNotDivideByZero()
	{
		InertialScroll s = new();

		s.Drag(delta: 5.0f, deltaTime: 0.0f);

		Assert.AreEqual(5.0f, s.Position, 1e-5f);
		// Velocity is left at its previous (zero) value rather than producing infinity.
		Assert.AreEqual(0.0f, s.Velocity, 1e-5f);
	}

	[TestMethod]
	public void Fling_AndUpdate_CoastsAndDecays()
	{
		InertialScroll s = new() { Friction = 4.0f, MinVelocity = 0.001f };
		s.Fling(velocity: 200.0f);

		float vBefore = s.Velocity;
		s.Update(0.1f);

		Assert.IsTrue(s.Position > 0.0f, "Coast should advance position.");
		Assert.IsTrue(s.Velocity < vBefore, "Velocity should decay.");
		Assert.IsTrue(s.IsActive, "Scroll should still be coasting after one short update.");
	}

	[TestMethod]
	public void Update_NoFling_DoesNothing()
	{
		InertialScroll s = new(initialPosition: 10.0f);

		s.Update(1.0f);

		Assert.AreEqual(10.0f, s.Position, 1e-5f);
		Assert.IsFalse(s.IsActive);
	}

	[TestMethod]
	public void Update_NegativeDelta_DoesNothing()
	{
		InertialScroll s = new();
		s.Fling(100.0f);
		float p = s.Position;
		float v = s.Velocity;

		s.Update(-0.1f);

		Assert.AreEqual(p, s.Position, 1e-5f);
		Assert.AreEqual(v, s.Velocity, 1e-5f);
	}

	[TestMethod]
	public void Update_RunsUntilRest()
	{
		InertialScroll s = new() { Friction = 8.0f, MinVelocity = 0.5f };
		s.Fling(500.0f);

		// 5 seconds is plenty for friction=8 to settle.
		for (int i = 0; i < 300; i++)
		{
			s.Update(0.016f);
		}

		Assert.IsFalse(s.IsActive, $"Should be at rest. Velocity={s.Velocity}");
		Assert.AreEqual(0.0f, s.Velocity, 1e-5f);
	}

	[TestMethod]
	public void Update_ClampsAtMaxExtent_AndZerosVelocity()
	{
		InertialScroll s = new() { MaxExtent = 100.0f };
		s.Fling(10_000.0f);

		// One big step should drive past the bound.
		s.Update(1.0f);

		Assert.AreEqual(100.0f, s.Position, 1e-3f);
		Assert.AreEqual(0.0f, s.Velocity, 1e-5f);
		Assert.IsFalse(s.IsActive);
	}

	[TestMethod]
	public void Update_ClampsAtMinExtent_AndZerosVelocity()
	{
		InertialScroll s = new() { MinExtent = -50.0f };
		s.Fling(-10_000.0f);

		s.Update(1.0f);

		Assert.AreEqual(-50.0f, s.Position, 1e-3f);
		Assert.AreEqual(0.0f, s.Velocity, 1e-5f);
	}

	[TestMethod]
	public void Drag_ClampsToBounds()
	{
		InertialScroll s = new() { MaxExtent = 20.0f };

		s.Drag(delta: 100.0f, deltaTime: 0.1f);

		Assert.AreEqual(20.0f, s.Position, 1e-5f);
		// Velocity zeroed because we hit the bound during the drag.
		Assert.AreEqual(0.0f, s.Velocity, 1e-5f);
	}

	[TestMethod]
	public void Stop_KeepsPosition_ClearsVelocity()
	{
		InertialScroll s = new();
		s.Fling(500.0f);
		s.Update(0.05f);
		float p = s.Position;

		s.Stop();

		Assert.AreEqual(p, s.Position, 1e-5f);
		Assert.AreEqual(0.0f, s.Velocity, 1e-5f);
		Assert.IsFalse(s.IsActive);
	}

	[TestMethod]
	public void SnapTo_ClampsAndClearsVelocity()
	{
		InertialScroll s = new() { MaxExtent = 100.0f };
		s.Fling(50.0f);

		s.SnapTo(250.0f);

		Assert.AreEqual(100.0f, s.Position, 1e-5f);
		Assert.AreEqual(0.0f, s.Velocity, 1e-5f);
	}

	[TestMethod]
	public void Update_PositionIntegration_IsMonotonicWhileCoasting()
	{
		InertialScroll s = new() { Friction = 3.0f, MinVelocity = 0.1f };
		s.Fling(300.0f);

		float prev = s.Position;
		for (int i = 0; i < 60 && s.IsActive; i++)
		{
			s.Update(0.016f);
			Assert.IsTrue(s.Position >= prev - 1e-4f, $"Position regressed: prev={prev} curr={s.Position} on step {i}");
			prev = s.Position;
		}
	}

	[TestMethod]
	public void Friction_Higher_StopsSooner()
	{
		InertialScroll slow = new() { Friction = 2.0f, MinVelocity = 0.5f };
		InertialScroll fast = new() { Friction = 12.0f, MinVelocity = 0.5f };
		slow.Fling(500.0f);
		fast.Fling(500.0f);

		int slowSteps = 0;
		int fastSteps = 0;
		for (int i = 0; i < 600; i++)
		{
			if (slow.IsActive)
			{
				slow.Update(0.016f);
				slowSteps++;
			}

			if (fast.IsActive)
			{
				fast.Update(0.016f);
				fastSteps++;
			}

			if (!slow.IsActive && !fast.IsActive)
			{
				break;
			}
		}

		Assert.IsTrue(fastSteps < slowSteps, $"Higher friction should settle sooner. slow={slowSteps} fast={fastSteps}");
	}

	[TestMethod]
	public void Drag_ThenFling_ResumesCoastingFromCurrentVelocity()
	{
		InertialScroll s = new() { Friction = 4.0f, MinVelocity = 0.1f };
		s.Drag(20.0f, 0.1f);  // velocity = 200
		float posAfterDrag = s.Position;

		s.Fling(s.Velocity);
		s.Update(0.1f);

		Assert.IsTrue(s.Position > posAfterDrag, "Coast should advance position past the drag end.");
	}

	[TestMethod]
	public void Update_LargeDeltaTime_StaysFinite()
	{
		InertialScroll s = new();
		s.Fling(1_000.0f);

		float result = s.Update(10.0f);

		Assert.IsFalse(float.IsNaN(result));
		Assert.IsFalse(float.IsInfinity(result));
	}
}
