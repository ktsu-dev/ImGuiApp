// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;

/// <summary>Where one zone of a <see cref="ImGuiWidgets.DividerContainer"/> sits along the container's axis.</summary>
/// <param name="Start">The zone's leading edge, from the container's leading edge.</param>
/// <param name="Length">The zone's extent along the axis.</param>
internal readonly record struct DividerZoneSpan(float Start, float Length)
{
	/// <summary>Gets the zone's trailing edge, from the container's leading edge.</summary>
	public float End => Start + Length;
}

/// <summary>
/// The arithmetic behind <see cref="ImGuiWidgets.DividerContainer"/>'s layout, along one axis, with no
/// ImGui dependency.
/// </summary>
/// <remarks>
/// <para>
/// Every zone is given the same gap: a whole <c>gap</c> between two zones and half of one at each
/// end, so <c>n</c> zones cost <c>n × gap</c> and the rest is shared out by the zones' fractions. Each
/// divider sits in the middle of the gap it is drawn in. A single zone fills the container, since
/// there is nothing to divide.
/// </para>
/// <para>
/// The layout used to subtract half a gap from the end zones and a whole one from the middle ones,
/// and move on by the fraction plus half a gap only after the first zone, rounding sizes down and
/// advances to nearest. That came out right for two zones on paper, but every rounding was taken
/// separately, so with three or more zones, or a length that did not divide evenly, the gaps and
/// the two outer edges drifted apart by a pixel or more each (#724). Here every edge is rounded once,
/// from its exact position, so neighbouring zones share their rounding and every gap is the same.
/// </para>
/// </remarks>
internal static class DividerContainerLayout
{
	/// <summary>Lays zones out along an axis.</summary>
	/// <param name="fractions">Each zone's share of the space left once the gaps are taken out, in order. They are expected to sum to one.</param>
	/// <param name="length">The container's extent along the axis.</param>
	/// <param name="gap">The space between two zones; half of it is left at each end.</param>
	/// <returns>One span per zone, in order.</returns>
	public static DividerZoneSpan[] Spans(IReadOnlyList<float> fractions, float length, float gap)
	{
		Ensure.NotNull(fractions);
		int count = fractions.Count;
		DividerZoneSpan[] spans = new DividerZoneSpan[count];
		if (count == 0)
		{
			return spans;
		}

		if (count == 1)
		{
			spans[0] = new DividerZoneSpan(0f, MathF.Floor(length));
			return spans;
		}

		float usable = Usable(count, length, gap);
		float before = 0f;
		for (int i = 0; i < count; i++)
		{
			float start = MathF.Round(ExactStart(i, before, usable, gap), MidpointRounding.AwayFromZero);
			before += fractions[i];
			float end = MathF.Round(ExactStart(i, before, usable, gap), MidpointRounding.AwayFromZero);
			spans[i] = new DividerZoneSpan(start, Math.Max(end - start, 0f));
		}

		return spans;
	}

	/// <summary>Where the divider after a zone is drawn: the middle of the gap between it and the next.</summary>
	/// <param name="spans">The spans <see cref="Spans"/> returned.</param>
	/// <param name="index">The zone the divider follows. It must not be the last.</param>
	/// <returns>The divider's position, from the container's leading edge.</returns>
	public static float DividerPosition(IReadOnlyList<DividerZoneSpan> spans, int index)
	{
		Ensure.NotNull(spans);
		return (spans[index].End + spans[index + 1].Start) * 0.5f;
	}

	/// <summary>
	/// The fraction a zone has to take for the divider after it to sit at <paramref name="position"/>,
	/// the inverse of <see cref="Spans"/> for that one divider.
	/// </summary>
	/// <param name="fractions">The zones' current fractions.</param>
	/// <param name="index">The zone being resized, which the divider follows.</param>
	/// <param name="position">Where the divider is wanted, from the container's leading edge.</param>
	/// <param name="length">The container's extent along the axis.</param>
	/// <param name="gap">The space between two zones.</param>
	/// <returns>The zone's new fraction, unclamped.</returns>
	public static float FractionForDividerAt(IReadOnlyList<float> fractions, int index, float position, float length, float gap)
	{
		Ensure.NotNull(fractions);
		float usable = Usable(fractions.Count, length, gap);
		if (usable <= 0f)
		{
			return fractions[index];
		}

		float before = 0f;
		for (int i = 0; i < index; i++)
		{
			before += fractions[i];
		}

		// The divider is half a gap past the zone's end, which is the zone's start plus its share.
		float start = ExactStart(index, before, usable, gap);
		return (position - (gap * 0.5f) - start) / usable;
	}

	private static float Usable(int count, float length, float gap) =>
		count < 2 ? length : Math.Max(length - (count * gap), 0f);

	// Where zone i starts: half a gap, a whole one for each zone before it, and their shares.
	private static float ExactStart(int index, float fractionBefore, float usable, float gap) =>
		(gap * 0.5f) + (index * gap) + (usable * fractionBefore);
}
