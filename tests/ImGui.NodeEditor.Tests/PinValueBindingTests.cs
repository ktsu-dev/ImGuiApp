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
/// The seam between the two places a pin's value can live: the object
/// <see cref="AttributeBasedNodeFactory"/> constructs for a node, and the engine's own
/// <see cref="PinValueStore"/>.
/// </summary>
/// <remarks>
/// The claim under test is that a parameter has exactly one home and that
/// <see cref="NodeEditorEngine.GetPinValue(int)"/> is the only question worth asking about it. A
/// bound pin's home is the instance, an unbound pin's is the store, and neither surface that edits
/// a pin — an inline editor or an inspector row — knows or needs to know which it is talking to.
/// No ImGui is involved; nodes are created through the engine.
/// </remarks>
[TestClass]
public sealed class PinValueBindingTests
{
	private readonly NodeEditorEngine engine = new();
	private AttributeBasedNodeFactory Factory => new(engine);

	/// <summary>Finds the one input pin with the given display name.</summary>
	private static Pin Input(Node node, string displayName) =>
		node.InputPins.SingleOrDefault(p => p.EffectiveDisplayName == displayName)
			?? throw new AssertFailedException($"No input pin named '{displayName}'.");

	/// <summary>Finds the one declared input pin with the given display name.</summary>
	private static PinDefinition Declared(NodeDefinition definition, string displayName) =>
		definition.InputPins.SingleOrDefault(p => p.DisplayName == displayName)
			?? throw new AssertFailedException($"No declared input pin named '{displayName}'.");

	/// <summary>
	/// The whole point of the unification, read from the editors' side: a value written through the
	/// engine is the value the instance holds, not a second copy of it.
	/// </summary>
	[TestMethod]
	public void SetPinValue_OnABoundPin_WritesTheInstance()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType<TunableNode>();
		Node node = factory.CreateNode<TunableNode>(Vector2.Zero);

		Assert.IsTrue(engine.SetPinValue(Input(node, "Threshold").Id, 200.0));

