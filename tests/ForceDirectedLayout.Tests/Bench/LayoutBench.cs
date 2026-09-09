// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ForceDirectedLayout.Tests.Bench;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ktsu.ForceDirectedLayout;

/// <summary>How a benchmark run is set up.</summary>
/// <param name="Starts">
/// How many starting arrangements to settle. One is not a measurement: the simulation is chaotic, and
/// a single start will happily report a change as an improvement one run and a regression the next.
/// </param>
/// <param name="Frames">Frames to settle for, at <paramref name="FrameDelta"/> each.</param>
/// <param name="ReadableAfter">
/// Frame at which to ask whether the graph is readable yet. Settling eventually is not enough — an
/// editor's user watches it unfold, and ten seconds is about as long as anyone waits.
/// </param>
/// <param name="FrameDelta">Simulated seconds per frame.</param>
public sealed record BenchOptions(
	int Starts = 10,
	int Frames = 6000,
	int ReadableAfter = 600,
	double FrameDelta = 1.0 / 60.0)
{
	/// <summary>The defaults, which are what every comparison in the tests uses.</summary>
	public static BenchOptions Default { get; } = new();
}

/// <summary>What a benchmark run found, aggregated over its starting arrangements.</summary>
/// <param name="Label">What this run is called in a table.</param>
/// <param name="Starts">How many arrangements were settled.</param>
/// <param name="MeanArea">Mean bounding-box area.</param>
/// <param name="MeanEdgeAngle">Mean angle off horizontal across every edge of every start.</param>
/// <param name="MeanLinksOverBodies">Mean count of links drawn across a node they are no end of.</param>
/// <param name="MeanTightestGap">Mean, over starts, of each start's closest pair.</param>
/// <param name="MeanClearGap">Mean clear space over every pair of every start.</param>
/// <param name="WorstOverlap">Deepest overlap seen in any start. Anything above zero is a defect.</param>
/// <param name="MeanTwistedPairs">Mean count of crossed link pairs.</param>
/// <param name="SettledStarts">How many starts reported themselves stable.</param>
/// <param name="ReadableStarts">How many were already readable at <see cref="BenchOptions.ReadableAfter"/>.</param>
/// <param name="PerStart">Each start's own metrics, for a caller that wants the spread and not the mean.</param>
public sealed record BenchResult(
	string Label,
	int Starts,
	double MeanArea,
	double MeanEdgeAngle,
	double MeanLinksOverBodies,
	double MeanTightestGap,
	double MeanClearGap,
	double WorstOverlap,
	double MeanTwistedPairs,
	int SettledStarts,
	int ReadableStarts,
	IReadOnlyList<LayoutMetrics> PerStart);

/// <summary>
/// Settles a benchmark graph over several starting arrangements and reports what it measures.
/// </summary>
/// <remarks>
/// This exists because the obvious way to judge a layout change — run the graph once, look at the
/// number — does not work. The simulation is chaotic: which local minimum one start happens to fall
/// into says nothing about the change that was made, and a single-start assertion will flip between
/// passing and failing across parameter values that are all perfectly reasonable. Settling the same
/// claim over a range of starts, from nodes piled on top of each other to nodes flung far apart, is
/// what turns a number into evidence.
/// <para>
/// Use <see cref="Run"/> to measure one configuration, <see cref="Sweep"/> to walk one setting across
/// a range, and <see cref="Compare"/> to put named variants side by side. All three return rows that
/// <see cref="Table"/> renders, so iterating on a force is: change it, run a scratch test method that
/// prints a sweep, and read the column that should have moved.
/// </para>
/// </remarks>
public static class LayoutBench
{
	/// <summary>Narrowest scatter to start from: every node piled almost on top of the others.</summary>
	private const double TightestSpread = 0.05;

	/// <summary>Widest scatter to start from: nodes flung across a thousand units.</summary>
	private const double WidestSpread = 1.0;

	/// <summary>Settles one configuration over <see cref="BenchOptions.Starts"/> arrangements.</summary>
	/// <param name="graph">The graph to settle.</param>
	/// <param name="settings">The settings to settle it under.</param>
	/// <param name="label">What to call this run in a table.</param>
	/// <param name="options">Run shape, or <see langword="null"/> for <see cref="BenchOptions.Default"/>.</param>
	public static BenchResult Run(BenchGraph graph, LayoutSettings settings, string label, BenchOptions? options = null)
	{
		ArgumentNullException.ThrowIfNull(graph);

		BenchOptions shape = options ?? BenchOptions.Default;
		List<LayoutMetrics> perStart = [];
		int readable = 0;

		for (int start = 0; start < shape.Starts; start++)
		{
			LayoutCore core = graph.Start(settings, SeedFor(start), SpreadFor(start, shape.Starts));

			for (int frame = 0; frame < shape.ReadableAfter && frame < shape.Frames; frame++)
			{
				core.Step(shape.FrameDelta);
			}

			if (LayoutMetrics.Measure(core).Readable)
			{
				readable++;
			}

			for (int frame = shape.ReadableAfter; frame < shape.Frames; frame++)
			{
				core.Step(shape.FrameDelta);
			}

			perStart.Add(LayoutMetrics.Measure(core));
		}

		return new BenchResult(
			label,
			perStart.Count,
			perStart.Average(m => m.Area),
			perStart.Average(m => m.MeanEdgeAngle),
			perStart.Average(m => (double)m.LinksOverBodies),
			perStart.Average(m => m.TightestClearGap),
			perStart.Average(m => m.MeanClearGap),
			perStart.Max(m => m.WorstOverlap),
			perStart.Average(m => (double)m.TwistedPairs),
			perStart.Count(m => m.Settled),
			readable,
			perStart);
	}

