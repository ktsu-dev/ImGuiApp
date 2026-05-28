// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets.Animation;

using System;

/// <summary>
/// Easing functions normalized over <c>t &#x2208; [0, 1]</c> returning values in the same range
/// (back / elastic / bounce variants can overshoot slightly). Pass any of these as the curve
/// argument to <see cref="Tween"/>.
/// </summary>
public static class Easing
{
	/// <summary>No easing &#x2014; returns <paramref name="t"/> unchanged.</summary>
	public static float Linear(float t) => t;

	/// <summary>Slow start, accelerates quadratically.</summary>
	public static float InQuad(float t) => t * t;

	/// <summary>Fast start, decelerates quadratically. Good default for snappy UI fades.</summary>
	public static float OutQuad(float t) => 1.0f - ((1.0f - t) * (1.0f - t));

	/// <summary>Symmetric quadratic easing.</summary>
	public static float InOutQuad(float t) => t < 0.5f
		? 2.0f * t * t
		: 1.0f - (MathF.Pow((-2.0f * t) + 2.0f, 2.0f) / 2.0f);

	/// <summary>Slow start, accelerates cubically.</summary>
	public static float InCubic(float t) => t * t * t;

	/// <summary>Fast start, decelerates cubically. The mobile-UI default for entrances.</summary>
	public static float OutCubic(float t) => 1.0f - MathF.Pow(1.0f - t, 3.0f);

	/// <summary>Symmetric cubic easing.</summary>
	public static float InOutCubic(float t) => t < 0.5f
		? 4.0f * t * t * t
		: 1.0f - (MathF.Pow((-2.0f * t) + 2.0f, 3.0f) / 2.0f);

	/// <summary>Cubic with a small overshoot at the end &#x2014; "pops" into place.</summary>
	public static float OutBack(float t)
	{
		const float c1 = 1.70158f;
		const float c3 = c1 + 1.0f;
		float u = t - 1.0f;
		return 1.0f + (c3 * u * u * u) + (c1 * u * u);
	}

	/// <summary>Cubic that pulls back before launching forward.</summary>
	public static float InBack(float t)
	{
		const float c1 = 1.70158f;
		const float c3 = c1 + 1.0f;
		return (c3 * t * t * t) - (c1 * t * t);
	}
}
