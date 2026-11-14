// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Monsoon.Effects;

/// <summary>
/// A stateless hash turning a seed and an index into a repeatable value in [0, 1).
/// </summary>
/// <remarks>
/// Every effect draws its randomness from here rather than from <see cref="Random"/>, so a widget
/// looks the same at the same time on every frame and every machine, which is what lets a UI test
/// photograph it.
/// </remarks>
internal static class Noise
{
	/// <summary>Hashes <paramref name="seed"/> and <paramref name="index"/> into [0, 1).</summary>
	/// <param name="seed">The caller's seed, usually one per widget.</param>
	/// <param name="index">Which value of that seed's sequence.</param>
	/// <returns>A value at least 0 and below 1.</returns>
	internal static float Hash(uint seed, long index)
	{
		unchecked
		{
			ulong x = (seed * 0x9E3779B97F4A7C15UL) ^ ((ulong)index * 0xBF58476D1CE4E5B9UL);
			x ^= x >> 30;
			x *= 0xBF58476D1CE4E5B9UL;
			x ^= x >> 27;
			x *= 0x94D049BB133111EBUL;
			x ^= x >> 31;
			return (x >> 40) / (float)(1UL << 24);
		}
	}

	/// <summary>Hashes a string into a seed.</summary>
	/// <param name="text">The text, usually a widget's label.</param>
	/// <returns>The seed. The same text always gives the same seed.</returns>
	internal static uint SeedOf(string text)
	{
		unchecked
		{
			uint hash = 2166136261u;
			foreach (char c in text)
			{
				hash = (hash ^ c) * 16777619u;
			}

			return hash;
		}
	}
}
