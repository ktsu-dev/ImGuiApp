// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ForceDirectedLayout.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using ktsu.ForceDirectedLayout;
using ktsu.ForceDirectedLayout.Tests.Bench;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ForceLayoutTests
{
	private static LayoutSettings EnabledDefaults()
	{
		LayoutSettings s = LayoutSettings.Defaults;
		s.Enabled = 1;
		return s;
	}

	[TestMethod]
	public void NewLayout_HasNoBodiesOrEdges()
	{
		ForceLayout layout = new();
		Assert.AreEqual(0, layout.NodeCount);
		Assert.AreEqual(0, layout.EdgeCount);
		Assert.IsFalse(layout.IsStable);
	}

	[TestMethod]
	public void SetNodes_PopulatesCount_AndIndexLookupResolves()
	{
		ForceLayout layout = new();
		NodeInit[] nodes =
		[
			new() { Id = 10, Position = new Vec2D(0, 0), Dimensions = new Vec2D(100, 50) },
			new() { Id = 20, Position = new Vec2D(300, 0), Dimensions = new Vec2D(100, 50) },
		];
		layout.SetNodes(nodes);

		Assert.AreEqual(2, layout.NodeCount);
		Assert.AreEqual(0, layout.GetIndexOf(10));
		Assert.AreEqual(1, layout.GetIndexOf(20));
		Assert.AreEqual(-1, layout.GetIndexOf(999));
	}

	[TestMethod]
	public void SetEdges_ResolvesEndpointIds_ToIndices()
	{
		ForceLayout layout = new();
		NodeInit[] nodes =
		[
			new() { Id = 1, Dimensions = new Vec2D(50, 50) },
			new() { Id = 2, Position = new Vec2D(200, 0), Dimensions = new Vec2D(50, 50) },
		];
		layout.SetNodes(nodes);
		layout.SetEdges(
		[
			new() { SourceBodyId = 1, TargetBodyId = 2 },
		]);

		Assert.AreEqual(1, layout.EdgeCount);
	}

	[TestMethod]
	public void Step_WithDisabled_IsNoOp()
	{
		ForceLayout layout = new(LayoutSettings.Defaults);
		layout.SetNodes(
		[
			new() { Id = 1, Position = new Vec2D(100, 0), Dimensions = new Vec2D(50, 50) },
			new() { Id = 2, Position = new Vec2D(101, 0), Dimensions = new Vec2D(50, 50) },
		]);

		layout.Step(0.016);

		// Disabled → no force integration → bodies unchanged.
		Span<NodePosition> positions = stackalloc NodePosition[2];
		layout.GetPositions(positions);
		Assert.AreEqual(100.0, positions[0].Position.X, 0.0001);
		Assert.AreEqual(101.0, positions[1].Position.X, 0.0001);
	}

	[TestMethod]
	public void Step_OverlappingBodies_RepelAlongSeparatingAxis()
	{
		ForceLayout layout = new(EnabledDefaults());
		// Two bodies stacked nearly on top of each other on the X axis.
		layout.SetNodes(
		[
			new() { Id = 1, Position = new Vec2D(0, 0), Dimensions = new Vec2D(50, 50) },
			new() { Id = 2, Position = new Vec2D(5, 0), Dimensions = new Vec2D(50, 50) },
		]);

		for (int i = 0; i < 60; i++)
		{
			layout.Step(0.016);
		}

		Span<NodePosition> positions = stackalloc NodePosition[2];
		layout.GetPositions(positions);
		double finalGap = Math.Abs(positions[1].Position.X - positions[0].Position.X);
		Assert.IsTrue(finalGap > 10.0, $"Bodies should have repelled apart; final gap was {finalGap}.");
	}

	[TestMethod]
	public void Step_PinnedBody_DoesNotMove()
	{
		LayoutSettings s = EnabledDefaults();
		s.GravityStrength = 1000.0; // crank gravity so unpinned bodies definitely move
		ForceLayout layout = new(s);
		layout.SetNodes(
		[
			new() { Id = 1, Position = new Vec2D(0, 0), Dimensions = new Vec2D(50, 50), IsPinned = 1 },
			new() { Id = 2, Position = new Vec2D(500, 500), Dimensions = new Vec2D(50, 50) },
		]);

		for (int i = 0; i < 30; i++)
		{
			layout.Step(0.016);
		}

		Span<NodePosition> positions = stackalloc NodePosition[2];
		layout.GetPositions(positions);
		Assert.AreEqual(0.0, positions[0].Position.X, 0.0001, "Pinned body must not move.");
		Assert.AreEqual(0.0, positions[0].Position.Y, 0.0001, "Pinned body must not move.");
	}

	[TestMethod]
	public void Solve_ReachesStability_WithinIterationCap()
	{
		ForceLayout layout = new(EnabledDefaults());
		layout.SetNodes(
		[
			new() { Id = 1, Position = new Vec2D(0, 0), Dimensions = new Vec2D(50, 50) },
			new() { Id = 2, Position = new Vec2D(225, 0), Dimensions = new Vec2D(50, 50) },
		]);
		layout.SetEdges(
		[
			new() { SourceBodyId = 1, TargetBodyId = 2 },
		]);
		layout.InitializeWorldOriginToCentroid();

		int iterations = layout.Solve(maxIterations: 2000, tolerance: 0.5);
		Assert.IsTrue(iterations > 0, "Solve must run at least one iteration.");
		Assert.IsTrue(layout.IsStable, $"System should have stabilised; final energy {layout.TotalSystemEnergy}.");
	}

	[TestMethod]
	public void GetPositions_ThrowsOnUndersizedBuffer()
	{
		ForceLayout layout = new();
		layout.SetNodes(
		[
			new() { Id = 1, Dimensions = new Vec2D(50, 50) },
			new() { Id = 2, Dimensions = new Vec2D(50, 50) },
		]);

		Assert.ThrowsExactly<ArgumentException>(() =>
		{
			NodePosition[] tooSmall = new NodePosition[1];
			layout.GetPositions(tooSmall);
		});
	}

	[TestMethod]
	public void PhysicsSettings_RoundTrips_ThroughLayoutSettings()
	{
		PhysicsSettings p = new()
		{
			Enabled = true,
			RepulsionStrength = 42.0,
			LinkSpringStrength = 0.25,
			DirectionalBias = 0.1,
			LinkFlatteningStrength = 0.7,
			LinkFlatteningMargin = 12.0,
			GravityStrength = 7.0,
			OriginAnchorWeight = 0.3,
			DampingFactor = 0.4,
			MinRepulsionDistance = 60.0,
			RestLinkLength = 180.0,
			MaxForce = 999.0,
			MaxVelocity = 33.0,
			TargetPhysicsHz = 144.0,
			StabilityThreshold = 0.5,
			OverlapMargin = 12.0,
			MaxOverlapCorrection = 25.0,
		};

		LayoutSettings s = p.ToLayoutSettings();
		PhysicsSettings round = PhysicsSettings.FromLayoutSettings(in s);

		Assert.AreEqual(p, round);
	}
}

