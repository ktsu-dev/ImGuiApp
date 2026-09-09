// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ForceDirectedLayout.Tests;

using System;
using ktsu.ForceDirectedLayout;
using ktsu.ForceDirectedLayout.Tests.Bench;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests that a settled graph sits where a reader is looking: centred on the world origin.
/// </summary>
/// <remarks>
/// This is a placement property rather than a shape one, and nothing else in the suite covers it —
/// every other test asks what the arrangement looks like, not where it ended up. It went unnoticed
/// for exactly that reason: a graph can settle into a perfectly good shape a couple of hundred units
/// off to one side, and every metric reads well while a user watches their document sit against the
/// edge of the canvas.
/// </remarks>
[TestClass]
public class RecentringTests
{
	/// <summary>Frames to settle for; the corpus settles well inside this.</summary>
	private const int SettleFrames = 4000;

	/// <summary>The centre of the box a renderer would draw around every body.</summary>
	/// <param name="core">The settled layout.</param>
	private static Vec2D DrawnCentre(LayoutCore core)
	{
		double minX = double.MaxValue;
		double minY = double.MaxValue;
		double maxX = double.MinValue;
		double maxY = double.MinValue;

		for (int i = 0; i < core.BodyCount; i++)
		{
			BodyState body = core.Bodies[i];
			minX = Math.Min(minX, body.Position.X);
			minY = Math.Min(minY, body.Position.Y);
			maxX = Math.Max(maxX, body.Position.X + body.Dimensions.X);
			maxY = Math.Max(maxY, body.Position.Y + body.Dimensions.Y);
		}

		return new Vec2D((minX + maxX) * 0.5, (minY + maxY) * 0.5);
	}

	/// <summary>Settles a corpus graph from one starting arrangement.</summary>
	/// <param name="graph">The graph to settle.</param>
	/// <param name="seed">Which arrangement to start from.</param>
	private static LayoutCore Settle(BenchGraph graph, int seed)
	{
		LayoutCore core = graph.Start(LayoutSettings.Defaults, seed, 0.5);
		for (int frame = 0; frame < SettleFrames; frame++)
		{
			core.Step(1.0 / 60.0);
		}

		return core;
	}

	[TestMethod]
	public void EveryCorpusGraph_SettlesCentredOnTheOrigin()
	{
		foreach (BenchGraph graph in GraphCorpus.All)
		{
			Vec2D centre = DrawnCentre(Settle(graph, 7919 * 3));

			// Generous against the drawn size rather than tight against zero: the recentring force is
			// balanced against everything else rather than applied as a correction, so it comes to rest
			// near the origin instead of exactly on it.
			Assert.IsLessThan(60.0, Math.Abs(centre.X), $"{graph.Name} settled {centre.X:F0} off the origin horizontally");
			Assert.IsLessThan(60.0, Math.Abs(centre.Y), $"{graph.Name} settled {centre.Y:F0} off the origin vertically");
		}
	}

	/// <summary>
	/// Tests that a graph pushed off the origin comes back to the same place from either direction.
	/// </summary>
	/// <remarks>
	/// The regression this exists for. Gravity alone counts bodies rather than measuring them, so its
	/// net is a step function of position and is exactly zero anywhere the counts balance — a band, not
	/// a point. A chain settled 79 units to one side and stayed there, and shoved 600 the other way came
	/// to rest 79 units to the other side: the same distance out, on whichever side it arrived from.
	/// Asserting the two agree is what catches a return to that, where asserting either one alone would
	/// not.
	/// </remarks>
	[TestMethod]
	public void AGraphPushedAside_ComesBackToTheSamePlaceFromEitherSide()
	{
		static Vec2D Resettle(double shove)
		{
			LayoutCore core = Settle(GraphCorpus.Chain, 7919 * 3);

			for (int i = 0; i < core.BodyCount; i++)
			{
				core.Bodies[i].Position += new Vec2D(shove, 0);
				core.Bodies[i].Velocity = Vec2D.Zero;
			}

			for (int frame = 0; frame < SettleFrames; frame++)
			{
				core.Step(1.0 / 60.0);
			}

			return DrawnCentre(core);
		}

		Vec2D fromTheRight = Resettle(600.0);
		Vec2D fromTheLeft = Resettle(-600.0);

		Assert.AreEqual(fromTheLeft.X, fromTheRight.X, 20.0,
			$"pushed either way it should come back to the same place; it settled {fromTheRight.X:F0} from one side and {fromTheLeft.X:F0} from the other");
		Assert.IsLessThan(60.0, Math.Abs(fromTheRight.X), $"and near the origin; it was {fromTheRight.X:F0}");
	}

	/// <summary>
	/// Tests that the whole simulation is translation-equivariant: move where a graph starts and where
	/// it is anchored by the same amount, and it settles into the same arrangement, moved by that much.
	/// </summary>
	/// <remarks>
	/// This is the property that lets the recentring pass exist. It slides every body by the same
	/// vector, so it cannot change any distance between them — but that is only true if sliding a graph
	/// is something the rest of the simulation is indifferent to, which is what this asserts.
	/// <para>
	/// It matters because the obvious alternative does not have it. Gravity made proportional to
	/// distance would centre a graph too, and would pull a distant body harder than a near one, so a
	/// pair of wide nodes ends up squeezed closer together than a pair of narrow ones: measured, a
	/// 400-wide pair settled 160 apart against a 60-wide pair's 224. Spacing that depends on how big a
	/// node is drawn is the thing measuring repulsion across clear space was for.
	/// </para>
	/// </remarks>
	[TestMethod]
	public void SlidingAGraphAndItsAnchor_SettlesIntoTheSameArrangement()
	{
		Vec2D delta = new(5000, -3000);

		static LayoutCore SettleAt(Vec2D origin, Vec2D offset)
		{
			LayoutCore core = GraphCorpus.Counter.Start(LayoutSettings.Defaults, 7919 * 3, 0.5);
			core.WorldOrigin = origin;
			for (int i = 0; i < core.BodyCount; i++)
			{
				core.Bodies[i].Position += offset;
			}

			for (int frame = 0; frame < SettleFrames; frame++)
			{
				core.Step(1.0 / 60.0);
			}

			return core;
		}

		LayoutCore here = SettleAt(Vec2D.Zero, Vec2D.Zero);
		LayoutCore there = SettleAt(delta, delta);

		for (int i = 0; i < here.BodyCount; i++)
		{
			for (int j = i + 1; j < here.BodyCount; j++)
			{
				double a = (here.Bodies[i].Position - here.Bodies[j].Position).Length();
				double b = (there.Bodies[i].Position - there.Bodies[j].Position).Length();

				Assert.AreEqual(a, b, 1.0,
					$"bodies {i} and {j} sat {a:F1} apart at the origin and {b:F1} apart {delta.X:F0} away");
			}
		}

		Assert.AreEqual(delta.X, DrawnCentre(there).X - DrawnCentre(here).X, 20.0,
			"and the second should have settled a whole delta away from the first");
	}
}
