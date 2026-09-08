// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor.Tests;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

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

		// ImNodes reports interactions for the editor that just closed, so the handler runs after
		// the render rather than before it.
		lastEvents = input.ProcessInput();

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
