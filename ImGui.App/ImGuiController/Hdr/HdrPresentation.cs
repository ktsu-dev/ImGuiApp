// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.ImGuiController.Hdr;

using System;
using System.Numerics;

/// <summary>What <see cref="HdrPresentation"/> draws each frame into: the half-float frame and the presenter it goes to.</summary>
/// <remarks><see cref="HdrCompositor"/> is the one that draws with OpenGL; the tests stand in their own.</remarks>
internal interface IHdrFrameTarget : IDisposable
{
	/// <summary>Gets or sets what the screen can show, which the last pass scales to.</summary>
	public HdrDisplayInfo Display { get; set; }

	/// <summary>Gets the presenter the frame goes to.</summary>
	public IHdrPresenter Presenter { get; }

	/// <summary>Binds the frame for drawing and clears it.</summary>
	/// <param name="frameWidth">The frame's width in pixels.</param>
	/// <param name="frameHeight">The frame's height in pixels.</param>
	/// <param name="clear">The colour to clear to, encoded.</param>
	public void BeginFrame(int frameWidth, int frameHeight, Vector4 clear);

	/// <summary>Presents the frame.</summary>
	/// <returns>Whether the presenter took it.</returns>
	public bool EndFrame();
}

/// <summary>
/// The window's HDR presentation from frame to frame: asking the screen what it shows, and giving up on
/// a presenter that keeps refusing frames.
/// </summary>
/// <param name="target">Where frames are drawn. It is disposed with this.</param>
/// <param name="time">The clock the screen is polled by.</param>
internal sealed class HdrPresentation(IHdrFrameTarget target, TimeProvider time) : IDisposable
{
	/// <summary>How often the screen is asked whether it shows HDR, since the setting can change while the window is open.</summary>
	internal static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

	/// <summary>How many frames in a row the presenter may refuse before the window gives up on HDR.</summary>
	internal const int MaximumFailures = 3;

	private long polledAt;
	private bool polled;
	private int failures;

	/// <summary>Gets what the screen showed when last asked.</summary>
	public HdrDisplayInfo Display { get; private set; } = HdrDisplayInfo.Sdr;

	/// <summary>Gets the presenter frames go to.</summary>
	public IHdrPresenter Presenter => target.Presenter;

	/// <summary>Asks the screen again what it shows, at most once an interval unless forced.</summary>
	/// <param name="force">Whether to ask now whatever the interval, as when the window moves.</param>
	/// <returns>What the screen shows.</returns>
	public HdrDisplayInfo Refresh(bool force)
	{
		long now = time.GetTimestamp();
		if (force || !polled || time.GetElapsedTime(polledAt, now) >= PollInterval)
		{
			polled = true;
			polledAt = now;
			Display = target.Presenter.QueryDisplay();
			target.Display = Display;
		}

		return Display;
	}

	/// <summary>Starts a frame: asks the screen if it is time to, then binds the frame and clears it.</summary>
	/// <param name="frameWidth">The frame's width in pixels.</param>
	/// <param name="frameHeight">The frame's height in pixels.</param>
	/// <param name="clear">The colour to clear to, encoded.</param>
	public void BeginFrame(int frameWidth, int frameHeight, Vector4 clear)
	{
		Refresh(force: false);
		target.BeginFrame(frameWidth, frameHeight, clear);
	}

	/// <summary>Presents the frame.</summary>
	/// <returns>False once the presenter has refused <see cref="MaximumFailures"/> frames in a row, when the window should go back to SDR.</returns>
	public bool EndFrame()
	{
		if (target.EndFrame())
		{
			failures = 0;
			return true;
		}

		return ++failures < MaximumFailures;
	}

	/// <inheritdoc />
	public void Dispose() => target.Dispose();
}
