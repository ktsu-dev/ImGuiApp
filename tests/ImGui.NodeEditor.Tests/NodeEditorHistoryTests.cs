// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor.Tests;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using ktsu.ImGui.NodeEditor;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Undo and redo through <see cref="NodeEditorHistory"/>, against the engine alone.
/// </summary>
/// <remarks>
/// The claim under test is that undoing a step puts back exactly what the step changed and nothing
/// else, and that redoing it puts the step back the same way — ids, pins, links and values
/// included, since everything else a host keeps is keyed by those.
/// </remarks>
[TestClass]
public sealed class NodeEditorHistoryTests
{
	private static readonly string[] FreshNames = ["Fresh A", "Fresh B"];
	private static readonly string[] OriginalNames = ["Source", "Target"];

	private readonly NodeEditorEngine engine = new();

	private NodeEditorHistory NewHistory(AttributeBasedNodeFactory? factory = null) => new(engine, factory);

	/// <summary>A source and a target joined by one link, the smallest graph with something to lose.</summary>
	private (Node Source, Node Target, Link Link) Pair()
	{
		Node source = engine.CreateNodeFromSpecs(new Vector2(0, 0), "Source", [], [new PinSpec("Value", typeof(double))]);
		Node target = engine.CreateNodeFromSpecs(
			new Vector2(300, 0),
			"Target",
			[new PinSpec("In", typeof(double), 1.0), new PinSpec("Gain", typeof(double), 2.0)],
			[]);
		Link link = engine.TryCreateLink(source.OutputPins[0].Id, target.InputPins[0].Id).Link!;
		return (source, target, link);
	}

	[TestMethod]
	public void Undo_AfterCreatingANode_RemovesIt_AndRedoPutsItBackUnderTheSameId()
	{
		using NodeEditorHistory history = NewHistory();

		Node created = history.Record("Add node", () => engine.CreateNode(new Vector2(10, 20), "Made", 1, 1));
		Assert.IsTrue(history.CanUndo);
		Assert.AreEqual("Add node", history.NextUndoDescription);

		Assert.IsTrue(history.Undo());
		Assert.IsEmpty(engine.Nodes);

		Assert.IsTrue(history.Redo());
		Node back = engine.Nodes.Single();
		Assert.AreEqual(created.Id, back.Id, "Redo should restore the node under the id it was created with, since anything the host keys by id depends on it.");
		CollectionAssert.AreEqual(created.InputPins.Select(p => p.Id).ToList(), back.InputPins.Select(p => p.Id).ToList());
		Assert.AreEqual(new Vector2(10, 20), back.Position);
	}

	[TestMethod]
	public void Undo_AfterDeletingANode_RestoresItsLinksAndValues()
	{
		(Node _, Node target, Link link) = Pair();
		int gainPin = target.InputPins[1].Id;
		engine.SetPinValue(gainPin, 7.5);

		using NodeEditorHistory history = NewHistory();
		Assert.IsTrue(history.RemoveNode(target.Id));
		Assert.IsEmpty(engine.Links, "Removing the node removes the link reaching it.");

		Assert.IsTrue(history.Undo());

		Assert.IsTrue(engine.Nodes.Any(n => n.Id == target.Id));
		Assert.AreEqual(link, engine.Links.Single(), "The link should come back as it was, id and all.");
		Assert.AreEqual(7.5, engine.GetPinValue(gainPin), "A value set before the deletion should survive the undo.");
		Assert.IsTrue(engine.ResetPinValue(gainPin), "The pin's seeded default should survive the undo too.");
		Assert.AreEqual(2.0, engine.GetPinValue(gainPin));
	}

	[TestMethod]
	public void Undo_OfADeletion_DoesNotMoveNodesTheLayoutMovedSince()
	{
		(Node source, Node target, Link _) = Pair();
		using NodeEditorHistory history = NewHistory();
		history.RemoveNode(target.Id);

		// Something other than the history moves the surviving node, as the physics would.
		engine.UpdateNodePosition(source.Id, new Vector2(-500, 40));

		history.Undo();

		Assert.AreEqual(new Vector2(-500, 40), engine.Nodes.Single(n => n.Id == source.Id).Position, "Undoing a deletion is about the deleted node, not about where everything else stood at the time.");
		Assert.AreEqual(new Vector2(300, 0), engine.Nodes.Single(n => n.Id == target.Id).Position);
	}

	[TestMethod]
	public void Undo_AfterCreatingALink_RemovesIt_AndARefusedLinkRecordsNothing()
	{
		Node source = engine.CreateNode(Vector2.Zero, "Source", 0, 1);
		Node target = engine.CreateNode(new Vector2(300, 0), "Target", 1, 0);
		using NodeEditorHistory history = NewHistory();

		Assert.IsTrue(history.TryCreateLink(source.OutputPins[0].Id, target.InputPins[0].Id).Success);
		Assert.IsFalse(history.TryCreateLink(source.OutputPins[0].Id, target.InputPins[0].Id).Success, "The same pair twice is refused.");

		Assert.AreEqual(1, history.Service.CommandCount, "A refused link changed nothing, so it should not be a step.");

		history.Undo();
		Assert.IsEmpty(engine.Links);
		history.Redo();
		Assert.HasCount(1, engine.Links);
	}

