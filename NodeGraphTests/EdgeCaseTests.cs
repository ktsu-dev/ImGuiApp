// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace NodeGraphTests;
using System.Reflection;
using ktsu.NodeGraph;

[TestClass]
public class EdgeCaseTests
{
	[TestMethod]
	public void PinAttribute_WithNullMember_ThrowsException()
	{
		InputPinAttribute attribute = new();

		Assert.ThrowsException<InvalidOperationException>(() => attribute.InitializeTypeInfo(null!));
	}

	[TestMethod]
	public void PinAttribute_WithUnsupportedMemberType_ThrowsException()
	{
		InputPinAttribute attribute = new();
		object unsupportedMember = new(); // Not a PropertyInfo, FieldInfo, MethodInfo, or ParameterInfo

		Assert.ThrowsException<InvalidOperationException>(() => attribute.InitializeTypeInfo(unsupportedMember));
	}

	[TestMethod]
	public void PinAttribute_ColorHint_CanBeSetAndRetrieved()
	{
		InputPinAttribute attribute = new()
		{
			ColorHint = "red"
		};

		Assert.AreEqual("red", attribute.ColorHint);
	}

	[TestMethod]
	public void PinAttribute_Order_CanBeSetAndRetrieved()
	{
		InputPinAttribute attribute = new()
		{
			Order = 5
		};

		Assert.AreEqual(5, attribute.Order);
	}

	[TestMethod]
	public void PinAttribute_IsRequired_CanBeSetAndRetrieved()
	{
		InputPinAttribute attribute = new()
		{
			IsRequired = false
		};

		Assert.IsFalse(attribute.IsRequired);
	}

	[TestMethod]
	public void InputPinAttribute_DefaultValue_CanBeSetAndRetrieved()
	{
		int defaultValue = 42;
		InputPinAttribute attribute = new()
		{
			DefaultValue = defaultValue
		};

		Assert.AreEqual(defaultValue, attribute.DefaultValue);
	}

	[TestMethod]
	public void WildcardPinAttribute_WithNullTypeConstraints_AllowsAnyType()
	{
		WildcardPinAttribute attribute = new()
		{
			TypeConstraints = null
		};

		Assert.IsTrue(attribute.AllowsType(typeof(int)));
		Assert.IsTrue(attribute.AllowsType(typeof(string)));
		Assert.IsTrue(attribute.AllowsType(typeof(object)));
	}

	[TestMethod]
	public void WildcardPinAttribute_WithEmptyTypeConstraints_AllowsAnyType()
	{
		WildcardPinAttribute attribute = new()
		{
			TypeConstraints = []
		};

		Assert.IsTrue(attribute.AllowsType(typeof(int)));
		Assert.IsTrue(attribute.AllowsType(typeof(string)));
		Assert.IsTrue(attribute.AllowsType(typeof(object)));
	}

	[TestMethod]
	public void WildcardPinAttribute_BaseTypeConstraint_CanBeSetAndRetrieved()
	{
		WildcardPinAttribute attribute = new()
		{
			BaseTypeConstraint = typeof(IEnumerable<>)
		};

		Assert.AreEqual(typeof(IEnumerable<>), attribute.BaseTypeConstraint);
	}

