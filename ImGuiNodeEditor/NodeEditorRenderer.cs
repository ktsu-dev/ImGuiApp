// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiNodeEditor;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Hexa.NET.ImGui;
using Hexa.NET.ImNodes;
using ktsu.Semantics;

/// <summary>
/// Pure rendering class - only handles ImNodes display, no business logic
/// </summary>
public class NodeEditorRenderer
{
	private readonly Dictionary<int, Vector2> lastKnownNodePositions = [];
	private readonly Dictionary<int, Vector2> lastKnownNodeDimensions = [];

	/// <summary>
	/// Render the entire node editor
	/// </summary>
	public void Render(NodeEditorEngine engine, Vector2 editorSize)
	{
		ImNodes.BeginNodeEditor();

		// Render all nodes
		foreach (Node node in engine.Nodes)
		{
			RenderNode(node);
		}

		// Render all links
		foreach (Link link in engine.Links)
		{
			ImNodes.Link(link.Id, link.OutputPinId, link.InputPinId);
		}

		ImNodes.EndNodeEditor();
	}

	/// <summary>
	/// Render a single node
	/// </summary>
	private void RenderNode(Node node)
	{
		ImNodes.BeginNode(node.Id);

		// Node title
		ImNodes.BeginNodeTitleBar();
		ImGui.Text(node.Name);
		ImNodes.EndNodeTitleBar();

		// Input pins
		foreach (Pin pin in node.InputPins)
		{
			ImNodes.BeginInputAttribute(pin.Id);
			ImGui.Text(pin.EffectiveDisplayName);
			ImNodes.EndInputAttribute();
		}

		// Add some spacing between inputs and outputs
		if (node.InputPins.Count > 0 && node.OutputPins.Count > 0)
		{
			ImGui.Spacing();
		}

		// Output pins
		foreach (Pin pin in node.OutputPins)
		{
			ImNodes.BeginOutputAttribute(pin.Id);

			// Right-align output pin text by calculating node content width
			string pinText = pin.EffectiveDisplayName;
			Vector2 textSize = ImGui.CalcTextSize(pinText);

			// Calculate the node's content width based on the longest text
			float nodeContentWidth = CalculateNodeContentWidth(node);
			float paddingWidth = nodeContentWidth - textSize.X;

			// Add padding to push text to the right
			if (paddingWidth > 0)
			{
				ImGui.Dummy(new Vector2(paddingWidth, 0));
				ImGui.SameLine();
			}

			ImGui.Text(pinText);
			ImNodes.EndOutputAttribute();
		}

		ImNodes.EndNode();

		// Set position if this is the first time we're seeing this node
		if (!lastKnownNodePositions.ContainsKey(node.Id))
		{
			ImNodes.SetNodeEditorSpacePos(node.Id, node.Position);
			lastKnownNodePositions[node.Id] = node.Position;
		}
	}

	/// <summary>
	/// Check for nodes that have moved and return their new positions
	/// </summary>
	public Dictionary<int, Vector2> GetNodePositionUpdates(NodeEditorEngine engine)
	{
		Dictionary<int, Vector2> updates = [];

		foreach (Node node in engine.Nodes)
		{
			// Only query positions for nodes that have been rendered at least once
			if (!lastKnownNodePositions.ContainsKey(node.Id))
			{
				continue; // Skip nodes that haven't been rendered yet
			}

			Vector2 currentImNodesPos = ImNodes.GetNodeEditorSpacePos(node.Id);
			Vector2 lastPos = lastKnownNodePositions[node.Id];

			if (Vector2.Distance(lastPos, currentImNodesPos) > 0.1f)
			{
				updates[node.Id] = currentImNodesPos;
				lastKnownNodePositions[node.Id] = currentImNodesPos;
			}
		}

		return updates;
	}

	/// <summary>
	/// Check for nodes that have been resized and return their new dimensions
	/// </summary>
	public Dictionary<int, Vector2> GetNodeDimensionUpdates(NodeEditorEngine engine)
	{
		Dictionary<int, Vector2> updates = [];

		foreach (Node node in engine.Nodes)
		{
			// Only query dimensions for nodes that have been rendered at least once
			if (!lastKnownNodePositions.ContainsKey(node.Id))
			{
				continue; // Skip nodes that haven't been rendered yet
			}

			Vector2 currentImNodesDims = ImNodes.GetNodeDimensions(node.Id);

			// Check if this is a new node or if dimensions changed
			if (!lastKnownNodeDimensions.TryGetValue(node.Id, out Vector2 lastDims))
			{
				// Initialize with current dimensions for new nodes
				lastKnownNodeDimensions[node.Id] = currentImNodesDims;
				updates[node.Id] = currentImNodesDims;
			}
			else if (Vector2.Distance(lastDims, currentImNodesDims) > 0.1f)
			{
				updates[node.Id] = currentImNodesDims;
				lastKnownNodeDimensions[node.Id] = currentImNodesDims;
			}
		}

		return updates;
	}

