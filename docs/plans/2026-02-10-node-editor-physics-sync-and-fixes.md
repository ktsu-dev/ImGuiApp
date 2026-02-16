# Node Editor Physics Sync & Bug Fixes Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Fix physics position synchronization so physics-calculated positions are reflected in ImNodes, fix additional bugs, and add missing functionality.

**Architecture:** The node editor has clean separation: `NodeEditorEngine` (business logic/physics), `NodeEditorRenderer` (ImNodes display), `NodeEditorInputHandler` (input events). The primary bug is a missing Engine-to-ImNodes position sync path. The renderer only sets ImNodes positions on first render, then only reads FROM ImNodes, never writing physics updates back.

**Tech Stack:** C# / .NET 10, Hexa.NET.ImGui, Hexa.NET.ImNodes, ktsu.Semantics

---

## Investigation Summary

### Primary Bug: Physics positions never applied to ImNodes

**Root Cause:** In `NodeEditorRenderer.RenderNode()` (line 98-102), `ImNodes.SetNodeEditorSpacePos()` is only called when a node ID is NOT in `lastKnownNodePositions` (i.e., first render only). After that, physics updates `engine.Nodes[i].Position` but nothing pushes that to ImNodes. Meanwhile, `GetNodePositionUpdates()` reads FROM ImNodes and overwrites the engine's physics positions.

**Data flow (broken):**
- Physics -> engine.Nodes[i].Position (updated) -> Renderer.RenderNode() -> SKIPS SetNodeEditorSpacePos -> ImNodes stays stale
- ImNodes (stale) -> GetNodePositionUpdates() -> engine.UpdateNodePosition() -> OVERWRITES physics positions

**Proof:** The old `ImNodesDemo.cs` works because `ApplyForces()` (line 209) calls `ImNodes.SetNodeEditorSpacePos(node.Id, newPosition)` directly after every physics update.

**Debug UI works because:** It reads `engine.Nodes` directly and draws via `ImDrawListPtr`, bypassing ImNodes entirely.

### Additional Bugs Found

| # | Severity | Location | Description |
|---|----------|----------|-------------|
| 2 | High | `RenderNode:98` | `SetNodeEditorSpacePos` called AFTER `EndNode` - should be before `BeginNode` for immediate effect |
| 3 | High | `CleanImNodesDemo:88` | `UpdateNodeTransforms()` overwrites physics positions every frame |
| 4 | Medium | `NodeEditorEngine:449` | Physics stores Force in node record after integration (should be zero or pre-integration value) |
| 5 | Low | `NodeEditorRenderer:298` | Redundant `engine.Nodes.Count == 0` check INSIDE `foreach` over `engine.Nodes` |
| 6 | Low | `NodeEditorEngine:362` | DampingFactor applied both as velocity multiplier AND as link force component (double-damping) |
| 7 | Low | `DomainModels:14` | `Node` record contains mutable `List<Pin>` - breaks value equality semantics |
| 8 | Perf | `NodeEditorEngine:297` | O(N^2) repulsion + O(L*N) link force lookups via `FindNodeByPin` every substep |

### Suggested Enhancements

1. **Pinned/locked nodes** - Freeze specific nodes so physics doesn't move them
2. **Drag-aware physics** - Pause physics on node being dragged
3. **Physics stability detection** - Auto-disable when system energy drops below threshold
4. **Node deletion** - No keyboard-based node deletion support
5. **Layout algorithms** - Add tree/hierarchical/grid layout besides physics
6. **Serialization** - No save/load for the graph
7. **Undo/Redo** - No undo/redo support

---

## Task 1: Fix primary physics-to-ImNodes position sync

**Files:**
- Modify: `ImGuiNodeEditor/NodeEditorRenderer.cs:48-103`

**Step 1: Write the failing test scenario description**

No unit test project exists for ImGuiNodeEditor (ImNodes requires a GPU context). Document the expected behavior as a comment.

**Step 2: Fix `RenderNode` to always sync engine positions to ImNodes**

Replace the first-render-only guard with continuous position sync. The key insight: we need to detect whether the position change came from physics (engine -> ImNodes) or from user drag (ImNodes -> engine) and handle both directions.

```csharp
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

		// Right-align output pin text
		string pinText = pin.EffectiveDisplayName;
		Vector2 textSize = ImGui.CalcTextSize(pinText);
		float nodeContentWidth = CalculateNodeContentWidth(node);
		float paddingWidth = nodeContentWidth - textSize.X;

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
```

**Step 3: Build and verify it compiles**

Run: `dotnet build ImGuiNodeEditor/ImGuiNodeEditor.csproj`
Expected: Build succeeded

**Step 4: Run the demo app to visually verify**

Run: `dotnet run --project examples/ImGuiAppDemo`
Expected: Enable physics in Clean ImNodes tab - nodes should now move according to physics simulation

