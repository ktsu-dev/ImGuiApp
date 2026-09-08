// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGuiNodeEditor.Tests;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGuiNodeEditor;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives <see cref="NodeEditorRenderer.Zoom"/> through real frames, because that is the only place
/// it can be judged: ImNodes has no zoom, so this one is made of what the renderer writes into it
/// and what it reads back, and neither is visible without drawing.
/// </summary>
/// <remarks>
/// The invariant these are built around is that the engine never sees the zoom. Its positions and
/// sizes are what the force-directed layout runs on, and a space that changed whenever the user
/// zoomed would change what the layout's rest length, repulsion distance and overlap margin mean.
/// A transform applied on the way in and undone on the way out is easy to get subtly wrong in a way
/// that only shows up after many frames, so these run the frames.
/// </remarks>
[TestClass]
public sealed class ZoomTests
{
	private static readonly HarnessOptions Viewport = new() { Width = 900, Height = 700 };

	private readonly NodeEditorEngine engine = new();
	private readonly NodeEditorRenderer renderer = new();

	private ImGuiAppHarness harness = null!;

	[TestCleanup]
	public void TearDown() => harness?.Dispose();

	/// <summary>Starts a harness whose whole content is the graph the engine holds.</summary>
	private void Start()
	{
		harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Title = nameof(ZoomTests),
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

	/// <summary>Builds a graph of three linked nodes spread across the editor.</summary>
	private void BuildGraph()
	{
		Node source = engine.CreateNode(new Vector2(150, 150), "Source", [], ["Value"]);
		Node middle = engine.CreateNode(new Vector2(400, 300), "Middle", ["In"], ["Out"]);
		Node target = engine.CreateNode(new Vector2(650, 450), "Target", ["In"], []);

		engine.TryCreateLink(source.OutputPins[0].Id, middle.InputPins[0].Id);
		engine.TryCreateLink(middle.OutputPins[0].Id, target.InputPins[0].Id);
	}

	private Vector2[] Positions() => [.. engine.Nodes.Select(node => node.Position)];

	private Vector2[] Dimensions() => [.. engine.Nodes.Select(node => node.Dimensions)];

	/// <summary>
	/// Tests that zooming leaves the engine's positions exactly as they were, however many frames go
	/// by, which is what keeps the simulation in a space that does not move under it.
	/// </summary>
	[TestMethod]
	public void Zooming_LeavesTheEnginesPositionsAlone()
	{
		BuildGraph();
		Start();

		Vector2[] before = Positions();

		renderer.Zoom = 0.5f;
		harness.Step(30);

		Vector2[] after = Positions();
		for (int i = 0; i < before.Length; i++)
		{
			Assert.AreEqual(before[i].X, after[i].X, 0.5f, $"node {i} moved in the engine's space while only the view changed");
			Assert.AreEqual(before[i].Y, after[i].Y, 0.5f, $"node {i} moved in the engine's space while only the view changed");
		}
	}

	/// <summary>
	/// Tests that a node's size in the engine's space does not drift as the view is zoomed, which is
	/// the failure a transform undone once per frame instead of once per application invites.
	/// </summary>
	/// <remarks>
	/// A size only arrives from ImNodes when it has measured a new one, so a renderer that unscaled
	/// sizes unconditionally would unscale the same value again every frame: at a zoom of one half
	/// the nodes double every frame, and a graph left alone for a second is thousands of times its
	/// real size — enough geometry, in practice, to trip ImGui's assertion on 16-bit vertex indices.
	/// <para>
	/// A measurement taken while zoomed is not the node's size either, however it is scaled back:
	/// the font size is rounded to whole pixels and ImGui's own spacing inside the node does not
	/// scale, so what comes back depends on how far out the user was. A node already measured
	/// therefore keeps its size, and this asserts it exactly rather than within a tolerance.
	/// </para>
	/// </remarks>
	[TestMethod]
	public void Zooming_LeavesTheEnginesDimensionsAlone()
	{
		BuildGraph();
		Start();
		harness.Step(10);

		Vector2[] before = Dimensions();
		Assert.IsTrue(before.All(size => size.X > 0f), "the nodes should have been measured");

		renderer.Zoom = 0.5f;
		harness.Step(60);

		CollectionAssert.AreEqual(before, Dimensions(), "a node's size in the engine changed while only the view did");
	}

	/// <summary>
	/// Tests that zooming out and back in leaves the graph exactly where it started, so looking at
	/// something is not an edit to it.
	/// </summary>
	[TestMethod]
	public void ZoomingOutAndBackIn_LeavesTheGraphWhereItWas()
	{
		BuildGraph();
		Start();
		harness.Step(10);

		Vector2[] before = Positions();

		renderer.Zoom = 0.25f;
		harness.Step(20);
		renderer.Zoom = 2f;
		harness.Step(20);
		renderer.Zoom = 1f;
		harness.Step(20);

		Vector2[] after = Positions();
		for (int i = 0; i < before.Length; i++)
		{
			Assert.AreEqual(before[i].X, after[i].X, 0.5f, $"node {i} did not come back to where it was");
			Assert.AreEqual(before[i].Y, after[i].Y, 0.5f, $"node {i} did not come back to where it was");
		}
	}

	/// <summary>
	/// Tests that the zoom stays inside the range the renderer allows, however it is set.
	/// </summary>
	[TestMethod]
	public void Zoom_IsHeldWithinItsRange()
	{
		renderer.Zoom = 50f;
		Assert.AreEqual(NodeEditorRenderer.MaxZoom, renderer.Zoom);

		renderer.Zoom = -1f;
		Assert.AreEqual(NodeEditorRenderer.MinZoom, renderer.Zoom);
	}

	/// <summary>
	/// Tests that fitting a graph too big for the editor centres it and zooms out until it fits.
	/// </summary>
	[TestMethod]
	public void FitToView_CentresAndZoomsOutUntilTheGraphFits()
	{
		engine.CreateNode(new Vector2(0, 0), "First", [], ["Out"]);
		engine.CreateNode(new Vector2(2000, 1200), "Second", ["In"], []);
		Start();
		harness.Step(10);

		Vector2 editorSize = new(600, 400);
		Assert.IsTrue(renderer.FitToView(engine, editorSize));

		Vector2 lowest = new(float.MaxValue, float.MaxValue);
		Vector2 highest = new(float.MinValue, float.MinValue);
		foreach (Node node in engine.Nodes)
		{
			lowest = Vector2.Min(lowest, node.Position);
			highest = Vector2.Max(highest, node.Position + node.Dimensions);
		}

		Vector2 centre = (lowest + highest) * 0.5f;
		Assert.AreEqual(300f, centre.X, 0.01f, "the arrangement should be centred in the editor");
		Assert.AreEqual(200f, centre.Y, 0.01f, "the arrangement should be centred in the editor");

		Vector2 extent = highest - lowest;
		Assert.IsTrue(renderer.Zoom < 1f, $"a graph {extent.X} wide should not fit a 600 editor at {renderer.Zoom}");
		Assert.IsTrue(extent.X * renderer.Zoom <= editorSize.X, $"the graph still overflows at {renderer.Zoom}");
		Assert.IsTrue(extent.Y * renderer.Zoom <= editorSize.Y, $"the graph still overflows at {renderer.Zoom}");
	}

	/// <summary>
	/// Tests that fitting a graph the editor already has room for leaves it at its own size, since
	/// magnifying it is not what "fit" means to someone who asked to see all of it.
	/// </summary>
	[TestMethod]
	public void FitToView_DoesNotMagnifyAGraphThatAlreadyFits()
	{
		engine.CreateNode(new Vector2(0, 0), "First", [], ["Out"]);
		engine.CreateNode(new Vector2(40, 30), "Second", ["In"], []);
		Start();
		harness.Step(10);

		renderer.Zoom = 0.5f;
		Assert.IsTrue(renderer.FitToView(engine, new Vector2(2000, 1600)));

		Assert.AreEqual(1f, renderer.Zoom, 0.0001f);
	}

	/// <summary>
	/// Tests that fitting an empty graph reports that there was nothing to fit rather than choosing
	/// a zoom from no extent at all.
	/// </summary>
	[TestMethod]
	public void FitToView_ReportsAnEmptyGraph()
	{
		Start();

		Assert.IsFalse(renderer.FitToView(engine, new Vector2(600, 400)));
		Assert.AreEqual(1f, renderer.Zoom, 0.0001f);
	}

	/// <summary>
	/// Tests that drawing a zoomed graph is not reported by ImGui as misuse, which is the check the
	/// sibling rendering tests are built around.
	/// </summary>
	[TestMethod]
	public void AZoomedGraphDrawsWithoutError()
	{
		BuildGraph();
		Start();

		renderer.Zoom = 0.4f;
		harness.Step(10);

		Assert.AreEqual(0, ImGui.GetCurrentContext().ErrorCountCurrentFrame, "ImGui reported the zoomed drawing as misuse");
	}
}
