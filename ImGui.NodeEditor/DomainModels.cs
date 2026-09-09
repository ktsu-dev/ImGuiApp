// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor;

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
public record Pin(
	int Id,
	PinDirection Direction,
	string Name,
	string? DisplayName = null,
	bool? AllowMultipleConnections = null
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
/// Direction of pin (input or output)
/// </summary>
public enum PinDirection
{
	/// <inheritdoc/>
	Input,
	/// <inheritdoc/>
	Output
}

