// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

#if IOS

namespace ktsu.ImGui.App;

using Hexa.NET.ImGui;
using ktsu.Invoker;

/// <summary>
/// Scaffolding for the iOS build of ImGuiApp. The UIKit/Metal platform layer is under active
/// development; this type exposes the same public surface as the desktop build so cross-platform
/// callers compile against <c>net10.0-ios</c>, but the render loop is not wired up yet. Calling
/// <see cref="Start"/> currently throws — the next chunk lands the <c>UIApplicationDelegate</c> +
/// <c>CADisplayLink</c> lifecycle. Track progress in docs/plans/2026-05-28-ios-platform-port.md.
/// </summary>
public static class ImGuiApp
{
	private const string NotImplementedMessage =
		"ImGuiApp iOS backend is not yet implemented. Track progress in docs/plans/2026-05-28-ios-platform-port.md.";

	/// <summary>
	/// Gets the active application configuration. Populated by <see cref="Start"/> so the rest of
	/// the (forthcoming) iOS platform layer can read lifecycle callbacks, fonts, and settings.
	/// </summary>
	internal static ImGuiAppConfig Config { get; set; } = new();

	/// <summary>
	/// Bootstraps the iOS application. Mirrors the desktop <c>Start(ImGuiAppConfig)</c> signature so
	/// cross-platform code compiles unchanged; the UIKit + Metal lifecycle is not yet implemented, so
	/// this currently records the configuration and throws.
	/// </summary>
	/// <param name="config">The application configuration (lifecycle callbacks, fonts, settings).</param>
	/// <exception cref="PlatformNotSupportedException">Always thrown on iOS until the platform layer lands.</exception>
	public static void Start(ImGuiAppConfig config)
	{
		ArgumentNullException.ThrowIfNull(config);
		Config = config;
		throw new PlatformNotSupportedException(NotImplementedMessage);
	}

	/// <summary>
	/// Placeholder. Throws until the iOS platform layer lands.
	/// </summary>
	/// <exception cref="PlatformNotSupportedException">Always thrown on iOS for now.</exception>
	public static void Stop() => throw new PlatformNotSupportedException(NotImplementedMessage);

	/// <summary>
	/// Gets the DPI scale factor for the application. On iOS this will initialise from
	/// <c>UIScreen.MainScreen.Scale</c> once the platform layer lands; until then it reports 1.
	/// </summary>
	public static float ScaleFactor { get; private set; } = 1.0f;

	/// <summary>
	/// Gets the user-controlled global UI scale factor for accessibility. Identical semantics to desktop.
	/// </summary>
	public static float GlobalScale { get; private set; } = 1.0f;

	/// <summary>
	/// Converts a measurement in ems to pixels. Until the ImGui frame is wired up on iOS there is no
	/// active font, so this mirrors the desktop uninitialised fallback of ems times the default point size.
	/// </summary>
	/// <param name="ems">The measurement in ems.</param>
	/// <returns>The equivalent measurement in pixels.</returns>
	public static int EmsToPx(float ems) => (int)(ems * FontAppearance.DefaultFontPointSize);

	/// <summary>
	/// Converts a measurement in points to pixels using the current scale factor. Matches desktop semantics.
	/// </summary>
	/// <param name="pts">The measurement in points.</param>
	/// <returns>The equivalent measurement in pixels.</returns>
	public static int PtsToPx(int pts) => (int)(pts * ScaleFactor);

	// Below: symbol-compatibility stubs so the platform-neutral helpers in the same assembly
	// (UIScaler, FontAppearance) link successfully against the iOS TFM. Because Start()
	// throws unconditionally above, nothing in user code reaches these on iOS today — they
	// only need to exist. When the real iOS port lands, these get replaced with backing
	// fields driven by ImGuiAppDelegate / ImGuiAppViewController.

	/// <summary>
	/// Marshals delegates to the main UI thread. iOS placeholder — unset on the stub; the
	/// real port will populate this from <c>NSObject.InvokeOnMainThread</c> equivalent.
	/// </summary>
	public static Invoker Invoker { get; internal set; } = null!;

	internal static ImFontPtr FindBestFontForAppearance(string name, int sizePoints, out float sizePixels)
	{
		sizePixels = sizePoints;
		throw new PlatformNotSupportedException(NotImplementedMessage);
	}
}

#endif
