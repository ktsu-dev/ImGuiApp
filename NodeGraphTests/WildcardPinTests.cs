// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace NodeGraphTests;
using System.Reflection;
using ktsu.NodeGraph;

[TestClass]
public class WildcardPinTests
{
	[TestMethod]
	public void WildcardPinAttribute_WithTypes_SetsCorrectValues()
	{
		WildcardPinAttribute attribute = new(typeof(int), typeof(string), typeof(double));

		CollectionAssert.AreEqual(new[] { typeof(int), typeof(string), typeof(double) }, attribute.TypeConstraints);
	}

	[TestMethod]
	public void WildcardPinAttribute_CanBeAppliedToProperty()
	{
		PropertyInfo? property = typeof(TestWildcardClass).GetProperty(nameof(TestWildcardClass.WildcardValue));
		WildcardPinAttribute? attribute = property?.GetCustomAttribute<WildcardPinAttribute>();

		Assert.IsNotNull(attribute);
		CollectionAssert.AreEqual(new[] { typeof(int), typeof(string), typeof(double) }, attribute.TypeConstraints);
	}

	[TestMethod]
	public void WildcardPinAttribute_AcceptsType_ForAllowedType_ReturnsTrue()
	{
		WildcardPinAttribute attribute = new(typeof(int), typeof(string));

		Assert.IsTrue(attribute.AllowsType(typeof(int)));
		Assert.IsTrue(attribute.AllowsType(typeof(string)));
	}

	[TestMethod]
	public void WildcardPinAttribute_AcceptsType_ForDisallowedType_ReturnsFalse()
	{
		WildcardPinAttribute attribute = new(typeof(int), typeof(string));

		Assert.IsFalse(attribute.AllowsType(typeof(double)));
		Assert.IsFalse(attribute.AllowsType(typeof(bool)));
	}

	[TestMethod]
	public void WildcardPinAttribute_AcceptsType_WithNullArgument_ThrowsArgumentNullException()
	{
		WildcardPinAttribute attribute = new(typeof(int));

		Assert.ThrowsException<ArgumentNullException>(() => attribute.AllowsType(null!));
	}

	[TestMethod]
	public void WildcardPinAttribute_EmptyTypeConstraints_AllowsAnyType()
	{
		WildcardPinAttribute attribute = new();

		Assert.IsTrue(attribute.AllowsType(typeof(int)));
		Assert.IsTrue(attribute.AllowsType(typeof(string)));
		Assert.IsTrue(attribute.AllowsType(typeof(object)));
	}
}

[Node("Test Wildcard Class")]
public class TestWildcardClass
{
	[WildcardPin(typeof(int), typeof(string), typeof(double))]
	public object? WildcardValue { get; set; }
}
