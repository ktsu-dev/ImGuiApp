// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ForceDirectedLayout.Tests.Bench;

using System;
using System.Collections.Generic;
using ktsu.ForceDirectedLayout;

/// <summary>A node in a benchmark graph, sized the way a node editor would draw it.</summary>
/// <param name="Id">Stable identifier, unique within the graph.</param>
/// <param name="Width">Drawn width.</param>
/// <param name="Height">Drawn height.</param>
/// <param name="InputRows">How many input pin rows it has, which is the row its outputs start at.</param>
public sealed record BenchNode(int Id, double Width, double Height, int InputRows);

/// <summary>An edge that knows which pin row it leaves and which it arrives at.</summary>
/// <param name="From">Id of the source node.</param>
/// <param name="FromRow">Which of the source's output rows it leaves from, counted from the first.</param>
/// <param name="To">Id of the target node.</param>
/// <param name="ToRow">Which of the target's input rows it arrives at.</param>
public sealed record BenchEdge(int From, int FromRow, int To, int ToRow);

/// <summary>
/// A graph to measure a layout against: nodes with real sizes, and edges that attach at real pins.
/// </summary>
/// <remarks>
/// Sizes and pin rows are not decoration. Repulsion is measured across the clear space between two
/// boxes, the link spring and every angle force are measured between the pins a link is drawn
/// between, and the untwist force only has an order to preserve when two links arrive at different
/// pins. A graph of equal-sized points exercises none of that, so a layout can look perfect on one
/// and be unusable on a real document.
/// </remarks>
public sealed class BenchGraph
{
	/// <summary>Height of a node's header, above its first pin row.</summary>
	private const double HeaderHeight = 28.0;

	/// <summary>Vertical pitch from one pin row to the next.</summary>
	private const double RowPitch = 21.0;

	/// <summary>Offset from a row's top to the pin itself.</summary>
	private const double RowCentre = 10.0;

	/// <summary>Clearance kept between the last usable pin position and a node's bottom edge.</summary>
	private const double BottomMargin = 8.0;

	/// <summary>Constructs a graph from its nodes and edges.</summary>
	/// <param name="name">What to call it in a report.</param>
	/// <param name="nodes">The nodes, each with a distinct id.</param>
	/// <param name="edges">The edges between them.</param>
	public BenchGraph(string name, IReadOnlyList<BenchNode> nodes, IReadOnlyList<BenchEdge> edges)
	{
		ArgumentNullException.ThrowIfNull(nodes);
		ArgumentNullException.ThrowIfNull(edges);

		Name = name;
		Nodes = nodes;
		Edges = edges;

		Dictionary<int, int> indexById = [];
		for (int i = 0; i < nodes.Count; i++)
		{
			indexById[nodes[i].Id] = i;
		}
		IndexById = indexById;
	}

	/// <summary>What to call this graph in a report.</summary>
	public string Name { get; }

	/// <summary>The nodes, in the order they are submitted to a core.</summary>
	public IReadOnlyList<BenchNode> Nodes { get; }

	/// <summary>The edges between them.</summary>
	public IReadOnlyList<BenchEdge> Edges { get; }

	/// <summary>Body index of each node id, matching the order <see cref="Nodes"/> is submitted in.</summary>
	public IReadOnlyDictionary<int, int> IndexById { get; }

	/// <summary>
	/// Where a pin sits on a node, relative to the node's own origin.
	/// </summary>
	/// <param name="node">The node the pin belongs to.</param>
	/// <param name="row">Which row, counted from the node's first input row.</param>
	/// <param name="onRight">True for an output pin, on the node's right edge.</param>
	public static Vec2D PinOffset(BenchNode node, int row, bool onRight)
	{
		ArgumentNullException.ThrowIfNull(node);

		double y = Math.Min(HeaderHeight + (row * RowPitch) + RowCentre, node.Height - BottomMargin);
		return new Vec2D(onRight ? node.Width : 0.0, y);
	}

	/// <summary>
	/// Builds a core holding this graph, scattered into one particular starting arrangement.
	/// </summary>
	/// <param name="settings">Settings for the core; <see cref="LayoutSettings.Enabled"/> is forced on.</param>
	/// <param name="seed">Chooses the arrangement. The same seed always gives the same one.</param>
	/// <param name="spread">
	/// How far the scatter reaches, as a fraction of a thousand units. Small values pile every node
	/// almost on top of the others and large ones fling them apart, and a layout that only settles
	/// from one of those is not settling, it is inheriting its answer from where it started.
	/// </param>
	public LayoutCore Start(LayoutSettings settings, int seed, double spread)
	{
		settings.Enabled = 1;
		LayoutCore core = new() { Settings = settings };

		core.ResizeBodies(Nodes.Count);
		int state = seed;
		for (int i = 0; i < Nodes.Count; i++)
		{
			core.Bodies[i] = new BodyState
			{
				Id = Nodes[i].Id,
				Position = new Vec2D(NextScatter(ref state, spread), NextScatter(ref state, spread)),
				Dimensions = new Vec2D(Nodes[i].Width, Nodes[i].Height),
			};
		}

		core.ResizeEdges(Edges.Count);
		for (int e = 0; e < Edges.Count; e++)
		{
			BenchEdge edge = Edges[e];
			BenchNode from = Nodes[IndexById[edge.From]];
			BenchNode to = Nodes[IndexById[edge.To]];

			core.Edges[e] = new EdgeRef
			{
				SourceIndex = IndexById[edge.From],
				TargetIndex = IndexById[edge.To],
				SourcePinOffset = PinOffset(from, from.InputRows + edge.FromRow, onRight: true),
				TargetPinOffset = PinOffset(to, edge.ToRow, onRight: false),
				HasPinOffsets = 1,
			};
		}

		return core;
	}

	/// <summary>
	/// One coordinate of the scatter, from a linear congruential generator.
	/// </summary>
	/// <remarks>
	/// Deliberately not <see cref="Random"/>: a starting arrangement has to be identical on every
	/// machine and every runtime, or a measurement taken today cannot be compared with one written
	/// into a comment last month.
	/// </remarks>
	/// <param name="state">Generator state, advanced by the call.</param>
	/// <param name="spread">Scatter reach, as a fraction of a thousand units.</param>
	private static double NextScatter(ref int state, double spread)
	{
		state = ((state * 1103515245) + 12345) & 0x7fffffff;
		return state % 1000 * spread;
	}
}
