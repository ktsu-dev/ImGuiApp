// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace NodeGraphTests;
using System.Reflection;
using ktsu.NodeGraph;

[TestClass]
public class PinAttributeTests
{
	[TestMethod]
	public void InputPinAttribute_DefaultConstructor_SetsCorrectValues()
	{
		InputPinAttribute attribute = new();

		Assert.AreEqual(PinType.Data, attribute.PinType);
		Assert.IsNull(attribute.DisplayName);
	}

	[TestMethod]
	public void InputPinAttribute_WithDisplayName_SetsCorrectValues()
	{
		string displayName = "Test Input";
		InputPinAttribute attribute = new(displayName);

		Assert.AreEqual(PinType.Data, attribute.PinType);
		Assert.AreEqual(displayName, attribute.DisplayName);
	}

	[TestMethod]
	public void OutputPinAttribute_DefaultConstructor_SetsCorrectValues()
	{
		OutputPinAttribute attribute = new();

		Assert.AreEqual(PinType.Data, attribute.PinType);
		Assert.IsNull(attribute.DisplayName);
	}

	[TestMethod]
	public void OutputPinAttribute_WithDisplayName_SetsCorrectValues()
	{
		string displayName = "Test Output";
		OutputPinAttribute attribute = new(displayName);

		Assert.AreEqual(PinType.Data, attribute.PinType);
		Assert.AreEqual(displayName, attribute.DisplayName);
	}

	[TestMethod]
	public void ExecutionInputAttribute_DefaultConstructor_SetsCorrectValues()
	{
		ExecutionInputAttribute attribute = new();

		Assert.AreEqual(PinType.Execution, attribute.PinType);
		Assert.IsNull(attribute.DisplayName);
	}

	[TestMethod]
	public void ExecutionOutputAttribute_DefaultConstructor_SetsCorrectValues()
	{
		ExecutionOutputAttribute attribute = new();

		Assert.AreEqual(PinType.Execution, attribute.PinType);
		Assert.IsNull(attribute.DisplayName);
	}

	[TestMethod]
	public void PinAttribute_InitializeTypeInfo_ForProperty_SetsCorrectValues()
	{
		PropertyInfo? property = typeof(TestPinClass).GetProperty(nameof(TestPinClass.IntValue));
		InputPinAttribute? attribute = property?.GetCustomAttribute<InputPinAttribute>();

		Assert.IsNotNull(attribute);
		attribute.InitializeTypeInfo(property);

		Assert.AreEqual(typeof(int), attribute.DataType);
		Assert.AreEqual("int", attribute.DisplayTypeId);
	}

	[TestMethod]
	public void PinAttribute_InitializeTypeInfo_ForField_SetsCorrectValues()
	{
		FieldInfo? field = typeof(TestPinClass).GetField(nameof(TestPinClass.StringField));
		OutputPinAttribute? attribute = field?.GetCustomAttribute<OutputPinAttribute>();

		Assert.IsNotNull(attribute);
		attribute.InitializeTypeInfo(field);

		Assert.AreEqual(typeof(string), attribute.DataType);
		Assert.AreEqual("string", attribute.DisplayTypeId);
	}

	[TestMethod]
	public void PinAttribute_InitializeTypeInfo_ForMethodParameter_SetsCorrectValues()
	{
		MethodInfo? method = typeof(TestPinClass).GetMethod(nameof(TestPinClass.TestMethodWithParameters));
		ParameterInfo? parameter = method?.GetParameters().FirstOrDefault();
		InputPinAttribute? attribute = parameter?.GetCustomAttribute<InputPinAttribute>();

		Assert.IsNotNull(attribute);
		attribute.InitializeTypeInfo(parameter);

		Assert.AreEqual(typeof(double), attribute.DataType);
		Assert.AreEqual("double", attribute.DisplayTypeId);
	}

	[TestMethod]
	public void PinAttribute_InitializeTypeInfo_ForMethodReturnValue_SetsCorrectValues()
	{
		MethodInfo? method = typeof(TestPinClass).GetMethod(nameof(TestPinClass.TestMethodWithoutParameters));
		OutputPinAttribute? attribute = method?.GetCustomAttribute<OutputPinAttribute>();

		Assert.IsNotNull(attribute);
		attribute.InitializeTypeInfo(method);

		Assert.AreEqual(typeof(double), attribute.DataType);
		Assert.AreEqual("double", attribute.DisplayTypeId);
	}

	[TestMethod]
	public void PinAttribute_InitializeTypeInfo_GeneratesCorrectDisplayTypeId_ForNullableType()
	{
		PropertyInfo? property = typeof(TestPinClass).GetProperty(nameof(TestPinClass.NullableIntValue));
		InputPinAttribute? attribute = property?.GetCustomAttribute<InputPinAttribute>();

		Assert.IsNotNull(attribute);
		attribute.InitializeTypeInfo(property);

		Assert.AreEqual(typeof(int?), attribute.DataType);
		Assert.AreEqual("Nullable<int>", attribute.DisplayTypeId);
	}

	[TestMethod]
	public void PinAttribute_InitializeTypeInfo_GeneratesCorrectDisplayTypeId_ForArrayType()
	{
		FieldInfo? field = typeof(TestPinClass).GetField(nameof(TestPinClass.StringArray));
		OutputPinAttribute? attribute = field?.GetCustomAttribute<OutputPinAttribute>();

		Assert.IsNotNull(attribute);
		attribute.InitializeTypeInfo(field);

		Assert.AreEqual(typeof(string[]), attribute.DataType);
		Assert.AreEqual("string[]", attribute.DisplayTypeId);
	}

	[TestMethod]
	public void PinAttribute_InitializeTypeInfo_GeneratesCorrectDisplayTypeId_ForGenericType()
	{
		PropertyInfo? property = typeof(TestPinClass).GetProperty(nameof(TestPinClass.IntList));
		InputPinAttribute? attribute = property?.GetCustomAttribute<InputPinAttribute>();

		Assert.IsNotNull(attribute);
		attribute.InitializeTypeInfo(property);

		Assert.AreEqual(typeof(List<int>), attribute.DataType);
		Assert.AreEqual("List<int>", attribute.DisplayTypeId);
	}

	[TestMethod]
	public void PinAttribute_InitializeTypeInfo_GeneratesCorrectDisplayTypeId_ForNestedGenericType()
	{
		PropertyInfo? property = typeof(TestPinClass).GetProperty(nameof(TestPinClass.StringIntDictionary));
		OutputPinAttribute? attribute = property?.GetCustomAttribute<OutputPinAttribute>();

		Assert.IsNotNull(attribute);
		attribute.InitializeTypeInfo(property);

		Assert.AreEqual(typeof(Dictionary<string, List<int>>), attribute.DataType);
		Assert.AreEqual("Dictionary<string, List`1>", attribute.DisplayTypeId);
	}
}

[Node("Test Pin Class")]
[System.ComponentModel.Description("Test class for pin attribute tests")]
public class TestPinClass
{
	[InputPin]
	public int IntValue { get; set; }

	[InputPin]
	public int? NullableIntValue { get; set; }

	[InputPin]
	public List<int> IntList { get; set; } = [];

	[OutputPin]
	public Dictionary<string, List<int>> StringIntDictionary { get; set; } = [];

	[OutputPin]
	public string StringField = "";

	[OutputPin]
	public string[] StringArray = [];

	[OutputPin]
	[System.ComponentModel.Description("Test method without parameters")]
	public static double TestMethodWithoutParameters() => 42.0;

	[System.ComponentModel.Description("Test method with parameters")]
	public static double TestMethodWithParameters([InputPin] double input) => input * 2.0;
}
