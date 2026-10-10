// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using TransformBoxState = ImGuiWidgets.TransformBoxState;

/// <summary>
/// Tests the hit-testing and drag rules behind <see cref="ImGuiWidgets.TransformBox"/>. All pure, no
/// ImGui context required. Unless a test says otherwise the box is the unit square drawn 200 pixels
/// wide and 100 tall with its top left at (100, 100), so the frame is stretched twice as much
/// horizontally as vertically, which is what a layer's own frame usually is.
/// </summary>
[TestClass]
public class TransformBoxStateTests
{
	private const float Tolerance = 1e-4f;

	private static readonly Matrix3x2 Stretched = Matrix3x2.CreateScale(200f, 100f) * Matrix3x2.CreateTranslation(100f, 100f);

	[TestMethod]
	public void HitTest_FindsEachCornerOnScreen()
	{
		Assert.AreEqual(TransformBoxHandle.TopLeft, Hit(new Vector2(103, 102)));
		Assert.AreEqual(TransformBoxHandle.TopRight, Hit(new Vector2(298, 96)));
		Assert.AreEqual(TransformBoxHandle.BottomRight, Hit(new Vector2(305, 205)));
		Assert.AreEqual(TransformBoxHandle.BottomLeft, Hit(new Vector2(100, 200)));
	}

	[TestMethod]
	public void HitTest_FindsEachEdgeAlongItsLength()
	{
		Assert.AreEqual(TransformBoxHandle.Top, Hit(new Vector2(150, 104)));
		Assert.AreEqual(TransformBoxHandle.Bottom, Hit(new Vector2(250, 195)));
		Assert.AreEqual(TransformBoxHandle.Left, Hit(new Vector2(95, 150)));
		Assert.AreEqual(TransformBoxHandle.Right, Hit(new Vector2(306, 130)));
	}

	[TestMethod]
	public void HitTest_FindsTheBodyAndNothingBeyondIt()
	{
		Assert.AreEqual(TransformBoxHandle.Body, Hit(new Vector2(200, 150)));
		Assert.AreEqual(TransformBoxHandle.None, Hit(new Vector2(50, 50)));
		Assert.AreEqual(TransformBoxHandle.None, Hit(new Vector2(200, 215)));
	}

	[TestMethod]
	public void HitTest_ReachIsInScreenPixelsHoweverTheFrameIsStretched()
	{
		// Eight pixels from each edge, which is an eighth of the unit square vertically and a
		// twenty-fifth horizontally; one radius in the frame would have reached unequally.
		Assert.AreEqual(TransformBoxHandle.Left, Hit(new Vector2(92, 150)));
		Assert.AreEqual(TransformBoxHandle.Top, Hit(new Vector2(200, 92)));
		Assert.AreEqual(TransformBoxHandle.None, Hit(new Vector2(91, 150)));
		Assert.AreEqual(TransformBoxHandle.None, Hit(new Vector2(200, 91)));
	}

	[TestMethod]
	public void HitTest_FollowsATurnedFrame()
	{
		// A quarter turn clockwise about the box's centre: the frame's top left lands at the screen's
		// top right.
		Matrix3x2 turned = Matrix3x2.CreateScale(100f) * Matrix3x2.CreateRotation(MathF.PI / 2f, new Vector2(50f, 50f)) * Matrix3x2.CreateTranslation(100f, 100f);

		Assert.AreEqual(TransformBoxHandle.TopLeft, TransformBoxState.HitTest(TransformBoxRect.Unit, turned, new Vector2(200, 100), true));
		Assert.AreEqual(TransformBoxHandle.Top, TransformBoxState.HitTest(TransformBoxRect.Unit, turned, new Vector2(200, 150), true));
	}

	[TestMethod]
	public void HitTest_ATinyBoxStillHasABody()
	{
		Matrix3x2 tiny = Matrix3x2.CreateScale(12f) * Matrix3x2.CreateTranslation(100f, 100f);

		Assert.AreEqual(TransformBoxHandle.Body, TransformBoxState.HitTest(TransformBoxRect.Unit, tiny, new Vector2(106, 106), true));
		Assert.AreEqual(TransformBoxHandle.TopLeft, TransformBoxState.HitTest(TransformBoxRect.Unit, tiny, new Vector2(97, 97), true));
	}

