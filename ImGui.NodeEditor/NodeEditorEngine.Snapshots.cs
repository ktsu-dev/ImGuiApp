// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

/// <summary>
/// The capture and restore primitives <see cref="NodeEditorHistory"/> is built on.
/// </summary>
/// <remarks>
/// These are internal on purpose. Restoring a node by id is only safe for a node this engine issued
/// and then removed, which is exactly what the history knows and nothing else does.
/// </remarks>
public partial class NodeEditorEngine
{
	/// <summary>
	/// Capture everything needed to put a node back after it is removed.
	/// </summary>
	/// <param name="nodeId">The node.</param>
	/// <returns>The node's state, or null if there is no such node.</returns>
	/// <remarks>
	/// The pin lists are copied, since <see cref="SetPinAllowsMultipleConnections"/> edits them in
	/// place. A bound pin's accessor is kept rather than its instance, so restoring the node puts
	/// its value back on the same object it lived on.
	/// </remarks>
	internal NodeSnapshot? CaptureNode(int nodeId)
	{
		int index = nodes.FindIndex(n => n.Id == nodeId);
		if (index < 0)
		{
			return null;
		}

		Node node = nodes[index];
		Node copy = node with { InputPins = [.. node.InputPins], OutputPins = [.. node.OutputPins] };

		List<PinSnapshot> pins = [];
		foreach (Pin pin in node.InputPins.Concat(node.OutputPins))
		{
			pinValues.TryGetDefault(pin.Id, out bool hasDefault, out object? defaultValue);
			pins.Add(new PinSnapshot(
				pin.Id,
				GetPinValue(pin.Id),
				hasDefault,
				defaultValue,
				pinValueAccessors.TryGetValue(pin.Id, out PinValueAccessor? accessor) ? accessor : null,
				pinIdToOffset.TryGetValue(pin.Id, out Vector2 offset) ? offset : null));
		}

		return new NodeSnapshot(copy, index, pins);
	}

	/// <summary>
	/// Put a removed node back exactly as it was captured: the same id, the same pins, the same
	/// values.
	/// </summary>
	/// <param name="snapshot">What <see cref="CaptureNode"/> returned before the node was removed.</param>
	/// <returns>True if it was put back, false if a node with its id is already present.</returns>
	/// <remarks>
	/// The id counters are raised past the restored ids, so a node created afterwards cannot be
	/// issued an id the restored one already holds. It comes back at rest: whatever velocity the
	/// layout had given it when it was removed is not handed back to fling it across the view.
	/// </remarks>
	internal bool RestoreNode(NodeSnapshot snapshot)
	{
		Node captured = snapshot.Node;
		if (nodes.Exists(n => n.Id == captured.Id))
		{
			return false;
		}

		Node restored = captured with
		{
			InputPins = [.. captured.InputPins],
			OutputPins = [.. captured.OutputPins],
			Velocity = Vector2.Zero,
			Force = Vector2.Zero,
		};

		nodes.Insert(Math.Clamp(snapshot.Index, 0, nodes.Count), restored);
		nextNodeId = Math.Max(nextNodeId, restored.Id + 1);

		foreach (PinSnapshot pin in snapshot.Pins)
		{
			nextPinId = Math.Max(nextPinId, pin.PinId + 1);

			pinValues.Restore(pin.PinId, pin.Value, pin.HasDefault, pin.DefaultValue);

			if (pin.Accessor is PinValueAccessor accessor)
			{
				pinValueAccessors[pin.PinId] = accessor;
				accessor.Set(pin.Value);
			}

			if (pin.Offset is Vector2 offset)
			{
				pinIdToOffset[pin.PinId] = offset;
			}
		}

		return true;
	}

	/// <summary>
	/// Put a removed link back with the id it had.
	/// </summary>
	/// <param name="link">The link as it was.</param>
	/// <returns>True if it was put back, false if either pin is missing or the id is taken.</returns>
	/// <remarks>
	/// This does not go through <see cref="TryCreateLink"/>, whose rules decide whether a link may be
	/// drawn; this link was drawn already, and putting it back is not a new decision.
	/// </remarks>
	internal bool RestoreLink(Link link)
	{
		if (links.Exists(l => l.Id == link.Id) || FindPin(link.OutputPinId) is null || FindPin(link.InputPinId) is null)
		{
			return false;
		}

		links.Add(link);
		nextLinkId = Math.Max(nextLinkId, link.Id + 1);
		return true;
	}

	/// <summary>
	/// Write a pin's value back as it was, bypassing the type check and not reporting it as an edit.
	/// </summary>
	/// <param name="pinId">The pin.</param>
	/// <param name="value">The value it held.</param>
	/// <remarks>
	/// The type check is bypassed because the value came off this pin: a value-typed pin that has
	/// never been set holds null, and that null is a state an undo has to be able to go back to.
	/// </remarks>
	internal void RestorePinValue(int pinId, object? value)
	{
		if (FindPin(pinId) is null)
		{
			return;
		}

		if (pinValueAccessors.TryGetValue(pinId, out PinValueAccessor? accessor))
		{
			accessor.Set(value);
			return;
		}

		pinValues.Restore(pinId, value);
	}

	/// <summary>
	/// Put a node's own fields back — where it is, what it is called, whether it is pinned, and its
	/// pins' capacity — leaving what the layout and the renderer measure alone.
	/// </summary>
	/// <param name="state">The node as it should be.</param>
	/// <returns>True if the node exists.</returns>
	internal bool RestoreNodeFields(Node state)
	{
		int index = nodes.FindIndex(n => n.Id == state.Id);
		if (index < 0)
		{
			return false;
		}

		Node current = nodes[index];
		nodes[index] = current with
		{
			Position = state.Position,
			Name = state.Name,
			IsPinned = state.IsPinned,
			InputPins = [.. state.InputPins],
			OutputPins = [.. state.OutputPins],
			Velocity = Vector2.Zero,
		};

		return true;
	}
}

/// <summary>What a node was, so it can be put back after it is removed.</summary>
/// <param name="Node">The node, with its pin lists copied.</param>
/// <param name="Index">Where it stood in the drawing order.</param>
/// <param name="Pins">What each of its pins held.</param>
internal sealed record NodeSnapshot(Node Node, int Index, IReadOnlyList<PinSnapshot> Pins);

/// <summary>What a pin held, so it can be put back.</summary>
/// <param name="PinId">The pin.</param>
/// <param name="Value">Its value.</param>
/// <param name="HasDefault">Whether it had a seeded default.</param>
/// <param name="DefaultValue">That default.</param>
/// <param name="Accessor">Where its value lives, when not in the engine's own store.</param>
/// <param name="Offset">Where the renderer last measured it, if it has.</param>
internal sealed record PinSnapshot(int PinId, object? Value, bool HasDefault, object? DefaultValue, PinValueAccessor? Accessor, Vector2? Offset);
