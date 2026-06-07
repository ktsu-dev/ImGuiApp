// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

#if IOS

namespace ktsu.ImGui.App;

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;

using CoreAnimation;

using CoreGraphics;

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

	// Input state. The primary touch drives the single ImGui mouse (additional fingers are ignored
	// for v1); the soft keyboard is an invisible first-responder presented while ImGui wants text.
	private UITouch? primaryTouch;
	private SoftKeyboardView? softKeyboard;

	/// <summary>The controller becomes first responder so hardware key presses reach <c>PressesBegan</c>.</summary>
	public override bool CanBecomeFirstResponder => true;

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

		// Invisible, zero-sized soft-keyboard responder. Zero frame means it never intercepts touches;
		// it only matters as a first responder when ImGui wants text input.
		softKeyboard = new SoftKeyboardView { Frame = CGRect.Empty };
		View!.AddSubview(softKeyboard);

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

	/// <summary>Takes first-responder status so a hardware keyboard reaches the press handlers.</summary>
	/// <param name="animated">Whether the appearance is animated.</param>
	public override void ViewDidAppear(bool animated)
	{
		base.ViewDidAppear(animated);
		BecomeFirstResponder();
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
		UpdateSoftKeyboard();

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

	/// <summary>Begins tracking the first finger as the ImGui mouse and presses the left button.</summary>
	/// <param name="touches">The touches that began.</param>
	/// <param name="evt">The owning UIKit event.</param>
	public override void TouchesBegan(NSSet touches, UIEvent? evt)
	{
		base.TouchesBegan(touches, evt);
		if (primaryTouch is null && touches.AnyObject is UITouch touch)
		{
			primaryTouch = touch;
			ImGuiApp.OnPointerMoved(LocationOf(touch));
			ImGuiApp.OnPointerButton(down: true);
		}
	}

	/// <summary>Tracks the primary finger's movement as ImGui mouse motion.</summary>
	/// <param name="touches">The touches that moved.</param>
	/// <param name="evt">The owning UIKit event.</param>
	public override void TouchesMoved(NSSet touches, UIEvent? evt)
	{
		base.TouchesMoved(touches, evt);
		if (primaryTouch is not null && touches.Contains(primaryTouch))
		{
			ImGuiApp.OnPointerMoved(LocationOf(primaryTouch));
		}
	}

	/// <summary>Releases the ImGui mouse button when the primary finger lifts.</summary>
	/// <param name="touches">The touches that ended.</param>
	/// <param name="evt">The owning UIKit event.</param>
	public override void TouchesEnded(NSSet touches, UIEvent? evt)
	{
		base.TouchesEnded(touches, evt);
		EndPrimaryTouch(touches);
	}

	/// <summary>Releases the ImGui mouse button when the primary touch is cancelled.</summary>
	/// <param name="touches">The touches that were cancelled.</param>
	/// <param name="evt">The owning UIKit event.</param>
	public override void TouchesCancelled(NSSet touches, UIEvent? evt)
	{
		base.TouchesCancelled(touches, evt);
		EndPrimaryTouch(touches);
	}

	/// <summary>Forwards hardware key-down events (key, modifiers, typed characters) to ImGui.</summary>
	/// <param name="presses">The presses that began.</param>
	/// <param name="evt">The owning UIKit presses event.</param>
	public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent? evt)
	{
		if (!HandlePresses(presses, down: true))
		{
			base.PressesBegan(presses, evt);
		}
	}

	/// <summary>Forwards hardware key-up events to ImGui.</summary>
	/// <param name="presses">The presses that ended.</param>
	/// <param name="evt">The owning UIKit presses event.</param>
	public override void PressesEnded(NSSet<UIPress> presses, UIPressesEvent? evt)
	{
		if (!HandlePresses(presses, down: false))
		{
			base.PressesEnded(presses, evt);
		}
	}

	private void EndPrimaryTouch(NSSet touches)
	{
		if (primaryTouch is not null && touches.Contains(primaryTouch))
		{
			ImGuiApp.OnPointerMoved(LocationOf(primaryTouch));
			ImGuiApp.OnPointerButton(down: false);
			primaryTouch = null;
		}
	}

	private Vector2 LocationOf(UITouch touch)
	{
		CGPoint point = touch.LocationInView(View);
		return new Vector2((float)point.X, (float)point.Y);
	}

	private bool HandlePresses(NSSet<UIPress> presses, bool down)
	{
		bool handled = false;
		foreach (UIPress press in presses)
		{
			if (press.Key is not UIKey key)
			{
				continue;
			}

			UIKeyModifierFlags mods = key.ModifierFlags;
			ImGuiApp.OnModifiers(
				mods.HasFlag(UIKeyModifierFlags.Control),
				mods.HasFlag(UIKeyModifierFlags.Shift),
				mods.HasFlag(UIKeyModifierFlags.Alternate),
				mods.HasFlag(UIKeyModifierFlags.Command));
			ImGuiApp.OnKey(IOSKeyMap.Map(key.KeyCode), down);

			// Hardware-typed characters, but only when the soft keyboard isn't also capturing them
			// (otherwise iPad-with-Magic-Keyboard would deliver each character twice).
			if (down && softKeyboard?.IsFirstResponder != true)
			{
				foreach (Rune rune in (key.Characters ?? string.Empty).EnumerateRunes())
				{
					if (rune.Value >= 0x20 && rune.Value != 0x7f)
					{
						ImGuiApp.OnTextInput((uint)rune.Value);
					}
				}
			}

			handled = true;
		}

		return handled;
	}

	/// <summary>
	/// Presents or dismisses the soft keyboard to match ImGui's text-input intent. Called each frame
	/// after <see cref="ImGuiApp.Tick(float)"/> so a freshly focused input field raises the keyboard.
	/// </summary>
	private void UpdateSoftKeyboard()
	{
		if (softKeyboard is null)
		{
			return;
		}

		bool want = ImGuiApp.WantsTextInput;
		if (want && !softKeyboard.IsFirstResponder)
		{
			softKeyboard.BecomeFirstResponder();
		}
		else if (!want && softKeyboard.IsFirstResponder)
		{
			softKeyboard.ResignFirstResponder();
			BecomeFirstResponder(); // restore hardware-key capture to the controller
		}
	}

	/// <summary>Tears down the display link and input responder.</summary>
	/// <param name="disposing"><see langword="true"/> when called from <see cref="IDisposable.Dispose"/>.</param>
	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			displayLink?.Invalidate();
			displayLink?.Dispose();
			displayLink = null;
			softKeyboard?.Dispose();
			softKeyboard = null;
		}

		base.Dispose(disposing);
	}
}

#endif
