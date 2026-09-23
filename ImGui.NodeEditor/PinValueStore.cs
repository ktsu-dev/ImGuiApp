// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor;

using System;
using System.Collections.Generic;

/// <summary>
/// What each pin currently holds, and what it should go back to.
/// </summary>
/// <remarks>
/// Keyed by pin id rather than by node and member name, which is what the engine's pin offset table
/// is keyed by and what lets a graph built through the name-only <c>CreateNode</c> overloads hold
/// values too, rather than only an attribute-declared one.
/// <para>
/// It holds values and decides which ones a pin accepts. It knows nothing about drawing, nothing
/// about the graph's shape, and nothing about what a connected pin means: a pin fed by a link still
/// has whatever literal was last written to it, and it is the caller that decides to prefer the
/// link.
/// </para>
/// </remarks>
public sealed class PinValueStore
{
	private readonly Dictionary<int, object?> values = [];
	private readonly Dictionary<int, object?> defaults = [];

	/// <summary>
	/// Record what a pin starts at and should reset to.
	/// </summary>
	/// <param name="pinId">The pin.</param>
	/// <param name="defaultValue">Its declared default, which may be null.</param>
	/// <remarks>
	/// The seed is not type checked. It comes from the pin's own declaration, so refusing it here
	/// would mean a node that cannot be created rather than a value that cannot be set.
	/// </remarks>
	public void Seed(int pinId, object? defaultValue)
	{
		defaults[pinId] = defaultValue;
		values[pinId] = defaultValue;
	}

	/// <summary>
	/// What a pin currently holds.
	/// </summary>
	/// <param name="pinId">The pin.</param>
	/// <returns>Its value, or null if it has none.</returns>
	public object? Get(int pinId) => values.TryGetValue(pinId, out object? value) ? value : null;

	/// <summary>
	/// Write a value to a pin, if the pin will take it.
	/// </summary>
	/// <param name="pin">The pin, which carries the type the value has to match.</param>
	/// <param name="value">The value.</param>
	/// <returns>True if it was written, false if the pin's type refused it.</returns>
	public bool TrySet(Pin pin, object? value)
	{
		Ensure.NotNull(pin);

		if (!Accepts(pin.DataType, value))
		{
			return false;
		}

		values[pin.Id] = value;
		return true;
	}

	/// <summary>
	/// Put a pin back to the value it was seeded with.
	/// </summary>
	/// <param name="pinId">The pin.</param>
	/// <returns>True if it had a seed to go back to.</returns>
	public bool Reset(int pinId)
	{
		if (!defaults.TryGetValue(pinId, out object? seeded))
		{
			return false;
		}

		values[pinId] = seeded;
		return true;
	}

	/// <summary>Drop everything held for one pin.</summary>
	/// <param name="pinId">The pin.</param>
	public void Forget(int pinId)
	{
		values.Remove(pinId);
		defaults.Remove(pinId);
	}

	/// <summary>Drop everything held for every pin.</summary>
	public void Clear()
	{
		values.Clear();
		defaults.Clear();
	}

	/// <summary>
	/// Whether a pin of this type will take this value.
	/// </summary>
	/// <param name="dataType">The pin's declared type, or null when it has none.</param>
	/// <param name="value">The value offered.</param>
	/// <returns>True if the value may be stored.</returns>
	/// <remarks>
	/// A null type accepts anything. Otherwise the check is against the underlying type, because a
	/// boxed <c>T?</c> is indistinguishable from a boxed <c>T</c> and
	/// <c>typeof(double?).IsInstanceOfType(1.0)</c> is false. Null is accepted for a reference type
	/// and for a <c>Nullable{T}</c>, and refused for any other value type.
	/// <para>
	/// The match is exact rather than widening: an int offered to a double pin is refused. The
	/// editors write the pin's own type, so a mismatch is a caller's bug and is better reported than
	/// silently converted.
	/// </para>
	/// </remarks>
	public static bool Accepts(Type? dataType, object? value)
	{
		if (dataType is null)
		{
			return true;
		}

		Type underlying = Nullable.GetUnderlyingType(dataType) ?? dataType;

		return value is null
			? !underlying.IsValueType || underlying != dataType
			: underlying.IsInstanceOfType(value);
	}
}
