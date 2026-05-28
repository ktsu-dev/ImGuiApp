// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

#if IOS

namespace ktsu.ImGui.App;

using Hexa.NET.ImGui;
using ktsu.Invoker;

/// <summary>
/// Scaffolding stub for the iOS build of ImGuiApp. The UIKit/Metal platform layer is under
/// active development; this type exists so the library compiles for net10.0-ios. Calling
/// <see cref="Start"/> currently throws.
/// </summary>
public static class ImGuiApp
{
	private const string NotImplementedMessage =
		"ImGuiApp iOS backend is not yet implemented. Track progress in docs/plans/2026-05-28-ios-platform-port.md.";

	/// <summary>
	/// Placeholder entry point. Throws until the iOS platform layer (UIKit + Metal) lands.
	/// </summary>
	/// <exception cref="PlatformNotSupportedException">Always thrown on iOS for now.</exception>
	public static void Start() => throw new PlatformNotSupportedException(NotImplementedMessage);

	/// <summary>
	/// Placeholder. Throws until the iOS platform layer lands.
	/// </summary>
	/// <exception cref="PlatformNotSupportedException">Always thrown on iOS for now.</exception>
	public static void Stop() => throw new PlatformNotSupportedException(NotImplementedMessage);

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
