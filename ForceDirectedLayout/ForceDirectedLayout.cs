// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ForceDirectedLayout;

using System;
using System.Collections.Generic;
using System.Numerics;
using ktsu.Semantics;

/// <summary>
/// Renderer-agnostic force-directed graph layout simulation.
/// Operates on caller-defined body and edge types through accessor adapters.
/// </summary>
/// <typeparam name="TBody">Caller-defined body type. Must expose physics state via <see cref="BodyAccessor{TBody}"/>.</typeparam>
/// <typeparam name="TEdge">Caller-defined edge type. Must resolve to a pair of body ids via <see cref="EdgeAccessor{TEdge}"/>.</typeparam>
/// <remarks>
/// Create a layout simulation that reads and writes body state through the given accessors.
/// </remarks>
public class ForceDirectedLayout<TBody, TEdge>(BodyAccessor<TBody> bodyAccessor, EdgeAccessor<TEdge> edgeAccessor)
{
	private readonly BodyAccessor<TBody> bodyAccessor = bodyAccessor ?? throw new ArgumentNullException(nameof(bodyAccessor));
	private readonly EdgeAccessor<TEdge> edgeAccessor = edgeAccessor ?? throw new ArgumentNullException(nameof(edgeAccessor));
	private readonly HashSet<int> frozenBodyIds = [];

	// Working state - reused across Step calls to avoid per-frame allocations.
	private BodyState[] working = [];
	private int workingCount;
	private readonly Dictionary<int, int> idToWorkingIndex = [];

	/// <summary>Tunable simulation parameters. Mutate freely between frames.</summary>
	public PhysicsSettings Settings { get; set; } = new();

	/// <summary>
	/// World origin in body-position space. Gravity blends toward this point per <see cref="PhysicsSettings.OriginAnchorWeight"/>.
	/// Track this with uniform body shifts (panning) so it stays in the same coordinate space as body positions.
	/// </summary>
	public Vector2 WorldOrigin { get; set; } = Vector2.Zero;

	/// <summary>Last computed gravity target (blend of centroid and world origin). Published for debug rendering.</summary>
	public Vector2 GravityCenter { get; private set; } = Vector2.Zero;

	/// <summary>Total kinetic energy in the system after the last <see cref="Step"/> (sum of |velocity|² across bodies).</summary>
	public float TotalSystemEnergy { get; private set; }

	/// <summary>True when <see cref="TotalSystemEnergy"/> is below <see cref="PhysicsSettings.StabilityThreshold"/>.</summary>
	public bool IsStable { get; private set; }

	/// <summary>Substep count and per-substep delta time from the last <see cref="Step"/>. For debug display.</summary>
	public (int SubstepCount, float SubstepDeltaTime) LastStepInfo { get; private set; }

	/// <summary>
	/// Mark bodies that should be temporarily excluded from integration (e.g. while the user is dragging them).
	/// Frozen bodies still exert forces on others. Pinned bodies (see <see cref="BodyAccessor{TBody}.GetIsPinned"/>) behave the same way.
	/// </summary>
	public void SetFrozenBodies(IReadOnlySet<int> bodyIds)
	{
		frozenBodyIds.Clear();
		foreach (int id in bodyIds)
		{
			frozenBodyIds.Add(id);
		}
	}

	/// <summary>
	/// Initialize <see cref="WorldOrigin"/> to the centroid of the supplied bodies.
	/// Call after populating bodies so gravity doesn't immediately pull them toward (0,0).
	/// </summary>
	public void InitializeWorldOriginToCentroid(IReadOnlyList<TBody> bodies)
	{
		if (bodies.Count == 0)
		{
			WorldOrigin = Vector2.Zero;
			return;
		}

		Vector2 centroid = Vector2.Zero;
		for (int i = 0; i < bodies.Count; i++)
		{
			centroid += bodyAccessor.GetPosition(bodies[i]) + (bodyAccessor.GetDimensions(bodies[i]) * 0.5f);
		}
		WorldOrigin = centroid / bodies.Count;
	}

