// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor.Tests;

using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;
using Hexa.NET.ImNodes;

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
	/// A screen-space point inside the first input pin's inline editor.
	/// </summary>
	/// <remarks>
	/// Read live from the style rather than assumed from the node's right edge: once the output row
	/// accounts for the editor's width too (the fix for the label-drift bug this file also covers),
	/// the node can be wider than the input row alone, so the editor no longer necessarily reaches
	/// the node's right edge. The input row starts flush at the node's own left content edge
	/// regardless, so the editor's midpoint is found by walking out from there: past the node's left
	/// padding, the label, the item spacing <c>SameLine()</c> leaves, and half the editor's own width.
	/// </remarks>
	private Vector2 EditorMidpoint(Node node)
	{
		Assert.IsTrue(renderer.TryGetNodeScreenRect(node.Id, out ScreenRect rect));
		Assert.IsTrue(renderer.TryGetPinScreenPosition(node.InputPins[0].Id, out Vector2 pin));

		float labelWidth = ImGui.CalcTextSize(node.InputPins[0].EffectiveDisplayName).X;
		float nodePadding = ImNodes.GetStyle().NodePadding.X;
		float itemSpacing = ImGui.GetStyle().ItemSpacing.X;
		float editorWidth = renderer.InlineEditorWidth * renderer.Zoom;

		float x = rect.Min.X + nodePadding + labelWidth + itemSpacing + (editorWidth * 0.5f);
		return new Vector2(x, pin.Y);
	}

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

		Vector2 target = EditorMidpoint(node);
		harness.Mouse.Click(target.X, target.Y);
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
		Vector2 target = EditorMidpoint(node);
		harness.Mouse.Drag(target.X, target.Y, target.X + 20f, target.Y);
		harness.Step(3);

		Assert.AreEqual(before, engine.Nodes[0].Position, "The node moved, so the editor's drag reached ImNodes as a node drag.");
	}

	/// <summary>
	/// An editor makes its row taller, so the link's attach point moves down with it. What must not
	/// happen is the offset being measured from the editor alone and landing off the row.
	/// </summary>
	/// <remarks>
	/// A one-input node makes this nearly unfalsifiable: any Y strictly between the node's top and
	/// bottom edges would pass, editor row or not. Two typed inputs give two rows whose pin Ys must
	/// come out distinct, in order, and about one editor row apart.
	/// </remarks>
	[TestMethod]
	public void ThePinOffset_StaysOnTheRowTheEditorIsDrawnOn()
	{
		Node node = engine.CreateNodeFromSpecs(
			new Vector2(250, 200),
			"Blob Filter",
			[
				new PinSpec("Threshold", typeof(double), 128.0),
				new PinSpec("Sigma", typeof(double), 2.0),
			],
			[]);
		Start();
		harness.Step(5);

		Assert.IsTrue(renderer.TryGetPinScreenPosition(node.InputPins[0].Id, out Vector2 firstPin));
		Assert.IsTrue(renderer.TryGetPinScreenPosition(node.InputPins[1].Id, out Vector2 secondPin));
		Assert.IsTrue(renderer.TryGetNodeScreenRect(node.Id, out ScreenRect rect));

		Assert.IsTrue(firstPin.Y > rect.Min.Y, "The first pin sits below the node's top edge.");
		Assert.IsTrue(secondPin.Y < rect.Max.Y, "The second pin sits above the node's bottom edge.");
		Assert.IsLessThan(secondPin.Y, firstPin.Y, "The two rows should be in the order they were declared.");

		float rowSpacing = secondPin.Y - firstPin.Y;
		Assert.IsTrue(rowSpacing is > 10f and < 60f, $"The two rows should be about one editor row apart, not on top of each other or spread across the whole node. Was {rowSpacing}.");
	}

	/// <summary>
	/// Reproduces the layout bug the fix in the renderer's node-width estimate addresses: an inline
	/// editor widens an input row past what that estimate used to account for, which under-sizes the
	/// padding pushing an output label to the right and leaves it stranded mid-node instead of beside
	/// its own pin circle.
	/// </summary>
	[TestMethod]
	public void AnOutputLabel_EndsNearTheNodesRightEdge_WhenInputsHaveEditors()
	{
		Node node = engine.CreateNodeFromSpecs(
			new Vector2(250, 200),
			"Blob Filter",
			[
				new PinSpec("Threshold", typeof(double), 128.0),
				new PinSpec("AreaMin", typeof(double), 50.0),
				new PinSpec("Sigma", typeof(double), 2.0),
				new PinSpec("Invert", typeof(bool), false),
			],
			[new PinSpec("Count", typeof(int))]);

		// DrawNodeBody runs immediately after the output pins loop and before ImNodes.EndNode(), so
		// ImGui's own "last item" state still names the output label's Text() call — the same trick
		// RecordPinRow relies on for the row it measures.
		Vector2 labelRectMax = default;
		renderer.DrawNodeBody = _ => labelRectMax = ImGui.GetItemRectMax();

		Start();
		harness.Step(5);

		Assert.IsTrue(renderer.TryGetNodeScreenRect(node.Id, out ScreenRect rect));
		Assert.IsTrue(renderer.TryGetPinScreenPosition(node.OutputPins[0].Id, out Vector2 pin));

		float gapToNodeEdge = rect.Max.X - labelRectMax.X;
		float gapToPin = pin.X - labelRectMax.X;

		Assert.IsTrue(gapToNodeEdge is >= 0f and < 20f, $"The output label should end close to the node's right edge (within node padding), not the ~80px short the unfixed estimate produced. Gap was {gapToNodeEdge}.");
		Assert.IsTrue(gapToPin is >= 0f and < 30f, $"The output label should end close to its own pin circle. Gap was {gapToPin}.");
	}
}
