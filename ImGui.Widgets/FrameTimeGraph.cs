// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Buffers;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.ImGui.Probes;
using ktsu.ImGui.Styler;

/// <summary>
/// Options for <see cref="ImGuiWidgets.FrameTimeGraph(string, ReadOnlySpan{float}, FrameTimeGraphOptions?)"/>.
/// </summary>
public sealed record class FrameTimeGraphOptions
{
	/// <summary>
	/// Gets the frame budget in milliseconds. Defaults to 1000/60. Zero, negative or non-finite means
	/// no budget: no lines, and every bar in the plot colour.
	/// </summary>
	public float BudgetMilliseconds { get; init; } = 1000.0f / 60.0f;

	/// <summary>
	/// Gets the value at the top of the box in milliseconds. 0 (the default), negative or non-finite
	/// means automatic: twice the budget, grown to fit a stutter, and never more than eight budgets.
	/// </summary>
	public float ScaleMilliseconds { get; init; }

	/// <summary>Gets a value indicating whether the statistics row is drawn under the graph. Defaults to true.</summary>
	public bool ShowStatistics { get; init; } = true;

	/// <summary>
	/// Gets the box size. A component less than or equal to zero falls back: the width to
	/// <c>CalcItemWidth()</c>, the height to five text lines.
	/// </summary>
	public Vector2 Size { get; init; }
}

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws frame times as bars, oldest left and newest right, coloured against a budget.
	/// </summary>
	/// <param name="label">The ImGui id and probe name of the graph box.</param>
	/// <param name="frameTimesMilliseconds">Frame times in milliseconds, oldest first.</param>
	/// <param name="options">The options; <see langword="null"/> uses the defaults.</param>
	/// <remarks>
	/// Bars at or under the budget use the theme's plot colour, bars between one and two budgets the
	/// warning colour, and bars beyond two budgets the error colour. The scale is fixed or anchored to
	/// the budget rather than to the peak, so a stutter stands out instead of flattening every other
	/// frame, and a spike taller than the scale is drawn full height with a cap. When there are more
	/// frames than pixels of width, only the newest that fit are drawn and summarised. The graph is
	/// display-only and keeps no state; hovering a bar shows its time and age.
	/// </remarks>
	/// <exception cref="ArgumentNullException"><paramref name="label"/> is <see langword="null"/>.</exception>
	public static void FrameTimeGraph(string label, ReadOnlySpan<float> frameTimesMilliseconds, FrameTimeGraphOptions? options = null)
	{
		Ensure.NotNull(label);
		FrameTimeGraphImpl.Draw(label, frameTimesMilliseconds, options ?? FrameTimeGraphImpl.DefaultOptions);
	}

	/// <summary>
	/// Draws a <see cref="FrameTimeHistory"/>. The output is identical to passing its frames as a span.
	/// </summary>
	/// <param name="label">The ImGui id and probe name of the graph box.</param>
	/// <param name="history">The frames to draw, oldest first.</param>
	/// <param name="options">The options; <see langword="null"/> uses the defaults.</param>
	/// <exception cref="ArgumentNullException"><paramref name="label"/> or <paramref name="history"/> is <see langword="null"/>.</exception>
	public static void FrameTimeGraph(string label, FrameTimeHistory history, FrameTimeGraphOptions? options = null)
	{
		Ensure.NotNull(label);
		Ensure.NotNull(history);

		float[] buffer = ArrayPool<float>.Shared.Rent(Math.Max(history.Count, 1));
		try
		{
			int count = history.CopyTo(buffer);
			FrameTimeGraphImpl.Draw(label, buffer.AsSpan(0, count), options ?? FrameTimeGraphImpl.DefaultOptions);
		}
		finally
		{
			ArrayPool<float>.Shared.Return(buffer);
		}
	}

	internal static class FrameTimeGraphImpl
	{
		internal static readonly FrameTimeGraphOptions DefaultOptions = new();

		private const float BudgetLineThickness = 1.0f;
		private const float SecondBudgetLineAlpha = 0.6f;

		public static void Draw(string label, ReadOnlySpan<float> values, FrameTimeGraphOptions options)
		{
			float lineHeight = ImGui.GetTextLineHeight();
			Vector2 boxSize = new(
				options.Size.X > 0 ? options.Size.X : ImGui.CalcItemWidth(),
				options.Size.Y > 0 ? options.Size.Y : lineHeight * 5.0f);

			Vector2 origin = ImGui.GetCursorScreenPos();
			ImGui.Dummy(boxSize);
			ImGuiProbes.MarkItem(label);
			bool hovered = ImGui.IsItemHovered();

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			Span<Vector4> styleColors = ImGui.GetStyle().Colors;
			Vector2 min = origin;
			Vector2 max = new(origin.X + boxSize.X, origin.Y + boxSize.Y);

			drawList.AddRectFilled(min, max, ImGui.GetColorU32(styleColors[(int)ImGuiCol.FrameBg]));

			int start = FrameTimeGraphMath.VisibleStart(values.Length, boxSize.X);
			ReadOnlySpan<float> visible = values[start..];
			float budget = FrameTimeGraphMath.ResolveBudget(options.BudgetMilliseconds);
			float scale = FrameTimeGraphMath.ResolveScale(visible, budget, options.ScaleMilliseconds);

			ImColor warning = Palette.Semantic.Warning;
			ImColor error = Palette.Semantic.Error;
			HistogramBarColors barColors = new(
				Normal: ImGui.GetColorU32(styleColors[(int)ImGuiCol.PlotHistogram]),
				WarningAbove: budget > 0.0f ? budget : float.PositiveInfinity,
				Warning: warning.ToImGuiU32(),
				ErrorAbove: budget > 0.0f ? 2.0f * budget : float.PositiveInfinity,
				Error: error.ToImGuiU32());

			HistogramImpl.DrawBars(drawList, min, max, visible, scale, barColors, ImGui.GetColorU32(styleColors[(int)ImGuiCol.PlotLinesHovered]));

			if (budget > 0.0f)
			{
				DrawBudgetLine(drawList, min, max, budget, scale, warning.ToImGuiU32());
				DrawBudgetLine(drawList, min, max, 2.0f * budget, scale, error.WithAlpha(SecondBudgetLineAlpha).ToImGuiU32());
			}

			if (hovered && visible.Length > 0)
			{
				DrawHover(drawList, min, max, values, start, scale);
			}

			if (options.ShowStatistics)
			{
				using (new ScopedId(label))
				{
					ImGui.TextDisabled(FrameTimeGraphMath.FormatStatistics(visible, budget));
					ImGuiProbes.MarkItem("stats");
				}
			}
		}

		private static void DrawBudgetLine(ImDrawListPtr drawList, Vector2 min, Vector2 max, float milliseconds, float scale, uint color)
		{
			if (float.IsNaN(milliseconds) || float.IsNaN(scale) || milliseconds >= scale)
			{
				return;
			}

			float height = max.Y - min.Y;
			float lineY = MathF.Round(max.Y - (milliseconds / scale * height));
			drawList.AddRectFilled(new Vector2(min.X, lineY), new Vector2(max.X, lineY + BudgetLineThickness), color);

			string text = string.Create(CultureInfo.InvariantCulture, $"{milliseconds:0.#} ms");
			Vector2 textSize = ImGui.CalcTextSize(text);
			float textX = max.X - textSize.X - ImGui.GetStyle().FramePadding.X;
			float textY = MathF.Max(lineY - textSize.Y - 1.0f, min.Y);
			drawList.AddText(new Vector2(textX, textY), ImGui.GetColorU32(ImGuiCol.TextDisabled), text);
		}

		private static void DrawHover(ImDrawListPtr drawList, Vector2 min, Vector2 max, ReadOnlySpan<float> values, int start, float scale)
		{
			int visibleCount = values.Length - start;
			float barWidth = (max.X - min.X) / visibleCount;
			float mouseX = ImGui.GetIO().MousePos.X;
			int slot = Math.Clamp((int)((mouseX - min.X) / barWidth), 0, visibleCount - 1);
			int index = start + slot;
			float value = values[index];

			float x = min.X + (slot * barWidth);
			float top = max.Y - 1.0f;
			if (float.IsFinite(value) && value > 0.0f && float.IsFinite(scale) && scale > 0.0f)
			{
				top = max.Y - (MathF.Min(value / scale, 1.0f) * (max.Y - min.Y));
			}

			drawList.AddRectFilled(new Vector2(x, top), new Vector2(x + barWidth, max.Y), ImGui.GetColorU32(ImGuiCol.PlotHistogramHovered));

			ImGui.SetTooltip(FrameTimeGraphMath.FormatBarTooltip(value, values.Length - 1 - index));
		}
	}
}

