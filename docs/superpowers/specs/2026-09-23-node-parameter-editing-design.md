# Node parameter editing

Gives `ktsu.ImGui.NodeEditor` somewhere for a node's parameter values to live, and two ways to edit them: an editor drawn on an unconnected input pin's own row, and an inspector panel for the selected node.

The immediate reason is #437, which asked what the intended pattern is for editing parameters such as `Threshold`, `AreaMin`, `Sigma` and `EdgePolarity` on a machine vision node. The honest answer today is that there isn't one. Of the three patterns the question offered, constant nodes into input pins cannot work because no pin holds a value, an inspector panel cannot work because nothing maps a node back to its definition, and widgets inside the node body became reachable only when #441 landed.

## The problem

`AttributeBasedNodeFactory.CreateNode<T>` reads a `NodeDefinition` that knows each pin's `DataType` and `DefaultValue`, extracts the pin display names, and calls `engine.CreateNode(position, name, inputPinNames, outputPinNames)`. Everything else is dropped at that boundary. The factory never constructs `T`, so there is no instance either.

The runtime types have nowhere to put a value even if one survived. `Node` is id, position, name, pins, dimensions, velocity, force and pinned state. `Pin` is id, direction, name, display name and connection capacity. Neither carries a value, an instance or a user-data slot. Verified against a type declaring `[InputPin] public double Threshold { get; set; } = 128.0;`:

```
Pin members carrying a value: 0
Node members carrying an instance/payload: 0
Factory can map a node id back to its definition: False
```

The shipped node library states the intent clearly and then cannot act on it. `RandomNode.Min`, `RandomNode.Max` and `TimerNode.StartTime` are all `[InputPin]` properties with C# initializers for their defaults. That is a statement about where parameter state belongs. None of it reaches a created node.

So a host that wants an editable `Threshold` keeps a parallel dictionary of instances keyed by node id, re-deriving a mapping the factory already had and throwing away the type information the definition already carried.

## What changed while this was being designed

Three of the four gaps found while investigating #437 were fixed and merged before this design was finished. The design is written against that state, not against what was there at the start.

- **#439 landed.** `PinDefinition.DefaultValue` now falls back to reading the member off a lazily built prototype instance, so a C# initializer's value is visible. The attribute still wins where it supplied one. This design depended on that and no longer has to build it.
- **#441 landed**, but with a different shape than this design was going to propose. See the inline editor decision below.
- **#440 landed** as documentation, stating that `ktsu.NodeGraph` declares execution rather than running it.

#438 remains open and is what this design closes.

## Decisions

**Values live in the library, keyed by pin id.** The alternative was to keep the library metadata-only and ship a worked sample of the host-owned dictionary. That answers #437 without fixing it, and leaves every host reimplementing the same plumbing. Keying by pin id rather than by node and member name matches the existing `pinIdToOffset` side table exactly, and it means graphs built through the plain `engine.CreateNode(position, name, 2, 1)` path get values too, not only attribute-declared ones.

**The store is its own class, not more fields on the engine.** `NodeEditorEngine.cs` is already 587 lines carrying the graph and the physics delegation. `PinValueStore` goes in its own file, is exposed through engine methods, and is testable with no engine and no ImGui context.

**`Pin` gains `Type? DataType`.** Without it the renderer cannot choose an editor and the store cannot reject a mistyped write, and the host ends up writing the same type switch in every application. Null means untyped and accepts anything, which is what the two older `CreateNode` overloads produce.

**A `PinSpec` replaces the name list, and the capacity post-pass goes.** Today the factory creates pins from names and then calls `SetPinAllowsMultipleConnections` for each, matching declared pins to created pins by creation order. That works and is also the kind of thing that breaks quietly if either list is ever reordered. One spec carrying name, type, default and capacity removes the second pass:

```csharp
public readonly record struct PinSpec(
    string Name,
    Type? DataType = null,
    object? DefaultValue = null,
    bool? AllowMultipleConnections = null);

public Node CreateNodeFromSpecs(Vector2 position, string name,
    IReadOnlyList<PinSpec> inputs, IReadOnlyList<PinSpec> outputs);
```

Named `CreateNodeFromSpecs` rather than a third `CreateNode` overload: a `CreateNode` taking `IReadOnlyList<PinSpec>` is ambiguous against the existing `List<string>` overload for a `[]` collection expression argument, and that ambiguity broke the build. Both existing string-based overloads delegate to it internally; neither is removed, since dropping a public overload from a published package breaks whoever already calls it.

**Inline editors are the renderer's own feature, not a use of `DrawNodeBody`.** The hook that landed for #441 is `Action<Node>?`, called once per node after every pin and before `EndNode`. That placement is deliberate and guarded by `DrawNodeBody_LeavesThePinPositionsWhereTheyWere`: `PublishPinOffsets` measures from rows `RecordPinRow` collects as each pin is submitted, so host content drawn later cannot move where a link attaches. It produces a body under the pins, which is not the layout a node editor is expected to have:

