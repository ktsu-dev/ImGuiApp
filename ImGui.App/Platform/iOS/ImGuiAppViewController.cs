// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

#if IOS

namespace ktsu.ImGui.App;

using System.Diagnostics.CodeAnalysis;

using CoreAnimation;

using Foundation;

using UIKit;

/// <summary>
/// The root view controller for an ImGuiApp iOS process. Hosts the rendering view and drives the
/// frame loop via a <see cref="CADisplayLink"/> synchronised to the display's refresh, forwarding
/// each tick to <see cref="ImGuiApp.Tick(float)"/>. Until the Metal backend lands, the view is a
/// solid colour with a centred title label so the lifecycle can be verified in isolation.
/// </summary>
[SuppressMessage("Design", "CA1010:Generic interface should also be provided", Justification = "Inherited non-generic IEnumerable comes from the UIKit base type UIViewController; a generic counterpart cannot be added to a framework base class.")]
public class ImGuiAppViewController : UIViewController
{
	private CADisplayLink? displayLink;
	private UILabel? titleLabel;
	private double lastTimestamp;

	/// <summary>
	/// Builds the view contents and the display link. Called once by UIKit after the view loads.
	/// </summary>
	public override void ViewDidLoad()
	{
		base.ViewDidLoad();

		UIView view = View!;
		view.BackgroundColor = UIColor.FromRGB((byte)115, (byte)140, (byte)153); // matches the desktop clear colour

		titleLabel = new UILabel(view.Bounds)
		{
			Text = ImGuiApp.Config.Title,
			TextAlignment = UITextAlignment.Center,
			TextColor = UIColor.White,
			AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight,
		};
		view.AddSubview(titleLabel);

		float fps = Math.Max(1, (float)ImGuiApp.Config.PerformanceSettings.FocusedFps);
		displayLink = CADisplayLink.Create(OnDisplayLink);
		// PreferredFramesPerSecond is obsolete from iOS 15; CAFrameRateRange is the supported control.
		displayLink.PreferredFrameRateRange = CAFrameRateRange.Create(fps, fps, fps);
		displayLink.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.Common);
		displayLink.Paused = true; // resumed in ViewWillAppear / on activation
	}

	/// <summary>Marks the app visible, runs one-time startup, and resumes the frame loop.</summary>
	/// <param name="animated">Whether the appearance is animated.</param>
	public override void ViewWillAppear(bool animated)
	{
		base.ViewWillAppear(animated);
		ImGuiApp.IsVisible = true;
		ImGuiApp.RaiseStart();
		ResumeRendering();
	}

	/// <summary>Marks the app not visible and pauses the frame loop.</summary>
	/// <param name="animated">Whether the disappearance is animated.</param>
	public override void ViewDidDisappear(bool animated)
	{
		base.ViewDidDisappear(animated);
		ImGuiApp.IsVisible = false;
		PauseRendering();
	}

	/// <summary>Forwards layout changes (rotation, size class changes) to the resize callback.</summary>
	public override void ViewDidLayoutSubviews()
	{
		base.ViewDidLayoutSubviews();
		ImGuiApp.Config.OnMoveOrResize?.Invoke();
	}

	/// <summary>Resumes the display link if it has been created.</summary>
	internal void ResumeRendering()
	{
		if (displayLink is not null)
		{
			lastTimestamp = 0; // forces the next frame to seed the delta rather than report a huge gap
			displayLink.Paused = false;
		}
	}

	/// <summary>Pauses the display link if it has been created.</summary>
	internal void PauseRendering()
	{
		if (displayLink is not null)
		{
			displayLink.Paused = true;
		}
	}

	/// <summary>Per-frame callback from the display link; computes delta and advances the app.</summary>
	private void OnDisplayLink()
	{
		double timestamp = displayLink!.Timestamp;
		if (lastTimestamp == 0)
		{
			lastTimestamp = timestamp;
			return;
		}

		float delta = (float)(timestamp - lastTimestamp);
		lastTimestamp = timestamp;

		ImGuiApp.Tick(delta);
	}

	/// <summary>Tears down the display link.</summary>
	/// <param name="disposing"><see langword="true"/> when called from <see cref="IDisposable.Dispose"/>.</param>
	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			displayLink?.Invalidate();
			displayLink?.Dispose();
			displayLink = null;
			titleLabel?.Dispose();
			titleLabel = null;
		}

		base.Dispose(disposing);
	}
}

#endif
