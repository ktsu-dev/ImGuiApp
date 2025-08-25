// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiWidgets;

using System.Numerics;

using Hexa.NET.ImGui;

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
	public static void ColorIndicator(ImColor color, bool enabled) => ColorIndicatorImpl.Show(color, enabled);

	internal static class ColorIndicatorImpl
	{
		public static void Show(ImColor color, bool enabled)
		{
			float frameHeight = ImGui.GetFrameHeight();
			ImGui.Dummy(new Vector2(frameHeight, frameHeight));
			Vector2 dummyRectMin = ImGui.GetItemRectMin();
			Vector2 dummyRectMax = ImGui.GetItemRectMax();
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			uint colorToShow = enabled ? ImGui.GetColorU32(color.Value) : ImGui.GetColorU32(ImGuiCol.FrameBg);
			drawList.AddRectFilled(dummyRectMin, dummyRectMax, colorToShow);
		}
	}
}
