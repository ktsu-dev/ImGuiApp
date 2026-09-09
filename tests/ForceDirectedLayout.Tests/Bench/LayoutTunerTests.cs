// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ForceDirectedLayout.Tests.Bench;

using System;
using System.Collections.Generic;
using ktsu.ForceDirectedLayout;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the scoring and tuning layer over the benchmark harness.
/// </summary>
/// <remarks>
/// These matter more than they look. A tuner is a machine for producing confident-sounding numbers,
/// and the ways it goes wrong are quiet: a candidate list that never offers the value a setting
/// already has cannot decline to move, a writer that reads back a different setting tunes the wrong
/// thing entirely, and a score that is not reproducible turns a whole run into an elaborate way of
/// sampling noise. Each of those is a test here.
/// </remarks>
[TestClass]
public class LayoutTunerTests
{
	/// <summary>A run small enough to be a test and large enough to settle into something.</summary>
	private static BenchOptions Quick => new(Starts: 2, Frames: 400, ReadableAfter: 200);

	/// <summary>Two graphs, so a corpus score is an average of more than one thing.</summary>
	private static IReadOnlyList<BenchGraph> TinyCorpus => [GraphCorpus.Chain, GraphCorpus.Disconnected];

	[TestMethod]
	public void EverySetting_OffersTheValueItAlreadyHas()
	{
		// Without this a pass cannot decline to move: the incumbent would have no score to beat, and
		// every setting would be dragged to whichever of the offered values happened to be least bad.
		foreach (TunableSetting setting in LayoutTuner.Settings)
		{
			double current = setting.Read(LayoutSettings.Defaults);
			CollectionAssert.Contains(
				(System.Collections.ICollection)setting.Candidates,
				current,
				$"{setting.Name}: its candidates should include the default it starts at, {current:G6}");
		}
	}

	[TestMethod]
	public void EverySetting_WritesTheFieldItReads()
	{
		// A copy-pasted lambda that writes one setting and reads another would tune the wrong knob
		// while reporting the right name, and nothing else in a run would say so.
		foreach (TunableSetting setting in LayoutTuner.Settings)
		{
			double written = setting.Read(LayoutSettings.Defaults) + 1.0;
			LayoutSettings after = setting.Write(LayoutSettings.Defaults, written);

			Assert.AreEqual(written, setting.Read(after), $"{setting.Name}: should read back what it wrote");
		}
	}

	[TestMethod]
	public void EverySetting_LeavesTheOthersAlone()
	{
		foreach (TunableSetting setting in LayoutTuner.Settings)
		{
			LayoutSettings after = setting.Write(LayoutSettings.Defaults, setting.Read(LayoutSettings.Defaults) + 1.0);

			foreach (TunableSetting other in LayoutTuner.Settings)
			{
				if (other.Name != setting.Name)
				{
					Assert.AreEqual(
						other.Read(LayoutSettings.Defaults),
						other.Read(after),
						$"writing {setting.Name} should not have moved {other.Name}");
				}
			}
		}
	}

	[TestMethod]
	public void Score_IsReproducible()
	{
		// The whole method rests on this. If the same settings scored differently twice, the
		// difference between two rows of a sweep would not be the setting.
		LayoutScore first = LayoutScore.OfCorpus(LayoutSettings.Defaults, Quick, TinyCorpus);
		LayoutScore second = LayoutScore.OfCorpus(LayoutSettings.Defaults, Quick, TinyCorpus);

		Assert.AreEqual(first, second);
	}

	[TestMethod]
	public void Score_TotalIsTheWeightedSumOfItsTerms()
	{
		LayoutScore score = LayoutScore.OfCorpus(LayoutSettings.Defaults, Quick, TinyCorpus);

		double expected =
			(score.Overlap * 4.0) + (score.HiddenLinks * 2.0) + (score.EdgeAngle * 2.0) +
			(score.Crossings * 1.0) + (score.Unreadable * 2.0) + (score.Unsettled * 1.0) +
			(score.Crowding * 1.0) + (score.Sprawl * 1.0);

		Assert.AreEqual(expected, score.Total, 1e-9,
			"the total should be the terms times the weights the remarks claim, or the remarks are wrong");
	}

	[TestMethod]
	public void Score_PenalisesALayoutThatNeverRan()
	{
		// Every force zeroed, so the nodes stay exactly where they were scattered - which is the worst
		// arrangement available. A score that cannot tell that apart from a settled one is measuring
		// nothing.
		//
		// Silencing only the spring and gravity is not enough and made this test wrong once already:
		// the remaining forces still organise a graph perfectly well, so "inert" has to mean all of
		// them rather than the two that happen to look like the important ones.
		LayoutSettings inert = LayoutSettings.Defaults with
		{
			RepulsionStrength = 0.0,
			LinkSpringStrength = 0.0,
			GravityStrength = 0.0,
			DirectionalBias = 0.0,
			LinkFlatteningStrength = 0.0,
			LinkUntwistStrength = 0.0,
			OriginAnchorWeight = 0.0,
			OverlapMargin = 0.0,
		};

		LayoutScore settled = LayoutScore.OfCorpus(LayoutSettings.Defaults, Quick, TinyCorpus);
		LayoutScore scattered = LayoutScore.OfCorpus(inert, Quick, TinyCorpus);

		Assert.IsGreaterThan(settled.Total, scattered.Total,
			$"a graph left scattered should score worse than a settled one; {scattered.Total:F3} against {settled.Total:F3}");
	}

