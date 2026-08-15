// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.App.Demos;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;

/// <summary>
/// A window that docks into the dockspace created by ImGuiWidgets.DrawDeferredDocked.
/// </summary>
internal sealed class DockedWindowDemo : ImGuiWidgets.DockedWindow
{
	private int clicks;

	/// <inheritdoc/>
	protected override string Title => "Docked Window";

	/// <inheritdoc/>
	protected override void DrawContent()
	{
		ImGui.TextWrapped("This window is managed by Hexa's WidgetManager and docks into the dockspace that DrawDeferredDocked creates. Drag its tab to re-dock it.");

		if (ImGui.Button("Click me"))
		{
			clicks++;
		}

		ImGui.TextUnformatted($"Clicks: {clicks}");
	}
}
