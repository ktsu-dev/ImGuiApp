// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.App.Demos;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ForceDirectedLayout;
using ktsu.ImGui.NodeEditor;
using ktsu.ImGui.Widgets;
using ktsu.Keybinding.Core.Services;
using ktsu.NodeGraph.Library.Operations;
using ktsu.NodeGraph.Library.Primitives;
using ktsu.NodeGraph.Library.Utilities;

/// <summary>
/// Clean architecture ImNodes demo with proper separation of concerns
/// </summary>
/// <remarks>
/// Every edit goes through a <see cref="NodeEditorHistory"/>, so the whole tab is undoable with
/// Ctrl+Z and Ctrl+Y. The keys come from an in-memory <c>ktsu.Keybinding</c> keymap, the way an
/// application with its own keymap would supply them.
/// </remarks>
internal sealed class CleanImNodesDemo : IDemoTab, IDisposable
{
	public string TabName => "Clean ImNodes";

	// Business logic layer
	private readonly NodeEditorEngine engine = new();
	private readonly AttributeBasedNodeFactory nodeFactory;

	// Undo and redo for everything below
	private readonly NodeEditorHistory history;

	// Presentation layers
	private readonly NodeEditorRenderer renderer = new();
	private readonly NodeEditorInputHandler inputHandler;
	private readonly KeybindingService keybindings;

	// UI state
	private bool showDebugVisualization;
	private bool showNodeBodies;
	private Vector2 lastEditorSize;
	private string lastActionMessage = "";
	private Vector4 lastActionColor = new(1.0f, 1.0f, 1.0f, 1.0f);

	public CleanImNodesDemo()
	{
		nodeFactory = new AttributeBasedNodeFactory(engine);
		RegisterNodeTypes();
		CreateDemoData();
		engine.InitializeWorldOriginToCentroid();

		// Created after the demo data, so the starting graph is where the history begins rather than
		// something Ctrl+Z can take apart.
		history = new NodeEditorHistory(engine, nodeFactory);
		renderer.History = history;

		CommandRegistry commands = new();
		keybindings = new KeybindingService(commands, new ProfileManager());
		keybindings.CreateProfile("default", "Default");
		keybindings.SetActiveProfile("default");
		NodeEditorCommands.Register(commands, keybindings);
		inputHandler = new NodeEditorInputHandler(keybindings);
	}

	public void Dispose() => history.Dispose();

	public void Update(float deltaTime)
	{
		// Inform physics which nodes are being dragged so they're excluded from simulation
		engine.SetDraggedNodes(renderer.CurrentlyDraggedNodes);

		// Update physics simulation
		engine.UpdatePhysics(deltaTime);
	}

	public void Render()
	{
		if (DemoProbe.TabItem(TabName))
		{
			if (ImGui.BeginChild("##content"))
			{
				// Create horizontal layout: editor on left, controls on right
				ImGui.BeginTable("CleanNodeEditorLayout", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV);
				ImGui.TableSetupColumn("Editor", ImGuiTableColumnFlags.WidthStretch, 0.7f);
				ImGui.TableSetupColumn("Controls", ImGuiTableColumnFlags.WidthStretch, 0.3f);

				// Editor column
				ImGui.TableNextColumn();
				RenderNodeEditor();

				// Controls column
				ImGui.TableNextColumn();
				RenderControlsPanel();

				ImGui.EndTable();
			}
			ImGui.EndChild();

			ImGui.EndTabItem();
		}
	}

	private void RenderNodeEditor()
	{
		Vector2 editorAreaPos = ImGui.GetCursorScreenPos();
		Vector2 editorAreaSize = ImGui.GetContentRegionAvail();
		lastEditorSize = editorAreaSize;

		// Handle input events first
		ProcessInputEvents();

		// Render the editor
		renderer.Render(engine, editorAreaSize);

		// Update node positions/dimensions from ImNodes (AFTER rendering when context is active)
		UpdateNodeTransforms();

		// Render debug overlays on top
		renderer.RenderDebugOverlays(engine, editorAreaPos, editorAreaSize, showDebugVisualization);
	}

