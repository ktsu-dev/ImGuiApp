// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.WidgetGallery.Catalog;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Widgets;

/// <summary>The tiles in the DataAndSignals group.</summary>
internal static class DataAndSignalsTiles
{
	/// <summary>Builds the group's entries.</summary>
	/// <returns>The entries, in gallery order.</returns>
	public static IEnumerable<GalleryEntry> Build()
	{
		const GalleryCategory Category = GalleryCategory.DataAndSignals;
		float[] bins = BuildHistogram(96);
		Vector2 plot = new(300f, 110f);

		yield return new("Histogram", Category, [nameof(ImGuiWidgets.Histogram)], _ =>
			ImGuiWidgets.Histogram("##histogram", bins, 1, plot));

		float[] levels = [12f, 45f, 88f];
		yield return new("HandleTrack", Category, [nameof(ImGuiWidgets.HandleTrack)], _ =>
		{
			// A levels control: the histogram, with the black point, midpoint and white point on a
			// strip of their own underneath it.
			ImGuiWidgets.Histogram("##levels", bins, 1, plot);
			Vector2 min = ImGui.GetCursorScreenPos();
			Vector2 strip = new(plot.X, 18f);
			ImGuiWidgets.HandleTrack("##handles", levels, min, min + strip, 0f, 100f, 2f);

			// The track restores the cursor rather than reserving its strip.
			ImGui.Dummy(strip);
		});

		List<Vector2> curve = [new(0f, 0f), new(0.3f, 0.2f), new(0.7f, 0.85f), new(1f, 1f)];
		yield return new("CurveTrack", Category, [nameof(ImGuiWidgets.CurveTrack)], _ =>
		{
			ImGuiWidgets.Histogram("##tone", bins, 1, new Vector2(plot.Y * 1.6f, plot.Y * 1.6f));
			Vector2 min = ImGui.GetItemRectMin();
			Vector2 max = ImGui.GetItemRectMax();
			ImGuiWidgets.CurveTrack("##curve", curve, x => SmoothStepThrough(curve, x), min, max, Vector2.Zero, Vector2.One, pinEnds: true, minGap: 0.02f);
			ImGui.Dummy(Vector2.Zero);
		});

		(float Level, float Peak)[] meters = [(-18f, -9f), (-6f, -3f), (-30f, -20f), (1.5f, 2.5f)];
		yield return new("DbMeter", Category, [nameof(ImGuiWidgets.DbMeter)], _ =>
		{
			for (int i = 0; i < meters.Length; i++)
			{
				if (i > 0)
				{
					ImGui.SameLine();
				}

				ImGuiWidgets.DbMeter($"##meter{i}", meters[i].Level, new Vector2(18f, 140f), peakDb: meters[i].Peak);
			}
		});

		float[] wave = BuildWave(256);
		yield return new("Scope", Category, [nameof(ImGuiWidgets.Scope)], _ =>
			ImGuiWidgets.Scope("##scope", wave, plot, 1.2f));

		float[] audio = BuildAudio(48_000);
		float[] peakMinimums = new float[360];
		float[] peakMaximums = new float[360];
		ImGuiWidgets.ComputeWaveformPeaks(audio, peakMinimums, peakMaximums);
		float playhead = 3.4f;
		float loopStart = 5.2f;
		float loopEnd = 7.6f;
		yield return new("Waveform", Category, [nameof(ImGuiWidgets.Waveform), nameof(ImGuiWidgets.ComputeWaveformPeaks)], _ =>
			ImGuiWidgets.Waveform("##waveform", peakMinimums, peakMaximums, 10f, ref playhead, ref loopStart, ref loopEnd, new Vector2(360f, 90f)));

		ImGuiWidgets.SpectrumAnalyzerState spectrum = new(bandCount: 40);
		spectrum.Update(BuildSpectrum(1025, boost: 8f), 48_000f, 0.02f);
		spectrum.Update(BuildSpectrum(1025, boost: 0f), 48_000f, 0.5f);
		yield return new("SpectrumAnalyzer", Category, [nameof(ImGuiWidgets.SpectrumAnalyzer), nameof(ImGuiWidgets.SpectrumAnalyzerState)], _ =>
			ImGuiWidgets.SpectrumAnalyzer("##spectrum", spectrum, new Vector2(360f, 130f)));

		ImGuiWidgets.Envelope envelope = new(0.05f, 0.2f, 0.1f, 0.4f, 0.6f, 0.8f, 0.4f, 0.5f, 0.6f);
		yield return new("EnvelopeEditor", Category, [nameof(ImGuiWidgets.EnvelopeEditor), nameof(ImGuiWidgets.Envelope)], _ =>
			ImGuiWidgets.EnvelopeEditor("##envelope", ref envelope, new Vector2(360f, 130f), 3f));

		FlameGraphSample[] samples =
		[
			new(0f, 100f, 0, "main"),
			new(0f, 38f, 1, "load"),
			new(38f, 96f, 1, "frame"),
			new(0f, 20f, 2, "parse"),
			new(20f, 36f, 2, "decode"),
			new(40f, 70f, 2, "update"),
			new(70f, 94f, 2, "render"),
			new(44f, 60f, 3, "physics"),
			new(72f, 90f, 3, "draw calls"),
		];
		int selectedSample = -1;
		FlameGraphOptions flameOptions = new() { GraphSize = new Vector2(380f, 110f) };
		yield return new("FlameGraph", Category, [nameof(ImGuiWidgets.FlameGraph)], _ =>
			ImGuiWidgets.FlameGraph("##profile", samples, ref selectedSample, flameOptions));
	}

