// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace NodeGraphTests;
using System.Reflection;
using ktsu.NodeGraph;

[TestClass]
public class PinValidationTests
{
	[TestMethod]
	public void InputPin_OnPropertyWithoutNodeParent_ThrowsInvalidOperationException()
	{
		PropertyInfo? property = typeof(InvalidClassWithoutNode).GetProperty(nameof(InvalidClassWithoutNode.InvalidProperty));
		InputPinAttribute? attribute = property?.GetCustomAttribute<InputPinAttribute>();

		Assert.IsNotNull(attribute);

		InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(() => attribute.InitializeTypeInfo(property));
		StringAssert.Contains(exception.Message, "does not have a [Node] attribute");
	}

	[TestMethod]
	public void InputPin_OnMethod_IsPreventedAtCompileTime()
	{
		// This test verifies that the AttributeUsage prevents InputPin from being applied to methods
		// The AttributeUsage is: [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
		// So methods are not allowed at compile time

		AttributeUsageAttribute? inputPinUsage = typeof(InputPinAttribute).GetCustomAttribute<AttributeUsageAttribute>();
		Assert.IsNotNull(inputPinUsage);
		Assert.IsFalse(inputPinUsage.ValidOn.HasFlag(AttributeTargets.Method));
	}

	[TestMethod]
	public void InputPin_OnReadonlyField_ThrowsInvalidOperationException()
	{
		FieldInfo? field = typeof(ValidNodeClass).GetField(nameof(ValidNodeClass.ReadonlyField));
		InputPinAttribute? attribute = field?.GetCustomAttribute<InputPinAttribute>();

		Assert.IsNotNull(attribute);

		InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(() => attribute.InitializeTypeInfo(field));
		StringAssert.Contains(exception.Message, "Input pins cannot be applied to readonly fields");
	}

	[TestMethod]
	public void InputPin_OnReadonlyProperty_ThrowsInvalidOperationException()
	{
		PropertyInfo? property = typeof(ValidNodeClass).GetProperty(nameof(ValidNodeClass.ReadonlyProperty));
		InputPinAttribute? attribute = property?.GetCustomAttribute<InputPinAttribute>();

		Assert.IsNotNull(attribute);

		InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(() => attribute.InitializeTypeInfo(property));
		StringAssert.Contains(exception.Message, "Input pins cannot be applied to readonly properties");
	}

	[TestMethod]
	public void OutputPin_OnMethodWithParameters_ThrowsInvalidOperationException()
	{
		MethodInfo? method = typeof(ValidNodeClass).GetMethod(nameof(ValidNodeClass.InvalidMethodWithOutputPin));
		OutputPinAttribute? attribute = method?.GetCustomAttribute<OutputPinAttribute>();

		Assert.IsNotNull(attribute);

		InvalidOperationException exception = Assert.ThrowsException<InvalidOperationException>(() => attribute.InitializeTypeInfo(method));
		StringAssert.Contains(exception.Message, "Output pins cannot be applied to methods with parameters");
	}

	[TestMethod]
	public void InputPin_OnValidProperty_Succeeds()
	{
		PropertyInfo? property = typeof(ValidNodeClass).GetProperty(nameof(ValidNodeClass.ValidInputProperty));
		InputPinAttribute? attribute = property?.GetCustomAttribute<InputPinAttribute>();

		Assert.IsNotNull(attribute);

		// Should not throw
		attribute.InitializeTypeInfo(property);

		Assert.AreEqual(typeof(int), attribute.DataType);
		Assert.AreEqual("int", attribute.DisplayTypeId);
	}

	[TestMethod]
	public void OutputPin_OnValidMethod_Succeeds()
	{
		MethodInfo? method = typeof(ValidNodeClass).GetMethod(nameof(ValidNodeClass.ValidMethodWithOutputPin));
		OutputPinAttribute? attribute = method?.GetCustomAttribute<OutputPinAttribute>();

		Assert.IsNotNull(attribute);

		// Should not throw
		attribute.InitializeTypeInfo(method);

		Assert.AreEqual(typeof(double), attribute.DataType);
		Assert.AreEqual("double", attribute.DisplayTypeId);
	}

	[TestMethod]
	public void InputPin_OnMethodParameter_Succeeds()
	{
		MethodInfo? method = typeof(ValidNodeClass).GetMethod(nameof(ValidNodeClass.ValidMethodWithParameters));
		ParameterInfo? parameter = method?.GetParameters().FirstOrDefault();
		InputPinAttribute? attribute = parameter?.GetCustomAttribute<InputPinAttribute>();

		Assert.IsNotNull(attribute);

		// Should not throw
		attribute.InitializeTypeInfo(parameter);

		Assert.AreEqual(typeof(string), attribute.DataType);
		Assert.AreEqual("string", attribute.DisplayTypeId);
	}
}

// Test class without [Node] attribute - should cause validation errors
public class InvalidClassWithoutNode
{
	[InputPin]
	public int InvalidProperty { get; set; }
}

// Valid test class with [Node] attribute
[Node("Valid Test Node")]
[System.ComponentModel.Description("A valid node class for testing validation")]
public class ValidNodeClass
{
	[InputPin]
	public int ValidInputProperty { get; set; }

	[OutputPin]
	public string ValidOutputProperty { get; set; } = "";

	[InputPin]
	public readonly int ReadonlyField = 42;

	[InputPin]
	public int ReadonlyProperty { get; }

	// Methods cannot have InputPin at compile time due to AttributeUsage restrictions

	[OutputPin]
	public static double InvalidMethodWithOutputPin(int parameter) => parameter * 2.0;

	[OutputPin]
	public static double ValidMethodWithOutputPin() => 42.0;

	public static string ValidMethodWithParameters([InputPin] string input) => input.ToUpper();
}
