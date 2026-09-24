// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor.Tests;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;
using Hexa.NET.ImNodes;

using ktsu.ForceDirectedLayout;
using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.NodeEditor;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives the parts of the editor that need a live ImNodes context: the input handler, the
/// renderer's measurement feedback, and the debug overlays. Everything runs headlessly through the
/// harness, so there is no window and no GPU.
/// </summary>
[TestClass]
public sealed class NodeEditorInteractionTests
{
	private static readonly HarnessOptions Viewport = new() { Width = 900, Height = 700 };

	private readonly NodeEditorEngine engine = new();
	private readonly NodeEditorRenderer renderer = new();
	private readonly NodeEditorInputHandler input = new();

	private ImGuiAppHarness harness = null!;
	private InputEvents? lastEvents;
	private bool drawDebugOverlays;
	private int? linkToSelect;
	private readonly List<int> nodesToSelect = [];
	private bool drawTextField;
	private bool focusTextFieldNextFrame;
	private string textFieldValue = "";

	// A key press spans more than one frame and lastEvents only ever holds the newest, so the frame
	// that carried the request would be overwritten before a test could read it.
	private readonly List<int> observedLinkDeletions = [];
	private readonly List<int> observedNodeDeletions = [];

	// Read on the same frame the handler reads it, since it is only meaningful inside one.
	private bool lastWantTextInput;

	[TestCleanup]
	public void TearDown() => harness?.Dispose();

