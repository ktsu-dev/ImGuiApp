// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ForceDirectedLayout.Tests.Bench;

using System.Collections.Generic;

/// <summary>
/// The graphs a layout change is measured against.
/// </summary>
/// <remarks>
/// Each is a shape that breaks a layout differently, which is why there is more than one. A change
/// that helps a wide fan-in can easily hurt a deep chain, and a single graph will not say so.
/// </remarks>
public static class GraphCorpus
{
	/// <summary>
	/// The Counter document as a node editor lays it out: twenty nodes, twenty-one edges, five levels
	/// deep, with sizes running from a 60-wide literal to a 118x180 function.
	/// </summary>
	/// <remarks>
	/// This is the reference graph. It is a real document rather than a synthetic shape, its nodes
	/// differ in size by a factor of three in each direction, and it has every feature the forces care
	/// about: a backward edge, several nodes feeding one, and pins spread down tall bodies.
	/// </remarks>
	public static BenchGraph Counter { get; } = BuildCounter();

	/// <summary>A single chain, which is the shape that most wants to be a straight horizontal line.</summary>
	/// <remarks>
	/// Nothing here competes: there is one path, every node has one input and one output, and a
	/// correct layout is a row. It is the cheapest way to catch a change that leaves edges steep or a
	/// graph taller than it is wide.
	/// </remarks>
	public static BenchGraph Chain { get; } = BuildChain(12);

	/// <summary>
	/// Eight sources feeding one wide node, which is where crossings and crowding come from.
	/// </summary>
	/// <remarks>
	/// Every source arrives at a different pin on the same target, so the vertical order of the
	/// sources is forced by the order of the pins — this is the arrangement the untwist force exists
	/// for. The sources are all the same size and the target is much larger, which is exactly the
	/// pairing a centre-measured repulsion got wrong.
	/// </remarks>
	public static BenchGraph FanIn { get; } = BuildFanIn(8);

	/// <summary>
	/// A graph of wildly mismatched node sizes, which is what spacing measured between centres broke on.
	/// </summary>
	/// <remarks>
	/// Alternating 400-wide slabs and 50-wide literals, all unlinked to each other except through a
	/// spine. If spacing depends on how big a node is rather than on the room around it, the slabs end
	/// up crowded and the literals marooned, and <see cref="LayoutMetrics.TightestClearGap"/> says so.
	/// </remarks>
	public static BenchGraph MixedSizes { get; } = BuildMixedSizes();

	/// <summary>Every graph in the corpus, for a change that should be measured against all of them.</summary>
	public static IReadOnlyList<BenchGraph> All { get; } = [Counter, Chain, FanIn, MixedSizes];

	/// <summary>Builds the Counter document.</summary>
	private static BenchGraph BuildCounter()
	{
		BenchNode[] nodes =
		[
			new(1, 60, 50, 0), new(2, 115, 62, 1),            // 0 -> var count
			new(3, 60, 50, 0), new(4, 115, 62, 1),            // 1 -> var step
			new(5, 110, 50, 0),                               // param amount
			new(6, 75, 50, 0), new(7, 75, 50, 0), new(8, 75, 50, 0), new(9, 75, 50, 0),
			new(10, 90, 75, 2),                               // binary + (Add)
			new(11, 90, 70, 2),                               // assign
			new(12, 105, 60, 1),                              // return (Add)
			new(13, 118, 180, 7),                             // function Add
			new(14, 75, 50, 0), new(15, 75, 50, 0),
			new(16, 90, 75, 2),                               // binary + (Next)
			new(17, 115, 62, 1),                              // var result
			new(18, 105, 60, 1),                              // return (Next)
			new(19, 118, 160, 6),                             // function Next
			new(20, 117, 160, 6),                             // class Counter
		];

		BenchEdge[] edges =
		[
			new(1, 0, 2, 0), new(2, 0, 20, 0),
			new(3, 0, 4, 0), new(4, 0, 20, 1),
			new(5, 0, 13, 0),
			new(6, 0, 11, 0), new(7, 0, 10, 0), new(8, 0, 10, 1), new(10, 0, 11, 1),
			new(11, 0, 13, 3), new(9, 0, 12, 0), new(12, 0, 13, 4), new(13, 0, 20, 2),
			new(14, 0, 16, 0), new(15, 0, 16, 1), new(16, 0, 17, 0), new(17, 0, 19, 2),
			new(18, 0, 19, 3), new(19, 0, 20, 3),
			new(9, 0, 18, 0),
		];

		return new BenchGraph("Counter", nodes, edges);
	}

	/// <summary>Builds a single chain of the given length.</summary>
	/// <param name="length">How many nodes the chain has.</param>
	private static BenchGraph BuildChain(int length)
	{
		List<BenchNode> nodes = [];
		List<BenchEdge> edges = [];

		for (int i = 0; i < length; i++)
		{
			nodes.Add(new BenchNode(i + 1, 100, 55, i == 0 ? 0 : 1));
			if (i > 0)
			{
				edges.Add(new BenchEdge(i, 0, i + 1, 0));
			}
		}

		return new BenchGraph("Chain", nodes, edges);
	}

	/// <summary>Builds a fan of sources into one target.</summary>
	/// <param name="sources">How many sources feed the target.</param>
	private static BenchGraph BuildFanIn(int sources)
	{
		List<BenchNode> nodes = [];
		List<BenchEdge> edges = [];

		for (int i = 0; i < sources; i++)
		{
			nodes.Add(new BenchNode(i + 1, 70, 50, 0));
			edges.Add(new BenchEdge(i + 1, 0, sources + 1, i));
		}

		// Tall enough to give every arriving link its own pin row.
		nodes.Add(new BenchNode(sources + 1, 130, 40 + (sources * 21), sources));

		return new BenchGraph("FanIn", nodes, edges);
	}

	/// <summary>Builds the mismatched-size graph.</summary>
	private static BenchGraph BuildMixedSizes()
	{
		List<BenchNode> nodes = [];
		List<BenchEdge> edges = [];

		// A spine of alternating slabs and literals, each feeding the next.
		for (int i = 0; i < 10; i++)
		{
			bool slab = i % 2 == 0;
			nodes.Add(new BenchNode(i + 1, slab ? 400 : 50, slab ? 120 : 40, i == 0 ? 0 : 1));
			if (i > 0)
			{
				edges.Add(new BenchEdge(i, 0, i + 1, 0));
			}
		}

		return new BenchGraph("MixedSizes", nodes, edges);
	}
}
