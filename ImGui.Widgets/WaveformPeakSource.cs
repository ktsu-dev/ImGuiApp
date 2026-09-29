// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Supplies the peaks a zoomable <see cref="Waveform(string, WaveformPeakSource, TimelineView, ref float, System.Numerics.Vector2)"/>
	/// draws, for whatever part of the clip is in view.
	/// </summary>
	/// <remarks>
	/// A zoomable waveform cannot take a fixed overview the way the overview overloads do, because
	/// what one pixel covers changes with every zoom. It asks for the peaks of the span it is showing
	/// instead, one column per pixel, every frame. <see cref="WaveformPeakCache"/> answers from a
	/// multi-resolution summary built once; derive from this to answer from anything else, such as a
	/// file too long to hold in memory.
	/// </remarks>
	public abstract class WaveformPeakSource
	{
		/// <summary>Gets the length of the clip, on the same timeline <see cref="GetPeaks"/> is asked about.</summary>
		public abstract float Duration { get; }

		/// <summary>
		/// Fills one column per element of <paramref name="minimums"/> and <paramref name="maximums"/>
		/// with the lowest and highest sample between <paramref name="startPosition"/> and <paramref name="endPosition"/>.
		/// </summary>
		/// <param name="startPosition">The timeline position at the left edge of the first column.</param>
		/// <param name="endPosition">The timeline position at the right edge of the last column.</param>
		/// <param name="minimums">Receives the lowest sample in each column, in -1..1.</param>
		/// <param name="maximums">Receives the highest sample in each column, in -1..1.</param>
		/// <remarks>
		/// Called every frame the waveform is drawn, so an implementation should not scan the clip.
		/// A column must never under-report its extremes: a transient narrower than a column still has
		/// to show in it.
		/// </remarks>
		public abstract void GetPeaks(float startPosition, float endPosition, Span<float> minimums, Span<float> maximums);
	}
}