	/// <summary>
	/// Run one frame of simulation. Snapshots body state into a working buffer, performs N substeps
	/// targeting <see cref="PhysicsSettings.TargetPhysicsHz"/>, then writes results back through
	/// <see cref="BodyAccessor{TBody}.WithPhysicsState"/>. The supplied <paramref name="bodies"/> list is mutated in place.
	/// </summary>
	/// <param name="bodies">Bodies in the simulation. Mutated in place via <see cref="BodyAccessor{TBody}.WithPhysicsState"/>.</param>
	/// <param name="edges">Edges in the simulation. Each edge must reference body ids present in <paramref name="bodies"/>; unknown ids are skipped.</param>
	/// <param name="deltaTime">Wall-clock elapsed time since the previous frame, in seconds.</param>
	public void Step(IList<TBody> bodies, IReadOnlyList<TEdge> edges, float deltaTime)
	{
		ArgumentNullException.ThrowIfNull(bodies);
		ArgumentNullException.ThrowIfNull(edges);

		if (!Settings.Enabled || bodies.Count == 0)
		{
			LastStepInfo = (0, 0.0f);
			return;
		}

		SnapshotBodies(bodies);

		// Calculate substeps to achieve target physics frequency
		Time<float> frameDeltaTime = Time<float>.FromSeconds(deltaTime);
		Time<float> targetTimestep = 1.0f / Settings.TargetPhysicsHz;

		int numberOfSubsteps = Math.Max(1, (int)Math.Ceiling(frameDeltaTime.In(Units.Second) / targetTimestep.In(Units.Second)));
		Time<float> substepDeltaTime = Time<float>.FromSeconds(deltaTime / numberOfSubsteps);

		LastStepInfo = (numberOfSubsteps, substepDeltaTime.In(Units.Second));

		for (int substep = 0; substep < numberOfSubsteps; substep++)
		{
			ResetForces();

			CalculateRepulsionForces();
			CalculateLinkForces(edges);
			CalculateDirectionalForces(edges);
			CalculateGravityForces();

			IntegrateMotion(substepDeltaTime);

			ApplyDirectionalConstraints(edges);
		}

		TotalSystemEnergy = 0.0f;
		for (int i = 0; i < workingCount; i++)
		{
			TotalSystemEnergy += working[i].Velocity.LengthSquared();
		}
		IsStable = TotalSystemEnergy < Settings.StabilityThreshold;

		CommitBodies(bodies);
	}

	private void SnapshotBodies(IList<TBody> bodies)
	{
		workingCount = bodies.Count;
		if (working.Length < workingCount)
		{
			working = new BodyState[workingCount];
		}
		idToWorkingIndex.Clear();

		for (int i = 0; i < workingCount; i++)
		{
			TBody body = bodies[i];
			int id = bodyAccessor.GetId(body);
			working[i] = new BodyState
			{
				Id = id,
				Position = bodyAccessor.GetPosition(body),
				Dimensions = bodyAccessor.GetDimensions(body),
				Velocity = bodyAccessor.GetVelocity(body),
				Force = Vector2.Zero,
				IsPinned = bodyAccessor.GetIsPinned(body),
				IsFrozen = frozenBodyIds.Contains(id),
			};
			idToWorkingIndex[id] = i;
		}
	}

	private void CommitBodies(IList<TBody> bodies)
	{
		for (int i = 0; i < workingCount; i++)
		{
			ref BodyState state = ref working[i];
			bodies[i] = bodyAccessor.WithPhysicsState(bodies[i], state.Position, state.Velocity, state.Force);
		}
	}

	private void ResetForces()
	{
		for (int i = 0; i < workingCount; i++)
		{
			working[i].Force = Vector2.Zero;
		}
	}

