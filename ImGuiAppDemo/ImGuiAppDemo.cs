// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Demo;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.Extensions;
using ktsu.ImGuiApp;
using ktsu.ImGuiAppDemo.Properties;
using ktsu.StrongPaths;

internal static class ImGuiAppDemo
{
	private static bool showImGuiDemo;
	private static bool showStyleEditor;
	private static bool showMetrics;
	private static bool showAbout;

	// Demo state
	private static float sliderValue = 0.5f;
	private static int counter;
	private static bool checkboxState;
	private static string inputText = "";
	private static Vector3 colorPickerValue = new(0.4f, 0.7f, 0.2f);
	private static readonly Random random = new();
	private static readonly List<float> plotValues = [];
	private static float plotRefreshTime;

	private static void Main() => ImGuiApp.Start(new()
	{
		Title = "ImGuiApp Demo",
		IconPath = AppContext.BaseDirectory.As<AbsoluteDirectoryPath>() / "icon.png".As<FileName>(),
		OnRender = OnRender,
		OnAppMenu = OnAppMenu,
		SaveIniSettings = false,
		Fonts = new Dictionary<string, byte[]>
		{
			{ nameof(Resources.CARDCHAR), Resources.CARDCHAR }
		},
		// Example of configuring performance settings for throttled rendering
		PerformanceSettings = new()
		{
			EnableThrottledRendering = true,
			FocusedFps = 60.0,      // Full frame rate when focused
			UnfocusedFps = 15.0,    // Reduced frame rate when unfocused
			IdleFps = 2.0,          // Very low frame rate when idle
			EnableIdleDetection = true,
			IdleTimeoutSeconds = 5.0, // Consider idle after 5 seconds of no input
			DisableVSyncWhenThrottling = true
		},
	});

	private static void OnRender(float dt)
	{
		RenderMainDemoWindow();

		// Show additional windows based on menu toggles
		if (showImGuiDemo)
		{
			ImGui.ShowDemoWindow(ref showImGuiDemo);
		}

		if (showStyleEditor)
		{
			ImGui.Begin("Style Editor", ref showStyleEditor);
			ImGui.ShowStyleEditor();
			ImGui.End();
		}

		if (showMetrics)
		{
			ImGui.ShowMetricsWindow(ref showMetrics);
		}

		if (showAbout)
		{
			RenderAboutWindow();
		}

		// Update plot data
		UpdatePlotData(dt);
	}

