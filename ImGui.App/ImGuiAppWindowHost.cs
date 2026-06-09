// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.App;

/// <summary>
/// Describes how an <see cref="ImGuiApp"/> window is hosted by the operating system.
/// </summary>
public enum ImGuiAppWindowHost
{
	/// <summary>
	/// The application creates and owns its own top-level window. This is the default and matches the
	/// behaviour of <see cref="ImGuiApp.Start(ImGuiAppConfig)"/>.
	/// </summary>
	Standalone,

	/// <summary>
	/// The application renders into a borderless window reparented under a host-provided
	/// <see cref="ImGuiAppConfig.ParentWindowHandle"/>, for example a VST3 plugin editor's parent window.
	/// Use <see cref="ImGuiApp.StartEmbedded(ImGuiAppConfig)"/> with this mode.
	/// </summary>
	EmbeddedChild,
}
