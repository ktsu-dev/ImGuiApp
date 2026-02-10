// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiNodeEditor;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using ktsu.NodeGraph;

/// <summary>
/// Factory for creating nodes from classes decorated with node attributes.
/// This provides type-safe node creation from domain models.
/// </summary>
/// <remarks>
/// Initializes a new instance of the AttributeBasedNodeFactory class.
/// </remarks>
/// <param name="engine">The node editor engine to create nodes in.</param>
public class AttributeBasedNodeFactory(NodeEditorEngine engine)
{
	private readonly NodeEditorEngine engine = engine ?? throw new ArgumentNullException(nameof(engine));
	private readonly Dictionary<object, NodeDefinition> nodeDefinitions = []; // Changed to object to support both Type and MethodInfo keys

	/// <summary>
	/// Registers a type as a node definition by scanning its attributes.
	/// </summary>
	/// <typeparam name="T">The type to register as a node.</typeparam>
	public void RegisterNodeType<T>() => RegisterNodeType(typeof(T));

	/// <summary>
	/// Registers a type as a node definition by scanning its attributes.
	/// Also automatically discovers and registers any methods on the type that have [Node] attributes.
	/// </summary>
	/// <param name="nodeType">The type to register as a node.</param>
	public void RegisterNodeType(Type nodeType)
	{
		if (nodeType == null)
		{
			throw new ArgumentNullException(nameof(nodeType));
		}

		// Register the type itself if it has a [Node] attribute
		NodeAttribute? typeNodeAttr = nodeType.GetCustomAttribute<NodeAttribute>();
		if (typeNodeAttr != null)
		{
			NodeDefinition definition = CreateNodeDefinition(nodeType, typeNodeAttr);
			nodeDefinitions[nodeType] = definition;
		}

		// Discover and register any methods on this type that have [Node] attributes
		IEnumerable<MethodInfo> nodeMethods = nodeType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
			.Where(m => m.GetCustomAttribute<NodeAttribute>() != null);

		foreach (MethodInfo nodeMethod in nodeMethods)
		{
			NodeAttribute methodNodeAttr = nodeMethod.GetCustomAttribute<NodeAttribute>()!;
			NodeDefinition methodDefinition = CreateMethodNodeDefinition(nodeMethod, methodNodeAttr);

			// Use the MethodInfo itself as the key for method nodes
			nodeDefinitions[nodeMethod] = methodDefinition;
		}

		// If neither the type nor any methods have [Node] attributes, throw an exception
		if (typeNodeAttr == null && !nodeMethods.Any())
		{
			throw new InvalidOperationException($"Type {nodeType.Name} and its methods are not decorated with [Node] attributes");
		}
	}

	/// <summary>
	/// Registers all types in an assembly that are decorated with [Node] attributes.
	/// Also automatically discovers and registers methods with [Node] attributes on those types.
	/// </summary>
	/// <param name="assembly">The assembly to scan for node types.</param>
	public void RegisterNodeTypesFromAssembly(Assembly assembly)
	{
		if (assembly == null)
		{
			throw new ArgumentNullException(nameof(assembly));
		}

		// Get all types that either have [Node] attributes themselves OR have methods with [Node] attributes
		IEnumerable<Type> candidateTypes = assembly.GetTypes()
			.Where(t => !t.IsAbstract && !t.IsInterface)
			.Where(t =>
				t.GetCustomAttribute<NodeAttribute>() != null || // Type has [Node]
				t.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
					.Any(m => m.GetCustomAttribute<NodeAttribute>() != null) // Or has methods with [Node]
			);

		foreach (Type candidateType in candidateTypes)
		{
			RegisterNodeType(candidateType); // This will now handle both type and method nodes automatically
		}
	}

	/// <summary>
	/// Creates a node instance from a registered type.
	/// </summary>
	/// <typeparam name="T">The type of node to create.</typeparam>
	/// <param name="position">The position to place the node.</param>
	/// <returns>The created node.</returns>
	public Node CreateNode<T>(Vector2 position) => CreateNode(typeof(T), position);

