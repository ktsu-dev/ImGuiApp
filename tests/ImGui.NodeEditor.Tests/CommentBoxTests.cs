// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor.Tests;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using ktsu.ImGui.NodeEditor;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Comment boxes in the engine: what they contain, and what moving one carries.
/// </summary>
/// <remarks>
/// Containment is geometric and decided when asked, so these tests set node sizes by hand the way
/// the renderer would after measuring them.
/// </remarks>
[TestClass]
public sealed class CommentBoxTests
{
	private readonly NodeEditorEngine engine = new();

	private Node NodeAt(Vector2 position, Vector2 size, string name = "Node")
	{
		Node node = engine.CreateNode(position, name, 0, 0);
		engine.UpdateNodeDimensions(node.Id, size);
		return engine.Nodes.Single(n => n.Id == node.Id);
	}

	[TestMethod]
	public void GetNodesInCommentBox_CountsOnlyNodesWhollyInside()
	{
		Node inside = NodeAt(new Vector2(20, 40), new Vector2(60, 30), "Inside");
		NodeAt(new Vector2(180, 40), new Vector2(60, 30), "Straddling");
		NodeAt(new Vector2(400, 400), new Vector2(60, 30), "Outside");

		CommentBox box = engine.CreateCommentBox(Vector2.Zero, new Vector2(200, 200), "Group");

		CollectionAssert.AreEqual(new[] { inside.Id }, engine.GetNodesInCommentBox(box.Id).ToList(), "A node half out of the box is not in it.");
	}

	[TestMethod]
	public void MoveCommentBox_CarriesItsNodesAndNestedBoxes_ButNotItsNeighbours()
	{
		Node inside = NodeAt(new Vector2(20, 40), new Vector2(60, 30));
		Node outside = NodeAt(new Vector2(400, 400), new Vector2(60, 30));
		CommentBox outer = engine.CreateCommentBox(Vector2.Zero, new Vector2(300, 300), "Outer");
		CommentBox nested = engine.CreateCommentBox(new Vector2(10, 10), new Vector2(120, 120), "Nested");

		IReadOnlyList<NodeMove> moves = engine.MoveCommentBox(outer.Id, new Vector2(50, -10));

		Assert.AreEqual(new Vector2(50, -10), engine.FindCommentBox(outer.Id)!.Position);
		Assert.AreEqual(new Vector2(60, 0), engine.FindCommentBox(nested.Id)!.Position, "A box inside the one being moved goes with it.");
		Assert.AreEqual(new Vector2(70, 30), engine.Nodes.Single(n => n.Id == inside.Id).Position);
		Assert.AreEqual(new Vector2(400, 400), engine.Nodes.Single(n => n.Id == outside.Id).Position);
		Assert.AreEqual(new NodeMove(inside.Id, new Vector2(20, 40), new Vector2(70, 30)), moves.Single(), "A node inside both boxes is moved once.");
	}

	[TestMethod]
	public void CreateCommentBoxAround_EnclosesTheNodes_WithRoomForTheTitle()
	{
		Node a = NodeAt(new Vector2(100, 100), new Vector2(80, 40));
		Node b = NodeAt(new Vector2(300, 220), new Vector2(60, 50));

		CommentBox box = engine.CreateCommentBoxAround([a.Id, b.Id], "Preprocessing")!;

		CollectionAssert.AreEquivalent(new[] { a.Id, b.Id }, engine.GetNodesInCommentBox(box.Id).ToList());
		Assert.AreEqual(100 - NodeEditorEngine.DefaultCommentBoxPadding - NodeEditorEngine.CommentBoxTitleAllowance, box.Position.Y, "The top edge should leave room for the title bar above the nodes.");
		Assert.IsNull(engine.CreateCommentBoxAround([999], "Nothing"), "No box around nothing.");
	}

	[TestMethod]
	public void ResizeCommentBox_NeverGoesBelowTheMinimum()
	{
		CommentBox box = engine.CreateCommentBox(Vector2.Zero, new Vector2(200, 200), "Group");

		engine.ResizeCommentBox(box.Id, new Vector2(1, 1));

		Assert.AreEqual(NodeEditorEngine.MinimumCommentBoxSize, engine.FindCommentBox(box.Id)!.Size);
	}

	[TestMethod]
	public void RemoveCommentBox_LeavesItsNodes_AndClearRemovesEveryBox()
	{
		NodeAt(new Vector2(20, 40), new Vector2(60, 30));
		CommentBox box = engine.CreateCommentBox(Vector2.Zero, new Vector2(200, 200), "Group");

		Assert.IsTrue(engine.RemoveCommentBox(box.Id));
		Assert.HasCount(1, engine.Nodes);
		Assert.IsFalse(engine.RemoveCommentBox(box.Id));

		engine.CreateCommentBox(Vector2.Zero, new Vector2(200, 200), "Again");
		engine.Clear();
		Assert.IsEmpty(engine.CommentBoxes);
		Assert.AreEqual(1, engine.CreateCommentBox(Vector2.Zero, Vector2.One, "First").Id, "Clear restarts comment box ids the way it restarts node ids.");
	}

	[TestMethod]
	public void RenameAndRecolour_ChangeOnlyWhatTheySay()
	{
		CommentBox box = engine.CreateCommentBox(new Vector2(5, 5), new Vector2(200, 100), "Old");

		engine.RenameCommentBox(box.Id, "New");
		engine.SetCommentBoxColor(box.Id, new Vector4(1, 0, 0, 0.3f));

		Assert.AreEqual(box with { Title = "New", Color = new Vector4(1, 0, 0, 0.3f) }, engine.FindCommentBox(box.Id));
	}
}
