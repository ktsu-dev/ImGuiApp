// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ForceDirectedLayout.Tests;

using System;
using ktsu.ForceDirectedLayout;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the pairwise repulsion force, which is measured across the clear space between two bodies'
/// bounding boxes rather than between their centres.
/// </summary>
/// <remarks>
/// A centre measurement counts each body's own extent as part of the distance between them, so what
/// it calls a comfortable distance depends on how big the pair happens to be: press a node against
/// the side of a wide one and the two centres are still far apart, so nothing pushes back. In a node
/// editor, where a literal is a fraction of the size of the class it belongs to, that is the
/// difference between a graph that breathes evenly and one whose big nodes are crowded and whose
/// small ones are marooned.
/// <para>
/// These measure the force directly, one substep in with every other force switched off, so that what
/// is asserted is the law itself rather than the arrangement some graph happens to settle into.
/// </para>
/// </remarks>
[TestClass]
public class RepulsionTests
{
	/// <summary>Everything off but repulsion, so a body's force after a step is repulsion alone.</summary>
	private static LayoutSettings RepulsionOnly()
	{
		LayoutSettings s = LayoutSettings.Defaults;
		s.Enabled = 1;
		s.GravityStrength = 0;
		s.DirectionalBias = 0;
		s.LinkFlatteningStrength = 0;
		s.LinkUntwistStrength = 0;
		s.OverlapMargin = 0;
		return s;
	}

	/// <summary>
	/// The repulsion on the first of two bodies placed at a given offset between their centres.
	/// </summary>
	/// <param name="firstSize">Dimensions of the body the force is read from.</param>
	/// <param name="secondSize">Dimensions of the other body.</param>
	/// <param name="betweenCentres">Offset from the first body's centre to the second's.</param>
	private static Vec2D RepulsionOn(Vec2D firstSize, Vec2D secondSize, Vec2D betweenCentres)
	{
		LayoutCore core = new() { Settings = RepulsionOnly() };
		core.ResizeBodies(2);
		core.Bodies[0] = new BodyState { Id = 1, Position = Vec2D.Zero, Dimensions = firstSize };
		core.Bodies[1] = new BodyState
		{
			Id = 2,
			Position = betweenCentres + ((firstSize - secondSize) * 0.5),
			Dimensions = secondSize,
		};

		// One substep exactly, so nothing has moved between the force being computed and being read.
		core.Step(1.0 / LayoutSettings.Defaults.TargetPhysicsHz);
		return core.Bodies[0].Force;
	}

	/// <summary>
	/// Tests the property the whole measurement exists for: two pairs with the same clear space between
	/// them are pushed apart equally hard, however big the bodies themselves are.
	/// </summary>
	[TestMethod]
	public void Repulsion_AtEqualClearSpace_IsTheSameWhateverTheBodiesMeasure()
	{
		// Both pairs have exactly 100 of clear space between their facing edges.
		double small = RepulsionOn(new Vec2D(60, 60), new Vec2D(60, 60), new Vec2D(160, 0)).Length();
		double large = RepulsionOn(new Vec2D(400, 60), new Vec2D(400, 60), new Vec2D(500, 0)).Length();
		double mixed = RepulsionOn(new Vec2D(400, 60), new Vec2D(60, 60), new Vec2D(330, 0)).Length();

		double expected = LayoutSettings.Defaults.RepulsionStrength / (100.0 * 100.0);
		Assert.AreEqual(expected, small, 0.001, $"a small pair 100 apart should be pushed {expected:F1} hard");
		Assert.AreEqual(expected, large, 0.001, $"and so should a wide pair 100 apart; it was {large:F1}");
		Assert.AreEqual(expected, mixed, 0.001, $"and so should one of each; it was {mixed:F1}");
	}

