// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App;

using System.Numerics;

using Hexa.NET.ImGui;

/// <summary>
/// Reads, from the ImGui context, how far a wheel step scrolls the window a pan would scroll, so a
/// two-finger pan's content follows the fingers exactly.
/// </summary>
internal static class TouchScrollWheel
{
	/// <summary>
	/// Gets how many points of pan make one wheel step for the window under the mouse, or for the
	/// current font with no size limit when the mouse is over no window.
	/// </summary>
	/// <returns>Points per wheel step, across and down.</returns>
	internal static Vector2 PointsPerWheelStep()
	{
		ImGuiContextPtr context = ImGui.GetCurrentContext();
		ImGuiWindowPtr window = context.IsNull ? default : context.HoveredWindow;
		if (window.IsNull)
		{
			return TouchScrollGesture.PointsPerWheelStep(ImGui.GetFontSize(), new Vector2(float.MaxValue));
		}

		ImRect inner = window.InnerRect;
		return TouchScrollGesture.PointsPerWheelStep(window.FontRefSize, inner.Max - inner.Min);
	}
}
