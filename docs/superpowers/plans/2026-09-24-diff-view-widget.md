# Diff view widget implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give this library a widget that draws a diff someone else computed, unified or side by side, with whole hunks tickable so a caller can stage what it selected.

**Architecture:** A neutral model of hunks and lines, a pure state class holding the three rules worth testing (pairing, summarizing, selection), and a drawing layer over both. The model is shaped like `ktsu.GitIntegration`'s so a caller maps field for field, but depends on nothing.

**Tech Stack:** C#, net10.0 and net9.0, MSTest, `Hexa.NET.ImGui`, `ktsu.ImGui.Color`, `ktsu.ImGui.Styler`.

**Spec:** `docs/superpowers/specs/2026-09-24-diff-view-widget-design.md`

## Global Constraints

Every task's requirements implicitly include this section.

- Copyright header on every new file, exactly: `// Copyright (c) 2023-2026 ktsu-dev contributors`
- Namespaces: `ktsu.ImGui.Widgets` for library files, `ktsu.ImGui.Widgets.Tests` for tests. File-scoped, with every `using` **inside** the namespace declaration.
- Indent with tabs, never spaces.
- Every widget file is `public static partial class ImGuiWidgets`, and every public type the widget exposes is nested inside it. That is how `SearchBoxOptions`, `CurveTrackState` and the rest already work.
- The public entry point is a `public static` method on `ImGuiWidgets`. The drawing lives in a nested `internal static class DiffViewImpl`, and per-widget transient state in `private static readonly Dictionary<uint, DiffViewState> States = []` inside it, keyed by the ImGui id. `CurveTrack.cs` is the model to follow.
- `DiffViewState` contains **no ImGui calls at all**. That is what lets its rules be tested without a graphics context, and every other `*State` type here holds the same line.
- US spelling in identifiers, comments and user-facing strings.
- **No semicolons or dashes joining clauses in prose**, including XML doc comments.
- Booleans are named `Is`, `Can` or `Has` followed by the thing they describe, never `Allow` or `Use`.
- Null guards are `Ensure.NotNull(x)` from Polyfill. `KTSU0003` in `ktsu.Sdk.Analyzers` is an **error**-level rule that rejects `ArgumentNullException.ThrowIfNull`, and the library uses `Ensure.NotNull` throughout.
- Compare sequences with `Assert.AreSequenceEqual` where the assertion is about a sequence.
- Warnings are errors. The build fails on an unused `using` and on formatting.
- Public API additions require the `CompatibilitySuppressions.xml` dance only when something is **removed or changed**. Adding new members needs nothing.
- Commit messages carry a version tag: `[major]`, `[minor]`, `[patch]`. This work is `[minor]`. **Do not add `Co-Authored-By` lines.**
- Do not edit `VERSION.md`, `CHANGELOG.md` or `LICENSE.md`.
- Build with `dotnet build ImGui.Widgets/ImGui.Widgets.csproj`. Run the widget tests with `./tests/ImGui.Widgets.Tests/bin/Debug/net10.0/ktsu.ImGui.Widgets.Tests`, optionally with `--filter "FullyQualifiedName~DiffViewStateTests"`.
- Baseline before any change: **283 tests passing, 0 warnings.**

### One refinement to the spec

The spec says the added and removed tints derive from a fixed hue with saturation and value taken from `ImGuiCol.FrameBg`. Use `Palette.Semantic.Success` and `Palette.Semantic.Error` from `ktsu.ImGui.Styler` instead, at low alpha. They resolve against the current theme already, which is what the spec was reaching for, and hand-rolling an HSV derivation beside a facility that exists would be the widget guessing again. Context and filler still come from `ImGuiCol.FrameBg`.

## Review Focus

Five inputs the spec implies that no task's happy path exercises, most likely to bite first.

1. **An empty hunk list, and a hunk with no lines.** A staging view shows a file whose hunks have all been staged. Both must draw nothing rather than an empty table or an exception. Test in Task 1 and Task 2.
2. **A selection holding an index past the end of the hunks.** The spec says stale indices are ignored rather than tracked. Nothing in the happy path produces one. Test in Task 1.
3. **A hunk that is only additions, or only removals.** Pairing then has nothing to pair against and every row on one side is filler, which is exactly where an off-by-one lives. Test in Task 1.
4. **Two `DiffView` calls in one frame.** They must not share collapsed state or fight over ids. Test in Task 2 by drawing two in the demo.
5. **A line longer than the window.** The text column must not force the whole table wider than its parent, or the hunk header and checkbox scroll out of reach. Test in Task 3 in the demo.

---

## File Structure

| File | Responsibility |
|---|---|
| `ImGui.Widgets/DiffViewModel.cs` | **Create.** `DiffLineKind`, `DiffLine`, `DiffHunk` |
| `ImGui.Widgets/DiffViewState.cs` | **Create.** Pairing, summarizing, selection, collapse. No ImGui |
| `ImGui.Widgets/DiffView.cs` | **Create.** `DiffView`, `DiffViewOptions`, `DiffViewMode`, `DiffViewImpl` |
| `tests/ImGui.Widgets.Tests/DiffViewStateTests.cs` | **Create.** The three rules |
| `examples/ImGuiWidgetsDemo/DiffViewDemo.cs` | **Create.** A page showing both modes |
| `examples/ImGuiWidgetsDemo/ImGuiWidgetsDemo.cs` | **Modify.** Call the new page |
| `tests/ImGuiWidgetsDemo.UITests/WidgetsDemoUITests.cs` | **Modify.** Register the new demo section so it is exercised headlessly |
| `ImGui.Widgets/README.md` | **Modify.** An entry under Display and Status |

---

## Task 1: The model and the rules

