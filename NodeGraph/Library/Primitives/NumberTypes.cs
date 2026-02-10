// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

// Number data types and their Make/Split/Set node implementations.

namespace ktsu.NodeGraph.Library.Primitives;
using System;
using System.ComponentModel;

/// <summary>
/// Number data structure.
/// </summary>
public struct NumberData : IEquatable<NumberData>
{
	public double Value { get; set; }

	public readonly bool Equals(NumberData other) => Value == other.Value;
	public override readonly bool Equals(object? obj) => obj is NumberData other && Equals(other);
	public override readonly int GetHashCode() => Value.GetHashCode();
	public static bool operator ==(NumberData left, NumberData right) => left.Equals(right);
	public static bool operator !=(NumberData left, NumberData right) => !left.Equals(right);
}

/// <summary>
/// Creates a number data structure.
/// </summary>
[Node("Make Number")]
[Description("Creates a number data structure from a value")]
public class MakeNumberNode
{
	[InputPin]
	[Description("The numeric value")]
	public double Value { get; set; }

	[OutputPin]
	[Description("The created number data")]
	public NumberData Result => new() { Value = Value };
}

/// <summary>
/// Extracts values from a number data structure.
/// </summary>
[Node("Split Number")]
[Description("Extracts values and conversions from a number data structure")]
public class SplitNumberNode
{
	[InputPin]
	[Description("The number data to split")]
	public NumberData Input { get; set; }

	[OutputPin]
	[Description("The stored value")]
	public double Value => Input.Value;

	[OutputPin]
	[Description("Absolute value")]
	public double Absolute => Math.Abs(Input.Value);

	[OutputPin]
	[Description("Value as integer")]
	public int AsInt => (int)Input.Value;

	[OutputPin]
	[Description("Value as string")]
	public string AsString => Input.Value.ToString();
}

/// <summary>
/// Updates a number data structure with new value.
/// </summary>
[Node("Set Number")]
[Description("Creates a new number data structure with updated value")]
public class SetNumberNode
{
	[InputPin]
	[Description("The existing number data")]
	public NumberData Input { get; set; }

	[InputPin]
	[Description("The new value to set")]
	public double Value { get; set; }

	[OutputPin]
	[Description("The updated number data")]
	public NumberData Result => new() { Value = Value };
}

/// <summary>
/// Integer data structure.
/// </summary>
public struct IntegerData : IEquatable<IntegerData>
{
	public int Value { get; set; }

	public readonly bool Equals(IntegerData other) => Value == other.Value;
	public override readonly bool Equals(object? obj) => obj is IntegerData other && Equals(other);
	public override readonly int GetHashCode() => Value.GetHashCode();
	public static bool operator ==(IntegerData left, IntegerData right) => left.Equals(right);
	public static bool operator !=(IntegerData left, IntegerData right) => !left.Equals(right);
}

/// <summary>
/// Creates an integer data structure.
/// </summary>
[Node("Make Integer")]
[Description("Creates an integer data structure from a value")]
public class MakeIntegerNode
{
	[InputPin]
	[Description("The integer value")]
	public int Value { get; set; }

	[OutputPin]
	[Description("The created integer data")]
	public IntegerData Result => new() { Value = Value };
}

/// <summary>
/// Extracts values from an integer data structure.
/// </summary>
[Node("Split Integer")]
[Description("Extracts values and conversions from an integer data structure")]
public class SplitIntegerNode
{
	[InputPin]
	[Description("The integer data to split")]
	public IntegerData Input { get; set; }

	[OutputPin]
	[Description("The stored value")]
	public int Value => Input.Value;

	[OutputPin]
	[Description("Value as double")]
	public double AsDouble => Input.Value;

	[OutputPin]
	[Description("Value as string")]
	public string AsString => Input.Value.ToString();

	[OutputPin]
	[Description("Is even number")]
	public bool IsEven => Input.Value % 2 == 0;

	[OutputPin]
	[Description("Is odd number")]
	public bool IsOdd => Input.Value % 2 != 0;
}

/// <summary>
/// Updates an integer data structure with new value.
/// </summary>
[Node("Set Integer")]
[Description("Creates a new integer data structure with updated value")]
public class SetIntegerNode
{
	[InputPin]
	[Description("The existing integer data")]
	public IntegerData Input { get; set; }

	[InputPin]
	[Description("The new value to set")]
	public int Value { get; set; }

	[OutputPin]
	[Description("The updated integer data")]
	public IntegerData Result => new() { Value = Value };
}
