// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives <see cref="ImGuiWidgets.CropOverlay"/> over an <see cref="ImGuiWidgets.ImageCanvas"/>. The
/// 32 pixel image is fitted to a 320 pixel canvas, so one image pixel is ten screen pixels and the
/// image fills the canvas exactly.
/// </summary>
[TestClass]
public sealed class CropOverlayTests : WidgetTest
{
	private const string Id = "crop";
	private const float Tolerance = 0.2f;
	private static readonly Vector2 ImageSize = new(32f, 32f);
	private static readonly Vector2 CanvasSize = new(320f, 320f);

	private ImGuiAppTextureInfo? texture;
	private readonly ImGuiWidgets.ImageCanvasState canvas = new();
	private CropRect crop = new(new Vector2(16, 16), new Vector2(16, 16), 0f);
	private Vector2 canvasMin;
	private float aspectRatio;
	private bool allowRotation = true;
	private bool changed;

	[TestInitialize]
	public void SetUpCanvas() => canvas.FitToViewport(ImageSize, CanvasSize);

	private void Draw()
	{
		texture ??= CreateTestTexture(32);
		ImGuiWidgets.ImageCanvas("img", texture.TextureId, ImageSize, canvas, CanvasSize);
		canvasMin = ImGui.GetItemRectMin();
		changed |= ImGuiWidgets.CropOverlay(Id, ref crop, ImageSize, canvas, canvasMin, CanvasSize, aspectRatio, true, allowRotation);

		// The overlay restores the cursor, so something has to follow it before the window ends.
		ImGui.TextUnformatted("after");
	}

	/// <summary>Drags from a point given in canvas pixels, relative to the canvas corner.</summary>
	private void DragFrom(Vector2 canvasPoint, Vector2 travel)
	{
		Vector2 from = canvasMin + canvasPoint;
		Harness.Mouse.Drag(from.X, from.Y, from.X + travel.X, from.Y + travel.Y);
		Step(2);
	}

	[TestMethod]
	public void CropOverlay_MarksItsHandles()
	{
		Start(Draw);

		foreach (string name in new[] { Id, "crop/body", "crop/topLeft", "crop/bottomRight", "crop/left", "crop/rotate" })
		{
			Assert.IsTrue(IsVisible(name), $"The overlay marked no probe item called {name}.");
		}
	}

	[TestMethod]
	public void CropOverlay_DraggingTheBodyMovesTheCrop()
	{
		Start(Draw);

		DragFrom(new Vector2(160, 160), new Vector2(40, 0));

		Assert.AreEqual(20f, crop.Center.X, Tolerance, "Forty screen pixels at zoom 10 should move the crop four image pixels.");
		Assert.IsTrue(changed, "Moving the crop did not report a change.");
	}

	[TestMethod]
	public void CropOverlay_DraggingTheBottomRightCornerResizes()
	{
		Start(Draw);

		DragFrom(new Vector2(240, 240), new Vector2(30, 20));

		Assert.AreEqual(19f, crop.Size.X, Tolerance);
		Assert.AreEqual(18f, crop.Size.Y, Tolerance);
		Vector2 topLeft = crop.Corners()[0];
		Assert.AreEqual(8f, topLeft.X, Tolerance, "The opposite corner moved.");
		Assert.AreEqual(8f, topLeft.Y, Tolerance, "The opposite corner moved.");
	}

	[TestMethod]
	public void CropOverlay_AspectLockKeepsTheRatio()
	{
		aspectRatio = 2f;
		crop = new CropRect(new Vector2(16, 16), new Vector2(16, 8), 0f);
		Start(Draw);

		DragFrom(new Vector2(240, 200), new Vector2(40, 40));

		Assert.AreEqual(2f, crop.Size.X / crop.Size.Y, 0.01f);
	}

	[TestMethod]
	public void CropOverlay_CannotLeaveTheImage()
	{
		Start(Draw);

		DragFrom(new Vector2(160, 160), new Vector2(400, 0));

		Assert.AreEqual(32f, crop.Center.X + (crop.Size.X / 2f), Tolerance, "The crop was dragged past the image's right edge.");
	}

	[TestMethod]
	public void CropOverlay_RotateHandleRotates()
	{
		Start(Draw);

		DragFrom(new Vector2(160, 56), new Vector2(80, 80));

		Assert.IsTrue(crop.AngleDegrees is > 0f and <= 45f, $"Dragging the rotate handle left the angle at {crop.AngleDegrees}.");
	}

	[TestMethod]
	public void CropOverlay_NoRotateHandleWhenRotationIsOff()
	{
		allowRotation = false;
		Start(Draw);

		Assert.IsFalse(IsVisible("crop/rotate"), "A rotate handle was marked with rotation turned off.");
	}

	[TestMethod]
	public void CropOverlay_DraggingOutsideTheCropPansTheCanvas()
	{
		Start(Draw);
		CropRect before = crop;
		float panBefore = canvas.Pan.X;

		DragFrom(new Vector2(20, 20), new Vector2(30, 0));

		Assert.AreEqual(panBefore + 30f, canvas.Pan.X, 1f, "Dragging outside the crop did not pan the canvas by the drag.");
		Assert.AreEqual(before, crop, "Panning the canvas moved the crop.");
	}

	[TestMethod]
	public void CropOverlay_WheelZoomsTheCanvasOnce()
	{
		Start(Draw);

		Vector2 center = canvasMin + (CanvasSize / 2f);
		Harness.Mouse.Wheel(center.X, center.Y, 1);
		Step();

		Assert.AreEqual(11f, canvas.Zoom, 0.01f, "One wheel click over the overlay should zoom by exactly one step.");
	}

	[TestMethod]
	public void CropOverlay_DimsOutsideTheCrop()
	{
		Start(Draw);

		CapturedFrame frame = Harness.Capture();
		Rgba32 outside = frame.GetPixel((int)(canvasMin.X + 40), (int)(canvasMin.Y + 40));
		Rgba32 inside = frame.GetPixel((int)(canvasMin.X + 160), (int)(canvasMin.Y + 160));

		Assert.IsTrue(outside.R < inside.R, $"The image outside the crop (R {outside.R}) was not darker than inside it (R {inside.R}).");
	}

	[TestMethod]
	public void CropOverlay_DefaultCropNormalizesToTheFullImage()
	{
		crop = default;
		Start(Draw);

		Assert.AreEqual(new CropRect(new Vector2(16, 16), new Vector2(32, 32), 0f), crop);
		Assert.IsTrue(changed, "Normalizing an empty crop did not report a change.");
	}
}
