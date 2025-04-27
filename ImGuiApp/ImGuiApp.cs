namespace ktsu.ImGuiApp;

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using ImGuiNET;

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
public static partial class ImGuiApp
{
	private static IWindow? window;
	private static GL? gl;
	private static ImGuiController.ImGuiController? controller;
	private static IInputContext? inputContext;

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
	public static void Stop() => window?.Close();

	private static ImGuiAppConfig Config { get; set; } = new();

	private static void InitializeWindow(ImGuiAppConfig config)
	{
		var silkWindowOptions = WindowOptions.Default;
		silkWindowOptions.Title = config.Title;
		silkWindowOptions.Size = new((int)config.InitialWindowState.Size.X, (int)config.InitialWindowState.Size.Y);
		silkWindowOptions.Position = new((int)config.InitialWindowState.Pos.X, (int)config.InitialWindowState.Pos.Y);
		silkWindowOptions.WindowState = Silk.NET.Windowing.WindowState.Normal;

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

			gl = window.CreateOpenGL();
			inputContext = window.CreateInput();

			controller = new(
				gl,
				view: window,
				input: inputContext,
				onConfigureIO: () =>
				{
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
			config.OnMoveOrResize?.Invoke();
		};
	}

	private static void SetupWindowMoveHandler(ImGuiAppConfig config)
	{
		window!.Move += (p) =>
		{
			CaptureWindowNormalState();
			UpdateDpiScale();
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
		FindBestFontForAppearance(FontAppearance.DefaultFontName, FontAppearance.DefaultFontPointSize, out int bestFontSize);
		float scaleRatio = bestFontSize / (float)FontAppearance.DefaultFontPointSize;
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
		gl?.Dispose();
		gl = null;
	}

	/// <summary>
	/// Starts the ImGui application with the specified configuration.
	/// </summary>
	/// <param name="config">The configuration settings for the ImGui application.</param>
	public static void Start(ImGuiAppConfig config)
	{
		ArgumentNullException.ThrowIfNull(config);

		Invoker = new();
		Config = config;
		ForceDpiAware.Windows();

		InitializeWindow(config);
		SetupWindowLoadHandler(config);
		SetupWindowResizeHandler(config);
		SetupWindowMoveHandler(config);
		SetupWindowUpdateHandler(config);
		SetupWindowRenderHandler(config);
		SetupWindowClosingHandler();

		window!.FocusChanged += (focused) => IsFocused = focused;

		// Hide console window
		nint handle = NativeMethods.GetConsoleWindow();
		_ = NativeMethods.ShowWindow(handle, SW_HIDE);

		window.Run();
		window.Dispose();
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
		int sizePixelsLocal = sizePixels;

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
			int bestFontIndex = candidatesBySize.First();
			return fonts[bestFontIndex];
		}

		// if there was no font size larger than our requested size, then fall back to the largest font size we have
		int largestFontIndex = candidatesByFace.Last().Value;
		return fonts[largestFontIndex];
	}

	private static void EnsureWindowPositionIsValid()
	{
		if (window?.Monitor is not null && window.WindowState is not Silk.NET.Windowing.WindowState.Minimized)
		{
			var bounds = window.Monitor.Bounds;
			bool onScreen = bounds.Contains(window.Position) ||
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
		bool b = true;
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
	/// Converts an ImageSharp image to a byte array.
	/// </summary>
	/// <param name="image">The ImageSharp image to convert.</param>
	/// <returns>A byte array containing the image data.</returns>
	public static byte[] GetImageBytes(Image<Rgba32> image)
	{
		ArgumentNullException.ThrowIfNull(image);

		byte[] pixelBytes = new byte[image.Width * image.Height * Unsafe.SizeOf<Rgba32>()];
		image.CopyPixelDataTo(pixelBytes);
		return pixelBytes;
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

		foreach (int size in iconSizes)
		{
			var resizeImage = sourceImage.Clone();
			int sourceSize = Math.Min(sourceImage.Width, sourceImage.Height);
			resizeImage.Mutate(x => x.Crop(sourceSize, sourceSize).Resize(size, size, KnownResamplers.Welch));
			byte[] iconData = GetImageBytes(resizeImage);
			icons.Add(new(size, size, new Memory<byte>(iconData)));
		}

		Invoker.Invoke(() => window?.SetWindowIcon([.. icons]));
	}

	/// <summary>
	/// Uploads a texture to the GPU using the specified RGBA byte array, width, and height.
	/// </summary>
	/// <param name="bytes">The byte array containing the texture data in RGBA format.</param>
	/// <param name="width">The width of the texture.</param>
	/// <param name="height">The height of the texture.</param>
	/// <returns>The OpenGL texture ID.</returns>
	/// <exception cref="InvalidOperationException">Thrown if the OpenGL context is not initialized.</exception>
	public static uint UploadTextureRGBA(byte[] bytes, int width, int height)
	{
		uint textureId = Invoker.Invoke(() =>
		{
			if (gl is null)
			{
				throw new InvalidOperationException("OpenGL context is not initialized.");
			}

			// Upload texture to graphics system
			gl.GetInteger(GLEnum.TextureBinding2D, out int previousTextureId);

			nint textureHandle = Marshal.AllocHGlobal(bytes.Length);
			Marshal.Copy(bytes, 0, textureHandle, bytes.Length);
			Texture texture = new(gl, width, height, textureHandle, pxFormat: PixelFormat.Rgba);
			Marshal.FreeHGlobal(textureHandle);

			texture.Bind();
			texture.SetMagFilter(TextureMagFilter.Linear);
			texture.SetMinFilter(TextureMinFilter.Linear);

			// Restore state
			gl.BindTexture(GLEnum.Texture2D, (uint)previousTextureId);

			return texture.GlTexture;
		});

		return textureId;
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

	/// <summary>
	/// Gets or loads a texture from the specified file path.
	/// </summary>
	/// <param name="path">The file path of the texture to load.</param>
	/// <returns>
	/// A <see cref="ImGuiAppTextureInfo"/> object containing information about the loaded texture,
	/// including its file path, texture ID, width, and height.
	/// </returns>
	/// <exception cref="InvalidOperationException">Thrown if the OpenGL context is not initialized.</exception>
	/// <exception cref="ArgumentNullException">Thrown if the specified path is null.</exception>
	/// <exception cref="FileNotFoundException">Thrown if the specified file does not exist.</exception>
	/// <exception cref="NotSupportedException">Thrown if the image format is not supported.</exception>
	/// <exception cref="Exception">Thrown if an error occurs while loading the image.</exception>
	public static ImGuiAppTextureInfo GetOrLoadTexture(AbsoluteFilePath path)
	{
		if (Textures.TryGetValue(path, out var textureInfo))
		{
			return textureInfo;
		}

		var image = Image.Load<Rgba32>(path);
		byte[] bytes = GetImageBytes(image);
		uint textureId = UploadTextureRGBA(bytes, image.Width, image.Height);
		textureInfo = new()
		{
			Path = path,
			TextureId = textureId,
			Width = image.Width,
			Height = image.Height
		};

		Textures[path] = textureInfo;

		return textureInfo;
	}

	private static void UpdateDpiScale()
	{
		float newScaleFactor = (float)ForceDpiAware.GetWindowScaleFactor();

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

					IntPtr fontDataPtr = pinnedFontData.AddrOfPinnedObject();

					foreach (int size in SupportedPixelFontSizes)
					{
						int fontIndex = fontAtlasPtr.Fonts.Size;
						fontAtlasPtr.AddFontFromMemoryTTF(fontDataPtr, fontBytes.Length, size, fontConfigNativePtr);
						fontSizes[size] = fontIndex;
					}
				}

				// Build the font atlas
				bool success = fontAtlasPtr.Build();
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
	public static int EmsToPx(float ems) => Invoker.Invoke(() => (int)(ems * ImGui.GetFontSize()));

	/// <summary>
	/// Converts a value in points to pixels based on the current scale factor.
	/// </summary>
	/// <param name="pts">The value in points to convert to pixels.</param>
	/// <returns>The equivalent value in pixels.</returns>
	public static int PtsToPx(int pts) => (int)(pts * ScaleFactor);
}
