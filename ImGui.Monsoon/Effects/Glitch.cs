// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Monsoon.Effects;

/// <summary>
/// One frame of a glitch: whether a burst is running, how far each horizontal slice is shifted,
/// and how far the colour channels have split.
/// </summary>
/// <param name="Active">Whether a burst is running.</param>
/// <param name="Offsets">One horizontal shift per slice, each within ±intensity, in units of the text's height.</param>
/// <param name="Split">How far the magenta and cyan ghosts sit from the text, within [0, intensity].</param>
internal readonly record struct GlitchFrame(bool Active, IReadOnlyList<float> Offsets, float Split);

/// <summary>
/// Short glitch bursts at irregular moments, quiet in between.
/// </summary>
internal static class Glitch
{
	/// <summary>The length of one window, in seconds; a window holds at most one burst.</summary>
	internal const double Window = 1.7d;

	/// <summary>How long a burst lasts, in seconds, from the start of its window.</summary>
	internal const double Burst = 0.18d;

	/// <summary>The share of windows that hold a burst.</summary>
	internal const float BurstChance = 0.45f;

	/// <summary>Computes the glitch at <paramref name="time"/>.</summary>
	/// <param name="time">The time in seconds.</param>
	/// <param name="seed">The text's seed.</param>
	/// <param name="intensity">How violent a burst is, 0 to 1. Zero never glitches.</param>
	/// <param name="sliceCount">How many horizontal slices the text is cut into.</param>
	/// <returns>The frame.</returns>
	internal static GlitchFrame Compute(double time, uint seed, float intensity, int sliceCount)
	{
		int slices = Math.Max(1, sliceCount);
		float strength = Math.Clamp(intensity, 0f, 1f);
		float[] offsets = new float[slices];
		if (strength <= 0f)
		{
			return new GlitchFrame(false, offsets, 0f);
		}

		long window = (long)Math.Floor(time / Window);
		double into = time - (window * Window);
		if (into >= Burst || Noise.Hash(seed, window) >= BurstChance)
		{
			return new GlitchFrame(false, offsets, 0f);
		}

		long jitter = (long)Math.Floor(into * 60d);
		for (int i = 0; i < slices; i++)
		{
			float r = Noise.Hash(seed + (uint)i + 1u, (window * 131) + jitter);
			offsets[i] = r < 0.5f ? 0f : (r - 0.75f) * 4f * strength;
		}

		float split = strength * (0.3f + (0.7f * Noise.Hash(seed ^ 0x5EEDu, window)));
		return new GlitchFrame(true, offsets, split);
	}
}
