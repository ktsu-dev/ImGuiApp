// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.NodeGraph;
using System;
using System.Linq;
using System.Reflection;

/// <summary>
/// Defines the fundamental types of pins in a node graph.
/// </summary>
public enum PinType
{
	/// <summary>
	/// Data pins carry typed data between nodes.
	/// </summary>
	Data,

	/// <summary>
	/// Execution pins control the flow of execution through the graph.
	/// </summary>
	Execution
}

/// <summary>
/// Base class for pin attributes that define input and output connections on nodes.
/// </summary>
public abstract class PinAttribute : Attribute
{
	/// <summary>
	/// The type of pin (Data or Execution).
	/// </summary>
	public abstract PinType PinType { get; }

	/// <summary>
	/// The display name of the pin. If null, automatically uses the member name.
	/// </summary>
	public string? DisplayName { get; set; }

	/// <summary>
	/// The order in which this pin should appear relative to other pins of the same type.
	/// </summary>
	public int Order { get; set; }

	/// <summary>
	/// Whether this pin is required for the node to function properly.
	/// </summary>
	public bool IsRequired { get; set; } = true;

	/// <summary>
	/// Gets the actual C# type of the member this attribute is applied to.
	/// For data pins, this is automatically determined from the member.
	/// For execution pins, this returns typeof(void).
	/// </summary>
	public Type? DataType { get; internal set; }

	/// <summary>
	/// Gets a string identifier for the type, used for display and serialization.
	/// For data pins, this is automatically generated from the DataType.
	/// For execution pins, this is always "execution".
	/// </summary>
	public string DisplayTypeId { get; internal set; } = string.Empty;

	/// <summary>
	/// A color hint for the pin (format agnostic).
	/// </summary>
	public string? ColorHint { get; set; }

	/// <summary>
	/// Initializes a new instance of the PinAttribute class.
	/// </summary>
	protected PinAttribute()
	{
	}

	/// <summary>
	/// Initializes a new instance of the PinAttribute class with a display name.
	/// </summary>
	/// <param name="displayName">The display name of the pin.</param>
	protected PinAttribute(string displayName) => DisplayName = displayName;

	/// <summary>
	/// Initializes the type information for this pin based on the member it's applied to.
	/// This is called by the attribute processing system.
	/// </summary>
	/// <param name="member">The member this attribute is applied to (MemberInfo or ParameterInfo).</param>
	public virtual void InitializeTypeInfo(object member)
	{
		// Validate that the member's parent has a [Node] attribute
		ValidateParentIsNode(member);

		// Validate specific pin type constraints
		ValidatePinConstraints(member);

		if (PinType == PinType.Data)
		{
			DataType = member switch
			{
				PropertyInfo prop => prop.PropertyType,
				FieldInfo field => field.FieldType,
				MethodInfo method => method.ReturnType,
				ParameterInfo param => param.ParameterType,
				_ => throw new InvalidOperationException($"Pin attributes can only be applied to properties, fields, parameters, or methods, not {member.GetType().Name}")
			};

			DisplayTypeId = GenerateDisplayTypeId(DataType);
		}
		else if (PinType == PinType.Execution)
		{
			DataType = typeof(void);
			DisplayTypeId = "execution";
		}
	}

