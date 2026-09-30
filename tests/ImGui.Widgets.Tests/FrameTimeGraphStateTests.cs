// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests <see cref="FrameTimeHistory"/>, <see cref="FrameTimeGraphMath"/> and <see cref="HistogramBarColors"/>.
/// All pure — no ImGui context required.
/// </summary>
[TestClass]
public class FrameTimeGraphStateTests
{
	private const float Budget60 = 1000.0f / 60.0f;

	private static float[] Repeat(int count, float value)
	{
		float[] values = new float[count];
		Array.Fill(values, value);
		return values;
	}

	private static FrameTimeHistory Filled(int capacity, int from, int to)
	{
		FrameTimeHistory history = new(capacity);
		for (int i = from; i <= to; i++)
		{
			history.Add(i);
		}

		return history;
	}

	[TestMethod]
	public void History_AddsInOrder_OldestFirst()
	{
		FrameTimeHistory history = Filled(3, 1, 2);

		Assert.AreEqual(2, history.Count);
		Assert.AreEqual(1.0f, history[0]);
		Assert.AreEqual(2.0f, history[1]);
		Assert.AreEqual(2.0f, history.Latest);
	}

	[TestMethod]
	public void History_WrapsAndDropsTheOldest()
	{
		FrameTimeHistory history = Filled(3, 1, 5);

		Assert.AreEqual(3, history.Count);
		Assert.AreEqual(3.0f, history[0]);
		Assert.AreEqual(5.0f, history[2]);
	}

	[TestMethod]
	public void History_IgnoresNonFiniteAndNegative()
	{
		FrameTimeHistory history = new(3);
		history.Add(float.NaN);
		history.Add(float.PositiveInfinity);
		history.Add(-1.0f);

		Assert.AreEqual(0, history.Count);
	}

	[TestMethod]
	public void History_AddDeltaSeconds_ConvertsToMilliseconds()
	{
		FrameTimeHistory history = new();
		history.AddDeltaSeconds(0.016f);

		Assert.AreEqual(16.0f, history.Latest, 1e-4f);
	}

	[TestMethod]
	public void History_Statistics()
	{
		FrameTimeHistory history = new(4);
		history.Add(10.0f);
		history.Add(20.0f);
		history.Add(30.0f);
		history.Add(40.0f);

		Assert.AreEqual(25.0f, history.Average);
		Assert.AreEqual(10.0f, history.Minimum);
		Assert.AreEqual(40.0f, history.Maximum);
		Assert.AreEqual(2, history.CountOver(25.0f));
		Assert.AreEqual(0, history.CountOver(0.0f));
		Assert.AreEqual(0, history.CountOver(float.NaN));
	}

	[TestMethod]
	public void History_EmptyStatisticsAreZero()
	{
		FrameTimeHistory history = new();

		Assert.AreEqual(0.0f, history.Average);
		Assert.AreEqual(0.0f, history.Minimum);
		Assert.AreEqual(0.0f, history.Maximum);
		Assert.AreEqual(0.0f, history.Latest);
		Assert.AreEqual(0.0f, history.Percentile(99.0f));
	}

	[TestMethod]
	public void History_Percentile_IsNearestRank()
	{
		FrameTimeHistory history = Filled(100, 1, 100);

		Assert.AreEqual(99.0f, history.Percentile(99.0f));
		Assert.AreEqual(50.0f, history.Percentile(50.0f));
		Assert.AreEqual(100.0f, history.Percentile(100.0f));
		Assert.AreEqual(1.0f, history.Percentile(0.0f));
		Assert.AreEqual(100.0f, history.Percentile(150.0f));
	}

	[TestMethod]
	public void History_Clear_EmptiesIt()
	{
		FrameTimeHistory history = Filled(3, 1, 5);

		history.Clear();
		Assert.AreEqual(0, history.Count);

		history.Add(5.0f);
		Assert.AreEqual(5.0f, history[0]);
	}

