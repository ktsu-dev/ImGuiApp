// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.NodeGraph;
using System;

/// <summary>
/// Marks a class, struct, or method as representing a node in a node graph.
/// This attribute is UI-agnostic and can be consumed by any node editor implementation.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = false)]
public sealed class NodeAttribute : Attribute
{
	/// <summary>
	/// The display name of the node. If not provided, the class name will be used.
	/// </summary>
	public string? DisplayName { get; set; }

	/// <summary>
	/// A color hint for the node (format agnostic - could be hex, named color, etc.).
	/// </summary>
	public string? ColorHint { get; set; }

	/// <summary>
	/// Tags for additional categorization and filtering.
	/// </summary>
	public string[]? Tags { get; set; }

	/// <summary>
	/// Initializes a new instance of the NodeAttribute class.
	/// </summary>
	public NodeAttribute()
	{
	}

	/// <summary>
	/// Initializes a new instance of the NodeAttribute class with a display name.
	/// </summary>
	/// <param name="displayName">The display name of the node.</param>
	public NodeAttribute(string displayName) => DisplayName = displayName;
}
