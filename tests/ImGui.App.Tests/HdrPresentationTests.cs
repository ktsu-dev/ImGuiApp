// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

using System;
using System.Numerics;

using ktsu.ImGui.App.ImGuiController.Hdr;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// The window's HDR presentation from frame to frame, over a stand-in for the half-float frame: when
/// the screen is asked what it shows, and when a presenter that keeps refusing frames is given up.
/// </summary>
[TestClass]
public sealed class HdrPresentationTests
{
	private static readonly HdrDisplayInfo HdrScreen = HdrDisplayInfo.FromLuminance(200, 1000);

	[TestCleanup]
	public void Cleanup() => ImGuiApp.Reset();

	[TestMethod]
	public void TheScreen_IsAskedOnTheFirstFrameAndThenOnceAnInterval()
	{
		ManualClock clock = new();
		using FakeFrameTarget target = new() { Screen = HdrScreen };
		using HdrPresentation presentation = new(target, clock);

		presentation.BeginFrame(64, 32, Vector4.One);
		Assert.AreEqual(1, target.Presenter.Queries);
		Assert.AreEqual(HdrScreen, presentation.Display);
		Assert.AreEqual(HdrScreen, target.Display, "The last pass wasn't told what the screen shows.");
		Assert.AreEqual((64, 32), target.LastFrameSize);

		target.Screen = HdrDisplayInfo.Sdr;
		clock.Advance(TimeSpan.FromMilliseconds(500));
		presentation.BeginFrame(64, 32, Vector4.One);
		Assert.AreEqual(1, target.Presenter.Queries, "The screen was asked again inside the interval.");
		Assert.AreEqual(HdrScreen, presentation.Display);

		clock.Advance(TimeSpan.FromMilliseconds(500));
		presentation.BeginFrame(64, 32, Vector4.One);
		Assert.AreEqual(2, target.Presenter.Queries);
		Assert.AreEqual(HdrDisplayInfo.Sdr, presentation.Display);
	}

	[TestMethod]
	public void AForcedRefresh_AsksWhateverTheInterval()
	{
		ManualClock clock = new();
		using FakeFrameTarget target = new() { Screen = HdrScreen };
		using HdrPresentation presentation = new(target, clock);

		presentation.Refresh(force: false);
		target.Screen = HdrDisplayInfo.FromLuminance(100, 1600);
		Assert.AreEqual(target.Screen, presentation.Refresh(force: true));
		Assert.AreEqual(2, target.Presenter.Queries);
	}

	[TestMethod]
	public void APresenter_IsGivenUpOnlyAfterRefusingThreeFramesInARow()
	{
		using FakeFrameTarget target = new();
		using HdrPresentation presentation = new(target, new ManualClock());

		target.Accepts = false;
		Assert.IsTrue(presentation.EndFrame());
		Assert.IsTrue(presentation.EndFrame());
		target.Accepts = true;
		Assert.IsTrue(presentation.EndFrame(), "A frame that went through didn't reset the count.");

		target.Accepts = false;
		Assert.IsTrue(presentation.EndFrame());
		Assert.IsTrue(presentation.EndFrame());
		Assert.IsFalse(presentation.EndFrame());
	}

	[TestMethod]
	public void Disposing_DisposesTheFrame()
	{
		using FakeFrameTarget target = new();
		HdrPresentation presentation = new(target, new ManualClock());
		Assert.AreSame(target.Presenter, presentation.Presenter);

		presentation.Dispose();

		Assert.IsTrue(target.Disposed);
	}

	[TestMethod]
	public void TheApp_FollowsTheScreen_AndGoesBackToSdrWhenThePresenterGivesUp()
	{
		ImGuiApp.Reset();
		using FakeFrameTarget target = new() { Screen = HdrScreen };
		ImGuiApp.hdrPresentation = new HdrPresentation(target, new ManualClock());
		Assert.IsTrue(ImGuiApp.IsHdrOutputActive);

		ImGuiApp.RefreshHdrDisplay();
		Assert.AreEqual(HdrScreen, ImGuiApp.HdrDisplay);

		target.Accepts = false;
		ImGuiApp.EndPresentedFrame();
		ImGuiApp.EndPresentedFrame();
		Assert.IsTrue(ImGuiApp.IsHdrOutputActive);
		ImGuiApp.EndPresentedFrame();

		Assert.IsFalse(ImGuiApp.IsHdrOutputActive);
		Assert.AreEqual(HdrDisplayInfo.Sdr, ImGuiApp.HdrDisplay);
		Assert.IsTrue(target.Disposed);

		ImGuiApp.RefreshHdrDisplay();
		ImGuiApp.EndPresentedFrame();
		Assert.AreEqual(HdrDisplayInfo.Sdr, ImGuiApp.HdrDisplay, "With no presentation there is nothing to ask.");
	}

	private sealed class ManualClock : TimeProvider
	{
		private long ticks;

		public override long TimestampFrequency => TimeSpan.TicksPerSecond;

		public override long GetTimestamp() => ticks;

		public void Advance(TimeSpan by) => ticks += by.Ticks;
	}

	private sealed class FakePresenter : IHdrPresenter
	{
		public HdrDisplayInfo Screen { get; set; } = HdrDisplayInfo.Sdr;

		public int Queries { get; private set; }

		public bool FlipsVertically => false;

		public HdrDisplayInfo QueryDisplay()
		{
			Queries++;
			return Screen;
		}

		public (float Scale, float Limit) OutputRange(HdrDisplayInfo display) => (1f, (float)display.Headroom);

		public bool BeginOutput(int frameWidth, int frameHeight, out uint framebuffer)
		{
			framebuffer = 0;
			return true;
		}

		public void EndOutput()
		{
		}

		public void Dispose()
		{
		}
	}

	private sealed class FakeFrameTarget : IHdrFrameTarget
	{
		public HdrDisplayInfo Screen
		{
			get => Presenter.Screen;
			set => Presenter.Screen = value;
		}

		public HdrDisplayInfo Display { get; set; } = HdrDisplayInfo.Sdr;

		public FakePresenter Presenter { get; } = new();

		IHdrPresenter IHdrFrameTarget.Presenter => Presenter;

		public bool Accepts { get; set; } = true;

		public bool Disposed { get; private set; }

		public (int Width, int Height) LastFrameSize { get; private set; }

		public void BeginFrame(int frameWidth, int frameHeight, Vector4 clear) => LastFrameSize = (frameWidth, frameHeight);

		public bool EndFrame() => Accepts;

		public void Dispose()
		{
			Disposed = true;
			Presenter.Dispose();
		}
	}
}
