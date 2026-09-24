// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Collections.Generic;
using System.Globalization;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;

/// <summary>Shows the diff view over a fixture diff.</summary>
internal static class DiffViewDemo
{
	private static readonly HashSet<int> FirstSelection = [];
	private static readonly HashSet<int> SecondSelection = [];
	private static ImGuiWidgets.DiffViewMode mode = ImGuiWidgets.DiffViewMode.Unified;

	private static readonly IReadOnlyList<ImGuiWidgets.DiffHunk> Hunks = BuildHunks();

	public static void Show()
	{
		if (!DemoProbe.Header("Diff view"))
		{
			return;
		}

		bool isSideBySide = mode == ImGuiWidgets.DiffViewMode.SideBySide;

		if (ImGui.Checkbox("Side by side", ref isSideBySide))
		{
			mode = isSideBySide ? ImGuiWidgets.DiffViewMode.SideBySide : ImGuiWidgets.DiffViewMode.Unified;
		}

		ImGuiWidgets.DiffViewOptions options = new() { Mode = mode };

		ImGui.TextUnformatted("Selectable");
		_ = ImGuiWidgets.DiffView("##diffOne", Hunks, FirstSelection, options);

		ImGui.Separator();

		// A second view in the same frame, to prove the two do not share collapsed state.
		ImGui.TextUnformatted("A second view, independently collapsible");
		_ = ImGuiWidgets.DiffView("##diffTwo", Hunks, SecondSelection, options);
	}

	private static IReadOnlyList<ImGuiWidgets.DiffHunk> BuildHunks()
	{
		List<ImGuiWidgets.DiffLine> longHunk = [];

		for (int line = 0; line < 400; line++)
		{
			longHunk.Add(new ImGuiWidgets.DiffLine
			{
				Kind = line % 7 == 0 ? ImGuiWidgets.DiffLineKind.Added : ImGuiWidgets.DiffLineKind.Context,
				Text = string.Create(CultureInfo.InvariantCulture, $"generated line {line}"),
				NewNumber = line + 1,
				OldNumber = line % 7 == 0 ? null : line + 1,
			});
		}

		return
		[
			new ImGuiWidgets.DiffHunk
			{
				Heading = "void Render()",
				Lines =
				[
					new() { Kind = ImGuiWidgets.DiffLineKind.Context, Text = "before", OldNumber = 1, NewNumber = 1 },
					new() { Kind = ImGuiWidgets.DiffLineKind.Removed, Text = "was this", OldNumber = 2 },
					new() { Kind = ImGuiWidgets.DiffLineKind.Added, Text = "is this now", NewNumber = 2 },
					new() { Kind = ImGuiWidgets.DiffLineKind.Added, Text = "and this too", NewNumber = 3 },
					new() { Kind = ImGuiWidgets.DiffLineKind.Context, Text = "after", OldNumber = 3, NewNumber = 4 },
				],
			},
			new ImGuiWidgets.DiffHunk
			{
				Heading = "only additions",
				Lines =
				[
					new() { Kind = ImGuiWidgets.DiffLineKind.Added, Text = "a brand new line", NewNumber = 10 },
					new() { Kind = ImGuiWidgets.DiffLineKind.Added, Text = "and another", NewNumber = 11 },
				],
			},
			new ImGuiWidgets.DiffHunk
			{
				Heading = "a very long line",
				Lines =
				[
					new()
					{
						Kind = ImGuiWidgets.DiffLineKind.Added,
						Text = new string('x', 400),
						NewNumber = 20,
					},
				],
			},
			new ImGuiWidgets.DiffHunk { Heading = "generated", Lines = longHunk },
		];
	}
}
