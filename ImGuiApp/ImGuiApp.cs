// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp;

using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using ktsu.Extensions;
using ktsu.ImGuiApp.ImGuiController;
using ktsu.Invoker;
using ktsu.ScopedAction;
using ktsu.Semantics;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = System.Drawing.Color;
using Texture = ImGuiController.Texture;

/// <summary>
/// Provides static methods and properties to manage the ImGui application.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "This class is the main entry point for the ImGui application and requires many dependencies. Consider refactoring in the future.")]
public static partial class ImGuiApp
{
	internal static IWindow? window;
	internal static GL? gl;
	internal static ImGuiController.ImGuiController? controller;
	internal static IInputContext? inputContext;
	internal static OpenGLProvider? glProvider;
	internal static IntPtr currentGLContextHandle; // Track the current GL context handle

	internal static ImGuiAppWindowState LastNormalWindowState { get; set; } = new();

	/// <summary>
	/// Gets the current state of the ImGui application window.
	/// </summary>
	/// <value>
	/// A new instance of <see cref="ImGuiAppWindowState"/> representing the current window state,
	/// including size, position, and layout state.
	/// </value>
	public static ImGuiAppWindowState WindowState
	{
		get => new()
		{
			Size = LastNormalWindowState.Size,
			Pos = LastNormalWindowState.Pos,
			LayoutState = window?.WindowState ?? Silk.NET.Windowing.WindowState.Normal
		};
	}

	internal static ConcurrentDictionary<string, int> FontIndices { get; } = [];
	internal static float lastFontScaleFactor;
	internal static readonly List<GCHandle> currentPinnedFontData = [];

	/// <summary>
	/// Stores unmanaged memory handles for font data that needs to be freed with Marshal.FreeHGlobal.
	/// </summary>
	internal static readonly List<nint> currentFontMemoryHandles = [];

	/// <summary>
	/// List of common font sizes to load for crisp rendering at multiple sizes
	/// </summary>
	internal static readonly int[] CommonFontSizes = [10, 12, 14, 16, 18, 20, 24, 32, 48];

	/// <summary>
	/// Gets an instance of the <see cref="Invoker"/> class to delegate tasks to the window thread.
	/// </summary>
	public static Invoker Invoker { get; internal set; } = null!;

	/// <summary>
	/// Gets a value indicating whether the ImGui application window is focused.
	/// </summary>
	public static bool IsFocused { get; private set; } = true;

	/// <summary>
	/// Gets a value indicating whether the ImGui application window is visible.
	/// </summary>
	public static bool IsVisible => (window?.WindowState != Silk.NET.Windowing.WindowState.Minimized) && (window?.IsVisible ?? false);

	/// <summary>
	/// Gets a value indicating whether the application is currently idle (no user input for a specified time).
	/// </summary>
	public static bool IsIdle { get; private set; }

	internal static DateTime lastInputTime = DateTime.UtcNow;
	internal static double targetFrameTimeMs = 1000.0 / 30.0; // Default to 30 FPS (33.33ms per frame)
	internal static readonly PidFrameLimiter frameLimiter = new();
	internal static double previousTargetFrameTimeMs = 1000.0 / 30.0;

	/// <summary>
	/// Updates the last input time to the current time. Called by the input system when user input is detected.
	/// </summary>
	internal static void OnUserInput() => lastInputTime = DateTime.UtcNow;

	internal static bool isImGuiMetricsVisible;
	internal static bool isImGuiDemoVisible;
	internal static bool isImGuiStyleEditorVisible;
	internal static bool isPerformanceMonitorVisible;

	// Track whether we need to focus windows on their first frame
	internal static bool shouldFocusImGuiDemo;
	internal static bool shouldFocusImGuiMetrics;
	internal static bool shouldFocusImGuiStyleEditor;
	internal static bool shouldFocusPerformanceMonitor;

	// Performance monitoring data structures
	internal static readonly Queue<float> performanceFrameTimes = new();
	internal static float performanceFrameTimeSum;
	internal static readonly Queue<float> performanceFpsHistory = new();
	internal static float performanceLastFpsUpdateTime;
	internal static int performanceFrameCount;

	/// <summary>
	/// Gets the scale factor for the ImGui application.
	/// </summary>
	public static float ScaleFactor { get; private set; } = 1;

	internal static ConcurrentDictionary<AbsoluteFilePath, ImGuiAppTextureInfo> Textures { get; } = [];

	/// <summary>
	/// Stops the ImGui application by closing the window.
	/// </summary>
	/// <exception cref="InvalidOperationException">Thrown when the application is not running.</exception>
	public static void Stop()
	{
		if (window == null)
		{
			throw new InvalidOperationException("Cannot stop the application because it is not running.");
		}

		window.Close();
	}

	/// <summary>
	/// Opens the ImGui Demo window.
	/// </summary>
	public static void ShowImGuiDemo()
	{
		if (!isImGuiDemoVisible)
		{
			shouldFocusImGuiDemo = true;
		}
		isImGuiDemoVisible = true;
	}

	/// <summary>
	/// Opens the ImGui Metrics window.
	/// </summary>
	public static void ShowImGuiMetrics()
	{
		if (!isImGuiMetricsVisible)
		{
			shouldFocusImGuiMetrics = true;
		}
		isImGuiMetricsVisible = true;
	}

	/// <summary>
	/// Opens the ImGui Style Editor window.
	/// </summary>
	public static void ShowImGuiStyleEditor()
	{
		if (!isImGuiStyleEditorVisible)
		{
			shouldFocusImGuiStyleEditor = true;
		}
		isImGuiStyleEditorVisible = true;
	}

	/// <summary>
	/// Opens the Performance Monitor window.
	/// </summary>
	public static void ShowPerformanceMonitor()
	{
		if (!isPerformanceMonitorVisible)
		{
			shouldFocusPerformanceMonitor = true;
		}
		isPerformanceMonitorVisible = true;
	}

	internal static ImGuiAppConfig Config { get; set; } = new();

	internal static void InitializeWindow(ImGuiAppConfig config)
	{
		if (config.TestMode)
		{
			// In test mode, use the test window from config
			window = config.TestWindow ?? throw new InvalidOperationException("TestWindow must be set when TestMode is true");
			return;
		}

		WindowOptions silkWindowOptions = WindowOptions.Default with
		{
			Title = config.Title,
			Size = new((int)config.InitialWindowState.Size.X, (int)config.InitialWindowState.Size.Y),
			Position = new((int)config.InitialWindowState.Pos.X, (int)config.InitialWindowState.Pos.Y),
			WindowState = Silk.NET.Windowing.WindowState.Normal,
			VSync = false, // Disable VSync to allow PID frame limiter to control frame rate
			API = OperatingSystem.IsLinux() ?
				new GraphicsAPI(
					ContextAPI.OpenGL,
					ContextProfile.Core,
					ContextFlags.Default,
					new APIVersion(3, 0)) :
				new GraphicsAPI(
					ContextAPI.OpenGL,
					ContextProfile.Core,
					ContextFlags.ForwardCompatible,
					new APIVersion(3, 3)),
			PreferredDepthBufferBits = 24,
			PreferredStencilBufferBits = 8
		};

		LastNormalWindowState = config.InitialWindowState;
		LastNormalWindowState.LayoutState = Silk.NET.Windowing.WindowState.Normal;

		window = Window.Create(silkWindowOptions);
	}

