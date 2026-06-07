// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

#if IOS

namespace ktsu.ImGui.App;

using System.Collections.Generic;

/// <summary>
/// iOS no-op surface for <see cref="ImGuiApp"/>: desktop window-management APIs that have no meaning
/// on iOS, where the OS owns window visibility, sizing, and the app icon. They are kept on the public
/// surface (matching the desktop signatures) so cross-platform consumer code compiles unchanged, and
/// each logs a one-time warning on first use rather than silently doing nothing (§3 of the port plan).
/// </summary>
/// <remarks>
/// Not stubbed: the overlay API (<c>EnableOverlay</c>/<c>DisableOverlay</c>/<c>SetOverlayGeometry</c>)
/// and <c>SetWindowIcon</c>'s overlay siblings depend on desktop-only types (e.g. <c>OverlayCorner</c>
/// in the iOS-excluded <c>OverlayWindow.cs</c>); the transparent click-through overlay is inherently a
/// desktop concept and stays desktop-only. <see cref="ImGuiAppConfig.InitialWindowState"/> is likewise
/// ignored on iOS — the OS controls window sizing.
/// </remarks>
public static partial class ImGuiApp
{
	/// <summary>Tracks which no-op APIs have already logged, so each warns at most once per process.</summary>
	private static readonly HashSet<string> warnedNoOps = [];

	/// <summary>Logs a one-time "no-op on iOS" warning for the named API.</summary>
	/// <param name="api">The API name (used as the dedupe key and in the message).</param>
	/// <param name="reason">Why the API does nothing on iOS.</param>
	private static void WarnIosNoOp(string api, string reason)
	{
		if (warnedNoOps.Add(api))
		{
			DebugLogger.Log($"ImGuiApp.{api} is a no-op on iOS: {reason}");
		}
	}

	/// <summary>
	/// No-op on iOS: the operating system controls window visibility; an app cannot show its own window.
	/// Logs a warning once. Present so cross-platform code that calls <see cref="Show"/> still compiles.
	/// </summary>
	public static void Show() =>
		WarnIosNoOp(nameof(Show), "iOS controls window visibility; an app cannot show or hide its own window.");

	/// <summary>
	/// No-op on iOS: the operating system controls window visibility; an app cannot hide its own window.
	/// Logs a warning once. Present so cross-platform code that calls <see cref="Hide"/> still compiles.
	/// </summary>
	public static void Hide() =>
		WarnIosNoOp(nameof(Hide), "iOS controls window visibility; an app cannot show or hide its own window.");

	/// <summary>
	/// No-op on iOS: the app icon is provided by the app bundle (asset catalogue / Info.plist), not set
	/// at runtime. Logs a warning once. Present so cross-platform code that calls
	/// <see cref="SetWindowIcon(string)"/> still compiles.
	/// </summary>
	/// <param name="iconPath">Ignored on iOS; the bundle icon is used instead.</param>
	public static void SetWindowIcon(string iconPath) =>
		WarnIosNoOp(nameof(SetWindowIcon), $"the app icon comes from the iOS app bundle, not a runtime path ('{iconPath}').");
}

#endif
