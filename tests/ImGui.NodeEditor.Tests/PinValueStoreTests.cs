// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor.Tests;

using System;

using ktsu.ImGui.NodeEditor;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives <see cref="PinValueStore"/> alone. It holds values and decides which ones a pin will
/// accept, and does so without an engine, a context or a frame.
/// </summary>
[TestClass]
public sealed class PinValueStoreTests
{
	private enum Polarity
	{
		Rising,
		Falling,
	}

	private readonly PinValueStore store = new();

	private static Pin TypedInput(int id, Type? dataType) =>
		new(id, PinDirection.Input, $"In {id}", DataType: dataType);

	[TestMethod]
	public void TrySet_WithAMatchingType_KeepsTheValue()
	{
		Pin pin = TypedInput(1, typeof(double));

		Assert.IsTrue(store.TrySet(pin, 128.0));
		Assert.AreEqual(128.0, store.Get(pin.Id));
	}

	[TestMethod]
	public void TrySet_WithTheWrongType_RefusesAndLeavesTheValueAlone()
	{
		Pin pin = TypedInput(1, typeof(double));
		store.TrySet(pin, 128.0);

		Assert.IsFalse(store.TrySet(pin, "not a number"), "A double pin has no reading of a string.");
		Assert.AreEqual(128.0, store.Get(pin.Id), "A refused write must not disturb what was there.");
	}

	[TestMethod]
	public void TrySet_OnAnUntypedPin_AcceptsAnything()
	{
		Pin pin = TypedInput(1, dataType: null);

		Assert.IsTrue(store.TrySet(pin, "anything"), "An untyped pin is what the name-only overloads make, and refusing every value would make them worse than they are.");
		Assert.AreEqual("anything", store.Get(pin.Id));
	}

	/// <summary>
	/// A boxed <c>double?</c> is just a boxed double, so <c>typeof(double?).IsInstanceOfType(1.0)</c>
	/// is false and the obvious check would refuse every value a nullable pin was ever given.
	/// <c>TimerNode.StartTime</c> is exactly this shape.
	/// </summary>
	[TestMethod]
	public void TrySet_OnANullablePin_AcceptsTheUnderlyingValueAndNull()
	{
		Pin pin = TypedInput(1, typeof(double?));

		Assert.IsTrue(store.TrySet(pin, 1.5));
		Assert.AreEqual(1.5, store.Get(pin.Id));

		Assert.IsTrue(store.TrySet(pin, null));
		Assert.IsNull(store.Get(pin.Id));
	}

	[TestMethod]
	public void TrySet_WithNullOnANonNullableValueType_Refuses()
	{
		Pin pin = TypedInput(1, typeof(double));

		Assert.IsFalse(store.TrySet(pin, null), "A double is never absent.");
	}

	[TestMethod]
	public void TrySet_WithAnEnumValue_KeepsIt()
	{
		Pin pin = TypedInput(1, typeof(Polarity));

		Assert.IsTrue(store.TrySet(pin, Polarity.Falling));
		Assert.AreEqual(Polarity.Falling, store.Get(pin.Id));
	}

	[TestMethod]
	public void Get_ForAPinNobodyHasWrittenTo_IsNull()
	{
		Assert.IsNull(store.Get(99));
	}

	[TestMethod]
	public void Reset_ReturnsTheValueToWhatItWasSeededWith()
	{
		Pin pin = TypedInput(1, typeof(double));
		store.Seed(pin.Id, 128.0);
		store.TrySet(pin, 200.0);

		Assert.IsTrue(store.Reset(pin.Id));
		Assert.AreEqual(128.0, store.Get(pin.Id));
	}

	[TestMethod]
	public void Reset_ForAPinThatWasNeverSeeded_ReportsSo()
	{
		Assert.IsFalse(store.Reset(99), "There is no default to go back to.");
	}

	[TestMethod]
	public void Forget_DropsTheValueAndItsDefault()
	{
		Pin pin = TypedInput(1, typeof(double));
		store.Seed(pin.Id, 128.0);

		store.Forget(pin.Id);

		Assert.IsNull(store.Get(pin.Id));
		Assert.IsFalse(store.Reset(pin.Id), "Forgetting a pin forgets what it defaulted to as well.");
	}
}
