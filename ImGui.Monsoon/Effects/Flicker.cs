// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Monsoon.Effects;

/// <summary>
/// The brightness of a neon tube that is mostly steady and sometimes stutters.
/// </summary>
internal static class Flicker
{
	/// <summary>How many times a second the brightness may change.</summary>
	internal const double StepsPerSecond = 20d;

	/// <summary>The share of steps that dip.</summary>
	internal const float DipChance = 0.08f;

	/// <summary>
	/// Gets the brightness at <paramref name="time"/>, between <c>1 - amount</c> and 1.
	/// </summary>
	/// <param name="time">The time in seconds.</param>
	/// <param name="seed">The tube's seed, so two signs do not stutter together.</param>
	/// <param name="amount">How deep a dip may go, 0 to 1.</param>
	/// <returns>The brightness multiplier.</returns>
	internal static float Brightness(double time, uint seed, float amount)
	{
		float depth = Math.Clamp(amount, 0f, 1f);
		if (depth == 0f)
		{
			return 1f;
		}

		long step = (long)Math.Floor(time * StepsPerSecond);
		if (Noise.Hash(seed, step) >= DipChance)
		{
			return 1f;
		}

		return 1f - (depth * (0.35f + (0.65f * Noise.Hash(seed ^ 0xA5A5A5A5u, step))));
	}
}
