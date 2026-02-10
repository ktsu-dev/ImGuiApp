// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.NodeGraph;
using System;

/// <summary>
/// Specifies the execution behavior of a node.
/// </summary>
public enum NodeExecutionMode
{
	/// <summary>
	/// Node is executed when any of its inputs change (reactive).
	/// </summary>
	OnInputChange,

	/// <summary>
	/// Node is executed only when explicitly triggered via execution pins.
	/// </summary>
	OnExecution,

	/// <summary>
	/// Node is executed once when the graph starts.
	/// </summary>
	OnStart,

	/// <summary>
	/// Node is executed continuously (every frame/tick).
	/// </summary>
	Continuous,

	/// <summary>
	/// Node execution is manually controlled.
	/// </summary>
	Manual
}

/// <summary>
/// Specifies how a node should behave during execution.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class NodeBehaviorAttribute : Attribute
{
	/// <summary>
	/// The execution mode for this node.
	/// </summary>
	public NodeExecutionMode ExecutionMode { get; set; } = NodeExecutionMode.OnInputChange;

	/// <summary>
	/// Whether this node can be executed asynchronously.
	/// </summary>
	public bool SupportsAsyncExecution { get; set; }

	/// <summary>
	/// Whether this node has side effects (I/O, state changes, etc.).
	/// </summary>
	public bool HasSideEffects { get; set; }

	/// <summary>
	/// Whether this node's output is deterministic (same inputs always produce same outputs).
	/// </summary>
	public bool IsDeterministic { get; set; } = true;

	/// <summary>
	/// Whether this node can be cached (if deterministic and no side effects).
	/// </summary>
	public bool IsCacheable { get; set; }

	/// <summary>
	/// Initializes a new instance of the NodeBehaviorAttribute class.
	/// </summary>
	public NodeBehaviorAttribute()
	{
	}

	/// <summary>
	/// Initializes a new instance of the NodeBehaviorAttribute class with an execution mode.
	/// </summary>
	/// <param name="executionMode">The execution mode for this node.</param>
	public NodeBehaviorAttribute(NodeExecutionMode executionMode) => ExecutionMode = executionMode;
}

/// <summary>
/// Marks a method as the primary execution method for a node.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class NodeExecuteAttribute : Attribute
{
	/// <summary>
	/// The order in which this execution method should be called if multiple exist.
	/// </summary>
	public int Order { get; set; }

	/// <summary>
	/// Whether this method is asynchronous.
	/// </summary>
	public bool IsAsync { get; set; }

	/// <summary>
	/// Initializes a new instance of the NodeExecuteAttribute class.
	/// </summary>
	public NodeExecuteAttribute()
	{
	}
}

/// <summary>
/// Marks a method as a validation method that should be called before execution.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class NodeValidateAttribute : Attribute
{
	/// <summary>
	/// The validation phase when this method should be called.
	/// </summary>
	public string? Phase { get; set; }

	/// <summary>
	/// Initializes a new instance of the NodeValidateAttribute class.
	/// </summary>
	public NodeValidateAttribute()
	{
	}
}

/// <summary>
/// Marks a node as deprecated and provides migration information.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class NodeDeprecatedAttribute : Attribute
{
	/// <summary>
	/// The reason why this node is deprecated.
	/// </summary>
	public string? Reason { get; set; }

	/// <summary>
	/// The recommended replacement node type.
	/// </summary>
	public Type? ReplacementType { get; set; }

	/// <summary>
	/// The version when this node was deprecated.
	/// </summary>
	public string? DeprecatedInVersion { get; set; }

	/// <summary>
	/// The version when this node will be removed.
	/// </summary>
	public string? RemovalVersion { get; set; }

	/// <summary>
	/// Initializes a new instance of the NodeDeprecatedAttribute class.
	/// </summary>
	public NodeDeprecatedAttribute()
	{
	}

	/// <summary>
	/// Initializes a new instance of the NodeDeprecatedAttribute class with a reason.
	/// </summary>
	/// <param name="reason">The reason why this node is deprecated.</param>
	public NodeDeprecatedAttribute(string reason) => Reason = reason;
}

/// <summary>
/// Controls the visibility and availability of a node in the editor.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = false)]
public sealed class NodeVisibilityAttribute : Attribute
{
	/// <summary>
	/// Whether this node is visible in the node menu.
	/// </summary>
	public bool VisibleInMenu { get; set; } = true;

	/// <summary>
	/// Whether this node can be instantiated by users.
	/// </summary>
	public bool CanBeInstantiated { get; set; } = true;

	/// <summary>
	/// Whether this node is experimental and should be marked as such.
	/// </summary>
	public bool IsExperimental { get; set; }

	/// <summary>
	/// Minimum editor version required to use this node.
	/// </summary>
	public string? MinimumEditorVersion { get; set; }

	/// <summary>
	/// Features required to use this node.
	/// </summary>
	public string[]? RequiredFeatures { get; set; }

	/// <summary>
	/// Initializes a new instance of the NodeVisibilityAttribute class.
	/// </summary>
	public NodeVisibilityAttribute()
	{
	}
}
