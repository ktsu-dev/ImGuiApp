// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Collections.Generic;
using System.Linq;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;
using ktsu.ImGui.Probes;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.ReorderableTree"/> on its own, over a small tree the test owns.</summary>
/// <remarks>
/// The tree starts as
/// <code>
/// Photos     group
///   Beach
///   Forest
/// Notes
/// Archive    empty group
/// </code>
/// and every reported move is applied to it, the way a caller would, so the assertions read the tree
/// rather than the move.
/// </remarks>
[TestClass]
public sealed class ReorderableTreeTests : WidgetTest
{
	private const string Label = "layers";

	private List<TreeNode> roots = [];
	private string editing = string.Empty;
	private bool editNotes;
	private readonly List<ReorderableTreeMove> moves = [];
	private readonly List<string> clicked = [];

	[TestInitialize]
	public void SetUp()
	{
		roots =
		[
			new TreeNode("Photos", true, [new TreeNode("Beach", false, []), new TreeNode("Forest", false, [])]),
			new TreeNode("Notes", false, []),
			new TreeNode("Archive", true, []),
		];
		moves.Clear();
		clicked.Clear();
		editing = "Notes";
		editNotes = false;
	}

	private void Draw()
	{
		List<(TreeNode Node, TreeNode? Parent)> flat = [];
		Flatten(roots, null, flat);
		ReorderableTreeRow[] rows = [.. flat.Select(entry => new ReorderableTreeRow(Depth(entry.Node), entry.Node.IsGroup))];

		ReorderableTreeMove? move = ImGuiWidgets.ReorderableTree(Label, rows, i =>
		{
			if (editNotes && flat[i].Node.Name == "Notes")
			{
				ImGui.SetNextItemWidth(200f);
				ImGui.InputText("##edit", ref editing, 64u);
				ImGuiProbes.MarkItem("edit");
				return;
			}

			if (ImGui.Selectable(flat[i].Node.Name))
			{
				clicked.Add(flat[i].Node.Name);
			}

			ImGuiProbes.MarkItem(flat[i].Node.Name);
		});

		if (move is ReorderableTreeMove landed)
		{
			moves.Add(landed);
			Apply(flat, landed);
		}
	}

	private int Depth(TreeNode node)
	{
		int depth = 0;
		for (TreeNode? parent = ParentOf(roots, node); parent is not null; parent = ParentOf(roots, parent))
		{
			depth++;
		}

		return depth;
	}

	private static TreeNode? ParentOf(List<TreeNode> siblings, TreeNode node)
	{
		foreach (TreeNode sibling in siblings)
		{
			if (sibling.Children.Contains(node))
			{
				return sibling;
			}

			if (ParentOf(sibling.Children, node) is TreeNode found)
			{
				return found;
			}
		}

		return null;
	}

	private static void Flatten(List<TreeNode> siblings, TreeNode? parent, List<(TreeNode, TreeNode?)> into)
	{
		foreach (TreeNode node in siblings)
		{
			into.Add((node, parent));
			Flatten(node.Children, node, into);
		}
	}

	/// <summary>Applies a move as a caller whose list insert takes the final position would.</summary>
	private void Apply(List<(TreeNode Node, TreeNode? Parent)> flat, ReorderableTreeMove move)
	{
		(TreeNode node, TreeNode? oldParent) = flat[move.Row];
		List<TreeNode> from = oldParent?.Children ?? roots;
		List<TreeNode> to = move.ParentRow < 0 ? roots : flat[move.ParentRow].Node.Children;

		int oldIndex = from.IndexOf(node);
		int index = move.Index;

		// The index names a gap among the children before the move, so leaving a place above that
		// gap in the same list moves the gap up by one.
		if (ReferenceEquals(from, to) && oldIndex < index)
		{
			index--;
		}

		from.RemoveAt(oldIndex);
		to.Insert(index, node);
	}

	private string Shape()
	{
		List<(TreeNode Node, TreeNode? Parent)> flat = [];
		Flatten(roots, null, flat);
		return string.Join(" ", flat.Select(entry => new string('.', Depth(entry.Node)) + entry.Node.Name));
	}

	/// <summary>Drags from a fraction of the way down one row to a fraction of the way down another.</summary>
	private void DragRow(int fromRow, int toRow, float toFraction, float toX = 30f)
	{
		Rectangle from = RectOf($"{Label}/row{fromRow}");
		Rectangle to = RectOf($"{Label}/row{toRow}");
		Harness.Mouse.Drag(
			from.MinX + 30f,
			from.MinY + (from.Height / 2f),
			to.MinX + toX,
			to.MinY + (to.Height * toFraction),
			steps: 8,
			button: 0);
		Step(2);
	}

	[TestMethod]
	public void ReorderableTree_DrawsAndMarksEveryRow()
	{
		Start(Draw);

		for (int i = 0; i < 5; i++)
		{
			Assert.IsTrue(IsVisible($"{Label}/row{i}"), $"row {i} was not marked");
		}

		Assert.IsTrue(IsVisible($"{Label}/Beach"), "the row's own content was not drawn inside the tree's scope");
	}

