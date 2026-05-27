// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ForceDirectedLayout;

/// <summary>
/// Managed-surface mirror of <see cref="LayoutSettings"/> with idiomatic .NET semantics
/// (record/with syntax, <c>bool</c> instead of <c>byte</c>, default constructor with defaults).
/// Converts to/from the POD <see cref="LayoutSettings"/> at the core boundary.
/// </summary>
public sealed record PhysicsSettings
{
	/// <summary>Whether simulation is active. When false, Step is a no-op.</summary>
	public bool Enabled { get; init; }

	/// <summary>Strength of pairwise inverse-square repulsion between bodies.</summary>
	public double RepulsionStrength { get; init; } = 1_200_000.0;

	/// <summary>Dimensionless Hooke's-law spring constant for edges.</summary>
	public double LinkSpringStrength { get; init; } = 0.5;

	/// <summary>Strength of the horizontal source-left/target-right ordering bias. 0 disables it.</summary>
	public double DirectionalBias { get; init; } = 0.5;

	/// <summary>Strength of the gravity force pulling each body toward the gravity target.</summary>
	public double GravityStrength { get; init; } = 50.0;

	/// <summary>Blend factor from centroid (0) to world origin (1) for the gravity target.</summary>
	public double OriginAnchorWeight { get; init; } = 1.0;

	/// <summary>Per-second velocity retention. 0.5 means velocity halves every second.</summary>
	public double DampingFactor { get; init; } = 0.5;

	/// <summary>Distance floor used to clamp the inverse-square repulsion denominator.</summary>
	public double MinRepulsionDistance { get; init; } = 50.0;

	/// <summary>Spring rest length for edges.</summary>
	public double RestLinkLength { get; init; } = 225.0;

	/// <summary>Per-body force magnitude cap (applied before integration).</summary>
	public double MaxForce { get; init; } = 5000.0;

	/// <summary>Per-body velocity magnitude cap (applied after integration).</summary>
	public double MaxVelocity { get; init; } = 50.0;

	/// <summary>Target substep frequency. Per-frame substep count is ceil(deltaTime * TargetPhysicsHz).</summary>
	public double TargetPhysicsHz { get; init; } = 120.0;

	/// <summary>System energy threshold below which the simulation reports IsStable.</summary>
	public double StabilityThreshold { get; init; } = 1.0;

	/// <summary>Convert to the POD <see cref="LayoutSettings"/> used by the AOT core.</summary>
	public LayoutSettings ToLayoutSettings() => new()
	{
		Enabled = (byte)(Enabled ? 1 : 0),
		RepulsionStrength = RepulsionStrength,
		LinkSpringStrength = LinkSpringStrength,
		DirectionalBias = DirectionalBias,
		GravityStrength = GravityStrength,
		OriginAnchorWeight = OriginAnchorWeight,
		DampingFactor = DampingFactor,
		MinRepulsionDistance = MinRepulsionDistance,
		RestLinkLength = RestLinkLength,
		MaxForce = MaxForce,
		MaxVelocity = MaxVelocity,
		TargetPhysicsHz = TargetPhysicsHz,
		StabilityThreshold = StabilityThreshold,
	};

	/// <summary>Construct a managed record from the POD <see cref="LayoutSettings"/>.</summary>
	public static PhysicsSettings FromLayoutSettings(in LayoutSettings s) => new()
	{
		Enabled = s.Enabled != 0,
		RepulsionStrength = s.RepulsionStrength,
		LinkSpringStrength = s.LinkSpringStrength,
		DirectionalBias = s.DirectionalBias,
		GravityStrength = s.GravityStrength,
		OriginAnchorWeight = s.OriginAnchorWeight,
		DampingFactor = s.DampingFactor,
		MinRepulsionDistance = s.MinRepulsionDistance,
		RestLinkLength = s.RestLinkLength,
		MaxForce = s.MaxForce,
		MaxVelocity = s.MaxVelocity,
		TargetPhysicsHz = s.TargetPhysicsHz,
		StabilityThreshold = s.StabilityThreshold,
	};
}
