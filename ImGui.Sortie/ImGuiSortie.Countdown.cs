// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Sortie;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Probes;

public static partial class ImGuiSortie
{
	/// <summary>
	/// Draws a seven-segment countdown reading minutes, seconds and hundredths, under a caption. It turns to the
	/// alert colour once less than <paramref name="warnBelow"/> remains, and blinks at zero.
	/// </summary>
	/// <param name="label">Caption, also the probe name.</param>
	/// <param name="remaining">Time remaining; negative values read as zero.</param>
	/// <param name="digitHeight">Height of a digit; zero takes three times the font size.</param>
	/// <param name="warnBelow">Remaining time under which the readout turns to the alert colour; zero uses one minute.</param>
	public static void Countdown(string label, TimeSpan remaining, float digitHeight = 0f, TimeSpan warnBelow = default)
	{
		Ensure.NotNull(label);
		SortiePalette palette = SortieStyle.Palette;
		TimeSpan time = remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
		TimeSpan threshold = warnBelow <= TimeSpan.Zero ? TimeSpan.FromMinutes(1) : warnBelow;
		int minutes = Math.Min(99, (int)time.TotalMinutes);
		int[] digits = [minutes / 10, minutes % 10, time.Seconds / 10, time.Seconds % 10, time.Milliseconds / 100, time.Milliseconds / 10 % 10];

		float h = digitHeight > 0f ? digitHeight : ImGui.GetFontSize() * 3f;
		float w = h * 0.55f;
		float gap = h * 0.08f;
		float colon = h * 0.25f;
		string caption = VisibleLabel(label).ToUpperInvariant();
		Vector2 captionSize = ImGui.CalcTextSize(caption);
		float width = (6f * w) + (5f * gap) + (2f * colon) + 12f;
		float height = captionSize.Y + h + 14f;
		Vector2 min = ImGui.GetCursorScreenPos();
		ImGui.Dummy(new Vector2(width, height));
		ImGuiProbes.MarkItem("countdown", label);

		bool expired = time == TimeSpan.Zero;
		Vector4 lit = time < threshold ? palette.Alert : palette.Caution;
		if (expired && !SortieDraw.BlinkPhase(2f))
		{
			lit = SortiePalette.WithAlpha(lit, 0.15f);
		}

		Vector4 ghost = SortiePalette.WithAlpha(lit, 0.07f);
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		Vector2 plateMin = new(min.X, min.Y + captionSize.Y + 2f);
		Vector2 plateMax = new(min.X + width, min.Y + height);
		drawList.AddRectFilled(min, new Vector2(min.X + captionSize.X + 12f, plateMin.Y), SortieDraw.U32(palette.Lead));
		drawList.AddText(new Vector2(min.X + 6f, min.Y + 1f), SortieDraw.U32(palette.Field), caption);
		drawList.AddRectFilled(plateMin, plateMax, SortieDraw.U32(palette.Field));
		drawList.AddRect(plateMin, plateMax, SortieDraw.U32(palette.Lead), 0f, ImDrawFlags.None, 1.5f);

		float x = min.X + 6f;
		float y = plateMin.Y + 6f;
		for (int i = 0; i < digits.Length; i++)
		{
			float scale = i >= 4 ? 0.7f : 1f;
			Vector2 cell = new(w * scale, h * scale);
			SortieDraw.SevenSegmentDigit(drawList, new Vector2(x, y + (h - cell.Y)), cell, digits[i], lit, ghost);
			x += cell.X + gap;
			if (i is 1 or 3)
			{
				float dot = MathF.Max(2f, h * 0.06f);
				uint color = SortieDraw.U32(lit);
				drawList.AddRectFilled(new Vector2(x + (colon / 2f) - dot, y + (h * 0.3f)), new Vector2(x + (colon / 2f) + dot, y + (h * 0.3f) + (2f * dot)), color);
				drawList.AddRectFilled(new Vector2(x + (colon / 2f) - dot, y + (h * 0.7f)), new Vector2(x + (colon / 2f) + dot, y + (h * 0.7f) + (2f * dot)), color);
				x += colon;
			}
		}

		if (expired || time < threshold)
		{
			AnimationActivity.RequestActiveFrame();
		}
	}
}
