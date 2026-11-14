// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Sortie;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Probes;

public static partial class ImGuiSortie
{
	/// <summary>
	/// Opens a captioned panel: a black plate with its top right and bottom left corners cut, a lead-coloured
	/// outline, and a solid caption bar carrying the title in heavy black lettering and an optional code on the right.
	/// Draw the content inside a <c>using</c> block over the returned scope, which closes the panel when disposed.
	/// </summary>
	/// <param name="title">Caption text, also the panel's id.</param>
	/// <param name="size">Panel size; zero width fills, zero height fills, negative values follow ImGui's child rules.</param>
	/// <param name="code">Optional code shown at the right of the caption, such as a section number.</param>
	/// <returns>The panel's scope; <see cref="SortiePanel.IsVisible"/> says whether its content is visible.</returns>
	public static SortiePanel Panel(string title, Vector2 size = default, string? code = null) =>
		new(OpenPanel(title, size, code));

	private static bool OpenPanel(string title, Vector2 size, string? code)
	{
		Ensure.NotNull(title);
		SortiePalette palette = SortieStyle.Palette;
		float captionHeight = ImGui.GetFrameHeight() + 6f;
		ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10f, 8f));
		bool visible = ImGui.BeginChild(title, size, ImGuiChildFlags.AlwaysUseWindowPadding, ImGuiWindowFlags.NoBackground);
		ImGui.PopStyleVar();

		Vector2 min = ImGui.GetWindowPos();
		Vector2 max = min + ImGui.GetWindowSize();
		float cut = MathF.Min(14f, captionHeight * 0.6f);
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		drawList.PushClipRect(min, max, false);
		SortieDraw.ChamferedFill(drawList, min, max, cut, palette.Field);
		Vector2 captionMax = new(max.X, min.Y + captionHeight);
		SortieDraw.ChamferedFill(drawList, min, captionMax, cut, palette.Lead);
		SortieDraw.ChamferedOutline(drawList, min + new Vector2(0.5f), max - new Vector2(0.5f), cut, palette.Lead, 1.5f);

		float letterHeight = captionHeight - 6f;
		string caption = VisibleLabel(title).ToUpperInvariant();
		SortieDraw.HeavyText(drawList, new Vector2(min.X + 8f, min.Y + 3f), letterHeight, palette.Field, caption);
		if (!string.IsNullOrEmpty(code))
		{
			Vector2 codeSize = SortieDraw.MeasureHeavy(code, letterHeight * 0.8f);
			SortieDraw.HeavyText(drawList, new Vector2(max.X - cut - 6f - codeSize.X, min.Y + 3f + (letterHeight * 0.1f)), letterHeight * 0.8f, palette.Field, code);
		}

		drawList.PopClipRect();
		ImGui.SetCursorPosY(ImGui.GetCursorPosY() + captionHeight);
		ImGuiProbes.MarkRegion($"panel/{title}", min, max);
		return visible;
	}
}
