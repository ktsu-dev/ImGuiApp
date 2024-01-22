using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using Veldrid;
using Veldrid.StartupUtilities;
using Window = Veldrid.Sdl2.Sdl2Window;
using WindowSizeState = Veldrid.WindowState;

namespace ktsu.io.ImGuiApp;

public class ImGuiAppWindowState
{
	public Vector2 Size { get; set; } = new(1280, 720);
	public Vector2 Pos { get; set; } = new(50, 50);
	public WindowSizeState WindowSizeState { get; set; } = WindowSizeState.Normal;
}

public static partial class ImGuiApp
{
	public static Window? Window => sdlWindow;

	private static Window? sdlWindow;
	private static GraphicsDevice? graphicsDevice;
	private static CommandList? commandList;
	private static ImGuiController? imguiController;

	public static ImGuiAppWindowState WindowState => new()
	{
		Pos = Window is null ? Vector2.Zero : new(Window.X, Window.Y),
		Size = Window is null ? Vector2.Zero : new(Window.Width, Window.Height),
		WindowSizeState = Window is null ? WindowSizeState.Normal : Window.WindowState,
	};

	private static Vector4 ClearColor { get; } = new(0.45f, 0.55f, 0.6f, 1.0f);
	private static ImGuiAppWindowState InitialWindowState { get; set; } = new();

	[LibraryImport("kernel32.dll")]
	[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
	private static partial nint GetConsoleWindow();

	[LibraryImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool ShowWindow(nint hWnd, int nCmdShow);

	private const int SW_HIDE = 0;
	//private const int SW_SHOW = 5;

	private static bool showImGuiMetrics;
	private static bool showImGuiDemo;

	private static bool shouldTick = true;

	public static void Stop() => shouldTick = false;

	public static void Start(string windowTitle, ImGuiAppWindowState initialWindowState, Action<float> tickDelegate) => Start(windowTitle, initialWindowState, tickDelegate, menuDelegate: null, windowResizedDelegate: null);
	public static void Start(string windowTitle, ImGuiAppWindowState initialWindowState, Action<float> tickDelegate, Action menuDelegate) => Start(windowTitle, initialWindowState, tickDelegate, menuDelegate, windowResizedDelegate: null);
	public static void Start(string windowTitle, ImGuiAppWindowState initialWindowState, Action<float> tickDelegate, Action? menuDelegate, Action? windowResizedDelegate)
	{
		InitialWindowState = initialWindowState ?? new();
		nint handle = GetConsoleWindow();
		_ = ShowWindow(handle, SW_HIDE);

		VeldridStartup.CreateWindowAndGraphicsDevice
		(
			windowCI: new WindowCreateInfo
			(
				x: (int)InitialWindowState.Pos.X,
				y: (int)InitialWindowState.Pos.Y,
				windowWidth: (int)InitialWindowState.Size.X,
				windowHeight: (int)InitialWindowState.Size.Y,
				windowInitialState: InitialWindowState.WindowSizeState,
				windowTitle: windowTitle
			),
			deviceOptions: new
			(
				debug: true,
				swapchainDepthFormat: null,
				syncToVerticalBlank: true
			),
			window: out sdlWindow,
			gd: out graphicsDevice
		);

		commandList = graphicsDevice.ResourceFactory.CreateCommandList();
		imguiController = new ImGuiController(graphicsDevice, graphicsDevice.MainSwapchain.Framebuffer.OutputDescription, sdlWindow.Width, sdlWindow.Height);

		sdlWindow.Resized += () =>
		{
			graphicsDevice.MainSwapchain.Resize((uint)sdlWindow.Width, (uint)sdlWindow.Height);
			imguiController.WindowResized(sdlWindow.Width, sdlWindow.Height);
			windowResizedDelegate?.Invoke();
		};

		ImGui.GetStyle().WindowRounding = 0;

		Stopwatch stopWatch = new();
		while (tickDelegate != null)
		{
			float dt = stopWatch.ElapsedMilliseconds / 1000f;
			stopWatch.Restart();
			if (dt == 0)
			{
				dt = 1f / 60f;
			}

			var snapshot = sdlWindow.PumpEvents();
			if (!sdlWindow.Exists || !shouldTick)
			{
				return;
			}

			imguiController.Update(dt, snapshot);

			if (menuDelegate != null)
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

			bool b = true;
			ImGui.SetNextWindowSize(ImGui.GetMainViewport().WorkSize, ImGuiCond.Always);
			ImGui.SetNextWindowPos(ImGui.GetMainViewport().WorkPos);
			if (ImGui.Begin(windowTitle, ref b, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoResize))
			{
				tickDelegate(dt);
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

			commandList.Begin();
			commandList.SetFramebuffer(graphicsDevice.MainSwapchain.Framebuffer);
			commandList.ClearColorTarget(0, new RgbaFloat(ClearColor));
			imguiController.Render(graphicsDevice, commandList);
			commandList.End();
			graphicsDevice.SubmitCommands(commandList);
			graphicsDevice.SwapBuffers(graphicsDevice.MainSwapchain);
		}
	}
}
