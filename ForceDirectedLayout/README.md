# ktsu.ForceDirectedLayout

[![NuGet](https://img.shields.io/nuget/v/ktsu.ForceDirectedLayout?logo=nuget)](https://nuget.org/packages/ktsu.ForceDirectedLayout)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](https://github.com/ktsu-dev/ImGuiApp/blob/main/LICENSE.md)

ForceDirectedLayout settles a graph into a readable shape: bodies repel each other, edges pull like springs, gravity keeps the whole thing together, and overlaps are pushed apart. It is a pure simulation with no rendering, no UI dependency, and no runtime package dependencies — double precision throughout, AOT- and trim-clean, and exposed at three levels so a caller can pick how much ceremony they want. The same core is published as a native shared library for consumers outside .NET.

## Features

- **Three surfaces over one core**: a generic facade for your own body and edge types, a non-generic id-based facade for bulk POD submission, and the flat `LayoutCore` underneath
- **Renderer agnostic**: nothing here knows what a node looks like; `ktsu.ImGui.NodeEditor` is one consumer, an Unreal plugin driving the C ABI is another
- **Step or solve**: advance the simulation by a frame delta with automatic substepping, or run it to convergence with `Solve(maxIterations, tolerance)`
- **Stability reporting**: total system energy, an `IsStable` flag, and what the last step actually ran (substep count and substep delta)
- **Pinning and freezing**: a pinned body still pushes on others but does not move; a frozen body is one the user is currently dragging
- **Overlap resolution**: bodies have dimensions, so the layout separates boxes rather than points
- **AOT and trim clean**: `IsAotCompatible`, `IsTrimmable`, and analyzers enabled, with blittable POD settings and state structs
- **A C ABI**: `ForceDirectedLayout.Native` publishes a Native AOT shared library (`ktsu_force_directed_layout`) with a `Layout_*` entry point set and a generated `ktsu_force_directed_layout.h`

## Installation

### Package Manager Console

```powershell
Install-Package ktsu.ForceDirectedLayout
```

### .NET CLI

```bash
dotnet add package ktsu.ForceDirectedLayout
```

### Package Reference

```xml
<PackageReference Include="ktsu.ForceDirectedLayout" Version="x.y.z" />
```

## Usage Examples

### Basic Example — your own types

`ForceDirectedLayout<TBody, TEdge>` reads and writes your types through two accessor records, so the simulation never owns your model. `WithPhysicsState` returns the body with new position, velocity and force, which suits immutable records as readily as mutable classes.

```csharp
using ktsu.ForceDirectedLayout;

BodyAccessor<MyNode> bodies = new(
    GetId: n => n.Id,
    GetPosition: n => new Vec2D(n.X, n.Y),
    GetDimensions: n => new Vec2D(n.Width, n.Height),
    GetVelocity: n => n.Velocity,
    GetForce: n => n.Force,
    GetIsPinned: n => n.IsPinned,
    WithPhysicsState: (n, position, velocity, force) => n with
    {
        X = position.X,
        Y = position.Y,
        Velocity = velocity,
        Force = force,
    });

EdgeAccessor<MyEdge> edges = new(
    GetSourceBodyId: e => e.From,
    GetTargetBodyId: e => e.To);

ForceDirectedLayout<MyNode, MyEdge> layout = new(bodies, edges)
{
    Settings = new PhysicsSettings { Enabled = true },
};

// Once per frame
layout.SetFrozenBodies(draggedNodeIds);
layout.Step(nodes, links, deltaTime);
```

### Id-based submission

`ForceLayout` takes plain structs and hands back positions as a span, which is the shape a bulk producer or an interop caller wants.

```csharp
ForceLayout layout = new();

layout.SetNodes([
    new NodeInit { Id = 1, Position = new Vec2D(0, 0), Dimensions = new Vec2D(120, 60) },
    new NodeInit { Id = 2, Position = new Vec2D(300, 40), Dimensions = new Vec2D(120, 60) },
]);

layout.SetEdges([new EdgeInit { SourceBodyId = 1, TargetBodyId = 2 }]);

int iterations = layout.Solve(maxIterations: 500, tolerance: 0.5);

foreach (NodePosition node in layout.GetPositionsView())
{
    Console.WriteLine($"{node.Id}: {node.Position.X}, {node.Position.Y}");
}
```

### Tuning

Every force is a setting, and the defaults are tuned for node-editor-sized graphs.

```csharp
PhysicsSettings settings = new()
{
    Enabled = true,
    RepulsionStrength = 1_200_000.0,   // pairwise inverse-square repulsion
    LinkSpringStrength = 0.5,          // Hooke's-law constant for edges
    RestLinkLength = 225.0,            // spring rest length
    DirectionalBias = 0.5,             // biases sources left and targets right
    GravityStrength = 50.0,            // pull toward the gravity target
    OriginAnchorWeight = 1.0,          // 0 = centroid, 1 = world origin
    DampingFactor = 0.5,               // velocity retained per second
    MaxForce = 5000.0,
    MaxVelocity = 50.0,
    TargetPhysicsHz = 120.0,           // substep rate, independent of frame rate
    StabilityThreshold = 1.0,
    OverlapMargin = 20.0,
    MaxOverlapCorrection = 40.0,
};
```

### From native code

`ForceDirectedLayout.Native` publishes a shared library with a C entry point set — `Layout_Create`, `Layout_Destroy`, `Layout_SetSettings`, `Layout_SetNodes`, `Layout_SetEdges`, `Layout_Step`, `Layout_Solve`, `Layout_GetPositions`, `Layout_SetPinned`, `Layout_GetIndexOf`, `Layout_GetNodeCount` and `Layout_GetLastErrorMessage` — and ships `ktsu_force_directed_layout.h` beside the binary. The settings and node/edge structs are laid out sequentially and cross the ABI unchanged.

```bash
dotnet publish ForceDirectedLayout.Native -c Release -r win-x64
```

## API Reference

### `ForceDirectedLayout<TBody, TEdge>`

| Name | Return Type | Description |
| ---- | ----------- | ----------- |
| `Settings` | `PhysicsSettings` | The managed-facing settings; mutate freely between frames |
| `SetFrozenBodies(IReadOnlySet<int>)` | `void` | Bodies excluded from integration, typically the ones being dragged |
| `InitializeWorldOriginToCentroid(IReadOnlyList<TBody>)` | `void` | Anchors the world origin to the current centroid |
| `Step(IList<TBody>, IReadOnlyList<TEdge>, double)` | `void` | Advances the simulation and writes state back through the accessor |
| `LastStepInfo` | `(int SubstepCount, double SubstepDeltaTime)` | What the last step actually ran |

### `ForceLayout`

| Name | Return Type | Description |
| ---- | ----------- | ----------- |
| `SetNodes(ReadOnlySpan<NodeInit>)` / `SetEdges(ReadOnlySpan<EdgeInit>)` | `void` | Bulk submission of POD state |
| `Step(double)` | `void` | Advances by a delta, substepping to `TargetPhysicsHz` |
| `Solve(int, double)` | `int` | Runs to convergence, returning the iterations used |
| `GetPositions(Span<NodePosition>)` | `int` | Copies positions into a caller buffer |
| `GetPositionsView()` | `ReadOnlySpan<NodePosition>` | Zero-copy view of the current positions |
| `SetPinned(int, bool)` / `SetFrozen(int, bool)` | `void` | Pins or freezes a body by index |
| `SetPosition(int, Vec2D)` | `void` | Moves a body directly |
| `GetIndexOf(int)` | `int` | Index of a body id, or -1 |
| `InitializeWorldOriginToCentroid()` | `void` | Anchors the world origin to the current centroid |

### `LayoutCore`

The flat working buffers under both facades: `Settings`, `WorldOrigin`, `GravityCenter`, `TotalSystemEnergy`, `IsStable`, `LastStepInfo`, `Bodies`, `Edges`, `ResizeBodies`, `ResizeEdges`, `Step` and `Solve`. Use it when you want to own the buffers yourself.

### Supporting types

`Vec2D` (double-precision vector with the usual operators), `BodyState`, `EdgeRef`, `NodeInit`, `EdgeInit`, `NodePosition`, `LayoutSettings` (blittable POD, the C ABI form) and `PhysicsSettings` (the managed form, convertible both ways).

## Acknowledgments

This package has no runtime dependencies; [Polyfill](https://github.com/SimonCropp/Polyfill) is used at build time only, to backport newer APIs.

## Contributing

Contributions are welcome! For feature requests, bug reports, or questions, please open an issue on the GitHub repository. If you would like to contribute code, please open a pull request with your changes.

## License

ForceDirectedLayout is licensed under the MIT License. See [LICENSE.md](https://github.com/ktsu-dev/ImGuiApp/blob/main/LICENSE.md) for more information.