	[TestMethod]
	public void HitTest_WithoutEdgeHandlesAnEdgeIsBody()
	{
		Assert.AreEqual(TransformBoxHandle.Body, TransformBoxState.HitTest(TransformBoxRect.Unit, Stretched, new Vector2(150, 104), false));
	}

	[TestMethod]
	public void HitTest_ASingularFrameHoldsNothing()
	{
		Matrix3x2 flat = Matrix3x2.CreateScale(200f, 0f) * Matrix3x2.CreateTranslation(100f, 100f);

		Assert.AreEqual(TransformBoxHandle.None, TransformBoxState.HitTest(TransformBoxRect.Unit, flat, new Vector2(200, 100), true));
	}

	[TestMethod]
	public void Drag_MeasuresInTheFrameThePressBeganIn()
	{
		TransformBoxState state = new();
		Assert.IsTrue(state.Begin(TransformBoxHandle.Body, TransformBoxRect.Unit, Stretched, new Vector2(200, 150)));

		TransformBoxDrag drag = state.Drag(new Vector2(240, 160), uniform: false);

		AssertVector(new Vector2(0.2f, 0.1f), drag.Delta);
		Assert.AreEqual(Stretched, drag.FrameToScreen);
	}

	[TestMethod]
	public void Begin_RefusesASingularFrame()
	{
		TransformBoxState state = new();

		Assert.IsFalse(state.Begin(TransformBoxHandle.Body, TransformBoxRect.Unit, Matrix3x2.CreateScale(0f), Vector2.Zero));
		Assert.AreEqual(TransformBoxHandle.None, state.Active);
	}

	[TestMethod]
	public void Resize_TheBodyMovesWithoutResizing()
	{
		TransformBoxRect moved = Resize(TransformBoxHandle.Body, new Vector2(0.25f, -0.5f));

		Assert.AreEqual(new TransformBoxRect(new Vector2(0.25f, -0.5f), new Vector2(1.25f, 0.5f)), moved);
	}

	[TestMethod]
	public void Resize_AnEdgeMovesOnlyThatEdge()
	{
		TransformBoxRect right = Resize(TransformBoxHandle.Right, new Vector2(0.5f, 3f));
		TransformBoxRect top = Resize(TransformBoxHandle.Top, new Vector2(3f, 0.25f));

		Assert.AreEqual(new TransformBoxRect(Vector2.Zero, new Vector2(1.5f, 1f)), right);
		Assert.AreEqual(new TransformBoxRect(new Vector2(0f, 0.25f), Vector2.One), top);
	}

	[TestMethod]
	public void Resize_AFreeCornerMovesBothItsEdges()
	{
		TransformBoxRect corner = Resize(TransformBoxHandle.TopLeft, new Vector2(0.25f, 0.5f));

		Assert.AreEqual(new TransformBoxRect(new Vector2(0.25f, 0.5f), Vector2.One), corner);
	}

	[TestMethod]
	public void Resize_AUniformCornerKeepsTheProportionsAboutTheOppositeCorner()
	{
		TransformBoxRect scaled = Resize(TransformBoxHandle.BottomRight, new Vector2(1f, 0.2f), uniform: true);

		AssertVector(Vector2.Zero, scaled.Min);
		Assert.AreEqual(1f, scaled.Size.X / scaled.Size.Y, Tolerance, "The proportions changed.");
	}

	[TestMethod]
	public void Resize_AUniformCornerScalesByTheDistanceSeenOnScreen()
	{
		// On screen the frame's x is twice its y. A pointer that moved 0.5 of the frame to the right
		// travelled 100 pixels, and the diagonal it is projected on runs 200 by 100, so the pointer
		// reaches 200·100/(200²+100²) = 0.4 of the way further along it: a scale of 1.4. Measured in the
		// frame, where the diagonal is (1, 1), it would have been 1.25.
		TransformBoxRect scaled = Resize(TransformBoxHandle.BottomRight, new Vector2(0.5f, 0f), uniform: true);

		AssertVector(new Vector2(1.4f, 1.4f), scaled.Max);
	}

