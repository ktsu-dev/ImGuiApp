// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Numerics;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives <see cref="ImGuiWidgets.GainReductionMeter"/>, <see cref="ImGuiWidgets.CorrelationMeter"/> and
/// <see cref="ImGuiWidgets.Goniometer"/> on their own. No test calls <c>Mark</c>: each meter marks itself.
/// </summary>
[TestClass]
public sealed class StereoMetersTests : WidgetTest
{
	private const string GainReduction = "gr";
	private const string Correlation = "corr";
	private const string Gonio = "gonio";
	private const float SampleRate = 48000f;

	private static readonly Vector2 MeterSize = new(24f, 200f);
	private static readonly Vector2 PlotSize = new(200f, 200f);

	private float reductionDb;
	private float correlation;
	private float[] left = [];
	private float[] right = [];

	private void DrawGainReduction() => ImGuiWidgets.GainReductionMeter(GainReduction, reductionDb, MeterSize, maxReductionDb: 24f);

	private void DrawCorrelation() => ImGuiWidgets.CorrelationMeter(Correlation, correlation, new Vector2(200f, 20f));

	private void DrawGoniometer() => ImGuiWidgets.Goniometer(Gonio, left, right, PlotSize);

	private static float[] Sine(float amplitude)
	{
		// 1024 samples of 220 Hz is several periods, so the trace sweeps its whole extent.
		float[] samples = new float[1024];
		for (int i = 0; i < samples.Length; i++)
		{
			samples[i] = amplitude * MathF.Sin(2.0f * MathF.PI * 220f * i / SampleRate);
		}

		return samples;
	}

	private static float[] Negate(float[] samples)
	{
		float[] result = new float[samples.Length];
		for (int i = 0; i < samples.Length; i++)
		{
			result[i] = -samples[i];
		}

		return result;
	}

	private bool PixelDiffers(byte[] baseline, int x, int y)
	{
		Span<byte> now = Harness.Target.Pixels;
		int i = ((y * Harness.Options.Width) + x) * 4;
		return baseline[i] != now[i] || baseline[i + 1] != now[i + 1] || baseline[i + 2] != now[i + 2] || baseline[i + 3] != now[i + 3];
	}

	[TestMethod]
	public void StereoMeters_EachMarksItself()
	{
		left = Sine(0.5f);
		right = Sine(0.5f);
		Start(() =>
		{
			DrawGainReduction();
			DrawCorrelation();
			DrawGoniometer();
		});

		Assert.IsTrue(IsVisible(GainReduction), "The gain-reduction meter did not mark itself.");
		Assert.IsTrue(IsVisible(Correlation), "The correlation meter did not mark itself.");
		Assert.IsTrue(IsVisible(Gonio), "The goniometer did not mark itself.");
	}

	[TestMethod]
	public void GainReductionMeter_ReservesTheSizeItIsGiven()
	{
		Start(DrawGainReduction);

		Rectangle rect = RectOf(GainReduction);

		Assert.IsTrue(Math.Abs(rect.Width - MeterSize.X) <= 2, $"The meter reserved {rect.Width}px of width rather than {MeterSize.X}.");
		Assert.IsTrue(Math.Abs(rect.Height - MeterSize.Y) <= 2, $"The meter reserved {rect.Height}px of height rather than {MeterSize.Y}.");
	}

	[TestMethod]
	public void GainReductionMeter_FillsFromTheTopDown()
	{
		reductionDb = 0f;
		Start(DrawGainReduction);
		MoveAway();
		byte[] empty = Snapshot();
		Rectangle rect = RectOf(GainReduction);

		reductionDb = 12f;
		Step(2);

		Rectangle fill = BoundsOfDifference(empty) ?? throw new InvalidOperationException("12 dB of reduction drew nothing.");
		Assert.IsTrue(Math.Abs(fill.MinY - rect.MinY) <= 2, $"The fill started at y={fill.MinY}, not at the top edge y={rect.MinY}.");
		Assert.IsTrue(Math.Abs(fill.MaxY - (rect.MinY + 100)) <= 3, $"Half of full scale filled down to y={fill.MaxY}, not to y={rect.MinY + 100}.");
	}

