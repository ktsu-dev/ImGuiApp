// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Reflection;
using ktsu.NodeGraph;

/// <summary>
/// Factory for creating nodes from classes decorated with node attributes.
/// This provides type-safe node creation from domain models.
/// </summary>
public class AttributeBasedNodeFactory
{
	private readonly NodeEditorEngine engine;
	private readonly Dictionary<object, NodeDefinition> nodeDefinitions = []; // Changed to object to support both Type and MethodInfo keys

	/// <summary>Everything this factory has created, keyed by the node id the engine issued.</summary>
	private readonly Dictionary<int, NodeBinding> bindings = [];

	/// <summary>
	/// Initializes a new instance of the AttributeBasedNodeFactory class.
	/// </summary>
	/// <param name="engine">The node editor engine to create nodes in.</param>
	/// <remarks>
	/// The factory subscribes to the engine for the lifetime of both. A node id only means anything
	/// while the engine still holds that node, so the bindings have to be told when one is removed
	/// or the graph is cleared; nothing else can know.
	/// </remarks>
	public AttributeBasedNodeFactory(NodeEditorEngine engine)
	{
		this.engine = engine ?? throw new ArgumentNullException(nameof(engine));
		this.engine.NodeRemoved += OnNodeRemoved;
		this.engine.Cleared += OnCleared;
	}

	private void OnNodeRemoved(object? sender, NodeRemovedEventArgs e) => bindings.Remove(e.NodeId);

	private void OnCleared(object? sender, EventArgs e) => bindings.Clear();

	/// <summary>
	/// Reattach a binding to a node that was removed and has been put back.
	/// </summary>
	/// <param name="binding">The binding the node had, as <see cref="GetBinding"/> reported it before the removal.</param>
	/// <returns>True if the engine holds a node with the binding's id and the binding was attached.</returns>
	/// <remarks>
	/// Removing a node drops its binding, because an id that names no node must not keep an instance
	/// alive. <see cref="NodeEditorHistory"/> undoes a removal by restoring the node under the same
	/// id, and this is how the instance comes back with it — the same instance, so its pin accessors,
	/// which the engine restores alongside the node, still read and write the object this binding
	/// names.
	/// </remarks>
	public bool RestoreBinding(NodeBinding binding)
	{
		Ensure.NotNull(binding);
		if (!engine.Nodes.Any(n => n.Id == binding.NodeId))
		{
			return false;
		}

		bindings[binding.NodeId] = binding;
		return true;
	}

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

		// One ordered list drives both the specs the engine creates pins from and the accessors bound
		// to those pins, so the declared pins and the created pins cannot fall out of step.
		List<PinDefinition> orderedInputs = Ordered(definition.InputPins);

		Node node = engine.CreateNodeFromSpecs(
			position,
			definition.DisplayName,
			ToSpecs(orderedInputs),
			ToSpecs(Ordered(definition.OutputPins)));

		object? instance = CreateBackingInstance(definition);
		bindings[node.Id] = new NodeBinding(node.Id, definition, instance);

		if (instance is not null)
		{
			BindPinValues(node, orderedInputs, instance);
		}

		return node;
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

		Node node = engine.CreateNodeFromSpecs(
			position,
			definition.DisplayName,
			ToSpecs(Ordered(definition.InputPins)),
			ToSpecs(Ordered(definition.OutputPins)));

