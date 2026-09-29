// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the bands, ballistics and peak hold behind SpectrumAnalyzer. All pure — no ImGui context required.
/// </summary>
[TestClass]
public class SpectrumAnalyzerStateTests
{
	private const float SampleRate = 48000f;

	// A 4096-point real FFT at 48 kHz: 2049 bins, 11.72 Hz apart.
	private const int BinCount = 2049;
	private const float BinSpacing = SampleRate / 2f / (BinCount - 1);

	private static float[] Silence() => BuildBins(float.NegativeInfinity);

	private static float[] BuildBins(float value)
	{
		float[] bins = new float[BinCount];
		Array.Fill(bins, value);
		return bins;
	}

	private static float[] Tone(float frequency, float db, float floor = -120f)
	{
		float[] bins = BuildBins(floor);
		bins[(int)MathF.Round(frequency / BinSpacing)] = db;
		return bins;
	}

	[TestMethod]
	public void Bands_AreEvenlySpacedOnALogAxis()
	{
		ImGuiWidgets.SpectrumAnalyzerState state = new(bandCount: 3, minFrequency: 10f, maxFrequency: 10000f);

		Assert.AreEqual(10f, state.GetBandLowerEdge(0), 1e-3f);
		Assert.AreEqual(100f, state.GetBandUpperEdge(0), 1e-2f);
		Assert.AreEqual(1000f, state.GetBandUpperEdge(1), 1e-1f);
		Assert.AreEqual(10000f, state.GetBandUpperEdge(2), 1e-3f);
		Assert.AreEqual(MathF.Sqrt(10f * 100f), state.GetBandCenter(0), 1e-2f);
	}

	[TestMethod]
	public void FrequencyToPosition_IsLogarithmic()
	{
		ImGuiWidgets.SpectrumAnalyzerState state = new(bandCount: 3, minFrequency: 10f, maxFrequency: 10000f);

		Assert.AreEqual(0f, state.FrequencyToPosition(10f), 1e-6f);
		Assert.AreEqual(1f / 3f, state.FrequencyToPosition(100f), 1e-5f);
		Assert.AreEqual(1f, state.FrequencyToPosition(10000f), 1e-5f);
		Assert.AreEqual(float.NegativeInfinity, state.FrequencyToPosition(0f));
	}

	[TestMethod]
	public void GetBandIndex_FindsTheBandHoldingAFrequency()
	{
		ImGuiWidgets.SpectrumAnalyzerState state = new(bandCount: 3, minFrequency: 10f, maxFrequency: 10000f);

		Assert.AreEqual(0, state.GetBandIndex(10f));
		Assert.AreEqual(0, state.GetBandIndex(50f));
		Assert.AreEqual(1, state.GetBandIndex(500f));
		Assert.AreEqual(2, state.GetBandIndex(10000f), "The top edge belongs to the last band.");
		Assert.AreEqual(-1, state.GetBandIndex(9f));
		Assert.AreEqual(-1, state.GetBandIndex(10001f));
		Assert.AreEqual(-1, state.GetBandIndex(float.NaN));
	}

