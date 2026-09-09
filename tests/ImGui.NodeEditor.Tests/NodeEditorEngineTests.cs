// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor.Tests;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using ktsu.ForceDirectedLayout;
using ktsu.ImGui.NodeEditor;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives <see cref="NodeEditorEngine"/> on its own. The engine owns the graph and its physics and
/// makes no ImGui calls, so everything here runs without a context, a window, or a harness.
/// </summary>
[TestClass]
public sealed class NodeEditorEngineTests
{
	private readonly NodeEditorEngine engine = new();

	private (Node Source, Node Target) TwoConnectedNodes()
	{
		Node source = engine.CreateNode(new Vector2(0, 0), "Source", [], ["Value"]);
		Node target = engine.CreateNode(new Vector2(300, 0), "Target", ["Input"], []);
		engine.TryCreateLink(source.OutputPins[0].Id, target.InputPins[0].Id);
		return (source, target);
	}

	[TestMethod]
	public void CreateNode_WithPinCounts_NumbersThePins()
	{
		Node node = engine.CreateNode(new Vector2(10, 20), "Counted", inputPinCount: 2, outputPinCount: 1);

		Assert.AreEqual("Counted", node.Name);
		Assert.AreEqual(new Vector2(10, 20), node.Position);
		Assert.HasCount(2, node.InputPins);
		Assert.HasCount(1, node.OutputPins);
		Assert.AreEqual("In 1", node.InputPins[0].EffectiveDisplayName);
		Assert.AreEqual("In 2", node.InputPins[1].EffectiveDisplayName);
		Assert.AreEqual("Out 1", node.OutputPins[0].EffectiveDisplayName);
	}

	[TestMethod]
	public void CreateNode_WithPinNames_KeepsTheNameForDisplayAndNumbersTheIdentity()
	{
		// The positional name is the pin's identity within its node; the caller's name is what a
		// renderer shows, which is why both are kept rather than one overwriting the other.
		Node node = engine.CreateNode(new Vector2(0, 0), "Named", ["Left"], ["Right"]);

		Assert.AreEqual("In 1", node.InputPins[0].Name);
		Assert.AreEqual("Left", node.InputPins[0].EffectiveDisplayName);
		Assert.AreEqual(PinDirection.Input, node.InputPins[0].Direction);
		Assert.AreEqual(PinDirection.Output, node.OutputPins[0].Direction);
	}

	[TestMethod]
	public void CreateNode_GivesEveryNodeAndPinADistinctId()
	{
		Node first = engine.CreateNode(new Vector2(0, 0), "First", ["A"], ["B"]);
		Node second = engine.CreateNode(new Vector2(0, 0), "Second", ["A"], ["B"]);

		Assert.AreNotEqual(first.Id, second.Id);
		int[] pinIds =
		[
			.. first.InputPins.Concat(first.OutputPins).Concat(second.InputPins).Concat(second.OutputPins).Select(p => p.Id)
		];

		Assert.AreEqual(pinIds.Length, pinIds.Distinct().Count(), "Pin ids collided across nodes.");
		Assert.HasCount(2, engine.Nodes);
	}

	[TestMethod]
	public void TryCreateLink_ConnectsAnOutputToAnInput()
	{
		Node source = engine.CreateNode(new Vector2(0, 0), "Source", [], ["Value"]);
		Node target = engine.CreateNode(new Vector2(300, 0), "Target", ["Input"], []);

		LinkCreationResult result = engine.TryCreateLink(source.OutputPins[0].Id, target.InputPins[0].Id);

		Assert.IsTrue(result.Success, result.Message);
		Assert.IsNotNull(result.Link);
		Assert.AreEqual(source.OutputPins[0].Id, result.Link.OutputPinId);
		Assert.AreEqual(target.InputPins[0].Id, result.Link.InputPinId);
		Assert.HasCount(1, engine.Links);
	}

	[TestMethod]
	public void TryCreateLink_AcceptsThePinsInEitherOrder()
	{
		Node source = engine.CreateNode(new Vector2(0, 0), "Source", [], ["Value"]);
		Node target = engine.CreateNode(new Vector2(300, 0), "Target", ["Input"], []);

		// Dragging from the input end is the same connection as dragging from the output end.
		LinkCreationResult result = engine.TryCreateLink(target.InputPins[0].Id, source.OutputPins[0].Id);

		Assert.IsTrue(result.Success, result.Message);
		Link created = result.Link ?? throw new AssertFailedException("A successful result carried no link.");
		Assert.AreEqual(source.OutputPins[0].Id, created.OutputPinId);
	}