	/// <summary>A bimodal distribution, the shape a photograph's luminance histogram usually has.</summary>
	private static float[] BuildHistogram(int count)
	{
		float[] bins = new float[count];

		for (int i = 0; i < count; i++)
		{
			float x = i / (float)(count - 1);
			float shadows = MathF.Exp(-MathF.Pow((x - 0.25f) / 0.1f, 2f));
			float highlights = 0.7f * MathF.Exp(-MathF.Pow((x - 0.68f) / 0.14f, 2f));
			float ripple = 0.06f * (1f + MathF.Sin(i * 1.7f));
			bins[i] = shadows + highlights + ripple;
		}

		return bins;
	}

	/// <summary>A fundamental with a couple of harmonics, so the trace looks like a signal and not a textbook sine.</summary>
	private static float[] BuildWave(int count)
	{
		float[] samples = new float[count];

		for (int i = 0; i < count; i++)
		{
			float t = i / (float)count * MathF.Tau * 2f;
			samples[i] = (0.6f * MathF.Sin(t)) + (0.25f * MathF.Sin(3f * t)) + (0.1f * MathF.Sin(7f * t));
		}

		return samples;
	}

	/// <summary>
	/// FFT bin levels in decibels from 0 Hz to Nyquist: a pink-ish slope with a bass bump and a couple
	/// of tonal peaks, raised by <paramref name="boost"/> so an earlier louder frame leaves peak markers.
	/// </summary>
	private static float[] BuildSpectrum(int bins, float boost)
	{
		float[] levels = new float[bins];

		for (int i = 0; i < bins; i++)
		{
			float frequency = MathF.Max(i * 24_000f / (bins - 1), 1f);
			float octave = MathF.Log2(frequency / 1000f);
			float slope = -40f - (3.5f * octave);
			float bass = 22f * MathF.Exp(-MathF.Pow(MathF.Log2(frequency / 90f) / 0.8f, 2f));
			float tone = 18f * MathF.Exp(-MathF.Pow(MathF.Log2(frequency / 2500f) / 0.15f, 2f));
			float air = -24f * MathF.Max(0f, MathF.Log2(frequency / 12_000f));
			levels[i] = slope + bass + tone + air + boost - (4f * (1f + MathF.Sin(i * 0.9f)));
		}

		return levels;
	}

	/// <summary>A few phrases of a decaying tone with quieter gaps, so the overview has shape.</summary>
	private static float[] BuildAudio(int count)
	{
		float[] samples = new float[count];

		for (int i = 0; i < count; i++)
		{
			float t = i / (float)count;
			float phrase = t * 6f % 1f;
			float envelope = MathF.Exp(-phrase * 4f) * (0.55f + (0.4f * MathF.Sin(t * 9f)));
			samples[i] = envelope * MathF.Sin(i * 0.31f) * (0.8f + (0.2f * MathF.Sin(i * 0.013f)));
		}

		return samples;
	}

	/// <summary>Interpolates smoothly between consecutive control points, for the curve track's plotted curve.</summary>
	private static float SmoothStepThrough(List<Vector2> points, float x)
	{
		for (int i = 1; i < points.Count; i++)
		{
			if (x <= points[i].X)
			{
				Vector2 a = points[i - 1];
				Vector2 b = points[i];
				float t = b.X > a.X ? (x - a.X) / (b.X - a.X) : 0f;
				t = t * t * (3f - (2f * t));
				return a.Y + ((b.Y - a.Y) * t);
			}
		}

		return points[^1].Y;
	}
}
