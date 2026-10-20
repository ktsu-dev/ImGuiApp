// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;

using ktsu.ImGui.Probes;

/// <summary>
/// How much of a collapsing app bar is tucked away, with no ImGui calls: the bar slides away as the
/// content under it scrolls down and back as it scrolls up, never hides more than has been scrolled,
/// and settles fully in or fully out once the scrolling stops.
/// </summary>
internal sealed class AppBarState
{
	/// <summary>How long the scroll must rest, in seconds, before a half-hidden bar settles.</summary>
	internal const float SettleDelay = 0.15f;

	/// <summary>How long a settle takes for the bar's whole height, in seconds.</summary>
	internal const float SettleTime = 0.15f;

	private float lastScroll = float.NaN;
	private float resting;

	/// <summary>Gets how much of the bar is tucked away, in pixels.</summary>
	public float Hidden { get; private set; }

	/// <summary>Follows the content's scroll position.</summary>
	/// <param name="scroll">How far the content under the bar is scrolled down, in pixels.</param>
	/// <param name="height">The bar's full height, in pixels.</param>
	/// <param name="deltaTime">Seconds since the last update.</param>
	/// <returns>How much of the bar shows, in pixels.</returns>
	public float Track(float scroll, float height, float deltaTime)
	{
		float delta = float.IsNaN(lastScroll) ? 0f : scroll - lastScroll;
		lastScroll = scroll;

		Hidden = Math.Clamp(Hidden + delta, 0f, MathF.Min(height, MathF.Max(scroll, 0f)));
		resting = delta == 0f ? resting + MathF.Max(deltaTime, 0f) : 0f;

		if (resting >= SettleDelay && Hidden > 0f && Hidden < height)
		{
			float target = Hidden >= height * 0.5f && scroll >= height ? height : 0f;
			float step = height * MathF.Max(deltaTime, 0f) / SettleTime;
			Hidden = target > Hidden ? MathF.Min(target, Hidden + step) : MathF.Max(target, Hidden - step);
			AnimationActivity.RequestActiveFrame();
		}

		return height - Hidden;
	}
}
