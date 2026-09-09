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

	/// <summary>
	/// A two-class document: thirty-four nodes, two roots, and calls that cross from one class to the
	/// other.
	/// </summary>
	/// <remarks>
	/// <see cref="Counter"/> is one class, so every one of its edges is short and its whole graph is a
	/// tree hanging off a single root. Real files are not that, and the difference is the thing a
	/// layout is worst at: two roots compete for the same middle, and the calls from one class into
	/// the other are long edges that have to cross the space between them without being drawn through
	/// whatever is parked there. That is where a link disappears behind a body, and no single-root
	/// graph in this corpus produces one.
	/// </remarks>
	public static BenchGraph TwoClasses { get; } = BuildTwoClasses();

	/// <summary>
	/// Three small components with no link between them, which is the shape only gravity holds together.
	/// </summary>
	/// <remarks>
	/// Every other graph in the corpus is connected, so its springs alone would keep it in one piece and
	/// a measurement over those four cannot see what gravity is for. Here there is nothing but repulsion
	/// between the components, so without gravity they accelerate apart for as long as the simulation
	/// runs and the graph has no settled size at all. A document with an orphaned node or a second
	/// unconnected function in it is this shape, and it is common.
	/// </remarks>
	public static BenchGraph Disconnected { get; } = BuildDisconnected();

	/// <summary>Every graph in the corpus, for a change that should be measured against all of them.</summary>
	public static IReadOnlyList<BenchGraph> All { get; } = [Counter, TwoClasses, Chain, FanIn, MixedSizes, Disconnected];

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

	/// <summary>Builds the two-class document.</summary>
	private static BenchGraph BuildTwoClasses()
	{
		BenchNode[] nodes =
		[
			// class Account: two fields and three methods.
			new(1, 60, 50, 0), new(2, 115, 62, 1),              // 0 -> field balance
			new(3, 80, 50, 0), new(4, 115, 62, 1),              // "" -> field owner
			new(5, 110, 50, 0),                                 // param amount (Deposit)
			new(6, 90, 75, 2),                                  // binary + (Deposit)
			new(7, 90, 70, 2),                                  // assign (Deposit)
			new(8, 105, 60, 1),                                 // return (Deposit)
			new(9, 118, 140, 5),                                // method Deposit
			new(10, 110, 50, 0),                                // param amount (Withdraw)
			new(11, 95, 75, 2),                                 // compare <=
			new(12, 120, 118, 3),                               // if
			new(13, 90, 75, 2),                                 // binary - (Withdraw)
			new(14, 90, 70, 2),                                 // assign (Withdraw)
			new(15, 100, 60, 1),                                // throw
			new(16, 105, 60, 1),                                // return (Withdraw)
			new(17, 118, 160, 6),                               // method Withdraw
			new(18, 100, 75, 2),                                // concat
			new(19, 105, 60, 1),                                // return (ToString)
			new(20, 118, 120, 4),                               // method ToString
			new(21, 130, 180, 7),                               // class Account
			// class Bank: one field and two methods, calling into Account.
			new(22, 90, 50, 0), new(23, 125, 62, 1),            // [] -> field accounts
			new(24, 110, 50, 0), new(25, 110, 50, 0), new(26, 110, 50, 0),  // params from, to, amount
			new(27, 128, 96, 3),                                // call Withdraw
			new(28, 128, 96, 3),                                // call Deposit
			new(29, 118, 150, 5),                               // method Transfer
			new(30, 118, 96, 3),                                // foreach
			new(31, 90, 75, 2),                                 // accumulate +
			new(32, 105, 60, 1),                                // return (Total)
			new(33, 118, 130, 4),                               // method Total
			new(34, 125, 160, 6),                               // class Bank
		];

		BenchEdge[] edges =
		[
			// Account's fields.
			new(1, 0, 2, 0), new(2, 0, 21, 0),
			new(3, 0, 4, 0), new(4, 0, 21, 1),
			// Deposit.
			new(5, 0, 6, 0), new(2, 0, 6, 1), new(6, 0, 7, 1), new(2, 0, 7, 0),
			new(7, 0, 9, 2), new(8, 0, 9, 3), new(5, 0, 9, 0), new(9, 0, 21, 2),
			// Withdraw: a guard, a branch, and two arms.
			new(10, 0, 11, 0), new(2, 0, 11, 1), new(11, 0, 12, 0),
			new(13, 0, 14, 1), new(2, 0, 13, 0), new(10, 0, 13, 1),
			new(14, 0, 12, 1), new(15, 0, 12, 2),
			new(12, 0, 17, 2), new(16, 0, 17, 3), new(10, 0, 17, 0), new(17, 0, 21, 3),
			// ToString reads both fields.
			new(4, 0, 18, 0), new(2, 0, 18, 1), new(18, 0, 19, 0),
			new(19, 0, 20, 2), new(20, 0, 21, 4),
			// Bank's field.
			new(22, 0, 23, 0), new(23, 0, 34, 0),
			// Transfer, whose two calls reach across into Account's methods.
			new(24, 0, 27, 0), new(26, 0, 27, 1), new(17, 0, 27, 2),
			new(25, 0, 28, 0), new(26, 0, 28, 1), new(9, 0, 28, 2),
			new(27, 0, 29, 2), new(28, 0, 29, 3), new(29, 0, 34, 1),
			// Total walks the accounts and calls ToString on each.
			new(23, 0, 30, 0), new(30, 0, 31, 0), new(20, 0, 31, 1),
			new(31, 0, 32, 0), new(32, 0, 33, 2), new(33, 0, 34, 2),
		];

		return new BenchGraph("TwoClasses", nodes, edges);
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

		// Tall enough to give every arriving link its own pin row. The height is computed in double
		// rather than widened from an int expression, so a large source count cannot overflow it.
		nodes.Add(new BenchNode(sources + 1, 130, 40.0 + (sources * 21.0), sources));

		return new BenchGraph("FanIn", nodes, edges);
	}

	/// <summary>Builds three unconnected components.</summary>
	private static BenchGraph BuildDisconnected()
	{
		List<BenchNode> nodes = [];
		List<BenchEdge> edges = [];

		// Three components of three nodes each: a pair joined by one link, plus a lone node.
		for (int component = 0; component < 3; component++)
		{
			int first = (component * 3) + 1;
			nodes.Add(new BenchNode(first, 100, 55, 0));
			nodes.Add(new BenchNode(first + 1, 100, 55, 1));
			nodes.Add(new BenchNode(first + 2, 80, 50, 0));
			edges.Add(new BenchEdge(first, 0, first + 1, 0));
		}

		return new BenchGraph("Disconnected", nodes, edges);
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
