// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp;

using System;
using System.Collections.ObjectModel;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Widgets;
using Hexa.NET.KittyUI;
using Silk.NET.Core;

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

/// <summary>
/// Extension methods for MainWindow to handle textures and window operations.
/// </summary>
public static class MainWindowExtensions
{
	// Store a simple counter for texture IDs
	private static uint nextTextureId = 1;

	/// <summary>
	/// Creates a texture using the specified RGBA byte array, width, and height.
	/// </summary>
	/// <param name="window">The main window instance.</param>
	/// <param name="bytes">The RGBA byte array containing texture data.</param>
	/// <param name="width">The width of the texture.</param>
	/// <param name="height">The height of the texture.</param>
	/// <returns>The texture ID.</returns>
	public static uint CreateTexture(this MainWindow window, byte[] bytes, int width, int height)
	{
		ArgumentNullException.ThrowIfNull(window);
		ArgumentNullException.ThrowIfNull(bytes);

		// For now, just log the texture creation and return a dummy texture ID
		// This is a placeholder until proper Hexa.NET implementation details are known
		Console.WriteLine($"Creating texture: {width}x{height}, {bytes.Length} bytes");

		// Return a unique ID for this texture
		return nextTextureId++;
	}

	/// <summary>
	/// Deletes the specified texture from the GPU.
	/// </summary>
	/// <param name="window">The main window instance.</param>
	/// <param name="textureId">The texture ID to delete.</param>
	public static void DeleteTexture(this MainWindow window, uint textureId)
	{
		ArgumentNullException.ThrowIfNull(window);

		// Just log the deletion for now
		// This is a placeholder until proper Hexa.NET implementation details are known
		Console.WriteLine($"Deleting texture ID: {textureId}");
	}

	/// <summary>
	/// Sets the window icon using the specified icon images.
	/// </summary>
	/// <param name="window">The main window instance.</param>
	/// <param name="icons">The collection of icons in various sizes.</param>
	public static void SetWindowIcon(this MainWindow window, Collection<RawImage> icons)
	{
		ArgumentNullException.ThrowIfNull(window);
		ArgumentNullException.ThrowIfNull(icons);

		// Check if we have any icons
		if (icons.Count <= 0)
		{
			return;
		}

		// Log the icon setting
		Console.WriteLine($"Setting window icon with {icons.Count} images");

		// Log the sizes of icons for debugging
		foreach (var icon in icons)
		{
			Console.WriteLine($"  Icon size: {icon.Width}x{icon.Height}");
		}
	}
}
