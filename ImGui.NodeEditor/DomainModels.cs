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
/// Says which node was removed from the graph.
/// </summary>
/// <param name="nodeId">The removed node's identifier.</param>
/// <remarks>
/// The node itself is deliberately not carried. By the time the event is raised the node is gone
/// from the graph, and handing back a record of it invites a handler to treat it as still live.
/// The id is what a handler keyed by node id actually needs.
/// </remarks>
public sealed class NodeRemovedEventArgs(int nodeId) : EventArgs
{
	/// <summary>Gets the removed node's identifier.</summary>
	public int NodeId { get; } = nodeId;
}

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

/// <summary>
/// Says which pin's value changed, from what, to what, and as part of which edit.
/// </summary>
/// <param name="PinId">The pin whose value was written.</param>
/// <param name="OldValue">What it held before.</param>
/// <param name="NewValue">What it holds now.</param>
/// <param name="EditGesture">
/// The continuous edit the write belongs to, or null for a write that stands alone. See
/// <see cref="NodeEditorEngine.SetPinValue(int, object?, long?)"/>.
/// </param>
public sealed class PinValueChangedEventArgs(int PinId, object? OldValue, object? NewValue, long? EditGesture) : EventArgs
{
	/// <summary>Gets the pin whose value was written.</summary>
	public int PinId { get; } = PinId;

	/// <summary>Gets what it held before.</summary>
	public object? OldValue { get; } = OldValue;

	/// <summary>Gets what it holds now.</summary>
	public object? NewValue { get; } = NewValue;

	/// <summary>Gets the continuous edit the write belongs to, or null for a write that stands alone.</summary>
	public long? EditGesture { get; } = EditGesture;
}

/// <summary>
/// A labelled rectangle drawn behind the nodes, used to name and organise a region of the graph.
/// </summary>
/// <param name="Id">The comment box's identifier, unique among comment boxes. It shares no space with node ids.</param>
/// <param name="Title">The label drawn in its title bar.</param>
/// <param name="Position">Its top-left corner, in the same space as node positions.</param>
/// <param name="Size">Its width and height, in the same space as node dimensions.</param>
/// <param name="Color">
/// The colour it is filled with, or null to take one from the editor's theme. The title bar and
/// border are drawn from the same colour at a higher opacity.
/// </param>
/// <remarks>
/// A comment box does not own the nodes inside it. What it contains is decided by geometry every
/// time it is asked — a node lying wholly within its rectangle is in it — so dragging a node out of
/// a box takes it out, and nothing has to be kept in step when a node is added, removed or moved by
/// the layout. <see cref="NodeEditorEngine.MoveCommentBox"/> is what carries the contents along.
/// <para>
/// Comment boxes take no part in the force-directed layout. They are not bodies, and nothing pushes
/// a node out of one or pulls it in.
/// </para>
/// </remarks>
public sealed record CommentBox(int Id, string Title, Vector2 Position, Vector2 Size, Vector4? Color = null)
{
	/// <summary>The bottom-right corner.</summary>
	public Vector2 Max => Position + Size;

	/// <summary>
	/// Whether a rectangle lies wholly inside this box.
	/// </summary>
	/// <param name="position">The rectangle's top-left corner.</param>
	/// <param name="size">Its width and height.</param>
	/// <returns>True when no part of it is outside.</returns>
	public bool Contains(Vector2 position, Vector2 size) =>
		position.X >= Position.X && position.Y >= Position.Y &&
		position.X + size.X <= Max.X && position.Y + size.Y <= Max.Y;
}

/// <summary>
/// One node's move from where a gesture found it to where the gesture left it.
/// </summary>
/// <param name="NodeId">The node.</param>
/// <param name="From">Where it was before.</param>
/// <param name="To">Where it is after.</param>
public readonly record struct NodeMove(int NodeId, Vector2 From, Vector2 To);
