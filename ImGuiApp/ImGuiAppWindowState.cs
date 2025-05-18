// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp;
using System.Numerics;

/// <summary>
/// Represents the state of the ImGui application window, including size and position.
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
	public Vector2 Position { get; set; } = new(-short.MinValue, -short.MinValue);
}
