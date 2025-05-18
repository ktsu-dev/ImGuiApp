// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp;

using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Widgets;
using Hexa.NET.KittyUI;

/// <summary>
/// Represents the main window for the Machine Monitor application.
/// </summary>
public class MainWindow : ImWindow
{
	/// <summary>
	/// Gets the name of the Machine Monitor application.
	/// </summary>
	public override string Name => nameof(ImGuiApp);

	/// <summary>
	/// Controls whether to display the ImGui metrics window.
	/// </summary>
	private static bool showImGuiMetrics;

	/// <summary>
	/// Controls whether to display the ImGui demo window.
	/// </summary>
	private static bool showImGuiDemo;

	/// <summary>
	/// Initializes a new instance of the <see cref="MainWindow"/> class.
	/// </summary>
	/// <remarks>
	/// Sets up the window as embedded within the application and configures it with a menu bar.
	/// </remarks>
	public MainWindow()
	{
		IsEmbedded = true;
		Flags = ImGuiWindowFlags.MenuBar;
	}

	/// <summary>
	/// Initializes the window by calling the application's initialization handler.
	/// </summary>
	public override void Init() => ImGuiApp.OnInit(this);

	/// <summary>
	/// Draws the main menu bar of the application.
	/// </summary>
	/// <remarks>
	/// Creates both a window-specific menu bar and the main application menu bar.
	/// The main menu bar includes application-specific menus and debug options.
	/// </remarks>
	private static void DrawMenuBar()
	{
		if (ImGui.BeginMenuBar())
		{
			ImGui.EndMenuBar();
		}

		if (ImGui.BeginMainMenuBar())
		{
			ImGuiApp.Config.OnAppMenu?.Invoke();

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

	/// <summary>
	/// Draws the content of the main window including updating metrics and rendering tabs.
	/// </summary>
	/// <remarks>
	/// Invokes the application's update and render callbacks, and handles UI scaling.
	/// The menu bar is drawn as part of the window content.
	/// </remarks>
	public override void DrawContent()
	{
		ImGuiApp.Config.OnUpdate?.Invoke(Time.Delta);
		UIScaler.Render(() =>
		{
			DrawMenuBar();
			ImGuiApp.Config.OnRender?.Invoke(Time.Delta);
		});
	}
}
