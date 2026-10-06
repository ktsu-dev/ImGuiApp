# Node Parameter Editing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give `ktsu.ImGui.NodeEditor` a pin-keyed store for parameter values, and two ways to edit them: an editor on an unconnected input pin's own row, and an inspector panel for one node.

**Architecture:** `Pin` gains a `DataType`. A new `PinSpec` carries name, type, default and connection capacity into `NodeEditorEngine.CreateNode` in one go, replacing the pin name lists and the order-matched capacity post-pass. A `PinValueStore`, owned by the engine and testable alone, holds a value and a seeded default per pin id. One `PinValueKind` classifier decides which types are editable, so the inline renderer and the inspector never disagree.

**Tech Stack:** C# 13, .NET 10, MSTest.Sdk with Microsoft Testing Platform, Hexa.NET.ImGui 2.2.9, Hexa.NET.ImNodes 2.2.9, `ktsu.ImGui.App.Testing` harness for headless frames.

**Spec:** `docs/superpowers/specs/2026-09-23-node-parameter-editing-design.md`

## Global Constraints

- **Copyright header on every new C# file**, matching the neighbouring files in this project: `// Copyright (c) 2023-2026 ktsu-dev contributors`. This differs from the header named in `CLAUDE.md`; follow the project's files, not `CLAUDE.md`, inside `ImGui.NodeEditor` and its tests.
- **Tabs for indentation.** File-scoped namespaces with usings inside. Explicit types, never `var`. No `this.` qualifier. Always braces.
- **Never pass `--nologo` to `dotnet test`.** On Microsoft Testing Platform it reports `Zero tests ran` and exit code 5 instead of running anything (dotnet/sdk#55309).
- **`[assembly: DoNotParallelize]` already applies** to `tests/ImGui.NodeEditor.Tests`. ImGui contexts are global and the harness refuses to start while another is live.
- **Analyzers are errors here.** `IDE0005` (unused using), `IDE2001` (embedded statement on one line) and `IDE0055` (formatting) fail the build.
- **Run in Release for harness tests.** The software rasterizer is roughly 17x faster than in Debug.
- **Supported value kinds are exactly:** bool, int, float, double, string, `Vector2`, `Vector3`, enum, and `Nullable<T>` of those. `long` is deliberately unsupported in v1.
- **Test project path:** `tests/ImGui.NodeEditor.Tests/ImGui.NodeEditor.Tests.csproj`.

---

### Task 1: `PinSpec` and `Pin.DataType`

Gives a pin a declared type and gives `CreateNode` one parameter object carrying everything a pin needs, so the factory stops creating pins and then patching them.

**Files:**
- Modify: `ImGui.NodeEditor/DomainModels.cs`
- Modify: `ImGui.NodeEditor/NodeEditorEngine.cs` (the two `CreateNode` overloads, around lines 140-180)
- Test: `tests/ImGui.NodeEditor.Tests/NodeEditorEngineTests.cs`

**Interfaces:**
- Consumes: nothing from earlier tasks.
- Produces: `Pin.DataType` (`Type?`), `PinSpec(string Name, Type? DataType = null, bool? AllowMultipleConnections = null)`, and `NodeEditorEngine.CreateNode(Vector2 position, string name, IReadOnlyList<PinSpec> inputs, IReadOnlyList<PinSpec> outputs)` returning `Node`. Task 2 adds `DefaultValue` to `PinSpec`.

- [ ] **Step 1: Write the failing test**

Add to `tests/ImGui.NodeEditor.Tests/NodeEditorEngineTests.cs`:

```csharp
	[TestMethod]
	public void CreateNode_WithPinSpecs_CarriesTypeAndCapacityOntoThePins()
	{
		Node node = engine.CreateNode(
			new Vector2(0, 0),
			"Typed",
			[new PinSpec("Threshold", typeof(double)), new PinSpec("Tags", typeof(string), AllowMultipleConnections: true)],
			[new PinSpec("Count", typeof(int), AllowMultipleConnections: false)]);

		Assert.AreEqual(typeof(double), node.InputPins[0].DataType);
		Assert.AreEqual("Threshold", node.InputPins[0].EffectiveDisplayName);
		Assert.AreEqual("In 1", node.InputPins[0].Name);
		Assert.IsFalse(node.InputPins[0].AllowsMultipleConnections, "An input takes one link unless it says otherwise.");
		Assert.IsTrue(node.InputPins[1].AllowsMultipleConnections, "The spec asked for many.");
		Assert.IsFalse(node.OutputPins[0].AllowsMultipleConnections, "The spec asked for one.");
	}

	[TestMethod]
	public void CreateNode_WithPinNames_LeavesThePinsUntyped()
	{
		Node node = engine.CreateNode(new Vector2(0, 0), "Untyped", ["In"], ["Out"]);

		Assert.IsNull(node.InputPins[0].DataType, "A name carries no type, and inventing one would be a lie.");
		Assert.IsNull(node.OutputPins[0].DataType);
	}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/ImGui.NodeEditor.Tests/ImGui.NodeEditor.Tests.csproj --filter "FullyQualifiedName~CreateNode_WithPinSpecs|FullyQualifiedName~CreateNode_WithPinNames"`

Expected: build failure, `PinSpec` could not be found and `Pin` has no `DataType`.

- [ ] **Step 3: Add `DataType` to `Pin` and introduce `PinSpec`**

In `ImGui.NodeEditor/DomainModels.cs`, add the parameter to the `Pin` record (last, so every existing positional construction still compiles) and document it:

```csharp
/// <param name="DataType">
/// The .NET type this pin carries, or null when it is not known. Null is what the name-only
/// <see cref="NodeEditorEngine.CreateNode(Vector2, string, List{string}, List{string})"/> overloads
/// produce, and it means the pin accepts any value rather than none.
/// </param>
public record Pin(
	int Id,
	PinDirection Direction,
	string Name,
	string? DisplayName = null,
	bool? AllowMultipleConnections = null,
	Type? DataType = null
)
```

Add `using System;` to the file's usings if it is not already there.

Then add, in the same file, after the `Pin` record:

```csharp
/// <summary>
/// What a pin should be created as: everything the engine needs in one value, so a caller does not
/// create a pin and then patch it.
/// </summary>
/// <param name="Name">The pin's display name.</param>
/// <param name="DataType">The .NET type it carries, or null for an untyped pin.</param>
/// <param name="AllowMultipleConnections">
/// How many links it accepts, or null to take the default for its direction.
/// </param>
public readonly record struct PinSpec(
	string Name,
	Type? DataType = null,
	bool? AllowMultipleConnections = null
);
```

- [ ] **Step 4: Add the `CreateNode` overload and delegate the old ones to it**

In `ImGui.NodeEditor/NodeEditorEngine.cs`, replace the body of the `List<string>` overload so it builds specs and calls the new one, and add the new overload:

```csharp
	/// <summary>
	/// Create a new node with specified pin names. The pins are untyped.
	/// </summary>
	public Node CreateNode(Vector2 position, string name, List<string> inputPinNames, List<string> outputPinNames) =>
		CreateNode(
			position,
			name,
			[.. inputPinNames.Select(n => new PinSpec(n))],
			[.. outputPinNames.Select(n => new PinSpec(n))]);

	/// <summary>
	/// Create a new node from a description of each of its pins.
	/// </summary>
	/// <param name="position">Where to place the node.</param>
	/// <param name="name">The node's name.</param>
	/// <param name="inputs">Its input pins, in the order they should be drawn.</param>
	/// <param name="outputs">Its output pins, in the order they should be drawn.</param>
	/// <returns>The created node.</returns>
	public Node CreateNode(Vector2 position, string name, IReadOnlyList<PinSpec> inputs, IReadOnlyList<PinSpec> outputs)
	{
		List<Pin> inputPins = [];
		List<Pin> outputPins = [];

		foreach (PinSpec spec in inputs)
		{
			inputPins.Add(new Pin(
				nextPinId++,
				PinDirection.Input,
				$"In {inputPins.Count + 1}",
				spec.Name,
				spec.AllowMultipleConnections,
				spec.DataType));
		}

		foreach (PinSpec spec in outputs)
		{
			outputPins.Add(new Pin(
				nextPinId++,
				PinDirection.Output,
				$"Out {outputPins.Count + 1}",
				spec.Name,
				spec.AllowMultipleConnections,
				spec.DataType));
		}

		Node node = new(nextNodeId++, position, name, inputPins, outputPins);
		nodes.Add(node);
		return node;
	}
```

The `Name` stays `"In 1"` / `"Out 1"` with the caller's text as `DisplayName`, which is exactly what the old overload did. Changing that would rename every pin in every existing graph.

- [ ] **Step 5: Run the full test class to verify it passes and nothing regressed**

Run: `dotnet test tests/ImGui.NodeEditor.Tests/ImGui.NodeEditor.Tests.csproj --filter "FullyQualifiedName~NodeEditorEngineTests"`

Expected: PASS, including the pre-existing `CreateNode_WithPinCounts_NumbersThePins`.

- [ ] **Step 6: Commit**

```bash
git add ImGui.NodeEditor/DomainModels.cs ImGui.NodeEditor/NodeEditorEngine.cs tests/ImGui.NodeEditor.Tests/NodeEditorEngineTests.cs
git commit -m "[minor] Let a pin declare the type it carries"
```

---

### Task 2: `PinValueStore`

The store on its own, with no engine and no ImGui. Everything about type acceptance is decided here and tested here.

**Files:**
- Create: `ImGui.NodeEditor/PinValueStore.cs`
- Modify: `ImGui.NodeEditor/DomainModels.cs` (add `DefaultValue` to `PinSpec`)
- Test: `tests/ImGui.NodeEditor.Tests/PinValueStoreTests.cs` (create)

**Interfaces:**
- Consumes: `Pin` and `PinSpec` from Task 1.
- Produces: `PinValueStore` with `Seed(int pinId, object? defaultValue)`, `object? Get(int pinId)`, `bool TrySet(Pin pin, object? value)`, `bool Reset(int pinId)`, `void Forget(int pinId)`, `void Clear()`. `PinSpec` gains `object? DefaultValue` as its third positional parameter.

- [ ] **Step 1: Write the failing tests**

Create `tests/ImGui.NodeEditor.Tests/PinValueStoreTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor.Tests;

using System;

using ktsu.ImGui.NodeEditor;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives <see cref="PinValueStore"/> alone. It holds values and decides which ones a pin will
/// accept, and does so without an engine, a context or a frame.
/// </summary>
[TestClass]
public sealed class PinValueStoreTests
{
	private enum Polarity
	{
		Rising,
		Falling,
	}

	private readonly PinValueStore store = new();

	private static Pin TypedInput(int id, Type? dataType) =>
		new(id, PinDirection.Input, $"In {id}", DataType: dataType);

	[TestMethod]
	public void TrySet_WithAMatchingType_KeepsTheValue()
	{
		Pin pin = TypedInput(1, typeof(double));

		Assert.IsTrue(store.TrySet(pin, 128.0));
		Assert.AreEqual(128.0, store.Get(pin.Id));
	}

	[TestMethod]
	public void TrySet_WithTheWrongType_RefusesAndLeavesTheValueAlone()
	{
		Pin pin = TypedInput(1, typeof(double));
		store.TrySet(pin, 128.0);

		Assert.IsFalse(store.TrySet(pin, "not a number"), "A double pin has no reading of a string.");
		Assert.AreEqual(128.0, store.Get(pin.Id), "A refused write must not disturb what was there.");
	}

	[TestMethod]
	public void TrySet_OnAnUntypedPin_AcceptsAnything()
	{
		Pin pin = TypedInput(1, dataType: null);

		Assert.IsTrue(store.TrySet(pin, "anything"), "An untyped pin is what the name-only overloads make, and refusing every value would make them worse than they are.");
		Assert.AreEqual("anything", store.Get(pin.Id));
	}

	/// <summary>
	/// A boxed <c>double?</c> is just a boxed double, so <c>typeof(double?).IsInstanceOfType(1.0)</c>
	/// is false and the obvious check would refuse every value a nullable pin was ever given.
	/// <c>TimerNode.StartTime</c> is exactly this shape.
	/// </summary>
	[TestMethod]
	public void TrySet_OnANullablePin_AcceptsTheUnderlyingValueAndNull()
	{
		Pin pin = TypedInput(1, typeof(double?));

		Assert.IsTrue(store.TrySet(pin, 1.5));
		Assert.AreEqual(1.5, store.Get(pin.Id));

		Assert.IsTrue(store.TrySet(pin, null));
		Assert.IsNull(store.Get(pin.Id));
	}

	[TestMethod]
	public void TrySet_WithNullOnANonNullableValueType_Refuses()
	{
		Pin pin = TypedInput(1, typeof(double));

		Assert.IsFalse(store.TrySet(pin, null), "A double is never absent.");
	}

	[TestMethod]
	public void TrySet_WithAnEnumValue_KeepsIt()
	{
		Pin pin = TypedInput(1, typeof(Polarity));

		Assert.IsTrue(store.TrySet(pin, Polarity.Falling));
		Assert.AreEqual(Polarity.Falling, store.Get(pin.Id));
	}

	[TestMethod]
	public void Get_ForAPinNobodyHasWrittenTo_IsNull()
	{
		Assert.IsNull(store.Get(99));
	}

	[TestMethod]
	public void Reset_ReturnsTheValueToWhatItWasSeededWith()
	{
		Pin pin = TypedInput(1, typeof(double));
		store.Seed(pin.Id, 128.0);
		store.TrySet(pin, 200.0);

		Assert.IsTrue(store.Reset(pin.Id));
		Assert.AreEqual(128.0, store.Get(pin.Id));
	}

	[TestMethod]
	public void Reset_ForAPinThatWasNeverSeeded_ReportsSo()
	{
		Assert.IsFalse(store.Reset(99), "There is no default to go back to.");
	}

	[TestMethod]
	public void Forget_DropsTheValueAndItsDefault()
	{
		Pin pin = TypedInput(1, typeof(double));
		store.Seed(pin.Id, 128.0);

		store.Forget(pin.Id);

		Assert.IsNull(store.Get(pin.Id));
		Assert.IsFalse(store.Reset(pin.Id), "Forgetting a pin forgets what it defaulted to as well.");
	}
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/ImGui.NodeEditor.Tests/ImGui.NodeEditor.Tests.csproj --filter "FullyQualifiedName~PinValueStoreTests"`

Expected: build failure, `PinValueStore` could not be found.

- [ ] **Step 3: Add `DefaultValue` to `PinSpec`**

In `ImGui.NodeEditor/DomainModels.cs`, add the parameter in third position with its documentation:

```csharp
/// <param name="DefaultValue">
/// What the pin holds before anything sets it, or null for nothing. This is the value
/// <see cref="PinValueStore.Reset"/> goes back to.
/// </param>
public readonly record struct PinSpec(
	string Name,
	Type? DataType = null,
	object? DefaultValue = null,
	bool? AllowMultipleConnections = null
);
```

Task 1's call sites used `AllowMultipleConnections:` by name, so they still compile.

- [ ] **Step 4: Write `PinValueStore`**

Create `ImGui.NodeEditor/PinValueStore.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor;

using System;
using System.Collections.Generic;

/// <summary>
/// What each pin currently holds, and what it should go back to.
/// </summary>
/// <remarks>
/// Keyed by pin id rather than by node and member name, which is what the engine's pin offset table
/// is keyed by and what lets a graph built through the name-only <c>CreateNode</c> overloads hold
/// values too, rather than only an attribute-declared one.
/// <para>
/// It holds values and decides which ones a pin accepts. It knows nothing about drawing, nothing
/// about the graph's shape, and nothing about what a connected pin means: a pin fed by a link still
/// has whatever literal was last written to it, and it is the caller that decides to prefer the
/// link.
/// </para>
/// </remarks>
public sealed class PinValueStore
{
	private readonly Dictionary<int, object?> values = [];
	private readonly Dictionary<int, object?> defaults = [];

	/// <summary>
	/// Record what a pin starts at and should reset to.
	/// </summary>
	/// <param name="pinId">The pin.</param>
	/// <param name="defaultValue">Its declared default, which may be null.</param>
	/// <remarks>
	/// The seed is not type checked. It comes from the pin's own declaration, so refusing it here
	/// would mean a node that cannot be created rather than a value that cannot be set.
	/// </remarks>
	public void Seed(int pinId, object? defaultValue)
	{
		defaults[pinId] = defaultValue;
		values[pinId] = defaultValue;
	}

	/// <summary>
	/// What a pin currently holds.
	/// </summary>
	/// <param name="pinId">The pin.</param>
	/// <returns>Its value, or null if it has none.</returns>
	public object? Get(int pinId) => values.TryGetValue(pinId, out object? value) ? value : null;

	/// <summary>
	/// Write a value to a pin, if the pin will take it.
	/// </summary>
	/// <param name="pin">The pin, which carries the type the value has to match.</param>
	/// <param name="value">The value.</param>
	/// <returns>True if it was written, false if the pin's type refused it.</returns>
	public bool TrySet(Pin pin, object? value)
	{
		ArgumentNullException.ThrowIfNull(pin);

		if (!Accepts(pin.DataType, value))
		{
			return false;
		}

		values[pin.Id] = value;
		return true;
	}

	/// <summary>
	/// Put a pin back to the value it was seeded with.
	/// </summary>
	/// <param name="pinId">The pin.</param>
	/// <returns>True if it had a seed to go back to.</returns>
	public bool Reset(int pinId)
	{
		if (!defaults.TryGetValue(pinId, out object? seeded))
		{
			return false;
		}

		values[pinId] = seeded;
		return true;
	}

	/// <summary>Drop everything held for one pin.</summary>
	/// <param name="pinId">The pin.</param>
	public void Forget(int pinId)
	{
		values.Remove(pinId);
		defaults.Remove(pinId);
	}

	/// <summary>Drop everything held for every pin.</summary>
	public void Clear()
	{
		values.Clear();
		defaults.Clear();
	}

	/// <summary>
	/// Whether a pin of this type will take this value.
	/// </summary>
	/// <param name="dataType">The pin's declared type, or null when it has none.</param>
	/// <param name="value">The value offered.</param>
	/// <returns>True if the value may be stored.</returns>
	/// <remarks>
	/// A null type accepts anything. Otherwise the check is against the underlying type, because a
	/// boxed <c>T?</c> is indistinguishable from a boxed <c>T</c> and
	/// <c>typeof(double?).IsInstanceOfType(1.0)</c> is false. Null is accepted for a reference type
	/// and for a <c>Nullable{T}</c>, and refused for any other value type.
	/// <para>
	/// The match is exact rather than widening: an int offered to a double pin is refused. The
	/// editors write the pin's own type, so a mismatch is a caller's bug and is better reported than
	/// silently converted.
	/// </para>
	/// </remarks>
	public static bool Accepts(Type? dataType, object? value)
	{
		if (dataType is null)
		{
			return true;
		}

		Type underlying = Nullable.GetUnderlyingType(dataType) ?? dataType;

		return value is null
			? !underlying.IsValueType || underlying != dataType
			: underlying.IsInstanceOfType(value);
	}
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test tests/ImGui.NodeEditor.Tests/ImGui.NodeEditor.Tests.csproj --filter "FullyQualifiedName~PinValueStoreTests"`

Expected: PASS, 10 tests.

- [ ] **Step 6: Commit**

```bash
git add ImGui.NodeEditor/PinValueStore.cs ImGui.NodeEditor/DomainModels.cs tests/ImGui.NodeEditor.Tests/PinValueStoreTests.cs
git commit -m "[minor] Hold a value and a default per pin"
```

---

### Task 3: Wire the store into the engine

Seeds values as nodes are created, answers whether a pin is connected, and forgets what a removed node held. Also fixes the pre-existing pin offset leak in `RemoveNode`.

**Files:**
- Modify: `ImGui.NodeEditor/NodeEditorEngine.cs`
- Test: `tests/ImGui.NodeEditor.Tests/NodeEditorEngineTests.cs`

**Interfaces:**
- Consumes: `PinValueStore` and `PinSpec.DefaultValue` from Task 2.
- Produces: `NodeEditorEngine.GetPinValue(int pinId)` returning `object?`, `SetPinValue(int pinId, object? value)` returning `bool`, `ResetPinValue(int pinId)` returning `bool`, `IsPinConnected(int pinId)` returning `bool`.

- [ ] **Step 1: Write the failing tests**

Add to `tests/ImGui.NodeEditor.Tests/NodeEditorEngineTests.cs`:

```csharp
	[TestMethod]
	public void CreateNode_SeedsEachPinWithItsDeclaredDefault()
	{
		Node node = engine.CreateNode(
			new Vector2(0, 0),
			"Seeded",
			[new PinSpec("Threshold", typeof(double), 128.0), new PinSpec("Label", typeof(string))],
			[]);

		Assert.AreEqual(128.0, engine.GetPinValue(node.InputPins[0].Id));
		Assert.IsNull(engine.GetPinValue(node.InputPins[1].Id), "A spec with no default seeds nothing.");
	}

	[TestMethod]
	public void SetPinValue_RefusesAValueThePinsTypeWillNotTake()
	{
		Node node = engine.CreateNode(new Vector2(0, 0), "Typed", [new PinSpec("Threshold", typeof(double), 128.0)], []);
		int pinId = node.InputPins[0].Id;

		Assert.IsFalse(engine.SetPinValue(pinId, "not a number"));
		Assert.AreEqual(128.0, engine.GetPinValue(pinId));
	}

	[TestMethod]
	public void SetPinValue_ForAPinThatDoesNotExist_ReportsSo()
	{
		Assert.IsFalse(engine.SetPinValue(999, 1.0));
	}

	[TestMethod]
	public void ResetPinValue_ReturnsThePinToItsDeclaredDefault()
	{
		Node node = engine.CreateNode(new Vector2(0, 0), "Seeded", [new PinSpec("Threshold", typeof(double), 128.0)], []);
		int pinId = node.InputPins[0].Id;
		engine.SetPinValue(pinId, 200.0);

		Assert.IsTrue(engine.ResetPinValue(pinId));
		Assert.AreEqual(128.0, engine.GetPinValue(pinId));
	}

	[TestMethod]
	public void IsPinConnected_FollowsTheLinks()
	{
		(Node source, Node target) = TwoConnectedNodes();

		Assert.IsTrue(engine.IsPinConnected(source.OutputPins[0].Id));
		Assert.IsTrue(engine.IsPinConnected(target.InputPins[0].Id));

		engine.RemoveLink(engine.Links[0].Id);

		Assert.IsFalse(engine.IsPinConnected(source.OutputPins[0].Id), "The link is gone, so the pin is free again.");
	}

	[TestMethod]
	public void RemoveNode_ForgetsWhatItsPinsHeld()
	{
		Node node = engine.CreateNode(new Vector2(0, 0), "Doomed", [new PinSpec("Threshold", typeof(double), 128.0)], []);
		int pinId = node.InputPins[0].Id;

		engine.RemoveNode(node.Id);

		Assert.IsNull(engine.GetPinValue(pinId), "A removed node's values would otherwise outlive it for the life of the process.");
		Assert.IsFalse(engine.TryGetPinOffset(pinId, out _), "And so would its measured pin offsets.");
	}

	[TestMethod]
	public void Clear_ForgetsEveryValue()
	{
		Node node = engine.CreateNode(new Vector2(0, 0), "Seeded", [new PinSpec("Threshold", typeof(double), 128.0)], []);
		int pinId = node.InputPins[0].Id;

		engine.Clear();

		Assert.IsNull(engine.GetPinValue(pinId));
	}
```

For `RemoveNode_ForgetsWhatItsPinsHeld` to observe the offset, record one first. Insert this line immediately before the `engine.RemoveNode(node.Id);` call in that test:

```csharp
		engine.UpdatePinOffset(pinId, new Vector2(4, 8));
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/ImGui.NodeEditor.Tests/ImGui.NodeEditor.Tests.csproj --filter "FullyQualifiedName~NodeEditorEngineTests"`

Expected: build failure, `GetPinValue` and the rest are not defined on `NodeEditorEngine`.

- [ ] **Step 3: Add the store, the accessors, and the seeding**

In `ImGui.NodeEditor/NodeEditorEngine.cs`, add the field next to the other side tables (near `pinIdToOffset`):

```csharp
	/// <summary>What each pin holds, and what it resets to. See <see cref="PinValueStore"/>.</summary>
	private readonly PinValueStore pinValues = new();
```

Seed inside the `PinSpec` overload of `CreateNode` from Task 1. After `nodes.Add(node);` and before `return node;`:

```csharp
		for (int i = 0; i < inputPins.Count; i++)
		{
			pinValues.Seed(inputPins[i].Id, inputs[i].DefaultValue);
		}

		for (int i = 0; i < outputPins.Count; i++)
		{
			pinValues.Seed(outputPins[i].Id, outputs[i].DefaultValue);
		}
```

Add the public accessors after `TryGetPinOffset`:

```csharp
	/// <summary>
	/// What a pin currently holds.
	/// </summary>
	/// <param name="pinId">The pin.</param>
	/// <returns>Its value, or null if it has none.</returns>
	/// <remarks>
	/// A connected input pin still reports the literal last written to it. Whether that literal or
	/// the link's value is the one that matters is a question about evaluating the graph, which this
	/// library does not do.
	/// </remarks>
	public object? GetPinValue(int pinId) => pinValues.Get(pinId);

	/// <summary>
	/// Write a value to a pin.
	/// </summary>
	/// <param name="pinId">The pin.</param>
	/// <param name="value">The value.</param>
	/// <returns>True if it was written, false if there is no such pin or its type refused the value.</returns>
	public bool SetPinValue(int pinId, object? value)
	{
		Pin? pin = FindPin(pinId);
		return pin is not null && pinValues.TrySet(pin, value);
	}

	/// <summary>
	/// Put a pin back to the value it was created with.
	/// </summary>
	/// <param name="pinId">The pin.</param>
	/// <returns>True if it had a default to go back to.</returns>
	public bool ResetPinValue(int pinId) => pinValues.Reset(pinId);

	/// <summary>
	/// Whether any link meets this pin.
	/// </summary>
	/// <param name="pinId">The pin.</param>
	/// <returns>True if at least one link ends at it.</returns>
	public bool IsPinConnected(int pinId) =>
		links.Any(l => l.OutputPinId == pinId || l.InputPinId == pinId);
```

- [ ] **Step 4: Forget a removed node's pins, and clear on `Clear`**

In `RemoveNode`, after the connected links are removed and before `nodes.Remove(node);`:

```csharp
		// A removed node's pins are gone, so what they held and where they were measured goes with
		// them. Without this both tables grow for the life of the process.
		foreach (Pin pin in node.InputPins.Concat(node.OutputPins))
		{
			pinValues.Forget(pin.Id);
			pinIdToOffset.Remove(pin.Id);
		}
```

In `Clear`, alongside the existing resets:

```csharp
		pinValues.Clear();
		pinIdToOffset.Clear();
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test tests/ImGui.NodeEditor.Tests/ImGui.NodeEditor.Tests.csproj --filter "FullyQualifiedName~NodeEditorEngineTests"`

Expected: PASS, the whole class.

- [ ] **Step 6: Commit**

```bash
git add ImGui.NodeEditor/NodeEditorEngine.cs tests/ImGui.NodeEditor.Tests/NodeEditorEngineTests.cs
git commit -m "[minor] Give the engine a value per pin"
```

---

### Task 4: The factory builds `PinSpec`s

Carries the type and default the definition already knew onto the created pins, and removes the order-matched capacity post-pass.

**Files:**
- Modify: `ImGui.NodeEditor/AttributeBasedNodeFactory.cs` (`CreateNode(Type, Vector2)` around lines 112-136, `CreateMethodNode` around 144-168, delete `ApplyConnectionCapacities` around 181-193)
- Test: `tests/ImGui.NodeEditor.Tests/AttributeBasedNodeFactoryTests.cs`

**Interfaces:**
- Consumes: `PinSpec` with `DefaultValue` from Task 2, engine value accessors from Task 3.
- Produces: no new API. Behaviour change only.

- [ ] **Step 1: Write the failing test**

Add to `tests/ImGui.NodeEditor.Tests/AttributeBasedNodeFactoryTests.cs`. Put the node type next to the other fixture types already in that file:

```csharp
	[Node("Blob Filter")]
	private sealed class BlobFilterNode
	{
		[InputPin("Threshold", Order = 0)]
		public double Threshold { get; set; } = 128.0;

		[InputPin("AreaMin", Order = 1, DefaultValue = 50.0)]
		public double AreaMin { get; set; }

		[OutputPin("Count", AllowMultipleConnections = false)]
		public int Count => 0;
	}

	[TestMethod]
	public void CreateNode_CarriesEachPinsTypeOntoTheGraph()
	{
		AttributeBasedNodeFactory factory = new(engine);
		factory.RegisterNodeType<BlobFilterNode>();

		Node node = factory.CreateNode<BlobFilterNode>(new Vector2(0, 0));

		Assert.AreEqual(typeof(double), node.InputPins[0].DataType);
		Assert.AreEqual(typeof(double), node.InputPins[1].DataType);
	}

	/// <summary>
	/// The definition has known each pin's default since #439 taught it to read a C# initializer.
	/// Until now it dropped them on the way to the graph, so a node was created holding nothing.
	/// </summary>
	[TestMethod]
	public void CreateNode_SeedsEachPinFromItsDeclaredDefault()
	{
		AttributeBasedNodeFactory factory = new(engine);
		factory.RegisterNodeType<BlobFilterNode>();

		Node node = factory.CreateNode<BlobFilterNode>(new Vector2(0, 0));

		Assert.AreEqual(128.0, engine.GetPinValue(node.InputPins[0].Id), "Read from the property initializer.");
		Assert.AreEqual(50.0, engine.GetPinValue(node.InputPins[1].Id), "Read from the attribute.");
	}

	[TestMethod]
	public void CreateNode_StillAppliesDeclaredConnectionCapacities()
	{
		AttributeBasedNodeFactory factory = new(engine);
		factory.RegisterNodeType<BlobFilterNode>();

		Node node = factory.CreateNode<BlobFilterNode>(new Vector2(0, 0));

		Assert.IsFalse(
			node.OutputPins.Single(p => p.EffectiveDisplayName == "Count").AllowsMultipleConnections,
			"An output fans out by default, and this one declared otherwise.");
	}
```

Make sure the file's usings include `System.Linq` for `Single`, and `System.Numerics` for `Vector2`. Add them only if absent, since `IDE0005` makes an unused using an error.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/ImGui.NodeEditor.Tests/ImGui.NodeEditor.Tests.csproj --filter "FullyQualifiedName~AttributeBasedNodeFactoryTests"`

Expected: the two new type and default tests FAIL (`DataType` is null, `GetPinValue` returns null). `CreateNode_StillAppliesDeclaredConnectionCapacities` PASSES already, which is what makes it a regression guard rather than a new feature.

- [ ] **Step 3: Build specs in both creation paths**

In `ImGui.NodeEditor/AttributeBasedNodeFactory.cs`, replace the name extraction and the `ApplyConnectionCapacities` call in `CreateNode(Type, Vector2)` with:

```csharp
		Node node = engine.CreateNode(
			position,
			definition.DisplayName,
			ToSpecs(definition.InputPins),
			ToSpecs(definition.OutputPins));

		return node;
```

Make the identical replacement in `CreateMethodNode`.

Then delete both `ApplyConnectionCapacities` overloads and add in their place:

```csharp
	/// <summary>
	/// Turn declared pins into what the engine creates pins from.
	/// </summary>
	/// <param name="pins">The declared pins, in any order.</param>
	/// <returns>One spec per pin, ordered as the declaration asked.</returns>
	/// <remarks>
	/// This replaced a pass that created pins from names and then set each one's connection capacity
	/// by index, matching declared pins to created pins by the order they happened to be created in.
	/// Carrying everything in one value means there is no second list to fall out of step with.
	/// </remarks>
	private static List<PinSpec> ToSpecs(List<PinDefinition> pins) =>
	[
		.. pins
			.OrderBy(p => p.Order)
			.Select(p => new PinSpec(
				p.DisplayName,
				p.DataType,
				p.DefaultValue,
				p.AllowMultipleConnections)),
	];
```

- [ ] **Step 4: Run the whole factory test class to verify it passes**

Run: `dotnet test tests/ImGui.NodeEditor.Tests/ImGui.NodeEditor.Tests.csproj --filter "FullyQualifiedName~AttributeBasedNodeFactoryTests"`

Expected: PASS. Every pre-existing capacity and ordering test in this class must still pass. If one fails, the ordering in `ToSpecs` disagrees with what the old two-pass code did, and that is the bug.

- [ ] **Step 5: Commit**

```bash
git add ImGui.NodeEditor/AttributeBasedNodeFactory.cs tests/ImGui.NodeEditor.Tests/AttributeBasedNodeFactoryTests.cs
git commit -m "[minor] Carry a declared pin's type and default onto the graph"
```

---

### Task 5: `PinValueKind`

One place that decides which types are editable, so the two drawing surfaces cannot disagree.

**Files:**
- Create: `ImGui.NodeEditor/PinValueKind.cs`
- Test: `tests/ImGui.NodeEditor.Tests/PinValueKindTests.cs` (create)

**Interfaces:**
- Consumes: nothing.
- Produces: `enum PinValueKind { Unsupported, Boolean, Int32, Single, Double, String, Vector2, Vector3, Enum }` and `static PinValueKind PinValueKinds.Classify(Type? dataType)`.

- [ ] **Step 1: Write the failing tests**

Create `tests/ImGui.NodeEditor.Tests/PinValueKindTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor.Tests;

using System.Numerics;

using ktsu.ImGui.NodeEditor;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// The one place that decides which pin types can be edited. Both the inline editors and the
/// inspector ask it, so that a type either has an editor on both surfaces or on neither.
/// </summary>
[TestClass]
public sealed class PinValueKindTests
{
	private enum Polarity
	{
		Rising,
		Falling,
	}

	[TestMethod]
	public void Classify_RecognizesEverySupportedType()
	{
		Assert.AreEqual(PinValueKind.Boolean, PinValueKinds.Classify(typeof(bool)));
		Assert.AreEqual(PinValueKind.Int32, PinValueKinds.Classify(typeof(int)));
		Assert.AreEqual(PinValueKind.Single, PinValueKinds.Classify(typeof(float)));
		Assert.AreEqual(PinValueKind.Double, PinValueKinds.Classify(typeof(double)));
		Assert.AreEqual(PinValueKind.String, PinValueKinds.Classify(typeof(string)));
		Assert.AreEqual(PinValueKind.Vector2, PinValueKinds.Classify(typeof(Vector2)));
		Assert.AreEqual(PinValueKind.Vector3, PinValueKinds.Classify(typeof(Vector3)));
		Assert.AreEqual(PinValueKind.Enum, PinValueKinds.Classify(typeof(Polarity)));
	}

	[TestMethod]
	public void Classify_LooksThroughNullable()
	{
		Assert.AreEqual(PinValueKind.Double, PinValueKinds.Classify(typeof(double?)));
		Assert.AreEqual(PinValueKind.Enum, PinValueKinds.Classify(typeof(Polarity?)));
	}

	[TestMethod]
	public void Classify_AnUntypedPin_IsUnsupported()
	{
		Assert.AreEqual(PinValueKind.Unsupported, PinValueKinds.Classify(null));
	}

	/// <summary>
	/// Dear ImGui has no InputLong, so a 64-bit integer needs an unsafe DragScalar. It is left out of
	/// v1 rather than carried in on one unverified interop call.
	/// </summary>
	[TestMethod]
	public void Classify_Int64_IsUnsupportedForNow()
	{
		Assert.AreEqual(PinValueKind.Unsupported, PinValueKinds.Classify(typeof(long)));
	}

	[TestMethod]
	public void Classify_ATypeWithNoEditor_IsUnsupported()
	{
		Assert.AreEqual(PinValueKind.Unsupported, PinValueKinds.Classify(typeof(NodeEditorEngine)));
	}
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/ImGui.NodeEditor.Tests/ImGui.NodeEditor.Tests.csproj --filter "FullyQualifiedName~PinValueKindTests"`

Expected: build failure, `PinValueKind` could not be found.

- [ ] **Step 3: Write the classifier**

Create `ImGui.NodeEditor/PinValueKind.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor;

using System;
using System.Numerics;

/// <summary>
/// The kinds of value a pin can be edited as.
/// </summary>
public enum PinValueKind
{
	/// <summary>No editor. The pin is drawn as it always was.</summary>
	Unsupported,

	/// <inheritdoc cref="bool"/>
	Boolean,

	/// <inheritdoc cref="int"/>
	Int32,

	/// <inheritdoc cref="float"/>
	Single,

	/// <inheritdoc cref="double"/>
	Double,

	/// <inheritdoc cref="string"/>
	String,

	/// <inheritdoc cref="System.Numerics.Vector2"/>
	Vector2,

	/// <inheritdoc cref="System.Numerics.Vector3"/>
	Vector3,

	/// <summary>Any enumeration, drawn as a list of its names.</summary>
	Enum,
}

/// <summary>
/// Decides which pin types have an editor.
/// </summary>
/// <remarks>
/// Both the inline editors on a node face and <see cref="NodeInspectorPanel"/> classify through
/// this, so a type has an editor on both surfaces or on neither. The two draw differently, one with
/// raw ImGui and one with a property grid, and that is the only difference between them.
/// </remarks>
public static class PinValueKinds
{
	/// <summary>
	/// What kind of editor a pin of this type needs.
	/// </summary>
	/// <param name="dataType">The pin's declared type, or null when it has none.</param>
	/// <returns>Its kind, or <see cref="PinValueKind.Unsupported"/> if nothing here can edit it.</returns>
	/// <remarks>
	/// <c>long</c> is deliberately unsupported. Dear ImGui has no <c>InputLong</c>, so it needs an
	/// unsafe <c>DragScalar</c> with <c>ImGuiDataType.S64</c>, which is not worth one unverified
	/// interop call in v1. Adding it is a case here and a case in each surface's switch.
	/// </remarks>
	public static PinValueKind Classify(Type? dataType)
	{
		if (dataType is null)
		{
			return PinValueKind.Unsupported;
		}

		Type underlying = Nullable.GetUnderlyingType(dataType) ?? dataType;

		if (underlying.IsEnum)
		{
			return PinValueKind.Enum;
		}

		return underlying switch
		{
			Type t when t == typeof(bool) => PinValueKind.Boolean,
			Type t when t == typeof(int) => PinValueKind.Int32,
			Type t when t == typeof(float) => PinValueKind.Single,
			Type t when t == typeof(double) => PinValueKind.Double,
			Type t when t == typeof(string) => PinValueKind.String,
			Type t when t == typeof(Vector2) => PinValueKind.Vector2,
			Type t when t == typeof(Vector3) => PinValueKind.Vector3,
			_ => PinValueKind.Unsupported,
		};
	}
}
```

The `<see cref="NodeInspectorPanel"/>` reference resolves in Task 8. Until then the build passes but emits no warning, because `CS1591` and friends are in this project's `NoWarn`. If the documentation build complains, leave the reference and complete Task 8.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test tests/ImGui.NodeEditor.Tests/ImGui.NodeEditor.Tests.csproj --filter "FullyQualifiedName~PinValueKindTests"`

Expected: PASS, 5 tests.

- [ ] **Step 5: Commit**

```bash
git add ImGui.NodeEditor/PinValueKind.cs tests/ImGui.NodeEditor.Tests/PinValueKindTests.cs
git commit -m "[minor] Name the pin types that can be edited"
```

---

### Task 6: Inline editors on the pin row

Draws an editor beside an unconnected input pin's label, and measures the pin row from a group so the link still attaches to the middle of the row.

**Files:**
- Modify: `ImGui.NodeEditor/NodeEditorRenderer.cs` (`RenderNode` input pin loop around lines 356-363, `RecordPinRow` around 581)
- Test: `tests/ImGui.NodeEditor.Tests/InlinePinEditorTests.cs` (create)

**Interfaces:**
- Consumes: `PinValueKinds.Classify` from Task 5, `engine.GetPinValue` / `SetPinValue` / `IsPinConnected` from Task 3.
- Produces: `NodeEditorRenderer.DrawInlinePinEditors` (`bool`, default true) and `NodeEditorRenderer.InlineEditorWidth` (`float`, default 90).

- [ ] **Step 1: Write the failing tests**

Create `tests/ImGui.NodeEditor.Tests/InlinePinEditorTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor.Tests;

using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.NodeEditor;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives the editors the renderer draws on a node face. An editor is made of what ImNodes lays out
/// and what the mouse does to it, so none of this is observable without drawing real frames.
/// </summary>
[TestClass]
public sealed class InlinePinEditorTests
{
	private static readonly HarnessOptions Viewport = new() { Width = 900, Height = 700 };

	private readonly NodeEditorEngine engine = new();
	private readonly NodeEditorRenderer renderer = new();

	private ImGuiAppHarness harness = null!;

	[TestCleanup]
	public void TearDown() => harness?.Dispose();

	private void Start()
	{
		harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Title = nameof(InlinePinEditorTests),
				OnRender = _ => DrawGraph(),
				SaveIniSettings = false,
			},
			Viewport);

		harness.Step(3);
	}

	private void DrawGraph()
	{
		renderer.Render(engine, ImGui.GetContentRegionAvail());

		foreach (KeyValuePair<int, Vector2> update in renderer.GetNodePositionUpdates(engine))
		{
			engine.UpdateNodePosition(update.Key, update.Value);
		}

		foreach (KeyValuePair<int, Vector2> update in renderer.GetNodeDimensionUpdates(engine))
		{
			engine.UpdateNodeDimensions(update.Key, update.Value);
		}
	}

	/// <summary>A node whose one input is an unconnected, typed, editable pin.</summary>
	private Node TypedNode() =>
		engine.CreateNode(
			new Vector2(250, 200),
			"Blob Filter",
			[new PinSpec("Threshold", typeof(double), 128.0)],
			[new PinSpec("Count", typeof(int))]);

	/// <summary>
	/// An editor is wider than a label, so a node carrying one is wider than the same node without.
	/// That difference is what says an editor was drawn at all.
	/// </summary>
	[TestMethod]
	public void AnUnconnectedTypedInput_IsDrawnWiderThanItsLabelAlone()
	{
		TypedNode();
		renderer.DrawInlinePinEditors = false;
		Start();
		harness.Step(5);
		float withoutEditor = engine.Nodes[0].Dimensions.X;

		renderer.DrawInlinePinEditors = true;
		harness.Step(5);
		float withEditor = engine.Nodes[0].Dimensions.X;

		Assert.IsGreaterThan(withoutEditor, withEditor, "The editor should have widened the node.");
	}

	/// <summary>
	/// A connected pin takes its value from the link, so offering a box to type a different one in
	/// would be offering a value that nothing reads.
	/// </summary>
	[TestMethod]
	public void AConnectedInput_GetsNoEditor()
	{
		Node target = TypedNode();
		Node source = engine.CreateNode(new Vector2(0, 200), "Source", [], [new PinSpec("Value", typeof(double))]);
		Start();
		harness.Step(5);
		float unconnected = engine.Nodes[0].Dimensions.X;

		engine.TryCreateLink(source.OutputPins[0].Id, target.InputPins[0].Id);
		harness.Step(5);

		Assert.IsLessThan(unconnected, engine.Nodes[0].Dimensions.X, "Connecting the pin should have taken its editor away.");
	}

	[TestMethod]
	public void AnUnsupportedType_GetsNoEditor()
	{
		engine.CreateNode(
			new Vector2(250, 200),
			"Opaque",
			[new PinSpec("Engine", typeof(NodeEditorEngine))],
			[]);
		renderer.DrawInlinePinEditors = false;
		Start();
		harness.Step(5);
		float withoutEditors = engine.Nodes[0].Dimensions.X;

		renderer.DrawInlinePinEditors = true;
		harness.Step(5);

		Assert.AreEqual(withoutEditors, engine.Nodes[0].Dimensions.X, "Nothing here can edit a NodeEditorEngine, so nothing should have been drawn.");
	}

	/// <summary>
	/// The point of the whole feature: what the user does to the editor reaches the graph.
	/// </summary>
	[TestMethod]
	public void DraggingTheEditor_WritesThroughToTheEngine()
	{
		Node node = TypedNode();
		Start();
		harness.Step(5);

		Assert.IsTrue(renderer.TryGetNodeScreenRect(node.Id, out ScreenRect rect));
		Assert.IsTrue(renderer.TryGetPinScreenPosition(node.InputPins[0].Id, out Vector2 pin));

		// The editor sits to the right of the label on the pin's own row, so aim at the right-hand
		// side of the node at the pin's height.
		float x = rect.Max.X - 20f;
		harness.Mouse.Drag(x, pin.Y, x + 60f, pin.Y);
		harness.Step(3);

		Assert.AreNotEqual(128.0, engine.GetPinValue(node.InputPins[0].Id), "Dragging the editor should have changed the value.");
	}

	/// <summary>
	/// ImNodes marks an attribute active while a widget inside it is, which is what stops the node
	/// running away with the pointer. If that ever stops holding, editing a value drags the node.
	/// </summary>
	[TestMethod]
	public void DraggingTheEditor_DoesNotDragTheNode()
	{
		Node node = TypedNode();
		Start();
		harness.Step(5);

		Vector2 before = engine.Nodes[0].Position;
		Assert.IsTrue(renderer.TryGetNodeScreenRect(node.Id, out ScreenRect rect));
		Assert.IsTrue(renderer.TryGetPinScreenPosition(node.InputPins[0].Id, out Vector2 pin));

		float x = rect.Max.X - 20f;
		harness.Mouse.Drag(x, pin.Y, x + 60f, pin.Y);
		harness.Step(3);

		Assert.AreEqual(before, engine.Nodes[0].Position, "The node moved, so the editor's drag reached ImNodes as a node drag.");
	}

	/// <summary>
	/// An editor makes its row taller, so the link's attach point moves down with it. What must not
	/// happen is the offset being measured from the editor alone and landing off the row.
	/// </summary>
	[TestMethod]
	public void ThePinOffset_StaysOnTheRowTheEditorIsDrawnOn()
	{
		Node node = TypedNode();
		Start();
		harness.Step(5);

		Assert.IsTrue(renderer.TryGetPinScreenPosition(node.InputPins[0].Id, out Vector2 pin));
		Assert.IsTrue(renderer.TryGetNodeScreenRect(node.Id, out ScreenRect rect));

		Assert.IsTrue(pin.Y > rect.Min.Y, "The pin sits below the node's top edge.");
		Assert.IsTrue(pin.Y < rect.Max.Y, "And above its bottom edge.");
	}
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test tests/ImGui.NodeEditor.Tests/ImGui.NodeEditor.Tests.csproj -c Release --filter "FullyQualifiedName~InlinePinEditorTests"`

Expected: build failure, `DrawInlinePinEditors` is not defined.

- [ ] **Step 3: Add the two settings**

In `ImGui.NodeEditor/NodeEditorRenderer.cs`, beside the other public settings near `DrawNodeBody`:

```csharp
	/// <summary>
	/// Whether an unconnected input pin whose type can be edited is drawn with an editor beside its
	/// label. On by default.
	/// </summary>
	/// <remarks>
	/// This is the renderer's own drawing, not a use of <see cref="DrawNodeBody"/>. That hook runs
	/// after every pin so host content cannot move a recorded pin row, which is the right contract
	/// for arbitrary content and the wrong place for a parameter, which belongs on its pin's line.
	/// The two compose: editors on the rows, host content underneath.
	/// </remarks>
	public bool DrawInlinePinEditors { get; set; } = true;

	/// <summary>
	/// How wide an inline editor is drawn, before zoom. Defaults to 90.
	/// </summary>
	public float InlineEditorWidth { get; set; } = 90f;
```

- [ ] **Step 4: Draw the editor inside the input attribute, and measure the row from a group**

Replace the input pin loop in `RenderNode`:

```csharp
		// Input pins
		foreach (Pin pin in node.InputPins)
		{
			ImNodes.BeginInputAttribute(pin.Id);

			// The label and any editor are one row, and the row is what a link attaches to the middle
			// of. Grouped so that RecordPinRow measures both rather than whichever was submitted last.
			ImGui.BeginGroup();
			ImGui.Text(pin.EffectiveDisplayName);
			DrawInlineEditor(engine, pin);
			ImGui.EndGroup();

			ImNodes.EndInputAttribute();
			RecordPinRow(pin.Id, isInput: true);
		}
```

Then add the drawing itself, below `RenderNode`:

```csharp
	/// <summary>
	/// Draw an editor for a pin's value, when the pin has one to edit.
	/// </summary>
	/// <param name="engine">The engine holding the value.</param>
	/// <param name="pin">The pin.</param>
	/// <remarks>
	/// Nothing is drawn for a pin that is connected, whose type has no editor, or when
	/// <see cref="DrawInlinePinEditors"/> is off. Submitted inside the pin's attribute, so ImNodes
	/// marks the attribute active while the widget is and does not read the drag as a node drag.
	/// </remarks>
	private void DrawInlineEditor(NodeEditorEngine engine, Pin pin)
	{
		if (!DrawInlinePinEditors || engine.IsPinConnected(pin.Id))
		{
			return;
		}

		PinValueKind kind = PinValueKinds.Classify(pin.DataType);
		if (kind == PinValueKind.Unsupported)
		{
			return;
		}

		ImGui.SameLine();
		ImGui.SetNextItemWidth(InlineEditorWidth * Zoom);

		string id = $"##pin{pin.Id}";
		object? current = engine.GetPinValue(pin.Id);

		switch (kind)
		{
			case PinValueKind.Boolean:
			{
				bool value = current as bool? ?? false;
				if (ImGui.Checkbox(id, ref value))
				{
					engine.SetPinValue(pin.Id, value);
				}

				break;
			}

			case PinValueKind.Int32:
			{
				int value = current as int? ?? 0;
				if (ImGui.DragInt(id, ref value))
				{
					engine.SetPinValue(pin.Id, value);
				}

				break;
			}

			case PinValueKind.Single:
			{
				float value = current as float? ?? 0f;
				if (ImGui.DragFloat(id, ref value))
				{
					engine.SetPinValue(pin.Id, value);
				}

				break;
			}

			case PinValueKind.Double:
			{
				double value = current as double? ?? 0.0;
				if (ImGui.InputDouble(id, ref value))
				{
					engine.SetPinValue(pin.Id, value);
				}

				break;
			}

			case PinValueKind.String:
			{
				string value = current as string ?? string.Empty;
				if (ImGui.InputText(id, ref value, 256))
				{
					engine.SetPinValue(pin.Id, value);
				}

				break;
			}

			case PinValueKind.Vector2:
			{
				Vector2 value = current as Vector2? ?? Vector2.Zero;
				if (ImGui.InputFloat2(id, ref value))
				{
					engine.SetPinValue(pin.Id, value);
				}

				break;
			}

			case PinValueKind.Vector3:
			{
				Vector3 value = current as Vector3? ?? Vector3.Zero;
				if (ImGui.InputFloat3(id, ref value))
				{
					engine.SetPinValue(pin.Id, value);
				}

				break;
			}

			case PinValueKind.Enum:
			{
				DrawEnumEditor(engine, pin, id, current);
				break;
			}

			case PinValueKind.Unsupported:
			default:
				break;
		}
	}

	/// <summary>
	/// Draw an enum pin as a list of its names.
	/// </summary>
	/// <param name="engine">The engine holding the value.</param>
	/// <param name="pin">The pin.</param>
	/// <param name="id">The widget's id.</param>
	/// <param name="current">What the pin holds now.</param>
	private static void DrawEnumEditor(NodeEditorEngine engine, Pin pin, string id, object? current)
	{
		Type enumType = Nullable.GetUnderlyingType(pin.DataType!) ?? pin.DataType!;
		string[] names = Enum.GetNames(enumType);

		int index = current is null ? 0 : Array.IndexOf(names, current.ToString());
		if (index < 0)
		{
			index = 0;
		}

		if (ImGui.Combo(id, ref index, names, names.Length))
		{
			engine.SetPinValue(pin.Id, Enum.Parse(enumType, names[index]));
		}
	}
```

Add `using System;` to the file's usings if absent.

- [ ] **Step 5: Run the new tests and the existing rendering tests**

Run: `dotnet test tests/ImGui.NodeEditor.Tests/ImGui.NodeEditor.Tests.csproj -c Release --filter "FullyQualifiedName~InlinePinEditorTests|FullyQualifiedName~NodeRenderingTests"`

Expected: PASS. `DrawNodeBody_LeavesThePinPositionsWhereTheyWere` must still pass. It draws a node with an untyped pin, which now gets no editor, so the rows it measures are unchanged.

- [ ] **Step 6: Commit**

```bash
git add ImGui.NodeEditor/NodeEditorRenderer.cs tests/ImGui.NodeEditor.Tests/InlinePinEditorTests.cs
git commit -m "[minor] Draw an editor on an unconnected input pin"
```

---

### Task 7: `SelectedNodeIds`

Reports what ImNodes has selected, so a host can show an inspector for it.

**Files:**
- Modify: `ImGui.NodeEditor/NodeEditorRenderer.cs` (`ReadHoverState` around line 488, and the `Render` call site around line 187)
- Test: `tests/ImGui.NodeEditor.Tests/NodeRenderingTests.cs`

**Interfaces:**
- Consumes: nothing from earlier tasks.
- Produces: `NodeEditorRenderer.SelectedNodeIds` returning `IReadOnlySet<int>`.

- [ ] **Step 1: Write the failing test**

Add to `tests/ImGui.NodeEditor.Tests/NodeRenderingTests.cs`:

```csharp
	/// <summary>
	/// ImNodes knows what is selected and nothing exposed it, so an inspector had no way to know what
	/// it was inspecting.
	/// </summary>
	[TestMethod]
	public void SelectedNodeIds_ReportsWhatTheUserClicked()
	{
		Node node = engine.CreateNode(new Vector2(250, 200), "Target", ["In"], []);
		Start();

		Assert.IsEmpty(renderer.SelectedNodeIds, "Nothing is selected before anything is clicked.");

		Assert.IsTrue(renderer.TryGetNodeScreenRect(node.Id, out ScreenRect rect));
		harness.Mouse.Click(rect.Centre.X, rect.Min.Y + 6f);
		harness.Step(3);

		Assert.Contains(node.Id, renderer.SelectedNodeIds);
	}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/ImGui.NodeEditor.Tests/ImGui.NodeEditor.Tests.csproj -c Release --filter "FullyQualifiedName~SelectedNodeIds_ReportsWhatTheUserClicked"`

Expected: build failure, `SelectedNodeIds` is not defined.

- [ ] **Step 3: Read selection back where hover is read**

In `ImGui.NodeEditor/NodeEditorRenderer.cs`, add the backing field beside the other read-back state:

```csharp
	private readonly HashSet<int> selectedNodes = [];
```

Add the property beside `HoveredNodeId`:

```csharp
	/// <summary>The nodes ImNodes had selected as of the last frame drawn.</summary>
	/// <remarks>
	/// Read after the editor ends, like the hover state and for the same reason: ImNodes only answers
	/// once it has laid the frame out.
	/// </remarks>
	public IReadOnlySet<int> SelectedNodeIds => selectedNodes;
```

Extend `ReadHoverState` to fill it:

```csharp
		selectedNodes.Clear();
		int selectedCount = ImNodes.NumSelectedNodes();
		if (selectedCount > 0)
		{
			int[] buffer = new int[selectedCount];
			unsafe
			{
				fixed (int* first = buffer)
				{
					ImNodes.GetSelectedNodes(first);
				}
			}

			foreach (int id in buffer)
			{
				selectedNodes.Add(id);
			}
		}
```

The method already runs after `EndNodeEditor`. Add `[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here.", Justification = "Required for native ImNodes interop; the pointer is scoped to the call and not retained.")]` to `ReadHoverState`, matching how `NodeEditorInputHandler` justifies the same thing, and add `using System.Diagnostics.CodeAnalysis;` if absent.

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test tests/ImGui.NodeEditor.Tests/ImGui.NodeEditor.Tests.csproj -c Release --filter "FullyQualifiedName~NodeRenderingTests"`

Expected: PASS, the whole class.

- [ ] **Step 5: Commit**

```bash
git add ImGui.NodeEditor/NodeEditorRenderer.cs tests/ImGui.NodeEditor.Tests/NodeRenderingTests.cs
git commit -m "[minor] Report which nodes are selected"
```

---

### Task 8: `NodeInspectorPanel`

A panel for one node's parameters, drawn with `PropertyGrid`. This is the task that takes the dependency on `ktsu.ImGui.Widgets`.

**Files:**
- Create: `ImGui.NodeEditor/NodeInspectorPanel.cs`
- Modify: `ImGui.NodeEditor/ImGui.NodeEditor.csproj`
- Test: `tests/ImGui.NodeEditor.Tests/NodeInspectorPanelTests.cs` (create)

**Interfaces:**
- Consumes: `PinValueKinds.Classify` from Task 5, engine value accessors from Task 3.
- Produces: `static bool NodeInspectorPanel.Draw(NodeEditorEngine engine, int nodeId)`.

- [ ] **Step 1: Add the project reference**

In `ImGui.NodeEditor/ImGui.NodeEditor.csproj`, inside the `ItemGroup` holding the other `ProjectReference` entries:

```xml
    <ProjectReference Include="..\ImGui.Widgets\ImGui.Widgets.csproj" />
```

This is a deliberate, recorded cost: `ktsu.ImGui.Widgets` brings `Hexa.NET.ImGui.Widgets.Extras` and with it Roslyn, about 107 MB unpacked, for every consumer of the node editor. See #384 and the spec's decision on packaging.

- [ ] **Step 2: Write the failing tests**

Create `tests/ImGui.NodeEditor.Tests/NodeInspectorPanelTests.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor.Tests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.NodeEditor;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives <see cref="NodeInspectorPanel"/>, which draws one node's parameters as a property grid.
/// It needs a real context because a property grid is a table, and a table that never opened draws
/// no rows.
/// </summary>
[TestClass]
public sealed class NodeInspectorPanelTests
{
	private static readonly HarnessOptions Viewport = new() { Width = 700, Height = 500 };

	private readonly NodeEditorEngine engine = new();

	private ImGuiAppHarness harness = null!;
	private int inspected;

	[TestCleanup]
	public void TearDown() => harness?.Dispose();

	private void Start()
	{
		harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Title = nameof(NodeInspectorPanelTests),
				OnRender = _ => NodeInspectorPanel.Draw(engine, inspected),
				SaveIniSettings = false,
			},
			Viewport);

		harness.Step(3);
	}

	private bool IsVisible(string name) => harness.Probe.WasSeenInFrame(name, harness.FrameCount - 1);

	[TestMethod]
	public void Draw_ShowsARowForEachInputPin()
	{
		Node node = engine.CreateNode(
			new Vector2(0, 0),
			"Blob Filter",
			[new PinSpec("Threshold", typeof(double), 128.0), new PinSpec("AreaMin", typeof(double), 50.0)],
			[new PinSpec("Count", typeof(int))]);
		inspected = node.Id;
		Start();

		Assert.IsTrue(IsVisible("Threshold"));
		Assert.IsTrue(IsVisible("AreaMin"));
	}

	/// <summary>
	/// There is no evaluator to give an output pin a value, per #440, so an output row would be a
	/// box showing nothing.
	/// </summary>
	[TestMethod]
	public void Draw_ShowsNoRowForAnOutputPin()
	{
		Node node = engine.CreateNode(
			new Vector2(0, 0),
			"Blob Filter",
			[new PinSpec("Threshold", typeof(double), 128.0)],
			[new PinSpec("Count", typeof(int))]);
		inspected = node.Id;
		Start();

		Assert.IsFalse(IsVisible("Count"));
	}

	[TestMethod]
	public void Draw_ForANodeThatDoesNotExist_DrawsNothingAndDoesNotThrow()
	{
		inspected = 999;
		Start();
		harness.Step(3);

		Assert.AreEqual(0, ImGui.GetCurrentContext().ErrorCountCurrentFrame, "ImGui reported the panel's drawing as misuse.");
	}

	[TestMethod]
	public void Draw_EditingARow_WritesThroughToTheEngine()
	{
		Node node = engine.CreateNode(
			new Vector2(0, 0),
			"Blob Filter",
			[new PinSpec("Threshold", typeof(double), 128.0)],
			[]);
		inspected = node.Id;
		Start();

		// The grid's double row is an InputDouble, so clicking it places the caret in text that is
		// already there. Without the select-all, typing appends to "128.000000" instead of replacing
		// it.
		harness.Click("Threshold");
		harness.Step(1);
		harness.Keyboard.Press(ImGuiKey.A, ctrl: true);
		harness.Keyboard.Type("200");
		harness.Keyboard.Press(ImGuiKey.Enter);
		harness.Step(3);

		Assert.AreEqual(200.0, engine.GetPinValue(node.InputPins[0].Id));
	}
}
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `dotnet test tests/ImGui.NodeEditor.Tests/ImGui.NodeEditor.Tests.csproj -c Release --filter "FullyQualifiedName~NodeInspectorPanelTests"`

