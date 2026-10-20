// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;

/// <summary>
/// The app bar, collapsing away as the feed under it scrolls down and coming back as it scrolls up.
/// </summary>
internal static class AppBarDemo
{
	private static readonly AppBarOptions Options = new()
	{
		Leading = new AppBarAction("=", "Menu"),
		Actions = [new AppBarAction("?", "Search"), new AppBarAction("...", "More")],
		CollapseOnScroll = true,
	};

	private static float scroll;

	/// <summary>Gets the name of the app bar button last clicked.</summary>
	internal static string LastAction { get; private set; } = "(none)";

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		scroll = 0f;
		LastAction = "(none)";
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Mobile - App Bar"))
		{
			ImGui.TextWrapped("Scroll the feed down and the bar slides away; scroll up and it comes back.");
			ImGui.TextUnformatted($"Last clicked: {LastAction}");

			if (ImGui.BeginChild("##appBarPhone", new Vector2(360f, 260f), ImGuiChildFlags.Borders))
			{
				AppBarResult result = ImGuiWidgets.AppBar("demoAppBar", "Feed", Options, scroll);
				if (result.LeadingClicked)
				{
					LastAction = Options.Leading!.Value.Name;
				}
				else if (result.ActionClicked >= 0)
				{
					LastAction = Options.Actions[result.ActionClicked].Name;
				}

				if (ImGui.BeginChild("##appBarFeed", Vector2.Zero))
				{
					for (int i = 1; i <= 40; i++)
					{
						ImGui.TextUnformatted($"Post {i}: something worth scrolling past");
					}

					scroll = ImGui.GetScrollY();
				}

				ImGui.EndChild();
			}

			ImGui.EndChild();
		}
	}
}
