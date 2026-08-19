// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

using ktsu.ImGui.Widgets.Gestures;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a pannable, zoomable image canvas with a checkerboard backing for transparency.
	/// </summary>
	/// <param name="id">Unique widget id.</param>
	/// <param name="textureId">GPU texture handle to draw.</param>
	/// <param name="imageSize">Native image size in pixels.</param>
	/// <param name="state">View state, mutated by user interaction.</param>
	/// <param name="canvasSize">Size of the canvas region in screen pixels.</param>
	/// <remarks>
	/// Drag to pan, scroll to zoom toward the cursor, double-click to fit. Panning and the double-click
	/// come from <see cref="GestureDetector"/>, which also claims the region; only the wheel is handled
	/// here, because zoom is not one of the gestures it reports.
	/// </remarks>
	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here.", Justification = "Required for native ImGui interop; pointer is scoped to the call and not retained.")]
	public static void ImageCanvas(string id, nint textureId, Vector2 imageSize, ImageCanvasState state, Vector2 canvasSize)
	{
		Ensure.NotNull(state);

		using ScopedId scopedId = new(id);

		Vector2 origin = ImGui.GetCursorScreenPos();

		// Claims the region with its own invisible button, so IsItemHovered below refers to it.
		GestureResult gesture = GestureDetector("##canvas", canvasSize);
		ImGuiProbes.MarkItem(id);
		bool hovered = ImGui.IsItemHovered();

		// MouseDelta, not gesture.Delta: gesture.Delta is total travel since press start, and PanBy is
		// incremental. The Pan flag supplies the movement threshold that keeps a jittery click still.
		if ((gesture.Gestures & GestureFlags.Pan) != 0)
		{
			state.PanBy(ImGui.GetIO().MouseDelta);
		}

		if (gesture.DoubleTapped)
		{
			state.FitToViewport(imageSize, canvasSize);
		}

		if (hovered)
		{
			float wheel = ImGui.GetIO().MouseWheel;
			if (wheel != 0f)
			{
				// 1.1 per notch is a shallow enough curve to feel controllable at high zoom.
				float factor = MathF.Pow(1.1f, wheel);
				state.ZoomAt(factor, ImGui.GetMousePos() - origin, canvasSize);
			}
		}

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		drawList.PushClipRect(origin, origin + canvasSize, true);

		DrawCheckerboard(drawList, origin, canvasSize);

		(Vector2 min, Vector2 max) = state.ImageRectInViewport(imageSize, canvasSize);
		unsafe
		{
			drawList.AddImage(new ImTextureRef(texId: textureId), origin + min, origin + max);
		}

		drawList.PopClipRect();
	}

	private static void DrawCheckerboard(ImDrawListPtr drawList, Vector2 origin, Vector2 size)
	{
		const float cell = 8f;
		uint light = ImGui.GetColorU32(new Vector4(0.35f, 0.35f, 0.35f, 1f));
		uint dark = ImGui.GetColorU32(new Vector4(0.25f, 0.25f, 0.25f, 1f));

		drawList.AddRectFilled(origin, origin + size, dark);

		int columns = (int)MathF.Ceiling(size.X / cell);
		int rows = (int)MathF.Ceiling(size.Y / cell);
		for (int row = 0; row < rows; row++)
		{
			for (int column = row % 2; column < columns; column += 2)
			{
				Vector2 cellMin = origin + new Vector2(column * cell, row * cell);
				Vector2 cellMax = Vector2.Min(cellMin + new Vector2(cell, cell), origin + size);
				drawList.AddRectFilled(cellMin, cellMax, light);
			}
		}
	}
}
