// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System;
using System.Numerics;
using System.Threading.Tasks;

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

	// The long clip: ten minutes at 4 kHz is 2.4 M samples, which is why its peak cache is built on
	// a worker thread the first time the section is shown rather than when the demo starts.
	private const int LongClipSampleRate = 4000;
	private const float LongClipSeconds = 600.0f;
	private const float LongLoopStart = 120.0f;
	private const float LongLoopEnd = 150.0f;

	private static Task<ImGuiWidgets.WaveformPeakCache>? longClip;
	private static ImGuiWidgets.TimelineView longView = new();
	private static float longPlayhead;
	private static float longLoopStart = LongLoopStart;
	private static float longLoopEnd = LongLoopEnd;
	private static bool longPlaying;
	private static WaveformChange longLastChange;

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
		longView = new();
		longPlayhead = 0.0f;
		longLoopStart = LongLoopStart;
		longLoopEnd = LongLoopEnd;
		longPlaying = false;
		longLastChange = WaveformChange.None;
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

		ShowLongClip();
	}

	// A synthesized ten-minute clip: a pulse whose tempo and loudness drift over the whole length,
	// with a single loud click every twenty seconds. The clicks are one sample wide, so they are
	// what shows that the peak cache keeps a spike at every zoom.
	private static ImGuiWidgets.WaveformPeakCache BuildLongClip()
	{
		float[] samples = new float[(int)(LongClipSampleRate * LongClipSeconds)];
		for (int i = 0; i < samples.Length; i++)
		{
			float t = i / (float)LongClipSampleRate;
			float swell = 0.35f + (0.3f * MathF.Sin(t * 0.02f));
			float beat = 0.4f + (0.1f * MathF.Sin(t * 0.005f));
			float envelope = MathF.Exp(-(t % beat) * 12.0f);
			samples[i] = swell * envelope * MathF.Sin(2.0f * MathF.PI * 90.0f * t);
		}

		for (int i = 0; i < samples.Length; i += LongClipSampleRate * 20)
		{
			samples[i] = 0.95f;
		}

		return new ImGuiWidgets.WaveformPeakCache(samples, LongClipSeconds);
	}

	private static void ShowLongClip()
	{
		ImGui.Separator();
		ImGui.TextUnformatted("Long clip: ten minutes, zoomable down to single samples.");

		longClip ??= Task.Run(BuildLongClip);
		if (!longClip.IsCompletedSuccessfully)
		{
			ImGui.TextUnformatted("Building peak cache…");
			return;
		}

		if (longPlaying)
		{
			longPlayhead += ImGui.GetIO().DeltaTime;
			bool hasLoop = longLoopEnd > longLoopStart;
			if (hasLoop && longPlayhead >= longLoopEnd)
			{
				longPlayhead = longLoopStart;
			}
			else if (longPlayhead >= LongClipSeconds)
			{
				longPlayhead = 0.0f;
				longPlaying = false;
			}
		}

		// The scope qualifies the probe name, so a test addresses this one as "Waveform/long clip".
		using (new ImGuiWidgets.ScopedId("Waveform"))
		{
			WaveformChange change = ImGuiWidgets.Waveform(
				"long clip",
				longClip.Result,
				longView,
				ref longPlayhead,
				ref longLoopStart,
				ref longLoopEnd,
				new Vector2(560.0f, 120.0f),
				minLoopLength: 0.1f);

			if (change != WaveformChange.None)
			{
				longLastChange = change;
			}
		}

		if (DemoProbe.Button("Show all##longWaveform"))
		{
			longView.ShowAll();
		}

		ImGui.SameLine();
		if (ImGui.Button(longPlaying ? "Pause##longWaveform" : "Play##longWaveform"))
		{
			longPlaying = !longPlaying;
		}

		ImGui.SameLine();
		bool follow = longView.FollowPlayhead;
		if (ImGui.Checkbox("Follow playhead##longWaveform", ref follow))
		{
			longView.FollowPlayhead = follow;
		}

		ImGui.TextUnformatted("Ctrl+wheel zooms, Shift+wheel scrolls, middle-drag pans.");
		ImGui.TextUnformatted(
			$"View: {longView.ViewStart:0.000}s - {longView.ViewEnd:0.000}s   Playhead: {longPlayhead:0.00}s   Last change: {longLastChange}");
	}
}
