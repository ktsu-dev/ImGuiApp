// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;

/// <summary>Shows the spectrum analyzer over a synthesized spectrum: a pink noise floor, a pulsing bass note and a sweeping tone.</summary>
internal static class SpectrumAnalyzerDemo
{
	private const float SampleRate = 48000f;

	// The 2049 bins a 4096-point real FFT produces. Nothing here takes an FFT: the demo writes the
	// magnitudes straight into the bins, which keeps it about the widget rather than about DSP.
	private const int BinCount = 2049;
	private const float SweepSeconds = 8f;

	private static readonly float[] Bins = new float[BinCount];
	private static uint noiseState = 0x9E3779B9u;
	private static ImGuiWidgets.SpectrumAnalyzerState state = CreateState(32);
	private static int bandCount = 32;
	private static float peakHold = 1f;
	private static float barFall = 48f;
	private static float time;
	private static bool lineStyle;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics,
	/// which outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		bandCount = 32;
		peakHold = 1f;
		barFall = 48f;
		time = 0f;
		lineStyle = false;
		noiseState = 0x9E3779B9u;
		state = CreateState(bandCount);
	}

	private static ImGuiWidgets.SpectrumAnalyzerState CreateState(int bands) => new(bands, 20f, 20000f);

	public static void Show()
	{
		if (!DemoProbe.Header("Spectrum Analyzer"))
		{
			return;
		}

		ImGui.TextUnformatted("Log-frequency bars with peak hold, coloured in DbMeter's zones:");
		ImGui.Separator();

		if (DemoProbe.SliderInt("Bands", ref bandCount, 8, 96))
		{
			state = CreateState(bandCount);
		}

		DemoProbe.SliderFloat("Peak hold (s)", ref peakHold, 0f, 3f);
		DemoProbe.SliderFloat("Bar fall (dB/s)", ref barFall, 6f, 200f);
		DemoProbe.Checkbox("Draw as a line", ref lineStyle);
		state.PeakHoldSeconds = peakHold;
		state.BarFallRate = barFall;

		time += ImGui.GetIO().DeltaTime;
		Synthesize(time);
		ImGuiWidgets.SpectrumAnalyzer(
			"##demoSpectrum",
			state,
			Bins,
			SampleRate,
			new Vector2(480f, 180f),
			style: lineStyle ? ImGuiWidgets.SpectrumAnalyzerStyle.Line : ImGuiWidgets.SpectrumAnalyzerStyle.Bars);

		ImGui.TextUnformatted("The tone sweeps 50 Hz to 10 kHz; watch each band's peak hang, then fall.");
	}

	private static void Synthesize(float seconds)
	{
		float spacing = SampleRate / 2f / (BinCount - 1);

		// A pink floor falls 3 dB per octave; the jitter is what a real analyzer's floor looks like.
		for (int bin = 0; bin < BinCount; bin++)
		{
			float frequency = Math.Max(bin * spacing, spacing);
			Bins[bin] = -60f - (3f * MathF.Log2(frequency / 100f)) + (NextNoise() * 6f) - 3f;
		}

		float sweep = seconds % SweepSeconds / SweepSeconds;
		AddTone(50f * MathF.Pow(200f, sweep), -8f, spacing);

		// Pulses once a second, like a kick drum under the sweep.
		float pulse = 1f - (seconds % 1f);
		AddTone(80f, -40f + (38f * pulse * pulse), spacing);
	}

	// A xorshift, rather than System.Random, because the jitter only has to look like noise and
	// the analyzers reject Random wherever it appears.
	private static float NextNoise()
	{
		noiseState ^= noiseState << 13;
		noiseState ^= noiseState >> 17;
		noiseState ^= noiseState << 5;
		return noiseState / (float)uint.MaxValue;
	}

	private static void AddTone(float frequency, float db, float spacing)
	{
		int center = (int)MathF.Round(frequency / spacing);

		// A little leakage into the neighbouring bins, as a windowed FFT would show.
		for (int offset = -2; offset <= 2; offset++)
		{
			int bin = center + offset;
			if (bin is >= 0 and < BinCount)
			{
				Bins[bin] = Math.Max(Bins[bin], db - (Math.Abs(offset) * 12f));
			}
		}
	}
}
