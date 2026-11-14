// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Bulkhead;

using System.Numerics;

/// <summary>How <see cref="ImGuiBulkhead.Panel"/> lays out its frame.</summary>
public sealed record BulkheadPanelOptions
{
	/// <summary>Gets the width of the left-hand column, in pixels. Zero picks four frame heights.</summary>
	public float ColumnWidth { get; init; }

	/// <summary>Gets the thickness of the top and bottom bars, in pixels. Zero picks two thirds of a frame height.</summary>
	public float BarThickness { get; init; }

	/// <summary>Gets the frame colour. Null uses <see cref="BulkheadPalette.Amber"/>.</summary>
	public Vector4? Color { get; init; }

	/// <summary>Gets the colour the column is filled with when the panel has no sidebar. Null uses a darker shade of <see cref="Color"/>.</summary>
	public Vector4? ColumnColor { get; init; }

	/// <summary>Gets the gap between the frame's parts and between the frame and the content, in pixels.</summary>
	public float Gap { get; init; } = 4f;
}