	[TestMethod]
	public void Undo_AfterRemovingALink_PutsItBack()
	{
		(Node _, Node _, Link link) = Pair();
		using NodeEditorHistory history = NewHistory();

		Assert.IsTrue(history.RemoveLink(link.Id));
		history.Undo();

		Assert.AreEqual(link, engine.Links.Single());
	}

	[TestMethod]
	public void Undo_AfterDuplicating_RemovesTheCopies()
	{
		(Node source, Node target, Link _) = Pair();
		using NodeEditorHistory history = NewHistory();

		IReadOnlyList<Node> copies = history.DuplicateNodes([source.Id, target.Id], new Vector2(40, 40));
		Assert.HasCount(2, copies);
		Assert.HasCount(4, engine.Nodes);
		Assert.HasCount(2, engine.Links);

		history.Undo();
		Assert.HasCount(2, engine.Nodes);
		Assert.HasCount(1, engine.Links);

		history.Redo();
		Assert.HasCount(4, engine.Nodes);
		Assert.HasCount(2, engine.Links, "The copied link should come back with the copies.");
	}

	[TestMethod]
	public void PinValueWrites_AreRecordedWithoutBeingAsked()
	{
		(Node _, Node target, Link _) = Pair();
		int gain = target.InputPins[1].Id;
		using NodeEditorHistory history = NewHistory();

		engine.SetPinValue(gain, 3.0);
		engine.SetPinValue(gain, 4.0);

		Assert.AreEqual(2, history.Service.CommandCount, "Two writes with no gesture are two edits.");
		history.Undo();
		Assert.AreEqual(3.0, engine.GetPinValue(gain));
		history.Undo();
		Assert.AreEqual(2.0, engine.GetPinValue(gain));
		history.Redo();
		history.Redo();
		Assert.AreEqual(4.0, engine.GetPinValue(gain));
	}

	[TestMethod]
	public void PinValueWrites_SharingAGesture_UndoInOneStep()
	{
		(Node _, Node target, Link _) = Pair();
		int gain = target.InputPins[1].Id;
		using NodeEditorHistory history = NewHistory();

		// One drag of a slider: a write every frame, all under one gesture.
		for (int frame = 1; frame <= 30; frame++)
		{
			engine.SetPinValue(gain, 2.0 + (frame * 0.1), editGesture: 900);
		}

		Assert.AreEqual(1, history.Service.CommandCount, "A drag should be one step, not one per frame.");
		history.Undo();
		Assert.AreEqual(2.0, engine.GetPinValue(gain), "Undoing the drag should go back to where it started.");
		history.Redo();
		Assert.AreEqual(5.0, (double)engine.GetPinValue(gain)!, 1e-9);
	}

	[TestMethod]
	public void PinValueWrites_UnderDifferentGestures_StaySeparate()
	{
		(Node _, Node target, Link _) = Pair();
		int gain = target.InputPins[1].Id;
		using NodeEditorHistory history = NewHistory();

		engine.SetPinValue(gain, 3.0, editGesture: 1);
		engine.SetPinValue(gain, 4.0, editGesture: 2);

		Assert.AreEqual(2, history.Service.CommandCount, "Two separate drags of the same slider are two steps.");
	}

	[TestMethod]
	public void NewStep_AfterUndo_DiscardsTheRedo()
	{
		using NodeEditorHistory history = NewHistory();
		history.Record("A", () => engine.CreateNode(Vector2.Zero, "A", 0, 0));
		history.Undo();
		Assert.IsTrue(history.CanRedo);

		history.Record("B", () => engine.CreateNode(Vector2.Zero, "B", 0, 0));

		Assert.IsFalse(history.CanRedo);
		Assert.AreEqual("B", engine.Nodes.Single().Name);
	}

	[TestMethod]
	public void NodeMoves_UndoToWhereTheDragStarted()
	{
		(Node source, Node _, Link _) = Pair();
		using NodeEditorHistory history = NewHistory();

		// The drag has already happened by the time it is recorded.
		engine.UpdateNodePosition(source.Id, new Vector2(80, 90));
		Assert.IsTrue(history.RecordNodeMoves([new NodeMove(source.Id, Vector2.Zero, new Vector2(80, 90))]));
		Assert.IsFalse(history.RecordNodeMoves([new NodeMove(source.Id, new Vector2(80, 90), new Vector2(80, 90))]), "A move that goes nowhere is not a step.");

		history.Undo();
		Assert.AreEqual(Vector2.Zero, engine.Nodes.Single(n => n.Id == source.Id).Position);
		history.Redo();
		Assert.AreEqual(new Vector2(80, 90), engine.Nodes.Single(n => n.Id == source.Id).Position);
	}