	private void CalculateRepulsionForces()
	{
		float minDist = Settings.MinRepulsionDistance.In(Units.Meter);
		float strength = Settings.RepulsionStrength.In(Units.Newton);

		for (int i = 0; i < workingCount; i++)
		{
			for (int j = i + 1; j < workingCount; j++)
			{
				Vector2 aCenter = working[i].Position + (working[i].Dimensions * 0.5f);
				Vector2 bCenter = working[j].Position + (working[j].Dimensions * 0.5f);

				Vector2 direction = aCenter - bCenter;
				float dist = direction.Length();

				if (dist < 0.1f)
				{
					continue;
				}

				Vector2 normalizedDirection = direction / dist;

				// Inverse-square repulsion, clamped at MinRepulsionDistance to prevent explosions when bodies overlap.
				float effectiveDist = MathF.Max(dist, minDist);
				float magnitude = strength / (effectiveDist * effectiveDist);
				Vector2 repulsionForce = normalizedDirection * magnitude;

				working[i].Force += repulsionForce;
				working[j].Force -= repulsionForce;
			}
		}
	}

	private void CalculateLinkForces(IReadOnlyList<TEdge> edges)
	{
		float restLength = Settings.RestLinkLength.In(Units.Meter);

		for (int e = 0; e < edges.Count; e++)
		{
			TEdge edge = edges[e];
			if (!idToWorkingIndex.TryGetValue(edgeAccessor.GetSourceBodyId(edge), out int sourceIndex) ||
				!idToWorkingIndex.TryGetValue(edgeAccessor.GetTargetBodyId(edge), out int targetIndex))
			{
				continue;
			}

			Vector2 sourceCenter = working[sourceIndex].Position + (working[sourceIndex].Dimensions * 0.5f);
			Vector2 targetCenter = working[targetIndex].Position + (working[targetIndex].Dimensions * 0.5f);

			Vector2 direction = targetCenter - sourceCenter;
			float currentLength = direction.Length();

			if (currentLength <= 0.1f)
			{
				continue;
			}

			Vector2 normalizedDirection = direction / currentLength;

			// Hooke's law: force proportional to displacement from rest length.
			float extension = currentLength - restLength;
			float springForceMagnitude = Settings.LinkSpringStrength * extension;
			Vector2 springForce = normalizedDirection * springForceMagnitude;

			working[sourceIndex].Force += springForce;
			working[targetIndex].Force -= springForce;
		}
	}

	private void CalculateDirectionalForces(IReadOnlyList<TEdge> edges)
	{
		// Bias linked source/target bodies to flow source-left, target-right.
		// Only push when bodies overlap horizontally or are in the wrong order,
		// so middle-chain bodies don't receive conflicting pulls from correctly-ordered edges.
		float bias = Settings.DirectionalBias;
		if (bias <= 0)
		{
			return;
		}

		for (int e = 0; e < edges.Count; e++)
		{
			TEdge edge = edges[e];
			if (!idToWorkingIndex.TryGetValue(edgeAccessor.GetSourceBodyId(edge), out int sourceIndex) ||
				!idToWorkingIndex.TryGetValue(edgeAccessor.GetTargetBodyId(edge), out int targetIndex))
			{
				continue;
			}

			float sourceCenterX = working[sourceIndex].Position.X + (working[sourceIndex].Dimensions.X * 0.5f);
			float targetCenterX = working[targetIndex].Position.X + (working[targetIndex].Dimensions.X * 0.5f);

			// Required gap: source's right edge clears target's left edge plus a small margin.
			float minGap = ((working[sourceIndex].Dimensions.X + working[targetIndex].Dimensions.X) * 0.5f) + 20.0f;
			float currentGap = targetCenterX - sourceCenterX;
			float violation = minGap - currentGap;

			if (violation > 0)
			{
				float forceX = bias * violation;

				// Stronger boost when source is to the right of target (wrong direction).
				if (currentGap < 0)
				{
					forceX *= 1.0f + (MathF.Abs(currentGap) / minGap);
				}

				working[sourceIndex].Force += new Vector2(-forceX, 0);
				working[targetIndex].Force += new Vector2(forceX, 0);
			}
		}
	}

