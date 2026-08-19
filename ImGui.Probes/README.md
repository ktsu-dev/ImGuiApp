# ktsu.ImGui.Probes

Lets a user interface library record where it drew named items, so an automated test can address a
widget by name instead of by pixel position.

The package is deliberately tiny and depends on nothing but the ImGui binding. Marking is a
cross-cutting concern: widget libraries, dialog libraries and application hosts all want to mark
items, and none of them should have to depend on another to do it. Keeping the registry here means a
library can be probe-aware without adopting an application framework or a test framework.

## Marking an Item

Call immediately after submitting the widget, while it is still the current ImGui item:

```csharp
if (ImGui.Button("Save"))
{
	Save();
}

ImGuiProbes.MarkItem("save");
```

In production nothing is recording, and the call costs one null check, so a library can mark
unconditionally. `MarkItem(prefix, label)` builds a qualified name for a component that owns several
items, and `MarkRegion(name, min, max)` records an explicit rectangle for something drawn manually
rather than submitted as an ImGui item.

## Name Qualification

A bare label does not identify an item. Two widgets can share a label, and ImGui keeps them apart
through its identifier stack rather than through the label alone, so a name that ignored that context
would collide exactly where ImGui does not.

Names are recorded as the current ImGui window, then any pushed scopes, then the item's own name:

```csharp
ImGuiProbes.PushScope("inspector");
// items marked here become "Settings/inspector/<name>"
ImGuiProbes.PopScope();
```

`ktsu.ImGui.Widgets` pushes a probe scope from `ScopedId`, so anything already scoped for ImGui is
scoped for probes without further work.

## Recording

A test host installs a callback and receives every mark:

```csharp
ImGuiProbes.SetProbe((name, min, max) => record[name] = (min, max));
```

`Enabled` is a master switch, independent of whether a probe is installed. Setting it false suppresses
marking without disturbing the callback, which is useful to exclude a hot path from recording, or to
run a scenario twice and confirm the application behaves the same whether or not it is observed.
`IsRecording` reports whether both conditions hold, and is worth checking before composing a name
that costs something to build.

`ktsu.ImGui.App.Testing` consumes this package to resolve names to rectangles and click them.
