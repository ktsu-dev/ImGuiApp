// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;
using Hexa.NET.ImNodes;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.NodeEditor;
using ktsu.Keybinding.Core.Services;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// The gestures only a live editor has — dragging, snapping, the undo keys, and comment boxes —
/// driven headlessly through the harness and recorded into a <see cref="NodeEditorHistory"/>.
/// </summary>
/// <remarks>
/// The physics is never stepped, so a node stays wherever a gesture or an undo puts it and every
/// position asserted is the gesture's doing.
/// </remarks>
[TestClass]
public sealed class NodeEditorGestureTests : IDisposable
{
	private static readonly HarnessOptions Viewport = new() { Width = 900, Height = 700 };

	private readonly NodeEditorEngine engine = new();
	private readonly NodeEditorRenderer renderer = new();
	private readonly NodeEditorInputHandler input = new();
	private readonly NodeEditorHistory history;
	private ImGuiAppHarness harness = null!;

	public NodeEditorGestureTests()
	{
		history = new NodeEditorHistory(engine);
		renderer.History = history;
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		harness?.Dispose();
		history.Dispose();
	}

	private void Start()
	{
		harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Title = nameof(NodeEditorGestureTests),
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

		InputEvents events = input.ProcessInput();
		if (events.UndoRequested)
		{
			history.Undo();
		}

		if (events.RedoRequested)
		{
			history.Redo();
		}
	}

	private Node NodeById(int id) => engine.Nodes.Single(n => n.Id == id);

	/// <summary>A point on a node's title bar, which is where a drag has to start to move it.</summary>
	private Vector2 TitleOf(int nodeId)
	{
		Assert.IsTrue(renderer.TryGetNodeScreenRect(nodeId, out ScreenRect rect), "The node has not been drawn.");
		return new Vector2(rect.Centre.X, rect.Min.Y + 6f);
	}

	private void DragBy(Vector2 from, Vector2 delta) =>
		harness.Mouse.Drag(from.X, from.Y, from.X + delta.X, from.Y + delta.Y);

	[TestMethod]
	public void DraggingANode_RecordsOneStep_ThatPutsItBack()
	{
		Node node = engine.CreateNode(new Vector2(200, 200), "Dragged", 1, 1);
		Start();

		DragBy(TitleOf(node.Id), new Vector2(120, 40));
		harness.Step(2);

		Vector2 dropped = NodeById(node.Id).Position;
		Assert.IsGreaterThan(100f, Vector2.Distance(new Vector2(200, 200), dropped), "The drag should have moved the node.");
		Assert.AreEqual(1, history.Service.CommandCount, "A drag across sixteen frames should be one step.");

		history.Undo();
		harness.Step(2);

		Assert.AreEqual(new Vector2(200, 200), NodeById(node.Id).Position);
		Assert.AreEqual(1, history.Service.CommandCount, "Putting the node back should not itself have been recorded as a drag.");
		Assert.IsTrue(history.CanRedo);
	}

	[TestMethod]
	public void SnapToGrid_LandsADraggedNodeOnTheGrid()
	{
		renderer.SnapToGrid = true;
		renderer.GridSpacing = 16f;
		Node node = engine.CreateNode(new Vector2(203, 197), "Snapped", 1, 1);
		Start();

		DragBy(TitleOf(node.Id), new Vector2(37, 23));
		harness.Step(2);

		Vector2 dropped = NodeById(node.Id).Position;
		Assert.AreEqual(renderer.SnapPositionToGrid(dropped), dropped, $"{dropped} is not a grid point.");
		Assert.AreEqual(0f, MathF.IEEERemainder(dropped.X - ImNodes.EditorContextGetPanning().X, 16f), 0.01f);
	}

