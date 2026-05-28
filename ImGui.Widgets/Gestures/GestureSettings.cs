// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets.Gestures;

/// <summary>
/// Thresholds controlling how the gesture state machine classifies pointer input.
/// Distances are in screen pixels; durations are in seconds; velocity in pixels per second.
/// </summary>
public sealed record GestureSettings
{
	/// <summary>Maximum total movement that still counts as a tap rather than a pan.</summary>
	public float TapMaxDistance { get; init; } = 10.0f;

	/// <summary>Maximum press duration that still counts as a tap.</summary>
	public float TapMaxDuration { get; init; } = 0.3f;

	/// <summary>Maximum interval between two taps for the second to count as a double-tap.</summary>
	public float DoubleTapMaxInterval { get; init; } = 0.3f;

	/// <summary>Maximum distance between two taps for the second to count as a double-tap.</summary>
	public float DoubleTapMaxDistance { get; init; } = 25.0f;

	/// <summary>Minimum press duration before a long-press fires.</summary>
	public float LongPressMinDuration { get; init; } = 0.5f;

	/// <summary>Minimum release velocity (px/sec) along the dominant axis for a swipe.</summary>
	public float SwipeMinVelocity { get; init; } = 600.0f;

	/// <summary>Minimum total travel along the dominant axis for a swipe.</summary>
	public float SwipeMinDistance { get; init; } = 50.0f;

	/// <summary>Movement past this distance promotes the gesture from "pending" to a pan.</summary>
	public float PanMinDistance { get; init; } = 6.0f;

	/// <summary>
	/// Smoothing factor for exponential velocity averaging (0 = instantaneous, 1 = frozen).
	/// Values around 0.6 give a stable reading while still reacting to flicks.
	/// </summary>
	public float VelocitySmoothing { get; init; } = 0.6f;
}
