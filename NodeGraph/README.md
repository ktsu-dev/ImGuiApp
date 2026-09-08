# ktsu.NodeGraph

[![NuGet](https://img.shields.io/nuget/v/ktsu.NodeGraph?logo=nuget)](https://nuget.org/packages/ktsu.NodeGraph)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](https://github.com/ktsu-dev/ImGuiApp/blob/main/LICENSE.md)

NodeGraph describes node graphs with attributes, and nothing else. Decorate a class, struct, or method with node and pin metadata and any editor can read it back by reflection — this package draws nothing, references no UI library, and has no dependencies at all. `ktsu.ImGuiNodeEditor` is one consumer of it; a different renderer, a code generator, or a headless graph runner can be another.

## Features

- **UI-agnostic metadata**: `[Node]`, `[InputPin]`, `[OutputPin]`, `[ExecutionInput]`, `[ExecutionOutput]` describe a node without naming an editor
- **Execution semantics**: `[NodeBehavior]` declares an execution mode (reactive, execution-pin driven, on start, continuous, or manual), async support, side effects, determinism and cacheability; `[NodeExecute]` and `[NodeValidate]` mark the methods that run and check a node
- **Lifecycle metadata**: `[NodeDeprecated]` and `[NodeVisibility]` let an editor hide, warn about, or redirect a node without the graph code changing
- **Type compatibility rules**: `PinTypeUtilities` answers whether two pin types can connect, whether the connection needs a conversion, and whether that conversion loses information
- **Wildcards and custom rules**: `[WildcardPin]` accepts any type, and `[PinConnectionRule]` declares a pairing the built-in rules do not cover
- **A built-in node library**: math, string, logic, collection, date/time, control-flow, generator, counter, geometry and calculator nodes, all declared with these same attributes — see [README-Library.md](README-Library.md)

## Installation

### Package Manager Console

```powershell
Install-Package ktsu.NodeGraph
```

### .NET CLI

```bash
dotnet add package ktsu.NodeGraph
```

### Package Reference

```xml
<PackageReference Include="ktsu.NodeGraph" Version="x.y.z" />
```

## Usage Examples

### Basic Example

A node is an ordinary type with attributes. Inputs and outputs are properties; the work happens in the method marked `[NodeExecute]`.

```csharp
using ktsu.NodeGraph;

[Node("Add")]
[NodeBehavior(ExecutionMode = NodeExecutionMode.OnInputChange, IsDeterministic = true)]
public class AddNode
{
    [InputPin("A")]
    public double A { get; set; }

    [InputPin("B")]
    public double B { get; set; }

    [OutputPin("Sum")]
    public double Sum { get; private set; }

    [NodeExecute]
    public void Execute() => Sum = A + B;
}
```

### Execution flow

Data pins carry values; execution pins carry control. A node that should fire when something else tells it to, rather than when its inputs change, declares both.

```csharp
[Node("Log Message")]
[NodeBehavior(ExecutionMode = NodeExecutionMode.OnExecution, HasSideEffects = true)]
public class LogNode
{
    [ExecutionInput]
    public object? In { get; set; }

    [ExecutionOutput]
    public object? Out { get; set; }

    [InputPin("Message")]
    public string Message { get; set; } = string.Empty;

    [NodeExecute]
    public void Execute() => Console.WriteLine(Message);
}
```

### Method nodes

A method can be a node on its own, with its parameters as inputs and its return value as the output.

```csharp
[Node("Clamp")]
public static double Clamp(
    [InputPin("Value")] double value,
    [InputPin("Min")] double min,
    [InputPin("Max")] double max) => Math.Clamp(value, min, max);
```

### Deciding whether two pins may connect

```csharp
bool ok = PinTypeUtilities.CanConnect(typeof(int), typeof(double));       // true
bool needsCast = PinTypeUtilities.RequiresConversion(typeof(int), typeof(double));  // true
bool lossy = PinTypeUtilities.IsLossyConversion(typeof(double), typeof(int));       // true
```

## API Reference

### Attributes

| Attribute | Target | Description |
| --------- | ------ | ----------- |
| `[Node]` | Class, struct, method | Marks the type or method as a node. `DisplayName`, `ColorHint`, `Tags` |
| `[InputPin]` | Property, field, parameter | Declares an input. `DisplayName`, `Order`, `IsRequired`, `ColorHint`, `AllowMultipleConnections`, `DefaultValue` |
| `[OutputPin]` | Property, field, method, return value | Declares an output. Same members, with `AllowMultipleConnections` defaulting to `true` |
| `[ExecutionInput]` / `[ExecutionOutput]` | Property, field | Declares control-flow pins rather than data pins |
| `[NodeBehavior]` | Class | `ExecutionMode`, async support, side effects, determinism, cacheability |
| `[NodeExecute]` | Method | The method that performs the node's work |
| `[NodeValidate]` | Method | A method that validates the node's inputs before execution |
| `[NodeDeprecated]` | Class, struct | Marks a node as deprecated, with a reason, replacement, and versions |
| `[NodeVisibility]` | Class, struct | Controls menu visibility, instantiability, and experimental status |
| `[WildcardPin]` | Property, field | The pin accepts any type |
| `[PinConnectionRule]` | Class | Declares a source/target type pairing beyond the built-in rules |

### `NodeExecutionMode`

`OnInputChange` (reactive), `OnExecution` (triggered through execution pins), `OnStart`, `Continuous`, `Manual`.

### `PinTypeUtilities`

| Name | Return Type | Description |
| ---- | ----------- | ----------- |
| `CanConnect(Type, Type)` | `bool` | Whether a source pin type may feed a target pin type |
| `RequiresConversion(Type, Type)` | `bool` | Whether the connection needs a conversion |
| `IsLossyConversion(Type, Type)` | `bool` | Whether that conversion can lose information |
| `IsNumericType(Type)` | `bool` | Whether the type is one of the numeric types |
| `AllNumericTypes` | `IEnumerable<Type>` | Every numeric type the rules know about |

## Acknowledgments

This package has no dependencies — it is attributes and rules over the base class library, which is what lets any editor consume it without inheriting a UI stack.

## Contributing

Contributions are welcome! For feature requests, bug reports, or questions, please open an issue on the GitHub repository. If you would like to contribute code, please open a pull request with your changes.

## License

NodeGraph is licensed under the MIT License. See [LICENSE.md](https://github.com/ktsu-dev/ImGuiApp/blob/main/LICENSE.md) for more information.
