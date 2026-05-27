// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ForceDirectedLayout;

using ktsu.Semantics;

/// <summary>
/// Tunable parameters for a force-directed layout simulation.
/// </summary>
public record PhysicsSettings
{
	/// <summary>Whether simulation is active. When false, <see cref="ForceDirectedLayout{TBody, TEdge}.Step"/> is a no-op.</summary>
	public bool Enabled { get; init; }

	/// <summary>Strength of pairwise inverse-square repulsion between bodies.</summary>
	public Force<float> RepulsionStrength { get; init; } = Force<float>.FromNewtons(1_200_000.0f);

	/// <summary>Dimensionless Hooke's-law spring constant for edges.</summary>
	public float LinkSpringStrength { get; init; } = 0.5f;

	/// <summary>Strength of the horizontal source-left/target-right ordering bias along edges. 0 disables it.</summary>
	public float DirectionalBias { get; init; } = 0.5f;

	/// <summary>Strength of the gravity force pulling each body toward the gravity target.</summary>
	public Force<float> GravityStrength { get; init; } = Force<float>.FromNewtons(50.0f);

	/// <summary>Blend factor from centroid (0) to world origin (1) for the gravity target.</summary>
	public float OriginAnchorWeight { get; init; } = 1.0f;

	/// <summary>Per-second velocity retention. 0.5 means velocity halves every second (time-independent).</summary>
	public float DampingFactor { get; init; } = 0.5f;

	/// <summary>Distance floor used to clamp the inverse-square repulsion denominator. Prevents force explosions when bodies overlap.</summary>
	public Length<float> MinRepulsionDistance { get; init; } = Length<float>.FromMeters(50.0f);

	/// <summary>Spring rest length for edges.</summary>
	public Length<float> RestLinkLength { get; init; } = Length<float>.FromMeters(225.0f);

	/// <summary>Per-body force magnitude cap (applied before integration).</summary>
	public Force<float> MaxForce { get; init; } = Force<float>.FromNewtons(5000.0f);

	/// <summary>Per-body velocity magnitude cap (applied after integration).</summary>
	public Velocity<float> MaxVelocity { get; init; } = Velocity<float>.FromMetersPerSecond(50.0f);

	/// <summary>Target substep frequency. Per-frame substep count is ceil(frameDeltaTime * TargetPhysicsHz).</summary>
	public Frequency<float> TargetPhysicsHz { get; init; } = Frequency<float>.FromHertz(120.0f);

	/// <summary>System energy threshold below which <see cref="ForceDirectedLayout{TBody, TEdge}.IsStable"/> reports true.</summary>
	public float StabilityThreshold { get; init; } = 1.0f;
}
