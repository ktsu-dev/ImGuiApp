// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;
using ktsu.ImGui.Widgets;

/// <summary>
/// The crop overlay, drawn over an image canvas.
/// </summary>
internal static class CropDemo
{
	private static readonly Vector2 CanvasSize = new(480f, 300f);
	private static readonly string[] AspectNames = ["Free", "1:1", "4:3", "16:9", "3:2"];
	private static readonly float[] AspectRatios = [0f, 1f, 4f / 3f, 16f / 9f, 3f / 2f];

	private static readonly ImGuiWidgets.ImageCanvasState Canvas = new();
	private static bool fitted;
	private static int aspectIndex;
	private static bool showThirds = true;
	private static bool allowRotation = true;

	/// <summary>Gets the crop the demo is editing.</summary>
	internal static CropRect Crop { get; private set; }

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		Crop = default;
		fitted = false;
		aspectIndex = 0;
		showThirds = true;
		allowRotation = true;
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (!DemoProbe.Header("Crop Overlay"))
		{
			return;
		}

		ImGui.TextUnformatted("Drag the crop, its edges and corners, or the handle above it to rotate.");
		ImGui.TextUnformatted("Dragging outside the crop pans the canvas; scroll to zoom.");

		ImGui.SetNextItemWidth(120f);
		ImGui.Combo("Aspect", ref aspectIndex, AspectNames, AspectNames.Length);
		ImGuiProbes.MarkItem("Aspect");
		ImGui.SameLine();
		DemoProbe.Checkbox("Thirds", ref showThirds);
		ImGui.SameLine();
		DemoProbe.Checkbox("Rotation", ref allowRotation);

		Vector2 imageSize = new(DemoContext.KtsuTexture.Width, DemoContext.KtsuTexture.Height);
		if (!fitted)
		{
			// Starts inside the image rather than covering it, so there is room to drag it about.
			Canvas.FitToViewport(imageSize, CanvasSize);
			Crop = new CropRect(imageSize / 2f, imageSize * 0.6f, 0f);
			fitted = true;
		}

		ImGuiWidgets.ImageCanvas("crop_demo_canvas", DemoContext.KtsuTexture.TextureId, imageSize, Canvas, CanvasSize);
		Vector2 canvasMin = ImGui.GetItemRectMin();

		CropRect crop = Crop;
		ImGuiWidgets.CropOverlay("crop_demo", ref crop, imageSize, Canvas, canvasMin, CanvasSize, AspectRatios[aspectIndex], showThirds, allowRotation);
		Crop = crop;

		ImGui.TextUnformatted($"Center: ({crop.Center.X:0.0}, {crop.Center.Y:0.0})  Size: {crop.Size.X:0.0} x {crop.Size.Y:0.0}  Angle: {crop.AngleDegrees:0.0}°");
	}
}
