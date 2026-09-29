// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using static ktsu.ImGui.Widgets.ImGuiWidgets;

/// <summary>
/// Tests the multi-resolution peak summary behind the zoomable Waveform. All pure — no ImGui
/// context required.
/// </summary>
[TestClass]
public class WaveformPeakCacheTests
{
	[TestMethod]
	public void Constructor_RejectsABadDuration()
	{
		foreach (float duration in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
		{
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new WaveformPeakCache([0f], duration), $"A duration of {duration} was accepted.");
		}
	}

	[TestMethod]
	public void Levels_GrowUntilOneBlockCoversTheClip()
	{
		WaveformPeakCache cache = new(new float[10_000], 1f);

		// The samples, then blocks of 16, 64, 256, 1024, 4096 and 16384.
		Assert.AreEqual(7, cache.LevelCount);
		Assert.AreEqual(10_000, cache.SampleCount);
	}

	[TestMethod]
	public void SelectLevel_PicksTheCoarsestNoWiderThanAColumn()
	{
		WaveformPeakCache cache = new(new float[10_000], 1f);

		Assert.AreEqual(0, cache.SelectLevel(10f));
		Assert.AreEqual(1, cache.SelectLevel(16f));
		Assert.AreEqual(1, cache.SelectLevel(63.9f));
		Assert.AreEqual(2, cache.SelectLevel(64f));
		Assert.AreEqual(cache.LevelCount - 1, cache.SelectLevel(1e9f));
		Assert.AreEqual(0, cache.SelectLevel(float.NaN));
	}

	[TestMethod]
	public void GetPeaks_NeverNarrowsOrWidensBeyondOneBlock()
	{
		const int SampleCount = 48_000;
		float[] samples = new float[SampleCount];
		Random random = new(7);
		for (int i = 0; i < samples.Length; i++)
		{
			samples[i] = (float)((random.NextDouble() * 2.0) - 1.0);
		}

		WaveformPeakCache cache = new(samples, 1f);
		const int Columns = 300;
		float[] minimums = new float[Columns];
		float[] maximums = new float[Columns];

		foreach ((float start, float end) in new[] { (0f, 1f), (0.3f, 0.31f), (0.5f, 0.5001f) })
		{
			cache.GetPeaks(start, end, minimums, maximums);

			double span = (end - (double)start) * SampleCount;
			int blockSize = BlockSizeOf(cache.SelectLevel((float)(span / Columns)));

			for (int column = 0; column < Columns; column++)
			{
				double left = (start * (double)SampleCount) + (column * span / Columns);
				double right = (start * (double)SampleCount) + ((column + 1) * span / Columns);
				int first = Math.Clamp((int)Math.Floor(left), 0, SampleCount);
				int last = Math.Clamp((int)Math.Ceiling(right), 0, SampleCount);
				if (first >= last)
				{
					continue;
				}

				(float exactLow, float exactHigh) = Extremes(samples, first, last);
				(float wideLow, float wideHigh) = Extremes(samples, Math.Max(0, first - blockSize), Math.Min(SampleCount, last + blockSize));

				string where = $"window ({start}, {end}), column {column}";
				Assert.IsTrue(maximums[column] >= exactHigh, $"The maximum under-reported the samples it covers: {where}.");
				Assert.IsTrue(minimums[column] <= exactLow, $"The minimum under-reported the samples it covers: {where}.");
				Assert.IsTrue(maximums[column] <= wideHigh, $"The maximum reached further than one block: {where}.");
				Assert.IsTrue(minimums[column] >= wideLow, $"The minimum reached further than one block: {where}.");
			}
		}
	}

	[TestMethod]
	public void GetPeaks_KeepsASingleSampleSpikeAtEveryZoom()
	{
		const int SpikeAt = 523_456;
		float[] samples = new float[1_000_000];
		samples[SpikeAt] = 0.9f;
		WaveformPeakCache cache = new(samples, 10f);

		foreach (int columns in new[] { 100, 1_000, 10_000 })
		{
			foreach ((float start, float end) in new[] { (0f, 10f), (5.2f, 5.3f) })
			{
				float[] minimums = new float[columns];
				float[] maximums = new float[columns];
				cache.GetPeaks(start, end, minimums, maximums);

				int showing = 0;
				for (int column = 0; column < columns; column++)
				{
					if (maximums[column] == 0.9f)
					{
						showing++;
					}
					else
					{
						Assert.AreEqual(0f, maximums[column], $"A column away from the spike was not silent: {columns} columns over ({start}, {end}), column {column}.");
						Assert.AreEqual(0f, minimums[column], $"A column away from the spike was not silent: {columns} columns over ({start}, {end}), column {column}.");
					}
				}

				Assert.IsTrue(showing is >= 1 and <= 2, $"The spike showed in {showing} columns of {columns} over ({start}, {end}).");
			}
		}
	}

	[TestMethod]
	public void GetPeaks_ZoomedPastSampleResolution_RepeatsTheSample()
	{
		float[] samples = [0.1f, -0.2f, 0.3f, -0.4f, 0.5f, -0.6f, 0.7f, -0.8f, 0.9f, -1f];
		WaveformPeakCache cache = new(samples, 1f);
		float[] minimums = new float[100];
		float[] maximums = new float[100];

		cache.GetPeaks(0f, 1f, minimums, maximums);

		for (int column = 0; column < 100; column++)
		{
			Assert.AreEqual(samples[column / 10], maximums[column], $"Column {column}.");
			Assert.AreEqual(samples[column / 10], minimums[column], $"Column {column}.");
		}
	}

	[TestMethod]
	public void GetPeaks_OutsideTheClip_IsSilence()
	{
		WaveformPeakCache cache = new([1f, -1f, 1f, -1f], 4f);
		float[] minimums = [5f, 5f];
		float[] maximums = [5f, 5f];

		foreach ((float start, float end) in new[] { (-2f, -1f), (5f, 3f) })
		{
			cache.GetPeaks(start, end, minimums, maximums);

			float[] silence = [0f, 0f];
			CollectionAssert.AreEqual(silence, minimums, $"({start}, {end}).");
			CollectionAssert.AreEqual(silence, maximums, $"({start}, {end}).");
		}
	}

	[TestMethod]
	public void GetPeaks_SkipsNonFiniteSamples()
	{
		float[] samples = new float[64];
		samples[3] = float.NaN;
		samples[4] = float.PositiveInfinity;
		samples[5] = 0.4f;
		WaveformPeakCache cache = new(samples, 1f);
		float[] minimums = new float[1];
		float[] maximums = new float[1];

		cache.GetPeaks(0f, 1f, minimums, maximums);

		Assert.AreEqual(0.4f, maximums[0], 1e-6f);
		Assert.AreEqual(0f, minimums[0], 1e-6f);
	}

	[TestMethod]
	public void GetPeaks_WithMismatchedBuffers_Throws()
	{
		WaveformPeakCache cache = new([0f], 1f);

		Assert.ThrowsExactly<ArgumentException>(() => cache.GetPeaks(0f, 1f, new float[2], new float[3]));
	}

	private static int BlockSizeOf(int level) => level == 0 ? 1 : 16 << (2 * (level - 1));

	private static (float Low, float High) Extremes(float[] samples, int first, int last)
	{
		float low = float.PositiveInfinity;
		float high = float.NegativeInfinity;
		for (int i = first; i < last; i++)
		{
			low = MathF.Min(low, samples[i]);
			high = MathF.Max(high, samples[i]);
		}

		return (low, high);
	}
}
