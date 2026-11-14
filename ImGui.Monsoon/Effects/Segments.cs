// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Monsoon.Effects;

/// <summary>The arithmetic of a segmented bar.</summary>
internal static class Segments
{
	/// <summary>The share of a bar above which segments light in the alarm colour.</summary>
	internal const float AlarmThreshold = 0.85f;

	/// <summary>How many of <paramref name="segments"/> are lit at <paramref name="value"/>.</summary>
	/// <param name="value">The value, 0 to 1; anything outside is clamped, NaN is zero.</param>
	/// <param name="segments">How many segments the bar has.</param>
	/// <returns>The lit count, 0 to <paramref name="segments"/>.</returns>
	internal static int LitCount(float value, int segments)
	{
		if (segments <= 0 || float.IsNaN(value))
		{
			return 0;
		}

		return (int)MathF.Round(Math.Clamp(value, 0f, 1f) * segments, MidpointRounding.AwayFromZero);
	}

	/// <summary>Whether segment <paramref name="index"/> sits in the alarm band.</summary>
	/// <param name="index">The segment, from zero.</param>
	/// <param name="segments">How many segments the bar has.</param>
	/// <returns>True when its start is past <see cref="AlarmThreshold"/>.</returns>
	internal static bool IsAlarm(int index, int segments) =>
		segments > 0 && (float)index / segments >= AlarmThreshold;
}
