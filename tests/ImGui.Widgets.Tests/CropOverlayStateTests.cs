// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using CropHandle = ImGuiWidgets.CropOverlayState.CropHandle;
using CropOverlayState = ImGuiWidgets.CropOverlayState;

/// <summary>
/// Tests the crop geometry and drag rules behind <see cref="ImGuiWidgets.CropOverlay"/>. All pure, no
/// ImGui context required. The image is 100x100 unless a test says otherwise.
/// </summary>
[TestClass]
public class CropOverlayStateTests
{
	private const float Tolerance = 1e-3f;
	private static readonly Vector2 Image = new(100, 100);
	private static readonly CropRect Square = new(new Vector2(50, 50), new Vector2(40, 40), 0f);

	[TestMethod]
	public void Normalize_DefaultBecomesTheFullImage()
	{
		CropRect crop = CropOverlayState.Normalize(default, Image, 0f, true);

		Assert.AreEqual(new CropRect(new Vector2(50, 50), new Vector2(100, 100), 0f), crop);
	}

	[TestMethod]
	public void Normalize_NaNBecomesTheFullImage()
	{
		CropRect crop = CropOverlayState.Normalize(new CropRect(new Vector2(float.NaN, 10), new Vector2(20, 20), 0f), Image, 0f, true);

		Assert.AreEqual(CropRect.FullImage(Image), crop);
	}

	[TestMethod]
	public void Normalize_AspectLockOnDefaultIsTheLargestCentredRect()
	{
		Vector2 wide = new(200, 100);

		CropRect twoToOne = CropOverlayState.Normalize(default, wide, 2f, true);
		CropRect square = CropOverlayState.Normalize(default, wide, 1f, true);

		AssertVector(new Vector2(200, 100), twoToOne.Size);
		AssertVector(new Vector2(100, 100), square.Size);
		AssertVector(new Vector2(100, 50), square.Center);
	}

	[TestMethod]
	public void Normalize_ClampsTheAngle()
	{
		CropRect clockwise = CropOverlayState.Normalize(Square with { AngleDegrees = 60f }, Image, 0f, true);
		CropRect anticlockwise = CropOverlayState.Normalize(Square with { AngleDegrees = -60f }, Image, 0f, true);

		Assert.AreEqual(45f, clockwise.AngleDegrees, Tolerance);
		Assert.AreEqual(-45f, anticlockwise.AngleDegrees, Tolerance);
	}

	[TestMethod]
	public void Normalize_ForcesZeroAngleWithoutRotation()
	{
		CropRect crop = CropOverlayState.Normalize(Square with { AngleDegrees = 30f }, Image, 0f, false);

		Assert.AreEqual(0f, crop.AngleDegrees);
	}

	[TestMethod]
	public void Normalize_ClampsSizeToTheImage()
	{
		CropRect crop = CropOverlayState.Normalize(new CropRect(new Vector2(50, 50), new Vector2(150, 20), 0f), Image, 0f, true);

		AssertVector(new Vector2(100, 20), crop.Size);
	}

	[TestMethod]
	public void Normalize_TranslatesInside()
	{
		CropRect crop = CropOverlayState.Normalize(new CropRect(new Vector2(95, 50), new Vector2(20, 20), 0f), Image, 0f, true);

		AssertVector(new Vector2(90, 50), crop.Center);
	}

	[TestMethod]
	public void Normalize_ShrinksARotatedCropUntilItFits()
	{
		CropRect crop = CropOverlayState.Normalize(new CropRect(new Vector2(50, 50), new Vector2(100, 100), 45f), Image, 0f, true);

		Assert.IsTrue(CropOverlayState.IsInside(crop, Image));
		AssertVector(new Vector2(50, 50), crop.Center);
		Assert.AreEqual(70.71f, crop.Size.X, 0.05f);
		Assert.AreEqual(70.71f, crop.Size.Y, 0.05f);
	}

	[TestMethod]
	public void Normalize_LeavesAValidCropUnchanged()
	{
		CropRect valid = new(new Vector2(40, 40), new Vector2(20, 30), 10f);

		Assert.AreEqual(valid, CropOverlayState.Normalize(valid, Image, 0f, true));
	}

	[TestMethod]
	public void IsInside_FalseWhenARotatedCornerLeaves()
	{
		Assert.IsFalse(CropOverlayState.IsInside(new CropRect(new Vector2(50, 50), new Vector2(100, 100), 10f), Image));
	}

	[TestMethod]
	public void HitTest_FindsEachCorner()
	{
		Assert.AreEqual(CropHandle.TopLeft, CropOverlayState.HitTest(Square, new Vector2(30, 30), 1f, true));
		Assert.AreEqual(CropHandle.TopRight, CropOverlayState.HitTest(Square, new Vector2(70, 30), 1f, true));
		Assert.AreEqual(CropHandle.BottomLeft, CropOverlayState.HitTest(Square, new Vector2(30, 70), 1f, true));
		Assert.AreEqual(CropHandle.BottomRight, CropOverlayState.HitTest(Square, new Vector2(70, 70), 1f, true));
	}

