namespace ktsu.ImGuiApp;

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using ImGuiNET;

using ktsu.StrongPaths;

using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

using Color = System.Drawing.Color;

/// <summary>
/// Represents the state of the ImGui application window, including size, position, and layout state.
/// </summary>
public class ImGuiAppWindowState
{
	/// <summary>
	/// Gets or sets the size of the window.
	/// </summary>
	public Vector2 Size { get; set; } = new(1280, 720);

	/// <summary>
	/// Gets or sets the position of the window.
	/// </summary>
	public Vector2 Pos { get; set; } = new(-short.MinValue, -short.MinValue);

	/// <summary>
	/// Gets or sets the layout state of the window.
	/// </summary>
	public WindowState LayoutState { get; set; }
}

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

	private static int[] FontSizes { get; } = [12, 13, 14, 16, 18, 20, 24, 28, 32, 40, 48];

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

	/// <summary>
	/// Represents information about a texture, including its file path, texture ID, width, and height.
	/// </summary>
	public class TextureInfo
	{
		/// <summary>
		/// Gets or sets the file path of the texture.
		/// </summary>
		public AbsoluteFilePath Path { get; set; } = new();

		/// <summary>
		/// Gets or sets the OpenGL texture ID.
		/// </summary>
		public uint TextureId { get; set; }

		/// <summary>
		/// Gets or sets the width of the texture.
		/// </summary>
		public int Width { get; set; }

		/// <summary>
		/// Gets or sets the height of the texture.
		/// </summary>
		public int Height { get; set; }
	}

	internal static ConcurrentDictionary<AbsoluteFilePath, TextureInfo> Textures { get; } = [];

	private static int WindowThreadId { get; set; }

	/// <summary>
	/// Stops the ImGui application by closing the window.
	/// </summary>
	public static void Stop() => window?.Close();

	/// <summary>
	/// Represents the configuration settings for the ImGui application.
	/// </summary>
	public class AppConfig
	{
		/// <summary>
		/// Gets or sets the title of the application window.
		/// </summary>
		public string Title { get; init; } = nameof(ImGuiApp);

		/// <summary>
		/// Gets or sets the file path to the application window icon.
		/// </summary>
		public string IconPath { get; init; } = string.Empty;

		/// <summary>
		/// Gets or sets the initial state of the application window.
		/// </summary>
		public ImGuiAppWindowState InitialWindowState { get; init; } = new();

		/// <summary>
		/// Gets or sets the action to be performed when the application starts.
		/// </summary>
		public Action OnStart { get; init; } = () => { };

		/// <summary>
		/// Gets or sets the action to be performed on each update tick.
		/// </summary>
		public Action<float> OnUpdate { get; init; } = (delta) => { };

		/// <summary>
		/// Gets or sets the action to be performed on each render tick.
		/// </summary>
		public Action<float> OnRender { get; init; } = (delta) => { };

		/// <summary>
		/// Gets or sets the action to be performed when rendering the application menu.
		/// </summary>
		public Action OnAppMenu { get; init; } = () => { };

		/// <summary>
		/// Gets or sets the action to be performed when the application window is moved or resized.
		/// </summary>
		public Action OnMoveOrResize { get; init; } = () => { };
	}

	private static AppConfig Config { get; set; } = new();

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
		Start(new AppConfig
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
	public static void Start(AppConfig config)
	{
		ArgumentNullException.ThrowIfNull(config);

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
		WindowThreadId = Environment.CurrentManagedThreadId;

		// Our loading function
		window.Load += () =>
		{
			if (!string.IsNullOrEmpty(config.IconPath))
			{
				SetWindowIcon(config.IconPath);
			}

			gl = window.CreateOpenGL(); // load OpenGL

			inputContext = window.CreateInput(); // create an input context
			controller = new ImGuiController.ImGuiController
			(
				gl,
				view: window,
				input: inputContext,
				onConfigureIO: () =>
				{
					UpdateDpiScale();
					config.OnStart?.Invoke();
				}
			);

			InitFonts();

			controller.BeginFrame();

			ImGui.GetStyle().WindowRounding = 0;
			window.WindowState = config.InitialWindowState.LayoutState;
		};

		window.FramebufferResize += s =>
		{
			gl?.Viewport(s);

			if (window.WindowState == Silk.NET.Windowing.WindowState.Normal)
			{
				LastNormalWindowState.Size = new(window.Size.X, window.Size.Y);
				LastNormalWindowState.Pos = new(window.Position.X, window.Position.Y);
				LastNormalWindowState.LayoutState = Silk.NET.Windowing.WindowState.Normal;
			}

			UpdateDpiScale();

			config.OnMoveOrResize?.Invoke();
		};

		window.Move += (p) =>
		{
			if (window?.WindowState == Silk.NET.Windowing.WindowState.Normal)
			{
				LastNormalWindowState.Size = new(window.Size.X, window.Size.Y);
				LastNormalWindowState.Pos = new(window.Position.X, window.Position.Y);
				LastNormalWindowState.LayoutState = Silk.NET.Windowing.WindowState.Normal;
			}

			UpdateDpiScale();

			config.OnMoveOrResize?.Invoke();
		};

		window.Update += (delta) =>
		{
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
		};

		// The render function
		window.Render += delta =>
		{
			if (IsVisible)
			{
				gl?.ClearColor(Color.FromArgb(255, (int)(.45f * 255), (int)(.55f * 255), (int)(.60f * 255)));
				gl?.Clear((uint)ClearBufferMask.ColorBufferBit);

				//get scaled font size
				const float normalFontSize = 13;
				float scaledFontSize = normalFontSize * ScaleFactor;

				var io = ImGui.GetIO();
				var fonts = io.Fonts.Fonts;
				var bestFont = fonts[fonts.Size - 1];
				float bestFontSize = bestFont.FontSize;

				for (int i = 0; i < fonts.Size; i++)
				{
					var font = fonts[i];
					float fontSize = font.FontSize;
					if (fontSize < bestFontSize && fontSize >= scaledFontSize)
					{
						bestFont = font;
						bestFontSize = fontSize;
					}
				}

				float scaleRatio = bestFontSize / normalFontSize;
				ImGui.PushFont(bestFont);

				using (new UIScaler(scaleRatio))
				{
					RenderAppMenu(config.OnAppMenu);
					RenderWindowContents(config.OnRender, (float)delta);
				}

				ImGui.PopFont();
				controller?.Render();
			}
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
		};

		window.FocusChanged += (focused) => IsFocused = focused;

		nint handle = NativeMethods.GetConsoleWindow();
		_ = NativeMethods.ShowWindow(handle, SW_HIDE);

		// Now that everything's defined, let's run this bad boy!
		window.Run();

		// Dispose the window
		window.Dispose();
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

		InvokeOnWindowThread(() => window?.SetWindowIcon([.. icons]));
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
		uint textureId = InvokeOnWindowThread(() =>
		{
			if (gl is null)
			{
				throw new InvalidOperationException("OpenGL context is not initialized.");
			}

			textureId = gl.GenTexture();
			gl.ActiveTexture(TextureUnit.Texture0);
			gl.BindTexture(TextureTarget.Texture2D, textureId);

			unsafe
			{
				fixed (byte* ptr = bytes)
				{
					gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)width, (uint)height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
				}
			}

			gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			gl.BindTexture(TextureTarget.Texture2D, 0);
			return textureId;
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
		InvokeOnWindowThread(() =>
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
	/// A <see cref="TextureInfo"/> object containing information about the loaded texture,
	/// including its file path, texture ID, width, and height.
	/// </returns>
	/// <exception cref="InvalidOperationException">Thrown if the OpenGL context is not initialized.</exception>
	/// <exception cref="ArgumentNullException">Thrown if the specified path is null.</exception>
	/// <exception cref="FileNotFoundException">Thrown if the specified file does not exist.</exception>
	/// <exception cref="NotSupportedException">Thrown if the image format is not supported.</exception>
	/// <exception cref="Exception">Thrown if an error occurs while loading the image.</exception>
	public static TextureInfo GetOrLoadTexture(AbsoluteFilePath path)
	{
		if (Textures.TryGetValue(path, out var textureInfo))
		{
			return textureInfo;
		}

		var image = Image.Load<Rgba32>(path);
		byte[] bytes = GetImageBytes(image);
		uint textureId = UploadTextureRGBA(bytes, image.Width, image.Height);
		textureInfo = new TextureInfo
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
	// This was to avoid an unnecessary copy, and is perhaps not a good API (a future version will redesign it). If you want to keep
	internal static void InitFonts()
	{
		byte[] fontBytes = Resources.Resources.RobotoMonoNerdFontMono_Medium;
		var io = ImGui.GetIO();
		var fontAtlasPtr = io.Fonts;
		nint fontBytesPtr = Marshal.AllocHGlobal(fontBytes.Length);
		Marshal.Copy(fontBytes, 0, fontBytesPtr, fontBytes.Length);
		foreach (int size in FontSizes)
		{
			unsafe
			{
				var fontConfigNativePtr = ImGuiNative.ImFontConfig_ImFontConfig();
				var fontConfig = new ImFontConfigPtr(fontConfigNativePtr)
				{
					OversampleH = 2,
					OversampleV = 2,
					PixelSnapH = true,
					FontDataOwnedByAtlas = false,
				};
				_ = fontAtlasPtr.AddFontFromMemoryTTF(fontBytesPtr, fontBytes.Length, size, fontConfig, fontAtlasPtr.GetGlyphRangesDefault());
			}
		}

		_ = fontAtlasPtr.Build();

		controller?.CreateFontTexture();

		Marshal.FreeHGlobal(fontBytesPtr);
	}

	/// <summary>
	/// Converts a value in ems to pixels based on the current ImGui font size.
	/// </summary>
	/// <param name="ems">The value in ems to convert to pixels.</param>
	/// <returns>The equivalent value in pixels.</returns>
	public static int EmsToPx(float ems) => InvokeOnWindowThread(() => (int)(ems * ImGui.GetFontSize()));

	/// <summary>
	/// Invokes the specified function on the window thread and returns the result.
	/// </summary>
	/// <typeparam name="TReturn">The type of the return value.</typeparam>
	/// <param name="func">The function to invoke on the window thread.</param>
	/// <returns>The result of the function invocation.</returns>
	/// <exception cref="ArgumentNullException">Thrown if the specified function is null.</exception>
	/// <exception cref="InvalidOperationException">Thrown if the window is not initialized.</exception>
	public static TReturn InvokeOnWindowThread<TReturn>(Func<TReturn> func)
	{
		ArgumentNullException.ThrowIfNull(func);

		return window is null
			? throw new InvalidOperationException("Window is not initialized.")
			: WindowThreadId != Environment.CurrentManagedThreadId
			? (TReturn)window.Invoke(func)
			: func();
	}

	/// <summary>
	/// Invokes the specified action on the window thread.
	/// </summary>
	/// <param name="action">The action to invoke on the window thread.</param>
	/// <exception cref="ArgumentNullException">Thrown if the specified action is null.</exception>
	/// <exception cref="InvalidOperationException">Thrown if the window is not initialized.</exception>
	public static void InvokeOnWindowThread(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);

		if (window is null)
		{
			throw new InvalidOperationException("Window is not initialized.");
		}

		if (WindowThreadId != Environment.CurrentManagedThreadId)
		{
			window.Invoke(action);
		}
		else
		{
			action();
		}
	}
}
