// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the peak reduction behind the Waveform overview. All pure — no ImGui context required.
/// </summary>
[TestClass]
public class WaveformStateTests
{
	[TestMethod]
	public void ComputeWaveformPeaks_TakesTheExtremesOfEachColumn()
	{
		float[] samples = [0.1f, -0.5f, 0.9f, 0.2f, -0.2f, 0.3f];
		float[] minimums = new float[3];
		float[] maximums = new float[3];

		ImGuiWidgets.ComputeWaveformPeaks(samples, minimums, maximums);

		float[] expectedMinimums = [-0.5f, 0.2f, -0.2f];
		CollectionAssert.AreEqual(expectedMinimums, minimums);
		float[] expectedMaximums = [0.1f, 0.9f, 0.3f];
		CollectionAssert.AreEqual(expectedMaximums, maximums);
	}

	[TestMethod]
	public void ComputeWaveformPeaks_WithFewerSamplesThanColumns_LeavesNoColumnEmpty()
	{
		float[] samples = [0.5f, -0.5f];
		float[] minimums = new float[4];
		float[] maximums = new float[4];

		ImGuiWidgets.ComputeWaveformPeaks(samples, minimums, maximums);

		float[] expectedMaximums = [0.5f, 0.5f, -0.5f, -0.5f];
		CollectionAssert.AreEqual(expectedMaximums, maximums);
	}

	[TestMethod]
	public void ComputeWaveformPeaks_SkipsNonFiniteSamples()
	{
		float[] samples = [float.NaN, 0.4f, float.PositiveInfinity, float.NaN];
		float[] minimums = new float[2];
		float[] maximums = new float[2];

		ImGuiWidgets.ComputeWaveformPeaks(samples, minimums, maximums);

		Assert.AreEqual(0.4f, maximums[0], 1e-6f);
		Assert.AreEqual(0f, maximums[1], 1e-6f, "A column of nothing finite did not read as silence.");
		Assert.AreEqual(0f, minimums[1], 1e-6f, "A column of nothing finite did not read as silence.");
	}

	[TestMethod]
	public void Reduce_KeepsASpikeNarrowerThanAPixel()
	{
		float[] minimums = new float[1000];
		float[] maximums = new float[1000];
		maximums[503] = 0.8f;

		// Ten columns per pixel: the pixel covering 500..510 must still show the spike at 503.
		Assert.IsTrue(ImGuiWidgets.WaveformImpl.Reduce(minimums, maximums, 0.5f, 0.51f, out _, out float high));
		Assert.AreEqual(0.8f, high, 1e-6f);
	}

	[TestMethod]
	public void Reduce_ClipsToTheBox()
	{
		float[] minimums = [-3f];
		float[] maximums = [3f];

		ImGuiWidgets.WaveformImpl.Reduce(minimums, maximums, 0f, 1f, out float low, out float high);

		Assert.AreEqual(-1f, low, 1e-6f);
		Assert.AreEqual(1f, high, 1e-6f);
	}

	[TestMethod]
	public void Reduce_OfAnEmptyOverview_ReportsNothing()
	{
		Assert.IsFalse(ImGuiWidgets.WaveformImpl.Reduce([], [], 0f, 1f, out _, out _));
	}
}