	/// <summary>
	/// Walks one setting across a range of values, holding everything else where it is.
	/// </summary>
	/// <remarks>
	/// This is how a default gets chosen. Changing what a force measures changes the units its
	/// strength is in, so the old value is meaningless and the new one has to be found: sweep it,
	/// and read off where the settled graph has the room it used to have.
	/// </remarks>
	/// <param name="graph">The graph to settle.</param>
	/// <param name="baseline">Settings to vary from.</param>
	/// <param name="setting">Name of the setting being swept, for the row labels.</param>
	/// <param name="values">The values to try.</param>
	/// <param name="apply">Writes one value into a copy of the settings.</param>
	/// <param name="options">Run shape, or <see langword="null"/> for <see cref="BenchOptions.Default"/>.</param>
	public static IReadOnlyList<BenchResult> Sweep(
		BenchGraph graph,
		LayoutSettings baseline,
		string setting,
		IReadOnlyList<double> values,
		Func<LayoutSettings, double, LayoutSettings> apply,
		BenchOptions? options = null)
	{
		ArgumentNullException.ThrowIfNull(values);
		ArgumentNullException.ThrowIfNull(apply);

		List<BenchResult> rows = [];
		foreach (double value in values)
		{
			string label = $"{setting}={value.ToString("G6", CultureInfo.InvariantCulture)}";
			rows.Add(Run(graph, apply(baseline, value), label, options));
		}

		return rows;
	}

	/// <summary>Settles several named configurations of the same graph, for a side-by-side read.</summary>
	/// <param name="graph">The graph to settle.</param>
	/// <param name="options">Run shape, or <see langword="null"/> for <see cref="BenchOptions.Default"/>.</param>
	/// <param name="variants">Each variant's label and settings.</param>
	public static IReadOnlyList<BenchResult> Compare(
		BenchGraph graph,
		BenchOptions? options,
		params (string Label, LayoutSettings Settings)[] variants)
	{
		ArgumentNullException.ThrowIfNull(variants);

		return [.. variants.Select(v => Run(graph, v.Settings, v.Label, options))];
	}

	/// <summary>Renders rows as a fixed-width table, for printing from a test.</summary>
	/// <param name="rows">The rows to render, in the order they should appear.</param>
	public static string Table(IEnumerable<BenchResult> rows)
	{
		ArgumentNullException.ThrowIfNull(rows);

		List<BenchResult> ordered = [.. rows];
		int labelWidth = Math.Max(8, ordered.Count == 0 ? 8 : ordered.Max(r => r.Label.Length));

		System.Text.StringBuilder text = new();
		text.Append(Cell("variant", labelWidth))
			.Append(Cell("area", 12))
			.Append(Cell("angle", 7))
			.Append(Cell("overBody", 9))
			.Append(Cell("tightGap", 9))
			.Append(Cell("meanGap", 9))
			.Append(Cell("overlap", 8))
			.Append(Cell("twisted", 8))
			.Append(Cell("settled", 8))
			.AppendLine(Cell("readable", 9));

		foreach (BenchResult row in ordered)
		{
			text.Append(Cell(row.Label, labelWidth))
				.Append(Cell(row.MeanArea.ToString("F0", CultureInfo.InvariantCulture), 12))
				.Append(Cell(row.MeanEdgeAngle.ToString("F1", CultureInfo.InvariantCulture), 7))
				.Append(Cell(row.MeanLinksOverBodies.ToString("F1", CultureInfo.InvariantCulture), 9))
				.Append(Cell(row.MeanTightestGap.ToString("F1", CultureInfo.InvariantCulture), 9))
				.Append(Cell(row.MeanClearGap.ToString("F1", CultureInfo.InvariantCulture), 9))
				.Append(Cell(row.WorstOverlap.ToString("F1", CultureInfo.InvariantCulture), 8))
				.Append(Cell(row.MeanTwistedPairs.ToString("F1", CultureInfo.InvariantCulture), 8))
				.Append(Cell($"{row.SettledStarts}/{row.Starts}", 8))
				.AppendLine(Cell($"{row.ReadableStarts}/{row.Starts}", 9));
		}

		return text.ToString();
	}

	/// <summary>One padded table cell.</summary>
	/// <param name="text">Cell contents.</param>
	/// <param name="width">Column width, including the separating space.</param>
	private static string Cell(string text, int width) => text.PadLeft(width) + "  ";

	/// <summary>
	/// The seed for one start. Fixed multiples rather than a sequence, so adding a start to a run does
	/// not renumber the ones already measured.
	/// </summary>
	/// <param name="start">Index of the start.</param>
	private static int SeedFor(int start) => (start * 7919) + 1;

	/// <summary>
	/// The scatter reach for one start, walked from tightest to widest across the run.
	/// </summary>
	/// <remarks>
	/// Both ends matter and they fail differently. A tight scatter starts every node overlapping, so
	/// it exercises the floor on repulsion and the overlap pass; a wide one starts them further apart
	/// than any force wants them, so it exercises how fast the graph can actually travel.
	/// </remarks>
	/// <param name="start">Index of the start.</param>
	/// <param name="starts">How many starts the run has.</param>
	private static double SpreadFor(int start, int starts) =>
		starts <= 1
			? TightestSpread
			: TightestSpread + ((WidestSpread - TightestSpread) * start / (starts - 1));
}