**Files:**
- Create: `ImGui.Widgets/DiffViewModel.cs`
- Create: `ImGui.Widgets/DiffViewState.cs`
- Test: `tests/ImGui.Widgets.Tests/DiffViewStateTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `public enum DiffLineKind { Context, Added, Removed }`
  - `public sealed record DiffLine { DiffLineKind Kind; string Text; int? OldNumber; int? NewNumber; }`
  - `public sealed record DiffHunk { IReadOnlyList<DiffLine> Lines; string Heading; }`
  - `public static IReadOnlyList<int> SelectedHunks(ISet<int> selection, int hunkCount)`
  - `internal readonly record struct DiffRowPair(DiffLine? Left, DiffLine? Right)`
  - `internal sealed class DiffViewState` with `Pair`, `Summarize`, `Toggle`, `SelectAll`, `SelectNone`, `IsCollapsed`, `ToggleCollapsed`

This task runs no ImGui and draws nothing. Everything worth asserting about this widget is here.

- [ ] **Step 1: Write the failing tests**

Create `tests/ImGui.Widgets.Tests/DiffViewStateTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the rules behind DiffView. All pure, so no ImGui context is required.
/// </summary>
[TestClass]
public class DiffViewStateTests
{
	private static readonly int[] FirstThreeIndices = [0, 1, 2];
	private static readonly int[] FirstIndexOnly = [0];

	private static ImGuiWidgets.DiffLine Context(string text) =>
		new() { Kind = ImGuiWidgets.DiffLineKind.Context, Text = text };

	private static ImGuiWidgets.DiffLine Added(string text) =>
		new() { Kind = ImGuiWidgets.DiffLineKind.Added, Text = text };

	private static ImGuiWidgets.DiffLine Removed(string text) =>
		new() { Kind = ImGuiWidgets.DiffLineKind.Removed, Text = text };

	private static ImGuiWidgets.DiffHunk Hunk(params ImGuiWidgets.DiffLine[] lines) =>
		new() { Lines = lines };

	[TestMethod]
	public void Pair_ContextLine_AppearsOnBothSides()
	{
		IReadOnlyList<ImGuiWidgets.DiffRowPair> rows =
			ImGuiWidgets.DiffViewState.Pair(Hunk(Context("a")));

		Assert.AreEqual(1, rows.Count);
		Assert.AreEqual("a", rows[0].Left!.Text);
		Assert.AreEqual("a", rows[0].Right!.Text);
	}

	[TestMethod]
	public void Pair_EqualRuns_PairsThemByIndex()
	{
		IReadOnlyList<ImGuiWidgets.DiffRowPair> rows = ImGuiWidgets.DiffViewState.Pair(
			Hunk(Removed("x"), Removed("y"), Added("X"), Added("Y")));

		Assert.AreEqual(2, rows.Count);
		Assert.AreEqual("x", rows[0].Left!.Text);
		Assert.AreEqual("X", rows[0].Right!.Text);
		Assert.AreEqual("y", rows[1].Left!.Text);
		Assert.AreEqual("Y", rows[1].Right!.Text);
	}

	[TestMethod]
	public void Pair_MoreRemovedThanAdded_FillsTheRightSide()
	{
		IReadOnlyList<ImGuiWidgets.DiffRowPair> rows = ImGuiWidgets.DiffViewState.Pair(
			Hunk(Removed("x"), Removed("y"), Removed("z"), Added("X")));

		Assert.AreEqual(3, rows.Count);
		Assert.IsNotNull(rows[0].Right);
		Assert.IsNull(rows[1].Right, "A row with nothing on the new side is filler, which keeps the two sides level.");
		Assert.IsNull(rows[2].Right);
		Assert.AreEqual("z", rows[2].Left!.Text);
	}

	[TestMethod]
	public void Pair_MoreAddedThanRemoved_FillsTheLeftSide()
	{
		IReadOnlyList<ImGuiWidgets.DiffRowPair> rows = ImGuiWidgets.DiffViewState.Pair(
			Hunk(Removed("x"), Added("X"), Added("Y")));

		Assert.AreEqual(2, rows.Count);
		Assert.IsNull(rows[1].Left);
		Assert.AreEqual("Y", rows[1].Right!.Text);
	}

	[TestMethod]
	public void Pair_OnlyAdditions_IsEntirelyFillerOnTheLeft()
	{
		IReadOnlyList<ImGuiWidgets.DiffRowPair> rows = ImGuiWidgets.DiffViewState.Pair(
			Hunk(Added("X"), Added("Y")));

		Assert.AreEqual(2, rows.Count);
		Assert.IsTrue(
			rows.All(static row => row.Left is null),
			"A hunk that only adds has no old side at all, which is what a new file's hunk looks like.");
	}

	[TestMethod]
	public void Pair_OnlyRemovals_IsEntirelyFillerOnTheRight()
	{
		IReadOnlyList<ImGuiWidgets.DiffRowPair> rows = ImGuiWidgets.DiffViewState.Pair(
			Hunk(Removed("x"), Removed("y")));

		Assert.AreEqual(2, rows.Count);
		Assert.IsTrue(rows.All(static row => row.Right is null));
	}

	[TestMethod]
	public void Pair_TwoRunsSeparatedByContext_DoesNotPairAcrossTheContext()
	{
		IReadOnlyList<ImGuiWidgets.DiffRowPair> rows = ImGuiWidgets.DiffViewState.Pair(
			Hunk(Removed("x"), Context("c"), Added("Y")));

		Assert.AreEqual(3, rows.Count);
		Assert.IsNull(rows[0].Right, "The first run closes at the context line, so its removal has no partner.");
		Assert.AreEqual("c", rows[1].Left!.Text);
		Assert.IsNull(rows[2].Left);
	}

	[TestMethod]
	public void Pair_EmptyHunk_IsNoRows()
	{
		IReadOnlyList<ImGuiWidgets.DiffRowPair> rows = ImGuiWidgets.DiffViewState.Pair(Hunk());

		Assert.AreEqual(0, rows.Count);
	}

	[TestMethod]
	public void Summarize_NoChanges_ShowsNothing()
	{
		(int removed, int added) = ImGuiWidgets.DiffViewState.Summarize(0, 0);

		Assert.AreEqual(0, removed);
		Assert.AreEqual(0, added);
	}

