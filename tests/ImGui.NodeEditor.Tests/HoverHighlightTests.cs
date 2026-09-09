// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor.Tests;

using System;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.NodeEditor;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives the renderer's hover highlighting through real frames, because hover is a thing ImNodes
/// only answers once a frame has been drawn: what is highlighted comes from where the pointer was
/// when the last frame ended, and neither half of that exists without running the frames.
/// </summary>
[TestClass]
public sealed class HoverHighlightTests
{
	private static readonly HarnessOptions Viewport = new() { Width = 1000, Height = 700 };

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
				Title = nameof(HoverHighlightTests),
				OnRender = _ => renderer.Render(engine, ImGui.GetContentRegionAvail()),
				SaveIniSettings = false,
			},
			Viewport);

		harness.Step(3);
	}

	/// <summary>Source → Middle → Target, drawn far enough apart that each can be pointed at alone.</summary>
	private (Node Source, Node Middle, Node Target) AChain()
	{
		Node source = engine.CreateNode(new Vector2(80, 120), "Source", [], ["Value"]);
		Node middle = engine.CreateNode(new Vector2(380, 120), "Middle", ["In"], ["Out"]);
		Node target = engine.CreateNode(new Vector2(680, 120), "Target", ["In"], []);
		engine.TryCreateLink(source.OutputPins[0].Id, middle.InputPins[0].Id);
		engine.TryCreateLink(middle.OutputPins[0].Id, target.InputPins[0].Id);
		return (source, middle, target);
	}

	/// <summary>Puts the pointer over a node and runs the frame that notices it, plus the one that acts on it.</summary>
	private void Hover(Node node)
	{
		Assert.IsTrue(renderer.TryGetNodeScreenRect(node.Id, out ScreenRect rect), $"{node.Name} has not been drawn.");
		harness.Mouse.MoveTo(rect.Centre.X, rect.Centre.Y);
		harness.Step(2);
	}

	[TestMethod]
	public void HoveringANodeHighlightsTheLinksThatMeetIt()
	{
		(_, Node middle, _) = AChain();
		Start();

		Hover(middle);

		Assert.AreEqual(middle.Id, renderer.HoveredNodeId);
		Assert.HasCount(2, renderer.HighlightedLinks, "Both of the middle node's links should be highlighted.");
		Assert.IsEmpty(renderer.HighlightedNodes, "Highlighting a node's links should not outline any node.");
	}

	[TestMethod]
	public void HoveringANodeHighlightsOnlyItsOwnLinks()
	{
		(Node source, _, _) = AChain();
		Start();

		Hover(source);

		Assert.HasCount(1, renderer.HighlightedLinks, "Only the link leaving the source node should be highlighted.");
	}

	[TestMethod]
	public void MovingOffANodeDropsTheHighlight()
	{
		(_, Node middle, _) = AChain();
		Start();
		Hover(middle);

		harness.Mouse.MoveTo(Viewport.Width - 5, Viewport.Height - 5);
		harness.Step(2);

		Assert.IsNull(renderer.HoveredNodeId);
		Assert.IsEmpty(renderer.HighlightedLinks);
	}

	[TestMethod]
	public void HighlightingCanBeTurnedOff()
	{
		(_, Node middle, _) = AChain();
		renderer.HighlightLinksOnNodeHover = false;
		Start();

		Hover(middle);

		Assert.AreEqual(middle.Id, renderer.HoveredNodeId, "The node was still hovered, only the highlight was off.");
		Assert.IsEmpty(renderer.HighlightedLinks);
	}

	/// <summary>
	/// The downstream option answers "what does this node end up affecting", so it reaches past the
	/// nodes the hovered one touches and stops at what feeds it.
	/// </summary>
	[TestMethod]
	public void HoveringANodeHighlightsEverythingDownstreamOfItWhenAsked()
	{
		(Node source, Node middle, Node target) = AChain();
		renderer.HighlightDownstreamOnNodeHover = true;
		Start();

		Hover(source);

		// Sorted before comparing: what is highlighted is a set, and it promises no order.
		Assert.AreSequenceEqual([middle.Id, target.Id], renderer.HighlightedNodes.Order());
		Assert.HasCount(2, renderer.HighlightedLinks);
	}

	[TestMethod]
	public void TheDownstreamHighlightLeavesOutWhatFeedsTheHoveredNode()
	{
		(_, Node middle, Node target) = AChain();
		renderer.HighlightDownstreamOnNodeHover = true;
		Start();

		Hover(middle);

		Assert.AreSequenceEqual([target.Id], renderer.HighlightedNodes.Order());
	}

	[TestMethod]
	public void NothingIsHighlightedDownstreamUnlessAsked()
	{
		(Node source, _, _) = AChain();
		Start();

		Hover(source);

		Assert.IsEmpty(renderer.HighlightedNodes);
	}

	/// <summary>
	/// A long link with a node parked in the middle of it, which is the arrangement the hovered link
	/// is supposed to be drawn out of: ImNodes puts every link under every node, so the middle of
	/// this one is behind the blocker until something draws it again.
	/// </summary>
	private (Link Link, Node Blocker) ALinkPassingBehindANode()
	{
		Node source = engine.CreateNode(new Vector2(60, 300), "Source", [], ["Value"]);
		Node target = engine.CreateNode(new Vector2(760, 300), "Target", ["In"], []);
		Node blocker = engine.CreateNode(new Vector2(400, 300), "Blocker", ["A"], ["B"]);
		LinkCreationResult created = engine.TryCreateLink(source.OutputPins[0].Id, target.InputPins[0].Id);
		Assert.IsTrue(created.Success, created.Message);

		Start();

		// Where the link runs is only known once it has been drawn, so the blocker is moved onto it
		// afterwards rather than guessed at: a test that assumed the geometry would be testing its
		// own arithmetic rather than the drawing.
		Assert.IsTrue(renderer.TryGetPinScreenPosition(source.OutputPins[0].Id, out Vector2 start));
		Assert.IsTrue(renderer.TryGetPinScreenPosition(target.InputPins[0].Id, out Vector2 end));
		Assert.IsTrue(renderer.TryGetNodeScreenRect(blocker.Id, out ScreenRect blockerRect));

		Vector2 shift = ((start + end) * 0.5f) - blockerRect.Centre;
		engine.UpdateNodePosition(blocker.Id, engine.Nodes.First(n => n.Id == blocker.Id).Position + shift);
		harness.Step(2);

		// Pointed at near the source end, where the link is in the open: ImNodes resolves a hovered
		// node before a hovered link, so the pointer cannot ask about the part behind the blocker.
		Assert.IsTrue(renderer.TryGetPinScreenPosition(source.OutputPins[0].Id, out start));
		Assert.IsTrue(renderer.TryGetPinScreenPosition(target.InputPins[0].Id, out end));
		Vector2 exposed = Vector2.Lerp(start, end, 0.12f);
		harness.Mouse.MoveTo(exposed.X, exposed.Y);

		Assert.IsTrue(
			harness.StepUntil(() => renderer.HoveredLinkId == created.Link!.Id, 5),
			"The pointer was placed on the link and ImNodes did not report it as hovered.");

		return (created.Link!, blocker);
	}

	/// <summary>How many pixels of a rectangle differ between two captures.</summary>
	private static int PixelsDiffering(CapturedFrame before, CapturedFrame after, ScreenRect rect)
	{
		int minX = Math.Max((int)Math.Ceiling(rect.Min.X), 0);
		int minY = Math.Max((int)Math.Ceiling(rect.Min.Y), 0);
		int maxX = Math.Min((int)Math.Floor(rect.Max.X), before.Width - 1);
		int maxY = Math.Min((int)Math.Floor(rect.Max.Y), before.Height - 1);

		int differing = 0;
		for (int y = minY; y <= maxY; y++)
		{
			for (int x = minX; x <= maxX; x++)
			{
				if (before.GetPixel(x, y) != after.GetPixel(x, y))
				{
					differing++;
				}
			}
		}

		return differing;
	}

	[TestMethod]
	public void TheHoveredLinkIsDrawnOverTheNodeItPassesBehind()
	{
		renderer.DrawHoveredLinkOnTop = false;
		(_, Node blocker) = ALinkPassingBehindANode();

		Assert.IsTrue(renderer.TryGetNodeScreenRect(blocker.Id, out ScreenRect rect));
		CapturedFrame hidden = harness.Capture();

		renderer.DrawHoveredLinkOnTop = true;
		harness.Step(2);
		CapturedFrame drawn = harness.Capture();

		Assert.IsGreaterThan(
			0,
			PixelsDiffering(hidden, drawn, rect),
			"Nothing was drawn over the node the hovered link passes behind.");
	}

	/// <summary>
	/// The other half of the previous test: without the option, the node the link passes behind is
	/// untouched, so what that test measures is the overlay and not the frame moving under it.
	/// </summary>
	[TestMethod]
	public void TheHoveredLinkStaysBehindTheNodeWhenTheOptionIsOff()
	{
		renderer.DrawHoveredLinkOnTop = false;
		(_, Node blocker) = ALinkPassingBehindANode();

		Assert.IsTrue(renderer.TryGetNodeScreenRect(blocker.Id, out ScreenRect rect));
		CapturedFrame first = harness.Capture();
		harness.Step(2);
		CapturedFrame second = harness.Capture();

		Assert.AreEqual(0, PixelsDiffering(first, second, rect));
	}
}