	internal static void SetupWindowLoadHandler(ImGuiAppConfig config)
	{
		window!.Load += () =>
		{
			DebugLogger.Log("Window.Load: Starting window load handler");

			if (!string.IsNullOrEmpty(config.IconPath))
			{
				DebugLogger.Log("Window.Load: Setting window icon");
				SetWindowIcon(config.IconPath);
			}

			DebugLogger.Log("Window.Load: Creating OpenGL factory");
			WindowOpenGLFactory glFactory = new(window);
			glProvider = new OpenGLProvider(glFactory);
			GLWrapper glWrapper = (GLWrapper)glProvider.GetGL();
			gl = glWrapper.UnderlyingGL;
			DebugLogger.Log("Window.Load: OpenGL initialized");

			DebugLogger.Log("Window.Load: Creating input context");
			inputContext = window.CreateInput();
			DebugLogger.Log("Window.Load: Input context created");

			DebugLogger.Log("Window.Load: Creating ImGuiController");
			controller = new(
				gl,
				view: window,
				input: inputContext,
				onConfigureIO: () =>
				{
					DebugLogger.Log("onConfigureIO: Starting configuration");
					unsafe
					{
						currentGLContextHandle = (nint)ImGui.GetCurrentContext().Handle;

						ImGuiIOPtr io = ImGui.GetIO();

						// Configure imgui.ini file saving based on user preference
						if (!config.SaveIniSettings)
						{
							io.IniFilename = null;
						}

						io.ConfigDebugIsDebuggerPresent = Debugger.IsAttached;
						io.ConfigErrorRecoveryEnableAssert = false;
						io.ConfigErrorRecoveryEnableTooltip = true;
						io.ConfigErrorRecoveryEnableDebugLog = true;
					}
					DebugLogger.Log("onConfigureIO: Context configured");

					DebugLogger.Log("onConfigureIO: Updating DPI scale");
					UpdateDpiScale();
					DebugLogger.Log("onConfigureIO: Initializing fonts");
					InitFonts();
					DebugLogger.Log("onConfigureIO: Calling user OnStart");
					config.OnStart?.Invoke();
					DebugLogger.Log("onConfigureIO: Configuration completed");
				}
			);
			DebugLogger.Log("Window.Load: ImGuiController created");

			ImGui.GetStyle().WindowRounding = 0;
			window.WindowState = config.InitialWindowState.LayoutState;
			DebugLogger.Log("Window.Load: Window load handler completed");
		};
	}

	internal static void SetupWindowResizeHandler(ImGuiAppConfig config)
	{
		window!.FramebufferResize += s =>
		{
			gl?.Viewport(s);
			CaptureWindowNormalState();
			UpdateDpiScale();
			CheckAndHandleContextChange();
			config.OnMoveOrResize?.Invoke();
		};
	}

	internal static void SetupWindowMoveHandler(ImGuiAppConfig config)
	{
		window!.Move += (p) =>
		{
			CaptureWindowNormalState();
			UpdateDpiScale();
			CheckAndHandleContextChange();
			config.OnMoveOrResize?.Invoke();
		};
	}

	internal static void UpdateWindowPerformance()
	{
		ImGuiAppPerformanceSettings settings = Config.PerformanceSettings;

		if (!settings.EnableThrottledRendering)
		{
			return;
		}

		// Skip throttling during PID auto-tuning to maintain consistent target frame rate
		(bool isTuningActive, _, _, _, _) = frameLimiter.GetTuningStatus();
		if (isTuningActive)
		{
			return;
		}

		// Update idle state if idle detection is enabled
		if (settings.EnableIdleDetection)
		{
			double timeSinceLastInput = (DateTime.UtcNow - lastInputTime).TotalSeconds;
			IsIdle = timeSinceLastInput >= settings.IdleTimeoutSeconds;
		}
		else
		{
			IsIdle = false;
		}

		// Evaluate all throttling conditions and select the lowest frame rate
		List<(double fps, string reason)> candidateRates = [];

		// Add focused/unfocused rate
		if (IsFocused)
		{
			candidateRates.Add((settings.FocusedFps, "Focused"));
		}
		else
		{
			candidateRates.Add((settings.UnfocusedFps, "Unfocused"));
		}

		// Add idle rate if applicable
		if (IsIdle && settings.EnableIdleDetection)
		{
			candidateRates.Add((settings.IdleFps, "Idle"));
		}

		// Add not visible rate if applicable
		if (!IsVisible)
		{
			candidateRates.Add((settings.NotVisibleFps, "Not Visible"));
		}

		// Select the lowest frame rate
		(double targetFps, string _) = candidateRates
			.OrderBy(rate => rate.fps)
			.First();

		// Update target frame time (convert FPS to milliseconds per frame)
		double newTargetFrameTimeMs = targetFps > 0 ? (1000.0 / targetFps) : 10000.0; // Cap at 10 seconds for very low FPS
		targetFrameTimeMs = newTargetFrameTimeMs;
	}

	internal static void SetupWindowUpdateHandler(ImGuiAppConfig config)
	{
		window!.Update += (delta) =>
		{
			if (!controller?.FontsConfigured ?? true)
			{
				throw new InvalidOperationException("Fonts are not configured before Update()");
			}

			EnsureWindowPositionIsValid();
			UpdateWindowPerformance();
			UpdatePerformanceMonitoring((float)delta);

			// SetupContext prepares ImGui context for this frame
			controller?.SetupContext();
			controller?.Update((float)delta);
			config.OnUpdate?.Invoke((float)delta);
			Invoker.DoInvokes();
		};
	}

	internal static void SetupWindowRenderHandler(ImGuiAppConfig config)
	{
		window!.Render += delta =>
		{
			if (!controller?.FontsConfigured ?? true)
			{
				throw new InvalidOperationException("Fonts are not configured before Render()");
			}

			gl?.ClearColor(Color.FromArgb(255, (int)(.45f * 255), (int)(.55f * 255), (int)(.60f * 255)));
			gl?.Clear(ClearBufferMask.ColorBufferBit);

			RenderWithScaling(() =>
			{
				using ScopedAction? frameWrapper = config.FrameWrapperFactory.Invoke();
				RenderAppMenu(config.OnAppMenu);
				RenderWindowContents(config.OnRender, (float)delta);
				RenderPerformanceMonitor();
			});

			controller?.Render();

			// Apply sleep-based frame rate limiting
			ApplyFrameRateLimit();
		};
	}

	internal static void ApplyFrameRateLimit()
	{
		// Reset PID controller if target frame rate has changed
		if (Math.Abs(targetFrameTimeMs - previousTargetFrameTimeMs) > 0.1)
		{
			frameLimiter.Reset();
			previousTargetFrameTimeMs = targetFrameTimeMs;
		}

		// Use PID controller for accurate frame rate limiting
		frameLimiter.LimitFrameRate(targetFrameTimeMs);
	}