**Step 5: Commit**

```bash
git add ImGuiNodeEditor/NodeEditorRenderer.cs
git commit -m "[patch] Fix physics positions not reflected in ImNodes - sync engine positions before rendering"
```

---

## Task 2: Fix position sync feedback loop

**Files:**
- Modify: `ImGuiNodeEditor/NodeEditorRenderer.cs:108-131` (GetNodePositionUpdates)

**Step 1: Understand the problem**

After Task 1, there's a potential feedback loop: physics sets position -> renderer applies to ImNodes -> `GetNodePositionUpdates` detects "change" and pushes ImNodes position back to engine -> overwrites next frame's physics. We need `GetNodePositionUpdates` to only report positions that differ from the engine's current position (i.e., user drags).

**Step 2: Fix `GetNodePositionUpdates` to detect user drags vs physics updates**

```csharp
public Dictionary<int, Vector2> GetNodePositionUpdates(NodeEditorEngine engine)
{
	Dictionary<int, Vector2> updates = [];

	foreach (Node node in engine.Nodes)
	{
		// Only query positions for nodes that have been rendered at least once
		if (!lastKnownNodePositions.ContainsKey(node.Id))
		{
			continue;
		}

		Vector2 currentImNodesPos = ImNodes.GetNodeEditorSpacePos(node.Id);

		// Only report a change if the ImNodes position differs from the ENGINE position
		// This means the user dragged the node (ImNodes changed independently of us)
		if (Vector2.Distance(node.Position, currentImNodesPos) > 0.1f)
		{
			updates[node.Id] = currentImNodesPos;
			lastKnownNodePositions[node.Id] = currentImNodesPos;
		}
	}

	return updates;
}
```

**Step 3: Build and verify**

Run: `dotnet build ImGuiNodeEditor/ImGuiNodeEditor.csproj`
Expected: Build succeeded

**Step 4: Visual test**

Run: `dotnet run --project examples/ImGuiAppDemo`
Expected: Physics moves nodes AND manual dragging still works. After dragging, physics continues from new position.

**Step 5: Commit**

```bash
git add ImGuiNodeEditor/NodeEditorRenderer.cs
git commit -m "[patch] Fix position sync feedback loop - only report user drags, not physics updates"
```

---

## Task 3: Remove redundant check in physics debug rendering

**Files:**
- Modify: `ImGuiNodeEditor/NodeEditorRenderer.cs:296-301`

**Step 1: Remove the redundant `engine.Nodes.Count == 0` check inside the foreach loop**

```csharp
// BEFORE (line 296-301):
foreach (Node node in engine.Nodes)
{
	if (engine.Nodes.Count == 0)
	{
		continue;
	}
	// ... rest uses referenceNode from engine.Nodes[0]
}

// AFTER: Move referenceNode lookup outside the loop
```

The full fix:

```csharp
private static void RenderPhysicsDebugInfo(ImDrawListPtr drawList, NodeEditorEngine engine, Vector2 editorAreaPos, Vector2 editorAreaSize)
{
	Vector2 panning = ImNodes.EditorContextGetPanning();
	Vector2 editorCenter = editorAreaPos + (editorAreaSize * 0.5f);

	if (engine.Nodes.Count == 0)
	{
		return;
	}

	// Use reference node method for coordinate transformation
	Node referenceNode = engine.Nodes[0];
	Vector2 referenceScreenPos = ImNodes.GetNodeScreenSpacePos(referenceNode.Id);
	Vector2 referenceGridPos = referenceNode.Position;

	foreach (Node node in engine.Nodes)
	{
		Vector2 nodeCenter = node.Position + (node.Dimensions * 0.5f);
		Vector2 nodeCenterScreen = referenceScreenPos + (nodeCenter - (referenceGridPos + (referenceNode.Dimensions * 0.5f)));

		// Render force vector
		if (node.Force.Length() > 1.0f)
		{
			Vector2 forceEnd = nodeCenterScreen + (node.Force * 0.1f);
			uint forceColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 0.0f, 0.8f));
			drawList.AddLine(nodeCenterScreen, forceEnd, forceColor, 2.0f);
			drawList.AddCircleFilled(forceEnd, 3.0f, forceColor);
		}

		// Render velocity vector
		if (node.Velocity.Length() > 1.0f)
		{
			Vector2 velocityEnd = nodeCenterScreen + (node.Velocity * 0.5f);
			uint velocityColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 1.0f, 0.0f, 0.8f));
			drawList.AddLine(nodeCenterScreen, velocityEnd, velocityColor, 2.0f);
			drawList.AddCircleFilled(velocityEnd, 3.0f, velocityColor);
		}

		// Render repulsion zone
		float repulsionRadius = engine.PhysicsSettings.MinRepulsionDistance.In(Units.Meter);
		uint repulsionZoneColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.5f, 0.0f, 0.2f));
		drawList.AddCircle(nodeCenterScreen, repulsionRadius, repulsionZoneColor, 32, 1.0f);
	}

	// Render physics center (origin)
	Vector2 physicsCenterScreen = editorCenter + panning;
	uint physicsCenterColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 1.0f, 0.9f));
	drawList.AddCircleFilled(physicsCenterScreen, 8.0f, physicsCenterColor);
	drawList.AddCircle(physicsCenterScreen, 15.0f, physicsCenterColor, 16, 2.0f);
	drawList.AddText(physicsCenterScreen + new Vector2(20, -10), physicsCenterColor, "PHYSICS CENTER");
}
```

