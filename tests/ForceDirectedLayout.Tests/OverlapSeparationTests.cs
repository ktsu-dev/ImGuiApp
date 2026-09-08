// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ForceDirectedLayout.Tests;

using System;
using ktsu.ForceDirectedLayout;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the positional pass that keeps body rectangles off one another.
/// </summary>
/// <remarks>
/// Every force in the simulation treats a body as a point, so none of them can see that two bodies
/// are drawn over each other: the comfortable distance depends on the pair's sizes and the forces do
/// not have them. These cover the pass that does, including the case no amount of force tuning could
/// ever fix — two bodies at exactly the same point, which repulsion skips for want of a direction.
/// </remarks>
[TestClass]
public class OverlapSeparationTests
{
	private static LayoutSettings EnabledDefaults()
	{
		LayoutSettings s = LayoutSettings.Defaults;
		s.Enabled = 1;
		return s;
	}

	/// <summary>
	/// Runs a second of simulation at sixty frames a second.
	/// </summary>
	/// <param name="layout">The layout to step.</param>
	/// <param name="frames">How many frames to run.</param>
	private static void Step(ForceLayout layout, int frames)
	{
		for (int frame = 0; frame < frames; frame++)
		{
			layout.Step(1.0 / 60.0);
		}
	}

	/// <summary>
	/// Asserts that no two of the given bodies are drawn over one another.
	/// </summary>
	/// <param name="positions">Where each body ended up.</param>
	/// <param name="dimensions">How big each body is, in the same order.</param>
	private static void AssertNoOverlaps(ReadOnlySpan<NodePosition> positions, Vec2D[] dimensions)
	{
		for (int i = 0; i < positions.Length; i++)
		{
			for (int j = i + 1; j < positions.Length; j++)
			{
				bool apart =
					positions[i].Position.X + dimensions[i].X <= positions[j].Position.X ||
					positions[j].Position.X + dimensions[j].X <= positions[i].Position.X ||
					positions[i].Position.Y + dimensions[i].Y <= positions[j].Position.Y ||
					positions[j].Position.Y + dimensions[j].Y <= positions[i].Position.Y;

				Assert.IsTrue(
					apart,
					$"Bodies {i} at {positions[i].Position} and {j} at {positions[j].Position} are drawn over one another.");
			}
		}
	}

	[TestMethod]
	public void Step_LinkedWideBodies_EndUpClearOfEachOther()
	{
		// Wider than the spring's rest length, so the forces alone hold them overlapping: the spring
		// wants their centers 225 apart and they need 420 to be clear.
		Vec2D[] dimensions = [new Vec2D(400, 100), new Vec2D(400, 100)];
		ForceLayout layout = new(EnabledDefaults());
		layout.SetNodes(
		[
			new() { Id = 1, Position = new Vec2D(0, 0), Dimensions = dimensions[0] },
			new() { Id = 2, Position = new Vec2D(225, 0), Dimensions = dimensions[1] },
		]);
		layout.SetEdges(
		[
			new() { SourceBodyId = 1, TargetBodyId = 2 },
		]);

		Step(layout, frames: 120);

		Span<NodePosition> positions = stackalloc NodePosition[2];
		layout.GetPositions(positions);
		AssertNoOverlaps(positions, dimensions);
	}

	[TestMethod]
	public void Step_CoincidentBodies_Separate()
	{
		// Repulsion is computed from the direction between two centers, and coincident centers have
		// none, so it skips the pair entirely. Only the overlap pass moves these.
		Vec2D[] dimensions = [new Vec2D(120, 60), new Vec2D(120, 60)];
		ForceLayout layout = new(EnabledDefaults());
		layout.SetNodes(
		[
			new() { Id = 1, Position = new Vec2D(200, 200), Dimensions = dimensions[0] },
			new() { Id = 2, Position = new Vec2D(200, 200), Dimensions = dimensions[1] },
		]);

		Step(layout, frames: 60);

		Span<NodePosition> positions = stackalloc NodePosition[2];
		layout.GetPositions(positions);
		AssertNoOverlaps(positions, dimensions);

		// Shared equally, so neither body carries the whole correction.
		Assert.AreNotEqual(200.0, positions[0].Position.Y, 0.0001, "the first body should have moved too");
		Assert.AreNotEqual(200.0, positions[1].Position.Y, 0.0001, "the second body should have moved too");
	}

