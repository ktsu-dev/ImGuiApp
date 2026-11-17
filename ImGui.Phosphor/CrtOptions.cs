// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Phosphor;

/// <summary>How strongly <see cref="ImGuiPhosphor.CrtOverlay(CrtOptions?)"/> imitates a cathode ray tube.</summary>
/// <param name="ScanlineSpacing">Pixels from one dark scanline to the next. Zero or less draws none.</param>
/// <param name="ScanlineAlpha">How dark each scanline is, from 0 to 1.</param>
/// <param name="Vignette">How much the corners of the glass fall off, from 0 to 1.</param>
/// <param name="Flicker">How much the whole screen's brightness wavers, from 0 to 1. Zero keeps it steady and lets the application idle.</param>
public sealed record CrtOptions(
	float ScanlineSpacing = 3f,
	float ScanlineAlpha = 0.32f,
	float Vignette = 0.45f,
	float Flicker = 0f)
{
	/// <summary>Gets the defaults: scanlines and vignette, no flicker.</summary>
	public static CrtOptions Default { get; } = new();
}
