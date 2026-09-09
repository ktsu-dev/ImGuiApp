// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ForceDirectedLayout.Tests.Bench;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using ktsu.ForceDirectedLayout;

/// <summary>One setting the tuner can move, and the values it is allowed to try.</summary>
/// <param name="Name">What to call it in a report.</param>
/// <param name="Candidates">The values to try, in ascending order.</param>
/// <param name="Read">Reads the setting's current value.</param>
/// <param name="Write">Returns a copy of the settings with the value applied.</param>
public sealed record TunableSetting(
	string Name,
	IReadOnlyList<double> Candidates,
	Func<LayoutSettings, double> Read,
	Func<LayoutSettings, double, LayoutSettings> Write);

/// <summary>What one setting's sweep found.</summary>
/// <param name="Name">The setting swept.</param>
/// <param name="From">Its value before the sweep.</param>
/// <param name="To">The value that scored best.</param>
/// <param name="ScoreBefore">The corpus score before.</param>
/// <param name="ScoreAfter">The corpus score at the chosen value.</param>
/// <param name="Rows">Every candidate tried, with its score.</param>
public sealed record TuningStep(
	string Name,
	double From,
	double To,
	double ScoreBefore,
	double ScoreAfter,
	IReadOnlyList<(double Value, LayoutScore Score)> Rows)
{
	/// <summary>How much the corpus score improved, positive being better.</summary>
	public double Gain => ScoreBefore - ScoreAfter;
}

/// <summary>
/// Tunes settings one at a time against <see cref="LayoutScore"/>, by coordinate descent.
/// </summary>
/// <remarks>
/// Each pass sweeps one setting across its candidates with everything else held where it is, keeps
/// whichever value scored best, and moves to the next. Passes repeat until a whole round changes
/// nothing worth having, because the settings interact: the best repulsion depends on the gravity
/// it is balanced against, so a value chosen in the first round is worth revisiting once the rest
/// have moved.
/// <para>
/// What this is not: a search of the whole space. Coordinate descent finds a point no single change
/// improves, which is not the same as the best point — two settings that would only help if moved
/// together will not be found. It is chosen anyway because its output is legible: every step is one
/// setting, one sweep and one reason, which is what makes a tuned default arguable rather than
/// merely asserted.
/// </para>
/// <para>
/// A candidate list always includes the value the setting starts at, so a pass can decline to move.
/// Ties go to the incumbent, so a change has to actually earn its place.
/// </para>
/// </remarks>
public static class LayoutTuner
{
	/// <summary>Score improvement below which a round counts as having found nothing.</summary>
	public const double NegligibleGain = 0.005;

	/// <summary>
	/// Default improvement a single change has to beat before it is kept, being three times the
	/// corpus score's own standard deviation at forty-eight starts.
	/// </summary>
	/// <remarks>
	/// Not a nicety, and the single thing that decides whether a tuning run means anything. The corpus
	/// score is itself a random variable: measured eight times over independent families of starting
	/// arrangements, the same settings scored with a standard deviation of 0.317 at twelve starts,
	/// 0.096 at twenty-four and 0.032 at forty-eight. So a run of twelve starts cannot see a change
	/// worth less than about a whole point, and a descent that keeps every improvement will spend
	/// almost all of its steps fitting the arrangements it happened to be given.
	/// <para>
	/// That is not hypothetical: two descents at eight and twelve starts with no floor reached values
	/// disagreeing on six of the fifteen settings and scored within 0.01 of each other. Both were
	/// noise. Raise <see cref="BenchOptions.Starts"/> until the deviation is small against the gains
	/// being claimed, set this to a few times that deviation, and a kept change is a real one.
	/// </para>
	/// </remarks>
	public const double DefaultMinimumGain = 0.1;

