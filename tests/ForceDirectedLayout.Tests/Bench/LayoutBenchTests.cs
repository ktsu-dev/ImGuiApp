// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ForceDirectedLayout.Tests.Bench;

using System;
using System.Collections.Generic;
using ktsu.ForceDirectedLayout;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the benchmark harness itself, and holds the quality gate the corpus has to clear.
/// </summary>
/// <remarks>
/// The harness is what every claim about a layout force is measured with, so it needs to be right in
/// its own tests rather than trusted: a metric that silently counted the wrong thing would let a
/// regression through while reading like evidence.
/// <para>
/// To iterate on a force, add a scratch test that prints a sweep and read the column that should
/// have moved:
/// </para>
/// <code>
/// LayoutSettings baseline = LayoutSettings.Defaults;
/// Console.WriteLine(LayoutBench.Table(LayoutBench.Sweep(
///     GraphCorpus.Counter, baseline, "repulsion",
///     [300_000, 600_000, 1_200_000],
///     (s, v) => s with { RepulsionStrength = v })));
/// </code>
/// <para>
/// And to see one of them rather than read it, settle a single start and write it out:
/// </para>
/// <code>
/// LayoutCore core = GraphCorpus.Counter.Start(baseline, seed: 1, spread: 0.5);
/// core.Solve(maxIterations: 6000, tolerance: 0);
/// LayoutSvg.Write(core, "/tmp/counter.svg", LayoutMetrics.Measure(core).ToString());
/// </code>
/// </remarks>
[TestClass]
public class LayoutBenchTests
{
	/// <summary>Settings with everything switched off, so a test can place bodies and measure them.</summary>
	private static LayoutSettings Inert()
	{
		LayoutSettings s = LayoutSettings.Defaults;
		s.Enabled = 0;
		return s;
	}

	/// <summary>A core holding bodies at exactly the positions given, with no simulation run.</summary>
	/// <param name="bodies">Id, position and dimensions of each body.</param>
	private static LayoutCore Arrange(params (int Id, double X, double Y, double W, double H)[] bodies)
	{
		LayoutCore core = new() { Settings = Inert() };
		core.ResizeBodies(bodies.Length);
		for (int i = 0; i < bodies.Length; i++)
		{
			core.Bodies[i] = new BodyState
			{
				Id = bodies[i].Id,
				Position = new Vec2D(bodies[i].X, bodies[i].Y),
				Dimensions = new Vec2D(bodies[i].W, bodies[i].H),
			};
		}

		return core;
	}

	[TestMethod]
	public void Metrics_OnAKnownArrangement_ReportWhatIsThere()
	{
		// Two 100x50 boxes, 300 apart horizontally and level, so 200 of clear space between them.
		LayoutCore core = Arrange((1, 0, 0, 100, 50), (2, 300, 0, 100, 50));
		LayoutMetrics metrics = LayoutMetrics.Measure(core);

		Assert.AreEqual(400.0, metrics.Width, 0.001, "the bounding box spans from the first left edge to the second right");
		Assert.AreEqual(50.0, metrics.Height, 0.001);
		Assert.AreEqual(200.0, metrics.TightestClearGap, 0.001, "the clear space is the gap between the facing edges");
		Assert.AreEqual(200.0, metrics.MeanClearGap, 0.001, "and with one pair the mean is that same gap");
		Assert.AreEqual(0.0, metrics.WorstOverlap, 0.001);
		Assert.AreEqual(0, metrics.LinksOverBodies);
		Assert.IsTrue(metrics.Readable, "wider than it is tall, with no edges to be steep");
	}

	[TestMethod]
	public void Metrics_OnOverlappingBoxes_ReportTheDepthAndNoGap()
	{
		// Overlapping by 40 across and 30 down, so the shallower axis is the depth.
		LayoutCore core = Arrange((1, 0, 0, 100, 50), (2, 60, 20, 100, 50));
		LayoutMetrics metrics = LayoutMetrics.Measure(core);

		Assert.AreEqual(0.0, metrics.TightestClearGap, 0.001, "overlapping boxes have no clear space between them");
		Assert.AreEqual(30.0, metrics.WorstOverlap, 0.001, "and the overlap is measured on the shallower axis");
	}

	[TestMethod]
	public void Metrics_CountALinkDrawnAcrossABodyItIsNoEndOf()
	{
		// Three in a row, with a link from the first to the last straight through the middle one.
		LayoutCore core = Arrange((1, 0, 0, 100, 50), (2, 200, 0, 100, 50), (3, 400, 0, 100, 50));
		core.ResizeEdges(1);
		core.Edges[0] = new EdgeRef
		{
			SourceIndex = 0,
			TargetIndex = 2,
			SourcePinOffset = new Vec2D(100, 25),
			TargetPinOffset = new Vec2D(0, 25),
			HasPinOffsets = 1,
		};

		Assert.AreEqual(1, LayoutMetrics.Measure(core).LinksOverBodies,
			"the link runs the width of the middle body, which would hide it");
	}