	[TestMethod]
	public void Summarize_UnderTheCap_ShowsTheCountsThemselves()
	{
		(int removed, int added) = ImGuiWidgets.DiffViewState.Summarize(2, 3);

		Assert.AreEqual(2, removed);
		Assert.AreEqual(3, added);
	}

	[TestMethod]
	public void Summarize_OverTheCap_KeepsTheProportionsAndTheTotal()
	{
		(int removed, int added) = ImGuiWidgets.DiffViewState.Summarize(300, 100);

		Assert.AreEqual(10, removed + added, "The bar never grows past its cap however large the hunk is.");
		Assert.AreEqual(8, removed);
		Assert.AreEqual(2, added);
	}

	[TestMethod]
	public void Summarize_OneSideVastlyOutnumbered_StillShowsIt()
	{
		(int removed, int added) = ImGuiWidgets.DiffViewState.Summarize(999, 1);

		Assert.AreEqual(1, added, "A change that exists must not round away to nothing, or the bar lies about what is in the hunk.");
		Assert.AreEqual(9, removed);
	}

	[TestMethod]
	public void Summarize_OnlyRemovals_ShowsNoAdditions()
	{
		(int removed, int added) = ImGuiWidgets.DiffViewState.Summarize(4, 0);

		Assert.AreEqual(4, removed);
		Assert.AreEqual(0, added);
	}

	[TestMethod]
	public void Toggle_UnselectedHunk_SelectsIt()
	{
		HashSet<int> selection = [];

		Assert.IsTrue(ImGuiWidgets.DiffViewState.Toggle(selection, 2));
		Assert.IsTrue(selection.Contains(2));
	}

	[TestMethod]
	public void Toggle_SelectedHunk_DeselectsIt()
	{
		HashSet<int> selection = [2];

		Assert.IsTrue(ImGuiWidgets.DiffViewState.Toggle(selection, 2));
		Assert.IsFalse(selection.Contains(2));
	}

	[TestMethod]
	public void SelectAll_AlreadyComplete_ReportsNoChange()
	{
		HashSet<int> selection = [0, 1];

		Assert.IsFalse(
			ImGuiWidgets.DiffViewState.SelectAll(selection, 2),
			"Reporting a change when nothing changed would make a caller rebuild a patch every frame.");
	}

	[TestMethod]
	public void SelectAll_PartiallySelected_AddsTheRest()
	{
		HashSet<int> selection = [1];

		Assert.IsTrue(ImGuiWidgets.DiffViewState.SelectAll(selection, 3));
		Assert.AreSequenceEqual(FirstThreeIndices, selection.Order());
	}

	[TestMethod]
	public void SelectNone_EmptySelection_ReportsNoChange()
	{
		HashSet<int> selection = [];

		Assert.IsFalse(ImGuiWidgets.DiffViewState.SelectNone(selection));
	}

	[TestMethod]
	public void SelectedIndices_HoldsAnIndexPastTheEnd_IgnoresIt()
	{
		HashSet<int> selection = [0, 7];

		Assert.AreSequenceEqual(
			FirstIndexOnly,
			ImGuiWidgets.SelectedHunks(selection, hunkCount: 2),
			"A selection outlives the patch it was made against, so an index past the end is dropped rather than drawn.");
	}

	[TestMethod]
	public void Collapse_ToggledTwice_ReturnsToExpanded()
	{
		ImGuiWidgets.DiffViewState state = new();

		state.ToggleCollapsed(1);
		Assert.IsTrue(state.IsCollapsed(1));

		state.ToggleCollapsed(1);
		Assert.IsFalse(state.IsCollapsed(1));
	}
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet build tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj`
Expected: build failure, `DiffLine`, `DiffHunk`, `DiffLineKind`, `DiffRowPair` and `DiffViewState` do not exist.

- [ ] **Step 3: Create the model**

Create `ImGui.Widgets/DiffViewModel.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>What one line of a diff does.</summary>
	public enum DiffLineKind
	{
		/// <summary>Present on both sides, shown for context.</summary>
		Context,

		/// <summary>Present only after the change.</summary>
		Added,

		/// <summary>Present only before the change.</summary>
		Removed,
	}

	/// <summary>One line of a diff.</summary>
	/// <remarks>
	/// <see cref="Text"/> carries the content without the leading marker. The widget draws the marker
	/// in its own column, so a caller mapping from a source that keeps it strips it once rather than
	/// leaving the widget to guess whether a line beginning with a hyphen is a removal or content.
	/// </remarks>
	public sealed record DiffLine
	{
		/// <summary>Gets what this line does.</summary>
		public required DiffLineKind Kind { get; init; }

		/// <summary>Gets the line's content, without a leading marker.</summary>
		public required string Text { get; init; }

		/// <summary>Gets the line's number on the old side, or <see langword="null"/> when it has none.</summary>
		public int? OldNumber { get; init; }

		/// <summary>Gets the line's number on the new side, or <see langword="null"/> when it has none.</summary>
		public int? NewNumber { get; init; }
	}

	/// <summary>A run of lines forming one change, with the context around it.</summary>
	public sealed record DiffHunk
	{
		/// <summary>Gets the hunk's lines, in the order they are shown.</summary>
		public required IReadOnlyList<DiffLine> Lines { get; init; }

		/// <summary>Gets what the source names this hunk, which for git is the enclosing function.</summary>
		public string Heading { get; init; } = string.Empty;
	}