	[TestMethod]
	public void History_RejectsZeroCapacity() =>
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new FrameTimeHistory(0));

	[TestMethod]
	public void History_CopyTo_IsOldestFirst()
	{
		FrameTimeHistory history = Filled(3, 1, 5);
		float[] destination = new float[3];

		Assert.AreEqual(3, history.CopyTo(destination));
		Assert.AreSequenceEqual([3.0f, 4.0f, 5.0f], destination);
		Assert.ThrowsExactly<ArgumentException>(() => history.CopyTo(new float[2]));
	}

	[TestMethod]
	public void History_Indexer_OutOfRange_Throws()
	{
		FrameTimeHistory history = Filled(3, 1, 2);

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => history[history.Count]);
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => history[-1]);
	}

	[TestMethod]
	public void NiceCeiling_RoundsUpToOneTwoTwoPointFiveFive()
	{
		Assert.AreEqual(1.0f, FrameTimeGraphMath.NiceCeiling(0.7f), 1e-6f);
		Assert.AreEqual(5.0f, FrameTimeGraphMath.NiceCeiling(3.0f));
		Assert.AreEqual(20.0f, FrameTimeGraphMath.NiceCeiling(17.0f));
		Assert.AreEqual(25.0f, FrameTimeGraphMath.NiceCeiling(21.0f));
		Assert.AreEqual(50.0f, FrameTimeGraphMath.NiceCeiling(26.0f));
		Assert.AreEqual(50.0f, FrameTimeGraphMath.NiceCeiling(50.0f));
		Assert.AreEqual(200.0f, FrameTimeGraphMath.NiceCeiling(120.0f));
		Assert.AreEqual(0.0f, FrameTimeGraphMath.NiceCeiling(0.0f));
		Assert.AreEqual(0.0f, FrameTimeGraphMath.NiceCeiling(float.NaN));
	}

	[TestMethod]
	public void ResolveScale_DefaultsToTwiceTheBudget() =>
		Assert.AreEqual(33.333f, FrameTimeGraphMath.ResolveScale(Repeat(60, 10.0f), Budget60, 0.0f), 1e-3f);

	[TestMethod]
	public void ResolveScale_GrowsForSpikes()
	{
		float[] values = [.. Repeat(90, 10.0f), .. Repeat(10, 45.0f)];

		Assert.AreEqual(50.0f, FrameTimeGraphMath.ResolveScale(values, Budget60, 0.0f));
	}

	[TestMethod]
	public void ResolveScale_IsCappedAtEightBudgets() =>
		Assert.AreEqual(133.333f, FrameTimeGraphMath.ResolveScale(Repeat(50, 900.0f), Budget60, 0.0f), 1e-3f);

	[TestMethod]
	public void ResolveScale_WithoutBudget_UsesTheMaximum()
	{
		Assert.AreEqual(10.0f, FrameTimeGraphMath.ResolveScale([3.0f, 7.0f], 0.0f, 0.0f));
		Assert.AreEqual(1.0f, FrameTimeGraphMath.ResolveScale([], 0.0f, 0.0f));
	}

	[TestMethod]
	public void ResolveScale_HonoursAnExplicitScale()
	{
		float[] values = Repeat(60, 10.0f);
		float automatic = FrameTimeGraphMath.ResolveScale(values, Budget60, 0.0f);

		Assert.AreEqual(40.0f, FrameTimeGraphMath.ResolveScale(values, Budget60, 40.0f));
		Assert.AreEqual(automatic, FrameTimeGraphMath.ResolveScale(values, Budget60, float.NaN));
		Assert.AreEqual(automatic, FrameTimeGraphMath.ResolveScale(values, Budget60, -5.0f));
	}

	[TestMethod]
	public void ResolveBudget_RejectsNonPositiveAndNonFinite()
	{
		Assert.AreEqual(0.0f, FrameTimeGraphMath.ResolveBudget(0.0f));
		Assert.AreEqual(0.0f, FrameTimeGraphMath.ResolveBudget(-1.0f));
		Assert.AreEqual(0.0f, FrameTimeGraphMath.ResolveBudget(float.NaN));
		Assert.AreEqual(16.0f, FrameTimeGraphMath.ResolveBudget(16.0f));
	}

	[TestMethod]
	public void VisibleStart_KeepsTheNewestThatFit()
	{
		Assert.AreEqual(700, FrameTimeGraphMath.VisibleStart(1000, 300.0f));
		Assert.AreEqual(0, FrameTimeGraphMath.VisibleStart(10, 300.0f));
		Assert.AreEqual(4, FrameTimeGraphMath.VisibleStart(5, 0.5f));
	}

	[TestMethod]
	public void FormatStatistics_WithBudget() =>
		Assert.AreEqual(
			"avg 25.0 ms (40 fps) · p99 40.0 ms · max 40.0 ms · 2 over budget",
			FrameTimeGraphMath.FormatStatistics([10.0f, 20.0f, 30.0f, 40.0f], 25.0f));

	[TestMethod]
	public void FormatStatistics_WithoutBudget_OmitsTheCount() =>
		Assert.AreEqual(
			"avg 25.0 ms (40 fps) · p99 40.0 ms · max 40.0 ms",
			FrameTimeGraphMath.FormatStatistics([10.0f, 20.0f, 30.0f, 40.0f], 0.0f));

	[TestMethod]
	public void FormatStatistics_Empty()
	{
		Assert.AreEqual("no frames", FrameTimeGraphMath.FormatStatistics([], 25.0f));
		Assert.AreEqual("no frames", FrameTimeGraphMath.FormatStatistics([float.NaN, -1.0f], 25.0f));
	}

	[TestMethod]
	public void FormatBarTooltip_NamesTheFrame()
	{
		Assert.AreEqual("16.67 ms (60 fps)\nlatest", FrameTimeGraphMath.FormatBarTooltip(16.666f, 0));
		Assert.AreEqual("20.00 ms (50 fps)\n1 frame ago", FrameTimeGraphMath.FormatBarTooltip(20.0f, 1));
		Assert.AreEqual("20.00 ms (50 fps)\n3 frames ago", FrameTimeGraphMath.FormatBarTooltip(20.0f, 3));
		Assert.AreEqual("0.00 ms (— fps)\n2 frames ago", FrameTimeGraphMath.FormatBarTooltip(0.0f, 2));
	}

	[TestMethod]
	public void HistogramBarColors_ClassifiesAgainstThresholds()
	{
		HistogramBarColors colors = new(1, 16.667f, 2, 33.333f, 3);

		Assert.AreEqual(1u, colors.ColorFor(10.0f));
		Assert.AreEqual(1u, colors.ColorFor(16.667f));
		Assert.AreEqual(2u, colors.ColorFor(20.0f));
		Assert.AreEqual(3u, colors.ColorFor(40.0f));
	}

	[TestMethod]
	public void HistogramBarColors_Solid_IsOneColour()
	{
		HistogramBarColors colors = HistogramBarColors.Solid(7);

		Assert.AreEqual(7u, colors.ColorFor(1e9f));
		Assert.AreEqual(7u, colors.ColorFor(0.0f));
	}
}
