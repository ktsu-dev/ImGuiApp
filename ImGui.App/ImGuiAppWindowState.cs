// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App;

using System.Numerics;

#if !IOS
using Silk.NET.Windowing;
#endif

/// <summary>
/// Represents the state of the ImGui application window, including size, position, and layout state.
/// </summary>
public class ImGuiAppWindowState
{
	/// <summary>
	/// Gets or sets the size of the window.
	/// </summary>
	public Vector2 Size { get; set; } = new(1280, 720);

	/// <summary>
	/// Gets or sets the position of the window.
	/// </summary>
	public Vector2 Pos { get; set; } = new(-short.MinValue, -short.MinValue);

#if !IOS
	/// <summary>
	/// Gets or sets the layout state of the window.
	/// </summary>
	/// <remarks>
	/// Backed by the desktop windowing layer (Silk.NET). iOS controls window sizing itself, so
	/// this member is excluded from the <c>net10.0-ios</c> build; see the iOS platform port plan.
	/// </remarks>
	public WindowState LayoutState { get; set; }
#endif
}