	[TestMethod]
	public void StartOffset_ChangesTheArrangementsButRepeatsItself()
	{
		// This is what a holdout is made of: a family of starts that shares nothing with the family a
		// value was chosen on, and that measures the same twice.
		BenchResult home = LayoutBench.Run(GraphCorpus.Counter, LayoutSettings.Defaults, "home", Quick);
		BenchResult away = LayoutBench.Run(GraphCorpus.Counter, LayoutSettings.Defaults, "away", Quick with { StartOffset = 131 });
		BenchResult againstAway = LayoutBench.Run(GraphCorpus.Counter, LayoutSettings.Defaults, "away", Quick with { StartOffset = 131 });

		Assert.AreNotEqual(home.MeanArea, away.MeanArea, "an offset family should settle different arrangements");
		Assert.AreEqual(away.MeanArea, againstAway.MeanArea, "the same offset should settle the same arrangements");
	}

	[TestMethod]
	public void Sweep_ReportsEveryCandidateAndPicksTheBest()
	{
		TunableSetting setting = new(
			"LinkSpringStrength", [0.25, 0.5, 1.0],
			s => s.LinkSpringStrength, (s, v) => s with { LinkSpringStrength = v });

		TuningStep step = LayoutTuner.Sweep(setting, LayoutSettings.Defaults, Quick, TinyCorpus);

		Assert.AreEqual(3, step.Rows.Count, "every candidate should appear in the rows, not just the winner");

		double best = double.MaxValue;
		double chosen = double.NaN;
		foreach ((double value, LayoutScore score) in step.Rows)
		{
			if (score.Total < best)
			{
				best = score.Total;
				chosen = value;
			}
		}

		Assert.AreEqual(chosen, step.To, "the chosen value should be the one that scored lowest");
		Assert.AreEqual(best, step.ScoreAfter, 1e-9);
	}

	[TestMethod]
	public void Sweep_LeavesTheIncumbentAloneWhenNothingBeatsIt()
	{
		// One candidate, and it is the value already in place: there is nothing to find, and a tuner
		// that reported a move here would be reporting a move on every flat sweep.
		TunableSetting setting = new(
			"DampingFactor", [LayoutSettings.Defaults.DampingFactor],
			s => s.DampingFactor, (s, v) => s with { DampingFactor = v });

		TuningStep step = LayoutTuner.Sweep(setting, LayoutSettings.Defaults, Quick, TinyCorpus);

		Assert.AreEqual(step.From, step.To);
		Assert.AreEqual(0.0, step.Gain, 1e-9);
	}

	[TestMethod]
	public void Descend_KeepsNothingThatDoesNotBeatTheGainFloor()
	{
		// The floor is the difference between a tuned default and one fitted to whichever starting
		// arrangements the run was handed, so a run that ignores it is worth catching here.
		(LayoutSettings tuned, IReadOnlyList<TuningStep> steps) = LayoutTuner.Descend(
			LayoutSettings.Defaults,
			maxRounds: 1,
			Quick,
			log: null,
			minimumGain: double.MaxValue);

		Assert.IsGreaterThan(0, steps.Count, "it should still have swept every setting and said what it found");
		Assert.AreEqual(LayoutSettings.Defaults.RepulsionStrength, tuned.RepulsionStrength);
		Assert.AreEqual(LayoutSettings.Defaults.LinkFlatteningStrength, tuned.LinkFlatteningStrength);
		Assert.AreEqual(LayoutSettings.Defaults.GravityStrength, tuned.GravityStrength);
	}

	[TestMethod]
	public void Table_ShowsTheWholeCurveAndMarksTheChoice()
	{
		TunableSetting setting = new(
			"GravityStrength", [0.0, LayoutSettings.Defaults.GravityStrength, 200.0],
			s => s.GravityStrength, (s, v) => s with { GravityStrength = v });

		string table = LayoutTuner.Table(LayoutTuner.Sweep(setting, LayoutSettings.Defaults, Quick, TinyCorpus));

		StringAssert.Contains(table, "GravityStrength");
		StringAssert.Contains(table, "<-", StringComparison.Ordinal);
		Assert.AreEqual(4, table.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length,
			"a header and one row per candidate, so the shape of the curve is visible and not just its minimum");
	}
}
