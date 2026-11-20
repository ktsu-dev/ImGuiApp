// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Monsoon.Effects;

using System.Numerics;
using Hexa.NET.ImGui;

/// <summary>Draws strokes and text as neon tubing: a wide soft glow, the tube, and a hot core.</summary>
internal static class NeonDraw
{
	private static readonly Vector4 HotWhite = new(1f, 0.97f, 0.98f, 1f);

	/// <summary>Converts a colour at a brightness to a draw-list colour, honouring the style alpha.</summary>
	/// <param name="color">The colour.</param>
	/// <param name="brightness">The brightness, 0 to 1.</param>
	/// <param name="alpha">An extra alpha multiplier.</param>
	/// <returns>The packed colour.</returns>
	internal static uint Color(Vector4 color, float brightness = 1f, float alpha = 1f)
	{
		Vector4 lit = MonsoonPalette.Scale(color, brightness);
		lit.W = color.W * alpha;
		return ImGui.GetColorU32(lit);
	}

	/// <summary>Draws one polyline as a neon tube.</summary>
	/// <param name="drawList">The draw list.</param>
	/// <param name="points">The points, in screen space.</param>
	/// <param name="color">The tube colour.</param>
	/// <param name="thickness">The tube thickness in pixels.</param>
	/// <param name="brightness">The brightness, 0 to 1.</param>
	internal static void Stroke(ImDrawListPtr drawList, Vector2[] points, Vector4 color, float thickness, float brightness)
	{
		if (points.Length < 2 || brightness <= 0f)
		{
			return;
		}

		drawList.AddPolyline(ref points[0], points.Length, Color(color, brightness, 0.12f), ImDrawFlags.None, thickness * 4f);
		drawList.AddPolyline(ref points[0], points.Length, Color(color, brightness, 0.25f), ImDrawFlags.None, thickness * 2.5f);
		drawList.AddPolyline(ref points[0], points.Length, Color(color, brightness), ImDrawFlags.None, thickness);
		drawList.AddPolyline(ref points[0], points.Length, Color(MonsoonPalette.Mix(color, HotWhite, 0.6f), brightness, 0.8f), ImDrawFlags.None, MathF.Max(1f, thickness * 0.4f));
	}

	/// <summary>Draws text with a glow behind it.</summary>
	/// <param name="drawList">The draw list.</param>
	/// <param name="position">The top-left corner of the text.</param>
	/// <param name="color">The text colour.</param>
	/// <param name="text">The text.</param>
	/// <param name="brightness">The brightness, 0 to 1.</param>
	internal static void Text(ImDrawListPtr drawList, Vector2 position, Vector4 color, string text, float brightness = 1f)
	{
		uint glow = Color(color, brightness, 0.3f);
		drawList.AddText(position + new Vector2(-1f, 0f), glow, text);
		drawList.AddText(position + new Vector2(1f, 0f), glow, text);
		drawList.AddText(position + new Vector2(0f, -1f), glow, text);
		drawList.AddText(position + new Vector2(0f, 1f), glow, text);
		drawList.AddText(position, Color(color, brightness), text);
	}

	/// <summary>Draws a sign: katakana as tubes, anything else as text, each glyph with its own flicker.</summary>
	/// <param name="drawList">The draw list.</param>
	/// <param name="origin">The top-left corner of the sign.</param>
	/// <param name="height">The glyph height in pixels.</param>
	/// <param name="color">The tube colour.</param>
	/// <param name="text">The text.</param>
	/// <param name="brightnessOf">The brightness of the glyph at an index.</param>
	internal static void Sign(ImDrawListPtr drawList, Vector2 origin, float height, Vector4 color, string text, Func<int, float> brightnessOf)
	{
		float scale = height / KatakanaGlyphs.GridSize;
		float thickness = MathF.Max(1.5f, height / 16f);
		float advance = KatakanaGlyphs.Advance(height);
		IReadOnlyList<GlyphCell> cells = KatakanaGlyphs.Layout(text);
		for (int i = 0; i < cells.Count; i++)
		{
			Vector2 cellOrigin = origin + new Vector2(i * advance, 0f);
			float brightness = brightnessOf(i);
			GlyphCell cell = cells[i];
			if (cell.Strokes is null)
			{
				float fontSize = ImGui.GetFontSize();
				Text(drawList, cellOrigin + new Vector2((height - fontSize) / 2f, height - fontSize), color, cell.Character.ToString(), brightness);
				continue;
			}

			foreach (GlyphStroke stroke in cell.Strokes)
			{
				Vector2[] points = new Vector2[stroke.Points.Count];
				for (int p = 0; p < points.Length; p++)
				{
					points[p] = cellOrigin + (stroke.Points[p] * scale);
				}

				Stroke(drawList, points, color, thickness, brightness);
			}
		}
	}

	/// <summary>
	/// Draws a line of text in which katakana are drawn as strokes at the line's height and anything
	/// else with the current font, so a header can mix Latin and kana with no CJK font loaded.
	/// </summary>
	/// <param name="drawList">The draw list.</param>
	/// <param name="position">The top-left corner of the line.</param>
	/// <param name="color">The colour.</param>
	/// <param name="text">The text.</param>
	/// <returns>The width drawn, in pixels.</returns>
	internal static float MixedText(ImDrawListPtr drawList, Vector2 position, Vector4 color, string text)
	{
		float height = ImGui.GetFontSize();
		float x = position.X;
		int start = 0;
		while (start < text.Length)
		{
			bool kana = KatakanaGlyphs.CanDraw(text[start]);
			int end = start + 1;
			while (end < text.Length && KatakanaGlyphs.CanDraw(text[end]) == kana)
			{
				end++;
			}

			string run = text[start..end];
			if (kana)
			{
				float glyph = height * 0.85f;
				Vector2 origin = new(x, position.Y + ((height - glyph) / 2f));
				Sign(drawList, origin, glyph, color, run, _ => 1f);
				x += SignSize(run, glyph).X + (glyph * KatakanaGlyphs.Gap);
			}
			else
			{
				Text(drawList, new Vector2(x, position.Y), color, run);
				x += ImGui.CalcTextSize(run).X;
			}

			start = end;
		}

		return x - position.X;
	}

	/// <summary>Gets the size a sign takes up.</summary>
	/// <param name="text">The text.</param>
	/// <param name="height">The glyph height.</param>
	/// <returns>The size in pixels.</returns>
	internal static Vector2 SignSize(string text, float height)
	{
		int count = KatakanaGlyphs.Layout(text).Count;
		return count == 0 ? new Vector2(0f, height) : new Vector2((count * KatakanaGlyphs.Advance(height)) - (height * KatakanaGlyphs.Gap), height);
	}
}
