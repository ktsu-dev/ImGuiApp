// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;

/// <summary>
/// The long-press menu: hold a card, or right-click it, for a menu of actions.
/// </summary>
internal static class LongPressMenuDemo
{
	private static readonly string[] Actions = ["Copy", "Share", "Delete"];
	private static readonly string[] Cards = ["Beach", "Forest", "City"];

	/// <summary>Gets what the last chosen action did.</summary>
	internal static string LastAction { get; private set; } = "(none)";

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState() => LastAction = "(none)";

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Mobile - Long Press Menu"))
		{
			ImGui.TextWrapped("Press and hold a card until it gives a little bump, or right-click it. A quick click still clicks the card.");

			for (int i = 0; i < Cards.Length; i++)
			{
				string card = Cards[i];
				if (i > 0)
				{
					ImGui.SameLine();
				}

				int chosen = ImGuiWidgets.LongPressMenu($"demoCard{i}", Actions, () =>
				{
					if (ImGui.Button($"{card}##demoCardButton{i}", new Vector2(110f, 80f)))
					{
						LastAction = $"Opened {card}";
					}
				});

				if (chosen >= 0)
				{
					LastAction = $"{Actions[chosen]} {card}";
				}
			}

			ImGui.TextUnformatted($"Last action: {LastAction}");
		}
	}
}
