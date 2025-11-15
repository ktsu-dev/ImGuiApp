// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp;
using System.Numerics;

using Silk.NET.Windowing;

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

	/// <summary>
	/// Gets or sets the layout state of the window.
	/// </summary>
	public WindowState LayoutState { get; set; }
}
