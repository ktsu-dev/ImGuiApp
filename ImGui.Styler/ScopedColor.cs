// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Styler;

using Hexa.NET.ImGui;

using ktsu.ScopedAction;

/// <summary>
/// Represents a scoped color change in ImGui.
/// </summary>
/// <remarks>
/// This class ensures that the color change is reverted when the scope ends.
/// </remarks>
public class ScopedColor : ScopedAction
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ScopedColor"/> class with a specified target and color.
	/// </summary>
	/// <param name="target">The ImGui color target to change.</param>
	/// <param name="color">The color to apply to the target.</param>
	public ScopedColor(ImGuiCol target, ImColor color) : base(
	onOpen: () => ImGui.PushStyleColor(target, color.Value),
	onClose: ImGui.PopStyleColor)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ScopedColor"/> class with a specified color for the button.
	/// </summary>
	/// <param name="color">The color to apply to the button.</param>
	public ScopedColor(ImColor color)
	{
		ImGui.PushStyleColor(ImGuiCol.Button, color.Value);
		OnClose = ImGui.PopStyleColor;
	}
}
