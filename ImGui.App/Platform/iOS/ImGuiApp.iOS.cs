// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

#if IOS

namespace ktsu.ImGui.App;

/// <summary>
/// Scaffolding stub for the iOS build of ImGuiApp. The UIKit/Metal platform layer is under
/// active development; this type exists so the library compiles for net10.0-ios. Calling
/// <see cref="Start"/> currently throws.
/// </summary>
public static class ImGuiApp
{
	private const string NotImplementedMessage =
		"ImGuiApp iOS backend is not yet implemented. Track progress on branch claude/friendly-davinci-sjuX9.";

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
}

#endif