	/// <summary>The selected hunk indices that still name a hunk, ascending.</summary>
	/// <remarks>
	/// A selection outlives the diff it was made against. A caller that stages something and re-reads
	/// gets a different set of hunks, and an index that pointed at the third of five means nothing
	/// about the third of two. This is what a caller reads its selection back through, so a stale
	/// index is dropped rather than turned into a hunk that is not there.
	/// </remarks>
	/// <param name="selection">The selected hunk indices.</param>
	/// <param name="hunkCount">How many hunks there are.</param>
	/// <returns>The indices within range, ascending.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="selection"/> is <see langword="null"/>.</exception>
	public static IReadOnlyList<int> SelectedHunks(ISet<int> selection, int hunkCount)
	{
		Ensure.NotNull(selection);

		return [.. selection.Where(index => index >= 0 && index < hunkCount).Order()];
	}
}
```

- [ ] **Step 4: Create the state**

Create `ImGui.Widgets/DiffViewState.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>One row of a side-by-side view, holding whichever sides exist.</summary>
	/// <param name="Left">The old side's line, or <see langword="null"/> when this row is filler there.</param>
	/// <param name="Right">The new side's line, or <see langword="null"/> when this row is filler there.</param>
	internal readonly record struct DiffRowPair(DiffLine? Left, DiffLine? Right);

	/// <summary>
	/// The rules behind <see cref="DiffView(string, IReadOnlyList{DiffHunk}, ISet{int}, DiffViewOptions?)"/>:
	/// how a hunk lays out side by side, how its change summary is scaled, and what selecting does.
	/// </summary>
	/// <remarks>
	/// Deliberately free of ImGui, like <see cref="CurveTrackState"/>, which is what lets every rule
	/// here be tested without a graphics context. Only the collapse set is per widget. Everything
	/// else is static, because it depends on nothing but its arguments.
	/// </remarks>
	internal sealed class DiffViewState
	{
		/// <summary>The longest a change summary bar is allowed to be, in characters.</summary>
		internal const int SummaryWidth = 10;

		private readonly HashSet<int> collapsed = [];

		/// <summary>Whether a hunk is collapsed.</summary>
		/// <param name="index">The hunk's index.</param>
		/// <returns><see langword="true"/> when it is collapsed.</returns>
		internal bool IsCollapsed(int index) => collapsed.Contains(index);

		/// <summary>Collapses an expanded hunk, or expands a collapsed one.</summary>
		/// <param name="index">The hunk's index.</param>
		internal void ToggleCollapsed(int index)
		{
			if (!collapsed.Add(index))
			{
				_ = collapsed.Remove(index);
			}
		}

		/// <summary>
		/// Lays a hunk out as side-by-side rows, filling whichever side runs out first.
		/// </summary>
		/// <remarks>
		/// Removed and added lines are gathered as they are met and paired by index when the run ends,
		/// which is at a context line or at the end of the hunk. Pairing by index rather than by
		/// content is what keeps a change's two halves level with each other, and the filler rows are
		/// what stop the longer side from sliding the shorter one out of alignment.
		/// <para>
		/// A hunk carries both sides of its own change, so this needs nothing outside it. That is what
		/// makes a side-by-side view possible from a patch, which never carries the whole file.
		/// </para>
		/// </remarks>
		/// <param name="hunk">The hunk to lay out.</param>
		/// <returns>The rows, in order.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="hunk"/> is <see langword="null"/>.</exception>
		internal static IReadOnlyList<DiffRowPair> Pair(DiffHunk hunk)
		{
			Ensure.NotNull(hunk);

			List<DiffRowPair> rows = [];
			List<DiffLine> removed = [];
			List<DiffLine> added = [];

			foreach (DiffLine line in hunk.Lines)
			{
				switch (line.Kind)
				{
					case DiffLineKind.Removed:
						removed.Add(line);
						break;

					case DiffLineKind.Added:
						added.Add(line);
						break;

					case DiffLineKind.Context:
					default:
						FlushRun(rows, removed, added);
						rows.Add(new DiffRowPair(line, line));
						break;
				}
			}

			FlushRun(rows, removed, added);

			return rows;
		}

		/// <summary>Emits the gathered run as paired rows and clears it.</summary>
		/// <param name="rows">The rows built so far.</param>
		/// <param name="removed">The removed lines gathered since the last run ended.</param>
		/// <param name="added">The added lines gathered since the last run ended.</param>
		private static void FlushRun(List<DiffRowPair> rows, List<DiffLine> removed, List<DiffLine> added)
		{
			int height = Math.Max(removed.Count, added.Count);

			for (int index = 0; index < height; index++)
			{
				rows.Add(new DiffRowPair(
					index < removed.Count ? removed[index] : null,
					index < added.Count ? added[index] : null));
			}

			removed.Clear();
			added.Clear();
		}

		/// <summary>
		/// Scales a hunk's removed and added counts into the two halves of its summary bar.
		/// </summary>
		/// <remarks>
		/// The bar is proportional rather than absolute, so a hunk of four hundred changes and one of
		/// forty read the same shape at a glance, which is the whole point of showing a bar instead of
		/// two numbers. A side with any changes at all is never rounded away to nothing, because a bar
		/// that says a hunk only adds when it also removes is worse than no bar.
		/// </remarks>
		/// <param name="removedCount">How many lines the hunk removes.</param>
		/// <param name="addedCount">How many lines the hunk adds.</param>
		/// <returns>The two bar lengths, together no longer than <see cref="SummaryWidth"/>.</returns>
		internal static (int Removed, int Added) Summarize(int removedCount, int addedCount)
		{
			int total = removedCount + addedCount;

			if (total == 0)
			{
				return (0, 0);
			}

			int width = Math.Min(total, SummaryWidth);

			int removed = removedCount == 0
				? 0
				: Math.Max(1, (int)Math.Round((double)removedCount / total * width, MidpointRounding.AwayFromZero));

			int added = width - removed;

			// Both sides have changes but rounding gave the whole bar to one of them. Whichever side
			// lost is put back to a single character, which is the smallest honest thing the bar can
			// say about a change that exists.
			if (added == 0 && addedCount > 0)
			{
				added = 1;
				removed = width - 1;
			}

			return (removed, added);
		}

		/// <summary>Selects an unselected hunk, or deselects a selected one.</summary>
		/// <param name="selection">The selected hunk indices.</param>
		/// <param name="index">The hunk to toggle.</param>
		/// <returns><see langword="true"/> always, since a toggle is always a change.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="selection"/> is <see langword="null"/>.</exception>
		internal static bool Toggle(ISet<int> selection, int index)
		{
			Ensure.NotNull(selection);

			return selection.Add(index) || selection.Remove(index);
		}

		/// <summary>Selects every hunk.</summary>
		/// <param name="selection">The selected hunk indices.</param>
		/// <param name="hunkCount">How many hunks there are.</param>
		/// <returns><see langword="true"/> when the selection changed.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="selection"/> is <see langword="null"/>.</exception>
		internal static bool SelectAll(ISet<int> selection, int hunkCount)
		{
			Ensure.NotNull(selection);

			bool changed = false;

			for (int index = 0; index < hunkCount; index++)
			{
				changed |= selection.Add(index);
			}

			return changed;
		}

		/// <summary>Clears the selection.</summary>
		/// <param name="selection">The selected hunk indices.</param>
		/// <returns><see langword="true"/> when the selection changed.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="selection"/> is <see langword="null"/>.</exception>
		internal static bool SelectNone(ISet<int> selection)
		{
			Ensure.NotNull(selection);

			if (selection.Count == 0)
			{
				return false;
			}

			selection.Clear();

			return true;
		}

	}
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet build tests/ImGui.Widgets.Tests/ImGui.Widgets.Tests.csproj && ./tests/ImGui.Widgets.Tests/bin/Debug/net10.0/ktsu.ImGui.Widgets.Tests --filter "FullyQualifiedName~DiffViewStateTests"`
Expected: 20 passing.

- [ ] **Step 6: Run the full widget suite**

Run: `./tests/ImGui.Widgets.Tests/bin/Debug/net10.0/ktsu.ImGui.Widgets.Tests`
Expected: 303 passing (283 baseline plus 20), 0 warnings.

- [ ] **Step 7: Commit**

```bash
git add ImGui.Widgets/DiffViewModel.cs ImGui.Widgets/DiffViewState.cs tests/ImGui.Widgets.Tests/DiffViewStateTests.cs
git commit -m "[minor] Add the diff view's model and its rules

