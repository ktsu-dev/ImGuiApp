// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor.Tests;

using System.Reflection;

using ktsu.ImGui.NodeEditor;
using ktsu.NodeGraph;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers what a <see cref="PinDefinition"/> answers about a prospective connection, and how it
/// reads and writes the member behind it. Pure metadata — no engine, no ImGui.
/// </summary>
[TestClass]
public sealed class PinDefinitionTests
{
	private static PinDefinition Input(System.Type dataType, MemberInfo? member = null) => new()
	{
		Member = member ?? ValueProperty,
		DisplayName = "In",
		DataType = dataType,
		IsInput = true,
	};

	private static PinDefinition Output(System.Type dataType, MemberInfo? member = null) => new()
	{
		Member = member ?? ValueProperty,
		DisplayName = "Out",
		DataType = dataType,
		IsInput = false,
	};

	private static PropertyInfo ValueProperty => typeof(Sample).GetProperty(nameof(Sample.Value))!;
	private static PropertyInfo ReadOnlyProperty => typeof(Sample).GetProperty(nameof(Sample.Computed))!;
	private static FieldInfo LabelField => typeof(Sample).GetField(nameof(Sample.Label))!;

	[TestMethod]
	public void CanConnectTo_RefusesTwoPinsOfTheSameDirection()
	{
		Assert.IsFalse(Output(typeof(int)).CanConnectTo(Output(typeof(int))));
		Assert.IsFalse(Input(typeof(int)).CanConnectTo(Input(typeof(int))));
	}

	[TestMethod]
	public void CanConnectTo_AcceptsMatchingTypesInEitherDirection()
	{
		Assert.IsTrue(Output(typeof(int)).CanConnectTo(Input(typeof(int))));
		Assert.IsTrue(Input(typeof(int)).CanConnectTo(Output(typeof(int))));
	}

	[TestMethod]
	public void CanConnectTo_FollowsTheSameRulesAsPinTypeUtilities()
	{
		// The pin only knows its own direction; which side is the source is decided from that, and
		// the answer has to agree with the rule table the metadata package publishes.
		Assert.AreEqual(
			PinTypeUtilities.CanConnect(typeof(int), typeof(double)),
			Output(typeof(int)).CanConnectTo(Input(typeof(double))));

		Assert.AreEqual(
			PinTypeUtilities.CanConnect(typeof(string), typeof(int)),
			Output(typeof(string)).CanConnectTo(Input(typeof(int))));
	}

	[TestMethod]
	public void RequiresConversionTo_IsFalseForAnImpossibleOrIdenticalConnection()
	{
		Assert.IsFalse(Output(typeof(int)).RequiresConversionTo(Output(typeof(int))), "Same direction cannot connect at all.");
		Assert.IsFalse(Output(typeof(int)).RequiresConversionTo(Input(typeof(int))), "Identical types need no conversion.");
	}

	[TestMethod]
	public void RequiresConversionTo_ReportsAWideningConnection()
	{
		Assert.AreEqual(
			PinTypeUtilities.RequiresConversion(typeof(int), typeof(double)),
			Output(typeof(int)).RequiresConversionTo(Input(typeof(double))));
	}

	[TestMethod]
	public void IsLossyConversionTo_MatchesTheRuleTableAndIsFalseWithoutAConversion()
	{
		Assert.AreEqual(
			PinTypeUtilities.IsLossyConversion(typeof(double), typeof(int)),
			Output(typeof(double)).IsLossyConversionTo(Input(typeof(int))));

		Assert.IsFalse(Output(typeof(int)).IsLossyConversionTo(Input(typeof(int))));
	}

	[TestMethod]
	public void GetValueAndSetValue_WorkThroughAProperty()
	{
		Sample sample = new() { Value = 3 };
		PinDefinition pin = Input(typeof(int), ValueProperty);

		Assert.AreEqual(3, pin.GetValue(sample));

		pin.SetValue(sample, 42);
		Assert.AreEqual(42, sample.Value);
	}

	[TestMethod]
	public void GetValueAndSetValue_WorkThroughAField()
	{
		Sample sample = new() { Label = "before" };
		PinDefinition pin = Input(typeof(string), LabelField);

		Assert.AreEqual("before", pin.GetValue(sample));

		pin.SetValue(sample, "after");
		Assert.AreEqual("after", sample.Label);
	}

	[TestMethod]
	public void SetValue_LeavesAReadOnlyPropertyAlone()
	{
		Sample sample = new() { Value = 5 };
		PinDefinition pin = Output(typeof(int), ReadOnlyProperty);

		Assert.AreEqual(10, pin.GetValue(sample));

		pin.SetValue(sample, 999);
		Assert.AreEqual(10, pin.GetValue(sample), "A computed pin has nothing to write back to.");
	}

	[TestMethod]
	public void GetValue_ReturnsNullWhenTheMemberIsNotAPropertyOrField()
	{
		// Method and parameter pins carry their member for identity, not for reading a value.
		PinDefinition pin = Input(typeof(int), typeof(Sample).GetMethod(nameof(Sample.Method))!);

		Assert.IsNull(pin.GetValue(new Sample()));
		pin.SetValue(new Sample(), 1);
	}

	[TestMethod]
	public void Defaults_AreTheUnconnectedUntypedOnes()
	{
		PinDefinition pin = new();

		Assert.AreEqual(typeof(object), pin.DataType);
		Assert.IsTrue(pin.IsRequired);
		Assert.IsFalse(pin.AllowMultipleConnections);
		Assert.IsNull(pin.DefaultValue);
		Assert.AreEqual(0, pin.Order);
	}

	[TestMethod]
	public void NodeDefinition_StartsEmptyAndUncategorized()
	{
		NodeDefinition definition = new();

		Assert.AreEqual("Custom", definition.Category);
		Assert.AreEqual(NodeExecutionMode.OnInputChange, definition.ExecutionMode);
		Assert.IsTrue(definition.IsDeterministic);
		Assert.IsTrue(definition.VisibleInMenu);
		Assert.IsTrue(definition.CanBeInstantiated);
		Assert.IsFalse(definition.IsDeprecated);
		Assert.IsEmpty(definition.InputPins);
		Assert.IsEmpty(definition.OutputPins);
		Assert.IsEmpty(definition.Tags);
	}

	public sealed class Sample
	{
		public int Value { get; set; }

		public int Computed => Value * 2;

		public string Label = string.Empty;

		public int Method() => Value;
	}
}
