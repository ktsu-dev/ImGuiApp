// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Examples.App.Demos;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGuiNodeEditor;
using ktsu.NodeGraph.Library.Operations;
using ktsu.NodeGraph.Library.Primitives;
using ktsu.NodeGraph.Library.Utilities;
using ktsu.Semantics;

/// <summary>
/// Clean architecture ImNodes demo with proper separation of concerns
/// </summary>
internal sealed class CleanImNodesDemo : IDemoTab
{
	public string TabName => "Clean ImNodes";

	// Business logic layer
	private readonly NodeEditorEngine engine = new();
	private readonly AttributeBasedNodeFactory nodeFactory;

	// Presentation layers
	private readonly NodeEditorRenderer renderer = new();
	private readonly NodeEditorInputHandler inputHandler = new();

	// UI state
	private bool showDebugVisualization;
	private string lastActionMessage = "";
	private Vector4 lastActionColor = new(1.0f, 1.0f, 1.0f, 1.0f);
	private int lastSubstepCount;
	private float lastSubstepDeltaTime;

	public CleanImNodesDemo()
	{
		nodeFactory = new AttributeBasedNodeFactory(engine);
		RegisterNodeTypes();
		CreateDemoData();
		engine.InitializeWorldOriginToCentroid();
	}

	public void Update(float deltaTime)
	{
		// Inform physics which nodes are being dragged so they're excluded from simulation
		engine.SetDraggedNodes(renderer.CurrentlyDraggedNodes);

		// Update physics simulation
		engine.UpdatePhysics(deltaTime);

		// Store debug info
		(lastSubstepCount, lastSubstepDeltaTime) = engine.LastPhysicsStepInfo;
	}

