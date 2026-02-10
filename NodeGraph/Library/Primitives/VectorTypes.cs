// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

// Vector data types and their Make/Split/Set node implementations.

namespace ktsu.NodeGraph.Library.Primitives;
using System;
using System.ComponentModel;

/// <summary>
/// 2D Vector data structure.
/// </summary>
public struct Vector2Data : IEquatable<Vector2Data>
{
	public float X { get; set; }
	public float Y { get; set; }

	public readonly bool Equals(Vector2Data other) => X == other.X && Y == other.Y;
	public override readonly bool Equals(object? obj) => obj is Vector2Data other && Equals(other);
	public override readonly int GetHashCode() => HashCode.Combine(X, Y);
	public static bool operator ==(Vector2Data left, Vector2Data right) => left.Equals(right);
	public static bool operator !=(Vector2Data left, Vector2Data right) => !left.Equals(right);
}

/// <summary>
/// Creates a 2D vector data structure.
/// </summary>
[Node("Make Vector2")]
[Description("Creates a 2D vector data structure from X and Y components")]
public class MakeVector2Node
{
	[InputPin]
	[Description("X component")]
	public float X { get; set; }

	[InputPin]
	[Description("Y component")]
	public float Y { get; set; }

	[OutputPin]
	[Description("The created vector data")]
	public Vector2Data Result => new() { X = X, Y = Y };
}

/// <summary>
/// Extracts values from a 2D vector data structure.
/// </summary>
[Node("Split Vector2")]
[Description("Extracts components and calculations from a 2D vector data structure")]
public class SplitVector2Node
{
	[InputPin]
	[Description("The vector data to split")]
	public Vector2Data Input { get; set; }

	[OutputPin]
	[Description("X component")]
	public float X => Input.X;

	[OutputPin]
	[Description("Y component")]
	public float Y => Input.Y;

	[OutputPin]
	[Description("Length of the vector")]
	public float Length => (float)Math.Sqrt((Input.X * Input.X) + (Input.Y * Input.Y));

	[OutputPin]
	[Description("Squared length (faster than Length)")]
	public float LengthSquared => (Input.X * Input.X) + (Input.Y * Input.Y);

	[OutputPin]
	[Description("Normalized vector")]
	public Vector2Data Normalized
	{
		get
		{
			float len = Length;
			return len > 0 ? new Vector2Data { X = Input.X / len, Y = Input.Y / len } : new Vector2Data();
		}
	}
}

/// <summary>
/// Updates a 2D vector data structure with new components.
/// </summary>
[Node("Set Vector2")]
[Description("Creates a new 2D vector data structure with updated components")]
public class SetVector2Node
{
	[InputPin]
	[Description("The existing vector data")]
	public Vector2Data Input { get; set; }

	[InputPin]
	[Description("The new X component")]
	public float X { get; set; }

	[InputPin]
	[Description("The new Y component")]
	public float Y { get; set; }

	[OutputPin]
	[Description("The updated vector data")]
	public Vector2Data Result => new() { X = X, Y = Y };
}
