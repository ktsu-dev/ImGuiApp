// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets.Gestures;

using System.Numerics;

/// <summary>
/// Snapshot of the gesture state for a single frame.
/// </summary>
/// <param name="Gestures">Discrete events that fired this frame.</param>
/// <param name="IsPressed">True while the pointer is held over the detector.</param>
/// <param name="StartPos">Screen position where the current press began (zero if no press is active and no gesture fired).</param>
/// <param name="CurrentPos">Pointer position this frame.</param>
/// <param name="Delta">Total movement from press start to the current frame.</param>
/// <param name="Velocity">Smoothed pointer velocity in pixels per second.</param>
/// <param name="PressDuration">Seconds elapsed since the current press began (zero when not pressed).</param>
public readonly record struct GestureResult(
	GestureFlags Gestures,
	bool IsPressed,
	Vector2 StartPos,
	Vector2 CurrentPos,
	Vector2 Delta,
	Vector2 Velocity,
	float PressDuration)
{
	/// <summary>True if any discrete gesture fired this frame.</summary>
	public bool HasGesture => Gestures != GestureFlags.None;

	/// <summary>Convenience accessor for <see cref="GestureFlags.Tap"/>.</summary>
	public bool Tapped => (Gestures & GestureFlags.Tap) != 0;

	/// <summary>Convenience accessor for <see cref="GestureFlags.DoubleTap"/>.</summary>
	public bool DoubleTapped => (Gestures & GestureFlags.DoubleTap) != 0;

	/// <summary>Convenience accessor for <see cref="GestureFlags.LongPress"/>.</summary>
	public bool LongPressed => (Gestures & GestureFlags.LongPress) != 0;
}