	/// <summary>
	/// Hands each kind of request the handler reported to the engine.
	/// </summary>
	/// <remarks>
	/// One method per request kind rather than four loops in a row: the gestures grew one at a time
	/// and the combined method had crossed the cognitive-complexity threshold. Each still reads top
	/// to bottom in the order the requests are drained, which is the part that matters — a link
	/// selected alongside the node it hangs off is removed by its own request rather than silently
	/// by RemoveNode.
	/// </remarks>
	private void ProcessInputEvents()
	{
		InputEvents events = inputHandler.ProcessInput();

		ProcessLinkCreationRequests(events);
		ProcessLinkDeletionRequests(events);
		ProcessNodeDeletionRequests(events);
		ProcessNodeDuplicationRequests(events);
		ProcessHistoryRequests(events);
	}

	private void ProcessHistoryRequests(InputEvents events)
	{
		if (events.UndoRequested)
		{
			Undo();
		}

		if (events.RedoRequested)
		{
			Redo();
		}
	}

	private void Undo()
	{
		string? description = history.NextUndoDescription;
		if (history.Undo())
		{
			lastActionMessage = $"Undid: {description}";
			lastActionColor = new Vector4(0.8f, 0.8f, 1.0f, 1.0f);
		}
	}

	private void Redo()
	{
		string? description = history.NextRedoDescription;
		if (history.Redo())
		{
			lastActionMessage = $"Redid: {description}";
			lastActionColor = new Vector4(0.8f, 0.8f, 1.0f, 1.0f);
		}
	}

