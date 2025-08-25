// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiStyler;

using Hexa.NET.ImGui;
using ktsu.ScopedAction;

/// <summary>
/// Represents a scoped action that applies a theme color to ImGui elements.
/// This provides a simple color-based theming that automatically reverts when disposed.
/// </summary>
public class ScopedThemeColor : ScopedAction
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ScopedThemeColor"/> class.
	/// </summary>
	/// <param name="baseColor">The base color to apply to the theme.</param>
	/// <param name="enabled">Whether the theme should be enabled or disabled.</param>
	public ScopedThemeColor(ImColor baseColor, bool enabled)
	{
		// Simple color adjustments for basic theming
		ImColor primaryColor = enabled ? baseColor : baseColor.MultiplySaturation(0.3f);
		ImColor hoveredColor = primaryColor.MultiplyLuminance(1.2f);
		ImColor activeColor = primaryColor.MultiplyLuminance(0.8f);
		ImColor textColor = primaryColor.CalculateOptimalContrastingColor();
		ImColor backgroundColor = primaryColor.MultiplyLuminance(0.1f).MultiplySaturation(0.1f);

		int numStyles = 0;
		PushStyleAndCount(ImGuiCol.Text, textColor, ref numStyles);
		PushStyleAndCount(ImGuiCol.TextSelectedBg, primaryColor, ref numStyles);
		PushStyleAndCount(ImGuiCol.TextDisabled, textColor.MultiplySaturation(0.5f), ref numStyles);
		PushStyleAndCount(ImGuiCol.Button, primaryColor, ref numStyles);
		PushStyleAndCount(ImGuiCol.ButtonActive, activeColor, ref numStyles);
		PushStyleAndCount(ImGuiCol.ButtonHovered, hoveredColor, ref numStyles);
		PushStyleAndCount(ImGuiCol.CheckMark, textColor, ref numStyles);
		PushStyleAndCount(ImGuiCol.Header, primaryColor, ref numStyles);
		PushStyleAndCount(ImGuiCol.HeaderActive, activeColor, ref numStyles);
		PushStyleAndCount(ImGuiCol.HeaderHovered, hoveredColor, ref numStyles);
		PushStyleAndCount(ImGuiCol.SliderGrab, primaryColor, ref numStyles);
		PushStyleAndCount(ImGuiCol.SliderGrabActive, activeColor, ref numStyles);
		PushStyleAndCount(ImGuiCol.Tab, primaryColor, ref numStyles);
		PushStyleAndCount(ImGuiCol.TabSelected, activeColor, ref numStyles);
		PushStyleAndCount(ImGuiCol.TabHovered, hoveredColor, ref numStyles);
		PushStyleAndCount(ImGuiCol.FrameBg, primaryColor, ref numStyles);
		PushStyleAndCount(ImGuiCol.FrameBgActive, activeColor, ref numStyles);
		PushStyleAndCount(ImGuiCol.FrameBgHovered, hoveredColor, ref numStyles);
		PushStyleAndCount(ImGuiCol.WindowBg, backgroundColor, ref numStyles);
		PushStyleAndCount(ImGuiCol.ChildBg, backgroundColor, ref numStyles);
		PushStyleAndCount(ImGuiCol.PopupBg, backgroundColor, ref numStyles);

		OnClose = () => ImGui.PopStyleColor(numStyles);
	}

	private static void PushStyleAndCount(ImGuiCol style, ImColor color, ref int numStyles)
	{
		ImGui.PushStyleColor(style, color.Value);
		++numStyles;
	}
}
