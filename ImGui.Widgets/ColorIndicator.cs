// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.ImGui.Probes;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Displays a colored square.
	/// </summary>
	/// <param name="color">The color to be displayed when enabled.</param>
	/// <param name="enabled">A boolean indicating whether the ColorIndicator is enabled.</param>
	/// <remarks>Marked for UI probes as <c>color-indicator</c>; separate two in one window with <see cref="ScopedId"/>.</remarks>
	public static void ColorIndicator(ImColor color, bool enabled) => ColorIndicatorImpl.Show(color, enabled);

	internal static class ColorIndicatorImpl
	{
		public static void Show(ImColor color, bool enabled)
		{
			float frameHeight = ImGui.GetFrameHeight();
			ImGui.Dummy(new Vector2(frameHeight, frameHeight));
			ImGuiProbes.MarkItem("color-indicator");
			Vector2 dummyRectMin = ImGui.GetItemRectMin();
			Vector2 dummyRectMax = ImGui.GetItemRectMax();
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			uint colorToShow = enabled ? color.ToImGuiU32() : ImGui.GetColorU32(ImGuiCol.FrameBg);
			drawList.AddRectFilled(dummyRectMin, dummyRectMax, colorToShow);
		}
	}
}