	[TestMethod]
	public void Constructor_RejectsRangesALogAxisCannotHold()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ImGuiWidgets.SpectrumAnalyzerState(bandCount: 0));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ImGuiWidgets.SpectrumAnalyzerState(minFrequency: 0f));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ImGuiWidgets.SpectrumAnalyzerState(minFrequency: 1000f, maxFrequency: 1000f));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ImGuiWidgets.SpectrumAnalyzerState(maxFrequency: float.PositiveInfinity));
	}

	[TestMethod]
	public void NewState_ReadsSilenceEverywhere()
	{
		ImGuiWidgets.SpectrumAnalyzerState state = new();

		foreach (float level in state.Levels)
		{
			Assert.AreEqual(float.NegativeInfinity, level);
		}

		foreach (float peak in state.Peaks)
		{
			Assert.AreEqual(float.NegativeInfinity, peak);
		}
	}

	[TestMethod]
	public void Update_ATonesLevelLandsInItsBandAtFullStrength()
	{
		ImGuiWidgets.SpectrumAnalyzerState state = new();

		state.Update(Tone(1000f, -12f), SampleRate, 0.016f);

		int band = state.GetBandIndex(1000f);
		Assert.AreEqual(-12f, state.Levels[band], 1e-6f, "A band takes its loudest bin, so a pure tone reads its true level.");
		Assert.AreEqual(-120f, state.Levels[band - 2], 1e-6f);
		Assert.AreEqual(-120f, state.Levels[band + 2], 1e-6f);
	}

	[TestMethod]
	public void Update_BandsNarrowerThanABinInterpolateRatherThanReadingSilence()
	{
		// 64 bands from 20 Hz: the lowest are far narrower than 11.7 Hz, so most contain no bin.
		ImGuiWidgets.SpectrumAnalyzerState state = new(bandCount: 64);
		float[] bins = BuildBins(-100f);
		bins[1] = -40f;
		bins[2] = -60f;

		state.Update(bins, SampleRate, 0.016f);

		int band = state.GetBandIndex(18.5f * 1.1f);
		float center = state.GetBandCenter(band);
		Assert.IsTrue(center is > BinSpacing and < BinSpacing * 2f, $"The test band's centre {center} Hz is not between bins 1 and 2.");
		float expected = -40f + ((-60f + 40f) * ((center / BinSpacing) - 1f));
		Assert.AreEqual(expected, state.Levels[band], 1e-3f);
	}

	[TestMethod]
	public void Update_BandsAboveNyquistReadSilence()
	{
		ImGuiWidgets.SpectrumAnalyzerState state = new(bandCount: 16, minFrequency: 20f, maxFrequency: 20000f);

		// At 8 kHz, Nyquist is 4 kHz: every band starting above it has nothing to read.
		state.Update(new float[257].AsSpan(), 8000f, 0.016f);

		Assert.AreEqual(0f, state.Levels[0], 1e-6f);
		Assert.AreEqual(float.NegativeInfinity, state.Levels[state.BandCount - 1]);
	}

	[TestMethod]
	public void Update_BarsRiseAtOnceAndFallAtTheirRate()
	{
		ImGuiWidgets.SpectrumAnalyzerState state = new() { BarFallRate = 20f };

		state.Update(BuildBins(-10f), SampleRate, 0.1f);
		Assert.AreEqual(-10f, state.Levels[0], 1e-6f);

		state.Update(BuildBins(-80f), SampleRate, 0.5f);
		Assert.AreEqual(-20f, state.Levels[0], 1e-4f, "20 dB/s for half a second is 10 dB.");

		state.Update(BuildBins(-80f), SampleRate, 10f);
		Assert.AreEqual(-80f, state.Levels[0], 1e-6f, "A bar stops at the level it is falling to.");
	}

	[TestMethod]
	public void Update_AnInfiniteFallRateShowsTheLatestFrame()
	{
		ImGuiWidgets.SpectrumAnalyzerState state = new() { BarFallRate = float.PositiveInfinity };

		state.Update(BuildBins(-10f), SampleRate, 0.016f);
		state.Update(BuildBins(-70f), SampleRate, 0.016f);

		Assert.AreEqual(-70f, state.Levels[0], 1e-6f);
	}

	[TestMethod]
	public void Update_BarsFallThroughSilenceRatherThanVanishing()
	{
		ImGuiWidgets.SpectrumAnalyzerState state = new() { BarFallRate = 10f };

		state.Update(BuildBins(-10f), SampleRate, 0.016f);
		state.Update(Silence(), SampleRate, 1f);

		Assert.AreEqual(-20f, state.Levels[0], 1e-4f);
	}

	[TestMethod]
	public void Peak_HoldsForItsHoldTimeThenFalls()
	{
		ImGuiWidgets.SpectrumAnalyzerState state = new()
		{
			BarFallRate = float.PositiveInfinity,
			PeakHoldSeconds = 1f,
			PeakFallRate = 10f,
		};

		state.Update(BuildBins(-6f), SampleRate, 0.016f);
		state.Update(BuildBins(-60f), SampleRate, 0.9f);
		Assert.AreEqual(-6f, state.Peaks[0], 1e-6f, "The peak moved inside its hold time.");

		state.Update(BuildBins(-60f), SampleRate, 0.6f);
		Assert.AreEqual(-11f, state.Peaks[0], 1e-4f, "The 0.1 s of hold left is spent first; the remaining 0.5 s falls 5 dB.");
	}

	[TestMethod]
	public void Peak_FallsTheSameHoweverTheTimeIsSliced()
	{
		ImGuiWidgets.SpectrumAnalyzerState coarse = new() { BarFallRate = float.PositiveInfinity, PeakHoldSeconds = 0.5f, PeakFallRate = 12f };
		ImGuiWidgets.SpectrumAnalyzerState fine = new() { BarFallRate = float.PositiveInfinity, PeakHoldSeconds = 0.5f, PeakFallRate = 12f };

		coarse.Update(BuildBins(0f), SampleRate, 0.016f);
		fine.Update(BuildBins(0f), SampleRate, 0.016f);

		coarse.Update(BuildBins(-90f), SampleRate, 1.5f);
		for (int i = 0; i < 15; i++)
		{
			fine.Update(BuildBins(-90f), SampleRate, 0.1f);
		}

		Assert.AreEqual(coarse.Peaks[0], fine.Peaks[0], 1e-3f);
		Assert.AreEqual(-12f, coarse.Peaks[0], 1e-3f);
	}

	[TestMethod]
	public void Peak_NeverFallsBelowItsBar()
	{
		ImGuiWidgets.SpectrumAnalyzerState state = new() { BarFallRate = 1f, PeakHoldSeconds = 0f, PeakFallRate = 100f };

		state.Update(BuildBins(-6f), SampleRate, 0.016f);
		state.Update(Silence(), SampleRate, 1f);

		Assert.AreEqual(state.Levels[0], state.Peaks[0], 1e-6f);
	}

	[TestMethod]
	public void Peak_ALouderLevelRestartsTheHold()
	{
		ImGuiWidgets.SpectrumAnalyzerState state = new() { BarFallRate = float.PositiveInfinity, PeakHoldSeconds = 1f, PeakFallRate = 10f };

		state.Update(BuildBins(-20f), SampleRate, 0.016f);
		state.Update(BuildBins(-60f), SampleRate, 0.9f);
		state.Update(BuildBins(-10f), SampleRate, 0.016f);
		state.Update(BuildBins(-60f), SampleRate, 0.9f);

		Assert.AreEqual(-10f, state.Peaks[0], 1e-6f);
	}

	[TestMethod]
	public void Update_IgnoresNaNBinsAndDegenerateInput()
	{
		ImGuiWidgets.SpectrumAnalyzerState state = new() { BarFallRate = float.PositiveInfinity };
		float[] bins = Tone(1000f, -12f);
		bins[(int)MathF.Round(1000f / BinSpacing) + 1] = float.NaN;

		state.Update(bins, SampleRate, 0.016f);
		Assert.AreEqual(-12f, state.Levels[state.GetBandIndex(1000f)], 1e-6f);

		state.Update([], SampleRate, float.NaN);
		state.Update(bins, 0f, -1f);
		foreach (float level in state.Levels)
		{
			Assert.AreEqual(float.NegativeInfinity, level);
		}
	}

	[TestMethod]
	public void Reset_ReturnsEverythingToSilence()
	{
		ImGuiWidgets.SpectrumAnalyzerState state = new();
		state.Update(BuildBins(-6f), SampleRate, 0.016f);

		state.Reset();

		Assert.AreEqual(float.NegativeInfinity, state.Levels[0]);
		Assert.AreEqual(float.NegativeInfinity, state.Peaks[0]);
	}
}
