// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Styler;

using Hexa.NET.ImGui;

/// <summary>
/// Represents a scoped text color change in ImGui.
/// </summary>
/// <param name="color">The color to apply to the text.</param>
public class ScopedTextColor(ImColor color) : ScopedColor(ImGuiCol.Text, color)
{
}
