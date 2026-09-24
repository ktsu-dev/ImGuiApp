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