	/// <summary>
	/// Creates a node instance from a registered type.
	/// </summary>
	/// <param name="nodeType">The type of node to create.</param>
	/// <param name="position">The position to place the node.</param>
	/// <returns>The created node.</returns>
	public Node CreateNode(Type nodeType, Vector2 position)
	{
		if (!nodeDefinitions.TryGetValue(nodeType, out NodeDefinition? definition))
		{
			throw new InvalidOperationException($"Node type {nodeType.Name} is not registered");
		}

		// Extract pin names from definition
		List<string> inputPinNames = [.. definition.InputPins
			.OrderBy(p => p.Order)
			.Select(p => p.DisplayName)];

		List<string> outputPinNames = [.. definition.OutputPins
			.OrderBy(p => p.Order)
			.Select(p => p.DisplayName)];

		return engine.CreateNode(
			position,
			definition.DisplayName,
			inputPinNames,
			outputPinNames);
	}

	/// <summary>
	/// Creates a node instance from a registered method.
	/// </summary>
	/// <param name="method">The method to create a node for.</param>
	/// <param name="position">The position to place the node.</param>
	/// <returns>The created node.</returns>
	public Node CreateMethodNode(MethodInfo method, Vector2 position)
	{
		if (!nodeDefinitions.TryGetValue(method, out NodeDefinition? definition))
		{
			throw new InvalidOperationException($"Method {method.DeclaringType?.Name}.{method.Name} is not registered");
		}

		// Extract pin names from definition
		List<string> inputPinNames = [.. definition.InputPins
			.OrderBy(p => p.Order)
			.Select(p => p.DisplayName)];

		List<string> outputPinNames = [.. definition.OutputPins
			.OrderBy(p => p.Order)
			.Select(p => p.DisplayName)];

		return engine.CreateNode(
			position,
			definition.DisplayName,
			inputPinNames,
			outputPinNames);
	}

	/// <summary>
	/// Gets the definition for a registered node type.
	/// </summary>
	/// <param name="nodeType">The node type.</param>
	/// <returns>The node definition, or null if not registered.</returns>
	public NodeDefinition? GetNodeDefinition(Type nodeType) => nodeDefinitions.TryGetValue(nodeType, out NodeDefinition? definition) ? definition : null;

	/// <summary>
	/// Gets the definition for a registered method node.
	/// </summary>
	/// <param name="method">The method.</param>
	/// <returns>The node definition, or null if not registered.</returns>
	public NodeDefinition? GetNodeDefinition(MethodInfo method) => nodeDefinitions.TryGetValue(method, out NodeDefinition? definition) ? definition : null;

	/// <summary>
	/// Gets all registered node definitions.
	/// </summary>
	/// <returns>A collection of all registered node definitions.</returns>
	public IEnumerable<NodeDefinition> GetAllNodeDefinitions() => nodeDefinitions.Values;

	private static NodeDefinition CreateNodeDefinition(Type nodeType, NodeAttribute nodeAttr)
	{
		NodeDefinition definition = new()
		{
			NodeType = nodeType,
			DisplayName = nodeAttr.DisplayName ?? GetNodeDisplayName(nodeType.Name),
			Description = nodeType.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description,
			ColorHint = nodeAttr.ColorHint,
			Tags = nodeAttr.Tags?.ToList() ?? []
		};

		// Scan for behavior attributes
		NodeBehaviorAttribute? behaviorAttr = nodeType.GetCustomAttribute<NodeBehaviorAttribute>();
		if (behaviorAttr != null)
		{
			definition.ExecutionMode = behaviorAttr.ExecutionMode;
			definition.SupportsAsyncExecution = behaviorAttr.SupportsAsyncExecution;
			definition.HasSideEffects = behaviorAttr.HasSideEffects;
			definition.IsDeterministic = behaviorAttr.IsDeterministic;
			definition.IsCacheable = behaviorAttr.IsCacheable;
		}

		// Scan for visibility attributes
		NodeVisibilityAttribute? visibilityAttr = nodeType.GetCustomAttribute<NodeVisibilityAttribute>();
		if (visibilityAttr != null)
		{
			definition.VisibleInMenu = visibilityAttr.VisibleInMenu;
			definition.CanBeInstantiated = visibilityAttr.CanBeInstantiated;
			definition.IsExperimental = visibilityAttr.IsExperimental;
			definition.MinimumEditorVersion = visibilityAttr.MinimumEditorVersion;
			definition.RequiredFeatures = visibilityAttr.RequiredFeatures?.ToList() ?? [];
		}

		// Scan for deprecated attribute
		NodeDeprecatedAttribute? deprecatedAttr = nodeType.GetCustomAttribute<NodeDeprecatedAttribute>();
		if (deprecatedAttr != null)
		{
			definition.IsDeprecated = true;
			definition.DeprecationReason = deprecatedAttr.Reason;
			definition.ReplacementType = deprecatedAttr.ReplacementType;
			definition.DeprecatedInVersion = deprecatedAttr.DeprecatedInVersion;
			definition.RemovalVersion = deprecatedAttr.RemovalVersion;
		}

		// Add automatic instance pin for classes and structs
		AddInstancePin(nodeType, definition);

		// Scan for input and output pins
		ScanPins(nodeType, definition);

		return definition;
	}

