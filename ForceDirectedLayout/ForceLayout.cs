// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ForceDirectedLayout;

using System;
using System.Collections.Generic;

/// <summary>
/// POD initial state for a body submitted via <see cref="ForceLayout.SetNodes"/>.
/// Mirrors the C ABI struct used by the native exports.
/// </summary>
[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct NodeInit
{
	/// <summary>Caller-assigned stable id used in edges and external queries.</summary>
	public int Id;

	/// <summary>1 if the body starts pinned, 0 otherwise.</summary>
	public byte IsPinned;

	private byte pad0;
	private byte pad1;
	private byte pad2;

	/// <summary>Initial top-left position in layout space.</summary>
	public Vec2D Position;

	/// <summary>Body width/height. Used for centering and source/target ordering checks.</summary>
	public Vec2D Dimensions;
}

/// <summary>
/// POD initial state for an edge submitted via <see cref="ForceLayout.SetEdges"/>.
/// </summary>
[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct EdgeInit
{
	/// <summary>Id of the source body. Must match a <see cref="NodeInit.Id"/> previously submitted.</summary>
	public int SourceBodyId;

	/// <summary>Id of the target body. Must match a <see cref="NodeInit.Id"/> previously submitted.</summary>
	public int TargetBodyId;

	/// <summary>
	/// Reserved per-edge anisotropy weight. Ignored by V1; populated for future use
	/// (e.g. execution vs data pin biasing in node-editor consumers).
	/// </summary>
	public Vec2D Anisotropy;
}

/// <summary>
/// POD output entry returned by <see cref="ForceLayout.GetPositions"/>.
/// </summary>
[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct NodePosition
{
	/// <summary>Caller-assigned id matching the originating <see cref="NodeInit.Id"/>.</summary>
	public int Id;

	private int pad0;

	/// <summary>Current top-left position in layout space.</summary>
	public Vec2D Position;

	/// <summary>Current velocity.</summary>
	public Vec2D Velocity;
}

/// <summary>
/// Non-generic, double-precision force-directed layout. The idiomatic .NET surface over <see cref="LayoutCore"/>,
/// and the same code path the native C ABI exports drive. Accepts POD <see cref="NodeInit"/>/<see cref="EdgeInit"/>
/// for bulk submission and returns positions as <see cref="ReadOnlySpan{T}"/> for zero-copy reads.
/// </summary>
public sealed class ForceLayout
{
	private readonly LayoutCore core = new();
	private readonly Dictionary<int, int> idToIndex = [];
	private NodePosition[] positionBuffer = [];

	/// <summary>Construct with default settings.</summary>
	public ForceLayout()
	{
	}

	/// <summary>Construct with the given settings.</summary>
	public ForceLayout(LayoutSettings settings)
	{
		core.Settings = settings;
	}

	/// <summary>Raw POD simulation settings. Replaceable at any time.</summary>
	public LayoutSettings Settings
	{
		get => core.Settings;
		set => core.Settings = value;
	}

	/// <summary>World origin used by the gravity term.</summary>
	public Vec2D WorldOrigin
	{
		get => core.WorldOrigin;
		set => core.WorldOrigin = value;
	}

	/// <summary>Last computed gravity target (centroid blended toward world origin).</summary>
	public Vec2D GravityCenter => core.GravityCenter;

	/// <summary>True when total system energy is below the stability threshold.</summary>
	public bool IsStable => core.IsStable;

	/// <summary>Sum of |velocity|² across all bodies after the last <see cref="Step"/>.</summary>
	public double TotalSystemEnergy => core.TotalSystemEnergy;

	/// <summary>Substep count and per-substep delta time from the last <see cref="Step"/>.</summary>
	public (int SubstepCount, double SubstepDeltaTime) LastStepInfo => core.LastStepInfo;

	/// <summary>Number of bodies currently in the simulation.</summary>
	public int NodeCount => core.BodyCount;

	/// <summary>Number of edges currently in the simulation.</summary>
	public int EdgeCount => core.EdgeCount;

	/// <summary>
	/// Replace the body set with the given initial state. Ids inside <paramref name="nodes"/> must be unique.
	/// </summary>
	public void SetNodes(ReadOnlySpan<NodeInit> nodes)
	{
		core.ResizeBodies(nodes.Length);
		Span<BodyState> dst = core.Bodies;
		idToIndex.Clear();

		for (int i = 0; i < nodes.Length; i++)
		{
			NodeInit init = nodes[i];
			dst[i] = new BodyState
			{
				Id = init.Id,
				IsPinned = init.IsPinned,
				IsFrozen = 0,
				Position = init.Position,
				Dimensions = init.Dimensions,
				Velocity = Vec2D.Zero,
				Force = Vec2D.Zero,
			};
			idToIndex[init.Id] = i;
		}

		// Drop any edges whose endpoints no longer exist - they would silently get pruned next Step otherwise.
		Span<EdgeRef> edgeBuf = core.Edges;
		for (int i = 0; i < edgeBuf.Length; i++)
		{
			if ((uint)edgeBuf[i].SourceIndex >= (uint)nodes.Length || (uint)edgeBuf[i].TargetIndex >= (uint)nodes.Length)
			{
				edgeBuf[i].SourceIndex = -1;
				edgeBuf[i].TargetIndex = -1;
			}
		}
	}

	/// <summary>
	/// Replace the edge set. Each edge's source and target ids must reference bodies submitted to <see cref="SetNodes"/>.
	/// </summary>
	public void SetEdges(ReadOnlySpan<EdgeInit> edges)
	{
		core.ResizeEdges(edges.Length);
		Span<EdgeRef> dst = core.Edges;

		for (int i = 0; i < edges.Length; i++)
		{
			EdgeInit init = edges[i];
			int sourceIndex = idToIndex.TryGetValue(init.SourceBodyId, out int s) ? s : -1;
			int targetIndex = idToIndex.TryGetValue(init.TargetBodyId, out int t) ? t : -1;
			dst[i] = new EdgeRef
			{
				SourceIndex = sourceIndex,
				TargetIndex = targetIndex,
				Anisotropy = init.Anisotropy,
			};
		}
	}

	/// <summary>Advance the simulation by <paramref name="deltaTime"/> seconds.</summary>
	public void Step(double deltaTime) => core.Step(deltaTime);

	/// <summary>
	/// Solve to convergence (or until <paramref name="maxIterations"/> ticks are exhausted).
	/// Each iteration is a full <see cref="Step"/> at the substep-sized timestep.
	/// </summary>
	/// <returns>Number of iterations actually executed.</returns>
	public int Solve(int maxIterations, double tolerance) => core.Solve(maxIterations, tolerance);

	/// <summary>
	/// Pin or unpin a body by index. Pinned bodies do not integrate but still exert forces on others.
	/// </summary>
	public void SetPinned(int nodeIndex, bool pinned)
	{
		if ((uint)nodeIndex >= (uint)core.BodyCount)
		{
			throw new ArgumentOutOfRangeException(nameof(nodeIndex));
		}
		core.Bodies[nodeIndex].IsPinned = pinned ? (byte)1 : (byte)0;
	}

	/// <summary>Set the dragged/frozen flag on a body. Same integration semantics as pinned.</summary>
	public void SetFrozen(int nodeIndex, bool frozen)
	{
		if ((uint)nodeIndex >= (uint)core.BodyCount)
		{
			throw new ArgumentOutOfRangeException(nameof(nodeIndex));
		}
		core.Bodies[nodeIndex].IsFrozen = frozen ? (byte)1 : (byte)0;
	}

	/// <summary>Override the position of a body (e.g. while the user drags it).</summary>
	public void SetPosition(int nodeIndex, Vec2D position)
	{
		if ((uint)nodeIndex >= (uint)core.BodyCount)
		{
			throw new ArgumentOutOfRangeException(nameof(nodeIndex));
		}
		core.Bodies[nodeIndex].Position = position;
	}

	/// <summary>Look up the working-buffer index of a body by its caller-assigned id, or -1 if unknown.</summary>
	public int GetIndexOf(int nodeId) => idToIndex.TryGetValue(nodeId, out int i) ? i : -1;

	/// <summary>
	/// Read current positions into a caller-owned buffer. The buffer must be at least <see cref="NodeCount"/> long.
	/// </summary>
	/// <returns>The number of entries written.</returns>
	public int GetPositions(Span<NodePosition> destination)
	{
		ReadOnlySpan<BodyState> src = core.Bodies;
		if (destination.Length < src.Length)
		{
			throw new ArgumentException("Destination buffer is smaller than NodeCount.", nameof(destination));
		}

		for (int i = 0; i < src.Length; i++)
		{
			destination[i] = new NodePosition
			{
				Id = src[i].Id,
				Position = src[i].Position,
				Velocity = src[i].Velocity,
			};
		}
		return src.Length;
	}

	/// <summary>
	/// Read current positions into an internal buffer and return a span over it.
	/// The span is valid until the next call to <see cref="GetPositionsView"/> or until the body set is resized.
	/// </summary>
	public ReadOnlySpan<NodePosition> GetPositionsView()
	{
		if (positionBuffer.Length < core.BodyCount)
		{
			positionBuffer = new NodePosition[core.BodyCount];
		}
		GetPositions(positionBuffer.AsSpan(0, core.BodyCount));
		return positionBuffer.AsSpan(0, core.BodyCount);
	}

	/// <summary>Set the world origin to the centroid of the current body centers.</summary>
	public void InitializeWorldOriginToCentroid() => core.InitializeWorldOriginToCentroid();
}
