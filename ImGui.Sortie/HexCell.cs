// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Sortie;

/// <summary>One cell of a <see cref="ImGuiSortie.HexGrid"/>, addressed by column and row.</summary>
/// <param name="Column">Column from the left, starting at zero.</param>
/// <param name="Row">Row from the top, starting at zero. Odd columns sit half a cell lower.</param>
public readonly record struct HexCell(int Column, int Row);
