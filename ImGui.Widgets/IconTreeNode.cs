// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.Semantics.Color;

using HexaTreeNode = Hexa.NET.ImGui.Widgets.ImGuiTreeNode;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a tree node with a coloured icon glyph before its label.
	/// </summary>
	/// <param name="label">Label for display and identity.</param>
	/// <param name="icon">Icon glyph to draw before the label, typically a single character from an icon font.</param>
	/// <param name="iconColor">Colour applied to the icon glyph only; the label uses the current text colour.</param>
	/// <param name="flags">Tree node behaviour flags.</param>
	/// <returns><see langword="true"/> if the node is open and its children should be drawn.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="label"/> or <paramref name="icon"/> is <see langword="null"/>.</exception>
	public static bool IconTreeNode(string label, string icon, Color iconColor, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
	{
		Ensure.NotNull(label);
		Ensure.NotNull(icon);
		return HexaTreeNode.IconTreeNode(label, icon, iconColor.ToImGuiVector4(), flags);
	}
}
