// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests where a drag in ReorderableTree lands. All pure — no ImGui context required.
/// </summary>
/// <remarks>
/// The fixture is laid out as the widget lays rows out: each row 20 pixels tall, 4 pixels of item
/// spacing between rows, and 20 pixels of indent per level, starting at x = 0.
/// <code>
/// 0  A        group, expanded
/// 1    a1     leaf
/// 2    a2     leaf
/// 3  B        leaf
/// 4  C        group, collapsed
/// </code>
/// </remarks>
[TestClass]
public class ReorderableTreeStateTests
{
	private const float RowHeight = 20f;
	private const float Spacing = 4f;
	private const float Indent = 20f;

	private const int A = 0;
	private const int A1 = 1;
	private const int A2 = 2;
	private const int B = 3;
	private const int C = 4;

	private static readonly ReorderableTreeRow[] Rows =
	[
		new(0, true),
		new(1, false),
		new(1, false),
		new(0, false),
		new(0, true),
	];

	private static readonly int[] ExpectedParents = [-1, A, A, -1, -1];
	private static readonly int[] ExpectedSiblings = [0, 0, 1, 1, 2];
	private static readonly int[] ExpectedSubtreeEnds = [3, 2, 3, 4, 5];

	private static List<ImGuiWidgets.ReorderableTreeRowSpan> SpansFor(int count) =>
		[.. Enumerable.Range(0, count).Select(i => new ImGuiWidgets.ReorderableTreeRowSpan(i * (RowHeight + Spacing), (i * (RowHeight + Spacing)) + RowHeight))];

	/// <summary>A point a given fraction of the way down a row, at a given depth of indent.</summary>
	private static Vector2 At(int row, float fraction, int depth = 0) =>
		new((depth * Indent) + 5f, (row * (RowHeight + Spacing)) + (fraction * RowHeight));

	private static ImGuiWidgets.ReorderableTreeDrop? Drop(int dragged, Vector2 pointer) =>
		ImGuiWidgets.ReorderableTreeState.Resolve(Rows, SpansFor(Rows.Length), 0f, Indent, dragged, pointer);

	private static void AssertDrop(ImGuiWidgets.ReorderableTreeDrop? drop, int parent, int index)
	{
		Assert.IsNotNull(drop);
		Assert.AreEqual(parent, drop.Value.ParentRow, "parent");
		Assert.AreEqual(index, drop.Value.Index, "index");
	}

	[TestMethod]
	public void Analyze_RecoversParentsSiblingsAndSubtrees()
	{
		ImGuiWidgets.ReorderableTreeState.Structure tree = ImGuiWidgets.ReorderableTreeState.Analyze(Rows);

		CollectionAssert.AreEqual(ExpectedParents, tree.Parents);
		CollectionAssert.AreEqual(ExpectedSiblings, tree.SiblingIndices);
		CollectionAssert.AreEqual(ExpectedSubtreeEnds, tree.SubtreeEnds);
	}

	[TestMethod]
	public void Analyze_RefusesRowsThatAreNotAFlattenedTree()
	{
		Assert.ThrowsExactly<ArgumentException>(() => ImGuiWidgets.ReorderableTreeState.Analyze([new(1, false)]));
		Assert.ThrowsExactly<ArgumentException>(() => ImGuiWidgets.ReorderableTreeState.Analyze([new(0, true), new(2, false)]));
		Assert.ThrowsExactly<ArgumentException>(() => ImGuiWidgets.ReorderableTreeState.Analyze([new(0, true), new(-1, false)]));
	}

	[TestMethod]
	public void Analyze_AcceptsNoRows()
	{
		ImGuiWidgets.ReorderableTreeState.Structure tree = ImGuiWidgets.ReorderableTreeState.Analyze([]);

		Assert.IsEmpty(tree.Parents);
	}

	[TestMethod]
	public void Leaf_TopHalfDropsAboveIt() => AssertDrop(Drop(B, At(A1, 0.4f, 1)), A, 0);

	[TestMethod]
	public void Leaf_BottomHalfDropsBelowIt() => AssertDrop(Drop(B, At(A1, 0.6f, 1)), A, 1);

	[TestMethod]
	public void Container_TopQuarterDropsAboveIt() => AssertDrop(Drop(A1, At(C, 0.2f)), -1, 2);

	[TestMethod]
	public void Container_MiddleHalfDropsInsideIt()
	{
		ImGuiWidgets.ReorderableTreeDrop? drop = Drop(B, At(C, 0.5f));

		AssertDrop(drop, C, 0);
		Assert.AreEqual(ImGuiWidgets.ReorderableTreeDropKind.Inside, drop!.Value.Kind);
	}

	[TestMethod]
	public void Container_MiddleHalfStartsAndEndsAtTheQuarters()
	{
		AssertDrop(Drop(B, At(C, 0.25f)), C, 0);
		AssertDrop(Drop(B, At(C, 0.74f)), C, 0);
		AssertDrop(Drop(A1, At(C, 0.75f)), -1, 3);
	}

