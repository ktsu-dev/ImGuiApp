// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Sortie;

using System;
using System.Globalization;
using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Probes;

public static partial class ImGuiSortie
{
	/// <summary>
	/// Draws a sync-ratio gauge: a horizontal bar of slanted segments filled to the ratio, a scale beneath it with
	/// ticks every ten percent, and the ratio as a heavy percentage at the right.
	/// </summary>
	/// <param name="label">Caption, also the probe name.</param>
	/// <param name="ratio">The ratio, from 0 to 1; values outside are clamped for the bar but printed as given.</param>
	/// <param name="size">Gauge size; zero width fills, zero height takes three font heights.</param>
	public static void SyncGauge(string label, float ratio, Vector2 size = default)
	{
		Ensure.NotNull(label);
		SortiePalette palette = SortieStyle.Palette;
		float fontSize = ImGui.GetFontSize();
		Vector2 area = SortieDraw.Resolve(size, fontSize * 3.4f);
		Vector2 min = ImGui.GetCursorScreenPos();
		ImGui.Dummy(area);
		ImGuiProbes.MarkItem("sync-gauge", label);

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		string caption = VisibleLabel(label).ToUpperInvariant();
		string percent = (ratio * 100f).ToString("0.0", CultureInfo.InvariantCulture) + "%";
		float percentHeight = area.Y * 0.7f;
		Vector2 percentSize = SortieDraw.MeasureHeavy(percent, percentHeight);
		float barRight = min.X + area.X - percentSize.X - 10f;

		drawList.AddText(min, SortieDraw.U32(palette.LeadDim), caption);
		float barTop = min.Y + fontSize + 2f;
		float barBottom = min.Y + area.Y - fontSize - 2f;
		Vector2 barMin = new(min.X, barTop);
		Vector2 barMax = new(barRight, MathF.Max(barBottom, barTop + 6f));
		drawList.AddRectFilled(barMin, barMax, SortieDraw.U32(palette.Field));
		drawList.AddRect(barMin, barMax, SortieDraw.U32(palette.LeadDim));

		float filled = Math.Clamp(ratio, 0f, 1f);
		int segments = Math.Max(10, (int)((barMax.X - barMin.X) / 9f));
		float segmentWidth = (barMax.X - barMin.X - 4f) / segments;
		float slant = MathF.Min(segmentWidth * 0.5f, (barMax.Y - barMin.Y) * 0.4f);
		Vector4 fill = filled >= 0.8f ? palette.Signal : filled >= 0.4f ? palette.Lead : palette.Alert;
		for (int i = 0; i < segments; i++)
		{
			bool on = (i + 0.5f) / segments <= filled;
			float x0 = barMin.X + 2f + (i * segmentWidth);
			float x1 = x0 + segmentWidth - 2f;
			Vector4 color = on ? fill : SortiePalette.WithAlpha(palette.LeadFaint, 0.35f);
			drawList.AddQuadFilled(
				new Vector2(x0 + slant, barMin.Y + 2f),
				new Vector2(x1 + slant, barMin.Y + 2f),
				new Vector2(x1, barMax.Y - 2f),
				new Vector2(x0, barMax.Y - 2f),
				SortieDraw.U32(color));
		}

		uint tick = SortieDraw.U32(palette.LeadDim);
		for (int i = 0; i <= 10; i++)
		{
			float x = barMin.X + ((barMax.X - barMin.X) * i / 10f);
			float length = i % 5 == 0 ? 6f : 3f;
			drawList.AddLine(new Vector2(x, barMax.Y + 1f), new Vector2(x, barMax.Y + 1f + length), tick);
			if (i % 5 == 0)
			{
				string mark = (i * 10).ToString(CultureInfo.InvariantCulture);
				float markWidth = ImGui.CalcTextSize(mark).X;
				float markX = Math.Clamp(x - (markWidth / 2f), barMin.X, barMax.X - markWidth);
				drawList.AddText(new Vector2(markX, barMax.Y + 6f), tick, mark);
			}
		}

		SortieDraw.HeavyText(drawList, new Vector2(barRight + 10f, min.Y + ((area.Y - percentSize.Y) / 2f)), percentHeight, fill, percent);
	}