	public void Render()
	{
		if (ImGui.BeginTabItem(TabName))
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

			ImGui.EndTabItem();
		}
	}

	private void RenderNodeEditor()
	{
		Vector2 editorAreaPos = ImGui.GetCursorScreenPos();
		Vector2 editorAreaSize = ImGui.GetContentRegionAvail();

		// Handle input events first
		ProcessInputEvents();

		// Render the editor
		renderer.Render(engine, editorAreaSize);

		// Update node positions/dimensions from ImNodes (AFTER rendering when context is active)
		UpdateNodeTransforms();

		// Render debug overlays on top
		renderer.RenderDebugOverlays(engine, editorAreaPos, editorAreaSize, showDebugVisualization);
	}

	private void ProcessInputEvents()
	{
		InputEvents events = inputHandler.ProcessInput();

		// Process link creation requests
		foreach (LinkCreationRequest request in events.LinkCreationRequests)
		{
			LinkCreationResult result = engine.TryCreateLink(request.FromPinId, request.ToPinId);

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

		// Process link deletion requests
		foreach (int linkId in events.LinkDeletionRequests)
		{
			if (engine.RemoveLink(linkId))
			{
				lastActionMessage = $"Link {linkId} deleted";
				lastActionColor = new Vector4(1.0f, 0.7f, 0.0f, 1.0f); // Orange
			}
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
		if (ImGui.Button("Add Input Node"))
		{
			Vector2 position = new(100, 100 + (engine.Nodes.Count * 50));
			engine.CreateNode(position, $"Input {engine.Nodes.Count + 1}", 0, 2);
		}

		ImGui.SameLine();
		if (ImGui.Button("Add Process Node"))
		{
			Vector2 position = new(300, 100 + (engine.Nodes.Count * 50));
			engine.CreateNode(position, $"Process {engine.Nodes.Count + 1}", 2, 2);
		}

		ImGui.SameLine();
		if (ImGui.Button("Add Output Node"))
		{
			Vector2 position = new(500, 100 + (engine.Nodes.Count * 50));
			engine.CreateNode(position, $"Output {engine.Nodes.Count + 1}", 2, 0);
		}

		if (ImGui.Button("Reset Demo"))
		{
			engine.Clear();
			CreateDemoData();
			engine.InitializeWorldOriginToCentroid();
			lastActionMessage = "Reset to demo data";
			lastActionColor = new Vector4(0.0f, 0.8f, 1.0f, 1.0f); // Cyan
		}

		ImGui.SameLine();
		if (ImGui.Button("Clear All"))
		{
			engine.Clear();
			lastActionMessage = "All nodes and links cleared";
			lastActionColor = new Vector4(1.0f, 0.7f, 0.0f, 1.0f); // Orange
		}

		// Physics settings
		ImGui.SeparatorText("Physics Simulation");
		RenderPhysicsControls();

		// Debug visualization toggle
		ImGui.Separator();
		ImGui.Checkbox("Show Debug Visualization", ref showDebugVisualization);

		// Status information
		ImGui.SeparatorText("Status");
		ImGui.Text($"Nodes: {engine.Nodes.Count}");
		ImGui.Text($"Links: {engine.Links.Count}");

		if (!string.IsNullOrEmpty(lastActionMessage))
		{
			ImGui.TextColored(lastActionColor, lastActionMessage);
		}

		// Debug information
		if (showDebugVisualization)
		{
			RenderDebugInformation();
		}
	}

	private void RenderDebugInformation()
	{
		ImGui.SeparatorText("Debug Information");

		// Node information
		if (ImGui.CollapsingHeader("Nodes"))
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
		if (ImGui.CollapsingHeader("Links"))
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

	private void RenderPhysicsControls()
	{
		PhysicsSettings currentSettings = engine.PhysicsSettings;
		bool settingsChanged = false;

		// Physics enabled checkbox
		bool enabled = currentSettings.Enabled;
		if (ImGui.Checkbox("Enable Physics", ref enabled))
		{
			currentSettings = currentSettings with { Enabled = enabled };
			settingsChanged = true;
		}

		if (!enabled)
		{
			ImGui.BeginDisabled();
		}

		// Repulsion settings
		if (ImGui.CollapsingHeader("Repulsion Forces"))
		{
			float repulsionStrength = currentSettings.RepulsionStrength.In(Units.Newton);
			if (ImGui.SliderFloat("Repulsion Strength (N)", ref repulsionStrength, 100_000.0f, 50_000_000.0f))
			{
				currentSettings = currentSettings with { RepulsionStrength = Force<float>.FromNewtons(repulsionStrength) };
				settingsChanged = true;
			}

			float minRepulsionDistance = currentSettings.MinRepulsionDistance.In(Units.Meter);
			if (ImGui.SliderFloat("Min Repulsion Clamp (px)", ref minRepulsionDistance, 10.0f, 200.0f))
			{
				currentSettings = currentSettings with { MinRepulsionDistance = Length<float>.FromMeters(minRepulsionDistance) };
				settingsChanged = true;
			}
		}

		// Link spring settings
		if (ImGui.CollapsingHeader("Link Springs"))
		{
			float linkSpringStrength = currentSettings.LinkSpringStrength;
			if (ImGui.SliderFloat("Spring Strength (dimensionless)", ref linkSpringStrength, 0.1f, 2.0f))
			{
				currentSettings = currentSettings with { LinkSpringStrength = linkSpringStrength };
				settingsChanged = true;
			}

			float restLinkLength = currentSettings.RestLinkLength.In(Units.Meter);
			if (ImGui.SliderFloat("Rest Length (m)", ref restLinkLength, 100.0f, 400.0f))
			{
				currentSettings = currentSettings with { RestLinkLength = Length<float>.FromMeters(restLinkLength) };
				settingsChanged = true;
			}
		}

		// Gravity settings
		if (ImGui.CollapsingHeader("Gravity"))
		{
			float gravityStrength = currentSettings.GravityStrength.In(Units.Newton);
			if (ImGui.SliderFloat("Gravity Strength (N)", ref gravityStrength, 0.0f, 200.0f))
			{
				currentSettings = currentSettings with { GravityStrength = Force<float>.FromNewtons(gravityStrength) };
				settingsChanged = true;
			}

			float originAnchorWeight = currentSettings.OriginAnchorWeight;
			if (ImGui.SliderFloat("Origin Anchor Weight", ref originAnchorWeight, 0.0f, 1.0f))
			{
				currentSettings = currentSettings with { OriginAnchorWeight = originAnchorWeight };
				settingsChanged = true;
			}
		}

		// Damping and limits
		(currentSettings, settingsChanged) = RenderDampingAndLimitsControls(currentSettings, settingsChanged, enabled);

		// Quick presets
		if (ImGui.Button("Gentle Physics"))
		{
			currentSettings = new PhysicsSettings
			{
				Enabled = true,
				RepulsionStrength = Force<float>.FromNewtons(2_000_000.0f),
				LinkSpringStrength = 0.3f,
				GravityStrength = Force<float>.FromNewtons(20.0f),
				OriginAnchorWeight = 0.2f,
				DampingFactor = 0.95f,
				MinRepulsionDistance = Length<float>.FromMeters(50.0f),
				RestLinkLength = Length<float>.FromMeters(250.0f),
				MaxForce = Force<float>.FromNewtons(300.0f),
				MaxVelocity = Velocity<float>.FromMetersPerSecond(100.0f),
				TargetPhysicsHz = Frequency<float>.FromHertz(120.0f)
			};
			settingsChanged = true;
		}

		ImGui.SameLine();
		if (ImGui.Button("Strong Physics"))
		{
			currentSettings = new PhysicsSettings
			{
				Enabled = true,
				RepulsionStrength = Force<float>.FromNewtons(10_000_000.0f),
				LinkSpringStrength = 1.0f,
				GravityStrength = Force<float>.FromNewtons(100.0f),
				OriginAnchorWeight = 0.4f,
				DampingFactor = 0.85f,
				MinRepulsionDistance = Length<float>.FromMeters(30.0f),
				RestLinkLength = Length<float>.FromMeters(200.0f),
				MaxForce = Force<float>.FromNewtons(800.0f),
				MaxVelocity = Velocity<float>.FromMetersPerSecond(300.0f),
				TargetPhysicsHz = Frequency<float>.FromHertz(120.0f)
			};
			settingsChanged = true;
		}

		if (!enabled)
		{
			ImGui.EndDisabled();
		}

		if (settingsChanged)
		{
			engine.UpdatePhysicsSettings(currentSettings);
		}
	}

	private (PhysicsSettings Settings, bool Changed) RenderDampingAndLimitsControls(PhysicsSettings currentSettings, bool settingsChanged, bool enabled)
	{
		if (ImGui.CollapsingHeader("Damping & Limits"))
		{
			float dampingFactor = currentSettings.DampingFactor;
			if (ImGui.SliderFloat("Damping Factor (dimensionless)", ref dampingFactor, 0.1f, 0.99f))
			{
				currentSettings = currentSettings with { DampingFactor = dampingFactor };
				settingsChanged = true;
			}

			float maxForce = currentSettings.MaxForce.In(Units.Newton);
			if (ImGui.SliderFloat("Max Force (N)", ref maxForce, 10.0f, 2000.0f))
			{
				currentSettings = currentSettings with { MaxForce = Force<float>.FromNewtons(maxForce) };
				settingsChanged = true;
			}

			float maxVelocity = currentSettings.MaxVelocity.In(Units.MetersPerSecond);
			if (ImGui.SliderFloat("Max Velocity (m/s)", ref maxVelocity, 5.0f, 500.0f))
			{
				currentSettings = currentSettings with { MaxVelocity = Velocity<float>.FromMetersPerSecond(maxVelocity) };
				settingsChanged = true;
			}

			float targetPhysicsHz = currentSettings.TargetPhysicsHz.In(Units.Hertz);
			if (ImGui.SliderFloat("Target Physics Hz", ref targetPhysicsHz, 60.0f, 240.0f))
			{
				currentSettings = currentSettings with { TargetPhysicsHz = Frequency<float>.FromHertz(targetPhysicsHz) };
				settingsChanged = true;
			}

			// Show actual physics timing info
			if (enabled && lastSubstepCount > 0)
			{
				ImGui.Separator();
				ImGui.Text($"Actual Substeps: {lastSubstepCount}");

				Time<float> substepTime = Time<float>.FromSeconds(lastSubstepDeltaTime);
				ImGui.Text($"Substep Δt: {substepTime.In(Units.Millisecond):F2}ms");

				Frequency<float> effectiveFrequency = Frequency<float>.FromHertz(1.0f / lastSubstepDeltaTime);
				ImGui.Text($"Effective Hz: {effectiveFrequency.In(Units.Hertz):F1}");

				// Visual indicator of physics quality
				bool targetAchieved = effectiveFrequency >= currentSettings.TargetPhysicsHz;
				Vector4 qualityColor = targetAchieved ?
					new Vector4(0.0f, 1.0f, 0.0f, 1.0f) : // Green - target achieved
					new Vector4(1.0f, 0.5f, 0.0f, 1.0f);   // Orange - below target

				ImGui.TextColored(qualityColor, targetAchieved ?
					"✓ Target achieved" : "⚠ Below target");

				// Stability detection
				ImGui.Text($"System Energy: {engine.TotalSystemEnergy:F2}");
				if (engine.IsStable)
				{
					ImGui.TextColored(new Vector4(0.0f, 1.0f, 0.0f, 1.0f), "Layout stable");
				}
			}
		}

		return (currentSettings, settingsChanged);
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
	}
}
