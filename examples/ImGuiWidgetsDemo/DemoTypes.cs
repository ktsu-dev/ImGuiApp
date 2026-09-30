// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using ktsu.Semantics.Strings;

/// <summary>
/// Demo strong string example.
/// </summary>
public sealed record class StrongStringExample : SemanticString<StrongStringExample> { }

/// <summary>
/// Demo enum values.
/// </summary>
public enum EnumValues
{
	/// <summary>
	/// First enum value.
	/// </summary>
	Value1,
	/// <summary>
	/// Second enum value.
	/// </summary>
	ValueB,
	/// <summary>
	/// Third enum value.
	/// </summary>
	ValueIII,
}