	/// <summary>
	/// Render debug overlays on top of the editor
	/// </summary>
	public void RenderDebugOverlays(NodeEditorEngine engine, Vector2 editorAreaPos, Vector2 editorAreaSize, bool showDebug)
	{
		if (!showDebug)
		{
			return;
		}

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();

		// Render canvas origin
		RenderOrigin(drawList, editorAreaPos, editorAreaSize);

		// Render node debug info
		RenderNodeDebugInfo(drawList, engine, editorAreaPos, editorAreaSize);

		// Render link debug info
		RenderLinkDebugInfo(drawList, engine);

		// Render physics debug info
		if (engine.PhysicsSettings.Enabled)
		{
			RenderPhysicsDebugInfo(drawList, engine, editorAreaPos, editorAreaSize);
		}
	}

	private static void RenderOrigin(ImDrawListPtr drawList, Vector2 editorAreaPos, Vector2 editorAreaSize)
	{
		Vector2 panning = ImNodes.EditorContextGetPanning();
		Vector2 editorCenter = editorAreaPos + (editorAreaSize * 0.5f);
		Vector2 originScreen = editorCenter + panning;

		uint originColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 0.8f, 1.0f, 1.0f));

		// Draw crosshair
		drawList.AddLine(originScreen + new Vector2(-15, 0), originScreen + new Vector2(15, 0), originColor, 2.0f);
		drawList.AddLine(originScreen + new Vector2(0, -15), originScreen + new Vector2(0, 15), originColor, 2.0f);
		drawList.AddCircle(originScreen, 20.0f, originColor, 16, 2.0f);
		drawList.AddText(originScreen + new Vector2(25, -10), originColor, "ORIGIN (0,0)");
	}

	private static void RenderNodeDebugInfo(ImDrawListPtr drawList, NodeEditorEngine engine, Vector2 editorAreaPos, Vector2 editorAreaSize)
	{
		if (engine.Nodes.Count == 0)
		{
			return;
		}

		// Calculate bounding box
		Vector2 minPos = new(float.MaxValue);
		Vector2 maxPos = new(float.MinValue);

		Vector2 weightedCenterSum = Vector2.Zero;
		float totalArea = 0.0f;

		Vector2 panning = ImNodes.EditorContextGetPanning();
		Vector2 editorCenter = editorAreaPos + (editorAreaSize * 0.5f);

		foreach (Node node in engine.Nodes)
		{
			Vector2 nodeCenter = node.Position + (node.Dimensions * 0.5f);
			float nodeArea = node.Dimensions.X * node.Dimensions.Y;

			minPos = Vector2.Min(minPos, node.Position);
			maxPos = Vector2.Max(maxPos, node.Position + node.Dimensions);

			weightedCenterSum += nodeCenter * nodeArea;
			totalArea += nodeArea;
		}

		// Convert to screen space using reference node method
		Node referenceNode = engine.Nodes[0];
		Vector2 referenceScreenPos = ImNodes.GetNodeScreenSpacePos(referenceNode.Id);
		Vector2 referenceGridPos = referenceNode.Position;

		// Bounding box
		uint boundingBoxColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 0.0f, 0.6f));
		Vector2 minPosScreen = referenceScreenPos + (minPos - referenceGridPos);
		Vector2 maxPosScreen = referenceScreenPos + (maxPos - referenceGridPos);
		drawList.AddRect(minPosScreen, maxPosScreen, boundingBoxColor, 0.0f, ImDrawFlags.None, 2.0f);

		// Center of mass
		if (totalArea > 0)
		{
			Vector2 centerOfMass = weightedCenterSum / totalArea;
			Vector2 centerOfMassScreen = referenceScreenPos + (centerOfMass - referenceGridPos);
			uint centerOfMassColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 1.0f, 1.0f, 0.9f));
			drawList.AddCircleFilled(centerOfMassScreen, 6.0f, centerOfMassColor);
			drawList.AddCircle(centerOfMassScreen, 12.0f, centerOfMassColor, 16, 2.0f);
		}
	}

	private static void RenderLinkDebugInfo(ImDrawListPtr drawList, NodeEditorEngine engine)
	{
		foreach (Link link in engine.Links)
		{
			float? distance = engine.GetLinkDistance(link.Id);
			if (!distance.HasValue)
			{
				continue;
			}

			// Find the nodes for this link
			Node? outputNode = engine.Nodes.FirstOrDefault(n => n.OutputPins.Any(p => p.Id == link.OutputPinId));
			Node? inputNode = engine.Nodes.FirstOrDefault(n => n.InputPins.Any(p => p.Id == link.InputPinId));

			if (outputNode == null || inputNode == null)
			{
				continue;
			}

			// Get screen positions
			Vector2 startScreen = ImNodes.GetNodeScreenSpacePos(outputNode.Id);
			Vector2 endScreen = ImNodes.GetNodeScreenSpacePos(inputNode.Id);

			// Draw distance text at midpoint
			Vector2 midpointScreen = (startScreen + endScreen) * 0.5f;
			uint linkColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.7f, 1.0f, 0.8f));
			drawList.AddText(midpointScreen, linkColor, $"{distance.Value:F0}px");
		}
	}

	private static void RenderPhysicsDebugInfo(ImDrawListPtr drawList, NodeEditorEngine engine, Vector2 editorAreaPos, Vector2 editorAreaSize)
	{
		Vector2 panning = ImNodes.EditorContextGetPanning();
		Vector2 editorCenter = editorAreaPos + (editorAreaSize * 0.5f);

		foreach (Node node in engine.Nodes)
		{
			if (engine.Nodes.Count == 0)
			{
				continue;
			}

			// Use reference node method for coordinate transformation
			Node referenceNode = engine.Nodes[0];
			Vector2 referenceScreenPos = ImNodes.GetNodeScreenSpacePos(referenceNode.Id);
			Vector2 referenceGridPos = referenceNode.Position;

			Vector2 nodeCenter = node.Position + (node.Dimensions * 0.5f);
			Vector2 nodeCenterScreen = referenceScreenPos + (nodeCenter - (referenceGridPos + (referenceNode.Dimensions * 0.5f)));

			// Render force vector
			if (node.Force.Length() > 1.0f)
			{
				Vector2 forceEnd = nodeCenterScreen + (node.Force * 0.1f); // Scale for visibility
				uint forceColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 0.0f, 0.8f)); // Red
				drawList.AddLine(nodeCenterScreen, forceEnd, forceColor, 2.0f);
				drawList.AddCircleFilled(forceEnd, 3.0f, forceColor);
			}

			// Render velocity vector
			if (node.Velocity.Length() > 1.0f)
			{
				Vector2 velocityEnd = nodeCenterScreen + (node.Velocity * 0.5f); // Scale for visibility
				uint velocityColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 1.0f, 0.0f, 0.8f)); // Green
				drawList.AddLine(nodeCenterScreen, velocityEnd, velocityColor, 2.0f);
				drawList.AddCircleFilled(velocityEnd, 3.0f, velocityColor);
			}

			// Render repulsion zone
			float repulsionRadius = engine.PhysicsSettings.MinRepulsionDistance.In(Units.Meter);
			uint repulsionZoneColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.5f, 0.0f, 0.2f)); // Orange, transparent
			drawList.AddCircle(nodeCenterScreen, repulsionRadius, repulsionZoneColor, 32, 1.0f);
		}

		// Render physics center (origin)
		Vector2 physicsCenter = Vector2.Zero;
		Vector2 physicsCenterScreen = editorCenter + panning; // Origin is at panning offset
		uint physicsCenterColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 1.0f, 0.9f)); // Magenta
		drawList.AddCircleFilled(physicsCenterScreen, 8.0f, physicsCenterColor);
		drawList.AddCircle(physicsCenterScreen, 15.0f, physicsCenterColor, 16, 2.0f);
		drawList.AddText(physicsCenterScreen + new Vector2(20, -10), physicsCenterColor, "PHYSICS CENTER");
	}

	/// <summary>
	/// Calculate the content width of a node based on its longest text element
	/// </summary>
	private static float CalculateNodeContentWidth(Node node)
	{
		float maxWidth = 0;

		// Check node title
		Vector2 titleSize = ImGui.CalcTextSize(node.Name);
		maxWidth = Math.Max(maxWidth, titleSize.X);

		// Check all input pin names
		foreach (Pin pin in node.InputPins)
		{
			Vector2 pinSize = ImGui.CalcTextSize(pin.EffectiveDisplayName);
			maxWidth = Math.Max(maxWidth, pinSize.X);
		}

		// Check all output pin names
		foreach (Pin pin in node.OutputPins)
		{
			Vector2 pinSize = ImGui.CalcTextSize(pin.EffectiveDisplayName);
			maxWidth = Math.Max(maxWidth, pinSize.X);
		}

		// Add some padding to account for node styling
		return maxWidth + 20.0f; // 20px padding
	}
}
