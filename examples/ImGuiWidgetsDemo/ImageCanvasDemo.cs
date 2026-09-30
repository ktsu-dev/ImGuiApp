// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;

/// <summary>
/// The pannable, zoomable image canvas.
/// </summary>
internal static class ImageCanvasDemo
{
	private static ImGuiWidgets.ImageCanvasState ImageCanvasDemoState { get; } = new();

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		// Nothing to reset: the canvas's pan and zoom are not restored by the demo's reset.
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("ImageCanvas"))
		{
			ImGui.TextUnformatted("Pannable, zoomable image canvas with a checkerboard behind transparency:");
			ImGui.BulletText("Drag to pan");
			ImGui.BulletText("Scroll to zoom toward the cursor");
			ImGui.BulletText("Double-click to fit the image to the canvas");
			ImGui.Separator();

			if (DemoProbe.Button("Fit"))
			{
				ImageCanvasDemoState.FitToViewport(new Vector2(DemoContext.KtsuTexture.Width, DemoContext.KtsuTexture.Height), new Vector2(ImGui.GetContentRegionAvail().X, 300f));
			}
			ImGui.SameLine();
			if (DemoProbe.Button("1:1"))
			{
				ImageCanvasDemoState.ResetToActualSize();
			}
			ImGui.SameLine();
			ImGui.TextUnformatted($"Zoom: {ImageCanvasDemoState.Zoom:0.##}x");

			Vector2 imageSize = new(DemoContext.KtsuTexture.Width, DemoContext.KtsuTexture.Height);
			Vector2 canvasSize = new(ImGui.GetContentRegionAvail().X, 300f);
			ImGuiWidgets.ImageCanvas("image_canvas_demo", DemoContext.KtsuTexture.TextureId, imageSize, ImageCanvasDemoState, canvasSize);
		}
	}
}
