// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Demo;

using Hexa.NET.ImGui;
using ktsu.Extensions;
using ktsu.ImGuiApp;
using ktsu.ImGuiApp.Demo.Demos;
using ktsu.Semantics;

internal static class ImGuiAppDemo
{
	private static bool showAbout;
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
		demoTabs.Add(new CleanImNodesDemo());
		demoTabs.Add(new ImPlotDemo());
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
			{ nameof(ktsu.ImGuiAppDemo.Properties.Resources.CARDCHAR), ktsu.ImGuiAppDemo.Properties.Resources.CARDCHAR }
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
		},
	});

	private static void OnRender(float dt)
	{
		// Update all demo tabs
		foreach (IDemoTab demo in demoTabs)
		{
			demo.Update(dt);
		}

		// Render main demo window
		RenderMainDemoWindow();

		// Show about window if requested
		if (showAbout)
		{
			RenderAboutWindow();
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
		if (ImGui.BeginMenu("Help"))
		{
			ImGui.MenuItem("About", string.Empty, ref showAbout);
			ImGui.EndMenu();
		}
	}
}
