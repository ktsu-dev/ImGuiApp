// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Phosphor;

using System;
using System.Numerics;
using Hexa.NET.ImGui;

/// <summary>
/// Drawing primitives shared by the widgets: packing a colour, and the glow a lit phosphor line
/// throws onto the glass around it.
/// </summary>
internal static class PhosphorDraw
{
	/// <summary>How much brighter than the halo the core of a glowing stroke is drawn.</summary>
	internal const float HaloAlpha = 0.18f;

	/// <summary>Packs a colour for a draw list, with the current style alpha applied.</summary>
	/// <param name="color">The colour.</param>
	/// <returns>The packed colour.</returns>
	internal static uint U32(Vector4 color) =>
		ImGui.ColorConvertFloat4ToU32(color with { W = color.W * ImGui.GetStyle().Alpha });

	/// <summary>Packs a colour at a given alpha.</summary>
	/// <param name="color">The colour.</param>
	/// <param name="alpha">Its alpha, from 0 to 1.</param>
	/// <returns>The packed colour.</returns>
	internal static uint U32(Vector4 color, float alpha) => U32(color with { W = Math.Clamp(alpha, 0f, 1f) });

	/// <summary>Draws a line with a soft halo either side of its core.</summary>
	/// <param name="drawList">Where to draw.</param>
	/// <param name="from">Start point.</param>
	/// <param name="to">End point.</param>
	/// <param name="color">Colour of the core.</param>
	/// <param name="thickness">Thickness of the core.</param>
	internal static void GlowLine(ImDrawListPtr drawList, Vector2 from, Vector2 to, Vector4 color, float thickness)
	{
		drawList.AddLine(from, to, U32(color, color.W * HaloAlpha), thickness + 4f);
		drawList.AddLine(from, to, U32(color, color.W * HaloAlpha * 2f), thickness + 2f);
		drawList.AddLine(from, to, U32(color), thickness);
	}

	/// <summary>Draws text with a one pixel bloom around each glyph.</summary>
	/// <param name="drawList">Where to draw.</param>
	/// <param name="position">Top left of the text.</param>
	/// <param name="color">Colour of the glyphs.</param>
	/// <param name="text">The text.</param>
	internal static void GlowText(ImDrawListPtr drawList, Vector2 position, Vector4 color, string text)
	{
		uint halo = U32(color, color.W * HaloAlpha);
		drawList.AddText(position + new Vector2(-1f, 0f), halo, text);
		drawList.AddText(position + new Vector2(1f, 0f), halo, text);
		drawList.AddText(position + new Vector2(0f, -1f), halo, text);
		drawList.AddText(position + new Vector2(0f, 1f), halo, text);
		drawList.AddText(position, U32(color), text);
	}

	/// <summary>Draws the four corner brackets of a vector frame.</summary>
	/// <param name="drawList">Where to draw.</param>
	/// <param name="min">Top left.</param>
	/// <param name="max">Bottom right.</param>
	/// <param name="length">Length of each bracket arm.</param>
	/// <param name="color">Bracket colour.</param>
	internal static void CornerBrackets(ImDrawListPtr drawList, Vector2 min, Vector2 max, float length, Vector4 color)
	{
		float arm = MathF.Min(length, MathF.Min(max.X - min.X, max.Y - min.Y) / 2f);
		uint u32 = U32(color);
		drawList.AddLine(min, min + new Vector2(arm, 0f), u32, 2f);
		drawList.AddLine(min, min + new Vector2(0f, arm), u32, 2f);
		drawList.AddLine(new Vector2(max.X, min.Y), new Vector2(max.X - arm, min.Y), u32, 2f);
		drawList.AddLine(new Vector2(max.X, min.Y), new Vector2(max.X, min.Y + arm), u32, 2f);
		drawList.AddLine(new Vector2(min.X, max.Y), new Vector2(min.X + arm, max.Y), u32, 2f);
		drawList.AddLine(new Vector2(min.X, max.Y), new Vector2(min.X, max.Y - arm), u32, 2f);
		drawList.AddLine(max, max - new Vector2(arm, 0f), u32, 2f);
		drawList.AddLine(max, max - new Vector2(0f, arm), u32, 2f);
	}

	/// <summary>Whether a blinking element is in its lit half at the current time.</summary>
	/// <param name="hertz">Blinks per second.</param>
	/// <returns>True for the lit half of each period.</returns>
	internal static bool BlinkPhase(float hertz) => hertz <= 0f || ImGui.GetTime() * hertz % 1.0 < 0.5;

	/// <summary>
	/// Resolves a requested size: a width of zero or less fills the available width, a negative height fills the
	/// available height, and a height of zero takes <paramref name="defaultHeight"/>.
	/// </summary>
	/// <param name="size">The requested size.</param>
	/// <param name="defaultHeight">Height to use when none was asked for.</param>
	/// <returns>The size to lay out.</returns>
	internal static Vector2 Resolve(Vector2 size, float defaultHeight)
	{
		Vector2 available = ImGui.GetContentRegionAvail();
		float width = size.X > 0f ? size.X : MathF.Max(available.X, 1f);
		float height = size.Y > 0f ? size.Y : size.Y < 0f ? MathF.Max(available.Y, 1f) : defaultHeight;
		return new Vector2(width, height);
	}
}
