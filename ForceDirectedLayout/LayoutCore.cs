// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ForceDirectedLayout;

using System;

/// <summary>
/// Non-generic, double-precision force-directed simulation core.
/// Operates on contiguous arrays of <see cref="BodyState"/> and <see cref="EdgeRef"/>.
/// Allocation-free in the steady state - all working buffers are pooled for the lifetime of the instance.
/// </summary>
/// <remarks>
/// This is the AOT-friendly algorithm layer. It contains no reflection, no generics, no managed-only types,
/// and no allocations per step. It is the implementation underneath both the generic managed facade
/// (<see cref="ForceDirectedLayout{TBody, TEdge}"/>) and the C ABI exports.
/// </remarks>
public sealed class LayoutCore
{
	private BodyState[] bodies = [];
	private int bodyCount;

	private EdgeRef[] edges = [];
	private int edgeCount;

	/// <summary>Simulation settings. Mutate between frames as needed.</summary>
	public LayoutSettings Settings { get; set; } = LayoutSettings.Defaults;

	/// <summary>World origin in body-position space. Gravity blends toward this point per <see cref="LayoutSettings.OriginAnchorWeight"/>.</summary>
	public Vec2D WorldOrigin { get; set; }

	/// <summary>Last computed gravity target (blend of centroid and world origin). Published for debug rendering.</summary>
	public Vec2D GravityCenter { get; private set; }

	/// <summary>Total kinetic energy in the system after the last <see cref="Step"/>.</summary>
	public double TotalSystemEnergy { get; private set; }

	/// <summary>True when <see cref="TotalSystemEnergy"/> is below <see cref="LayoutSettings.StabilityThreshold"/>.</summary>
	public bool IsStable { get; private set; }

	/// <summary>Substep count and per-substep delta time from the last <see cref="Step"/>.</summary>
	public (int SubstepCount, double SubstepDeltaTime) LastStepInfo { get; private set; }

	/// <summary>Mutable view of the current bodies. Length equals <see cref="BodyCount"/>.</summary>
	public Span<BodyState> Bodies => bodies.AsSpan(0, bodyCount);

	/// <summary>Number of active bodies.</summary>
	public int BodyCount => bodyCount;

	/// <summary>Mutable view of the current edges. Length equals <see cref="EdgeCount"/>.</summary>
	public Span<EdgeRef> Edges => edges.AsSpan(0, edgeCount);

	/// <summary>Number of active edges.</summary>
	public int EdgeCount => edgeCount;

