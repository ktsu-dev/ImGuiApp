// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Sortie;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Probes;

public static partial class ImGuiSortie
{
	/// <summary>
	/// Draws a full-width alarm banner: hazard stripes top and bottom, a solid alert band across the middle and the
	/// message in heavy black lettering. Flashing alternates the band with a black one and scrolls the stripes.
	/// </summary>
	/// <param name="text">The message, conventionally a single upper-case word.</param>
	/// <param name="flashing">Whether the banner flashes and its stripes march.</param>
	/// <param name="height">Banner height; zero takes four times the font size.</param>
	public static void EmergencyBanner(string text, bool flashing = true, float height = 0f)
	{
		Ensure.NotNull(text);
		SortiePalette palette = SortieStyle.Palette;
		Vector2 size = SortieDraw.Resolve(new Vector2(0f, height), ImGui.GetFontSize() * 4f);
		Vector2 min = ImGui.GetCursorScreenPos();
		Vector2 max = min + size;
		ImGui.Dummy(size);
		ImGuiProbes.MarkItem("emergency", text);

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		float stripe = MathF.Max(4f, size.Y * 0.16f);
		float offset = flashing ? (float)(ImGui.GetTime() * 40.0) : 0f;
		SortieDraw.HazardStripes(drawList, min, new Vector2(max.X, min.Y + stripe), palette.Alert, palette.Field, offset);
		SortieDraw.HazardStripes(drawList, new Vector2(min.X, max.Y - stripe), max, palette.Alert, palette.Field, -offset);

		bool lit = !flashing || SortieDraw.BlinkPhase(1.6f);
		Vector2 bandMin = new(min.X, min.Y + stripe + 2f);
		Vector2 bandMax = new(max.X, max.Y - stripe - 2f);
		Vector4 band = lit ? palette.Alert : palette.Field;
		Vector4 ink = lit ? palette.Field : palette.Alert;
		drawList.AddRectFilled(bandMin, bandMax, SortieDraw.U32(band));
		if (!lit)
		{
			drawList.AddRect(bandMin, bandMax, SortieDraw.U32(palette.Alert), 0f, ImDrawFlags.None, 2f);
		}

		float letterHeight = (bandMax.Y - bandMin.Y) * 0.9f;
		string word = text.ToUpperInvariant();
		Vector2 wordSize = SortieDraw.MeasureHeavy(word, letterHeight);
		float x = min.X + ((size.X - wordSize.X) / 2f);
		float y = bandMin.Y + ((bandMax.Y - bandMin.Y - wordSize.Y) / 2f);
		drawList.PushClipRect(bandMin, bandMax, true);
		SortieDraw.HeavyText(drawList, new Vector2(x, y), letterHeight, ink, word);
		drawList.PopClipRect();

		if (flashing)
		{
			AnimationActivity.RequestActiveFrame();
		}
	}
}
