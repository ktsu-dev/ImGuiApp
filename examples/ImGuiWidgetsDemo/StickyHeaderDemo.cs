// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Numerics;

using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;

/// <summary>
/// Sticky headers: a contact list whose letter headers stay at the top while their section scrolls.
/// </summary>
internal static class StickyHeaderDemo
{
	private static readonly (string Letter, string[] Names)[] Contacts =
	[
		("A", ["Ada", "Alan", "Alice", "Amir", "Anika", "Arjun"]),
		("B", ["Beatriz", "Ben", "Bianca", "Boris", "Bruno"]),
		("C", ["Carmen", "Chen", "Chloe", "Clara", "Cyrus", "Celine"]),
		("D", ["Dana", "Daniel", "Dmitri", "Doris", "Duc"]),
		("E", ["Elena", "Elif", "Emeka", "Esther", "Ezra"]),
	];

	/// <summary>Gets the section whose header was pinned on the last frame, or null.</summary>
	internal static string? Pinned { get; private set; }

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState() => Pinned = null;

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Mobile - Sticky Headers"))
		{
			ImGui.TextWrapped("Scroll the contacts. Each letter stays at the top until the next one pushes it out.");

			ImGuiWidgets.StickyHeaderList list = new("demoContacts", new Vector2(0f, 200f));
			foreach ((string letter, string[] names) in Contacts)
			{
				list.Section(letter);
				foreach (string name in names)
				{
					ImGui.TextUnformatted(name);
				}
			}

			list.Dispose();
			Pinned = list.PinnedTitle;
		}
	}
}