/// <summary>
/// The arithmetic and text behind <see cref="ImGuiWidgets.FrameTimeGraph(string, ReadOnlySpan{float}, FrameTimeGraphOptions?)"/>
/// and the statistics of <see cref="FrameTimeHistory"/>. Has no ImGui dependency. Non-finite and
/// negative entries are skipped everywhere, as if absent.
/// </summary>
internal static class FrameTimeGraphMath
{
	private const float AutomaticPercentile = 99.0f;
	private const float MaximumBudgetsOfScale = 8.0f;

	/// <summary>Returns the value at the top of the box.</summary>
	/// <param name="values">The frames to fit.</param>
	/// <param name="budget">The resolved budget, 0 for none.</param>
	/// <param name="requested">The caller's explicit scale; not finite and positive means automatic.</param>
	/// <returns>The scale in milliseconds, always finite and positive.</returns>
	public static float ResolveScale(ReadOnlySpan<float> values, float budget, float requested)
	{
		if (float.IsFinite(requested) && requested > 0.0f)
		{
			return requested;
		}

		if (float.IsFinite(budget) && budget > 0.0f)
		{
			float grown = MathF.Max(2.0f * budget, NiceCeiling(Percentile(values, AutomaticPercentile)));
			return MathF.Min(grown, MaximumBudgetsOfScale * budget);
		}

		float peak = NiceCeiling(Maximum(values));
		return peak > 0.0f ? peak : 1.0f;
	}