	[TestMethod]
	public void Resize_AUniformTopLeftCornerHoldsTheBottomRight()
	{
		TransformBoxRect scaled = Resize(TransformBoxHandle.TopLeft, new Vector2(0.5f, 0.5f), uniform: true);

		AssertVector(Vector2.One, scaled.Max);
		AssertVector(new Vector2(0.5f, 0.5f), scaled.Min);
	}

	[TestMethod]
	public void Resize_AnEdgeDraggedPastItsOppositeStopsAtTheMinimum()
	{
		// Four pixels, which is 0.02 of the frame across and 0.04 down.
		TransformBoxRect right = Resize(TransformBoxHandle.Right, new Vector2(-3f, 0f));
		TransformBoxRect bottom = Resize(TransformBoxHandle.Bottom, new Vector2(0f, -3f));

		Assert.AreEqual(0.02f, right.Size.X, Tolerance);
		Assert.AreEqual(0.04f, bottom.Size.Y, Tolerance);
	}

	[TestMethod]
	public void Resize_AUniformCornerDraggedThroughItsOppositeStopsAtTheMinimum()
	{
		TransformBoxRect scaled = Resize(TransformBoxHandle.BottomRight, new Vector2(-3f, -3f), uniform: true);

		AssertVector(Vector2.Zero, scaled.Min);
		Assert.AreEqual(0.04f, scaled.Size.Y, Tolerance, "The shorter side on screen should be held at four pixels.");
		Assert.AreEqual(scaled.Size.X, scaled.Size.Y, Tolerance, "The proportions changed.");
	}

	[TestMethod]
	public void Resize_NoHandleChangesNothing()
	{
		Assert.AreEqual(TransformBoxRect.Unit, Resize(TransformBoxHandle.None, new Vector2(1f, 1f)));
	}

	[TestMethod]
	public void Skew_TheTopEdgeSlidesAlongItselfAndTheBottomStays()
	{
		// The pointer's travel down is ignored: a skew only moves the edge along its own length.
		TransformBoxRect skewed = Skew(TransformBoxRect.Unit, TransformBoxHandle.Top, new Vector2(0.2f, 0.3f));

		AssertCorners(skewed, new Vector2(0.2f, 0f), new Vector2(1.2f, 0f), Vector2.One, new Vector2(0f, 1f));
		Assert.AreEqual(-0.2f, skewed.Skew.X, Tolerance);
		Assert.AreEqual(0f, skewed.Skew.Y, Tolerance);
	}

	[TestMethod]
	public void Skew_TheRightEdgeSlidesDownAndTheLeftStays()
	{
		TransformBoxRect skewed = Skew(TransformBoxRect.Unit, TransformBoxHandle.Right, new Vector2(3f, 0.25f));

		AssertCorners(skewed, Vector2.Zero, new Vector2(1f, 0.25f), new Vector2(1f, 1.25f), new Vector2(0f, 1f));
	}

	[TestMethod]
	public void Skew_TheBottomAndLeftEdgesSlantTheOtherWay()
	{
		TransformBoxRect bottom = Skew(TransformBoxRect.Unit, TransformBoxHandle.Bottom, new Vector2(0.5f, 0f));
		TransformBoxRect left = Skew(TransformBoxRect.Unit, TransformBoxHandle.Left, new Vector2(0f, 0.5f));

		AssertCorners(bottom, Vector2.Zero, new Vector2(1f, 0f), new Vector2(1.5f, 1f), new Vector2(0.5f, 1f));
		AssertCorners(left, new Vector2(0f, 0.5f), new Vector2(1f, 0f), Vector2.One, new Vector2(0f, 1.5f));
	}

	[TestMethod]
	public void Skew_AddsToASkewAlreadyThere()
	{
		TransformBoxRect once = Skew(TransformBoxRect.Unit, TransformBoxHandle.Top, new Vector2(0.2f, 0f));
		TransformBoxRect twice = Skew(once, TransformBoxHandle.Top, new Vector2(0.2f, 0f));

		AssertCorners(twice, new Vector2(0.4f, 0f), new Vector2(1.4f, 0f), Vector2.One, new Vector2(0f, 1f));
	}

