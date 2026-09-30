// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws one or more binned distributions as overlaid bars.
	/// </summary>
	/// <param name="label">A unique label, used for the probe name.</param>
	/// <param name="bins">
	/// Bin values, laid out as <paramref name="seriesCount"/> contiguous runs of equal length. A
	/// trailing partial run (when <c>bins.Length</c> is not an exact multiple of
	/// <paramref name="seriesCount"/>) is dropped.
	/// </param>
	/// <param name="seriesCount">
	/// How many series <paramref name="bins"/> holds. Values less than or equal to zero draw only
	/// the empty frame.
	/// </param>
	/// <param name="size">The box to draw into. Non-positive components fall back to sensible defaults.</param>
	/// <param name="seriesColors">
	/// One color per series. Entries missing for a series fall back to red, green and blue, then the
	/// theme's plot color; entries beyond <paramref name="seriesCount"/> are ignored. A single-series
	/// histogram with no entry skips the red fallback and goes straight to the theme's plot color.
	/// </param>
	/// <remarks>
	/// Takes bins rather than the data they came from, which is what keeps it usable for any
	/// distribution and keeps a full scan of the source off the render thread. Bars are scaled
	/// against the largest finite, positive bin across every series that is actually drawn — a
	/// trailing partial run dropped because <c>bins.Length</c> is not an exact multiple of
	/// <paramref name="seriesCount"/> never competes for the peak, so the tallest bar always fills
	/// the box and callers do not have to decide what full scale means. Negative, zero,
	/// <see cref="float.NaN"/> and infinite bins are treated as empty and draw nothing for that bar;
	/// they are also excluded when finding the peak, so one bad value cannot flatten every other bar
	/// or poison the scale with <see cref="float.NaN"/>. Degenerate input (an empty or short
	/// <paramref name="bins"/>, a non-positive <paramref name="seriesCount"/>, or a distribution with
	/// no positive finite bin) still reserves layout and draws the empty frame rather than nothing.
	/// </remarks>
	public static void Histogram(string label, ReadOnlySpan<float> bins, int seriesCount, Vector2 size, ReadOnlySpan<uint> seriesColors = default) =>
		HistogramImpl.Draw(label, bins, seriesCount, size, seriesColors);

	internal static class HistogramImpl
	{
		private const float ClipCapHeight = 2.0f;

		public static void Draw(string label, ReadOnlySpan<float> bins, int seriesCount, Vector2 size, ReadOnlySpan<uint> seriesColors)
		{
			float lineHeight = ImGui.GetTextLineHeight();
			Vector2 boxSize = new(
				size.X > 0 ? size.X : ImGui.CalcItemWidth(),
				size.Y > 0 ? size.Y : lineHeight * 6.0f);

			Vector2 origin = ImGui.GetCursorScreenPos();
			ImGui.Dummy(boxSize);
			ImGuiProbes.MarkItem(label);

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			Span<Vector4> colors = ImGui.GetStyle().Colors;
			Vector2 min = origin;
			Vector2 max = new(origin.X + boxSize.X, origin.Y + boxSize.Y);

			// Background is drawn unconditionally so a degenerate call still reserves layout and
			// leaves a sane, visible frame instead of drawing nothing at all.
			drawList.AddRectFilled(min, max, ImGui.GetColorU32(colors[(int)ImGuiCol.FrameBg]));

			if (seriesCount <= 0)
			{
				return;
			}

			int binCount = bins.Length / seriesCount;
			if (binCount <= 0)
			{
				return;
			}

			// Scope the peak search to the range that is actually drawn. bins.Length may not be an
			// exact multiple of seriesCount, in which case a trailing partial run belongs to no
			// series and is never drawn below (see the per-series Slice) — letting it compete for
			// peak would scale every real bar against data that never appears on screen.
			ReadOnlySpan<float> drawn = bins[..(seriesCount * binCount)];

			// Only finite, positive bins can define the scale. A NaN or infinite bin is excluded here
			// rather than allowed to win the max: MathF.Max would otherwise propagate NaN (or an
			// infinite peak would flatten every other bar to zero height), turning one bad sample into
			// a frame with no readable bars at all.
			float peak = 0.0f;
			foreach (float bin in drawn)
			{
				if (float.IsFinite(bin) && bin > peak)
				{
					peak = bin;
				}
			}

			if (peak <= 0.0f)
			{
				return;
			}

			for (int series = 0; series < seriesCount; series++)
			{
				uint color = SeriesColor(series, seriesCount, seriesColors, colors);
				DrawBars(drawList, min, max, bins.Slice(series * binCount, binCount), peak, HistogramBarColors.Solid(color));
			}
		}

		/// <summary>
		/// Draws one run of bars filling <paramref name="min"/>..<paramref name="max"/> from the bottom,
		/// shared by <see cref="Histogram"/> and <see cref="FrameTimeGraph(string, ReadOnlySpan{float}, FrameTimeGraphOptions?)"/>.
		/// </summary>
		/// <param name="drawList">The draw list to add to.</param>
		/// <param name="min">The box's top-left corner.</param>
		/// <param name="max">The box's bottom-right corner.</param>
		/// <param name="values">One value per bar, left to right. Each bar is the box width over <c>values.Length</c> wide.</param>
		/// <param name="scale">The value that fills the box. Not finite and positive draws nothing.</param>
		/// <param name="colors">The colour of each bar, chosen by its value.</param>
		/// <param name="clipCapColor">
		/// When non-zero, a bar whose value exceeds <paramref name="scale"/> gets a 2 px cap in this colour
		/// at the top, so a clipped bar is told apart from one that merely fills the box.
		/// </param>
		/// <remarks>A non-finite value, or one less than or equal to zero, draws nothing but keeps its slot.</remarks>
		internal static void DrawBars(ImDrawListPtr drawList, Vector2 min, Vector2 max, ReadOnlySpan<float> values, float scale, HistogramBarColors colors, uint clipCapColor = 0)
		{
			if (values.IsEmpty || !float.IsFinite(scale) || scale <= 0.0f)
			{
				return;
			}

			float width = max.X - min.X;
			float height = max.Y - min.Y;
			float barWidth = width / values.Length;

			for (int bar = 0; bar < values.Length; bar++)
			{
				float value = values[bar];
				if (!float.IsFinite(value) || value <= 0.0f)
				{
					continue;
				}

				float fraction = MathF.Min(value / scale, 1.0f);
				float x = min.X + (bar * barWidth);
				float top = max.Y - (fraction * height);
				drawList.AddRectFilled(new Vector2(x, top), new Vector2(x + barWidth, max.Y), colors.ColorFor(value));

				if (clipCapColor != 0 && value > scale)
				{
					drawList.AddRectFilled(new Vector2(x, top), new Vector2(x + barWidth, top + ClipCapHeight), clipCapColor);
				}
			}
		}

		// Additive-looking overlays without a blend mode: the default channel colors are given a low
		// alpha so overlapping series read as a lighter mix rather than the last one drawn.
		private static uint SeriesColor(int series, int seriesCount, ReadOnlySpan<uint> seriesColors, ReadOnlySpan<Vector4> styleColors)
		{
			if (series < seriesColors.Length)
			{
				return seriesColors[series];
			}

			if (seriesCount == 1)
			{
				return ImGui.GetColorU32(styleColors[(int)ImGuiCol.PlotHistogram]);
			}

			return series switch
			{
				0 => ImGui.GetColorU32(new Vector4(1.0f, 0.25f, 0.25f, 0.6f)),
				1 => ImGui.GetColorU32(new Vector4(0.25f, 1.0f, 0.25f, 0.6f)),
				2 => ImGui.GetColorU32(new Vector4(0.35f, 0.5f, 1.0f, 0.6f)),
				_ => ImGui.GetColorU32(styleColors[(int)ImGuiCol.PlotHistogram]),
			};
		}
	}
}

