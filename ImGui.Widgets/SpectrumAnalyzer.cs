// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.ImGui.Probes;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>How <see cref="SpectrumAnalyzer(string, SpectrumAnalyzerState, Vector2, float, float, SpectrumAnalyzerStyle, float)"/> draws its bands.</summary>
	public enum SpectrumAnalyzerStyle
	{
		/// <summary>One filled bar per band, the classic graphic-analyzer look.</summary>
		Bars,

		/// <summary>A line through the top of each band, which reads better with many narrow bands.</summary>
		Line,
	}

	/// <summary>
	/// Folds one frame of FFT magnitudes into <paramref name="state"/>, advancing it by the frame's
	/// delta time, and draws it.
	/// </summary>
	/// <param name="label">A unique identifier for the analyzer, and its probe name.</param>
	/// <param name="state">The bands, ballistics and peak hold, kept by the caller across frames.</param>
	/// <param name="binsDb">
	/// The magnitude of each FFT bin in decibels, from 0 Hz to the Nyquist frequency inclusive.
	/// See <see cref="SpectrumAnalyzerState.Update"/>.
	/// </param>
	/// <param name="sampleRate">The sample rate the FFT was taken at, in hertz.</param>
	/// <param name="size">The analyzer size in pixels. Non-positive components fall back to sensible defaults.</param>
	/// <param name="minDb">The level mapped to the bottom of the analyzer.</param>
	/// <param name="maxDb">The level mapped to the top of the analyzer.</param>
	/// <param name="style">Whether to draw bars or a line.</param>
	/// <param name="gridStepDb">The spacing of the horizontal level gridlines, in decibels; zero or less draws none.</param>
	public static void SpectrumAnalyzer(string label, SpectrumAnalyzerState state, ReadOnlySpan<float> binsDb, float sampleRate, Vector2 size, float minDb = -90f, float maxDb = 6f, SpectrumAnalyzerStyle style = SpectrumAnalyzerStyle.Bars, float gridStepDb = 12f)
	{
		Ensure.NotNull(state);
		state.Update(binsDb, sampleRate, ImGui.GetIO().DeltaTime);
		SpectrumAnalyzer(label, state, size, minDb, maxDb, style, gridStepDb);
	}

	/// <summary>
	/// Draws a spectrum analyzer: one bar (or one point of a line) per logarithmically spaced band
	/// of <paramref name="state"/>, each with a peak-hold marker.
	/// </summary>
	/// <param name="label">A unique identifier for the analyzer, and its probe name.</param>
	/// <param name="state">The bands, ballistics and peak hold to draw. Advance it with <see cref="SpectrumAnalyzerState.Update"/>.</param>
	/// <param name="size">The analyzer size in pixels. Non-positive components fall back to sensible defaults.</param>
	/// <param name="minDb">The level mapped to the bottom of the analyzer.</param>
	/// <param name="maxDb">The level mapped to the top of the analyzer.</param>
	/// <param name="style">Whether to draw bars or a line.</param>
	/// <param name="gridStepDb">The spacing of the horizontal level gridlines, in decibels; zero or less draws none.</param>
	/// <remarks>
	/// Bars, line segments and peaks take <see cref="DbMeter"/>'s zone colours — green below -6 dB,
	/// amber up to 0 dB, red above — and the frame is laid out like <see cref="Scope"/>'s. Faint
	/// vertical lines mark each decade (100 Hz, 1 kHz, 10 kHz) inside the range, and horizontal ones
	/// every <paramref name="gridStepDb"/> down from 0 dB. This overload only draws, so the same state
	/// can be shown in more than one place without advancing twice.
	/// </remarks>
	public static void SpectrumAnalyzer(string label, SpectrumAnalyzerState state, Vector2 size, float minDb = -90f, float maxDb = 6f, SpectrumAnalyzerStyle style = SpectrumAnalyzerStyle.Bars, float gridStepDb = 12f)
	{
		Ensure.NotNull(state);

		float lineHeight = ImGui.GetTextLineHeight();
		Vector2 analyzerSize = new(
			size.X > 0 ? size.X : lineHeight * 16.0f,
			size.Y > 0 ? size.Y : lineHeight * 6.0f);

		Vector2 cursorPos = ImGui.GetCursorScreenPos();
		ImGui.PushID(label);
		ImGui.Dummy(analyzerSize);
		ImGui.PopID();
		ImGuiProbes.MarkItem(label);

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		Span<Vector4> colors = ImGui.GetStyle().Colors;
		Vector2 min = cursorPos;
		Vector2 max = new(cursorPos.X + analyzerSize.X, cursorPos.Y + analyzerSize.Y);
		uint borderColor = ImGui.GetColorU32(colors[(int)ImGuiCol.Border]);
		float range = maxDb - minDb;

		drawList.AddRectFilled(min, max, ImGui.GetColorU32(colors[(int)ImGuiCol.FrameBg]));

		// Gridlines, drawn under the bars: decades across, level steps down from 0 dB.
		for (float decade = state.Axis.FirstDecade; decade < state.MaxFrequency; decade *= 10f)
		{
			float x = min.X + (state.FrequencyToPosition(decade) * analyzerSize.X);
			if (x > min.X)
			{
				drawList.AddLine(new Vector2(x, min.Y), new Vector2(x, max.Y), borderColor);
			}
		}

		if (range > 0 && gridStepDb > 0)
		{
			float firstStep = MathF.Ceiling(minDb / gridStepDb) * gridStepDb;
			for (float db = firstStep; db < maxDb; db += gridStepDb)
			{
				float y = max.Y - ((db - minDb) / range * analyzerSize.Y);
				if (y < max.Y)
				{
					drawList.AddLine(new Vector2(min.X, y), new Vector2(max.X, y), borderColor);
				}
			}
		}

		if (range > 0)
		{
			float bandWidth = analyzerSize.X / state.BandCount;

			// A pixel between neighbouring bars once they are wide enough to spare one, so a run of
			// equal bands still reads as bands rather than as one block.
			float gap = style == SpectrumAnalyzerStyle.Bars && bandWidth >= 4f ? 1f : 0f;
			ReadOnlySpan<float> levels = state.Levels;
			ReadOnlySpan<float> peaks = state.Peaks;
			Vector2 previousPoint = default;

			for (int band = 0; band < state.BandCount; band++)
			{
				float left = min.X + (band * bandWidth);
				float right = left + bandWidth - gap;

				float level = levels[band];
				float fill = Math.Clamp((level - minDb) / range, 0f, 1f);
				float top = max.Y - (fill * analyzerSize.Y);

				if (style == SpectrumAnalyzerStyle.Line)
				{
					Vector2 point = new(left + (bandWidth * 0.5f), top);
					if (band > 0)
					{
						// Each segment takes the zone of its louder end, so the line turns red
						// exactly where a bar would have.
						float louder = Math.Max(level, levels[band - 1]);
						drawList.AddLine(previousPoint, point, MeterScale.ZoneColor(louder).ToImGuiU32(), 1.5f);
					}

					previousPoint = point;
				}
				else if (fill > 0f)
				{
					drawList.AddRectFilled(new Vector2(left, top), new Vector2(right, max.Y), MeterScale.ZoneColor(level).ToImGuiU32());
				}

				float peakDb = peaks[band];
				if (float.IsFinite(peakDb) && peakDb > minDb)
				{
					float peak = Math.Clamp((peakDb - minDb) / range, 0f, 1f);
					float peakY = max.Y - (peak * analyzerSize.Y);
					drawList.AddLine(new Vector2(left, peakY), new Vector2(right, peakY), MeterScale.ZoneColor(peakDb).ToImGuiU32(), 2.0f);
				}
			}
		}

		drawList.AddRect(min, max, borderColor);
	}
}
