// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.NodeGraph.Tests;

using System.Reflection;
using ktsu.NodeGraph;

[TestClass]
public class NodeAttributeTests
{
	[TestMethod]
	public void NodeAttribute_DefaultConstructor_SetsCorrectValues()
	{
		NodeAttribute attribute = new();

		Assert.IsNull(attribute.DisplayName);
	}

	[TestMethod]
	public void NodeAttribute_WithDisplayName_SetsCorrectValues()
	{
		string displayName = "Test Node";
		NodeAttribute attribute = new(displayName);

		Assert.AreEqual(displayName, attribute.DisplayName);
	}

	[TestMethod]
	public void NodeAttribute_CanBeAppliedToClass()
	{
		Type type = typeof(TestNodeClass);
		NodeAttribute? attribute = type.GetCustomAttribute<NodeAttribute>();

		Assert.IsNotNull(attribute);
		Assert.AreEqual("Test Class Node", attribute.DisplayName);
	}

	[TestMethod]
	public void NodeAttribute_CanBeAppliedToStruct()
	{
		Type type = typeof(TestNodeStruct);
		NodeAttribute? attribute = type.GetCustomAttribute<NodeAttribute>();

		Assert.IsNotNull(attribute);
		Assert.AreEqual("Test Struct Node", attribute.DisplayName);
	}

	[TestMethod]
	public void NodeAttribute_CanBeAppliedToMethod()
	{
		MethodInfo? method = typeof(TestNodeClass).GetMethod(nameof(TestNodeClass.TestMethod));
		NodeAttribute? attribute = method?.GetCustomAttribute<NodeAttribute>();

		Assert.IsNotNull(attribute);
		Assert.AreEqual("Test Method Node", attribute.DisplayName);
	}

	[TestMethod]
	public void NodeAttribute_WithDescriptionAttribute_CanBeReadTogether()
	{
		Type type = typeof(TestNodeClass);
		NodeAttribute? nodeAttribute = type.GetCustomAttribute<NodeAttribute>();
		System.ComponentModel.DescriptionAttribute? descriptionAttribute = type.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();

		Assert.IsNotNull(nodeAttribute);
		Assert.IsNotNull(descriptionAttribute);
		Assert.AreEqual("Test Class Node", nodeAttribute.DisplayName);
		Assert.AreEqual("A test class for node attributes", descriptionAttribute.Description);
	}
}

[Node("Test Class Node")]
[System.ComponentModel.Description("A test class for node attributes")]
public class TestNodeClass
{
	[InputPin]
	public int Value { get; set; }

	[OutputPin]
	public int Result { get; set; }

	[Node("Test Method Node")]
	[System.ComponentModel.Description("A test method node")]
	public static int TestMethod([InputPin] int input) => input * 2;
}

[Node("Test Struct Node")]
[System.ComponentModel.Description("A test struct for node attributes")]
public struct TestNodeStruct
{
	[InputPin]
	public float X { get; set; }

	[InputPin]
	public float Y { get; set; }

	[OutputPin]
	public readonly float Magnitude => (float)Math.Sqrt((X * X) + (Y * Y));
}
