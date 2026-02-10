// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.NodeGraph;
using System;

/// <summary>
/// Marks a property, field, or parameter as an execution input pin.
/// Execution pins control the flow of execution through the node graph.
/// For methods, this can be applied to the method itself or parameters.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Method, AllowMultiple = false)]
public sealed class ExecutionInputAttribute : PinAttribute
{
	/// <summary>
	/// This is an execution pin.
	/// </summary>
	public override PinType PinType => PinType.Execution;

	/// <summary>
	/// Whether this execution input can accept multiple connections.
	/// Typically false for execution flow.
	/// </summary>
	public bool AllowMultipleConnections { get; set; } = false;

	/// <summary>
	/// Initializes a new instance of the ExecutionInputAttribute class.
	/// </summary>
	public ExecutionInputAttribute() => ColorHint = "white";

	/// <summary>
	/// Initializes a new instance of the ExecutionInputAttribute class with a display name.
	/// </summary>
	/// <param name="displayName">The display name of the execution pin.</param>
	public ExecutionInputAttribute(string displayName) : base(displayName) => ColorHint = "white";
}

/// <summary>
/// Marks a property, field, or method as an execution output pin.
/// Execution pins control the flow of execution through the node graph.
/// For methods, this represents completion of execution.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = false)]
public sealed class ExecutionOutputAttribute : PinAttribute
{
	/// <summary>
	/// This is an execution pin.
	/// </summary>
	public override PinType PinType => PinType.Execution;

	/// <summary>
	/// Whether this execution output can connect to multiple inputs.
	/// Typically true for execution flow.
	/// </summary>
	public bool AllowMultipleConnections { get; set; } = true;

	/// <summary>
	/// Initializes a new instance of the ExecutionOutputAttribute class.
	/// </summary>
	public ExecutionOutputAttribute() => ColorHint = "white";

	/// <summary>
	/// Initializes a new instance of the ExecutionOutputAttribute class with a display name.
	/// </summary>
	/// <param name="displayName">The display name of the execution pin.</param>
	public ExecutionOutputAttribute(string displayName) : base(displayName) => ColorHint = "white";
}
