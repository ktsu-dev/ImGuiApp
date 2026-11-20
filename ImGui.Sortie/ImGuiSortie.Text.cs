// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Sortie;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Probes;

public static partial class ImGuiSortie
{
	/// <summary>Draws a line of heavy condensed lettering in the lead colour, the type of a command display's headings.</summary>
	/// <param name="text">The text.</param>
	/// <param name="pixelHeight">Letter height in pixels; zero takes three times the font size.</param>
	public static void HeavyText(string text, float pixelHeight = 0f) =>
		HeavyText(text, SortieStyle.Palette.Lead, pixelHeight);

	/// <summary>Draws a line of heavy condensed lettering.</summary>
	/// <param name="text">The text.</param>
	/// <param name="color">Colour of the lettering.</param>
	/// <param name="pixelHeight">Letter height in pixels; zero takes three times the font size.</param>
	public static void HeavyText(string text, Vector4 color, float pixelHeight = 0f)
	{
		Ensure.NotNull(text);
		float height = pixelHeight > 0f ? pixelHeight : ImGui.GetFontSize() * 3f;
		Vector2 size = SortieDraw.MeasureHeavy(text, height);
		Vector2 position = ImGui.GetCursorScreenPos();
		ImGui.Dummy(size);
		SortieDraw.HeavyText(ImGui.GetWindowDrawList(), position, height, color, text);
		ImGuiProbes.MarkItem("heavy-text", text);
	}

	/// <summary>
	/// Draws a labelled readout: a small caption over a value in heavy lettering, with a lead rule down its left side.
	/// </summary>
	/// <param name="label">Caption, also the probe name.</param>
	/// <param name="value">The value.</param>
	/// <param name="unit">Optional unit after the value.</param>
	/// <param name="lamp">Colours the value; <see cref="SortieLamp.Off"/> uses the text colour.</param>
	public static void Readout(string label, string value, string? unit = null, SortieLamp lamp = SortieLamp.Off)
	{
		Ensure.NotNull(label);
		Ensure.NotNull(value);
		SortiePalette palette = SortieStyle.Palette;
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		float fontSize = ImGui.GetFontSize();
		float valueHeight = fontSize * 2.2f;
		string caption = VisibleLabel(label).ToUpperInvariant();
		Vector2 captionSize = ImGui.CalcTextSize(caption);
		Vector2 valueSize = SortieDraw.MeasureHeavy(value, valueHeight);
		Vector2 unitSize = string.IsNullOrEmpty(unit) ? Vector2.Zero : ImGui.CalcTextSize(unit);
		float width = MathF.Max(captionSize.X, valueSize.X + (unitSize.X > 0f ? unitSize.X + 4f : 0f)) + 10f;
		float height = captionSize.Y + valueSize.Y + 2f;

		Vector2 min = ImGui.GetCursorScreenPos();
		ImGui.Dummy(new Vector2(width, height));
		drawList.AddRectFilled(min, new Vector2(min.X + 3f, min.Y + height), SortieDraw.U32(palette.Lead));
		drawList.AddText(new Vector2(min.X + 8f, min.Y), SortieDraw.U32(palette.LeadDim), caption);
		Vector4 color = lamp == SortieLamp.Off ? palette.Text : palette.LampColor(lamp);
		Vector2 valuePosition = new(min.X + 8f, min.Y + captionSize.Y + 2f);
		SortieDraw.HeavyText(drawList, valuePosition, valueHeight, color, value);
		if (unitSize.X > 0f)
		{
			drawList.AddText(new Vector2(valuePosition.X + valueSize.X + 4f, valuePosition.Y + valueSize.Y - unitSize.Y - 2f), SortieDraw.U32(palette.LeadDim), unit);
		}

		ImGuiProbes.MarkItem("readout", label);
	}
}