Expected: build failure, `NodeInspectorPanel` could not be found.

- [ ] **Step 4: Write the panel**

Create `ImGui.NodeEditor/NodeInspectorPanel.cs`:

```csharp
// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor;

using System;
using System.Linq;
using System.Numerics;

using ktsu.ImGui.Widgets;

/// <summary>
/// Draws one node's parameters as a two-column property grid.
/// </summary>
/// <remarks>
/// The panel is told which node to inspect rather than asking what is selected, so it holds no
/// ImNodes state and can be drawn without a renderer. <see cref="NodeEditorRenderer.SelectedNodeIds"/>
/// is where a host gets the id from.
/// <para>
/// Which pins are editable is decided by <see cref="PinValueKinds"/>, the same classifier the
/// renderer's inline editors use, so a type has an editor on both surfaces or on neither.
/// </para>
/// </remarks>
public static class NodeInspectorPanel
{
	/// <summary>
	/// Draw the parameters of one node.
	/// </summary>
	/// <param name="engine">The engine holding the graph and its values.</param>
	/// <param name="nodeId">The node to inspect.</param>
	/// <returns>True if any row changed its value this frame.</returns>
	/// <remarks>
	/// Only input pins are drawn. An output pin's value would come from evaluating the graph, which
	/// this library does not do.
	/// <para>
	/// A connected pin's row is drawn disabled, because its value arrives along the link and editing
	/// the stored literal would change nothing anyone reads. A pin whose type has no editor is drawn
	/// disabled too, rather than omitted, so the panel does not quietly show less than the node has.
	/// </para>
	/// </remarks>
	public static bool Draw(NodeEditorEngine engine, int nodeId)
	{
		ArgumentNullException.ThrowIfNull(engine);

		Node? node = engine.Nodes.FirstOrDefault(n => n.Id == nodeId);
		if (node is null)
		{
			return false;
		}

		using ImGuiWidgets.PropertyGrid grid = new($"NodeInspector{nodeId}");

		foreach (Pin pin in node.InputPins)
		{
			DrawRow(grid, engine, pin);
		}

		return grid.Changed;
	}

	private static void DrawRow(ImGuiWidgets.PropertyGrid grid, NodeEditorEngine engine, Pin pin)
	{
		PinValueKind kind = PinValueKinds.Classify(pin.DataType);
		bool editable = kind != PinValueKind.Unsupported && !engine.IsPinConnected(pin.Id);

		using (new ImGuiWidgets.ScopedDisable(!editable))
		{
			string label = pin.EffectiveDisplayName;
			object? current = engine.GetPinValue(pin.Id);

			switch (kind)
			{
				case PinValueKind.Boolean:
				{
					bool value = current as bool? ?? false;
					if (grid.Value(label, ref value) && editable)
					{
						engine.SetPinValue(pin.Id, value);
					}

					break;
				}

				case PinValueKind.Int32:
				{
					int value = current as int? ?? 0;
					if (grid.Value(label, ref value) && editable)
					{
						engine.SetPinValue(pin.Id, value);
					}

					break;
				}

				case PinValueKind.Single:
				{
					float value = current as float? ?? 0f;
					if (grid.Value(label, ref value) && editable)
					{
						engine.SetPinValue(pin.Id, value);
					}

					break;
				}

				case PinValueKind.Double:
				{
					double value = current as double? ?? 0.0;
					if (grid.Value(label, ref value) && editable)
					{
						engine.SetPinValue(pin.Id, value);
					}

					break;
				}

				case PinValueKind.String:
				{
					string value = current as string ?? string.Empty;
					if (grid.Value(label, ref value) && editable)
					{
						engine.SetPinValue(pin.Id, value);
					}

					break;
				}

				case PinValueKind.Vector2:
				{
					Vector2 value = current as Vector2? ?? Vector2.Zero;
					if (grid.Value(label, ref value) && editable)
					{
						engine.SetPinValue(pin.Id, value);
					}

					break;
				}

				case PinValueKind.Vector3:
				{
					Vector3 value = current as Vector3? ?? Vector3.Zero;
					if (grid.Value(label, ref value) && editable)
					{
						engine.SetPinValue(pin.Id, value);
					}

					break;
				}

				case PinValueKind.Enum:
				{
					// The grid's Enum row is generic over the enum type, which is only known here as a
					// Type, so the value is shown as text rather than reflected into that overload.
					string value = current?.ToString() ?? string.Empty;
					grid.Value(label, ref value);
					break;
				}

				case PinValueKind.Unsupported:
				default:
				{
					// Shown, not omitted: a pin with no editor is still part of the node.
					string value = current?.ToString() ?? string.Empty;
					grid.Value(label, ref value);
					break;
				}
			}
		}
	}
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test tests/ImGui.NodeEditor.Tests/ImGui.NodeEditor.Tests.csproj -c Release --filter "FullyQualifiedName~NodeInspectorPanelTests"`

