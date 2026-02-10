// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

// String data types and their Make/Split/Set node implementations.

namespace ktsu.NodeGraph.Library.Primitives;
using System;
using System.ComponentModel;

/// <summary>
/// String data structure.
/// </summary>
public struct StringData : IEquatable<StringData>
{
	public string Value { get; set; }

	public readonly bool Equals(StringData other) => Value == other.Value;
	public override readonly bool Equals(object? obj) => obj is StringData other && Equals(other);
	public override readonly int GetHashCode() => Value?.GetHashCode(StringComparison.Ordinal) ?? 0;
	public static bool operator ==(StringData left, StringData right) => left.Equals(right);
	public static bool operator !=(StringData left, StringData right) => !left.Equals(right);
}

/// <summary>
/// Creates a string data structure.
/// </summary>
[Node("Make String")]
[Description("Creates a string data structure from a value")]
public class MakeStringNode
{
	[InputPin]
	[Description("The string value")]
	public string Value { get; set; } = string.Empty;

	[OutputPin]
	[Description("The created string data")]
	public StringData Result => new() { Value = Value };
}

/// <summary>
/// Extracts values from a string data structure.
/// </summary>
[Node("Split String")]
[Description("Extracts values and operations from a string data structure")]
public class SplitStringNode
{
	[InputPin]
	[Description("The string data to split")]
	public StringData Input { get; set; }

	[OutputPin]
	[Description("The stored value")]
	public string Value => Input.Value ?? string.Empty;

	[OutputPin]
	[Description("String length")]
	public int Length => Input.Value?.Length ?? 0;

	[OutputPin]
	[Description("Is empty or null")]
	public bool IsEmpty => string.IsNullOrEmpty(Input.Value);

	[OutputPin]
	[Description("Uppercase version")]
	public string Upper => Input.Value?.ToUpperInvariant() ?? string.Empty;

	[OutputPin]
	[Description("Lowercase version")]
	public string Lower => Input.Value?.ToLowerInvariant() ?? string.Empty;
}

/// <summary>
/// Updates a string data structure with new value.
/// </summary>
[Node("Set String")]
[Description("Creates a new string data structure with updated value")]
public class SetStringNode
{
	[InputPin]
	[Description("The existing string data")]
	public StringData Input { get; set; }

	[InputPin]
	[Description("The new value to set")]
	public string Value { get; set; } = string.Empty;

	[OutputPin]
	[Description("The updated string data")]
	public StringData Result => new() { Value = Value };
}
