// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

/// <summary>A drop a <see cref="ImGuiWidgets.ReorderableTree"/> reports: which row moves, and where to.</summary>
/// <param name="Row">The index of the dragged row in the rows the tree was drawn from.</param>
/// <param name="ParentRow">
/// The index of the row the dragged one becomes a child of, or -1 for the top level of the tree.
/// </param>
/// <param name="Index">
/// <para>
/// Where among the new parent's children it goes, counted in the rows' own top-to-bottom order and
/// <b>against the children as they stand before the move</b>. It names the gap the insertion line was
/// drawn in: 0 is above the first child, and the number of children is below the last.
/// </para>
/// <para>
/// When the row moves down within its own parent, its old place is among the children counted, so
/// the index it ends up at once removed is one less than this. A caller whose move operation takes
/// the final position subtracts one in exactly that case; a caller whose operation takes the gap,
/// as this does, passes it through.
/// </para>
/// <para>
/// A drop inside a container is index 0, the top of its children, whether it is expanded or not.
/// </para>
/// </param>
public readonly record struct ReorderableTreeMove(int Row, int ParentRow, int Index);
