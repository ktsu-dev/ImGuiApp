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
	private readonly HashSet<int> currentlyDraggedNodes = [];

	// Cached editor-to-screen transform: derived empirically from ImNodes
	// during Render() so it matches ImNodes' internal coordinate system exactly
	private Vector2 editorToScreenBase;
	private bool hasEditorTransform;

	/// <summary>
	/// Set of node IDs currently being dragged by the user
	/// </summary>
	public IReadOnlySet<int> CurrentlyDraggedNodes => currentlyDraggedNodes;

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

		// Cache the editor-to-screen transform while inside the editor context.
		// Derived empirically from a reference node so it matches ImNodes' internal
		// coordinate system exactly, regardless of how panning/origin are composed.
		CacheEditorTransform(engine);

		ImNodes.EndNodeEditor();
	}

	/// <summary>
	/// Render a single node
	/// </summary>
	private void RenderNode(Node node)
	{
		// Apply engine position to ImNodes BEFORE rendering the node
		// This ensures physics-calculated positions are reflected immediately
		if (lastKnownNodePositions.TryGetValue(node.Id, out Vector2 lastPos))
		{
			// Check if engine position differs from what we last set in ImNodes
			if (Vector2.Distance(lastPos, node.Position) > 0.1f)
			{
				ImNodes.SetNodeEditorSpacePos(node.Id, node.Position);
				lastKnownNodePositions[node.Id] = node.Position;
			}
		}
		else
		{
			// First render - set initial position
			ImNodes.SetNodeEditorSpacePos(node.Id, node.Position);
			lastKnownNodePositions[node.Id] = node.Position;
		}

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
	}

	/// <summary>
	/// Check for nodes that have moved and return their new positions
	/// </summary>
	public Dictionary<int, Vector2> GetNodePositionUpdates(NodeEditorEngine engine)
	{
		Dictionary<int, Vector2> updates = [];
		currentlyDraggedNodes.Clear();

		foreach (Node node in engine.Nodes)
		{
			// Only query positions for nodes that have been rendered at least once
			if (!lastKnownNodePositions.ContainsKey(node.Id))
			{
				continue; // Skip nodes that haven't been rendered yet
			}

			Vector2 currentImNodesPos = ImNodes.GetNodeEditorSpacePos(node.Id);

			// Only report a change if the ImNodes position differs from the ENGINE position
			// This means the user dragged the node (ImNodes changed independently of us)
			if (Vector2.Distance(node.Position, currentImNodesPos) > 0.1f)
			{
				updates[node.Id] = currentImNodesPos;
				lastKnownNodePositions[node.Id] = currentImNodesPos;
				currentlyDraggedNodes.Add(node.Id);
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
	/// Cache the editor-to-screen transform by deriving it from a reference node's
	/// known editor-space and screen-space positions within the active editor context
	/// </summary>
	private void CacheEditorTransform(NodeEditorEngine engine)
	{
		if (engine.Nodes.Count > 0)
		{
			int refNodeId = engine.Nodes[0].Id;
			Vector2 refScreenPos = ImNodes.GetNodeScreenSpacePos(refNodeId);
			Vector2 refEditorPos = ImNodes.GetNodeEditorSpacePos(refNodeId);
			editorToScreenBase = refScreenPos - refEditorPos;
			hasEditorTransform = true;
		}
		else
		{
			hasEditorTransform = false;
		}
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

		if (engine.Nodes.Count == 0 || !hasEditorTransform)
		{
			return;
		}

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();

		// Clip debug overlays to the editor area so they don't spill into adjacent panels
		drawList.PushClipRect(editorAreaPos, editorAreaPos + editorAreaSize, true);

		// Render canvas origin
		RenderOrigin(drawList, editorAreaPos, editorAreaSize, engine);

		// Render node debug info
		RenderNodeDebugInfo(drawList, engine);

		// Render link debug info
		RenderLinkDebugInfo(drawList, engine);

		// Render physics debug info
		if (engine.PhysicsSettings.Enabled)
		{
			RenderPhysicsDebugInfo(drawList, engine, editorAreaPos, editorAreaSize);
		}

		drawList.PopClipRect();
	}

	/// <summary>
	/// Convert an editor-space position to screen-space using the cached transform
	/// </summary>
	private Vector2 EditorToScreen(Vector2 editorPos) =>
		editorToScreenBase + editorPos;

	private void RenderOrigin(ImDrawListPtr drawList, Vector2 editorAreaPos, Vector2 editorAreaSize, NodeEditorEngine engine)
	{
		if (!hasEditorTransform)
		{
			return;
		}

		// Use WorldOrigin (which tracks with panning) rather than a fixed Vector2.Zero
		Vector2 originScreen = EditorToScreen(engine.WorldOrigin);

		uint originColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 0.8f, 1.0f, 1.0f));

		// Draw crosshair
		drawList.AddLine(originScreen + new Vector2(-15, 0), originScreen + new Vector2(15, 0), originColor, 2.0f);
		drawList.AddLine(originScreen + new Vector2(0, -15), originScreen + new Vector2(0, 15), originColor, 2.0f);
		drawList.AddCircle(originScreen, 20.0f, originColor, 16, 2.0f);
		drawList.AddText(originScreen + new Vector2(25, -10), originColor, "ORIGIN (0,0)");
	}

	private void RenderNodeDebugInfo(ImDrawListPtr drawList, NodeEditorEngine engine)
	{
		if (!hasEditorTransform)
		{
			return;
		}

		// Calculate bounding box
		Vector2 minPos = new(float.MaxValue);
		Vector2 maxPos = new(float.MinValue);

		Vector2 weightedCenterSum = Vector2.Zero;
		float totalArea = 0.0f;

		foreach (Node node in engine.Nodes)
		{
			Vector2 nodeCenter = node.Position + (node.Dimensions * 0.5f);
			float nodeArea = node.Dimensions.X * node.Dimensions.Y;

			minPos = Vector2.Min(minPos, node.Position);
			maxPos = Vector2.Max(maxPos, node.Position + node.Dimensions);

			weightedCenterSum += nodeCenter * nodeArea;
			totalArea += nodeArea;
		}

		// Bounding box (using cached reference data)
		uint boundingBoxColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 0.0f, 0.6f));
		Vector2 minPosScreen = EditorToScreen(minPos);
		Vector2 maxPosScreen = EditorToScreen(maxPos);
		drawList.AddRect(minPosScreen, maxPosScreen, boundingBoxColor, 0.0f, ImDrawFlags.None, 2.0f);

		// Center of mass
		if (totalArea > 0)
		{
			Vector2 centerOfMass = weightedCenterSum / totalArea;
			Vector2 centerOfMassScreen = EditorToScreen(centerOfMass);
			uint centerOfMassColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 1.0f, 1.0f, 0.9f));
			drawList.AddCircleFilled(centerOfMassScreen, 6.0f, centerOfMassColor);
			drawList.AddCircle(centerOfMassScreen, 12.0f, centerOfMassColor, 16, 2.0f);
		}
	}

	private void RenderLinkDebugInfo(ImDrawListPtr drawList, NodeEditorEngine engine)
	{
		if (!hasEditorTransform)
		{
			return;
		}

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

			// Get screen positions using cached reference data (use node centers, not top-left)
			Vector2 outputCenter = outputNode.Position + (outputNode.Dimensions * 0.5f);
			Vector2 inputCenter = inputNode.Position + (inputNode.Dimensions * 0.5f);
			Vector2 startScreen = EditorToScreen(outputCenter);
			Vector2 endScreen = EditorToScreen(inputCenter);

			// Draw distance text at midpoint
			Vector2 midpointScreen = (startScreen + endScreen) * 0.5f;
			uint linkColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.7f, 1.0f, 0.8f));
			drawList.AddText(midpointScreen, linkColor, $"{distance.Value:F0}px");
		}
	}

	private void RenderPhysicsDebugInfo(ImDrawListPtr drawList, NodeEditorEngine engine, Vector2 editorAreaPos, Vector2 editorAreaSize)
	{
		if (!hasEditorTransform)
		{
			return;
		}

		foreach (Node node in engine.Nodes)
		{
			Vector2 nodeCenter = node.Position + (node.Dimensions * 0.5f);
			Vector2 nodeCenterScreen = EditorToScreen(nodeCenter);

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

		// Render gravity center (fixed point, in editor/position space)
		Vector2 centroidScreen = EditorToScreen(engine.GravityCenter);
		uint physicsCenterColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 1.0f, 0.9f)); // Magenta
		drawList.AddCircleFilled(centroidScreen, 8.0f, physicsCenterColor);
		drawList.AddCircle(centroidScreen, 15.0f, physicsCenterColor, 16, 2.0f);
		drawList.AddText(centroidScreen + new Vector2(20, -10), physicsCenterColor, "GRAVITY CENTER");
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
