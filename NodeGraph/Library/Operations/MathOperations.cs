// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

// Mathematical operations as static method nodes.

namespace ktsu.NodeGraph.Library.Operations;
using System;
using System.ComponentModel;

/// <summary>
/// Basic mathematical operations as static method nodes.
/// </summary>
public static class MathOperations
{
	[Node("Add")]
	[Description("Adds two numbers")]
	public static double Add(double a, double b) => a + b;

	[Node("Subtract")]
	[Description("Subtracts second number from first")]
	public static double Subtract(double a, double b) => a - b;

	[Node("Multiply")]
	[Description("Multiplies two numbers")]
	public static double Multiply(double a, double b) => a * b;

	[Node("Divide")]
	[Description("Divides first number by second")]
	public static double Divide(double a, double b) => b != 0 ? a / b : double.NaN;

	[Node("Power")]
	[Description("Raises base to the power of exponent")]
	public static double Power(double baseValue, double exponent) => Math.Pow(baseValue, exponent);

	[Node("Square Root")]
	[Description("Calculates square root")]
	public static double SquareRoot(double value) => Math.Sqrt(value);

	[Node("Absolute")]
	[Description("Returns absolute value")]
	public static double Absolute(double value) => Math.Abs(value);

	[Node("Min")]
	[Description("Returns the smaller of two numbers")]
	public static double Min(double a, double b) => Math.Min(a, b);

	[Node("Max")]
	[Description("Returns the larger of two numbers")]
	public static double Max(double a, double b) => Math.Max(a, b);

	[Node("Clamp")]
	[Description("Clamps a value between min and max")]
	public static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));

	[Node("Sin")]
	[Description("Sine function (input in radians)")]
	public static double Sin(double radians) => Math.Sin(radians);

	[Node("Cos")]
	[Description("Cosine function (input in radians)")]
	public static double Cos(double radians) => Math.Cos(radians);

	[Node("Round")]
	[Description("Rounds to nearest integer")]
	public static double Round(double value) => Math.Round(value);
}

/// <summary>
/// Advanced mathematical operations.
/// </summary>
public static class AdvancedMath
{
	[Node("Degrees To Radians")]
	[Description("Converts degrees to radians")]
	public static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

	[Node("Radians To Degrees")]
	[Description("Converts radians to degrees")]
	public static double RadiansToDegrees(double radians) => radians * 180.0 / Math.PI;

	[Node("Factorial")]
	[Description("Calculates factorial of a number")]
	public static long Factorial(int n)
	{
		if (n < 0)
		{
			return 0;
		}

		if (n <= 1)
		{
			return 1;
		}

		long result = 1;
		for (int i = 2; i <= n; i++)
		{
			result *= i;
		}

		return result;
	}

	[Node("GCD")]
	[Description("Greatest Common Divisor of two numbers")]
	public static int GCD(int a, int b)
	{
		a = Math.Abs(a);
		b = Math.Abs(b);
		while (b != 0)
		{
			int temp = b;
			b = a % b;
			a = temp;
		}
		return a;
	}

	[Node("LCM")]
	[Description("Least Common Multiple of two numbers")]
	public static int LCM(int a, int b) => Math.Abs(a * b) / GCD(a, b);

	[Node("Is Prime")]
	[Description("Checks if a number is prime")]
	public static bool IsPrime(int n)
	{
		if (n <= 1)
		{
			return false;
		}

		if (n <= 3)
		{
			return true;
		}

		if (n % 2 == 0 || n % 3 == 0)
		{
			return false;
		}

		for (int i = 5; i * i <= n; i += 6)
		{
			if (n % i == 0 || n % (i + 2) == 0)
			{
				return false;
			}
		}
		return true;
	}

	[Node("Random Range")]
	[Description("Random integer between min and max (inclusive)")]
	public static int RandomRange(int min, int max) => Random.Shared.Next(min, max + 1);

	[Node("Lerp")]
	[Description("Linear interpolation between two values")]
	public static double Lerp(double a, double b, double t) => a + ((b - a) * Math.Max(0, Math.Min(1, t)));

	[Node("Map Range")]
	[Description("Maps a value from one range to another")]
	public static double MapRange(double value, double fromMin, double fromMax, double toMin, double toMax)
	{
		if (Math.Abs(fromMax - fromMin) < double.Epsilon)
		{
			return toMin;
		}

		return toMin + ((value - fromMin) * (toMax - toMin) / (fromMax - fromMin));
	}
}