	[TestMethod]
	public void Metrics_CountAPairOfLinksThatCross()
	{
		// Two sources feeding one target, each arriving at the pin the other one should have.
		LayoutCore core = Arrange((1, 0, 200, 100, 60), (2, 0, 0, 100, 60), (3, 400, 0, 120, 140));
		core.ResizeEdges(2);
		core.Edges[0] = new EdgeRef
		{
			SourceIndex = 0,
			TargetIndex = 2,
			SourcePinOffset = new Vec2D(100, 30),
			TargetPinOffset = new Vec2D(0, 40),
			HasPinOffsets = 1,
		};
		core.Edges[1] = new EdgeRef
		{
			SourceIndex = 1,
			TargetIndex = 2,
			SourcePinOffset = new Vec2D(100, 30),
			TargetPinOffset = new Vec2D(0, 100),
			HasPinOffsets = 1,
		};

		Assert.AreEqual(1, LayoutMetrics.Measure(core).TwistedPairs,
			"the body feeding the upper pin sits below the one feeding the lower pin");
	}

	[TestMethod]
	public void BenchGraph_Start_IsReproducibleAndScattersWithTheSpread()
	{
		LayoutSettings settings = LayoutSettings.Defaults;

		LayoutCore first = GraphCorpus.Counter.Start(settings, seed: 42, spread: 0.5);
		LayoutCore again = GraphCorpus.Counter.Start(settings, seed: 42, spread: 0.5);
		LayoutCore elsewhere = GraphCorpus.Counter.Start(settings, seed: 43, spread: 0.5);

		Assert.AreEqual(first.Bodies[0].Position, again.Bodies[0].Position, "the same seed gives the same arrangement");
		Assert.AreNotEqual(first.Bodies[0].Position, elsewhere.Bodies[0].Position, "a different seed gives a different one");

		// A tight spread piles the graph up; a wide one flings it out.
		LayoutCore tight = GraphCorpus.Counter.Start(settings, seed: 42, spread: 0.05);
		Assert.IsLessThan(LayoutMetrics.Measure(first).Width, LayoutMetrics.Measure(tight).Width,
			"a tighter spread should start the graph in a smaller area");
	}

	[TestMethod]
	public void BenchGraph_Start_AttachesEdgesToPinsWithinTheirNodes()
	{
		LayoutCore core = GraphCorpus.Counter.Start(LayoutSettings.Defaults, seed: 1, spread: 0.5);

		for (int e = 0; e < core.EdgeCount; e++)
		{
			EdgeRef edge = core.Edges[e];
			Assert.AreNotEqual(0, edge.HasPinOffsets, "every corpus edge should carry pin offsets");

			BodyState source = core.Bodies[edge.SourceIndex];
			BodyState target = core.Bodies[edge.TargetIndex];

			Assert.AreEqual(source.Dimensions.X, edge.SourcePinOffset.X, 0.001, "an output pin sits on its node's right edge");
			Assert.AreEqual(0.0, edge.TargetPinOffset.X, 0.001, "and an input pin on its node's left edge");
			Assert.IsTrue(edge.SourcePinOffset.Y >= 0 && edge.SourcePinOffset.Y <= source.Dimensions.Y,
				$"the source pin should sit within its node's height; it was at {edge.SourcePinOffset.Y}");
			Assert.IsTrue(edge.TargetPinOffset.Y >= 0 && edge.TargetPinOffset.Y <= target.Dimensions.Y,
				$"and so should the target pin; it was at {edge.TargetPinOffset.Y}");
		}
	}

	[TestMethod]
	public void Bench_MeasuringTwice_GivesTheSameAnswer()
	{
		// Determinism is the whole point: a sweep is only readable if the difference between two rows
		// is the setting and nothing else.
		BenchOptions quick = new(Starts: 3, Frames: 400, ReadableAfter: 200);

		BenchResult first = LayoutBench.Run(GraphCorpus.Chain, LayoutSettings.Defaults, "first", quick);
		BenchResult again = LayoutBench.Run(GraphCorpus.Chain, LayoutSettings.Defaults, "again", quick);

		Assert.AreEqual(first.MeanArea, again.MeanArea, 0.0001);
		Assert.AreEqual(first.MeanTightestGap, again.MeanTightestGap, 0.0001);
		Assert.AreEqual(first.SettledStarts, again.SettledStarts);
	}