[TestClass]
public class GenericFacadeTests
{
	private sealed record TestBody(int Id, Vec2D Position, Vec2D Dimensions, Vec2D Velocity, Vec2D Force, bool IsPinned);
	private sealed record TestEdge(int SourceId, int TargetId);

	private static ForceDirectedLayout<TestBody, TestEdge> CreateLayout(PhysicsSettings? settings = null)
	{
		BodyAccessor<TestBody> bodyAccessor = new(
			GetId: b => b.Id,
			GetPosition: b => b.Position,
			GetDimensions: b => b.Dimensions,
			GetVelocity: b => b.Velocity,
			GetForce: b => b.Force,
			GetIsPinned: b => b.IsPinned,
			WithPhysicsState: (b, pos, vel, force) => b with { Position = pos, Velocity = vel, Force = force }
		);
		EdgeAccessor<TestEdge> edgeAccessor = new(
			GetSourceBodyId: e => e.SourceId,
			GetTargetBodyId: e => e.TargetId
		);
		ForceDirectedLayout<TestBody, TestEdge> layout = new(bodyAccessor, edgeAccessor);
		if (settings != null)
		{
			layout.Settings = settings;
		}
		return layout;
	}

	[TestMethod]
	public void GenericFacade_DefaultSettings_MatchPhysicsSettingsDefaults()
	{
		ForceDirectedLayout<TestBody, TestEdge> layout = CreateLayout();
		Assert.AreEqual(new PhysicsSettings(), layout.Settings);
	}

	[TestMethod]
	public void GenericFacade_Step_WithNoNodes_IsNoOp()
	{
		ForceDirectedLayout<TestBody, TestEdge> layout = CreateLayout(new PhysicsSettings { Enabled = true });
		List<TestBody> bodies = [];
		List<TestEdge> edges = [];
		layout.Step(bodies, edges, 0.016);
		Assert.IsFalse(layout.IsStable);
	}

	[TestMethod]
	public void GenericFacade_Step_WithDisabledPhysics_DoesNotMoveNodes()
	{
		ForceDirectedLayout<TestBody, TestEdge> layout = CreateLayout(new PhysicsSettings { Enabled = false });
		List<TestBody> bodies =
		[
			new TestBody(1, new Vec2D(0, 0), new Vec2D(50, 50), Vec2D.Zero, Vec2D.Zero, false),
			new TestBody(2, new Vec2D(300, 0), new Vec2D(50, 50), Vec2D.Zero, Vec2D.Zero, false),
		];
		List<TestEdge> edges = [];

		Vec2D pos1Before = bodies[0].Position;
		Vec2D pos2Before = bodies[1].Position;

		layout.Step(bodies, edges, 0.016);

		Assert.AreEqual(pos1Before, bodies[0].Position);
		Assert.AreEqual(pos2Before, bodies[1].Position);
	}

	[TestMethod]
	public void GenericFacade_Step_WithEnabledPhysics_MovesNodes()
	{
		PhysicsSettings enabled = new()
		{
			Enabled = true,
			RepulsionStrength = 1_200_000.0,
			GravityStrength = 50.0,
		};
		ForceDirectedLayout<TestBody, TestEdge> layout = CreateLayout(enabled);
		List<TestBody> bodies =
		[
			new TestBody(1, new Vec2D(0, 0), new Vec2D(50, 50), Vec2D.Zero, Vec2D.Zero, false),
			new TestBody(2, new Vec2D(10, 0), new Vec2D(50, 50), Vec2D.Zero, Vec2D.Zero, false),
		];
		List<TestEdge> edges = [];

		layout.Step(bodies, edges, 0.016);

		// Repulsion should have moved the nodes apart.
		double gap = Math.Abs(bodies[1].Position.X - bodies[0].Position.X);
		Assert.IsTrue(gap > 10.0, $"Repulsion should push nodes apart; gap was {gap}.");
	}

