// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor.Tests;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.NodeEditor;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Comment boxes under the two view operations that shift the whole graph: panning and
/// <see cref="NodeEditorRenderer.FitToView"/>.
/// </summary>
/// <remarks>
/// Both move every node in the engine's space, so a box that stays where it was loses its nodes:
/// they slide out of it on screen and <see cref="NodeEditorEngine.GetNodesInCommentBox"/> stops
/// finding them. These run real frames, because a pan only exists as ImNodes' response to the mouse.
/// </remarks>
[TestClass]
public sealed class CommentBoxViewTests
{
	private static readonly HarnessOptions Viewport = new() { Width = 900, Height = 700 };

	private readonly NodeEditorEngine engine = new();
	private readonly NodeEditorRenderer renderer = new();

	private ImGuiAppHarness harness = null!;

	[TestCleanup]
	public void TearDown() => harness?.Dispose();

	private void Start()
	{
		harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Title = nameof(CommentBoxViewTests),
				OnRender = _ => DrawGraph(),
				SaveIniSettings = false,
			},
			Viewport);

		harness.Step(3);
	}

	private void DrawGraph()
	{
		renderer.Render(engine, ImGui.GetContentRegionAvail());

		foreach (KeyValuePair<int, Vector2> update in renderer.GetNodePositionUpdates(engine))
		{
			engine.UpdateNodePosition(update.Key, update.Value);
		}

		foreach (KeyValuePair<int, Vector2> update in renderer.GetNodeDimensionUpdates(engine))
		{
			engine.UpdateNodeDimensions(update.Key, update.Value);
		}
	}

	/// <summary>A drawn node at (200, 200) with a box made around it once its size is known.</summary>
	private (Node Node, CommentBox Box) NodeInABox()
	{
		Node node = engine.CreateNode(new Vector2(200, 200), "Grouped", ["In"], ["Out"]);
		Start();

		CommentBox box = engine.CreateCommentBoxAround([node.Id], "Group")!;
		harness.Step(2);

		CollectionAssert.AreEqual(new[] { node.Id }, engine.GetNodesInCommentBox(box.Id).ToList(), "The box should hold its node before the view moves.");
		return (node, box);
	}

	private Vector2 NodePosition(int nodeId) => engine.Nodes.Single(n => n.Id == nodeId).Position;

	private Vector2 BoxPosition(int boxId) => engine.FindCommentBox(boxId)!.Position;

	[TestMethod]
	public void Panning_CarriesCommentBoxesWithTheirNodes()
	{
		(Node node, CommentBox box) = NodeInABox();
		Vector2 nodeBefore = NodePosition(node.Id);
		Vector2 offsetBefore = nodeBefore - BoxPosition(box.Id);

		// A middle-button drag across empty canvas pans the editor.
		harness.Mouse.Drag(700, 600, 600, 500, button: 2);
		harness.Step(2);

		Assert.AreNotEqual(nodeBefore, NodePosition(node.Id), "The drag should have panned the view, moving the node.");
		Assert.AreEqual(offsetBefore, NodePosition(node.Id) - BoxPosition(box.Id), "The node should stay where it was within its box.");
		CollectionAssert.AreEqual(new[] { node.Id }, engine.GetNodesInCommentBox(box.Id).ToList(), "The box should still hold its node after a pan.");
	}

	[TestMethod]
	public void FitToView_CarriesCommentBoxesWithTheirNodes()
	{
		(Node node, CommentBox box) = NodeInABox();
		Vector2 nodeBefore = NodePosition(node.Id);
		Vector2 offsetBefore = nodeBefore - BoxPosition(box.Id);

		Assert.IsTrue(renderer.FitToView(engine, new Vector2(900, 700)));
		harness.Step(2);

		Assert.AreNotEqual(nodeBefore, NodePosition(node.Id), "Fitting should have moved the node to the centre.");
		Assert.AreEqual(offsetBefore, NodePosition(node.Id) - BoxPosition(box.Id), "The node should stay where it was within its box.");
		CollectionAssert.AreEqual(new[] { node.Id }, engine.GetNodesInCommentBox(box.Id).ToList(), "The box should still hold its node after fitting.");
	}

	[TestMethod]
	public void FitToView_CentresTheBoxesAsWellAsTheNodes()
	{
		(_, CommentBox box) = NodeInABox();

		Assert.IsTrue(renderer.FitToView(engine, new Vector2(900, 700)));

		CommentBox fitted = engine.FindCommentBox(box.Id)!;
		Vector2 centre = (fitted.Position + fitted.Max) * 0.5f;
		Assert.AreEqual(450f, centre.X, 0.01f, "The box encloses the node, so the arrangement's centre is the box's.");
		Assert.AreEqual(350f, centre.Y, 0.01f, "The box encloses the node, so the arrangement's centre is the box's.");
	}
}
