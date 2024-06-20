// Ignore Spelling: App Im

namespace ktsu.io.ImGuiApp;

using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

public class ImGuiAppWindowState

{
	public Vector2 Size { get; set; } = new(1280, 720);
	public Vector2 Pos { get; set; } = new(50, 50);
	public WindowState LayoutState { get; set; }
}

public static partial class ImGuiApp
{
	private static IWindow? window;

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
#if NET7_0_OR_GREATER
	[LibraryImport("kernel32.dll")]
	private static partial nint GetConsoleWindow();
#else
	[DllImport("kernel32.dll")]
	private static extern nint GetConsoleWindow();
#endif

	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	[return: MarshalAs(UnmanagedType.Bool)]
#if NET7_0_OR_GREATER
	[LibraryImport("user32.dll")]
	private static partial bool ShowWindow(nint hWnd, int nCmdShow);
#else
	[DllImport("user32.dll")]
	private static extern bool ShowWindow(nint hWnd, int nCmdShow);
#endif

	private const int SW_HIDE = 0;

	private static bool showImGuiMetrics;
	private static bool showImGuiDemo;

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
		GL? gl = null;
		IInputContext? inputContext = null;

		// Our loading function
		window.Load += () =>
		{
			gl = window.CreateOpenGL(); // load OpenGL
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
			gl?.Viewport(s);
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
			// Make sure ImGui is up-to-date
			controller?.Update((float)delta);

			// This is where you'll do any rendering beneath the ImGui context
			// Here, we just have a blank screen.
			gl?.ClearColor(System.Drawing.Color.FromArgb(255, (int)(.45f * 255), (int)(.55f * 255), (int)(.60f * 255)));
			gl?.Clear((uint)ClearBufferMask.ColorBufferBit);

			RenderMenu(onMenu);
			RenderWindowContents(onTick, (float)delta);

			// Make sure ImGui renders too!
			controller?.Render();
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
}