	private static NodeDefinition CreateMethodNodeDefinition(MethodInfo method, NodeAttribute nodeAttr)
	{
		NodeDefinition definition = new()
		{
			NodeType = method.DeclaringType ?? typeof(object),
			Method = method,
			DisplayName = nodeAttr.DisplayName ?? method.Name, // Methods don't need "Node" suffix removal
			Description = method.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description,
			ColorHint = nodeAttr.ColorHint,
			Tags = nodeAttr.Tags?.ToList() ?? []
		};

		// Scan for behavior attributes on the method
		NodeBehaviorAttribute? behaviorAttr = method.GetCustomAttribute<NodeBehaviorAttribute>();
		if (behaviorAttr != null)
		{
			definition.ExecutionMode = behaviorAttr.ExecutionMode;
			definition.SupportsAsyncExecution = behaviorAttr.SupportsAsyncExecution;
			definition.HasSideEffects = behaviorAttr.HasSideEffects;
			definition.IsDeterministic = behaviorAttr.IsDeterministic;
			definition.IsCacheable = behaviorAttr.IsCacheable;
		}

		// Scan for visibility attributes on the method
		NodeVisibilityAttribute? visibilityAttr = method.GetCustomAttribute<NodeVisibilityAttribute>();
		if (visibilityAttr != null)
		{
			definition.VisibleInMenu = visibilityAttr.VisibleInMenu;
			definition.CanBeInstantiated = visibilityAttr.CanBeInstantiated;
			definition.IsExperimental = visibilityAttr.IsExperimental;
			definition.MinimumEditorVersion = visibilityAttr.MinimumEditorVersion;
			definition.RequiredFeatures = visibilityAttr.RequiredFeatures?.ToList() ?? [];
		}

		// Scan for deprecated attribute on the method
		NodeDeprecatedAttribute? deprecatedAttr = method.GetCustomAttribute<NodeDeprecatedAttribute>();
		if (deprecatedAttr != null)
		{
			definition.IsDeprecated = true;
			definition.DeprecationReason = deprecatedAttr.Reason;
			definition.ReplacementType = deprecatedAttr.ReplacementType;
			definition.DeprecatedInVersion = deprecatedAttr.DeprecatedInVersion;
			definition.RemovalVersion = deprecatedAttr.RemovalVersion;
		}

		// Scan method for pins
		ScanMethodPins(method, definition);

		return definition;
	}

