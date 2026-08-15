// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the pure frame-tracking logic behind the deferred-drawing pumps. The pumps themselves
/// need a live ImGui context and are verified visually in the demos.
/// </summary>
[TestClass]
public sealed class DeferredDrawingTests
{
	[TestMethod]
	public void EvaluatePump_FirstCallEver_ReportsOk()
	{
		ImGuiWidgets.PumpTracker tracker = default;

		Assert.AreEqual(ImGuiWidgets.PumpState.Ok, ImGuiWidgets.EvaluatePump(0, ref tracker));
	}

	[TestMethod]
	public void EvaluatePump_SecondCallInSameFrame_ReportsDoublePumped()
	{
		ImGuiWidgets.PumpTracker tracker = default;
		_ = ImGuiWidgets.EvaluatePump(5, ref tracker);

		Assert.AreEqual(ImGuiWidgets.PumpState.DoublePumped, ImGuiWidgets.EvaluatePump(5, ref tracker));
	}

	[TestMethod]
	public void EvaluatePump_AdvancingFrames_ReportsOkEachTime()
	{
		ImGuiWidgets.PumpTracker tracker = default;

		for (int frame = 0; frame < 5; frame++)
		{
			Assert.AreEqual(ImGuiWidgets.PumpState.Ok, ImGuiWidgets.EvaluatePump(frame, ref tracker));
		}
	}

	[TestMethod]
	public void EvaluatePump_FrameCounterResets_ReportsOk()
	{
		// A recreated ImGui context restarts the frame counter; a >= comparison would stall here.
		ImGuiWidgets.PumpTracker tracker = default;
		_ = ImGuiWidgets.EvaluatePump(500, ref tracker);

		Assert.AreEqual(ImGuiWidgets.PumpState.Ok, ImGuiWidgets.EvaluatePump(0, ref tracker));
	}

	[TestMethod]
	public void HasEverPumped_DefaultTracker_IsFalse()
	{
		ImGuiWidgets.PumpTracker tracker = default;

		Assert.IsFalse(tracker.HasEverPumped);
	}

	[TestMethod]
	public void HasEverPumped_AfterFirstPump_IsTrue()
	{
		ImGuiWidgets.PumpTracker tracker = default;
		_ = ImGuiWidgets.EvaluatePump(0, ref tracker);

		Assert.IsTrue(tracker.HasEverPumped);
	}

	[TestMethod]
	public void EvaluateFallbackTick_PumpNeverRun_Ticks()
	{
		ImGuiWidgets.PumpTracker tracker = default;

		Assert.IsTrue(ImGuiWidgets.EvaluateFallbackTick(0, pumpHasEverRun: false, ref tracker));
	}

	[TestMethod]
	public void EvaluateFallbackTick_PumpHasRun_DoesNotTick()
	{
		ImGuiWidgets.PumpTracker tracker = default;

		Assert.IsFalse(ImGuiWidgets.EvaluateFallbackTick(0, pumpHasEverRun: true, ref tracker));
	}

	[TestMethod]
	public void EvaluateFallbackTick_SecondCallInSameFrame_TicksOnce()
	{
		ImGuiWidgets.PumpTracker tracker = default;
		_ = ImGuiWidgets.EvaluateFallbackTick(5, pumpHasEverRun: false, ref tracker);

		Assert.IsFalse(ImGuiWidgets.EvaluateFallbackTick(5, pumpHasEverRun: false, ref tracker));
	}
}
