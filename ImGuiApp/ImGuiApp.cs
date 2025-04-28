// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp;

using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
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

	private static int[] SupportedPixelFontSizes { get; } = [12, 13, 14, 16, 18, 20, 24, 28, 32, 40, 48];
	private static ConcurrentDictionary<string, ConcurrentDictionary<int, int>> FontIndices { get; } = [];
	private static float lastFontScaleFactor;
	private static List<GCHandle> currentPinnedFontData = [];

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

	private const int SW_HIDE = 0;

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

		var silkWindowOptions = WindowOptions.Default with
		{
			Title = config.Title,
			Size = new((int)config.InitialWindowState.Size.X, (int)config.InitialWindowState.Size.Y),
			Position = new((int)config.InitialWindowState.Pos.X, (int)config.InitialWindowState.Pos.Y),
			WindowState = Silk.NET.Windowing.WindowState.Normal,
			API = new GraphicsAPI(
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
			if (!string.IsNullOrEmpty(config.IconPath))
			{
				SetWindowIcon(config.IconPath);
			}

			var glFactory = new WindowOpenGLFactory(window);
			glProvider = new OpenGLProvider(glFactory);
			var glWrapper = (GLWrapper)glProvider.GetGL();
			gl = glWrapper.UnderlyingGL;
			inputContext = window.CreateInput();

			controller = new(
				gl,
				view: window,
				input: inputContext,
				onConfigureIO: () =>
				{
					currentGLContextHandle = ImGui.GetCurrentContext();
					UpdateDpiScale();
					InitFonts();
					config.OnStart?.Invoke();
				}
			);

			ImGui.GetStyle().WindowRounding = 0;
			window.WindowState = config.InitialWindowState.LayoutState;
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
		var currentFps = window!.FramesPerSecond;
		var currentUps = window.UpdatesPerSecond;
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
			gl?.Clear((uint)ClearBufferMask.ColorBufferBit);

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
		FindBestFontForAppearance(FontAppearance.DefaultFontName, FontAppearance.DefaultFontPointSize, out var bestFontSize);
		var scaleRatio = bestFontSize / (float)FontAppearance.DefaultFontPointSize;
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
		foreach (var handle in currentPinnedFontData)
		{
			if (handle.IsAllocated)
			{
				handle.Free();
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
		ArgumentNullException.ThrowIfNull(config);

		if (window != null)
		{
			throw new InvalidOperationException("Application is already running.");
		}

		Invoker = new();
		Config = config;

		ValidateConfig(config);

		ForceDpiAware.Windows();

		InitializeWindow(config);
		SetupWindowLoadHandler(config);
		SetupWindowResizeHandler(config);
		SetupWindowMoveHandler(config);
		SetupWindowUpdateHandler(config);
		SetupWindowRenderHandler(config);
		SetupWindowClosingHandler();

		window!.FocusChanged += (focused) => IsFocused = focused;

		if (!config.TestMode)
		{
			// Hide console window only in non-test mode
			var handle = NativeMethods.GetConsoleWindow();
			NativeMethods.ShowWindow(handle, SW_HIDE);

			window.Run();
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

		foreach (var font in config.Fonts)
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

		foreach (var font in config.DefaultFonts)
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

	internal static ImFontPtr FindBestFontForAppearance(string name, int sizePoints, out int sizePixels)
	{
		var io = ImGui.GetIO();
		var fonts = io.Fonts.Fonts;
		sizePixels = PtsToPx(sizePoints);
		var sizePixelsLocal = sizePixels;

		var candidatesByFace = FontIndices
			.Where(f => f.Key == name)
			.SelectMany(f => f.Value)
			.OrderBy(f => f.Key)
			.ToArray();

		if (candidatesByFace.Length == 0)
		{
			throw new InvalidOperationException($"No fonts found for the specified font appearance: {name} {sizePoints}pt");
		}

		int[] candidatesBySize = [.. candidatesByFace
			.Where(x => x.Key >= sizePixelsLocal)
			.Select(x => x.Value)];

		if (candidatesBySize.Length != 0)
		{
			var bestFontIndex = candidatesBySize.First();
			return fonts[bestFontIndex];
		}

		// if there was no font size larger than our requested size, then fall back to the largest font size we have
		var largestFontIndex = candidatesByFace.Last().Value;
		return fonts[largestFontIndex];
	}

	private static void EnsureWindowPositionIsValid()
	{
		if (window?.Monitor is not null && window.WindowState is not Silk.NET.Windowing.WindowState.Minimized)
		{
			var bounds = window.Monitor.Bounds;
			var onScreen = bounds.Contains(window.Position) ||
				bounds.Contains(window.Position + new Silk.NET.Maths.Vector2D<int>(window.Size.X, 0)) ||
				bounds.Contains(window.Position + new Silk.NET.Maths.Vector2D<int>(0, window.Size.Y)) ||
				bounds.Contains(window.Position + new Silk.NET.Maths.Vector2D<int>(window.Size.X, window.Size.Y));

			if (!onScreen)
			{
				// If the window is not on a monitor, move it to the primary monitor
				var defaultWindowState = new ImGuiAppWindowState();
				var halfSize = defaultWindowState.Size / 2;
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
		var b = true;
		ImGui.SetNextWindowSize(ImGui.GetMainViewport().WorkSize, ImGuiCond.Always);
		ImGui.SetNextWindowPos(ImGui.GetMainViewport().WorkPos);
		var colors = ImGui.GetStyle().Colors;
		var borderColor = colors[(int)ImGuiCol.Border];
		if (ImGui.Begin("##mainWindow", ref b, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings))
		{
			colors[(int)ImGuiCol.Border] = borderColor;
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
		using var stream = File.OpenRead(iconPath);
		using var sourceImage = Image.Load<Rgba32>(stream);

		int[] iconSizes = [128, 64, 48, 32, 28, 24, 22, 20, 18, 16];

		var icons = new Collection<Silk.NET.Core.RawImage>();

		foreach (var size in iconSizes)
		{
			var resizeImage = sourceImage.Clone();
			var sourceSize = Math.Min(sourceImage.Width, sourceImage.Height);
			resizeImage.Mutate(x => x.Crop(sourceSize, sourceSize).Resize(size, size, KnownResamplers.Welch));

			UseImageBytes(resizeImage, bytes =>
			{
				// Create a permanent copy since RawImage needs to keep the data
				var iconData = new byte[bytes.Length];
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
		if (Textures.TryGetValue(path, out var existingTexture))
		{
			return existingTexture;
		}

		using var image = Image.Load<Rgba32>(path);

		var textureInfo = new ImGuiAppTextureInfo
		{
			Path = path,
			Width = image.Width,
			Height = image.Height
		};

		UseImageBytes(image, bytes => textureInfo.TextureId = UploadTextureRGBA(bytes, image.Width, image.Height));

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

		var bufferSize = image.Width * image.Height * Unsafe.SizeOf<Rgba32>();

		// Rent buffer from pool
		var pooledBuffer = _bytePool.Rent(bufferSize);
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
			gl.GetInteger(GLEnum.TextureBinding2D, out var previousTextureId);

			var textureHandle = Marshal.AllocHGlobal(bytes.Length);
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
			Textures.Where(x => x.Value.TextureId == textureId).ToList().ForEach(x => Textures.Remove(x.Key, out var _));
		});
	}

	private static void UpdateDpiScale()
	{
		var newScaleFactor = (float)ForceDpiAware.GetWindowScaleFactor();

		// Only update if the scale factor changed significantly
		if (Math.Abs(ScaleFactor - newScaleFactor) > 0.1f)
		{
			ScaleFactor = newScaleFactor;
			// We'll let InitFonts decide whether to rebuild based on the scale change
		}
	}

	// https://github.com/ocornut/imgui/blob/master/docs/FONTS.md#loading-font-data-from-memory
	// IMPORTANT: AddFontFromMemoryTTF() by default transfer ownership of the data buffer to the font atlas, which will attempt to free it on destruction.
	// This was to avoid an unnecessary copy, and is perhaps not a good API (a future version will redesign it).
	// If you want to keep ownership of the data and free it yourself, you need to clear the FontDataOwnedByAtlas field
	internal static void InitFonts()
	{
		// Only load fonts if they haven't been loaded or if scale factor has changed significantly
		if (controller?.FontsConfigured == true && Math.Abs(lastFontScaleFactor - ScaleFactor) < 0.1f)
		{
			return; // Skip reloading fonts if they're already loaded and scale hasn't changed much
		}

		lastFontScaleFactor = ScaleFactor;

		var fontsToLoad = Config.Fonts.Concat(Config.DefaultFonts);

		var io = ImGui.GetIO();
		var fontAtlasPtr = io.Fonts;

		// Clear existing font data and indices
		fontAtlasPtr.Clear();
		FontIndices.Clear();

		// Track fonts that need disposal after rebuilding the atlas
		List<GCHandle> fontPinnedData = [];

		unsafe
		{
			var fontConfigNativePtr = ImGuiNative.ImFontConfig_ImFontConfig();
			try
			{
				// We'll still tell ImGui not to own the data, but we'll track it ourselves
				fontConfigNativePtr->FontDataOwnedByAtlas = 0;
				fontConfigNativePtr->PixelSnapH = 1;
				fontConfigNativePtr->OversampleH = 2; // Improved oversampling for better quality
				fontConfigNativePtr->OversampleV = 2;
				fontConfigNativePtr->RasterizerMultiply = 1.0f; // Adjust if needed for better rendering

				foreach (var (name, fontBytes) in fontsToLoad)
				{
					if (!FontIndices.TryGetValue(name, out var fontSizes))
					{
						fontSizes = new();
						FontIndices[name] = fontSizes;
					}

					// Pin the font data so the GC doesn't move or collect it while ImGui is using it
					var pinnedFontData = GCHandle.Alloc(fontBytes, GCHandleType.Pinned);
					fontPinnedData.Add(pinnedFontData);

					var fontDataPtr = pinnedFontData.AddrOfPinnedObject();

					foreach (var size in SupportedPixelFontSizes)
					{
						var fontIndex = fontAtlasPtr.Fonts.Size;
						fontAtlasPtr.AddFontFromMemoryTTF(fontDataPtr, fontBytes.Length, size, fontConfigNativePtr);
						fontSizes[size] = fontIndex;
					}
				}

				// Build the font atlas
				var success = fontAtlasPtr.Build();
				if (!success)
				{
					throw new InvalidOperationException("Failed to build ImGui font atlas");
				}
			}
			finally
			{
				// Cleanup the font config
				ImGuiNative.ImFontConfig_destroy(fontConfigNativePtr);
			}
		}

		// Store the pinned font data for later cleanup
		StorePinnedFontData(fontPinnedData);
	}

	private static void StorePinnedFontData(List<GCHandle> newPinnedData)
	{
		// Free any previously pinned font data
		foreach (var handle in currentPinnedFontData)
		{
			if (handle.IsAllocated)
			{
				handle.Free();
			}
		}

		// Store the new pinned data
		currentPinnedFontData = newPinnedData;
	}

	/// <inheritdoc/>
	public static void CleanupAllTextures()
	{
		if (gl == null)
		{
			return;
		}

		// Make a copy of the keys to avoid collection modification issues
		var texturesToRemove = Textures.Keys.ToList();

		foreach (var texturePath in texturesToRemove)
		{
			if (Textures.TryGetValue(texturePath, out var info))
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
		var newContextHandle = ImGui.GetCurrentContext();

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
		var texturesToReload = Textures.ToList();

		foreach (var texture in texturesToReload)
		{
			try
			{
				var path = texture.Key;
				var oldInfo = texture.Value;

				// Only reload from file if the path exists
				if (File.Exists(path))
				{
					using var image = Image.Load<Rgba32>(path);
					var oldTextureId = oldInfo.TextureId;

					// Upload new texture
					UseImageBytes(image, bytes => oldInfo.TextureId = UploadTextureRGBA(bytes, image.Width, image.Height));

					// No need to delete old texture as the context is already gone
				}
			}
			catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException)
			{
			}
		}
	}
}
