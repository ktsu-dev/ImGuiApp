// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ForceDirectedLayout.Tests.Bench;

using System;
using ktsu.ForceDirectedLayout;

/// <summary>
/// What a settled layout measures: the numbers that say whether a reader could follow the graph.
/// </summary>
/// <remarks>
/// No one of these is the answer, and that is the point of having them together. A graph can be
/// spread wide and still illegible because its links run under its nodes; it can have no crossings
/// and no overlaps and still be a tall column no one can read; and a collapse into a crushed ribbon
/// flatters both the area and the angle while being the worst outcome of the lot. Reading them as a
/// row is what stops a change that traded one for another from looking like an improvement.
/// </remarks>
/// <param name="Width">Width of the bounding box around every node.</param>
/// <param name="Height">Height of that box.</param>
/// <param name="MeanEdgeAngle">Mean angle off horizontal across the edges, in degrees, folded into 0-90.</param>
/// <param name="LinksOverBodies">Links drawn across a node they are not an end of, which a renderer hides.</param>
/// <param name="TightestClearGap">Clear space between the closest pair of node boxes.</param>
/// <param name="MeanClearGap">Mean clear space over every pair.</param>
/// <param name="WorstOverlap">Deepest overlap between any two boxes; anything above zero is a defect.</param>
/// <param name="TwistedPairs">Pairs of links meeting at a node whose far ends sit in the wrong order, so they cross.</param>
/// <param name="Settled">Whether the simulation reported itself stable.</param>
public readonly record struct LayoutMetrics(
	double Width,
	double Height,
	double MeanEdgeAngle,
	int LinksOverBodies,
	double TightestClearGap,
	double MeanClearGap,
	double WorstOverlap,
	int TwistedPairs,
	bool Settled)
{
	/// <summary>Area of the bounding box, the coarsest measure of how much room a graph has.</summary>
	public double Area => Width * Height;

	/// <summary>
	/// True when the graph reads left to right with roughly horizontal edges, which is the shape a
	/// node editor is trying to reach.
	/// </summary>
	public bool Readable => Width > Height && MeanEdgeAngle < 45.0;

	/// <summary>How many points along a link are tested against each node it might be hidden behind.</summary>
	private const int LinkSamples = 40;

	/// <summary>Measures a core's current arrangement.</summary>
	/// <param name="core">The core to measure. Its edges are expected to carry pin offsets.</param>
	public static LayoutMetrics Measure(LayoutCore core)
	{
		ArgumentNullException.ThrowIfNull(core);

		(double width, double height) = BoundingBox(core);
		(double tightest, double mean, double worstOverlap) = GapStatistics(core);

		return new LayoutMetrics(
			width,
			height,
			MeasureMeanEdgeAngle(core),
			CountLinksOverBodies(core),
			tightest,
			mean,
			worstOverlap,
			CountTwistedPairs(core),
			core.IsStable);
	}

	/// <summary>Where an edge's two ends are drawn, in world space.</summary>
	/// <param name="core">The core holding the edge.</param>
	/// <param name="e">Index of the edge.</param>
	private static (Vec2D From, Vec2D To) Ends(LayoutCore core, int e)
	{
		EdgeRef edge = core.Edges[e];
		BodyState source = core.Bodies[edge.SourceIndex];
		BodyState target = core.Bodies[edge.TargetIndex];

		return edge.HasPinOffsets != 0
			? (source.Position + edge.SourcePinOffset, target.Position + edge.TargetPinOffset)
			: (source.Position + (source.Dimensions * 0.5), target.Position + (target.Dimensions * 0.5));
	}

	/// <summary>The box around every node.</summary>
	/// <param name="core">The core to measure.</param>
	private static (double Width, double Height) BoundingBox(LayoutCore core)
	{
		if (core.BodyCount == 0)
		{
			return (0.0, 0.0);
		}

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

		return (maxX - minX, maxY - minY);
	}

	/// <summary>Mean angle off horizontal across the edges, folded into 0-90 degrees.</summary>
	/// <param name="core">The core to measure.</param>
	private static double MeasureMeanEdgeAngle(LayoutCore core)
	{
		if (core.EdgeCount == 0)
		{
			return 0.0;
		}

		double total = 0.0;
		for (int e = 0; e < core.EdgeCount; e++)
		{
			(Vec2D from, Vec2D to) = Ends(core, e);
			double angle = Math.Abs(Math.Atan2(to.Y - from.Y, to.X - from.X) * 180.0 / Math.PI);
			total += angle > 90.0 ? 180.0 - angle : angle;
		}

		return total / core.EdgeCount;
	}

	/// <summary>
	/// Counts the (link, node) pairs where a link is drawn across a node it is no end of.
	/// </summary>
	/// <remarks>
	/// Links render beneath node backgrounds, so such a link disappears for that node's width. The
	/// path is approximated by the straight line between the pins, which is what the rendered curve
	/// stays close to once the flattening force has done its work.
	/// </remarks>
	/// <param name="core">The core to measure.</param>
	private static int CountLinksOverBodies(LayoutCore core)
	{
		int over = 0;

		for (int e = 0; e < core.EdgeCount; e++)
		{
			(Vec2D from, Vec2D to) = Ends(core, e);

			for (int b = 0; b < core.BodyCount; b++)
			{
				if (b == core.Edges[e].SourceIndex || b == core.Edges[e].TargetIndex)
				{
					continue;
				}

				BodyState body = core.Bodies[b];
				for (int step = 1; step < LinkSamples; step++)
				{
					Vec2D at = Vec2D.Lerp(from, to, (double)step / LinkSamples);
					if (at.X >= body.Position.X && at.X <= body.Position.X + body.Dimensions.X &&
						at.Y >= body.Position.Y && at.Y <= body.Position.Y + body.Dimensions.Y)
					{
						over++;
						break;
					}
				}
			}
		}

		return over;
	}

	/// <summary>
	/// The tightest and mean clear space between node boxes, and the deepest overlap among them.
	/// </summary>
	/// <remarks>
	/// Clear space is measured the way repulsion measures it — between the two closest points on a
	/// pair's boxes — so what this reports is the same quantity the force is working on, and the
	/// tightest of them is the one pair a reader would call crowded.
	/// </remarks>
	/// <param name="core">The core to measure.</param>
	private static (double Tightest, double Mean, double WorstOverlap) GapStatistics(LayoutCore core)
	{
		double tightest = double.MaxValue;
		double total = 0.0;
		double worstOverlap = 0.0;
		int pairs = 0;

		for (int i = 0; i < core.BodyCount; i++)
		{
			for (int j = i + 1; j < core.BodyCount; j++)
			{
				BodyState a = core.Bodies[i];
				BodyState b = core.Bodies[j];

				double betweenX = Math.Abs(b.Position.X + (b.Dimensions.X * 0.5) - a.Position.X - (a.Dimensions.X * 0.5));
				double betweenY = Math.Abs(b.Position.Y + (b.Dimensions.Y * 0.5) - a.Position.Y - (a.Dimensions.Y * 0.5));

				double gapX = betweenX - ((a.Dimensions.X + b.Dimensions.X) * 0.5);
				double gapY = betweenY - ((a.Dimensions.Y + b.Dimensions.Y) * 0.5);

				double gap = Math.Sqrt((Math.Max(gapX, 0.0) * Math.Max(gapX, 0.0)) + (Math.Max(gapY, 0.0) * Math.Max(gapY, 0.0)));
				tightest = Math.Min(tightest, gap);
				total += gap;
				pairs++;

				if (gapX < 0.0 && gapY < 0.0)
				{
					worstOverlap = Math.Max(worstOverlap, Math.Min(-gapX, -gapY));
				}
			}
		}

		return pairs == 0 ? (0.0, 0.0, 0.0) : (tightest, total / pairs, worstOverlap);
	}

	/// <summary>
	/// Counts pairs of links meeting at a node whose far ends sit in the opposite vertical order to
	/// the pins they meet at, which is exactly the arrangement in which the two are drawn crossing.
	/// </summary>
	/// <param name="core">The core to measure.</param>
	private static int CountTwistedPairs(LayoutCore core)
	{
		int twisted = 0;

		for (int i = 0; i < core.EdgeCount; i++)
		{
			for (int j = i + 1; j < core.EdgeCount; j++)
			{
				EdgeRef first = core.Edges[i];
				EdgeRef second = core.Edges[j];

				(Vec2D firstFrom, Vec2D firstTo) = Ends(core, i);
				(Vec2D secondFrom, Vec2D secondTo) = Ends(core, j);

				double atPins;
				double atFarEnds;

				if (first.TargetIndex == second.TargetIndex && first.SourceIndex != second.SourceIndex)
				{
					atPins = firstTo.Y - secondTo.Y;
					atFarEnds = firstFrom.Y - secondFrom.Y;
				}
				else if (first.SourceIndex == second.SourceIndex && first.TargetIndex != second.TargetIndex)
				{
					atPins = firstFrom.Y - secondFrom.Y;
					atFarEnds = firstTo.Y - secondTo.Y;
				}
				else
				{
					continue;
				}

				if (atPins * atFarEnds < 0.0)
				{
					twisted++;
				}
			}
		}

		return twisted;
	}
}
