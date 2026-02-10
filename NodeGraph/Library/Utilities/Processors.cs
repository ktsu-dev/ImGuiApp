// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

// Data processor utility nodes.

namespace ktsu.NodeGraph.Library.Utilities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

/// <summary>
/// Number array processor with common mathematical operations.
/// </summary>
[Node] // Display name will be "ArrayProcessor"
[Description("Performs mathematical operations on arrays of numbers")]
public class ArrayProcessorNode
{
	[InputPin]
	[Description("Array of numbers to process")]
	public IReadOnlyList<double> Numbers { get; set; } = [];

	[OutputPin]
	[Description("Sum of all numbers")]
	public double Sum => Numbers.Sum();

	[OutputPin]
	[Description("Average of all numbers")]
	public double Average => Numbers.Count > 0 ? Numbers.Average() : 0;

	[OutputPin]
	[Description("Maximum value")]
	public double Max => Numbers.Count > 0 ? Numbers.Max() : 0;

	[OutputPin]
	[Description("Minimum value")]
	public double Min => Numbers.Count > 0 ? Numbers.Min() : 0;

	[OutputPin]
	[Description("Number of elements")]
	public int Count => Numbers.Count;
}
