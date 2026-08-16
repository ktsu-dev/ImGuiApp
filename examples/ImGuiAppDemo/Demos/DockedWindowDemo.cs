// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.App.Demos;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;

/// <summary>
/// A floating window the user can drag into the dockspace ImGuiWidgets.DrawDeferredDocked creates.
/// </summary>
internal sealed class DockedWindowDemo : ImGuiWidgets.DockedWindow
{
	private int clicks;

	/// <inheritdoc/>
	protected override string Title => "Docked Window";

	/// <inheritdoc/>
	protected override void DrawContent()
	{
		ImGui.TextWrapped("This window is managed by Hexa's WidgetManager. It opens floating, not docked -- drag its title bar into the dockspace that DrawDeferredDocked creates to dock it.");

		if (ImGui.Button("Click me"))
		{
			clicks++;
		}

		ImGui.TextUnformatted($"Clicks: {clicks}");
	}
}
