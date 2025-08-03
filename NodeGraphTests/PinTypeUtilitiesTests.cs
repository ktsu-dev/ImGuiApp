// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace NodeGraphTests;
using ktsu.NodeGraph;

[TestClass]
public class PinTypeUtilitiesTests
{
	[TestMethod]
	public void CanConnect_SameTypes_ReturnsTrue()
	{
		Assert.IsTrue(PinTypeUtilities.CanConnect(typeof(int), typeof(int)));
		Assert.IsTrue(PinTypeUtilities.CanConnect(typeof(string), typeof(string)));
		Assert.IsTrue(PinTypeUtilities.CanConnect(typeof(object), typeof(object)));
	}

	[TestMethod]
	public void CanConnect_AssignableTypes_ReturnsTrue()
	{
		Assert.IsTrue(PinTypeUtilities.CanConnect(typeof(string), typeof(object)));
		Assert.IsTrue(PinTypeUtilities.CanConnect(typeof(int), typeof(object)));
		Assert.IsTrue(PinTypeUtilities.CanConnect(typeof(List<int>), typeof(IEnumerable<int>)));
	}

	[TestMethod]
	public void CanConnect_NumericTypes_ReturnsTrue()
	{
		Assert.IsTrue(PinTypeUtilities.CanConnect(typeof(int), typeof(double)));
		Assert.IsTrue(PinTypeUtilities.CanConnect(typeof(float), typeof(double)));
		Assert.IsTrue(PinTypeUtilities.CanConnect(typeof(byte), typeof(int)));
		Assert.IsTrue(PinTypeUtilities.CanConnect(typeof(short), typeof(long)));
	}

	[TestMethod]
	public void CanConnect_IncompatibleTypes_ReturnsFalse()
	{
		Assert.IsFalse(PinTypeUtilities.CanConnect(typeof(DateTime), typeof(int)));
		Assert.IsFalse(PinTypeUtilities.CanConnect(typeof(bool), typeof(int)));
		Assert.IsFalse(PinTypeUtilities.CanConnect(typeof(Guid), typeof(double)));
	}

	[TestMethod]
	public void RequiresConversion_SameTypes_ReturnsFalse()
	{
		Assert.IsFalse(PinTypeUtilities.RequiresConversion(typeof(int), typeof(int)));
		Assert.IsFalse(PinTypeUtilities.RequiresConversion(typeof(string), typeof(string)));
	}

	[TestMethod]
	public void RequiresConversion_AssignableTypes_ReturnsFalse()
	{
		Assert.IsFalse(PinTypeUtilities.RequiresConversion(typeof(string), typeof(object)));
		Assert.IsFalse(PinTypeUtilities.RequiresConversion(typeof(List<int>), typeof(IEnumerable<int>)));
	}

	[TestMethod]
	public void RequiresConversion_NumericTypes_ReturnsTrue()
	{
		Assert.IsTrue(PinTypeUtilities.RequiresConversion(typeof(int), typeof(double)));
		Assert.IsTrue(PinTypeUtilities.RequiresConversion(typeof(float), typeof(double)));
		Assert.IsTrue(PinTypeUtilities.RequiresConversion(typeof(byte), typeof(int)));
	}

	[TestMethod]
	public void IsLossyConversion_IntToDouble_ReturnsFalse()
	{
		Assert.IsFalse(PinTypeUtilities.IsLossyConversion(typeof(int), typeof(double)));
		Assert.IsFalse(PinTypeUtilities.IsLossyConversion(typeof(float), typeof(double)));
	}

	[TestMethod]
	public void IsLossyConversion_DoubleToInt_ReturnsTrue()
	{
		Assert.IsTrue(PinTypeUtilities.IsLossyConversion(typeof(double), typeof(int)));
		Assert.IsTrue(PinTypeUtilities.IsLossyConversion(typeof(double), typeof(float)));
		Assert.IsTrue(PinTypeUtilities.IsLossyConversion(typeof(long), typeof(int)));
	}

	[TestMethod]
	public void IsLossyConversion_SameTypes_ReturnsFalse()
	{
		Assert.IsFalse(PinTypeUtilities.IsLossyConversion(typeof(int), typeof(int)));
		Assert.IsFalse(PinTypeUtilities.IsLossyConversion(typeof(double), typeof(double)));
	}

	[TestMethod]
	public void AllNumericTypes_ContainsExpectedTypes()
	{
		IEnumerable<Type> numericTypes = PinTypeUtilities.AllNumericTypes;

		Assert.IsTrue(numericTypes.Contains(typeof(int)));
		Assert.IsTrue(numericTypes.Contains(typeof(double)));
		Assert.IsTrue(numericTypes.Contains(typeof(float)));
		Assert.IsTrue(numericTypes.Contains(typeof(decimal)));
		Assert.IsTrue(numericTypes.Contains(typeof(byte)));
		Assert.IsTrue(numericTypes.Contains(typeof(sbyte)));
		Assert.IsTrue(numericTypes.Contains(typeof(short)));
		Assert.IsTrue(numericTypes.Contains(typeof(ushort)));
		Assert.IsTrue(numericTypes.Contains(typeof(uint)));
		Assert.IsTrue(numericTypes.Contains(typeof(long)));
		Assert.IsTrue(numericTypes.Contains(typeof(ulong)));

		Assert.IsFalse(numericTypes.Contains(typeof(string)));
		Assert.IsFalse(numericTypes.Contains(typeof(bool)));
		Assert.IsFalse(numericTypes.Contains(typeof(DateTime)));
	}

	[TestMethod]
	public void CanConnect_WithNullArguments_ReturnsFalse()
	{
		Assert.IsFalse(PinTypeUtilities.CanConnect(null!, typeof(int)));
		Assert.IsFalse(PinTypeUtilities.CanConnect(typeof(int), null!));
	}

	[TestMethod]
	public void RequiresConversion_WithNullArguments_ThrowsArgumentNullException()
	{
		Assert.ThrowsException<ArgumentNullException>(() => PinTypeUtilities.RequiresConversion(null!, typeof(int)));
		Assert.ThrowsException<ArgumentNullException>(() => PinTypeUtilities.RequiresConversion(typeof(int), null!));
	}

	[TestMethod]
	public void IsLossyConversion_WithNullArguments_ThrowsArgumentNullException()
	{
		Assert.ThrowsException<ArgumentNullException>(() => PinTypeUtilities.IsLossyConversion(null!, typeof(int)));
		Assert.ThrowsException<ArgumentNullException>(() => PinTypeUtilities.IsLossyConversion(typeof(int), null!));
	}
}
