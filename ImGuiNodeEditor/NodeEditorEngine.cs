// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiNodeEditor;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ktsu.ForceDirectedLayout;
using ktsu.Semantics;

/// <summary>
/// Core business logic for the node editor - completely independent of ImNodes.
/// Force-directed physics is delegated to <see cref="ForceDirectedLayout{TBody, TEdge}"/>.
/// </summary>
public class NodeEditorEngine
{
	private readonly List<Node> nodes = [];
	private readonly List<Link> links = [];
	private readonly Dictionary<int, int> pinIdToNodeId = [];
	private readonly HashSet<int> draggedNodeIds = [];
	private int nextNodeId = 1;
	private int nextLinkId = 1;
	private int nextPinId = 1;

	private readonly ForceDirectedLayout<Node, Link> layout;

	/// <summary>
	/// Create a new node editor engine with default physics settings.
	/// </summary>
	public NodeEditorEngine()
	{
		BodyAccessor<Node> bodyAccessor = new(
			GetId: n => n.Id,
			GetPosition: n => n.Position,
			GetDimensions: n => n.Dimensions,
			GetVelocity: n => n.Velocity,
			GetForce: n => n.Force,
			GetIsPinned: n => n.IsPinned,
			WithPhysicsState: (n, pos, vel, force) => n with { Position = pos, Velocity = vel, Force = force }
		);

		EdgeAccessor<Link> edgeAccessor = new(
			GetSourceBodyId: l => pinIdToNodeId.TryGetValue(l.OutputPinId, out int id) ? id : -1,
			GetTargetBodyId: l => pinIdToNodeId.TryGetValue(l.InputPinId, out int id) ? id : -1
		);

		layout = new ForceDirectedLayout<Node, Link>(bodyAccessor, edgeAccessor);
	}

	/// <inheritdoc/>
	public IReadOnlyList<Node> Nodes => nodes.AsReadOnly();
	/// <inheritdoc/>
	public IReadOnlyList<Link> Links => links.AsReadOnly();

	/// <summary>Current physics simulation settings. Update via <see cref="UpdatePhysicsSettings"/>.</summary>
	public PhysicsSettings PhysicsSettings => layout.Settings;

	/// <summary>Computed gravity target (blend of centroid and world origin), published for debug rendering.</summary>
	public Vector2 GravityCenter => layout.GravityCenter;

	/// <summary>
	/// World origin in node-position space. Tracks with uniform node shifts (panning)
	/// so it stays in the same coordinate space as node positions.
	/// </summary>
	public Vector2 WorldOrigin
	{
		get => layout.WorldOrigin;
		set => layout.WorldOrigin = value;
	}

	/// <summary>Current physics simulation info (for debugging).</summary>
	public (int SubstepCount, float SubstepDeltaTime) LastPhysicsStepInfo => layout.LastStepInfo;

	/// <summary>Total kinetic energy in the system (sum of velocity squared for all nodes).</summary>
	public float TotalSystemEnergy => layout.TotalSystemEnergy;

	/// <summary>Whether the physics simulation has settled (total energy below threshold).</summary>
	public bool IsStable => layout.IsStable;

	/// <summary>
	/// Create a new node with the specified number of input and output pins.
	/// </summary>
	public Node CreateNode(Vector2 position, string name, int inputPinCount, int outputPinCount)
	{
		List<string> inputPinNames = [];
		List<string> outputPinNames = [];

		for (int i = 0; i < inputPinCount; i++)
		{
			inputPinNames.Add($"In {i + 1}");
		}

		for (int i = 0; i < outputPinCount; i++)
		{
			outputPinNames.Add($"Out {i + 1}");
		}

		return CreateNode(position, name, inputPinNames, outputPinNames);
	}

	/// <summary>
	/// Create a new node with specified pin names.
	/// </summary>
	public Node CreateNode(Vector2 position, string name, List<string> inputPinNames, List<string> outputPinNames)
	{
		List<Pin> inputPins = [];
		List<Pin> outputPins = [];

		foreach (string pinName in inputPinNames)
		{
			inputPins.Add(new Pin(nextPinId++, PinDirection.Input, $"In {inputPins.Count + 1}", pinName));
		}

		foreach (string pinName in outputPinNames)
		{
			outputPins.Add(new Pin(nextPinId++, PinDirection.Output, $"Out {outputPins.Count + 1}", pinName));
		}

		Node node = new(nextNodeId++, position, name, inputPins, outputPins);
		nodes.Add(node);
		return node;
	}

