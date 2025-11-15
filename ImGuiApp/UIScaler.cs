// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ScopedAction;

/// <summary>
/// Class responsible for scaling UI elements in ImGui.
/// </summary>
public class UIScaler : ScopedAction
{
	/// <summary>
	/// Initializes a new instance of the <see cref="UIScaler"/> class.
	/// Scales various ImGui style variables by the specified scale factor.
	/// </summary>
	/// <param name="scale">The scale factor to apply to the UI elements.</param>
	public UIScaler(float scale)
	{
		ImGuiStylePtr style = ImGui.GetStyle();
		int numStyles = 0;
		PushStyleAndCount(ImGuiStyleVar.CellPadding, style.CellPadding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.ChildBorderSize, style.ChildBorderSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.ChildRounding, style.ChildRounding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.DockingSeparatorSize, style.DockingSeparatorSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.FrameBorderSize, style.FrameBorderSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.FramePadding, style.FramePadding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.FrameRounding, style.FrameRounding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.GrabMinSize, style.GrabMinSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.GrabRounding, style.GrabRounding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.IndentSpacing, style.IndentSpacing * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.ItemInnerSpacing, style.ItemInnerSpacing * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.ItemSpacing, style.ItemSpacing * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.PopupBorderSize, style.PopupBorderSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.PopupRounding, style.PopupRounding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.ScrollbarRounding, style.ScrollbarRounding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.ScrollbarSize, style.ScrollbarSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.SeparatorTextBorderSize, style.SeparatorTextBorderSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.SeparatorTextPadding, style.SeparatorTextPadding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.TabRounding, style.TabRounding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.WindowBorderSize, style.WindowBorderSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.WindowMinSize, style.WindowMinSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.WindowPadding, style.WindowPadding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.WindowRounding, style.WindowRounding * scale, ref numStyles);

		OnClose = () => ImGuiApp.Invoker.Invoke(() => ImGui.PopStyleVar(numStyles));
	}

	/// <summary>
	/// Pushes a style variable and increments the style count.
	/// </summary>
	/// <param name="style">The style variable to push.</param>
	/// <param name="value">The value to set for the style variable.</param>
	/// <param name="numStyles">The reference to the style count.</param>
	internal static void PushStyleAndCount(ImGuiStyleVar style, float value, ref int numStyles)
	{
		ImGuiApp.Invoker.Invoke(() => ImGui.PushStyleVar(style, value));
		++numStyles;
	}

	/// <summary>
	/// Pushes a style variable and increments the style count.
	/// </summary>
	/// <param name="style">The style variable to push.</param>
	/// <param name="value">The value to set for the style variable.</param>
	/// <param name="numStyles">The reference to the style count.</param>
	internal static void PushStyleAndCount(ImGuiStyleVar style, Vector2 value, ref int numStyles)
	{
		ImGuiApp.Invoker.Invoke(() => ImGui.PushStyleVar(style, value));
		++numStyles;
	}
}
