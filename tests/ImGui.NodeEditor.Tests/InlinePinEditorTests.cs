// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor.Tests;

using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.NodeEditor;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives the editors the renderer draws on a node face. An editor is made of what ImNodes lays out
/// and what the mouse does to it, so none of this is observable without drawing real frames.
/// </summary>
[TestClass]
public sealed class InlinePinEditorTests
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
				Title = nameof(InlinePinEditorTests),
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

	/// <summary>A node whose one input is an unconnected, typed, editable pin.</summary>
	private Node TypedNode() =>
		engine.CreateNodeFromSpecs(
			new Vector2(250, 200),
			"Blob Filter",
			[new PinSpec("Threshold", typeof(double), 128.0)],
			[new PinSpec("Count", typeof(int))]);

	/// <summary>
	/// An editor is wider than a label, so a node carrying one is wider than the same node without.
	/// That difference is what says an editor was drawn at all.
	/// </summary>
	[TestMethod]
	public void AnUnconnectedTypedInput_IsDrawnWiderThanItsLabelAlone()
	{
		TypedNode();
		renderer.DrawInlinePinEditors = false;
		Start();
		harness.Step(5);
		float withoutEditor = engine.Nodes[0].Dimensions.X;

		renderer.DrawInlinePinEditors = true;
		harness.Step(5);
		float withEditor = engine.Nodes[0].Dimensions.X;

		Assert.IsGreaterThan(withoutEditor, withEditor, "The editor should have widened the node.");
	}

	/// <summary>
	/// A connected pin takes its value from the link, so offering a box to type a different one in
	/// would be offering a value that nothing reads.
	/// </summary>
	[TestMethod]
	public void AConnectedInput_GetsNoEditor()
	{
		Node target = TypedNode();
		Node source = engine.CreateNodeFromSpecs(new Vector2(0, 200), "Source", [], [new PinSpec("Value", typeof(double))]);
		Start();
		harness.Step(5);
		float unconnected = engine.Nodes[0].Dimensions.X;

		engine.TryCreateLink(source.OutputPins[0].Id, target.InputPins[0].Id);
		harness.Step(5);

		Assert.IsLessThan(unconnected, engine.Nodes[0].Dimensions.X, "Connecting the pin should have taken its editor away.");
	}

	[TestMethod]
	public void AnUnsupportedType_GetsNoEditor()
	{
		engine.CreateNodeFromSpecs(
			new Vector2(250, 200),
			"Opaque",
			[new PinSpec("Engine", typeof(NodeEditorEngine))],
			[]);
		renderer.DrawInlinePinEditors = false;
		Start();
		harness.Step(5);
		float withoutEditors = engine.Nodes[0].Dimensions.X;

		renderer.DrawInlinePinEditors = true;
		harness.Step(5);

		Assert.AreEqual(withoutEditors, engine.Nodes[0].Dimensions.X, "Nothing here can edit a NodeEditorEngine, so nothing should have been drawn.");
	}

	/// <summary>
	/// The point of the whole feature: what the user does to the editor reaches the graph.
	/// </summary>
	/// <remarks>
	/// The "Threshold" pin is a <see cref="double"/>, so its editor is <c>ImGui.InputDouble</c>, a
	/// plain text field with a step of zero: dragging across it only extends a text selection, it
	/// never scrubs the value the way <c>DragFloat</c>/<c>DragInt</c> do. So this exercises the
	/// editor the way a real user commits a typed value, by clicking to focus it, selecting the
	/// existing text, typing a replacement, and pressing Enter. That still proves what the brief
	/// asks for (interacting with the editor reaches the engine), through the interaction this
	/// particular widget actually supports.
	/// </remarks>
	[TestMethod]
	public void EditingTheEditor_WritesThroughToTheEngine()
	{
		Node node = TypedNode();
		Start();
		harness.Step(5);

		Assert.IsTrue(renderer.TryGetPinScreenPosition(node.InputPins[0].Id, out Vector2 pin));
		Assert.IsTrue(renderer.TryGetNodeScreenRect(node.Id, out ScreenRect rect));

		// The editor sits to the right of the label on the pin's own row, so aim at the right-hand
		// side of the node at the pin's height. Measured: with InlineEditorWidth 90 at Zoom 1, the
		// editor's box occupies roughly the rightmost 90 pixels of the node before its own padding,
		// so 20 pixels in from the node's right edge lands inside it.
		float x = rect.Max.X - 20f;
		harness.Mouse.Click(x, pin.Y);
		harness.Step();

		harness.Keyboard.Press(ImGuiKey.A, ctrl: true);
		harness.Keyboard.Type("999");
		harness.Keyboard.Press(ImGuiKey.Enter);
		harness.Step(3);

		Assert.AreNotEqual(128.0, engine.GetPinValue(node.InputPins[0].Id), "Editing the editor should have changed the value.");
	}

	/// <summary>
	/// ImNodes marks an attribute active while a widget inside it is, which is what stops the node
	/// running away with the pointer. If that ever stops holding, editing a value drags the node.
	/// </summary>
	[TestMethod]
	public void DraggingTheEditor_DoesNotDragTheNode()
	{
		Node node = TypedNode();
		Start();
		harness.Step(5);

		Vector2 before = engine.Nodes[0].Position;
		Assert.IsTrue(renderer.TryGetNodeScreenRect(node.Id, out ScreenRect rect));
		Assert.IsTrue(renderer.TryGetPinScreenPosition(node.InputPins[0].Id, out Vector2 pin));

		float x = rect.Max.X - 20f;
		harness.Mouse.Drag(x, pin.Y, x + 60f, pin.Y);
		harness.Step(3);

		Assert.AreEqual(before, engine.Nodes[0].Position, "The node moved, so the editor's drag reached ImNodes as a node drag.");
	}

	/// <summary>
	/// An editor makes its row taller, so the link's attach point moves down with it. What must not
	/// happen is the offset being measured from the editor alone and landing off the row.
	/// </summary>
	[TestMethod]
	public void ThePinOffset_StaysOnTheRowTheEditorIsDrawnOn()
	{
		Node node = TypedNode();
		Start();
		harness.Step(5);

		Assert.IsTrue(renderer.TryGetPinScreenPosition(node.InputPins[0].Id, out Vector2 pin));
		Assert.IsTrue(renderer.TryGetNodeScreenRect(node.Id, out ScreenRect rect));

		Assert.IsTrue(pin.Y > rect.Min.Y, "The pin sits below the node's top edge.");
		Assert.IsTrue(pin.Y < rect.Max.Y, "And above its bottom edge.");
	}
}