	[TestMethod]
	public void Skew_IsHeldShortOfFoldingTheBoxFlat()
	{
		TransformBoxRect steep = Skew(TransformBoxRect.Unit, TransformBoxHandle.Bottom, new Vector2(1000f, 0f));
		Assert.AreEqual(TransformBoxState.MaximumSkew, steep.Skew.X, Tolerance);

		// With one axis already slanted, the other stops while the box keeps a tenth of its area.
		TransformBoxRect slanted = TransformBoxRect.Unit with { Skew = new Vector2(0f, 2f) };
		TransformBoxRect both = Skew(slanted, TransformBoxHandle.Bottom, new Vector2(5f, 0f));
		Assert.AreEqual(1f - TransformBoxState.MinimumSkewArea, both.Skew.X * both.Skew.Y, Tolerance);
	}

	[TestMethod]
	public void Skew_OnlyAnEdgeSkews()
	{
		TransformBoxState state = new();

		Assert.IsTrue(state.Begin(TransformBoxHandle.TopLeft, TransformBoxRect.Unit, Stretched, new Vector2(100, 100), skew: true));
		Assert.IsFalse(state.Skewing, "A corner scales; it has nothing to slide along.");
		Assert.IsFalse(state.Drag(new Vector2(120, 100), uniform: false).Skew);

		Assert.IsTrue(state.Begin(TransformBoxHandle.Top, TransformBoxRect.Unit, Stretched, new Vector2(200, 100), skew: true));
		Assert.IsTrue(state.Drag(new Vector2(240, 100), uniform: false).Skew);

		state.End();
		Assert.IsFalse(state.Skewing);
	}

	[TestMethod]
	public void Resize_TheBodyCarriesTheSkew()
	{
		TransformBoxRect skewed = TransformBoxRect.Unit with { Skew = new Vector2(-0.2f, 0f) };

		TransformBoxRect moved = TransformBoxState.Resize(new TransformBoxDrag(TransformBoxHandle.Body, skewed, Vector2.Zero, new Vector2(0.5f, 0f), false, Stretched), 4f);

		Assert.AreEqual(skewed.Skew, moved.Skew);
		AssertVector(new Vector2(0.5f, 0f), moved.Min);
	}

	[TestMethod]
	public void Resize_ASkewedEdgeGrowsAlongTheSlantAndTheOppositeEdgeStays()
	{
		// Leaning right by a fifth of its height: top at 0.1 to 1.1, bottom at -0.1 to 0.9. Pulling the
		// bottom down half a unit carries it a further tenth left, along the slant.
		TransformBoxRect skewed = TransformBoxRect.Unit with { Skew = new Vector2(-0.2f, 0f) };

		TransformBoxRect taller = TransformBoxState.Resize(new TransformBoxDrag(TransformBoxHandle.Bottom, skewed, Vector2.Zero, new Vector2(0f, 0.5f), false, Stretched), 4f);

		AssertCorners(taller, new Vector2(0.1f, 0f), new Vector2(1.1f, 0f), new Vector2(0.8f, 1.5f), new Vector2(-0.2f, 1.5f));
	}

	[TestMethod]
	public void Resize_ASkewedUniformCornerHoldsTheOppositeCorner()
	{
		TransformBoxRect skewed = TransformBoxRect.Unit with { Skew = new Vector2(-0.2f, 0f) };
		Vector2 topLeft = skewed.Corners()[0];

		TransformBoxRect scaled = TransformBoxState.Resize(new TransformBoxDrag(TransformBoxHandle.BottomRight, skewed, Vector2.Zero, new Vector2(0.5f, 0.5f), true, Stretched), 4f);

		AssertVector(topLeft, scaled.Corners()[0]);
		Assert.AreEqual(scaled.Size.X, scaled.Size.Y, Tolerance, "The proportions changed.");
		Assert.AreEqual(skewed.Skew, scaled.Skew);
	}