	internal static void RenderWithScaling(Action renderAction)
	{
		FindBestFontForAppearance(FontAppearance.DefaultFontName, FontAppearance.DefaultFontPointSize, out float bestFontSize);
		float scaleRatio = bestFontSize / FontAppearance.DefaultFontPointSize;
		using (new UIScaler(scaleRatio))
		{
			RenderWithDefaultFont(renderAction);
		}
	}

	internal static void SetupWindowClosingHandler()
	{
		window!.Closing += () =>
		{
			CleanupPinnedFontData();
			FontHelper.CleanupCustomFonts();
			CleanupController();
			CleanupInputContext();
			CleanupOpenGL();
		};
	}

	internal static void CleanupPinnedFontData()
	{
		// Free our font data handles since we own the data
		foreach (GCHandle handle in currentPinnedFontData)
		{
			try
			{
				if (handle.IsAllocated)
				{
					handle.Free();
				}
			}
			catch (InvalidOperationException)
			{
				// Handle was already freed, ignore
			}
		}

		currentPinnedFontData.Clear();

		// Free unmanaged font memory handles
		foreach (nint memoryHandle in currentFontMemoryHandles)
		{
			try
			{
				Marshal.FreeHGlobal(memoryHandle);
			}
			catch (ArgumentException)
			{
				// Handle was already freed or invalid, ignore
			}
		}

		currentFontMemoryHandles.Clear();
	}

	internal static void CleanupController()
	{
		controller?.Dispose();
		controller = null;
	}

	internal static void CleanupInputContext()
	{
		inputContext?.Dispose();
		inputContext = null;
	}

	internal static void CleanupOpenGL()
	{
		if (gl != null)
		{
			gl.Dispose();
			gl = null;
		}

		if (glProvider != null)
		{
			glProvider.Dispose();
			glProvider = null;
		}
	}

	/// <summary>
	/// Starts the ImGui application with the specified configuration.
	/// </summary>
	/// <param name="config">The configuration settings for the ImGui application.</param>
	public static void Start(ImGuiAppConfig config)
	{
		DebugLogger.Log("ImGuiApp.Start: Starting application");
		ArgumentNullException.ThrowIfNull(config);

		if (window != null)
		{
			throw new InvalidOperationException("Application is already running.");
		}

		AdjustConfigForStartup(config);

		DebugLogger.Log("ImGuiApp.Start: Creating invoker and setting config");
		Invoker = new();
		Config = config;

		DebugLogger.Log("ImGuiApp.Start: Validating config");
		ValidateConfig(config);

		DebugLogger.Log("ImGuiApp.Start: Setting DPI awareness");
		ForceDpiAware.Windows();

		DebugLogger.Log("ImGuiApp.Start: Initializing window");
		InitializeWindow(config);
		DebugLogger.Log("ImGuiApp.Start: Setting up window handlers");
		SetupWindowLoadHandler(config);
		SetupWindowResizeHandler(config);
		SetupWindowMoveHandler(config);
		SetupWindowUpdateHandler(config);
		SetupWindowRenderHandler(config);
		SetupWindowClosingHandler();

		window!.FocusChanged += (focused) => IsFocused = focused;

		if (!config.TestMode)
		{
			// Temporarily keep console window visible for debugging
			// if (OperatingSystem.IsWindows())
			// {
			//     DebugLogger.Log("ImGuiApp.Start: Hiding console window");
			//     nint handle = NativeMethods.GetConsoleWindow();
			//     NativeMethods.ShowWindow(handle, SW_HIDE);
			// }

			DebugLogger.Log("ImGuiApp.Start: Starting window run loop");
			window.Run();
			DebugLogger.Log("ImGuiApp.Start: Window run loop completed");
			window.Dispose();
		}
	}

	internal static void AdjustConfigForStartup(ImGuiAppConfig config)
	{
		if (config.InitialWindowState.LayoutState == Silk.NET.Windowing.WindowState.Minimized)
		{
			// Automatically convert minimized state to normal to prevent startup issues
			// This commonly happens when restoring persisted window state
			config.InitialWindowState.LayoutState = Silk.NET.Windowing.WindowState.Normal;
			DebugLogger.Log("ImGuiApp.Start: Initial window state was minimized, automatically converted to normal for proper startup.");
		}
	}

	internal static void ValidateConfig(ImGuiAppConfig config)
	{
		if (config.InitialWindowState.Size.X <= 0 || config.InitialWindowState.Size.Y <= 0)
		{
			throw new ArgumentException("Initial window size must be greater than zero.", nameof(config));
		}

		if (config.InitialWindowState.Pos.X < 0 || config.InitialWindowState.Pos.Y < 0)
		{
			throw new ArgumentException("Initial window position must be non-negative.", nameof(config));
		}

		if (!string.IsNullOrEmpty(config.IconPath) && !File.Exists(config.IconPath))
		{
			throw new FileNotFoundException("Icon file not found.", config.IconPath);
		}

		foreach (KeyValuePair<string, byte[]> font in config.Fonts)
		{
			if (string.IsNullOrEmpty(font.Key) || font.Value == null)
			{
				throw new ArgumentException("Font name and data must be specified.", nameof(config));
			}
		}

		if (config.DefaultFonts.Count == 0)
		{
			throw new ArgumentException("At least one default font must be specified in the configuration.", nameof(config));
		}

		foreach (KeyValuePair<string, byte[]> font in config.DefaultFonts)
		{
			if (string.IsNullOrEmpty(font.Key) || font.Value == null)
			{
				throw new ArgumentException("Default font name and data must be specified.", nameof(config));
			}
		}
	}

	internal static void RenderWithDefaultFont(Action action)
	{
		using (new FontAppearance(FontAppearance.DefaultFontName, FontAppearance.DefaultFontPointSize))
		{
			action();
		}
	}

	internal static void CaptureWindowNormalState()
	{
		if (window?.WindowState == Silk.NET.Windowing.WindowState.Normal)
		{
			LastNormalWindowState = new()
			{
				Size = new(window.Size.X, window.Size.Y),
				Pos = new(window.Position.X, window.Position.Y),
				LayoutState = Silk.NET.Windowing.WindowState.Normal
			};
		}
	}

	internal static ImFontPtr FindBestFontForAppearance(string name, int sizePoints, out float sizePixels)
	{
		ImGuiIOPtr io = ImGui.GetIO();
		ImVector<ImFontPtr> fonts = io.Fonts.Fonts;

		// Calculate the pixel size for this point size
		sizePixels = CalculateOptimalPixelSize(sizePoints);

		// First, try to find the exact font with the requested size
		string exactFontKey = $"{name}_{sizePoints}";
		if (FontIndices.TryGetValue(exactFontKey, out int fontIndex))
		{
			return fonts[fontIndex];
		}

		// If exact size not found, try to find the closest size for this font name
		int closestSize = -1;
		int smallestDifference = int.MaxValue;

		foreach (int size in CommonFontSizes)
		{
			string fontKey = $"{name}_{size}";
			if (FontIndices.ContainsKey(fontKey))
			{
				int difference = Math.Abs(size - sizePoints);
				if (difference < smallestDifference)
				{
					smallestDifference = difference;
					closestSize = size;
				}
			}
		}

		// If we found a closest size, use it
		if (closestSize != -1)
		{
			string closestFontKey = $"{name}_{closestSize}";
			fontIndex = FontIndices[closestFontKey];
			return fonts[fontIndex];
		}

		// Try to get font index for the specified name directly (for backwards compatibility)
		if (FontIndices.TryGetValue(name, out fontIndex))
		{
			return fonts[fontIndex];
		}

		// Fallback to Default font
		if (FontIndices.TryGetValue("Default", out fontIndex))
		{
			return fonts[fontIndex];
		}

		// If no default font, use the first font
		fontIndex = 0;
		return fonts[fontIndex];
	}

