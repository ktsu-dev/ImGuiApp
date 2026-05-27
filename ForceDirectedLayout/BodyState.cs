// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ForceDirectedLayout;

using System.Runtime.InteropServices;

/// <summary>
/// Per-body simulation state held in the core's working buffer.
/// Blittable POD layout so the same memory can back both managed and native callers.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct BodyState
{
	/// <summary>Caller-assigned stable id. Used to map external updates back to internal indices.</summary>
	public int Id;

	/// <summary>True if pinned (does not integrate but still exerts forces on others).</summary>
	public byte IsPinned;

	/// <summary>True if temporarily frozen (e.g. while the user is dragging). Same integration semantics as pinned.</summary>
	public byte IsFrozen;

	private byte pad0;
	private byte pad1;

	/// <summary>Top-left position in layout space.</summary>
	public Vec2D Position;

	/// <summary>Width/height. Used for centering and source/target ordering checks.</summary>
	public Vec2D Dimensions;

	/// <summary>Current velocity.</summary>
	public Vec2D Velocity;

	/// <summary>Net force accumulator for the current substep.</summary>
	public Vec2D Force;
}

/// <summary>
/// An edge resolved to body indices into the core's working buffer.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct EdgeRef
{
	/// <summary>Index of the source body in the core's working buffer.</summary>
	public int SourceIndex;

	/// <summary>Index of the target body in the core's working buffer.</summary>
	public int TargetIndex;

	/// <summary>Reserved per-edge anisotropy weight. Ignored by V1; populated for future use (e.g. execution vs data pin biasing).</summary>
	public Vec2D Anisotropy;
}