	private static void RenderMainDemoWindow()
	{
		ImGui.Begin("ImGuiApp Demo Features");

		// Performance information section
		if (ImGui.CollapsingHeader("Performance & Throttling"))
		{
			ImGui.Text("This section shows the current performance state and throttling behavior.");
			ImGui.Separator();

			ImGui.Text($"Window Focused: {ImGuiApp.IsFocused}");
			ImGui.Text($"Application Idle: {ImGuiApp.IsIdle}");
			ImGui.Text($"Window Visible: {ImGuiApp.IsVisible}");

			ImGui.Separator();
			ImGui.Text("Throttling helps save system resources when the window is unfocused or idle.");
			ImGui.Text("Try unfocusing the window or leaving it idle for 5 seconds to see the effect.");
			ImGui.Text("Current FPS will be reduced to save CPU and GPU resources.");
		}

		// Basic widgets section
		if (ImGui.CollapsingHeader("Basic Widgets"))
		{
			ImGui.Text("This section demonstrates basic ImGui widgets.");

			if (ImGui.Button("Click Me!"))
			{
				counter++;
			}

			ImGui.SameLine();
			ImGui.Text($"Counter: {counter}");

			ImGui.Checkbox("Toggle Me", ref checkboxState);
			ImGui.SliderFloat("Slide Me", ref sliderValue, 0.0f, 1.0f);
			ImGui.InputText("Type Here", ref inputText, 100);
		}

		// Styling and colors section
		if (ImGui.CollapsingHeader("Styling & Colors"))
		{
			ImGui.Text("Custom font examples:");
			using (new FontAppearance(nameof(Resources.CARDCHAR), 24))
			{
				ImGui.Text("Hello, ImGui.NET!");
			}

			using (new FontAppearance(nameof(Resources.CARDCHAR)))
			{
				ImGui.Text("Fancy Text with Custom Font!");
			}

			ImGui.ColorEdit3("Color Picker", ref colorPickerValue);
			ImGui.TextColored(new Vector4(colorPickerValue.X, colorPickerValue.Y, colorPickerValue.Z, 1.0f),
				"This text is colored by the picker above!");
		}

		// Graphics and plotting section
		if (ImGui.CollapsingHeader("Graphics & Plotting"))
		{
			AbsoluteFilePath iconPath = AppContext.BaseDirectory.As<AbsoluteDirectoryPath>() / "icon.png".As<FileName>();
			ImGuiAppTextureInfo iconTexture = ImGuiApp.GetOrLoadTexture(iconPath);
			ImGui.Text("Image Example:");
			ImGui.Image(iconTexture.TextureRef, new Vector2(64, 64));

			ImGui.Text("Real-time Plot:");
			if (plotValues.Count > 0)
			{
				ImGui.PlotLines("##plot", ref plotValues.ToArray()[0], plotValues.Count, 0,
					"Random Values", float.MinValue, float.MaxValue, new Vector2(ImGui.GetContentRegionAvail().X, 80));
			}
		}

		// Layout and positioning section
		if (ImGui.CollapsingHeader("Layout & Positioning"))
		{
			ImGui.Text("Columns Example:");
			ImGui.Columns(3, "##columns");

			for (int i = 0; i < 6; i++)
			{
				ImGui.Text($"Item {i + 1}");
				ImGui.NextColumn();
			}

			ImGui.Columns(1);

			ImGui.Separator();

			ImGui.Text("Child Windows Example:");
			if (ImGui.BeginChild("##child1", new Vector2(200, 100), ImGuiChildFlags.Borders))
			{
				ImGui.Text("This is a child window");
				ImGui.Text("with its own scroll area.");
				for (int i = 0; i < 10; i++)
				{
					ImGui.Text($"Scroll item {i + 1}");
				}
			}

			ImGui.EndChild();
		}

		ImGui.End();
	}

	private static void RenderAboutWindow()
	{
		ImGui.Begin("About ImGuiApp Demo", ref showAbout);
		ImGui.Text("ImGuiApp Demo Application");
		ImGui.Separator();
		ImGui.Text("This demo showcases various ImGui.NET features");
		ImGui.Text("and demonstrates how to use the ImGuiApp framework.");
		ImGui.Separator();
		ImGui.Text("Built with:");
		ImGui.BulletText("ImGui.NET");
		ImGui.BulletText("Silk.NET");
		ImGui.BulletText("ktsu.ImGuiApp");
		ImGui.End();
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "This is a demo application")]
	private static void UpdatePlotData(float dt)
	{
		plotRefreshTime += dt;
		if (plotRefreshTime >= 0.1f) // Update every 100ms
		{
			plotRefreshTime = 0;
			plotValues.Add((float)random.NextDouble());
			if (plotValues.Count > 100) // Keep last 100 values
			{
				plotValues.RemoveAt(0);
			}
		}
	}

	private static void OnAppMenu()
	{
		if (ImGui.BeginMenu("View"))
		{
			ImGui.MenuItem("ImGui Demo", string.Empty, ref showImGuiDemo);
			ImGui.MenuItem("Style Editor", string.Empty, ref showStyleEditor);
			ImGui.MenuItem("Metrics", string.Empty, ref showMetrics);
			ImGui.EndMenu();
		}

		if (ImGui.BeginMenu("Help"))
		{
			ImGui.MenuItem("About", string.Empty, ref showAbout);
			ImGui.EndMenu();
		}
	}
}
