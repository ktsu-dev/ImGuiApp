// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor;

using System;
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
	/// Raised after a node has been removed from the graph.
	/// </summary>
	/// <remarks>
	/// Anything the caller keys by node id — an instance, a cached definition, a selection — has to
	/// be told when that id stops standing for a node, because the graph is the only thing that
	/// knows.
	/// </remarks>
	public event EventHandler<NodeRemovedEventArgs>? NodeRemoved;

	/// <summary>
	/// Raised after the graph has been cleared.
	/// </summary>
	/// <remarks>
	/// This is not merely <see cref="NodeRemoved"/> repeated. <see cref="Clear"/> also restarts the
	/// id counters, so the next node created takes id 1 again. Anything keyed by node id must drop
	/// every entry here rather than ageing them out, or a stale entry is silently inherited by an
	/// unrelated node that happens to be issued the same id.
	/// </remarks>
	public event EventHandler<EventArgs>? Cleared;

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

	/// <summary>What each pin holds, and what it resets to. See <see cref="PinValueStore"/>.</summary>
	private readonly PinValueStore pinValues = new();

	/// <summary>Pins whose value lives somewhere else. See <see cref="PinValueAccessor"/>.</summary>
	private readonly Dictionary<int, PinValueAccessor> pinValueAccessors = [];

	/// <summary>
	/// Say that a pin's value lives somewhere other than this engine's own store.
	/// </summary>
	/// <param name="pinId">The pin.</param>
	/// <param name="accessor">How to read and write the value where it actually lives.</param>
	/// <remarks>
	/// From here on <see cref="GetPinValue(int)"/> and <see cref="SetPinValue(int, object?)"/> go
	/// through the accessor for this pin, so the pin has one home rather than two.
	/// <para>
	/// Binding re-seeds the pin's default from the accessor, because for a bound pin the value it
	/// was created with is the one its home was holding when it was bound — not the one the store
	/// was seeded with from a declaration the home may have read differently. That is what keeps
	/// <see cref="ResetPinValue(int)"/> honest: a reset of a bound pin puts back a value the home
	/// itself produced.
	/// </para>
	/// <para>
	/// The engine holds delegates and nothing else: it learns nothing about reflection, about
	/// <c>PinDefinition</c>, or about the object on the other side.
	/// <see cref="AttributeBasedNodeFactory"/> is the caller that has all three and registers the
	/// pair; a host with its own way of modelling a node can register its own.
	/// </para>
	/// </remarks>
	public void BindPinValue(int pinId, PinValueAccessor accessor)
	{
		Ensure.NotNull(accessor);
		pinValueAccessors[pinId] = accessor;
		pinValues.Seed(pinId, accessor.Get());
	}

	/// <summary>
	/// Stop routing a pin's value elsewhere, returning it to this engine's own store.
	/// </summary>
	/// <param name="pinId">The pin.</param>
	/// <returns>True if the pin had a bound accessor to drop.</returns>
	/// <remarks>
	/// The store still holds whatever it last held for the pin, which for a bound pin is its seeded
	/// default rather than the value the accessor was reporting. Unbinding is what
	/// <see cref="RemoveNode(int)"/> and <see cref="Clear"/> do as a pin stops existing, so nothing
	/// reads the store afterwards.
	/// </remarks>
	public bool UnbindPinValue(int pinId) => pinValueAccessors.Remove(pinId);

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
	/// What a pin currently holds.
	/// </summary>
	/// <param name="pinId">The pin.</param>
	/// <returns>Its value, or null if it has none.</returns>
	/// <remarks>
	/// A connected input pin still reports the literal last written to it. Whether that literal or
	/// the link's value is the one that matters is a question about evaluating the graph, which this
	/// library does not do.
	/// <para>
	/// A pin bound through <see cref="BindPinValue(int, PinValueAccessor)"/> is read from wherever
	/// its accessor keeps it; every other pin is read from this engine's own store. Either way this
	/// is the one call that answers what a pin holds.
	/// </para>
	/// </remarks>
	public object? GetPinValue(int pinId) =>
		pinValueAccessors.TryGetValue(pinId, out PinValueAccessor? accessor)
			? accessor.Get()
			: pinValues.Get(pinId);

	/// <summary>
	/// Write a value to a pin.
	/// </summary>
	/// <param name="pinId">The pin.</param>
	/// <param name="value">The value.</param>
	/// <returns>True if it was written, false if there is no such pin or its type refused the value.</returns>
	/// <remarks>
	/// The pin's declared type is checked first, whichever home the value has, so a refused value
	/// reaches neither the accessor nor the store. A bound accessor can still report a write it did
	/// not make, which is what its own false return means.
	/// </remarks>
	public bool SetPinValue(int pinId, object? value)
	{
		Pin? pin = FindPin(pinId);
		if (pin is null || !PinValueStore.Accepts(pin.DataType, value))
		{
			return false;
		}

		// TrySet checks the type again, which is the store's own guard and stays its business. The
		// check above is what extends the same refusal to a bound pin, whose home has no such guard.
		return pinValueAccessors.TryGetValue(pinId, out PinValueAccessor? accessor)
			? accessor.Set(value)
			: pinValues.TrySet(pin, value);
	}

	/// <summary>
	/// Put a pin back to the value it was created with.
	/// </summary>
	/// <param name="pinId">The pin.</param>
	/// <returns>True if it had a default to go back to and that default was written.</returns>
	/// <remarks>
	/// The seeded default lives in the store whether or not the pin is bound, because it is what the
	/// pin was created with rather than what it holds now — see
	/// <see cref="BindPinValue(int, PinValueAccessor)"/> for where a bound pin's seed comes from. A
	/// bound pin has that default written back through its accessor as well, so the reset is visible
	/// to whoever the value actually lives on rather than only to a store nothing is reading.
	/// </remarks>
	public bool ResetPinValue(int pinId)
	{
		if (!pinValues.Reset(pinId))
		{
			return false;
		}

		return !pinValueAccessors.TryGetValue(pinId, out PinValueAccessor? accessor)
			|| accessor.Set(pinValues.Get(pinId));
	}

	/// <summary>
	/// Whether any link meets this pin.
	/// </summary>
	/// <param name="pinId">The pin.</param>
	/// <returns>True if at least one link ends at it.</returns>
	public bool IsPinConnected(int pinId) =>
		links.Any(l => l.OutputPinId == pinId || l.InputPinId == pinId);

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
		PinSpec[] inputSpecs = new PinSpec[inputPinCount];
		PinSpec[] outputSpecs = new PinSpec[outputPinCount];

		for (int i = 0; i < inputPinCount; i++)
		{
			inputSpecs[i] = new PinSpec($"In {i + 1}");
		}

		for (int i = 0; i < outputPinCount; i++)
		{
			outputSpecs[i] = new PinSpec($"Out {i + 1}");
		}

		return CreateNodeFromSpecs(position, name, inputSpecs, outputSpecs);
	}

	/// <summary>
	/// Create a new node with specified pin names. The pins are untyped.
	/// </summary>
	public Node CreateNode(Vector2 position, string name, List<string> inputPinNames, List<string> outputPinNames) =>
		CreateNodeFromSpecs(
			position,
			name,
			[.. inputPinNames.Select(n => new PinSpec(n))],
			[.. outputPinNames.Select(n => new PinSpec(n))]);

	/// <summary>
	/// Create a new node from a description of each of its pins.
	/// </summary>
	/// <param name="position">Where to place the node.</param>
	/// <param name="name">The node's name.</param>
	/// <param name="inputs">Its input pins, in the order they should be drawn.</param>
	/// <param name="outputs">Its output pins, in the order they should be drawn.</param>
	/// <returns>The created node.</returns>
	public Node CreateNodeFromSpecs(Vector2 position, string name, IReadOnlyList<PinSpec> inputs, IReadOnlyList<PinSpec> outputs)
	{
		List<Pin> inputPins = [];
		List<Pin> outputPins = [];

		foreach (PinSpec spec in inputs)
		{
			inputPins.Add(new Pin(
				nextPinId++,
				PinDirection.Input,
				$"In {inputPins.Count + 1}",
				spec.Name,
				spec.AllowMultipleConnections,
				spec.DataType));
		}

		foreach (PinSpec spec in outputs)
		{
			outputPins.Add(new Pin(
				nextPinId++,
				PinDirection.Output,
				$"Out {outputPins.Count + 1}",
				spec.Name,
				spec.AllowMultipleConnections,
				spec.DataType));
		}

		Node node = new(nextNodeId++, position, name, inputPins, outputPins);
		nodes.Add(node);

		for (int i = 0; i < inputPins.Count; i++)
		{
			pinValues.Seed(inputPins[i].Id, PinValueStore.Coerce(inputs[i].DataType, inputs[i].DefaultValue));
		}

		for (int i = 0; i < outputPins.Count; i++)
		{
			pinValues.Seed(outputPins[i].Id, PinValueStore.Coerce(outputs[i].DataType, outputs[i].DefaultValue));
		}

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

		// A removed node's pins are gone, so what they held and where they were measured goes with
		// them. Without this both tables grow for the life of the process.
		foreach (Pin pin in node.InputPins.Concat(node.OutputPins))
		{
			pinValues.Forget(pin.Id);
			pinValueAccessors.Remove(pin.Id);
			pinIdToOffset.Remove(pin.Id);
		}

		nodes.Remove(node);
		NodeRemoved?.Invoke(this, new NodeRemovedEventArgs(nodeId));
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
		pinValues.Clear();
		pinValueAccessors.Clear();
		pinIdToOffset.Clear();
		layout.WorldOrigin = Vec2D.Zero;
		Cleared?.Invoke(this, EventArgs.Empty);
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
