// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiNodeEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ktsu.Semantics;

/// <summary>
/// Core business logic for the node editor - completely independent of ImNodes
/// </summary>
public class NodeEditorEngine
{
	private readonly List<Node> nodes = [];
	private readonly List<Link> links = [];
	private int nextNodeId = 1;
	private int nextLinkId = 1;
	private int nextPinId = 1;

	/// <inheritdoc/>
	public IReadOnlyList<Node> Nodes => nodes.AsReadOnly();
	/// <inheritdoc/>
	public IReadOnlyList<Link> Links => links.AsReadOnly();
	/// <inheritdoc/>
	public PhysicsSettings PhysicsSettings { get; private set; } = new();

	/// <summary>
	/// Create a new node with the specified number of input and output pins
	/// </summary>
	public Node CreateNode(Vector2 position, string name, int inputPinCount, int outputPinCount)
	{
		List<string> inputPinNames = [];
		List<string> outputPinNames = [];

		// Generate generic pin names
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
	/// Create a new node with specified pin names
	/// </summary>
	public Node CreateNode(Vector2 position, string name, List<string> inputPinNames, List<string> outputPinNames)
	{
		List<Pin> inputPins = [];
		List<Pin> outputPins = [];

		// Create input pins with specific names
		foreach (string pinName in inputPinNames)
		{
			inputPins.Add(new Pin(nextPinId++, PinDirection.Input, $"In {inputPins.Count + 1}", pinName));
		}

		// Create output pins with specific names
		foreach (string pinName in outputPinNames)
		{
			outputPins.Add(new Pin(nextPinId++, PinDirection.Output, $"Out {outputPins.Count + 1}", pinName));
		}

		Node node = new(nextNodeId++, position, name, inputPins, outputPins);
		nodes.Add(node);
		return node;
	}

	/// <summary>
	/// Attempt to create a link between two pins
	/// </summary>
	public LinkCreationResult TryCreateLink(int fromPinId, int toPinId)
	{
		Pin? fromPin = FindPin(fromPinId);
		Pin? toPin = FindPin(toPinId);

		if (fromPin == null || toPin == null)
		{
			return new LinkCreationResult(false, "One or both pins not found");
		}

		// Ensure we have one input and one output
		if (fromPin.Direction == toPin.Direction)
		{
			return new LinkCreationResult(false, $"Cannot connect {fromPin.Direction} to {toPin.Direction}");
		}

		// Ensure correct direction (output -> input)
		Pin outputPin = fromPin.Direction == PinDirection.Output ? fromPin : toPin;
		Pin inputPin = fromPin.Direction == PinDirection.Input ? fromPin : toPin;

		// Check if input pin already has a connection (only one input connection allowed)
		if (links.Any(l => l.InputPinId == inputPin.Id))
		{
			return new LinkCreationResult(false, "Input pin already connected");
		}

		// Check for cycles (basic check - can be enhanced)
		Node? outputNode = FindNodeByPin(outputPin.Id);
		Node? inputNode = FindNodeByPin(inputPin.Id);

		if (outputNode?.Id == inputNode?.Id)
		{
			return new LinkCreationResult(false, "Cannot connect node to itself");
		}

		// Create the link
		Link link = new(nextLinkId++, outputPin.Id, inputPin.Id);
		links.Add(link);

		return new LinkCreationResult(true, "Link created successfully", link);
	}

	/// <summary>
	/// Remove a link by ID
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
	/// Remove a node and all its connected links
	/// </summary>
	public bool RemoveNode(int nodeId)
	{
		Node? node = nodes.FirstOrDefault(n => n.Id == nodeId);
		if (node == null)
		{
			return false;
		}

		// Remove all links connected to this node
		List<Link> connectedLinks = [.. links.Where(l =>
			node.InputPins.Any(p => p.Id == l.InputPinId) ||
			node.OutputPins.Any(p => p.Id == l.OutputPinId))];

		foreach (Link? link in connectedLinks)
		{
			links.Remove(link);
		}

		// Remove the node
		nodes.Remove(node);
		return true;
	}

	/// <summary>
	/// Update a node's position
	/// </summary>
	public void UpdateNodePosition(int nodeId, Vector2 newPosition)
	{
		Node? node = nodes.FirstOrDefault(n => n.Id == nodeId);
		if (node != null)
		{
			// Create updated node with new position
			Node updatedNode = node with { Position = newPosition };
			int index = nodes.IndexOf(node);
			nodes[index] = updatedNode;
		}
	}

	/// <summary>
	/// Update a node's dimensions
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
	/// Get all links connected to a specific node
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
	/// Calculate the distance between two connected nodes
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

		return Vector2.Distance(outputNode.Position, inputNode.Position);
	}