	[TestMethod]
	public void Undo_OfAFactoryNodesDeletion_ReattachesItsInstance()
	{
		AttributeBasedNodeFactory factory = new(engine);
		factory.RegisterNodeType<PinValueBindingTests.TunableNode>();
		Node node = factory.CreateNode<PinValueBindingTests.TunableNode>(Vector2.Zero);
		Assert.IsTrue(factory.TryGetNodeInstance(node.Id, out object? before));
		int threshold = node.InputPins.Single(p => p.EffectiveDisplayName == "Threshold").Id;
		engine.SetPinValue(threshold, 42.0);

		using NodeEditorHistory history = NewHistory(factory);
		history.RemoveNode(node.Id);
		Assert.IsFalse(factory.TryGetNodeInstance(node.Id, out _));

		history.Undo();

		Assert.IsTrue(factory.TryGetNodeInstance(node.Id, out object? after), "The factory should know the node again once it is back.");
		Assert.AreSame(before, after, "It should be the same instance, not a fresh one.");
		Assert.IsTrue(engine.SetPinValue(threshold, 64.0));
		Assert.AreEqual(64.0, ((PinValueBindingTests.TunableNode)after).Threshold, "The restored pin should still write through to the instance.");
	}

	[TestMethod]
	public void Clear_OutsideARecording_EmptiesTheHistory()
	{
		using NodeEditorHistory history = NewHistory();
		history.Record("Add", () => engine.CreateNode(Vector2.Zero, "A", 0, 0));

		engine.Clear();

		Assert.IsFalse(history.CanUndo, "Clearing restarts the id counters, so every recorded id would name something else.");
	}

	[TestMethod]
	public void Clear_InsideARecording_IsUndoable_EvenThoughIdsAreReissued()
	{
		Pair();
		using NodeEditorHistory history = NewHistory();

		// The shape of a "reset" button: clear, then build something new that reuses ids 1 and 2.
		history.Record("Reset", () =>
		{
			engine.Clear();
			engine.CreateNode(new Vector2(5, 5), "Fresh A", 0, 0);
			engine.CreateNode(new Vector2(6, 6), "Fresh B", 0, 0);
		});

		CollectionAssert.AreEquivalent(FreshNames, engine.Nodes.Select(n => n.Name).ToList());

		history.Undo();
		CollectionAssert.AreEquivalent(OriginalNames, engine.Nodes.Select(n => n.Name).ToList());
		Assert.HasCount(1, engine.Links);

		history.Redo();
		CollectionAssert.AreEquivalent(FreshNames, engine.Nodes.Select(n => n.Name).ToList());
		Assert.IsEmpty(engine.Links);
	}

	[TestMethod]
	public void RestoredIds_AreNeverReissued()
	{
		using NodeEditorHistory history = NewHistory();
		Node first = history.Record("Add", () => engine.CreateNode(Vector2.Zero, "First", 1, 1));
		history.Undo();
		history.Redo();

		Node second = engine.CreateNode(Vector2.Zero, "Second", 1, 1);

		Assert.AreNotEqual(first.Id, second.Id);
		Assert.IsFalse(first.InputPins.Select(p => p.Id).Intersect(second.InputPins.Select(p => p.Id)).Any());
	}

	[TestMethod]
	public void CommentBoxes_CreateMoveAndRemove_AreUndoable()
	{
		Node inside = engine.CreateNode(new Vector2(20, 60), "Inside", 0, 0);
		engine.UpdateNodeDimensions(inside.Id, new Vector2(50, 30));
		using NodeEditorHistory history = NewHistory();

		CommentBox box = history.CreateCommentBox(Vector2.Zero, new Vector2(200, 200), "Group");
		history.MoveCommentBox(box.Id, new Vector2(100, 0));
		Assert.AreEqual(new Vector2(120, 60), engine.Nodes.Single().Position);

		history.Undo();
		Assert.AreEqual(Vector2.Zero, engine.FindCommentBox(box.Id)!.Position);
		Assert.AreEqual(new Vector2(20, 60), engine.Nodes.Single().Position, "The node the box carried should go back with it.");

		history.RemoveCommentBox(box.Id);
		Assert.IsEmpty(engine.CommentBoxes);
		Assert.HasCount(1, engine.Nodes, "Removing a comment box leaves what was in it.");

		history.Undo();
		Assert.AreEqual(box, engine.CommentBoxes.Single());
	}

	[TestMethod]
	public void Dispose_StopsRecording()
	{
		(Node _, Node target, Link _) = Pair();
		NodeEditorHistory history = NewHistory();
		history.Dispose();

		engine.SetPinValue(target.InputPins[1].Id, 9.0);

		Assert.AreEqual(0, history.Service.CommandCount);
	}
}