	internal static void EnsureWindowPositionIsValid()
	{
		if (window?.Monitor is not null && window.WindowState is not Silk.NET.Windowing.WindowState.Minimized)
		{
			Silk.NET.Maths.Rectangle<int> bounds = window.Monitor.Bounds;
			bool onScreen = bounds.Contains(window.Position) ||
				bounds.Contains(window.Position + new Silk.NET.Maths.Vector2D<int>(window.Size.X, 0)) ||
				bounds.Contains(window.Position + new Silk.NET.Maths.Vector2D<int>(0, window.Size.Y)) ||
				bounds.Contains(window.Position + new Silk.NET.Maths.Vector2D<int>(window.Size.X, window.Size.Y));

			if (!onScreen)
			{
				// If the window is not on a monitor, move it to the primary monitor
				ImGuiAppWindowState defaultWindowState = new();
				System.Numerics.Vector2 halfSize = defaultWindowState.Size / 2;
				window.Size = new((int)defaultWindowState.Size.X, (int)defaultWindowState.Size.Y);
				window.Position = window.Monitor.Bounds.Center - new Silk.NET.Maths.Vector2D<int>((int)halfSize.X, (int)halfSize.Y);
				window.WindowState = defaultWindowState.LayoutState;
			}
		}
	}

	/// <summary>
	/// Renders the application menu using the provided delegate.
	/// </summary>
	/// <param name="menuDelegate">The delegate to render the menu.</param>
	internal static void RenderAppMenu(Action? menuDelegate)
	{
		if (menuDelegate is not null)
		{
			if (ImGui.BeginMainMenuBar())
			{
				menuDelegate();

				if (ImGui.BeginMenu("Debug"))
				{
					if (ImGui.MenuItem("Show ImGui Demo", "", isImGuiDemoVisible))
					{
						if (!isImGuiDemoVisible)
						{
							ShowImGuiDemo();
						}
						else
						{
							isImGuiDemoVisible = false;
						}
					}

					if (ImGui.MenuItem("Show ImGui Metrics", "", isImGuiMetricsVisible))
					{
						if (!isImGuiMetricsVisible)
						{
							ShowImGuiMetrics();
						}
						else
						{
							isImGuiMetricsVisible = false;
						}
					}

					if (ImGui.MenuItem("Show Style Editor", "", isImGuiStyleEditorVisible))
					{
						if (!isImGuiStyleEditorVisible)
						{
							ShowImGuiStyleEditor();
						}
						else
						{
							isImGuiStyleEditorVisible = false;
						}
					}

					ImGui.Separator();

					if (ImGui.MenuItem("Show Performance Monitor", "", isPerformanceMonitorVisible))
					{
						if (!isPerformanceMonitorVisible)
						{
							ShowPerformanceMonitor();
						}
						else
						{
							isPerformanceMonitorVisible = false;
						}
					}

					ImGui.EndMenu();
				}

				ImGui.EndMainMenuBar();
			}
		}
	}

