// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp;

using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using ktsu.Extensions;
using ktsu.ImGuiApp.ImGuiController;
using ktsu.Invoker;
using ktsu.StrongPaths;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = System.Drawing.Color;
using Texture = ImGuiController.Texture;

/// <summary>
/// Simple file logger for debugging crashes
/// </summary>

/// <summary>
/// Provides static methods and properties to manage the ImGui application.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "This class is the main entry point for the ImGui application and requires many dependencies. Consider refactoring in the future.")]
public static partial class ImGuiApp
{
	private static IWindow? window;
	private static GL? gl;
	private static ImGuiController.ImGuiController? controller;
	private static IInputContext? inputContext;
	private static OpenGLProvider? glProvider;
	private static IntPtr currentGLContextHandle; // Track the current GL context handle

	private static ImGuiAppWindowState LastNormalWindowState { get; set; } = new();

	/// <summary>
	/// Simple file logger for debugging crashes
	/// </summary>
	internal static class DebugLogger
	{
		private static readonly string LogFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ImGuiApp_Debug.log");

		static DebugLogger()
		{
			// Clear previous log file
			try
			{
				if (File.Exists(LogFilePath))
				{
					File.Delete(LogFilePath);
				}
			}
			catch (IOException) { }
			catch (UnauthorizedAccessException) { }
			catch (ArgumentException) { }
			catch (NotSupportedException) { }
		}

		public static void Log(string message)
		{
			try
			{
				string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
				File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
				Console.WriteLine(logEntry);
			}
			catch (IOException) { }
			catch (UnauthorizedAccessException) { }
			catch (ArgumentException) { }
			catch (NotSupportedException) { }
		}
	}

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

	private static ConcurrentDictionary<string, int> FontIndices { get; } = [];
	private static float lastFontScaleFactor;
	private static readonly List<GCHandle> currentPinnedFontData = [];

	/// <summary>
	/// Gets an instance of the <see cref="Invoker"/> class to delegate tasks to the window thread.
	/// </summary>
	public static Invoker Invoker { get; private set; } = null!;

	/// <summary>
	/// Gets a value indicating whether the ImGui application window is focused.
	/// </summary>
	public static bool IsFocused { get; private set; } = true;
	/// <summary>
	/// Gets a value indicating whether the ImGui application window is visible.
	/// </summary>
	public static bool IsVisible => (window?.WindowState != Silk.NET.Windowing.WindowState.Minimized) && (window?.IsVisible ?? false);

	private static bool showImGuiMetrics;
	private static bool showImGuiDemo;

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

	private static ImGuiAppConfig Config { get; set; } = new();

	private static void InitializeWindow(ImGuiAppConfig config)
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

	private static void SetupWindowLoadHandler(ImGuiAppConfig config)
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

						// Configure imgui.ini file saving based on user preference
						if (!config.SaveIniSettings)
						{
							ImGuiIOPtr io = ImGui.GetIO();
							io.IniFilename = null;
						}
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

	private static void SetupWindowResizeHandler(ImGuiAppConfig config)
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

	private static void SetupWindowMoveHandler(ImGuiAppConfig config)
	{
		window!.Move += (p) =>
		{
			CaptureWindowNormalState();
			UpdateDpiScale();
			CheckAndHandleContextChange();
			config.OnMoveOrResize?.Invoke();
		};
	}

	private static void UpdateWindowPerformance()
	{
		double currentFps = window!.FramesPerSecond;
		double currentUps = window.UpdatesPerSecond;
		double requiredFps = IsFocused ? 30 : 5;
		double requiredUps = IsFocused ? 30 : 5;

		if (currentFps != requiredFps)
		{
			window.VSync = false;
			window.FramesPerSecond = requiredFps;
		}

		if (currentUps != requiredUps)
		{
			window.UpdatesPerSecond = requiredUps;
		}
	}