	[TestMethod]
	public void TryCreateLink_RefusesAnUnknownPin()
	{
		Node node = engine.CreateNode(new Vector2(0, 0), "Only", ["In"], ["Out"]);

		LinkCreationResult result = engine.TryCreateLink(node.OutputPins[0].Id, 9999);

		Assert.IsFalse(result.Success);
		Assert.Contains("not found", result.Message);
		Assert.IsNull(result.Link);
		Assert.IsEmpty(engine.Links);
	}

	[TestMethod]
	public void TryCreateLink_RefusesTwoPinsOfTheSameDirection()
	{
		Node first = engine.CreateNode(new Vector2(0, 0), "First", ["In"], ["Out"]);
		Node second = engine.CreateNode(new Vector2(300, 0), "Second", ["In"], ["Out"]);

		LinkCreationResult result = engine.TryCreateLink(first.OutputPins[0].Id, second.OutputPins[0].Id);

		Assert.IsFalse(result.Success);
		Assert.Contains("Cannot connect", result.Message);
	}

	[TestMethod]
	public void TryCreateLink_RefusesASecondConnectionIntoOneInput()
	{
		Node first = engine.CreateNode(new Vector2(0, 0), "First", [], ["Out"]);
		Node second = engine.CreateNode(new Vector2(150, 0), "Second", [], ["Out"]);
		Node target = engine.CreateNode(new Vector2(300, 0), "Target", ["In"], []);

		engine.TryCreateLink(first.OutputPins[0].Id, target.InputPins[0].Id);
		LinkCreationResult result = engine.TryCreateLink(second.OutputPins[0].Id, target.InputPins[0].Id);

		Assert.IsFalse(result.Success);
		Assert.Contains("already connected", result.Message);
		Assert.HasCount(1, engine.Links);
	}

	/// <summary>
	/// One value can feed as many consumers as want it, which is the whole point of a graph rather
	/// than a chain.
	/// </summary>
	[TestMethod]
	public void TryCreateLink_LetsOneOutputFeedSeveralInputs()
	{
		Node source = engine.CreateNode(new Vector2(0, 0), "Source", [], ["Value"]);
		Node first = engine.CreateNode(new Vector2(300, 0), "First", ["Input"], []);
		Node second = engine.CreateNode(new Vector2(300, 200), "Second", ["Input"], []);
		Node third = engine.CreateNode(new Vector2(300, 400), "Third", ["Input"], []);

		LinkCreationResult[] results =
		[
			engine.TryCreateLink(source.OutputPins[0].Id, first.InputPins[0].Id),
			engine.TryCreateLink(source.OutputPins[0].Id, second.InputPins[0].Id),
			engine.TryCreateLink(source.OutputPins[0].Id, third.InputPins[0].Id),
		];

		Assert.IsTrue(results.All(r => r.Success), string.Join("; ", results.Select(r => r.Message)));
		Assert.HasCount(3, engine.Links);
	}

	[TestMethod]
	public void TryCreateLink_RefusesTheSamePairTwice()
	{
		Node source = engine.CreateNode(new Vector2(0, 0), "Source", [], ["Value"]);
		Node target = engine.CreateNode(new Vector2(300, 0), "Target", ["Input"], []);

		engine.TryCreateLink(source.OutputPins[0].Id, target.InputPins[0].Id);
		LinkCreationResult result = engine.TryCreateLink(source.OutputPins[0].Id, target.InputPins[0].Id);

		Assert.IsFalse(result.Success);
		Assert.HasCount(1, engine.Links);
	}

	/// <summary>
	/// An output that has been narrowed to one connection keeps the one it has and refuses the next,
	/// which is how a declared <c>[OutputPin(AllowMultipleConnections = false)]</c> reads once the
	/// factory has carried it onto the pin.
	/// </summary>
	[TestMethod]
	public void TryCreateLink_RefusesASecondLinkFromAnOutputNarrowedToOne()
	{
		Node source = engine.CreateNode(new Vector2(0, 0), "Source", [], ["Value"]);
		Node first = engine.CreateNode(new Vector2(300, 0), "First", ["Input"], []);
		Node second = engine.CreateNode(new Vector2(300, 200), "Second", ["Input"], []);

		Assert.IsTrue(engine.SetPinAllowsMultipleConnections(source.OutputPins[0].Id, allowMultiple: false));
		engine.TryCreateLink(source.OutputPins[0].Id, first.InputPins[0].Id);
		LinkCreationResult result = engine.TryCreateLink(source.OutputPins[0].Id, second.InputPins[0].Id);

		Assert.IsFalse(result.Success);
		Assert.Contains("already connected", result.Message);
		Assert.HasCount(1, engine.Links);
	}