Expected: PASS, 4 tests.

If `Draw_EditingARow_WritesThroughToTheEngine` fails because the click lands on the label rather than the value box, qualify the probe name. `PropertyGrid` pushes its id as a probe scope, so the value widget is addressed as `NodeInspector<id>/Threshold`. Use `harness.Probe.Matches("Threshold").Single()` to see what the name actually is before changing the click.

- [ ] **Step 6: Commit**

```bash
git add ImGui.NodeEditor/NodeInspectorPanel.cs ImGui.NodeEditor/ImGui.NodeEditor.csproj tests/ImGui.NodeEditor.Tests/NodeInspectorPanelTests.cs
git commit -m "[minor] Draw one node's parameters as a property grid"
```

---

### Task 9: Demo and documentation

Shows the feature in the demo the node editor already has, and corrects the README, including the type-aware claim that was never true.

**Files:**
- Modify: `examples/ImGuiAppDemo/Demos/CleanImNodesDemo.cs`
- Modify: `ImGui.NodeEditor/README.md`
- Test: `tests/ImGuiAppDemo.UITests/` (the existing suite for this demo)

**Interfaces:**
- Consumes: everything from Tasks 1 to 8.
- Produces: no API.

- [ ] **Step 1: Add a parameter node to the demo's seeded graph**