	/// <summary>
	/// Attempt to create a link between two pins.
	/// </summary>
	public LinkCreationResult TryCreateLink(int fromPinId, int toPinId)
	{
		Pin? fromPin = FindPin(fromPinId);
		Pin? toPin = FindPin(toPinId);

		if (fromPin == null || toPin == null)
		{
			return new LinkCreationResult(false, "One or both pins not found");
		}

		if (fromPin.Direction == toPin.Direction)
		{
			return new LinkCreationResult(false, $"Cannot connect {fromPin.Direction} to {toPin.Direction}");
		}

		Pin outputPin = fromPin.Direction == PinDirection.Output ? fromPin : toPin;
		Pin inputPin = fromPin.Direction == PinDirection.Input ? fromPin : toPin;

		// Only one input connection per pin.
		if (links.Any(l => l.InputPinId == inputPin.Id))
		{
			return new LinkCreationResult(false, "Input pin already connected");
		}

		Node? outputNode = FindNodeByPin(outputPin.Id);
		Node? inputNode = FindNodeByPin(inputPin.Id);

		if (outputNode?.Id == inputNode?.Id)
		{
			return new LinkCreationResult(false, "Cannot connect node to itself");
		}

		Link link = new(nextLinkId++, outputPin.Id, inputPin.Id);
		links.Add(link);

		return new LinkCreationResult(true, "Link created successfully", link);
	}

	/// <summary>
	/// Remove a link by ID.
	/// </summary>
	public bool RemoveLink(int linkId)
	{
		Link? link = links.FirstOrDefault(l => l.Id == linkId);
		if (link != null)
		{
			links.Remove(link);
			return true;
		}
		return false;
	}

	/// <summary>
	/// Remove a node and all its connected links.
	/// </summary>
	public bool RemoveNode(int nodeId)
	{
		Node? node = nodes.FirstOrDefault(n => n.Id == nodeId);
		if (node == null)
		{
			return false;
		}

		List<Link> connectedLinks = [.. links.Where(l =>
			node.InputPins.Any(p => p.Id == l.InputPinId) ||
			node.OutputPins.Any(p => p.Id == l.OutputPinId))];

		foreach (Link? link in connectedLinks)
		{
			links.Remove(link);
		}

		nodes.Remove(node);
		return true;
	}

	/// <summary>
	/// Update a node's position.
	/// </summary>
	public void UpdateNodePosition(int nodeId, Vector2 newPosition)
	{
		Node? node = nodes.FirstOrDefault(n => n.Id == nodeId);
		if (node != null)
		{
			Node updatedNode = node with { Position = newPosition };
			int index = nodes.IndexOf(node);
			nodes[index] = updatedNode;
		}
	}

	/// <summary>
	/// Update a node's dimensions.
	/// </summary>
	public void UpdateNodeDimensions(int nodeId, Vector2 newDimensions)
	{
		Node? node = nodes.FirstOrDefault(n => n.Id == nodeId);
		if (node != null)
		{
			Node updatedNode = node with { Dimensions = newDimensions };
			int index = nodes.IndexOf(node);
			nodes[index] = updatedNode;
		}
	}

	/// <summary>
	/// Get all links connected to a specific node.
	/// </summary>
	public IEnumerable<Link> GetNodeLinks(int nodeId)
	{
		Node? node = nodes.FirstOrDefault(n => n.Id == nodeId);
		if (node == null)
		{
			return [];
		}

		return links.Where(l =>
			node.InputPins.Any(p => p.Id == l.InputPinId) ||
			node.OutputPins.Any(p => p.Id == l.OutputPinId));
	}