		Assert.IsTrue(factory.TryGetNodeInstance(node.Id, out object? instance));
		Assert.AreEqual(200.0, ((TunableNode)instance).Threshold, "An editor's write should have landed on the object the parameter lives on.");
	}

	/// <summary>
	/// And from the host's side: a value written onto the instance is what the editors draw, with no
	/// push or refresh in between.
	/// </summary>
	[TestMethod]
	public void GetPinValue_OnABoundPin_ReadsTheInstance()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType<TunableNode>();
		Node node = factory.CreateNode<TunableNode>(Vector2.Zero);

		Assert.IsTrue(factory.TryGetNodeInstance(node.Id, out object? instance));
		((TunableNode)instance).Threshold = 12.5;

		Assert.AreEqual(12.5, engine.GetPinValue(Input(node, "Threshold").Id), "The engine should be reporting the member, not a stale copy of its default.");
	}

	/// <summary>
	/// The round trip both ways through the two APIs a host has: the pin id the editors use, and the
	/// <see cref="PinDefinition"/> an inspector written against the instance uses.
	/// </summary>
	[TestMethod]
	public void ABoundPin_ReadsBackIdenticallyOnBothSurfaces()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType<TunableNode>();
		Node node = factory.CreateNode<TunableNode>(Vector2.Zero);
		NodeDefinition definition = factory.GetNodeDefinition(node.Id)!;
		PinDefinition threshold = Declared(definition, "Threshold");
		Assert.IsTrue(factory.TryGetNodeInstance(node.Id, out object? instance));

		engine.SetPinValue(Input(node, "Threshold").Id, 300.0);
		Assert.AreEqual(300.0, threshold.GetValue(instance), "Written through the engine, read through the definition.");

		threshold.SetValue(instance, 400.0);
		Assert.AreEqual(400.0, engine.GetPinValue(Input(node, "Threshold").Id), "Written through the definition, read through the engine.");
	}

	/// <summary>
	/// A graph built without a factory has no instance to live on, so the store is still the home and
	/// the same two calls still answer for it.
	/// </summary>
	[TestMethod]
	public void AnUnboundPin_StillReadsAndWritesThroughTheStore()
	{
		Node node = engine.CreateNodeFromSpecs(
			Vector2.Zero,
			"Blob Filter",
			[new PinSpec("Threshold", typeof(double), 128.0)],
			[]);
		int pinId = node.InputPins[0].Id;

		Assert.AreEqual(128.0, engine.GetPinValue(pinId));
		Assert.IsTrue(engine.SetPinValue(pinId, 200.0));
		Assert.AreEqual(200.0, engine.GetPinValue(pinId));
	}

	/// <summary>
	/// A method node takes its receiver over the <c>Instance</c> pin, so there is no object for its
	/// parameters to live on and the store keeps them.
	/// </summary>
	[TestMethod]
	public void AMethodNodesPins_AreLeftToTheStore()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType(typeof(MathNodes));
		MethodInfo clamp = typeof(MathNodes).GetMethod(nameof(MathNodes.Clamp))!;
		Node node = factory.CreateMethodNode(clamp, Vector2.Zero);

		Assert.IsFalse(factory.TryGetNodeInstance(node.Id, out _));

		int pinId = Input(node, "min").Id;
		Assert.AreEqual(0.0, engine.GetPinValue(pinId), "A parameter's declared default still seeds the store.");
		Assert.IsTrue(engine.SetPinValue(pinId, 5.0));
		Assert.AreEqual(5.0, engine.GetPinValue(pinId));
	}

	/// <summary>
	/// A type that cannot be constructed has no instance either, and its node must still be editable.
	/// </summary>
	[TestMethod]
	public void ANodeWithNoInstance_IsLeftToTheStore()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType<ParameterisedNode>();
		Node node = factory.CreateNode<ParameterisedNode>(Vector2.Zero);

		Assert.IsFalse(factory.TryGetNodeInstance(node.Id, out _));
		Assert.IsTrue(engine.SetPinValue(Input(node, "Factor").Id, 3.0));
		Assert.AreEqual(3.0, engine.GetPinValue(Input(node, "Factor").Id));
	}

	/// <summary>
	/// A constructor parameter's value belongs to an argument list, not to a member, so its pin is
	/// left with the store even on a node that does have an instance.
	/// </summary>
	[TestMethod]
	public void AConstructorParameterPin_IsLeftToTheStore()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType<ConstructedNode>();
		Node node = factory.CreateNode<ConstructedNode>(Vector2.Zero);

		int pinId = Input(node, "label").Id;
		Assert.AreEqual("unnamed", engine.GetPinValue(pinId));
		Assert.IsTrue(engine.SetPinValue(pinId, "renamed"));
		Assert.AreEqual("renamed", engine.GetPinValue(pinId));
	}

	/// <summary>
	/// The type check happens before the value goes anywhere, so a refused value reaches neither
	/// home. Without that, a bound pin would hand a string to a double property and throw.
	/// </summary>
	[TestMethod]
	public void SetPinValue_OnABoundPin_StillRefusesAValueThePinsTypeWillNotTake()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType<TunableNode>();
		Node node = factory.CreateNode<TunableNode>(Vector2.Zero);

		Assert.IsFalse(engine.SetPinValue(Input(node, "Threshold").Id, "not a number"));

		Assert.IsTrue(factory.TryGetNodeInstance(node.Id, out object? instance));
		Assert.AreEqual(128.0, ((TunableNode)instance).Threshold);
		Assert.AreEqual(128.0, engine.GetPinValue(Input(node, "Threshold").Id));
	}

	/// <summary>
	/// The seeded default lives in the store whether or not the pin is bound, so a reset has to reach
	/// the instance too — otherwise the store would go back and the instance the editors read would
	/// not, which is the divergence this whole design removes.
	/// </summary>
	[TestMethod]
	public void ResetPinValue_OnABoundPin_PutsTheDefaultBackOnTheInstance()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType<TunableNode>();
		Node node = factory.CreateNode<TunableNode>(Vector2.Zero);
		engine.SetPinValue(Input(node, "Threshold").Id, 200.0);

		Assert.IsTrue(engine.ResetPinValue(Input(node, "Threshold").Id));

		Assert.IsTrue(factory.TryGetNodeInstance(node.Id, out object? instance));
		Assert.AreEqual(128.0, ((TunableNode)instance).Threshold);
		Assert.AreEqual(128.0, engine.GetPinValue(Input(node, "Threshold").Id));
	}

	/// <summary>
	/// A removed node's pins stop existing, so the accessors closed over its instance have to go with
	/// them or the factory's binding is dropped while the engine still holds a delegate to the object.
	/// </summary>
	[TestMethod]
	public void RemoveNode_UnbindsItsPins()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType<TunableNode>();
		Node node = factory.CreateNode<TunableNode>(Vector2.Zero);
		int pinId = Input(node, "Threshold").Id;

		Assert.IsTrue(engine.RemoveNode(node.Id));

		Assert.IsFalse(engine.UnbindPinValue(pinId), "The pin should already have been unbound with its node.");
		Assert.IsNull(engine.GetPinValue(pinId));
	}

	[TestMethod]
	public void Clear_UnbindsEveryPin()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType<TunableNode>();
		Node node = factory.CreateNode<TunableNode>(Vector2.Zero);
		int pinId = Input(node, "Threshold").Id;

		engine.Clear();

		Assert.IsFalse(engine.UnbindPinValue(pinId));
		Assert.IsNull(engine.GetPinValue(pinId), "Clear reissues pin ids, so a stale accessor would be inherited by an unrelated pin.");
	}

	/// <summary>
	/// The reconciled coercion, seen from both homes at once. The declared default is a boxed
	/// <c>int</c> on a <see langword="double"/> member: seeding one home with <c>50.0</c> and leaving
	/// the other on its initializer is precisely the disagreement the shared conversion removes.
	/// </summary>
	[TestMethod]
	public void AMistypedDeclaredDefault_MeansTheSameThingOnTheInstanceAndInTheStore()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType<TunableNode>();
		Node node = factory.CreateNode<TunableNode>(Vector2.Zero);

		Assert.IsTrue(factory.TryGetNodeInstance(node.Id, out object? instance));
		Assert.AreEqual(50.0, ((TunableNode)instance).AreaMin, "The attribute's 50 should have been converted, not skipped for leaving the initializer's 1.0.");
		Assert.AreEqual(50.0, engine.GetPinValue(Input(node, "AreaMin").Id));
	}

	/// <summary>
	/// A declared default that cannot convert at all leaves the member's own initializer standing,
	/// which is the only value there is. It must not throw: a bad attribute still has to produce a
	/// creatable node.
	/// </summary>
	[TestMethod]
	public void ADeclaredDefaultThatCannotConvert_LeavesTheInitializerStanding()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType<NonsenseDefaultNode>();
		Node node = factory.CreateNode<NonsenseDefaultNode>(Vector2.Zero);

		Assert.AreEqual(new Vector2(3f, 4f), engine.GetPinValue(Input(node, "Offset").Id));

		// The store's own seed for this pin is null, since that is what the unusable default coerced
		// to. Binding re-seeded it from the instance, so resetting puts back the initializer rather
		// than the Vector2 a null would have collapsed to.
		engine.SetPinValue(Input(node, "Offset").Id, Vector2.One);
		Assert.IsTrue(engine.ResetPinValue(Input(node, "Offset").Id));
		Assert.AreEqual(new Vector2(3f, 4f), engine.GetPinValue(Input(node, "Offset").Id));
	}

	/// <summary>
	/// A getter is allowed to throw until it has been written — the factory already allows for that
	/// when it reads a prototype. A bound pin is read on every frame that draws its node, so a node
	/// carrying one of those must still be readable rather than throwing out of the render loop.
	/// </summary>
	[TestMethod]
	public void APinWhoseGetterThrows_ReadsAsNullRatherThanThrowing()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType<TouchyGetterNode>();
		Node node = factory.CreateNode<TouchyGetterNode>(Vector2.Zero);

		Assert.IsNull(engine.GetPinValue(Input(node, "Fragile").Id));

		Assert.IsTrue(engine.SetPinValue(Input(node, "Fragile").Id, "written"));
		Assert.AreEqual("written", engine.GetPinValue(Input(node, "Fragile").Id), "Once it has a value it reads like any other pin.");
	}

	/// <summary>
	/// A setter that refuses a value did not store it, and
	/// <see cref="NodeEditorEngine.SetPinValue(int, object?)"/> says so rather than reporting a write
	/// that never happened. A validating setter is the ordinary reason for this.
	/// </summary>
	[TestMethod]
	public void SetPinValue_WhenTheSetterRefusesTheValue_ReportsThatNothingWasWritten()
	{
		AttributeBasedNodeFactory factory = Factory;
		factory.RegisterNodeType<ValidatingSetterNode>();
		Node node = factory.CreateNode<ValidatingSetterNode>(Vector2.Zero);
		int pinId = Input(node, "Count").Id;

		Assert.IsFalse(engine.SetPinValue(pinId, -1));
		Assert.AreEqual(0, engine.GetPinValue(pinId), "The refused value must not have landed.");

		Assert.IsTrue(engine.SetPinValue(pinId, 3), "A value it does accept still goes through.");
		Assert.AreEqual(3, engine.GetPinValue(pinId));
	}

	/// <summary>
	/// A host modelling its nodes its own way can bind a pin itself, with no reflection and no
	/// factory. This is the whole of the engine's side of the contract.
	/// </summary>
	[TestMethod]
	public void BindPinValue_LetsAHostPutAPinsValueWhereverItLikes()
	{
		Node node = engine.CreateNodeFromSpecs(
			Vector2.Zero,
			"Blob Filter",
			[new PinSpec("Threshold", typeof(double), 128.0)],
			[]);
		int pinId = node.InputPins[0].Id;

		double home = 1.0;
		engine.BindPinValue(pinId, new PinValueAccessor(
			() => home,
			value =>
			{
				home = (double)value!;
				return true;
			}));

		Assert.AreEqual(1.0, engine.GetPinValue(pinId), "Binding made the accessor the home, so the store's 128 is no longer what is read.");
		Assert.IsTrue(engine.SetPinValue(pinId, 9.0));
		Assert.AreEqual(9.0, home);

		Assert.IsTrue(engine.UnbindPinValue(pinId));
		Assert.AreEqual(1.0, engine.GetPinValue(pinId), "Unbinding returns the pin to the store, holding what the accessor read when it was bound.");
	}

	[Node("Tunable")]
	public sealed class TunableNode
	{
		[InputPin("Threshold", Order = 0)]
		public double Threshold { get; set; } = 128.0;

		// A boxed int on a double member: the mistyped declared default the two models disagreed on.
		[InputPin("AreaMin", Order = 1, DefaultValue = 50)]
		public double AreaMin { get; set; } = 1.0;
	}

	[Node("Nonsense Default")]
	public sealed class NonsenseDefaultNode
	{
		[InputPin("Offset", DefaultValue = 7)]
		public Vector2 Offset { get; set; } = new(3f, 4f);
	}

	[Node("Touchy")]
	public sealed class TouchyGetterNode
	{
		[InputPin("Fragile")]
		public string? Fragile
		{
			get => field ?? throw new InvalidOperationException("Set me before reading me.");
			set;
		}
	}

	[Node("Validating Setter")]
	public sealed class ValidatingSetterNode
	{
		[InputPin("Count")]
		public int Count
		{
			get;
			set => field = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
		}
	}

	[Node("Constructed")]
	public sealed class ConstructedNode(string label = "unnamed")
	{
		public string Label { get; } = label;
	}

	[Node("Parameterised")]
	public sealed class ParameterisedNode(double scale)
	{
		[InputPin("Factor")]
		public double Factor { get; set; } = scale;
	}

	public static class MathNodes
	{
		[Node("Clamp")]
		public static double Clamp(double value, double min = 0.0, double max = 1.0) =>
			Math.Clamp(value, min, max);
	}
}