	/// <summary>
	/// Resize the body buffer to hold <paramref name="count"/> entries.
	/// Existing entries (up to the new count) are preserved. Caller is expected to populate <see cref="Bodies"/> after.
	/// </summary>
	public void ResizeBodies(int count)
	{
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(count));
		}

		if (bodies.Length < count)
		{
			Array.Resize(ref bodies, count);
		}
		bodyCount = count;
	}

	/// <summary>
	/// Resize the edge buffer to hold <paramref name="count"/> entries.
	/// Existing entries (up to the new count) are preserved. Caller is expected to populate <see cref="Edges"/> after.
	/// </summary>
	public void ResizeEdges(int count)
	{
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(count));
		}

		if (edges.Length < count)
		{
			Array.Resize(ref edges, count);
		}
		edgeCount = count;
	}

	/// <summary>
	/// Set <see cref="WorldOrigin"/> to the centroid of the current bodies' centers.
	/// </summary>
	public void InitializeWorldOriginToCentroid()
	{
		if (bodyCount == 0)
		{
			WorldOrigin = Vec2D.Zero;
			return;
		}

		Vec2D centroid = Vec2D.Zero;
		for (int i = 0; i < bodyCount; i++)
		{
			centroid += bodies[i].Position + (bodies[i].Dimensions * 0.5);
		}
		WorldOrigin = centroid / bodyCount;
	}

	/// <summary>
	/// Advance the simulation by <paramref name="deltaTime"/> seconds.
	/// </summary>
	public void Step(double deltaTime)
	{
		if (Settings.Enabled == 0 || bodyCount == 0)
		{
			LastStepInfo = (0, 0.0);
			return;
		}

		double targetTimestep = 1.0 / Settings.TargetPhysicsHz;
		int numberOfSubsteps = Math.Max(1, (int)Math.Ceiling(deltaTime / targetTimestep));
		double substepDt = deltaTime / numberOfSubsteps;

		LastStepInfo = (numberOfSubsteps, substepDt);

		for (int substep = 0; substep < numberOfSubsteps; substep++)
		{
			ResetForces();

			CalculateRepulsionForces();
			CalculateLinkForces();
			CalculateDirectionalForces();
			CalculateGravityForces();

			IntegrateMotion(substepDt);

			ApplyDirectionalConstraints();
		}

		double energy = 0.0;
		for (int i = 0; i < bodyCount; i++)
		{
			energy += bodies[i].Velocity.LengthSquared();
		}
		TotalSystemEnergy = energy;
		IsStable = energy < Settings.StabilityThreshold;
	}

	private void ResetForces()
	{
		for (int i = 0; i < bodyCount; i++)
		{
			bodies[i].Force = Vec2D.Zero;
		}
	}

	private void CalculateRepulsionForces()
	{
		double minDist = Settings.MinRepulsionDistance;
		double strength = Settings.RepulsionStrength;

		for (int i = 0; i < bodyCount; i++)
		{
			for (int j = i + 1; j < bodyCount; j++)
			{
				Vec2D aCenter = bodies[i].Position + (bodies[i].Dimensions * 0.5);
				Vec2D bCenter = bodies[j].Position + (bodies[j].Dimensions * 0.5);

				Vec2D direction = aCenter - bCenter;
				double dist = direction.Length();

				if (dist < 0.1)
				{
					continue;
				}

				// Inverse-square, clamped at MinRepulsionDistance to prevent explosions when bodies overlap.
				double effectiveDist = Math.Max(dist, minDist);
				double magnitude = strength / (effectiveDist * effectiveDist);
				Vec2D force = direction * (magnitude / dist);

				bodies[i].Force += force;
				bodies[j].Force -= force;
			}
		}
	}

	private void CalculateLinkForces()
	{
		double restLength = Settings.RestLinkLength;
		double springK = Settings.LinkSpringStrength;

		for (int e = 0; e < edgeCount; e++)
		{
			int s = edges[e].SourceIndex;
			int t = edges[e].TargetIndex;
			if ((uint)s >= (uint)bodyCount || (uint)t >= (uint)bodyCount)
			{
				continue;
			}

			Vec2D sourceCenter = bodies[s].Position + (bodies[s].Dimensions * 0.5);
			Vec2D targetCenter = bodies[t].Position + (bodies[t].Dimensions * 0.5);

			Vec2D direction = targetCenter - sourceCenter;
			double currentLength = direction.Length();
			if (currentLength <= 0.1)
			{
				continue;
			}

			// Hooke's law: force proportional to displacement from rest length.
			double extension = currentLength - restLength;
			double magnitude = springK * extension;
			Vec2D force = direction * (magnitude / currentLength);

			bodies[s].Force += force;
			bodies[t].Force -= force;
		}
	}

	private void CalculateDirectionalForces()
	{
		double bias = Settings.DirectionalBias;
		if (bias <= 0)
		{
			return;
		}

		for (int e = 0; e < edgeCount; e++)
		{
			int s = edges[e].SourceIndex;
			int t = edges[e].TargetIndex;
			if ((uint)s >= (uint)bodyCount || (uint)t >= (uint)bodyCount)
			{
				continue;
			}

			double sourceCenterX = bodies[s].Position.X + (bodies[s].Dimensions.X * 0.5);
			double targetCenterX = bodies[t].Position.X + (bodies[t].Dimensions.X * 0.5);

			double minGap = ((bodies[s].Dimensions.X + bodies[t].Dimensions.X) * 0.5) + 20.0;
			double currentGap = targetCenterX - sourceCenterX;
			double violation = minGap - currentGap;

			if (violation > 0)
			{
				double forceX = bias * violation;
				if (currentGap < 0)
				{
					forceX *= 1.0 + (Math.Abs(currentGap) / minGap);
				}

				bodies[s].Force += new Vec2D(-forceX, 0);
				bodies[t].Force += new Vec2D(forceX, 0);
			}
		}
	}

	private void ApplyDirectionalConstraints()
	{
		double bias = Settings.DirectionalBias;
		if (bias <= 0)
		{
			return;
		}

		for (int e = 0; e < edgeCount; e++)
		{
			int s = edges[e].SourceIndex;
			int t = edges[e].TargetIndex;
			if ((uint)s >= (uint)bodyCount || (uint)t >= (uint)bodyCount)
			{
				continue;
			}

			double sourceCenterX = bodies[s].Position.X + (bodies[s].Dimensions.X * 0.5);
			double targetCenterX = bodies[t].Position.X + (bodies[t].Dimensions.X * 0.5);

			if (sourceCenterX > targetCenterX)
			{
				double overlap = sourceCenterX - targetCenterX;
				double correction = overlap * bias * 0.05;

				bool sourceMovable = bodies[s].IsPinned == 0 && bodies[s].IsFrozen == 0;
				bool targetMovable = bodies[t].IsPinned == 0 && bodies[t].IsFrozen == 0;

				if (sourceMovable && targetMovable)
				{
					bodies[s].Position += new Vec2D(-correction, 0);
					bodies[t].Position += new Vec2D(correction, 0);
				}
				else if (sourceMovable)
				{
					bodies[s].Position += new Vec2D(-correction * 2.0, 0);
				}
				else if (targetMovable)
				{
					bodies[t].Position += new Vec2D(correction * 2.0, 0);
				}
			}
		}
	}

	private void CalculateGravityForces()
	{
		if (bodyCount == 0)
		{
			return;
		}

		Vec2D centroid = Vec2D.Zero;
		for (int i = 0; i < bodyCount; i++)
		{
			centroid += bodies[i].Position + (bodies[i].Dimensions * 0.5);
		}
		centroid /= bodyCount;

		Vec2D gravityTarget = Vec2D.Lerp(centroid, WorldOrigin, Settings.OriginAnchorWeight);
		GravityCenter = gravityTarget;

		double magnitude = Settings.GravityStrength;

		for (int i = 0; i < bodyCount; i++)
		{
			Vec2D nodeCenter = bodies[i].Position + (bodies[i].Dimensions * 0.5);
			Vec2D toCenter = gravityTarget - nodeCenter;
			double distance = toCenter.Length();
			if (distance > 0.1)
			{
				bodies[i].Force += toCenter * (magnitude / distance);
			}
		}
	}

	private void IntegrateMotion(double dt)
	{
		double maxForce = Settings.MaxForce;
		double maxVelocity = Settings.MaxVelocity;
		double dampingPerSubstep = Math.Pow(Settings.DampingFactor, dt);

		for (int i = 0; i < bodyCount; i++)
		{
			ref BodyState body = ref bodies[i];

			if (body.IsPinned != 0 || body.IsFrozen != 0)
			{
				body.Velocity = Vec2D.Zero;
				body.Force = Vec2D.Zero;
				continue;
			}

			Vec2D clampedForce = body.Force;
			double forceLen = clampedForce.Length();
			if (forceLen > maxForce && forceLen > 0)
			{
				clampedForce *= maxForce / forceLen;
			}

			Vec2D newVelocity = body.Velocity + (clampedForce * dt);
			newVelocity *= dampingPerSubstep;

			double velLen = newVelocity.Length();
			if (velLen > maxVelocity && velLen > 0)
			{
				newVelocity *= maxVelocity / velLen;
			}

			body.Position += newVelocity * dt;
			body.Velocity = newVelocity;
			body.Force = clampedForce;
		}
	}

	/// <summary>
	/// Solve to convergence: repeatedly call <see cref="Step"/> with a substep-sized delta time until
	/// the system reports <see cref="IsStable"/> or <paramref name="maxIterations"/> is reached.
	/// </summary>
	/// <param name="maxIterations">Cap on integration ticks. Each tick is a full <see cref="Step"/> call.</param>
	/// <param name="tolerance">If non-zero, overrides <see cref="LayoutSettings.StabilityThreshold"/> for the duration of the call.</param>
	/// <returns>Number of iterations actually executed.</returns>
	public int Solve(int maxIterations, double tolerance)
	{
		double originalThreshold = Settings.StabilityThreshold;
		if (tolerance > 0)
		{
			LayoutSettings s = Settings;
			s.StabilityThreshold = tolerance;
			Settings = s;
		}

		try
		{
			double dt = 1.0 / Settings.TargetPhysicsHz;
			int i;
			for (i = 0; i < maxIterations; i++)
			{
				Step(dt);
				if (IsStable)
				{
					return i + 1;
				}
			}
			return i;
		}
		finally
		{
			if (tolerance > 0)
			{
				LayoutSettings s = Settings;
				s.StabilityThreshold = originalThreshold;
				Settings = s;
			}
		}
	}
}