	/// <summary>
	/// Validates that the member's parent (declaring type or method) has a [Node] attribute.
	/// </summary>
	/// <param name="member">The member to validate.</param>
	protected virtual void ValidateParentIsNode(object member)
	{
		Type? parentType = member switch
		{
			PropertyInfo prop => prop.DeclaringType,
			FieldInfo field => field.DeclaringType,
			MethodInfo method => method.DeclaringType,
			ParameterInfo param => param.Member?.DeclaringType,
			_ => null
		} ?? throw new InvalidOperationException("Unable to determine parent type for pin validation");

		// Check if the parent type has a [Node] attribute
		bool hasNodeAttribute = parentType.GetCustomAttribute<NodeAttribute>() != null;

		// For parameters, also check if the method itself has a [Node] attribute
		if (!hasNodeAttribute && member is ParameterInfo parameter)
		{
			hasNodeAttribute = parameter.Member?.GetCustomAttribute<NodeAttribute>() != null;
		}

		// For methods being processed as nodes, the method itself should have [Node] attribute
		if (!hasNodeAttribute && member is MethodInfo methodInfo)
		{
			hasNodeAttribute = methodInfo.GetCustomAttribute<NodeAttribute>() != null;
		}

		if (!hasNodeAttribute)
		{
			throw new InvalidOperationException($"Pin attributes can only be applied to members of types or methods decorated with [Node]. " +
				$"The parent '{parentType.Name}' does not have a [Node] attribute.");
		}
	}

	/// <summary>
	/// Validates pin-specific constraints based on the pin type and member type.
	/// </summary>
	/// <param name="member">The member to validate.</param>
	protected virtual void ValidatePinConstraints(object member)
	{
		// No additional constraints for base PinAttribute
	}

	/// <summary>
	/// Generates a display string identifier from a .NET type for UI purposes.
	/// Uses the actual type names instead of custom mappings.
	/// </summary>
	/// <param name="type">The type to generate a display identifier for.</param>
	/// <returns>A friendly string identifier for the type.</returns>
	private static string GenerateDisplayTypeId(Type type)
	{
		// Handle execution pins specially
		if (type == typeof(void))
		{
			return "execution";
		}

		// Handle arrays with element type
		if (type.IsArray)
		{
			Type elementType = type.GetElementType()!;
			return $"{GetFriendlyTypeName(elementType)}[]";
		}

		// Handle generic types
		if (type.IsGenericType)
		{
			return GetGenericTypeDisplayName(type);
		}

		// Handle nullable types
		Type? underlyingType = Nullable.GetUnderlyingType(type);
		if (underlyingType != null)
		{
			return $"{GetFriendlyTypeName(underlyingType)}?";
		}

		// Use friendly name for the type
		return GetFriendlyTypeName(type);
	}

	/// <summary>
	/// Gets a friendly display name for a type, using C# aliases where appropriate.
	/// </summary>
	/// <param name="type">The type to get a friendly name for.</param>
	/// <returns>A friendly type name.</returns>
	private static string GetFriendlyTypeName(Type type)
	{
		// Use C# aliases for common types
		return Type.GetTypeCode(type) switch
		{
			TypeCode.Boolean => "bool",
			TypeCode.Byte => "byte",
			TypeCode.SByte => "sbyte",
			TypeCode.Char => "char",
			TypeCode.Decimal => "decimal",
			TypeCode.Double => "double",
			TypeCode.Single => "float",
			TypeCode.Int32 => "int",
			TypeCode.UInt32 => "uint",
			TypeCode.Int64 => "long",
			TypeCode.UInt64 => "ulong",
			TypeCode.Int16 => "short",
			TypeCode.UInt16 => "ushort",
			TypeCode.String => "string",
			TypeCode.Object => GetObjectTypeName(type),
			_ => type.Name
		};
	}

	/// <summary>
	/// Gets display name for object types (non-primitives).
	/// </summary>
	/// <param name="type">The object type.</param>
	/// <returns>A friendly name for the object type.</returns>
	private static string GetObjectTypeName(Type type)
	{
		// Handle some common framework types with friendly names
		if (type == typeof(object))
		{
			return "object";
		}

		if (type == typeof(System.Numerics.Vector2))
		{
			return "Vector2";
		}

		if (type == typeof(System.Numerics.Vector3))
		{
			return "Vector3";
		}

		if (type == typeof(System.Numerics.Vector4))
		{
			return "Vector4";
		}

		// For other types, use the simple name without namespace
		return type.Name;
	}