**Step 2: Build and verify**

Run: `dotnet build ImGuiNodeEditor/ImGuiNodeEditor.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add ImGuiNodeEditor/NodeEditorRenderer.cs
git commit -m "[patch] Remove redundant empty-check inside foreach in physics debug rendering"
```

---

## Task 4: Fix double-damping in physics simulation

**Files:**
- Modify: `ImGuiNodeEditor/NodeEditorEngine.cs:362`

**Step 1: Remove the velocity-based damping force from link forces**

The damping is already applied as a velocity multiplier in `UpdateNodePositions()` (line 434). Applying it again as a force in `CalculateLinkForces()` causes double-damping. Remove the damping force from link calculations and keep only the spring force.

In `CalculateLinkForces()`, change:
```csharp
// BEFORE:
// Damping force (velocity-based) - dimensionless damping coefficient
Vector2 relativeVelocity = inputNode.Velocity - outputNode.Velocity;
Vector2 dampingForce = relativeVelocity * PhysicsSettings.DampingFactor * 0.1f;

Vector2 totalForce = springForce + dampingForce;

// AFTER:
Vector2 totalForce = springForce;
```

**Step 2: Build and verify**

Run: `dotnet build ImGuiNodeEditor/ImGuiNodeEditor.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add ImGuiNodeEditor/NodeEditorEngine.cs
git commit -m "[patch] Remove double-damping in link force calculation"
```

---

## Task 5: Add pin-to-node lookup cache for physics performance

**Files:**
- Modify: `ImGuiNodeEditor/NodeEditorEngine.cs`

**Step 1: Add a pin-to-node-index dictionary**

Replace the O(N) `FindNodeByPin` calls in physics with O(1) dictionary lookups. The cache is rebuilt at the start of each physics update.

Add field:
```csharp
private readonly Dictionary<int, int> pinToNodeIndex = [];
```

Add method:
```csharp
private void RebuildPinToNodeIndex()
{
	pinToNodeIndex.Clear();
	for (int i = 0; i < nodes.Count; i++)
	{
		foreach (Pin pin in nodes[i].InputPins)
		{
			pinToNodeIndex[pin.Id] = i;
		}
		foreach (Pin pin in nodes[i].OutputPins)
		{
			pinToNodeIndex[pin.Id] = i;
		}
	}
}
```

Call `RebuildPinToNodeIndex()` at the start of `UpdatePhysics()`, before the substep loop.

Update `CalculateLinkForces()` to use the index:
```csharp
private void CalculateLinkForces()
{
	foreach (Link link in links)
	{
		if (!pinToNodeIndex.TryGetValue(link.OutputPinId, out int outputIndex) ||
			!pinToNodeIndex.TryGetValue(link.InputPinId, out int inputIndex))
		{
			continue;
		}

		Node outputNode = nodes[outputIndex];
		Node inputNode = nodes[inputIndex];

		// ... rest of spring force calculation using outputIndex/inputIndex directly ...
	}
}
```

**Step 2: Build and verify**

Run: `dotnet build ImGuiNodeEditor/ImGuiNodeEditor.csproj`
Expected: Build succeeded

**Step 3: Commit**

```bash
git add ImGuiNodeEditor/NodeEditorEngine.cs
git commit -m "[patch] Add pin-to-node lookup cache for O(1) physics force lookups"
```

---

## Task 6: Add node pinning support (freeze individual nodes)

**Files:**
- Modify: `ImGuiNodeEditor/DomainModels.cs` (add IsPinned to Node)
- Modify: `ImGuiNodeEditor/NodeEditorEngine.cs` (skip pinned nodes in physics)
- Modify: `examples/ImGuiAppDemo/Demos/CleanImNodesDemo.cs` (add pin toggle UI)

**Step 1: Add `IsPinned` property to Node record**

```csharp
public record Node(
	int Id,
	Vector2 Position,
	string Name,
	List<Pin> InputPins,
	List<Pin> OutputPins,
	Vector2 Dimensions = default,
	Vector2 Velocity = default,
	Vector2 Force = default,
	bool IsPinned = false
);
```