	[TestMethod]
	public void GenericFacade_Step_WithEdge_PullsNodesCloser()
	{
		PhysicsSettings enabled = new()
		{
			Enabled = true,
			RepulsionStrength = 0,
			GravityStrength = 0,
			LinkSpringStrength = 1.0,
			RestLinkLength = 100.0,
			DampingFactor = 0.1,
		};
		ForceDirectedLayout<TestBody, TestEdge> layout = CreateLayout(enabled);
		List<TestBody> bodies =
		[
			new TestBody(1, new Vec2D(0, 0), new Vec2D(50, 50), Vec2D.Zero, Vec2D.Zero, false),
			new TestBody(2, new Vec2D(500, 0), new Vec2D(50, 50), Vec2D.Zero, Vec2D.Zero, false),
		];
		List<TestEdge> edges = [new TestEdge(1, 2)];

		double initialDistance = Math.Abs(bodies[1].Position.X - bodies[0].Position.X);

		for (int i = 0; i < 30; i++)
		{
			layout.Step(bodies, edges, 0.016);
		}

		double finalDistance = Math.Abs(bodies[1].Position.X - bodies[0].Position.X);
		Assert.IsTrue(finalDistance < initialDistance, $"Spring should pull nodes closer; was {initialDistance}, now {finalDistance}.");
	}

	/// <summary>
	/// Settings that isolate the flattening force: no repulsion, no gravity, no ordering bias, and a
	/// spring only strong enough to hold the pair together.
	/// </summary>
	private static PhysicsSettings FlatteningOnly() => new()
	{
		Enabled = true,
		RepulsionStrength = 0,
		GravityStrength = 0,
		DirectionalBias = 0,
		LinkSpringStrength = 0.1,
		OverlapMargin = 0,
		DampingFactor = 0.1,
	};

	[TestMethod]
	public void LinkFlattening_VerticallyStackedPair_SplaysApartHorizontally()
	{
		ForceDirectedLayout<TestBody, TestEdge> layout = CreateLayout(FlatteningOnly());
		List<TestBody> bodies =
		[
			new TestBody(1, new Vec2D(0, 0), new Vec2D(100, 50), Vec2D.Zero, Vec2D.Zero, false),
			new TestBody(2, new Vec2D(0, 400), new Vec2D(100, 50), Vec2D.Zero, Vec2D.Zero, false),
		];
		List<TestEdge> edges = [new TestEdge(1, 2)];

		// Heavy damping makes this settle slowly, so give it enough simulated time to converge.
		for (int i = 0; i < 3000; i++)
		{
			layout.Step(bodies, edges, 0.016);
		}

		double gap = bodies[1].Position.X - (bodies[0].Position.X + bodies[0].Dimensions.X);
		// Both bodies are the same height, so the centre offsets cancel out of the drop.
		double drop = Math.Abs(bodies[1].Position.Y - bodies[0].Position.Y);
		double required = drop * LayoutCore.BezierClearanceRatio;

		// The force is a soft constraint balancing the link spring, so equilibrium sits just inside the
		// bound rather than exactly on it - the residual violation is what holds the spring off.
		Assert.IsTrue(gap >= required * 0.95, $"Clear span {gap} should reach the bezier bound {required} for a drop of {drop}.");
		Assert.IsTrue(drop < 400.0, $"The pair started 400 apart vertically and should have flattened; drop was {drop}.");
	}

	/// <summary>Builds a body at rest, so a test's graph reads as a list of placements.</summary>
	private static TestBody Body(int id, double x, double y, double width, double height, bool pinned = false) =>
		new(id, new Vec2D(x, y), new Vec2D(width, height), Vec2D.Zero, Vec2D.Zero, pinned);

	/// <summary>
	/// Settles a graph whose only edge runs from body 1 to body 2, and reports where the two ended up.
	/// </summary>
	/// <returns>The horizontal centres of the edge's source and target.</returns>
	private static (double SourceCenterX, double TargetCenterX) SettleBackwardEdge(List<TestBody> bodies)
	{
		ForceDirectedLayout<TestBody, TestEdge> layout = CreateLayout(new PhysicsSettings { Enabled = true });
		List<TestEdge> edges = [new TestEdge(1, 2)];

		// The pair crosses within about 30 steps; the rest is settling back into a row.
		for (int i = 0; i < 1200; i++)
		{
			layout.Step(bodies, edges, 0.016);
		}

		return (bodies[0].Position.X + (bodies[0].Dimensions.X * 0.5),
			bodies[1].Position.X + (bodies[1].Dimensions.X * 0.5));
	}

	/// <summary>An edge that knows which pin it attaches to at each end.</summary>
	private sealed record PinnedEdge(int SourceId, int TargetId, Vec2D SourcePin, Vec2D TargetPin);

	private static ForceDirectedLayout<TestBody, PinnedEdge> CreatePinnedLayout(PhysicsSettings settings) =>
		new(
			new BodyAccessor<TestBody>(
				GetId: b => b.Id,
				GetPosition: b => b.Position,
				GetDimensions: b => b.Dimensions,
				GetVelocity: b => b.Velocity,
				GetForce: b => b.Force,
				GetIsPinned: b => b.IsPinned,
				WithPhysicsState: (b, p, v, f) => b with { Position = p, Velocity = v, Force = f }),
			new EdgeAccessor<PinnedEdge>(
				GetSourceBodyId: e => e.SourceId,
				GetTargetBodyId: e => e.TargetId,
				GetSourcePinOffset: e => e.SourcePin,
				GetTargetPinOffset: e => e.TargetPin))
		{
			Settings = settings,
		};

