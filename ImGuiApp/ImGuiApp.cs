namespace ktsu.ImGuiApp;

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using ImGuiNET;

using ktsu.StrongPaths;
using ktsu.Invoker;

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
	private static Collection<nint> FontHandles { get; } = [];

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

	/// <summary>
	/// Starts the ImGui application with the specified window title, initial window state, and optional actions.
	/// </summary>
	/// <param name="windowTitle">The title of the application window.</param>
	/// <param name="initialWindowState">The initial state of the application window.</param>
	/// <param name="onStart">The action to be performed when the application starts.</param>
	/// <param name="onTick">The action to be performed on each update tick.</param>
	public static void Start(string windowTitle, ImGuiAppWindowState initialWindowState, Action? onStart, Action<float>? onTick) => Start(windowTitle, initialWindowState, onStart, onTick, onMenu: null, onWindowResized: null);

	/// <summary>
	/// Starts the ImGui application with the specified window title, initial window state, and optional actions.
	/// </summary>
	/// <param name="windowTitle">The title of the application window.</param>
	/// <param name="initialWindowState">The initial state of the application window.</param>
	/// <param name="onStart">The action to be performed when the application starts.</param>
	/// <param name="onTick">The action to be performed on each update tick.</param>
	/// <param name="onMenu">The action to be performed when rendering the application menu.</param>
	public static void Start(string windowTitle, ImGuiAppWindowState initialWindowState, Action? onStart, Action<float>? onTick, Action? onMenu) => Start(windowTitle, initialWindowState, onStart, onTick, onMenu, onWindowResized: null);

	/// <summary>
	/// Starts the ImGui application with the specified window title, initial window state, and optional actions.
	/// </summary>
	/// <param name="windowTitle">The title of the application window.</param>
	/// <param name="initialWindowState">The initial state of the application window.</param>
	/// <param name="onStart">The action to be performed when the application starts.</param>
	/// <param name="onTick">The action to be performed on each update tick.</param>
	/// <param name="onMenu">The action to be performed when rendering the application menu.</param>
	/// <param name="onWindowResized">The action to be performed when the application window is moved or resized.</param>
	public static void Start(string windowTitle, ImGuiAppWindowState initialWindowState, Action? onStart, Action<float>? onTick, Action? onMenu, Action? onWindowResized) =>
		Start(new ImGuiAppConfig
		{
			Title = windowTitle,
			InitialWindowState = initialWindowState,
			OnStart = onStart ?? new(() => { }),
			OnRender = onTick ?? new((delta) => { }),
			OnAppMenu = onMenu ?? new(() => { }),
			OnMoveOrResize = onWindowResized ?? new(() => { }),
		});

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

		var silkWindowOptions = WindowOptions.Default;
		silkWindowOptions.Title = config.Title;
		silkWindowOptions.Size = new((int)config.InitialWindowState.Size.X, (int)config.InitialWindowState.Size.Y);
		silkWindowOptions.Position = new((int)config.InitialWindowState.Pos.X, (int)config.InitialWindowState.Pos.Y);
		silkWindowOptions.WindowState = Silk.NET.Windowing.WindowState.Normal;

		LastNormalWindowState = config.InitialWindowState;
		LastNormalWindowState.LayoutState = Silk.NET.Windowing.WindowState.Normal;

		// Adapted from: https://github.com/dotnet/Silk.NET/blob/main/examples/CSharp/OpenGL%20Demos/ImGui/Program.cs

		// Create a Silk.NET window as usual
		window = Window.Create(silkWindowOptions);

		// Our loading function
		window.Load += () =>
		{
			if (!string.IsNullOrEmpty(config.IconPath))
			{
				SetWindowIcon(config.IconPath);
			}

			gl = window.CreateOpenGL(); // load OpenGL

			inputContext = window.CreateInput(); // create an input context
			controller = new
			(
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

		window.FramebufferResize += s =>
		{
			gl?.Viewport(s);

			CaptureWindowNormalState();

			UpdateDpiScale();

			config.OnMoveOrResize?.Invoke();
		};

		window.Move += (p) =>
		{
			CaptureWindowNormalState();

			UpdateDpiScale();

			config.OnMoveOrResize?.Invoke();
		};

		window.Update += (delta) =>
		{
			if (!controller?.FontsConfigured ?? true)
			{
				throw new InvalidOperationException("Fonts are not configured before Update()");
			}

			EnsureWindowPositionIsValid();

			double currentFps = window.FramesPerSecond;
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

			controller?.Update((float)delta);
			config.OnUpdate?.Invoke((float)delta);
			Invoker.DoInvokes();
		};

		// The render function
		window.Render += delta =>
		{
			if (!controller?.FontsConfigured ?? true)
			{
				throw new InvalidOperationException("Fonts are not configured before Render()");
			}

			gl?.ClearColor(Color.FromArgb(255, (int)(.45f * 255), (int)(.55f * 255), (int)(.60f * 255)));
			gl?.Clear((uint)ClearBufferMask.ColorBufferBit);

			using (new FontAppearance(FontAppearance.DefaultFontName, FontAppearance.DefaultFontPointSize, out int bestFontSize))
			{
				float scaleRatio = bestFontSize / (float)FontAppearance.DefaultFontPointSize;
				using (new UIScaler(scaleRatio))
				{
					RenderAppMenu(config.OnAppMenu);
					RenderWindowContents(config.OnRender, (float)delta);
				}
			}

			controller?.Render();
		};

		// The closing function
		window.Closing += () =>
		{
			// Dispose our controller first
			controller?.Dispose();
			controller = null;

			// Dispose the input context
			inputContext?.Dispose();
			inputContext = null;

			// Unload OpenGL
			gl?.Dispose();
			gl = null;

			foreach (nint fontHandle in FontHandles)
			{
				Marshal.FreeHGlobal(fontHandle);
			}
		};

		window.FocusChanged += (focused) => IsFocused = focused;

		nint handle = NativeMethods.GetConsoleWindow();
		_ = NativeMethods.ShowWindow(handle, SW_HIDE);

		// Now that everything's defined, let's run this bad boy!
		window.Run();

		window.Dispose();
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
		colors[(int)ImGuiCol.Border] = new();
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

	private static void UpdateDpiScale() => ScaleFactor = (float)ForceDpiAware.GetWindowScaleFactor();

	// https://github.com/ocornut/imgui/blob/master/docs/FONTS.md#loading-font-data-from-memory
	// IMPORTANT: AddFontFromMemoryTTF() by default transfer ownership of the data buffer to the font atlas, which will attempt to free it on destruction.
	// This was to avoid an unnecessary copy, and is perhaps not a good API (a future version will redesign it).
	// If you want to keep ownership of the data and free it yourself, you need to clear the FontDataOwnedByAtlas field
	internal static void InitFonts()
	{
		var fontsToLoad = Config.Fonts.Concat(Config.DefaultFonts);

		var io = ImGui.GetIO();
		var fontAtlasPtr = io.Fonts;
		fontAtlasPtr.Clear();

		foreach (var font in fontsToLoad)
		{
			byte[] fontBytes = font.Value;
			nint fontHandle = Marshal.AllocHGlobal(fontBytes.Length);
			FontHandles.Add(fontHandle);
			Marshal.Copy(fontBytes, 0, fontHandle, fontBytes.Length);
			LoadFont(fontAtlasPtr, fontHandle, fontBytes.Length, font.Key);
		}
	}

	private static void LoadFont(ImFontAtlasPtr fontAtlasPtr, nint fontHandle, int fontBytesLength, string name)
	{
		if (!FontIndices.TryGetValue(name, out var fontSizes))
		{
			fontSizes = new();
			FontIndices[name] = fontSizes;
		}

		foreach (int size in SupportedPixelFontSizes)
		{
			int fontIndex = fontAtlasPtr.Fonts.Size;

			unsafe
			{
				var fontConfigNativePtr = ImGuiNative.ImFontConfig_ImFontConfig();
				fontConfigNativePtr->FontDataOwnedByAtlas = 0;
				fontConfigNativePtr->PixelSnapH = 1;
				fontConfigNativePtr->OversampleH = 1;
				fontConfigNativePtr->OversampleV = 1;

				fontAtlasPtr.AddFontFromMemoryTTF(fontHandle, fontBytesLength, size, fontConfigNativePtr);
				ImGuiNative.ImFontConfig_destroy(fontConfigNativePtr);
			}

			fontSizes[size] = fontIndex;
		}
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