	/// <summary>
	/// Tests the converse, which is what a centre measurement got wrong: at the same centre distance,
	/// the pair with less room between them is pushed apart harder.
	/// </summary>
	/// <remarks>
	/// Measured between centres these two are identical, so the wide pair - whose facing edges are 100
	/// apart against the small pair's 440 - felt no more push than bodies with four times the room.
	/// </remarks>
	[TestMethod]
	public void Repulsion_AtEqualCentreDistance_IsHarderBetweenTheBiggerBodies()
	{
		double small = RepulsionOn(new Vec2D(60, 60), new Vec2D(60, 60), new Vec2D(500, 0)).Length();
		double large = RepulsionOn(new Vec2D(400, 60), new Vec2D(400, 60), new Vec2D(500, 0)).Length();

		Assert.IsTrue(large > small * 10.0,
			$"The pair with a tenth of the room should be pushed far harder; {large:F1} against {small:F1}.");
	}

	/// <summary>
	/// Tests that the axes a pair overlaps on drop out of the measurement, so two bodies sharing a row
	/// are as far apart as their horizontal gap says however their rows are offset.
	/// </summary>
	[TestMethod]
	public void Repulsion_BetweenBodiesSharingARow_MeasuresTheHorizontalGapAlone()
	{
		Vec2D size = new(100, 100);
		double level = RepulsionOn(size, size, new Vec2D(300, 0)).Length();
		double offset = RepulsionOn(size, size, new Vec2D(300, 40)).Length();

		// Their centres are further apart in the second case, but the clear space between them is not.
		Assert.AreEqual(level, offset, 0.001,
			$"A row offset inside the bodies' own height should not change the push; {offset:F2} against {level:F2}.");
		Assert.AreEqual(LayoutSettings.Defaults.RepulsionStrength / (200.0 * 200.0), level, 0.001,
			"and the distance measured should be the 200 of clear space, not the 300 between centres");
	}

	/// <summary>
	/// Tests that the direction is still the one between the centres, which is what lets repulsion move
	/// a body diagonally out of another's way.
	/// </summary>
	/// <remarks>
	/// Taking the direction from the closest points too would make this force exactly horizontal, since
	/// the pair overlaps vertically and their closest points are level with one another.
	/// </remarks>
	[TestMethod]
	public void Repulsion_PushesAlongTheLineBetweenTheCentres()
	{
		Vec2D size = new(100, 100);
		Vec2D force = RepulsionOn(size, size, new Vec2D(300, 40));

		// Away from the other body: it sits to the lower right, so the push is up and to the left.
		Assert.IsTrue(force.X < 0, $"the push should be away from the other body along X; it was {force.X:F2}");
		Assert.IsTrue(force.Y < 0, $"and should carry the centres' vertical offset too; it was {force.Y:F2}");
		Assert.AreEqual(40.0 / 300.0, force.Y / force.X, 0.001, "in proportion to the offset between the centres");
	}

	/// <summary>
	/// Tests that a pair with no clear space between them is pushed at the floor rather than infinitely
	/// hard, and that the floor is reached at contact rather than at coincidence.
	/// </summary>
	[TestMethod]
	public void Repulsion_WithNoClearSpace_PushesAtTheFloor()
	{
		Vec2D size = new(100, 100);
		double floor = LayoutSettings.Defaults.RepulsionStrength /
			(LayoutSettings.Defaults.MinRepulsionDistance * LayoutSettings.Defaults.MinRepulsionDistance);

		double overlapping = RepulsionOn(size, size, new Vec2D(30, 0)).Length();
		double touching = RepulsionOn(size, size, new Vec2D(100, 0)).Length();
		double atTheFloor = RepulsionOn(size, size, new Vec2D(150, 0)).Length();
		double beyond = RepulsionOn(size, size, new Vec2D(200, 0)).Length();

		Assert.AreEqual(floor, overlapping, 0.001, "overlapping bodies should be pushed at the floor");
		Assert.AreEqual(floor, touching, 0.001, "and so should touching ones");
		Assert.AreEqual(floor, atTheFloor, 0.001, "and so should a pair exactly one floor of clear space apart");
		Assert.IsTrue(beyond < floor, $"beyond it the law takes over again; {beyond:F1} against the floor's {floor:F1}");
	}

