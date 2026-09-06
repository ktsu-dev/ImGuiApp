// Copyright (c) 2023-2026 ktsu-dev contributors

// ImGui contexts are global and the harness refuses to start while another is live, so every test
// in this assembly must have the process to itself.
[assembly: Microsoft.VisualStudio.TestTools.UnitTesting.DoNotParallelize]

namespace ktsu.ImGuiNodeEditor.Tests;

using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGuiNodeEditor;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Draws a graph through <see cref="NodeEditorRenderer"/> and asks what ImGui made of it: whether
/// the nodes hold their size frame to frame, and whether ImGui reported the drawing as misuse.
/// </summary>
[TestClass]
public sealed class NodeRenderingTests
{
	private static readonly HarnessOptions Viewport = new() { Width = 900, Height = 700 };

	private readonly NodeEditorEngine engine = new();
	private readonly NodeEditorRenderer renderer = new();

	private ImGuiAppHarness harness = null!;

	[TestCleanup]
	public void TearDown() => harness?.Dispose();

	/// <summary>
	/// How many usage errors ImGui reported while drawing the last frame.
	/// </summary>
	/// <remarks>
	/// ImGui counts these per frame and clears the count in the next one, so this is only the
	/// frame just drawn, and only while no frame is in flight.
	/// </remarks>
	private static int ErrorsLastFrame() => ImGui.GetCurrentContext().ErrorCountCurrentFrame;

	/// <summary>Starts a harness whose whole content is the graph the engine holds.</summary>
	private void Start()
	{
		harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Title = nameof(NodeRenderingTests),
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

	private Vector2 SizeOf(int index) => engine.Nodes[index].Dimensions;

	/// <summary>
	/// A node with no pins is as ordinary as any other: something the graph knows of but that
	/// nothing connects to.
	/// </summary>
	/// <remarks>
	/// ImNodes ends a node's title bar by moving the cursor to where the node's content starts.
	/// With no pin submitted after that, the node's group closed on a cursor that had been moved
	/// and never used, which cost two things at once: ImGui reported "code uses SetCursorPos() to
	/// extend window/parent boundaries" over the application every frame, and the node's measured
	/// width grew by eight pixels a frame, without limit, for as long as it was on screen.
	/// </remarks>
	[TestMethod]
	public void ANodeWithNoPinsKeepsItsSizeAndDrawsWithoutError()
	{
		engine.CreateNode(new Vector2(250, 200), "Lonely", [], []);
		Start();

		Vector2 settled = SizeOf(0);
		harness.Step(10);

		Assert.AreEqual(
			settled,
			SizeOf(0),
			$"The node grew from {settled} to {SizeOf(0)} while nothing happened to it.");

		Assert.AreEqual(0, ErrorsLastFrame(), "ImGui reported the node's drawing as misuse.");
	}

	[TestMethod]
	public void ANodeWithPinsKeepsItsSizeAndDrawsWithoutError()
	{
		Node source = engine.CreateNode(new Vector2(200, 200), "Source", [], ["Value"]);
		Node target = engine.CreateNode(new Vector2(500, 200), "Target", ["Source.Value"], []);
		engine.TryCreateLink(source.OutputPins[0].Id, target.InputPins[0].Id);
		Start();

		Vector2[] settled = [SizeOf(0), SizeOf(1)];
		harness.Step(10);

		CollectionAssert.AreEqual(
			settled,
			new[] { SizeOf(0), SizeOf(1) },
			"A node changed size while nothing happened to it.");

		Assert.AreEqual(0, ErrorsLastFrame(), "ImGui reported the graph's drawing as misuse.");
	}

	/// <summary>
	/// Both kinds of node in one graph, which is what a real one looks like: some referenced,
	/// some not.
	/// </summary>
	[TestMethod]
	public void AGraphMixingBothKindsOfNodeDrawsWithoutError()
	{
		Node source = engine.CreateNode(new Vector2(200, 150), "Source", [], ["Value"]);
		Node target = engine.CreateNode(new Vector2(500, 150), "Target", ["Source.Value"], []);
		engine.CreateNode(new Vector2(350, 400), "Lonely", [], []);
		engine.TryCreateLink(source.OutputPins[0].Id, target.InputPins[0].Id);
		Start();

		harness.Step(10);

		Assert.AreEqual(0, ErrorsLastFrame(), "ImGui reported the graph's drawing as misuse.");
	}
}