	/// <summary>
	/// Renders the main window contents and handles ImGui demo and metrics windows.
	/// </summary>
	/// <param name="tickDelegate">The delegate to render the main window contents.</param>
	/// <param name="dt">The delta time since the last frame.</param>
	internal static void RenderWindowContents(Action<float>? tickDelegate, float dt)
	{
		bool b = true;
		ImGui.SetNextWindowSize(ImGui.GetMainViewport().WorkSize, ImGuiCond.Always);
		ImGui.SetNextWindowPos(ImGui.GetMainViewport().WorkPos);
		ImGuiStylePtr style = ImGui.GetStyle();
		System.Numerics.Vector4 borderColor = style.Colors[(int)ImGuiCol.Border];
		if (ImGui.Begin("##mainWindow", ref b, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoBringToFrontOnFocus))
		{
			style.Colors[(int)ImGuiCol.Border] = borderColor;
			tickDelegate?.Invoke(dt);
		}

		ImGui.End();

		if (isImGuiDemoVisible)
		{
			// Position the demo window to ensure it's visible and accessible
			ImGui.SetNextWindowPos(new System.Numerics.Vector2(50, 50), ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSize(new System.Numerics.Vector2(800, 600), ImGuiCond.FirstUseEver);

			// Focus on first frame after opening for good UX
			if (shouldFocusImGuiDemo)
			{
				ImGui.SetNextWindowFocus();
				shouldFocusImGuiDemo = false;
			}

			ImGui.ShowDemoWindow(ref isImGuiDemoVisible);
		}

		if (isImGuiMetricsVisible)
		{
			// Position the metrics window to ensure it's visible and accessible
			ImGui.SetNextWindowPos(new System.Numerics.Vector2(100, 100), ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSize(new System.Numerics.Vector2(600, 500), ImGuiCond.FirstUseEver);

			// Focus on first frame after opening for good UX
			if (shouldFocusImGuiMetrics)
			{
				ImGui.SetNextWindowFocus();
				shouldFocusImGuiMetrics = false;
			}

			ImGui.ShowMetricsWindow(ref isImGuiMetricsVisible);
		}

		if (isImGuiStyleEditorVisible)
		{
			// Position and configure the style editor window for better accessibility
			ImGui.SetNextWindowPos(new System.Numerics.Vector2(150, 150), ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSize(new System.Numerics.Vector2(500, 400), ImGuiCond.FirstUseEver);

			// Focus on first frame after opening for good UX
			if (shouldFocusImGuiStyleEditor)
			{
				ImGui.SetNextWindowFocus();
				shouldFocusImGuiStyleEditor = false;
			}

			if (ImGui.Begin("Style Editor", ref isImGuiStyleEditorVisible))
			{
				ImGui.ShowStyleEditor();
			}

			ImGui.End();
		}
	}

	/// <summary>
	/// Sets the window icon using the specified icon file path.
	/// </summary>
	/// <param name="iconPath">The file path to the icon image.</param>
	public static void SetWindowIcon(string iconPath)
	{
		using FileStream stream = File.OpenRead(iconPath);
		using Image<Rgba32> sourceImage = Image.Load<Rgba32>(stream);

		int[] iconSizes = [128, 64, 48, 32, 28, 24, 22, 20, 18, 16];

		Collection<Silk.NET.Core.RawImage> icons = [];

		foreach (int size in iconSizes)
		{
			Image<Rgba32> resizeImage = sourceImage.Clone();
			int sourceSize = Math.Min(sourceImage.Width, sourceImage.Height);
			resizeImage.Mutate(x => x.Crop(sourceSize, sourceSize).Resize(size, size, KnownResamplers.Welch));

			UseImageBytes(resizeImage, bytes =>
			{
				// Create a permanent copy since RawImage needs to keep the data
				byte[] iconData = new byte[bytes.Length];
				Array.Copy(bytes, iconData, bytes.Length);
				icons.Add(new(size, size, new Memory<byte>(iconData)));
			});
		}

		Invoker.Invoke(() => window?.SetWindowIcon([.. icons]));
	}

	internal static readonly ArrayPool<byte> _bytePool = ArrayPool<byte>.Shared;

	/// <summary>
	/// Gets or loads a texture from the specified file path with optimized memory usage.
	/// </summary>
	public static ImGuiAppTextureInfo GetOrLoadTexture(AbsoluteFilePath path)
	{
		// Check if the texture is already loaded
		if (Textures.TryGetValue(path, out ImGuiAppTextureInfo? existingTexture))
		{
			return existingTexture;
		}

		using Image<Rgba32> image = Image.Load<Rgba32>(path);

		ImGuiAppTextureInfo textureInfo = new()
		{
			Path = path,
			Width = image.Width,
			Height = image.Height
		};

		UseImageBytes(image, bytes =>
		{
			textureInfo.TextureId = UploadTextureRGBA(bytes, image.Width, image.Height);
			unsafe
			{
				textureInfo.TextureRef = new ImTextureRef(default, textureInfo.TextureId);
			}
		});

		Textures[path] = textureInfo;
		return textureInfo;
	}

	/// <summary>
	/// Executes an action with temporary access to image bytes using pooled memory for efficiency.
	/// The bytes are returned to the pool after the action completes.
	/// </summary>
	/// <param name="image">The image to process.</param>
	/// <param name="action">The action to perform with the image bytes.</param>
	public static void UseImageBytes(Image<Rgba32> image, Action<byte[]> action)
	{
		ArgumentNullException.ThrowIfNull(image);
		ArgumentNullException.ThrowIfNull(action);

		int bufferSize = image.Width * image.Height * Unsafe.SizeOf<Rgba32>();

		// Rent buffer from pool
		byte[] pooledBuffer = _bytePool.Rent(bufferSize);
		try
		{
			// Copy the image data to the pooled buffer
			image.CopyPixelDataTo(pooledBuffer.AsSpan(0, bufferSize));

			// Execute the action with the pooled buffer
			action(pooledBuffer);
		}
		finally
		{
			// Always return the buffer to the pool
			_bytePool.Return(pooledBuffer);
		}
	}

	/// <summary>
	/// Tries to get a texture from the texture dictionary without loading it.
	/// </summary>
	/// <param name="path">The path to the texture file.</param>
	/// <param name="textureInfo">When this method returns, contains the texture information if the texture is found; otherwise, null.</param>
	/// <returns>true if the texture was found; otherwise, false.</returns>
	public static bool TryGetTexture(AbsoluteFilePath path, out ImGuiAppTextureInfo? textureInfo) => Textures.TryGetValue(path, out textureInfo);

	/// <summary>
	/// Tries to get a texture from the texture dictionary without loading it.
	/// </summary>
	/// <param name="path">The path to the texture file as a string.</param>
	/// <param name="textureInfo">When this method returns, contains the texture information if the texture is found; otherwise, null.</param>
	/// <returns>true if the texture was found; otherwise, false.</returns>
	public static bool TryGetTexture(string path, out ImGuiAppTextureInfo? textureInfo) => TryGetTexture(path.As<AbsoluteFilePath>(), out textureInfo);

	/// <summary>
	/// Uploads a texture to the GPU using the specified RGBA byte array, width, and height.
	/// </summary>
	internal static uint UploadTextureRGBA(byte[] bytes, int width, int height)
	{
		return Invoker.Invoke(() =>
		{
			if (gl is null)
			{
				throw new InvalidOperationException("OpenGL context is not initialized.");
			}

			// Upload texture to graphics system
			gl.GetInteger(GLEnum.TextureBinding2D, out int previousTextureId);

			nint textureHandle = Marshal.AllocHGlobal(bytes.Length);
			try
			{
				Marshal.Copy(bytes, 0, textureHandle, bytes.Length);
				Texture texture = new(gl, width, height, textureHandle, pxFormat: PixelFormat.Rgba);
				texture.Bind();
				texture.SetMagFilter(TextureMagFilter.Linear);
				texture.SetMinFilter(TextureMinFilter.Linear);

				// Restore state
				gl.BindTexture(GLEnum.Texture2D, (uint)previousTextureId);

				return texture.GlTexture;
			}
			finally
			{
				Marshal.FreeHGlobal(textureHandle);
			}
		});
	}

	/// <summary>
	/// Deletes the specified texture from the GPU.
	/// </summary>
	/// <param name="textureId">The OpenGL texture ID to delete.</param>
	/// <exception cref="InvalidOperationException">Thrown if the OpenGL context is not initialized.</exception>
	public static void DeleteTexture(uint textureId)
	{
		Invoker.Invoke(() =>
		{
			if (gl is null)
			{
				throw new InvalidOperationException("OpenGL context is not initialized.");
			}

			gl.DeleteTexture(textureId);
			Textures.Where(x => x.Value.TextureId == textureId).ToList().ForEach(x => Textures.Remove(x.Key, out ImGuiAppTextureInfo? _));
		});
	}

	/// <summary>
	/// Deletes the specified texture from the GPU.
	/// </summary>
	/// <param name="textureInfo">The texture info containing the texture ID to delete.</param>
	/// <exception cref="InvalidOperationException">Thrown if the OpenGL context is not initialized.</exception>
	/// <exception cref="ArgumentNullException">Thrown if the textureInfo is null.</exception>
	public static void DeleteTexture(ImGuiAppTextureInfo textureInfo) => DeleteTexture(textureInfo?.TextureId ?? throw new ArgumentNullException(nameof(textureInfo)));

	internal static void UpdateDpiScale()
	{
		float newScaleFactor = (float)ForceDpiAware.GetWindowScaleFactor();

		// Only update if the scale factor changed significantly (more than 1% difference)
		if (Math.Abs(ScaleFactor - newScaleFactor) > 0.01f)
		{
			float oldScaleFactor = ScaleFactor;
			ScaleFactor = newScaleFactor;

			// Only reload fonts if scale factor changed significantly (more than 5% difference)
			// This prevents unnecessary font reloading for minor DPI changes
			if (controller?.FontsConfigured == true && Math.Abs(oldScaleFactor - newScaleFactor) > 0.05f)
			{
				InitFonts();
			}
		}
	}

	// Using the new ImGui 1.92.0 dynamic font system
	// Fonts can now be rendered at any size dynamically - no need to preload multiple sizes!
	internal static unsafe void InitFonts()
	{
		DebugLogger.Log("InitFonts: Starting font initialization");

		// Only load fonts if they haven't been loaded or if scale factor has changed
		if (controller?.FontsConfigured == true && Math.Abs(lastFontScaleFactor - ScaleFactor) < 0.01f)
		{
			DebugLogger.Log("InitFonts: Skipping font reload - already configured");
			return; // Skip reloading fonts if they're already loaded and scale hasn't changed
		}

		lastFontScaleFactor = ScaleFactor;

		ImGuiIOPtr io = ImGui.GetIO();
		ImFontAtlasPtr fontAtlasPtr = io.Fonts;

		// Clear existing font data and indices
		fontAtlasPtr.Clear();
		FontIndices.Clear();

		// Track fonts that need disposal after rebuilding the atlas
		List<GCHandle> fontPinnedData = [];

		// Load fonts from configuration at multiple sizes
		IEnumerable<KeyValuePair<string, byte[]>> fontsToLoad = Config.Fonts.Concat(Config.DefaultFonts);
		int defaultFontIndex = -1;

		// Pre-allocate emoji font memory outside the loops for reuse
		nint emojiHandle = IntPtr.Zero;
		int emojiLength = 0;
		byte[]? emojiBytes = ImGuiAppConfig.EmojiFont;
		if (emojiBytes != null)
		{
			emojiHandle = Marshal.AllocHGlobal(emojiBytes.Length);
			currentFontMemoryHandles.Add(emojiHandle);
			Marshal.Copy(emojiBytes, 0, emojiHandle, emojiBytes.Length);
			emojiLength = emojiBytes.Length;
		}

		foreach ((string name, byte[] fontBytes) in fontsToLoad)
		{
			// Pre-allocate main font memory outside the size loop for reuse
			nint fontHandle = Marshal.AllocHGlobal(fontBytes.Length);
			currentFontMemoryHandles.Add(fontHandle);
			Marshal.Copy(fontBytes, 0, fontHandle, fontBytes.Length);

			foreach (int size in CommonFontSizes)
			{
				LoadFontFromMemory($"{name}_{size}", fontHandle, fontBytes.Length, fontAtlasPtr, size);

				// Load and merge emoji font immediately after main font for proper merging
				if (emojiHandle != IntPtr.Zero)
				{
					LoadEmojiFontFromMemory(emojiHandle, emojiLength, fontAtlasPtr, size);
				}

				// Prioritize DefaultFonts over custom Fonts for setting the default font
				if (size == FontAppearance.DefaultFontPointSize)
				{
					// If this is from DefaultFonts, use it as the default font
					if (Config.DefaultFonts.ContainsKey(name))
					{
						defaultFontIndex = FontIndices[$"{name}_{size}"];
					}
					// If no DefaultFonts font has been set yet, use the first custom font as fallback
					else if (defaultFontIndex == -1)
					{
						defaultFontIndex = FontIndices[$"{name}_{size}"];
					}
				}
			}
		}

		// Set the font indices for the default font
		if (defaultFontIndex != -1)
		{
			FontIndices["default"] = defaultFontIndex; // Store with "default" key for FontAppearance.DefaultFontName
			FontIndices["Default"] = defaultFontIndex; // Store with "Default" key for FindBestFontForAppearance fallback
			FontIndices[$"Default_{FontAppearance.DefaultFontPointSize}"] = defaultFontIndex; // Store with "Default_12" key for compatibility
		}

		// Add ImGui default font as fallback if no custom fonts were loaded
		if (defaultFontIndex == -1)
		{
			defaultFontIndex = fontAtlasPtr.Fonts.Size;
			fontAtlasPtr.AddFontDefault();
			FontIndices[$"Default_{FontAppearance.DefaultFontPointSize}"] = defaultFontIndex;
			FontIndices["default"] = defaultFontIndex;
			FontIndices["Default"] = defaultFontIndex;
		}

		// Build the font atlas to generate the texture
		ImGuiP.ImFontAtlasBuildMain(fontAtlasPtr);

		// Set the default font for ImGui rendering
		if (defaultFontIndex != -1 && defaultFontIndex < fontAtlasPtr.Fonts.Size)
		{
			io.FontDefault = fontAtlasPtr.Fonts[defaultFontIndex];
		}

		// Store the pinned font data for later cleanup
		StorePinnedFontData(fontPinnedData);
		DebugLogger.Log("InitFonts: Font initialization completed");
	}

	/// <summary>
	/// Loads a font from pre-allocated memory into the font atlas with custom glyph ranges.
	/// This method reuses already allocated memory to avoid duplicate allocations for multiple font sizes.
	/// </summary>
	/// <param name="name">The name of the font.</param>
	/// <param name="fontHandle">The handle to pre-allocated font data.</param>
	/// <param name="fontLength">The length of the font data in bytes.</param>
	/// <param name="fontAtlasPtr">The ImGui font atlas.</param>
	/// <param name="pointSize">The point size for the font.</param>
	/// <param name="glyphRanges">Custom glyph ranges, or null for default ranges.</param>
	internal static unsafe void LoadFontFromMemory(string name, nint fontHandle, int fontLength, ImFontAtlasPtr fontAtlasPtr, int pointSize, uint* glyphRanges = null)
	{
		// Calculate optimal pixel size for the font
		float fontSize = CalculateOptimalPixelSize(pointSize);

		// Create font configuration
		ImFontConfigPtr fontConfig = ImGui.ImFontConfig();
		fontConfig.FontDataOwnedByAtlas = false; // We manage the memory ourselves
		fontConfig.PixelSnapH = true;

		// Use custom glyph ranges if provided, otherwise use extended Unicode ranges if enabled
		uint* ranges = glyphRanges != null ? glyphRanges : (Config.EnableUnicodeSupport ? FontHelper.GetExtendedUnicodeRanges(fontAtlasPtr) : fontAtlasPtr.GetGlyphRangesDefault());

		// Add font to atlas using pre-allocated memory
		int fontIndex = fontAtlasPtr.Fonts.Size;
		fontAtlasPtr.AddFontFromMemoryTTF((void*)fontHandle, fontLength, fontSize, fontConfig, ranges);

		// Store the font index for later retrieval
		FontIndices[name] = fontIndex;
	}

	/// <summary>
	/// Loads emoji font from pre-allocated memory and merges it with the previously loaded font for emoji support.
	/// This method reuses already allocated emoji font memory to avoid duplicate allocations for multiple font sizes.
	/// </summary>
	/// <param name="emojiHandle">The handle to pre-allocated emoji font data.</param>
	/// <param name="emojiLength">The length of the emoji font data in bytes.</param>
	/// <param name="fontAtlasPtr">The ImGui font atlas.</param>
	/// <param name="size">The font size to load emoji font for.</param>
	internal static unsafe void LoadEmojiFontFromMemory(nint emojiHandle, int emojiLength, ImFontAtlasPtr fontAtlasPtr, int size)
	{
		// Get emoji-specific ranges (separate from main font to avoid conflicts)
		uint* emojiRanges = FontHelper.GetEmojiRanges();

		// Calculate optimal pixel size for the emoji font
		float fontSize = CalculateOptimalPixelSize(size);

		// Create font configuration for merging
		ImFontConfigPtr emojiConfig = ImGui.ImFontConfig();
		emojiConfig.FontDataOwnedByAtlas = false; // We manage the memory ourselves
		emojiConfig.PixelSnapH = true;
		emojiConfig.MergeMode = true; // This is the key - merge with previous font
		emojiConfig.GlyphMinAdvanceX = fontSize; // Ensure emoji have consistent width

		// Merge emoji font with the previously added font using pre-allocated memory
		fontAtlasPtr.AddFontFromMemoryTTF((void*)emojiHandle, emojiLength, fontSize, emojiConfig, emojiRanges);
	}

	/// <summary>
	/// Calculates the optimal pixel size for a given point size based on current scale factor.
	/// </summary>
	/// <param name="pointSize">The desired point size.</param>
	/// <returns>The optimal pixel size for crisp rendering.</returns>
	internal static float CalculateOptimalPixelSize(int pointSize) =>
		// Round to exact pixels for crisp rendering, avoiding fractional sizes that cause blurry text
		Math.Max(1.0f, MathF.Round(pointSize * ScaleFactor));

	internal static void StorePinnedFontData(List<GCHandle> newPinnedData)
	{
		// Free old font data handles before storing new ones to prevent memory leak
		foreach (GCHandle handle in currentPinnedFontData)
		{
			try
			{
				if (handle.IsAllocated)
				{
					handle.Free();
				}
			}
			catch (InvalidOperationException)
			{
				// Handle was already freed, ignore
			}
		}

		// Clear the old list and store the new handles
		currentPinnedFontData.Clear();
		currentPinnedFontData.AddRange(newPinnedData);

		// Note: currentFontMemoryHandles is managed separately in LoadFont method
		// and will be cleaned up by CleanupPinnedFontData when needed
	}

	/// <inheritdoc/>
	public static void CleanupAllTextures()
	{
		if (gl == null)
		{
			return;
		}

		// Make a copy of the keys to avoid collection modification issues
		List<AbsoluteFilePath> texturesToRemove = [.. Textures.Keys];

		foreach (AbsoluteFilePath? texturePath in texturesToRemove)
		{
			if (Textures.TryGetValue(texturePath, out ImGuiAppTextureInfo? info))
			{
				DeleteTexture(info.TextureId);
			}
		}

		Textures.Clear();
	}

	/// <summary>
	/// Converts a value in ems to pixels based on the current ImGui font size.
	/// </summary>
	/// <param name="ems">The value in ems to convert to pixels.</param>
	/// <returns>The equivalent value in pixels.</returns>
	public static int EmsToPx(float ems)
	{
		// if imgui is not initialized, use default font size
		return controller is null
			? (int)(ems * FontAppearance.DefaultFontPointSize)
			: Invoker.Invoke(() => (int)(ems * ImGui.GetFontSize()));
	}

	/// <summary>
	/// Converts a value in points to pixels based on the current scale factor.
	/// </summary>
	/// <param name="pts">The value in points to convert to pixels.</param>
	/// <returns>The equivalent value in pixels.</returns>
	public static int PtsToPx(int pts) => (int)(pts * ScaleFactor);

	/// <summary>
	/// Resets all static state for testing purposes.
	/// </summary>
	internal static void Reset()
	{
		window = null;
		gl = null;
		controller = null;
		inputContext = null;
		glProvider = null;
		LastNormalWindowState = new();
		FontIndices.Clear();
		lastFontScaleFactor = 0;
		currentPinnedFontData.Clear();
		currentFontMemoryHandles.Clear();
		Invoker = null!;
		IsFocused = true;
		IsIdle = false;
		lastInputTime = DateTime.UtcNow;
		targetFrameTimeMs = 1000.0 / 30.0;
		isImGuiMetricsVisible = false;
		isImGuiDemoVisible = false;
		isImGuiStyleEditorVisible = false;
		isPerformanceMonitorVisible = false;

		shouldFocusImGuiDemo = false;
		shouldFocusImGuiMetrics = false;
		shouldFocusImGuiStyleEditor = false;
		shouldFocusPerformanceMonitor = false;

		performanceFrameTimes.Clear();
		performanceFrameTimeSum = 0;
		performanceFpsHistory.Clear();
		performanceLastFpsUpdateTime = 0;
		performanceFrameCount = 0;
		ScaleFactor = 1;
		Textures.Clear();
		Config = new();
	}

	/// <summary>
	/// Checks if the OpenGL context has changed and handles texture reloading if needed
	/// </summary>
	internal static void CheckAndHandleContextChange()
	{
		if (gl == null)
		{
			return;
		}

		// Get the current context handle
		nint newContextHandle;
		unsafe
		{
			newContextHandle = (nint)ImGui.GetCurrentContext().Handle;
		}

		// If context has changed, reload all textures
		if (newContextHandle != currentGLContextHandle && newContextHandle != nint.Zero)
		{
			currentGLContextHandle = newContextHandle;
			ReloadAllTextures();
		}
	}

	/// <summary>
	/// Reloads all previously loaded textures in the new context
	/// </summary>
	internal static void ReloadAllTextures()
	{
		if (gl == null)
		{
			return;
		}

		// Make a copy to avoid modification issues during iteration
		List<KeyValuePair<AbsoluteFilePath, ImGuiAppTextureInfo>> texturesToReload = [.. Textures];

		foreach (KeyValuePair<AbsoluteFilePath, ImGuiAppTextureInfo> texture in texturesToReload)
		{
			try
			{
				AbsoluteFilePath path = texture.Key;
				ImGuiAppTextureInfo oldInfo = texture.Value;

				// Only reload from file if the path exists
				if (File.Exists(path))
				{
					using Image<Rgba32> image = Image.Load<Rgba32>(path);
					uint oldTextureId = oldInfo.TextureId;

					// Upload new texture
					UseImageBytes(image, bytes =>
					{
						oldInfo.TextureId = UploadTextureRGBA(bytes, image.Width, image.Height);
						unsafe
						{
							oldInfo.TextureRef = new ImTextureRef(default, oldInfo.TextureId);
						}
					});

					// No need to delete old texture as the context is already gone
				}
			}
			catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException)
			{
			}
		}
	}

	/// <summary>
	/// Updates performance monitoring metrics.
	/// </summary>
	/// <param name="dt">The delta time since the last frame.</param>
	internal static void UpdatePerformanceMonitoring(float dt)
	{
		performanceFrameTimes.Enqueue(dt);
		performanceFrameTimeSum += dt;

		while (performanceFrameTimes.Count > 60) // Keep last 60 frames
		{
			performanceFrameTimeSum -= performanceFrameTimes.Dequeue();
		}

		// Track FPS over time (update every 0.5 seconds for smoother graph)
		performanceFrameCount++;
		performanceLastFpsUpdateTime += dt;

		if (performanceLastFpsUpdateTime >= 0.5f)
		{
			float currentFps = performanceFrameCount / performanceLastFpsUpdateTime;
			performanceFpsHistory.Enqueue(currentFps);

			while (performanceFpsHistory.Count > 120) // Keep last 2 minutes of data (120 * 0.5s = 60s)
			{
				performanceFpsHistory.Dequeue();
			}

			performanceFrameCount = 0;
			performanceLastFpsUpdateTime = 0;
		}
	}

	/// <summary>
	/// Renders the performance monitor window.
	/// </summary>
	internal static void RenderPerformanceMonitor()
	{
		if (!isPerformanceMonitorVisible)
		{
			return;
		}

		// Position the performance monitor window to ensure it's visible and accessible
		ImGui.SetNextWindowPos(new System.Numerics.Vector2(200, 200), ImGuiCond.FirstUseEver);
		ImGui.SetNextWindowSize(new System.Numerics.Vector2(500, 350), ImGuiCond.FirstUseEver);

		// Focus on first frame after opening for good UX
		if (shouldFocusPerformanceMonitor)
		{
			ImGui.SetNextWindowFocus();
			shouldFocusPerformanceMonitor = false;
		}

		if (ImGui.Begin("Performance Monitor", ref isPerformanceMonitorVisible))
		{
			ImGui.TextWrapped("This window shows the current performance state and throttling behavior.");
			ImGui.Separator();

			ImGui.Text($"Window Focused: {IsFocused}");
			ImGui.Text($"Application Idle: {IsIdle}");
			ImGui.Text($"Window Visible: {IsVisible}");

			ImGui.Separator();
			ImGui.TextWrapped("Throttling uses sleep-based timing to control frame rate and save resources. It evaluates all conditions and uses the lowest frame rate. VSync is disabled to allow proper frame rate control below monitor refresh rate.");
			ImGui.Text("Rates: Focused=30 FPS, Unfocused=5 FPS, Idle=10 FPS, Not Visible=2 FPS (0.5s per frame)");
			ImGui.TextWrapped("The system automatically selects the lowest applicable rate using Thread.Sleep for precise timing control. Try combining conditions (e.g., unfocused + idle, or minimized) to see the effect.");

			// Real-time FPS graph
			ImGui.Separator();
			ImGui.Text("Real-Time Frame Rate Graph:");

			if (performanceFpsHistory.Count > 1)
			{
				float[] fpsArray = [.. performanceFpsHistory];
				float currentFps = fpsArray[^1];
				float avgFps = performanceFrameTimes.Count > 0 ? performanceFrameTimes.Count / performanceFrameTimeSum : 0;

				ImGui.Text($"Current FPS: {currentFps:F1} | Average FPS: {avgFps:F1}");

				// Show PID controller diagnostics
				ImGui.Separator();
				ImGui.Text("PID Frame Limiter Diagnostics:");
				double currentTargetFps = 1000.0 / targetFrameTimeMs;
				ImGui.Text($"Target: {currentTargetFps:F1} FPS ({targetFrameTimeMs:F1}ms)");
				ImGui.Text(frameLimiter.GetDiagnosticInfo());
				(double currentKp, double currentKi, double currentKd) = frameLimiter.GetCurrentParameters();
				ImGui.Text($"PID Gains: Kp={currentKp:F3}, Ki={currentKi:F3}, Kd={currentKd:F3}");
				ImGui.TextWrapped("The PID controller uses high-precision timing (Thread.Sleep + Stopwatch spin-wait) for accurate frame rate control, replacing simple Thread.Sleep limitations for better precision at higher frame rates. VSync is disabled to prevent interference with frame limiting.");

				// PID Auto-Tuning Section
				ImGui.Separator();
				ImGui.Text("PID Auto-Tuning:");

				(bool isTuningActive, int currentStep, int totalSteps, double progressPercent, PidFrameLimiter.TuningResult? bestResult, string phase) = frameLimiter.GetTuningStatusDetailed();

				if (isTuningActive)
				{
					ImGui.Text($"Phase: {phase}");
					ImGui.Text($"Progress: Step {currentStep}/{totalSteps} ({progressPercent:F1}%)");
					ImGui.ProgressBar((float)(progressPercent / 100.0), new System.Numerics.Vector2(300, 0));
					ImGui.TextColored(new System.Numerics.Vector4(1.0f, 0.8f, 0.4f, 1.0f), "⚠️ Throttling disabled during tuning for consistent results");
					ImGui.TextWrapped("Comprehensive tuning: Coarse (8s/test) → Fine (12s/test) → Precision (15s/test)");

					if (ImGui.Button("Stop Auto-Tuning"))
					{
						frameLimiter.StopAutoTuning();
					}
				}
				else
				{
					ImGui.TextWrapped("Comprehensive auto-tuning tests different PID parameter combinations across multiple phases to find optimal settings for maximum accuracy. Throttling is automatically disabled during tuning for consistent results.");
					ImGui.TextWrapped("• Phase 1: Coarse tuning (8s per test, 24 parameters)");
					ImGui.TextWrapped("• Phase 2: Fine tuning (12s per test, 25 parameters around best result)");
					ImGui.TextWrapped("• Phase 3: Precision tuning (15s per test, 9 parameters for final optimization)");
					ImGui.TextColored(new System.Numerics.Vector4(1.0f, 0.6f, 0.6f, 1.0f), "⏱️ Total time: ~12-15 minutes for maximum accuracy");

					if (ImGui.Button("Start Comprehensive Auto-Tuning"))
					{
						frameLimiter.StartAutoTuning();
					}

					if (bestResult.HasValue)
					{
						ImGui.Text("Best tuned parameters:");
						ImGui.Text($"  Kp: {bestResult.Value.Kp:F3}, Ki: {bestResult.Value.Ki:F3}, Kd: {bestResult.Value.Kd:F3}");
						ImGui.Text($"  Score: {bestResult.Value.Score:F3} (Avg Error: {bestResult.Value.AverageError:F2}ms)");
					}
				}

				// Show current throttling state
				string currentState = "Unknown";
				if (isTuningActive)
				{
					currentState = "Throttling Disabled (PID Tuning Active)";
				}
				else if (!IsVisible)
				{
					currentState = "Not Visible (2 FPS target)";
				}
				else if (!IsFocused && IsIdle)
				{
					currentState = "Unfocused + Idle (5 FPS target)";
				}
				else if (!IsFocused)
				{
					currentState = "Unfocused (5 FPS target)";
				}
				else if (IsIdle)
				{
					currentState = "Idle (10 FPS target)";
				}
				else
				{
					currentState = "Focused (30 FPS target)";
				}

				ImGui.Text($"Current State: {currentState}");

				// Create the plot
				ImGui.PlotLines("##FPS", ref fpsArray[0], fpsArray.Length, 0,
					$"FPS Over Time (Last {performanceFpsHistory.Count * 0.5f:F0}s)", 0.0f, 35.0f,
					new System.Numerics.Vector2(400, 100));

				ImGui.TextWrapped("Graph shows FPS over the last 60 seconds. Watch it change as you focus/unfocus the window or minimize it!");
				ImGui.Separator();
				ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.9f, 0.7f, 1.0f), "💡 Testing Tips:");
				ImGui.BulletText("Click on another window to see unfocused throttling (5 FPS)");
				ImGui.BulletText("Stop moving the mouse for 5+ seconds to see idle throttling (10 FPS)");
				ImGui.BulletText("Minimize this window to see not visible throttling (2 FPS)");
				ImGui.BulletText("The lowest applicable rate always wins!");
			}
			else
			{
				ImGui.Text("Collecting data for graph...");
			}
		}

		ImGui.End();
	}
}
