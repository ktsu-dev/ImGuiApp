// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

#if IOS

namespace ktsu.ImGui.App;

using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.Invoker;

using ObjCRuntime;

using UIKit;

/// <summary>
/// iOS entry point for ImGuiApp. This is the platform-neutral orchestration half of the port:
/// it owns the application configuration, the main-thread <see cref="Invoker"/>, the lifecycle
/// state (focus / visibility / idle / scale), and the per-frame tick. The UIKit plumbing lives in
/// <c>ImGuiAppDelegate</c> (application lifecycle) and <c>ImGuiAppViewController</c> (view +
/// <c>CADisplayLink</c>), which call back into the <c>internal</c> hooks below.
/// </summary>
/// <remarks>
/// Renderer status: this layer drives the lifecycle (<see cref="ImGuiAppConfig.OnStart"/>,
/// <see cref="ImGuiAppConfig.OnUpdate"/>, <see cref="ImGuiAppConfig.OnRender"/>) but does not yet
/// stand up an ImGui frame or submit draw data — the Metal backend is the next chunk. See
/// docs/plans/2026-05-28-ios-platform-port.md.
/// </remarks>
public static partial class ImGuiApp
{
	private const string NotImplementedMessage =
		"This ImGuiApp feature is not yet implemented on iOS. Track progress in docs/plans/2026-05-28-ios-platform-port.md.";

	/// <summary>Gets the active application configuration, populated by <see cref="Start"/>.</summary>
	internal static ImGuiAppConfig Config { get; set; } = new();

	/// <summary>The root view controller, retained so <see cref="Stop"/> can pause the display link.</summary>
	internal static ImGuiAppViewController? ViewController { get; set; }

	/// <summary>Tracks whether <see cref="ImGuiAppConfig.OnStart"/> has already run for this process.</summary>
	private static bool started;

	/// <summary>Timestamp of the most recent user input, used for idle detection (mirrors desktop).</summary>
	private static DateTime lastInputTime = DateTime.UtcNow;

	/// <summary>
	/// Gets an instance of the <see cref="Invoker"/> used to marshal delegates onto the main UI
	/// thread. Constructed on the main thread in <see cref="RaiseStart"/> and drained each frame.
	/// </summary>
	public static Invoker Invoker { get; internal set; } = null!;

	/// <summary>Gets a value indicating whether the application is the active (foreground) app.</summary>
	public static bool IsFocused { get; internal set; } = true;

	/// <summary>Gets a value indicating whether the application's view is on screen.</summary>
	public static bool IsVisible { get; internal set; } = true;

	/// <summary>Gets a value indicating whether the application is idle (no recent user input).</summary>
	public static bool IsIdle { get; private set; }

	/// <summary>
	/// Gets the DPI scale factor for the application, initialised from <c>UIScreen.MainScreen.Scale</c>
	/// (typically 2 or 3) when the platform layer starts.
	/// </summary>
	public static float ScaleFactor { get; internal set; } = 1.0f;

	/// <summary>
	/// Gets the user-controlled global UI scale factor for accessibility. Identical semantics to desktop.
	/// </summary>
	public static float GlobalScale { get; private set; } = 1.0f;

	/// <summary>
	/// Gets the current window state. iOS owns window sizing, so this reports the screen's native
	/// size at origin (0, 0); there is no separate layout state on iOS.
	/// </summary>
	public static ImGuiAppWindowState WindowState
	{
		get
		{
			CoreGraphics.CGSize size = UIScreen.MainScreen.NativeBounds.Size;
			return new ImGuiAppWindowState
			{
				Size = new Vector2((float)size.Width, (float)size.Height),
				Pos = Vector2.Zero,
			};
		}
	}

