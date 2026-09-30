// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;

using ktsu.Semantics.Color;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// The interaction behind <see cref="GradientEditor"/>: which stop is selected, what a press on
	/// the bar does, where a drag moves a stop, and when dragging a stop away removes it.
	/// </summary>
	/// <remarks>
	/// Deliberately free of ImGui, like <see cref="HandleTrackState"/>, which it delegates the
	/// dragging to. Stops are sorted on entry and a <see cref="HandleTrackState"/> never lets
	/// handles cross, so a stop's index is stable for the whole drag and its colour never has to
	/// move between indices. Positions and grab radii are in 0..1 bar units; the removal distance
	/// is in pixels, measured from the widget's rectangle.
	/// </remarks>
	internal sealed class GradientEditorState
	{
		/// <summary>The fewest stops a gradient keeps; removal below this is refused.</summary>
		public const int MinimumStops = 2;

		/// <summary>How far outside the widget, in pixels, a dragged stop has to go before releasing it removes it.</summary>
		public const float RemoveDistance = 24f;

		private const int StackallocLimit = 256;

		private readonly HandleTrackState track = new();

		/// <summary>Gets the index of the selected stop, or -1 when none is.</summary>
		public int SelectedIndex { get; private set; } = -1;

		/// <summary>Gets a value indicating whether releasing now would remove the selected stop.</summary>
		public bool PendingRemoval { get; private set; }

		/// <summary>Evaluates a gradient at <paramref name="t"/>, interpolating in linear RGB.</summary>
		/// <param name="stops">The stops. They need not be sorted.</param>
		/// <param name="t">Position along the gradient; clamped to 0..1, NaN treated as 0.</param>
		/// <returns>The colour at <paramref name="t"/>; fully transparent black for an empty list.</returns>
		/// <remarks>
		/// Two stops at the same position make a hard edge: below it the earlier stop wins, and at
		/// exactly that position the later one does, because the lower neighbour is the last stop at
		/// or before <paramref name="t"/> and the upper neighbour the first one strictly after it.
		/// </remarks>
		public static Color Sample(IReadOnlyList<GradientStop> stops, float t)
		{
			Ensure.NotNull(stops);

			if (stops.Count == 0)
			{
				return new Color(0, 0, 0, 0);
			}

			t = float.IsNaN(t) ? 0f : Math.Clamp(t, 0f, 1f);

			int lower = -1;
			int upper = -1;
			for (int i = 0; i < stops.Count; i++)
			{
				float position = stops[i].Position;
				if (position <= t)
				{
					if (lower < 0 || position >= stops[lower].Position)
					{
						lower = i;
					}
				}
				else if (upper < 0 || position < stops[upper].Position)
				{
					upper = i;
				}
			}

			if (lower < 0)
			{
				return stops[upper].Color;
			}

			if (upper < 0)
			{
				return stops[lower].Color;
			}

			GradientStop from = stops[lower];
			GradientStop to = stops[upper];
			return from.Color.Lerp(to.Color, (t - from.Position) / (to.Position - from.Position));
		}

		/// <summary>
		/// Puts the stops into the state every other member expects: finite positions in 0..1, at
		/// least <see cref="MinimumStops"/> of them, sorted ascending and stably.
		/// </summary>
		/// <param name="stops">The stops, fixed in place.</param>
		/// <returns><see langword="true"/> if anything changed; otherwise <see langword="false"/>.</returns>
		public bool EnsureValid(IList<GradientStop> stops)
		{
			Ensure.NotNull(stops);

			bool changed = false;

			for (int i = 0; i < stops.Count; i++)
			{
				float position = stops[i].Position;
				float fixedPosition = float.IsFinite(position) ? Math.Clamp(position, 0f, 1f) : 0f;
				if (!fixedPosition.Equals(position))
				{
					stops[i] = stops[i] with { Position = fixedPosition };
					changed = true;
				}
			}

			if (stops.Count == 0)
			{
				stops.Add(new GradientStop(0f, new Color(0, 0, 0, 1)));
				stops.Add(new GradientStop(1f, new Color(1, 1, 1, 1)));
				changed = true;
			}
			else if (stops.Count == 1)
			{
				GradientStop only = stops[0];
				stops.Add(new GradientStop(only.Position <= 0.5f ? 1f : 0f, only.Color));
				changed = true;
			}

			// An insertion sort, because it is stable and the list is almost always already sorted.
			for (int i = 1; i < stops.Count; i++)
			{
				GradientStop moving = stops[i];
				int j = i - 1;
				while (j >= 0 && stops[j].Position > moving.Position)
				{
					stops[j + 1] = stops[j];
					if (SelectedIndex == j)
					{
						SelectedIndex = j + 1;
					}

					j--;
				}

				if (j + 1 != i)
				{
					stops[j + 1] = moving;
					if (SelectedIndex == i)
					{
						SelectedIndex = j + 1;
					}

					changed = true;
				}
			}

			if (SelectedIndex >= stops.Count)
			{
				SelectedIndex = -1;
			}

			return changed;
		}

		/// <summary>
		/// Selects the stop under <paramref name="t"/> and starts dragging it, or inserts a stop there
		/// coloured by the gradient at that point when there is none.
		/// </summary>
		/// <param name="stops">The stops, sorted.</param>
		/// <param name="t">Where the press landed, in 0..1.</param>
		/// <param name="grabRadius">How close to a stop, in 0..1, counts as pressing it.</param>
		/// <returns><see langword="true"/> if a stop was inserted; otherwise <see langword="false"/>.</returns>
		public bool Press(IList<GradientStop> stops, float t, float grabRadius)
		{
			Ensure.NotNull(stops);

			PendingRemoval = false;
			t = float.IsNaN(t) ? 0f : Math.Clamp(t, 0f, 1f);

			int hit = FindStop(stops, t, grabRadius);
			bool inserted = false;
			if (hit < 0)
			{
				Color color = Sample(ToReadOnly(stops), t);
				hit = 0;
				while (hit < stops.Count && stops[hit].Position <= t)
				{
					hit++;
				}

				stops.Insert(hit, new GradientStop(t, color));
				inserted = true;
			}

			SelectedIndex = hit;
			Span<float> positions = stops.Count <= StackallocLimit ? stackalloc float[stops.Count] : new float[stops.Count];
			CopyPositions(stops, positions);
			track.Activate(positions, t);
			return inserted;
		}

		/// <summary>Moves the stop being dragged, and notes whether it has been dragged far enough away to remove.</summary>
		/// <param name="stops">The stops, sorted.</param>
		/// <param name="t">Where the pointer is, in bar units.</param>
		/// <param name="distanceOutside">How far the pointer is outside the widget, in pixels; 0 inside it.</param>
		/// <returns><see langword="true"/> if a position changed; otherwise <see langword="false"/>.</returns>
		public bool Drag(IList<GradientStop> stops, float t, float distanceOutside)
		{
			Ensure.NotNull(stops);

			Span<float> positions = stops.Count <= StackallocLimit ? stackalloc float[stops.Count] : new float[stops.Count];
			CopyPositions(stops, positions);

			bool changed = false;
			if (track.Drag(positions, t, 0f, 1f, minGap: 0f))
			{
				for (int i = 0; i < stops.Count; i++)
				{
					if (!positions[i].Equals(stops[i].Position))
					{
						stops[i] = stops[i] with { Position = positions[i] };
						changed = true;
					}
				}
			}

			PendingRemoval = SelectedIndex >= 0 && stops.Count > MinimumStops && distanceOutside > RemoveDistance;
			return changed;
		}

		/// <summary>Ends a drag, removing the dragged stop if it was released far enough away.</summary>
		/// <param name="stops">The stops.</param>
		/// <returns><see langword="true"/> if a stop was removed; otherwise <see langword="false"/>.</returns>
		public bool Release(IList<GradientStop> stops)
		{
			Ensure.NotNull(stops);

			bool removed = false;
			if (PendingRemoval && SelectedIndex >= 0 && SelectedIndex < stops.Count && stops.Count > MinimumStops)
			{
				stops.RemoveAt(SelectedIndex);
				SelectedIndex = -1;
				removed = true;
			}

			PendingRemoval = false;
			track.Release();
			return removed;
		}

		/// <summary>Removes the selected stop, unless that would leave fewer than <see cref="MinimumStops"/>.</summary>
		/// <param name="stops">The stops.</param>
		/// <returns><see langword="true"/> if a stop was removed; otherwise <see langword="false"/>.</returns>
		public bool DeleteSelected(IList<GradientStop> stops)
		{
			Ensure.NotNull(stops);

			if (SelectedIndex < 0 || SelectedIndex >= stops.Count || stops.Count <= MinimumStops)
			{
				return false;
			}

			stops.RemoveAt(SelectedIndex);
			SelectedIndex = -1;
			PendingRemoval = false;
			track.Release();
			return true;
		}

		/// <summary>Recolours the selected stop.</summary>
		/// <param name="stops">The stops.</param>
		/// <param name="color">The new colour.</param>
		/// <returns><see langword="true"/> if the colour changed; otherwise <see langword="false"/>.</returns>
		public bool SetSelectedColor(IList<GradientStop> stops, Color color)
		{
			Ensure.NotNull(stops);

			if (SelectedIndex < 0 || SelectedIndex >= stops.Count || stops[SelectedIndex].Color == color)
			{
				return false;
			}

			stops[SelectedIndex] = stops[SelectedIndex] with { Color = color };
			return true;
		}

		/// <summary>Finds the stop nearest <paramref name="t"/> within <paramref name="grabRadius"/>, ties going to the lower index.</summary>
		/// <param name="stops">The stops.</param>
		/// <param name="t">The position to search from.</param>
		/// <param name="grabRadius">The furthest a stop may be and still count.</param>
		/// <returns>The stop's index, or -1 when none is close enough.</returns>
		public static int FindStop(IList<GradientStop> stops, float t, float grabRadius)
		{
			Ensure.NotNull(stops);

			int nearest = -1;
			float bestDistance = float.MaxValue;
			for (int i = 0; i < stops.Count; i++)
			{
				float distance = MathF.Abs(stops[i].Position - t);
				if (distance <= grabRadius && distance < bestDistance)
				{
					nearest = i;
					bestDistance = distance;
				}
			}

			return nearest;
		}

		private static void CopyPositions(IList<GradientStop> stops, Span<float> positions)
		{
			for (int i = 0; i < positions.Length; i++)
			{
				positions[i] = stops[i].Position;
			}
		}

		private static IReadOnlyList<GradientStop> ToReadOnly(IList<GradientStop> stops) =>
			stops as IReadOnlyList<GradientStop> ?? [.. stops];
	}
}
