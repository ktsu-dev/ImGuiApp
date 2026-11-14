// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Phosphor;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Probes;

public static partial class ImGuiPhosphor
{
	/// <summary>
	/// Begins a panel outlined by corner brackets, with its title on an inverted tab at the top left.
	/// Always pair with <see cref="EndFrame"/>, whatever this returns.
	/// </summary>
	/// <param name="title">The title, shown in capitals, and the panel's identity.</param>
	/// <param name="size">Size of the panel; the available space where a component is zero or less.</param>
	/// <returns>True when the panel's content is visible and should be drawn.</returns>
	public static bool BeginFrame(string title, Vector2 size = default)
	{
		Ensure.NotNull(title);
		PhosphorPalette palette = PhosphorStyle.Palette;
		bool visible = ImGui.BeginChild(title, size, ImGuiChildFlags.AlwaysUseWindowPadding, ImGuiWindowFlags.NoBackground);
		Vector2 min = ImGui.GetWindowPos();
		Vector2 max = min + ImGui.GetWindowSize() - Vector2.One;
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();

		drawList.AddRectFilled(min, max, PhosphorDraw.U32(palette.Faint, 0.25f));
		drawList.AddRect(min, max, PhosphorDraw.U32(palette.Faint), 0f, ImDrawFlags.None, 1f);
		PhosphorDraw.CornerBrackets(drawList, min, max, ImGui.GetFrameHeight(), palette.Normal);

		string caption = VisibleLabel(title).ToUpperInvariant();
		if (caption.Length > 0)
		{
			Vector2 padding = ImGui.GetStyle().FramePadding;
			Vector2 textSize = ImGui.CalcTextSize(caption);
			Vector2 tabMin = min + new Vector2(ImGui.GetFrameHeight(), 0f);
			Vector2 tabMax = tabMin + textSize + (padding * 2f);
			drawList.AddRectFilled(tabMin, tabMax, PhosphorDraw.U32(palette.Normal));
			drawList.AddText(tabMin + padding, PhosphorDraw.U32(palette.Screen), caption);
			ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (tabMax.Y - tabMin.Y));
		}

		ImGuiProbes.MarkRegion($"frame/{title}", min, max);
		return visible;
	}

	/// <summary>Ends a panel begun with <see cref="BeginFrame"/>.</summary>
	public static void EndFrame() => ImGui.EndChild();
}
