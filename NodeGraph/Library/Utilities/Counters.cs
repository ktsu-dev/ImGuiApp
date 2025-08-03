// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

// Counter utility nodes.

namespace ktsu.NodeGraph.Library.Utilities;
using System.ComponentModel;

/// <summary>
/// Counter node for tracking numeric values.
/// </summary>
[Node] // Display name will be "Counter"
[Description("Maintains a counter value with increment/decrement operations")]
public class CounterNode
{
	[InputPin]
	[Description("Amount to increment by")]
	public int IncrementBy { get; set; } = 1;

	[InputPin]
	[Description("Reset counter to this value")]
	public int ResetValue { get; set; } = 0;

	[OutputPin]
	[Description("Current counter value")]
	public int Value { get; private set; } = 0;

	[OutputPin]
	[Description("Is counter at zero")]
	public bool IsZero => Value == 0;

	[OutputPin]
	[Description("Is counter positive")]
	public bool IsPositive => Value > 0;

	[OutputPin]
	[Description("Is counter negative")]
	public bool IsNegative => Value < 0;

	public void Increment() => Value += IncrementBy;
	public void Decrement() => Value -= IncrementBy;
	public void Reset() => Value = ResetValue;
}