A neutral model of hunks and lines, so any source of a diff can be drawn
without this library knowing which one produced it, and the three rules
worth asserting: laying a hunk out side by side with filler where one side
runs out, scaling a change summary so a side with changes is never rounded
away, and selecting whole hunks.

No ImGui here at all, which is what lets every rule be tested rather than
looked at."
```

---

## Task 2: The unified view

**Files:**
- Create: `ImGui.Widgets/DiffView.cs`
- Create: `examples/ImGuiWidgetsDemo/DiffViewDemo.cs`
- Modify: `examples/ImGuiWidgetsDemo/ImGuiWidgetsDemo.cs`
- Modify: `ImGui.Widgets/README.md`

**Interfaces:**
- Consumes: the model and `DiffViewState` from Task 1.
- Produces:
  - `public enum DiffViewMode { Unified, SideBySide }`
  - `public sealed record DiffViewOptions { DiffViewMode Mode; bool CanShowLineNumbers; bool CanCollapseHunks; }`
  - `public static bool DiffView(string label, IReadOnlyList<DiffHunk> hunks, ISet<int> selection, DiffViewOptions? options = null)`
  - `public static void DiffView(string label, IReadOnlyList<DiffHunk> hunks, DiffViewOptions? options = null)`

`DiffViewMode.SideBySide` is declared here and drawn in Task 3. Until then it falls through to the unified layout rather than throwing, so a caller setting it early gets something readable instead of an exception.

- [ ] **Step 1: Create the widget**

Create `ImGui.Widgets/DiffView.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Styler;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>How a diff is laid out.</summary>
	public enum DiffViewMode
	{
		/// <summary>One column, in the order the lines appear, which is the shape a patch has.</summary>
		Unified,

		/// <summary>Two columns, the old side against the new, aligned with filler rows.</summary>
		SideBySide,
	}

	/// <summary>How a diff view is drawn.</summary>
	public sealed record DiffViewOptions
	{
		/// <summary>Gets the layout.</summary>
		public DiffViewMode Mode { get; init; } = DiffViewMode.Unified;

		/// <summary>Gets a value indicating whether the line number columns are drawn.</summary>
		public bool CanShowLineNumbers { get; init; } = true;

		/// <summary>Gets a value indicating whether clicking a hunk's header collapses it.</summary>
		public bool CanCollapseHunks { get; init; } = true;
	}

	/// <summary>
	/// Draws a diff with a checkbox on each hunk, and reports whether the selection changed.
	/// </summary>
	/// <remarks>
	/// The diff is drawn, never computed. Whoever produced it owns that, and mapping their result onto
	/// <see cref="DiffHunk"/> is what connects the two.
	/// <para>
	/// <paramref name="selection"/> is mutated in place and holds indices into <paramref name="hunks"/>.
	/// It is the caller's because it is the part that outlives the frame, which is what a caller hands
	/// on to stage what was ticked. An index no longer naming a hunk is ignored rather than tracked,
	/// so a caller re-reading a diff after staging clears the selection.
	/// </para>
	/// </remarks>
	/// <param name="label">The widget's ImGui id.</param>
	/// <param name="hunks">The hunks to draw.</param>
	/// <param name="selection">The selected hunk indices, mutated in place.</param>
	/// <param name="options">How to draw it, or <see langword="null"/> for the defaults.</param>
	/// <returns><see langword="true"/> when the selection changed this frame.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="hunks"/> or <paramref name="selection"/> is <see langword="null"/>.</exception>
	public static bool DiffView(
		string label,
		IReadOnlyList<DiffHunk> hunks,
		ISet<int> selection,
		DiffViewOptions? options = null)
	{
		Ensure.NotNull(hunks);
		Ensure.NotNull(selection);

		return DiffViewImpl.Draw(label, hunks, selection, options ?? new DiffViewOptions());
	}

	/// <summary>Draws a diff with nothing to select.</summary>
	/// <param name="label">The widget's ImGui id.</param>
	/// <param name="hunks">The hunks to draw.</param>
	/// <param name="options">How to draw it, or <see langword="null"/> for the defaults.</param>
	/// <exception cref="ArgumentNullException"><paramref name="hunks"/> is <see langword="null"/>.</exception>
	public static void DiffView(
		string label,
		IReadOnlyList<DiffHunk> hunks,
		DiffViewOptions? options = null)
	{
		Ensure.NotNull(hunks);

		_ = DiffViewImpl.Draw(label, hunks, selection: null, options ?? new DiffViewOptions());
	}

	internal static class DiffViewImpl
	{
		private static readonly Dictionary<uint, DiffViewState> States = [];

		/// <summary>How opaque a changed line's row tint is.</summary>
		/// <remarks>
		/// Low enough that the text stays the text's color rather than becoming a shade of the tint,
		/// which is the failure mode of drawing a diff over a colored row.
		/// </remarks>
		private const float ChangedRowAlpha = 0.22f;

		/// <summary>How opaque a context row's tint is.</summary>
		private const float ContextRowAlpha = 0.10f;

		public static bool Draw(
			string label,
			IReadOnlyList<DiffHunk> hunks,
			ISet<int>? selection,
			DiffViewOptions options)
		{
			if (hunks.Count == 0)
			{
				return false;
			}

			ImGui.PushID(label);

			uint id = ImGui.GetID(label);

			if (!States.TryGetValue(id, out DiffViewState? state))
			{
				state = new DiffViewState();
				States[id] = state;
			}

			bool changed = false;

			for (int index = 0; index < hunks.Count; index++)
			{
				changed |= DrawHunk(index, hunks[index], selection, options, state);
			}

			ImGui.PopID();

			return changed;
		}

		private static bool DrawHunk(
			int index,
			DiffHunk hunk,
			ISet<int>? selection,
			DiffViewOptions options,
			DiffViewState state)
		{
			ImGui.PushID(index);

			bool changed = false;

			try
			{
				if (selection is not null)
				{
					bool isSelected = selection.Contains(index);

					if (ImGui.Checkbox("##select", ref isSelected))
					{
						changed = DiffViewState.Toggle(selection, index);
					}

					ImGui.SameLine();
				}

				DrawSummary(hunk);
				ImGui.SameLine();

				if (DrawHeading(index, hunk, options, state))
				{
					state.ToggleCollapsed(index);
				}

				if (!state.IsCollapsed(index))
				{
					DrawLines(hunk, options);
				}
			}
			finally
			{
				ImGui.PopID();
			}

			return changed;
		}

		/// <summary>Draws the hunk's heading, which collapses it when the options allow.</summary>
		/// <returns><see langword="true"/> when the heading was clicked.</returns>
		private static bool DrawHeading(int index, DiffHunk hunk, DiffViewOptions options, DiffViewState state)
		{
			string heading = hunk.Heading.Length == 0
				? string.Create(CultureInfo.InvariantCulture, $"Hunk {index + 1}")
				: hunk.Heading;

			if (!options.CanCollapseHunks)
			{
				ImGui.TextUnformatted(heading);
				return false;
			}

			string marker = state.IsCollapsed(index) ? "▶" : "▼";

			// Selectable rather than a tree node, because the checkbox above has already been
			// submitted on this line and a tree node would take the whole row's width from it.
			return ImGui.Selectable($"{marker} {heading}");
		}

		/// <summary>Draws the proportional bar of removals against additions.</summary>
		private static void DrawSummary(DiffHunk hunk)
		{
			int removedCount = 0;
			int addedCount = 0;

			foreach (DiffLine line in hunk.Lines)
			{
				if (line.Kind == DiffLineKind.Removed)
				{
					removedCount++;
				}
				else if (line.Kind == DiffLineKind.Added)
				{
					addedCount++;
				}
			}

			(int removed, int added) = DiffViewState.Summarize(removedCount, addedCount);

			ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0.0f, 0.0f));

			try
			{
				if (removed > 0)
				{
					ImGui.PushStyleColor(ImGuiCol.Text, Palette.Semantic.Error.Value);
					ImGui.TextUnformatted(new string('-', removed));
					ImGui.PopStyleColor();
				}

				if (added > 0)
				{
					if (removed > 0)
					{
						ImGui.SameLine();
					}

					ImGui.PushStyleColor(ImGuiCol.Text, Palette.Semantic.Success.Value);
					ImGui.TextUnformatted(new string('+', added));
					ImGui.PopStyleColor();
				}
			}
			finally
			{
				ImGui.PopStyleVar();
			}
		}

		/// <summary>Draws a hunk's lines as a unified table.</summary>
		/// <remarks>
		/// Clipped rather than drawn whole, because a hunk of a few thousand lines is ordinary on a
		/// generated file and the cost of a frame should follow what is on screen.
		/// </remarks>
		private static void DrawLines(DiffHunk hunk, DiffViewOptions options)
		{
			int columns = options.CanShowLineNumbers ? 4 : 2;

			if (!ImGui.BeginTable("lines", columns, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.ScrollX))
			{
				return;
			}

			try
			{
				if (options.CanShowLineNumbers)
				{
					ImGui.TableSetupColumn("old", ImGuiTableColumnFlags.WidthFixed);
					ImGui.TableSetupColumn("new", ImGuiTableColumnFlags.WidthFixed);
				}

				ImGui.TableSetupColumn("marker", ImGuiTableColumnFlags.WidthFixed);
				ImGui.TableSetupColumn("text", ImGuiTableColumnFlags.WidthStretch);

				ImGuiListClipper clipper = default;
				clipper.Begin(hunk.Lines.Count);

				while (clipper.Step())
				{
					for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
					{
						DrawLine(hunk.Lines[row], options);
					}
				}

				clipper.End();
			}
			finally
			{
				ImGui.EndTable();
			}
		}

		private static void DrawLine(DiffLine line, DiffViewOptions options)
		{
			ImGui.TableNextRow();
			ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, TintFor(line.Kind));

			if (options.CanShowLineNumbers)
			{
				_ = ImGui.TableNextColumn();
				DrawNumber(line.OldNumber);

				_ = ImGui.TableNextColumn();
				DrawNumber(line.NewNumber);
			}

			_ = ImGui.TableNextColumn();
			ImGui.TextUnformatted(MarkerFor(line.Kind));

			_ = ImGui.TableNextColumn();
			ImGui.TextUnformatted(line.Text);
		}

		private static void DrawNumber(int? number)
		{
			if (number is int value)
			{
				ImGui.TextUnformatted(value.ToString(CultureInfo.InvariantCulture));
			}
		}

		internal static string MarkerFor(DiffLineKind kind) => kind switch
		{
			DiffLineKind.Added => "+",
			DiffLineKind.Removed => "-",
			_ => " ",
		};

		/// <summary>The row background for a line of this kind, resolved against the current theme.</summary>
		internal static uint TintFor(DiffLineKind kind)
		{
			Vector4 color = kind switch
			{
				DiffLineKind.Added => Palette.Semantic.Success.Value,
				DiffLineKind.Removed => Palette.Semantic.Error.Value,
				_ => ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg],
			};

			color.W = kind == DiffLineKind.Context ? ContextRowAlpha : ChangedRowAlpha;

			return ImGui.ColorConvertFloat4ToU32(color);
		}
	}
}
```

- [ ] **Step 2: Build and check the API compiles against the demo**

Run: `dotnet build ImGui.Widgets/ImGui.Widgets.csproj`
Expected: success, 0 warnings. The clipper is declared by value as `ImGuiListClipper clipper = default;`, which is what `VirtualTable.cs` does. This binding has no `ImGuiListClipperPtr`.

- [ ] **Step 3: Add the demo page**

Create `examples/ImGuiWidgetsDemo/DiffViewDemo.cs` with a fixture diff and two views, so the drawing can be looked at. Include the cases Review Focus names: a hunk that only adds, a hunk long enough that clipping matters, a line wider than the window, and two `DiffView` calls in one frame to prove they do not share collapsed state.

```csharp
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
```

- [ ] **Step 4: Call the demo page and register it**

In `examples/ImGuiWidgetsDemo/ImGuiWidgetsDemo.cs`, call `DiffViewDemo.Show();` from `ShowWidgetDemos`, which is the body of the "Widget Demos" tab, beside the other section calls and following the order the file already uses.

Then add `"Diff view"` to the `WidgetDemoSections` array in `tests/ImGuiWidgetsDemo.UITests/WidgetsDemoUITests.cs`. That array is hardcoded, so a section missing from it is never expanded by `EverySection_CanBeExpandedWithoutError`, which is the test that actually runs the widget's submission path.

- [ ] **Step 5: Run the headless UI tests**

Run: `dotnet build tests/ImGuiWidgetsDemo.UITests/ImGuiWidgetsDemo.UITests.csproj && ./tests/ImGuiWidgetsDemo.UITests/bin/Debug/net10.0/ktsu.examples.ImGuiWidgetsDemo.UITests`
Expected: 29 passing before your change and 29 after, with the new section now among those `EverySection_CanBeExpandedWithoutError` expands. It takes about two minutes.

This harness starts the demo headless, renders frames into a pixel buffer, and `AssertSectionDrewContent` proves a section drew something beyond its own header, so a widget that throws on submission or draws nothing fails here. It is a real gate, not a smoke test.

- [ ] **Step 6: Look at it**

Run: `dotnet run --project examples/ImGuiWidgetsDemo/ImGuiWidgetsDemo.csproj`

The harness proves the widget draws. It cannot say whether it looks right, and these are the things only a person can judge: the tints read against the theme rather than washing the text out, the checkboxes tick, collapsing a hunk in the first view leaves the second alone, the long line scrolls horizontally without pushing the checkbox off screen, and the 400-line hunk does not stutter.

If you cannot open a window from where you are running, say so in your report and leave this to the human rather than claiming it passed.

- [ ] **Step 7: Add the README entry**

In `ImGui.Widgets/README.md`, under **Display and Status**, matching the surrounding entries' voice:

```markdown
- **`DiffView`**: Draws a diff someone else computed, unified or side by side, with a checkbox per hunk and a proportional summary bar
```

- [ ] **Step 8: Run the suite and commit**

```bash
./tests/ImGui.Widgets.Tests/bin/Debug/net10.0/ktsu.ImGui.Widgets.Tests
git add ImGui.Widgets/DiffView.cs ImGui.Widgets/README.md examples/ImGuiWidgetsDemo/DiffViewDemo.cs examples/ImGuiWidgetsDemo/ImGuiWidgetsDemo.cs tests/ImGuiWidgetsDemo.UITests/WidgetsDemoUITests.cs
git commit -m "[minor] Draw a unified diff with selectable hunks

