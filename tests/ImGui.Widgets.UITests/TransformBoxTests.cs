// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives <see cref="ImGuiWidgets.TransformBox"/> over an <see cref="ImGuiWidgets.ImageCanvas"/>, the way
/// an image editor arranges a layer. The box is the unit square, drawn 100 pixels wide and 50 tall with
/// its top left 100 pixels into the canvas, so one frame unit is 100 screen pixels across and 50 down.
/// </summary>
[TestClass]
public sealed class TransformBoxTests : WidgetTest
{
	private const string Id = "box";
	private const float Tolerance = 0.01f;
	private static readonly Vector2 ImageSize = new(32f, 32f);
	private static readonly Vector2 CanvasSize = new(320f, 320f);
	private static readonly Vector2 BoxOffset = new(100f, 100f);
	private static readonly Vector2 BoxScale = new(100f, 50f);

	private readonly ImGuiWidgets.ImageCanvasState canvas = new();
	private readonly TransformBoxOptions options = new();
	private ImGuiAppTextureInfo? texture;
	private TransformBoxRect rect = TransformBoxRect.Unit;
	private Vector2 canvasMin;
	private bool changed;
	private bool sawHeld;
	private bool sawReleased;
	private bool sawSkewing;
	private bool sawDistorting;

	[TestInitialize]
	public void SetUpCanvas() => canvas.FitToViewport(ImageSize, CanvasSize);

