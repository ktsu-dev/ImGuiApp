// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Monsoon;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Monsoon.Effects;
using ktsu.ImGui.Probes;

/// <summary>Panels, gauges and controls.</summary>
public static partial class MonsoonWidgets
{
	/// <summary>Opens a pane of dark glass with a lit header, corner brackets and rain running down it. Dispose the result to close it.</summary>
	/// <param name="id">The id, also the probe name.</param>
	/// <param name="title">The header text. Katakana in it are drawn as strokes, so no CJK font is needed; other non-Latin text needs a font that has it.</param>
	/// <param name="size">The size; zero on an axis fills the space left.</param>
	/// <param name="accent">The accent colour; cyan when null.</param>
	/// <param name="rain">Whether rain runs down the glass.</param>
	/// <returns>The scope that ends the panel.</returns>
	public static MonsoonGlassPanel GlassPanel(string id, string title, Vector2 size, Vector4? accent = null, bool rain = true)
	{
		Ensure.NotNull(id);
		Ensure.NotNull(title);
		Vector4 tint = accent ?? MonsoonPalette.Cyan;
		ImGui.PushStyleColor(ImGuiCol.ChildBg, MonsoonPalette.WithAlpha(MonsoonPalette.Panel, 0.82f));
		ImGui.PushStyleColor(ImGuiCol.Border, MonsoonPalette.WithAlpha(tint, 0.45f));
		ImGui.PushStyleVar(ImGuiStyleVar.ChildBorderSize, 1f);
		ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10f, 8f));
		bool visible = ImGui.BeginChild(id, size, ImGuiChildFlags.Borders, ImGuiWindowFlags.None);
		ImGui.PopStyleVar(2);
		ImGui.PopStyleColor(2);

		Vector2 min = ImGui.GetWindowPos();
		Vector2 max = min + ImGui.GetWindowSize();
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		if (rain && MonsoonEffects.Motion != MonsoonMotion.None)
		{
			IReadOnlyList<RainStreak> streaks = RainField.Streaks(max - min, ImGui.GetTime(), Noise.SeedOf(id), 40, 140f * MonsoonEffects.RainSpeedScale);
			foreach (RainStreak streak in streaks)
			{
				drawList.AddLine(min + streak.Top, min + streak.Bottom, NeonDraw.Color(MonsoonPalette.Text, 1f, streak.Alpha), 1f);
			}

			MonsoonEffects.RequestFrame();
		}
		else if (rain)
		{
			IReadOnlyList<RainStreak> streaks = RainField.Streaks(max - min, 0d, Noise.SeedOf(id), 40, 0f);
			foreach (RainStreak streak in streaks)
			{
				drawList.AddLine(min + streak.Top, min + streak.Bottom, NeonDraw.Color(MonsoonPalette.Text, 1f, streak.Alpha), 1f);
			}
		}

		float headerHeight = ImGui.GetTextLineHeight() + 6f;
		drawList.AddRectFilled(min + new Vector2(1f, 1f), new Vector2(max.X - 1f, min.Y + headerHeight), NeonDraw.Color(tint, 1f, 0.14f));
		drawList.AddLine(new Vector2(min.X + 1f, min.Y + headerHeight), new Vector2(max.X - 1f, min.Y + headerHeight), NeonDraw.Color(tint, 1f, 0.7f), 1f);
		drawList.AddRectFilled(min + new Vector2(6f, 6f), min + new Vector2(10f, headerHeight - 5f), NeonDraw.Color(tint));
		NeonDraw.MixedText(drawList, min + new Vector2(16f, 3f), tint, title);
		DrawBrackets(drawList, min, max, tint);

		ImGui.SetCursorPosY(headerHeight + 6f);
		return new MonsoonGlassPanel(id, visible);
	}

	/// <summary>Draws a gauge of discrete segments, the top of its range lit red.</summary>
	/// <param name="label">The label, also the probe name.</param>
	/// <param name="value">The value, 0 to 1.</param>
	/// <param name="segments">How many segments.</param>
	/// <param name="color">The lit colour; cyan when null.</param>
	public static void SegmentedBar(string label, float value, int segments = 24, Vector4? color = null)
	{
		Ensure.NotNull(label);
		int count = Math.Max(1, segments);
		int lit = Segments.LitCount(value, count);
		Vector4 tint = color ?? MonsoonPalette.Cyan;
		Vector2 position = ImGui.GetCursorScreenPos();
		float width = MathF.Max(ImGui.GetContentRegionAvail().X, 40f);
		float lineHeight = ImGui.GetTextLineHeight();
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		drawList.AddText(position, NeonDraw.Color(MonsoonPalette.TextDim), label);
		string percent = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{Math.Clamp(value, 0f, 1f) * 100f:0}%");
		float percentWidth = ImGui.CalcTextSize(percent).X;
		drawList.AddText(new Vector2(position.X + width - percentWidth, position.Y), NeonDraw.Color(tint), percent);
		float barTop = position.Y + lineHeight + 3f;
		float barHeight = MathF.Max(6f, lineHeight * 0.6f);
		float gap = 2f;
		float segmentWidth = MathF.Max(1f, (width - ((count - 1) * gap)) / count);
		for (int i = 0; i < count; i++)
		{
			Vector2 a = new(position.X + (i * (segmentWidth + gap)), barTop);
			Vector2 b = a + new Vector2(segmentWidth, barHeight);
			Vector4 segmentColor = Segments.IsAlarm(i, count) ? MonsoonPalette.Alarm : tint;
			if (i < lit)
			{
				drawList.AddRectFilled(a - new Vector2(1f, 1f), b + new Vector2(1f, 1f), NeonDraw.Color(segmentColor, 1f, 0.25f));
				drawList.AddRectFilled(a, b, NeonDraw.Color(segmentColor));
			}
			else
			{
				drawList.AddRectFilled(a, b, NeonDraw.Color(segmentColor, 1f, 0.1f));
			}
		}

		ImGui.Dummy(new Vector2(width, lineHeight + 3f + barHeight));
		ImGuiProbes.MarkItem(label);
	}

	/// <summary>Draws a button outlined in neon that fills as it is hovered and held.</summary>
	/// <param name="label">The label; text after <c>##</c> is not shown.</param>
	/// <param name="color">The colour; magenta when null.</param>
	/// <returns>Whether the button was clicked this frame.</returns>
	public static bool NeonButton(string label, Vector4? color = null)
	{
		Ensure.NotNull(label);
		Vector4 tint = color ?? MonsoonPalette.Magenta;
		string shown = VisibleLabel(label);
		Vector2 textSize = ImGui.CalcTextSize(shown);
		Vector2 padding = ImGui.GetStyle().FramePadding;
		Vector2 size = textSize + (padding * 2f) + new Vector2(8f, 0f);
		Vector2 min = ImGui.GetCursorScreenPos();
		bool clicked = ImGui.InvisibleButton(label, size);
		bool hovered = ImGui.IsItemHovered();
		bool held = ImGui.IsItemActive();
		if (hovered || held)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		Vector2 max = min + size;
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		float fill = held ? 0.35f : hovered ? 0.15f : 0.04f;
		drawList.AddRectFilled(min, max, NeonDraw.Color(tint, 1f, fill));
		drawList.AddRect(min - new Vector2(2f, 2f), max + new Vector2(2f, 2f), NeonDraw.Color(tint, 1f, 0.15f), 0f, ImDrawFlags.None, 3f);
		drawList.AddRect(min, max, NeonDraw.Color(tint), 0f, ImDrawFlags.None, 1.5f);
		NeonDraw.Text(drawList, min + padding + new Vector2(4f, 0f), held ? MonsoonPalette.Text : tint, shown);
		ImGuiProbes.MarkItem(label);
		return clicked;
	}

	/// <summary>Draws a scanning grid with a sweeping line and a crosshair a click moves.</summary>
	/// <param name="id">The id, also the probe name.</param>
	/// <param name="size">The size in pixels.</param>
	/// <param name="target">The crosshair, 0 to 1 on each axis from the top left.</param>
	/// <returns>Whether a click moved the crosshair this frame.</returns>
	public static bool ScanGrid(string id, Vector2 size, ref Vector2 target)
	{
		Ensure.NotNull(id);
		Vector2 extent = new(MathF.Max(size.X, 16f), MathF.Max(size.Y, 16f));
		Vector2 min = ImGui.GetCursorScreenPos();
		Vector2 max = min + extent;
		bool pressed = ImGui.InvisibleButton(id, extent);
		if (ImGui.IsItemActive() || ImGui.IsItemHovered())
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		bool moved = false;
		if (pressed)
		{
			Vector2 relative = (ImGui.GetIO().MousePos - min) / extent;
			target = Vector2.Clamp(relative, Vector2.Zero, Vector2.One);
			moved = true;
		}

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		drawList.AddRectFilled(min, max, NeonDraw.Color(MonsoonPalette.Background, 1f, 0.9f));
		uint gridColor = NeonDraw.Color(MonsoonPalette.Cyan, 1f, 0.15f);
		const int Divisions = 8;
		for (int i = 1; i < Divisions; i++)
		{
			float x = min.X + (extent.X * i / Divisions);
			float y = min.Y + (extent.Y * i / Divisions);
			drawList.AddLine(new Vector2(x, min.Y), new Vector2(x, max.Y), gridColor, 1f);
			drawList.AddLine(new Vector2(min.X, y), new Vector2(max.X, y), gridColor, 1f);
		}

		drawList.AddRect(min, max, NeonDraw.Color(MonsoonPalette.Cyan, 1f, 0.6f));
		if (MonsoonEffects.Motion != MonsoonMotion.None)
		{
			float sweep = (float)(MonsoonEffects.Time * 0.35 % 1.0);
			float sweepY = min.Y + (extent.Y * sweep);
			drawList.AddRectFilledMultiColor(new Vector2(min.X, sweepY - 18f), new Vector2(max.X, sweepY), 0u, 0u, NeonDraw.Color(MonsoonPalette.Cyan, 1f, 0.18f), NeonDraw.Color(MonsoonPalette.Cyan, 1f, 0.18f));
			drawList.AddLine(new Vector2(min.X, sweepY), new Vector2(max.X, sweepY), NeonDraw.Color(MonsoonPalette.Cyan, 1f, 0.6f), 1f);
			MonsoonEffects.RequestFrame();
		}

		Vector2 point = min + (target * extent);
		uint amber = NeonDraw.Color(MonsoonPalette.Amber);
		drawList.AddLine(new Vector2(min.X, point.Y), new Vector2(max.X, point.Y), NeonDraw.Color(MonsoonPalette.Amber, 1f, 0.35f), 1f);
		drawList.AddLine(new Vector2(point.X, min.Y), new Vector2(point.X, max.Y), NeonDraw.Color(MonsoonPalette.Amber, 1f, 0.35f), 1f);
		drawList.AddRect(point - new Vector2(7f, 7f), point + new Vector2(7f, 7f), amber, 0f, ImDrawFlags.None, 1.5f);
		drawList.AddCircleFilled(point, 2f, amber);
		string readout = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"X {target.X:0.000}  Y {target.Y:0.000}");
		drawList.AddText(new Vector2(min.X + 6f, max.Y - ImGui.GetTextLineHeight() - 4f), amber, readout);
		ImGuiProbes.MarkItem(id);
		return moved;
	}

	/// <summary>Draws a lamp and its label; an alarm lamp blinks.</summary>
	/// <param name="label">The label, also the probe name.</param>
	/// <param name="state">What the lamp shows.</param>
	public static void StatusLamp(string label, MonsoonLampState state)
	{
		Ensure.NotNull(label);
		Vector2 position = ImGui.GetCursorScreenPos();
		float lineHeight = ImGui.GetTextLineHeight();
		float radius = lineHeight * 0.32f;
		Vector2 center = position + new Vector2(radius + 2f, lineHeight * 0.5f);
		Vector4 color = state switch
		{
			MonsoonLampState.Ok => MonsoonPalette.Cyan,
			MonsoonLampState.Warn => MonsoonPalette.Amber,
			MonsoonLampState.Alarm => MonsoonPalette.Alarm,
			_ => MonsoonPalette.TextDim,
		};
		bool lit = state != MonsoonLampState.Off;
		if (state == MonsoonLampState.Alarm && MonsoonEffects.Motion != MonsoonMotion.None)
		{
			lit = MonsoonEffects.Time * 2d % 1d < 0.6d;
			MonsoonEffects.RequestFrame();
		}

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		if (lit)
		{
			drawList.AddCircleFilled(center, radius * 2.2f, NeonDraw.Color(color, 1f, 0.15f));
			drawList.AddCircleFilled(center, radius, NeonDraw.Color(color));
		}
		else
		{
			drawList.AddCircle(center, radius, NeonDraw.Color(color, 1f, 0.5f), 0, 1f);
		}

		float textX = position.X + (radius * 2f) + 10f;
		drawList.AddText(new Vector2(textX, position.Y), NeonDraw.Color(lit ? MonsoonPalette.Text : MonsoonPalette.TextDim), label);
		ImGui.Dummy(new Vector2(textX - position.X + ImGui.CalcTextSize(label).X, lineHeight));
		ImGuiProbes.MarkItem(label);
	}

	/// <summary>Gets the part of a label ImGui shows: everything before <c>##</c>.</summary>
	/// <param name="label">The label.</param>
	/// <returns>The visible part.</returns>
	internal static string VisibleLabel(string label)
	{
		int hidden = label.IndexOf("##", StringComparison.Ordinal);
		return hidden < 0 ? label : label[..hidden];
	}

	private static void DrawBrackets(ImDrawListPtr drawList, Vector2 min, Vector2 max, Vector4 tint)
	{
		const float Arm = 12f;
		uint color = NeonDraw.Color(tint);
		Vector2[][] corners =
		[
			[new(min.X, min.Y + Arm), min, new(min.X + Arm, min.Y)],
			[new(max.X - Arm, min.Y), new(max.X, min.Y), new(max.X, min.Y + Arm)],
			[new(min.X, max.Y - Arm), new(min.X, max.Y), new(min.X + Arm, max.Y)],
			[new(max.X - Arm, max.Y), max, new(max.X, max.Y - Arm)],
		];
		foreach (Vector2[] corner in corners)
		{
			drawList.AddPolyline(ref corner[0], corner.Length, color, ImDrawFlags.None, 2f);
		}
	}
}
