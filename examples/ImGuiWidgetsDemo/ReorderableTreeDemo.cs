// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;

/// <summary>
/// The reorderable tree, over a small tree of layers the demo owns.
/// </summary>
internal static class ReorderableTreeDemo
{
	private static List<DemoNode> roots = Initial();

	private static string lastMove = "Drag a row to move it.";

	/// <summary>Gets the tree's shape, top first, with one dot per level of depth, for tests to read.</summary>
	internal static string Shape
	{
		get
		{
			List<(DemoNode Node, int Depth, DemoNode? Parent)> flat = [];
			Flatten(roots, 0, null, flat);
			return string.Join(" ", flat.Select(entry => new string('.', entry.Depth) + entry.Node.Name));
		}
	}

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		roots = Initial();
		lastMove = "Drag a row to move it.";
	}

	private static List<DemoNode> Initial() =>
	[
		new("Background", false),
		new("Photos", true) { Children = { new("Beach", false), new("Forest", false) } },
		new("Notes", false),
		new("Archive", true),
	];

	private static void Flatten(List<DemoNode> siblings, int depth, DemoNode? parent, List<(DemoNode, int, DemoNode?)> into)
	{
		foreach (DemoNode node in siblings)
		{
			into.Add((node, depth, parent));
			if (node.Expanded)
			{
				Flatten(node.Children, depth + 1, node, into);
			}
		}
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (!DemoProbe.Header("Reorderable Tree"))
		{
			return;
		}

		ImGui.TextUnformatted("Rows the caller draws, dragged to reorder them and to move them in and out of groups:");
		ImGui.Separator();

		List<(DemoNode Node, int Depth, DemoNode? Parent)> flat = [];
		Flatten(roots, 0, null, flat);
		ReorderableTreeRow[] rows = [.. flat.Select(entry => new ReorderableTreeRow(entry.Depth, entry.Node.IsGroup))];

		ReorderableTreeMove? move = ImGuiWidgets.ReorderableTree("demoTree", rows, i => DrawRow(flat[i].Node));

		if (move is ReorderableTreeMove landed)
		{
			Apply(flat, landed);
		}

		ImGui.Separator();
		ImGui.TextUnformatted("The top quarter of a group drops above it, the middle half inside, the bottom quarter below.");
		ImGui.TextUnformatted("Below the last row of a group, how far left the pointer is picks how far out the row lands.");
		ImGui.TextUnformatted(lastMove);
	}

	private static void DrawRow(DemoNode node)
	{
		if (node.IsGroup)
		{
			if (ImGui.ArrowButton("##expand", node.Expanded ? ImGuiDir.Down : ImGuiDir.Right))
			{
				node.Expanded = !node.Expanded;
			}

			ImGui.SameLine();
		}

		ImGui.TextUnformatted(node.Name);
	}

	/// <summary>Applies a move to the demo's tree, as an application would through its own model.</summary>
	private static void Apply(List<(DemoNode Node, int Depth, DemoNode? Parent)> flat, ReorderableTreeMove move)
	{
		(DemoNode node, _, DemoNode? oldParent) = flat[move.Row];
		DemoNode? newParent = move.ParentRow < 0 ? null : flat[move.ParentRow].Node;
		List<DemoNode> from = oldParent?.Children ?? roots;
		List<DemoNode> to = newParent?.Children ?? roots;

		int oldIndex = from.IndexOf(node);
		int index = move.Index;

		// The index is the gap among the children before the move. A list insert takes the position
		// after it, which is one less when the row leaves a place above the gap in the same list.
		if (ReferenceEquals(from, to) && oldIndex < index)
		{
			index--;
		}

		from.RemoveAt(oldIndex);
		to.Insert(index, node);
		lastMove = $"Moved {node.Name} into {newParent?.Name ?? "the top level"} at {index}.";
	}

	private sealed class DemoNode(string name, bool isGroup)
	{
		public string Name { get; } = name;

		public bool IsGroup { get; } = isGroup;

		public bool Expanded { get; set; } = true;

		public List<DemoNode> Children { get; } = [];
	}
}
