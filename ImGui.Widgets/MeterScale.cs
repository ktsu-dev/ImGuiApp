// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.Semantics.Color;

/// <summary>
/// The decibel-to-screen mapping and zone colouring shared by the vertical level meters, so every
/// meter in the library reads a level the same way and colours it by the same thresholds.
/// </summary>
internal static class MeterScale
{
	/// <summary>Gets the colour of a level safely below full scale, and of positive correlation.</summary>
	internal static ImColor Safe => new Srgb(0.25f, 0.80f, 0.35f).ToImColor(1.0f);

	/// <summary>Gets the colour of a level within 6 dB of full scale.</summary>
	internal static ImColor Caution => new Srgb(0.90f, 0.78f, 0.20f).ToImColor(1.0f);

	/// <summary>Gets the colour of a level above full scale, and of negative correlation.</summary>
	internal static ImColor Clip => new Srgb(0.90f, 0.20f, 0.20f).ToImColor(1.0f);

	/// <summary>
	/// Maps a level onto <c>[0, 1]</c> across <c>[minDb, maxDb]</c>, clamping outside that range.
	/// </summary>
	/// <param name="db">The level in decibels.</param>
	/// <param name="minDb">The level at the bottom of the scale.</param>
	/// <param name="maxDb">The level at the top of the scale.</param>
	/// <returns>
	/// The fraction of the scale the level reaches. An empty or inverted range and a
	/// <see cref="float.NaN"/> level both map to 0; infinities clamp to the ends.
	/// </returns>
	internal static float DbToFraction(float db, float minDb, float maxDb)
	{
		float range = maxDb - minDb;
		if (!(range > 0.0f) || float.IsNaN(db))
		{
			return 0.0f;
		}

		if (float.IsPositiveInfinity(db))
		{
			return 1.0f;
		}

		if (float.IsNegativeInfinity(db))
		{
			return 0.0f;
		}

		return Math.Clamp((db - minDb) / range, 0.0f, 1.0f);
	}

	/// <summary>
	/// Picks the zone colour for a level: <see cref="Clip"/> above 0 dB, <see cref="Caution"/> above
	/// -6 dB, and <see cref="Safe"/> otherwise.
	/// </summary>
	/// <param name="db">The level in decibels.</param>
	/// <returns>The colour the level is drawn in.</returns>
	internal static ImColor ZoneColor(float db)
	{
		if (db > 0.0f)
		{
			return Clip;
		}

		if (db > -6.0f)
		{
			return Caution;
		}

		return Safe;
	}

	/// <summary>
	/// Draws a vertical level meter into an already reserved rectangle: the frame background, a
	/// zone-coloured fill rising from the bottom, an optional 2 px peak line, and the border.
	/// </summary>
	/// <param name="drawList">The draw list to draw into.</param>
	/// <param name="min">The top-left corner of the meter.</param>
	/// <param name="max">The bottom-right corner of the meter.</param>
	/// <param name="db">The level in decibels.</param>
	/// <param name="minDb">The level at the bottom of the scale.</param>
	/// <param name="maxDb">The level at the top of the scale.</param>
	/// <param name="peakDb">The held peak in decibels; a non-finite value draws no peak line.</param>
	internal static void DrawVerticalMeter(ImDrawListPtr drawList, Vector2 min, Vector2 max, float db, float minDb, float maxDb, float peakDb)
	{
		Span<Vector4> colors = ImGui.GetStyle().Colors;
		float height = max.Y - min.Y;

		drawList.AddRectFilled(min, max, ImGui.GetColorU32(colors[(int)ImGuiCol.FrameBg]));

		float fill = DbToFraction(db, minDb, maxDb);
		if (fill > 0.0f)
		{
			float fillTop = max.Y - (fill * height);
			drawList.AddRectFilled(new Vector2(min.X, fillTop), max, ZoneColor(db).ToImGuiU32());
		}

		if (float.IsFinite(peakDb) && maxDb - minDb > 0.0f)
		{
			float peak = DbToFraction(peakDb, minDb, maxDb);
			float peakY = max.Y - (peak * height);
			drawList.AddLine(new Vector2(min.X, peakY), new Vector2(max.X, peakY), ZoneColor(peakDb).ToImGuiU32(), 2.0f);
		}

		drawList.AddRect(min, max, ImGui.GetColorU32(colors[(int)ImGuiCol.Border]));
	}
}