	[TestMethod]
	public void TryCreateLink_AcceptsASecondLinkIntoAnInputThatAllowsThem()
	{
		Node first = engine.CreateNode(new Vector2(0, 0), "First", [], ["Value"]);
		Node second = engine.CreateNode(new Vector2(0, 200), "Second", [], ["Value"]);
		Node target = engine.CreateNode(new Vector2(300, 0), "Target", ["Any"], []);

		engine.SetPinAllowsMultipleConnections(target.InputPins[0].Id, allowMultiple: true);
		engine.TryCreateLink(first.OutputPins[0].Id, target.InputPins[0].Id);
		LinkCreationResult result = engine.TryCreateLink(second.OutputPins[0].Id, target.InputPins[0].Id);

		Assert.IsTrue(result.Success, result.Message);
		Assert.HasCount(2, engine.Links);
	}

	[TestMethod]
	public void SetPinAllowsMultipleConnections_ReportsAPinItDoesNotHave() =>
		Assert.IsFalse(engine.SetPinAllowsMultipleConnections(9999, allowMultiple: true));

	[TestMethod]
	public void PinsDefaultToOneLinkIntoAnInputAndManyOutOfAnOutput()
	{
		Node node = engine.CreateNode(new Vector2(0, 0), "Node", ["In"], ["Out"]);

		Assert.IsFalse(node.InputPins[0].AllowsMultipleConnections);
		Assert.IsTrue(node.OutputPins[0].AllowsMultipleConnections);
	}

	[TestMethod]
	public void TryCreateLink_RefusesANodeConnectedToItself()
	{
		Node node = engine.CreateNode(new Vector2(0, 0), "Loop", ["In"], ["Out"]);

		LinkCreationResult result = engine.TryCreateLink(node.OutputPins[0].Id, node.InputPins[0].Id);

		Assert.IsFalse(result.Success);
		Assert.Contains("itself", result.Message);
		Assert.IsEmpty(engine.Links);
	}

	[TestMethod]
	public void RemoveLink_ReportsWhetherItRemovedAnything()
	{
		TwoConnectedNodes();
		int linkId = engine.Links[0].Id;

		Assert.IsTrue(engine.RemoveLink(linkId));
		Assert.IsEmpty(engine.Links);
		Assert.IsFalse(engine.RemoveLink(linkId), "Removing the same link twice should report nothing was removed.");
	}

	[TestMethod]
	public void RemoveNode_TakesItsLinksWithIt()
	{
		(Node source, Node target) = TwoConnectedNodes();

		Assert.IsTrue(engine.RemoveNode(source.Id));
		Assert.HasCount(1, engine.Nodes);
		Assert.IsEmpty(engine.Links, "The link into the surviving node outlived the node it came from.");
		Assert.AreEqual(target.Id, engine.Nodes[0].Id);
		Assert.IsFalse(engine.RemoveNode(source.Id));
	}

	[TestMethod]
	public void UpdateNodePositionAndDimensions_ReplaceTheRecordInPlace()
	{
		Node node = engine.CreateNode(new Vector2(0, 0), "Measured", ["In"], ["Out"]);

		engine.UpdateNodePosition(node.Id, new Vector2(120, 40));
		engine.UpdateNodeDimensions(node.Id, new Vector2(200, 80));

		Node updated = engine.Nodes.Single(n => n.Id == node.Id);
		Assert.AreEqual(new Vector2(120, 40), updated.Position);
		Assert.AreEqual(new Vector2(200, 80), updated.Dimensions);
		Assert.AreEqual(node.Name, updated.Name, "Updating layout should not disturb the rest of the node.");
	}

