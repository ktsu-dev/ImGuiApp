// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>The lines each crop guide draws, tested without a graphics context because none of it needs one.</summary>
[TestClass]
public sealed class CropGuidesTests
{
	private static readonly Vector2 Landscape = new(600f, 400f);

	private static IEnumerable<Vector2> PointsOf(IReadOnlyList<Vector2[]> guides) => guides.SelectMany(static line => line);

	private static void AssertInside(IReadOnlyList<Vector2[]> guides, Vector2 size, string because)
	{
		foreach (Vector2 point in PointsOf(guides))
		{
			Assert.IsTrue(point.X is >= -0.01f && point.X <= size.X + 0.01f, $"x {point.X} is outside the window: {because}");
			Assert.IsTrue(point.Y is >= -0.01f && point.Y <= size.Y + 0.01f, $"y {point.Y} is outside the window: {because}");
		}
	}

	[TestMethod]
	public void Thirds_AreTwoLinesEachWayAThirdIn()
	{
		IReadOnlyList<Vector2[]> guides = CropGuides.For(CropGuide.Thirds, 0, Landscape);

		Assert.AreEqual(4, guides.Count);
		float[] verticals = [.. guides.Where(static line => line[0].X == line[1].X).Select(static line => line[0].X).Order()];
		Assert.AreEqual(2, verticals.Length);
		Assert.AreEqual(200f, verticals[0], 0.01f);
		Assert.AreEqual(400f, verticals[1], 0.01f);
	}

	[TestMethod]
	public void TheGoldenRatio_SitsAtTheGoldenSectionFromEachEdge()
	{
		IReadOnlyList<Vector2[]> guides = CropGuides.For(CropGuide.GoldenRatio, 0, Landscape);

		float[] verticals = [.. guides.Where(static line => line[0].X == line[1].X).Select(static line => line[0].X).Order()];
		Assert.AreEqual(2, verticals.Length);

		// The larger part over the whole is the smaller over the larger, which is what phi is.
		Assert.AreEqual(600f / CropGuides.Phi, verticals[1], 0.01f);
		Assert.AreEqual(600f - (600f / CropGuides.Phi), verticals[0], 0.01f);
	}

	[TestMethod]
	public void Diagonals_RunAtFortyFiveDegreesFromEveryCorner()
	{
		IReadOnlyList<Vector2[]> guides = CropGuides.For(CropGuide.Diagonals, 0, Landscape);

		Assert.AreEqual(4, guides.Count);
		foreach (Vector2[] line in guides)
		{
			Vector2 run = line[1] - line[0];
			Assert.AreEqual(MathF.Abs(run.X), MathF.Abs(run.Y), 0.01f, "a diagonal is not at 45 degrees");
		}

		AssertInside(guides, Landscape, "diagonals");
	}

	[TestMethod]
	public void Triangles_MeetTheDiagonalSquareOn()
	{
		IReadOnlyList<Vector2[]> guides = CropGuides.For(CropGuide.Triangles, 0, Landscape);

		Assert.AreEqual(3, guides.Count);
		Vector2 diagonal = guides[0][1] - guides[0][0];

		foreach (Vector2[] line in guides.Skip(1))
		{
			Assert.AreEqual(0f, Vector2.Dot(diagonal, line[1] - line[0]), 0.5f, "a triangle's line is not square to the diagonal");
		}
	}

	[TestMethod]
	public void FlippingTheTriangles_MirrorsThemAcross()
	{
		IReadOnlyList<Vector2[]> plain = CropGuides.For(CropGuide.Triangles, 0, Landscape);
		IReadOnlyList<Vector2[]> flipped = CropGuides.For(CropGuide.Triangles, CropGuides.NextOrientation(0), Landscape);

		Assert.AreEqual(new Vector2(0f, 0f), plain[0][0]);
		Assert.AreEqual(new Vector2(600f, 0f), flipped[0][0], "the flipped diagonal does not start from the other top corner");
	}

	[TestMethod]
	public void FlippingASymmetricGuide_ChangesNothing()
	{
		IReadOnlyList<Vector2[]> plain = CropGuides.For(CropGuide.Diagonals, 0, Landscape);
		IReadOnlyList<Vector2[]> flipped = CropGuides.For(CropGuide.Diagonals, 3, Landscape);

		CollectionAssert.AreEqual(PointsOf(plain).ToArray(), PointsOf(flipped).ToArray());
	}