Each hunk carries a checkbox, a proportional bar of what it removes against
what it adds, and a heading that collapses it. The lines are clipped rather
than drawn whole, because a hunk of a few thousand lines is ordinary on a
generated file.

The tints come from the semantic palette rather than fixed colors, so a
theme moves them instead of fighting them, and the alpha stays low enough
that the text keeps its own color."
```

---

## Task 3: The side-by-side view

**Files:**
- Modify: `ImGui.Widgets/DiffView.cs`
- Modify: `examples/ImGuiWidgetsDemo/DiffViewDemo.cs`

**Interfaces:**
- Consumes: `DiffViewState.Pair` from Task 1, `DiffViewImpl` from Task 2.
- Produces: no new public surface. `DiffViewMode.SideBySide` starts drawing what it names.

- [ ] **Step 1: Split the line drawing by mode**

In `DiffViewImpl.DrawLines`, branch on `options.Mode` and add the side-by-side table beside the unified one. Both go through the same clipper, over `hunk.Lines.Count` for unified and over the paired row count for side by side:

```csharp
		private static void DrawLines(DiffHunk hunk, DiffViewOptions options)
		{
			if (options.Mode == DiffViewMode.SideBySide)
			{
				DrawPairedLines(hunk, options);
				return;
			}

			DrawUnifiedLines(hunk, options);
		}
