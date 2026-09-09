// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ktsu.ForceDirectedLayout;

/// <summary>
/// Core business logic for the node editor - completely independent of ImNodes.
/// Force-directed physics is delegated to <see cref="ForceDirectedLayout{TBody, TEdge}"/>,
/// which operates in double precision. Node positions remain float-precision <see cref="Vector2"/>
/// to match the surrounding ImGui/ImNodes ecosystem; conversion happens at the accessor boundary.
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
			GetPosition: n => ToVec2D(n.Position),
			GetDimensions: n => ToVec2D(n.Dimensions),
			GetVelocity: n => ToVec2D(n.Velocity),
			GetForce: n => ToVec2D(n.Force),
			GetIsPinned: n => n.IsPinned,
			WithPhysicsState: (n, pos, vel, force) => n with
			{
				Position = ToVector2(pos),
				Velocity = ToVector2(vel),
				Force = ToVector2(force),
			}
		);

		EdgeAccessor<Link> edgeAccessor = new(
			GetSourceBodyId: l => pinIdToNodeId.TryGetValue(l.OutputPinId, out int id) ? id : -1,
			GetTargetBodyId: l => pinIdToNodeId.TryGetValue(l.InputPinId, out int id) ? id : -1,
			GetSourcePinOffset: l => ToVec2D(PinOffsetOrCentre(l.OutputPinId)),
			GetTargetPinOffset: l => ToVec2D(PinOffsetOrCentre(l.InputPinId))
		);

		layout = new ForceDirectedLayout<Node, Link>(bodyAccessor, edgeAccessor);
	}

	/// <summary>Where each pin sits relative to its node's origin, as the renderer last measured it.</summary>
	private readonly Dictionary<int, Vector2> pinIdToOffset = [];

	/// <summary>
	/// Records where a pin sits on its node, so the layout can measure a link between the points a
	/// renderer joins rather than between node centres.
	/// </summary>
	/// <param name="pinId">The pin.</param>
	/// <param name="offset">Its position relative to its node's origin, in engine space.</param>
	public void UpdatePinOffset(int pinId, Vector2 offset) => pinIdToOffset[pinId] = offset;

	/// <summary>
	/// Where a renderer last measured a pin, relative to its node's origin.
	/// </summary>
	/// <param name="pinId">The pin.</param>
	/// <param name="offset">Its measured offset, when one has been recorded.</param>
	/// <returns>True when the pin has been drawn and measured at least once.</returns>
	public bool TryGetPinOffset(int pinId, out Vector2 offset) => pinIdToOffset.TryGetValue(pinId, out offset);

	/// <summary>
	/// Where a pin sits on its node, falling back to that node's centre until a renderer has measured
	/// it.
	/// </summary>
	/// <remarks>
	/// The centre is what every force used before pin offsets existed, so a pin nobody has drawn yet
	/// behaves as it always did rather than snapping to a node's top-left corner.
	/// </remarks>
	private Vector2 PinOffsetOrCentre(int pinId)
	{
		if (pinIdToOffset.TryGetValue(pinId, out Vector2 offset))
		{
			return offset;
		}

		if (pinIdToNodeId.TryGetValue(pinId, out int nodeId))
		{
			Node? owner = nodes.Find(n => n.Id == nodeId);
			if (owner is not null)
			{
				return owner.Dimensions * 0.5f;
			}
		}

		return Vector2.Zero;
	}

	/// <inheritdoc/>
	public IReadOnlyList<Node> Nodes => nodes.AsReadOnly();
	/// <inheritdoc/>
	public IReadOnlyList<Link> Links => links.AsReadOnly();

	/// <summary>Current physics simulation settings. Update via <see cref="UpdatePhysicsSettings"/>.</summary>
	public PhysicsSettings PhysicsSettings => layout.Settings;

	/// <summary>Computed gravity target (blend of centroid and world origin), published for debug rendering.</summary>
	public Vector2 GravityCenter => ToVector2(layout.GravityCenter);

	/// <summary>
	/// World origin in node-position space. Tracks with uniform node shifts (panning)
	/// so it stays in the same coordinate space as node positions.
	/// </summary>
	public Vector2 WorldOrigin
	{
		get => ToVector2(layout.WorldOrigin);
		set => layout.WorldOrigin = ToVec2D(value);
	}

	/// <summary>Current physics simulation info (for debugging).</summary>
	public (int SubstepCount, float SubstepDeltaTime) LastPhysicsStepInfo
	{
		get
		{
			(int s, double dt) = layout.LastStepInfo;
			return (s, (float)dt);
		}
	}

	/// <summary>Total kinetic energy in the system (sum of velocity squared for all nodes).</summary>
	public float TotalSystemEnergy => (float)layout.TotalSystemEnergy;

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

		if (links.Any(l => l.OutputPinId == outputPin.Id && l.InputPinId == inputPin.Id))
		{
			return new LinkCreationResult(false, "Those pins are already linked");
		}

		// How many links a pin accepts is the pin's own business: an output fans out to every
		// consumer that wants its value, an input takes one, and either can say otherwise.
		if (!outputPin.AllowsMultipleConnections && links.Any(l => l.OutputPinId == outputPin.Id))
		{
			return new LinkCreationResult(false, "Output pin already connected");
		}

		if (!inputPin.AllowsMultipleConnections && links.Any(l => l.InputPinId == inputPin.Id))
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

	/// <summary>Remove a link by ID.</summary>
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

	/// <summary>Remove a node and all its connected links.</summary>
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

	/// <summary>Update a node's position.</summary>
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

	/// <summary>Update a node's dimensions.</summary>
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

	/// <summary>Get all links connected to a specific node.</summary>
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
	/// Change how many links a pin accepts, overriding the default for its direction.
	/// </summary>
	/// <param name="pinId">The pin to change.</param>
	/// <param name="allowMultiple">True to let the pin take any number of links, false for one.</param>
	/// <returns>True if the pin was found.</returns>
	/// <remarks>
	/// Links already made are left alone: narrowing a pin that is connected twice does not
	/// disconnect either link, it only refuses the next one.
	/// </remarks>
	public bool SetPinAllowsMultipleConnections(int pinId, bool allowMultiple)
	{
		// A pin id is unique across the graph, so there is one node to find rather than a sequence
		// to walk looking for it.
		Node? owner = nodes.Find(n =>
			n.InputPins.Any(p => p.Id == pinId) ||
			n.OutputPins.Any(p => p.Id == pinId));

		return owner is not null &&
			(TrySetPinCapacity(owner.InputPins, pinId, allowMultiple) ||
			TrySetPinCapacity(owner.OutputPins, pinId, allowMultiple));
	}

	private static bool TrySetPinCapacity(List<Pin> pins, int pinId, bool allowMultiple)
	{
		int index = pins.FindIndex(p => p.Id == pinId);
		if (index < 0)
		{
			return false;
		}

		pins[index] = pins[index] with { AllowMultipleConnections = allowMultiple };
		return true;
	}

	/// <summary>Get the links leaving a node through its output pins.</summary>
	/// <param name="nodeId">The node.</param>
	/// <returns>Its outgoing links, or nothing if there is no such node.</returns>
	public IEnumerable<Link> GetOutgoingLinks(int nodeId)
	{
		Node? node = nodes.FirstOrDefault(n => n.Id == nodeId);
		return node is null
			? []
			: links.Where(l => node.OutputPins.Any(p => p.Id == l.OutputPinId));
	}

	/// <summary>Get the links arriving at a node through its input pins.</summary>
	/// <param name="nodeId">The node.</param>
	/// <returns>Its incoming links, or nothing if there is no such node.</returns>
	public IEnumerable<Link> GetIncomingLinks(int nodeId)
	{
		Node? node = nodes.FirstOrDefault(n => n.Id == nodeId);
		return node is null
			? []
			: links.Where(l => node.InputPins.Any(p => p.Id == l.InputPinId));
	}

	/// <summary>
	/// Everything a node's value reaches: the nodes found by following links forward from it, and
	/// the links walked to get there.
	/// </summary>
	/// <param name="nodeId">The node to start from.</param>
	/// <returns>The reach, which is empty when the node has no outgoing links or does not exist.</returns>
	/// <remarks>
	/// The starting node is not part of its own reach unless a cycle leads back to it, which is
	/// reported rather than hidden: a node that feeds itself round a loop really is downstream of
	/// itself, and the walk visits each node once so a cycle terminates.
	/// </remarks>
	public GraphReach GetDownstream(int nodeId) => Walk(nodeId, forward: true);

	/// <summary>
	/// Everything a node's value comes from: the nodes found by following links backward from it,
	/// and the links walked to get there.
	/// </summary>
	/// <param name="nodeId">The node to start from.</param>
	/// <returns>The reach, which is empty when the node has no incoming links or does not exist.</returns>
	/// <remarks>
	/// The mirror of <see cref="GetDownstream"/> in every respect, cycles included.
	/// </remarks>
	public GraphReach GetUpstream(int nodeId) => Walk(nodeId, forward: false);

	/// <summary>
	/// Walk the graph from one node, following links in one direction.
	/// </summary>
	/// <param name="nodeId">The node to start from.</param>
	/// <param name="forward">True to follow links away from the node, false to follow them back.</param>
	/// <returns>The nodes reached and the links walked to reach them.</returns>
	private GraphReach Walk(int nodeId, bool forward)
	{
		// Off the map of pins to nodes, so following a link is a lookup rather than a search
		// through every node's pins. It is a cache of what the nodes already say, so building it
		// here costs a pass and changes nothing.
		RebuildPinIdToNodeIdMap();

		HashSet<int> reachedNodes = [];
		HashSet<int> walkedLinks = [];
		HashSet<int> visited = [nodeId];
		Queue<int> pending = new();
		pending.Enqueue(nodeId);

		while (pending.Count > 0)
		{
			int current = pending.Dequeue();

			foreach (Link link in forward ? GetOutgoingLinks(current) : GetIncomingLinks(current))
			{
				walkedLinks.Add(link.Id);

				// A link is walked towards the far end of it, which is the input pin going forward
				// and the output pin going back.
				int farPin = forward ? link.InputPinId : link.OutputPinId;
				if (!pinIdToNodeId.TryGetValue(farPin, out int reachedId))
				{
					continue;
				}

				reachedNodes.Add(reachedId);

				if (visited.Add(reachedId))
				{
					pending.Enqueue(reachedId);
				}
			}
		}

		return new GraphReach(reachedNodes, walkedLinks);
	}

	/// <summary>Calculate the distance between two connected nodes.</summary>
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

		double restLength = PhysicsSettings.RestLinkLength;
		if (restLength < 0.1)
		{
			return null;
		}

		return (float)((distance.Value - restLength) / restLength);
	}

	/// <summary>Set the world origin to the centroid of all current node positions.</summary>
	public void InitializeWorldOriginToCentroid() => layout.InitializeWorldOriginToCentroid(nodes);

	/// <summary>Clear all nodes and links.</summary>
	public void Clear()
	{
		nodes.Clear();
		links.Clear();
		nextNodeId = 1;
		nextLinkId = 1;
		nextPinId = 1;
		layout.WorldOrigin = Vec2D.Zero;
	}

	/// <summary>Toggle whether a node is pinned (frozen during physics simulation).</summary>
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

	/// <summary>Replace the current physics settings.</summary>
	public void UpdatePhysicsSettings(PhysicsSettings newSettings) => layout.Settings = newSettings;

	/// <summary>Advance the force-directed layout simulation by one frame.</summary>
	public void UpdatePhysics(float deltaTime)
	{
		if (nodes.Count == 0)
		{
			return;
		}

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

	private static Vec2D ToVec2D(Vector2 v) => new(v.X, v.Y);
	private static Vector2 ToVector2(Vec2D v) => new((float)v.X, (float)v.Y);
}

/// <summary>Result of attempting to create a link.</summary>
public record LinkCreationResult(bool Success, string Message, Link? Link = null);

/// <summary>
/// The part of a graph found by walking away from one node, as node and link identifiers.
/// </summary>
/// <param name="NodeIds">The nodes reached.</param>
/// <param name="LinkIds">The links walked to reach them.</param>
public record GraphReach(IReadOnlySet<int> NodeIds, IReadOnlySet<int> LinkIds)
{
	/// <summary>A reach containing nothing.</summary>
	public static GraphReach Empty { get; } = new(new HashSet<int>(), new HashSet<int>());
}