**Step 2: Add `ToggleNodePinned` method to engine**

```csharp
public void ToggleNodePinned(int nodeId)
{
	Node? node = nodes.FirstOrDefault(n => n.Id == nodeId);
	if (node != null)
	{
		int index = nodes.IndexOf(node);
		nodes[index] = node with { IsPinned = !node.IsPinned };
	}
}
```

**Step 3: Skip pinned nodes in `UpdateNodePositions`**

In the `UpdateNodePositions` loop, skip pinned nodes:
```csharp
Node node = nodes[i];
if (node.IsPinned)
{
	// Reset velocity and force for pinned nodes
	nodes[i] = node with { Velocity = Vector2.Zero, Force = Vector2.Zero };
	continue;
}
```

Note: Pinned nodes should still exert repulsion/spring forces on OTHER nodes, just not have their own position updated.

**Step 4: Build and verify**

Run: `dotnet build ImGuiNodeEditor/ImGuiNodeEditor.csproj`
Expected: Build succeeded

**Step 5: Commit**

```bash
git add ImGuiNodeEditor/DomainModels.cs ImGuiNodeEditor/NodeEditorEngine.cs
git commit -m "[minor] Add node pinning support - freeze individual nodes during physics simulation"
```

---

## Task 7: Add physics stability detection

**Files:**
- Modify: `ImGuiNodeEditor/NodeEditorEngine.cs`
- Modify: `ImGuiNodeEditor/DomainModels.cs` (add StabilityThreshold to PhysicsSettings)

**Step 1: Add stability threshold to PhysicsSettings**

```csharp
public record PhysicsSettings
{
	// ... existing properties ...
	public float StabilityThreshold { get; init; } = 1.0f; // Total energy below this = stable
}
```

**Step 2: Add stability detection to engine**

```csharp
public float TotalSystemEnergy { get; private set; }
public bool IsStable { get; private set; }
```

At the end of `UpdatePhysics()`, after the substep loop:
```csharp
// Calculate total system energy
TotalSystemEnergy = 0.0f;
for (int i = 0; i < nodes.Count; i++)
{
	TotalSystemEnergy += nodes[i].Velocity.LengthSquared();
}
IsStable = TotalSystemEnergy < PhysicsSettings.StabilityThreshold;
```

**Step 3: Display stability info in CleanImNodesDemo physics controls**

Add after the physics timing info:
```csharp
ImGui.Text($"System Energy: {engine.TotalSystemEnergy:F2}");
if (engine.IsStable)
{
	ImGui.TextColored(new Vector4(0.0f, 1.0f, 0.0f, 1.0f), "Layout stable");
}
```

**Step 4: Build and verify**

Run: `dotnet build`
Expected: Build succeeded

**Step 5: Commit**

```bash
git add ImGuiNodeEditor/NodeEditorEngine.cs ImGuiNodeEditor/DomainModels.cs examples/ImGuiAppDemo/Demos/CleanImNodesDemo.cs
git commit -m "[minor] Add physics stability detection with configurable energy threshold"
```

---

## Task 8: Add drag-aware physics (freeze node while being dragged)

**Files:**
- Modify: `ImGuiNodeEditor/NodeEditorRenderer.cs`
- Modify: `ImGuiNodeEditor/NodeEditorEngine.cs`

**Step 1: Track which nodes are being dragged**

In `NodeEditorRenderer`, after `ImNodes.EndNodeEditor()`, check for selected/dragged nodes:

Add to renderer:
```csharp
private readonly HashSet<int> currentlyDraggedNodes = [];

public IReadOnlySet<int> CurrentlyDraggedNodes => currentlyDraggedNodes;
```

In `GetNodePositionUpdates`, when a position difference is detected from ImNodes, add the node to dragged set. Clear the set when no position changes are detected for a node.

**Step 2: Skip dragged nodes in physics**

In the demo's `Update()`, set dragged nodes as temporarily pinned or skip them in physics:

```csharp
// In UpdateNodePositions, also skip nodes that are being dragged
if (node.IsPinned)
{
	nodes[i] = node with { Velocity = Vector2.Zero, Force = Vector2.Zero };
	continue;
}
```

The renderer already tracks position changes. If a node's ImNodes position changed (user drag), we skip physics for it that frame.

**Step 3: Build and verify**

Run: `dotnet build`
Expected: Build succeeded

**Step 4: Visual test**

Run: `dotnet run --project examples/ImGuiAppDemo`
Expected: Dragging a node while physics is active - the dragged node moves with mouse while other nodes continue physics simulation

**Step 5: Commit**

```bash
git add ImGuiNodeEditor/NodeEditorRenderer.cs ImGuiNodeEditor/NodeEditorEngine.cs
git commit -m "[minor] Add drag-aware physics - freeze node position while user is dragging it"
```
