// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace NodeGraphTests;
using System.Reflection;
using ktsu.NodeGraph;

[TestClass]
public class NodeBehaviorTests
{
	[TestMethod]
	public void NodeBehaviorAttribute_DefaultConstructor_SetsCorrectValues()
	{
		NodeBehaviorAttribute attribute = new();

		Assert.AreEqual(NodeExecutionMode.OnInputChange, attribute.ExecutionMode);
		Assert.IsFalse(attribute.SupportsAsyncExecution);
		Assert.IsFalse(attribute.HasSideEffects);
		Assert.IsTrue(attribute.IsDeterministic);
		Assert.IsFalse(attribute.IsCacheable);
	}

	[TestMethod]
	public void NodeBehaviorAttribute_WithExecutionMode_SetsCorrectValues()
	{
		NodeBehaviorAttribute attribute = new(NodeExecutionMode.Continuous);

		Assert.AreEqual(NodeExecutionMode.Continuous, attribute.ExecutionMode);
	}

	[TestMethod]
	public void NodeBehaviorAttribute_AllProperties_CanBeSet()
	{
		NodeBehaviorAttribute attribute = new()
		{
			ExecutionMode = NodeExecutionMode.OnExecution,
			SupportsAsyncExecution = true,
			HasSideEffects = true,
			IsDeterministic = false,
			IsCacheable = true
		};

		Assert.AreEqual(NodeExecutionMode.OnExecution, attribute.ExecutionMode);
		Assert.IsTrue(attribute.SupportsAsyncExecution);
		Assert.IsTrue(attribute.HasSideEffects);
		Assert.IsFalse(attribute.IsDeterministic);
		Assert.IsTrue(attribute.IsCacheable);
	}

	[TestMethod]
	public void NodeBehaviorAttribute_CanBeAppliedToClass()
	{
		Type type = typeof(TestBehaviorNode);
		NodeBehaviorAttribute? attribute = type.GetCustomAttribute<NodeBehaviorAttribute>();

		Assert.IsNotNull(attribute);
		Assert.AreEqual(NodeExecutionMode.OnStart, attribute.ExecutionMode);
	}

	[TestMethod]
	public void NodeExecuteAttribute_DefaultConstructor_SetsCorrectValues()
	{
		NodeExecuteAttribute attribute = new();

		Assert.AreEqual(0, attribute.Order);
		Assert.IsFalse(attribute.IsAsync);
	}

	[TestMethod]
	public void NodeExecuteAttribute_Properties_CanBeSet()
	{
		NodeExecuteAttribute attribute = new()
		{
			Order = 5,
			IsAsync = true
		};

		Assert.AreEqual(5, attribute.Order);
		Assert.IsTrue(attribute.IsAsync);
	}

	[TestMethod]
	public void NodeExecuteAttribute_CanBeAppliedToMethod()
	{
		MethodInfo? method = typeof(TestBehaviorNode).GetMethod(nameof(TestBehaviorNode.ExecuteNode));
		NodeExecuteAttribute? attribute = method?.GetCustomAttribute<NodeExecuteAttribute>();

		Assert.IsNotNull(attribute);
		Assert.AreEqual(0, attribute.Order); // Default value
		Assert.IsFalse(attribute.IsAsync); // Default value
	}

	[TestMethod]
	public void NodeValidateAttribute_DefaultConstructor_SetsCorrectValues()
	{
		NodeValidateAttribute attribute = new();

		Assert.IsNull(attribute.Phase);
	}

	[TestMethod]
	public void NodeValidateAttribute_Phase_CanBeSet()
	{
		NodeValidateAttribute attribute = new()
		{
			Phase = "PreExecution"
		};

		Assert.AreEqual("PreExecution", attribute.Phase);
	}

	[TestMethod]
	public void NodeValidateAttribute_CanBeAppliedToMethod()
	{
		MethodInfo? method = typeof(TestBehaviorNode).GetMethod(nameof(TestBehaviorNode.ValidateInputs));
		NodeValidateAttribute? attribute = method?.GetCustomAttribute<NodeValidateAttribute>();

		Assert.IsNotNull(attribute);
		Assert.IsNull(attribute.Phase); // Default value
	}

	[TestMethod]
	public void NodeDeprecatedAttribute_DefaultConstructor_SetsCorrectValues()
	{
		NodeDeprecatedAttribute attribute = new();

		Assert.IsNull(attribute.Reason);
		Assert.IsNull(attribute.ReplacementType);
		Assert.IsNull(attribute.DeprecatedInVersion);
		Assert.IsNull(attribute.RemovalVersion);
	}

	[TestMethod]
	public void NodeDeprecatedAttribute_WithReason_SetsCorrectValues()
	{
		string reason = "This node is obsolete";
		NodeDeprecatedAttribute attribute = new(reason);

		Assert.AreEqual(reason, attribute.Reason);
	}

	[TestMethod]
	public void NodeDeprecatedAttribute_AllProperties_CanBeSet()
	{
		NodeDeprecatedAttribute attribute = new()
		{
			Reason = "Use NewNode instead",
			ReplacementType = typeof(TestBehaviorNode),
			DeprecatedInVersion = "2.0",
			RemovalVersion = "3.0"
		};

		Assert.AreEqual("Use NewNode instead", attribute.Reason);
		Assert.AreEqual(typeof(TestBehaviorNode), attribute.ReplacementType);
		Assert.AreEqual("2.0", attribute.DeprecatedInVersion);
		Assert.AreEqual("3.0", attribute.RemovalVersion);
	}

	[TestMethod]
	public void NodeDeprecatedAttribute_CanBeAppliedToClass()
	{
		Type type = typeof(DeprecatedTestNode);
		NodeDeprecatedAttribute? attribute = type.GetCustomAttribute<NodeDeprecatedAttribute>();

		Assert.IsNotNull(attribute);
		Assert.AreEqual("Use TestBehaviorNode instead", attribute.Reason);
		Assert.IsNull(attribute.ReplacementType); // Default value since we can't set it in attribute constructor
	}

	[TestMethod]
	public void NodeExecutionMode_AllValues_AreValid()
	{
		NodeExecutionMode[] modes = Enum.GetValues<NodeExecutionMode>();

		Assert.IsTrue(modes.Contains(NodeExecutionMode.OnInputChange));
		Assert.IsTrue(modes.Contains(NodeExecutionMode.OnExecution));
		Assert.IsTrue(modes.Contains(NodeExecutionMode.OnStart));
		Assert.IsTrue(modes.Contains(NodeExecutionMode.Continuous));
		Assert.IsTrue(modes.Contains(NodeExecutionMode.Manual));
	}
}

[Node("Test Behavior Node")]
[NodeBehavior(NodeExecutionMode.OnStart)]
[System.ComponentModel.Description("A test node for behavior testing")]
public class TestBehaviorNode
{
	[NodeExecute]
	public void ExecuteNode()
	{
	}

	[NodeValidate]
	public bool ValidateInputs() => true;
}

// Set properties in constructor since attributes are read-only at runtime

[Node("Deprecated Node")]
[NodeDeprecated("Use TestBehaviorNode instead")]
[System.ComponentModel.Description("A deprecated test node")]
public class DeprecatedTestNode
{
}
