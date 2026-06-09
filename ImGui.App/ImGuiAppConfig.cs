// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App;

using System.Resources;
using ktsu.ScopedAction;
#if !IOS
using Silk.NET.Windowing;
#endif

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

#if !IOS
	/// <summary>
	/// Gets or sets the test window to use when TestMode is true.
	/// This must be set when TestMode is true.
	/// </summary>
	/// <remarks>
	/// Backed by the desktop windowing layer (Silk.NET) and consumed only by the desktop render
	/// loop, so it is excluded from the <c>net10.0-ios</c> build.
	/// </remarks>
	internal IWindow? TestWindow { get; init; }
#endif

	/// <summary>
	/// Gets or sets the title of the application window.
	/// </summary>
	public string Title { get; init; } = nameof(ImGuiApp);

	/// <summary>
	/// Gets or sets how the application window is hosted.
	/// </summary>
	/// <remarks>
	/// Defaults to <see cref="ImGuiAppWindowHost.Standalone"/>, which preserves the classic behaviour of
	/// <see cref="ImGuiApp.Start(ImGuiAppConfig)"/> (a top-level window owning a blocking loop). Set this to
	/// <see cref="ImGuiAppWindowHost.EmbeddedChild"/> together with <see cref="ParentWindowHandle"/> and
	/// start via <see cref="ImGuiApp.StartEmbedded(ImGuiAppConfig)"/> to render as a child of a
	/// host-provided window (for example a VST3 plugin editor).
	/// </remarks>
	public ImGuiAppWindowHost WindowHost { get; init; } = ImGuiAppWindowHost.Standalone;

	/// <summary>
	/// Gets or sets the native handle of the parent window to embed into.
	/// </summary>
	/// <remarks>
	/// Only consulted when <see cref="WindowHost"/> is <see cref="ImGuiAppWindowHost.EmbeddedChild"/>. The
	/// value is a platform-native window handle: an <c>HWND</c> on Windows, an <c>NSView*</c> on macOS, or
	/// an X11 <c>Window</c> on Linux.
	/// </remarks>
	public nint ParentWindowHandle { get; init; }

	/// <summary>
	/// Gets or sets the file path to the application window icon.
	/// </summary>
	public string IconPath { get; init; } = string.Empty;

	/// <summary>
	/// Gets or sets the initial state of the application window.
	/// </summary>
	public ImGuiAppWindowState InitialWindowState { get; init; } = new();

	/// <summary>
	/// Gets or sets a value indicating whether the window is created hidden.
	/// When true, the window starts invisible and must be shown with <c>ImGuiApp.Show</c>
	/// (typically from a system tray icon). The render loop still runs while hidden.
	/// </summary>
	public bool StartHidden { get; init; }

	/// <summary>
	/// Gets or sets a value indicating whether clicking the window's close button hides the
	/// window instead of stopping the application. This keeps the render loop alive so the
	/// window can be shown again via <c>ImGuiApp.Show</c>. Implemented on Windows;
	/// on other platforms the window closes normally.
	/// </summary>
	public bool HideOnClose { get; init; }

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
	/// Gets or sets the action to be performed when the global UI scale changes.
	/// The parameter is the new scale factor (e.g., 1.0 for 100%, 1.5 for 150%).
	/// This can be used to persist the scale preference.
	/// </summary>
	public Action<float> OnGlobalScaleChanged { get; init; } = (scale) => { };

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
	/// Gets or sets a value indicating whether to auto-discover ImGui extensions (ImGuizmo, ImNodes,
	/// ImPlot) by reflecting over loaded assemblies at startup. Default is true.
	/// <para>
	/// On iOS this is effectively always off: reflective assembly scanning is unfriendly to the AOT
	/// trimmer, so the iOS build excludes the extension manager entirely and never auto-discovers,
	/// regardless of this value. iOS consumers that want an extension must wire it up manually.
	/// </para>
	/// </summary>
	public bool AutoDiscoverExtensions { get; init; } = true;

	/// <summary>
	/// Gets or sets the performance settings for throttled rendering.
	/// </summary>
	public ImGuiAppPerformanceSettings PerformanceSettings { get; init; } = new();

	/// <summary>
	/// Gets or sets the font memory configuration for limiting texture memory allocation.
	/// This helps prevent excessive memory usage on small GPUs or high-resolution displays.
	/// </summary>
	public FontMemoryGuard.FontMemoryConfig FontMemoryConfig { get; init; } = new();

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
