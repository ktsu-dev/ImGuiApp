// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.ImGui.Probes;

public static partial class ImGuiWidgets
{
	/// <summary>Draws a vertical gain-reduction meter that fills from the top edge down.</summary>
	/// <param name="label">ImGui id and probe name.</param>
	/// <param name="reductionDb">Gain reduction in dB. The magnitude is used, so 6 and -6 both mean "6 dB of reduction".</param>
	/// <param name="size">Reserved size; <c>default</c> means 1.5 x 8 text line heights, the same default as <see cref="DbMeter"/>.</param>
	/// <param name="maxReductionDb">Reduction shown at full height. Default 24. Values below 1 are treated as 1.</param>
	/// <param name="peakReductionDb">Optional held peak, drawn as a 2 px line; NaN (the default) draws none. Magnitude is used.</param>
	/// <remarks>
	/// Gain reduction reads "down" on every console, so the fill is drawn in
	/// <see cref="ImGuiCol.PlotHistogram"/> rather than zone-coloured like <see cref="DbMeter"/>.
	/// </remarks>
	public static void GainReductionMeter(string label, float reductionDb, Vector2 size = default, float maxReductionDb = 24f, float peakReductionDb = float.NaN)
	{
		ImGui.PushID(label);
		float lineHeight = ImGui.GetTextLineHeight();
		Vector2 meterSize = new(
			size.X > 0 ? size.X : lineHeight * 1.5f,
			size.Y > 0 ? size.Y : lineHeight * 8.0f);

		Vector2 min = ImGui.GetCursorScreenPos();
		ImGui.Dummy(meterSize);
		ImGuiProbes.MarkItem(label);
		bool hovered = ImGui.IsItemHovered();

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		Span<Vector4> colors = ImGui.GetStyle().Colors;
		Vector2 max = min + meterSize;
		float fullScale = maxReductionDb >= 1.0f ? maxReductionDb : 1.0f;
		float magnitude = MathF.Abs(reductionDb);

		drawList.AddRectFilled(min, max, ImGui.GetColorU32(colors[(int)ImGuiCol.FrameBg]));

		float fill = MeterScale.DbToFraction(magnitude, 0.0f, fullScale);
		if (fill > 0.0f)
		{
			drawList.AddRectFilled(min, new Vector2(max.X, min.Y + (fill * meterSize.Y)), ImGui.GetColorU32(colors[(int)ImGuiCol.PlotHistogram]));
		}

		if (float.IsFinite(peakReductionDb))
		{
			float peakY = min.Y + (MeterScale.DbToFraction(MathF.Abs(peakReductionDb), 0.0f, fullScale) * meterSize.Y);
			drawList.AddLine(new Vector2(min.X, peakY), new Vector2(max.X, peakY), ImGui.GetColorU32(colors[(int)ImGuiCol.Text]), 2.0f);
		}

		drawList.AddRect(min, max, ImGui.GetColorU32(colors[(int)ImGuiCol.Border]));

		if (hovered)
		{
			ShowMeterTooltip(string.Format(CultureInfo.InvariantCulture, "-{0:0.0} dB", magnitude));
		}

		ImGui.PopID();
	}

	/// <summary>Draws a horizontal phase-correlation bar from -1 (left edge) through 0 (centre) to +1 (right edge).</summary>
	/// <param name="label">ImGui id and probe name.</param>
	/// <param name="correlation">Correlation coefficient, normally <see cref="StereoMetersState.Correlation"/>. Clamped to [-1, 1]; NaN draws no fill.</param>
	/// <param name="size">Reserved size; <c>default</c> means (<c>ImGui.CalcItemWidth()</c>, <c>ImGui.GetFrameHeight()</c>).</param>
	/// <remarks>
	/// Negative correlation, which cancels when summed to mono, fills left of centre in the clip
	/// colour; positive correlation fills right of centre in the safe colour.
	/// </remarks>
	public static void CorrelationMeter(string label, float correlation, Vector2 size = default)
	{
		ImGui.PushID(label);
		Vector2 meterSize = new(
			size.X > 0 ? size.X : ImGui.CalcItemWidth(),
			size.Y > 0 ? size.Y : ImGui.GetFrameHeight());

		Vector2 min = ImGui.GetCursorScreenPos();
		ImGui.Dummy(meterSize);
		ImGuiProbes.MarkItem(label);
		bool hovered = ImGui.IsItemHovered();

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		Span<Vector4> colors = ImGui.GetStyle().Colors;
		Vector2 max = min + meterSize;
		float centreX = min.X + (meterSize.X * 0.5f);
		uint border = ImGui.GetColorU32(colors[(int)ImGuiCol.Border]);

		drawList.AddRectFilled(min, max, ImGui.GetColorU32(colors[(int)ImGuiCol.FrameBg]));

		if (!float.IsNaN(correlation))
		{
			float c = Math.Clamp(correlation, -1.0f, 1.0f);
			if (c != 0.0f)
			{
				float endX = centreX + (c * meterSize.X * 0.5f);
				ImColor fillColor = c < 0.0f ? MeterScale.Clip : MeterScale.Safe;
				drawList.AddRectFilled(new Vector2(MathF.Min(centreX, endX), min.Y), new Vector2(MathF.Max(centreX, endX), max.Y), fillColor.ToImGuiU32());
			}
		}

		drawList.AddLine(new Vector2(centreX, min.Y), new Vector2(centreX, max.Y), border);
		drawList.AddRect(min, max, border);

		if (hovered)
		{
			ShowMeterTooltip(string.Format(CultureInfo.InvariantCulture, "{0:+0.00;-0.00;0.00}", correlation));
		}

		ImGui.PopID();
	}