	/// <summary>
	/// The settings worth tuning, with the values each is allowed to take.
	/// </summary>
	/// <remarks>
	/// <see cref="LayoutSettings.StabilityThreshold"/> and <see cref="LayoutSettings.TargetPhysicsHz"/>
	/// are deliberately absent. The first defines the settled metric the score rewards, so tuning it
	/// against that score is circular — raise it far enough and every graph "settles" without a node
	/// moving. The second buys integration accuracy with CPU time, which is a performance decision
	/// rather than a layout one.
	/// </remarks>
	public static IReadOnlyList<TunableSetting> Settings { get; } =
	[
		new("RepulsionStrength", [150_000, 300_000, 450_000, 600_000, 900_000, 1_200_000, 2_000_000],
			s => s.RepulsionStrength, (s, v) => s with { RepulsionStrength = v }),
		new("MinRepulsionDistance", [0, 2, 5, 10, 25, 50, 75, 100, 150],
			s => s.MinRepulsionDistance, (s, v) => s with { MinRepulsionDistance = v }),
		new("LinkSpringStrength", [0.1, 0.25, 0.5, 0.75, 1.0, 2.0],
			s => s.LinkSpringStrength, (s, v) => s with { LinkSpringStrength = v }),
		new("RestLinkLength", [50, 75, 100, 150, 225, 300, 400, 500],
			s => s.RestLinkLength, (s, v) => s with { RestLinkLength = v }),
		new("DirectionalBias", [0, 0.25, 0.5, 1.0, 2.0, 4.0, 8.0, 16.0],
			s => s.DirectionalBias, (s, v) => s with { DirectionalBias = v }),
		new("LinkFlatteningStrength", [0, 0.25, 0.5, 1.0, 2.0, 3.0, 4.0, 6.0, 8.0, 12.0],
			s => s.LinkFlatteningStrength, (s, v) => s with { LinkFlatteningStrength = v }),
		new("LinkFlatteningMargin", [0, 10, 25, 50, 100],
			s => s.LinkFlatteningMargin, (s, v) => s with { LinkFlatteningMargin = v }),
		new("LinkUntwistStrength", [0, 0.05, 0.1, 0.25, 0.5, 1.0],
			s => s.LinkUntwistStrength, (s, v) => s with { LinkUntwistStrength = v }),
		new("GravityStrength", [0, 5, 10, 25, 50, 100, 200],
			s => s.GravityStrength, (s, v) => s with { GravityStrength = v }),
		new("OriginAnchorWeight", [0, 0.25, 0.5, 0.75, 1.0],
			s => s.OriginAnchorWeight, (s, v) => s with { OriginAnchorWeight = v }),
		new("DampingFactor", [0.1, 0.25, 0.5, 0.75, 0.9],
			s => s.DampingFactor, (s, v) => s with { DampingFactor = v }),
		new("MaxForce", [1_000, 2_500, 5_000, 10_000, 20_000],
			s => s.MaxForce, (s, v) => s with { MaxForce = v }),
		new("MaxVelocity", [100, 250, 500, 1_000],
			s => s.MaxVelocity, (s, v) => s with { MaxVelocity = v }),
		new("OverlapMargin", [5, 10, 20, 40, 80],
			s => s.OverlapMargin, (s, v) => s with { OverlapMargin = v }),
		new("MaxOverlapCorrection", [10, 20, 40, 80],
			s => s.MaxOverlapCorrection, (s, v) => s with { MaxOverlapCorrection = v }),
	];

