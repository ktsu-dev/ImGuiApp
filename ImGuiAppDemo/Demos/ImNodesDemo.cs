// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiApp.Demo.Demos;

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

	private sealed record SimpleNode(int Id, Vector2 Position, string Name, List<int> InputPins, List<int> OutputPins);
	private sealed record SimpleLink(int Id, int InputPinId, int OutputPinId);

	public string TabName => "ImNodes Editor";

	public ImNodesDemo()
	{
		// Initialize ImNodes context
		ImNodes.CreateContext();

		// Initialize demo data for ImNodes
		nodes.Add(new SimpleNode(nextNodeId++, new Vector2(50, 50), "Input Node", [], [1, 2]));
		nodes.Add(new SimpleNode(nextNodeId++, new Vector2(250, 100), "Process Node", [3], [4]));
		nodes.Add(new SimpleNode(nextNodeId++, new Vector2(450, 50), "Output Node", [5, 6], []));

		// Create some demo links
		links.Add(new SimpleLink(nextLinkId++, 1, 3)); // Connect Input to Process
		links.Add(new SimpleLink(nextLinkId++, 4, 5)); // Connect Process to Output
	}

	public void Update(float deltaTime)
	{
		// No updates needed for ImNodes demo
	}

	public void Render()
	{
		if (ImGui.BeginTabItem(TabName))
		{
			ImGui.TextWrapped("ImNodes provides a node editor with support for nodes, pins, and connections.");
			ImGui.Separator();

			// Node editor controls
			if (ImGui.Button("Add Node"))
			{
				Vector2 mousePos = ImGui.GetMousePos();
				Vector2 canvasPos = ImGui.GetCursorScreenPos();
				Vector2 nodePos = mousePos - canvasPos + new Vector2(0, 50); // Offset for controls

				nodes.Add(new SimpleNode(
					nextNodeId++,
					nodePos,
					$"Node {nodes.Count + 1}",
					[nextNodeId, nextNodeId + 1], // Input pins
					[nextNodeId + 2, nextNodeId + 3] // Output pins
				));
				nextNodeId += 4; // Reserve IDs for pins
			}

			ImGui.SameLine();
			if (ImGui.Button("Clear All"))
			{
				nodes.Clear();
				links.Clear();
				nextNodeId = 1;
				nextLinkId = 1;
			}

			ImGui.Separator();

			// Node editor
			ImNodes.BeginNodeEditor();

			// Render nodes
			for (int i = 0; i < nodes.Count; i++)
			{
				var node = nodes[i];
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

			// Render links
			foreach (var link in links)
			{
				ImNodes.Link(link.Id, link.InputPinId, link.OutputPinId);
			}

			ImNodes.EndNodeEditor();

			// Handle new links
			int startPin, endPin;
			if (ImNodes.IsLinkCreated(out startPin, out endPin))
			{
				links.Add(new SimpleLink(nextLinkId++, startPin, endPin));
			}

			// Handle link deletion
			int linkId;
			if (ImNodes.IsLinkDestroyed(out linkId))
			{
				links.RemoveAll(link => link.Id == linkId);
			}

			// Display node count
			ImGui.Text($"Nodes: {nodes.Count}, Links: {links.Count}");

			ImGui.EndTabItem();
		}
	}
}
