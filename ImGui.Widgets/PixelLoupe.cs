// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.ImGui.Probes;

using SemanticColor = ktsu.Semantics.Color.Color;

public static partial class ImGuiWidgets
{
	/// <summary>Draws a magnified grid of the pixels around the pointer on an ImageCanvas, and an RGBA readout of the centre pixel.</summary>
	/// <param name="label">Unique id; also the probe name of the loupe panel.</param>
	/// <param name="canvasState">The state passed to the ImageCanvas being inspected. Read only.</param>
	/// <param name="imageSize">Native image size in pixels, as passed to ImageCanvas.</param>
	/// <param name="canvasMin">Screen position of the canvas's top-left; pass <c>ImGui.GetItemRectMin()</c> right after ImageCanvas.</param>
	/// <param name="canvasSize">The canvas size passed to ImageCanvas.</param>
	/// <param name="readPixel">Returns the colour of image pixel (x, y). Only called with 0 &lt;= x &lt; width and 0 &lt;= y &lt; height.</param>
	/// <param name="pixelX">The inspected pixel's x, or -1 when no pixel has been hovered yet.</param>
	/// <param name="pixelY">The inspected pixel's y, or -1 when no pixel has been hovered yet.</param>
	/// <param name="radius">Pixels shown either side of the centre; the grid is (2r+1)². Default 4, clamped to [0, 32].</param>
	/// <param name="cellSize">On-screen size of one magnified pixel. Default 12, clamped to [2, 64].</param>
	/// <returns>True if the pointer is over an image pixel this frame.</returns>
	/// <remarks>
	/// The loupe is passive: it submits no button and takes no input, so the canvas keeps every gesture.
	/// It is drawn wherever the cursor is, and keeps showing the last hovered pixel after the pointer
	/// leaves the image, re-read through <paramref name="readPixel"/> every frame so edits show.
	/// </remarks>
	public static bool PixelLoupe(string label, ImageCanvasState canvasState, Vector2 imageSize, Vector2 canvasMin, Vector2 canvasSize, Func<int, int, SemanticColor> readPixel, out int pixelX, out int pixelY, int radius = 4, float cellSize = 12f)
	{
		Ensure.NotNull(label);
		Ensure.NotNull(canvasState);
		Ensure.NotNull(readPixel);
		return PixelLoupeImpl.Draw(label, canvasState, imageSize, canvasMin, canvasSize, readPixel, out pixelX, out pixelY, radius, cellSize);
	}

	/// <summary>The ImGui half of <see cref="PixelLoupe"/>: hover, drawing and probe regions.</summary>
	internal static class PixelLoupeImpl
	{
		/// <summary>The inspected pixel, per loupe id.</summary>
		private static readonly Dictionary<uint, PixelLoupeState> States = [];

		/// <summary>The smallest side the on-canvas marker is drawn at, in screen pixels.</summary>
		private const float MinMarkerSide = 3f;

		/// <summary>The smallest cell size that still gets grid lines; below it they would cover the pixels.</summary>
		private const float GridLineMinCellSize = 6f;

		/// <summary>Draws the loupe; see <see cref="PixelLoupe"/>.</summary>
		internal static bool Draw(string label, ImageCanvasState canvasState, Vector2 imageSize, Vector2 canvasMin, Vector2 canvasSize, Func<int, int, SemanticColor> readPixel, out int pixelX, out int pixelY, int radius, float cellSize)
		{
			radius = PixelLoupeState.ClampRadius(radius);
			cellSize = PixelLoupeState.ClampCellSize(cellSize);

			uint id = ImGui.GetID(label);
			if (!States.TryGetValue(id, out PixelLoupeState? state))
			{
				state = new PixelLoupeState();
				States[id] = state;
			}

			Vector2 mouse = ImGui.GetMousePos();
			bool pointerOverCanvas = ImGui.IsMouseHoveringRect(canvasMin, canvasMin + canvasSize)
				&& ImGui.IsWindowHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
			Vector2 imagePoint = canvasState.ViewportToImage(mouse - canvasMin, imageSize, canvasSize);
			bool over = state.Update(pointerOverCanvas, imagePoint, imageSize);

			float gridSide = ((2 * radius) + 1) * cellSize;
			float lineHeight = ImGui.GetTextLineHeightWithSpacing();
			Vector2 origin = ImGui.GetCursorScreenPos();

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			SemanticColor centre = default;
			if (state.HasPixel)
			{
				centre = DrawGrid(drawList, state, origin, radius, cellSize, imageSize, readPixel);
			}
			else
			{
				DrawEmptyGrid(drawList, origin, gridSide);
			}

			DrawReadout(state, centre, origin + new Vector2(0f, gridSide), lineHeight);

			// The box is reserved last, from the corner the drawing started at. Reserving first and moving
			// the cursor back below the box afterwards leaves a cursor move with no item after it, which
			// ImGui asserts on when the loupe is the last thing in its window.
			ImGui.SetCursorScreenPos(origin);
			ImGui.Dummy(new Vector2(gridSide, gridSide + (3 * lineHeight)));
			ImGuiProbes.MarkItem(label);

			if (over)
			{
				DrawCanvasMarker(label, state, canvasState, imageSize, canvasMin, canvasSize);
			}

			pixelX = state.X;
			pixelY = state.Y;
			return over;
		}