	/// <summary>Draws a status lamp: a small square block in the lamp's colour followed by its caption.</summary>
	/// <param name="label">Caption, also the probe name.</param>
	/// <param name="lamp">The lamp's state.</param>
	/// <param name="blink">Whether a lit lamp blinks.</param>
	public static void StatusLamp(string label, SortieLamp lamp, bool blink = false)
	{
		Ensure.NotNull(label);
		SortiePalette palette = SortieStyle.Palette;
		float side = ImGui.GetFrameHeight();
		string caption = VisibleLabel(label).ToUpperInvariant();
		Vector2 captionSize = ImGui.CalcTextSize(caption);
		Vector2 min = ImGui.GetCursorScreenPos();
		ImGui.Dummy(new Vector2(side + 8f + captionSize.X, side));
		ImGuiProbes.MarkItem("status-lamp", label);

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		Vector2 boxMax = min + new Vector2(side);
		bool on = lamp != SortieLamp.Off && (!blink || SortieDraw.BlinkPhase(2f));
		drawList.AddRectFilled(min, boxMax, SortieDraw.U32(on ? palette.LampColor(lamp) : palette.Field));
		drawList.AddRect(min, boxMax, SortieDraw.U32(lamp == SortieLamp.Off ? palette.LeadFaint : palette.LampColor(lamp)), 0f, ImDrawFlags.None, 1.5f);
		drawList.AddText(new Vector2(boxMax.X + 8f, min.Y + ((side - captionSize.Y) / 2f)), SortieDraw.U32(lamp == SortieLamp.Off ? palette.LeadDim : palette.Text), caption);

		if (blink && lamp != SortieLamp.Off)
		{
			AnimationActivity.RequestActiveFrame();
		}
	}

	/// <summary>
	/// Draws a pattern monitor: a waveform traced across a gridded black screen, with a sweep line at the newest sample.
	/// </summary>
	/// <param name="label">Caption, also the probe name.</param>
	/// <param name="samples">The samples, oldest first, each from -1 to 1.</param>
	/// <param name="size">Monitor size; zero width fills, zero height takes six font heights.</param>
	/// <param name="lamp">Colours the trace; <see cref="SortieLamp.Off"/> uses the lead colour.</param>
	public static void PatternMonitor(string label, ReadOnlySpan<float> samples, Vector2 size = default, SortieLamp lamp = SortieLamp.Off)
	{
		Ensure.NotNull(label);
		SortiePalette palette = SortieStyle.Palette;
		Vector2 area = SortieDraw.Resolve(size, ImGui.GetFontSize() * 6f);
		Vector2 min = ImGui.GetCursorScreenPos();
		Vector2 max = min + area;
		ImGui.Dummy(area);
		ImGuiProbes.MarkItem("pattern-monitor", label);

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		drawList.AddRectFilled(min, max, SortieDraw.U32(palette.Field));
		uint grid = SortieDraw.U32(palette.LeadFaint, 0.45f);
		for (int i = 1; i < 8; i++)
		{
			float x = min.X + (area.X * i / 8f);
			drawList.AddLine(new Vector2(x, min.Y), new Vector2(x, max.Y), grid);
		}

		for (int i = 1; i < 4; i++)
		{
			float y = min.Y + (area.Y * i / 4f);
			drawList.AddLine(new Vector2(min.X, y), new Vector2(max.X, y), grid);
		}

		Vector4 trace = lamp == SortieLamp.Off ? palette.Lead : palette.LampColor(lamp);
		if (samples.Length >= 2)
		{
			Vector2[] points = new Vector2[samples.Length];
			for (int i = 0; i < samples.Length; i++)
			{
				float value = Math.Clamp(samples[i], -1f, 1f);
				points[i] = new Vector2(min.X + (area.X * i / (samples.Length - 1)), min.Y + (area.Y * (0.5f - (value * 0.45f))));
			}

			drawList.AddPolyline(ref points[0], points.Length, SortieDraw.U32(trace, 0.3f), ImDrawFlags.None, 4f);
			drawList.AddPolyline(ref points[0], points.Length, SortieDraw.U32(trace), ImDrawFlags.None, 1.5f);
			drawList.AddLine(new Vector2(max.X - 1f, min.Y), new Vector2(max.X - 1f, max.Y), SortieDraw.U32(palette.LeadBright, 0.8f), 2f);
		}

		drawList.AddRect(min, max, SortieDraw.U32(palette.Lead), 0f, ImDrawFlags.None, 1.5f);
		string caption = VisibleLabel(label).ToUpperInvariant();
		Vector2 captionSize = ImGui.CalcTextSize(caption);
		drawList.AddRectFilled(min, min + captionSize + new Vector2(10f, 2f), SortieDraw.U32(palette.Lead));
		drawList.AddText(min + new Vector2(5f, 1f), SortieDraw.U32(palette.Field), caption);
	}
}
