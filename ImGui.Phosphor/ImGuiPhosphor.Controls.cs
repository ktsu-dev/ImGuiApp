// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Phosphor;

using System;
using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Probes;

public static partial class ImGuiPhosphor
{
	/// <summary>
	/// Draws a bracketed console button, <c>[ LABEL ]</c>, that inverts to a lit block under the pointer.
	/// </summary>
	/// <param name="label">The caption, shown in capitals, and the button's identity.</param>
	/// <param name="size">Size of the button; fitted to the caption where a component is zero or less.</param>
	/// <returns>True on the frame the button is clicked.</returns>
	public static bool Button(string label, Vector2 size = default)
	{
		Ensure.NotNull(label);
		PhosphorPalette palette = PhosphorStyle.Palette;
		string caption = $"[ {VisibleLabel(label).ToUpperInvariant()} ]";
		Vector2 textSize = ImGui.CalcTextSize(caption);
		Vector2 padding = ImGui.GetStyle().FramePadding;
		Vector2 fitted = textSize + (padding * 2f);
		Vector2 actual = new(size.X > 0f ? size.X : fitted.X, size.Y > 0f ? size.Y : fitted.Y);

		Vector2 min = ImGui.GetCursorScreenPos();
		bool pressed = ImGui.InvisibleButton(label, actual);
		bool hovered = ImGui.IsItemHovered();
		bool held = ImGui.IsItemActive();
		Vector2 max = min + actual;
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		Vector2 textPosition = min + ((actual - textSize) / 2f);

		if (hovered || held)
		{
			drawList.AddRectFilled(min, max, PhosphorDraw.U32(held ? palette.Hot : palette.Bright));
			drawList.AddText(textPosition, PhosphorDraw.U32(palette.Screen), caption);
		}
		else
		{
			PhosphorDraw.GlowText(drawList, textPosition, palette.Bright, caption);
		}

		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		ImGuiProbes.MarkItem(label);
		return pressed;
	}

	/// <summary>Draws a square indicator lamp with a caption beside it.</summary>
	/// <param name="label">The caption, and the lamp's identity.</param>
	/// <param name="lit">Whether the lamp is on.</param>
	/// <param name="blinkHertz">Blinks per second while lit; zero holds it steady.</param>
	public static void StatusLight(string label, bool lit, float blinkHertz = 0f)
	{
		Ensure.NotNull(label);
		PhosphorPalette palette = PhosphorStyle.Palette;
		float side = ImGui.GetTextLineHeight();
		Vector2 min = ImGui.GetCursorScreenPos();
		Vector2 lampMax = min + new Vector2(side, side);
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		bool on = lit && PhosphorDraw.BlinkPhase(blinkHertz);

		if (on)
		{
			drawList.AddRectFilled(min - new Vector2(2f, 2f), lampMax + new Vector2(2f, 2f), PhosphorDraw.U32(palette.Hot, PhosphorDraw.HaloAlpha));
			drawList.AddRectFilled(min, lampMax, PhosphorDraw.U32(palette.Hot));
		}
		else
		{
			drawList.AddRectFilled(min, lampMax, PhosphorDraw.U32(palette.Faint));
		}

		drawList.AddRect(min, lampMax, PhosphorDraw.U32(palette.Dim), 0f, ImDrawFlags.None, 1f);
		string caption = VisibleLabel(label);
		float spacing = ImGui.GetStyle().ItemInnerSpacing.X;
		drawList.AddText(new Vector2(lampMax.X + spacing, min.Y), PhosphorDraw.U32(on ? palette.Bright : palette.Dim), caption);

		if (lit && blinkHertz > 0f)
		{
			AnimationActivity.RequestActiveFrame();
		}

		ImGui.Dummy(new Vector2(side + spacing + ImGui.CalcTextSize(caption).X, side));
		ImGuiProbes.MarkItem("status-light", label);
	}

	/// <summary>
	/// Draws a horizontal bar of discrete segments, lit from the left in proportion to a fraction, the way
	/// a block-character gauge fills.
	/// </summary>
	/// <param name="label">The bar's identity; shown as a caption when it has visible text.</param>
	/// <param name="fraction">How full the bar is, from 0 to 1.</param>
	/// <param name="segments">How many segments it has.</param>
	/// <param name="size">Size of the bar; the available width and one frame high where a component is zero or less.</param>
	public static void SegmentBar(string label, float fraction, int segments = 20, Vector2 size = default)
	{
		Ensure.NotNull(label);
		PhosphorPalette palette = PhosphorStyle.Palette;
		int count = Math.Max(segments, 1);
		float filled = Math.Clamp(float.IsNaN(fraction) ? 0f : fraction, 0f, 1f) * count;
		string caption = VisibleLabel(label);
		Vector2 min = ImGui.GetCursorScreenPos();
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		float captionHeight = 0f;

		if (caption.Length > 0)
		{
			drawList.AddText(min, PhosphorDraw.U32(palette.Dim), caption);
			captionHeight = ImGui.GetTextLineHeightWithSpacing();
		}

		Vector2 actual = PhosphorDraw.Resolve(size, ImGui.GetFrameHeight());
		Vector2 barMin = min + new Vector2(0f, captionHeight);
		const float gap = 2f;
		float segmentWidth = (actual.X - (gap * (count - 1))) / count;

		for (int i = 0; i < count; i++)
		{
			Vector2 a = barMin + new Vector2(i * (segmentWidth + gap), 0f);
			Vector2 b = a + new Vector2(segmentWidth, actual.Y);
			if (i + 1 <= filled)
			{
				drawList.AddRectFilled(a, b, PhosphorDraw.U32(palette.Bright));
			}
			else if (i < filled)
			{
				drawList.AddRectFilled(a, b, PhosphorDraw.U32(palette.Dim));
			}
			else
			{
				drawList.AddRect(a, b, PhosphorDraw.U32(palette.Faint), 0f, ImDrawFlags.None, 1f);
			}
		}

		ImGui.Dummy(new Vector2(actual.X, actual.Y + captionHeight));
		ImGuiProbes.MarkItem("segment-bar", label);
	}

	/// <summary>The part of an ImGui label before any <c>##</c> identity suffix.</summary>
	/// <param name="label">The label.</param>
	/// <returns>The text to show.</returns>
	internal static string VisibleLabel(string label)
	{
		int hidden = label.IndexOf("##", StringComparison.Ordinal);
		return hidden < 0 ? label : label[..hidden];
	}
}