```
┌─ Blob Filter ──────┐        ┌─ Blob Filter ──────────┐
│ Threshold   o      │        │ Threshold  [ 128.0  ]  │
│ AreaMin     o      │        │ AreaMin    [  50.0  ]  │
│          Count o   │   vs   │ Sigma   o───────────   │
│ [128.0] [50.0]     │        │             Count o    │
└────────────────────┘        └────────────────────────┘
   what DrawNodeBody           what this design draws
   can draw today
```

So the renderer draws editors itself, between `BeginInputAttribute` and `EndInputAttribute`, on the label's line, for an input pin that has a `DataType` and no link. `DrawNodeBody` keeps its contract and its guard test untouched, and the two compose: built-in editors on the rows, arbitrary host content underneath.

**The pin row is measured from a group.** An editor on the row genuinely makes that row taller, so the attach point genuinely moves, which is correct rather than a regression. But `RecordPinRow` reads `GetItemRectMin/Max` of the last submitted item, which with an editor present would be the editor rather than the row. Wrapping the label and editor in `BeginGroup`/`EndGroup` makes that rectangle cover the whole row. Two lines at the existing call site, and its own test.

**Selection is read back where hover already is.** Nothing in the library exposes what ImNodes has selected, though `GetSelectedNodes` and `NumSelectedNodes` are available. An inspector needs it. `ReadHoverState` runs after `EndNodeEditor` because that is when ImNodes will answer, and selection is answered in the same place, exposed as `IReadOnlySet<int> SelectedNodeIds` the way `CurrentlyDraggedNodes` already is.

**`NodeInspectorPanel` takes a node id, not a selection.** The host decides what it is inspecting, which keeps the panel free of ImNodes state and testable without a renderer.

**`ktsu.ImGui.NodeEditor` takes a dependency on `ktsu.ImGui.Widgets`,** so the inspector can use `PropertyGrid` rather than hand-rolling two columns. This was raised as a concern and confirmed as the maintainer's decision. The cost is real and worth recording: `ktsu.ImGui.Widgets` references `Hexa.NET.ImGui.Widgets.Extras`, which brings Roslyn with it, five packages and roughly 107 MB unpacked, restored and published by every node editor consumer whether or not they open an inspector. That is #384, and a package cannot prune its dependency's dependencies for its consumers. The rejected alternative was a separate `ktsu.ImGui.NodeEditor.Inspector` package holding the same code, which keeps the core lean at the price of one more project.

## The value model

```csharp
public sealed class PinValueStore
{
    public bool TrySet(Pin pin, object? value);   // false on type mismatch
    public object? Get(int pinId);
    public bool Reset(int pinId);                  // back to the declared default
    public void Forget(int pinId);
}
```

Exposed through the engine as `GetPinValue`, `SetPinValue`, `ResetPinValue` and `IsPinConnected`. The
shipped `PinValueStore` carries three members beyond this sketch: `Seed`, which `CreateNodeFromSpecs`
calls to record each pin's declared default (not type-checked itself, since a mistyped attribute
should still produce a creatable node), `Clear`, which the engine's own `Clear` delegates to, and a
`public static bool Accepts(Type? dataType, object? value)` doing the type check `TrySet` uses
internally. `Accepts` is public rather than private because `CreateNodeFromSpecs` needs the same
check on the way in, to decide whether a declared default can be seeded as written or has to be
converted to the pin's type first: a later fix (post-review) found that a mistyped
`[InputPin("AreaMin", DefaultValue = 50)]` on a `double` property was seeding a boxed `int`
unchecked, so `CreateNodeFromSpecs` now runs each spec's default through `Accepts`, attempts an
`IConvertible` conversion when it fails, and seeds null rather than throwing when neither works.

**Seeding and reset.** `CreateNode` seeds the store from each `PinSpec.DefaultValue`, and the store records that seed alongside the current value so `Reset` has something to go back to. A pin whose spec carried no default is seeded with null and resets to null. The engine looks the pin up by id and hands it to the store, which is why `TrySet` takes a `Pin` while the engine's `SetPinValue` takes a pin id.

Two details that will bite if left unspecified:

- **`Nullable<T>` must be unwrapped before the type check.** `typeof(double?).IsInstanceOfType(1.0)` is `false`, so a `[InputPin] public double? StartTime` would otherwise reject every value it was given. `TimerNode.StartTime` is exactly that shape, so this is not hypothetical.
- **A null `DataType` accepts anything.** That is what the two older `CreateNode` overloads produce, and refusing values there would make the untyped path worse than it is today.

**Lifetime.** `RemoveNode` forgets its pins' values and `Clear` empties the store. `RemoveNode` does not currently clear `pinIdToOffset` either, which is a small leak in code this design is already touching, so it gets fixed in the same pass.

