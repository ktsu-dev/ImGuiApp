namespace ktsu.io.ImGuiApp;

using ktsu.io.ScopedAction;
using ImGuiNET;
using System.Numerics;

public class UIScaler : ScopedAction
{
	public UIScaler(float scale)
	{
		var style = ImGui.GetStyle();
		int numStyles = 0;
		PushStyleAndCount(ImGuiStyleVar.CellPadding, style.CellPadding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.ChildBorderSize, style.ChildBorderSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.ChildRounding, style.ChildRounding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.DockingSeparatorSize, style.DockingSeparatorSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.FrameBorderSize, style.FrameBorderSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.FramePadding, style.FramePadding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.FrameRounding, style.FrameRounding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.GrabMinSize, style.GrabMinSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.GrabRounding, style.GrabRounding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.IndentSpacing, style.IndentSpacing * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.ItemInnerSpacing, style.ItemInnerSpacing * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.ItemSpacing, style.ItemSpacing * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.PopupBorderSize, style.PopupBorderSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.PopupRounding, style.PopupRounding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.ScrollbarRounding, style.ScrollbarRounding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.ScrollbarSize, style.ScrollbarSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.SeparatorTextBorderSize, style.SeparatorTextBorderSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.SeparatorTextPadding, style.SeparatorTextPadding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.TabRounding, style.TabRounding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.WindowBorderSize, style.WindowBorderSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.WindowMinSize, style.WindowMinSize * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.WindowPadding, style.WindowPadding * scale, ref numStyles);
		PushStyleAndCount(ImGuiStyleVar.WindowRounding, style.WindowRounding * scale, ref numStyles);

		OnClose = () => ImGui.PopStyleVar(numStyles);
	}

	private static void PushStyleAndCount(ImGuiStyleVar style, float value, ref int numStyles)
	{
		ImGui.PushStyleVar(style, value);
		++numStyles;
	}

	private static void PushStyleAndCount(ImGuiStyleVar style, Vector2 value, ref int numStyles)
	{
		ImGui.PushStyleVar(style, value);
		++numStyles;
	}
}