	/// <summary>
	/// Tests that a graph the size of a small class reaches a readable shape within the first few
	/// seconds, rather than only after a minute and a half of settling.
	/// </summary>
	/// <remarks>
	/// 600 steps is ten seconds in an editor running at sixty frames a second, which is about as long
	/// as anyone watches a graph unfold. With MaxVelocity at 50 this same graph was still a tall narrow
	/// column at that point - bodies simply could not travel the several hundred units to their places
	/// fast enough - and only squared up around 6000 steps. Nothing measured settling speed, so the
	/// layout looked broken while being perfectly correct.
	/// </remarks>
	[TestMethod]
	public void ASmallGraph_IsReadableWithinTenSeconds()
	{
		// Over several starts rather than one: the simulation is chaotic, so which local minimum a
		// single arrangement falls into says nothing about how fast the layout gets there in general.
		BenchResult result = LayoutBench.Run(
			GraphCorpus.Counter,
			LayoutSettings.Defaults,
			"counter",
			new BenchOptions(Starts: 6, Frames: 600, ReadableAfter: 600));

		Assert.IsTrue(result.ReadableStarts >= 5,
			$"A left-to-right graph should be wider than it is tall with near-horizontal edges by now; " +
			$"{result.ReadableStarts} of {result.Starts} starts were, at a mean angle of {result.MeanEdgeAngle:F1} degrees.");
	}

	/// <summary>
	/// Counts pairs of links meeting at a body whose far ends sit in the opposite vertical order to the
	/// pins they meet at, which is exactly the arrangement in which the two are drawn crossing.
	/// </summary>
	private static int TwistedPairs(List<TestBody> bodies, List<PinnedEdge> edges)
	{
		Dictionary<int, TestBody> byId = bodies.ToDictionary(b => b.Id);
		Vec2D Source(PinnedEdge e) => byId[e.SourceId].Position + e.SourcePin;
		Vec2D Target(PinnedEdge e) => byId[e.TargetId].Position + e.TargetPin;

		int twisted = 0;
		for (int i = 0; i < edges.Count; i++)
		{
			for (int j = i + 1; j < edges.Count; j++)
			{
				double atPins;
				double atFarEnds;

				if (edges[i].TargetId == edges[j].TargetId && edges[i].SourceId != edges[j].SourceId)
				{
					atPins = Target(edges[i]).Y - Target(edges[j]).Y;
					atFarEnds = Source(edges[i]).Y - Source(edges[j]).Y;
				}
				else if (edges[i].SourceId == edges[j].SourceId && edges[i].TargetId != edges[j].TargetId)
				{
					atPins = Source(edges[i]).Y - Source(edges[j]).Y;
					atFarEnds = Target(edges[i]).Y - Target(edges[j]).Y;
				}
				else
				{
					continue;
				}

				if (atPins * atFarEnds < 0)
				{
					twisted++;
				}
			}
		}

		return twisted;
	}

	/// <summary>
	/// Tests that repulsion is still what spreads a graph out, now that an overlap pass keeps bodies off
	/// one another and an untwisting force reorders their far ends.
	/// </summary>
	/// <remarks>
	/// The overlap pass only guarantees bodies do not sit on top of each other; it creates no room beyond
	/// that, and untwisting only says which way round two of them go. Without repulsion a settled graph
	/// collapses, and the links then have nowhere to run but across the bodies.
	/// <para>
	/// Neither shape nor edge angle can say this. Before there was an untwisting force the collapse was
	/// into a tall column of near-vertical links, and both did; with one the collapse is into a flat
	/// crushed ribbon whose links are flatter than the properly spread graph's. The graph is no better
	/// for it - everything is simply drawn on top of everything - so what is asserted here is the room
	/// itself, and what the want of it does to the links.
	/// </para>
	/// <para>
	/// Measured over several starts rather than one. On a single arrangement this claim is true on
	/// average and unreliable in particular: the settled area of one start swings far enough with the
	/// repulsion strength that the same assertion passed at 1,200,000 and 600,000, failed at 800,000,
	/// and passed again at 400,000 - chaos, not a threshold.
	/// </para>
	/// </remarks>
	[TestMethod]
	public void Repulsion_IsWhatSpreadsAGraphOut()
	{
		BenchOptions shape = new(Starts: 8, Frames: 6000, ReadableAfter: 600);

		LayoutSettings without = LayoutSettings.Defaults;
		without.RepulsionStrength = 0;

		IReadOnlyList<BenchResult> rows = LayoutBench.Compare(
			GraphCorpus.Counter,
			shape,
			("with", LayoutSettings.Defaults),
			("without", without));

		BenchResult with = rows[0];
		BenchResult none = rows[1];

		Assert.IsTrue(with.MeanArea > none.MeanArea * 2.0,
			$"Repulsion should leave the graph far roomier; with {with.MeanArea:F0}, without {none.MeanArea:F0}.");
		Assert.IsTrue(with.MeanLinksOverBodies * 3 < none.MeanLinksOverBodies,
			$"Without repulsion far more links should be drawn over bodies; with {with.MeanLinksOverBodies:F1}, " +
			$"without {none.MeanLinksOverBodies:F1}.");
	}

	/// <summary>
	/// Tests that two links arriving at one node from bodies in the wrong vertical order swap those
	/// bodies over, so the links stop crossing.
	/// </summary>
	/// <remarks>
	/// The two links here are individually perfect - short, level and well spaced - and every force that
	/// existed before this one is satisfied by the starting arrangement. They are only wrong about each
	/// other: the body feeding the upper pin starts below the body feeding the lower one, so its link has
	/// to dive under the other's to reach its pin.
	/// </remarks>
	[TestMethod]
	public void TwistedLinks_SwapTheirFarEndsIntoPinOrder()
	{
		// One target with two input pins, 60 apart, fed by two sources that start the wrong way round.
		List<TestBody> bodies = [Body(1, 0, 260, 100, 60), Body(2, 0, 0, 100, 60), Body(3, 400, 100, 120, 140)];
		List<PinnedEdge> edges =
		[
			new(1, 3, new Vec2D(100, 30), new Vec2D(0, 40)),
			new(2, 3, new Vec2D(100, 30), new Vec2D(0, 100)),
		];

		Assert.AreEqual(1, TwistedPairs(bodies, edges), "the pair should start twisted");

		ForceDirectedLayout<TestBody, PinnedEdge> layout = CreatePinnedLayout(new PhysicsSettings { Enabled = true });
		for (int i = 0; i < 2000; i++)
		{
			layout.Step(bodies, edges, 0.016);
		}

		Assert.AreEqual(0, TwistedPairs(bodies, edges),
			$"The pair should have swapped; body 1 is at y {bodies[0].Position.Y:F0} and body 2 at y {bodies[1].Position.Y:F0}.");
		Assert.IsTrue(bodies[0].Position.Y < bodies[1].Position.Y,
			"the body feeding the upper pin should end up above the one feeding the lower pin");
	}

