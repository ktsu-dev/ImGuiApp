// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor;

using System;

/// <summary>
/// Where a pin's value lives, when it lives somewhere other than the engine's own
/// <see cref="PinValueStore"/>.
/// </summary>
/// <param name="Get">Reads the value from wherever it is kept.</param>
/// <param name="Set">
/// Writes the value, and reports whether the write happened. False means the value did not land —
/// a setter that threw, or a member that refused the value — which is what
/// <see cref="NodeEditorEngine.SetPinValue(int, object?)"/> reports back to its caller.
/// </param>
/// <remarks>
/// This is the whole of what the engine knows about a value that lives elsewhere: two delegates. It
/// deliberately carries no reflection, no <c>PinDefinition</c> and no node instance, because the
/// engine knows the graph and nothing about how a host chose to model a node. Whoever has both the
/// declaration and the object — <see cref="AttributeBasedNodeFactory"/>, or a host with its own
/// scheme — closes over them and registers the pair with
/// <see cref="NodeEditorEngine.BindPinValue(int, PinValueAccessor)"/>.
/// <para>
/// A bound pin has exactly one home: the accessor's. The store still holds that pin's seeded
/// default, which is what <see cref="NodeEditorEngine.ResetPinValue(int)"/> writes back through the
/// accessor, so the two can never be read as disagreeing.
/// </para>
/// </remarks>
public sealed record PinValueAccessor(Func<object?> Get, Func<object?, bool> Set);
