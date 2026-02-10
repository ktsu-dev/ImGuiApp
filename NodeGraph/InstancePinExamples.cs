// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

// Examples demonstrating automatic instance pin behavior for classes and structs.

namespace ktsu.NodeGraph.Examples;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

/// <summary>
/// Example class showing automatic instance pin behavior.
/// Instance Input: null = create new Counter, connected = use existing Counter
/// Instance Output: the Counter instance after processing (for chaining)
/// </summary>
[Node("Counter")]
[Description("A simple counter with increment/decrement operations")]

public class Counter
{
	[InputPin]
	[Description("Amount to add to counter")]
	public int Increment { get; set; } = 1;

	[OutputPin]
	[Description("Current counter value")]
	public int Value { get; private set; } = 0;

	/// <summary>
	/// Increments the counter by the specified amount.
	/// </summary>
	public void IncrementBy(int amount) => Value += amount;

	/// <summary>
	/// Resets the counter to zero.
	/// </summary>
	public void Reset() => Value = 0;
}

/// <summary>
/// Example struct showing automatic instance pin behavior.
/// Instance Input: null = create new Point, connected = use existing Point
/// Instance Output: the Point instance after processing (for chaining)
/// </summary>
[Node("Point")]
[Description("A 2D point with transformation operations")]

public struct Point : IEquatable<Point>
{
	[InputPin]
	[Description("X coordinate")]
	public float X { get; set; }

	[InputPin]
	[Description("Y coordinate")]
	public float Y { get; set; }

	[OutputPin]
	[Description("Distance from origin")]
	public readonly float DistanceFromOrigin => (float)Math.Sqrt((X * X) + (Y * Y));

	[OutputPin]
	[Description("Point translated by offset")]
	public Point Translated => new() { X = X + OffsetX, Y = Y + OffsetY };

	// These would typically be connected from other nodes
	public float OffsetX { get; set; }
	public float OffsetY { get; set; }

	public readonly bool Equals(Point other) => X == other.X && Y == other.Y && OffsetX == other.OffsetX && OffsetY == other.OffsetY;
	public override readonly bool Equals(object? obj) => obj is Point other && Equals(other);
	public override readonly int GetHashCode() => HashCode.Combine(X, Y, OffsetX, OffsetY);
	public static bool operator ==(Point left, Point right) => left.Equals(right);
	public static bool operator !=(Point left, Point right) => !left.Equals(right);
}

/// <summary>
/// Example showing a more complex class with multiple operations.
/// Demonstrates how instance pins enable object lifecycle management.
/// </summary>
[Node("String Builder")]
[Description("Builds strings with various operations")]

public class StringBuilder
{
	private readonly System.Text.StringBuilder builder = new();

	[InputPin]
	[Description("Text to append")]
	public string TextToAppend { get; set; } = string.Empty;

	[InputPin]
	[Description("Text to prepend")]
	public string TextToPrepend { get; set; } = string.Empty;

	[OutputPin]
	[Description("Current string content")]
	public string Content => builder.ToString();

	[OutputPin]
	[Description("Length of current content")]
	public int Length => builder.Length;

	[OutputPin]
	[Description("Is empty")]
	public bool IsEmpty => builder.Length == 0;

	/// <summary>
	/// Node method to append text.
	/// </summary>
	[Node("Append Text")]
	[Description("Appends text to the string builder")]
	[ExecutionInput]
	[ExecutionOutput]
	public void Append([InputPin][Description("Text to append")] string text) => builder.Append(text);

	/// <summary>
	/// Node method to clear the builder.
	/// </summary>
	[Node("Clear")]
	[Description("Clears all content from the string builder")]
	[ExecutionInput]
	[ExecutionOutput]
	public void Clear() => builder.Clear();
}

/// <summary>
/// Example showing how structs can be used for immutable value transformations.
/// Each operation returns a new instance while maintaining chaining capability.
/// </summary>
[Node("Color")]
[Description("RGB color with transformation operations")]

public struct Color : IEquatable<Color>
{
	[InputPin]
	[Description("Red component (0-1)")]
	public float R { get; set; }

	[InputPin]
	[Description("Green component (0-1)")]
	public float G { get; set; }

	[InputPin]
	[Description("Blue component (0-1)")]
	public float B { get; set; }

	[OutputPin]
	[Description("Brightness of the color")]
	public readonly float Brightness => (R + G + B) / 3.0f;

	[OutputPin]
	[Description("Color with adjusted brightness")]
	public Color WithBrightness
	{
		get
		{
			float currentBrightness = Brightness;
			if (currentBrightness == 0)
			{
				return this;
			}

			float factor = BrightnessAdjustment / currentBrightness;
			return new Color
			{
				R = Math.Min(1.0f, R * factor),
				G = Math.Min(1.0f, G * factor),
				B = Math.Min(1.0f, B * factor)
			};
		}
	}

	[OutputPin]
	[Description("Inverted color")]
	public Color Inverted => new() { R = 1.0f - R, G = 1.0f - G, B = 1.0f - B };

	// Input for brightness adjustment
	public float BrightnessAdjustment { get; set; }

	/// <summary>
	/// Initializes a new Color with default brightness adjustment.
	/// </summary>
	public Color() => BrightnessAdjustment = 1.0f;

	public readonly bool Equals(Color other) => R == other.R && G == other.G && B == other.B && BrightnessAdjustment == other.BrightnessAdjustment;
	public override readonly bool Equals(object? obj) => obj is Color other && Equals(other);
	public override readonly int GetHashCode() => HashCode.Combine(R, G, B, BrightnessAdjustment);
	public static bool operator ==(Color left, Color right) => left.Equals(right);
	public static bool operator !=(Color left, Color right) => !left.Equals(right);
}

/// <summary>
/// Example showing a collection-based class with instance pin behavior.
/// Demonstrates how complex objects can be built up through chaining.
/// </summary>
[Node("Number List")]
[Description("A list of numbers with various operations")]

public class NumberList
{
	private readonly List<double> numbers = [];

	[InputPin]
	[Description("Number to add to the list")]
	public double NumberToAdd { get; set; }

	[OutputPin]
	[Description("Count of numbers in the list")]
	public int Count => numbers.Count;

	[OutputPin]
	[Description("Sum of all numbers")]
	public double Sum => numbers.Sum();

	[OutputPin]
	[Description("Average of all numbers")]
	public double Average => numbers.Count > 0 ? numbers.Average() : 0;

	[OutputPin]
	[Description("Maximum value in the list")]
	public double Max => numbers.Count > 0 ? numbers.Max() : 0;

	[OutputPin]
	[Description("Minimum value in the list")]
	public double Min => numbers.Count > 0 ? numbers.Min() : 0;

	/// <summary>
	/// Node method to add a number to the list.
	/// </summary>
	[Node("Add Number")]
	[Description("Adds a number to the list")]
	[ExecutionInput]
	[ExecutionOutput]
	public void AddNumber([InputPin][Description("Number to add")] double number) => numbers.Add(number);

	/// <summary>
	/// Node method to clear all numbers.
	/// </summary>
	[Node("Clear Numbers")]
	[Description("Removes all numbers from the list")]
	[ExecutionInput]
	[ExecutionOutput]
	public void Clear() => numbers.Clear();

	/// <summary>
	/// Node method to sort the numbers.
	/// </summary>
	[Node("Sort Numbers")]
	[Description("Sorts all numbers in ascending order")]
	[ExecutionInput]
	[ExecutionOutput]
	public void Sort() => numbers.Sort();
}
