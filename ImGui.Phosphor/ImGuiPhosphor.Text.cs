// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Phosphor;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Probes;

public static partial class ImGuiPhosphor
{
	/// <summary>Draws a line of text with the bloom of an over-driven beam around each glyph.</summary>
	/// <param name="text">The text.</param>
	/// <param name="shade">How bright it is.</param>
	public static void GlowText(string text, PhosphorShade shade = PhosphorShade.Bright)
	{
		Ensure.NotNull(text);
		Vector2 position = ImGui.GetCursorScreenPos();
		Vector2 size = ImGui.CalcTextSize(text);
		PhosphorDraw.GlowText(ImGui.GetWindowDrawList(), position, PhosphorStyle.Palette.Of(shade), text);
		ImGui.Dummy(size);
		ImGuiProbes.MarkItem("glow-text", text);
	}

	/// <summary>
	/// Draws a boxed numeric readout: a small caption over a value in the hottest shade, inside a vector
	/// outline, the way a flight console shows one quantity.
	/// </summary>
	/// <param name="label">The caption, also the readout's identity.</param>
	/// <param name="value">The value, already formatted.</param>
	/// <param name="width">Width of the box; the wider of the two texts when zero or less.</param>
	public static void Readout(string label, string value, float width = 0f)
	{
		Ensure.NotNull(label);
		Ensure.NotNull(value);
		PhosphorPalette palette = PhosphorStyle.Palette;
		ImGuiStylePtr style = ImGui.GetStyle();
		Vector2 padding = style.FramePadding;
		Vector2 labelSize = ImGui.CalcTextSize(label);
		Vector2 valueSize = ImGui.CalcTextSize(value);
		float inner = width > 0f ? width - (padding.X * 2f) : System.MathF.Max(labelSize.X, valueSize.X);
		Vector2 size = new(inner + (padding.X * 2f), labelSize.Y + valueSize.Y + (padding.Y * 3f));

		Vector2 min = ImGui.GetCursorScreenPos();
		Vector2 max = min + size;
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		drawList.AddRectFilled(min, max, PhosphorDraw.U32(palette.Faint, 0.35f));
		drawList.AddRect(min, max, PhosphorDraw.U32(palette.Dim), 0f, ImDrawFlags.None, 1f);
		drawList.AddText(min + padding, PhosphorDraw.U32(palette.Dim), label);
		Vector2 valuePosition = new(max.X - padding.X - valueSize.X, min.Y + (padding.Y * 2f) + labelSize.Y);
		PhosphorDraw.GlowText(drawList, valuePosition, palette.Hot, value);

		ImGui.Dummy(size);
		ImGuiProbes.MarkItem("readout", label);
	}

	/// <summary>
	/// Draws a full-width alert that flashes between an inverted block and an outline, for the one message
	/// the operator must not miss.
	/// </summary>
	/// <param name="text">The message.</param>
	/// <param name="flashing">Whether it flashes; a steady inverted block when false.</param>
	public static void AlertBanner(string text, bool flashing = true)
	{
		Ensure.NotNull(text);
		PhosphorPalette palette = PhosphorStyle.Palette;
		Vector2 textSize = ImGui.CalcTextSize(text);
		Vector2 size = PhosphorDraw.Resolve(Vector2.Zero, ImGui.GetFrameHeight() * 1.6f);
		Vector2 min = ImGui.GetCursorScreenPos();
		Vector2 max = min + size;
		bool lit = !flashing || PhosphorDraw.BlinkPhase(1.5f);
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		Vector2 textPosition = min + ((size - textSize) / 2f);

		if (lit)
		{
			drawList.AddRectFilled(min, max, PhosphorDraw.U32(palette.Hot));
			drawList.AddText(textPosition, PhosphorDraw.U32(palette.Screen), text);
		}
		else
		{
			drawList.AddRect(min, max, PhosphorDraw.U32(palette.Hot), 0f, ImDrawFlags.None, 2f);
			PhosphorDraw.GlowText(drawList, textPosition, palette.Hot, text);
		}

		if (flashing)
		{
			AnimationActivity.RequestActiveFrame();
		}

		ImGui.Dummy(size);
		ImGuiProbes.MarkItem("alert", text);
	}
}