	/// <summary>
	/// Tests that a graph with no pin offsets is left alone, since without them there is no pin order to
	/// be wrong about.
	/// </summary>
	[TestMethod]
	public void Untwisting_DoesNothingWithoutPinOffsets()
	{
		List<TestBody> bodies = [Body(1, 0, 260, 100, 60), Body(2, 0, 0, 100, 60), Body(3, 400, 100, 120, 140)];
		List<TestEdge> edges = [new(1, 3), new(2, 3)];

		ForceDirectedLayout<TestBody, TestEdge> layout = CreateLayout(new PhysicsSettings { Enabled = true });
		for (int i = 0; i < 600; i++)
		{
			layout.Step(bodies, edges, 0.016);
		}

		// Both links fall back to their bodies' mid-heights, giving the two the same pin height at the
		// shared node, so neither ordering is the wrong one and the starting order survives.
		Assert.IsTrue(bodies[0].Position.Y > bodies[1].Position.Y,
			"with no pin offsets the two sources should keep the order they started in");
	}

	/// <summary>
	/// Tests that untwisting reduces the crossings in a graph the size of a small class, and does not
	/// pay for it by leaving bodies drawn over one another.
	/// </summary>
	/// <remarks>
	/// Crossings between links that share a node are what a settled tangle is mostly made of - measured
	/// over forty starting arrangements of this graph, 5.83 twisted pairs against 5.85 such crossings,
	/// so nearly every one of them is a twist and this force can reach it.
	/// <para>
	/// The second assertion is the one that constrains the design. A twisted pair has to pass through
	/// each other vertically to swap, and the overlap pass holding them apart on that axis is what
	/// stalls the swap - so a body mid-untwist is allowed to overlap along Y. It is still pushed apart
	/// on X, and the flag is recomputed every substep, so the exemption lasts exactly as long as the
	/// untwist does and a settled graph is left with no overlaps at all.
	/// </para>
	/// </remarks>
	[TestMethod]
	public void Untwisting_ReducesCrossingsWithoutLeavingBodiesOverlapping()
	{
		// Several starting arrangements, because which local minimum one start happens to land in says
		// nothing: the claim is about the shape of a settled graph in general, so it is measured the way
		// it was established.
		BenchOptions shape = new(Starts: 6, Frames: 4000, ReadableAfter: 600);

		LayoutSettings without = LayoutSettings.Defaults;
		without.LinkUntwistStrength = 0;

		IReadOnlyList<BenchResult> rows = LayoutBench.Compare(
			GraphCorpus.Counter,
			shape,
			("with", LayoutSettings.Defaults),
			("without", without));

		Assert.IsTrue(rows[0].MeanTwistedPairs < rows[1].MeanTwistedPairs,
			$"Untwisting should leave fewer crossed pairs across the starts; with {rows[0].MeanTwistedPairs:F1}, " +
			$"without {rows[1].MeanTwistedPairs:F1}.");
		Assert.AreEqual(0.0, rows[0].WorstOverlap, 0.5,
			"and no start should be left with bodies drawn over one another");
	}

	/// <summary>
	/// Tests that the overlap pass leaves the untwist axis free, rather than holding a twisted pair
	/// apart on the very axis its swap has to travel.
	/// </summary>
	/// <remarks>
	/// This is the standoff a backward edge hits, on the other axis: a reorder travels along X so the
	/// pair is pushed apart on Y, and a swap travels along Y so the pair is pushed apart on X.
	/// <para>
	/// The two bodies below are wide and short, so the overlap pass would rather separate them on Y -
	/// the cheaper axis, and the one direction the swap needs. With nothing untwisting them that is
	/// exactly what it does, and they come to rest at least a clearance apart vertically: half of each
	/// height plus the margin. With the untwist running they close well inside where they would
	/// otherwise have settled, and the separation goes on X instead.
	/// </para>
	/// <para>
	/// What is asserted is the difference the untwist makes rather than the resting distance itself,
	/// because the overlap pass is not the only thing holding the pair apart on Y. Repulsion measures
	/// the clear space between the two rectangles, and for a pair stacked in a column that space is
	/// their vertical gap alone, so it too pushes them apart along the swap - which is why the held pair
	/// sits a good way outside the clearance rather than exactly on it.
	/// </para>
	/// </remarks>
	[TestMethod]
	public void TwistedLinks_AreNotHeldApartOnTheAxisTheySwapAlong()
	{
		static (double Vertical, double Horizontal) Settle(double untwistStrength)
		{
			List<TestBody> bodies = [Body(1, 0, 150, 300, 50), Body(2, 0, 60, 300, 50), Body(3, 500, 60, 120, 140)];
			List<PinnedEdge> edges =
			[
				new(1, 3, new Vec2D(300, 25), new Vec2D(0, 40)),
				new(2, 3, new Vec2D(300, 25), new Vec2D(0, 100)),
			];

			ForceDirectedLayout<TestBody, PinnedEdge> layout = CreatePinnedLayout(
				new PhysicsSettings { Enabled = true, LinkUntwistStrength = untwistStrength });

			for (int i = 0; i < 3000; i++)
			{
				layout.Step(bodies, edges, 0.016);
			}

			return (Math.Abs(bodies[0].Position.Y - bodies[1].Position.Y),
				Math.Abs(bodies[0].Position.X - bodies[1].Position.X));
		}

		(double heldVertical, double heldHorizontal) = Settle(0.0);
		(double freeVertical, double freeHorizontal) = Settle(0.1);

		double clearance = (50 * 0.5) + (50 * 0.5) + LayoutSettings.Defaults.OverlapMargin;
		Assert.IsTrue(heldVertical >= clearance,
			$"With nothing untwisting them the pair should be held at least a clearance apart vertically; it was {heldVertical:F0} against {clearance:F0}.");
		Assert.IsTrue(freeVertical < heldVertical,
			$"A twisted pair should be allowed to close on the axis it swaps along; it settled {freeVertical:F0} apart against {heldVertical:F0}.");
		Assert.IsTrue(freeHorizontal > heldHorizontal,
			$"and should take the separation on X instead; {freeHorizontal:F0} against {heldHorizontal:F0}.");
	}

