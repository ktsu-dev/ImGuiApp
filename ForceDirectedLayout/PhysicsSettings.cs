// Copyright (c) 2023-2026 ktsu-dev contributors

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

	/// <summary>
	/// Strength of pairwise inverse-square repulsion between bodies, measured across the clear space
	/// between their bounding boxes rather than between their centres.
	/// </summary>
	public double RepulsionStrength { get; init; } = 600_000.0;

	/// <summary>Dimensionless Hooke's-law spring constant for edges.</summary>
	public double LinkSpringStrength { get; init; } = 0.5;

	/// <summary>Strength of the horizontal source-left/target-right ordering bias. 0 disables it.</summary>
	public double DirectionalBias { get; init; } = 0.5;

	/// <summary>
	/// Strength of the preference for horizontal edges: it both levels an edge's two ends and, when the
	/// rendered curve would otherwise hide, splays them apart horizontally. 0 disables both.
	/// See <see cref="LayoutCore.BezierClearanceRatio"/> for the clearance geometry.
	/// </summary>
	public double LinkFlatteningStrength { get; init; } = 0.5;

	/// <summary>Extra horizontal clearance demanded on top of the derived bezier bound, in position units.</summary>
	public double LinkFlatteningMargin { get; init; }

	/// <summary>
	/// Strength of the force that puts two links sharing a node into the same vertical order as the pins
	/// they attach to, so they stop crossing each other. 0 disables it.
	/// </summary>
	public double LinkUntwistStrength { get; init; } = 0.1;

	/// <summary>Strength of the gravity force pulling each body toward the gravity target.</summary>
	public double GravityStrength { get; init; } = 50.0;

	/// <summary>Blend factor from centroid (0) to world origin (1) for the gravity target.</summary>
	public double OriginAnchorWeight { get; init; } = 1.0;

	/// <summary>Per-second velocity retention. 0.5 means velocity halves every second.</summary>
	public double DampingFactor { get; init; } = 0.5;

	/// <summary>
	/// Floor on the clear space used as the inverse-square repulsion denominator, so a pair that touches
	/// pushes hard rather than infinitely hard.
	/// </summary>
	public double MinRepulsionDistance { get; init; } = 50.0;

	/// <summary>Spring rest length for edges.</summary>
	public double RestLinkLength { get; init; } = 225.0;

	/// <summary>Per-body force magnitude cap (applied before integration).</summary>
	public double MaxForce { get; init; } = 5000.0;

	/// <summary>
	/// Per-body velocity magnitude cap (applied after integration). This is what bounds how long a
	/// graph takes to settle: a body has to travel hundreds of units to reach its place, so too low a
	/// cap leaves a graph still visibly unfolding many seconds after it opens.
	/// </summary>
	public double MaxVelocity { get; init; } = 250.0;

	/// <summary>Target substep frequency. Per-frame substep count is ceil(deltaTime * TargetPhysicsHz).</summary>
	public double TargetPhysicsHz { get; init; } = 120.0;

	/// <summary>System energy threshold below which the simulation reports IsStable.</summary>
	public double StabilityThreshold { get; init; } = 1.0;

	/// <summary>Clear space kept between body rectangles by the overlap pass. 0 disables the pass.</summary>
	public double OverlapMargin { get; init; } = 20.0;

	/// <summary>Per-substep cap on how far an overlapping pair is pushed apart.</summary>
	public double MaxOverlapCorrection { get; init; } = 40.0;

	/// <summary>Convert to the POD <see cref="LayoutSettings"/> used by the AOT core.</summary>
	public LayoutSettings ToLayoutSettings() => new()
	{
		Enabled = (byte)(Enabled ? 1 : 0),
		RepulsionStrength = RepulsionStrength,
		LinkSpringStrength = LinkSpringStrength,
		DirectionalBias = DirectionalBias,
		LinkFlatteningStrength = LinkFlatteningStrength,
		LinkFlatteningMargin = LinkFlatteningMargin,
		LinkUntwistStrength = LinkUntwistStrength,
		GravityStrength = GravityStrength,
		OriginAnchorWeight = OriginAnchorWeight,
		DampingFactor = DampingFactor,
		MinRepulsionDistance = MinRepulsionDistance,
		RestLinkLength = RestLinkLength,
		MaxForce = MaxForce,
		MaxVelocity = MaxVelocity,
		TargetPhysicsHz = TargetPhysicsHz,
		StabilityThreshold = StabilityThreshold,
		OverlapMargin = OverlapMargin,
		MaxOverlapCorrection = MaxOverlapCorrection,
	};

	/// <summary>Construct a managed record from the POD <see cref="LayoutSettings"/>.</summary>
	public static PhysicsSettings FromLayoutSettings(in LayoutSettings s) => new()
	{
		Enabled = s.Enabled != 0,
		RepulsionStrength = s.RepulsionStrength,
		LinkSpringStrength = s.LinkSpringStrength,
		DirectionalBias = s.DirectionalBias,
		LinkFlatteningStrength = s.LinkFlatteningStrength,
		LinkFlatteningMargin = s.LinkFlatteningMargin,
		LinkUntwistStrength = s.LinkUntwistStrength,
		GravityStrength = s.GravityStrength,
		OriginAnchorWeight = s.OriginAnchorWeight,
		DampingFactor = s.DampingFactor,
		MinRepulsionDistance = s.MinRepulsionDistance,
		RestLinkLength = s.RestLinkLength,
		MaxForce = s.MaxForce,
		MaxVelocity = s.MaxVelocity,
		TargetPhysicsHz = s.TargetPhysicsHz,
		StabilityThreshold = s.StabilityThreshold,
		OverlapMargin = s.OverlapMargin,
		MaxOverlapCorrection = s.MaxOverlapCorrection,
	};
}
