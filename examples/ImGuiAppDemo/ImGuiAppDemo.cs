// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Examples.App;

using Hexa.NET.ImGui;
using ktsu.ImGui.App;
using ktsu.ImGui.Examples.App.Demos;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

internal static class ImGuiAppDemo
{
	private static bool showAbout;

	// Overlay-mode demo state: a borderless, always-on-top, translucent window locked to a corner.
	private static bool overlayEnabled;
	private static float overlayOpacity = 0.9f;
	private static bool overlayClickThrough;
	private static OverlayCorner overlayCorner = OverlayCorner.TopRight;

	private static readonly List<IDemoTab> demoTabs = [];

	static ImGuiAppDemo()
	{
		// Initialize all demo tabs
		demoTabs.Add(new BasicWidgetsDemo());
		demoTabs.Add(new AdvancedWidgetsDemo());
		demoTabs.Add(new LayoutDemo());
		demoTabs.Add(new GraphicsDemo());
		demoTabs.Add(new DataVisualizationDemo());
		demoTabs.Add(new InputHandlingDemo());
		demoTabs.Add(new AnimationDemo());
		demoTabs.Add(new UnicodeDemo());
		demoTabs.Add(new NerdFontDemo());
		demoTabs.Add(new ImGuizmoDemo());
		demoTabs.Add(new ImNodesDemo());
		demoTabs.Add(new ImPlotDemo());
		demoTabs.Add(new CleanImNodesDemo());
		demoTabs.Add(new UtilityDemo());
	}

	private static void Main() => ImGuiApp.Start(new()
	{
		Title = "ImGuiApp Demo",
		IconPath = AppContext.BaseDirectory.As<AbsoluteDirectoryPath>() / "icon.png".As<FileName>(),
		OnRender = OnRender,
		OnAppMenu = OnAppMenu,
		SaveIniSettings = false,
		// Note: EnableUnicodeSupport = true by default, so Unicode and emojis are automatically enabled!
		Fonts = new Dictionary<string, byte[]>
		{
			{ nameof(Properties.Resources.CARDCHAR), Properties.Resources.CARDCHAR }
		},
		// Example of configuring performance settings for throttled rendering
		// Uses PID controller for accurate frame rate limiting instead of simple sleep-based approach
		// VSync is disabled to allow frame limiting below monitor refresh rate
		// Defaults: Kp=1.8, Ki=0.048, Kd=0.237 (from comprehensive auto-tuning)
		PerformanceSettings = new()
		{
			EnableThrottledRendering = true,
			// Using default values: Focused=30, Unfocused=5, Idle=10 FPS
			// But with a shorter idle timeout for demo purposes
			IdleTimeoutSeconds = 5.0, // Consider idle after 5 seconds (default is 30)
			// Overlay mode keeps animating at 60 FPS even while unfocused (it's always-on-top
			// and shows live content), bypassing the focus/idle/visibility throttling above.
			OverlayFps = 60.0,
		},
	});

	private static void OnRender(float dt)
	{
		// Keep the native window styling in sync with the overlay-demo toggle. Calling these
		// every frame is cheap — the underlying window is only restyled when something changes.
		if (overlayEnabled)
		{
			ImGuiApp.EnableOverlay(overlayOpacity, overlayClickThrough);
			ImGuiApp.SetOverlayGeometry(overlayCorner, offsetX: 24, offsetY: 24, width: 380, height: 320);
		}
		else
		{
			ImGuiApp.DisableOverlay();
		}

		// Update all demo tabs
		foreach (IDemoTab demo in demoTabs)
		{
			demo.Update(dt);
		}

		// In overlay mode show a compact control strip instead of the full tabbed UI.
		if (overlayEnabled)
		{
			RenderOverlayControls();
		}
		else
		{
			RenderMainDemoWindow();
		}

		// Show about window if requested
		if (showAbout)
		{
			RenderAboutWindow();
		}
	}

	// Demonstrates the canonical overlay API: toggle, opacity, click-through, and corner anchor.
	private static void RenderOverlayControls()
	{
		ImGui.TextUnformatted("Overlay mode");
		ImGui.Separator();

		_ = ImGui.SliderFloat("Opacity", ref overlayOpacity, 0.2f, 1.0f, "%.2f");
		_ = ImGui.Checkbox("Click-through", ref overlayClickThrough);

		int corner = (int)overlayCorner;
		if (ImGui.Combo("Corner", ref corner, "Top-left\0Top-right\0Bottom-left\0Bottom-right\0"))
		{
			overlayCorner = (OverlayCorner)corner;
		}

		if (overlayClickThrough)
		{
			ImGui.TextDisabled("Click-through is on — toggle it from the View menu to interact.");
		}

		if (ImGui.Button("Exit overlay"))
		{
			overlayEnabled = false;
		}
	}

	private static void RenderMainDemoWindow()
	{
		// Create tabs for different demo sections
		if (ImGui.BeginTabBar("DemoTabs", ImGuiTabBarFlags.None))
		{
			// Render all demo tabs
			foreach (IDemoTab demo in demoTabs)
			{
				demo.Render();
			}
			ImGui.EndTabBar();
		}
	}

	private static void RenderAboutWindow()
	{
		ImGui.Begin("About ImGuiApp Demo", ref showAbout);
		ImGui.SeparatorText("ImGuiApp Demo Application");
		ImGui.Text("This demo showcases extensive ImGui.NET features including:");
		ImGui.BulletText("Basic and advanced widgets");
		ImGui.BulletText("Layout systems (columns, tables, tabs)");
		ImGui.BulletText("Custom graphics and drawing");
		ImGui.BulletText("Data visualization and plotting");
		ImGui.BulletText("Input handling and interaction");
		ImGui.BulletText("Animations and effects");
		ImGui.BulletText("File operations and utilities");
		ImGui.BulletText("3D manipulation gizmos (ImGuizmo)");
		ImGui.BulletText("Node-based editing (ImNodes)");
		ImGui.BulletText("Advanced plotting (ImPlot)");
		ImGui.SeparatorText("Built with");
		ImGui.BulletText("Hexa.NET.ImGui");
		ImGui.BulletText("Hexa.NET.ImGuizmo - 3D manipulation gizmos");
		ImGui.BulletText("Hexa.NET.ImNodes - Node editor system");
		ImGui.BulletText("Hexa.NET.ImPlot - Advanced plotting library");
		ImGui.BulletText("Silk.NET");
		ImGui.BulletText("ktsu.ImGuiApp Framework");
		ImGui.End();
	}

	private static void OnAppMenu()
	{
		if (ImGui.BeginMenu("View"))
		{
			ImGui.MenuItem("Overlay mode", string.Empty, ref overlayEnabled);
			if (overlayEnabled)
			{
				ImGui.MenuItem("Overlay click-through", string.Empty, ref overlayClickThrough);
			}
			ImGui.EndMenu();
		}

		if (ImGui.BeginMenu("Help"))
		{
			ImGui.MenuItem("About", string.Empty, ref showAbout);
			ImGui.EndMenu();
		}
	}
}