	[TestMethod]
	public void SnapNodesToGrid_MovesNodesPlacedInCode_AsOneStep()
	{
		renderer.GridSpacing = 16f;
		Node a = engine.CreateNode(new Vector2(203, 197), "A", 0, 0);
		Node b = engine.CreateNode(new Vector2(411, 305), "B", 0, 0);
		Start();

		Assert.AreEqual(2, renderer.SnapNodesToGrid(engine, [a.Id, b.Id]));
		harness.Step(2);

		Assert.AreEqual(renderer.SnapPositionToGrid(NodeById(a.Id).Position), NodeById(a.Id).Position);
		Assert.AreEqual(1, history.Service.CommandCount);
		history.Undo();
		Assert.AreEqual(new Vector2(203, 197), NodeById(a.Id).Position);
	}

	[TestMethod]
	public void CtrlZ_Undoes_AndCtrlYAndCtrlShiftZ_Redo()
	{
		Start();
		history.Record("Add", () => engine.CreateNode(new Vector2(200, 200), "Made", 0, 0));
		harness.Step(2);

		harness.Keyboard.Press(ImGuiKey.Z, ctrl: true);
		Assert.IsEmpty(engine.Nodes, "Ctrl+Z should have undone the creation.");

		harness.Keyboard.Press(ImGuiKey.Y, ctrl: true);
		Assert.HasCount(1, engine.Nodes, "Ctrl+Y should have redone it.");

		harness.Keyboard.Press(ImGuiKey.Z, ctrl: true);
		harness.Keyboard.Press(ImGuiKey.Z, ctrl: true, shift: true);
		Assert.HasCount(1, engine.Nodes, "Ctrl+Shift+Z should redo as well.");
	}

	[TestMethod]
	public void AKeymap_DecidesWhichKeysUndo()
	{
		CommandRegistry registry = new();
		KeybindingService keybindings = new(registry, new ProfileManager());
		keybindings.CreateProfile("default", "Default");
		keybindings.SetActiveProfile("default");
		Assert.AreEqual(NodeEditorCommands.DefaultChords.Count, NodeEditorCommands.Register(registry, keybindings));
		Assert.AreEqual(0, NodeEditorCommands.Register(registry, keybindings), "Registering again should not rebind anything.");

		// The user rebinds undo.
		keybindings.BindChord(NodeEditorCommands.Undo, keybindings.ParseChord("Ctrl+U"));
		input.Keybindings = keybindings;

		Start();
		history.Record("Add", () => engine.CreateNode(new Vector2(200, 200), "Made", 0, 0));
		harness.Step(2);

		harness.Keyboard.Press(ImGuiKey.Z, ctrl: true);
		Assert.HasCount(1, engine.Nodes, "Ctrl+Z is no longer bound to undo, so it should do nothing.");

		harness.Keyboard.Press(ImGuiKey.U, ctrl: true);
		Assert.IsEmpty(engine.Nodes, "Ctrl+U is undo now.");

		harness.Keyboard.Press(ImGuiKey.Y, ctrl: true);
		Assert.HasCount(1, engine.Nodes, "Redo keeps its default chord.");
	}

	[TestMethod]
	public void UndoingADeletion_DrawsTheNodeWhereItWas()
	{
		Node node = engine.CreateNode(new Vector2(260, 220), "Returning", 1, 1);
		Start();
		Assert.IsTrue(renderer.TryGetNodeScreenRect(node.Id, out ScreenRect before));

		history.RemoveNode(node.Id);
		harness.Step(2);
		history.Undo();
		harness.Step(3);

		Assert.AreEqual(new Vector2(260, 220), NodeById(node.Id).Position, "ImNodes forgot the node while it was gone, and must not hand back its own default position as a drag.");
		Assert.IsTrue(renderer.TryGetNodeScreenRect(node.Id, out ScreenRect after));
		Assert.AreEqual(before.Min, after.Min);
	}