	private void ApplyDirectionalConstraints(IReadOnlyList<TEdge> edges)
	{
		// Position-based correction applied after integration. Bypasses MaxForce so
		// source-left/target-right ordering still converges under strong repulsion.
		float bias = Settings.DirectionalBias;
		if (bias <= 0)
		{
			return;
		}

		for (int e = 0; e < edges.Count; e++)
		{
			TEdge edge = edges[e];
			if (!idToWorkingIndex.TryGetValue(edgeAccessor.GetSourceBodyId(edge), out int sourceIndex) ||
				!idToWorkingIndex.TryGetValue(edgeAccessor.GetTargetBodyId(edge), out int targetIndex))
			{
				continue;
			}

			float sourceCenterX = working[sourceIndex].Position.X + (working[sourceIndex].Dimensions.X * 0.5f);
			float targetCenterX = working[targetIndex].Position.X + (working[targetIndex].Dimensions.X * 0.5f);

			if (sourceCenterX > targetCenterX)
			{
				float overlap = sourceCenterX - targetCenterX;
				float correction = overlap * bias * 0.05f;

				bool sourceMovable = !working[sourceIndex].IsPinned && !working[sourceIndex].IsFrozen;
				bool targetMovable = !working[targetIndex].IsPinned && !working[targetIndex].IsFrozen;

				if (sourceMovable && targetMovable)
				{
					working[sourceIndex].Position += new Vector2(-correction, 0);
					working[targetIndex].Position += new Vector2(correction, 0);
				}
				else if (sourceMovable)
				{
					working[sourceIndex].Position += new Vector2(-correction * 2.0f, 0);
				}
				else if (targetMovable)
				{
					working[targetIndex].Position += new Vector2(correction * 2.0f, 0);
				}
			}
		}
	}

	private void CalculateGravityForces()
	{
		if (workingCount == 0)
		{
			return;
		}

		// Recompute centroid each step so gravity tracks the current cluster.
		Vector2 centroid = Vector2.Zero;
		for (int i = 0; i < workingCount; i++)
		{
			centroid += working[i].Position + (working[i].Dimensions * 0.5f);
		}
		centroid /= workingCount;

		Vector2 gravityTarget = Vector2.Lerp(centroid, WorldOrigin, Settings.OriginAnchorWeight);
		GravityCenter = gravityTarget;

		float magnitude = Settings.GravityStrength.In(Units.Newton);

		for (int i = 0; i < workingCount; i++)
		{
			Vector2 nodeCenter = working[i].Position + (working[i].Dimensions * 0.5f);
			Vector2 directionToCenter = gravityTarget - nodeCenter;
			float distance = directionToCenter.Length();

			if (distance > 0.1f)
			{
				working[i].Force += directionToCenter / distance * magnitude;
			}
		}
	}

	private void IntegrateMotion(Time<float> deltaTime)
	{
		float dt = deltaTime.In(Units.Second);
		float maxForce = Settings.MaxForce.In(Units.Newton);
		float maxVelocity = Settings.MaxVelocity.In(Units.MetersPerSecond);

		// Damping is per-second velocity retention, so raise to dt to stay time-independent across substep counts.
		float dampingPerSubstep = MathF.Pow(Settings.DampingFactor, dt);

		for (int i = 0; i < workingCount; i++)
		{
			ref BodyState body = ref working[i];

			// Pinned or frozen bodies still exert forces but don't integrate.
			if (body.IsPinned || body.IsFrozen)
			{
				body.Velocity = Vector2.Zero;
				body.Force = Vector2.Zero;
				continue;
			}

			Vector2 clampedForce = body.Force;
			float forceLen = clampedForce.Length();
			if (forceLen > maxForce && forceLen > 0)
			{
				clampedForce *= maxForce / forceLen;
			}

			// F = ma with m = 1 kg.
			Vector2 newVelocity = body.Velocity + (clampedForce * dt);
			newVelocity *= dampingPerSubstep;

			float velLen = newVelocity.Length();
			if (velLen > maxVelocity && velLen > 0)
			{
				newVelocity *= maxVelocity / velLen;
			}

			body.Position += newVelocity * dt;
			body.Velocity = newVelocity;
			body.Force = clampedForce;
		}
	}

	private struct BodyState
	{
		public int Id;
		public Vector2 Position;
		public Vector2 Dimensions;
		public Vector2 Velocity;
		public Vector2 Force;
		public bool IsPinned;
		public bool IsFrozen;
	}
}
