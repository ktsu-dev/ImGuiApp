// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;

/// <summary>
/// The text helpers: centering, clipping and alignment.
/// </summary>
internal static class TextUtilitiesDemo
{
	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		// Nothing to reset: the section keeps no state.
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Text Utilities"))
		{
			ImGui.TextUnformatted("Enhanced text rendering with alignment and clipping:");
			ImGui.Separator();

			// Regular text
			ImGuiWidgets.Text("Regular text");

			ImGui.Separator();

			// Centered text
			ImGui.TextUnformatted("Centered text in available space:");
			ImGuiWidgets.TextCentered("This text is centered!");

			ImGui.Separator();

			// Text centered within bounds
			ImGui.TextUnformatted("Text centered within 200px container:");
			Vector2 containerSize = new(200, 50);
			ImGui.GetWindowDrawList().AddRect(
				ImGui.GetCursorScreenPos(),
				ImGui.GetCursorScreenPos() + containerSize,
				ImGui.GetColorU32(ImGuiCol.Border)
			);
			ImGuiWidgets.TextCenteredWithin("Centered within bounds", containerSize);
			ImGui.SetCursorPosY(ImGui.GetCursorPosY() + containerSize.Y);

			ImGui.Separator();

			// Clipped text
			ImGui.TextUnformatted("Text clipping demo (150px width):");
			Vector2 clipSize = new(150, 30);
			ImGui.GetWindowDrawList().AddRect(
				ImGui.GetCursorScreenPos(),
				ImGui.GetCursorScreenPos() + clipSize,
				ImGui.GetColorU32(ImGuiCol.Border)
			);
			// Demonstrate text clipping by manually truncating long text
			string longText = "This is a very long text that will be clipped with ellipsis";
			float textWidth = ImGui.CalcTextSize(longText).X;
			string displayText = longText;
			if (textWidth > clipSize.X)
			{
				// Manually clip the text for demo purposes
				while (ImGui.CalcTextSize(displayText + "...").X > clipSize.X && displayText.Length > 0)
				{
					displayText = displayText[..^1];
				}
				displayText += "...";
			}
			ImGuiWidgets.TextCenteredWithin(displayText, clipSize);
			ImGui.SetCursorPosY(ImGui.GetCursorPosY() + clipSize.Y);
		}
	}
}