	[TestMethod]
	public void DraggingAnInlineEditor_IsOneStep()
	{
		Node node = engine.CreateNodeFromSpecs(new Vector2(200, 200), "Tuned", [new PinSpec("Sigma", typeof(float), 2f)], []);
		Start();

		Assert.IsTrue(renderer.TryGetNodeScreenRect(node.Id, out ScreenRect rect));
		Assert.IsTrue(renderer.TryGetPinScreenPosition(node.InputPins[0].Id, out Vector2 pin));
		float left = rect.Min.X + ImNodes.GetStyle().NodePadding.X + ImGui.CalcTextSize("Sigma").X + ImGui.GetStyle().ItemSpacing.X;
		Vector2 start = new(left + (renderer.InlineEditorWidth * 0.25f), pin.Y);

		DragBy(start, new Vector2(40, 0));
		harness.Step(2);

		Assert.IsGreaterThan(2f, (float)engine.GetPinValue(node.InputPins[0].Id)!);
		Assert.AreEqual(1, history.Service.CommandCount, "Scrubbing a value across many frames should undo in one step.");
		history.Undo();
		Assert.AreEqual(2f, engine.GetPinValue(node.InputPins[0].Id));
	}

	/// <summary>A node inside a comment box, both drawn and measured.</summary>
	private (Node Node, CommentBox Box) StartWithABoxedNode(Vector4? color = null)
	{
		Node node = engine.CreateNode(new Vector2(260, 240), "Boxed", 1, 1);
		Start();
		CommentBox box = engine.CreateCommentBoxAround([node.Id], "Preprocessing", color: color)!;
		harness.Step(2);
		return (node, box);
	}

	[TestMethod]
	public void ACommentBox_IsDrawnAroundItsNodes()
	{
		(Node node, CommentBox box) = StartWithABoxedNode();

		Assert.IsTrue(renderer.TryGetCommentBoxScreenRect(box.Id, out ScreenRect boxRect));
		Assert.IsTrue(renderer.TryGetNodeScreenRect(node.Id, out ScreenRect nodeRect));

		// The box and the node are placed through different ImNodes calls, so this is what shows the
		// box's editor-space rectangle lands on screen where the node's does.
		Assert.AreEqual(boxRect.Min.X + NodeEditorEngine.DefaultCommentBoxPadding, nodeRect.Min.X, 0.5f);
		Assert.AreEqual(boxRect.Max.Y - NodeEditorEngine.DefaultCommentBoxPadding, nodeRect.Max.Y, 0.5f);

		// The box's items move the cursor about inside the editor, which ImGui reports as misuse if
		// it is left anywhere it should not be.
		Assert.AreEqual(0, ImGui.GetCurrentContext().ErrorCountCurrentFrame, "ImGui reported the comment box's drawing as misuse.");
	}

	[TestMethod]
	public void ACommentBoxWithNoNodes_DrawsWithoutError()
	{
		Start();
		engine.CreateCommentBox(new Vector2(100, 100), new Vector2(300, 200), "Empty");
		harness.Step(3);

		Assert.AreEqual(0, ImGui.GetCurrentContext().ErrorCountCurrentFrame, "ImGui reported the comment box's drawing as misuse.");
	}

	[TestMethod]
	public void ACommentBox_IsDrawnUnderTheNodes()
	{
		(Node node, CommentBox box) = StartWithABoxedNode(new Vector4(1f, 0f, 0f, 1f));
		Assert.IsTrue(renderer.TryGetCommentBoxScreenRect(box.Id, out ScreenRect boxRect));
		Assert.IsTrue(renderer.TryGetNodeScreenRect(node.Id, out ScreenRect nodeRect));

		CapturedFrame frame = harness.Capture();
		static bool IsRed(Rgba32 p) => p.R > 200 && p.G < 60 && p.B < 60;

		Rgba32 inBoxOnly = frame.GetPixel((int)(boxRect.Min.X + 6), (int)(boxRect.Max.Y - 6));
		Rgba32 inNode = frame.GetPixel((int)nodeRect.Centre.X, (int)(nodeRect.Max.Y - 4));

		Assert.IsTrue(IsRed(inBoxOnly), $"The box's own fill should show where no node covers it, but found {inBoxOnly}.");
		Assert.IsFalse(IsRed(inNode), $"The node should be drawn over the box, but its body is {inNode}.");
	}