```

- [ ] **Step 2: Draw the paired rows**

```csharp
		/// <summary>Draws a hunk as an old side against a new side, aligned by filler.</summary>
		/// <remarks>
		/// One table with both sides in it rather than two scrolling panes, so they stay level by
		/// construction. Two panes would need their scroll positions kept in step, which is a state
		/// machine that exists only to undo a layout choice.
		/// </remarks>
		private static void DrawPairedLines(DiffHunk hunk, DiffViewOptions options)
		{
			IReadOnlyList<DiffRowPair> rows = DiffViewState.Pair(hunk);

			if (rows.Count == 0)
			{
				return;
			}

			int perSide = options.CanShowLineNumbers ? 3 : 2;

			if (!ImGui.BeginTable("paired", perSide * 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.ScrollX | ImGuiTableFlags.BordersInnerV))
			{
				return;
			}

			try
			{
				SetUpSideColumns(options, "old");
				SetUpSideColumns(options, "new");

				ImGuiListClipper clipper = default;
				clipper.Begin(rows.Count);

				while (clipper.Step())
				{
					for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
					{
						DrawPairedRow(rows[row], options);
					}
				}

				clipper.End();
			}
			finally
			{
				ImGui.EndTable();
			}
		}

		private static void SetUpSideColumns(DiffViewOptions options, string side)
		{
			if (options.CanShowLineNumbers)
			{
				ImGui.TableSetupColumn($"{side}#", ImGuiTableColumnFlags.WidthFixed);
			}

			ImGui.TableSetupColumn($"{side}marker", ImGuiTableColumnFlags.WidthFixed);
			ImGui.TableSetupColumn($"{side}text", ImGuiTableColumnFlags.WidthStretch);
		}

		private static void DrawPairedRow(DiffRowPair pair, DiffViewOptions options)
		{
			ImGui.TableNextRow();

			DrawSide(pair.Left, isOldSide: true, options);
			DrawSide(pair.Right, isOldSide: false, options);
		}

		/// <summary>Draws one side of a paired row, or an empty tinted gap where it has no line.</summary>
		private static void DrawSide(DiffLine? line, bool isOldSide, DiffViewOptions options)
		{
			if (options.CanShowLineNumbers)
			{
				_ = ImGui.TableNextColumn();
				DrawNumber(isOldSide ? line?.OldNumber : line?.NewNumber);
			}

			_ = ImGui.TableNextColumn();

			if (line is null)
			{
				// Filler. Two more empty cells keep the row's shape, and the tint is fainter than a
				// real change so the gap does not read as content.
				ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, FillerTint());
				_ = ImGui.TableNextColumn();
				ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, FillerTint());
				return;
			}

			ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, TintFor(line.Kind));
			ImGui.TextUnformatted(MarkerFor(line.Kind));

			_ = ImGui.TableNextColumn();
			ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, TintFor(line.Kind));
			ImGui.TextUnformatted(line.Text);
		}

		/// <summary>The background for a row that exists on one side only.</summary>
		internal static uint FillerTint()
		{
			Vector4 color = ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg];
			color.W = ContextRowAlpha * 0.5f;

			return ImGui.ColorConvertFloat4ToU32(color);
		}
