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
