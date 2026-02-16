// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Examples.App.Demos;

using System.Numerics;
using Hexa.NET.ImGui;
using Hexa.NET.ImNodes;

/// <summary>
/// Demo for ImNodes node editor
/// </summary>
internal sealed class ImNodesDemo : IDemoTab
{
	private int nextNodeId = 1;
	private int nextLinkId = 1;
	private readonly List<SimpleNode> nodes = [];
	private readonly List<SimpleLink> links = [];
	private bool initialPositionsSet;
	private bool automaticLayout;
	private readonly Dictionary<int, Vector2> nodeVelocities = [];
	private readonly Dictionary<int, Vector2> nodeForces = [];
	private Vector2? physicsCenter; // Cached center point for physics simulation
	private bool showDebugVisualization; // Show physics debug info

	// Debug information
	private List<string> linkFixLog = [];
	private string linkFixSummary = "";

	// Physics parameters
	private float repulsionStrength = 5000.0f;
	private float attractionStrength = 0.5f;
	private float centerForce = 0.12f;
	private float idealLinkDistance = 200.0f;
	private float damping = 0.8f;
	private float maxVelocity = 200.0f;

	private sealed record SimpleNode(int Id, Vector2 Position, string Name, List<int> InputPins, List<int> OutputPins, Vector2 Dimensions);
	private sealed record SimpleLink(int Id, int InputPinId, int OutputPinId);

	public string TabName => "ImNodes Editor";

	public ImNodesDemo()
	{
		// Initialize demo data for ImNodes with better spacing
		nodes.Add(new SimpleNode(nextNodeId++, new Vector2(100, 150), "Input Node", [], [1, 2], Vector2.Zero));
		nodes.Add(new SimpleNode(nextNodeId++, new Vector2(400, 100), "Process Node A", [3], [4, 5], Vector2.Zero));
		nodes.Add(new SimpleNode(nextNodeId++, new Vector2(400, 250), "Process Node B", [6], [7], Vector2.Zero));
		nodes.Add(new SimpleNode(nextNodeId++, new Vector2(700, 175), "Output Node", [8, 9], [], Vector2.Zero));

		// Create some demo links showing a more complex flow
		links.Add(new SimpleLink(nextLinkId++, 1, 3)); // Input to Process A
		links.Add(new SimpleLink(nextLinkId++, 2, 6)); // Input to Process B
		links.Add(new SimpleLink(nextLinkId++, 4, 8)); // Process A to Output
		links.Add(new SimpleLink(nextLinkId++, 7, 9)); // Process B to Output

		// Update nextNodeId to account for all the pin IDs we used
		nextNodeId = 10;
	}

	public void Update(float deltaTime)
	{
		if (automaticLayout && nodes.Count > 0)
		{
			// If we just enabled automatic layout, reset positioning flags
			if (initialPositionsSet)
			{
				initialPositionsSet = false;
			}
			UpdatePhysicsSimulation(deltaTime);
		}
		else if (!automaticLayout)
		{
			// Reset physics center when automatic layout is disabled
			physicsCenter = null;
		}
	}

	private void UpdatePhysicsSimulation(float deltaTime)
	{
		// Initialize forces and velocities for new nodes
		foreach (SimpleNode node in nodes)
		{
			if (!nodeForces.ContainsKey(node.Id))
			{
				nodeForces[node.Id] = Vector2.Zero;
				nodeVelocities[node.Id] = Vector2.Zero;
			}
		}

		// Calculate all forces
		CalculateForces();

		// Apply forces and update positions
		ApplyForces(deltaTime);
	}

	private void CalculateForces()
	{
		// Reset all forces
		foreach (int nodeId in nodeForces.Keys.ToList())
		{
			nodeForces[nodeId] = Vector2.Zero;
		}

		// Node repulsion forces (nodes push each other away)
		for (int i = 0; i < nodes.Count; i++)
		{
			for (int j = i + 1; j < nodes.Count; j++)
			{
				SimpleNode nodeA = nodes[i];
				SimpleNode nodeB = nodes[j];

				// Use node centers for physics calculations
				Vector2 centerA = nodeA.Position + (nodeA.Dimensions * 0.5f);
				Vector2 centerB = nodeB.Position + (nodeB.Dimensions * 0.5f);
				Vector2 direction = centerA - centerB;
				float distance = direction.Length();

				if (distance > 0)
				{
					// Repulsion force decreases with distance
					float force = repulsionStrength / ((distance * distance) + 1.0f);
					Vector2 forceVector = Vector2.Normalize(direction) * force;

					nodeForces[nodeA.Id] += forceVector;
					nodeForces[nodeB.Id] -= forceVector;
				}
			}
		}

		// Link attraction forces (connected nodes pull toward each other)
		foreach (SimpleLink link in links)
		{
			SimpleNode? startNode = GetNodeByOutputPin(link.OutputPinId);
			SimpleNode? endNode = GetNodeByInputPin(link.InputPinId);

			if (startNode != null && endNode != null)
			{
				// Use node centers for physics calculations
				Vector2 startCenter = startNode.Position + (startNode.Dimensions * 0.5f);
				Vector2 endCenter = endNode.Position + (endNode.Dimensions * 0.5f);
				Vector2 direction = endCenter - startCenter;
				float distance = direction.Length();

				if (distance > 0)
				{
					// Attraction force - stronger for longer links
					float force = (distance - idealLinkDistance) * attractionStrength;
					Vector2 forceVector = Vector2.Normalize(direction) * force;

					nodeForces[startNode.Id] += forceVector;
					nodeForces[endNode.Id] -= forceVector;
				}
			}
		}

		// Center attraction (pull all nodes toward the canvas origin)
		// Initialize physics center if not set - always use canvas origin (0,0)
		if (!physicsCenter.HasValue)
		{
			physicsCenter = Vector2.Zero; // Canvas origin (0,0)
		}

		// Use the physics center (canvas origin)
		Vector2 editorCenter = physicsCenter.Value;
		foreach (SimpleNode node in nodes)
		{
			// Use node center for physics calculations
			Vector2 nodeCenter = node.Position + (node.Dimensions * 0.5f);
			Vector2 toCenter = editorCenter - nodeCenter;
			float distance = toCenter.Length();
			if (distance > 10.0f) // Small deadzone to prevent jittering at center
			{
				// Centering force that increases with distance from center
				float force = distance * centerForce;
				nodeForces[node.Id] += Vector2.Normalize(toCenter) * force;
			}
		}
	}

