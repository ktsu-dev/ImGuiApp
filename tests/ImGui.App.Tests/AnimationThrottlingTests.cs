// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

using ktsu.ImGui.Probes;
using ktsu.ImGui.Widgets.Animation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// An animation in flight keeps the application at its active frame rate past the idle timeout, and
/// the rate drops back once the animation ends (#485).
/// </summary>
[TestClass]
public sealed class AnimationThrottlingTests
{
	private const double FocusedFps = 60.0;
	private const double IdleFps = 10.0;

	[TestInitialize]
	public void Setup()
	{
		ImGuiApp.Reset();
		_ = AnimationActivity.TakeRequest();

		// The idle timeout is zero, so the application is idle from the first frame unless something
		// keeps it active. There is no window in a unit test, so it reports as not visible; the
		// not-visible rate is set above the others to keep it out of the choice.
		ImGuiApp.Config = new ImGuiAppConfig
		{
			Title = "Test",
			PerformanceSettings = new ImGuiAppPerformanceSettings
			{
				FocusedFps = FocusedFps,
				UnfocusedFps = FocusedFps,
				IdleFps = IdleFps,
				NotVisibleFps = 1000.0,
				EnableIdleDetection = true,
				IdleTimeoutSeconds = 0.0,
			},
		};
	}

	[TestCleanup]
	public void Cleanup()
	{
		_ = AnimationActivity.TakeRequest();
		ImGuiApp.Reset();
	}

	[TestMethod]
	public void WithNothingAnimating_PastTheIdleTimeout_UsesTheIdleRate()
	{
		ImGuiApp.UpdateWindowPerformance();

		Assert.IsTrue(ImGuiApp.IsIdle);
		Assert.AreEqual(1000.0 / IdleFps, ImGuiApp.targetFrameTimeMs, 0.0001);
	}

	[TestMethod]
	public void AnActiveTween_KeepsTheActiveRate_UntilItCompletes()
	{
		Tween tween = new(0.0f, 1.0f, 0.5f);

		// Each frame the throttler runs, then the frame draws and the tween advances.
		for (int frame = 0; frame < 4; frame++)
		{
			_ = tween.Update(0.1f);
			Assert.IsTrue(tween.IsActive);

			ImGuiApp.UpdateWindowPerformance();

			Assert.IsFalse(ImGuiApp.IsIdle, $"Frame {frame}: a running tween should keep the application out of idle.");
			Assert.AreEqual(1000.0 / FocusedFps, ImGuiApp.targetFrameTimeMs, 0.0001);
		}

		_ = tween.Update(1.0f);
		Assert.IsFalse(tween.IsActive);

		// The frame that finished the tween requested nothing, so the next one drops back.
		ImGuiApp.UpdateWindowPerformance();

		Assert.IsTrue(ImGuiApp.IsIdle);
		Assert.AreEqual(1000.0 / IdleFps, ImGuiApp.targetFrameTimeMs, 0.0001);
	}

	[TestMethod]
	public void ARequest_LastsOneFrame()
	{
		AnimationActivity.RequestActiveFrame();

		ImGuiApp.UpdateWindowPerformance();
		Assert.IsFalse(ImGuiApp.IsIdle);

		ImGuiApp.UpdateWindowPerformance();
		Assert.IsTrue(ImGuiApp.IsIdle);
	}
}
