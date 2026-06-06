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
/// The root view controller for an ImGuiApp iOS process. Hosts the Metal-backed <see cref="MetalView"/>
/// and drives the frame loop via a <see cref="CADisplayLink"/> synchronised to the display's refresh,
/// forwarding each tick to <see cref="ImGuiApp.Tick(float)"/> which builds and submits an ImGui frame
/// through the Metal renderer.
/// </summary>
[SuppressMessage("Design", "CA1010:Generic interface should also be provided", Justification = "Inherited non-generic IEnumerable comes from the UIKit base type UIViewController; a generic counterpart cannot be added to a framework base class.")]
public class ImGuiAppViewController : UIViewController
{
	private CADisplayLink? displayLink;
	private double lastTimestamp;

	// CI smoke-test harness: when the IMGUIAPP_IOS_SMOKE_FRAMES environment variable is a positive
	// integer (set via simctl's SIMCTL_CHILD_ prefix), the app runs that many frames, prints a
	// success marker, and exits cleanly. This lets the iOS-simulator CI job verify the lifecycle
	// actually launches and ticks. It is a no-op in normal runs.
	private const string SmokeFramesEnvVar = "IMGUIAPP_IOS_SMOKE_FRAMES";
	private const string SmokeSuccessMarker = "IMGUIAPP_IOS_SMOKE_OK";
	private int smokeFramesRemaining;

	/// <summary>Installs the Metal-backed view as the controller's root view before it loads.</summary>
	public override void LoadView() => View = new MetalView(UIScreen.MainScreen.Bounds);

	/// <summary>
	/// Initialises the renderer against the Metal view and builds the display link. Called once by
	/// UIKit after the view loads.
	/// </summary>
	public override void ViewDidLoad()
	{
		base.ViewDidLoad();

		ImGuiApp.InitializeRenderer((MetalView)View!);

		float fps = Math.Max(1, (float)ImGuiApp.Config.PerformanceSettings.FocusedFps);
		displayLink = CADisplayLink.Create(OnDisplayLink);
		// PreferredFramesPerSecond is obsolete from iOS 15; CAFrameRateRange is the supported control.
		displayLink.PreferredFrameRateRange = CAFrameRateRange.Create(fps, fps, fps);
		displayLink.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.Common);
		displayLink.Paused = true; // resumed in ViewWillAppear / on activation

		if (int.TryParse(Environment.GetEnvironmentVariable(SmokeFramesEnvVar), out int frames) && frames > 0)
		{
			smokeFramesRemaining = frames;
			Console.WriteLine($"IMGUIAPP_IOS_SMOKE_BEGIN frames={frames}");
		}
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

		if (smokeFramesRemaining > 0)
		{
			smokeFramesRemaining--;
			if (smokeFramesRemaining == 0)
			{
				// Lifecycle ticked successfully for the requested number of frames; report and exit.
				Console.WriteLine(SmokeSuccessMarker);
				Console.Out.Flush();
				Environment.Exit(0);
			}
		}
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
		}

		base.Dispose(disposing);
	}
}

#endif
