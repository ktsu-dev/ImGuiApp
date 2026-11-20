// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Monsoon.Effects;

using System.Numerics;

/// <summary>One streak of rain on the glass, in the panel's own coordinates.</summary>
/// <param name="Top">Where the streak starts.</param>
/// <param name="Bottom">Where it ends; below and slightly left of <paramref name="Top"/>.</param>
/// <param name="Alpha">How visible it is, 0 to 1.</param>
internal readonly record struct RainStreak(Vector2 Top, Vector2 Bottom, float Alpha);

/// <summary>
/// Rain running down a pane of glass: streaks that fall, wrap and never leave the pane.
/// </summary>
internal static class RainField
{
	/// <summary>The slant of every streak, as horizontal travel per unit of fall.</summary>
	internal const float Slant = 0.12f;

	/// <summary>Computes the streaks at <paramref name="time"/>.</summary>
	/// <param name="size">The pane's size.</param>
	/// <param name="time">The time in seconds.</param>
	/// <param name="seed">The pane's seed.</param>
	/// <param name="count">How many streaks.</param>
	/// <param name="speed">How fast they fall, in pixels a second, before each streak's own variation.</param>
	/// <returns>The streaks, every point inside [0, size].</returns>
	internal static IReadOnlyList<RainStreak> Streaks(Vector2 size, double time, uint seed, int count, float speed)
	{
		if (size.X <= 0f || size.Y <= 0f || count <= 0)
		{
			return [];
		}

		RainStreak[] streaks = new RainStreak[count];
		for (int i = 0; i < count; i++)
		{
			float length = Math.Min(size.Y, 8f + (24f * Noise.Hash(seed, (i * 4) + 1)));
			float ownSpeed = speed * (0.6f + (0.8f * Noise.Hash(seed, (i * 4) + 2)));
			float travel = size.Y + length;
			double fallen = (Noise.Hash(seed, (i * 4) + 3) * travel) + (time * ownSpeed);
			float head = (float)(fallen % travel);
			float bottomY = Math.Clamp(head, 0f, size.Y);
			float topY = Math.Clamp(head - length, 0f, size.Y);
			float x = Noise.Hash(seed, i * 4) * size.X;
			float topX = Math.Clamp(x + ((size.Y - topY) * Slant * 0.1f), 0f, size.X);
			float bottomX = Math.Clamp(topX - ((bottomY - topY) * Slant), 0f, size.X);
			float alpha = 0.08f + (0.22f * Noise.Hash(seed ^ 0xBEEFu, i));
			streaks[i] = new RainStreak(new Vector2(topX, topY), new Vector2(bottomX, bottomY), alpha);
		}

		return streaks;
	}
}
