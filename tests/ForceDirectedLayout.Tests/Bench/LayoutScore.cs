// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ForceDirectedLayout.Tests.Bench;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ktsu.ForceDirectedLayout;

/// <summary>
/// One number for how good a settled layout is, so that settings can be compared and tuned.
/// </summary>
/// <remarks>
/// <see cref="LayoutMetrics"/> deliberately reports a row rather than a verdict, because no single
/// number is the answer. Tuning needs one anyway: you cannot ask which of two values is better
/// without saying what better means. So this is the verdict, and it is a judgement call written
/// down rather than a fact — the weights below are the tradeoff, and anyone who disagrees with a
/// tuned default should look here first.
/// <para>
/// Every term is a penalty normalised so that roughly 1.0 is as bad as that aspect plausibly gets,
/// and the total is their weighted sum. Lower is better; zero is a graph with no overlaps, no hidden
/// links, perfectly horizontal edges, no crossings, readable and settled from every start, real room
/// between its closest pair, and no bigger than its nodes need.
/// </para>
/// <para>
/// What the weights say, in order of how much they are trusted:
/// </para>
/// <list type="bullet">
/// <item><b>Overlap (4)</b> — two nodes drawn on top of each other is the one defect a user sees
/// instantly and cannot work around. It should always be zero, so it is weighted to dominate.</item>
/// <item><b>Hidden links (2) and unreadable starts (2)</b> — a link that disappears behind a node,
/// and a graph that does not read left to right within ten seconds, are both failures of the thing
/// the layout is for.</item>
/// <item><b>Edge angle (2)</b> — the same concern measured continuously rather than as a threshold,
/// so the tuner can see progress before a start flips to readable. It overlaps with the term above
/// on purpose; between them they are the largest share of the total.</item>
/// <item><b>Crossings (1), unsettled starts (1), crowding (1), sprawl (1)</b> — real costs, but ones
/// a reader can work around, or that trade against each other.</item>
/// </list>
/// <para>
/// Two cautions worth keeping in view. The corpus is five graphs and three of them are chains, so
/// chain-shaped documents carry more of the total than their share of real documents might justify.
/// And <see cref="LayoutSettings.StabilityThreshold"/> defines the settled metric while
/// <see cref="LayoutSettings.TargetPhysicsHz"/> buys accuracy with CPU, so neither is a fair thing
/// to tune against this score — raising the first makes everything "settle" without moving a node.
/// </para>
/// </remarks>
/// <param name="Total">The weighted sum. Lower is better.</param>
/// <param name="Overlap">Bodies drawn over one another.</param>
/// <param name="HiddenLinks">Links drawn across a body they are no end of.</param>
/// <param name="EdgeAngle">How far the edges sit from horizontal.</param>
/// <param name="Crossings">Pairs of links that cross where they meet at a node.</param>
/// <param name="Unreadable">Starts that did not read left-to-right within the readable window.</param>
/// <param name="Unsettled">Starts that had not come to rest.</param>
/// <param name="Crowding">How far the closest pair is inside the room a reader wants.</param>
/// <param name="Sprawl">How much larger the graph is than its nodes need.</param>
public readonly record struct LayoutScore(
	double Total,
	double Overlap,
	double HiddenLinks,
	double EdgeAngle,
	double Crossings,
	double Unreadable,
	double Unsettled,
	double Crowding,
	double Sprawl)
{
	/// <summary>Overlap depth, in position units, treated as an entirely failed layout.</summary>
	private const double RuinousOverlap = 20.0;

	/// <summary>Clear space the closest pair wants; below this the pair reads as crowded.</summary>
	private const double ComfortableGap = 40.0;

	/// <summary>
	/// Settled area a graph is allowed per unit of node area before it counts as sprawling.
	/// </summary>
	/// <remarks>
	/// Measured across the corpus under the defaults this change started from, the settled area ran
	/// between five and twelve times the total area of the nodes, averaging near eight. Eight is
	/// therefore "the size this graph already was", which is what the term is there to hold: a layout
	/// may grow if the room buys something, and pays for growth that buys nothing.
	/// </remarks>
	private const double NaturalAreaPerNodeArea = 8.0;

	/// <summary>Weight on bodies drawn over one another.</summary>
	private const double OverlapWeight = 4.0;

	/// <summary>Weight on links drawn across an unrelated body.</summary>
	private const double HiddenLinkWeight = 2.0;

	/// <summary>Weight on how far edges sit from horizontal.</summary>
	private const double EdgeAngleWeight = 2.0;

	/// <summary>Weight on crossed link pairs.</summary>
	private const double CrossingWeight = 1.0;

	/// <summary>Weight on starts that did not read left-to-right in time.</summary>
	private const double UnreadableWeight = 2.0;

	/// <summary>Weight on starts that had not come to rest.</summary>
	private const double UnsettledWeight = 1.0;

	/// <summary>Weight on the closest pair being crowded.</summary>
	private const double CrowdingWeight = 1.0;

	/// <summary>Weight on the graph being larger than its nodes need.</summary>
	private const double SprawlWeight = 1.0;

	/// <summary>Scores one graph's result.</summary>
	/// <param name="graph">The graph that was settled, for its edge count and node areas.</param>
	/// <param name="result">What settling it measured.</param>
	public static LayoutScore Of(BenchGraph graph, BenchResult result)
	{
		ArgumentNullException.ThrowIfNull(graph);
		ArgumentNullException.ThrowIfNull(result);

		double edges = Math.Max(1, graph.Edges.Count);
		double naturalArea = graph.Nodes.Sum(n => n.Width * n.Height) * NaturalAreaPerNodeArea;

		double overlap = Math.Min(1.0, result.WorstOverlap / RuinousOverlap);
		double hidden = Math.Min(1.0, result.MeanLinksOverBodies / edges);
		double angle = Math.Min(1.0, result.MeanEdgeAngle / 90.0);
		double crossings = Math.Min(1.0, result.MeanTwistedPairs / edges);
		double unreadable = 1.0 - ((double)result.ReadableStarts / result.Starts);
		double unsettled = 1.0 - ((double)result.SettledStarts / result.Starts);
		double crowding = Math.Max(0.0, 1.0 - (result.MeanTightestGap / ComfortableGap));

		// Capped, so one runaway graph cannot swamp the corpus - a layout that flew apart is simply
		// "as bad as sprawl gets" rather than a number in the millions.
		double sprawl = Math.Min(2.0, Math.Max(0.0, (result.MeanArea / naturalArea) - 1.0));

		double total =
			(overlap * OverlapWeight) +
			(hidden * HiddenLinkWeight) +
			(angle * EdgeAngleWeight) +
			(crossings * CrossingWeight) +
			(unreadable * UnreadableWeight) +
			(unsettled * UnsettledWeight) +
			(crowding * CrowdingWeight) +
			(sprawl * SprawlWeight);

		return new LayoutScore(total, overlap, hidden, angle, crossings, unreadable, unsettled, crowding, sprawl);
	}

	/// <summary>
	/// Scores a whole corpus under one set of settings, as the mean of its graphs' scores.
	/// </summary>
	/// <param name="settings">The settings to measure.</param>
	/// <param name="options">Run shape, or <see langword="null"/> for <see cref="BenchOptions.Default"/>.</param>
	/// <param name="graphs">The graphs to settle, or <see langword="null"/> for the whole corpus.</param>
	public static LayoutScore OfCorpus(LayoutSettings settings, BenchOptions? options = null, IReadOnlyList<BenchGraph>? graphs = null)
	{
		IReadOnlyList<BenchGraph> corpus = graphs ?? GraphCorpus.All;
		List<LayoutScore> scores = [];

		foreach (BenchGraph graph in corpus)
		{
			scores.Add(Of(graph, LayoutBench.Run(graph, settings, graph.Name, options)));
		}

		return new LayoutScore(
			scores.Average(s => s.Total),
			scores.Average(s => s.Overlap),
			scores.Average(s => s.HiddenLinks),
			scores.Average(s => s.EdgeAngle),
			scores.Average(s => s.Crossings),
			scores.Average(s => s.Unreadable),
			scores.Average(s => s.Unsettled),
			scores.Average(s => s.Crowding),
			scores.Average(s => s.Sprawl));
	}

	/// <summary>The term-by-term breakdown, so a total can be argued with rather than trusted.</summary>
	public readonly string Breakdown() => string.Create(
		CultureInfo.InvariantCulture,
		$"total {Total:F3}  overlap {Overlap:F2}  hidden {HiddenLinks:F2}  angle {EdgeAngle:F2}  " +
		$"cross {Crossings:F2}  unread {Unreadable:F2}  unsettled {Unsettled:F2}  crowd {Crowding:F2}  sprawl {Sprawl:F2}");
}
