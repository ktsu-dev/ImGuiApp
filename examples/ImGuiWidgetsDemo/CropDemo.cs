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
	private static readonly string[] GuideNames = ["None", "Thirds", "Golden ratio", "Golden spiral", "Diagonals", "Triangles", "Grid"];
	private static int guideIndex = 1;
	private static bool allowRotation = true;

	/// <summary>Gets the guide the demo draws, or null for none.</summary>
	internal static CropGuide? Guide => guideIndex == 0 ? null : (CropGuide)(guideIndex - 1);

	/// <summary>Chooses the guide, as the combo does, for a test that cannot open a combo by name.</summary>
	/// <param name="guide">The guide, or null for none.</param>
	internal static void ChooseGuide(CropGuide? guide) => guideIndex = guide is CropGuide chosen ? (int)chosen + 1 : 0;

	/// <summary>Gets which way round the demo draws an asymmetric guide.</summary>
	internal static int GuideOrientation { get; private set; }

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
		guideIndex = 1;
		GuideOrientation = 0;
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
		ImGui.SetNextItemWidth(140f);
		ImGui.Combo("Guide", ref guideIndex, GuideNames, GuideNames.Length);
		ImGuiProbes.MarkItem("Guide");
		if (Guide is CropGuide guide && CropGuides.IsAsymmetric(guide))
		{
			ImGui.SameLine();
			if (DemoProbe.Button("Flip guide"))
			{
				GuideOrientation = CropGuides.NextOrientation(GuideOrientation);
			}
		}

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
		CropOverlayOptions options = new()
		{
			AspectRatio = AspectRatios[aspectIndex],
			Guide = Guide,
			GuideOrientation = GuideOrientation,
			AllowRotation = allowRotation,
		};
		ImGuiWidgets.CropOverlay("crop_demo", ref crop, imageSize, Canvas, canvasMin, CanvasSize, options);
		Crop = crop;

		ImGui.TextUnformatted($"Center: ({crop.Center.X:0.0}, {crop.Center.Y:0.0})  Size: {crop.Size.X:0.0} x {crop.Size.Y:0.0}  Angle: {crop.AngleDegrees:0.0}°");
	}
}