In `examples/ImGuiAppDemo/Demos/CleanImNodesDemo.cs`, in the method that seeds the demo graph (near `CreateNode<MakeNumberNode>` around line 417), add a node whose pins are typed and defaulted so the inline editors have something to draw:

```csharp
		// A node whose parameters are edited rather than connected, which is what issue #437 asked
		// about. Typed pins with defaults are all an inline editor needs.
		Node blobFilter = engine.CreateNode(
			new Vector2(50, 550),
			"Blob Filter",
			[
				new PinSpec("Threshold", typeof(double), 128.0),
				new PinSpec("AreaMin", typeof(double), 50.0),
				new PinSpec("Sigma", typeof(double), 2.0),
				new PinSpec("Invert", typeof(bool), false),
			],
			[new PinSpec("Count", typeof(int))]);
```

- [ ] **Step 2: Draw the inspector beside the graph**

In the demo's draw method, where the side panel with the node and link listings is drawn (near the `Input Pins:` text around line 287), add the inspector for whatever is selected:

```csharp
		if (renderer.SelectedNodeIds.Count > 0)
		{
			ImGui.SeparatorText("Parameters");
			NodeInspectorPanel.Draw(engine, renderer.SelectedNodeIds.First());
		}
```

Add `using System.Linq;` to the file if absent.