	private void Draw()
	{
		texture ??= CreateTestTexture(32);

		// The box takes the pointer only over itself, and only if the canvas lets it.
		ImGui.SetNextItemAllowOverlap();
		ImGuiWidgets.ImageCanvas("img", texture.TextureId, ImageSize, canvas, CanvasSize);
		canvasMin = ImGui.GetItemRectMin();

		Matrix3x2 frameToScreen = Matrix3x2.CreateScale(BoxScale) * Matrix3x2.CreateTranslation(canvasMin + BoxOffset);
		TransformBoxResult result = ImGuiWidgets.TransformBox(Id, ref rect, frameToScreen, canvasMin, canvasMin + CanvasSize, options);
		changed |= result.Changed;
		sawHeld |= result.Held;
		sawReleased |= result.Released;
		sawSkewing |= result.Skewing;
		sawDistorting |= result.Distorting;

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
	public void TransformBox_MarksItsHandles()
	{
		Start(Draw);

		foreach (string name in new[] { "box/body", "box/topLeft", "box/topRight", "box/bottomRight", "box/bottomLeft", "box/top", "box/bottom", "box/left", "box/right" })
		{
			Assert.IsTrue(IsVisible(name), $"The box marked no probe region called {name}.");
		}
	}

	[TestMethod]
	public void TransformBox_DraggingTheBodyMovesTheRectangleInItsFrame()
	{
		Start(Draw);

		DragFrom(new Vector2(150, 125), new Vector2(40, 10));

		AssertVector(new Vector2(0.4f, 0.2f), rect.Min);
		AssertVector(new Vector2(1.4f, 1.2f), rect.Max);
		Assert.IsTrue(changed, "Moving the box did not report a change.");
	}

	[TestMethod]
	public void TransformBox_DraggingAnEdgeMovesOnlyThatEdge()
	{
		Start(Draw);

		DragFrom(new Vector2(200, 125), new Vector2(50, 30));

		AssertVector(Vector2.Zero, rect.Min);
		AssertVector(new Vector2(1.5f, 1f), rect.Max);
	}

	[TestMethod]
	public void TransformBox_AFreeCornerMovesBothEdges()
	{
		Start(Draw);

		DragFrom(new Vector2(200, 150), new Vector2(50, 10));

		AssertVector(new Vector2(1.5f, 1.2f), rect.Max);
	}

	[TestMethod]
	public void TransformBox_UniformCornersKeepTheProportions()
	{
		options.UniformCorners = true;
		Start(Draw);

		DragFrom(new Vector2(200, 150), new Vector2(50, 10));

		AssertVector(Vector2.Zero, rect.Min);
		Assert.AreEqual(rect.Size.X, rect.Size.Y, Tolerance, "A uniform corner changed the frame's proportions.");
		Assert.IsTrue(rect.Size.X > 1f, "Dragging the corner outward did not grow the box.");
	}

	[TestMethod]
	public void TransformBox_ShiftInvertsTheUniformSetting()
	{
		Start(Draw);

		HarnessKeyboard.KeyDown(ImGuiKey.ModShift);
		Step();
		DragFrom(new Vector2(200, 150), new Vector2(50, 10));
		HarnessKeyboard.KeyUp(ImGuiKey.ModShift);
		Step();

		Assert.AreEqual(rect.Size.X, rect.Size.Y, Tolerance, "Shift did not keep the proportions of a free box.");
	}

	[TestMethod]
	public void TransformBox_DraggingOutsideItPansTheCanvasUnderneath()
	{
		Start(Draw);
		float panBefore = canvas.Pan.X;

		DragFrom(new Vector2(20, 20), new Vector2(30, 0));

		// Not exactly thirty: the canvas pans by the travel after its own gesture threshold.
		Assert.IsTrue(canvas.Pan.X > panBefore + 10f, $"Dragging outside the box did not pan the canvas (pan {canvas.Pan.X}).");
		Assert.AreEqual(TransformBoxRect.Unit, rect, "Panning the canvas moved the box.");
	}

	[TestMethod]
	public void TransformBox_ReportsTheHoldAndTheRelease()
	{
		Start(Draw);

		DragFrom(new Vector2(150, 125), new Vector2(20, 0));

		Assert.IsTrue(sawHeld, "The box never reported a held handle.");
		Assert.IsTrue(sawReleased, "The box never reported the release.");
	}

	[TestMethod]
	public void TransformBox_ACallersRuleDecidesTheRectangle()
	{
		// A rule that keeps the box from leaving the frame's left edge, as a crop keeps to its canvas.
		options.Resize = drag =>
		{
			Vector2 shift = new(MathF.Max(drag.Delta.X, -drag.PressRect.Min.X), drag.Delta.Y);
			return new TransformBoxRect(drag.PressRect.Min + shift, drag.PressRect.Max + shift);
		};
		Start(Draw);

		DragFrom(new Vector2(150, 125), new Vector2(-80, 0));

		Assert.AreEqual(0f, rect.Min.X, Tolerance, "The caller's rule was not what decided the rectangle.");
	}

	[TestMethod]
	public void TransformBox_CtrlDraggingAnEdgeSkewsAlongIt()
	{
		options.Skew = true;
		Start(Draw);

		HarnessKeyboard.KeyDown(ImGuiKey.ModCtrl);
		Step();
		DragFrom(new Vector2(150, 100), new Vector2(30, 20));
		HarnessKeyboard.KeyUp(ImGuiKey.ModCtrl);
		Step();

		// Thirty pixels is 0.3 of the frame across; the pointer's travel down is not a skew's business.
		Vector2[] corners = rect.Corners();
		AssertVector(new Vector2(0.3f, 0f), corners[0]);
		AssertVector(new Vector2(1.3f, 0f), corners[1]);
		AssertVector(Vector2.One, corners[2]);
		AssertVector(new Vector2(0f, 1f), corners[3]);
		Assert.IsTrue(sawSkewing, "The box did not report the drag as a skew.");
	}

	[TestMethod]
	public void TransformBox_CtrlShiftDraggingACornerMovesItAlone()
	{
		options.Distort = true;
		Start(Draw);

		HarnessKeyboard.KeyDown(ImGuiKey.ModCtrl);
		HarnessKeyboard.KeyDown(ImGuiKey.ModShift);
		Step();
		DragFrom(new Vector2(200, 150), new Vector2(20, 10));
		HarnessKeyboard.KeyUp(ImGuiKey.ModShift);
		HarnessKeyboard.KeyUp(ImGuiKey.ModCtrl);
		Step();

		// Twenty pixels is 0.2 of the frame across and ten is 0.2 down; the other three corners stay put.
		Assert.IsTrue(rect.IsDistorted, "The box was not distorted.");
		Vector2[] corners = rect.Corners();
		AssertVector(Vector2.Zero, corners[0]);
		AssertVector(new Vector2(1f, 0f), corners[1]);
		AssertVector(new Vector2(1.2f, 1.2f), corners[2]);
		AssertVector(new Vector2(0f, 1f), corners[3]);
		Assert.IsTrue(sawDistorting, "The box did not report the drag as a distort.");
	}

	[TestMethod]
	public void TransformBox_CtrlShiftScalesWhenDistortIsOff()
	{
		Start(Draw);

		HarnessKeyboard.KeyDown(ImGuiKey.ModCtrl);
		HarnessKeyboard.KeyDown(ImGuiKey.ModShift);
		Step();
		DragFrom(new Vector2(200, 150), new Vector2(20, 10));
		HarnessKeyboard.KeyUp(ImGuiKey.ModShift);
		HarnessKeyboard.KeyUp(ImGuiKey.ModCtrl);
		Step();

		Assert.IsFalse(rect.IsDistorted, "A box that does not distort was distorted.");
		Assert.IsFalse(sawDistorting);
	}

	[TestMethod]
	public void TransformBox_CtrlStretchesWhenSkewIsOff()
	{
		Start(Draw);

		HarnessKeyboard.KeyDown(ImGuiKey.ModCtrl);
		Step();
		DragFrom(new Vector2(150, 100), new Vector2(30, 20));
		HarnessKeyboard.KeyUp(ImGuiKey.ModCtrl);
		Step();

		Assert.IsFalse(rect.IsSkewed, "A box that does not skew was skewed.");
		AssertVector(new Vector2(0f, 0.4f), rect.Min);
		Assert.IsFalse(sawSkewing);
	}

	[TestMethod]
	public void TransformBox_DrawsItsOutline()
	{
		Start(Draw);
		byte[] before = Snapshot();

		rect = new TransformBoxRect(new Vector2(0.5f, 0.5f), new Vector2(1.5f, 1.5f));
		Step(2);

		Assert.IsTrue(PixelsChangedSince(before) > 0, "Moving the box changed nothing on screen.");
	}

	private static void AssertVector(Vector2 expected, Vector2 actual)
	{
		Assert.AreEqual(expected.X, actual.X, Tolerance, $"X of {actual}");
		Assert.AreEqual(expected.Y, actual.Y, Tolerance, $"Y of {actual}");
	}
}
