// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;
using ktsu.ImGui.Widgets.Overlays;

/// <summary>
/// The action sheet: a stack of buttons that slides up from the bottom, with Cancel beneath it.
/// </summary>
internal static class ActionSheetDemo
{
	private static readonly ActionSheetButton[] Buttons = [new("Share"), new("Duplicate"), new("Move to album"), new("Delete photo", IsDestructive: true)];
	private static readonly OverlayHost Host = new();
	private static ImGuiWidgets.ActionSheet sheet = new(Host, "demoActionSheet");

	/// <summary>Gets what the sheet last answered.</summary>
	internal static string LastAnswer { get; private set; } = "(none)";

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		Host.Clear();
		sheet = new ImGuiWidgets.ActionSheet(Host, "demoActionSheet");
		LastAnswer = "(none)";
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Mobile - Action Sheet"))
		{
			ImGui.TextWrapped("An action sheet asks which of a few things to do. Cancel, a tap outside it or Escape cancels.");

			if (DemoProbe.Button("Photo options##actionSheetOpen"))
			{
				sheet.Open(Buttons, index => LastAnswer = index < 0 ? "Cancelled" : Buttons[index].Label, "Beach.jpg");
			}

			ImGui.SameLine();
			ImGui.TextUnformatted($"Answer: {LastAnswer}");
			Host.Render();
		}
	}
}
