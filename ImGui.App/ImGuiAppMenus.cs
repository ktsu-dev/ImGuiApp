// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App;

/// <summary>
/// The menus ImGuiApp appends to the main menu bar after <see cref="ImGuiAppConfig.OnAppMenu"/>.
/// </summary>
/// <remarks>
/// They are developer conveniences, so a finished application usually wants them out of its menu
/// bar: one that offers its own interface size setting would otherwise show two, and only its own
/// would be saved. The menu bar is only drawn at all when <see cref="ImGuiAppConfig.OnAppMenu"/> is
/// set, so these flags change nothing for an application without one.
/// </remarks>
[Flags]
public enum ImGuiAppMenus
{
	/// <summary>Append no built-in menu.</summary>
	None = 0,

	/// <summary>The Accessibility menu, which sets the UI scale through <see cref="ImGuiApp.SetGlobalScale"/>.</summary>
	Accessibility = 1,

	/// <summary>The Debug menu, which shows the ImGui demo, the ImGui metrics and the performance monitor. Not built on iOS.</summary>
	Debug = 2,

	/// <summary>Every built-in menu. The default.</summary>
	All = Accessibility | Debug,
}