		// No instance, so no accessors: a method node's parameter values stay in the engine's own
		// store, which is where a node built without a backing object keeps them.
		bindings[node.Id] = new NodeBinding(node.Id, definition, Instance: null);
		return node;
	}

	/// <summary>
	/// Constructs the object a type node's parameter values live on, and writes each input pin's
	/// declared default onto it.
	/// </summary>
	/// <param name="definition">The definition the node was created from.</param>
	/// <returns>The instance, or null if the type cannot be constructed without arguments.</returns>
	/// <remarks>
	/// A method node gets no instance. The library already models the receiver of a non-static
	/// method as an <c>Instance</c> input pin, so one arrives over a link from whichever node
	/// produced it; manufacturing a second one here would contradict that and give the node two
	/// receivers that disagree.
	/// </remarks>
	private static object? CreateBackingInstance(NodeDefinition definition)
	{
		object? instance = CreatePrototype(definition.NodeType);
		if (instance is null)
		{
			return null;
		}

		foreach (PinDefinition pin in definition.InputPins)
		{
			ApplyDeclaredDefault(pin, instance);
		}

		return instance;
	}

	/// <summary>
	/// Writes a pin's declared default onto an instance, where the pin has one that will fit.
	/// </summary>
	/// <param name="pin">The pin whose default to apply.</param>
	/// <param name="instance">The instance to write to.</param>
	/// <remarks>
	/// Only a pin backed by a property or field is written; a parameter pin's default belongs to a
	/// constructor or method argument and has nowhere to live on the instance. A default read off a
	/// prototype is written back unchanged, so the write only does visible work for a default that
	/// came from the attribute and disagrees with the member's own initializer — which is the case
	/// where leaving it out would let <see cref="PinDefinition.DefaultValue"/> and the instance
	/// report different values for the same pin.
	/// <para>
	/// A mistyped declared default — <c>[InputPin("X", DefaultValue = 50)]</c> on a
	/// <see langword="double"/> — is converted rather than ignored, because
	/// <see cref="PinValueStore.Coerce(Type?, object?)"/> is the same conversion the engine seeds its
	/// own store through. Ignoring it here would make the instance and the pin disagree on the frame
	/// the node was created. A default that will not convert at all coerces to null and the member's
	/// own initializer stands, which is the only value there is.
	/// </para>
	/// </remarks>
	private static void ApplyDeclaredDefault(PinDefinition pin, object instance)
	{
		Type? memberType = WritableMemberType(pin);
		if (memberType is null)
		{
			return;
		}

		object? declared = PinValueStore.Coerce(memberType, pin.DefaultValue);
		if (declared is null)
		{
			return;
		}

		pin.SetValue(instance, declared);
	}

	/// <summary>
	/// The type a pin's value would be written as, for a pin whose value can be written at all.
	/// </summary>
	/// <param name="pin">The pin.</param>
	/// <returns>The member's type, or null when the pin has no writable member behind it.</returns>
	/// <remarks>
	/// A pin standing for a constructor or method parameter is not writable in this sense: its
	/// default belongs to an argument list and has nowhere to live on the instance. Neither is a
	/// get-only property or an <c>init</c>/<c>readonly</c> field.
	/// </remarks>
	private static Type? WritableMemberType(PinDefinition pin) => pin.Member switch
	{
		PropertyInfo prop when prop.CanWrite => prop.PropertyType,
		FieldInfo field when !field.IsInitOnly => field.FieldType,
		_ => null
	};

	/// <summary>
	/// Point each of a node's writable input pins at the instance its value lives on.
	/// </summary>
	/// <param name="node">The node the engine created.</param>
	/// <param name="orderedInputs">The declared input pins, in the order the node's pins were created from.</param>
	/// <param name="instance">The object the node's members live on.</param>
	/// <remarks>
	/// This is what makes the instance the single home for a bound parameter: from here on
	/// <see cref="NodeEditorEngine.GetPinValue(int)"/> reads the member and
	/// <see cref="NodeEditorEngine.SetPinValue(int, object?)"/> writes it, so an inline editor, an
	/// inspector row and <see cref="PinDefinition.GetValue(object)"/> can no longer disagree.
	/// <para>
	/// Pins are paired with declarations by zipping the same ordered list the specs were built from,
	/// rather than re-deriving an order that could differ from the one the engine was handed.
	/// </para>
	/// </remarks>
	private void BindPinValues(Node node, IReadOnlyList<PinDefinition> orderedInputs, object instance)
	{
		foreach ((Pin createdPin, PinDefinition pinDef) in node.InputPins.Zip(orderedInputs))
		{
			if (WritableMemberType(pinDef) is null)
			{
				continue;
			}

			engine.BindPinValue(createdPin.Id, new PinValueAccessor(
				() => ReadMember(pinDef, instance),
				value => WriteMember(pinDef, instance, value)));
		}
	}

	/// <summary>
	/// Reads a pin's member off the instance its value lives on.
	/// </summary>
	/// <param name="pin">The pin.</param>
	/// <param name="instance">The instance.</param>
	/// <returns>What the member holds, or null if it refused to be read.</returns>
	/// <remarks>
	/// A getter is allowed to throw until it has been written, which
	/// <see cref="ReadDeclaredDefault(PinDefinition, Lazy{object})"/> already allows for when it
	/// reads a prototype. A bound pin is read on every frame that draws it, so the same allowance
	/// has to hold here or such a node could not be drawn at all.
	/// </remarks>
	private static object? ReadMember(PinDefinition pin, object instance)
	{
		try
		{
			return pin.GetValue(instance);
		}
		catch (TargetInvocationException)
		{
			return null;
		}
	}

	/// <summary>
	/// Writes a pin's member on the instance its value lives on.
	/// </summary>
	/// <param name="pin">The pin.</param>
	/// <param name="instance">The instance.</param>
	/// <param name="value">The value.</param>
	/// <returns>True if the member took it.</returns>
	/// <remarks>
	/// False is not an error path to be swallowed: it is what
	/// <see cref="NodeEditorEngine.SetPinValue(int, object?)"/> returns to its caller, so a setter
	/// that refused the value is reported as a write that did not happen rather than as one that
	/// silently did. A validating setter is the ordinary reason for that.
	/// <para>
	/// Only the setter's own refusal is caught. A value of the wrong type never reaches here:
	/// <c>PinAttribute.InitializeTypeInfo</c> takes a data pin's <c>DataType</c> straight off the
	/// member, and <see cref="NodeEditorEngine.SetPinValue(int, object?)"/> checks against that
	/// before writing anything.
	/// </para>
	/// </remarks>
	private static bool WriteMember(PinDefinition pin, object instance, object? value)
	{
		try
		{
			pin.SetValue(instance, value);
			return true;
		}
		catch (TargetInvocationException)
		{
			return false;
		}
	}

	/// <summary>
	/// Turn declared pins into what the engine creates pins from.
	/// </summary>
	/// <param name="pins">The declared pins, already in the order they should be created in.</param>
	/// <returns>One spec per pin, in the order given.</returns>
	/// <remarks>
	/// This replaced a pass that created pins from names and then set each one's connection capacity
	/// by index, matching declared pins to created pins by the order they happened to be created in.
	/// Carrying everything in one value means there is no second list to fall out of step with.
	/// <para>
	/// The ordering is done once by <see cref="Ordered(List{PinDefinition})"/> and the result is what
	/// both this and <see cref="BindPinValues(Node, IReadOnlyList{PinDefinition}, object)"/> read, so
	/// the specs and the accessors are built from one list rather than two that agree today.
	/// </para>
	/// </remarks>
	private static List<PinSpec> ToSpecs(IReadOnlyList<PinDefinition> pins) =>
	[
		.. pins.Select(p => new PinSpec(
			p.DisplayName,
			p.DataType,
			p.DefaultValue,
			p.AllowMultipleConnections)),
	];

	/// <summary>
	/// The declared pins in the order the engine should create them in.
	/// </summary>
	/// <param name="pins">The declared pins, in any order.</param>
	/// <returns>The same pins, ordered as the declaration asked.</returns>
	private static List<PinDefinition> Ordered(List<PinDefinition> pins) => [.. pins.OrderBy(p => p.Order)];

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
	/// Gets the definition a node was created from.
	/// </summary>
	/// <param name="nodeId">The node's identifier, as the engine issued it.</param>
	/// <returns>The definition, or null if this factory did not create that node or it has since been removed.</returns>
	/// <remarks>
	/// This is the lookup that answers "which type is this selected node", which the pin names on
	/// the <see cref="Node"/> alone cannot.
	/// </remarks>
	public NodeDefinition? GetNodeDefinition(int nodeId) => GetBinding(nodeId)?.Definition;

	/// <summary>
	/// Gets what a node created by this factory was created from.
	/// </summary>
	/// <param name="nodeId">The node's identifier, as the engine issued it.</param>
	/// <returns>The binding, or null if this factory did not create that node or it has since been removed.</returns>
	public NodeBinding? GetBinding(int nodeId) => bindings.TryGetValue(nodeId, out NodeBinding? binding) ? binding : null;

	/// <summary>
	/// Gets the object a node's parameter values live on.
	/// </summary>
	/// <param name="nodeId">The node's identifier, as the engine issued it.</param>
	/// <param name="instance">The instance, or null if the node has none.</param>
	/// <returns>True if this factory created that node and it has a backing instance.</returns>
	/// <remarks>
	/// Read and write its members through the owning <see cref="PinDefinition"/>'s
	/// <see cref="PinDefinition.GetValue(object)"/> and
	/// <see cref="PinDefinition.SetValue(object, object?)"/>, which is what an inspector panel
	/// editing a declared parameter such as a threshold needs.
	/// </remarks>
	public bool TryGetNodeInstance(int nodeId, [NotNullWhen(true)] out object? instance)
	{
		instance = GetBinding(nodeId)?.Instance;
		return instance is not null;
	}

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

		// A prototype answers what an initializer wrote, for the pins whose attribute said nothing.
		// Built at most once per type, and only if such a pin turns up.
		Lazy<object?> prototype = new(() => CreatePrototype(nodeType));

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

				// Set default value for input pins - prefer the attribute, then the member's own
				// initializer read off a prototype instance, matching what parameter pins already do.
				if (isInput && pinAttr is InputPinAttribute inputPin)
				{
					pinDef.DefaultValue = inputPin.DefaultValue ?? ReadDeclaredDefault(pinDef, prototype);
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

	/// <summary>
	/// Reads what a pin's member holds on a freshly constructed instance of its declaring type,
	/// which is the value its C# initializer wrote.
	/// </summary>
	/// <param name="pin">The pin whose member to read.</param>
	/// <param name="prototype">The prototype instance, or null if the type could not be constructed.</param>
	/// <returns>The declared default, or null if there is no prototype or the member cannot be read.</returns>
	private static object? ReadDeclaredDefault(PinDefinition pin, Lazy<object?> prototype)
	{
		object? instance = prototype.Value;
		if (instance is null)
		{
			return null;
		}

		try
		{
			return pin.GetValue(instance);
		}
		catch (TargetInvocationException)
		{
			// A getter that throws on a default-constructed instance has no default to report.
			return null;
		}
	}

	/// <summary>
	/// Constructs an instance of a node type purely to read its initializers, returning null when
	/// the type cannot be constructed without arguments or its constructor refuses to run.
	/// </summary>
	/// <param name="nodeType">The type to construct.</param>
	/// <returns>The instance, or null.</returns>
	/// <remarks>
	/// The guard is what makes the one catch enough: an abstract or open-generic type, and a
	/// reference type without a public parameterless constructor, are all turned away before
	/// <see cref="Activator.CreateInstance(Type)"/> is reached, which is where its
	/// <c>MissingMethodException</c> and <c>MemberAccessException</c> would have come from. What is
	/// left is a constructor that runs and throws.
	/// </remarks>
	private static object? CreatePrototype(Type nodeType)
	{
		bool constructible = !nodeType.IsAbstract
			&& !nodeType.ContainsGenericParameters
			&& (nodeType.IsValueType || nodeType.GetConstructor(Type.EmptyTypes) is not null);

		if (!constructible)
		{
			return null;
		}

		try
		{
			return Activator.CreateInstance(nodeType);
		}
		catch (TargetInvocationException)
		{
			// The constructor threw. Registration is metadata only, so this is not fatal here.
			return null;
		}
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
