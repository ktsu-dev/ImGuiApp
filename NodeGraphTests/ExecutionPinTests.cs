// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace NodeGraphTests;
using System.Reflection;
using ktsu.NodeGraph;

[TestClass]
public class ExecutionPinTests
{
	[TestMethod]
	public void ExecutionInputAttribute_DefaultConstructor_SetsCorrectValues()
	{
		ExecutionInputAttribute attribute = new();

		Assert.AreEqual(PinType.Execution, attribute.PinType);
		Assert.IsFalse(attribute.AllowMultipleConnections);
		Assert.AreEqual("white", attribute.ColorHint);
		Assert.IsNull(attribute.DisplayName);
	}

	[TestMethod]
	public void ExecutionInputAttribute_WithDisplayName_SetsCorrectValues()
	{
		string displayName = "Execute Input";
		ExecutionInputAttribute attribute = new(displayName);

		Assert.AreEqual(PinType.Execution, attribute.PinType);
		Assert.IsFalse(attribute.AllowMultipleConnections);
		Assert.AreEqual("white", attribute.ColorHint);
		Assert.AreEqual(displayName, attribute.DisplayName);
	}

	[TestMethod]
	public void ExecutionOutputAttribute_DefaultConstructor_SetsCorrectValues()
	{
		ExecutionOutputAttribute attribute = new();

		Assert.AreEqual(PinType.Execution, attribute.PinType);
		Assert.IsTrue(attribute.AllowMultipleConnections);
		Assert.AreEqual("white", attribute.ColorHint);
		Assert.IsNull(attribute.DisplayName);
	}

	[TestMethod]
	public void ExecutionOutputAttribute_WithDisplayName_SetsCorrectValues()
	{
		string displayName = "Execute Output";
		ExecutionOutputAttribute attribute = new(displayName);

		Assert.AreEqual(PinType.Execution, attribute.PinType);
		Assert.IsTrue(attribute.AllowMultipleConnections);
		Assert.AreEqual("white", attribute.ColorHint);
		Assert.AreEqual(displayName, attribute.DisplayName);
	}

	[TestMethod]
	public void ExecutionInputAttribute_AllowMultipleConnections_CanBeSet()
	{
		ExecutionInputAttribute attribute = new()
		{
			AllowMultipleConnections = true
		};

		Assert.IsTrue(attribute.AllowMultipleConnections);
	}

	[TestMethod]
	public void ExecutionOutputAttribute_AllowMultipleConnections_CanBeSet()
	{
		ExecutionOutputAttribute attribute = new()
		{
			AllowMultipleConnections = false
		};

		Assert.IsFalse(attribute.AllowMultipleConnections);
	}

	[TestMethod]
	public void ExecutionInputAttribute_InitializeTypeInfo_SetsExecutionType()
	{
		MethodInfo? method = typeof(TestExecutionNode).GetMethod(nameof(TestExecutionNode.ExecuteMethod));
		ExecutionInputAttribute? attribute = method?.GetCustomAttribute<ExecutionInputAttribute>();

		Assert.IsNotNull(attribute);
		attribute.InitializeTypeInfo(method);

		Assert.AreEqual(typeof(void), attribute.DataType);
		Assert.AreEqual("execution", attribute.DisplayTypeId);
	}

	[TestMethod]
	public void ExecutionOutputAttribute_InitializeTypeInfo_SetsExecutionType()
	{
		MethodInfo? method = typeof(TestExecutionNode).GetMethod(nameof(TestExecutionNode.ExecuteCompleted));
		ExecutionOutputAttribute? attribute = method?.GetCustomAttribute<ExecutionOutputAttribute>();

		Assert.IsNotNull(attribute);
		attribute.InitializeTypeInfo(method);

		Assert.AreEqual(typeof(void), attribute.DataType);
		Assert.AreEqual("execution", attribute.DisplayTypeId);
	}

	[TestMethod]
	public void ExecutionInputAttribute_CanBeAppliedToProperty()
	{
		PropertyInfo? property = typeof(TestExecutionNode).GetProperty(nameof(TestExecutionNode.ExecutionTrigger));
		ExecutionInputAttribute? attribute = property?.GetCustomAttribute<ExecutionInputAttribute>();

		Assert.IsNotNull(attribute);
		Assert.AreEqual("Trigger", attribute.DisplayName);
	}

	[TestMethod]
	public void ExecutionOutputAttribute_CanBeAppliedToProperty()
	{
		PropertyInfo? property = typeof(TestExecutionNode).GetProperty(nameof(TestExecutionNode.ExecutionComplete));
		ExecutionOutputAttribute? attribute = property?.GetCustomAttribute<ExecutionOutputAttribute>();

		Assert.IsNotNull(attribute);
		Assert.AreEqual("Complete", attribute.DisplayName);
	}

	[TestMethod]
	public void ExecutionInputAttribute_CanBeAppliedToField()
	{
		FieldInfo? field = typeof(TestExecutionNode).GetField(nameof(TestExecutionNode.ExecutionField));
		ExecutionInputAttribute? attribute = field?.GetCustomAttribute<ExecutionInputAttribute>();

		Assert.IsNotNull(attribute);
	}

	[TestMethod]
	public void ExecutionInputAttribute_CanBeAppliedToParameter()
	{
		MethodInfo? method = typeof(TestExecutionNode).GetMethod(nameof(TestExecutionNode.MethodWithExecutionParameter));
		ParameterInfo? parameter = method?.GetParameters().FirstOrDefault();
		ExecutionInputAttribute? attribute = parameter?.GetCustomAttribute<ExecutionInputAttribute>();

		Assert.IsNotNull(attribute);
	}
}

[Node("Test Execution Node")]
[System.ComponentModel.Description("A test node for execution pin testing")]
public class TestExecutionNode
{
	[ExecutionInput("Trigger")]
	public bool ExecutionTrigger { get; set; }

	[ExecutionOutput("Complete")]
	public bool ExecutionComplete { get; set; }

	[ExecutionInput]
	public bool ExecutionField = false;

	[ExecutionInput]
	public void ExecuteMethod()
	{
	}

	[ExecutionOutput]
	public void ExecuteCompleted()
	{
	}

	public void MethodWithExecutionParameter([ExecutionInput] bool trigger)
	{
	}
}
