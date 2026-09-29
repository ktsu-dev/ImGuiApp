// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;

/// <summary>
/// Shows the gain-reduction, correlation and goniometer meters over a synthesized 220 Hz stereo sine
/// pair whose phase offset and width are under the reader's control.
/// </summary>
internal static class StereoMetersDemo
{
	private const float SampleRate = 48000f;
	private const float ToneFrequency = 220f;
	private const int BlockLength = 1024;
	private const float PeakHoldSeconds = 1.5f;

	private static readonly float[] Left = new float[BlockLength];
	private static readonly float[] Right = new float[BlockLength];
	private static readonly ImGuiWidgets.StereoMetersState State = new();
	private static uint noiseState = 0x2545F491u;
	private static float phaseOffsetDegrees;
	private static float width;
	private static float reductionDb = 6f;
	private static float peakReductionDb;
	private static float peakAge;
	private static double phase;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics,
	/// which outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		phaseOffsetDegrees = 0f;
		width = 0f;
		reductionDb = 6f;
		peakReductionDb = 0f;
		peakAge = 0f;
		phase = 0.0;
		noiseState = 0x2545F491u;
		State.Reset();
	}

	public static void Show()
	{
		if (!DemoProbe.Header("Stereo Meters"))
		{
			return;
		}

		ImGui.TextUnformatted("Gain reduction, phase correlation and a goniometer over a 220 Hz stereo sine:");
		ImGui.Separator();

		DemoProbe.SliderFloat("Phase offset", ref phaseOffsetDegrees, 0f, 180f);
		DemoProbe.SliderFloat("Width", ref width, 0f, 1f);
		DemoProbe.SliderFloat("Reduction", ref reductionDb, 0f, 24f);

		float deltaSeconds = ImGui.GetIO().DeltaTime;
		Synthesize(deltaSeconds);
		HoldPeak(deltaSeconds);
		State.Push(Left, Right, deltaSeconds);

		ImGuiWidgets.GainReductionMeter("##demoGainReduction", reductionDb, new Vector2(24f, 160f), peakReductionDb: peakReductionDb);
		ImGui.SameLine();
		ImGuiWidgets.DbMeter("##demoLeftLevel", LeftLevelDb(), new Vector2(24f, 160f));
		ImGui.SameLine();
		ImGuiWidgets.Goniometer("##demoGoniometer", Left, Right, new Vector2(160f, 160f));

		ImGuiWidgets.CorrelationMeter("##demoCorrelation", State.Correlation, new Vector2(232f, ImGui.GetFrameHeight()));
		ImGui.TextUnformatted("0 degrees is mono and a vertical trace; 180 degrees is inverted and a horizontal one.");
	}

	private static void Synthesize(float deltaSeconds)
	{
		phase = (phase + (2.0 * Math.PI * ToneFrequency * deltaSeconds)) % (2.0 * Math.PI);
		double offset = phaseOffsetDegrees * Math.PI / 180.0;
		double step = 2.0 * Math.PI * ToneFrequency / SampleRate;

		// Width crossfades each channel from the tone to its own noise, so it pulls correlation to zero.
		float tone = 0.8f * (1f - width);
		float noise = 0.8f * width;
		for (int i = 0; i < BlockLength; i++)
		{
			double t = phase + (i * step);
			Left[i] = (tone * (float)Math.Sin(t)) + (noise * NextNoise());
			Right[i] = (tone * (float)Math.Sin(t + offset)) + (noise * NextNoise());
		}
	}

	private static void HoldPeak(float deltaSeconds)
	{
		peakAge += deltaSeconds;
		if (reductionDb >= peakReductionDb || peakAge > PeakHoldSeconds)
		{
			peakReductionDb = reductionDb;
			peakAge = 0f;
		}
	}

	private static float LeftLevelDb()
	{
		float peak = 0f;
		foreach (float sample in Left)
		{
			peak = Math.Max(peak, Math.Abs(sample));
		}

		return peak > 0f ? 20f * MathF.Log10(peak) : float.NegativeInfinity;
	}

	// A xorshift in [-1, 1], rather than System.Random, because the analyzers reject Random wherever it appears.
	private static float NextNoise()
	{
		noiseState ^= noiseState << 13;
		noiseState ^= noiseState >> 17;
		noiseState ^= noiseState << 5;
		return (noiseState / (float)uint.MaxValue * 2f) - 1f;
	}
}