	/// <summary>
	/// Clear all nodes and links
	/// </summary>
	public void Clear()
	{
		nodes.Clear();
		links.Clear();
		nextNodeId = 1;
		nextLinkId = 1;
		nextPinId = 1;
	}

	/// <summary>
	/// Update physics settings
	/// </summary>
	public void UpdatePhysicsSettings(PhysicsSettings newSettings) => PhysicsSettings = newSettings;

	/// <summary>
	/// Current physics simulation info (for debugging)
	/// </summary>
	public (int SubstepCount, float SubstepDeltaTime) LastPhysicsStepInfo { get; private set; }

	/// <summary>
	/// Update physics simulation for one frame with substeps for stability
	/// </summary>
	public void UpdatePhysics(float deltaTime)
	{
		if (!PhysicsSettings.Enabled || nodes.Count == 0)
		{
			LastPhysicsStepInfo = (0, 0.0f);
			return;
		}

		// Calculate substeps to achieve target physics frequency using semantic types
		Time<float> frameDeltaTime = Time<float>.FromSeconds(deltaTime);
		Time<float> targetTimestep = 1.0f / PhysicsSettings.TargetPhysicsHz; // 1/frequency = period

		int numberOfSubsteps = Math.Max(1, (int)Math.Ceiling(frameDeltaTime.In(Units.Second) / targetTimestep.In(Units.Second)));
		Time<float> substepDeltaTime = Time<float>.FromSeconds(deltaTime / numberOfSubsteps);

		// Store for debug info
		LastPhysicsStepInfo = (numberOfSubsteps, substepDeltaTime.In(Units.Second));

		// Run physics simulation for each substep
		for (int substep = 0; substep < numberOfSubsteps; substep++)
		{
			// Reset forces
			for (int i = 0; i < nodes.Count; i++)
			{
				nodes[i] = nodes[i] with { Force = Vector2.Zero };
			}

			// Calculate forces
			CalculateRepulsionForces();
			CalculateLinkForces();
			CalculateGravityForces();

			// Update velocities and positions
			UpdateNodePositions(substepDeltaTime);
		}
	}

	/// <summary>
	/// Calculate repulsion forces between nodes
	/// </summary>
	private void CalculateRepulsionForces()
	{
		for (int i = 0; i < nodes.Count; i++)
		{
			for (int j = i + 1; j < nodes.Count; j++)
			{
				Node nodeA = nodes[i];
				Node nodeB = nodes[j];

				Vector2 nodeACenter = nodeA.Position + (nodeA.Dimensions * 0.5f);
				Vector2 nodeBCenter = nodeB.Position + (nodeB.Dimensions * 0.5f);

				Vector2 direction = nodeACenter - nodeBCenter;
				Length<float> distance = Length<float>.FromMeters(direction.Length());

				// Check if nodes are within repulsion range
				if (distance < PhysicsSettings.MinRepulsionDistance && distance.In(Units.Meter) > 0.1f)
				{
					Vector2 normalizedDirection = direction / distance.In(Units.Meter);

					// Calculate repulsion magnitude using inverse square law
					float distanceSquared = distance.In(Units.Meter) * distance.In(Units.Meter);
					float repulsionMagnitude = PhysicsSettings.RepulsionStrength.In(Units.Newton) / distanceSquared;

					Vector2 repulsionForce = normalizedDirection * repulsionMagnitude;

					nodes[i] = nodes[i] with { Force = nodes[i].Force + repulsionForce };
					nodes[j] = nodes[j] with { Force = nodes[j].Force - repulsionForce };
				}
			}
		}
	}

