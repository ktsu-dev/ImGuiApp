// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets.Gestures;

using System;
using System.Numerics;

/// <summary>
/// UI-agnostic gesture state machine. Feed it the current pointer-down state, pointer position,
/// and frame delta time; it returns the discrete gestures that fired this frame plus continuous
/// state (start position, velocity, duration).
/// </summary>
/// <remarks>
/// Single-pointer only. Multi-touch and pinch gestures are deferred until a touch-capable
/// backend lands. Suitable for both mouse-driven desktop and (future) single-finger touch input.
/// </remarks>
public sealed class GestureMachine(GestureSettings? settings = null)
{
	private Vector2 _startPos;
	private Vector2 _currentPos;
	private Vector2 _lastPos;
	private Vector2 _velocity;
	private float _pressDuration;
	private bool _longPressFired;
	private bool _panActive;
	private float _timeSinceLastTap = float.MaxValue;
	private Vector2 _lastTapPos;

	/// <summary>Settings used by this machine.</summary>
	public GestureSettings Settings { get; } = settings ?? new GestureSettings();

	/// <summary>True while a press is active (pointer down since the last release).</summary>
	public bool IsPressed { get; private set; }

	/// <summary>
	/// Advance the state machine by one frame.
	/// </summary>
	/// <param name="pressed">True if the pointer is currently down over the detector.</param>
	/// <param name="pos">Current pointer position (any consistent coordinate space).</param>
	/// <param name="deltaTime">Seconds since the previous call. Must be &gt;= 0.</param>
	/// <returns>The gestures that fired this frame along with continuous state.</returns>
	public GestureResult Update(bool pressed, Vector2 pos, float deltaTime)
	{
		if (deltaTime < 0.0f)
		{
			deltaTime = 0.0f;
		}

		GestureFlags fired = GestureFlags.None;

		// Tick the double-tap window even when nothing is pressed.
		if (_timeSinceLastTap < float.MaxValue)
		{
			_timeSinceLastTap += deltaTime;
		}

		bool justPressed = pressed && !IsPressed;
		bool justReleased = !pressed && IsPressed;

		if (justPressed)
		{
			BeginPress(pos);
		}
		else if (IsPressed)
		{
			fired |= UpdateActivePress(pos, deltaTime);
		}

		if (justReleased)
		{
			fired |= EndPress();
		}

		Vector2 startPosResult = (!IsPressed && fired == GestureFlags.None) ? Vector2.Zero : _startPos;
		return new GestureResult(
			Gestures: fired,
			IsPressed: IsPressed,
			StartPos: startPosResult,
			CurrentPos: _currentPos,
			Delta: _currentPos - _startPos,
			Velocity: _velocity,
			PressDuration: IsPressed ? _pressDuration : 0.0f);
	}

	private void BeginPress(Vector2 pos)
	{
		_startPos = pos;
		_currentPos = pos;
		_lastPos = pos;
		_velocity = Vector2.Zero;
		_pressDuration = 0.0f;
		_longPressFired = false;
		_panActive = false;
		IsPressed = true;
	}

	private GestureFlags UpdateActivePress(Vector2 pos, float deltaTime)
	{
		GestureFlags fired = GestureFlags.None;

		_pressDuration += deltaTime;

		Vector2 frameDelta = pos - _lastPos;
		Vector2 instantVelocity = deltaTime > 0.0f ? frameDelta / deltaTime : Vector2.Zero;

		// Exponential smoothing: blend new sample toward stored velocity by the smoothing factor.
		float smoothing = Math.Clamp(Settings.VelocitySmoothing, 0.0f, 1.0f);
		_velocity = (_velocity * smoothing) + (instantVelocity * (1.0f - smoothing));

		_currentPos = pos;
		_lastPos = pos;

		Vector2 totalDelta = _currentPos - _startPos;
		float totalDistance = totalDelta.Length();

		// Long-press fires once when the pointer has stayed roughly still past the threshold.
		if (!_longPressFired
			&& _pressDuration >= Settings.LongPressMinDuration
			&& totalDistance <= Settings.TapMaxDistance)
		{
			fired |= GestureFlags.LongPress;
			_longPressFired = true;
		}

		// Pan promotion: once we've moved past the threshold we're in a pan for the rest of this press.
		if (!_panActive && totalDistance > Settings.PanMinDistance)
		{
			_panActive = true;
			fired |= GestureFlags.PanStart;
		}

		if (_panActive)
		{
			fired |= GestureFlags.Pan;
		}

		return fired;
	}

	private GestureFlags EndPress()
	{
		GestureFlags fired = GestureFlags.None;

		Vector2 totalDelta = _currentPos - _startPos;
		float totalDistance = totalDelta.Length();
		bool wasPan = _panActive;
		Vector2 releaseVelocity = _velocity;

		if (wasPan)
		{
			fired |= GestureFlags.PanEnd;

			GestureFlags swipe = ClassifySwipe(totalDelta, releaseVelocity);
			fired |= swipe;
		}
		else if (!_longPressFired
			&& _pressDuration <= Settings.TapMaxDuration
			&& totalDistance <= Settings.TapMaxDistance)
		{
			fired |= ClassifyTap();
		}

		IsPressed = false;
		_panActive = false;
		_pressDuration = 0.0f;

		return fired;
	}

	private GestureFlags ClassifyTap()
	{
		bool isDoubleTap =
			_timeSinceLastTap <= Settings.DoubleTapMaxInterval
			&& Vector2.Distance(_startPos, _lastTapPos) <= Settings.DoubleTapMaxDistance;

		if (isDoubleTap)
		{
			// Reset so a third quick tap does NOT chain into another double-tap.
			_timeSinceLastTap = float.MaxValue;
			return GestureFlags.DoubleTap;
		}

		_timeSinceLastTap = 0.0f;
		_lastTapPos = _startPos;
		return GestureFlags.Tap;
	}

	/// <summary>Drop all in-flight state. Use when the host element loses focus or unmounts.</summary>
	public void Reset()
	{
		IsPressed = false;
		_startPos = Vector2.Zero;
		_currentPos = Vector2.Zero;
		_lastPos = Vector2.Zero;
		_velocity = Vector2.Zero;
		_pressDuration = 0.0f;
		_longPressFired = false;
		_panActive = false;
		_timeSinceLastTap = float.MaxValue;
		_lastTapPos = Vector2.Zero;
	}

	private GestureFlags ClassifySwipe(Vector2 totalDelta, Vector2 releaseVelocity)
	{
		float absDx = MathF.Abs(totalDelta.X);
		float absDy = MathF.Abs(totalDelta.Y);
		float absVx = MathF.Abs(releaseVelocity.X);
		float absVy = MathF.Abs(releaseVelocity.Y);

		bool horizontalDominant = absDx >= absDy;

		if (horizontalDominant)
		{
			if (absDx >= Settings.SwipeMinDistance && absVx >= Settings.SwipeMinVelocity)
			{
				return totalDelta.X < 0 ? GestureFlags.SwipeLeft : GestureFlags.SwipeRight;
			}
		}
		else
		{
			if (absDy >= Settings.SwipeMinDistance && absVy >= Settings.SwipeMinVelocity)
			{
				return totalDelta.Y < 0 ? GestureFlags.SwipeUp : GestureFlags.SwipeDown;
			}
		}

		return GestureFlags.None;
	}
}
