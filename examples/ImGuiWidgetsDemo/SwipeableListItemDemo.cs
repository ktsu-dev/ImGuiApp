// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;

/// <summary>
/// The swipeable list: rows that slide aside to reveal actions.
/// </summary>
internal static class SwipeableListItemDemo
{
	private static readonly string[] InitialSubjects = ["Lunch on Friday?", "Your order has shipped", "Minutes from Tuesday", "Invoice 2041", "Welcome aboard"];
	private static readonly SwipeAction[] Leading = [new("Read")];
	private static readonly SwipeAction[] Trailing = [new("Delete", Destructive: true), new("Archive")];

	private static List<string> subjects = [.. InitialSubjects];

	/// <summary>Gets the subjects still in the list.</summary>
	internal static IReadOnlyList<string> Subjects => subjects;

	/// <summary>Gets what the last chosen action did.</summary>
	internal static string LastAction { get; private set; } = "(none)";

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		subjects = [.. InitialSubjects];
		LastAction = "(none)";
	}

	/// <summary>Draws a row's subject, inset from its left edge and centred vertically.</summary>
	/// <param name="subject">The subject.</param>
	/// <param name="size">The row's size.</param>
	internal static void DrawSubject(string subject, Vector2 size)
	{
		ImGui.SetCursorScreenPos(ImGui.GetCursorScreenPos() + new Vector2(ImGui.GetStyle().FramePadding.X, (size.Y - ImGui.GetTextLineHeight()) * 0.5f));
		ImGui.TextUnformatted(subject);
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("Mobile - Swipeable List"))
		{
			ImGui.TextWrapped("Drag a row left for Delete and Archive, or right for Read. Drag most of the way across to delete without a second tap.");
			ImGui.Separator();

			string? remove = null;
			foreach (string subject in subjects)
			{
				SwipeActionResult? result = ImGuiWidgets.SwipeableListItem(
					$"swipe-{subject}",
					ImGui.GetFrameHeightWithSpacing() * 1.5f,
					size => DrawSubject(subject, size),
					Leading,
					Trailing);

				if (result is SwipeActionResult chosen)
				{
					SwipeAction action = chosen.Edge == SwipeEdge.Leading ? Leading[chosen.Index] : Trailing[chosen.Index];
					LastAction = $"{action.Label}: {subject}";
					if (action.Destructive || action.Label == "Archive")
					{
						remove = subject;
					}
				}

				ImGui.Separator();
			}

			if (remove is not null)
			{
				subjects.Remove(remove);
			}

			ImGui.TextUnformatted($"Last action: {LastAction}");
			if (DemoProbe.Button("Restore rows##swipeRestore"))
			{
				ResetState();
			}
		}
	}
}