	private static void ScanPins(Type nodeType, NodeDefinition definition)
	{
		IEnumerable<MemberInfo> members = nodeType.GetMembers(BindingFlags.Public | BindingFlags.Instance)
			.Where(m => m is PropertyInfo or FieldInfo);

		foreach (MemberInfo? member in members)
		{
			// Check for any pin attribute (input, output, execution input, execution output)
			PinAttribute? pinAttr = member.GetCustomAttribute<PinAttribute>();
			if (pinAttr != null)
			{
				// Initialize type information based on the member
				pinAttr.InitializeTypeInfo(member);

				PinDefinition pinDef = new()
				{
					Member = member,
					DisplayName = GetPinDisplayName(pinAttr, member),
					Description = member.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description,
					Order = pinAttr.Order,
					IsRequired = pinAttr.IsRequired,
					PinType = pinAttr.PinType,
					DataType = pinAttr.DataType ?? typeof(object),
					DisplayTypeId = pinAttr.DisplayTypeId,
					ColorHint = pinAttr.ColorHint,
					AllowMultipleConnections = GetAllowMultipleConnections(pinAttr)
				};

				// Determine if this is an input or output pin
				bool isInput = pinAttr is InputPinAttribute or ExecutionInputAttribute;
				pinDef.IsInput = isInput;

				// Set default value for input pins
				if (isInput && pinAttr is InputPinAttribute inputPin)
				{
					pinDef.DefaultValue = inputPin.DefaultValue;
				}

				// Add to appropriate collection
				if (isInput)
				{
					definition.InputPins.Add(pinDef);
				}
				else
				{
					definition.OutputPins.Add(pinDef);
				}
			}
		}

		// Sort pins by order
		definition.InputPins.Sort((a, b) => a.Order.CompareTo(b.Order));
		definition.OutputPins.Sort((a, b) => a.Order.CompareTo(b.Order));
	}

	private static void ScanMethodPins(MethodInfo method, NodeDefinition definition)
	{
		AddInstancePinForMethod(method, definition);
		AddExecutionInputPin(method, definition);
		AddExecutionOutputPin(method, definition);
		AddParameterPins(method, definition);
		AddReturnValuePin(method, definition);
		SortPinsByOrder(definition);
	}

	private static void AddInstancePinForMethod(MethodInfo method, NodeDefinition definition)
	{
		if (!method.IsStatic)
		{
			PinDefinition instancePin = new()
			{
				Member = method,
				DisplayName = "Instance",
				Description = $"Instance of {method.DeclaringType?.Name} to call method on",
				Order = -1000, // Always first
				IsRequired = true,
				PinType = PinType.Data,
				DataType = method.DeclaringType ?? typeof(object),
				DisplayTypeId = GenerateDisplayTypeIdForType(method.DeclaringType ?? typeof(object)),
				IsInput = true,
				AllowMultipleConnections = false
			};

			definition.InputPins.Add(instancePin);
		}
	}

	private static void AddExecutionInputPin(MethodInfo method, NodeDefinition definition)
	{
		ExecutionInputAttribute? execInputAttr = method.GetCustomAttribute<ExecutionInputAttribute>();
		if (execInputAttr != null)
		{
			execInputAttr.InitializeTypeInfo(method);

			PinDefinition execPin = new()
			{
				Member = method,
				DisplayName = GetPinDisplayName(execInputAttr, method),
				Description = method.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description,
				Order = execInputAttr.Order,
				IsRequired = execInputAttr.IsRequired,
				PinType = execInputAttr.PinType,
				DataType = execInputAttr.DataType ?? typeof(void),
				DisplayTypeId = execInputAttr.DisplayTypeId,
				ColorHint = execInputAttr.ColorHint,
				IsInput = true,
				AllowMultipleConnections = execInputAttr.AllowMultipleConnections
			};

			definition.InputPins.Add(execPin);
		}
	}

	private static void AddExecutionOutputPin(MethodInfo method, NodeDefinition definition)
	{
		ExecutionOutputAttribute? execOutputAttr = method.GetCustomAttribute<ExecutionOutputAttribute>();
		if (execOutputAttr != null)
		{
			execOutputAttr.InitializeTypeInfo(method);

			PinDefinition execPin = new()
			{
				Member = method,
				DisplayName = GetPinDisplayName(execOutputAttr, method),
				Description = method.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description,
				Order = execOutputAttr.Order,
				IsRequired = execOutputAttr.IsRequired,
				PinType = execOutputAttr.PinType,
				DataType = execOutputAttr.DataType ?? typeof(void),
				DisplayTypeId = execOutputAttr.DisplayTypeId,
				ColorHint = execOutputAttr.ColorHint,
				IsInput = false,
				AllowMultipleConnections = execOutputAttr.AllowMultipleConnections
			};

			definition.OutputPins.Add(execPin);
		}
	}