	[TestMethod]
	public void CollapsedContainer_BottomQuarterDropsBelowIt() => AssertDrop(Drop(A1, At(C, 0.9f)), -1, 3);

	[TestMethod]
	public void ExpandedContainer_BottomQuarterDropsAboveItsFirstChild()
	{
		// The gap below an expanded group's row is the gap above its first child, so that is where the
		// line is drawn and where the drop lands.
		ImGuiWidgets.ReorderableTreeDrop? drop = Drop(B, At(A, 0.9f));

		AssertDrop(drop, A, 0);
		Assert.AreEqual(1, drop!.Value.LineDepth);
	}

	[TestMethod]
	public void EndOfSubtree_PointerIndentedStaysInside() => AssertDrop(Drop(C, At(A2, 0.9f, depth: 1)), A, 2);

	[TestMethod]
	public void EndOfSubtree_PointerAtTheLeftDropsAfterTheAncestor()
	{
		ImGuiWidgets.ReorderableTreeDrop? drop = Drop(C, At(A2, 0.9f, depth: 0));

		AssertDrop(drop, -1, 1);
		Assert.AreEqual(0, drop!.Value.LineDepth);
	}

	[TestMethod]
	public void EndOfSubtree_PointerFurtherInThanTheRowIsClampedToIt() => AssertDrop(Drop(C, At(A2, 0.9f, depth: 5)), A, 2);

	[TestMethod]
	public void MidSubtree_PointerAtTheLeftCannotLeaveIt()
	{
		// Below a1 the next row is a2, a sibling, so there is only one gap there however far left the
		// pointer is.
		AssertDrop(Drop(C, At(A1, 0.9f, depth: 0)), A, 1);
	}

	[TestMethod]
	public void AboveTheFirstRow_DropsAboveIt() => AssertDrop(Drop(C, new Vector2(5f, -30f)), -1, 0);

	[TestMethod]
	public void BelowTheLastRow_DropsBelowIt() => AssertDrop(Drop(A1, new Vector2(5f, 500f)), -1, 3);

	[TestMethod]
	public void TheGapBetweenRows_BelongsBelowTheRowAbove()
	{
		// Between A's bottom (20) and a1's top (24). Below A, which is expanded, is a1's gap.
		AssertDrop(Drop(B, new Vector2(25f, 22f)), A, 0);
	}

	[TestMethod]
	public void OntoTheDraggedRowItself_IsRefused()
	{
		Assert.IsNull(Drop(A, At(A, 0.5f)));
		Assert.IsNull(Drop(B, At(B, 0.1f)));
	}

	[TestMethod]
	public void IntoTheDraggedRowsSubtree_IsRefused()
	{
		Assert.IsNull(Drop(A, At(A1, 0.5f, 1)));
		Assert.IsNull(Drop(A, At(A2, 0.9f, 0)));
	}

	[TestMethod]
	public void ADropThatChangesNothing_IsRefused()
	{
		// Directly above B, from below a2 at the left: B's own gap.
		Assert.IsNull(Drop(B, At(A2, 0.9f, depth: 0)));

		// Directly below B, from the top of C: B's other gap.
		Assert.IsNull(Drop(B, At(C, 0.1f)));
	}

	[TestMethod]
	public void TheIndexCountsTheChildrenBeforeTheMove()
	{
		// a1 is the first of A's two children. Dropped below a2 it ends up second, at position 1 once
		// it has left position 0 — but the gap it was dropped in is the one after both, which is 2.
		// This is the convention ReorderableTreeMove.Index documents, and the one a caller moving down
		// within a parent has to adjust for if its move takes the final position.
		AssertDrop(Drop(A1, At(A2, 0.9f, depth: 1)), A, 2);
	}

	[TestMethod]
	public void MovingUpWithinAParent_NeedsNoAdjustment() => AssertDrop(Drop(A2, At(A1, 0.1f, depth: 1)), A, 0);

	[TestMethod]
	public void ARowCanBeDroppedIntoAContainerAboveIt() => AssertDrop(Drop(C, At(A, 0.5f)), A, 0);

	[TestMethod]
	public void AnUnknownDraggedRow_DropsNowhere()
	{
		Assert.IsNull(Drop(-1, At(B, 0.5f)));
		Assert.IsNull(Drop(Rows.Length, At(B, 0.5f)));
	}

	[TestMethod]
	public void State_APressBecomesADragOnlyWhenAsked()
	{
		ImGuiWidgets.ReorderableTreeState state = new();

		state.Press(B);
		Assert.AreEqual(B, state.PressedRow);
		Assert.IsFalse(state.IsDragging);

		Assert.IsTrue(state.BeginDrag());
		Assert.AreEqual(B, state.DraggedRow);
		Assert.IsFalse(state.BeginDrag(), "a drag already under way does not start again");

		state.Release();
		Assert.AreEqual(-1, state.PressedRow);
		Assert.IsFalse(state.IsDragging);
	}

	[TestMethod]
	public void State_NoPressMeansNoDrag()
	{
		ImGuiWidgets.ReorderableTreeState state = new();

		Assert.IsFalse(state.BeginDrag());
		Assert.IsFalse(state.IsDragging);
	}
}
