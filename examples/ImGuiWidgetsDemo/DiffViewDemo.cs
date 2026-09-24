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

	/// <summary>Gets the hunks ticked in the first diff view.</summary>
	internal static IReadOnlyCollection<int> FirstSelected => FirstSelection;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics,
	/// which outlive a harness, so a test that ticked a hunk would otherwise decide what the next
	/// test starts from.
	/// </summary>
	internal static void ResetState()
	{
		FirstSelection.Clear();
		SecondSelection.Clear();
		mode = ImGuiWidgets.DiffViewMode.Unified;
	}

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

		// Fixed to side by side rather than following the toggle. This is what puts the paired
		// layout under the headless suite on every run, since the toggle above defaults to unified
		// and the suite never touches it. Wiring this view to the toggle for symmetry would silently
		// remove that coverage.
		ImGuiWidgets.DiffViewOptions secondOptions = new() { Mode = ImGuiWidgets.DiffViewMode.SideBySide };

		ImGui.TextUnformatted("A second view, always side by side and independently collapsible");
		_ = ImGuiWidgets.DiffView("##diffTwo", Hunks, SecondSelection, secondOptions);

		ImGui.Separator();

		// Both options off, and drawn through the overload with no selection. This is the only place
		// the two-column table, the fixed heading and the view with nothing to tick are drawn at all,
		// so it is what puts those shapes under the headless suite.
		ImGuiWidgets.DiffViewOptions plainOptions = new()
		{
			CanShowLineNumbers = false,
			CanCollapseHunks = false,
		};

		ImGui.TextUnformatted("A third view: no line numbers, no collapsing, nothing to select");
		ImGuiWidgets.DiffView("##diffThree", Hunks, plainOptions);
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
