// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets.Animation;

using System;

/// <summary>
/// Time-based interpolation from <see cref="Start"/> to <see cref="End"/> over <see cref="Duration"/>
/// seconds, shaped by an easing function. Frame-rate independent: caller supplies <c>deltaTime</c>.
/// </summary>
/// <remarks>
/// Single-value (float) interpolation. Compose two Tweens for a Vector2, three for a Vector3, etc.
/// Set <see cref="Repeat"/> for an unbounded loop or <see cref="PingPong"/> for a back-and-forth.
/// </remarks>
public sealed class Tween
{
	private readonly Func<float, float> _curve;

	/// <summary>Initial value at <c>t = 0</c>.</summary>
	public float Start { get; }

	/// <summary>Final value at <c>t = Duration</c>.</summary>
	public float End { get; }

	/// <summary>Total length of one cycle in seconds. Zero or negative duration produces an instant snap.</summary>
	public float Duration { get; }

	/// <summary>When true the tween restarts from <see cref="Start"/> after reaching <see cref="End"/>.</summary>
	public bool Repeat { get; init; }

	/// <summary>When true the tween reverses direction at each endpoint instead of snapping back to <see cref="Start"/>.</summary>
	public bool PingPong { get; init; }

	/// <summary>Seconds elapsed in the current cycle.</summary>
	public float Elapsed { get; private set; }

	/// <summary>Current interpolated value, refreshed on each <see cref="Update"/>.</summary>
	public float Value { get; private set; }

	/// <summary>True while the tween still has work to do this frame (non-repeating tweens flip to false when complete).</summary>
	public bool IsActive { get; private set; }

	/// <summary>True for a non-repeating tween that has reached its end.</summary>
	public bool IsComplete => !IsActive;

	/// <summary>
	/// Initializes a new tween. Pass an easing function from <see cref="Easing"/> (e.g.
	/// <see cref="Easing.OutCubic"/>); defaults to <see cref="Easing.Linear"/> when null.
	/// </summary>
	public Tween(float start, float end, float duration, Func<float, float>? easing = null)
	{
		Start = start;
		End = end;
		Duration = duration;
		_curve = easing ?? Easing.Linear;
		Value = start;
		IsActive = duration > 0.0f;
		if (!IsActive)
		{
			Value = end;
			Elapsed = MathF.Max(duration, 0.0f);
		}
	}

	/// <summary>
	/// Advance the tween by <paramref name="deltaTime"/> seconds and return the new value.
	/// </summary>
	public float Update(float deltaTime)
	{
		if (deltaTime < 0.0f)
		{
			deltaTime = 0.0f;
		}

		if (!IsActive && !Repeat && !PingPong)
		{
			return Value;
		}

		if (Duration <= 0.0f)
		{
			Value = End;
			IsActive = false;
			return Value;
		}

		Elapsed += deltaTime;

		float cycle = Elapsed / Duration;
		bool reverse = false;

		if (Repeat || PingPong)
		{
			if (PingPong)
			{
				// Treat each Duration as a half-cycle; even halves go forward, odd halves backward.
				int half = (int)MathF.Floor(cycle);
				float frac = cycle - half;
				reverse = (half % 2) == 1;
				cycle = reverse ? 1.0f - frac : frac;
			}
			else
			{
				cycle -= MathF.Floor(cycle);
			}
		}
		else if (cycle >= 1.0f)
		{
			Value = End;
			IsActive = false;
			Elapsed = Duration;
			return Value;
		}

		float t = Math.Clamp(cycle, 0.0f, 1.0f);
		float eased = _curve(t);
		Value = Start + ((End - Start) * eased);
		return Value;
	}

	/// <summary>Rewind to the starting value and reactivate the tween.</summary>
	public void Restart()
	{
		Elapsed = 0.0f;
		Value = Start;
		IsActive = Duration > 0.0f;
	}

	/// <summary>Force the tween to its final value and mark it complete.</summary>
	public void Complete()
	{
		Elapsed = Duration;
		Value = End;
		IsActive = false;
	}

	/// <summary>Seek to a specific elapsed time. Useful for scrub controls or initial-state restoration.</summary>
	public void SeekTo(float elapsedSeconds)
	{
		Elapsed = MathF.Max(elapsedSeconds, 0.0f);
		IsActive = true;
		_ = Update(0.0f);
	}
}
