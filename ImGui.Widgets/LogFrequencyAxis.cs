// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// A logarithmic frequency axis: maps a frequency to a 0..1 position across a range and back.
	/// Shared by <see cref="SpectrumAnalyzer(string, SpectrumAnalyzerState, System.Numerics.Vector2, float, float, SpectrumAnalyzerStyle, float)"/>
	/// and <see cref="ParametricEq"/> so both place a frequency at the same pixel. Immutable.
	/// </summary>
	/// <remarks>
	/// Equal ratios take equal widths, so an octave is as wide at 100 Hz as at 10 kHz. That is the axis
	/// every audio tool draws frequency on, and the reason a spectrum analyzer drawn into the same
	/// rectangle as an EQ lines up with it: both ask this class where a frequency goes.
	/// </remarks>
	public sealed class LogFrequencyAxis
	{
		private readonly float logSpan;

		/// <summary>Creates an axis over [<paramref name="minFrequency"/>, <paramref name="maxFrequency"/>] Hz.</summary>
		/// <param name="minFrequency">The frequency at position 0, in hertz.</param>
		/// <param name="maxFrequency">The frequency at position 1, in hertz.</param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// <paramref name="minFrequency"/> is not finite and positive, or <paramref name="maxFrequency"/>
		/// is not finite and greater than it.
		/// </exception>
		public LogFrequencyAxis(float minFrequency = 20f, float maxFrequency = 20000f)
		{
			// A logarithmic axis has no zero, and a reversed or empty range has no width.
			if (minFrequency <= 0f || !float.IsFinite(minFrequency))
			{
				throw new ArgumentOutOfRangeException(nameof(minFrequency), minFrequency, "The lowest frequency must be positive and finite.");
			}

			if (maxFrequency <= minFrequency || !float.IsFinite(maxFrequency))
			{
				throw new ArgumentOutOfRangeException(nameof(maxFrequency), maxFrequency, "The highest frequency must be finite and above the lowest.");
			}

			MinFrequency = minFrequency;
			MaxFrequency = maxFrequency;
			logSpan = MathF.Log(maxFrequency / minFrequency);
			FirstDecade = MathF.Pow(10f, MathF.Ceiling(MathF.Log10(minFrequency)));
		}

		/// <summary>Gets the frequency at position 0, in hertz.</summary>
		public float MinFrequency { get; }

		/// <summary>Gets the frequency at position 1, in hertz.</summary>
		public float MaxFrequency { get; }

		/// <summary>
		/// Gets the lowest power of ten at or above <see cref="MinFrequency"/>; gridlines run from here,
		/// times ten, below <see cref="MaxFrequency"/>.
		/// </summary>
		public float FirstDecade { get; }

		/// <summary>Maps a frequency to its position across the axis.</summary>
		/// <param name="frequency">The frequency, in hertz.</param>
		/// <returns>
		/// 0 at <see cref="MinFrequency"/>, 1 at <see cref="MaxFrequency"/>, extrapolating outside;
		/// <see cref="float.NegativeInfinity"/> for a frequency &lt;= 0.
		/// </returns>
		public float FrequencyToPosition(float frequency) => frequency > 0f
			? MathF.Log(frequency / MinFrequency) / logSpan
			: float.NegativeInfinity;

		/// <summary>Maps a position across the axis back to its frequency.</summary>
		/// <param name="position">The position, 0 at <see cref="MinFrequency"/> and 1 at <see cref="MaxFrequency"/>.</param>
		/// <returns>
		/// <c>MinFrequency * (MaxFrequency / MinFrequency)^position</c>, extrapolating outside 0..1;
		/// NaN for NaN.
		/// </returns>
		public float PositionToFrequency(float position) => MinFrequency * MathF.Pow(MaxFrequency / MinFrequency, position);
	}
}