	[TestMethod]
	public void HitTest_FollowsASkewedBox()
	{
		// Leaning right by half its height, so the right side runs from (1.25, 0) to (0.75, 1).
		TransformBoxRect skewed = TransformBoxRect.Unit with { Skew = new Vector2(-0.5f, 0f) };

		// The frame's (1.0, 0.2) is on the unskewed right edge, and well inside the slanted one.
		Assert.AreEqual(TransformBoxHandle.Body, TransformBoxState.HitTest(skewed, Stretched, new Vector2(300, 120), true));

		// The frame's (0.05, 0.1) is inside the rectangle, and outside the slanted left side.
		Assert.AreEqual(TransformBoxHandle.None, TransformBoxState.HitTest(skewed, Stretched, new Vector2(110, 110), true));

		// The top-left corner has moved with the slant.
		Assert.AreEqual(TransformBoxHandle.TopLeft, TransformBoxState.HitTest(skewed, Stretched, new Vector2(150, 100), true));
	}

	[TestMethod]
	public void Shape_IsTheIdentityUntilTheBoxIsSkewed()
	{
		Assert.AreEqual(Matrix3x2.Identity, TransformBoxRect.Unit.Shape());
		Assert.IsFalse(TransformBoxRect.Unit.IsSkewed);
		Assert.AreNotEqual(TransformBoxRect.Unit, TransformBoxRect.Unit with { Skew = new Vector2(0.1f, 0f) });
	}

	[TestMethod]
	public void Distort_MovesTheHeldCornerAloneAndKeepsTheOthers()
	{
		TransformBoxRect distorted = Distort(TransformBoxRect.Unit, TransformBoxHandle.TopRight, new Vector2(0.2f, 0.3f));

		Assert.IsTrue(distorted.IsDistorted);
		AssertCorners(distorted, Vector2.Zero, new Vector2(1.2f, 0.3f), Vector2.One, new Vector2(0f, 1f));
		Assert.AreEqual(TransformBoxRect.Unit.Min, distorted.Min, "The rectangle the distortion started from was lost.");
	}

	[TestMethod]
	public void Distort_StartsFromASkewedBoxsCorners()
	{
		TransformBoxRect skewed = TransformBoxRect.Unit with { Skew = new Vector2(-0.2f, 0f) };
		Vector2[] before = skewed.Corners();

		TransformBoxRect distorted = Distort(skewed, TransformBoxHandle.BottomLeft, new Vector2(-0.1f, 0f));

		AssertCorners(distorted, before[0], before[1], before[2], before[3] + new Vector2(-0.1f, 0f));
	}

	[TestMethod]
	public void Distort_StopsShortOfADent()
	{
		// Pulling the top right corner past the diagonal from top left to bottom right would dent the
		// quadrilateral; the corner follows the pointer as far as the box stays convex and stops.
		TransformBoxRect distorted = Distort(TransformBoxRect.Unit, TransformBoxHandle.TopRight, new Vector2(-1f, 1f));
		Vector2 corner = distorted.Corners()[1];

		Assert.IsTrue(corner.X is > 0.45f and < 0.55f, $"The corner stopped at {corner}.");
		Assert.AreEqual(1f - corner.X, corner.Y, 1e-3f, "The corner left the line the pointer travelled.");
	}

	[TestMethod]
	public void Distort_OnlyACornerDistorts()
	{
		TransformBoxState state = new();

		Assert.IsTrue(state.Begin(TransformBoxHandle.Top, TransformBoxRect.Unit, Stretched, new Vector2(200, 100), distort: true));
		Assert.IsFalse(state.Distorting, "An edge has no one corner to move.");

		Assert.IsTrue(state.Begin(TransformBoxHandle.BottomLeft, TransformBoxRect.Unit, Stretched, new Vector2(100, 200), distort: true));
		Assert.IsTrue(state.Drag(new Vector2(90, 210), uniform: false).Distort);

		state.End();
		Assert.IsFalse(state.Distorting);
	}

