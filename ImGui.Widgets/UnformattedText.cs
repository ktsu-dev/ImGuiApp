// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using Hexa.NET.ImGui;

/// <summary>
/// Stand-ins for <c>ImGui.SetTooltip</c>, <c>SetItemTooltip</c> and <c>TextDisabled</c> that draw their text as it is.
/// Those calls bind to cimgui's variadic <c>igText</c> family, which reads the string as a printf format, so a
/// <c>%</c> in a caller's label or hint garbles the output or reads a vararg that was never passed.
/// </summary>
internal static class UnformattedText
{
	/// <summary>Shows <paramref name="text"/> as this frame's tooltip, replacing any set earlier, as <c>SetTooltip</c> does.</summary>
	/// <param name="text">The text to show, drawn as it is.</param>
	public static void Tooltip(string text)
	{
		if (ImGuiP.BeginTooltipEx(ImGuiTooltipFlags.OverridePrevious, ImGuiWindowFlags.None))
		{
			ImGui.TextUnformatted(text);
			ImGui.EndTooltip();
		}
	}

	/// <summary>Shows <paramref name="text"/> as a tooltip while the last item is hovered, as <c>SetItemTooltip</c> does.</summary>
	/// <param name="text">The text to show, drawn as it is.</param>
	public static void ItemTooltip(string text)
	{
		if (ImGui.IsItemHovered(ImGuiHoveredFlags.ForTooltip))
		{
			Tooltip(text);
		}
	}

	/// <summary>Draws <paramref name="text"/> in the disabled text colour, as <c>TextDisabled</c> does.</summary>
	/// <param name="text">The text to draw, as it is.</param>
	public static void Disabled(string text)
	{
		ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]);
		ImGui.TextUnformatted(text);
		ImGui.PopStyleColor();
	}
}
