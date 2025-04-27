// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp;

/// <summary>
/// Represents the configuration settings for the ImGui application.
/// </summary>
public class ImGuiAppConfig
{
	/// <summary>
	/// Gets or sets the title of the application window.
	/// </summary>
	public string Title { get; init; } = nameof(ImGuiApp);

	/// <summary>
	/// Gets or sets the file path to the application window icon.
	/// </summary>
	public string IconPath { get; init; } = string.Empty;

	/// <summary>
	/// Gets or sets the initial state of the application window.
	/// </summary>
	public ImGuiAppWindowState InitialWindowState { get; init; } = new();

	/// <summary>
	/// Gets or sets the action to be performed when the application starts.
	/// </summary>
	public Action OnStart { get; init; } = () => { };

	/// <summary>
	/// Gets or sets the action to be performed on each update tick.
	/// </summary>
	public Action<float> OnUpdate { get; init; } = (delta) => { };

	/// <summary>
	/// Gets or sets the action to be performed on each render tick.
	/// </summary>
	public Action<float> OnRender { get; init; } = (delta) => { };

	/// <summary>
	/// Gets or sets the action to be performed when rendering the application menu.
	/// </summary>
	public Action OnAppMenu { get; init; } = () => { };

	/// <summary>
	/// Gets or sets the action to be performed when the application window is moved or resized.
	/// </summary>
	public Action OnMoveOrResize { get; init; } = () => { };

	/// <summary>
	/// Gets or sets the fonts to be used in the application.
	/// </summary>
	/// <value>
	/// A dictionary where the key is the font name and the value is the byte array representing the font data.
	/// </value>
	public Dictionary<string, byte[]> Fonts { get; init; } = [];

	internal Dictionary<string, byte[]> DefaultFonts { get; init; } = new Dictionary<string, byte[]>
		{
			{ "default", Resources.Resources.RobotoMonoNerdFontMono_Medium }
		};
}
