// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ForceDirectedLayout.Tests;

using System;
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
		layout.SetEdges(new EdgeInit[]
		{
			new() { SourceBodyId = 1, TargetBodyId = 2 },
		});

		Assert.AreEqual(1, layout.EdgeCount);
	}

	[TestMethod]
	public void Step_WithDisabled_IsNoOp()
	{
		ForceLayout layout = new(LayoutSettings.Defaults);
		layout.SetNodes(new NodeInit[]
		{
			new() { Id = 1, Position = new Vec2D(100, 0), Dimensions = new Vec2D(50, 50) },
			new() { Id = 2, Position = new Vec2D(101, 0), Dimensions = new Vec2D(50, 50) },
		});

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
		layout.SetNodes(new NodeInit[]
		{
			new() { Id = 1, Position = new Vec2D(0, 0), Dimensions = new Vec2D(50, 50) },
			new() { Id = 2, Position = new Vec2D(5, 0), Dimensions = new Vec2D(50, 50) },
		});

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
		layout.SetNodes(new NodeInit[]
		{
			new() { Id = 1, Position = new Vec2D(0, 0), Dimensions = new Vec2D(50, 50), IsPinned = 1 },
			new() { Id = 2, Position = new Vec2D(500, 500), Dimensions = new Vec2D(50, 50) },
		});

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
		layout.SetNodes(new NodeInit[]
		{
			new() { Id = 1, Position = new Vec2D(0, 0), Dimensions = new Vec2D(50, 50) },
			new() { Id = 2, Position = new Vec2D(225, 0), Dimensions = new Vec2D(50, 50) },
		});
		layout.SetEdges(new EdgeInit[]
		{
			new() { SourceBodyId = 1, TargetBodyId = 2 },
		});
		layout.InitializeWorldOriginToCentroid();

		int iterations = layout.Solve(maxIterations: 2000, tolerance: 0.5);
		Assert.IsTrue(iterations > 0, "Solve must run at least one iteration.");
		Assert.IsTrue(layout.IsStable, $"System should have stabilised; final energy {layout.TotalSystemEnergy}.");
	}

	[TestMethod]
	public void GetPositions_ThrowsOnUndersizedBuffer()
	{
		ForceLayout layout = new();
		layout.SetNodes(new NodeInit[]
		{
			new() { Id = 1, Dimensions = new Vec2D(50, 50) },
			new() { Id = 2, Dimensions = new Vec2D(50, 50) },
		});

		Assert.ThrowsException<ArgumentException>(() =>
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
