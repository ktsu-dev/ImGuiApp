// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Styler;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.ScopedAction;
using ktsu.Semantics.Color;

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

	/// <summary>
	/// Initializes a new instance of the <see cref="ScopedColor"/> class with a specified target and semantic color.
	/// </summary>
	/// <param name="target">The ImGui color target to change.</param>
	/// <param name="color">The semantic color to apply; converted to the sRGB value ImGui expects.</param>
	public ScopedColor(ImGuiCol target, Color color) : this(target, color.ToImColor())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ScopedColor"/> class with a specified semantic color for the button.
	/// </summary>
	/// <param name="color">The semantic color to apply to the button.</param>
	public ScopedColor(Color color) : this(color.ToImColor())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ScopedColor"/> class with a specified target and sRGB color.
	/// </summary>
	/// <param name="target">The ImGui color target to change.</param>
	/// <param name="srgb">The sRGB color to apply; packed directly with no linear round-trip.</param>
	/// <param name="alpha">The alpha to apply (0-1, default opaque).</param>
	public ScopedColor(ImGuiCol target, Srgb srgb, float alpha = 1f) : this(target, srgb.ToImColor(alpha))
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ScopedColor"/> class with a specified sRGB color for the button.
	/// </summary>
	/// <param name="srgb">The sRGB color to apply to the button.</param>
	/// <param name="alpha">The alpha to apply (0-1, default opaque).</param>
	public ScopedColor(Srgb srgb, float alpha = 1f) : this(srgb.ToImColor(alpha))
	{
	}
}