	private void ApplyForces(float deltaTime)
	{

		for (int i = 0; i < nodes.Count; i++)
		{
			SimpleNode node = nodes[i];
			Vector2 force = nodeForces[node.Id];

			// Update velocity (F = ma, assuming mass = 1)
			nodeVelocities[node.Id] += force * deltaTime;
			nodeVelocities[node.Id] *= damping; // Apply damping

			// Limit velocity
			Vector2 velocity = nodeVelocities[node.Id];
			if (velocity.Length() > maxVelocity)
			{
				nodeVelocities[node.Id] = Vector2.Normalize(velocity) * maxVelocity;
			}

			// Update position
			Vector2 newPosition = node.Position + (nodeVelocities[node.Id] * deltaTime);

			// Create updated node and replace in list
			nodes[i] = node with { Position = newPosition };

			// Update ImNodes position in real-time
			ImNodes.SetNodeEditorSpacePos(node.Id, newPosition);
		}
	}

	private SimpleNode? GetNodeByOutputPin(int pinId) => nodes.FirstOrDefault(n => n.OutputPins.Contains(pinId));

	private SimpleNode? GetNodeByInputPin(int pinId) => nodes.FirstOrDefault(n => n.InputPins.Contains(pinId));

	private void RenderDebugInformation()
	{
		ImGui.SeparatorText("Debug Information:");

		// Get canvas panning once and reuse it throughout debug info
		Vector2 canvasPanning = ImNodes.EditorContextGetPanning();

		RenderGeneralDebugInfo();
		RenderCanvasDebugInfo(canvasPanning);
		RenderNodeLayoutDebugInfo(canvasPanning);

		if (automaticLayout)
		{
			RenderPhysicsDebugInfo();
		}
		else
		{
			RenderBasicNodeDebugInfo();
		}
	}

	private void RenderGeneralDebugInfo()
	{
		ImGui.Text($"Total Nodes: {nodes.Count}");
		ImGui.Text($"Total Links: {links.Count}");
	}

	private static void RenderCanvasDebugInfo(Vector2 canvasPanning)
	{
		// Canvas panning info (flip Y for intuitive display)
		Vector2 displayPanning = new(canvasPanning.X, -canvasPanning.Y);
		ImGui.Text($"Origin Offset: ({displayPanning.X:F1}, {displayPanning.Y:F1})");
		ImGui.TextDisabled("(Where origin is relative to center of view)");

		// Explain what the panning values mean
		if (canvasPanning.X == 0.0f && canvasPanning.Y == 0.0f)
		{
			ImGui.TextColored(new Vector4(0.0f, 1.0f, 0.0f, 1.0f), "✓ Origin at center - (0,0) marker should be visible");
		}
		else
		{
			string direction = GetDirectionString(canvasPanning);
			ImGui.TextColored(new Vector4(1.0f, 1.0f, 0.0f, 1.0f), $"Origin is {direction} from center");
		}
	}

	private static string GetDirectionString(Vector2 canvasPanning)
	{
		string direction = "";
		if (canvasPanning.X > 0)
		{
			direction += "right ";
		}
		else if (canvasPanning.X < 0)
		{
			direction += "left ";
		}

		if (canvasPanning.Y > 0)
		{
			direction += "down";
		}
		else if (canvasPanning.Y < 0)
		{
			direction += "up";
		}

		return direction.Trim();
	}

	private void RenderNodeLayoutDebugInfo(Vector2 canvasPanning)
	{
		if (nodes.Count == 0)
		{
			return;
		}

		// Calculate accurate bounding box using cached node dimensions
		SimpleNode firstNode = nodes[0];
		Vector2 firstNodePos = firstNode.Position;
		Vector2 firstNodeDims = firstNode.Dimensions;

		Vector2 minPos = firstNodePos;
		Vector2 maxPos = firstNodePos + firstNodeDims;

		// Area-weighted center of mass calculation
		Vector2 weightedCenterSum = Vector2.Zero;
		float totalArea = 0.0f;

		foreach (SimpleNode node in nodes)
		{
			Vector2 nodePos = node.Position;
			Vector2 nodeDims = node.Dimensions;

			// Update bounding box to include full node rectangle
			minPos.X = Math.Min(minPos.X, nodePos.X);
			minPos.Y = Math.Min(minPos.Y, nodePos.Y);
			maxPos.X = Math.Max(maxPos.X, nodePos.X + nodeDims.X);
			maxPos.Y = Math.Max(maxPos.Y, nodePos.Y + nodeDims.Y);

			// Area-weighted center of mass
			Vector2 nodeCenter = nodePos + (nodeDims * 0.5f);
			float nodeArea = nodeDims.X * nodeDims.Y;
			weightedCenterSum += nodeCenter * nodeArea;
			totalArea += nodeArea;
		}

		Vector2 centerOfMass = totalArea > 0 ? weightedCenterSum / totalArea : Vector2.Zero;

		Vector2 boundingSize = maxPos - minPos;
		ImGui.Text($"Bounding Box: {boundingSize.X:F1} × {boundingSize.Y:F1} (including node dimensions)");
		Vector2 displayCenterOfMass = new(centerOfMass.X, -centerOfMass.Y);
		ImGui.Text($"Center of Mass: ({displayCenterOfMass.X:F1}, {displayCenterOfMass.Y:F1}) (area-weighted)");

		// Show distances from current view center to key points
		float distanceToOrigin = canvasPanning.Length();
		Vector2 centerOffset = centerOfMass - (-canvasPanning); // Center relative to view center
		float distanceToCenter = centerOffset.Length();

		ImGui.Text($"Distance to Origin: {distanceToOrigin:F1}px");
		ImGui.Text($"Distance to Center: {distanceToCenter:F1}px");
	}

	private void RenderPhysicsDebugInfo()
	{
		// Physics center info
		if (physicsCenter.HasValue)
		{
			Vector2 center = physicsCenter.Value;
			Vector2 displayPhysicsCenter = new(center.X, -center.Y);
			ImGui.Text($"Physics Center: ({displayPhysicsCenter.X:F1}, {displayPhysicsCenter.Y:F1})");
		}
		else
		{
			ImGui.Text("Physics Center: Not set");
		}

		RenderNodePhysicsData();
		RenderPhysicsLinkDistances();
	}

