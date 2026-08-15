// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using HexaTooltipHelper = Hexa.NET.ImGui.Widgets.TooltipHelper;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Shows a tooltip for the preceding item while it is hovered.
	/// </summary>
	/// <param name="description">The tooltip text.</param>
	/// <exception cref="ArgumentNullException"><paramref name="description"/> is <see langword="null"/>.</exception>
	public static void Tooltip(string description)
	{
		Ensure.NotNull(description);
		HexaTooltipHelper.Tooltip(description);
	}
}
