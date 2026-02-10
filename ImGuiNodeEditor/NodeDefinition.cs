// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiNodeEditor;

using System;
using System.Collections.Generic;
using System.Reflection;
using ktsu.NodeGraph;

/// <summary>
/// Represents the metadata definition of a node type based on attributes.
/// </summary>
public class NodeDefinition
{
	/// <summary>
	/// The .NET type that this node definition represents.
	/// </summary>
	public Type NodeType { get; set; } = null!;

	/// <summary>
	/// The method that this node definition represents (for method-based nodes).
	/// Null for type-based nodes.
	/// </summary>
	public MethodInfo? Method { get; set; }

	/// <summary>
	/// The display name of the node.
	/// </summary>
	public string DisplayName { get; set; } = string.Empty;

	/// <summary>
	/// A description of what this node does.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	/// The category this node belongs to.
	/// </summary>
	public string Category { get; set; } = "Custom";

	/// <summary>
	/// A color hint for the node.
	/// </summary>
	public string? ColorHint { get; set; }

	/// <summary>
	/// Tags for additional categorization.
	/// </summary>
	public List<string> Tags { get; set; } = [];

	/// <summary>
	/// The execution mode for this node.
	/// </summary>
	public NodeExecutionMode ExecutionMode { get; set; } = NodeExecutionMode.OnInputChange;

	/// <summary>
	/// Whether this node supports asynchronous execution.
	/// </summary>
	public bool SupportsAsyncExecution { get; set; }

	/// <summary>
	/// Whether this node has side effects.
	/// </summary>
	public bool HasSideEffects { get; set; }

	/// <summary>
	/// Whether this node's output is deterministic.
	/// </summary>
	public bool IsDeterministic { get; set; } = true;

	/// <summary>
	/// Whether this node can be cached.
	/// </summary>
	public bool IsCacheable { get; set; }

	/// <summary>
	/// Whether this node is visible in the creation menu.
	/// </summary>
	public bool VisibleInMenu { get; set; } = true;

	/// <summary>
	/// Whether this node can be instantiated.
	/// </summary>
	public bool CanBeInstantiated { get; set; } = true;

	/// <summary>
	/// Whether this node is experimental.
	/// </summary>
	public bool IsExperimental { get; set; }

	/// <summary>
	/// The minimum editor version required for this node.
	/// </summary>
	public string? MinimumEditorVersion { get; set; }

	/// <summary>
	/// Required features for this node to function.
	/// </summary>
	public List<string> RequiredFeatures { get; set; } = [];

	/// <summary>
	/// Whether this node is deprecated.
	/// </summary>
	public bool IsDeprecated { get; set; }

	/// <summary>
	/// The reason why this node is deprecated.
	/// </summary>
	public string? DeprecationReason { get; set; }

	/// <summary>
	/// The recommended replacement node type.
	/// </summary>
	public Type? ReplacementType { get; set; }

	/// <summary>
	/// The version when this node was deprecated.
	/// </summary>
	public string? DeprecatedInVersion { get; set; }

	/// <summary>
	/// The version when this node will be removed.
	/// </summary>
	public string? RemovalVersion { get; set; }

	/// <summary>
	/// The input pin definitions for this node.
	/// </summary>
	public List<PinDefinition> InputPins { get; set; } = [];

	/// <summary>
	/// The output pin definitions for this node.
	/// </summary>
	public List<PinDefinition> OutputPins { get; set; } = [];
}

/// <summary>
/// Represents the metadata definition of a pin based on attributes.
/// </summary>
public class PinDefinition
{
	/// <summary>
	/// The member (property, field, method, or parameter) that this pin represents.
	/// </summary>
	public object Member { get; set; } = null!;

	/// <summary>
	/// The display name of the pin.
	/// </summary>
	public string DisplayName { get; set; } = string.Empty;

	/// <summary>
	/// A description of what this pin represents.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	/// The order in which this pin should appear.
	/// </summary>
	public int Order { get; set; }

	/// <summary>
	/// Whether this pin is required.
	/// </summary>
	public bool IsRequired { get; set; } = true;

	/// <summary>
	/// The type of pin (Data or Execution).
	/// </summary>
	public PinType PinType { get; set; }

	/// <summary>
	/// The actual .NET type of the pin's data.
	/// </summary>
	public Type DataType { get; set; } = typeof(object);

	/// <summary>
	/// String-based type identifier for display and serialization.
	/// </summary>
	public string DisplayTypeId { get; set; } = string.Empty;

	/// <summary>
	/// A color hint for the pin.
	/// </summary>
	public string? ColorHint { get; set; }

	/// <summary>
	/// Whether this is an input pin (false = output pin).
	/// </summary>
	public bool IsInput { get; set; }

	/// <summary>
	/// Whether this pin can have multiple connections.
	/// </summary>
	public bool AllowMultipleConnections { get; set; }

	/// <summary>
	/// The default value for input pins when no connection is present.
	/// </summary>
	public object? DefaultValue { get; set; }

	/// <summary>
	/// Determines if this pin can connect to another pin using Type-based compatibility.
	/// </summary>
	/// <param name="otherPin">The other pin to check compatibility with.</param>
	/// <returns>True if the pins can be connected.</returns>
	public bool CanConnectTo(PinDefinition otherPin)
	{
		// Can't connect pins of the same direction
		if (IsInput == otherPin.IsInput)
		{
			return false;
		}

		// Use Type-based connection logic
		Type sourceType = IsInput ? otherPin.DataType : DataType;
		Type targetType = IsInput ? DataType : otherPin.DataType;

		return PinTypeUtilities.CanConnect(sourceType, targetType);
	}

	/// <summary>
	/// Determines if connecting to another pin would require type conversion.
	/// </summary>
	/// <param name="otherPin">The other pin to check.</param>
	/// <returns>True if conversion is required.</returns>
	public bool RequiresConversionTo(PinDefinition otherPin)
	{
		if (!CanConnectTo(otherPin))
		{
			return false;
		}

		Type sourceType = IsInput ? otherPin.DataType : DataType;
		Type targetType = IsInput ? DataType : otherPin.DataType;

		return PinTypeUtilities.RequiresConversion(sourceType, targetType);
	}

	/// <summary>
	/// Determines if connecting to another pin would result in lossy conversion.
	/// </summary>
	/// <param name="otherPin">The other pin to check.</param>
	/// <returns>True if the conversion might lose data.</returns>
	public bool IsLossyConversionTo(PinDefinition otherPin)
	{
		if (!RequiresConversionTo(otherPin))
		{
			return false;
		}

		Type sourceType = IsInput ? otherPin.DataType : DataType;
		Type targetType = IsInput ? DataType : otherPin.DataType;

		return PinTypeUtilities.IsLossyConversion(sourceType, targetType);
	}

	/// <summary>
	/// Gets the value of this pin from an instance.
	/// </summary>
	/// <param name="instance">The instance to get the value from.</param>
	/// <returns>The pin value.</returns>
	public object? GetValue(object instance)
	{
		return Member switch
		{
			PropertyInfo prop => prop.GetValue(instance),
			FieldInfo field => field.GetValue(instance),
			_ => null
		};
	}

	/// <summary>
	/// Sets the value of this pin on an instance.
	/// </summary>
	/// <param name="instance">The instance to set the value on.</param>
	/// <param name="value">The value to set.</param>
	public void SetValue(object instance, object? value)
	{
		switch (Member)
		{
			case PropertyInfo prop when prop.CanWrite:
				prop.SetValue(instance, value);
				break;
			case FieldInfo field:
				field.SetValue(instance, value);
				break;
		}
	}
}
