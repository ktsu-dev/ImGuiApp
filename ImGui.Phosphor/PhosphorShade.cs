// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Phosphor;

/// <summary>
/// The brightness levels a phosphor screen draws in, from the overdriven centre of a lit glyph down to
/// the faint persistence left behind it.
/// </summary>
public enum PhosphorShade
{
	/// <summary>Overdriven, nearly white: alerts, cursors and the value that matters.</summary>
	Hot,

	/// <summary>Ordinary lit text.</summary>
	Bright,

	/// <summary>Lines, graticules and secondary text.</summary>
	Normal,

	/// <summary>Labels, unlit segments and borders.</summary>
	Dim,

	/// <summary>Barely there: fills, grids and afterglow.</summary>
	Faint,
}