	private void RenderNodePhysicsData()
	{
		ImGui.SeparatorText("Node Physics Data:");

		foreach (SimpleNode node in nodes)
		{
			ImGui.PushID(node.Id);

			if (ImGui.TreeNode($"Node {node.Id}: {node.Name}"))
			{
				Vector2 displayNodePos = new(node.Position.X, -node.Position.Y);
				ImGui.Text($"Position: ({displayNodePos.X:F1}, {displayNodePos.Y:F1})");

				if (nodeForces.TryGetValue(node.Id, out Vector2 force))
				{
					float forceMagnitude = force.Length();
					Vector2 displayForce = new(force.X, -force.Y);
					ImGui.Text($"Force: ({displayForce.X:F2}, {displayForce.Y:F2}) | Mag: {forceMagnitude:F2}");
				}

				if (nodeVelocities.TryGetValue(node.Id, out Vector2 velocity))
				{
					float velocityMagnitude = velocity.Length();
					Vector2 displayVelocity = new(velocity.X, -velocity.Y);
					ImGui.Text($"Velocity: ({displayVelocity.X:F2}, {displayVelocity.Y:F2}) | Mag: {velocityMagnitude:F2}");
				}

				ImGui.TreePop();
			}

			ImGui.PopID();
		}
	}

	private void RenderPhysicsLinkDistances()
	{
		ImGui.SeparatorText("Link Distances:");
		ImGui.Text($"Total Links: {links.Count}");

		if (links.Count == 0)
		{
			ImGui.TextColored(new Vector4(1.0f, 0.7f, 0.0f, 1.0f), "No links found - try creating connections between nodes");
			return;
		}

		foreach (SimpleLink link in links)
		{
			SimpleNode? startNode = GetNodeByOutputPin(link.OutputPinId);
			SimpleNode? endNode = GetNodeByInputPin(link.InputPinId);

			if (startNode != null && endNode != null)
			{
				float distance = (endNode.Position - startNode.Position).Length();
				Vector4 color = distance > idealLinkDistance
					? new Vector4(1.0f, 0.3f, 0.3f, 1.0f) // Red if too far
					: new Vector4(0.3f, 1.0f, 0.3f, 1.0f); // Green if close enough

				ImGui.TextColored(color, $"Link {link.Id}: {distance:F1}px (ideal: {idealLinkDistance:F0}px)");
			}
			else
			{
				ImGui.TextColored(new Vector4(1.0f, 0.3f, 0.3f, 1.0f), $"Link {link.Id}: ERROR - Missing nodes (Out:{link.OutputPinId} → In:{link.InputPinId})");
			}
		}
	}

	private void RenderBasicNodeDebugInfo()
	{
		// Basic node positions (when physics is not active)
		if (nodes.Count > 0)
		{
			ImGui.SeparatorText("Node Positions:");

			foreach (SimpleNode node in nodes)
			{
				Vector2 displayNodePos = new(node.Position.X, -node.Position.Y);
				ImGui.Text($"Node {node.Id} ({node.Name}): ({displayNodePos.X:F1}, {displayNodePos.Y:F1})");
			}
		}

		RenderBasicLinkDistances();
	}

	private void RenderBasicLinkDistances()
	{
		ImGui.SeparatorText("Link Distances:");
		ImGui.Text($"Total Links: {links.Count}");

		// Show link fix results if available
		if (!string.IsNullOrEmpty(linkFixSummary))
		{
			ImGui.SeparatorText("Link Fix Results:");
			ImGui.TextColored(new Vector4(0.0f, 1.0f, 0.0f, 1.0f), linkFixSummary);

			if (linkFixLog.Count > 0)
			{
				if (ImGui.CollapsingHeader("Fix Details"))
				{
					foreach (string logEntry in linkFixLog)
					{
						ImGui.Text(logEntry);
					}
				}
			}
		}

		// Debug: Show all node pin configurations
		ImGui.SeparatorText("Node Pin Debug:");
		foreach (SimpleNode node in nodes)
		{
			string inputPins = string.Join(",", node.InputPins);
			string outputPins = string.Join(",", node.OutputPins);
			ImGui.Text($"Node {node.Id} ({node.Name}): In=[{inputPins}] Out=[{outputPins}]");
		}

		if (links.Count == 0)
		{
			ImGui.TextColored(new Vector4(1.0f, 0.7f, 0.0f, 1.0f), "No links found - try creating connections between nodes");
			return;
		}

		foreach (SimpleLink link in links)
		{
			SimpleNode? startNode = GetNodeByOutputPin(link.OutputPinId);
			SimpleNode? endNode = GetNodeByInputPin(link.InputPinId);

			if (startNode != null && endNode != null)
			{
				float distance = (endNode.Position - startNode.Position).Length();
				ImGui.Text($"Link {link.Id}: {distance:F1}px");
			}
			else
			{
				ImGui.TextColored(new Vector4(1.0f, 0.3f, 0.3f, 1.0f), $"Link {link.Id}: ERROR - Missing nodes (Out:{link.OutputPinId} → In:{link.InputPinId})");

				// Debug: Show which nodes contain these pins
				SimpleNode? nodeWithOutput = nodes.FirstOrDefault(n => n.OutputPins.Contains(link.OutputPinId) || n.InputPins.Contains(link.OutputPinId));
				SimpleNode? nodeWithInput = nodes.FirstOrDefault(n => n.InputPins.Contains(link.InputPinId) || n.OutputPins.Contains(link.InputPinId));

				if (nodeWithOutput != null)
				{
					bool isOutput = nodeWithOutput.OutputPins.Contains(link.OutputPinId);
					string pinType = isOutput ? "OUTPUT" : "INPUT";
					ImGui.Text($"  Pin {link.OutputPinId} found in Node {nodeWithOutput.Id} as {pinType}");
				}
				else
				{
					ImGui.Text($"  Pin {link.OutputPinId} not found in any node");
				}

				if (nodeWithInput != null)
				{
					bool isInput = nodeWithInput.InputPins.Contains(link.InputPinId);
					string pinType = isInput ? "INPUT" : "OUTPUT";
					ImGui.Text($"  Pin {link.InputPinId} found in Node {nodeWithInput.Id} as {pinType}");
				}
				else
				{
					ImGui.Text($"  Pin {link.InputPinId} not found in any node");
				}
			}
		}
	}

