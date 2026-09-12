// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// The interaction behind a set of curve control points: which one is being dragged, where a
	/// drag may move it, and how points are added and removed while the set stays ordered.
	/// </summary>
	/// <remarks>
	/// Deliberately free of ImGui, like <see cref="HandleTrackState"/>. Callers convert a mouse
	/// position to a point in the curve's own value space and hand that in, which is what lets
	/// every rule here be tested without a graphics context.
	/// </remarks>
	internal sealed class CurveTrackState
	{
		/// <summary>The fewest points a curve is allowed to be reduced to.</summary>
		internal const int MinimumPoints = 2;

		/// <summary>Gets the index of the point being dragged, or -1 when none is.</summary>
		public int ActivePoint { get; private set; } = -1;

		/// <summary>
		/// Selects the point nearest <paramref name="value"/> as the one being dragged, if one is
		/// within <paramref name="grabRadius"/>.
		/// </summary>
		/// <param name="points">The control points, ordered by x.</param>
		/// <param name="value">Where the pointer is, in the curve's value space.</param>
		/// <param name="grabRadius">How far from a point counts as grabbing it, in value space.</param>
		/// <returns><see langword="true"/> if a point was grabbed; otherwise <see langword="false"/>.</returns>
		/// <remarks>
		/// Nearest in two dimensions rather than by x alone: two points can sit at nearly the same x
		/// with very different y, and picking by x would grab whichever happened to be closer
		/// horizontally however far away it looked. Ties resolve to the lower index, so a press
		/// exactly between two points behaves the same way every time.
		/// <para>
		/// Returning false is what tells the caller the press landed on empty track, which is the
		/// gesture that adds a point. That is why this reports a miss rather than grabbing the
		/// nearest point at any distance the way <see cref="HandleTrackState.Activate"/> does: a
		/// handle track has a fixed set of handles and every press must mean one of them, and a
		/// curve has an unbounded set and a press away from all of them means a new one.
		/// </para>
		/// </remarks>
		public bool Activate(IList<Vector2> points, Vector2 value, float grabRadius)
		{
			Ensure.NotNull(points);

			int nearest = -1;
			float bestDistance = float.MaxValue;

			for (int i = 0; i < points.Count; i++)
			{
				float distance = Vector2.Distance(points[i], value);
				if (distance < bestDistance)
				{
					bestDistance = distance;
					nearest = i;
				}
			}

			ActivePoint = nearest >= 0 && bestDistance <= grabRadius ? nearest : -1;
			return ActivePoint >= 0;
		}

		/// <summary>Clears the active point.</summary>
		public void Release() => ActivePoint = -1;

		/// <summary>Makes <paramref name="index"/> the active point, for a press that added one.</summary>
		public void Grab(int index) => ActivePoint = index;

		/// <summary>
		/// Moves the active point to <paramref name="value"/>, held inside its neighbors and the bounds.
		/// </summary>
		/// <param name="points">The control points, ordered by x.</param>
		/// <param name="value">Where to move it to, in the curve's value space.</param>
		/// <param name="lowerBound">The lower corner of the value space.</param>
		/// <param name="upperBound">The upper corner of the value space.</param>
		/// <param name="minGap">The narrowest horizontal gap kept between neighboring points.</param>
		/// <param name="pinEnds">Whether the first and last points keep their x.</param>
		/// <returns><see langword="true"/> if the point moved; otherwise <see langword="false"/>.</returns>
		/// <remarks>
		/// A drag that resolves to where the point already sits reports no change, so a stationary
		/// pointer held down does not mint an undo entry per frame.
		/// <para>
		/// x is held strictly between the neighbors rather than merely clamped into the bounds. Two
		/// control points sharing an x make the curve vertical there, which is not a function of x
		/// and which no interpolation through them is defined for — so the ordering constraint is
		/// the one thing here that cannot yield, and <paramref name="minGap"/> is how far apart it
		/// keeps them.
		/// </para>
		/// </remarks>
		public bool Drag(IList<Vector2> points, Vector2 value, Vector2 lowerBound, Vector2 upperBound, float minGap, bool pinEnds)
		{
			Ensure.NotNull(points);

			if (ActivePoint < 0 || ActivePoint >= points.Count)
			{
				return false;
			}

			Vector2 current = points[ActivePoint];
			float x = IsPinned(ActivePoint, points.Count, pinEnds)
				? current.X
				: ClampBetweenNeighbors(points, ActivePoint, value.X, lowerBound.X, upperBound.X, minGap);

			float y = Math.Clamp(value.Y, MathF.Min(lowerBound.Y, upperBound.Y), MathF.Max(lowerBound.Y, upperBound.Y));
			Vector2 moved = new(x, y);

			if (moved == current)
			{
				return false;
			}

			points[ActivePoint] = moved;
			return true;
		}

		/// <summary>Inserts a point, keeping the list ordered by x.</summary>
		/// <param name="points">The control points, ordered by x.</param>
		/// <param name="value">Where to add it, in the curve's value space.</param>
		/// <param name="lowerBound">The lower corner of the value space.</param>
		/// <param name="upperBound">The upper corner of the value space.</param>
		/// <param name="minGap">The narrowest horizontal gap kept between neighboring points.</param>
		/// <returns>The index the point was inserted at, or -1 when it would not fit.</returns>
		/// <remarks>
		/// Returns -1 rather than crowding when there is no room for the new point between its
		/// neighbors. A caller that added it anyway would be adding a point at the same x as an
		/// existing one, which is the state <see cref="Drag"/> exists to prevent.
		/// </remarks>
		public static int Add(IList<Vector2> points, Vector2 value, Vector2 lowerBound, Vector2 upperBound, float minGap)
		{
			Ensure.NotNull(points);

			float low = MathF.Min(lowerBound.X, upperBound.X);
			float high = MathF.Max(lowerBound.X, upperBound.X);
			float x = Math.Clamp(value.X, low, high);
			float y = Math.Clamp(value.Y, MathF.Min(lowerBound.Y, upperBound.Y), MathF.Max(lowerBound.Y, upperBound.Y));

			int index = 0;
			while (index < points.Count && points[index].X < x)
			{
				index++;
			}

			bool clearOfLower = index == 0 || x - points[index - 1].X >= minGap;
			bool clearOfUpper = index == points.Count || points[index].X - x >= minGap;
			if (!clearOfLower || !clearOfUpper)
			{
				return -1;
			}

			points.Insert(index, new Vector2(x, y));
			return index;
		}

		/// <summary>Removes a point, unless it is pinned or the curve needs it.</summary>
		/// <param name="points">The control points, ordered by x.</param>
		/// <param name="index">The point to remove.</param>
		/// <param name="pinEnds">Whether the first and last points may not be removed.</param>
		/// <returns><see langword="true"/> if the point was removed; otherwise <see langword="false"/>.</returns>
		/// <remarks>
		/// A pinned end is pinned against removal as well as against moving in x. Pinning an end
		/// only to let it be deleted would leave the curve with no value at that edge of its domain,
		/// which is the thing pinning is for.
		/// <para>
		/// Two points are kept whatever <paramref name="pinEnds"/> says. One point is not a curve
		/// over a domain, and none is not a function at all — and the caller is drawing whatever is
		/// left, so emptying the list is not a state it can render its way out of.
		/// </para>
		/// </remarks>
		public static bool Remove(IList<Vector2> points, int index, bool pinEnds)
		{
			Ensure.NotNull(points);

			if (index < 0 || index >= points.Count || points.Count <= MinimumPoints || IsPinned(index, points.Count, pinEnds))
			{
				return false;
			}

			points.RemoveAt(index);
			return true;
		}

		/// <summary>
		/// Clamps points into the bounds, orders them by x, and opens each neighboring pair to at
		/// least <paramref name="minGap"/>.
		/// </summary>
		/// <param name="points">The control points.</param>
		/// <param name="lowerBound">The lower corner of the value space.</param>
		/// <param name="upperBound">The upper corner of the value space.</param>
		/// <param name="minGap">The narrowest horizontal gap kept between neighboring points.</param>
		/// <param name="pinEnds">Whether the first and last points are held at the bounds' x.</param>
		/// <remarks>
		/// Run before interaction, so the points are in a valid state whatever the caller passed.
		/// The gap pass walks upward and then settles back down, because pushing up alone would
		/// drive the last point past <paramref name="upperBound"/> when the gaps cannot all fit —
		/// the same two-pass shape, for the same reason, as
		/// <see cref="HandleTrackState.Normalize"/>.
		/// </remarks>
		public static void Normalize(IList<Vector2> points, Vector2 lowerBound, Vector2 upperBound, float minGap, bool pinEnds)
		{
			Ensure.NotNull(points);

			if (points.Count == 0)
			{
				return;
			}

			float lowX = MathF.Min(lowerBound.X, upperBound.X);
			float highX = MathF.Max(lowerBound.X, upperBound.X);
			float lowY = MathF.Min(lowerBound.Y, upperBound.Y);
			float highY = MathF.Max(lowerBound.Y, upperBound.Y);

			// Narrowed so the gaps can always fit, rather than fought over below.
			float gap = points.Count > 1
				? Math.Clamp(minGap, 0f, (highX - lowX) / (points.Count - 1))
				: 0f;

			List<Vector2> ordered = [.. points];
			ordered.Sort(static (a, b) => a.X.CompareTo(b.X));

			for (int i = 0; i < ordered.Count; i++)
			{
				ordered[i] = new Vector2(
					Math.Clamp(ordered[i].X, lowX, highX),
					Math.Clamp(ordered[i].Y, lowY, highY));
			}

			for (int i = 1; i < ordered.Count; i++)
			{
				ordered[i] = new Vector2(MathF.Max(ordered[i].X, ordered[i - 1].X + gap), ordered[i].Y);
			}

			for (int i = ordered.Count - 1; i >= 0; i--)
			{
				float ceiling = i == ordered.Count - 1 ? highX : ordered[i + 1].X - gap;
				ordered[i] = new Vector2(MathF.Max(MathF.Min(ordered[i].X, ceiling), lowX), ordered[i].Y);
			}

			// The pinned ends take the bounds outright, after the gap passes rather than before, so
			// nothing can push them back off the edge they are pinned to.
			if (pinEnds)
			{
				ordered[0] = new Vector2(lowX, ordered[0].Y);
				ordered[^1] = new Vector2(highX, ordered[^1].Y);
			}

			for (int i = 0; i < ordered.Count; i++)
			{
				points[i] = ordered[i];
			}
		}

		/// <summary>Gets whether a point is held at its end of the track.</summary>
		internal static bool IsPinned(int index, int count, bool pinEnds) =>
			pinEnds && (index == 0 || index == count - 1);

		private static float ClampBetweenNeighbors(IList<Vector2> points, int index, float x, float lowerX, float upperX, float minGap)
		{
			float low = MathF.Min(lowerX, upperX);
			float high = MathF.Max(lowerX, upperX);

			float floor = index > 0 ? points[index - 1].X + minGap : low;
			float ceiling = index < points.Count - 1 ? points[index + 1].X - minGap : high;

			floor = Math.Clamp(floor, low, high);
			ceiling = Math.Clamp(ceiling, low, high);
			if (ceiling < floor)
			{
				// The neighbors are closer together than the gap asks for. Order still holds; the
				// gap is what yields, exactly as it does in HandleTrackState.
				ceiling = floor;
			}

			return Math.Clamp(x, floor, ceiling);
		}
	}
}
