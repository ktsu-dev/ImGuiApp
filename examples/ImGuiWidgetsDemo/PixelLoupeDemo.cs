// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Images;
using ktsu.ImGui.Widgets;
using ktsu.Semantics.Color;

/// <summary>
/// The pixel loupe, inspecting an image canvas drawn beside it.
/// </summary>
internal static class PixelLoupeDemo
{
	private static readonly Vector2 CanvasSize = new(320f, 320f);

	private static readonly ImGuiWidgets.ImageCanvasState Canvas = new();
	private static ImagePixels? pixels;
	private static bool fitted;
	private static int radius = 4;
	private static float cellSize = 12f;

	/// <summary>Gets the x of the pixel the loupe is inspecting, or -1 before one has been hovered.</summary>
	internal static int PixelX { get; private set; } = -1;

	/// <summary>Gets the y of the pixel the loupe is inspecting, or -1 before one has been hovered.</summary>
	internal static int PixelY { get; private set; } = -1;

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		fitted = false;
		radius = 4;
		cellSize = 12f;
		PixelX = -1;
		PixelY = -1;
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (!DemoProbe.Header("Pixel Loupe"))
		{
			return;
		}

		ImGui.TextUnformatted("Hover the image to magnify the pixels around the pointer and read the centre one.");
		ImGui.TextUnformatted("The loupe shows the decoded file's pixels, supplied by the host, not the texture's.");

		ImGui.SetNextItemWidth(160f);
		DemoProbe.SliderInt("Loupe radius", ref radius, 0, 12);
		ImGui.SameLine();
		ImGui.SetNextItemWidth(160f);
		DemoProbe.SliderFloat("Loupe cell size", ref cellSize, 4f, 24f);

		// The widgets library cannot read a texture back, so the demo decodes the file it was loaded from.
		pixels ??= ImageDecoder.Load(DemoContext.KtsuIconPath);
		Vector2 imageSize = new(pixels.Width, pixels.Height);
		if (!fitted)
		{
			Canvas.FitToViewport(imageSize, CanvasSize);
			fitted = true;
		}

		ImGuiWidgets.ImageCanvas("pixel_loupe_canvas", DemoContext.KtsuTexture.TextureId, imageSize, Canvas, CanvasSize);
		Vector2 canvasMin = ImGui.GetItemRectMin();
		ImGui.SameLine();
		ImGuiWidgets.PixelLoupe("pixel_loupe", Canvas, imageSize, canvasMin, CanvasSize, ReadPixel, out int x, out int y, radius, cellSize);
		PixelX = x;
		PixelY = y;
	}

	private static Color ReadPixel(int x, int y)
	{
		(byte r, byte g, byte b, byte a) = pixels!.GetPixel(x, y);
		return Color.FromBytes(r, g, b, a);
	}
}