	/// <summary>Draws a goniometer: each (left, right) sample pair plotted as side across, mid up, joined in order.</summary>
	/// <param name="label">ImGui id and probe name.</param>
	/// <param name="left">Left channel block, nominally in [-1, 1].</param>
	/// <param name="right">Right channel block. Only the first min(left.Length, right.Length) pairs are drawn.</param>
	/// <param name="size">Reserved size; <c>default</c> means a square of 8 text line heights.</param>
	/// <param name="amplitude">Gain applied before clamping to [-1, 1], exactly as <see cref="Scope"/> applies it. Default 1.</param>
	/// <remarks>
	/// Mono material draws a vertical line and opposite-polarity material a horizontal one. Side is
	/// negated on screen so left-only material rises up-left, as on hardware goniometers. A pair
	/// containing a non-finite sample breaks the trace rather than being bridged.
	/// </remarks>
	public static void Goniometer(string label, ReadOnlySpan<float> left, ReadOnlySpan<float> right, Vector2 size = default, float amplitude = 1.0f)
	{
		ImGui.PushID(label);
		float lineHeight = ImGui.GetTextLineHeight();
		Vector2 plotSize = new(
			size.X > 0 ? size.X : lineHeight * 8.0f,
			size.Y > 0 ? size.Y : lineHeight * 8.0f);

		Vector2 min = ImGui.GetCursorScreenPos();
		ImGui.Dummy(plotSize);
		ImGuiProbes.MarkItem(label);

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		Span<Vector4> colors = ImGui.GetStyle().Colors;
		Vector2 max = min + plotSize;
		Vector2 centre = min + (plotSize * 0.5f);
		float half = MathF.Min(plotSize.X, plotSize.Y) * 0.5f;
		uint border = ImGui.GetColorU32(colors[(int)ImGuiCol.Border]);

		drawList.AddRectFilled(min, max, ImGui.GetColorU32(colors[(int)ImGuiCol.FrameBg]));

		// Mid (vertical), side (horizontal), and the L and R axes on the diagonals.
		drawList.AddLine(new Vector2(centre.X, min.Y), new Vector2(centre.X, max.Y), border);
		drawList.AddLine(new Vector2(min.X, centre.Y), new Vector2(max.X, centre.Y), border);
		drawList.AddLine(centre + new Vector2(-half, -half), centre + new Vector2(half, half), border);
		drawList.AddLine(centre + new Vector2(half, -half), centre + new Vector2(-half, half), border);

		uint traceColor = ImGui.GetColorU32(colors[(int)ImGuiCol.PlotLines]);
		int count = Math.Min(left.Length, right.Length);
		bool hasPrevious = false;
		Vector2 previous = default;

		for (int i = 0; i < count; i++)
		{
			float l = left[i];
			float r = right[i];
			if (!float.IsFinite(l) || !float.IsFinite(r))
			{
				hasPrevious = false;
				continue;
			}

			Vector2 p = StereoMetersState.ToMidSide(l * amplitude, r * amplitude);
			float side = ClampUnit(p.X);
			float mid = ClampUnit(p.Y);
			Vector2 point = new(centre.X - (side * half), centre.Y - (mid * half));

			if (hasPrevious)
			{
				drawList.AddLine(previous, point, traceColor);
			}

			previous = point;
			hasPrevious = true;
		}

		drawList.AddRect(min, max, border);
		ImGui.PopID();
	}

	/// <summary>
	/// Clamps to <c>[-1, 1]</c>, collapsing <see cref="float.NaN"/> (from a NaN amplitude) to 0 so
	/// the point lands on the centre rather than off the plot.
	/// </summary>
	private static float ClampUnit(float value) => float.IsNaN(value) ? 0.0f : Math.Clamp(value, -1.0f, 1.0f);

	private static void ShowMeterTooltip(string text)
	{
		ImGui.BeginTooltip();
		ImGui.TextUnformatted(text);
		ImGui.EndTooltip();
	}
}
