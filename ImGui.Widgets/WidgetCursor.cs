// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using Hexa.NET.ImGui;

/// <summary>
/// Picks the mouse cursor for a widget's own hit area.
/// </summary>
/// <remarks>
/// A custom widget drawn over an <c>InvisibleButton</c> gives the pointer no hint of what it does,
/// where a built-in one would: ImGui shows a text beam over an input field and a resize arrow over
/// a window edge on its own. The rule the widgets follow is a hand on something clicked, a
/// horizontal or vertical resize arrow on something dragged along one axis, and the four-way arrow
/// on something dragged freely. A plain button keeps the arrow, as ImGui's own does.
/// </remarks>
internal static class WidgetCursor
{
	/// <summary>
	/// Shows <paramref name="cursor"/> while the last submitted item is hovered or held.
	/// </summary>
	/// <remarks>
	/// Held as well as hovered, so a drag keeps its cursor after the pointer leaves the item, which
	/// it routinely does when a handle is pulled past the end of its track.
	/// </remarks>
	/// <param name="cursor">The cursor to show.</param>
	internal static void OnLastItem(ImGuiMouseCursor cursor)
	{
		if (ImGui.IsItemActive() || ImGui.IsItemHovered())
		{
			ImGui.SetMouseCursor(cursor);
		}
	}
}
