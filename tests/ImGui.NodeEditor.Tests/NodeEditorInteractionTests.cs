// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor.Tests;

using System.Collections.Generic;
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

		Assert.IsNotNull(lastEvents);
		Assert.IsEmpty(lastEvents.LinkCreationRequests);
		Assert.IsEmpty(lastEvents.LinkDeletionRequests);
	}

	[TestMethod]
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
