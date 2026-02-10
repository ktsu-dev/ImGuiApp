// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

// Boolean data types and their Make/Split/Set node implementations.

namespace ktsu.NodeGraph.Library.Primitives;
using System;
using System.ComponentModel;

/// <summary>
/// Boolean data structure.
/// </summary>
public struct BooleanData : IEquatable<BooleanData>
{
	public bool Value { get; set; }

	public readonly bool Equals(BooleanData other) => Value == other.Value;
	public override readonly bool Equals(object? obj) => obj is BooleanData other && Equals(other);
	public override readonly int GetHashCode() => Value.GetHashCode();
	public static bool operator ==(BooleanData left, BooleanData right) => left.Equals(right);
	public static bool operator !=(BooleanData left, BooleanData right) => !left.Equals(right);
}

/// <summary>
/// Creates a boolean data structure.
/// </summary>
[Node("Make Boolean")]
[Description("Creates a boolean data structure from a value")]
public class MakeBooleanNode
{
	[InputPin]
	[Description("The boolean value")]
	public bool Value { get; set; }

	[OutputPin]
	[Description("The created boolean data")]
	public BooleanData Result => new() { Value = Value };
}

/// <summary>
/// Extracts values from a boolean data structure.
/// </summary>
[Node("Split Boolean")]
[Description("Extracts values and conversions from a boolean data structure")]
public class SplitBooleanNode
{
	[InputPin]
	[Description("The boolean data to split")]
	public BooleanData Input { get; set; }

	[OutputPin]
	[Description("The stored value")]
	public bool Value => Input.Value;

	[OutputPin]
	[Description("Inverted value")]
	public bool Not => !Input.Value;

	[OutputPin]
	[Description("Value as string")]
	public string AsString => Input.Value.ToString();

	[OutputPin]
	[Description("Value as integer (1 or 0)")]
	public int AsInt => Input.Value ? 1 : 0;
}

/// <summary>
/// Updates a boolean data structure with new value.
/// </summary>
[Node("Set Boolean")]
[Description("Creates a new boolean data structure with updated value")]
public class SetBooleanNode
{
	[InputPin]
	[Description("The existing boolean data")]
	public BooleanData Input { get; set; }

	[InputPin]
	[Description("The new value to set")]
	public bool Value { get; set; }

	[OutputPin]
	[Description("The updated boolean data")]
	public BooleanData Result => new() { Value = Value };
}
