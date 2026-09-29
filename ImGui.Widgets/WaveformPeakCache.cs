// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// A <see cref="WaveformPeakSource"/> over samples held in memory, summarised at several
	/// resolutions so a column costs a handful of lookups however far the view is zoomed out.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Level 0 is the samples themselves. Level <c>k</c> holds the lowest and highest sample of each
	/// block of <c>16 × 4^(k−1)</c> samples, and levels are added until one block covers the whole
	/// clip, so the summary costs about a sixth of the samples again. A request picks the coarsest
	/// level whose blocks are no wider than a column and reduces every block each column touches,
	/// which is what keeps a single-sample transient in view at every zoom: a column can over-report
	/// by up to one block either side, never under-report.
	/// </para>
	/// <para>
	/// Building it walks every sample, so build it off the render thread for a long clip. Once
	/// built it is read-only and safe to query from any thread. Non-finite samples are skipped; a
	/// column with nothing finite in it reads as silence.
	/// </para>
	/// </remarks>
	public sealed class WaveformPeakCache : WaveformPeakSource
	{
		private const int FirstBlock = 16;
		private const int BlockGrowth = 4;

		private readonly float[] samples;

		// levels[k - 1] holds level k. An empty block is stored as (+∞, −∞) so combining blocks never
		// has to ask whether one was empty; it only reads as silence when a whole column is.
		private readonly List<(float[] Minimums, float[] Maximums, int BlockSize)> levels = [];

		/// <summary>Initializes a new instance of the <see cref="WaveformPeakCache"/> class.</summary>
		/// <param name="samples">The samples, typically -1..1. Copied, so the caller may reuse the buffer.</param>
		/// <param name="duration">The length of the clip, on the timeline the waveform shows.</param>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="duration"/> is not positive and finite.</exception>
		public WaveformPeakCache(ReadOnlySpan<float> samples, float duration)
		{
			if (!float.IsFinite(duration) || duration <= 0f)
			{
				throw new ArgumentOutOfRangeException(nameof(duration), duration, "The duration must be positive and finite.");
			}

			Duration = duration;
			this.samples = samples.ToArray();
			BuildLevels();
		}

		/// <inheritdoc/>
		public override float Duration { get; }

		/// <summary>Gets how many samples the cache holds.</summary>
		public int SampleCount => samples.Length;

		/// <summary>Gets how many resolutions the cache holds, the samples themselves included.</summary>
		public int LevelCount => levels.Count + 1;

		/// <inheritdoc/>
		/// <exception cref="ArgumentException">The two spans differ in length.</exception>
		public override void GetPeaks(float startPosition, float endPosition, Span<float> minimums, Span<float> maximums)
		{
			if (minimums.Length != maximums.Length)
			{
				throw new ArgumentException("The minimum and maximum buffers must be the same length.", nameof(maximums));
			}

			int columns = minimums.Length;
			if (columns == 0)
			{
				return;
			}

			minimums.Clear();
			maximums.Clear();

			if (!float.IsFinite(startPosition) || !float.IsFinite(endPosition) || startPosition >= endPosition || samples.Length == 0)
			{
				return;
			}

			double rate = samples.Length / (double)Duration;
			double start = startPosition * rate;
			double span = (endPosition - (double)startPosition) * rate;
			int level = SelectLevel((float)(span / columns));

			for (int column = 0; column < columns; column++)
			{
				// Multiplied before dividing, so a column edge that lands on a whole sample lands on it
				// exactly rather than a rounding short of it.
				double left = start + (column * span / columns);
				double right = start + ((column + 1) * span / columns);
				long first = Math.Max((long)Math.Floor(left), 0L);
				long last = Math.Min((long)Math.Ceiling(right), samples.Length);
				if (first >= last)
				{
					continue;
				}

				Reduce(level, (int)first, (int)last, out float low, out float high);
				if (low <= high)
				{
					minimums[column] = low;
					maximums[column] = high;
				}
			}
		}

		/// <summary>The coarsest level whose blocks are no wider than <paramref name="samplesPerColumn"/>.</summary>
		internal int SelectLevel(float samplesPerColumn)
		{
			int level = 0;
			if (float.IsNaN(samplesPerColumn))
			{
				return level;
			}

			for (int k = 1; k <= levels.Count; k++)
			{
				if (levels[k - 1].BlockSize > samplesPerColumn)
				{
					break;
				}

				level = k;
			}

			return level;
		}

		// The extremes of samples [first, last) at the given level: every block the range touches.
		private void Reduce(int level, int first, int last, out float low, out float high)
		{
			low = float.PositiveInfinity;
			high = float.NegativeInfinity;

			if (level == 0)
			{
				for (int i = first; i < last; i++)
				{
					float sample = samples[i];
					if (float.IsFinite(sample))
					{
						low = MathF.Min(low, sample);
						high = MathF.Max(high, sample);
					}
				}

				return;
			}

			(float[] blockMinimums, float[] blockMaximums, int blockSize) = levels[level - 1];
			int firstBlock = first / blockSize;
			int lastBlock = (last - 1) / blockSize;
			for (int block = firstBlock; block <= lastBlock; block++)
			{
				low = MathF.Min(low, blockMinimums[block]);
				high = MathF.Max(high, blockMaximums[block]);
			}
		}

		private void BuildLevels()
		{
			if (samples.Length == 0)
			{
				return;
			}

			int blockSize = FirstBlock;
			float[] minimums = new float[Blocks(blockSize)];
			float[] maximums = new float[minimums.Length];
			for (int block = 0; block < minimums.Length; block++)
			{
				int start = block * blockSize;
				Reduce(0, start, Math.Min(start + blockSize, samples.Length), out minimums[block], out maximums[block]);
			}

			levels.Add((minimums, maximums, blockSize));

			while (blockSize < samples.Length)
			{
				float[] finerMinimums = minimums;
				float[] finerMaximums = maximums;
				blockSize *= BlockGrowth;
				minimums = new float[Blocks(blockSize)];
				maximums = new float[minimums.Length];

				for (int block = 0; block < minimums.Length; block++)
				{
					float low = float.PositiveInfinity;
					float high = float.NegativeInfinity;
					int end = Math.Min((block + 1) * BlockGrowth, finerMinimums.Length);
					for (int finer = block * BlockGrowth; finer < end; finer++)
					{
						low = MathF.Min(low, finerMinimums[finer]);
						high = MathF.Max(high, finerMaximums[finer]);
					}

					minimums[block] = low;
					maximums[block] = high;
				}

				levels.Add((minimums, maximums, blockSize));
			}
		}

		private int Blocks(int blockSize) => (int)(((long)samples.Length + blockSize - 1) / blockSize);
	}
}
