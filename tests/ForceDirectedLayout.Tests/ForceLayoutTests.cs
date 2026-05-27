// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ForceDirectedLayout.Tests;

using System;
using System.Collections.Generic;
using ktsu.ForceDirectedLayout;
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
			GravityStrength = 7.0,
			OriginAnchorWeight = 0.3,
			DampingFactor = 0.4,
			MinRepulsionDistance = 60.0,
			RestLinkLength = 180.0,
			MaxForce = 999.0,
			MaxVelocity = 33.0,
			TargetPhysicsHz = 144.0,
			StabilityThreshold = 0.5,
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