	[TestMethod]
	public void Sweep_MoreRepulsion_LeavesMoreRoom()
	{
		// The worked example from this class's remarks, kept as a test so it cannot rot. It is also the
		// harness's own sanity check: if turning a force up did not move the column it governs, the
		// measurement is not measuring it.
		BenchOptions quick = new(Starts: 4, Frames: 2000, ReadableAfter: 600);

		IReadOnlyList<BenchResult> rows = LayoutBench.Sweep(
			GraphCorpus.Counter,
			LayoutSettings.Defaults,
			"repulsion",
			[150_000.0, 600_000.0, 2_400_000.0],
			(s, v) => s with { RepulsionStrength = v },
			quick);

		Console.WriteLine(LayoutBench.Table(rows));

		Assert.IsLessThan(rows[1].MeanTightestGap, rows[0].MeanTightestGap,
			$"more repulsion should leave the closest pair more room; {rows[0].MeanTightestGap:F1} then {rows[1].MeanTightestGap:F1}");
		Assert.IsLessThan(rows[2].MeanTightestGap, rows[1].MeanTightestGap,
			$"and more again; {rows[1].MeanTightestGap:F1} then {rows[2].MeanTightestGap:F1}");
		Assert.IsLessThan(rows[2].MeanArea, rows[0].MeanArea,
			$"and the graph should end up larger overall; {rows[0].MeanArea:F0} against {rows[2].MeanArea:F0}");
	}

	[TestMethod]
	public void Compare_PutsNamedVariantsSideBySide()
	{
		BenchOptions quick = new(Starts: 3, Frames: 1200, ReadableAfter: 600);

		LayoutSettings withRepulsion = LayoutSettings.Defaults;
		LayoutSettings without = LayoutSettings.Defaults;
		without.RepulsionStrength = 0;

		IReadOnlyList<BenchResult> rows = LayoutBench.Compare(
			GraphCorpus.Counter,
			quick,
			("with", withRepulsion),
			("without", without));

		string table = LayoutBench.Table(rows);
		Console.WriteLine(table);

		Assert.IsTrue(table.Contains("with", StringComparison.Ordinal), "the table should name each variant");
		Assert.IsTrue(table.Contains("tightGap", StringComparison.Ordinal), "and carry a header row");
		Assert.IsGreaterThan(rows[1].MeanTightestGap, rows[0].MeanTightestGap,
			"and repulsion should be what leaves the closest pair its room");
	}

	[TestMethod]
	public void Svg_DrawsEveryNodeAndLink_AndCallsOutOverlaps()
	{
		LayoutCore core = Arrange((1, 0, 0, 100, 50), (2, 60, 20, 100, 50));
		core.ResizeEdges(1);
		core.Edges[0] = new EdgeRef
		{
			SourceIndex = 0,
			TargetIndex = 1,
			SourcePinOffset = new Vec2D(100, 25),
			TargetPinOffset = new Vec2D(0, 25),
			HasPinOffsets = 1,
		};

		string svg = LayoutSvg.Render(core, "a caption");

		Assert.IsTrue(svg.StartsWith("<svg", StringComparison.Ordinal), "it should be an svg document");
		Assert.IsTrue(svg.EndsWith("</svg>", StringComparison.Ordinal), "and a closed one");
		Assert.AreEqual(3, CountOf(svg, "<rect"), "a background plus one rect per node");
		Assert.AreEqual(1, CountOf(svg, "<path"), "and one path per link");
		Assert.IsTrue(svg.Contains("#e06c75", StringComparison.Ordinal), "the overlapping pair should be called out in red");
		Assert.IsTrue(svg.Contains("a caption", StringComparison.Ordinal), "and the caption drawn");
	}

	[TestMethod]
	public void Svg_UsesTheInvariantCultureForCoordinates()
	{
		// A comma decimal separator would produce coordinates no renderer can parse, and the failure
		// would only show on a machine whose culture happens to use one.
		LayoutCore core = Arrange((1, 10.5, 20.25, 100, 50));
		string svg = LayoutSvg.Render(core);

		Assert.IsTrue(svg.Contains("x=\"10.5\"", StringComparison.Ordinal), $"coordinates should use a point; got {svg}");
	}

	/// <summary>
	/// What each corpus graph is expected to settle into under the defaults.
	/// </summary>
	/// <param name="Graph">The graph.</param>
	/// <param name="MaxEdgeAngle">Mean angle off horizontal it must stay under.</param>
	/// <param name="MinReadableStarts">How many starts must read left-to-right within ten seconds.</param>
	private sealed record Expectation(BenchGraph Graph, double MaxEdgeAngle, int MinReadableStarts);

