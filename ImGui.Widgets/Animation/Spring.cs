// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets.Animation;

using System;

/// <summary>
/// Damped harmonic oscillator: a value chases a target with mass-spring physics.
/// Frame-rate independent via fixed-substep semi-implicit Euler.
/// </summary>
/// <remarks>
/// The motion is governed by <c>ẍ = -k(x - target) - c·v</c> where <c>k</c> is <see cref="Stiffness"/>
/// and <c>c</c> is <see cref="Damping"/>. Critical damping at <c>c = 2·&#x221A;k</c>; under-damped values
/// oscillate, over-damped values approach the target without overshoot but more slowly.
/// </remarks>
/// <param name="initial">Starting position; also the initial <see cref="Target"/>.</param>
/// <param name="stiffness">Spring constant <c>k</c>. Higher = snappier.</param>
/// <param name="damping">Damping coefficient <c>c</c>. Critical damping is <c>2 &#xB7; &#x221A;stiffness</c>.</param>
public sealed class Spring(float initial = 0.0f, float stiffness = 170.0f, float damping = 26.0f)
{
	private const float SubStep = 1.0f / 240.0f;

	/// <summary>Spring constant <c>k</c>. Higher = faster, stiffer pull toward the target.</summary>
	public float Stiffness { get; set; } = stiffness;

	/// <summary>Damping coefficient <c>c</c>. Critical damping is <c>2 &#xB7; &#x221A;Stiffness</c>.</summary>
	public float Damping { get; set; } = damping;

	/// <summary>Where the spring is trying to settle.</summary>
	public float Target { get; set; } = initial;

	/// <summary>Current position. Mutated each <see cref="Update"/>.</summary>
	public float Value { get; private set; } = initial;

	/// <summary>Current velocity. Mutated each <see cref="Update"/>.</summary>
	public float Velocity { get; private set; }

	/// <summary>
	/// Distance-and-velocity threshold below which the spring is considered at rest and
	/// <see cref="IsActive"/> flips false. The spring snaps to <see cref="Target"/> on rest.
	/// </summary>
	public float RestThreshold { get; init; } = 0.01f;

	/// <summary>True while the spring is still settling. Use this to gate frame-rate boosts.</summary>
	public bool IsActive => MathF.Abs(Value - Target) > RestThreshold
		|| MathF.Abs(Velocity) > RestThreshold;

	/// <summary>
	/// Advance the spring by <paramref name="deltaTime"/> seconds and return the new value.
	/// The simulation is sub-stepped internally so large dt does not destabilize the integrator.
	/// </summary>
	public float Update(float deltaTime)
	{
		if (deltaTime <= 0.0f)
		{
			return Value;
		}

		float remaining = deltaTime;
		while (remaining > 0.0f)
		{
			float step = MathF.Min(remaining, SubStep);
			remaining -= step;

			float accel = (-Stiffness * (Value - Target)) - (Damping * Velocity);
			// Semi-implicit Euler: integrate velocity first, then position with new velocity.
			Velocity += accel * step;
			Value += Velocity * step;
		}

		if (!IsActive)
		{
			Value = Target;
			Velocity = 0.0f;
		}

		return Value;
	}

	/// <summary>
	/// Jump immediately to <paramref name="value"/> and clear velocity. The spring is at rest afterward
	/// until <see cref="Target"/> is changed.
	/// </summary>
	public void SnapTo(float value)
	{
		Value = value;
		Target = value;
		Velocity = 0.0f;
	}

	/// <summary>Set a new target without disturbing position or velocity. Equivalent to assigning <see cref="Target"/>.</summary>
	public void SetTarget(float target) => Target = target;
}