	[TestMethod]
	public void UpdateNodePosition_IgnoresAnUnknownNode()
	{
		engine.CreateNode(new Vector2(0, 0), "Only", [], []);

		engine.UpdateNodePosition(4242, new Vector2(1, 1));
		engine.UpdateNodeDimensions(4242, new Vector2(1, 1));

		Assert.AreEqual(new Vector2(0, 0), engine.Nodes[0].Position);
	}

	[TestMethod]
	public void ToggleNodePinned_FlipsTheFlag()
	{
		Node node = engine.CreateNode(new Vector2(0, 0), "Pinned", [], []);
		Assert.IsFalse(node.IsPinned);

		engine.ToggleNodePinned(node.Id);
		Assert.IsTrue(engine.Nodes[0].IsPinned);

		engine.ToggleNodePinned(node.Id);
		Assert.IsFalse(engine.Nodes[0].IsPinned);
	}

	[TestMethod]
	public void GetNodeLinks_ReturnsBothEndsOfAConnection()
	{
		(Node source, Node target) = TwoConnectedNodes();

		Assert.HasCount(1, engine.GetNodeLinks(source.Id));
		Assert.HasCount(1, engine.GetNodeLinks(target.Id));
		Assert.IsEmpty(engine.GetNodeLinks(9999));
	}

	[TestMethod]
	public void GetLinkDistance_MeasuresBetweenNodeCentres()
	{
		Node source = engine.CreateNode(new Vector2(0, 0), "Source", [], ["Out"]);
		Node target = engine.CreateNode(new Vector2(300, 0), "Target", ["In"], []);
		engine.UpdateNodeDimensions(source.Id, new Vector2(100, 50));
		engine.UpdateNodeDimensions(target.Id, new Vector2(100, 50));
		engine.TryCreateLink(source.OutputPins[0].Id, target.InputPins[0].Id);

		if (engine.GetLinkDistance(engine.Links[0].Id) is not float distance)
		{
			Assert.Fail("The link reported no distance.");
			return;
		}

		Assert.AreEqual(300f, distance, 0.001f, "Equal dimensions cancel out, leaving the position delta.");
		Assert.IsNull(engine.GetLinkDistance(9999));
	}

	[TestMethod]
	public void GetLinkStress_IsZeroAtRestLength()
	{
		Node source = engine.CreateNode(new Vector2(0, 0), "Source", [], ["Out"]);
		Node target = engine.CreateNode(new Vector2((float)engine.PhysicsSettings.RestLinkLength, 0), "Target", ["In"], []);
		engine.TryCreateLink(source.OutputPins[0].Id, target.InputPins[0].Id);

		if (engine.GetLinkStress(engine.Links[0].Id) is not float stress)
		{
			Assert.Fail("The link reported no stress.");
			return;
		}

		Assert.AreEqual(0f, stress, 0.001f);
		Assert.IsNull(engine.GetLinkStress(9999));
	}

	[TestMethod]
	public void GetLinkStress_IsOneWhenStretchedToTwiceRestLength()
	{
		Node source = engine.CreateNode(new Vector2(0, 0), "Source", [], ["Out"]);
		Node target = engine.CreateNode(new Vector2((float)engine.PhysicsSettings.RestLinkLength * 2f, 0), "Target", ["In"], []);
		engine.TryCreateLink(source.OutputPins[0].Id, target.InputPins[0].Id);

		if (engine.GetLinkStress(engine.Links[0].Id) is not float stress)
		{
			Assert.Fail("The link reported no stress.");
			return;
		}

		Assert.AreEqual(1f, stress, 0.001f);
	}

	[TestMethod]
	public void Clear_EmptiesTheGraphAndRestartsTheIds()
	{
		TwoConnectedNodes();

		engine.Clear();

		Assert.IsEmpty(engine.Nodes);
		Assert.IsEmpty(engine.Links);

		Node afterClear = engine.CreateNode(new Vector2(0, 0), "Fresh", ["In"], []);
		Assert.AreEqual(1, afterClear.Id, "Ids should start over so a reloaded graph is addressed the same way twice.");
	}

