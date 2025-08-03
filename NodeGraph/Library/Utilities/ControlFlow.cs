// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

// Control flow utility nodes.

namespace ktsu.NodeGraph.Library.Utilities;
using System.ComponentModel;

/// <summary>
/// Conditional selection node - outputs one of two values based on a condition.
/// </summary>
[Node] // Display name will be "ControlFlow"
[Description("Selects between two values based on a boolean condition")]
public class ConditionalNode
{
	[InputPin]
	[Description("The condition to evaluate")]
	public bool Condition { get; set; }

	[InputPin]
	[Description("Value to output if condition is true")]
	public object? TrueValue { get; set; }

	[InputPin]
	[Description("Value to output if condition is false")]
	public object? FalseValue { get; set; }

	[OutputPin]
	[Description("The selected value based on condition")]
	public object? Result => Condition ? TrueValue : FalseValue;
}