	private void ProcessLinkCreationRequests(InputEvents events)
	{
		foreach (LinkCreationRequest request in events.LinkCreationRequests)
		{
			LinkCreationResult result = history.TryCreateLink(request.FromPinId, request.ToPinId);

			if (result.Success)
			{
				lastActionMessage = $"Link created: {result.Message}";
				lastActionColor = new Vector4(0.0f, 1.0f, 0.0f, 1.0f); // Green
			}
			else
			{
				lastActionMessage = $"Link failed: {result.Message}";
				lastActionColor = new Vector4(1.0f, 0.3f, 0.3f, 1.0f); // Red
			}
		}
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3267:Loops should be simplified using the \"Where\" LINQ method.", Justification = "RemoveLink is the mutation, not a predicate; a Where rewrite would hide a graph edit inside a lazily-evaluated filter.")]
	private void ProcessLinkDeletionRequests(InputEvents events)
	{
		foreach (int linkId in events.LinkDeletionRequests)
		{
			if (history.RemoveLink(linkId))
			{
				lastActionMessage = $"Link {linkId} deleted";
				lastActionColor = new Vector4(1.0f, 0.7f, 0.0f, 1.0f); // Orange
			}
		}
	}

	/// <remarks>
	/// Drained after the links so a link selected alongside the node it hangs off is removed by its
	/// own request rather than silently by RemoveNode; either order leaves the same graph.
	/// </remarks>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3267:Loops should be simplified using the \"Where\" LINQ method.", Justification = "RemoveNode is the mutation, not a predicate; a Where rewrite would hide a graph edit inside a lazily-evaluated filter.")]
	private void ProcessNodeDeletionRequests(InputEvents events)
	{
		foreach (int nodeId in events.NodeDeletionRequests)
		{
			if (history.RemoveNode(nodeId))
			{
				lastActionMessage = $"Node {nodeId} deleted";
				lastActionColor = new Vector4(1.0f, 0.7f, 0.0f, 1.0f); // Orange
			}
		}
	}

	/// <remarks>
	/// The whole selection goes over in one call rather than one node at a time, so a link between
	/// two selected nodes is copied along with them.
	/// </remarks>
	private void ProcessNodeDuplicationRequests(InputEvents events)
	{
		if (events.NodeDuplicationRequests.Count == 0)
		{
			return;
		}

		IReadOnlyList<Node> copies = history.DuplicateNodes(events.NodeDuplicationRequests, NodeEditorEngine.DefaultDuplicationOffset);
		if (copies.Count > 0)
		{
			lastActionMessage = copies.Count == 1
				? $"Node {copies[0].Id} duplicated"
				: $"{copies.Count} nodes duplicated";
			lastActionColor = new Vector4(0.4f, 0.8f, 1.0f, 1.0f); // Blue
		}
	}

	private void UpdateNodeTransforms()
	{
		// Update positions
		Dictionary<int, Vector2> positionUpdates = renderer.GetNodePositionUpdates(engine);

		// Detect uniform shift (all nodes moved by the same amount = panning).
		// Apply the same shift to WorldOrigin so it stays in the nodes' coordinate space.
		if (positionUpdates.Count == engine.Nodes.Count && positionUpdates.Count > 1)
		{
			Vector2 firstShift = Vector2.Zero;
			bool isUniformShift = true;

			foreach ((int nodeId, Vector2 newPosition) in positionUpdates)
			{
				Node? node = engine.Nodes.FirstOrDefault(n => n.Id == nodeId);
				if (node == null)
				{
					continue;
				}

				Vector2 shift = newPosition - node.Position;
				if (firstShift == Vector2.Zero)
				{
					firstShift = shift;
				}
				else if (Vector2.Distance(shift, firstShift) > 1.0f)
				{
					isUniformShift = false;
					break;
				}
			}

			if (isUniformShift && firstShift.LengthSquared() > 0.01f)
			{
				engine.WorldOrigin += firstShift;
			}
		}

		foreach ((int nodeId, Vector2 newPosition) in positionUpdates)
		{
			engine.UpdateNodePosition(nodeId, newPosition);
		}

		// Update dimensions
		Dictionary<int, Vector2> dimensionUpdates = renderer.GetNodeDimensionUpdates(engine);
		foreach ((int nodeId, Vector2 newDimensions) in dimensionUpdates)
		{
			engine.UpdateNodeDimensions(nodeId, newDimensions);
		}
	}

	private void RenderControlsPanel()
	{
		ImGui.SeparatorText("Node Editor Controls");

		// Action buttons
		if (DemoProbe.Button("Add Input Node"))
		{
			Vector2 position = new(100, 100 + (engine.Nodes.Count * 50));
			history.Record("Add input node", () => engine.CreateNode(position, $"Input {engine.Nodes.Count + 1}", 0, 2));
		}

		ImGui.SameLine();
		if (DemoProbe.Button("Add Process Node"))
		{
			Vector2 position = new(300, 100 + (engine.Nodes.Count * 50));
			history.Record("Add process node", () => engine.CreateNode(position, $"Process {engine.Nodes.Count + 1}", 2, 2));
		}

		ImGui.SameLine();
		if (DemoProbe.Button("Add Output Node"))
		{
			Vector2 position = new(500, 100 + (engine.Nodes.Count * 50));
			history.Record("Add output node", () => engine.CreateNode(position, $"Output {engine.Nodes.Count + 1}", 2, 0));
		}

		// Recorded like any other edit, so a reset or a clear is one Ctrl+Z away from being undone.
		if (DemoProbe.Button("Reset Demo"))
		{
			history.Record("Reset demo", () =>
			{
				engine.Clear();
				CreateDemoData();
				engine.InitializeWorldOriginToCentroid();
			});
			lastActionMessage = "Reset to demo data";
			lastActionColor = new Vector4(0.0f, 0.8f, 1.0f, 1.0f); // Cyan
		}

		ImGui.SameLine();
		if (DemoProbe.Button("Clear All"))
		{
			history.Record("Clear all", engine.Clear);
			lastActionMessage = "All nodes and links cleared";
			lastActionColor = new Vector4(1.0f, 0.7f, 0.0f, 1.0f); // Orange
		}

		ImGui.SeparatorText("History");
		RenderHistoryControls();

		ImGui.SeparatorText("Layout Tools");
		RenderLayoutTools();

		ImGui.SeparatorText("View & Node Content");
		RenderViewControls();

		// Physics settings
		ImGui.SeparatorText("Physics Simulation");
		RenderPhysicsControls();

		// Hover highlighting
		ImGui.SeparatorText("Hover Highlighting");
		RenderHighlightControls();

		// Debug visualization toggle
		ImGui.Separator();
		DemoProbe.Checkbox("Show Debug Visualization", ref showDebugVisualization);

		// Status information
		ImGui.SeparatorText("Status");
		ImGui.Text($"Nodes: {engine.Nodes.Count}");
		ImGui.Text($"Links: {engine.Links.Count}");

		if (!string.IsNullOrEmpty(lastActionMessage))
		{
			ImGui.TextColored(lastActionColor, lastActionMessage);
		}

		if (renderer.SelectedNodeIds.Count > 0)
		{
			ImGui.SeparatorText("Parameters");
			NodeInspectorPanel.Draw(engine, renderer.SelectedNodeIds.First());
		}

		// Debug information
		if (showDebugVisualization)
		{
			RenderDebugInformation();
		}
	}

	/// <summary>
	/// Draws undo and redo, with what each would do, and the keys the keymap has them on.
	/// </summary>
	private void RenderHistoryControls()
	{
		using (new ScopedDisable(!history.CanUndo))
		{
			if (DemoProbe.Button("Undo"))
			{
				Undo();
			}
		}

		ImGui.SameLine();
		using (new ScopedDisable(!history.CanRedo))
		{
			if (DemoProbe.Button("Redo"))
			{
				Redo();
			}
		}

		ImGui.TextDisabled($"Next undo: {history.NextUndoDescription ?? "nothing"}");
		ImGui.TextDisabled($"Next redo: {history.NextRedoDescription ?? "nothing"}");

		foreach (ktsu.Keybinding.Core.Models.Command command in NodeEditorCommands.All)
		{
			ImGui.TextDisabled($"{command.Name}: {keybindings.GetChord(command.Id)?.ToString() ?? "unbound"}");
		}
	}

	/// <summary>
	/// Draws grid snapping and comment boxes, the tools for laying a pipeline out by hand.
	/// </summary>
	private void RenderLayoutTools()
	{
		bool snap = renderer.SnapToGrid;
		if (DemoProbe.Checkbox("Snap to grid", ref snap))
		{
			renderer.SnapToGrid = snap;
		}

		ImGui.SameLine();
		float spacing = renderer.GridSpacing ?? 24f;
		ImGui.SetNextItemWidth(120f);
		if (DemoProbe.SliderFloat("Grid spacing", ref spacing, 8f, 64f, "%.0f"))
		{
			renderer.GridSpacing = spacing;
		}

		using (new ScopedDisable(renderer.SelectedNodeIds.Count == 0))
		{
			if (DemoProbe.Button("Snap Selection To Grid"))
			{
				renderer.SnapNodesToGrid(engine, renderer.SelectedNodeIds);
			}

			ImGui.SameLine();
			if (DemoProbe.Button("Comment Selection"))
			{
				history.CreateCommentBoxAround(renderer.SelectedNodeIds, "Comment");
			}
		}

		ImGui.TextDisabled("Drag a comment's title to move it with its nodes; double-click to rename.");
	}

	/// <summary>
	/// Draws the renderer's zoom, the fit-to-view action, and what is drawn inside each node: the
	/// inline editors on unconnected parameter rows, and host content through the body hook.
	/// </summary>
	private void RenderViewControls()
	{
		float zoom = renderer.Zoom;
		ImGui.SetNextItemWidth(120f);
		if (DemoProbe.SliderFloat("Zoom", ref zoom, NodeEditorRenderer.MinZoom, NodeEditorRenderer.MaxZoom, "%.2fx"))
		{
			renderer.Zoom = zoom;
		}

		ImGui.SameLine();
		if (DemoProbe.Button("Fit To View") && renderer.FitToView(engine, lastEditorSize))
		{
			lastActionMessage = $"Fitted the graph at {renderer.Zoom:0.00}x";
			lastActionColor = new Vector4(0.0f, 0.8f, 1.0f, 1.0f); // Cyan
		}

		bool inlineEditors = renderer.DrawInlinePinEditors;
		if (DemoProbe.Checkbox("Inline parameter editors", ref inlineEditors))
		{
			renderer.DrawInlinePinEditors = inlineEditors;
		}

		ImGui.SameLine();
		float inlineWidth = renderer.InlineEditorWidth;
		ImGui.SetNextItemWidth(120f);
		if (DemoProbe.SliderFloat("Editor width", ref inlineWidth, 40f, 200f, "%.0f"))
		{
			renderer.InlineEditorWidth = inlineWidth;
		}

		// The body hook runs inside each node after its pins, so host content sits under the rows
		// rather than among them. Here it summarises how the node sits in the graph.
		if (DemoProbe.Checkbox("Show connection summary in each node", ref showNodeBodies))
		{
			renderer.DrawNodeBody = showNodeBodies ? DrawNodeSummary : null;
		}
	}

	/// <summary>
	/// The host content drawn in each node's body while the summary is on.
	/// </summary>
	private void DrawNodeSummary(Node node)
	{
		int incoming = engine.GetIncomingLinks(node.Id).Count();
		int outgoing = engine.GetOutgoingLinks(node.Id).Count();
		int reach = engine.GetDownstream(node.Id).NodeIds.Count;
		ImGui.TextDisabled($"in {incoming} / out {outgoing} / reaches {reach}");
	}

	/// <summary>
	/// Draws the renderer's hover options, which are what a node under the pointer says about the
	/// rest of the graph.
	/// </summary>
	private void RenderHighlightControls()
	{
		bool highlightLinks = renderer.HighlightLinksOnNodeHover;
		if (DemoProbe.Checkbox("Highlight a hovered node's links", ref highlightLinks))
		{
			renderer.HighlightLinksOnNodeHover = highlightLinks;
		}

		bool highlightDownstream = renderer.HighlightDownstreamOnNodeHover;
		if (DemoProbe.Checkbox("Highlight everything downstream of it", ref highlightDownstream))
		{
			renderer.HighlightDownstreamOnNodeHover = highlightDownstream;
		}

		bool hoveredLinkOnTop = renderer.DrawHoveredLinkOnTop;
		if (DemoProbe.Checkbox("Draw a hovered link over the nodes", ref hoveredLinkOnTop))
		{
			renderer.DrawHoveredLinkOnTop = hoveredLinkOnTop;
		}
	}

	private void RenderDebugInformation()
	{
		ImGui.SeparatorText("Debug Information");

		// Node information
		if (DemoProbe.Header("Nodes"))
		{
			foreach (Node node in engine.Nodes)
			{
				ImGui.Text($"Node {node.Id} ({node.Name}):");
				ImGui.Text($"  Position: ({node.Position.X:F1}, {node.Position.Y:F1})");
				ImGui.Text($"  Dimensions: ({node.Dimensions.X:F1}, {node.Dimensions.Y:F1})");
				ImGui.Text($"  Input Pins: {string.Join(", ", node.InputPins.Select(p => $"{p.Id}({p.Name})"))}");
				ImGui.Text($"  Output Pins: {string.Join(", ", node.OutputPins.Select(p => $"{p.Id}({p.Name})"))}");

				if (engine.PhysicsSettings.Enabled)
				{
					ImGui.Text($"  Velocity: ({node.Velocity.X:F1}, {node.Velocity.Y:F1}) | Speed: {node.Velocity.Length():F1}");
					ImGui.Text($"  Force: ({node.Force.X:F1}, {node.Force.Y:F1}) | Magnitude: {node.Force.Length():F1}");
				}

				ImGui.Separator();
			}
		}

		// Link information
		if (DemoProbe.Header("Links"))
		{
			foreach (Link link in engine.Links)
			{
				float? distance = engine.GetLinkDistance(link.Id);
				ImGui.Text($"Link {link.Id}: Pin {link.OutputPinId} → Pin {link.InputPinId}");
				if (distance.HasValue)
				{
					ImGui.SameLine();
					ImGui.Text($"({distance.Value:F1}px)");
				}
			}
		}
	}

	/// <summary>
	/// Draws the layout tuning, which the node editor library supplies whole.
	/// </summary>
	/// <remarks>
	/// The panel covers every setting the simulation has and captions each one, so this demo shows the
	/// same controls a consuming application gets rather than a copy of them that drifts.
	/// </remarks>
	private void RenderPhysicsControls()
	{
		PhysicsSettings settings = engine.PhysicsSettings;
		if (PhysicsSettingsPanel.Draw(ref settings))
		{
			engine.UpdatePhysicsSettings(settings);
		}

		if (DemoProbe.Button("Gentle Physics"))
		{
			engine.UpdatePhysicsSettings(settings with
			{
				RepulsionStrength = 1_000_000.0,
				LinkSpringStrength = 0.3,
				DirectionalBias = 0.3,
				LinkFlatteningStrength = 0.3,
				LinkUntwistStrength = 0.05,
				GravityStrength = 20.0,
				OriginAnchorWeight = 0.2,
				DampingFactor = 0.95,
				RestLinkLength = 250.0,
				MaxForce = 3000.0,
				MaxVelocity = 100.0,
			});
		}

		ImGui.SameLine();
		if (DemoProbe.Button("Strong Physics"))
		{
			engine.UpdatePhysicsSettings(settings with
			{
				RepulsionStrength = 5_000_000.0,
				LinkSpringStrength = 1.0,
				DirectionalBias = 0.8,
				LinkFlatteningStrength = 1.0,
				LinkUntwistStrength = 0.25,
				GravityStrength = 100.0,
				OriginAnchorWeight = 0.4,
				DampingFactor = 0.85,
				MinRepulsionDistance = 30.0,
				RestLinkLength = 200.0,
				MaxForce = 10000.0,
				MaxVelocity = 300.0,
			});
		}

		ImGui.Separator();
		PhysicsSettingsPanel.DrawDiagnostics(engine);
	}

	private void RegisterNodeTypes()
	{
		// Register utility class nodes
		nodeFactory.RegisterNodeType<ConditionalNode>();
		nodeFactory.RegisterNodeType<RandomNode>();
		nodeFactory.RegisterNodeType<TimerNode>();
		nodeFactory.RegisterNodeType<CounterNode>();
		nodeFactory.RegisterNodeType<ArrayProcessorNode>();
		nodeFactory.RegisterNodeType<RectangleNode>();

		// Register primitive data type Make/Split/Set nodes
		nodeFactory.RegisterNodeType<MakeNumberNode>();
		nodeFactory.RegisterNodeType<SplitNumberNode>();
		nodeFactory.RegisterNodeType<SetNumberNode>();
		nodeFactory.RegisterNodeType<MakeIntegerNode>();
		nodeFactory.RegisterNodeType<SplitIntegerNode>();
		nodeFactory.RegisterNodeType<SetIntegerNode>();
		nodeFactory.RegisterNodeType<MakeBooleanNode>();
		nodeFactory.RegisterNodeType<SplitBooleanNode>();
		nodeFactory.RegisterNodeType<SetBooleanNode>();
		nodeFactory.RegisterNodeType<MakeStringNode>();
		nodeFactory.RegisterNodeType<SplitStringNode>();
		nodeFactory.RegisterNodeType<SetStringNode>();
		nodeFactory.RegisterNodeType<MakeVector2Node>();
		nodeFactory.RegisterNodeType<SplitVector2Node>();
		nodeFactory.RegisterNodeType<SetVector2Node>();

		// Register operation static classes
		nodeFactory.RegisterNodeType(typeof(MathOperations));
		nodeFactory.RegisterNodeType(typeof(AdvancedMath));
		nodeFactory.RegisterNodeType(typeof(StringOperations));
		nodeFactory.RegisterNodeType(typeof(TypeConversions));
		nodeFactory.RegisterNodeType(typeof(Comparisons));
		nodeFactory.RegisterNodeType(typeof(Collections));
		nodeFactory.RegisterNodeType(typeof(DateTimeOperations));
	}

	private void CreateDemoData()
	{
		// Get method references for creating method nodes
		System.Reflection.MethodInfo addMethod = typeof(MathOperations).GetMethod(nameof(MathOperations.Add))!;
		System.Reflection.MethodInfo multiplyMethod = typeof(MathOperations).GetMethod(nameof(MathOperations.Multiply))!;

		// Create Make/Split/Set pattern demo showing complete data lifecycle
		Node makeNumber1 = nodeFactory.CreateNode<MakeNumberNode>(new Vector2(50, 100));
		Node splitNumber1 = nodeFactory.CreateNode<SplitNumberNode>(new Vector2(250, 100));
		Node setNumber1 = nodeFactory.CreateNode<SetNumberNode>(new Vector2(450, 100));
		Node splitNumber2 = nodeFactory.CreateNode<SplitNumberNode>(new Vector2(650, 100));

		// Mathematical operation chain with data mutation
		Node makeNumber2 = nodeFactory.CreateNode<MakeNumberNode>(new Vector2(50, 250));
		Node addNode = nodeFactory.CreateMethodNode(addMethod, new Vector2(250, 250));
		Node setNumber2 = nodeFactory.CreateNode<SetNumberNode>(new Vector2(450, 250));

		// Vector processing with component updates
		Node makeVector = nodeFactory.CreateNode<MakeVector2Node>(new Vector2(50, 400));
		Node splitVector1 = nodeFactory.CreateNode<SplitVector2Node>(new Vector2(250, 400));
		Node multiplyNode = nodeFactory.CreateMethodNode(multiplyMethod, new Vector2(450, 350));
		Node setVector = nodeFactory.CreateNode<SetVector2Node>(new Vector2(650, 400));
		Node splitVector2 = nodeFactory.CreateNode<SplitVector2Node>(new Vector2(850, 400));

		// Create demo links showing Make/Split/Set lifecycle
		// Number lifecycle: Make → Split → Set (with modified value) → Split (final analysis)
		engine.TryCreateLink(makeNumber1.OutputPins[0].Id, splitNumber1.InputPins[0].Id);
		engine.TryCreateLink(makeNumber1.OutputPins[0].Id, setNumber1.InputPins[0].Id); // Original data to Set
		engine.TryCreateLink(splitNumber1.OutputPins[1].Id, setNumber1.InputPins[1].Id); // Absolute value as new value
		engine.TryCreateLink(setNumber1.OutputPins[0].Id, splitNumber2.InputPins[0].Id); // Updated data to final Split

		// Math operation with data update: Make → Add → Set (update existing with result)
		engine.TryCreateLink(makeNumber1.OutputPins[0].Id, addNode.InputPins[0].Id);
		engine.TryCreateLink(makeNumber2.OutputPins[0].Id, addNode.InputPins[1].Id);
		engine.TryCreateLink(makeNumber1.OutputPins[0].Id, setNumber2.InputPins[0].Id); // Original data
		engine.TryCreateLink(addNode.OutputPins[0].Id, setNumber2.InputPins[1].Id); // Add result as new value

		// Vector manipulation: Make → Split → Multiply component → Set → Split (final result)
		engine.TryCreateLink(makeVector.OutputPins[0].Id, splitVector1.InputPins[0].Id);
		engine.TryCreateLink(splitVector1.OutputPins[0].Id, multiplyNode.InputPins[0].Id); // X component
		engine.TryCreateLink(splitVector1.OutputPins[0].Id, multiplyNode.InputPins[1].Id); // X component (square it)
		engine.TryCreateLink(makeVector.OutputPins[0].Id, setVector.InputPins[0].Id); // Original vector
		engine.TryCreateLink(multiplyNode.OutputPins[0].Id, setVector.InputPins[1].Id); // X² as new X
		engine.TryCreateLink(splitVector1.OutputPins[1].Id, setVector.InputPins[2].Id); // Keep Y unchanged
		engine.TryCreateLink(setVector.OutputPins[0].Id, splitVector2.InputPins[0].Id); // Final vector analysis

		// A node whose parameters are edited rather than connected, which is what issue #437 asked
		// about. Typed pins with defaults are all an inline editor needs.
		engine.CreateNodeFromSpecs(
			new Vector2(50, 550),
			"Blob Filter",
			[
				new PinSpec("Threshold", typeof(double), 128.0),
				new PinSpec("AreaMin", typeof(double), 50.0),
				new PinSpec("Sigma", typeof(double), 2.0),
				new PinSpec("Invert", typeof(bool), false),
			],
			[new PinSpec("Count", typeof(int))]);

		// A comment box labelling a region of the graph, the way issue #468 asked for. It sits behind
		// the nodes, and dragging its title carries whatever lies inside it.
		engine.CreateCommentBox(new Vector2(20, 500), new Vector2(340, 240), "Parameters, not connections");
	}
}