	[TestMethod]
	public void PinOffsets_LevelThePinsRatherThanTheBodyCentres()
	{
		// A tall body whose pin sits near its top, feeding a short one whose pin is at its middle. Level
		// the centres and the link is still steep; level the pins and the tall body has to ride up.
		ForceDirectedLayout<TestBody, PinnedEdge> layout = CreatePinnedLayout(new PhysicsSettings { Enabled = true });
		List<TestBody> bodies = [Body(1, 0, 0, 160, 300), Body(2, 400, 0, 160, 60)];
		List<PinnedEdge> edges = [new PinnedEdge(1, 2, new Vec2D(160, 30), new Vec2D(0, 30))];

		for (int i = 0; i < 2000; i++)
		{
			layout.Step(bodies, edges, 0.016);
		}

		double sourcePinY = bodies[0].Position.Y + 30;
		double targetPinY = bodies[1].Position.Y + 30;
		double sourceCentreY = bodies[0].Position.Y + 150;
		double targetCentreY = bodies[1].Position.Y + 30;
		double centreGap = Math.Abs(targetCentreY - sourceCentreY);

		// Soft against gravity, so the pins settle near level rather than exactly on it. What matters is
		// which pair got levelled: the pins end up far closer together than the centres, where measuring
		// from centres would have produced the reverse.
		double pinGap = Math.Abs(targetPinY - sourcePinY);

		Assert.IsTrue(pinGap < 60.0, $"The pins should settle close to level; they are {pinGap} apart.");
		Assert.IsTrue(centreGap > pinGap * 1.5,
			$"The centres should stay further apart than the pins; centres {centreGap}, pins {pinGap}.");
	}

	[TestMethod]
	public void PinOffsets_MeasureTheSpringBetweenPins()
	{
		// No other force, so the spring alone settles the pair: pin-to-pin distance should reach the rest
		// length, which the centre-to-centre distance then cannot also equal.
		ForceDirectedLayout<TestBody, PinnedEdge> layout = CreatePinnedLayout(new PhysicsSettings
		{
			Enabled = true,
			RepulsionStrength = 0,
			GravityStrength = 0,
			DirectionalBias = 0,
			LinkFlatteningStrength = 0,
			OverlapMargin = 0,
			RestLinkLength = 200.0,
			DampingFactor = 0.1,
		});
		List<TestBody> bodies = [Body(1, 0, 0, 200, 80), Body(2, 800, 0, 200, 80)];
		List<PinnedEdge> edges = [new PinnedEdge(1, 2, new Vec2D(200, 40), new Vec2D(0, 40))];

		for (int i = 0; i < 4000; i++)
		{
			layout.Step(bodies, edges, 0.016);
		}

		double sourcePinX = bodies[0].Position.X + 200;
		double pinDistance = bodies[1].Position.X - sourcePinX;
		Assert.IsTrue(Math.Abs(pinDistance - 200.0) < 15.0,
			$"The spring should settle the pins at its rest length; they are {pinDistance} apart.");
	}

	[TestMethod]
	public void LinkFlattening_PullsAForwardEdgeTowardsHorizontal()
	{
		ForceDirectedLayout<TestBody, TestEdge> layout = CreateLayout(new PhysicsSettings { Enabled = true });
		List<TestBody> bodies = [Body(1, 0, 0, 160, 60), Body(2, 300, 400, 160, 60)];
		List<TestEdge> edges = [new TestEdge(1, 2)];

		double before = Math.Abs(bodies[1].Position.Y - bodies[0].Position.Y);

		for (int i = 0; i < 2000; i++)
		{
			layout.Step(bodies, edges, 0.016);
		}

		double after = Math.Abs(bodies[1].Position.Y - bodies[0].Position.Y);

		Assert.IsTrue(after < before * 0.25,
			$"A forward edge should settle close to level; vertical offset went from {before} to {after}.");
	}

