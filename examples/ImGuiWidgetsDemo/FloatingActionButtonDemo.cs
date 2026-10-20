// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;

/// <summary>
/// The floating action button and the speed dial, each floating in the corner of a scrolling list.
/// </summary>
internal static class FloatingActionButtonDemo
{
	private static readonly SpeedDialAction[] Actions = [new("P", "Photo"), new("N", "Note"), new("R", "Reminder")];

	/// <summary>Gets how many notes the floating button has added.</summary>
	internal static int Notes { get; private set; } = 3;

	/// <summary>Gets what the speed dial last created.</summary>
	internal static string Created { get; private set; } = "(none)";

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		Notes = 3;
		Created = "(none)";
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Mobile - Floating Action Button"))
		{
			ImGui.TextWrapped("The round button floats in the corner of its region while the content scrolls beneath it. The speed dial opens into smaller actions.");

			Vector2 size = new(300f, 220f);
			if (ImGui.BeginChild("##fabNotes", size, ImGuiChildFlags.Borders))
			{
				for (int i = 0; i < Notes; i++)
				{
					ImGui.TextUnformatted($"Note {i + 1}");
				}

				if (ImGuiWidgets.FloatingActionButton("demoFab", "+", new FloatingActionButtonOptions { Tooltip = "Add a note" }))
				{
					Notes++;
				}
			}

			ImGui.EndChild();
			ImGui.SameLine();
			if (ImGui.BeginChild("##fabDial", size, ImGuiChildFlags.Borders))
			{
				ImGui.TextUnformatted($"Created: {Created}");
				int chosen = ImGuiWidgets.SpeedDial("demoSpeedDial", "+", Actions);
				if (chosen >= 0)
				{
					Created = Actions[chosen].Label;
				}
			}

			ImGui.EndChild();
		}
	}
}
