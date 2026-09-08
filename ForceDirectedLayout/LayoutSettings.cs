// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ForceDirectedLayout;

using System.Runtime.InteropServices;

/// <summary>
/// Plain-double layout configuration. Blittable POD so it crosses the C ABI unchanged.
/// The managed surface may expose typed/semantic equivalents on top of this struct,
/// but everything inside the AOT-friendly core works in raw doubles.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct LayoutSettings
{
	/// <summary>Whether simulation is active. Non-zero = enabled. Step is a no-op when zero.</summary>
	public byte Enabled;

	private byte pad0;
	private byte pad1;
	private byte pad2;
	private int pad3;

	/// <summary>Strength of pairwise inverse-square repulsion between bodies (newtons-equivalent).</summary>
	public double RepulsionStrength;

	/// <summary>Dimensionless Hooke's-law spring constant for edges.</summary>
	public double LinkSpringStrength;

	/// <summary>Strength of the horizontal source-left/target-right ordering bias along edges. 0 disables it.</summary>
	public double DirectionalBias;

	/// <summary>
	/// Strength of the preference for horizontal edges: it both levels an edge's two ends and, when the
	/// rendered curve would otherwise hide, splays them apart horizontally. 0 disables both.
	/// See <see cref="LayoutCore.BezierClearanceRatio"/> for the clearance geometry.
	/// </summary>
	public double LinkFlatteningStrength;

	/// <summary>Extra horizontal clearance demanded on top of the derived bezier bound, in position units.</summary>
	public double LinkFlatteningMargin;

	/// <summary>Strength of the gravity force pulling each body toward the gravity target.</summary>
	public double GravityStrength;

	/// <summary>Blend factor from centroid (0) to world origin (1) for the gravity target.</summary>
	public double OriginAnchorWeight;

	/// <summary>Per-second velocity retention. 0.5 means velocity halves every second.</summary>
	public double DampingFactor;

	/// <summary>Distance floor used to clamp the inverse-square repulsion denominator.</summary>
	public double MinRepulsionDistance;

	/// <summary>Spring rest length for edges.</summary>
	public double RestLinkLength;

	/// <summary>Per-body force magnitude cap (applied before integration).</summary>
	public double MaxForce;

	/// <summary>Per-body velocity magnitude cap (applied after integration).</summary>
	public double MaxVelocity;

	/// <summary>Target substep frequency. Per-frame substep count is ceil(deltaTime * TargetPhysicsHz).</summary>
	public double TargetPhysicsHz;

	/// <summary>System energy threshold below which the simulation reports IsStable.</summary>
	public double StabilityThreshold;

	/// <summary>Clear space kept between body rectangles by the overlap pass. 0 disables the pass.</summary>
	public double OverlapMargin;

	/// <summary>Per-substep cap on how far an overlapping pair is pushed apart.</summary>
	public double MaxOverlapCorrection;

	/// <summary>Sensible defaults matching the previous Force&lt;float&gt;/Length&lt;float&gt; values.</summary>
	public static LayoutSettings Defaults => new()
	{
		Enabled = 0,
		RepulsionStrength = 1_200_000.0,
		LinkSpringStrength = 0.5,
		DirectionalBias = 0.5,
		LinkFlatteningStrength = 0.5,
		LinkFlatteningMargin = 0.0,
		GravityStrength = 50.0,
		OriginAnchorWeight = 1.0,
		DampingFactor = 0.5,
		MinRepulsionDistance = 50.0,
		RestLinkLength = 225.0,
		MaxForce = 5000.0,
		MaxVelocity = 50.0,
		TargetPhysicsHz = 120.0,
		StabilityThreshold = 1.0,
		OverlapMargin = 20.0,
		MaxOverlapCorrection = 40.0,
	};
}