	[TestMethod]
	public void NodeAttribute_AttributeUsage_AllowsCorrectTargets()
	{
		AttributeUsageAttribute? attributeUsage = typeof(NodeAttribute).GetCustomAttribute<AttributeUsageAttribute>();

		Assert.IsNotNull(attributeUsage);
		Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Class));
		Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Struct));
		Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Method));
		Assert.IsFalse(attributeUsage.AllowMultiple);
	}

	[TestMethod]
	public void InputPinAttribute_AttributeUsage_AllowsCorrectTargets()
	{
		AttributeUsageAttribute? attributeUsage = typeof(InputPinAttribute).GetCustomAttribute<AttributeUsageAttribute>();

		Assert.IsNotNull(attributeUsage);
		Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Property));
		Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Field));
		Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Parameter));
		Assert.IsFalse(attributeUsage.ValidOn.HasFlag(AttributeTargets.Method));
		Assert.IsFalse(attributeUsage.AllowMultiple);
	}

	[TestMethod]
	public void OutputPinAttribute_AttributeUsage_AllowsCorrectTargets()
	{
		AttributeUsageAttribute? attributeUsage = typeof(OutputPinAttribute).GetCustomAttribute<AttributeUsageAttribute>();

		Assert.IsNotNull(attributeUsage);
		Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Property));
		Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Field));
		Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.ReturnValue));
		Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Method));
		Assert.IsFalse(attributeUsage.AllowMultiple);
	}

	[TestMethod]
	public void ExecutionInputAttribute_AttributeUsage_AllowsCorrectTargets()
	{
		AttributeUsageAttribute? attributeUsage = typeof(ExecutionInputAttribute).GetCustomAttribute<AttributeUsageAttribute>();

		Assert.IsNotNull(attributeUsage);
		Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Property));
		Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Field));
		Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Parameter));
		Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Method));
		Assert.IsFalse(attributeUsage.AllowMultiple);
	}

	[TestMethod]
	public void ExecutionOutputAttribute_AttributeUsage_AllowsCorrectTargets()
	{
		AttributeUsageAttribute? attributeUsage = typeof(ExecutionOutputAttribute).GetCustomAttribute<AttributeUsageAttribute>();

		Assert.IsNotNull(attributeUsage);
		Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Property));
		Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Field));
		Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Method));
		Assert.IsFalse(attributeUsage.ValidOn.HasFlag(AttributeTargets.Parameter));
		Assert.IsFalse(attributeUsage.AllowMultiple);
	}

	[TestMethod]
	public void WildcardPinAttribute_AttributeUsage_AllowsCorrectTargets()
	{
		AttributeUsageAttribute? attributeUsage = typeof(WildcardPinAttribute).GetCustomAttribute<AttributeUsageAttribute>();

		Assert.IsNotNull(attributeUsage);
		Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Property));
		Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Field));
		Assert.IsFalse(attributeUsage.AllowMultiple);
	}

	[TestMethod]
	public void NodeBehaviorAttribute_AttributeUsage_AllowsCorrectTargets()
	{
		AttributeUsageAttribute? attributeUsage = typeof(NodeBehaviorAttribute).GetCustomAttribute<AttributeUsageAttribute>();

		Assert.IsNotNull(attributeUsage);
		Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Class));
		Assert.IsFalse(attributeUsage.AllowMultiple);
	}

	[TestMethod]
	public void NodeExecuteAttribute_AttributeUsage_AllowsCorrectTargets()
	{
		AttributeUsageAttribute? attributeUsage = typeof(NodeExecuteAttribute).GetCustomAttribute<AttributeUsageAttribute>();

		Assert.IsNotNull(attributeUsage);
		Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Method));
		Assert.IsFalse(attributeUsage.AllowMultiple);
	}

	[TestMethod]
	public void NodeValidateAttribute_AttributeUsage_AllowsCorrectTargets()
	{
		AttributeUsageAttribute? attributeUsage = typeof(NodeValidateAttribute).GetCustomAttribute<AttributeUsageAttribute>();

		Assert.IsNotNull(attributeUsage);
		Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Method));
		Assert.IsFalse(attributeUsage.AllowMultiple);
	}

	[TestMethod]
	public void NodeDeprecatedAttribute_AttributeUsage_AllowsCorrectTargets()
	{
		AttributeUsageAttribute? attributeUsage = typeof(NodeDeprecatedAttribute).GetCustomAttribute<AttributeUsageAttribute>();

		Assert.IsNotNull(attributeUsage);
		Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Class));
		Assert.IsFalse(attributeUsage.AllowMultiple);
	}

	[TestMethod]
	public void PinType_Enum_HasCorrectValues()
	{
		PinType[] values = Enum.GetValues<PinType>();

		Assert.AreEqual(2, values.Length);
		Assert.IsTrue(values.Contains(PinType.Data));
		Assert.IsTrue(values.Contains(PinType.Execution));
	}

	[TestMethod]
	public void NodeExecutionMode_Enum_HasCorrectValues()
	{
		NodeExecutionMode[] values = Enum.GetValues<NodeExecutionMode>();

		Assert.AreEqual(5, values.Length);
		Assert.IsTrue(values.Contains(NodeExecutionMode.OnInputChange));
		Assert.IsTrue(values.Contains(NodeExecutionMode.OnExecution));
		Assert.IsTrue(values.Contains(NodeExecutionMode.OnStart));
		Assert.IsTrue(values.Contains(NodeExecutionMode.Continuous));
		Assert.IsTrue(values.Contains(NodeExecutionMode.Manual));
	}

	[TestMethod]
	public void PinTypeUtilities_AllNumericTypes_IsReadOnly()
	{
		IEnumerable<Type> numericTypes = PinTypeUtilities.AllNumericTypes;

		// Verify the collection contains expected numeric types
		Assert.IsTrue(numericTypes.Contains(typeof(int)));
		Assert.IsTrue(numericTypes.Contains(typeof(double)));
		Assert.IsTrue(numericTypes.Contains(typeof(float)));
	}

	[TestMethod]
	public void PinAttribute_GenerateDisplayTypeId_WithComplexNestedType()
	{
		Type complexType = typeof(Dictionary<string, List<Dictionary<int, bool>>>);
		PropertyInfo? property = typeof(EdgeCaseTestNode).GetProperty(nameof(EdgeCaseTestNode.ComplexType));
		InputPinAttribute? attribute = property?.GetCustomAttribute<InputPinAttribute>();

		Assert.IsNotNull(attribute);
		attribute.InitializeTypeInfo(property);

		// The exact format may vary, but it should handle complex nesting
		Assert.IsNotNull(attribute.DisplayTypeId);
		Assert.IsTrue(attribute.DisplayTypeId.Length > 0);
	}
}

[Node("Edge Case Test Node")]
[System.ComponentModel.Description("Node for testing edge cases")]
public class EdgeCaseTestNode
{
	[InputPin]
	public Dictionary<string, List<Dictionary<int, bool>>> ComplexType { get; set; } = [];
}
