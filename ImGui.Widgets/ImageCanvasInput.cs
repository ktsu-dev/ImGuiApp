// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Numerics;

using Hexa.NET.ImGui;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Input handling shared by every widget that shows an image through an <see cref="ImageCanvasState"/>,
	/// so the canvas-like widgets zoom the same way.
	/// </summary>
	internal static class ImageCanvasInput
	{
		/// <summary>Zoom multiplier per wheel notch. 1.1 is a shallow enough curve to feel controllable at high zoom.</summary>
		private const float ZoomPerNotch = 1.1f;

		/// <summary>Applies this frame's mouse wheel as a zoom about the pointer: factor 1.1^wheel, anchored at the pointer relative to the viewport origin.</summary>
		/// <param name="state">The view state to zoom.</param>
		/// <param name="viewportOrigin">Screen position of the viewport's top-left corner.</param>
		/// <param name="viewportSize">Viewport size in pixels.</param>
		/// <returns>True if the wheel moved this frame and the zoom was applied.</returns>
		internal static bool ApplyWheelZoom(ImageCanvasState state, Vector2 viewportOrigin, Vector2 viewportSize)
		{
			Ensure.NotNull(state);

			float wheel = ImGui.GetIO().MouseWheel;
			if (wheel == 0f)
			{
				return false;
			}

			float factor = MathF.Pow(ZoomPerNotch, wheel);
			state.ZoomAt(factor, ImGui.GetMousePos() - viewportOrigin, viewportSize);
			return true;
		}
	}
}