- [ ] **Step 3: Cover the new demo content with a UI test**

The node's pin labels and inline editors are drawn with plain `ImGui.Text` and `ImGui.InputDouble`, neither of which marks itself for the probe, so they cannot be found by name. What is worth asserting is that a node carrying editors draws without ImGui reporting misuse, which is the failure class that inline widgets inside an ImNodes attribute would land in.

Add to `tests/ImGuiAppDemo.UITests/AppDemoUITests.cs`:

```csharp
	/// <summary>
	/// The Blob Filter node's inputs are typed and unconnected, so the renderer draws an editor on
	/// each of their rows. Widgets submitted inside an ImNodes attribute are exactly where a cursor
	/// or ID stack mistake shows up, and ImGui reports that as misuse rather than by crashing.
	/// </summary>
	[TestMethod]
	public void CleanImNodes_TabDrawsInlineParameterEditorsWithoutError()
	{
		OpenTab(CleanImNodesTab);
		harness.Step(10);

		Assert.AreEqual(
			0,
			ImGui.GetCurrentContext().ErrorCountCurrentFrame,
			"ImGui reported the node editor's drawing as misuse.");
	}
```

Add `using Hexa.NET.ImGui;` to that file if it is not already there.

- [ ] **Step 4: Run the demo's UI suite**