	[TestMethod]
	public void UpdatePhysics_MovesUnpinnedNodesApartOnceEnabled()
	{
		// Physics is off until an application asks for it, so a host that only wants a graph it
		// positions itself never pays for the simulation.
		Assert.IsFalse(engine.PhysicsSettings.Enabled, "The simulation should be opt-in.");

		Node first = engine.CreateNode(new Vector2(0, 0), "First", [], ["Out"]);
		Node second = engine.CreateNode(new Vector2(10, 0), "Second", ["In"], []);
		engine.UpdateNodeDimensions(first.Id, new Vector2(100, 50));
		engine.UpdateNodeDimensions(second.Id, new Vector2(100, 50));
		engine.UpdatePhysicsSettings(new PhysicsSettings { Enabled = true });

		for (int frame = 0; frame < 10; frame++)
		{
			engine.UpdatePhysics(1f / 60f);
		}

		float separation = Vector2.Distance(engine.Nodes[0].Position, engine.Nodes[1].Position);
		Assert.IsGreaterThan(10f, separation, "Overlapping nodes should repel.");
		Assert.IsGreaterThan(0, engine.LastPhysicsStepInfo.SubstepCount);
		Assert.IsGreaterThan(0f, engine.LastPhysicsStepInfo.SubstepDeltaTime);
		Assert.IsGreaterThan(0f, engine.TotalSystemEnergy);
	}

	[TestMethod]
	public void UpdatePhysics_DoesNothingWithoutNodes()
	{
		engine.UpdatePhysics(1f / 60f);

		Assert.IsEmpty(engine.Nodes);
		Assert.AreEqual(0, engine.LastPhysicsStepInfo.SubstepCount);
	}

	[TestMethod]
	public void SetDraggedNodes_HoldsThoseNodesStill()
	{
		Node dragged = engine.CreateNode(new Vector2(0, 0), "Dragged", [], ["Out"]);
		engine.CreateNode(new Vector2(10, 0), "Free", ["In"], []);
		engine.UpdateNodeDimensions(dragged.Id, new Vector2(100, 50));

		engine.UpdatePhysicsSettings(new PhysicsSettings { Enabled = true });
		engine.SetDraggedNodes(new HashSet<int> { dragged.Id });

		for (int frame = 0; frame < 10; frame++)
		{
			engine.UpdatePhysics(1f / 60f);
		}

		Assert.AreEqual(new Vector2(0, 0), engine.Nodes.Single(n => n.Id == dragged.Id).Position);
	}

	[TestMethod]
	public void UpdatePhysicsSettings_TakesEffectOnTheNextStep()
	{
		engine.CreateNode(new Vector2(0, 0), "First", [], ["Out"]);
		engine.CreateNode(new Vector2(10, 0), "Second", ["In"], []);
		engine.UpdatePhysicsSettings(new PhysicsSettings { Enabled = true, RepulsionStrength = 2_000_000.0 });

		Assert.IsTrue(engine.PhysicsSettings.Enabled);
		Assert.AreEqual(2_000_000.0, engine.PhysicsSettings.RepulsionStrength);

		engine.UpdatePhysicsSettings(new PhysicsSettings { Enabled = false });
		Vector2 before = engine.Nodes[0].Position;
		engine.UpdatePhysics(1f / 60f);

		Assert.AreEqual(before, engine.Nodes[0].Position, "A disabled simulation should not move anything.");
		Assert.IsFalse(engine.PhysicsSettings.Enabled);
	}

	[TestMethod]
	public void InitializeWorldOriginToCentroid_RunsOverTheCurrentNodes()
	{
		engine.CreateNode(new Vector2(-100, 0), "Left", [], []);
		engine.CreateNode(new Vector2(100, 0), "Right", [], []);

		engine.InitializeWorldOriginToCentroid();
		engine.UpdatePhysicsSettings(new PhysicsSettings { Enabled = true });
		engine.UpdatePhysics(1f / 60f);

		// The centroid is what gravity pulls toward, so it should sit between the two nodes.
		Assert.AreEqual(0f, engine.GravityCenter.X, 150f);
		Assert.AreEqual(0f, engine.GravityCenter.Y, 150f);
	}

	/// <summary>
	/// The reach of a node is what its value ends up affecting, which is the question behind
	/// highlighting everything downstream of the node under the pointer.
	/// </summary>
	[TestMethod]
	public void GetDownstream_FollowsLinksForwardThroughTheWholeChain()
	{
		Node first = engine.CreateNode(new Vector2(0, 0), "First", [], ["Out"]);
		Node second = engine.CreateNode(new Vector2(300, 0), "Second", ["In"], ["Out"]);
		Node third = engine.CreateNode(new Vector2(600, 0), "Third", ["In"], []);
		engine.TryCreateLink(first.OutputPins[0].Id, second.InputPins[0].Id);
		engine.TryCreateLink(second.OutputPins[0].Id, third.InputPins[0].Id);

		GraphReach reach = engine.GetDownstream(first.Id);

		CollectionAssert.AreEquivalent(new[] { second.Id, third.Id }, reach.NodeIds.ToArray());
		Assert.HasCount(2, reach.LinkIds);
	}