	/// <summary>Returns <paramref name="budget"/> when it is finite and positive, otherwise 0 (none).</summary>
	/// <param name="budget">The caller's budget.</param>
	/// <returns>The budget, or 0.</returns>
	public static float ResolveBudget(float budget) => float.IsFinite(budget) && budget > 0.0f ? budget : 0.0f;

	/// <summary>Returns the smallest <c>m · 10^k</c> at or above <paramref name="x"/>, with m in {1, 2, 2.5, 5}.</summary>
	/// <param name="x">The value to round up.</param>
	/// <returns>The rounded value, or 0 for a non-positive or non-finite <paramref name="x"/>.</returns>
	public static float NiceCeiling(float x)
	{
		if (!float.IsFinite(x) || x <= 0.0f)
		{
			return 0.0f;
		}

		double decade = Math.Pow(10.0, Math.Floor(Math.Log10(x)));
		ReadOnlySpan<double> steps = [1.0, 2.0, 2.5, 5.0, 10.0];
		foreach (double step in steps)
		{
			float candidate = (float)(step * decade);
			if (candidate >= x)
			{
				return candidate;
			}
		}

		// Unreachable for any finite x, since 10 · decade always exceeds it; kept so the method
		// cannot return less than its input.
		return (float)(10.0 * decade);
	}

	/// <summary>Returns the mean of the valid values, or 0 when there are none.</summary>
	/// <param name="values">The frames.</param>
	/// <returns>The mean.</returns>
	public static float Average(ReadOnlySpan<float> values)
	{
		double sum = 0.0;
		int count = 0;
		foreach (float value in values)
		{
			if (IsValid(value))
			{
				sum += value;
				count++;
			}
		}

		return count == 0 ? 0.0f : (float)(sum / count);
	}

	/// <summary>Returns the smallest valid value, or 0 when there are none.</summary>
	/// <param name="values">The frames.</param>
	/// <returns>The minimum.</returns>
	public static float Minimum(ReadOnlySpan<float> values)
	{
		float result = float.PositiveInfinity;
		foreach (float value in values)
		{
			if (IsValid(value) && value < result)
			{
				result = value;
			}
		}

		return float.IsPositiveInfinity(result) ? 0.0f : result;
	}

	/// <summary>Returns the largest valid value, or 0 when there are none.</summary>
	/// <param name="values">The frames.</param>
	/// <returns>The maximum.</returns>
	public static float Maximum(ReadOnlySpan<float> values)
	{
		float result = 0.0f;
		foreach (float value in values)
		{
			if (IsValid(value) && value > result)
			{
				result = value;
			}
		}

		return result;
	}

