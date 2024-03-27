using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace ktsu.io.ImGuiApp;

public class ImGuiAppWindowState
{
	public Vector2 Size { get; set; } = new(1280, 720);
	public Vector2 Pos { get; set; } = new(50, 50);
	public WindowState LayoutState { get; set; }
}

public static partial class ImGuiApp
{
	private static IWindow? window;

	public static ImGuiAppWindowState WindowState
	{
		get => new()
		{
			Size = new(window?.Size.X ?? 1280, window?.Size.Y ?? 720),
			Pos = new(window?.Position.X ?? 50, window?.Position.Y ?? 50),
			LayoutState = window?.WindowState ?? Silk.NET.Windowing.WindowState.Normal
		};
	}

	[LibraryImport("kernel32.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	private static partial nint GetConsoleWindow();

	[LibraryImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool ShowWindow(nint hWnd, int nCmdShow);

	private const int SW_HIDE = 0;

	private static bool showImGuiMetrics;
	private static bool showImGuiDemo;

	public static void Stop() => window?.Close();

	public static void Start(string windowTitle, ImGuiAppWindowState initialWindowState, Action onStart, Action<float> onTick) => Start(windowTitle, initialWindowState, onStart, onTick, onMenu: null, onWindowResized: null);
	public static void Start(string windowTitle, ImGuiAppWindowState initialWindowState, Action onStart, Action<float> onTick, Action onMenu) => Start(windowTitle, initialWindowState, onStart, onTick, onMenu, onWindowResized: null);
	public static void Start(string windowTitle, ImGuiAppWindowState initialWindowState, Action onStart, Action<float> onTick, Action? onMenu, Action? onWindowResized)
	{
		var options = WindowOptions.Default;
		options.Title = windowTitle;
		options.Size = new((int)initialWindowState.Size.X, (int)initialWindowState.Size.Y);
		options.Position = new((int)initialWindowState.Pos.X, (int)initialWindowState.Pos.Y);
		options.WindowState = initialWindowState.LayoutState;

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
			controller = new ImGuiController
			(
				gl = window.CreateOpenGL(), // load OpenGL
				window, // pass in our window
				inputContext = window.CreateInput() // create an input context
			);

			ImGui.GetStyle().WindowRounding = 0;
		};

		// Handle resizes
		window.FramebufferResize += s =>
		{
			// Adjust the viewport to the new window size
			gl?.Viewport(s);
			onWindowResized?.Invoke();
		};

		window.Move += (p) =>
		{
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

		onStart?.Invoke();

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
		if (ImGui.Begin("##mainWindow", ref b, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoResize))
		{
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
