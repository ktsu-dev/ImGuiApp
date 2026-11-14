// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Phosphor;

using System;
using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Probes;

public static partial class ImGuiPhosphor
{
	/// <summary>
	/// Draws scanlines, a vignette and optional flicker over the current window, so everything in it reads
	/// as light on curved glass. Call it last in the window, after the content it covers.
	/// </summary>
	/// <param name="options">How strong each effect is; <see cref="CrtOptions.Default"/> when null.</param>
	public static void CrtOverlay(CrtOptions? options = null)
	{
		Vector2 min = ImGui.GetWindowPos();
		CrtOverlay(min, min + ImGui.GetWindowSize(), options);
	}

	/// <summary>Draws scanlines, a vignette and optional flicker over a rectangle of the current window.</summary>
	/// <param name="min">Top left, in screen pixels.</param>
	/// <param name="max">Bottom right, in screen pixels.</param>
	/// <param name="options">How strong each effect is; <see cref="CrtOptions.Default"/> when null.</param>
	public static void CrtOverlay(Vector2 min, Vector2 max, CrtOptions? options = null)
	{
		CrtOptions crt = options ?? CrtOptions.Default;
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		drawList.PushClipRect(min, max, true);
		Vector4 black = new(0f, 0f, 0f, 1f);

		if (crt.ScanlineSpacing > 0f && crt.ScanlineAlpha > 0f)
		{
			uint line = PhosphorDraw.U32(black, crt.ScanlineAlpha);
			for (float y = MathF.Floor(min.Y); y < max.Y; y += crt.ScanlineSpacing)
			{
				drawList.AddLine(new Vector2(min.X, y), new Vector2(max.X, y), line, 1f);
			}
		}

		if (crt.Vignette > 0f)
		{
			Vector2 size = max - min;
			float band = MathF.Min(size.X, size.Y) * 0.25f;
			uint dark = PhosphorDraw.U32(black, crt.Vignette);
			uint clear = PhosphorDraw.U32(black, 0f);
			drawList.AddRectFilledMultiColor(min, new Vector2(max.X, min.Y + band), dark, dark, clear, clear);
			drawList.AddRectFilledMultiColor(new Vector2(min.X, max.Y - band), max, clear, clear, dark, dark);
			drawList.AddRectFilledMultiColor(min, new Vector2(min.X + band, max.Y), dark, clear, clear, dark);
			drawList.AddRectFilledMultiColor(new Vector2(max.X - band, min.Y), max, clear, dark, dark, clear);
		}

		if (crt.Flicker > 0f)
		{
			double time = ImGui.GetTime();
			float wave = (float)((Math.Sin(time * 61.0) * 0.5) + (Math.Sin(time * 7.3) * 0.5));
			float alpha = crt.Flicker * (0.5f + (0.5f * wave));
			drawList.AddRectFilled(min, max, PhosphorDraw.U32(black, alpha));
			AnimationActivity.RequestActiveFrame();
		}

		drawList.PopClipRect();
		ImGuiProbes.MarkRegion("crt", min, max);
	}
}
