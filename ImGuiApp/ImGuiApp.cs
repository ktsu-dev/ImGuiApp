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
	public Vector2 Pos { get; set; } = new(50, 50);
	public WindowState LayoutState { get; set; }
}

public static partial class ImGuiApp
{
	private static IWindow? window;
	private static GL? gl;

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

	public static string WindowIconPath { get; set; } = string.Empty;

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

	public static void Start(string windowTitle, ImGuiAppWindowState initialWindowState, Action? onStart, Action<float>? onTick) => Start(windowTitle, initialWindowState, onStart, onTick, onMenu: null, onWindowResized: null);
	public static void Start(string windowTitle, ImGuiAppWindowState initialWindowState, Action? onStart, Action<float>? onTick, Action? onMenu) => Start(windowTitle, initialWindowState, onStart, onTick, onMenu, onWindowResized: null);
	public static void Start(string windowTitle, ImGuiAppWindowState initialWindowState, Action? onStart, Action<float>? onTick, Action? onMenu, Action? onWindowResized)
	{
		ArgumentNullException.ThrowIfNull(windowTitle);
		ArgumentNullException.ThrowIfNull(initialWindowState);

		var options = WindowOptions.Default;
		options.Title = windowTitle;
		options.Size = new((int)initialWindowState.Size.X, (int)initialWindowState.Size.Y);
		options.Position = new((int)initialWindowState.Pos.X, (int)initialWindowState.Pos.Y);
		options.WindowState = initialWindowState.LayoutState;

		LastNormalWindowState = initialWindowState;

		// Adapted from: https://github.com/dotnet/Silk.NET/blob/main/examples/CSharp/OpenGL%20Demos/ImGui/Program.cs

		// Create a Silk.NET window as usual
		window = Window.Create(options);

		// Declare some variables
		ImGuiController? controller = null;
		IInputContext? inputContext = null;

		// Our loading function
		window.Load += () =>
		{
			if (!string.IsNullOrEmpty(WindowIconPath))
			{
				SetWindowIcon(WindowIconPath);
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
				onConfigureIO: onStart
			);

			ImGui.GetStyle().WindowRounding = 0;
		};

		// Handle resizes
		window.FramebufferResize += s =>
		{
			// Adjust the viewport to the new window size
			lock (LockGL)
			{
				gl?.Viewport(s);
			}

			if (window?.WindowState == Silk.NET.Windowing.WindowState.Normal)
			{
				LastNormalWindowState.Size = new(window.Size.X, window.Size.Y);
				LastNormalWindowState.Pos = new(window.Position.X, window.Position.Y);
				LastNormalWindowState.LayoutState = Silk.NET.Windowing.WindowState.Normal;
			}
			onWindowResized?.Invoke();
		};

		window.Move += (p) =>
		{
			if (window?.WindowState == Silk.NET.Windowing.WindowState.Normal)
			{
				LastNormalWindowState.Size = new(window.Size.X, window.Size.Y);
				LastNormalWindowState.Pos = new(window.Position.X, window.Position.Y);
				LastNormalWindowState.LayoutState = Silk.NET.Windowing.WindowState.Normal;
			}
			onWindowResized?.Invoke();
		};

		// The render function
		window.Render += delta =>
		{
			lock (LockGL)
			{
				// Make sure ImGui is up-to-date
				controller?.Update((float)delta);

				// This is where you'll do any rendering beneath the ImGui context
				// Here, we just have a blank screen.
				gl?.ClearColor(Color.FromArgb(255, (int)(.45f * 255), (int)(.55f * 255), (int)(.60f * 255)));
				gl?.Clear((uint)ClearBufferMask.ColorBufferBit);

				RenderMenu(onMenu);
				RenderWindowContents(onTick, (float)delta);

				// Make sure ImGui renders too!
				controller?.Render();
			}
		};

		// The closing function
		window.Closing += () =>
		{
			// Dispose our controller first
			controller?.Dispose();

			// Dispose the input context
			inputContext?.Dispose();

			// Unload OpenGL
			gl?.Dispose();
		};

		nint handle = GetConsoleWindow();
		_ = ShowWindow(handle, SW_HIDE);

		// Now that everything's defined, let's run this bad boy!
		window.Run();

		window.Dispose();
	}

	public static void RenderMenu(Action? menuDelegate)
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
}
