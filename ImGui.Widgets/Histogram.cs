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
	/// <param name="label">A unique label, used for the ImGui ID and the probe name.</param>
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

			float barWidth = boxSize.X / binCount;

			for (int series = 0; series < seriesCount; series++)
			{
				uint color = SeriesColor(series, seriesCount, seriesColors, colors);
				ReadOnlySpan<float> run = bins.Slice(series * binCount, binCount);

				for (int bin = 0; bin < binCount; bin++)
				{
					float value = run[bin];
					if (!float.IsFinite(value) || value <= 0.0f)
					{
						continue;
					}

					float fraction = MathF.Min(value / peak, 1.0f);
					float x = min.X + (bin * barWidth);
					float top = max.Y - (fraction * boxSize.Y);
					drawList.AddRectFilled(new Vector2(x, top), new Vector2(x + barWidth, max.Y), color);
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