	/// <summary>Returns the nearest-rank percentile of the valid values.</summary>
	/// <param name="values">The frames.</param>
	/// <param name="p">The percentile, clamped to <c>[0, 100]</c>. <see cref="float.NaN"/> reads as 0.</param>
	/// <returns>The value at that rank, or 0 when there are none.</returns>
	public static float Percentile(ReadOnlySpan<float> values, float p)
	{
		float clamped = float.IsNaN(p) ? 0.0f : Math.Clamp(p, 0.0f, 100.0f);

		float[] sorted = ArrayPool<float>.Shared.Rent(Math.Max(values.Length, 1));
		try
		{
			int count = 0;
			foreach (float value in values)
			{
				if (IsValid(value))
				{
					sorted[count++] = value;
				}
			}

			if (count == 0)
			{
				return 0.0f;
			}

			Span<float> held = sorted.AsSpan(0, count);
			held.Sort();
			int index = Math.Clamp((int)MathF.Ceiling(clamped / 100.0f * count) - 1, 0, count - 1);
			return held[index];
		}
		finally
		{
			ArrayPool<float>.Shared.Return(sorted);
		}
	}

	/// <summary>Counts the valid values strictly greater than <paramref name="budget"/>.</summary>
	/// <param name="values">The frames.</param>
	/// <param name="budget">The budget. Not finite and positive counts nothing.</param>
	/// <returns>How many frames went over budget.</returns>
	public static int CountOver(ReadOnlySpan<float> values, float budget)
	{
		if (!float.IsFinite(budget) || budget <= 0.0f)
		{
			return 0;
		}

		int count = 0;
		foreach (float value in values)
		{
			if (IsValid(value) && value > budget)
			{
				count++;
			}
		}

		return count;
	}

	/// <summary>Formats the statistics row.</summary>
	/// <param name="values">The frames summarised.</param>
	/// <param name="budget">The resolved budget, 0 for none.</param>
	/// <returns>The row, or <c>"no frames"</c> when there are no valid values.</returns>
	public static string FormatStatistics(ReadOnlySpan<float> values, float budget)
	{
		if (!HasValid(values))
		{
			return "no frames";
		}

		float average = Average(values);
		string text = string.Create(
			CultureInfo.InvariantCulture,
			$"avg {average:0.0} ms ({Fps(average)}) · p99 {Percentile(values, 99.0f):0.0} ms · max {Maximum(values):0.0} ms");

		return budget > 0.0f
			? string.Create(CultureInfo.InvariantCulture, $"{text} · {CountOver(values, budget)} over budget")
			: text;
	}

	/// <summary>Formats the tooltip for one bar.</summary>
	/// <param name="milliseconds">The frame's time.</param>
	/// <param name="framesAgo">How many frames before the newest it is; 0 is the newest.</param>
	/// <returns>The time and rate on one line, and the frame's age on the next.</returns>
	public static string FormatBarTooltip(float milliseconds, int framesAgo)
	{
		string age = framesAgo switch
		{
			0 => "latest",
			1 => "1 frame ago",
			_ => string.Create(CultureInfo.InvariantCulture, $"{framesAgo} frames ago"),
		};

		return string.Create(CultureInfo.InvariantCulture, $"{milliseconds:0.00} ms ({Fps(milliseconds)})\n{age}");
	}

	/// <summary>Returns the index of the first frame drawn, so every drawn bar is at least a pixel wide.</summary>
	/// <param name="count">The number of frames.</param>
	/// <param name="boxWidth">The box width in pixels.</param>
	/// <returns>The first index drawn.</returns>
	public static int VisibleStart(int count, float boxWidth) => Math.Max(0, count - Math.Max(1, (int)boxWidth));

	private static string Fps(float milliseconds) =>
		milliseconds > 0.0f
			? string.Create(CultureInfo.InvariantCulture, $"{1000.0f / milliseconds:0} fps")
			: "— fps";

	private static bool IsValid(float value) => float.IsFinite(value) && value >= 0.0f;

	private static bool HasValid(ReadOnlySpan<float> values)
	{
		foreach (float value in values)
		{
			if (IsValid(value))
			{
				return true;
			}
		}

		return false;
	}
}
