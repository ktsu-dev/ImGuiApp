// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

/// <summary>One row of a <see cref="ImGuiWidgets.ReorderableTree"/>, as the caller has laid it out.</summary>
/// <param name="Depth">
/// How many ancestors the row has. The first row is at depth 0, and each row is at most one deeper
/// than the row above it, which is what a tree flattened top to bottom always is.
/// </param>
/// <param name="CanHaveChildren">
/// Whether the row can hold children, which is what lets a drop land inside it. A container's
/// children are the rows directly beneath it at one more depth; a collapsed container simply has
/// none listed.
/// </param>
public readonly record struct ReorderableTreeRow(int Depth, bool CanHaveChildren);