/// <summary>
/// The colour of a bar drawn by <c>HistogramImpl.DrawBars</c>, chosen by the bar's value against two thresholds.
/// </summary>
/// <param name="Normal">The colour at or under <paramref name="WarningAbove"/>.</param>
/// <param name="WarningAbove">Values strictly above this are <paramref name="Warning"/>.</param>
/// <param name="Warning">The colour above <paramref name="WarningAbove"/> and at or under <paramref name="ErrorAbove"/>.</param>
/// <param name="ErrorAbove">Values strictly above this are <paramref name="Error"/>.</param>
/// <param name="Error">The colour above <paramref name="ErrorAbove"/>.</param>
internal readonly record struct HistogramBarColors(uint Normal, float WarningAbove, uint Warning, float ErrorAbove, uint Error)
{
	/// <summary>Returns the colour for <paramref name="value"/>.</summary>
	/// <param name="value">The bar's value.</param>
	/// <returns><see cref="Error"/>, <see cref="Warning"/> or <see cref="Normal"/>.</returns>
	public uint ColorFor(float value)
	{
		if (value > ErrorAbove)
		{
			return Error;
		}

		return value > WarningAbove ? Warning : Normal;
	}

	/// <summary>Returns colours that draw every bar in <paramref name="color"/>.</summary>
	/// <param name="color">The one colour.</param>
	/// <returns>The colours.</returns>
	public static HistogramBarColors Solid(uint color) => new(color, float.PositiveInfinity, color, float.PositiveInfinity, color);
}
