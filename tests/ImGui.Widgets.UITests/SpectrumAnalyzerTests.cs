// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Numerics;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.SpectrumAnalyzer(string, ImGuiWidgets.SpectrumAnalyzerState, Vector2, float, float, ImGuiWidgets.SpectrumAnalyzerStyle, float)"/> on its own.</summary>
[TestClass]
public sealed class SpectrumAnalyzerTests : WidgetTest
{
	private const string Label = "spectrum";
	private const float SampleRate = 48000f;
	private const int BinCount = 1025;
	private static readonly Vector2 Size = new(320f, 160f);

	private readonly ImGuiWidgets.SpectrumAnalyzerState state = new(bandCount: 16)
	{
		BarFallRate = float.PositiveInfinity,
	};

	private float[] bins = Flat(float.NegativeInfinity);
	private ImGuiWidgets.SpectrumAnalyzerStyle style = ImGuiWidgets.SpectrumAnalyzerStyle.Bars;

	private static float[] Flat(float db)
	{
		float[] built = new float[BinCount];
		Array.Fill(built, db);
		return built;
	}

	private void Draw()
	{
		// No Mark call: the analyzer marks itself under its label.
		ImGuiWidgets.SpectrumAnalyzer(Label, state, bins, SampleRate, Size, style: style);
	}

	private int XOfBand(Rectangle rect, int band) => rect.MinX + (int)((band + 0.5f) * rect.Width / state.BandCount);

	[TestMethod]
	public void SpectrumAnalyzer_ReservesTheSizeItIsGiven()
	{
		Start(Draw);

		Rectangle rect = RectOf(Label);

		Assert.IsTrue(Math.Abs(rect.Width - Size.X) <= 2, $"The analyzer reserved {rect.Width}px of width rather than {Size.X}.");
		Assert.IsTrue(Math.Abs(rect.Height - Size.Y) <= 2, $"The analyzer reserved {rect.Height}px of height rather than {Size.Y}.");
	}

	[TestMethod]
	public void SpectrumAnalyzer_SignalDrawsBars()
	{
		Start(Draw);
		byte[] silent = Snapshot();

		bins = Flat(-20f);
		Step(2);

		Assert.IsTrue(PixelsChangedSince(silent) > 0, "A signal changed nothing on screen.");
	}

	[TestMethod]
	public void SpectrumAnalyzer_BarsTakeTheMeterZoneColours()
	{
		bins = Flat(-30f);
		Start(Draw);
		MoveAway();

		Rectangle rect = RectOf(Label);
		Rgba32 nominal = Harness.Capture().GetPixel(XOfBand(rect, 3), rect.MaxY - 4);

		bins = Flat(3f);
		Step(2);
		Rgba32 hot = Harness.Capture().GetPixel(XOfBand(rect, 3), rect.MinY + 20);

		Assert.IsTrue(nominal.G > nominal.R, $"A -30 dB band drew ({nominal.R}, {nominal.G}, {nominal.B}) rather than green.");
		Assert.IsTrue(hot.R > hot.G, $"A +3 dB band drew ({hot.R}, {hot.G}, {hot.B}) rather than red.");
	}

	[TestMethod]
	public void SpectrumAnalyzer_OnlyTheTonesBandRises()
	{
		Start(Draw);
		MoveAway();

		bins = Flat(float.NegativeInfinity);
		float spacing = SampleRate / 2f / (BinCount - 1);
		bins[(int)MathF.Round(1000f / spacing)] = -10f;
		Step(2);

		Rectangle rect = RectOf(Label);
		int toneBand = state.GetBandIndex(1000f);
		int midY = rect.MinY + (rect.Height / 2);
		Rgba32 lit = Harness.Capture().GetPixel(XOfBand(rect, toneBand), midY);
		Rgba32 dark = Harness.Capture().GetPixel(XOfBand(rect, 1), midY);

		Assert.IsTrue(lit.G > dark.G + 40, $"The 1 kHz band drew ({lit.R}, {lit.G}, {lit.B}), no brighter than a silent band's ({dark.R}, {dark.G}, {dark.B}).");
	}

	[TestMethod]
	public void SpectrumAnalyzer_PeakMarkerStaysAfterTheBarFalls()
	{
		bins = Flat(-40f);
		Start(Draw);
		byte[] quiet = Snapshot();

		bins = Flat(-6.5f);
		Step(2);
		bins = Flat(-40f);
		Step(2);

		Assert.IsTrue(state.Peaks[0] > -10f, $"The peak fell to {state.Peaks[0]} dB inside its hold time.");
		Assert.IsTrue(PixelsChangedSince(quiet) > 0, "The held peak drew no marker once the bars dropped back.");
	}

	[TestMethod]
	public void SpectrumAnalyzer_LineStyleDrawsALineRatherThanFilledBars()
	{
		bins = Flat(-30f);
		Start(Draw);
		MoveAway();

		Rectangle rect = RectOf(Label);
		Rgba32 filled = Harness.Capture().GetPixel(XOfBand(rect, 3), rect.MaxY - 4);

		style = ImGuiWidgets.SpectrumAnalyzerStyle.Line;
		Step(2);
		Rgba32 empty = Harness.Capture().GetPixel(XOfBand(rect, 3), rect.MaxY - 4);

		Assert.IsTrue(filled.G > empty.G + 40, $"The line style still filled below the level: bars drew ({filled.R}, {filled.G}, {filled.B}), the line ({empty.R}, {empty.G}, {empty.B}).");
	}
}
