// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// The bands, ballistics and peak hold behind <see cref="SpectrumAnalyzer(string, SpectrumAnalyzerState, System.Numerics.Vector2, float, float, SpectrumAnalyzerStyle, float)"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Deliberately free of ImGui. The caller hands in one frame of FFT magnitudes in decibels and
	/// the time since the last frame; the state folds those linearly spaced bins into logarithmically
	/// spaced bands, lets each band's bar fall at a limited rate, and holds each band's peak before
	/// letting it fall too. The widget only draws what this class has already decided.
	/// </para>
	/// <para>
	/// A level of <see cref="float.NegativeInfinity"/> means "nothing", matching the
	/// <c>peakDb</c> convention of <see cref="DbMeter"/>: a new state reports it for every band.
	/// </para>
	/// </remarks>
	public sealed class SpectrumAnalyzerState
	{
		private readonly float[] edges;
		private readonly float[] levels;
		private readonly float[] peaks;
		private readonly float[] holdRemaining;
		private readonly float[] bandTargets;

		/// <summary>
		/// Initializes a new instance of the <see cref="SpectrumAnalyzerState"/> class.
		/// </summary>
		/// <param name="bandCount">How many bands to divide the frequency range into.</param>
		/// <param name="minFrequency">The lower edge of the lowest band, in hertz.</param>
		/// <param name="maxFrequency">The upper edge of the highest band, in hertz.</param>
		/// <exception cref="ArgumentOutOfRangeException">
		/// <paramref name="bandCount"/> is less than one, <paramref name="minFrequency"/> is not positive,
		/// or <paramref name="maxFrequency"/> is not above <paramref name="minFrequency"/>.
		/// </exception>
		public SpectrumAnalyzerState(int bandCount = 32, float minFrequency = 20f, float maxFrequency = 20000f)
		{
			ArgumentOutOfRangeException.ThrowIfLessThan(bandCount, 1);

			// The axis enforces the frequency range: a logarithmic axis has no zero, and a reversed or
			// empty range has no bands.
			Axis = new LogFrequencyAxis(minFrequency, maxFrequency);

			BandCount = bandCount;

			edges = new float[bandCount + 1];
			double ratio = Math.Log((double)maxFrequency / minFrequency);
			for (int i = 0; i <= bandCount; i++)
			{
				edges[i] = (float)(minFrequency * Math.Exp(ratio * i / bandCount));
			}

			// Pin the ends exactly, so the outermost edges are the values the caller passed rather
			// than whatever exp(log(x)) rounds to.
			edges[0] = minFrequency;
			edges[bandCount] = maxFrequency;

			levels = new float[bandCount];
			peaks = new float[bandCount];
			holdRemaining = new float[bandCount];
			bandTargets = new float[bandCount];
			Reset();
		}

		/// <summary>Gets the number of bands.</summary>
		public int BandCount { get; }

		/// <summary>
		/// Gets the log-frequency axis the bands are spaced along, which is also where the analyzer
		/// draws them. Hand it to <see cref="ParametricEq"/> to line an EQ up with this analyzer.
		/// </summary>
		public LogFrequencyAxis Axis { get; }

		/// <summary>Gets the lower edge of the lowest band, in hertz.</summary>
		public float MinFrequency => Axis.MinFrequency;

		/// <summary>Gets the upper edge of the highest band, in hertz.</summary>
		public float MaxFrequency => Axis.MaxFrequency;

		/// <summary>
		/// Gets or sets how fast a bar may fall, in decibels per second. A bar rises to a louder
		/// level at once. <see cref="float.PositiveInfinity"/> turns the smoothing off, so every bar
		/// shows exactly the latest frame.
		/// </summary>
		public float BarFallRate { get; set; } = 48f;

		/// <summary>Gets or sets how long a peak stays put after it is set, in seconds.</summary>
		public float PeakHoldSeconds { get; set; } = 1f;

		/// <summary>
		/// Gets or sets how fast a peak falls once its hold has run out, in decibels per second. A
		/// peak never falls below its band's bar.
		/// </summary>
		public float PeakFallRate { get; set; } = 12f;

		/// <summary>Gets each band's current bar level, in decibels.</summary>
		public ReadOnlySpan<float> Levels => levels;

		/// <summary>Gets each band's held peak level, in decibels.</summary>
		public ReadOnlySpan<float> Peaks => peaks;

		/// <summary>Gets the lower edge of a band, in hertz.</summary>
		/// <param name="band">The band index.</param>
		/// <returns>The band's lower edge.</returns>
		public float GetBandLowerEdge(int band) => edges[CheckBand(band)];

		/// <summary>Gets the upper edge of a band, in hertz.</summary>
		/// <param name="band">The band index.</param>
		/// <returns>The band's upper edge.</returns>
		public float GetBandUpperEdge(int band) => edges[CheckBand(band) + 1];

		/// <summary>Gets the geometric centre of a band, in hertz, which is its midpoint on the log axis.</summary>
		/// <param name="band">The band index.</param>
		/// <returns>The band's centre frequency.</returns>
		public float GetBandCenter(int band)
		{
			CheckBand(band);
			return MathF.Sqrt(edges[band] * edges[band + 1]);
		}

		/// <summary>Finds the band containing a frequency.</summary>
		/// <param name="frequency">The frequency, in hertz.</param>
		/// <returns>The band index, or -1 when the frequency lies outside every band.</returns>
		/// <remarks>Each band includes its lower edge and excludes its upper one, except the last, which includes both.</remarks>
		public int GetBandIndex(float frequency)
		{
			if (float.IsNaN(frequency) || frequency < MinFrequency || frequency > MaxFrequency)
			{
				return -1;
			}

			int index = Array.BinarySearch(edges, frequency);
			if (index < 0)
			{
				index = ~index - 1;
			}

			return Math.Min(index, BandCount - 1);
		}

		/// <summary>
		/// Maps a frequency to a horizontal position across the analyzer, 0 at
		/// <see cref="MinFrequency"/> and 1 at <see cref="MaxFrequency"/>, on a logarithmic scale.
		/// </summary>
		/// <param name="frequency">The frequency, in hertz.</param>
		/// <returns>The position, not clamped; non-positive frequencies return <see cref="float.NegativeInfinity"/>.</returns>
		/// <remarks>The same mapping as <see cref="LogFrequencyAxis.FrequencyToPosition"/> on <see cref="Axis"/>, which this delegates to.</remarks>
		public float FrequencyToPosition(float frequency) => Axis.FrequencyToPosition(frequency);

		/// <summary>Returns every bar and peak to <see cref="float.NegativeInfinity"/>.</summary>
		public void Reset()
		{
			Array.Fill(levels, float.NegativeInfinity);
			Array.Fill(peaks, float.NegativeInfinity);
			Array.Clear(holdRemaining);
		}

		/// <summary>Folds one frame of FFT magnitudes into the bands and advances the ballistics.</summary>
		/// <param name="binsDb">
		/// The magnitude of each FFT bin in decibels, from 0 Hz to the Nyquist frequency inclusive —
		/// the <c>N / 2 + 1</c> bins a real FFT of size <c>N</c> produces. Convert a linear magnitude
		/// with <c>20 * log10(m)</c>.
		/// </param>
		/// <param name="sampleRate">The sample rate the FFT was taken at, in hertz.</param>
		/// <param name="deltaSeconds">The time since the previous update, in seconds.</param>
		/// <remarks>
		/// <para>
		/// A band takes the loudest bin inside it, so a pure tone reads at its true level whichever
		/// band it lands in. A band narrower than the bin spacing — common at the low end of a log
		/// axis — contains no bin at all, and reads the level interpolated between the two bins
		/// either side of its centre instead of reading silence. A band wholly above the Nyquist
		/// frequency reads <see cref="float.NegativeInfinity"/>.
		/// </para>
		/// <para>
		/// NaN bins are ignored. Fewer than two bins, or a non-positive sample rate, leaves every
		/// band with nothing to read, so the bars fall as they would through silence.
		/// </para>
		/// </remarks>
		public void Update(ReadOnlySpan<float> binsDb, float sampleRate, float deltaSeconds)
		{
			ComputeBandTargets(binsDb, sampleRate, bandTargets);

			float dt = float.IsFinite(deltaSeconds) ? Math.Max(deltaSeconds, 0f) : 0f;
			for (int band = 0; band < BandCount; band++)
			{
				levels[band] = Fall(levels[band], bandTargets[band], BarFallRate, dt);
				AdvancePeak(band, dt);
			}
		}

		private void ComputeBandTargets(ReadOnlySpan<float> binsDb, float sampleRate, Span<float> targets)
		{
			if (binsDb.Length < 2 || sampleRate <= 0f || !float.IsFinite(sampleRate))
			{
				targets.Fill(float.NegativeInfinity);
				return;
			}

			float nyquist = sampleRate * 0.5f;
			float binSpacing = nyquist / (binsDb.Length - 1);

			for (int band = 0; band < BandCount; band++)
			{
				float low = edges[band];
				float high = edges[band + 1];

				if (low > nyquist)
				{
					targets[band] = float.NegativeInfinity;
					continue;
				}

				// Bins whose frequency lies in [low, high), and the last band keeps its upper edge.
				int first = (int)MathF.Ceiling(low / binSpacing);
				float lastExact = high / binSpacing;
				int last = band == BandCount - 1 ? (int)MathF.Floor(lastExact) : (int)MathF.Ceiling(lastExact) - 1;
				last = Math.Min(last, binsDb.Length - 1);

				float loudest = float.NegativeInfinity;
				bool found = false;
				for (int bin = first; bin <= last; bin++)
				{
					float value = binsDb[bin];
					if (float.IsNaN(value))
					{
						continue;
					}

					found = true;
					loudest = Math.Max(loudest, value);
				}

				targets[band] = found ? loudest : Interpolate(binsDb, MathF.Sqrt(low * Math.Min(high, nyquist)) / binSpacing);
			}
		}

		private static float Interpolate(ReadOnlySpan<float> binsDb, float position)
		{
			int below = Math.Clamp((int)MathF.Floor(position), 0, binsDb.Length - 1);
			int above = Math.Min(below + 1, binsDb.Length - 1);
			float a = binsDb[below];
			float b = binsDb[above];

			if (float.IsNaN(a))
			{
				return float.IsNaN(b) ? float.NegativeInfinity : b;
			}

			if (float.IsNaN(b) || below == above)
			{
				return a;
			}

			// Interpolating toward silence would produce NaN from -inf * 0 at the bin itself, and
			// -inf anywhere in between; the louder neighbour is the honest reading of a sliver
			// between a sounding bin and a silent one.
			if (float.IsNegativeInfinity(a) || float.IsNegativeInfinity(b))
			{
				return Math.Max(a, b);
			}

			float t = position - below;
			return a + ((b - a) * t);
		}

		private static float Fall(float current, float target, float rate, float dt)
		{
			if (float.IsNaN(target))
			{
				target = float.NegativeInfinity;
			}

			if (target >= current || float.IsNaN(rate) || float.IsPositiveInfinity(rate))
			{
				return target;
			}

			float fallen = current - (Math.Max(rate, 0f) * dt);
			return Math.Max(fallen, target);
		}

		private void AdvancePeak(int band, float dt)
		{
			float level = levels[band];

			if (level >= peaks[band])
			{
				peaks[band] = level;
				holdRemaining[band] = Math.Max(PeakHoldSeconds, 0f);
				return;
			}

			// A hold that runs out partway through a step spends the rest of that step falling,
			// so a peak's descent does not depend on how the time was sliced into frames.
			float fallTime = dt - holdRemaining[band];
			holdRemaining[band] = Math.Max(holdRemaining[band] - dt, 0f);

			if (fallTime > 0f)
			{
				peaks[band] = Fall(peaks[band], level, PeakFallRate, fallTime);
			}
		}

		private int CheckBand(int band)
		{
			ArgumentOutOfRangeException.ThrowIfNegative(band);
			ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(band, BandCount);
			return band;
		}
	}
}
