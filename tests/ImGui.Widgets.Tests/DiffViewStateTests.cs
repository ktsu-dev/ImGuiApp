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
	public void SelectAllHunks_AlreadyComplete_ReportsNoChange()
	{
		HashSet<int> selection = [0, 1];

		Assert.IsFalse(
			ImGuiWidgets.SelectAllHunks(selection, 2),
			"Reporting a change when nothing changed would make a caller rebuild a patch every frame.");
	}

	[TestMethod]
	public void SelectAllHunks_PartiallySelected_AddsTheRest()
	{
		HashSet<int> selection = [1];

		Assert.IsTrue(ImGuiWidgets.SelectAllHunks(selection, 3));
		Assert.AreSequenceEqual(FirstThreeIndices, selection.Order());
	}

	[TestMethod]
	public void ClearHunkSelection_EmptySelection_ReportsNoChange()
	{
		HashSet<int> selection = [];

		Assert.IsFalse(ImGuiWidgets.ClearHunkSelection(selection));
	}

	[TestMethod]
	public void ClearHunkSelection_HoldsASelection_EmptiesItAndReportsTheChange()
	{
		HashSet<int> selection = [0, 3];

		Assert.IsTrue(ImGuiWidgets.ClearHunkSelection(selection));
		Assert.IsEmpty(selection);
	}

	[TestMethod]
	public void SelectedHunkIndices_HoldsAnIndexPastTheEnd_IgnoresIt()
	{
		HashSet<int> selection = [0, 7];

		Assert.AreSequenceEqual(
			FirstIndexOnly,
			ImGuiWidgets.SelectedHunkIndices(selection, hunkCount: 2),
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

	[TestMethod]
	public void Pair_MixedHunk_EmitsEveryLineOnceOnTheSideItBelongsTo()
	{
		// The property the seam between the two views rests on: the side-by-side layout is a
		// rearrangement of the hunk, never a filter of it, so nothing a unified view would show can
		// go missing or be doubled when the same hunk is drawn paired.
		ImGuiWidgets.DiffHunk hunk = Hunk(
			Context("c1"),
			Removed("r1"),
			Removed("r2"),
			Added("a1"),
			Context("c2"),
			Added("a2"),
			Added("a3"),
			Removed("r3"),
			Context("c3"));

		IReadOnlyList<ImGuiWidgets.DiffRowPair> rows = ImGuiWidgets.DiffViewState.Pair(hunk);

		Assert.AreSequenceEqual(
			hunk.Lines.Where(line => line.Kind != ImGuiWidgets.DiffLineKind.Added).Select(line => line.Text),
			rows.Where(row => row.Left is not null).Select(row => row.Left!.Text),
			"The old side carries every context and removed line, in order and once each.");

		Assert.AreSequenceEqual(
			hunk.Lines.Where(line => line.Kind != ImGuiWidgets.DiffLineKind.Removed).Select(line => line.Text),
			rows.Where(row => row.Right is not null).Select(row => row.Right!.Text),
			"The new side carries every context and added line, in order and once each.");
	}

	[TestMethod]
	public void RowsFor_TheSameHunkTwice_LaysItOutOnce()
	{
		ImGuiWidgets.DiffViewState state = new();
		ImGuiWidgets.DiffHunk hunk = Hunk(Removed("x"), Added("X"));

		Assert.AreSame(
			state.RowsFor(0, hunk),
			state.RowsFor(0, hunk),
			"Laying the same hunk out again every frame is the cost the clipper exists to avoid.");
	}

	[TestMethod]
	public void RowsFor_ADifferentHunkAtTheSamePosition_LaysItOutAgain()
	{
		ImGuiWidgets.DiffViewState state = new();
		IReadOnlyList<ImGuiWidgets.DiffRowPair> first = state.RowsFor(0, Hunk(Added("X")));
		IReadOnlyList<ImGuiWidgets.DiffRowPair> second = state.RowsFor(0, Hunk(Added("X"), Added("Y")));

		Assert.AreEqual(1, first.Count);
		Assert.AreEqual(2, second.Count, "A new hunk instance is a new diff, so its layout is built again.");
	}

	[TestMethod]
	public void ChangeCounts_MixedHunk_CountsEachKindOnItsOwn()
	{
		ImGuiWidgets.DiffViewState state = new();

		(int removed, int added) = state.ChangeCounts(0, Hunk(Context("c"), Removed("x"), Added("X"), Added("Y")));

		Assert.AreEqual(1, removed);
		Assert.AreEqual(2, added, "Context lines count as neither, which is what keeps the summary bar honest.");
	}

	[TestMethod]
	public void MarkerFor_EachKind_IsTheGlyphAPatchUses()
	{
		Assert.AreEqual("+", ImGuiWidgets.DiffViewImpl.MarkerFor(ImGuiWidgets.DiffLineKind.Added));
		Assert.AreEqual("-", ImGuiWidgets.DiffViewImpl.MarkerFor(ImGuiWidgets.DiffLineKind.Removed));
		Assert.AreEqual(
			" ",
			ImGuiWidgets.DiffViewImpl.MarkerFor(ImGuiWidgets.DiffLineKind.Context),
			"A context line keeps the marker column's width without claiming the line changed.");
	}

	[TestMethod]
	public void HunkName_IsTheBracketedIndexTheLibraryProbesWith()
	{
		Assert.AreEqual("##diff/[2]", ImGuiWidgets.DiffViewImpl.HunkName("##diff", 2));
	}
}
