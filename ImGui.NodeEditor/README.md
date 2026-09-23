# ktsu.ImGui.NodeEditor

[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGui.NodeEditor?logo=nuget)](https://nuget.org/packages/ktsu.ImGui.NodeEditor)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](https://github.com/ktsu-dev/ImGuiApp/blob/main/LICENSE.md)

ImGui.NodeEditor is a visual node editor built on ImNodes, with the graph itself kept away from the drawing. `NodeEditorEngine` owns nodes, links and the physics that lays them out and knows nothing about ImGui; `NodeEditorRenderer` draws whatever the engine holds; `NodeEditorInputHandler` turns a frame's interactions into requests the engine can accept or refuse. Nodes can be declared as ordinary types decorated with [`ktsu.NodeGraph`](https://github.com/ktsu-dev/ImGuiApp) attributes and instantiated by reflection.

## Features

- **Separation of concerns**: business logic (`NodeEditorEngine`), rendering (`NodeEditorRenderer`), and input (`NodeEditorInputHandler`) are separate objects, so the graph can be built and tested without a renderer
- **Tuning panel**: `PhysicsSettingsPanel` draws every layout setting, grouped and captioned, so a graph can be tuned while it is on screen
- **Attribute-based nodes**: `AttributeBasedNodeFactory` reads `ktsu.NodeGraph` attributes off a type — or every decorated type in an assembly — and creates nodes with the right pins
- **Physics is opt-in**: the simulation does nothing until `PhysicsSettings.Enabled` is set, so a host that positions nodes itself pays nothing for it
- **Connections are checked**: `TryCreateLink` returns a result with a message rather than throwing, and refuses a link that joins two pins of the same direction, duplicates one that exists, exceeds what a pin will accept, or joins a node to itself. It does not yet compare the pins' declared types
- **Physics-based layout**: nodes repel, links pull, and the graph settles; powered by [`ktsu.ForceDirectedLayout`](https://github.com/ktsu-dev/ImGuiApp), with per-frame stability and energy readings for debug overlays
- **Drag-aware**: nodes being dragged are excluded from the simulation, and the renderer reports position and size changes back to the engine

## Installation

### Package Manager Console

```powershell
Install-Package ktsu.ImGui.NodeEditor
```

### .NET CLI

```bash
dotnet add package ktsu.ImGui.NodeEditor
```

### Package Reference

```xml
<PackageReference Include="ktsu.ImGui.NodeEditor" Version="x.y.z" />
```

ImNodes must be initialized before the editor draws. `ktsu.ImGui.App` detects and sets up the extension automatically; in a host that does not, initialize ImNodes yourself as its bindings document.

## Usage Examples

### Basic Example

```csharp
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.NodeEditor;

private readonly NodeEditorEngine engine = new();
private readonly NodeEditorRenderer renderer = new();
private readonly NodeEditorInputHandler input = new();

// Build a graph
Node source = engine.CreateNode(new Vector2(200, 200), "Source", [], ["Value"]);
Node target = engine.CreateNode(new Vector2(500, 200), "Target", ["Source.Value"], []);
engine.TryCreateLink(source.OutputPins[0].Id, target.InputPins[0].Id);

// Draw it, once per frame
void DrawGraph(float deltaTime)
{
    renderer.Render(engine, ImGui.GetContentRegionAvail());

    // The renderer measures what ImNodes actually laid out; hand that back to the engine
    foreach ((int id, Vector2 position) in renderer.GetNodePositionUpdates(engine))
    {
        engine.UpdateNodePosition(id, position);
    }

    foreach ((int id, Vector2 dimensions) in renderer.GetNodeDimensionUpdates(engine))
    {
        engine.UpdateNodeDimensions(id, dimensions);
    }

    // Apply what the user did this frame
    InputEvents events = input.ProcessInput();
    foreach (LinkCreationRequest request in events.LinkCreationRequests)
    {
        engine.TryCreateLink(request.FromPinId, request.ToPinId);
    }

    foreach (int linkId in events.LinkDeletionRequests)
    {
        engine.RemoveLink(linkId);
    }

    engine.SetDraggedNodes(renderer.CurrentlyDraggedNodes);
    engine.UpdatePhysics(deltaTime);
}
```

### Nodes from decorated types

```csharp
using ktsu.NodeGraph;

[Node("Add")]
public class AddNode
{
    [InputPin("A")] public double A { get; set; }
    [InputPin("B")] public double B { get; set; }
    [OutputPin("Sum")] public double Sum { get; private set; }

    [NodeExecute]
    public void Execute() => Sum = A + B;
}

AttributeBasedNodeFactory factory = new(engine);
factory.RegisterNodeType<AddNode>();
factory.RegisterNodeTypesFromAssembly(typeof(AddNode).Assembly);

Node node = factory.CreateNode<AddNode>(new Vector2(100, 100));
```

`[NodeExecute]` marks `Execute` for whoever runs the graph to call. This editor is not that: registering a type gives you a node that **draws**, and nothing here constructs the type or invokes the method. Running a graph is the host's job — see [Running a graph](https://github.com/ktsu-dev/ImGuiApp/blob/main/NodeGraph/README.md#running-a-graph) in `ktsu.NodeGraph`.

`GetAllNodeDefinitions()` returns the registered definitions, which is what a "add node" menu is built from: each one carries the display name, category, tags, execution mode, deprecation state and pin list read off the attributes.

Two things to know about registration. A class node also gets an `Instance` output pin (and input pins for its constructor's parameters), so it can be chained onward. And `RegisterNodeTypesFromAssembly` skips abstract types — which in IL includes every `static class` — so a `[Node]` method parked on a static holder class has to be registered by naming that holder: `factory.RegisterNodeType(typeof(MathNodes))`.

### Editing a node's parameters

A pin carries the type it was declared with, and the engine holds a value for it. An unconnected
input pin whose type has an editor is drawn with one beside its label, and `NodeInspectorPanel`
draws the same parameters as a property grid for whichever node is selected.

```csharp
Node filter = engine.CreateNodeFromSpecs(
    new Vector2(100, 100),
    "Blob Filter",
    [
        new PinSpec("Threshold", typeof(double), 128.0),
        new PinSpec("Polarity", typeof(EdgePolarity), EdgePolarity.Rising), // your own enum
    ],
    [new PinSpec("Count", typeof(int))]);

// Whatever the user typed, or the declared default until they do
double threshold = (double)engine.GetPinValue(filter.InputPins[0].Id)!;

// A panel for the selected node
if (renderer.SelectedNodeIds.Count > 0)
{
    NodeInspectorPanel.Draw(engine, renderer.SelectedNodeIds.First());
}
```

A node built by `AttributeBasedNodeFactory` gets this for free: the type and default come off the
`[InputPin]` declaration, including a default written as a C# initializer.

Editable types are bool, int, float, double, string, `Vector2`, `Vector3` and enums, plus
`Nullable<T>` of those. Anything else, `long` included, draws no inline editor and a disabled row in
the inspector. A connected input pin gets no editor either, because its value arrives along the
link.

Values are held per pin, not per node instance. The library does not construct the type a node was
declared from and does not run it, so a graph's meaning is still the host's to give it.

### Tuning the layout

```csharp
engine.UpdatePhysicsSettings(new PhysicsSettings
{
    Enabled = true,
    RepulsionStrength = 900_000.0,
    LinkSpringStrength = 0.1,
    RestLinkLength = 50.0,
});

// Readings worth putting behind a debug toggle
float energy = engine.TotalSystemEnergy;
(int substeps, float substepDelta) = engine.LastPhysicsStepInfo;
renderer.RenderDebugOverlays(engine, editorPosition, editorSize, showDebug: true);
```

## API Reference

### `NodeEditorEngine`

The graph and its physics. No ImGui calls.

| Name | Return Type | Description |
| ---- | ----------- | ----------- |
| `Nodes` | `IReadOnlyList<Node>` | Every node |
| `Links` | `IReadOnlyList<Link>` | Every link |
| `GravityCenter` | `Vector2` | The point the layout pulls toward |
| `TotalSystemEnergy` | `float` | Current energy, for stability readouts |
| `LastPhysicsStepInfo` | `(int SubstepCount, float SubstepDeltaTime)` | What the last `UpdatePhysics` actually ran |
| `CreateNode(Vector2, string, int, int)` | `Node` | Creates a node with a number of unnamed pins |
| `CreateNode(Vector2, string, List<string>, List<string>)` | `Node` | Creates a node with named pins |
| `TryCreateLink(int, int)` | `LinkCreationResult` | Attempts a connection; the result carries success, a message, and the link |
| `RemoveLink(int)` / `RemoveNode(int)` | `bool` | Removes a link or node |
| `UpdateNodePosition(int, Vector2)` / `UpdateNodeDimensions(int, Vector2)` | `void` | Feeds measured layout back in |
| `SetDraggedNodes(IReadOnlySet<int>)` | `void` | Excludes dragged nodes from the simulation |
| `UpdatePhysicsSettings(PhysicsSettings)` | `void` | Replaces the physics settings |
| `UpdatePhysics(float)` | `void` | Advances the layout by a frame delta |

### `NodeEditorRenderer`

| Name | Return Type | Description |
| ---- | ----------- | ----------- |
| `Render(NodeEditorEngine, Vector2)` | `void` | Draws every node and link through ImNodes |
| `GetNodePositionUpdates(NodeEditorEngine)` | `Dictionary<int, Vector2>` | Positions ImNodes moved since the last frame |
| `GetNodeDimensionUpdates(NodeEditorEngine)` | `Dictionary<int, Vector2>` | Sizes ImNodes measured |
| `RenderDebugOverlays(...)` | `void` | Force and stability overlays |
| `CurrentlyDraggedNodes` | `IReadOnlySet<int>` | Nodes the user is dragging this frame |
| `DrawNodeBody` | `Action<Node>?` | Called inside each node, after its pins, to draw host content in the node body |

#### Host content in a node body

`DrawNodeBody` runs between ImNodes' `BeginNode` and `EndNode`, so anything it submits is drawn in
the node and sized into it:

```csharp
renderer.DrawNodeBody = node =>
{
    float value = values[node.Id];
    if (ImGui.SliderFloat("amount", ref value, 0f, 1f))
    {
        values[node.Id] = value;
    }
};
```

The ID stack is already the node's, so a label only has to be unique within the one node. It is
called after the pins rather than among them, which is what keeps the published pin offsets
measuring the rows a link is actually drawn to. An exception thrown out of it escapes before
`EndNode` and leaves the frame unusable, so a host that can fail should catch its own failures.

### `PhysicsSettingsPanel`

| Name | Return Type | Description |
| ---- | ----------- | ----------- |
| `Draw(ref PhysicsSettings)` | `bool` | Draws every tunable the simulation has, grouped by force and captioned; true when the user changed one |
| `DrawDiagnostics(NodeEditorEngine)` | `void` | Energy, whether it has settled, substep count and rate |

The forces interact, so none can be judged alone: raising repulsion changes what the spring's rest
length means, and levelling links only works in the room repulsion made. The panel therefore exposes
the whole of `PhysicsSettings` rather than a chosen subset — a setting that is not on it is one
nobody can reach without recompiling. Every control marks itself with `ktsu.ImGui.Probes`, so a UI
test can address it by the label the user sees.

```csharp
PhysicsSettings settings = engine.PhysicsSettings;
if (PhysicsSettingsPanel.Draw(ref settings))
{
    engine.UpdatePhysicsSettings(settings);
}

PhysicsSettingsPanel.DrawDiagnostics(engine);
```

### `NodeEditorInputHandler`

`ProcessInput()` returns `InputEvents`, holding `LinkCreationRequests` (`LinkCreationRequest(FromPinId, ToPinId)`) and `LinkDeletionRequests`.

### `AttributeBasedNodeFactory`

| Name | Return Type | Description |
| ---- | ----------- | ----------- |
| `RegisterNodeType<T>()` / `RegisterNodeType(Type)` | `void` | Registers a decorated type |
| `RegisterNodeTypesFromAssembly(Assembly)` | `void` | Registers every decorated type in an assembly |
| `CreateNode<T>(Vector2)` / `CreateNode(Type, Vector2)` | `Node` | Creates a node from a registered type |
| `CreateMethodNode(MethodInfo, Vector2)` | `Node` | Creates a node from a decorated method |
| `GetNodeDefinition(Type)` / `GetNodeDefinition(MethodInfo)` | `NodeDefinition?` | The metadata read off a registration |
| `GetAllNodeDefinitions()` | `IEnumerable<NodeDefinition>` | Every registration, for building menus |

### Domain models

`Node(Id, Position, Name, InputPins, OutputPins, Dimensions, Velocity, Force, IsPinned)`, `Link(Id, OutputPinId, InputPinId)` and `Pin(Id, Direction, Name, DisplayName)` are records; `PinDirection` is `Input` or `Output`.

## Acknowledgments

- [Dear ImGui](https://github.com/ocornut/imgui) - The immediate mode GUI library the editor draws into
- [Hexa.NET.ImGui](https://github.com/HexaEngine/Hexa.NET.ImGui) - The .NET bindings for Dear ImGui, and for `Hexa.NET.ImNodes`, the node editor extension this renders through
- [ktsu.Semantics](https://github.com/ktsu-dev/Semantics) - `ktsu.Semantics.Quantities` for typed quantities in the physics settings

`ktsu.NodeGraph` supplies the node metadata and `ktsu.ForceDirectedLayout` the physics; both ship from this repository.

## Contributing

Contributions are welcome! For feature requests, bug reports, or questions, please open an issue on the GitHub repository. If you would like to contribute code, please open a pull request with your changes.

## License

ImGui.NodeEditor is licensed under the MIT License. See [LICENSE.md](https://github.com/ktsu-dev/ImGuiApp/blob/main/LICENSE.md) for more information.