	/// <summary>
	/// Bootstraps and runs the iOS application. Mirrors the desktop <c>Start(ImGuiAppConfig)</c>
	/// signature. This hands control to <c>UIApplication.Main</c> and does not return until the OS
	/// terminates the process (the standard iOS application model).
	/// </summary>
	/// <param name="config">The application configuration (lifecycle callbacks, fonts, settings).</param>
	public static void Start(ImGuiAppConfig config)
	{
		Ensure.NotNull(config);
		Config = config;

		if (config.TestMode)
		{
			// Headless path for any future iOS test target: run the lifecycle synchronously without
			// handing control to UIKit (UIApplication.Main cannot run inside a unit-test host).
			RaiseStart();
			Tick(1.0f / 60.0f);
			return;
		}

		// Pass the delegate Type (not just its registered name) so the trimmer roots it under AOT.
		UIApplication.Main(Environment.GetCommandLineArgs(), null, typeof(ImGuiAppDelegate));
	}

	/// <summary>
	/// Requests application shutdown. iOS applications are not meant to self-terminate (the OS owns
	/// the lifecycle), so this logs a warning, pauses rendering, and asks UIKit to exit.
	/// </summary>
	[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Best-effort termination via a private selector; any failure must be logged and swallowed so Stop never throws back into consumer code.")]
	public static void Stop()
	{
		DebugLogger.Log("ImGuiApp.Stop() called on iOS. iOS applications should not terminate themselves; " +
			"the OS owns the application lifecycle. Pausing rendering and requesting exit.");

		ViewController?.PauseRendering();

		try
		{
			// There is no public terminate API on iOS; this is the long-documented private selector.
			UIApplication.SharedApplication?.PerformSelector(new Selector("terminateWithSuccess"), null, 0);
		}
		catch (Exception ex)
		{
			DebugLogger.Log($"ImGuiApp.Stop(): terminate request failed: {ex.Message}");
		}
	}

	/// <summary>
	/// Sets the global UI scale factor for accessibility. Identical semantics and range to desktop.
	/// </summary>
	/// <param name="scale">The scale factor to set. Valid range is 0.5 to 3.0.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when scale is outside the valid range.</exception>
	public static void SetGlobalScale(float scale)
	{
		if (scale is < 0.5f or > 3.0f)
		{
			throw new ArgumentOutOfRangeException(nameof(scale), scale, "Scale must be between 0.5 and 3.0");
		}

		GlobalScale = scale;
		Config.OnGlobalScaleChanged?.Invoke(scale);
	}

	/// <summary>
	/// Converts a measurement in ems to pixels. Until the ImGui frame is wired up on iOS there is no
	/// active font, so this mirrors the desktop uninitialised fallback of ems times the default point size.
	/// </summary>
	/// <param name="ems">The measurement in ems.</param>
	/// <returns>The equivalent measurement in pixels.</returns>
	public static int EmsToPx(float ems) => (int)(ems * FontAppearance.DefaultFontPointSize);

	/// <summary>
	/// Converts a measurement in points to pixels using the current scale factor. Matches desktop semantics.
	/// </summary>
	/// <param name="pts">The measurement in points.</param>
	/// <returns>The equivalent measurement in pixels.</returns>
	public static int PtsToPx(int pts) => (int)(pts * ScaleFactor);

	/// <summary>
	/// Records that user input occurred, resetting the idle timer. Called by the (forthcoming) touch
	/// and keyboard input bridge.
	/// </summary>
	internal static void OnUserInput() => lastInputTime = DateTime.UtcNow;

	/// <summary>
	/// Runs one-time startup: constructs the main-thread <see cref="Invoker"/> and fires
	/// <see cref="ImGuiAppConfig.OnStart"/>. Idempotent — only the first call has an effect. Must be
	/// invoked on the main UI thread (the Invoker captures its owning thread here).
	/// </summary>
	internal static void RaiseStart()
	{
		if (started)
		{
			return;
		}

		started = true;
		Invoker = new Invoker();
		Config.OnStart?.Invoke();
	}

	/// <summary>
	/// Advances the application by one frame. Mirrors the desktop ordering: begin the ImGui frame,
	/// update the consumer, drain queued main-thread work, run the render callback, then submit the
	/// built draw data to the Metal backend. When the renderer is not yet initialised (the first
	/// ticks before the Metal layer exists, or the headless <see cref="ImGuiAppConfig.TestMode"/>
	/// path) the lifecycle callbacks still fire so consumers and the smoke harness keep ticking.
	/// </summary>
	/// <param name="deltaSeconds">Elapsed wall-clock time since the previous frame, in seconds.</param>
	internal static void Tick(float deltaSeconds)
	{
		UpdateIdleState();

		bool frameBegun = TryBeginImGuiFrame(deltaSeconds);

		Config.OnUpdate?.Invoke(deltaSeconds);
		Invoker?.DoInvokes();

		if (frameBegun)
		{
			RenderAppMenu();
		}

		Config.OnRender?.Invoke(deltaSeconds);

		if (frameBegun)
		{
			EndImGuiFrameAndRender();
		}
	}

	/// <summary>The discrete accessibility UI-scale steps offered in the app menu (75%–200%).</summary>
	private static readonly float[] AccessibilityScales = [0.75f, 1.0f, 1.25f, 1.5f, 1.75f, 2.0f];

	/// <summary>
	/// Renders the application menu bar. On iPad this shows the consumer's
	/// <see cref="ImGuiAppConfig.OnAppMenu"/> plus an accessibility UI-scale submenu, mirroring the
	/// desktop main menu bar. iPhone has no main-menu-bar paradigm and little vertical space, so this is
	/// a no-op there (§2.3 of the port plan). Must be called inside an active ImGui frame.
	/// </summary>
	private static void RenderAppMenu()
	{
		if (UIDevice.CurrentDevice.UserInterfaceIdiom != UIUserInterfaceIdiom.Pad)
		{
			return; // iPhone: no app menu bar.
		}

		if (ImGui.BeginMainMenuBar())
		{
			Config.OnAppMenu?.Invoke();
			RenderAccessibilityMenu();
			ImGui.EndMainMenuBar();
		}
	}

	/// <summary>Renders the accessibility UI-scale submenu, driving <see cref="SetGlobalScale"/>.</summary>
	private static void RenderAccessibilityMenu()
	{
		if (ImGui.BeginMenu("Accessibility"))
		{
			// TextUnformatted, not Text: ImGui.Text is the variadic igText, which crashes on the Apple
			// ARM64 ABI through HexaGen's fixed function-pointer.
			ImGui.TextUnformatted("UI Scale:");
			ImGui.Separator();

			foreach (float scale in AccessibilityScales)
			{
				if (ImGui.MenuItem($"{(int)(scale * 100)}%", "", Math.Abs(GlobalScale - scale) < 0.01f))
				{
					SetGlobalScale(scale);
				}
			}

			ImGui.EndMenu();
		}
	}

	/// <summary>Recomputes <see cref="IsIdle"/> from the configured idle timeout and last input time.</summary>
	private static void UpdateIdleState()
	{
		ImGuiAppPerformanceSettings settings = Config.PerformanceSettings;
		if (!settings.EnableIdleDetection)
		{
			IsIdle = false;
			return;
		}

		double secondsSinceInput = (DateTime.UtcNow - lastInputTime).TotalSeconds;
		IsIdle = secondsSinceInput >= settings.IdleTimeoutSeconds;
	}

	/// <summary>
	/// Resolves a font for the requested appearance. Not yet implemented on iOS (fonts land with the
	/// Metal backend); the symbol exists so <c>FontAppearance</c> links against the iOS assembly.
	/// </summary>
	internal static ImFontPtr FindBestFontForAppearance(string name, int sizePoints, out float sizePixels)
	{
		sizePixels = sizePoints;
		throw new PlatformNotSupportedException(NotImplementedMessage);
	}
}

#endif
