# ktsu.ForceDirectedLayout

[![NuGet](https://img.shields.io/nuget/v/ktsu.ForceDirectedLayout?logo=nuget)](https://nuget.org/packages/ktsu.ForceDirectedLayout)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](https://github.com/ktsu-dev/ImGuiApp/blob/main/LICENSE.md)

ForceDirectedLayout settles a graph into a readable shape: bodies repel each other across the clear space between their bounding boxes, edges pull like springs between the points they actually attach at, gravity keeps the whole thing together, edges are pulled towards horizontal and steep ones splayed apart so a renderer's curves stay clear of the bodies at their ends, and overlaps are pushed apart. Two edges meeting at one node put their far ends into the same vertical order as the pins they arrive at, so they stop crossing each other. Edges that run the wrong way reorder themselves. Both untangles are given the axis they travel on: the overlap pass separates them on the other one, rather than holding a pair apart on the very axis its swap has to cross, so nothing is left drawn overlapping once an untangle is done. It is a pure simulation with no rendering, no UI dependency, and no runtime package dependencies — double precision throughout, AOT- and trim-clean, and exposed at three levels so a caller can pick how much ceremony they want. The same core is published as a native shared library for consumers outside .NET.

## Features

- **Three surfaces over one core**: a generic facade for your own body and edge types, a non-generic id-based facade for bulk POD submission, and the flat `LayoutCore` underneath
- **Renderer agnostic**: nothing here knows what a node looks like; `ktsu.ImGui.NodeEditor` is one consumer, an Unreal plugin driving the C ABI is another
- **Step or solve**: advance the simulation by a frame delta with automatic substepping, or run it to convergence with `Solve(maxIterations, tolerance)`
- **Stability reporting**: total system energy, an `IsStable` flag, and what the last step actually ran (substep count and substep delta)
- **Pinning and freezing**: a pinned body still pushes on others but does not move; a frozen body is one the user is currently dragging
- **Boxes, not points**: bodies have dimensions, and the layout uses them — repulsion is measured between the two closest points on a pair's bounding boxes, so the same setting leaves the same room between a pair of literals as between a pair of classes, and an overlap pass separates any boxes that still end up drawn over one another
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
    RepulsionStrength = 600_000.0,     // inverse-square in the clear space between bounding boxes
    LinkSpringStrength = 0.5,          // Hooke's-law constant for edges
    RestLinkLength = 225.0,            // spring rest length
    DirectionalBias = 0.5,             // orders sources left of targets, reordering when needed
    LinkFlatteningStrength = 0.5,      // pulls edges towards horizontal, and keeps curves visible
    LinkFlatteningMargin = 0.0,        // extra clearance on top of the derived bound
    LinkUntwistStrength = 0.1,         // swaps two links sharing a node into their pins' order
    GravityStrength = 50.0,            // pull toward the gravity target
    OriginAnchorWeight = 1.0,          // 0 = centroid, 1 = world origin
    DampingFactor = 0.5,               // velocity retained per second
    MinRepulsionDistance = 50.0,       // floor on that clear space, so touching bodies push hard, not infinitely hard
    MaxForce = 5000.0,
    MaxVelocity = 250.0,             // also bounds how fast a graph settles
    TargetPhysicsHz = 120.0,           // substep rate, independent of frame rate
    StabilityThreshold = 1.0,
    OverlapMargin = 20.0,
    MaxOverlapCorrection = 40.0,
};
```

These values were not guessed. `tests/ForceDirectedLayout.Tests/Bench/` settles a corpus of graphs over a range of starting arrangements and reports what a layout measures — settled area, mean edge angle, links drawn across a body they are no end of, tightest clear gap, overlaps, crossed link pairs — because the simulation is chaotic and a single run says nothing. `LayoutBench.Sweep` walks one setting across a range and prints the rows as a table, `LayoutBench.Compare` puts named variants side by side, and `LayoutSvg` writes a settled graph out as SVG so it can be looked at rather than only read. Changing what a force measures changes the units its strength is in, so that is how a new default gets found.

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
