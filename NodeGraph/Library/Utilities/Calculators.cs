// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

// Calculator utility nodes with instance methods.

namespace ktsu.NodeGraph.Library.Utilities;
using System.ComponentModel;

/// <summary>
/// Example class with instance methods as nodes.
/// Gets automatic "Instance" input/output pins for object lifecycle management.
/// </summary>
public class Calculator
{
	private double accumulator = 0;

	/// <summary>
	/// Instance method node - automatically gets an "Instance" pin.
	/// </summary>
	[Node("Add to Accumulator")]
	[Description("Adds a value to the internal accumulator")]
	[ExecutionInput]
	[ExecutionOutput]
	public double AddToAccumulator(double value)
	{
		accumulator += value;
		return accumulator;
	}

	/// <summary>
	/// Another instance method showing current accumulator value.
	/// This is intentionally a method (not a property) to demonstrate method-based nodes.
	/// </summary>
	[Node("Get Accumulator")]
	[Description("Gets the current accumulator value")]

	[OutputPin]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Intentional method to demonstrate method-based nodes")]
	public double GetAccumulator() => accumulator;

	/// <summary>
	/// Instance method that resets the accumulator.
	/// </summary>
	[Node("Reset Accumulator")]
	[Description("Resets the accumulator to zero")]

	[ExecutionInput]
	[ExecutionOutput]
	public void Reset() => accumulator = 0;
}
