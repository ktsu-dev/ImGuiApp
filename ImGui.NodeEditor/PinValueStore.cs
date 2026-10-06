// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor;

using System;
using System.Collections.Generic;
using System.Globalization;

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
	/// Read a pin's seeded default, if it has one.
	/// </summary>
	/// <param name="pinId">The pin.</param>
	/// <param name="hasDefault">Whether it was seeded.</param>
	/// <param name="defaultValue">What it was seeded with.</param>
	internal void TryGetDefault(int pinId, out bool hasDefault, out object? defaultValue) =>
		hasDefault = defaults.TryGetValue(pinId, out defaultValue);

	/// <summary>
	/// Put a pin back exactly as it was held, default and all, with no type check.
	/// </summary>
	/// <param name="pinId">The pin.</param>
	/// <param name="value">Its value.</param>
	/// <param name="hasDefault">Whether it had a seeded default.</param>
	/// <param name="defaultValue">That default.</param>
	internal void Restore(int pinId, object? value, bool hasDefault, object? defaultValue)
	{
		values[pinId] = value;
		if (hasDefault)
		{
			defaults[pinId] = defaultValue;
		}
		else
		{
			defaults.Remove(pinId);
		}
	}

	/// <summary>
	/// Put a pin's value back with no type check, leaving its default alone.
	/// </summary>
	/// <param name="pinId">The pin.</param>
	/// <param name="value">Its value.</param>
	internal void Restore(int pinId, object? value) => values[pinId] = value;

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
	/// The match is assignable, not convertible: an int offered to a double pin is refused, even
	/// though a conversion exists. The editors write the pin's own type, so a mismatch is a
	/// caller's bug and is better reported than silently converted.
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

	/// <summary>
	/// Bring a declared default in line with a pin's declared type.
	/// </summary>
	/// <param name="dataType">The type the value has to end up as, or null when there is none.</param>
	/// <param name="value">The value as written on the declaration, which is untyped
	/// <see cref="object"/> and so is trusted by nothing until it gets here.</param>
	/// <returns>
	/// <paramref name="value"/> unchanged when <see cref="Accepts(Type?, object?)"/> already takes
	/// it, a converted value when <see cref="IConvertible"/> can bridge the mismatch (an <c>int</c>
	/// literal on a <c>double</c> pin, for instance), or null when neither holds.
	/// </returns>
	/// <remarks>
	/// <see cref="ktsu.NodeGraph.InputPinAttribute.DefaultValue"/> is declared as <c>object?</c>
	/// straight off the attribute, so <c>[InputPin("X", DefaultValue = 50)]</c> on a
	/// <see langword="double"/> property means a boxed <see langword="int"/> unless this catches it.
	/// <para>
	/// Every route a declared default can take to a pin goes through here — the seed
	/// <see cref="NodeEditorEngine.CreateNodeFromSpecs"/> writes to this store, and the value
	/// <see cref="AttributeBasedNodeFactory"/> writes onto a node's backing instance — so the two
	/// cannot read a mistyped default differently. A bad declaration must still produce a creatable
	/// node, so this never throws; it yields null when it cannot convert, and the callers treat null
	/// as "nothing usable was declared".
	/// </para>
	/// </remarks>
	public static object? Coerce(Type? dataType, object? value)
	{
		if (Accepts(dataType, value))
		{
			return value;
		}

		if (value is not IConvertible)
		{
			return null;
		}

		// Accepts takes anything when the type is null, so reaching here means there is one.
		Type target = Nullable.GetUnderlyingType(dataType!) ?? dataType!;

		try
		{
			return Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
		}
		catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
		{
			// A declared default that will not convert to its pin's type is seeded as null rather
			// than thrown: a bad attribute must still produce a creatable node.
			return null;
		}
	}
}
