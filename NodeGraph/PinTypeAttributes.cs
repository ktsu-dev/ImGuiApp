// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.NodeGraph;
using System;
using System.Collections.Generic;

/// <summary>
/// Utilities for working with pin types and connections using actual Type objects.
/// </summary>
public static class PinTypeUtilities
{
	// Define numeric type categories for compatibility checking
	private static readonly HashSet<Type> NumericTypes =
	[
		typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
		typeof(int), typeof(uint), typeof(long), typeof(ulong),
		typeof(float), typeof(double), typeof(decimal)
	];

	private static readonly HashSet<Type> IntegerTypes =
	[
		typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
		typeof(int), typeof(uint), typeof(long), typeof(ulong)
	];

	private static readonly HashSet<Type> FloatingPointTypes =
	[
		typeof(float), typeof(double), typeof(decimal)
	];

	/// <summary>
	/// Determines if two pin types can be connected together using Type objects.
	/// </summary>
	/// <param name="sourceType">The source pin's data type.</param>
	/// <param name="targetType">The target pin's data type.</param>
	/// <returns>True if the pins can be connected.</returns>
	public static bool CanConnect(Type sourceType, Type targetType)
	{
		// Handle null types
		if (sourceType == null || targetType == null)
		{
			return false;
		}

		// Exact type match is always allowed
		if (sourceType == targetType)
		{
			return true;
		}

		// Execution pins (void type) can only connect to other execution pins
		if (sourceType == typeof(void) || targetType == typeof(void))
		{
			return sourceType == typeof(void) && targetType == typeof(void);
		}

		// Use .NET's built-in assignability check
		if (targetType.IsAssignableFrom(sourceType))
		{
			return true;
		}

		// Check for numeric conversions
		if (IsNumericConversionAllowed(sourceType, targetType))
		{
			return true;
		}

		// Check for string conversions (most types can convert to string)
		if (targetType == typeof(string))
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// Determines if a connection requires explicit type conversion.
	/// </summary>
	/// <param name="sourceType">The source pin's data type.</param>
	/// <param name="targetType">The target pin's data type.</param>
	/// <returns>True if conversion is required.</returns>
	public static bool RequiresConversion(Type sourceType, Type targetType)
	{
		ArgumentNullException.ThrowIfNull(sourceType);
		ArgumentNullException.ThrowIfNull(targetType);

		// If they can't connect at all, no conversion can help
		if (!CanConnect(sourceType, targetType))
		{
			return false;
		}

		// Exact matches and direct assignability don't need conversion
		if (sourceType == targetType || targetType.IsAssignableFrom(sourceType))
		{
			return false;
		}

		return true;
	}

	/// <summary>
	/// Determines if a numeric conversion between two types is allowed.
	/// </summary>
	/// <param name="sourceType">The source numeric type.</param>
	/// <param name="targetType">The target numeric type.</param>
	/// <returns>True if the conversion is allowed.</returns>
	private static bool IsNumericConversionAllowed(Type sourceType, Type targetType)
	{
		// Both must be numeric types
		if (!IsNumericType(sourceType) || !IsNumericType(targetType))
		{
			return false;
		}

		// Allow all numeric conversions (runtime will handle them)
		return true;
	}

	/// <summary>
	/// Determines if a type is numeric.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <returns>True if the type is numeric.</returns>
	public static bool IsNumericType(Type type) => NumericTypes.Contains(type);

	/// <summary>
	/// Determines if a conversion might lose data (e.g., double to int).
	/// </summary>
	/// <param name="sourceType">The source type.</param>
	/// <param name="targetType">The target type.</param>
	/// <returns>True if the conversion might lose precision or range.</returns>
	public static bool IsLossyConversion(Type sourceType, Type targetType)
	{
		ArgumentNullException.ThrowIfNull(sourceType);
		ArgumentNullException.ThrowIfNull(targetType);

		if (!IsNumericType(sourceType) || !IsNumericType(targetType))
		{
			return false;
		}

		// Floating point to integer is lossy
		if (FloatingPointTypes.Contains(sourceType) && IntegerTypes.Contains(targetType))
		{
			return true;
		}

		// Larger integer to smaller integer might be lossy
		if (IntegerTypes.Contains(sourceType) && IntegerTypes.Contains(targetType))
		{
			return GetNumericPrecedence(sourceType) > GetNumericPrecedence(targetType);
		}

		// Higher precision float to lower precision might be lossy
		if (FloatingPointTypes.Contains(sourceType) && FloatingPointTypes.Contains(targetType))
		{
			return GetNumericPrecedence(sourceType) > GetNumericPrecedence(targetType);
		}

		return false;
	}

	/// <summary>
	/// Gets all numeric types.
	/// </summary>
	public static IEnumerable<Type> AllNumericTypes => NumericTypes;

	/// <summary>
	/// Gets a numeric precedence value for ordering types by size/precision.
	/// </summary>
	private static int GetNumericPrecedence(Type type)
	{
		return type.Name switch
		{
			nameof(Byte) => 1,
			nameof(SByte) => 1,
			nameof(Int16) => 2,
			nameof(UInt16) => 2,
			nameof(Int32) => 3,
			nameof(UInt32) => 3,
			nameof(Single) => 4, // float
			nameof(Int64) => 5,
			nameof(UInt64) => 5,
			nameof(Double) => 6,
			nameof(Decimal) => 7,
			_ => 0
		};
	}
}

/// <summary>
/// Provides metadata about what types of pins can connect to each other.
/// </summary>
/// <remarks>
/// Initializes a new instance of the PinConnectionRuleAttribute class.
/// </remarks>
/// <param name="sourceType">The source pin type.</param>
/// <param name="targetType">The target pin type.</param>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class, AllowMultiple = true)]
public sealed class PinConnectionRuleAttribute(string sourceType, string targetType) : Attribute
{
	/// <summary>
	/// The source pin type that can connect.
	/// </summary>
	public string SourceType { get; } = sourceType;