	[TestMethod]
	public void HitTest_FindsEdgesBodyAndPan()
	{
		Assert.AreEqual(CropHandle.Left, CropOverlayState.HitTest(Square, new Vector2(30, 50), 1f, true));
		Assert.AreEqual(CropHandle.Body, CropOverlayState.HitTest(Square, new Vector2(50, 50), 1f, true));
		Assert.AreEqual(CropHandle.Pan, CropOverlayState.HitTest(Square, new Vector2(5, 5), 1f, true));
	}

	[TestMethod]
	public void HitTest_FindsTheRotateHandleAboveTheTop()
	{
		Assert.AreEqual(CropHandle.Rotate, CropOverlayState.HitTest(Square, new Vector2(50, 6), 1f, true));
		Assert.AreEqual(CropHandle.Pan, CropOverlayState.HitTest(Square, new Vector2(50, 6), 1f, false));
	}

	[TestMethod]
	public void HitTest_GrabRadiusShrinksWithZoom()
	{
		Assert.AreEqual(CropHandle.Top, CropOverlayState.HitTest(Square, new Vector2(34, 30), 4f, true));
	}

	[TestMethod]
	public void HitTest_WorksInRotatedSpace()
	{
		CropRect rotated = Square with { AngleDegrees = 90f };

		Assert.AreEqual(CropHandle.TopLeft, CropOverlayState.HitTest(rotated, new Vector2(70, 30), 1f, true));
	}

	[TestMethod]
	public void Update_BodyDragMovesTheCentre()
	{
		CropOverlayState state = new();
		state.Begin(CropHandle.Body, Square, new Vector2(50, 50));

		CropRect crop = state.Update(new Vector2(60, 55), Image, 0f);

		AssertVector(new Vector2(60, 55), crop.Center);
		AssertVector(Square.Size, crop.Size);
	}

	[TestMethod]
	public void Update_BodyDragStopsAtTheImageEdge()
	{
		CropOverlayState state = new();
		state.Begin(CropHandle.Body, Square, new Vector2(50, 50));

		CropRect crop = state.Update(new Vector2(200, 50), Image, 0f);

		Assert.AreEqual(80f, crop.Center.X, 0.01f);
	}

	[TestMethod]
	public void Update_RightEdgeDragKeepsTheLeftEdge()
	{
		CropOverlayState state = new();
		state.Begin(CropHandle.Right, Square, new Vector2(70, 50));

		CropRect crop = state.Update(new Vector2(80, 50), Image, 0f);

		AssertVector(new Vector2(50, 40), crop.Size);
		AssertVector(new Vector2(55, 50), crop.Center);
	}

	[TestMethod]
	public void Update_EdgeCannotCrossTheOppositeEdge()
	{
		CropOverlayState state = new();
		state.Begin(CropHandle.Right, Square, new Vector2(70, 50));

		CropRect crop = state.Update(new Vector2(0, 50), Image, 0f);

		Assert.AreEqual(CropOverlayState.MinSize, crop.Size.X, Tolerance);
	}

	[TestMethod]
	public void Update_CornerDragKeepsTheOppositeCorner()
	{
		CropOverlayState state = new();
		state.Begin(CropHandle.BottomRight, Square, new Vector2(70, 70));

		Vector2[] corners = state.Update(new Vector2(80, 90), Image, 0f).Corners();

		AssertVector(new Vector2(30, 30), corners[0]);
		AssertVector(new Vector2(80, 90), corners[2]);
	}

	[TestMethod]
	public void Update_CornerDragHonoursTheAspectLock()
	{
		CropOverlayState state = new();
		state.Begin(CropHandle.BottomRight, Square, new Vector2(70, 70));

		CropRect crop = state.Update(new Vector2(90, 75), Image, 1f);

		AssertVector(new Vector2(60, 60), crop.Size);
	}

	[TestMethod]
	public void Update_EdgeDragWithAspectLockGrowsSymmetrically()
	{
		CropOverlayState state = new();
		state.Begin(CropHandle.Right, Square, new Vector2(70, 50));

		CropRect crop = state.Update(new Vector2(80, 50), Image, 1f);

		AssertVector(new Vector2(50, 50), crop.Size);
		Assert.AreEqual(50f, crop.Center.Y, Tolerance);
	}

	[TestMethod]
	public void Update_RotateSetsTheAngleFromThePointer()
	{
		CropOverlayState state = new();
		state.Begin(CropHandle.Rotate, Square, new Vector2(50, 6));

		Assert.AreEqual(45f, state.Update(new Vector2(94, 50), Image, 0f).AngleDegrees, Tolerance);
		Assert.AreEqual(12.8f, state.Update(new Vector2(60, 6), Image, 0f).AngleDegrees, 0.5f);
	}

	[TestMethod]
	public void Update_PanLeavesTheCropUnchanged()
	{
		CropOverlayState state = new();
		state.Begin(CropHandle.Pan, Square, new Vector2(5, 5));

		Assert.AreEqual(Square, state.Update(new Vector2(90, 90), Image, 0f));
	}

	[TestMethod]
	public void End_ClearsTheActiveHandle()
	{
		CropOverlayState state = new();
		state.Begin(CropHandle.Body, Square, new Vector2(50, 50));

		state.End();

		Assert.AreEqual(CropHandle.None, state.Active);
	}

	private static void AssertVector(Vector2 expected, Vector2 actual)
	{
		Assert.AreEqual(expected.X, actual.X, Tolerance);
		Assert.AreEqual(expected.Y, actual.Y, Tolerance);
	}
}
