// Ignore Spelling: App Im

namespace ktsu.io.ImGuiApp;

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using ktsu.io.StrongPaths;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = System.Drawing.Color;

public class ImGuiAppWindowState

{
	public Vector2 Size { get; set; } = new(1280, 720);
	public Vector2 Pos { get; set; } = new(-short.MinValue, -short.MinValue);
	public WindowState LayoutState { get; set; }
}

public static partial class ImGuiApp
{
	private static IWindow? window;
	private static GL? gl;
	private static ImGuiController? controller;
	private static IInputContext? inputContext;

	private static ImGuiAppWindowState LastNormalWindowState { get; set; } = new();

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
	private static Dictionary<int, ImFontPtr> Fonts { get; } = [];

	public static bool IsFocused { get; private set; } = true;
	public static bool IsVisible => (window?.WindowState != Silk.NET.Windowing.WindowState.Minimized) && (window?.IsVisible ?? false);

	[LibraryImport("kernel32.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	private static partial nint GetConsoleWindow();

	[LibraryImport("user32.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool ShowWindow(nint hWnd, int nCmdShow);

	private const int SW_HIDE = 0;

	private static bool showImGuiMetrics;
	private static bool showImGuiDemo;

	public static float ScaleFactor { get; private set; } = 1;

	public class TextureInfo
	{
		public AbsoluteFilePath Path { get; set; } = new();
		public uint TextureId { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
	}

	private static ConcurrentDictionary<AbsoluteFilePath, TextureInfo> Textures { get; } = [];

	private static object LockGL { get; } = new();

	public static void Stop() => window?.Close();

	public class AppConfig
	{
		public string Title { get; init; } = nameof(ImGuiApp);
		public string IconPath { get; init; } = string.Empty;
		public ImGuiAppWindowState InitialWindowState { get; init; } = new();
		public Action OnStart { get; init; } = () => { };
		public Action<float> OnUpdate { get; init; } = (delta) => { };
		public Action<float> OnRender { get; init; } = (delta) => { };
		public Action OnAppMenu { get; init; } = () => { };
		public Action OnMoveOrResize { get; init; } = () => { };
	}

	private static AppConfig Config { get; set; } = new();

	public static void Start(string windowTitle, ImGuiAppWindowState initialWindowState, Action? onStart, Action<float>? onTick) => Start(windowTitle, initialWindowState, onStart, onTick, onMenu: null, onWindowResized: null);
	public static void Start(string windowTitle, ImGuiAppWindowState initialWindowState, Action? onStart, Action<float>? onTick, Action? onMenu) => Start(windowTitle, initialWindowState, onStart, onTick, onMenu, onWindowResized: null);
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

		// Our loading function
		window.Load += () =>
		{
			if (!string.IsNullOrEmpty(config.IconPath))
			{
				SetWindowIcon(config.IconPath);
			}

			lock (LockGL)
			{
				gl = window.CreateOpenGL(); // load OpenGL
			}

			inputContext = window.CreateInput(); // create an input context
			controller = new ImGuiController
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

		// Handle resizes
		window.FramebufferResize += s =>
		{
			// Adjust the viewport to the new window size
			lock (LockGL)
			{
				gl?.Viewport(s);
			}

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
			lock (LockGL)
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
			}
		};

		// The render function
		window.Render += delta =>
		{
			lock (LockGL)
			{
				if (IsVisible)
				{
					gl?.ClearColor(Color.FromArgb(255, (int)(.45f * 255), (int)(.55f * 255), (int)(.60f * 255)));
					gl?.Clear((uint)ClearBufferMask.ColorBufferBit);

					//get scaled font size
					const float normalFontSize = 13;
					float scaledFontSize = normalFontSize * ScaleFactor;
					var (bestFontSize, bestFont) = Fonts.Where(x => x.Key >= scaledFontSize).OrderBy(x => x.Key).FirstOrDefault();
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
			}
		};

		// The closing function
		window.Closing += () =>
		{
			lock (LockGL)
			{
				var io = ImGui.GetIO();
				var fontAtlasPtr = io.Fonts;
				for (int i = 0; i < fontAtlasPtr.Fonts.Size; i++)
				{
					var font = fontAtlasPtr.Fonts[i];
					font.Destroy();
				}

				fontAtlasPtr.ClearFonts();

				// Dispose our controller first
				controller?.Dispose();

				// Dispose the input context
				inputContext?.Dispose();

				// Unload OpenGL
				gl?.Dispose();
			}
		};

		window.FocusChanged += (focused) => IsFocused = focused;

		nint handle = GetConsoleWindow();
		_ = ShowWindow(handle, SW_HIDE);

		// Now that everything's defined, let's run this bad boy!
		window.Run();

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

	public static void RenderAppMenu(Action? menuDelegate)
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

	public static void RenderWindowContents(Action<float>? tickDelegate, float dt)
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
			ImGui.End();
		}

		if (showImGuiDemo)
		{
			ImGui.ShowDemoWindow(ref showImGuiDemo);
		}

		if (showImGuiMetrics)
		{
			ImGui.ShowMetricsWindow(ref showImGuiMetrics);
		}
	}

	public static byte[] GetImageBytes(Image<Rgba32> image)
	{
		byte[] pixelBytes = new byte[image.Width * image.Height * Unsafe.SizeOf<Rgba32>()];
		image.CopyPixelDataTo(pixelBytes);
		return pixelBytes;
	}

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

		window?.SetWindowIcon([.. icons]);
	}

	public static uint UploadTextureRGBA(byte[] bytes, int width, int height)
	{
		uint textureId;
		lock (LockGL)
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
		}
		return textureId;
	}

	public static void DeleteTexture(uint textureId)
	{
		lock (LockGL)
		{
			if (gl is null)
			{
				throw new InvalidOperationException("OpenGL context is not initialized.");
			}

			gl.DeleteTexture(textureId);
			Textures.Where(x => x.Value.TextureId == textureId).ToList().ForEach(x => Textures.Remove(x.Key, out var _));
		}
	}

	public static TextureInfo GetOrLoadTexture(AbsoluteFilePath path)
	{
		TextureInfo? textureInfo;
		lock (LockGL)
		{
			if (Textures.TryGetValue(path, out textureInfo))
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
		}
		return textureInfo;
	}

	private static void UpdateDpiScale() => ScaleFactor = (float)ForceDpiAware.GetWindowScaleFactor();

	internal static void InitFonts()
	{
		byte[] fontBytes = Resources.Resources.RobotoMonoNerdFontMono_Medium;
		var io = ImGui.GetIO();
		var fontAtlasPtr = io.Fonts;
		nint fontBytesPtr = Marshal.AllocHGlobal(fontBytes.Length);
		Marshal.Copy(fontBytes, 0, fontBytesPtr, fontBytes.Length);
		_ = fontAtlasPtr.AddFontDefault();
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
				};
				_ = fontAtlasPtr.AddFontFromMemoryTTF(fontBytesPtr, fontBytes.Length, size, fontConfig, fontAtlasPtr.GetGlyphRangesDefault());
			}
		}

		_ = fontAtlasPtr.Build();

		int numFonts = fontAtlasPtr.Fonts.Size;
		for (int i = 0; i < numFonts; i++)
		{
			var font = fontAtlasPtr.Fonts[i];
			Fonts[(int)font.ConfigData.SizePixels] = font;
		}
	}

	public static int EmsToPx(float ems) => (int)(ems * ImGui.GetFontSize());
}