	/// <summary>
	/// The target pin type that can be connected to.
	/// </summary>
	public string TargetType { get; } = targetType;

	/// <summary>
	/// Whether this connection requires a type conversion.
	/// </summary>
	public bool RequiresConversion { get; set; }

	/// <summary>
	/// The converter type to use for this connection (if conversion is required).
	/// </summary>
	public Type? ConverterType { get; set; }
}

/// <summary>
/// Marks a pin as accepting wildcard connections (can connect to any compatible type).
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class WildcardPinAttribute : Attribute
{
	/// <summary>
	/// The base type constraint for wildcard connections.
	/// </summary>
	public Type? BaseTypeConstraint { get; set; }

	/// <summary>
	/// Type constraints for wildcard connections.
	/// </summary>
	public Type[]? TypeConstraints { get; set; }

	/// <summary>
	/// Initializes a new instance of the WildcardPinAttribute class.
	/// </summary>
	public WildcardPinAttribute()
	{
	}

	/// <summary>
	/// Initializes a new instance of the WildcardPinAttribute class with type constraints.
	/// </summary>
	/// <param name="typeConstraints">Type constraints for allowed connections.</param>
	public WildcardPinAttribute(params Type[] typeConstraints) => TypeConstraints = typeConstraints;

	/// <summary>
	/// Determines if a type is allowed by this wildcard constraint.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <returns>True if the type is allowed.</returns>
	public bool AllowsType(Type type)
	{
		ArgumentNullException.ThrowIfNull(type);

		// If no constraints, allow any type
		if (TypeConstraints == null || TypeConstraints.Length == 0)
		{
			return true;
		}

		// Check if type matches any constraint exactly
		foreach (Type constraintType in TypeConstraints)
		{
			if (type == constraintType)
			{
				return true;
			}
		}

		return false;
	}
}