	private static void AddParameterPins(MethodInfo method, NodeDefinition definition)
	{
		foreach (ParameterInfo parameter in method.GetParameters())
		{
			// Check if there's an explicit InputPin attribute for customization
			InputPinAttribute? inputPinAttr = parameter.GetCustomAttribute<InputPinAttribute>();

			PinDefinition pinDef = new()
			{
				Member = parameter,
				DisplayName = inputPinAttr?.DisplayName ?? parameter.Name ?? "Parameter",
				Description = parameter.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description,
				Order = inputPinAttr?.Order ?? 0, // Default order
				IsRequired = inputPinAttr?.IsRequired ?? true,
				PinType = PinType.Data,
				DataType = parameter.ParameterType,
				DisplayTypeId = GenerateDisplayTypeIdForType(parameter.ParameterType),
				ColorHint = inputPinAttr?.ColorHint,
				AllowMultipleConnections = inputPinAttr?.AllowMultipleConnections ?? false,
				IsInput = true, // Parameters are always input pins
								// Set default value - prefer attribute, then parameter default, then null
				DefaultValue = inputPinAttr?.DefaultValue ?? (parameter.HasDefaultValue ? parameter.DefaultValue : null)
			};

			definition.InputPins.Add(pinDef);
		}
	}

	private static void AddReturnValuePin(MethodInfo method, NodeDefinition definition)
	{
		if (method.ReturnType != typeof(void))
		{
			// Check if there's an explicit OutputPin attribute for customization
			OutputPinAttribute? outputAttr = method.GetCustomAttribute<OutputPinAttribute>();

			PinDefinition returnPin = new()
			{
				Member = method,
				DisplayName = outputAttr?.DisplayName ?? "Result",
				Description = method.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description,
				Order = outputAttr?.Order ?? 1000, // Default to last
				IsRequired = outputAttr?.IsRequired ?? false,
				PinType = PinType.Data,
				DataType = method.ReturnType,
				DisplayTypeId = GenerateDisplayTypeIdForType(method.ReturnType),
				ColorHint = outputAttr?.ColorHint,
				IsInput = false,
				AllowMultipleConnections = outputAttr?.AllowMultipleConnections ?? true
			};

			definition.OutputPins.Add(returnPin);
		}
	}

	private static void SortPinsByOrder(NodeDefinition definition)
	{
		definition.InputPins.Sort((a, b) => a.Order.CompareTo(b.Order));
		definition.OutputPins.Sort((a, b) => a.Order.CompareTo(b.Order));
	}

	/// <summary>
	/// Gets the display name for a node type, automatically removing "Node" suffix if present.
	/// </summary>
	/// <param name="typeName">The type name to process.</param>
	/// <returns>The display name with "Node" suffix removed if present.</returns>
	private static string GetNodeDisplayName(string typeName)
	{
		// Remove "Node" suffix if present (case-sensitive)
		if (typeName.EndsWith("Node", StringComparison.Ordinal) && typeName.Length > 4)
		{
			return typeName[..^4]; // Remove last 4 characters ("Node")
		}

		return typeName;
	}

	/// <summary>
	/// Helper method to generate display type ID for a type.
	/// </summary>
	/// <param name="type">The type to generate display ID for.</param>
	/// <returns>Display type ID string.</returns>
	private static string GenerateDisplayTypeIdForType(Type type)
	{
		// Reuse the logic from PinAttribute.GenerateDisplayTypeId
		// This is a simplified version - in practice you might want to refactor this into a shared utility
		if (type == typeof(void))
		{
			return "execution";
		}

		if (type == typeof(int))
		{
			return "int";
		}

		if (type == typeof(float))
		{
			return "float";
		}

		if (type == typeof(double))
		{
			return "double";
		}

		if (type == typeof(string))
		{
			return "string";
		}

		if (type == typeof(bool))
		{
			return "bool";
		}

		return type.Name;
	}