		/// <summary>Draws every cell around the inspected pixel.</summary>
		/// <returns>The centre pixel's colour, so the readout does not read it twice.</returns>
		private static SemanticColor DrawGrid(ImDrawListPtr drawList, PixelLoupeState state, Vector2 origin, int radius, float cellSize, Vector2 imageSize, Func<int, int, SemanticColor> readPixel)
		{
			uint outside = ImGui.GetColorU32(ImGuiCol.FrameBg);
			uint border = ImGui.GetColorU32(ImGuiCol.Border);
			Vector2 cell = new(cellSize);
			SemanticColor centre = default;

			for (int j = -radius; j <= radius; j++)
			{
				for (int i = -radius; i <= radius; i++)
				{
					int x = state.X + i;
					int y = state.Y + j;
					Vector2 cellMin = origin + (new Vector2(i + radius, j + radius) * cellSize);
					Vector2 cellMax = cellMin + cell;

					if (PixelLoupeState.InImage(x, y, imageSize))
					{
						SemanticColor color = readPixel(x, y);
						if (i == 0 && j == 0)
						{
							centre = color;
						}

						// The checkerboard shows through wherever the pixel is not opaque.
						DrawCheckerboard(drawList, cellMin, cell);
						drawList.AddRectFilled(cellMin, cellMax, color.ToImGuiU32());
					}
					else
					{
						drawList.AddRectFilled(cellMin, cellMax, outside);
					}

					if (cellSize >= GridLineMinCellSize)
					{
						drawList.AddRect(cellMin, cellMax, border, 0f, ImDrawFlags.None, 1f);
					}
				}
			}

			// Two outlines, so the centre reads on light and dark pixels alike.
			Vector2 centreMin = origin + new Vector2(radius * cellSize);
			Vector2 centreMax = centreMin + cell;
			drawList.AddRect(centreMin, centreMax, ImGui.GetColorU32(ImGuiCol.Text), 0f, ImDrawFlags.None, 2f);
			drawList.AddRect(centreMin + new Vector2(2f), centreMax - new Vector2(2f), ImGui.GetColorU32(ImGuiCol.WindowBg), 0f, ImDrawFlags.None, 1f);
			return centre;
		}

		/// <summary>Draws the grid area before any pixel has been hovered.</summary>
		private static void DrawEmptyGrid(ImDrawListPtr drawList, Vector2 origin, float gridSide)
		{
			drawList.AddRectFilled(origin, origin + new Vector2(gridSide), ImGui.GetColorU32(ImGuiCol.FrameBg));

			const string Hint = "Hover the image";
			Vector2 textSize = ImGui.CalcTextSize(Hint);
			Vector2 textPos = origin + ((new Vector2(gridSide) - textSize) / 2f);
			drawList.PushClipRect(origin, origin + new Vector2(gridSide), true);
			drawList.AddText(textPos, ImGui.GetColorU32(ImGuiCol.TextDisabled), Hint);
			drawList.PopClipRect();
		}

		/// <summary>Writes the three readout lines under the grid.</summary>
		private static void DrawReadout(PixelLoupeState state, SemanticColor centre, Vector2 origin, float lineHeight)
		{
			string position = "-";
			string channels = "-";
			string hex = "-";
			if (state.HasPixel)
			{
				(byte r, byte g, byte b, byte a) = centre.ToBytes();
				position = string.Create(CultureInfo.InvariantCulture, $"X {state.X}  Y {state.Y}");
				channels = string.Create(CultureInfo.InvariantCulture, $"R {r}  G {g}  B {b}  A {a}");
				hex = centre.ToHex();
			}

			string[] lines = [position, channels, hex];
			for (int line = 0; line < lines.Length; line++)
			{
				ImGui.SetCursorScreenPos(origin + new Vector2(0f, line * lineHeight));
				ImGui.TextUnformatted(lines[line]);
			}
		}

		/// <summary>Outlines the hovered pixel on the canvas itself.</summary>
		private static void DrawCanvasMarker(string label, PixelLoupeState state, ImageCanvasState canvasState, Vector2 imageSize, Vector2 canvasMin, Vector2 canvasSize)
		{
			Vector2 min = canvasMin + canvasState.ImageToViewport(new Vector2(state.X, state.Y), imageSize, canvasSize);
			Vector2 max = canvasMin + canvasState.ImageToViewport(new Vector2(state.X + 1, state.Y + 1), imageSize, canvasSize);

			// At low zoom a pixel is smaller than a line is wide, so grow it about its centre.
			Vector2 centre = (min + max) / 2f;
			Vector2 half = Vector2.Max((max - min) / 2f, new Vector2(MinMarkerSide / 2f));
			min = centre - half;
			max = centre + half;

			// The foreground list keeps the marker above anything drawn over the canvas later.
			ImDrawListPtr foreground = ImGui.GetForegroundDrawList();
			foreground.PushClipRect(canvasMin, canvasMin + canvasSize, true);
			foreground.AddRect(min, max, ImGui.GetColorU32(ImGuiCol.Text), 0f, ImDrawFlags.None, 1f);
			foreground.PopClipRect();
			ImGuiProbes.MarkRegion($"{label}/pixel", min, max);
		}
	}
}