	[TestMethod]
	public void DraggingACommentBoxTitle_CarriesItsNodes_AsOneStep()
	{
		(Node node, CommentBox box) = StartWithABoxedNode();
		Assert.IsTrue(renderer.TryGetCommentBoxScreenRect(box.Id, out ScreenRect rect));

		DragBy(new Vector2(rect.Min.X + 30f, rect.Min.Y + 6f), new Vector2(100, 50));
		harness.Step(2);

		Vector2 boxMoved = engine.FindCommentBox(box.Id)!.Position - box.Position;
		Vector2 nodeMoved = NodeById(node.Id).Position - new Vector2(260, 240);
		Assert.AreEqual(100f, boxMoved.X, 1f);
		Assert.AreEqual(50f, boxMoved.Y, 1f);
		Assert.AreEqual(boxMoved, nodeMoved, "The node inside should have moved exactly as far as the box.");
		Assert.AreEqual(1, history.Service.CommandCount, "The drag should be one step.");
	}

	[TestMethod]
	public void CommentBoxDrag_Undoes_BoxAndContentsTogether()
	{
		(Node node, CommentBox box) = StartWithABoxedNode();
		Assert.IsTrue(renderer.TryGetCommentBoxScreenRect(box.Id, out ScreenRect rect));
		DragBy(new Vector2(rect.Min.X + 30f, rect.Min.Y + 6f), new Vector2(100, 50));
		harness.Step(2);

		history.Undo();
		harness.Step(2);

		Assert.AreEqual(box.Position, engine.FindCommentBox(box.Id)!.Position);
		Assert.AreEqual(new Vector2(260, 240), NodeById(node.Id).Position);
	}

	[TestMethod]
	public void ACommentBoxsCloseButton_RemovesIt_ButNotItsNodes()
	{
		(Node _, CommentBox box) = StartWithABoxedNode();
		Assert.IsTrue(renderer.TryGetCommentBoxScreenRect(box.Id, out ScreenRect rect));
		float titleHeight = ImGui.GetFrameHeight();

		harness.Mouse.Click(rect.Max.X - (titleHeight * 0.5f), rect.Min.Y + (titleHeight * 0.5f));
		harness.Step(2);

		Assert.IsEmpty(engine.CommentBoxes);
		Assert.HasCount(1, engine.Nodes);
		history.Undo();
		Assert.HasCount(1, engine.CommentBoxes);
	}

	[TestMethod]
	public void ResizingACommentBox_ChangesItsSize_AsOneStep()
	{
		(Node _, CommentBox box) = StartWithABoxedNode();
		Assert.IsTrue(renderer.TryGetCommentBoxScreenRect(box.Id, out ScreenRect rect));

		DragBy(rect.Max - new Vector2(4f, 4f), new Vector2(80, 60));
		harness.Step(2);

		Vector2 grown = engine.FindCommentBox(box.Id)!.Size - box.Size;
		Assert.AreEqual(80f, grown.X, 1f);
		Assert.AreEqual(60f, grown.Y, 1f);
		Assert.AreEqual(box.Position, engine.FindCommentBox(box.Id)!.Position, "Resizing keeps the top-left corner where it is.");

		history.Undo();
		Assert.AreEqual(box.Size, engine.FindCommentBox(box.Id)!.Size);
	}

	[TestMethod]
	public void DoubleClickingACommentTitle_RenamesIt()
	{
		(Node _, CommentBox box) = StartWithABoxedNode();
		Assert.IsTrue(renderer.TryGetCommentBoxScreenRect(box.Id, out ScreenRect rect));
		Vector2 title = new(rect.Min.X + 30f, rect.Min.Y + 6f);

		harness.Mouse.Click(title.X, title.Y);
		harness.Mouse.Click(title.X, title.Y);
		harness.Step(2);
		Assert.AreEqual(box.Id, renderer.RenamingCommentBoxId, "A double-click on the title should open it for editing.");

		harness.Keyboard.Press(ImGuiKey.A, ctrl: true);
		harness.Keyboard.Type("Alignment");
		harness.Keyboard.Press(ImGuiKey.Enter);
		harness.Step(2);

		Assert.AreEqual("Alignment", engine.FindCommentBox(box.Id)!.Title);
		Assert.IsNull(renderer.RenamingCommentBoxId);
		history.Undo();
		Assert.AreEqual("Preprocessing", engine.FindCommentBox(box.Id)!.Title);
	}
}