```

Rename the unified path's method to `DrawUnifiedLines` and leave its body as Task 2 wrote it.

- [ ] **Step 3: Build**

Run: `dotnet build ImGui.Widgets/ImGui.Widgets.csproj`
Expected: success, 0 warnings.

- [ ] **Step 4: Run the headless UI tests, then look at it**

Run: `./tests/ImGuiWidgetsDemo.UITests/bin/Debug/net10.0/ktsu.examples.ImGuiWidgetsDemo.UITests`
Expected: still 29 passing. The demo's mode toggle defaults to unified, so this proves the side-by-side code compiles and the section still draws, not that the paired layout is right.

Then run the demo and judge what the harness cannot: ticking "Side by side" reflows both views, the two sides stay level through the addition-only hunk which is entirely filler on the left, the filler reads as a gap rather than as content, and the 400-line hunk still scrolls smoothly. If you cannot open a window, say so rather than claiming it passed.

- [ ] **Step 5: Run the suite and commit**

```bash
./tests/ImGui.Widgets.Tests/bin/Debug/net10.0/ktsu.ImGui.Widgets.Tests
git add ImGui.Widgets/DiffView.cs examples/ImGuiWidgetsDemo/DiffViewDemo.cs
git commit -m "[minor] Draw a diff side by side as well as unified

The old side against the new, in one table rather than two panes, so they
stay level without a scroll position to keep in step. Where one side has
more lines than the other the shorter one takes a filler row, tinted more
faintly than a change so the gap does not read as content.

A hunk carries both sides of its own change, which is what makes this
possible from a patch rather than needing the whole file."
```

---

## Self-review notes

**Spec coverage.** The model and the three rules are Task 1, the unified view with its clipper and its palette-derived tints is Task 2, and the paired layout is Task 3. The demo page and the README entry are folded into Task 2, where the drawing they document first exists, and extended in Task 3. Nothing in the spec is unassigned.

**Review Focus coverage.** An empty hunk list returns early in Task 2 Step 1 and an empty hunk is Task 1's `Pair_EmptyHunk_IsNoRows`. A stale index is `SelectedIndices_HoldsAnIndexPastTheEnd_IgnoresIt`, against the public `SelectedHunks`, which is what a caller reads a selection back through. An addition-only hunk is covered twice, as a pairing test in Task 1 and as a fixture hunk in the demo. Two views in one frame and an over-wide line are both in the demo, which is the only place they can be judged.

**One thing the plan deliberately does not promise.** The exact `ImGuiListClipper` construction is written from the shape this binding usually takes, and Task 2 Step 2 says to follow `VirtualTable.cs` if it differs rather than to guess.

**The drawing is testable here, which is unusual.** `tests/ImGuiWidgetsDemo.UITests` starts the demo headless, renders into a pixel buffer, and asserts a section drew content beyond its own header, so a widget that throws or draws nothing fails a test rather than needing to be noticed. Both drawing tasks run it. What it cannot judge is whether the result looks right, so both tasks also end with a step that says to open the demo, and to say so plainly rather than claim it passed if that is not possible.

**Where a mistake will surface.** Task 3 is where a pairing error becomes visible, and Task 1 is where it is cheap to fix. That is why the rule is built and tested a task before anything draws it.
