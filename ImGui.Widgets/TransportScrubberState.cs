// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Globalization;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// The rules a <see cref="TransportScrubber"/> adds on top of <see cref="TimelineGestureState"/>:
	/// frame snapping, marking in and out, stepping the playhead, laying out the thumbnail strip and
	/// the ruler, and formatting a time.
	/// </summary>
	/// <remarks>
	/// Free of ImGui, like <see cref="TimelineGestureState"/> and <see cref="HandleTrackState"/>, so every
	/// rule here is tested without a context. The gestures themselves are not repeated: the in/out
	/// pair is handed to <see cref="TimelineGestureState"/> as its two-handle region, unchanged.
	/// </remarks>
	internal static class TransportScrubberState
	{
		private static readonly float[] SecondSteps = [1f, 2f, 5f, 10f, 15f, 30f, 60f, 120f, 300f, 600f, 900f, 1800f, 3600f, 7200f, 18000f, 36000f];
		private static readonly float[] SubSecondSteps = [0.001f, 0.002f, 0.005f, 0.01f, 0.02f, 0.05f, 0.1f, 0.2f, 0.5f];
		private static readonly int[] FrameMultiples = [1, 2, 5];

		// TimeSpan cannot hold much more than this many seconds; anything beyond is shown as unknown
		// rather than thrown out of a draw call.
		private const double MaxFormattableSeconds = 9e11;

		/// <summary>Rounds <paramref name="position"/> to the nearest multiple of <paramref name="step"/>, halves away from zero.</summary>
		/// <param name="position">The position to snap.</param>
		/// <param name="step">The grid, typically one frame. Not finite or not positive leaves the position alone.</param>
		/// <returns>The snapped position, or <paramref name="position"/> unchanged when there is nothing to snap to.</returns>
		/// <remarks>
		/// Divided in single precision on purpose: the values a caller writes are floats, and a frame
		/// boundary such as 1.02 / 0.04 has to land on the half it looks like it lands on.
		/// </remarks>
		public static float Snap(float position, float step)
		{
			if (!float.IsFinite(step) || step <= 0f || !float.IsFinite(position))
			{
				return position;
			}

			return MathF.Round(position / step, MidpointRounding.AwayFromZero) * step;
		}

		/// <summary>The narrowest the in/out range may be.</summary>
		/// <param name="minInOutLength">The caller's minimum; 0 (or anything not finite and positive) means one frame.</param>
		/// <param name="frameDuration">The length of one frame; 0 (or anything not finite and positive) means no frames.</param>
		/// <returns>The minimum, one frame when none is given, or 0 without frames.</returns>
		public static float EffectiveMinLength(float minInOutLength, float frameDuration)
		{
			if (float.IsFinite(minInOutLength) && minInOutLength > 0f)
			{
				return minInOutLength;
			}

			return float.IsFinite(frameDuration) && frameDuration > 0f ? frameDuration : 0f;
		}

		/// <summary>Where the arrow keys move the playhead to.</summary>
		/// <param name="playhead">The playhead now. A non-finite playhead steps from 0.</param>
		/// <param name="direction">+1 for forward, -1 for back.</param>
		/// <param name="coarse">Whether to step ten times as far (Shift).</param>
		/// <param name="frameDuration">One frame; without frames the step is one percent of the view.</param>
		/// <param name="viewLength">The length of the view, used when there are no frames.</param>
		/// <param name="duration">The length of the clip.</param>
		/// <returns>The new playhead, on a frame boundary and inside the clip.</returns>
		public static float StepPlayhead(float playhead, int direction, bool coarse, float frameDuration, float viewLength, float duration)
		{
			float step = frameDuration > 0f ? frameDuration : viewLength * 0.01f;
			if (coarse)
			{
				step *= 10f;
			}

			float from = float.IsFinite(playhead) ? playhead : 0f;
			return Math.Clamp(Snap(from + (direction * step), frameDuration), 0f, MathF.Max(duration, 0f));
		}

		/// <summary>Sets the in point to the playhead.</summary>
		/// <param name="playhead">The playhead. A non-finite playhead marks nothing.</param>
		/// <param name="inPoint">The in point.</param>
		/// <param name="outPoint">The out point.</param>
		/// <param name="duration">The length of the clip. Not finite or not positive marks nothing.</param>
		/// <param name="minLength">The narrowest the range may be.</param>
		/// <returns><see langword="true"/> when either point changed.</returns>
		/// <remarks>
		/// With no range yet the out point goes to the end of the clip, so a single key press makes a
		/// range. An in point at or past the out point takes the out point with it, keeping the
		/// minimum length, and a mark at the very end pushes the range back instead. At the very end
		/// with a minimum of 0 and no range, both points land on the end, which reads as "no range".
		/// </remarks>
		public static bool MarkIn(float playhead, ref float inPoint, ref float outPoint, float duration, float minLength)
		{
			if (!float.IsFinite(playhead) || !float.IsFinite(duration) || duration <= 0f)
			{
				return false;
			}

			float oldIn = inPoint;
			float oldOut = outPoint;
			float position = Math.Clamp(playhead, 0f, duration);
			bool hadRange = HasRange(inPoint, outPoint);
			float minimum = float.IsFinite(minLength) ? MathF.Max(minLength, 0f) : 0f;

			inPoint = position;
			if (!hadRange)
			{
				outPoint = duration;
			}

			if (outPoint - inPoint < minimum)
			{
				outPoint = MathF.Min(duration, inPoint + minimum);
				inPoint = MathF.Max(0f, outPoint - minimum);
			}

			return Changed(oldIn, inPoint) || Changed(oldOut, outPoint);
		}

		/// <summary>Sets the out point to the playhead.</summary>
		/// <param name="playhead">The playhead. A non-finite playhead marks nothing.</param>
		/// <param name="inPoint">The in point.</param>
		/// <param name="outPoint">The out point.</param>
		/// <param name="duration">The length of the clip. Not finite or not positive marks nothing.</param>
		/// <param name="minLength">The narrowest the range may be.</param>
		/// <returns><see langword="true"/> when either point changed.</returns>
		/// <remarks>The mirror of <see cref="MarkIn"/>: with no range yet the in point goes to the start of the clip.</remarks>
		public static bool MarkOut(float playhead, ref float inPoint, ref float outPoint, float duration, float minLength)
		{
			if (!float.IsFinite(playhead) || !float.IsFinite(duration) || duration <= 0f)
			{
				return false;
			}

			float oldIn = inPoint;
			float oldOut = outPoint;
			float position = Math.Clamp(playhead, 0f, duration);
			bool hadRange = HasRange(inPoint, outPoint);
			float minimum = float.IsFinite(minLength) ? MathF.Max(minLength, 0f) : 0f;

			outPoint = position;
			if (!hadRange)
			{
				inPoint = 0f;
			}

			if (outPoint - inPoint < minimum)
			{
				inPoint = MathF.Max(0f, outPoint - minimum);
				outPoint = MathF.Min(duration, inPoint + minimum);
			}

			return Changed(oldIn, inPoint) || Changed(oldOut, outPoint);
		}

		/// <summary>Removes the range, setting both points to 0.</summary>
		/// <param name="inPoint">The in point.</param>
		/// <param name="outPoint">The out point.</param>
		/// <returns><see langword="true"/> when there was a range to remove.</returns>
		public static bool ClearInOut(ref float inPoint, ref float outPoint)
		{
			if (!HasRange(inPoint, outPoint))
			{
				return false;
			}

			inPoint = 0f;
			outPoint = 0f;
			return true;
		}

		/// <summary>Which thumbnail slots the view shows.</summary>
		/// <param name="viewStart">The start of the view.</param>
		/// <param name="viewLength">The length of the view.</param>
		/// <param name="duration">The length of the clip.</param>
		/// <param name="width">The width of the track, in pixels.</param>
		/// <param name="thumbnailWidth">The width of one thumbnail, in pixels.</param>
		/// <returns>The first visible slot, how many are visible, and how long a slot is on the timeline.</returns>
		/// <remarks>
		/// Slots are anchored to the timeline at multiples of <c>SlotLength</c>, so scrolling slides
		/// them rather than re-slicing them, and none starts past the end of the clip.
		/// </remarks>
		public static (int First, int Count, float SlotLength) ThumbnailSlots(float viewStart, float viewLength, float duration, float width, float thumbnailWidth)
		{
			if (!float.IsFinite(viewStart) || !float.IsFinite(viewLength) || !float.IsFinite(duration) || !float.IsFinite(width) || !float.IsFinite(thumbnailWidth)
				|| viewLength <= 0f || duration <= 0f || width <= 0f || thumbnailWidth <= 0f)
			{
				return (0, 0, 0f);
			}

			float slotLength = thumbnailWidth / width * viewLength;
			double first = Math.Floor(viewStart / slotLength);
			double last = Math.Ceiling(MathF.Min(viewStart + viewLength, duration) / slotLength) - 1d;

			if (!double.IsFinite(first) || !double.IsFinite(last) || first > int.MaxValue || first < int.MinValue)
			{
				return (0, 0, 0f);
			}

			double count = Math.Clamp(last - first + 1d, 0d, int.MaxValue);
			return ((int)first, (int)count, slotLength);
		}

		/// <summary>The time a thumbnail slot shows: its midpoint, on a frame boundary and inside the clip.</summary>
		/// <param name="slot">The slot index.</param>
		/// <param name="slotLength">The length of a slot on the timeline.</param>
		/// <param name="duration">The length of the clip.</param>
		/// <param name="frameDuration">One frame, or 0 for no snapping.</param>
		/// <returns>The time the thumbnail resolver is asked for.</returns>
		public static float SlotTime(int slot, float slotLength, float duration, float frameDuration) =>
			Math.Clamp(Snap((slot + 0.5f) * slotLength, frameDuration), 0f, MathF.Max(duration, 0f));

		/// <summary>The spacing of the ruler's major ticks.</summary>
		/// <param name="viewLength">The length of the view.</param>
		/// <param name="width">The width of the ruler, in pixels.</param>
		/// <param name="minPixelSpacing">The closest two major ticks may be, in pixels; wide enough for a label.</param>
		/// <param name="frameDuration">One frame, which the steps below a second are whole multiples of; 0 for none.</param>
		/// <returns>The smallest step that fits, the largest there is when none fits, or 0 for a degenerate view.</returns>
		public static float RulerStep(float viewLength, float width, float minPixelSpacing, float frameDuration)
		{
			if (!float.IsFinite(viewLength) || !float.IsFinite(width) || !float.IsFinite(minPixelSpacing)
				|| viewLength <= 0f || width <= 0f || minPixelSpacing <= 0f)
			{
				return 0f;
			}

			float frame = float.IsFinite(frameDuration) && frameDuration > 0f ? frameDuration : 0f;

			if (frame > 0f)
			{
				// 1, 2, 5, 10, 20, 50, … frames, for as long as that is under a second.
				for (float decade = 1f; decade * frame < 1f; decade *= 10f)
				{
					foreach (int multiple in FrameMultiples)
					{
						float candidate = decade * multiple * frame;
						if (candidate >= 1f)
						{
							break;
						}

						if (Fits(candidate, viewLength, width, minPixelSpacing))
						{
							return candidate;
						}
					}
				}
			}
			else
			{
				foreach (float candidate in SubSecondSteps)
				{
					if (Fits(candidate, viewLength, width, minPixelSpacing))
					{
						return candidate;
					}
				}
			}

			foreach (float candidate in SecondSteps)
			{
				if (Fits(candidate, viewLength, width, minPixelSpacing))
				{
					return candidate;
				}
			}

			return SecondSteps[^1];
		}

		/// <summary>Formats a time as <c>m:ss.fff</c>, or <c>h:mm:ss.fff</c> from an hour up, in the invariant culture.</summary>
		/// <param name="seconds">The time. Negative times are prefixed with a minus sign; non-finite ones read "--:--".</param>
		/// <returns>The formatted time.</returns>
		public static string DefaultFormat(float seconds)
		{
			if (!float.IsFinite(seconds) || Math.Abs((double)seconds) >= MaxFormattableSeconds)
			{
				return "--:--";
			}

			string sign = seconds < 0f ? "-" : string.Empty;
			TimeSpan time = TimeSpan.FromSeconds(Math.Abs((double)seconds));

			if (time.TotalSeconds < 3600d)
			{
				return sign + time.ToString(@"m\:ss\.fff", CultureInfo.InvariantCulture);
			}

			// Whole hours rather than the hours component, so a day or more is not folded back to zero.
			long hours = (long)Math.Floor(time.TotalHours);
			return sign + hours.ToString(CultureInfo.InvariantCulture) + ":" + time.ToString(@"mm\:ss\.fff", CultureInfo.InvariantCulture);
		}

		private static bool Fits(float candidate, float viewLength, float width, float minPixelSpacing) =>
			candidate / viewLength * width >= minPixelSpacing;

		// Exact comparison on purpose, as TimelineGestureState's Same is: a one-frame move on a long
		// clip must still register. NaN on the old side counts as a change.
		/// <summary>Whether an in/out pair describes a range; equal points mean there is none.</summary>
		/// <param name="inPoint">The in point.</param>
		/// <param name="outPoint">The out point.</param>
		/// <returns><see langword="true"/> when the two points differ.</returns>
		internal static bool HasRange(float inPoint, float outPoint) => !inPoint.Equals(outPoint);

		private static bool Changed(float before, float after) => !before.Equals(after);
	}
}
