// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor;

using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// Represents a node in the editor
/// </summary>
public record Node(
	int Id,
	Vector2 Position,
	string Name,
	List<Pin> InputPins,
	List<Pin> OutputPins,
	Vector2 Dimensions = default,
	Vector2 Velocity = default,
	Vector2 Force = default,
	bool IsPinned = false
);

/// <summary>
/// Represents a connection between two pins
/// </summary>
public record Link(
	int Id,
	int OutputPinId,
	int InputPinId
);

/// <summary>
/// Represents a pin on a node
/// </summary>
/// <param name="Id">The pin's identifier, unique across every pin in the graph.</param>
/// <param name="Direction">Whether the pin takes a connection in or sends one out.</param>
/// <param name="Name">The pin's name.</param>
/// <param name="DisplayName">What to show instead of <paramref name="Name"/>, when they differ.</param>
/// <param name="AllowMultipleConnections">
/// How many links may meet this pin, or null to take the default for its direction. See
/// <see cref="AllowsMultipleConnections"/>.
/// </param>
/// <param name="DataType">
/// The .NET type this pin carries, or null when it is not known. Null is what the name-only
/// overloads produce, and it means the pin accepts any value rather than none.
/// </param>
public record Pin(
	int Id,
	PinDirection Direction,
	string Name,
	string? DisplayName = null,
	bool? AllowMultipleConnections = null,
	Type? DataType = null
)
{
	/// <summary>
	/// Gets the name to display in the UI, preferring DisplayName over Name
	/// </summary>
	public string EffectiveDisplayName => DisplayName ?? Name;

	/// <summary>
	/// Whether more than one link may meet this pin.
	/// </summary>
	/// <remarks>
	/// An output pin fans out by default: one value can feed as many consumers as want it, and
	/// refusing the second link would mean inserting a node whose only job is to duplicate the
	/// first. An input pin takes one link by default, because a pin fed from two places has no
	/// answer to which value it holds.
	/// <para>
	/// Either default is overridden by giving <see cref="AllowMultipleConnections"/> a value, which
	/// is how a declared <c>[InputPin(AllowMultipleConnections = true)]</c> or
	/// <c>[OutputPin(AllowMultipleConnections = false)]</c> reaches the graph.
	/// </para>
	/// </remarks>
	public bool AllowsMultipleConnections => AllowMultipleConnections ?? (Direction == PinDirection.Output);
};

/// <summary>
/// What a pin should be created as: everything the engine needs in one value, so a caller does not
/// create a pin and then patch it.
/// </summary>
/// <param name="Name">The pin's display name.</param>
/// <param name="DataType">The .NET type it carries, or null for an untyped pin.</param>
/// <param name="DefaultValue">
/// What the pin holds before anything sets it, or null for nothing. This is the value
/// <see cref="PinValueStore.Reset"/> goes back to.
/// </param>
/// <param name="AllowMultipleConnections">
/// How many links it accepts, or null to take the default for its direction.
/// </param>
public readonly record struct PinSpec(
	string Name,
	Type? DataType = null,
	object? DefaultValue = null,
	bool? AllowMultipleConnections = null
);

/// <summary>
/// Direction of pin (input or output)
/// </summary>
public enum PinDirection
{
	/// <inheritdoc/>
	Input,
	/// <inheritdoc/>
	Output
}

