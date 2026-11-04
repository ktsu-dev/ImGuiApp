// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests how the single-curve editor maps values onto its frame and what picking, inserting and
/// dragging a point do.
/// </summary>
[TestClass]
public sealed class CurveFieldGeometryTests
{
	private static readonly CurveFieldGeometry Frame = new(new Vector2(100f, 50f), new Vector2(200f, 100f), new Vector2(-1f, 0f), new Vector2(1f, 10f));

	private static CurveData ThreePoints() =>
		new(
		[
			new CurveKnot(new Vector2(-1f, 0f), CurvePointKind.Smooth),
			new CurveKnot(new Vector2(0f, 5f), CurvePointKind.Smooth),
			new CurveKnot(new Vector2(1f, 10f), CurvePointKind.Corner),
		]);

	[TestMethod]
	public void ToScreen_MapsTheRangeOntoTheFrame_WithLargerValuesHigher()
	{
		Assert.AreEqual(new Vector2(100f, 150f), Frame.ToScreen(new Vector2(-1f, 0f)));
		Assert.AreEqual(new Vector2(300f, 50f), Frame.ToScreen(new Vector2(1f, 10f)));
		Assert.AreEqual(new Vector2(200f, 100f), Frame.ToScreen(new Vector2(0f, 5f)));
	}

	[TestMethod]
	public void ToValue_InvertsToScreen()
	{
		Vector2 value = new(0.5f, 2.5f);

		Vector2 roundTrip = Frame.ToValue(Frame.ToScreen(value));

		Assert.AreEqual(value.X, roundTrip.X, 1e-5f);
		Assert.AreEqual(value.Y, roundTrip.Y, 1e-5f);
	}

	[TestMethod]
	public void ToValue_OutsideTheFrame_ClampsToTheRange() =>
		Assert.AreEqual(new Vector2(1f, 0f), Frame.ToValue(new Vector2(900f, 900f)));

	[TestMethod]
	public void HitTest_FindsThePointUnderThePointer_AndNothingElsewhere()
	{
		CurveData curve = ThreePoints();

		Assert.AreEqual(1, Frame.HitTest(curve, new Vector2(203f, 98f), 6f));
		Assert.AreEqual(-1, Frame.HitTest(curve, new Vector2(250f, 120f), 6f));
	}

	[TestMethod]
	public void InsertionIndex_KeepsThePointsInOrder()
	{
		CurveData curve = ThreePoints();

		Assert.AreEqual(0, CurveFieldGeometry.InsertionIndex(curve, -2f));
		Assert.AreEqual(2, CurveFieldGeometry.InsertionIndex(curve, 0.5f));
		Assert.AreEqual(3, CurveFieldGeometry.InsertionIndex(curve, 2f));
		Assert.AreEqual(0, CurveFieldGeometry.InsertionIndex(new CurveData(), 0f));
	}

	[TestMethod]
	public void ConstrainDrag_StopsAtTheNeighboursX()
	{
		CurveData curve = ThreePoints();

		Assert.AreEqual(-1f, Frame.ConstrainDrag(curve, 1, new Vector2(-5f, 5f)).X);
		Assert.AreEqual(1f, Frame.ConstrainDrag(curve, 1, new Vector2(5f, 5f)).X);
		Assert.AreEqual(0.25f, Frame.ConstrainDrag(curve, 1, new Vector2(0.25f, 5f)).X);
	}

	[TestMethod]
	public void ConstrainDrag_KeepsYInsideTheRange()
	{
		CurveData curve = ThreePoints();

		Assert.AreEqual(10f, Frame.ConstrainDrag(curve, 1, new Vector2(0f, 50f)).Y);
		Assert.AreEqual(0f, Frame.ConstrainDrag(curve, 1, new Vector2(0f, -50f)).Y);
	}

	[TestMethod]
	public void InsertPoint_PutsThePointWhereItIsAsked()
	{
		CurveData curve = ThreePoints();

		curve.InsertPoint(2, new CurveKnot(new Vector2(0.5f, 7f), CurvePointKind.Smooth));

		Assert.AreEqual(4, curve.PointCount);
		Assert.AreEqual(new Vector2(0.5f, 7f), curve.GetPoint(2).Position);
		Assert.AreEqual(new Vector2(1f, 10f), curve.GetPoint(3).Position);
	}
}