	[TestMethod]
	public void ReorderableTree_IndentsChildrenByTheirDepth()
	{
		Start(Draw);

		float indent = RectOf($"{Label}/Beach").MinX - RectOf($"{Label}/Photos").MinX;

		Assert.AreEqual(ImGui.GetStyle().IndentSpacing, indent, 0.5f);
	}

	[TestMethod]
	public void ReorderableTree_DragIntoTheMiddleOfAGroupMovesTheRowInside()
	{
		Start(Draw);

		DragRow(fromRow: 3, toRow: 4, toFraction: 0.5f);

		Assert.AreEqual("Photos .Beach .Forest Archive .Notes", Shape());
	}

	[TestMethod]
	public void ReorderableTree_DragAboveTheFirstRowMovesTheRowToTheTop()
	{
		Start(Draw);

		DragRow(fromRow: 3, toRow: 0, toFraction: 0.1f);

		Assert.AreEqual("Notes Photos .Beach .Forest Archive", Shape());
	}

	[TestMethod]
	public void ReorderableTree_DragOutOfAGroupAtTheLeftLeavesIt()
	{
		Start(Draw);

		// Below Forest, the last child of Photos, with the pointer at depth 0: after Photos.
		DragRow(fromRow: 1, toRow: 2, toFraction: 0.9f, toX: 2f);

		Assert.AreEqual("Photos .Forest Beach Notes Archive", Shape());
	}

	[TestMethod]
	public void ReorderableTree_DragDownWithinAGroupReportsTheGapBeforeTheMove()
	{
		Start(Draw);

		DragRow(fromRow: 1, toRow: 2, toFraction: 0.9f, toX: ImGui.GetStyle().IndentSpacing + 10f);

		Assert.HasCount(1, moves);
		Assert.AreEqual(new ReorderableTreeMove(1, 0, 2), moves[0]);
		Assert.AreEqual("Photos .Forest .Beach Notes Archive", Shape());
	}

	[TestMethod]
	public void ReorderableTree_DragOntoItsOwnChildDoesNothing()
	{
		Start(Draw);

		DragRow(fromRow: 0, toRow: 1, toFraction: 0.5f);

		Assert.IsEmpty(moves);
		Assert.AreEqual("Photos .Beach .Forest Notes Archive", Shape());
	}

	[TestMethod]
	public void ReorderableTree_AClickWithoutMovingIsLeftToTheRow()
	{
		Start(Draw);

		Click($"{Label}/Notes");

		Assert.IsEmpty(moves);
		Assert.HasCount(1, clicked);
		Assert.AreEqual("Notes", clicked[0]);
	}

	[TestMethod]
	public void ReorderableTree_ShowsWhereTheDragWouldLandWhileHeld()
	{
		Start(Draw);

		Rectangle notes = RectOf($"{Label}/row3");
		Rectangle archive = RectOf($"{Label}/row4");

		Harness.Mouse.MoveTo(notes.MinX + 30f, notes.MinY + (notes.Height / 2f));
		Step();
		HarnessMouse.Down(0);
		Step();
		Harness.Mouse.MoveTo(notes.MinX + 30f, notes.MinY + (notes.Height / 2f) + 3f);
		Step();
		byte[] held = Snapshot();

		Harness.Mouse.MoveTo(archive.MinX + 30f, archive.MinY + (archive.Height / 2f));
		Step(2);

		Assert.IsGreaterThan(0, PixelsChangedSince(held), "nothing was drawn to show the drop into Archive");
		Assert.IsEmpty(moves, "a move was reported before the button was released");

		HarnessMouse.Up(0);
		Step(2);

		Assert.HasCount(1, moves);
	}

	[TestMethod]
	public void ReorderableTree_DraggingInATextFieldSelectsTextRatherThanMovingTheRow()
	{
		editNotes = true;
		Start(Draw);

		Rectangle field = RectOf($"{Label}/edit");
		Rectangle archive = RectOf($"{Label}/row4");
		Harness.Mouse.Drag(
			field.MinX + 4f,
			field.MinY + (field.Height / 2f),
			archive.MinX + 30f,
			archive.MinY + (archive.Height / 2f),
			steps: 8,
			button: 0);
		Step(2);

		Assert.IsEmpty(moves);
		Assert.AreEqual("Photos .Beach .Forest Notes Archive", Shape());
	}

	[TestMethod]
	public void ReorderableTree_ReleasingOutsideAnyDropReportsNothing()
	{
		Start(Draw);

		// Onto Notes itself: a drop that would leave it where it is.
		DragRow(fromRow: 3, toRow: 3, toFraction: 0.9f);

		Assert.IsEmpty(moves);
	}

	private sealed record TreeNode(string Name, bool IsGroup, List<TreeNode> Children);
}