	private void RenderAllDebugOverlays(Vector2 editorAreaPos, Vector2 editorAreaSize)
	{
		// Get window draw list for screen space drawing
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();

		// Helper function to convert grid space coordinates to screen space
		// For node-relative positions, use reference-based approach
		// For absolute world positions, use direct mathematical transformation
		Vector2 GridSpaceToScreenSpace(Vector2 gridPos, bool isAbsoluteWorldPosition = false)
		{
			if (isAbsoluteWorldPosition)
			{
				// For absolute world positions like origin (0,0)
				// Transform directly using ImNodes coordinate system: add panning, not subtract
				Vector2 panning = ImNodes.EditorContextGetPanning();
				Vector2 editorCenter = editorAreaPos + (editorAreaSize * 0.5f);
				Vector2 screenPos = editorCenter + gridPos + panning;
				return screenPos;
			}
			else if (nodes.Count > 0)
			{
				// For positions relative to nodes (like bounding box, center of mass)
				// Use the first node as a reference to ensure exact coordinate matching
				SimpleNode referenceNode = nodes[0];
				Vector2 referenceGridPos = referenceNode.Position;
				Vector2 referenceScreenPos = ImNodes.GetNodeScreenSpacePos(referenceNode.Id);

				// Calculate the offset from the reference node in grid space
				Vector2 offset = gridPos - referenceGridPos;

				// Apply the same offset in screen space
				return referenceScreenPos + offset;
			}
			else
			{
				// Fallback for when no nodes exist
				Vector2 panning = ImNodes.EditorContextGetPanning();
				Vector2 editorCenter = editorAreaPos + (editorAreaSize * 0.5f);
				Vector2 screenPos = editorCenter + gridPos + panning;
				return screenPos;
			}
		}

		// Colors
		uint forceColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.5f, 0.0f, 0.8f)); // Orange
		uint velocityColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 1.0f, 0.5f, 0.8f)); // Green
		uint centerColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 1.0f, 0.8f)); // Magenta
		uint repulsionColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 0.0f, 0.3f)); // Red
		uint originColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 0.8f, 1.0f, 1.0f)); // Cyan
		uint boundingBoxColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 0.0f, 0.6f)); // Yellow
		uint centerOfMassColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 1.0f, 1.0f, 0.9f)); // Cyan

		// Draw canvas origin (0,0) marker
		Vector2 originScreen = GridSpaceToScreenSpace(new Vector2(0.0f, 0.0f), isAbsoluteWorldPosition: true);
		drawList.AddLine(originScreen + new Vector2(-50, 0), originScreen + new Vector2(50, 0), originColor, 4.0f);
		drawList.AddLine(originScreen + new Vector2(0, -50), originScreen + new Vector2(0, 50), originColor, 4.0f);
		drawList.AddCircle(originScreen, 15.0f, originColor, 16, 3.0f);
		drawList.AddCircle(originScreen, 25.0f, originColor, 16, 2.0f);
		drawList.AddCircleFilled(originScreen, 5.0f, originColor);
		drawList.AddText(originScreen + new Vector2(30, -15), originColor, "ORIGIN (0,0)");

		// Physics-specific debug elements (only when physics is active)
		if (automaticLayout)
		{
			// Draw physics center
			if (physicsCenter.HasValue)
			{
				Vector2 centerScreen = GridSpaceToScreenSpace(physicsCenter.Value, isAbsoluteWorldPosition: false);
				drawList.AddCircleFilled(centerScreen, 8.0f, centerColor);
				drawList.AddText(centerScreen + new Vector2(10, -5), centerColor, "Physics Center");
			}

			// Draw forces and velocities for each node
			foreach (SimpleNode node in nodes)
			{
				// Draw from node center for accurate physics visualization
				Vector2 nodeCenter = node.Position + (node.Dimensions * 0.5f);
				Vector2 nodeCenterScreen = GridSpaceToScreenSpace(nodeCenter);

				// Draw force vector (scaled for visibility)
				if (nodeForces.TryGetValue(node.Id, out Vector2 force))
				{
					Vector2 forceEndScreen = GridSpaceToScreenSpace(nodeCenter + (force * 0.01f)); // Scale down for visibility
					if (force.Length() > 0.1f) // Only draw if significant
					{
						drawList.AddLine(nodeCenterScreen, forceEndScreen, forceColor, 2.0f);
						drawList.AddCircleFilled(forceEndScreen, 3.0f, forceColor);
					}
				}

				// Draw velocity vector (scaled for visibility)
				if (nodeVelocities.TryGetValue(node.Id, out Vector2 velocity))
				{
					Vector2 velocityEndScreen = GridSpaceToScreenSpace(nodeCenter + (velocity * 0.1f)); // Scale down for visibility
					if (velocity.Length() > 0.1f) // Only draw if significant
					{
						drawList.AddLine(nodeCenterScreen, velocityEndScreen, velocityColor, 2.0f);
						drawList.AddCircleFilled(velocityEndScreen, 3.0f, velocityColor);
					}
				}

				// Draw repulsion zones (faint circles)
				drawList.AddCircle(nodeCenterScreen, 100.0f, repulsionColor, 32, 1.0f);
			}
		}

		// Draw bounding box and center of mass for all nodes
		if (nodes.Count > 0)
		{
			// Calculate accurate bounding box using cached node dimensions and area-weighted center of mass
			SimpleNode firstNode = nodes[0];
			Vector2 firstNodePos = firstNode.Position;
			Vector2 firstNodeDims = firstNode.Dimensions;

			// Initialize bounding box with first node's full rectangle
			Vector2 minPosGrid = firstNodePos;
			Vector2 maxPosGrid = firstNodePos + firstNodeDims;

			// For area-weighted center of mass calculation
			Vector2 weightedCenterSum = Vector2.Zero;
			float totalArea = 0.0f;

			// Calculate accurate bounding box and area-weighted center of mass
			foreach (SimpleNode node in nodes)
			{
				Vector2 nodePos = node.Position;
				Vector2 nodeDims = node.Dimensions;

				// Update bounding box to include full node rectangle
				minPosGrid.X = Math.Min(minPosGrid.X, nodePos.X);
				minPosGrid.Y = Math.Min(minPosGrid.Y, nodePos.Y);
				maxPosGrid.X = Math.Max(maxPosGrid.X, nodePos.X + nodeDims.X);
				maxPosGrid.Y = Math.Max(maxPosGrid.Y, nodePos.Y + nodeDims.Y);

				// Calculate area-weighted center of mass (center of each node weighted by its area)
				Vector2 nodeCenter = nodePos + (nodeDims * 0.5f);
				float nodeArea = nodeDims.X * nodeDims.Y;
				weightedCenterSum += nodeCenter * nodeArea;
				totalArea += nodeArea;
			}

			// Convert to screen coordinates
			Vector2 minPosScreen = GridSpaceToScreenSpace(minPosGrid);
			Vector2 maxPosScreen = GridSpaceToScreenSpace(maxPosGrid);

			// Calculate final area-weighted center of mass
			Vector2 centerOfMassGrid = totalArea > 0 ? weightedCenterSum / totalArea : Vector2.Zero;
			Vector2 centerOfMassScreen = GridSpaceToScreenSpace(centerOfMassGrid);

			// Draw accurate bounding box
			drawList.AddRect(minPosScreen, maxPosScreen, boundingBoxColor, 0.0f, ImDrawFlags.None, 2.0f);
			drawList.AddText(minPosScreen + new Vector2(5, -20), boundingBoxColor, "Bounding Box");

			// Draw area-weighted center of mass
			drawList.AddCircleFilled(centerOfMassScreen, 6.0f, centerOfMassColor);
			drawList.AddCircle(centerOfMassScreen, 12.0f, centerOfMassColor, 16, 2.0f);
			drawList.AddText(centerOfMassScreen + new Vector2(15, -8), centerOfMassColor, "Center of Mass");
		}

		// Draw link connections and distances (always when debug visualization is on)
		foreach (SimpleLink link in links)
		{
			SimpleNode? startNode = GetNodeByOutputPin(link.OutputPinId);
			SimpleNode? endNode = GetNodeByInputPin(link.InputPinId);

			if (startNode != null && endNode != null)
			{
				Vector2 startScreen = GridSpaceToScreenSpace(startNode.Position);
				Vector2 endScreen = GridSpaceToScreenSpace(endNode.Position);
				float distance = (endNode.Position - startNode.Position).Length();

				// Color based on distance vs ideal (only when physics is enabled)
				Vector4 color;
				if (automaticLayout)
				{
					color = distance > idealLinkDistance
						? new Vector4(1.0f, 0.0f, 0.0f, 0.5f) // Red if too far
						: new Vector4(0.0f, 1.0f, 0.0f, 0.5f); // Green if close enough
				}
				else
				{
					// Use neutral blue color when physics is off
					color = new Vector4(0.3f, 0.7f, 1.0f, 0.6f); // Light blue
				}

				uint lineColor = ImGui.ColorConvertFloat4ToU32(color);
				drawList.AddLine(startScreen, endScreen, lineColor, 1.0f);

				// Draw distance text at midpoint
				Vector2 midpointScreen = (startScreen + endScreen) * 0.5f;
				string distanceText = automaticLayout ? $"{distance:F0}px" : $"{distance:F0}px";
				drawList.AddText(midpointScreen, lineColor, distanceText);
			}
		}

		// Draw compass (always visible when debug is on and not at origin)
		Vector2 currentPanning = ImNodes.EditorContextGetPanning();
		if (currentPanning.X != 0.0f || currentPanning.Y != 0.0f)
		{
			Vector2 directionToOrigin = currentPanning;
			float distance = directionToOrigin.Length();

			if (distance > 10.0f) // Only show if we're not too close to origin
			{
				Vector2 normalizedDirection = Vector2.Normalize(directionToOrigin);

				// Position compass in the center of the editor area in screen space
				Vector2 compassCenter = editorAreaPos + (editorAreaSize * 0.5f);

				// Colors
				uint compassBgColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.2f, 0.2f, 0.8f));
				uint compassArrowColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.3f, 0.3f, 1.0f)); // Red arrow

				// Draw compass background circle
				drawList.AddCircleFilled(compassCenter, 35.0f, compassBgColor);
				drawList.AddCircle(compassCenter, 35.0f, originColor, 32, 2.0f);

				// Draw compass arrow pointing to origin
				Vector2 arrowEnd = compassCenter + (normalizedDirection * 25.0f);
				Vector2 arrowLeft = compassCenter + (new Vector2(-normalizedDirection.Y, normalizedDirection.X) * 8.0f) + (normalizedDirection * 15.0f);
				Vector2 arrowRight = compassCenter + (new Vector2(normalizedDirection.Y, -normalizedDirection.X) * 8.0f) + (normalizedDirection * 15.0f);

				// Draw arrow shaft
				drawList.AddLine(compassCenter, arrowEnd, compassArrowColor, 3.0f);
				// Draw arrow head
				drawList.AddTriangleFilled(arrowEnd, arrowLeft, arrowRight, compassArrowColor);

				// Add distance text
				drawList.AddText(compassCenter + new Vector2(-15, 40), originColor, $"{distance:F0}px");
				drawList.AddText(compassCenter + new Vector2(-20, -50), originColor, "TO ORIGIN");
			}
		}
	}

	public void Render()
	{
		if (ImGui.BeginTabItem(TabName))
		{
			if (ImGui.BeginChild("##content"))
			{
				RenderHeader();
				RenderControls();
				RenderNodeEditor();
				HandleLinkEvents();
			}
			ImGui.EndChild();

			ImGui.EndTabItem();
		}
	}

	private static void RenderHeader()
	{
		ImGui.TextWrapped("ImNodes provides a node editor with support for nodes, pins, and connections.");
		ImGui.Separator();
	}

	private void RenderControls()
	{
		if (ImGui.Button("Add Node"))
		{
			// Place new nodes in a grid pattern to avoid overlap
			int nodeIndex = nodes.Count;
			int row = nodeIndex / 3;
			int col = nodeIndex % 3;
			Vector2 nodePos = new(150 + (col * 250), 100 + (row * 150));

			nodes.Add(new SimpleNode(
	nextNodeId++,
	nodePos,
	$"Custom Node {nodeIndex + 1}",
	[nextNodeId, nextNodeId + 1], // Input pins
	[nextNodeId + 2, nextNodeId + 3], // Output pins
	Vector2.Zero // Dimensions will be updated after rendering
));
			nextNodeId += 4; // Reserve IDs for pins
		}

		ImGui.SameLine();
		if (ImGui.Button("Clear All"))
		{
			ClearAllNodes();
		}

		ImGui.SameLine();
		if (ImGui.Button("Reset Demo"))
		{
			ResetToDemo();
		}

		ImGui.SameLine();
		if (ImGui.Button("Fix Links"))
		{
			FixCorruptedLinks();
		}
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip("Fix any corrupted links with incorrect pin mappings");
		}

		if (!string.IsNullOrEmpty(linkFixSummary))
		{
			ImGui.SameLine();
			if (ImGui.Button("Clear Log"))
			{
				linkFixLog.Clear();
				linkFixSummary = "";
			}
		}
	}

	private void ClearAllNodes()
	{
		nodes.Clear();
		links.Clear();
		nodeVelocities.Clear();
		nodeForces.Clear();
		physicsCenter = null;
		nextNodeId = 1;
		nextLinkId = 1;
		initialPositionsSet = false;
	}

	private void ResetToDemo()
	{
		ClearAllNodes();

		// Recreate the demo layout
		nodes.Add(new SimpleNode(nextNodeId++, new Vector2(100, 150), "Input Node", [], [1, 2], Vector2.Zero));
		nodes.Add(new SimpleNode(nextNodeId++, new Vector2(400, 100), "Process Node A", [3], [4, 5], Vector2.Zero));
		nodes.Add(new SimpleNode(nextNodeId++, new Vector2(400, 250), "Process Node B", [6], [7], Vector2.Zero));
		nodes.Add(new SimpleNode(nextNodeId++, new Vector2(700, 175), "Output Node", [8, 9], [], Vector2.Zero));

		links.Add(new SimpleLink(nextLinkId++, 1, 3)); // Input to Process A
		links.Add(new SimpleLink(nextLinkId++, 2, 6)); // Input to Process B
		links.Add(new SimpleLink(nextLinkId++, 4, 8)); // Process A to Output
		links.Add(new SimpleLink(nextLinkId++, 7, 9)); // Process B to Output

		nextNodeId = 10;
	}

	private void FixCorruptedLinks()
	{
		List<SimpleLink> fixedLinks = [];
		int originalCount = links.Count;
		int fixedCount = 0;
		int correctCount = 0;
		List<string> fixLog = [];

		foreach (SimpleLink link in links)
		{
			fixLog.Add($"Processing Link {link.Id}: Out:{link.OutputPinId} → In:{link.InputPinId}");

			// Find nodes containing these pins
			SimpleNode? nodeWithOutputPin = nodes.FirstOrDefault(n => n.OutputPins.Contains(link.OutputPinId) || n.InputPins.Contains(link.OutputPinId));
			SimpleNode? nodeWithInputPin = nodes.FirstOrDefault(n => n.InputPins.Contains(link.InputPinId) || n.OutputPins.Contains(link.InputPinId));

			fixLog.Add($"  nodeWithOutputPin: {nodeWithOutputPin?.Name} (ID: {nodeWithOutputPin?.Id})");
			fixLog.Add($"  nodeWithInputPin: {nodeWithInputPin?.Name} (ID: {nodeWithInputPin?.Id})");

			if (nodeWithOutputPin != null && nodeWithInputPin != null)
			{
				bool outputPinIsActuallyOutput = nodeWithOutputPin.OutputPins.Contains(link.OutputPinId);
				bool inputPinIsActuallyInput = nodeWithInputPin.InputPins.Contains(link.InputPinId);

				fixLog.Add($"  outputPinIsActuallyOutput: {outputPinIsActuallyOutput}");
				fixLog.Add($"  inputPinIsActuallyInput: {inputPinIsActuallyInput}");

				if (outputPinIsActuallyOutput && inputPinIsActuallyInput)
				{
					// Link is correct
					fixLog.Add($"  Link {link.Id} is already correct");
					fixedLinks.Add(link);
					correctCount++;
				}
				else
				{
					// Try to fix the link by finding the correct pin mappings
					int actualOutputPin = -1;
					int actualInputPin = -1;

					// Check if pins are swapped
					bool canSwap = nodeWithOutputPin.InputPins.Contains(link.OutputPinId) && nodeWithInputPin.OutputPins.Contains(link.InputPinId);
					fixLog.Add($"  Can swap pins: {canSwap}");

					if (canSwap)
					{
						// Pins are swapped
						actualOutputPin = link.InputPinId;
						actualInputPin = link.OutputPinId;
						fixedCount++;
						fixLog.Add($"  Original: Out:{link.OutputPinId} → In:{link.InputPinId}");
						fixLog.Add($"  Fixed to: Out:{actualOutputPin} → In:{actualInputPin}");
					}

					if (actualOutputPin != -1 && actualInputPin != -1)
					{
						// Create corrected link
						fixedLinks.Add(new SimpleLink(link.Id, actualOutputPin, actualInputPin));
					}
					else
					{
						fixLog.Add($"  Link {link.Id} could not be fixed");
					}
				}
			}
			else
			{
				fixLog.Add($"  Link {link.Id} - nodes not found, removing link");
			}
		}

		// Replace the links collection with fixed links
		links.Clear();
		links.AddRange(fixedLinks);

		// Add final verification
		fixLog.Add("");
		fixLog.Add("Final links after fix:");
		foreach (SimpleLink finalLink in links)
		{
			fixLog.Add($"  Link {finalLink.Id}: Out:{finalLink.OutputPinId} → In:{finalLink.InputPinId}");
		}

		// Store the fix results for display
		linkFixLog = fixLog;
		linkFixSummary = $"Link Fix Results: {originalCount} original, {correctCount} already correct, {fixedCount} fixed, {fixedLinks.Count} total after fix";
	}

	private void RenderNodeEditor()
	{
		// Create horizontal layout: editor on left, parameters on right
		ImGui.BeginTable("NodeEditorLayout", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV);
		ImGui.TableSetupColumn("Editor", ImGuiTableColumnFlags.WidthStretch, 0.75f);
		ImGui.TableSetupColumn("Parameters", ImGuiTableColumnFlags.WidthStretch, 0.25f);

		ImGui.TableNextRow();
		ImGui.TableNextColumn();

		// Store editor area position and size for compass rendering
		Vector2 editorAreaPos = ImGui.GetCursorScreenPos();
		Vector2 editorAreaSize = ImGui.GetContentRegionAvail();

		// Node editor
		ImNodes.BeginNodeEditor();

		// Set initial positions only once (skip if automatic layout is active)
		if (!initialPositionsSet && nodes.Count > 0 && !automaticLayout)
		{
			foreach (SimpleNode node in nodes)
			{
				ImNodes.SetNodeEditorSpacePos(node.Id, node.Position);
			}
			initialPositionsSet = true;
		}

		RenderNodes();
		RenderLinks();

		// Sync node positions and dimensions from ImNodes back to our data structure
		// This ensures manual node dragging works properly and keeps dimensions cached
		for (int i = 0; i < nodes.Count; i++)
		{
			SimpleNode node = nodes[i];
			Vector2 currentImNodesPos = ImNodes.GetNodeEditorSpacePos(node.Id);
			Vector2 currentImNodesDims = ImNodes.GetNodeDimensions(node.Id);

			// Update if position has changed or dimensions are not cached yet
			bool positionChanged = Vector2.Distance(node.Position, currentImNodesPos) > 0.1f;
			bool dimensionsNotCached = node.Dimensions == Vector2.Zero;
			bool dimensionsChanged = Vector2.Distance(node.Dimensions, currentImNodesDims) > 0.1f;

			if (positionChanged || dimensionsNotCached || dimensionsChanged)
			{
				nodes[i] = node with { Position = currentImNodesPos, Dimensions = currentImNodesDims };
			}
		}

		ImNodes.EndNodeEditor();

		// Move to the parameters column
		ImGui.TableNextColumn();
		RenderParametersPanel();

		ImGui.EndTable();

		// Draw all debug visualization after everything else so it appears on top
		if (showDebugVisualization)
		{
			RenderAllDebugOverlays(editorAreaPos, editorAreaSize);
		}
	}

	private void RenderNodes()
	{
		// Render nodes
		for (int i = 0; i < nodes.Count; i++)
		{
			SimpleNode node = nodes[i];
			ImNodes.BeginNode(node.Id);

			// Node title
			ImNodes.BeginNodeTitleBar();
			ImGui.TextUnformatted(node.Name);
			ImNodes.EndNodeTitleBar();

			// Input pins
			for (int j = 0; j < node.InputPins.Count; j++)
			{
				int pinId = node.InputPins[j];
				ImNodes.BeginInputAttribute(pinId);
				ImGui.Text($"In {j + 1}");
				ImNodes.EndInputAttribute();
			}

			// Node content
			ImGui.Text($"Node ID: {node.Id}");

			// Output pins
			for (int j = 0; j < node.OutputPins.Count; j++)
			{
				int pinId = node.OutputPins[j];
				ImNodes.BeginOutputAttribute(pinId);
				ImGui.Indent(40);
				ImGui.Text($"Out {j + 1}");
				ImNodes.EndOutputAttribute();
			}

			ImNodes.EndNode();
		}
	}

	private void RenderLinks()
	{
		// Render links
		foreach (SimpleLink link in links)
		{
			ImNodes.Link(link.Id, link.InputPinId, link.OutputPinId);
		}
	}

	private void RenderParametersPanel()
	{
		// Debug Visualization (always available)
		ImGui.SeparatorText("Debug Visualization:");
		ImGui.Checkbox("Show Debug Visualization", ref showDebugVisualization);
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip("Show visual overlays: canvas origin, node bounding box, center of mass, and physics data (if enabled)");
		}

		// Show debug information when enabled
		if (showDebugVisualization)
		{
			RenderDebugInformation();
		}

		ImGui.SeparatorText("Physics Layout:");

		// Physics layout toggle
		ImGui.Checkbox("Automatic Layout", ref automaticLayout);
		ImGui.SameLine();
		ImGui.TextDisabled("(?)");
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip("Physics simulation: node repulsion, link attraction, and center gravity");
		}

		if (automaticLayout)
		{
			ImGui.SameLine();
			ImGui.TextColored(new Vector4(0.0f, 1.0f, 0.0f, 1.0f), "● ACTIVE");
		}

		// Physics parameters panel
		if (automaticLayout)
		{
			ImGui.SeparatorText("Physics Parameters:");

			RenderPhysicsInputs();
			RenderPhysicsInfo();
		}
		else
		{
			ImGui.TextDisabled("Enable Automatic Layout above");
			ImGui.TextDisabled("to show physics parameters");
		}

		// Canvas Navigation Controls
		ImGui.SeparatorText("Canvas Navigation:");

		if (ImGui.Button("Reset Canvas to Origin"))
		{
			// Reset canvas panning to origin (0,0)
			ImNodes.EditorContextResetPanning(new Vector2(0.0f, 0.0f));
		}
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip("Pan the canvas so origin (0,0) is visible");
		}

		if (ImGui.Button("Center Canvas on Nodes"))
		{
			// Calculate area-weighted center of mass of all nodes using cached dimensions
			if (nodes.Count > 0)
			{
				Vector2 weightedCenterSum = Vector2.Zero;
				float totalArea = 0.0f;

				foreach (SimpleNode node in nodes)
				{
					// Use cached position and dimensions for efficient calculation
					Vector2 nodePos = node.Position;
					Vector2 nodeDims = node.Dimensions;

					// Calculate area-weighted center (center of each node weighted by its area)
					Vector2 nodeCenter = nodePos + (nodeDims * 0.5f);
					float nodeArea = nodeDims.X * nodeDims.Y;
					weightedCenterSum += nodeCenter * nodeArea;
					totalArea += nodeArea;
				}

				Vector2 centerOfMass = totalArea > 0 ? weightedCenterSum / totalArea : Vector2.Zero;

				// Pan canvas to center the area-weighted center of mass
				ImNodes.EditorContextResetPanning(centerOfMass);
			}
		}
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip("Pan the canvas to center all nodes in view");
		}
	}

	private void RenderPhysicsInputs()
	{
		ImGui.InputFloat("Repulsion Strength", ref repulsionStrength, 100.0f, 1000.0f, "%.0f");
		if (ImGui.IsItemHovered())
		{
			if (showDebugVisualization)
			{
				ImGui.SetTooltip("REPULSION FORCE IMPLEMENTATION:\n" +
					"• Formula: force = repulsionStrength / ((distance²) + 1)\n" +
					"• Applied between ALL node pairs (N² complexity)\n" +
					"• Prevents nodes from overlapping\n" +
					"• Higher values = stronger push-apart force\n" +
					"• Visible as: Red circles around nodes\n" +
					"• Default: 5000 (good balance for most layouts)");
			}
			else
			{
				ImGui.SetTooltip("How strongly nodes push each other away");
			}
		}

		ImGui.InputFloat("Attraction Strength", ref attractionStrength, 0.01f, 0.1f, "%.3f");
		if (ImGui.IsItemHovered())
		{
			if (showDebugVisualization)
			{
				ImGui.SetTooltip("ATTRACTION FORCE IMPLEMENTATION:\n" +
					"• Formula: force = (distance - idealLinkDistance) × attractionStrength\n" +
					"• Applied only between CONNECTED nodes\n" +
					"• Pulls connected nodes toward ideal distance\n" +
					"• Spring-like behavior: stronger when farther from ideal\n" +
					"• Visible as: Green/Red lines between connected nodes\n" +
					"• Default: 0.5 (moderate spring strength)");
			}
			else
			{
				ImGui.SetTooltip("How strongly connected nodes pull toward each other");
			}
		}

		ImGui.InputFloat("Center Force", ref centerForce, 0.001f, 0.01f, "%.4f");
		if (ImGui.IsItemHovered())
		{
			if (showDebugVisualization)
			{
				ImGui.SetTooltip("CENTER GRAVITY IMPLEMENTATION:\n" +
					"• Formula: force = distance × centerForce\n" +
					"• Applied to ALL nodes toward physics center\n" +
					"• Prevents nodes from drifting to edges\n" +
					"• Linear increase with distance from center\n" +
					"• Physics center = canvas origin (0,0)\n" +
					"• Visible as: Magenta circle at origin\n" +
					"• Default: 0.12 (gentle centering force)");
			}
			else
			{
				ImGui.SetTooltip("How strongly nodes are pulled toward the center");
			}
		}

		ImGui.InputFloat("Ideal Link Distance", ref idealLinkDistance, 10.0f, 50.0f, "%.0f px");
		if (ImGui.IsItemHovered())
		{
			if (showDebugVisualization)
			{
				ImGui.SetTooltip("IDEAL DISTANCE IMPLEMENTATION:\n" +
					"• Target distance for connected nodes\n" +
					"• Used in attraction force calculation\n" +
					"• Links shorter than ideal: nodes pull apart\n" +
					"• Links longer than ideal: nodes pull together\n" +
					"• Visible as: Distance labels on connections\n" +
					"• Green lines = close to ideal, Red lines = too far\n" +
					"• Default: 200px (good for typical node sizes)");
			}
			else
			{
				ImGui.SetTooltip("Preferred distance between connected nodes");
			}
		}

		ImGui.InputFloat("Damping", ref damping, 0.01f, 0.1f, "%.3f");
		if (ImGui.IsItemHovered())
		{
			if (showDebugVisualization)
			{
				ImGui.SetTooltip("VELOCITY DAMPING IMPLEMENTATION:\n" +
					"• Formula: velocity = velocity × damping (each frame)\n" +
					"• Applied to ALL nodes every simulation step\n" +
					"• Simulates friction/air resistance\n" +
					"• Higher values = nodes slow down faster\n" +
					"• Prevents oscillation and ensures convergence\n" +
					"• Visible as: Green arrows (velocity vectors)\n" +
					"• Range: 0.1-0.95 (0.8 = good stability)");
			}
			else
			{
				ImGui.SetTooltip("How quickly nodes slow down (higher = more stable)");
			}
		}

		ImGui.InputFloat("Max Velocity", ref maxVelocity, 10.0f, 100.0f, "%.0f px/s");
		if (ImGui.IsItemHovered())
		{
			if (showDebugVisualization)
			{
				ImGui.SetTooltip("VELOCITY LIMITING IMPLEMENTATION:\n" +
					"• Applied after force integration each frame\n" +
					"• Prevents explosive behavior with high forces\n" +
					"• Clamps velocity magnitude to this maximum\n" +
					"• Maintains velocity direction, only limits speed\n" +
					"• Essential for simulation stability\n" +
					"• Visible as: Length of green velocity arrows\n" +
					"• Default: 200px/s (smooth but responsive)");
			}
			else
			{
				ImGui.SetTooltip("Maximum speed limit for node movement");
			}
		}

		if (ImGui.Button("Reset Parameters"))
		{
			repulsionStrength = 5000.0f;
			attractionStrength = 0.5f;
			centerForce = 0.12f;
			idealLinkDistance = 200.0f;
			damping = 0.8f;
			maxVelocity = 200.0f;
		}
		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip("Restore default values");
		}
	}

	private void RenderPhysicsInfo()
	{
		// Display current physics info
		ImGui.SeparatorText("Physics Info:");
		ImGui.Text($"Active Nodes: {nodes.Count}");
		if (physicsCenter.HasValue)
		{
			Vector2 center = physicsCenter.Value;
			ImGui.Text($"Center: ({center.X:F0}, {center.Y:F0})");
		}

		// Show total system energy (sum of all velocities)
		float totalEnergy = 0.0f;
		foreach (KeyValuePair<int, Vector2> kvp in nodeVelocities)
		{
			totalEnergy += kvp.Value.LengthSquared();
		}
		ImGui.Text($"System Energy: {totalEnergy:F2}");
	}

	private void HandleLinkEvents()
	{
		// Handle new links
		bool isLinkCreated;
		int startPin = 0;
		int endPin = 0;

		unsafe
		{
			isLinkCreated = ImNodes.IsLinkCreated(&startPin, &endPin);
		}

		if (isLinkCreated)
		{
			// Copy to local variables for use in lambdas
			int startPinId = startPin;
			int endPinId = endPin;

			// Determine which pin is output and which is input
			SimpleNode? nodeWithStartPin = nodes.FirstOrDefault(n => n.OutputPins.Contains(startPinId) || n.InputPins.Contains(startPinId));
			SimpleNode? nodeWithEndPin = nodes.FirstOrDefault(n => n.OutputPins.Contains(endPinId) || n.InputPins.Contains(endPinId));

			if (nodeWithStartPin != null && nodeWithEndPin != null)
			{
				bool startIsOutput = nodeWithStartPin.OutputPins.Contains(startPinId);
				bool endIsOutput = nodeWithEndPin.OutputPins.Contains(endPinId);

				// Create link with correct pin order: (linkId, outputPinId, inputPinId)
				if (startIsOutput && !endIsOutput)
				{
					// Start pin is output, end pin is input - correct order
					links.Add(new SimpleLink(nextLinkId++, startPinId, endPinId));
				}
				else if (!startIsOutput && endIsOutput)
				{
					// Start pin is input, end pin is output - reverse order
					links.Add(new SimpleLink(nextLinkId++, endPinId, startPinId));
				}
				// If both are same type (output-output or input-input), don't create the link
			}
		}

		// Handle link destruction
		bool isLinkDestroyed;
		int linkId = 0;
		int safeLinkId = 0;

		unsafe
		{
			isLinkDestroyed = ImNodes.IsLinkDestroyed(&linkId);
			safeLinkId = linkId; // Store the link ID safely
		}

		if (isLinkDestroyed)
		{
			links.RemoveAll(link => link.Id == safeLinkId);
		}
	}
}