	[TestMethod]
	public void GainReductionMeter_SignDoesNotMatter()
	{
		reductionDb = -12f;
		Start(DrawGainReduction);
		MoveAway();
		byte[] negative = Snapshot();

		reductionDb = 12f;
		Step(2);

		Assert.AreEqual(0, PixelsChangedSince(negative), "-12 dB and +12 dB of reduction drew differently.");
	}

	[TestMethod]
	public void CorrelationMeter_PositiveFillsRightOfCentre()
	{
		correlation = 0f;
		Start(DrawCorrelation);
		MoveAway();
		byte[] centred = Snapshot();
		Rectangle rect = RectOf(Correlation);
		int centreX = rect.MinX + (rect.Width / 2);

		correlation = 1f;
		Step(2);

		Rectangle fill = BoundsOfDifference(centred) ?? throw new InvalidOperationException("A correlation of +1 drew nothing.");
		Assert.IsGreaterThanOrEqualTo(centreX - 2, fill.MinX, "Positive correlation filled left of centre.");
		Assert.IsGreaterThanOrEqualTo(rect.MaxX - 3, fill.MaxX, "Positive correlation did not fill to the right edge.");
	}

	[TestMethod]
	public void CorrelationMeter_NegativeFillsLeftOfCentre()
	{
		correlation = 0f;
		Start(DrawCorrelation);
		MoveAway();
		byte[] centred = Snapshot();
		Rectangle rect = RectOf(Correlation);
		int centreX = rect.MinX + (rect.Width / 2);

		correlation = -1f;
		Step(2);

		Rectangle fill = BoundsOfDifference(centred) ?? throw new InvalidOperationException("A correlation of -1 drew nothing.");
		Assert.IsLessThanOrEqualTo(centreX + 2, fill.MaxX, "Negative correlation filled right of centre.");
		Assert.IsLessThanOrEqualTo(rect.MinX + 3, fill.MinX, "Negative correlation did not fill to the left edge.");
	}

	[TestMethod]
	public void Goniometer_MonoDrawsAVerticalLine()
	{
		Start(DrawGoniometer);
		byte[] empty = Snapshot();

		left = Sine(0.8f);
		right = Sine(0.8f);
		Step(2);

		Rectangle trace = BoundsOfDifference(empty) ?? throw new InvalidOperationException("Mono material drew no trace.");
		Assert.IsLessThanOrEqualTo(4, trace.Width, $"Mono material drew a trace {trace.Width}px wide rather than a vertical line.");
		Assert.IsGreaterThanOrEqualTo(70, trace.Height, $"Mono material drew a trace only {trace.Height}px tall.");
	}

	[TestMethod]
	public void Goniometer_OppositePolarityDrawsAHorizontalLine()
	{
		Start(DrawGoniometer);
		byte[] empty = Snapshot();

		left = Sine(0.8f);
		right = Negate(left);
		Step(2);

		Rectangle trace = BoundsOfDifference(empty) ?? throw new InvalidOperationException("Opposite-polarity material drew no trace.");
		Assert.IsLessThanOrEqualTo(4, trace.Height, $"Opposite-polarity material drew a trace {trace.Height}px tall rather than a horizontal line.");
		Assert.IsGreaterThanOrEqualTo(70, trace.Width, $"Opposite-polarity material drew a trace only {trace.Width}px wide.");
	}

	[TestMethod]
	public void Goniometer_LeftOnlyRisesUpLeft()
	{
		Start(DrawGoniometer);
		byte[] empty = Snapshot();
		Rectangle rect = RectOf(Gonio);
		int centreX = rect.MinX + (rect.Width / 2);
		int centreY = rect.MinY + (rect.Height / 2);

		left = Sine(0.8f);
		right = new float[left.Length];
		Step(2);

		// Left-only at 0.8 plots side = mid = 0.4, so the trace runs 40 px out along the up-left diagonal.
		Assert.IsTrue(PixelDiffers(empty, centreX - 25, centreY - 25), "Left-only material did not rise up-left.");
		Assert.IsFalse(PixelDiffers(empty, centreX + 25, centreY - 25), "Left-only material drew up-right, where right-only material belongs.");
	}
}