	[TestMethod]
	public void LinkFlattening_ZeroStrength_LeavesAnEdgeAsSteepAsItStarted()
	{
		ForceDirectedLayout<TestBody, TestEdge> layout = CreateLayout(
			new PhysicsSettings { Enabled = true, LinkFlatteningStrength = 0, GravityStrength = 0, RepulsionStrength = 0, DirectionalBias = 0 });
		List<TestBody> bodies = [Body(1, 0, 0, 160, 60), Body(2, 300, 400, 160, 60)];
		List<TestEdge> edges = [new TestEdge(1, 2)];

		for (int i = 0; i < 2000; i++)
		{
			layout.Step(bodies, edges, 0.016);
		}

		// Both bodies are the same size, so the centre offsets cancel.
		double dx = bodies[1].Position.X - bodies[0].Position.X;
		double dy = bodies[1].Position.Y - bodies[0].Position.Y;

		Assert.IsTrue(Math.Abs(dy) > Math.Abs(dx),
			$"With the preference off, the spring alone should leave this edge steeper than it is wide; dx {dx}, dy {dy}.");
	}

	[TestMethod]
	public void BackwardEdge_SwapsTheEndpointsIntoOrder()
	{
		// Mirrors the node editor: a small "param value" body sits to the right of the big "function"
		// body it feeds, so the edge runs right-to-left and its curve hides behind both.
		List<TestBody> bodies = [Body(1, 400, 10, 160, 60), Body(2, 0, 0, 270, 230)];

		(double sourceCenterX, double targetCenterX) = SettleBackwardEdge(bodies);

		Assert.IsTrue(sourceCenterX < targetCenterX,
			$"The source should end up left of its target; source centre {sourceCenterX}, target centre {targetCenterX}.");

		// They went around one another rather than through, so they end up clear, not stacked.
		double gap = targetCenterX - sourceCenterX;
		double clearance = (bodies[0].Dimensions.X + bodies[1].Dimensions.X) * 0.5;
		Assert.IsTrue(gap >= clearance, $"The reordered pair should not overlap; gap {gap} against clearance {clearance}.");
	}

	[TestMethod]
	public void BackwardEdge_SlidesPastAnUnrelatedBodyInItsPath()
	{
		// The reordering body has to cross the space a third, unconnected body occupies to reach its
		// place in the order, which is the shape a real graph takes once it has more than two nodes.
		List<TestBody> bodies =
		[
			Body(1, 600, 0, 160, 60),
			Body(2, 0, 0, 160, 60),
			Body(3, 300, 0, 160, 60, pinned: true),
		];

		(double sourceCenterX, double targetCenterX) = SettleBackwardEdge(bodies);

		Assert.IsTrue(sourceCenterX < targetCenterX,
			$"A pinned body in the way should not stop the reorder; source centre {sourceCenterX}, target centre {targetCenterX}.");
		Assert.AreEqual(new Vec2D(300, 0), bodies[2].Position, "The pinned body should not have moved.");
	}

	[TestMethod]
	public void LinkFlattening_DanglingEdge_IsSkipped()
	{
		ForceDirectedLayout<TestBody, TestEdge> layout = CreateLayout(FlatteningOnly());
		List<TestBody> bodies =
		[
			new TestBody(1, new Vec2D(0, 0), new Vec2D(100, 50), Vec2D.Zero, Vec2D.Zero, false),
			new TestBody(2, new Vec2D(0, 400), new Vec2D(100, 50), Vec2D.Zero, Vec2D.Zero, false),
		];

		// Node 999 does not exist, so the edge resolves to index -1 at both ends.
		List<TestEdge> edges = [new TestEdge(1, 999), new TestEdge(999, 2)];

		for (int i = 0; i < 20; i++)
		{
			layout.Step(bodies, edges, 0.016);
		}

		Assert.AreEqual(new Vec2D(0, 0), bodies[0].Position, "A dangling edge must not push its resolvable end.");
		Assert.AreEqual(new Vec2D(0, 400), bodies[1].Position, "A dangling edge must not push its resolvable end.");
	}

	[TestMethod]
	public void LinkFlattening_AlreadyFlatPair_IsLeftAlone()
	{
		ForceDirectedLayout<TestBody, TestEdge> layout = CreateLayout(FlatteningOnly() with { LinkSpringStrength = 0 });
		List<TestBody> bodies =
		[
			new TestBody(1, new Vec2D(0, 0), new Vec2D(100, 50), Vec2D.Zero, Vec2D.Zero, false),
			new TestBody(2, new Vec2D(400, 0), new Vec2D(100, 50), Vec2D.Zero, Vec2D.Zero, false),
		];
		List<TestEdge> edges = [new TestEdge(1, 2)];

		for (int i = 0; i < 50; i++)
		{
			layout.Step(bodies, edges, 0.016);
		}

		Assert.AreEqual(new Vec2D(0, 0), bodies[0].Position, "A horizontal edge already clears the bound, so nothing should push.");
		Assert.AreEqual(new Vec2D(400, 0), bodies[1].Position, "A horizontal edge already clears the bound, so nothing should push.");
	}

	[TestMethod]
	public void LinkFlattening_ZeroStrength_DisablesTheForce()
	{
		ForceDirectedLayout<TestBody, TestEdge> layout = CreateLayout(
			FlatteningOnly() with { LinkFlatteningStrength = 0, LinkSpringStrength = 0 });
		List<TestBody> bodies =
		[
			new TestBody(1, new Vec2D(0, 0), new Vec2D(100, 50), Vec2D.Zero, Vec2D.Zero, false),
			new TestBody(2, new Vec2D(0, 400), new Vec2D(100, 50), Vec2D.Zero, Vec2D.Zero, false),
		];
		List<TestEdge> edges = [new TestEdge(1, 2)];

		for (int i = 0; i < 50; i++)
		{
			layout.Step(bodies, edges, 0.016);
		}

		Assert.AreEqual(0.0, bodies[0].Position.X, "With the force off, a stacked pair must not splay.");
		Assert.AreEqual(0.0, bodies[1].Position.X, "With the force off, a stacked pair must not splay.");
	}

