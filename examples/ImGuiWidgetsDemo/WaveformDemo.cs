// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;

/// <summary>Shows the waveform overview over a synthesized clip, with a playhead that plays through the loop.</summary>
internal static class WaveformDemo
{
	private const int SampleRate = 8000;
	private const float ClipSeconds = 6.0f;
	private const int OverviewColumns = 1024;

	private static readonly float[] Minimums = new float[OverviewColumns];
	private static readonly float[] Maximums = new float[OverviewColumns];

	private static float playhead;
	private static float loopStart = 1.5f;
	private static float loopEnd = 3.0f;
	private static bool playing;
	private static bool looping = true;
	private static WaveformChange lastChange;

	static WaveformDemo() => BuildOverview();

	/// <summary>Gets the playhead's position, in seconds.</summary>
	internal static float Playhead => playhead;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics,
	/// which outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		playhead = 0.0f;
		loopStart = 1.5f;
		loopEnd = 3.0f;
		playing = false;
		looping = true;
		lastChange = WaveformChange.None;
	}

	// The overview is built once, from samples, the way a real caller would build it when a clip
	// loads: the widget itself never sees the samples.
	private static void BuildOverview()
	{
		float[] samples = new float[(int)(SampleRate * ClipSeconds)];
		for (int i = 0; i < samples.Length; i++)
		{
			float t = i / (float)SampleRate;

			// A drum-ish hit every half second over a quiet pad, so the transients stand out.
			float sinceHit = t % 0.5f;
			float hit = MathF.Exp(-sinceHit * 18.0f) * MathF.Sin(2.0f * MathF.PI * 110.0f * t);
			float pad = 0.15f * MathF.Sin(2.0f * MathF.PI * 220.0f * t) * (0.5f + (0.5f * MathF.Sin(t * 1.3f)));
			samples[i] = Math.Clamp(hit + pad, -1.0f, 1.0f);
		}

		ImGuiWidgets.ComputeWaveformPeaks(samples, Minimums, Maximums);
	}

	public static void Show()
	{
		if (!DemoProbe.Header("Waveform"))
		{
			return;
		}

		ImGui.TextUnformatted("A waveform overview with a playhead and a loop region:");
		ImGui.Separator();

		if (playing)
		{
			playhead += ImGui.GetIO().DeltaTime;
			float end = looping && loopEnd > loopStart ? loopEnd : ClipSeconds;
			if (playhead >= end)
			{
				playhead = looping && loopEnd > loopStart ? loopStart : 0.0f;
				playing = looping;
			}
		}

		WaveformChange change = ImGuiWidgets.Waveform(
			"##demoWaveform",
			Minimums,
			Maximums,
			ClipSeconds,
			ref playhead,
			ref loopStart,
			ref loopEnd,
			new Vector2(560.0f, 120.0f),
			minLoopLength: 0.1f);

		if (change != WaveformChange.None)
		{
			lastChange = change;
		}

		if (ImGui.Button(playing ? "Pause##waveform" : "Play##waveform"))
		{
			playing = !playing;
		}

		ImGui.SameLine();
		ImGui.Checkbox("Loop##waveform", ref looping);
		ImGui.SameLine();
		if (ImGui.Button("Clear loop##waveform"))
		{
			loopStart = 0.0f;
			loopEnd = 0.0f;
		}

		ImGui.TextUnformatted("Click or drag to seek, drag a loop edge to move it, Shift-drag to draw a new loop.");
		ImGui.TextUnformatted($"Playhead: {playhead:0.00}s   Loop: {loopStart:0.00}s - {loopEnd:0.00}s   Last change: {lastChange}");
	}
}