	/// <summary>
	/// Calculate the distance between two connected nodes.
	/// </summary>
	public float? GetLinkDistance(int linkId)
	{
		Link? link = links.FirstOrDefault(l => l.Id == linkId);
		if (link == null)
		{
			return null;
		}

		Node? outputNode = FindNodeByPin(link.OutputPinId);
		Node? inputNode = FindNodeByPin(link.InputPinId);

		if (outputNode == null || inputNode == null)
		{
			return null;
		}

		Vector2 outputCenter = outputNode.Position + (outputNode.Dimensions * 0.5f);
		Vector2 inputCenter = inputNode.Position + (inputNode.Dimensions * 0.5f);
		return Vector2.Distance(outputCenter, inputCenter);
	}

	/// <summary>
	/// Get the normalized stress of a link: negative = compression, positive = tension.
	/// Value of 0 means the link is at rest length. Value of 1 means stretched to 2x rest length.
	/// </summary>
	public float? GetLinkStress(int linkId)
	{
		float? distance = GetLinkDistance(linkId);
		if (!distance.HasValue)
		{
			return null;
		}

		float restLength = PhysicsSettings.RestLinkLength.In(Units.Meter);
		if (restLength < 0.1f)
		{
			return null;
		}

		return (distance.Value - restLength) / restLength;
	}

	/// <summary>
	/// Set the world origin to the centroid of all current node positions.
	/// Call after populating nodes so gravity doesn't immediately pull them toward (0,0).
	/// </summary>
	public void InitializeWorldOriginToCentroid() => layout.InitializeWorldOriginToCentroid(nodes);

	/// <summary>
	/// Clear all nodes and links.
	/// </summary>
	public void Clear()
	{
		nodes.Clear();
		links.Clear();
		nextNodeId = 1;
		nextLinkId = 1;
		nextPinId = 1;
		layout.WorldOrigin = Vector2.Zero;
	}

	/// <summary>
	/// Toggle whether a node is pinned (frozen during physics simulation).
	/// </summary>
	public void ToggleNodePinned(int nodeId)
	{
		Node? node = nodes.FirstOrDefault(n => n.Id == nodeId);
		if (node != null)
		{
			int index = nodes.IndexOf(node);
			nodes[index] = node with { IsPinned = !node.IsPinned };
		}
	}

	/// <summary>
	/// Set which nodes are currently being dragged by the user.
	/// Dragged nodes are temporarily excluded from physics simulation.
	/// </summary>
	public void SetDraggedNodes(IReadOnlySet<int> nodeIds)
	{
		draggedNodeIds.Clear();
		foreach (int id in nodeIds)
		{
			draggedNodeIds.Add(id);
		}
		layout.SetFrozenBodies(draggedNodeIds);
	}

	/// <summary>
	/// Replace the current physics settings.
	/// </summary>
	public void UpdatePhysicsSettings(PhysicsSettings newSettings) => layout.Settings = newSettings;

	/// <summary>
	/// Advance the force-directed layout simulation by one frame.
	/// </summary>
	public void UpdatePhysics(float deltaTime)
	{
		if (nodes.Count == 0)
		{
			return;
		}

		// Rebuild pin -> node-id map so the edge accessor can resolve link endpoints to body ids.
		RebuildPinIdToNodeIdMap();

		layout.Step(nodes, links, deltaTime);
	}

	private void RebuildPinIdToNodeIdMap()
	{
		pinIdToNodeId.Clear();
		foreach (Node node in nodes)
		{
			foreach (Pin pin in node.InputPins)
			{
				pinIdToNodeId[pin.Id] = node.Id;
			}
			foreach (Pin pin in node.OutputPins)
			{
				pinIdToNodeId[pin.Id] = node.Id;
			}
		}
	}

	private Pin? FindPin(int pinId)
	{
		foreach (Node node in nodes)
		{
			Pin? pin = node.InputPins.FirstOrDefault(p => p.Id == pinId) ??
					  node.OutputPins.FirstOrDefault(p => p.Id == pinId);
			if (pin != null)
			{
				return pin;
			}
		}
		return null;
	}

	private Node? FindNodeByPin(int pinId)
	{
		return nodes.FirstOrDefault(node =>
			node.InputPins.Any(p => p.Id == pinId) ||
			node.OutputPins.Any(p => p.Id == pinId));
	}
}

/// <summary>
/// Result of attempting to create a link.
/// </summary>
public record LinkCreationResult(bool Success, string Message, Link? Link = null);