	[TestMethod]
	public void LinkFlattening_Margin_AddsClearanceOnTopOfTheBound()
	{
		ForceDirectedLayout<TestBody, TestEdge> layout = CreateLayout(
			FlatteningOnly() with { LinkFlatteningMargin = 150.0, LinkSpringStrength = 0 });
		List<TestBody> bodies =
		[
			new TestBody(1, new Vec2D(0, 0), new Vec2D(100, 50), Vec2D.Zero, Vec2D.Zero, false),
			new TestBody(2, new Vec2D(400, 0), new Vec2D(100, 50), Vec2D.Zero, Vec2D.Zero, false),
		];
		List<TestEdge> edges = [new TestEdge(1, 2)];

		for (int i = 0; i < 200; i++)
		{
			layout.Step(bodies, edges, 0.016);
		}

		double gap = bodies[1].Position.X - (bodies[0].Position.X + bodies[0].Dimensions.X);
		Assert.IsTrue(gap >= 150.0, $"A margin of 150 should hold even a flat edge that far apart; gap was {gap}.");
	}

	[TestMethod]
	public void GenericFacade_PinnedNode_DoesNotMove()
	{
		PhysicsSettings enabled = new()
		{
			Enabled = true,
			GravityStrength = 500.0,
		};
		ForceDirectedLayout<TestBody, TestEdge> layout = CreateLayout(enabled);
		List<TestBody> bodies =
		[
			new TestBody(1, new Vec2D(0, 0), new Vec2D(50, 50), Vec2D.Zero, Vec2D.Zero, true),
			new TestBody(2, new Vec2D(300, 300), new Vec2D(50, 50), Vec2D.Zero, Vec2D.Zero, false),
		];
		List<TestEdge> edges = [];

		Vec2D pinnedPosBefore = bodies[0].Position;

		for (int i = 0; i < 20; i++)
		{
			layout.Step(bodies, edges, 0.016);
		}

		Assert.AreEqual(pinnedPosBefore, bodies[0].Position, "Pinned node must not move.");
		Assert.AreNotEqual(new Vec2D(300, 300), bodies[1].Position, "Free node should have moved.");
	}

	[TestMethod]
	public void GenericFacade_SetFrozenBodies_ExcludesFromIntegration()
	{
		PhysicsSettings enabled = new() { Enabled = true, GravityStrength = 500.0 };
		ForceDirectedLayout<TestBody, TestEdge> layout = CreateLayout(enabled);
		List<TestBody> bodies =
		[
			new TestBody(1, new Vec2D(0, 0), new Vec2D(50, 50), Vec2D.Zero, Vec2D.Zero, false),
			new TestBody(2, new Vec2D(300, 0), new Vec2D(50, 50), Vec2D.Zero, Vec2D.Zero, false),
		];
		List<TestEdge> edges = [];

		layout.SetFrozenBodies(new HashSet<int>([1]));
		Vec2D frozenPosBefore = bodies[0].Position;

		for (int i = 0; i < 20; i++)
		{
			layout.Step(bodies, edges, 0.016);
		}

		Assert.AreEqual(frozenPosBefore, bodies[0].Position, "Frozen node must not move.");
	}

	[TestMethod]
	public void GenericFacade_InitializeWorldOriginToCentroid_SetsOriginCorrectly()
	{
		ForceDirectedLayout<TestBody, TestEdge> layout = CreateLayout();
		List<TestBody> bodies =
		[
			new TestBody(1, new Vec2D(0, 0), new Vec2D(100, 100), Vec2D.Zero, Vec2D.Zero, false),
			new TestBody(2, new Vec2D(200, 0), new Vec2D(100, 100), Vec2D.Zero, Vec2D.Zero, false),
		];

		layout.InitializeWorldOriginToCentroid(bodies);

		// Centroid of centers: center1=(50,50), center2=(250,50) → centroid=(150,50)
		Assert.AreEqual(150.0, layout.WorldOrigin.X, 0.001);
		Assert.AreEqual(50.0, layout.WorldOrigin.Y, 0.001);
	}

	[TestMethod]
	public void GenericFacade_InitializeWorldOriginToCentroid_EmptyBodies_SetsZero()
	{
		ForceDirectedLayout<TestBody, TestEdge> layout = CreateLayout();
		layout.InitializeWorldOriginToCentroid([]);
		Assert.AreEqual(Vec2D.Zero, layout.WorldOrigin);
	}

	[TestMethod]
	public void GenericFacade_SettingsRoundTrip_PreservesValues()
	{
		ForceDirectedLayout<TestBody, TestEdge> layout = CreateLayout();
		PhysicsSettings s = new() { Enabled = true, RepulsionStrength = 999.0, RestLinkLength = 123.0 };
		layout.Settings = s;
		Assert.AreEqual(s, layout.Settings);
	}

	[TestMethod]
	public void GenericFacade_GravityCenterAndEnergy_ArePublished()
	{
		PhysicsSettings enabled = new() { Enabled = true, OriginAnchorWeight = 0 };
		ForceDirectedLayout<TestBody, TestEdge> layout = CreateLayout(enabled);
		List<TestBody> bodies =
		[
			new TestBody(1, new Vec2D(0, 0), new Vec2D(50, 50), Vec2D.Zero, Vec2D.Zero, false),
			new TestBody(2, new Vec2D(200, 0), new Vec2D(50, 50), Vec2D.Zero, Vec2D.Zero, false),
		];

		layout.Step(bodies, [new TestEdge(1, 2)], 0.016);

		Assert.IsTrue(layout.TotalSystemEnergy >= 0.0);
		Assert.AreNotEqual(Vec2D.Zero, layout.GravityCenter);
		Assert.IsTrue(layout.LastStepInfo.SubstepCount > 0);
	}
}
