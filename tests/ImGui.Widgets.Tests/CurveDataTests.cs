// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the managed wrapper over Hexa's Curve struct. All pure — no ImGui context required.
/// </summary>
[TestClass]
public sealed class CurveDataTests
{
	[TestMethod]
	public void NewCurve_IsEmpty()
	{
		CurveData curve = new();

		Assert.AreEqual(0, curve.PointCount);
	}

	[TestMethod]
	public void AddPoint_IncreasesCountAndRoundTrips()
	{
		CurveData curve = new();
		CurveKnot knot = new(new Vector2(0.25f, 0.75f), CurvePointKind.Corner);

		curve.AddPoint(knot);

		Assert.AreEqual(1, curve.PointCount);
		Assert.AreEqual(knot, curve.GetPoint(0));
	}

	[TestMethod]
	public void AddPoint_PreservesPointKind()
	{
		// Bare Vector2 points would silently discard this; corner-vs-smooth is real authoring data.
		CurveData curve = new();
		curve.AddPoint(new CurveKnot(Vector2.Zero, CurvePointKind.Corner));
		curve.AddPoint(new CurveKnot(Vector2.One, CurvePointKind.Smooth));

		Assert.AreEqual(CurvePointKind.Corner, curve.GetPoint(0).Kind);
		Assert.AreEqual(CurvePointKind.Smooth, curve.GetPoint(1).Kind);
	}

	[TestMethod]
	public void SetPoint_ReplacesInPlace()
	{
		CurveData curve = new();
		curve.AddPoint(new CurveKnot(Vector2.Zero, CurvePointKind.Smooth));

		curve.SetPoint(0, new CurveKnot(new Vector2(1f, 2f), CurvePointKind.Corner));

		Assert.AreEqual(1, curve.PointCount);
		Assert.AreEqual(new CurveKnot(new Vector2(1f, 2f), CurvePointKind.Corner), curve.GetPoint(0));
	}

	[TestMethod]
	public void RemovePoint_DropsOnlyThatPoint()
	{
		CurveData curve = new();
		curve.AddPoint(new CurveKnot(new Vector2(0f, 0f), CurvePointKind.Smooth));
		curve.AddPoint(new CurveKnot(new Vector2(1f, 1f), CurvePointKind.Smooth));

		curve.RemovePoint(0);

		Assert.AreEqual(1, curve.PointCount);
		Assert.AreEqual(new Vector2(1f, 1f), curve.GetPoint(0).Position);
	}

	[TestMethod]
	public void Clear_EmptiesTheCurve()
	{
		CurveData curve = new();
		curve.AddPoint(new CurveKnot(Vector2.Zero, CurvePointKind.Smooth));

		curve.Clear();

		Assert.AreEqual(0, curve.PointCount);
	}

	[TestMethod]
	public void Shape_RoundTrips()
	{
		CurveData curve = new()
		{
			Shape = CurveShape.Freehand,
		};

		Assert.AreEqual(CurveShape.Freehand, curve.Shape);
	}

	[TestMethod]
	public void GetPoint_OutOfRange_Throws()
	{
		CurveData curve = new();

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => curve.GetPoint(0));
	}

	[TestMethod]
	public void ConstructedFromPoints_KeepsThemInOrder()
	{
		CurveData curve = new(
		[
			new CurveKnot(new Vector2(0f, 0f), CurvePointKind.Smooth),
			new CurveKnot(new Vector2(1f, 1f), CurvePointKind.Corner),
		]);

		Assert.AreEqual(2, curve.PointCount);
		Assert.AreEqual(CurvePointKind.Corner, curve.GetPoint(1).Kind);
	}

	[TestMethod]
	public void Sample_AfterEdit_ReflectsTheEdit()
	{
		// Upstream's Samples cache goes stale on edit and expects a manual Compute(). This pins
		// that the wrapper recomputes, rather than returning a stale sample.
		CurveData curve = new();
		curve.AddPoint(new CurveKnot(new Vector2(0f, 0f), CurvePointKind.Smooth));
		curve.AddPoint(new CurveKnot(new Vector2(1f, 1f), CurvePointKind.Smooth));
		float before = curve.Sample(0.5f);

		curve.SetPoint(1, new CurveKnot(new Vector2(1f, 10f), CurvePointKind.Smooth));
		float after = curve.Sample(0.5f);

		Assert.AreNotEqual(before, after);
	}
}
