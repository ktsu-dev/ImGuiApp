// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ForceDirectedLayout;

using System;
using System.Collections.Generic;

/// <summary>
/// Generic, renderer-agnostic facade over <see cref="LayoutCore"/>.
/// Adapts arbitrary caller-defined body and edge types into the core's flat working buffers via
/// <see cref="BodyAccessor{TBody}"/> and <see cref="EdgeAccessor{TEdge}"/>.
/// </summary>
/// <typeparam name="TBody">Caller-defined body type. Must expose physics state via <see cref="BodyAccessor{TBody}"/>.</typeparam>
/// <typeparam name="TEdge">Caller-defined edge type. Must resolve to a pair of body ids via <see cref="EdgeAccessor{TEdge}"/>.</typeparam>
public class ForceDirectedLayout<TBody, TEdge>
{
	private readonly BodyAccessor<TBody> bodyAccessor;
	private readonly EdgeAccessor<TEdge> edgeAccessor;
	private readonly HashSet<int> frozenBodyIds = [];
	private readonly Dictionary<int, int> idToWorkingIndex = [];
	private readonly LayoutCore core = new();
	private PhysicsSettings settings = new();

	/// <summary>Create a layout simulation that reads and writes body state through the given accessors.</summary>
	public ForceDirectedLayout(BodyAccessor<TBody> bodyAccessor, EdgeAccessor<TEdge> edgeAccessor)
	{
		this.bodyAccessor = bodyAccessor ?? throw new ArgumentNullException(nameof(bodyAccessor));
		this.edgeAccessor = edgeAccessor ?? throw new ArgumentNullException(nameof(edgeAccessor));
		core.Settings = settings.ToLayoutSettings();
	}

	/// <summary>Managed-facing physics settings. Mutate freely between frames.</summary>
	public PhysicsSettings Settings
	{
		get => settings;
		set
		{
			settings = value;
			core.Settings = value.ToLayoutSettings();
		}
	}

	/// <summary>World origin in body-position space. Gravity blends toward this per <see cref="PhysicsSettings.OriginAnchorWeight"/>.</summary>
	public Vec2D WorldOrigin
	{
		get => core.WorldOrigin;
		set => core.WorldOrigin = value;
	}

	/// <summary>Last computed gravity target.</summary>
	public Vec2D GravityCenter => core.GravityCenter;

	/// <summary>Total kinetic energy after the last <see cref="Step"/>.</summary>
	public double TotalSystemEnergy => core.TotalSystemEnergy;

	/// <summary>True when <see cref="TotalSystemEnergy"/> is below <see cref="PhysicsSettings.StabilityThreshold"/>.</summary>
	public bool IsStable => core.IsStable;

	/// <summary>Substep count and per-substep delta time from the last <see cref="Step"/>.</summary>
	public (int SubstepCount, double SubstepDeltaTime) LastStepInfo => core.LastStepInfo;

	/// <summary>Mark bodies temporarily excluded from integration (e.g. while being dragged).</summary>
	public void SetFrozenBodies(IReadOnlySet<int> bodyIds)
	{
		frozenBodyIds.Clear();
		foreach (int id in bodyIds)
		{
			frozenBodyIds.Add(id);
		}
	}

	/// <summary>Initialize <see cref="WorldOrigin"/> to the centroid of the supplied bodies.</summary>
	public void InitializeWorldOriginToCentroid(IReadOnlyList<TBody> bodies)
	{
		if (bodies.Count == 0)
		{
			core.WorldOrigin = Vec2D.Zero;
			return;
		}

		Vec2D centroid = Vec2D.Zero;
		for (int i = 0; i < bodies.Count; i++)
		{
			centroid += bodyAccessor.GetPosition(bodies[i]) + (bodyAccessor.GetDimensions(bodies[i]) * 0.5);
		}
		core.WorldOrigin = centroid / bodies.Count;
	}

	/// <summary>
	/// Run one frame of simulation. Snapshots body state into the core's working buffer,
	/// steps the core, then writes results back via <see cref="BodyAccessor{TBody}.WithPhysicsState"/>.
	/// </summary>
	public void Step(IList<TBody> bodies, IReadOnlyList<TEdge> edges, double deltaTime)
	{
		ArgumentNullException.ThrowIfNull(bodies);
		ArgumentNullException.ThrowIfNull(edges);

		SnapshotBodies(bodies);
		SnapshotEdges(edges);
		core.Step(deltaTime);
		CommitBodies(bodies);
	}

	private void SnapshotBodies(IList<TBody> bodies)
	{
		core.ResizeBodies(bodies.Count);
		Span<BodyState> buf = core.Bodies;
		idToWorkingIndex.Clear();

		for (int i = 0; i < bodies.Count; i++)
		{
			TBody body = bodies[i];
			int id = bodyAccessor.GetId(body);
			buf[i] = new BodyState
			{
				Id = id,
				Position = bodyAccessor.GetPosition(body),
				Dimensions = bodyAccessor.GetDimensions(body),
				Velocity = bodyAccessor.GetVelocity(body),
				Force = Vec2D.Zero,
				IsPinned = bodyAccessor.GetIsPinned(body) ? (byte)1 : (byte)0,
				IsFrozen = frozenBodyIds.Contains(id) ? (byte)1 : (byte)0,
			};
			idToWorkingIndex[id] = i;
		}
	}

	private void SnapshotEdges(IReadOnlyList<TEdge> edges)
	{
		core.ResizeEdges(edges.Count);
		Span<EdgeRef> buf = core.Edges;

		for (int i = 0; i < edges.Count; i++)
		{
			TEdge edge = edges[i];
			int sourceId = edgeAccessor.GetSourceBodyId(edge);
			int targetId = edgeAccessor.GetTargetBodyId(edge);

			if (!idToWorkingIndex.TryGetValue(sourceId, out int sourceIndex))
			{
				sourceIndex = -1;
			}
			if (!idToWorkingIndex.TryGetValue(targetId, out int targetIndex))
			{
				targetIndex = -1;
			}

			buf[i] = new EdgeRef
			{
				SourceIndex = sourceIndex,
				TargetIndex = targetIndex,
				Anisotropy = Vec2D.Zero,
			};
		}
	}

	private void CommitBodies(IList<TBody> bodies)
	{
		ReadOnlySpan<BodyState> buf = core.Bodies;
		for (int i = 0; i < bodies.Count; i++)
		{
			bodies[i] = bodyAccessor.WithPhysicsState(bodies[i], buf[i].Position, buf[i].Velocity, buf[i].Force);
		}
	}
}