	[TestMethod]
	public void GetDownstream_LeavesOutWhatFeedsTheNode()
	{
		Node upstream = engine.CreateNode(new Vector2(0, 0), "Upstream", [], ["Out"]);
		Node middle = engine.CreateNode(new Vector2(300, 0), "Middle", ["In"], ["Out"]);
		Node downstream = engine.CreateNode(new Vector2(600, 0), "Downstream", ["In"], []);
		engine.TryCreateLink(upstream.OutputPins[0].Id, middle.InputPins[0].Id);
		engine.TryCreateLink(middle.OutputPins[0].Id, downstream.InputPins[0].Id);

		GraphReach reach = engine.GetDownstream(middle.Id);

		CollectionAssert.AreEquivalent(new[] { downstream.Id }, reach.NodeIds.ToArray());
		Assert.HasCount(1, reach.LinkIds);
	}

	[TestMethod]
	public void GetDownstream_FollowsEveryBranchOfAFannedOutput()
	{
		Node source = engine.CreateNode(new Vector2(0, 0), "Source", [], ["Out"]);
		Node left = engine.CreateNode(new Vector2(300, 0), "Left", ["In"], []);
		Node right = engine.CreateNode(new Vector2(300, 200), "Right", ["In"], []);
		engine.TryCreateLink(source.OutputPins[0].Id, left.InputPins[0].Id);
		engine.TryCreateLink(source.OutputPins[0].Id, right.InputPins[0].Id);

		GraphReach reach = engine.GetDownstream(source.Id);

		CollectionAssert.AreEquivalent(new[] { left.Id, right.Id }, reach.NodeIds.ToArray());
		Assert.HasCount(2, reach.LinkIds);
	}

	/// <summary>
	/// A cycle terminates the walk rather than running it forever, and the node it leads back to is
	/// reported: a node that feeds itself round a loop is downstream of itself.
	/// </summary>
	[TestMethod]
	public void GetDownstream_WalksACycleOnce()
	{
		Node first = engine.CreateNode(new Vector2(0, 0), "First", ["In"], ["Out"]);
		Node second = engine.CreateNode(new Vector2(300, 0), "Second", ["In"], ["Out"]);
		engine.TryCreateLink(first.OutputPins[0].Id, second.InputPins[0].Id);
		engine.TryCreateLink(second.OutputPins[0].Id, first.InputPins[0].Id);

		GraphReach reach = engine.GetDownstream(first.Id);

		CollectionAssert.AreEquivalent(new[] { first.Id, second.Id }, reach.NodeIds.ToArray());
		Assert.HasCount(2, reach.LinkIds);
	}

	[TestMethod]
	public void GetDownstream_OfANodeThatFeedsNothingIsEmpty()
	{
		Node node = engine.CreateNode(new Vector2(0, 0), "Alone", ["In"], ["Out"]);

		GraphReach reach = engine.GetDownstream(node.Id);

		Assert.IsEmpty(reach.NodeIds);
		Assert.IsEmpty(reach.LinkIds);
	}

	[TestMethod]
	public void GetOutgoingAndIncomingLinks_SplitTheLinksAtANodeByDirection()
	{
		Node source = engine.CreateNode(new Vector2(0, 0), "Source", [], ["Out"]);
		Node middle = engine.CreateNode(new Vector2(300, 0), "Middle", ["In"], ["Out"]);
		Node target = engine.CreateNode(new Vector2(600, 0), "Target", ["In"], []);
		engine.TryCreateLink(source.OutputPins[0].Id, middle.InputPins[0].Id);
		engine.TryCreateLink(middle.OutputPins[0].Id, target.InputPins[0].Id);

		Assert.HasCount(1, engine.GetIncomingLinks(middle.Id).ToList());
		Assert.HasCount(1, engine.GetOutgoingLinks(middle.Id).ToList());
		Assert.IsEmpty(engine.GetIncomingLinks(source.Id).ToList());
		Assert.IsEmpty(engine.GetOutgoingLinks(target.Id).ToList());
	}
}
