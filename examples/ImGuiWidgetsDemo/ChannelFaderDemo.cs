// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;

/// <summary>
/// Shows a small mixer: three channel faders over synthesized sources and a master fader metering
/// their power sum.
/// </summary>
internal static class ChannelFaderDemo
{
	private const int ChannelCount = 3;
	private const float PeakHoldSeconds = 1.5f;
	private const float WobbleDb = 3f;

	private static readonly string[] Labels = ["Ch 1##demoFader1", "Ch 2##demoFader2", "Ch 3##demoFader3", "Master##demoFaderMaster"];
	private static readonly float[] SourceDb = [-6f, -12f, -18f];
	private static readonly float[] WobbleRates = [0.7f, 1.3f, 2.1f];
	private static readonly float[] GainDb = new float[ChannelCount + 1];
	private static readonly float[] PeakDb = [float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity];
	private static readonly float[] PeakAge = new float[ChannelCount + 1];
	private static readonly float[] LevelDb = [float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity];
	private static double time;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics,
	/// which outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		Array.Fill(GainDb, 0f);
		Array.Fill(PeakDb, float.NegativeInfinity);
		Array.Fill(PeakAge, 0f);
		Array.Fill(LevelDb, float.NegativeInfinity);
		time = 0.0;
	}

	public static void Show()
	{
		if (!DemoProbe.Header("Channel Fader"))
		{
			return;
		}

		ImGui.TextUnformatted("Drag a fader, click the track to jump, right-click for unity, scroll to nudge (Ctrl for 0.1 dB):");
		ImGui.Separator();

		float deltaSeconds = ImGui.GetIO().DeltaTime;
		time += deltaSeconds;
		Measure();

		Vector2 size = new(60f, 200f);
		for (int i = 0; i < Labels.Length; i++)
		{
			if (i > 0)
			{
				ImGui.SameLine();
			}

			HoldPeak(i, deltaSeconds);
			ImGuiWidgets.ChannelFader(Labels[i], ref GainDb[i], LevelDb[i], PeakDb[i], size);
		}

		ImGui.TextUnformatted(string.Join("   ", Gains()));
	}

	private static void Measure()
	{
		double sumPower = 0.0;
		for (int i = 0; i < ChannelCount; i++)
		{
			float wobble = WobbleDb * MathF.Sin((float)(time * 2.0 * Math.PI * WobbleRates[i]));
			float level = SourceDb[i] + wobble + GainDb[i];
			LevelDb[i] = level;
			if (!float.IsNegativeInfinity(level))
			{
				sumPower += Math.Pow(10.0, level / 10.0);
			}
		}

		LevelDb[ChannelCount] = sumPower > 0.0
			? (float)(10.0 * Math.Log10(sumPower)) + GainDb[ChannelCount]
			: float.NegativeInfinity;
	}

	private static void HoldPeak(int index, float deltaSeconds)
	{
		PeakAge[index] += deltaSeconds;
		if (LevelDb[index] >= PeakDb[index] || PeakAge[index] > PeakHoldSeconds)
		{
			PeakDb[index] = LevelDb[index];
			PeakAge[index] = 0f;
		}
	}

	private static string[] Gains()
	{
		string[] gains = new string[Labels.Length];
		for (int i = 0; i < Labels.Length; i++)
		{
			string name = Labels[i][..Labels[i].IndexOf("##", StringComparison.Ordinal)];
			string value = float.IsNegativeInfinity(GainDb[i])
				? "-inf"
				: GainDb[i].ToString("+0.0;-0.0;0.0", CultureInfo.CurrentCulture);
			gains[i] = $"{name}: {value} dB";
		}

		return gains;
	}
}