	private void Start()
	{
		harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Title = nameof(NodeEditorInteractionTests),
				OnRender = _ => DrawGraph(),
				SaveIniSettings = false,
			},
			Viewport);

		harness.Step(3);
	}

	private void DrawGraph()
	{
		Vector2 editorPosition = ImGui.GetCursorScreenPos();
		Vector2 editorSize = ImGui.GetContentRegionAvail();

		renderer.Render(engine, editorSize);

		foreach (KeyValuePair<int, Vector2> update in renderer.GetNodePositionUpdates(engine))
		{
			engine.UpdateNodePosition(update.Key, update.Value);
		}

		foreach (KeyValuePair<int, Vector2> update in renderer.GetNodeDimensionUpdates(engine))
		{
			engine.UpdateNodeDimensions(update.Key, update.Value);
		}

		// Stands in for the user clicking the link. Selecting by id keeps these tests off the link's
		// drawn curve, whose screen position depends on node measurement and the layout. It has to
		// happen outside BeginNodeEditor/EndNodeEditor, which is why it sits here and not in a test
		// body: ImNodes asserts on the scope, and Render has just closed the editor.
		if (linkToSelect is int selectId)
		{
			ImNodes.SelectLink(selectId);
			linkToSelect = null;
		}

		// The node equivalent, and outside the editor scope for the same reason. Clicking a node
		// would mean driving the mouse onto a body whose screen position the physics is still
		// moving, so the tests name the node instead.
		if (nodesToSelect.Count > 0)
		{
			foreach (int nodeId in nodesToSelect)
			{
				ImNodes.SelectNode(nodeId);
			}

			nodesToSelect.Clear();
		}

		// Stands in for the user having clicked into a text box. WantTextInput is what tells the
		// handler that a Delete belongs to the text field rather than to the graph, and ImGui only
		// raises it while a text widget is active - so the fixture has to draw a real one and give
		// it the keyboard, rather than setting a flag.
		if (drawTextField)
		{
			if (focusTextFieldNextFrame)
			{
				ImGui.SetKeyboardFocusHere();
				focusTextFieldNextFrame = false;
			}

			ImGui.InputText("##typing", ref textFieldValue, 64);
		}

		lastWantTextInput = ImGui.GetIO().WantTextInput;

		// ImNodes reports interactions for the editor that just closed, so the handler runs after
		// the render rather than before it.
		lastEvents = input.ProcessInput();
		observedLinkDeletions.AddRange(lastEvents.LinkDeletionRequests);
		observedNodeDeletions.AddRange(lastEvents.NodeDeletionRequests);

		engine.SetDraggedNodes(renderer.CurrentlyDraggedNodes);
		engine.UpdatePhysics(1f / 60f);

		if (drawDebugOverlays)
		{
			renderer.RenderDebugOverlays(engine, editorPosition, editorSize, showDebug: true);
		}
	}

	[TestMethod]
	public void ProcessInput_ReportsNothingWhenTheUserDidNothing()
	{
		engine.CreateNode(new Vector2(200, 200), "Source", [], ["Value"]);
		Start();

		if (lastEvents is not InputEvents events)
		{
			Assert.Fail("The input handler never ran.");
			return;
		}

		Assert.IsEmpty(events.LinkCreationRequests);
		Assert.IsEmpty(events.LinkDeletionRequests);
		Assert.IsEmpty(events.NodeDeletionRequests);
	}

	/// <summary>
	/// Draws a two-node graph joined by one link, runs frames until it is on screen, and selects
	/// the link the way a click would.
	/// </summary>
	/// <returns>The id of the selected link.</returns>
	private int StartWithOneSelectedLink()
	{
		Node source = engine.CreateNode(new Vector2(200, 200), "Source", [], ["Value"]);
		Node target = engine.CreateNode(new Vector2(600, 200), "Target", ["Source.Value"], []);
		LinkCreationResult created = engine.TryCreateLink(source.OutputPins[0].Id, target.InputPins[0].Id);
		Assert.IsTrue(created.Success, $"The fixture needs a link: {created.Message}");

		Start();

		linkToSelect = created.Link!.Id;
		harness.Step(2);
		observedLinkDeletions.Clear();

		return created.Link.Id;
	}

	[TestMethod]
	public void ProcessInput_WithALinkSelectedAndDeletePressed_RequestsItsDeletion()
	{
		// The gesture the issue reports as doing nothing: click a link, press Delete. ImNodes offers
		// the selection but never acts on it, and IsLinkDestroyed only fires for a link dragged off
		// its pin - a gesture that is itself disabled unless the editor opts into link detaching. So
		// before the fix, LinkDeletionRequests could not be made non-empty by any user gesture.
		int linkId = StartWithOneSelectedLink();

		harness.Keyboard.Press(ImGuiKey.Delete);

		CollectionAssert.Contains(observedLinkDeletions, linkId, "Deleting a selected link should reach the application as a deletion request");
	}

	[TestMethod]
	public void ProcessInput_WithALinkSelectedAndBackspacePressed_RequestsItsDeletion()
	{
		// Backspace is the key labelled Delete on a Mac keyboard, so it has to work too.
		int linkId = StartWithOneSelectedLink();

		harness.Keyboard.Press(ImGuiKey.Backspace);

		CollectionAssert.Contains(observedLinkDeletions, linkId, "Backspace should delete a selected link, for Mac keyboards");
	}

	[TestMethod]
	public void ProcessInput_WithALinkSelectedAndNoKeyPressed_RequestsNothing()
	{
		// Selecting a link is not asking for it to be removed. Without this, a click would delete.
		StartWithOneSelectedLink();

		harness.Step(3);

		Assert.IsEmpty(observedLinkDeletions, "Selecting a link is not a request to delete it");
	}

	[TestMethod]
	public void ProcessInput_AfterDeletingTheSelection_DoesNotRequestTheSameLinkAgain()
	{
		// The selection names links the application is about to remove, so it must not survive the
		// press. Left in place it would name the same ids on the next Delete, asking the application
		// to remove a link that is already gone.
		int linkId = StartWithOneSelectedLink();
		harness.Keyboard.Press(ImGuiKey.Delete);
		Assert.IsTrue(engine.RemoveLink(linkId), "The fixture should have a link to remove");
		observedLinkDeletions.Clear();

		harness.Keyboard.Press(ImGuiKey.Delete);

		Assert.IsEmpty(observedLinkDeletions, "A second Delete should not re-request a link that was already handed over");
	}

	/// <summary>
	/// Draws a two-node graph joined by one link, runs frames until it is on screen, and selects
	/// the named nodes the way a click would.
	/// </summary>
	/// <returns>The two node ids, source first.</returns>
	private (int SourceId, int TargetId) StartWithSelectedNodes(bool selectSource, bool selectTarget)
	{
		Node source = engine.CreateNode(new Vector2(200, 200), "Source", [], ["Value"]);
		Node target = engine.CreateNode(new Vector2(600, 200), "Target", ["Source.Value"], []);
		LinkCreationResult created = engine.TryCreateLink(source.OutputPins[0].Id, target.InputPins[0].Id);
		Assert.IsTrue(created.Success, $"The fixture needs a link: {created.Message}");

		Start();

		if (selectSource)
		{
			nodesToSelect.Add(source.Id);
		}

		if (selectTarget)
		{
			nodesToSelect.Add(target.Id);
		}

		harness.Step(2);
		observedNodeDeletions.Clear();
		observedLinkDeletions.Clear();

		return (source.Id, target.Id);
	}

	[TestMethod]
	public void ProcessInput_WithANodeSelectedAndDeletePressed_RequestsItsDeletion()
	{
		// The gesture #454 reports as doing nothing: click a node, press Delete. ImNodes offers the
		// selection but never acts on it, and unlike a link there is no IsNodeDestroyed to fall back
		// on, so before the fix no user gesture could ask for a node to be removed at all.
		(int sourceId, _) = StartWithSelectedNodes(selectSource: true, selectTarget: false);

		harness.Keyboard.Press(ImGuiKey.Delete);

		CollectionAssert.Contains(observedNodeDeletions, sourceId, "Deleting a selected node should reach the application as a deletion request");
	}

	[TestMethod]
	public void ProcessInput_WithANodeSelectedAndBackspacePressed_RequestsItsDeletion()
	{
		// Backspace is the key labelled Delete on a Mac keyboard, so it has to work here exactly as
		// it does for links.
		(int sourceId, _) = StartWithSelectedNodes(selectSource: true, selectTarget: false);

		harness.Keyboard.Press(ImGuiKey.Backspace);

		CollectionAssert.Contains(observedNodeDeletions, sourceId, "Backspace should delete a selected node, for Mac keyboards");
	}

	[TestMethod]
	public void ProcessInput_WithSeveralNodesSelected_RequestsAllOfThem()
	{
		// The issue asks for "node(s)", so one press has to clear the whole selection rather than
		// whichever node ImNodes happens to report first.
		(int sourceId, int targetId) = StartWithSelectedNodes(selectSource: true, selectTarget: true);

		harness.Keyboard.Press(ImGuiKey.Delete);

		CollectionAssert.Contains(observedNodeDeletions, sourceId, "Both selected nodes should be requested");
		CollectionAssert.Contains(observedNodeDeletions, targetId, "Both selected nodes should be requested");
	}

	[TestMethod]
	public void ProcessInput_WithANodeSelectedAndNoKeyPressed_RequestsNothing()
	{
		// Selecting a node is not asking for it to be removed. Without this, a click would delete.
		StartWithSelectedNodes(selectSource: true, selectTarget: false);

		harness.Step(3);

		Assert.IsEmpty(observedNodeDeletions, "Selecting a node is not a request to delete it");
	}

	[TestMethod]
	public void ProcessInput_AfterDeletingTheSelection_DoesNotRequestTheSameNodeAgain()
	{
		// The selection names nodes the application is about to remove, so it must not survive the
		// press. Left in place it would name the same ids on the next Delete, asking the application
		// to remove a node that is already gone.
		(int sourceId, _) = StartWithSelectedNodes(selectSource: true, selectTarget: false);
		harness.Keyboard.Press(ImGuiKey.Delete);
		Assert.IsTrue(engine.RemoveNode(sourceId), "The fixture should have a node to remove");
		observedNodeDeletions.Clear();

		harness.Keyboard.Press(ImGuiKey.Delete);

		Assert.IsEmpty(observedNodeDeletions, "A second Delete should not re-request a node that was already handed over");
	}

	[TestMethod]
	public void ProcessInput_WhileTypingInATextField_LeavesTheSelectionAlone()
	{
		// A Delete aimed at a text field is not aimed at the graph. Without the WantTextInput guard,
		// deleting a character in any input box on screen would silently take the user's selected
		// nodes and links with it - the one failure of this gesture that destroys work rather than
		// merely doing nothing.
		(int sourceId, _) = StartWithSelectedNodes(selectSource: true, selectTarget: false);
		linkToSelect = engine.Links[0].Id;
		drawTextField = true;
		focusTextFieldNextFrame = true;
		harness.Step(3);
		Assert.IsTrue(lastWantTextInput, "The fixture needs the text field to hold the keyboard");
		observedNodeDeletions.Clear();
		observedLinkDeletions.Clear();

		harness.Keyboard.Press(ImGuiKey.Delete);

		Assert.IsEmpty(observedNodeDeletions, "Typing must not delete the selected node");
		Assert.IsEmpty(observedLinkDeletions, "Typing must not delete the selected link either");
		Assert.IsNotEmpty(engine.Nodes.Where(n => n.Id == sourceId), "The node is still there to be deleted later");
	}

	[TestMethod]
	public void ProcessInput_WithBothANodeAndALinkSelected_RequestsBoth()
	{
		// One press, one gesture: "remove what I have selected". Reading the key separately per
		// selection kind would let the two drift apart, which is how a Delete ends up taking the
		// links and leaving the node behind.
		Node source = engine.CreateNode(new Vector2(200, 200), "Source", [], ["Value"]);
		Node target = engine.CreateNode(new Vector2(600, 200), "Target", ["Source.Value"], []);
		LinkCreationResult created = engine.TryCreateLink(source.OutputPins[0].Id, target.InputPins[0].Id);
		Assert.IsTrue(created.Success, $"The fixture needs a link: {created.Message}");

		Start();

		linkToSelect = created.Link!.Id;
		nodesToSelect.Add(source.Id);
		harness.Step(2);
		observedLinkDeletions.Clear();
		observedNodeDeletions.Clear();

		harness.Keyboard.Press(ImGuiKey.Delete);

		CollectionAssert.Contains(observedLinkDeletions, created.Link.Id, "A selected link should still be requested when a node is selected too");
		CollectionAssert.Contains(observedNodeDeletions, source.Id, "A selected node should be requested when a link is selected too");
	}

	[TestMethod]
	public void RemoveNode_ForADeletionRequest_TakesTheNodeAndItsLinksWithIt()
	{
		// What the application does with the request, since the handler only reports it. Deleting a
		// node cannot leave a link dangling off a pin that no longer exists.
		(int sourceId, int targetId) = StartWithSelectedNodes(selectSource: true, selectTarget: false);

		harness.Keyboard.Press(ImGuiKey.Delete);

		foreach (int nodeId in observedNodeDeletions)
		{
			engine.RemoveNode(nodeId);
		}

		Assert.IsEmpty(engine.Nodes.Where(n => n.Id == sourceId), "The requested node should be gone");
		Assert.IsNotEmpty(engine.Nodes.Where(n => n.Id == targetId), "Only the selected node should be gone");
		Assert.IsEmpty(engine.Links, "The link hanging off the deleted node should be gone with it");
	}

	[TestMethod]
	public void Renderer_MeasuresWhereEachPinSitsOnItsNode()
	{
		Node source = engine.CreateNode(new Vector2(150, 150), "Source", [], ["Out"]);
		Node target = engine.CreateNode(new Vector2(600, 150), "Target", ["First", "Second", "Third"], []);
		engine.TryCreateLink(source.OutputPins[0].Id, target.InputPins[0].Id);
		Start();

		Node measured = engine.Nodes.Single(n => n.Id == target.Id);

		List<float> inputYs = [];
		foreach (Pin pin in measured.InputPins)
		{
			Assert.IsTrue(engine.TryGetPinOffset(pin.Id, out Vector2 offset), $"{pin.Id} was never measured.");

			// Inputs hang off the left edge, and every pin sits somewhere down the node's own height.
			Assert.AreEqual(0f, offset.X, 0.01f, "an input pin should sit on the node's left edge");
			Assert.IsGreaterThan(0f, offset.Y, "a pin sits below the node's top edge");
			Assert.IsLessThan(measured.Dimensions.Y, offset.Y, "a pin sits above the node's bottom edge");
			inputYs.Add(offset.Y);
		}

		// Drawn top to bottom, so measured top to bottom - which is the whole point of measuring rather
		// than assuming every pin is at the node's middle.
		for (int i = 1; i < inputYs.Count; i++)
		{
			Assert.IsGreaterThan(inputYs[i - 1], inputYs[i], "pins should be measured in the order they are drawn");
		}

		Assert.IsGreaterThan(20f, inputYs[^1] - inputYs[0], "three rows of pins should span more than a few pixels");

		Node measuredSource = engine.Nodes.Single(n => n.Id == source.Id);
		Assert.IsTrue(engine.TryGetPinOffset(measuredSource.OutputPins[0].Id, out Vector2 outputOffset));
		Assert.AreEqual(measuredSource.Dimensions.X, outputOffset.X, 0.01f, "an output pin should sit on the node's right edge");
	}

	public void Renderer_MeasuresEveryNodeItDrew()
	{
		Node source = engine.CreateNode(new Vector2(200, 200), "Source", [], ["Value"]);
		Node target = engine.CreateNode(new Vector2(500, 200), "Target", ["Source.Value"], []);
		engine.TryCreateLink(source.OutputPins[0].Id, target.InputPins[0].Id);
		Start();

		// The dimensions come back from ImNodes through the renderer; without them the layout has
		// no idea how big anything is and nodes overlap.
		foreach (Node node in engine.Nodes)
		{
			Assert.IsGreaterThan(0f, node.Dimensions.X, $"{node.Name} was never measured.");
			Assert.IsGreaterThan(0f, node.Dimensions.Y, $"{node.Name} was never measured.");
		}

		Assert.IsEmpty(renderer.CurrentlyDraggedNodes, "Nothing was dragged.");
	}

	[TestMethod]
	public void Renderer_DrawsDebugOverlaysWithoutUpsettingImGui()
	{
		Node source = engine.CreateNode(new Vector2(200, 200), "Source", [], ["Value"]);
		Node target = engine.CreateNode(new Vector2(500, 260), "Target", ["Source.Value"], []);
		engine.TryCreateLink(source.OutputPins[0].Id, target.InputPins[0].Id);
		engine.UpdatePhysicsSettings(new PhysicsSettings { Enabled = true });
		drawDebugOverlays = true;

		Start();
		harness.Step(5);

		Assert.AreEqual(0, ImGui.GetCurrentContext().ErrorCountCurrentFrame, "ImGui reported the overlays as misuse.");
		Assert.IsNotNull(harness.Capture().FindBounds(pixel => pixel.A > 0), "The frame was blank.");
	}

	[TestMethod]
	public void Renderer_SurvivesAnEmptyGraph()
	{
		Start();
		harness.Step(5);

		Assert.IsEmpty(engine.Nodes);
		Assert.AreEqual(0, ImGui.GetCurrentContext().ErrorCountCurrentFrame);
	}
}
