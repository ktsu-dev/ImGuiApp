// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Sortie;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Probes;

public static partial class ImGuiSortie
{
	/// <summary>
	/// Draws a command button: a chamfered plate outlined in the lead colour, filled solid while hovered or held,
	/// with its label in upper case.
	/// </summary>
	/// <param name="label">Label, also the id and probe name; text after <c>##</c> is hidden.</param>
	/// <param name="size">Button size; zero fits the label.</param>
	/// <param name="lamp">Colour of the button; <see cref="SortieLamp.Off"/> uses the lead colour.</param>
	/// <returns>True on the frame the button is clicked.</returns>
	public static bool Button(string label, Vector2 size = default, SortieLamp lamp = SortieLamp.Off)
	{
		Ensure.NotNull(label);
		SortiePalette palette = SortieStyle.Palette;
		string caption = VisibleLabel(label).ToUpperInvariant();
		Vector2 text = ImGui.CalcTextSize(caption);
		Vector2 padding = ImGui.GetStyle().FramePadding;
		Vector2 natural = text + (padding * 2f) + new Vector2(16f, 4f);
		Vector2 area = new(size.X > 0f ? size.X : natural.X, size.Y > 0f ? size.Y : natural.Y);
		Vector2 min = ImGui.GetCursorScreenPos();
		Vector2 max = min + area;
		bool clicked = ImGui.InvisibleButton(label, area);
		bool hovered = ImGui.IsItemHovered();
		bool held = ImGui.IsItemActive();
		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		ImGuiProbes.MarkItem(label);

		Vector4 lead = lamp == SortieLamp.Off ? palette.Lead : palette.LampColor(lamp);
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		float cut = MathF.Min(10f, area.Y * 0.35f);
		Vector4 fill = held ? palette.LeadBright : hovered ? lead : palette.Field;
		Vector4 ink = held || hovered ? palette.Field : lead;
		SortieDraw.ChamferedFill(drawList, min, max, cut, fill);
		SortieDraw.ChamferedOutline(drawList, min + new Vector2(0.5f), max - new Vector2(0.5f), cut, lead, 1.5f);
		drawList.AddRectFilled(new Vector2(min.X + 3f, min.Y + 3f), new Vector2(min.X + 6f, max.Y - 3f), SortieDraw.U32(ink));
		drawList.AddText(min + ((area - text) / 2f) + new Vector2(3f, 0f), SortieDraw.U32(ink), caption);
		return clicked;
	}
}