	[TestMethod]
	public void Step_WithOverlapMarginZero_LeavesTheBodiesOverlapping()
	{
		// Opting out has to be possible for a consumer that arranges its own bodies, and leaving them
		// overlapping is what says the separation is this pass's doing rather than the forces'.
		// Repulsion and the ordering bias both push a pair apart on their own - the ordering bias alone
		// targets halfWidths + 20, which for these bodies is wider than they are - so they are turned
		// off here. Otherwise this measures how far the pair has travelled rather than whether the pass
		// is what separates them.
		LayoutSettings settings = EnabledDefaults();
		settings.OverlapMargin = 0.0;
		settings.RepulsionStrength = 0.0;
		settings.DirectionalBias = 0.0;
		settings.LinkFlatteningStrength = 0.0;

		Vec2D[] dimensions = [new Vec2D(400, 100), new Vec2D(400, 100)];
		ForceLayout layout = new(settings);
		layout.SetNodes(
		[
			new() { Id = 1, Position = new Vec2D(0, 0), Dimensions = dimensions[0] },
			new() { Id = 2, Position = new Vec2D(225, 0), Dimensions = dimensions[1] },
		]);
		layout.SetEdges(
		[
			new() { SourceBodyId = 1, TargetBodyId = 2 },
		]);

		Step(layout, frames: 120);

		Span<NodePosition> positions = stackalloc NodePosition[2];
		layout.GetPositions(positions);
		double gap = Math.Abs(positions[1].Position.X - positions[0].Position.X);
		Assert.IsTrue(gap < dimensions[0].X, $"Without the overlap pass the spring should still hold them overlapping; gap was {gap}.");
	}

	[TestMethod]
	public void Step_PinnedBody_TakesNoneOfTheSeparation()
	{
		Vec2D[] dimensions = [new Vec2D(100, 100), new Vec2D(100, 100)];
		ForceLayout layout = new(EnabledDefaults());
		layout.SetNodes(
		[
			new() { Id = 1, Position = new Vec2D(0, 0), Dimensions = dimensions[0], IsPinned = 1 },
			new() { Id = 2, Position = new Vec2D(20, 0), Dimensions = dimensions[1] },
		]);

		Step(layout, frames: 60);

		Span<NodePosition> positions = stackalloc NodePosition[2];
		layout.GetPositions(positions);

		Assert.AreEqual(0.0, positions[0].Position.X, 0.0001, "a pinned body is not pushed out of an overlap");
		Assert.AreEqual(0.0, positions[0].Position.Y, 0.0001, "a pinned body is not pushed out of an overlap");
		AssertNoOverlaps(positions, dimensions);
	}

	[TestMethod]
	public void Step_TwoPinnedBodies_AreLeftWhereTheyAre()
	{
		// Neither can move, so the pass has nothing to do rather than something to do wrongly.
		Vec2D[] dimensions = [new Vec2D(100, 100), new Vec2D(100, 100)];
		ForceLayout layout = new(EnabledDefaults());
		layout.SetNodes(
		[
			new() { Id = 1, Position = new Vec2D(0, 0), Dimensions = dimensions[0], IsPinned = 1 },
			new() { Id = 2, Position = new Vec2D(20, 0), Dimensions = dimensions[1], IsPinned = 1 },
		]);

		Step(layout, frames: 60);

		Span<NodePosition> positions = stackalloc NodePosition[2];
		layout.GetPositions(positions);
		Assert.AreEqual(0.0, positions[0].Position.X, 0.0001);
		Assert.AreEqual(20.0, positions[1].Position.X, 0.0001);
	}

	[TestMethod]
	public void Step_SeparationIsCappedPerSubstep()
	{
		// A deep overlap slides apart over several frames rather than snapping, which is what makes the
		// correction watchable rather than a jump.
		LayoutSettings settings = EnabledDefaults();
		settings.GravityStrength = 0.0;
		settings.RepulsionStrength = 0.0;
		settings.MaxOverlapCorrection = 10.0;

		ForceLayout layout = new(settings);
		layout.SetNodes(
		[
			new() { Id = 1, Position = new Vec2D(0, 0), Dimensions = new Vec2D(400, 400) },
			new() { Id = 2, Position = new Vec2D(0, 0), Dimensions = new Vec2D(400, 400) },
		]);

		// A frame exactly as long as the target substep is one substep, so the pair moves by at most
		// one correction: five each way.
		layout.Step(1.0 / settings.TargetPhysicsHz);
		Assert.AreEqual(1, layout.LastStepInfo.SubstepCount, "the cap is per substep, so the test has to run exactly one");

		Span<NodePosition> positions = stackalloc NodePosition[2];
		layout.GetPositions(positions);
		double moved = Math.Abs(positions[1].Position.Y - positions[0].Position.Y);
		Assert.IsTrue(moved <= 10.0 + 0.0001, $"a single substep moved the pair {moved} apart, past the cap");
		Assert.IsTrue(moved > 0.0, "the pair should have started to come apart");
	}
}