	/// <summary>
	/// Calculate spring-damper forces for links
	/// </summary>
	private void CalculateLinkForces()
	{
		foreach (Link link in links)
		{
			Node? outputNode = FindNodeByPin(link.OutputPinId);
			Node? inputNode = FindNodeByPin(link.InputPinId);

			if (outputNode == null || inputNode == null)
			{
				continue;
			}

			Vector2 outputCenter = outputNode.Position + (outputNode.Dimensions * 0.5f);
			Vector2 inputCenter = inputNode.Position + (inputNode.Dimensions * 0.5f);

			Vector2 direction = inputCenter - outputCenter;
			Length<float> currentLength = Length<float>.FromMeters(direction.Length());

			if (currentLength.In(Units.Meter) > 0.1f)
			{
				Vector2 normalizedDirection = direction / currentLength.In(Units.Meter);

				// Calculate spring extension from rest length
				Length<float> extension = currentLength - PhysicsSettings.RestLinkLength;

				// Spring force (Hooke's law) - dimensionless spring constant * extension
				float springForceMagnitude = PhysicsSettings.LinkSpringStrength * extension.In(Units.Meter);
				Vector2 springForce = normalizedDirection * springForceMagnitude;

				// Damping force (velocity-based) - dimensionless damping coefficient
				Vector2 relativeVelocity = inputNode.Velocity - outputNode.Velocity;
				Vector2 dampingForce = relativeVelocity * PhysicsSettings.DampingFactor * 0.1f;

				Vector2 totalForce = springForce + dampingForce;

				int outputIndex = nodes.IndexOf(outputNode);
				int inputIndex = nodes.IndexOf(inputNode);

				if (outputIndex >= 0)
				{
					nodes[outputIndex] = nodes[outputIndex] with { Force = nodes[outputIndex].Force + totalForce };
				}
				if (inputIndex >= 0)
				{
					nodes[inputIndex] = nodes[inputIndex] with { Force = nodes[inputIndex].Force - totalForce };
				}
			}
		}
	}

	/// <summary>
	/// Calculate gravity forces toward origin
	/// </summary>
	private void CalculateGravityForces()
	{
		Vector2 origin = Vector2.Zero;

		for (int i = 0; i < nodes.Count; i++)
		{
			Node node = nodes[i];
			Vector2 nodeCenter = node.Position + (node.Dimensions * 0.5f);

			Vector2 directionToOrigin = origin - nodeCenter;
			Length<float> distance = Length<float>.FromMeters(directionToOrigin.Length());

			if (distance.In(Units.Meter) > 0.1f)
			{
				Vector2 normalizedDirection = directionToOrigin / distance.In(Units.Meter);

				// Apply constant gravity force toward origin
				float gravityForceMagnitude = PhysicsSettings.GravityStrength.In(Units.Newton);
				Vector2 gravityForce = normalizedDirection * gravityForceMagnitude;

				nodes[i] = nodes[i] with { Force = nodes[i].Force + gravityForce };
			}
		}
	}

	/// <summary>
	/// Update node positions based on forces and velocities
	/// </summary>
	private void UpdateNodePositions(Time<float> deltaTime)
	{
		float dt = deltaTime.In(Units.Second);

		for (int i = 0; i < nodes.Count; i++)
		{
			Node node = nodes[i];

			// Clamp force using semantic type
			Vector2 clampedForce = node.Force;
			Force<float> forceMagnitude = Force<float>.FromNewtons(clampedForce.Length());

			if (forceMagnitude > PhysicsSettings.MaxForce)
			{
				float maxForceMagnitude = PhysicsSettings.MaxForce.In(Units.Newton);
				clampedForce = Vector2.Normalize(clampedForce) * maxForceMagnitude;
			}

			// Update velocity (F = ma, assume mass = 1 kg)
			Vector2 newVelocity = node.Velocity + (clampedForce * dt);

			// Apply damping
			newVelocity *= PhysicsSettings.DampingFactor;

			// Clamp velocity using semantic type
			Velocity<float> velocityMagnitude = Velocity<float>.FromMetersPerSecond(newVelocity.Length());

			if (velocityMagnitude > PhysicsSettings.MaxVelocity)
			{
				float maxVelocityMagnitude = PhysicsSettings.MaxVelocity.In(Units.MetersPerSecond);
				newVelocity = Vector2.Normalize(newVelocity) * maxVelocityMagnitude;
			}

			// Update position
			Vector2 newPosition = node.Position + (newVelocity * dt);

			// Update node
			nodes[i] = node with
			{
				Position = newPosition,
				Velocity = newVelocity,
				Force = clampedForce
			};
		}
	}

	/// <summary>
	/// Find a pin by ID across all nodes
	/// </summary>
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

	/// <summary>
	/// Find the node that contains the specified pin
	/// </summary>
	private Node? FindNodeByPin(int pinId)
	{
		return nodes.FirstOrDefault(node =>
			node.InputPins.Any(p => p.Id == pinId) ||
			node.OutputPins.Any(p => p.Id == pinId));
	}
}

/// <summary>
/// Result of attempting to create a link
/// </summary>
public record LinkCreationResult(bool Success, string Message, Link? Link = null);