	/// <summary>
	/// Adds instance pins for classes and structs.
	/// If the type has constructor parameters, creates input pins for them.
	/// Always adds an instance output pin for chaining operations.
	/// </summary>
	/// <param name="nodeType">The type to add instance pin for.</param>
	/// <param name="definition">The node definition to add the pin to.</param>
	private static void AddInstancePin(Type nodeType, NodeDefinition definition)
	{
		// Only add instance pins for classes and structs (not interfaces or abstract classes)
		if ((nodeType.IsClass && !nodeType.IsAbstract) || nodeType.IsValueType)
		{
			// Find the best constructor to use for creating instances
			ConstructorInfo? constructor = GetBestConstructor(nodeType);

			if (constructor != null && constructor.GetParameters().Length > 0)
			{
				// Add input pins for constructor parameters
				AddConstructorParameterPins(constructor, definition);
			}

			// Always add instance output pin for fluent chaining
			PinDefinition instanceOutputPin = new()
			{
				Member = nodeType,
				DisplayName = "Instance",
				Description = $"The {nodeType.Name} instance after processing.",
				Order = 1000, // Always last
				IsRequired = false,
				PinType = PinType.Data,
				DataType = nodeType,
				DisplayTypeId = GenerateDisplayTypeIdForType(nodeType),
				IsInput = false,
				AllowMultipleConnections = true // Can connect to multiple downstream nodes
			};

			definition.OutputPins.Add(instanceOutputPin);
		}
	}

	/// <summary>
	/// Gets the best constructor to use for creating instances.
	/// Prefers public constructors, then the one with the most parameters.
	/// </summary>
	/// <param name="nodeType">The type to find a constructor for.</param>
	/// <returns>The best constructor, or null if none found.</returns>
	private static ConstructorInfo? GetBestConstructor(Type nodeType)
	{
		ConstructorInfo[] constructors = nodeType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

		// Prefer the constructor with the most parameters (most specific)
		return constructors
			.OrderByDescending(c => c.GetParameters().Length)
			.FirstOrDefault();
	}

	/// <summary>
	/// Adds input pins for constructor parameters.
	/// </summary>
	/// <param name="constructor">The constructor to create pins for.</param>
	/// <param name="definition">The node definition to add pins to.</param>
	private static void AddConstructorParameterPins(ConstructorInfo constructor, NodeDefinition definition)
	{
		foreach (ParameterInfo parameter in constructor.GetParameters())
		{
			// Check if there's an explicit InputPin attribute for customization
			InputPinAttribute? inputPinAttr = parameter.GetCustomAttribute<InputPinAttribute>();

			PinDefinition pinDef = new()
			{
				Member = parameter,
				DisplayName = inputPinAttr?.DisplayName ?? parameter.Name ?? "Parameter",
				Description = parameter.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description ?? $"Constructor parameter: {parameter.Name}",
				Order = inputPinAttr?.Order ?? -500, // Constructor params come early but after instance
				IsRequired = inputPinAttr?.IsRequired ?? !parameter.HasDefaultValue, // Required if no default value
				PinType = PinType.Data,
				DataType = parameter.ParameterType,
				DisplayTypeId = GenerateDisplayTypeIdForType(parameter.ParameterType),
				ColorHint = inputPinAttr?.ColorHint,
				AllowMultipleConnections = inputPinAttr?.AllowMultipleConnections ?? false,
				IsInput = true,
				DefaultValue = inputPinAttr?.DefaultValue ?? (parameter.HasDefaultValue ? parameter.DefaultValue : null)
			};

			definition.InputPins.Add(pinDef);
		}
	}

	/// <summary>
	/// Gets the display name for a pin, defaulting to the member name if not explicitly set.
	/// </summary>
	/// <param name="pinAttr">The pin attribute.</param>
	/// <param name="member">The member the attribute is applied to.</param>
	/// <returns>The display name to use for the pin.</returns>
	private static string GetPinDisplayName(PinAttribute pinAttr, MemberInfo member)
	{
		// If explicitly set, use the provided display name
		if (!string.IsNullOrEmpty(pinAttr.DisplayName))
		{
			return pinAttr.DisplayName;
		}

		// Default to member name
		return member.Name;
	}

	private static bool GetAllowMultipleConnections(PinAttribute pinAttr)
	{
		return pinAttr switch
		{
			InputPinAttribute input => input.AllowMultipleConnections,
			OutputPinAttribute output => output.AllowMultipleConnections,
			ExecutionInputAttribute execInput => execInput.AllowMultipleConnections,
			ExecutionOutputAttribute execOutput => execOutput.AllowMultipleConnections,
			_ => false
		};
	}
}