	/// <summary>
	/// Sweeps one setting across its candidates, holding the rest, and reports what each scored.
	/// </summary>
	/// <remarks>
	/// The candidates are scored in parallel, which is safe and still deterministic: each score
	/// settles its own cores from its own seeds and writes to its own slot, so the rows come back in
	/// candidate order and read the same however many threads ran them.
	/// </remarks>
	/// <param name="setting">The setting to sweep.</param>
	/// <param name="from">The settings to vary from.</param>
	/// <param name="options">Run shape, or <see langword="null"/> for <see cref="BenchOptions.Default"/>.</param>
	/// <param name="graphs">The graphs to settle, or <see langword="null"/> for the whole corpus.</param>
	public static TuningStep Sweep(
		TunableSetting setting,
		LayoutSettings from,
		BenchOptions? options = null,
		IReadOnlyList<BenchGraph>? graphs = null)
	{
		ArgumentNullException.ThrowIfNull(setting);

		double incumbent = setting.Read(from);
		LayoutScore[] scores = new LayoutScore[setting.Candidates.Count];

		Parallel.For(0, setting.Candidates.Count, i =>
			scores[i] = LayoutScore.OfCorpus(setting.Write(from, setting.Candidates[i]), options, graphs));

		List<(double Value, LayoutScore Score)> rows = [];

		double bestValue = incumbent;
		double bestScore = double.MaxValue;
		double incumbentScore = double.MaxValue;

		for (int i = 0; i < setting.Candidates.Count; i++)
		{
			double candidate = setting.Candidates[i];
			LayoutScore score = scores[i];
			rows.Add((candidate, score));

			if (candidate == incumbent)
			{
				incumbentScore = score.Total;
			}

			// Strictly better only, so a tie leaves the incumbent alone.
			if (score.Total < bestScore)
			{
				bestScore = score.Total;
				bestValue = candidate;
			}
		}

		// A candidate list that omitted the incumbent still needs a before-score to compare against.
		if (incumbentScore == double.MaxValue)
		{
			incumbentScore = LayoutScore.OfCorpus(from, options, graphs).Total;
		}

		return new TuningStep(setting.Name, incumbent, bestValue, incumbentScore, bestScore, rows);
	}

	/// <summary>
	/// Runs coordinate descent over every setting until a round stops finding anything.
	/// </summary>
	/// <param name="from">The settings to start from.</param>
	/// <param name="maxRounds">Cap on passes over the whole setting list.</param>
	/// <param name="options">Run shape, or <see langword="null"/> for <see cref="BenchOptions.Default"/>.</param>
	/// <param name="log">Called with each step as it completes, for progress.</param>
	/// <returns>The tuned settings and every step taken, in order.</returns>
	/// <param name="minimumGain">
	/// How much better a value has to score than the incumbent before it is kept. Defaults to
	/// <see cref="DefaultMinimumGain"/>; pass zero to keep every improvement, which will fit noise.
	/// </param>
	public static (LayoutSettings Tuned, IReadOnlyList<TuningStep> Steps) Descend(
		LayoutSettings from,
		int maxRounds = 4,
		BenchOptions? options = null,
		Action<int, TuningStep>? log = null,
		double minimumGain = DefaultMinimumGain)
	{
		LayoutSettings current = from;
		current.Enabled = 1;
		List<TuningStep> steps = [];

		for (int round = 0; round < maxRounds; round++)
		{
			double roundGain = 0;

			foreach (TunableSetting setting in Settings)
			{
				TuningStep step = Sweep(setting, current, options);
				steps.Add(step);
				log?.Invoke(round, step);

				if (step.Gain > minimumGain)
				{
					current = setting.Write(current, step.To);
					roundGain += step.Gain;
				}
			}

			if (roundGain < NegligibleGain)
			{
				break;
			}
		}

		return (current, steps);
	}

	/// <summary>Renders one sweep as a table, so the shape of the curve is visible and not just its minimum.</summary>
	/// <param name="step">The step to render.</param>
	public static string Table(TuningStep step)
	{
		ArgumentNullException.ThrowIfNull(step);

		StringBuilder text = new();
		text.Append(string.Create(CultureInfo.InvariantCulture,
			$"{step.Name}: {step.From:G6} -> {step.To:G6}  (score {step.ScoreBefore:F3} -> {step.ScoreAfter:F3}, gain {step.Gain:+0.000;-0.000;0.000})"));
		text.AppendLine();

		foreach ((double value, LayoutScore score) in step.Rows)
		{
			string marker = value == step.To ? " <-" : (value == step.From ? " ." : "   ");
			text.AppendLine(string.Create(CultureInfo.InvariantCulture,
				$"    {value,12:G6}  {score.Breakdown()}{marker}"));
		}

		return text.ToString();
	}
}