	/// <summary>
	/// Gets a display name for generic types like List&lt;T&gt;.
	/// </summary>
	/// <param name="type">The generic type.</param>
	/// <returns>A friendly display name.</returns>
	private static string GetGenericTypeDisplayName(Type type)
	{
		string typeName = type.Name;

		// Remove the generic arity suffix (e.g., List`1 -> List)
		int backtickIndex = typeName.IndexOf('`', StringComparison.Ordinal);
		if (backtickIndex >= 0)
		{
			typeName = typeName[..backtickIndex];
		}

		// Get generic arguments
		Type[] genericArgs = type.GetGenericArguments();
		if (genericArgs.Length > 0)
		{
			string[] argNames = [.. genericArgs.Select(GetFriendlyTypeName)];
			return $"{typeName}<{string.Join(", ", argNames)}>";
		}

		return typeName;
	}
}

/// <summary>
/// Marks a property, field, or parameter as a data input pin on a node.
/// Input pins receive typed data from other nodes.
/// The pin type is automatically determined from the member type.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class InputPinAttribute : PinAttribute
{
	/// <summary>
	/// This is a data pin.
	/// </summary>
	public override PinType PinType => PinType.Data;

	/// <summary>
	/// Whether this input can accept multiple connections.
	/// </summary>
	public bool AllowMultipleConnections { get; set; } = false;

	/// <summary>
	/// The default value to use when no connection is present.
	/// Must be compatible with the member's type.
	/// </summary>
	public object? DefaultValue { get; set; }

	/// <summary>
	/// Initializes a new instance of the InputPinAttribute class.
	/// </summary>
	public InputPinAttribute()
	{
	}

	/// <summary>
	/// Initializes a new instance of the InputPinAttribute class with a display name.
	/// </summary>
	/// <param name="displayName">The display name of the pin.</param>
	public InputPinAttribute(string displayName) : base(displayName)
	{
	}

	/// <summary>
	/// Validates input pin specific constraints.
	/// Input pins cannot be applied to methods or readonly members.
	/// </summary>
	/// <param name="member">The member to validate.</param>
	protected override void ValidatePinConstraints(object member)
	{
		switch (member)
		{
			case MethodInfo:
				throw new InvalidOperationException("Input pins cannot be applied to methods. Methods can only have output pins.");

			case FieldInfo field when field.IsInitOnly:
				throw new InvalidOperationException($"Input pins cannot be applied to readonly fields. The field '{field.Name}' is readonly.");

			case PropertyInfo prop when prop.SetMethod == null:
				throw new InvalidOperationException($"Input pins cannot be applied to readonly properties. The property '{prop.Name}' does not have a setter.");
		}

		base.ValidatePinConstraints(member);
	}
}

/// <summary>
/// Marks a property, field, or method return value as a data output pin on a node.
/// Output pins send typed data to other nodes.
/// The pin type is automatically determined from the member type.
/// For methods, this represents the return value.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.ReturnValue | AttributeTargets.Method, AllowMultiple = false)]
public sealed class OutputPinAttribute : PinAttribute
{
	/// <summary>
	/// This is a data pin.
	/// </summary>
	public override PinType PinType => PinType.Data;

	/// <summary>
	/// Whether this output can connect to multiple inputs.
	/// </summary>
	public bool AllowMultipleConnections { get; set; } = true;

	/// <summary>
	/// Initializes a new instance of the OutputPinAttribute class.
	/// </summary>
	public OutputPinAttribute()
	{
	}

	/// <summary>
	/// Initializes a new instance of the OutputPinAttribute class with a display name.
	/// </summary>
	/// <param name="displayName">The display name of the pin.</param>
	public OutputPinAttribute(string displayName) : base(displayName)
	{
	}

	/// <summary>
	/// Validates output pin specific constraints.
	/// Output pins can be applied to methods to represent their return value.
	/// </summary>
	/// <param name="member">The member to validate.</param>
	protected override void ValidatePinConstraints(object member) =>
		// Output pins on methods represent the return value, which is valid
		// even if the method has parameters (which would have their own input pins)
		base.ValidatePinConstraints(member);
}
