// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor.Tests;

using System;
using System.Linq;
using System.Numerics;
using System.Reflection;

using ktsu.ImGui.NodeEditor;
using ktsu.NodeGraph;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers the reflection half of the editor: what <see cref="AttributeBasedNodeFactory"/> reads off
/// a decorated type or method, and what it builds from it. No ImGui is involved — a node is created
/// through the engine, so this runs without a context.
/// </summary>
[TestClass]
public sealed class AttributeBasedNodeFactoryTests
{
	private readonly NodeEditorEngine engine = new();
	private AttributeBasedNodeFactory Factory => new(engine);

	[TestMethod]
	public void RegisterNodeType_ReadsTheDisplayNameColorAndTags()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType<AddNumbersNode>();

		NodeDefinition? definition = factory.GetNodeDefinition(typeof(AddNumbersNode));

		Assert.IsNotNull(definition);
		Assert.AreEqual("Add Numbers", definition.DisplayName);
		Assert.AreEqual("#4488ff", definition.ColorHint);
		Assert.Contains("math", definition.Tags);
		Assert.AreEqual("Adds two numbers together.", definition.Description);
		Assert.AreEqual(typeof(AddNumbersNode), definition.NodeType);
		Assert.IsNull(definition.Method, "A type node is not a method node.");
	}

	[TestMethod]
	public void RegisterNodeType_FallsBackToTheTypeNameWithoutItsNodeSuffix()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType<UnnamedThingNode>();

		Assert.AreEqual("UnnamedThing", factory.GetNodeDefinition(typeof(UnnamedThingNode))!.DisplayName);
	}

	[TestMethod]
	public void RegisterNodeType_CarriesBehaviorMetadata()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType<AddNumbersNode>();

		NodeDefinition definition = factory.GetNodeDefinition(typeof(AddNumbersNode))!;

		Assert.AreEqual(NodeExecutionMode.OnExecution, definition.ExecutionMode);
		Assert.IsTrue(definition.HasSideEffects);
		Assert.IsTrue(definition.SupportsAsyncExecution);
		Assert.IsTrue(definition.IsCacheable);
		Assert.IsFalse(definition.IsDeterministic);
	}

	[TestMethod]
	public void RegisterNodeType_CarriesVisibilityAndDeprecationMetadata()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType<RetiredNode>();

		NodeDefinition definition = factory.GetNodeDefinition(typeof(RetiredNode))!;

		Assert.IsTrue(definition.IsDeprecated);
		Assert.AreEqual("Superseded by Add Numbers", definition.DeprecationReason);
		Assert.AreEqual(typeof(AddNumbersNode), definition.ReplacementType);
		Assert.AreEqual("1.2", definition.DeprecatedInVersion);
		Assert.AreEqual("2.0", definition.RemovalVersion);
		Assert.IsFalse(definition.VisibleInMenu);
		Assert.IsTrue(definition.IsExperimental);
		Assert.Contains("preview", definition.RequiredFeatures);
	}

	[TestMethod]
	public void RegisterNodeType_ReadsPinsInDeclaredOrderAndKeepsDefaults()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType<AddNumbersNode>();

		NodeDefinition definition = factory.GetNodeDefinition(typeof(AddNumbersNode))!;

		// B declares Order 0 and A declares Order 1, so B sorts first regardless of declaration order.
		Assert.AreEqual("B", definition.InputPins[0].DisplayName);
		Assert.AreEqual("A", definition.InputPins[1].DisplayName);
		Assert.AreEqual(2.0, definition.InputPins[0].DefaultValue);
		Assert.IsTrue(definition.InputPins.All(p => p.IsInput));
		Assert.AreEqual(typeof(double), definition.InputPins[0].DataType);
		Assert.Contains("Sum", definition.OutputPins.Select(p => p.DisplayName).ToList());
	}

	[TestMethod]
	public void RegisterNodeType_AddsAnInstanceOutputPinForAClass()
	{
		// Every class node can be chained onward, so the factory gives it an instance output even
		// when the type declares none of its own.
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType<AddNumbersNode>();

		PinDefinition instance = factory.GetNodeDefinition(typeof(AddNumbersNode))!
			.OutputPins.Single(p => p.DisplayName == "Instance");

		Assert.AreEqual(typeof(AddNumbersNode), instance.DataType);
		Assert.IsFalse(instance.IsInput);
		Assert.IsTrue(instance.AllowMultipleConnections);
		Assert.IsFalse(instance.IsRequired);
	}

	[TestMethod]
	public void RegisterNodeType_TurnsConstructorParametersIntoInputPins()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType<ConstructedNode>();

		NodeDefinition definition = factory.GetNodeDefinition(typeof(ConstructedNode))!;
		PinDefinition seed = definition.InputPins.Single(p => p.DisplayName == "seed");
		PinDefinition label = definition.InputPins.Single(p => p.DisplayName == "label");

		Assert.IsTrue(seed.IsRequired, "A parameter with no default has to be supplied.");
		Assert.IsFalse(label.IsRequired, "A parameter with a default does not.");
		Assert.AreEqual("unnamed", label.DefaultValue);
		Assert.AreEqual(typeof(int), seed.DataType);
	}

	[TestMethod]
	public void RegisterNodeType_ReadsExecutionPins()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType<SideEffectNode>();

		NodeDefinition definition = factory.GetNodeDefinition(typeof(SideEffectNode))!;

		Assert.Contains(PinType.Execution, definition.InputPins.Select(p => p.PinType).ToList());
		Assert.Contains(PinType.Execution, definition.OutputPins.Select(p => p.PinType).ToList());
	}

	[TestMethod]
	public void RegisterNodeType_RefusesATypeWithNoNodeAttributesAnywhere()
	{
		AttributeBasedNodeFactory factory = Factory;

		InvalidOperationException error = Assert.ThrowsExactly<InvalidOperationException>(
			factory.RegisterNodeType<NotANode>);

		Assert.Contains(nameof(NotANode), error.Message);
	}

	[TestMethod]
	public void RegisterNodeType_RefusesNull() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => Factory.RegisterNodeType(null!));

	[TestMethod]
	public void RegisterNodeTypesFromAssembly_RefusesNull() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => Factory.RegisterNodeTypesFromAssembly(null!));

	[TestMethod]
	public void RegisterNodeTypesFromAssembly_FindsDecoratedTypesAndTheirMethods()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeTypesFromAssembly(typeof(AddNumbersNode).Assembly);

		Assert.IsNotNull(factory.GetNodeDefinition(typeof(AddNumbersNode)));
		Assert.IsNotNull(factory.GetNodeDefinition(typeof(RetiredNode)));
		Assert.IsNotNull(
			factory.GetNodeDefinition(typeof(Counter).GetMethod(nameof(Counter.Increment))!),
			"A decorated method on an ordinary class should be found by the scan.");
		Assert.IsGreaterThan(2, factory.GetAllNodeDefinitions().Count());
	}

	[TestMethod]
	public void RegisterNodeTypesFromAssembly_SkipsMethodsOnStaticHolderClasses()
	{
		// A static class is abstract in IL, and the scan skips abstract types, so a node method
		// parked on one has to be registered by naming its holder. Worth pinning: the omission is
		// silent, and "register the assembly" reads like it would find everything.
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeTypesFromAssembly(typeof(AddNumbersNode).Assembly);

		Assert.IsNull(factory.GetNodeDefinition(ClampMethod));

		factory.RegisterNodeType(typeof(MathNodes));
		Assert.IsNotNull(factory.GetNodeDefinition(ClampMethod));
	}

	[TestMethod]
	public void CreateNode_BuildsAGraphNodeWithThePinsTheDefinitionDeclares()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType<AddNumbersNode>();

		Node node = factory.CreateNode<AddNumbersNode>(new Vector2(40, 50));

		Assert.AreEqual("Add Numbers", node.Name);
		Assert.AreEqual(new Vector2(40, 50), node.Position);
		Assert.AreSequenceEqual(["B", "A"], node.InputPins.Select(p => p.EffectiveDisplayName).ToArray());
		Assert.Contains("Instance", node.OutputPins.Select(p => p.EffectiveDisplayName).ToList());
		Assert.HasCount(1, engine.Nodes, "The factory should build through the engine, not beside it.");
	}

	[TestMethod]
	public void CreateNode_RefusesAnUnregisteredType()
	{
		InvalidOperationException error = Assert.ThrowsExactly<InvalidOperationException>(
			() => Factory.CreateNode<AddNumbersNode>(Vector2.Zero));

		Assert.Contains("not registered", error.Message);
	}

	[TestMethod]
	public void RegisterNodeType_RegistersDecoratedMethodsOnTheType()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType(typeof(MathNodes));

		NodeDefinition? definition = factory.GetNodeDefinition(ClampMethod);

		Assert.IsNotNull(definition);
		Assert.AreEqual("Clamp", definition.DisplayName);
		Assert.AreEqual(ClampMethod, definition.Method);
		Assert.IsNull(factory.GetNodeDefinition(typeof(MathNodes)), "The holder type itself is not a node.");
	}

	[TestMethod]
	public void MethodNode_TurnsParametersIntoInputsAndTheReturnIntoAnOutput()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType(typeof(MathNodes));

		NodeDefinition definition = factory.GetNodeDefinition(ClampMethod)!;

		Assert.AreSequenceEqual(["value", "min", "max"], definition.InputPins.Select(p => p.DisplayName).ToArray());
		Assert.AreEqual(0.0, definition.InputPins[1].DefaultValue);
		Assert.AreEqual("Result", definition.OutputPins.Single().DisplayName);
		Assert.AreEqual(typeof(double), definition.OutputPins.Single().DataType);
	}

	[TestMethod]
	public void MethodNode_GivesAnInstanceMethodAnInstancePinFirst()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType<Counter>();

		MethodInfo increment = typeof(Counter).GetMethod(nameof(Counter.Increment))!;
		NodeDefinition definition = factory.GetNodeDefinition(increment)!;

		Assert.AreEqual("Instance", definition.InputPins[0].DisplayName);
		Assert.AreEqual(typeof(Counter), definition.InputPins[0].DataType);
		Assert.IsFalse(definition.InputPins[0].AllowMultipleConnections);
		Assert.Contains("by", definition.InputPins.Select(p => p.DisplayName).ToList());
	}

	[TestMethod]
	public void CreateMethodNode_BuildsTheNodeAndRefusesAnUnregisteredMethod()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType(typeof(MathNodes));

		Node node = factory.CreateMethodNode(ClampMethod, new Vector2(5, 6));

		Assert.AreEqual("Clamp", node.Name);
		Assert.HasCount(3, node.InputPins);
		Assert.HasCount(1, node.OutputPins);

		MethodInfo unregistered = typeof(Counter).GetMethod(nameof(Counter.Increment))!;
		Assert.ThrowsExactly<InvalidOperationException>(() => factory.CreateMethodNode(unregistered, Vector2.Zero));
	}

	[TestMethod]
	public void GetNodeDefinition_ReturnsNullForAnythingUnregistered()
	{
		AttributeBasedNodeFactory factory = Factory;

		Assert.IsNull(factory.GetNodeDefinition(typeof(AddNumbersNode)));
		Assert.IsNull(factory.GetNodeDefinition(ClampMethod));
		Assert.IsEmpty(factory.GetAllNodeDefinitions());
	}

	private static MethodInfo ClampMethod => typeof(MathNodes).GetMethod(nameof(MathNodes.Clamp))!;

	[Node("Add Numbers", ColorHint = "#4488ff", Tags = ["math", "arithmetic"])]
	[NodeBehavior(
		ExecutionMode = NodeExecutionMode.OnExecution,
		HasSideEffects = true,
		SupportsAsyncExecution = true,
		IsCacheable = true,
		IsDeterministic = false)]
	[System.ComponentModel.Description("Adds two numbers together.")]
	public sealed class AddNumbersNode
	{
		[InputPin("A", Order = 1)]
		public double A { get; set; }

		[InputPin("B", Order = 0, DefaultValue = 2.0)]
		public double B { get; set; }

		[OutputPin("Sum")]
		public double Sum { get; set; }
	}

	[Node]
	public sealed class UnnamedThingNode
	{
		[InputPin("In")]
		public int In { get; set; }
	}

	[Node("Retired")]
	[NodeDeprecated("Superseded by Add Numbers", ReplacementType = typeof(AddNumbersNode), DeprecatedInVersion = "1.2", RemovalVersion = "2.0")]
	[NodeVisibility(VisibleInMenu = false, IsExperimental = true, RequiredFeatures = ["preview"])]
	public sealed class RetiredNode
	{
		[InputPin("In")]
		public int In { get; set; }
	}

	[Node("Constructed")]
	public sealed class ConstructedNode(int seed, string label = "unnamed")
	{
		[OutputPin("Seed")]
		public int Seed { get; } = seed;

		[OutputPin("Label")]
		public string Label { get; } = label;
	}

	[Node("Side Effect")]
	public sealed class SideEffectNode
	{
		[ExecutionInput]
		public object? In { get; set; }

		[ExecutionOutput]
		public object? Out { get; set; }

		[InputPin("Message")]
		public string Message { get; set; } = string.Empty;
	}

	public sealed class NotANode
	{
		public int Value { get; set; }
	}

	public static class MathNodes
	{
		[Node("Clamp")]
		public static double Clamp(double value, double min = 0.0, double max = 1.0) =>
			Math.Clamp(value, min, max);
	}

	public sealed class Counter
	{
		private int count;

		[Node("Increment")]
		public int Increment(int by)
		{
			count += by;
			return count;
		}
	}
}