	[TestMethod]
	public void TheSpiral_IsOneUnbrokenCurveInsideTheWindow()
	{
		IReadOnlyList<Vector2[]> guides = CropGuides.For(CropGuide.GoldenSpiral, 0, Landscape);
		Vector2[] spiral = guides[0];

		Assert.IsTrue(guides.Count > 4, "the spiral is drawn without the sections it turns at");
		Assert.AreEqual(new Vector2(0f, 400f), spiral[0], "the spiral does not start from the bottom-left corner");

		// Consecutive points are close together: an arc that started away from where the last ended
		// would show as a jump far longer than one segment.
		for (int point = 1; point < spiral.Length; point++)
		{
			Assert.IsTrue(Vector2.Distance(spiral[point - 1], spiral[point]) < 30f, $"the spiral jumps at point {point}");
		}

		AssertInside(guides, Landscape, "the spiral");
	}

	[TestMethod]
	public void TheSpiral_ClosesWhereItsSectionsConverge()
	{
		Vector2[] spiral = CropGuides.For(CropGuide.GoldenSpiral, 0, Landscape)[0];
		Vector2 eye = spiral[^1];

		// Each quarter turn keeps 1/phi^2 of one side, alternating, so the sections close on the
		// point phi^2 / (1 + phi^2) of the way across each axis: the meeting of the window's diagonal
		// with the diagonal of the first section's remainder.
		float phiSquared = CropGuides.Phi * CropGuides.Phi;
		float fraction = phiSquared / (1f + phiSquared);
		Assert.AreEqual(600f * fraction, eye.X, 5f);
		Assert.AreEqual(400f * fraction, eye.Y, 5f);
	}

	[TestMethod]
	public void EachOrientationOfTheSpiral_ClosesOnADifferentSection()
	{
		HashSet<(int, int)> eyes = [];

		for (int orientation = 0; orientation < CropGuides.Orientations; orientation++)
		{
			Vector2 eye = CropGuides.For(CropGuide.GoldenSpiral, orientation, Landscape)[0][^1];
			eyes.Add(((int)MathF.Round(eye.X / 100f), (int)MathF.Round(eye.Y / 100f)));
		}

		Assert.AreEqual(CropGuides.Orientations, eyes.Count);
	}

	[TestMethod]
	public void APortraitSpiral_CutsItsFirstSectionAcrossTheLongerSide()
	{
		Vector2 portrait = new(400f, 600f);
		IReadOnlyList<Vector2[]> guides = CropGuides.For(CropGuide.GoldenSpiral, 0, portrait);
		Vector2[] firstSection = guides[1];

		Assert.AreEqual(firstSection[0].Y, firstSection[1].Y, 0.01f, "the first section of a portrait spiral is not horizontal");
		Assert.AreEqual(600f / CropGuides.Phi, firstSection[0].Y, 0.01f);
		AssertInside(guides, portrait, "a portrait spiral");
	}

	[TestMethod]
	public void TheGrid_HasSquareCells()
	{
		IReadOnlyList<Vector2[]> guides = CropGuides.For(CropGuide.Grid, 0, Landscape);

		float[] verticals = [.. guides.Where(static line => line[0].X == line[1].X).Select(static line => line[0].X).Order()];
		float[] horizontals = [.. guides.Where(static line => line[0].Y == line[1].Y).Select(static line => line[0].Y).Order()];

		Assert.AreEqual(verticals[1] - verticals[0], horizontals[1] - horizontals[0], 0.01f);
		Assert.IsTrue(verticals[^1] < 600f && horizontals[^1] < 400f, "a grid line is drawn over the window's edge");
	}

	[TestMethod]
	public void AnEmptyWindow_DrawsNothing()
	{
		foreach (CropGuide overlay in Enum.GetValues<CropGuide>())
		{
			Assert.AreEqual(0, CropGuides.For(overlay, 0, new Vector2(0f, 300f)).Count, overlay.ToString());
		}
	}

	[TestMethod]
	public void NextCyclesEveryGuideAndComesBackRound()
	{
		CropGuide overlay = CropGuide.Thirds;
		HashSet<CropGuide> seen = [];

		do
		{
			seen.Add(overlay);
			overlay = CropGuides.Next(overlay);
		}
		while (overlay != CropGuide.Thirds);

		Assert.AreEqual(Enum.GetValues<CropGuide>().Length, seen.Count);
	}
}
