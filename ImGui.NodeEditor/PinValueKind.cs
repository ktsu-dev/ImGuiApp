// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.NodeEditor;

using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// The kinds of value a pin can be edited as.
/// </summary>
#pragma warning disable CA1720 // Identifier contains type name — the name IS the type it represents
public enum PinValueKind
{
	/// <summary>No editor. The pin is drawn as it always was.</summary>
	Unsupported,

	/// <inheritdoc cref="bool"/>
	Boolean,

	/// <inheritdoc cref="int"/>
	Int32,

	/// <inheritdoc cref="float"/>
	Single,

	/// <inheritdoc cref="double"/>
	Double,

	/// <inheritdoc cref="string"/>
	String,

	/// <inheritdoc cref="System.Numerics.Vector2"/>
	Vector2,

	/// <inheritdoc cref="System.Numerics.Vector3"/>
	Vector3,

	/// <summary>Any enumeration, drawn as a list of its names.</summary>
	Enum,
}
#pragma warning restore CA1720

/// <summary>
/// Decides which pin types have an editor.
/// </summary>
/// <remarks>
/// Both the inline editors on a node face and the node inspector panel classify through
/// this, so a type has an editor on both surfaces or on neither. The two draw differently, one with
/// raw ImGui and one with a property grid, and that is the only difference between them.
/// </remarks>
public static class PinValueKinds
{
	/// <summary>
	/// What kind of editor a pin of this type needs.
	/// </summary>
	/// <param name="dataType">The pin's declared type, or null when it has none.</param>
	/// <returns>Its kind, or <see cref="PinValueKind.Unsupported"/> if nothing here can edit it.</returns>
	/// <remarks>
	/// <c>long</c> is deliberately unsupported. Dear ImGui has no <c>InputLong</c>, so it needs an
	/// unsafe <c>DragScalar</c> with <c>ImGuiDataType.S64</c>, which is not worth one unverified
	/// interop call in v1. Adding it is an entry in <see cref="KindsByType"/> and a case in each
	/// surface's switch.
	/// </remarks>
	public static PinValueKind Classify(Type? dataType)
	{
		if (dataType is null)
		{
			return PinValueKind.Unsupported;
		}

		Type underlying = Nullable.GetUnderlyingType(dataType) ?? dataType;

		// An enum is matched by shape rather than by identity, so it cannot be a table entry and has
		// to be asked about before the lookup.
		if (underlying.IsEnum)
		{
			return PinValueKind.Enum;
		}

		return KindsByType.TryGetValue(underlying, out PinValueKind kind) ? kind : PinValueKind.Unsupported;
	}

	/// <summary>
	/// The types that have an editor, and which one.
	/// </summary>
	/// <remarks>
	/// A lookup rather than a switch over <c>Type t when t == typeof(...)</c>: each of those guards
	/// re-tested that the already-non-null <c>Type</c> was a <c>Type</c>, which is a condition that
	/// cannot be false.
	/// </remarks>
	private static readonly Dictionary<Type, PinValueKind> KindsByType = new()
	{
		[typeof(bool)] = PinValueKind.Boolean,
		[typeof(int)] = PinValueKind.Int32,
		[typeof(float)] = PinValueKind.Single,
		[typeof(double)] = PinValueKind.Double,
		[typeof(string)] = PinValueKind.String,
		[typeof(Vector2)] = PinValueKind.Vector2,
		[typeof(Vector3)] = PinValueKind.Vector3,
	};
}