## The two surfaces

Both use one `PinValueKind` classifier, so the set of editable types is defined once even though the inline path draws with raw ImGui and the inspector draws with `PropertyGrid` rows. It covers bool, int, float, double, string, `Vector2`, `Vector3`, enums, and `Nullable<T>` of those. An unrecognized type draws nothing inline and a disabled row in the inspector, which is what a pin looks like today.

`long` is deliberately not in that list. Dear ImGui has no `InputLong`, so a 64-bit integer needs an unsafe `DragScalar` with `ImGuiDataType.S64`, and one unverified interop call is not worth carrying into v1 for a type no motivating case uses. A `long` pin classifies as unsupported and both surfaces treat it as they treat any other unsupported type. Adding it later is a change to one classifier and one switch.

**Inline**, on the renderer:

```csharp
public bool DrawInlinePinEditors { get; set; } = true;
public float InlineEditorWidth { get; set; } = 90f;   // scaled by Zoom
```

Node dragging is not a problem: ImNodes marks an attribute active when a widget inside it is, which suppresses dragging for that frame, and `SetNodeDraggable` is available as a fallback. The UI tests confirm this rather than taking it on trust.

**Inspector:**

```csharp
public static class NodeInspectorPanel
{
    public static bool Draw(NodeEditorEngine engine, int nodeId);
}
```

Returns whether anything changed this frame, mirroring `PropertyGrid.Changed`. One row per input pin, in three cases, all visible rather than silently dropped:

- Unconnected and typed: an editable row.
- Connected: drawn inside `ScopedDisable`, because the value comes from upstream and editing the stored literal would be a lie.
- Untyped: a disabled row showing the name, so the panel does not quietly omit half a graph.

Output pins are skipped. There is no evaluator to produce a value for them, per #440.

## What this does not do

- **No per-row reset button.** `PropertyGrid` is two columns with no room for a third. `ResetPinValue` is public, so a host that wants the affordance builds it.
- **No multi-node editing.** One node at a time.
- **No evaluation.** #440 settled that executing a graph is the host's job, and nothing here changes it. Output pins hold values only if a host puts them there.
- **No type-aware link validation.** `TryCreateLink` still checks direction, duplicates, capacity and self-connection but never type, so the README's "type-aware connections" claim remains untrue. Making it true becomes cheap once `Pin.DataType` exists, but it changes which links existing graphs may make, so it is a follow-up issue rather than part of this. The README claim gets corrected here regardless.

## Testing

| Layer | Where | What |
|---|---|---|
| `PinValueStore` | `ImGui.NodeEditor.Tests`, no context | set and get, type rejection, `Nullable<T>` unwrapping, default seeding, reset, forget |
| Engine and factory | same, no context | `PinSpec` carries type and default through, `IsPinConnected` tracks links, `RemoveNode` forgets values, existing capacity tests still pass after the post-pass goes |
| Renderer | `ImGuiAppHarness`, real frames | editor present when unconnected and absent when connected, dragging it writes to the engine, the node does not drag while the editor is active, the pin offset lands on the row middle with an editor present, and `DrawNodeBody_LeavesThePinPositionsWhereTheyWere` still passes |
| Inspector | `ImGuiAppHarness` | a row per typed input, edits write through, a connected pin's row is disabled |

The capacity tests matter most. Removing the order-matched post-pass is the one change here that could silently break behavior that currently works.

## Files touched

| File | Change |
|---|---|
| `ImGui.NodeEditor/DomainModels.cs` | `Pin` gains `DataType`, new `PinSpec` |
| `ImGui.NodeEditor/PinValueStore.cs` | New |
| `ImGui.NodeEditor/NodeEditorEngine.cs` | `PinSpec` overload, value accessors, `IsPinConnected`, cleanup in `RemoveNode` and `Clear` |
| `ImGui.NodeEditor/AttributeBasedNodeFactory.cs` | Build `PinSpec`s, drop `ApplyConnectionCapacities` |
| `ImGui.NodeEditor/NodeEditorRenderer.cs` | Inline editors, group-measured pin rows, `SelectedNodeIds` |
| `ImGui.NodeEditor/PinValueKind.cs` | New, the shared classifier |
| `ImGui.NodeEditor/NodeInspectorPanel.cs` | New |
| `ImGui.NodeEditor/ImGui.NodeEditor.csproj` | Reference `ImGui.Widgets` |
| `ImGui.NodeEditor/README.md` | Parameters section, correct the type-aware claim |
| `examples/ImGuiAppDemo/Demos/CleanImNodesDemo.cs` | A parameters showcase |
| `tests/ImGui.NodeEditor.Tests/` | The four layers above |
| `tests/ImGuiAppDemo.UITests/` | Cover the demo section |