	private static void SetupWindowUpdateHandler(ImGuiAppConfig config)
	{
		window!.Update += (delta) =>
		{
			if (!controller?.FontsConfigured ?? true)
			{
				throw new InvalidOperationException("Fonts are not configured before Update()");
			}

			EnsureWindowPositionIsValid();
			UpdateWindowPerformance();

			controller?.Update((float)delta);
			config.OnUpdate?.Invoke((float)delta);
			Invoker.DoInvokes();
		};
	}

	private static void SetupWindowRenderHandler(ImGuiAppConfig config)
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
				RenderAppMenu(config.OnAppMenu);
				RenderWindowContents(config.OnRender, (float)delta);
			});

			controller?.Render();
		};
	}

	private static void RenderWithScaling(Action renderAction)
	{
		FindBestFontForAppearance(FontAppearance.DefaultFontName, FontAppearance.DefaultFontPointSize, out float bestFontSize);
		float scaleRatio = bestFontSize / FontAppearance.DefaultFontPointSize;
		using (new UIScaler(scaleRatio))
		{
			RenderWithDefaultFont(renderAction);
		}
	}

	private static void SetupWindowClosingHandler()
	{
		window!.Closing += () =>
		{
			CleanupPinnedFontData();
			CleanupController();
			CleanupInputContext();
			CleanupOpenGL();
		};
	}

	private static void CleanupPinnedFontData()
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
	}

	private static void CleanupController()
	{
		controller?.Dispose();
		controller = null;
	}

	private static void CleanupInputContext()
	{
		inputContext?.Dispose();
		inputContext = null;
	}

	private static void CleanupOpenGL()
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

	private static void ValidateConfig(ImGuiAppConfig config)
	{
		if (config.InitialWindowState.Size.X <= 0 || config.InitialWindowState.Size.Y <= 0)
		{
			throw new ArgumentException("Initial window size must be greater than zero.", nameof(config));
		}

		if (config.InitialWindowState.Pos.X < 0 || config.InitialWindowState.Pos.Y < 0)
		{
			throw new ArgumentException("Initial window position must be non-negative.", nameof(config));
		}

		if (config.InitialWindowState.LayoutState == Silk.NET.Windowing.WindowState.Minimized)
		{
			throw new ArgumentException("Initial window state cannot be minimized.", nameof(config));
		}

		if (config.InitialWindowState.LayoutState == Silk.NET.Windowing.WindowState.Fullscreen)
		{
			throw new ArgumentException("Initial window state cannot be fullscreen.", nameof(config));
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

	private static void RenderWithDefaultFont(Action action)
	{
		using (new FontAppearance(FontAppearance.DefaultFontName, FontAppearance.DefaultFontPointSize))
		{
			action();
		}
	}

	private static void CaptureWindowNormalState()
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

		// Try to get font index for the specified name, fallback to Default
		if (!FontIndices.TryGetValue(name, out int fontIndex))
		{
			if (!FontIndices.TryGetValue("Default", out fontIndex))
			{
				// If no default font, use the first font
				fontIndex = 0;
			}
		}

		// In ImGui 1.92.0+, we only load one instance of each font
		// The dynamic system handles scaling when we call PushFont(font, size)
		if (fontIndex < 0 || fontIndex >= fonts.Size)
		{
			// Fallback to first font if index is out of range
			fontIndex = 0;
		}

		return fonts[fontIndex];
	}

	private static void EnsureWindowPositionIsValid()
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
	private static void RenderAppMenu(Action? menuDelegate)
	{
		if (menuDelegate is not null)
		{
			if (ImGui.BeginMainMenuBar())
			{
				menuDelegate();

				if (ImGui.BeginMenu("Debug"))
				{
					if (ImGui.MenuItem("Show ImGui Demo", "", showImGuiDemo))
					{
						showImGuiDemo = !showImGuiDemo;
					}

					if (ImGui.MenuItem("Show ImGui Metrics", "", showImGuiMetrics))
					{
						showImGuiMetrics = !showImGuiMetrics;
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
	private static void RenderWindowContents(Action<float>? tickDelegate, float dt)
	{
		bool b = true;
		ImGui.SetNextWindowSize(ImGui.GetMainViewport().WorkSize, ImGuiCond.Always);
		ImGui.SetNextWindowPos(ImGui.GetMainViewport().WorkPos);
		ImGuiStylePtr style = ImGui.GetStyle();
		System.Numerics.Vector4 borderColor = style.Colors[(int)ImGuiCol.Border];
		if (ImGui.Begin("##mainWindow", ref b, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings))
		{
			style.Colors[(int)ImGuiCol.Border] = borderColor;
			tickDelegate?.Invoke(dt);
		}

		ImGui.End();

		if (showImGuiDemo)
		{
			ImGui.ShowDemoWindow(ref showImGuiDemo);
		}

		if (showImGuiMetrics)
		{
			ImGui.ShowMetricsWindow(ref showImGuiMetrics);
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

	private static readonly ArrayPool<byte> _bytePool = ArrayPool<byte>.Shared;

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
	private static uint UploadTextureRGBA(byte[] bytes, int width, int height)
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

	private static void UpdateDpiScale()
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
		DebugLogger.Log($"InitFonts: Scale factor = {ScaleFactor}");

		ImGuiIOPtr io = ImGui.GetIO();
		ImFontAtlasPtr fontAtlasPtr = io.Fonts;
		DebugLogger.Log("InitFonts: Got font atlas");

		// Clear existing font data and indices
		fontAtlasPtr.Clear();
		FontIndices.Clear();
		DebugLogger.Log("InitFonts: Cleared font atlas and indices");

		// Track fonts that need disposal after rebuilding the atlas
		List<GCHandle> fontPinnedData = [];

		// Add default font first for fallback
		int defaultFontIndex = fontAtlasPtr.Fonts.Size;
		DebugLogger.Log($"InitFonts: Adding default font at index {defaultFontIndex}");
		fontAtlasPtr.AddFontDefault();
		FontIndices["Default"] = defaultFontIndex;
		DebugLogger.Log("InitFonts: Default font added");

		// Temporarily disable custom font loading to isolate crash
		// TODO: Re-enable loading fonts from Config.Fonts and Config.DefaultFonts
		_ = Config.Fonts; // Temporary access to suppress IDE0052 warning
						  // IEnumerable<KeyValuePair<string, byte[]>> fontsToLoad = Config.Fonts.Concat(Config.DefaultFonts);
						  // foreach ((string name, byte[] fontBytes) in fontsToLoad)
						  // {
						  //     LoadFontWithDynamicScaling(name, fontBytes, fontAtlasPtr, fontPinnedData);
						  // }

		// Store the pinned font data for later cleanup
		StorePinnedFontData(fontPinnedData);
		DebugLogger.Log("InitFonts: Font initialization completed");
	}

	/// <summary>
	/// Calculates the optimal pixel size for a given point size based on current scale factor.
	/// </summary>
	/// <param name="pointSize">The desired point size.</param>
	/// <returns>The optimal pixel size for crisp rendering.</returns>
	private static float CalculateOptimalPixelSize(int pointSize) =>
		// With ImGui 1.92.0+, we can use fractional sizes for better scaling
		Math.Max(1.0f, pointSize * ScaleFactor);

	private static void StorePinnedFontData(List<GCHandle> newPinnedData)
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
		Invoker = null!;
		IsFocused = true;
		showImGuiMetrics = false;
		showImGuiDemo = false;
		ScaleFactor = 1;
		Textures.Clear();
		Config = new();
	}

	/// <summary>
	/// Checks if the OpenGL context has changed and handles texture reloading if needed
	/// </summary>
	private static void CheckAndHandleContextChange()
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
	private static void ReloadAllTextures()
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
}
