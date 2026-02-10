// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

// Logic operations including type conversions and comparisons.

namespace ktsu.NodeGraph.Library.Operations;
using System;
using System.ComponentModel;

/// <summary>
/// Type conversion operations as static method nodes.
/// </summary>
public static class TypeConversions
{
	[Node("To String")]
	[Description("Converts any value to string")]
	public static string ToString(object value) => value?.ToString() ?? "null";

	[Node("To Int")]
	[Description("Converts string or number to integer")]
	public static int ToInt(object value)
	{
		return value switch
		{
			int i => i,
			double d => (int)d,
			float f => (int)f,
			string s when int.TryParse(s, out int result) => result,
			_ => 0
		};
	}

	[Node("To Double")]
	[Description("Converts string or number to double")]
	public static double ToDouble(object value)
	{
		return value switch
		{
			double d => d,
			float f => f,
			int i => i,
			string s when double.TryParse(s, out double result) => result,
			_ => 0.0
		};
	}

	[Node("To Bool")]
	[Description("Converts value to boolean")]
	public static bool ToBool(object value)
	{
		return value switch
		{
			bool b => b,
			string s => s.Equals("true", StringComparison.OrdinalIgnoreCase),
			int i => i != 0,
			double d => d != 0.0,
			_ => false
		};
	}
}

/// <summary>
/// Comparison operations as static method nodes.
/// </summary>
public static class Comparisons
{
	[Node("Equal")]
	[Description("Checks if two values are equal")]
	public static bool Equal(object a, object b) => Equals(a, b);

	[Node("Not Equal")]
	[Description("Checks if two values are not equal")]
	public static bool NotEqual(object a, object b) => !Equals(a, b);

	[Node("Greater Than")]
	[Description("Checks if first number is greater than second")]
	public static bool GreaterThan(double a, double b) => a > b;

	[Node("Less Than")]
	[Description("Checks if first number is less than second")]
	public static bool LessThan(double a, double b) => a < b;

	[Node("Greater Or Equal")]
	[Description("Checks if first number is greater than or equal to second")]
	public static bool GreaterOrEqual(double a, double b) => a >= b;

	[Node("Less Or Equal")]
	[Description("Checks if first number is less than or equal to second")]
	public static bool LessOrEqual(double a, double b) => a <= b;

	[Node("And")]
	[Description("Logical AND operation")]
	public static bool And(bool a, bool b) => a && b;

	[Node("Or")]
	[Description("Logical OR operation")]
	public static bool Or(bool a, bool b) => a || b;

	[Node("Not")]
	[Description("Logical NOT operation")]
	public static bool Not(bool value) => !value;
}