	[TestMethod]
	public void Resize_ADistortedBoxMovesCornersRatherThanResizing()
	{
		TransformBoxRect distorted = Distort(TransformBoxRect.Unit, TransformBoxHandle.TopRight, new Vector2(0.2f, 0f));

		// An edge carries its two corners, a plain corner drag moves the corner alone, and the body all four.
		TransformBoxRect edge = TransformBoxState.Resize(new TransformBoxDrag(TransformBoxHandle.Bottom, distorted, Vector2.Zero, new Vector2(0f, 0.5f), false, Stretched), 4f);
		AssertCorners(edge, Vector2.Zero, new Vector2(1.2f, 0f), new Vector2(1f, 1.5f), new Vector2(0f, 1.5f));

		TransformBoxRect corner = TransformBoxState.Resize(new TransformBoxDrag(TransformBoxHandle.BottomLeft, distorted, Vector2.Zero, new Vector2(0.1f, 0f), true, Stretched), 4f);
		AssertCorners(corner, Vector2.Zero, new Vector2(1.2f, 0f), Vector2.One, new Vector2(0.1f, 1f));

		TransformBoxRect body = TransformBoxState.Resize(new TransformBoxDrag(TransformBoxHandle.Body, distorted, Vector2.Zero, new Vector2(0.5f, 0.5f), false, Stretched), 4f);
		AssertCorners(body, new Vector2(0.5f), new Vector2(1.7f, 0.5f), new Vector2(1.5f), new Vector2(0.5f, 1.5f));
	}

	[TestMethod]
	public void Skew_ADistortedBoxsEdgeMovesInstead()
	{
		TransformBoxRect distorted = Distort(TransformBoxRect.Unit, TransformBoxHandle.TopRight, new Vector2(0.2f, 0f));
		TransformBoxState state = new();

		Assert.IsTrue(state.Begin(TransformBoxHandle.Top, distorted, Stretched, new Vector2(200, 100), skew: true));
		Assert.IsFalse(state.Skewing);
	}

	[TestMethod]
	public void HitTest_FollowsADistortedBox()
	{
		// The top right corner pulled in to (0.5, 0.5): the frame's (0.8, 0.2) is inside the unit square
		// and outside the quadrilateral, and the corner is found where it now is.
		TransformBoxRect distorted = TransformBoxRect.Unit with { Quad = new TransformBoxQuad(Vector2.Zero, new Vector2(0.6f, 0.4f), Vector2.One, new Vector2(0f, 1f)) };

		Assert.AreEqual(TransformBoxHandle.None, TransformBoxState.HitTest(distorted, Stretched, new Vector2(260, 120), true));
		Assert.AreEqual(TransformBoxHandle.Body, TransformBoxState.HitTest(distorted, Stretched, new Vector2(200, 170), true));
		Assert.AreEqual(TransformBoxHandle.TopRight, TransformBoxState.HitTest(distorted, Stretched, new Vector2(221, 141), true));
	}

	private static TransformBoxRect Distort(TransformBoxRect from, TransformBoxHandle handle, Vector2 delta) =>
		TransformBoxState.Resize(new TransformBoxDrag(handle, from, Vector2.Zero, delta, false, Stretched) { Distort = true }, 4f);

	private static TransformBoxRect Skew(TransformBoxRect from, TransformBoxHandle handle, Vector2 delta) =>
		TransformBoxState.Resize(new TransformBoxDrag(handle, from, Vector2.Zero, delta, false, Stretched) { Skew = true }, 4f);

	private static void AssertCorners(TransformBoxRect rect, Vector2 topLeft, Vector2 topRight, Vector2 bottomRight, Vector2 bottomLeft)
	{
		Vector2[] corners = rect.Corners();
		AssertVector(topLeft, corners[0]);
		AssertVector(topRight, corners[1]);
		AssertVector(bottomRight, corners[2]);
		AssertVector(bottomLeft, corners[3]);
	}

	private static TransformBoxHandle Hit(Vector2 screenPoint) =>
		TransformBoxState.HitTest(TransformBoxRect.Unit, Stretched, screenPoint, true);

	private static TransformBoxRect Resize(TransformBoxHandle handle, Vector2 delta, bool uniform = false) =>
		TransformBoxState.Resize(new TransformBoxDrag(handle, TransformBoxRect.Unit, Vector2.Zero, delta, uniform, Stretched), 4f);

	private static void AssertVector(Vector2 expected, Vector2 actual)
	{
		Assert.AreEqual(expected.X, actual.X, Tolerance, $"X of {actual}");
		Assert.AreEqual(expected.Y, actual.Y, Tolerance, $"Y of {actual}");
	}
}
