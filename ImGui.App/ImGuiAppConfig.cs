// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App;

using System.Resources;
using ktsu.ScopedAction;
using Silk.NET.Windowing;

/// <summary>
/// Represents the configuration settings for the ImGui application.
/// </summary>
public class ImGuiAppConfig
{
	/// <summary>
	/// Gets or sets a value indicating whether the application is running in test mode.
	/// When true, the window will be invisible and optimized for testing.
	/// </summary>
	public bool TestMode { get; init; }

	/// <summary>
	/// Gets or sets the test window to use when TestMode is true.
	/// This must be set when TestMode is true.
	/// </summary>
	internal IWindow? TestWindow { get; init; }

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
	/// Gets or sets a scoped action to enclose the frame rendering.
	/// </summary>
	public Func<ScopedAction?> FrameWrapperFactory { get; init; } = () => null;

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

	/// <summary>
	/// Gets or sets a value indicating whether to enable extended Unicode support for fonts.
	/// When true, fonts will include extended character ranges for accented characters,
	/// mathematical symbols, currency symbols, emojis, and other Unicode blocks.
	/// When false, only basic ASCII characters (0-127) will be available.
	/// Default is true.
	/// </summary>
	public bool EnableUnicodeSupport { get; init; } = true;

	/// <summary>
	/// Gets or sets a value indicating whether ImGui should save window settings to imgui.ini.
	/// When false, window positions and sizes will not be persisted between sessions.
	/// </summary>
	public bool SaveIniSettings { get; init; } = true;

	/// <summary>
	/// Gets or sets the performance settings for throttled rendering.
	/// </summary>
	public ImGuiAppPerformanceSettings PerformanceSettings { get; init; } = new();

	internal Dictionary<string, byte[]> DefaultFonts { get; init; } = new Dictionary<string, byte[]>
		{
			{ "default", Resources.Resources.NerdFont}
		};

	/// <summary>
	/// Gets the emoji font data if available, null otherwise.
	/// </summary>
	internal static byte[]? EmojiFont
	{
		get
		{
			try
			{
				return Resources.Resources.NotoEmoji;
			}
			catch (MissingManifestResourceException)
			{
				// NotoEmoji.ttf not available in resources
				return null;
			}
			catch (InvalidOperationException)
			{
				// Resource manager not available
				return null;
			}
		}
	}
}