	/// <summary>
	/// The quality gate: every graph in the corpus settles into something a reader could follow.
	/// </summary>
	/// <remarks>
	/// This is the test a layout change is expected to break if it makes things worse, and the reason
	/// the corpus has four shapes rather than one — a change that helps a wide fan-in can hurt a deep
	/// chain, and only running both says so. The thresholds are current behaviour with headroom, not
	/// targets: they are here to catch a regression, not to pin the numbers a particular tuning
	/// happens to produce.
	/// <para>
	/// Two of them are loose for a reason worth knowing, because it is a real defect and not a quirk
	/// of the measurement. <see cref="GraphCorpus.Chain"/> is a plain twelve-node chain, the shape that
	/// most obviously wants to be a horizontal row, and it settles at about 53 degrees with only two
	/// starts in six reading left to right. It is not a question of settling time — 4000, 12000 and
	/// 30000 frames all land on the same 52.6 — it is gravity. Sweeping
	/// <see cref="LayoutSettings.GravityStrength"/> over the same six starts:
	/// </para>
	/// <code>
	/// gravity=0      angle  0.6   readable 5/6
	/// gravity=10     angle  1.9   readable 5/6
	/// gravity=50     angle 52.9   readable 2/6      (the default)
	/// gravity=200    angle 58.4   readable 0/6
	/// </code>
	/// <para>
	/// Pulling every body towards one centre folds a long chain into a coil, and the directional bias
	/// that is supposed to order it left-to-right does not undo that — raising the bias makes it worse
	/// (62.7 degrees at bias 2), because ordering the pairs says nothing about the shape of the whole.
	/// <see cref="GraphCorpus.MixedSizes"/> is a chain too and coils the same way. Fixing it is a
	/// change to gravity, not to this gate, so the thresholds record where it stands.
	/// </para>
	/// </remarks>
	[TestMethod]
	public void Corpus_SettlesIntoAReadableShape_UnderTheDefaults()
	{
		BenchOptions shape = new(Starts: 6, Frames: 4000, ReadableAfter: 600);

		Expectation[] expectations =
		[
			new(GraphCorpus.Counter, MaxEdgeAngle: 40.0, MinReadableStarts: 5),
			new(GraphCorpus.Chain, MaxEdgeAngle: 60.0, MinReadableStarts: 1),
			new(GraphCorpus.FanIn, MaxEdgeAngle: 45.0, MinReadableStarts: 4),
			new(GraphCorpus.MixedSizes, MaxEdgeAngle: 58.0, MinReadableStarts: 1),
		];

		List<BenchResult> rows = [];
		foreach (Expectation expectation in expectations)
		{
			rows.Add(LayoutBench.Run(expectation.Graph, LayoutSettings.Defaults, expectation.Graph.Name, shape));
		}

		Console.WriteLine(LayoutBench.Table(rows));

		for (int i = 0; i < rows.Count; i++)
		{
			BenchResult row = rows[i];
			Expectation expectation = expectations[i];
			string graph = expectation.Graph.Name;

			// These two hold for every shape: a settled graph never leaves bodies drawn over one
			// another, and never leaves a pair with no room between them.
			Assert.AreEqual(0.0, row.WorstOverlap, 0.5,
				$"{graph}: no start should be left with bodies drawn over one another");
			Assert.IsGreaterThan(10.0, row.MeanTightestGap,
				$"{graph}: the closest pair should have real room between them; it had {row.MeanTightestGap:F1}");

			Assert.IsLessThan(expectation.MaxEdgeAngle, row.MeanEdgeAngle,
				$"{graph}: mean edge angle should stay under {expectation.MaxEdgeAngle:F0} degrees; it was {row.MeanEdgeAngle:F1}");
			Assert.IsGreaterThanOrEqualTo(expectation.MinReadableStarts, row.ReadableStarts,
				$"{graph}: at least {expectation.MinReadableStarts} of {row.Starts} starts should read left-to-right " +
				$"within ten seconds; {row.ReadableStarts} did");
		}
	}

	/// <summary>Counts non-overlapping occurrences of a token.</summary>
	/// <param name="text">Text to search.</param>
	/// <param name="token">Token to count.</param>
	private static int CountOf(string text, string token)
	{
		int count = 0;
		int at = text.IndexOf(token, StringComparison.Ordinal);
		while (at >= 0)
		{
			count++;
			at = text.IndexOf(token, at + token.Length, StringComparison.Ordinal);
		}

		return count;
	}
}