	/// <summary>
	/// Tests that a zero <see cref="LayoutSettings.MinRepulsionDistance"/> still yields a finite layout
	/// rather than a NaN one.
	/// </summary>
	/// <remarks>
	/// That setting is itself the clamp keeping the inverse-square law finite where bodies touch, so
	/// setting it to zero removes the only thing preventing a division by zero: touching boxes have
	/// exactly no clear space between them, the force comes back infinite, and the integrator carries
	/// that into positions that are NaN forever after. It surfaced from a parameter sweep that happened
	/// to offer zero, where every measurement of the result read NaN rather than "bad" — which is the
	/// real cost, since a layout that silently stops being a number is worse than a crowded one.
	/// </remarks>
	[TestMethod]
	public void Repulsion_WithNoMinimumDistance_StaysFinite()
	{
		LayoutSettings settings = RepulsionOnly() with { MinRepulsionDistance = 0.0 };

		LayoutCore core = new() { Settings = settings };
		core.ResizeBodies(2);

		// Overlapping, which is where the clear distance between them is exactly zero.
		core.Bodies[0] = new BodyState { Id = 1, Position = Vec2D.Zero, Dimensions = new Vec2D(100, 100) };
		core.Bodies[1] = new BodyState { Id = 2, Position = new Vec2D(30, 0), Dimensions = new Vec2D(100, 100) };

		for (int frame = 0; frame < 60; frame++)
		{
			core.Step(1.0 / 60.0);
		}

		for (int i = 0; i < core.BodyCount; i++)
		{
			Vec2D position = core.Bodies[i].Position;
			Assert.IsTrue(double.IsFinite(position.X) && double.IsFinite(position.Y),
				$"body {i} should still have a real position; it was ({position.X}, {position.Y})");
		}
	}

	/// <summary>
	/// Tests what all of the above is for: two bodies settle with the same room between them whether
	/// they are small or wide, rather than the wide pair ending up crammed together.
	/// </summary>
	/// <remarks>
	/// This is the whole simulation, not one force - gravity is what repulsion comes to rest against
	/// here. Measured between centres, the wide pair settled the same distance apart as the small one,
	/// which put 340 less clear space between their edges: the same setting, and two graphs spaced
	/// entirely differently for no reason but how big their nodes were drawn.
	/// </remarks>
	[TestMethod]
	public void SettledPairs_KeepTheSameClearSpace_WhateverTheirSize()
	{
		static (double Gap, double BetweenCentres) Settle(double width)
		{
			LayoutSettings s = LayoutSettings.Defaults;
			s.Enabled = 1;
			ForceLayout layout = new(s);
			layout.SetNodes(
			[
				new() { Id = 1, Position = new Vec2D(-600, 0), Dimensions = new Vec2D(width, 60) },
				new() { Id = 2, Position = new Vec2D(600, 0), Dimensions = new Vec2D(width, 60) },
			]);

			for (int frame = 0; frame < 1200; frame++)
			{
				layout.Step(1.0 / 60.0);
			}

			Span<NodePosition> positions = stackalloc NodePosition[2];
			layout.GetPositions(positions);

			double betweenCentres = Math.Abs(positions[1].Position.X - positions[0].Position.X);
			return (betweenCentres - width, betweenCentres);
		}

		(double smallGap, double smallCentres) = Settle(60);
		(double wideGap, double wideCentres) = Settle(400);

		Assert.AreEqual(smallGap, wideGap, 2.0,
			$"Both pairs should settle with the same room between them; {smallGap:F0} against {wideGap:F0}.");
		Assert.IsTrue(smallGap > 50.0, $"and it should be real room rather than none; it was {smallGap:F0}");
		Assert.AreEqual(340.0, wideCentres - smallCentres, 4.0,
			"which is the wide pair's centres sitting a body width further apart, where a centre measurement " +
			"would have settled them both at the same centre distance");
	}
}