Run: `dotnet test tests/ImGuiAppDemo.UITests/ImGuiAppDemo.UITests.csproj -c Release`

Expected: PASS, including the new test and every pre-existing one. The existing tests do not know about the new node, so a failure there means it displaced something they click by position.

- [ ] **Step 5: Add a parameters section to the README**

In `ImGui.NodeEditor/README.md`, after the "Nodes from decorated types" section, add:

````markdown
### Editing a node's parameters

A pin carries the type it was declared with, and the engine holds a value for it. An unconnected
input pin whose type has an editor is drawn with one beside its label, and `NodeInspectorPanel`
draws the same parameters as a property grid for whichever node is selected.

```csharp
Node filter = engine.CreateNode(
    new Vector2(100, 100),
    "Blob Filter",
    [
        new PinSpec("Threshold", typeof(double), 128.0),
        new PinSpec("Polarity", typeof(EdgePolarity), EdgePolarity.Rising),
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
````

- [ ] **Step 6: Correct the type-aware connections claim**

In `ImGui.NodeEditor/README.md`, in the Features list, replace the `**Type-aware connections**` bullet with:

```markdown
- **Connections are checked**: `TryCreateLink` returns a result with a message rather than throwing, and refuses a link that joins two pins of the same direction, duplicates one that exists, exceeds what a pin will accept, or joins a node to itself. It does not yet compare the pins' declared types
```

- [ ] **Step 7: Commit**

```bash
git add examples/ImGuiAppDemo/Demos/CleanImNodesDemo.cs ImGui.NodeEditor/README.md tests/ImGuiAppDemo.UITests/AppDemoUITests.cs
git commit -m "[patch] Show parameter editing in the demo and the README"
```

---

## After the plan

Run the whole suite before opening a pull request:

```bash
dotnet test -c Release
```

Then open the pull request against `main`, with `Fixes #438` and `Refs #437` in the body, and answer #437 with a link to the README's new section.

Two follow-ups this work makes cheap but deliberately leaves alone:

- **Type-aware link validation.** `Pin.DataType` now exists and `PinTypeUtilities.CanConnect` already implements the rules, so `TryCreateLink` could enforce them. It changes which links existing graphs may make, so it wants its own issue and its own opt-in flag.
- **`long` pins.** One case in `PinValueKinds.Classify` and one in each surface's switch, using an unsafe `DragScalar` with `ImGuiDataType.S64`.
