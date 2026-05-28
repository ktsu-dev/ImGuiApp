// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets.Scroll;

using System;

/// <summary>
/// One-dimensional scroll offset that coasts after release. Drives carousels, pickers, and
/// long lists that should feel like native mobile scrollers rather than jumping to a stop
/// when the pointer is lifted.
/// </summary>
/// <remarks>
/// <para>
/// Usage during a frame: while the pointer is down, call <see cref="Drag"/> with the pan
/// delta and frame <c>deltaTime</c>; on release call <see cref="Fling"/> with the recorded
/// velocity (the <c>Velocity</c> field on a <c>GestureResult</c> is a fine source).
/// Each frame thereafter call <see cref="Update"/> to advance the coast and decay the
/// velocity exponentially. <see cref="IsActive"/> stays true until the motion settles —
/// gate frame-rate boosts on that flag.
/// </para>
/// <para>
/// Set <see cref="MinExtent"/> / <see cref="MaxExtent"/> to clamp to a content range; the
/// default is unbounded. When the scroll hits a bound velocity is zeroed so the motion
/// stops cleanly rather than rebounding.
/// </para>
/// </remarks>
/// <param name="initialPosition">Starting scroll offset in pixels.</param>
public sealed class InertialScroll(float initialPosition = 0.0f)
{
	/// <summary>Current scroll offset in pixels.</summary>
	public float Position { get; private set; } = initialPosition;

	/// <summary>Current velocity in pixels per second. Positive moves <see cref="Position"/> upward.</summary>
	public float Velocity { get; private set; }

	/// <summary>
	/// Exponential velocity-decay rate per second. Higher values stop the coast sooner;
	/// the default of 6.0 mirrors iOS-style "deceleration normal".
	/// </summary>
	public float Friction { get; init; } = 6.0f;

	/// <summary>
	/// Velocity magnitude below which the scroll is considered at rest. Update zeroes the
	/// velocity once it falls under this threshold so <see cref="IsActive"/> can flip false.
	/// </summary>
	public float MinVelocity { get; init; } = 1.0f;

	/// <summary>Lower bound for <see cref="Position"/>. Defaults to unbounded.</summary>
	public float MinExtent { get; set; } = float.NegativeInfinity;

	/// <summary>Upper bound for <see cref="Position"/>. Defaults to unbounded.</summary>
	public float MaxExtent { get; set; } = float.PositiveInfinity;

	/// <summary>True while the scroll is still coasting. Use this to keep the framerate elevated.</summary>
	public bool IsActive => MathF.Abs(Velocity) > MinVelocity;

	/// <summary>
	/// Apply a pointer-driven displacement of <paramref name="delta"/> pixels this frame.
	/// Tracks instantaneous velocity so a subsequent <see cref="Fling"/>-less release still
	/// coasts using the last drag's speed.
	/// </summary>
	/// <param name="delta">Pixel offset to add to <see cref="Position"/>.</param>
	/// <param name="deltaTime">Frame interval in seconds; used to derive instantaneous velocity.</param>
	public void Drag(float delta, float deltaTime)
	{
		Position += delta;
		if (deltaTime > 0.0f)
		{
			Velocity = delta / deltaTime;
		}

		ClampToBounds();
	}

	/// <summary>
	/// Release the scroll with the supplied velocity. The next <see cref="Update"/> calls
	/// will coast and decay until the motion settles or hits a bound.
	/// </summary>
	/// <param name="velocity">Initial coast velocity in pixels per second.</param>
	public void Fling(float velocity) => Velocity = velocity;

	/// <summary>
	/// Advance the coast by <paramref name="deltaTime"/> seconds and return the new
	/// <see cref="Position"/>. Returns immediately when the scroll is already at rest.
	/// </summary>
	public float Update(float deltaTime)
	{
		if (deltaTime <= 0.0f || !IsActive)
		{
			return Position;
		}

		// Exponential decay v(t) = v0 * exp(-friction * dt); integrate position
		// using the average of pre- and post-decay velocity for second-order accuracy.
		float decayed = Velocity * MathF.Exp(-Friction * deltaTime);
		float averageVelocity = (Velocity + decayed) * 0.5f;
		Position += averageVelocity * deltaTime;
		Velocity = decayed;

		if (Position <= MinExtent)
		{
			Position = MinExtent;
			Velocity = 0.0f;
		}
		else if (Position >= MaxExtent)
		{
			Position = MaxExtent;
			Velocity = 0.0f;
		}
		else if (MathF.Abs(Velocity) < MinVelocity)
		{
			Velocity = 0.0f;
		}

		return Position;
	}

	/// <summary>Halt the coast without changing <see cref="Position"/>. Useful when the pointer touches down again.</summary>
	public void Stop() => Velocity = 0.0f;

	/// <summary>Jump to <paramref name="position"/> (clamped to bounds) and clear velocity.</summary>
	public void SnapTo(float position)
	{
		Position = Math.Clamp(position, MinExtent, MaxExtent);
		Velocity = 0.0f;
	}

	private void ClampToBounds()
	{
		if (Position < MinExtent)
		{
			Position = MinExtent;
			Velocity = 0.0f;
		}
		else if (Position > MaxExtent)
		{
			Position = MaxExtent;
			Velocity = 0.0f;
		}
	}
}
