// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// A window onto a timeline of <see cref="Duration"/> units (seconds by convention). Caller-owned;
	/// share one instance between every widget showing the same clip.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A timeline is the thing two widgets over one clip have to agree on, so it is a plain object
	/// rather than per-widget state: a zoomable <see cref="Waveform(string, WaveformPeakSource, TimelineView, ref float, System.Numerics.Vector2)"/>
	/// and a transport scrubber handed the same instance zoom and scroll together. It holds no ImGui
	/// state and can be driven from anywhere.
	/// </para>
	/// <para>
	/// After every call the view holds these invariants: <see cref="Duration"/> is 0, or finite and
	/// positive; when it is 0 the view is (0, 0) and <see cref="IsShowingAll"/> is true; otherwise
	/// <see cref="MinViewLength"/> ≤ <see cref="ViewLength"/> ≤ <see cref="Duration"/> and
	/// 0 ≤ <see cref="ViewStart"/> ≤ <see cref="Duration"/> − <see cref="ViewLength"/>. Every method
	/// returning <see langword="bool"/> reports whether the view changed.
	/// </para>
	/// </remarks>
	public sealed class TimelineView
	{
		private float previousPlayhead = float.NaN;

		/// <summary>Gets the length of the whole timeline. 0 when unset or invalid.</summary>
		public float Duration { get; private set; }

		/// <summary>Gets the timeline position at the left edge of the view.</summary>
		public float ViewStart { get; private set; }

		/// <summary>Gets the timeline length the view spans.</summary>
		public float ViewLength { get; private set; }

		/// <summary>Gets the timeline position at the right edge of the view: <see cref="ViewStart"/> + <see cref="ViewLength"/>.</summary>
		public float ViewEnd => ViewStart + ViewLength;

		/// <summary>
		/// Gets the smallest view as a fraction of <see cref="Duration"/>. Default 1e-5 (6 ms of a
		/// 10-minute clip).
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">The value is not in (0, 1].</exception>
		public float MinViewFraction
		{
			get;
			init
			{
				if (value is not (> 0f and <= 1f))
				{
					throw new ArgumentOutOfRangeException(nameof(value), value, "The minimum view fraction must be in (0, 1].");
				}

				field = value;
			}
		} = 1e-5f;

		/// <summary>Gets the shortest the view can be: <see cref="Duration"/> × <see cref="MinViewFraction"/>.</summary>
		public float MinViewLength => Duration * MinViewFraction;

		/// <summary>Gets or sets whether <see cref="Follow"/> pages the view when the playhead leaves it. Default true.</summary>
		public bool FollowPlayhead { get; set; } = true;

		/// <summary>Gets a value indicating whether the view spans the whole timeline, an empty timeline included.</summary>
		public bool IsShowingAll => Duration <= 0f || (ViewStart <= 0f && ViewLength >= Duration);

		/// <summary>Sets the timeline length.</summary>
		/// <param name="duration">The new length. A non-finite or non-positive value empties the view.</param>
		/// <returns><see langword="true"/> when the duration or the view changed.</returns>
		/// <remarks>
		/// A view that was showing everything keeps showing everything, which is also what makes the
		/// first call on a new view show the whole clip. A zoomed view keeps its start and length,
		/// re-clamped to the new duration.
		/// </remarks>
		public bool SetDuration(float duration)
		{
			float oldDuration = Duration;
			float oldStart = ViewStart;
			float oldLength = ViewLength;

			if (!float.IsFinite(duration) || duration <= 0f)
			{
				Duration = 0f;
				ViewStart = 0f;
				ViewLength = 0f;
			}
			else if (IsShowingAll)
			{
				Duration = duration;
				ViewStart = 0f;
				ViewLength = duration;
			}
			else
			{
				Duration = duration;
				Clamp(oldStart, oldLength);
			}

			return !Same(oldDuration, Duration) || !Same(oldStart, ViewStart) || !Same(oldLength, ViewLength);
		}

		/// <summary>Sets the view, clamped to the invariants: the length first, then the start.</summary>
		/// <param name="start">The timeline position at the view's left edge.</param>
		/// <param name="length">The timeline length the view spans.</param>
		/// <returns><see langword="true"/> when the view changed. A non-finite argument or an empty timeline changes nothing.</returns>
		public bool SetView(float start, float length)
		{
			if (!float.IsFinite(start) || !float.IsFinite(length) || Duration <= 0f)
			{
				return false;
			}

			float oldStart = ViewStart;
			float oldLength = ViewLength;
			Clamp(start, length);
			return !Same(oldStart, ViewStart) || !Same(oldLength, ViewLength);
		}

		/// <summary>Shows the whole timeline: <c>SetView(0, Duration)</c>.</summary>
		/// <returns><see langword="true"/> when the view changed.</returns>
		public bool ShowAll() => SetView(0f, Duration);

		/// <summary>
		/// Divides the view length by <paramref name="factor"/> (above 1 zooms in), keeping
		/// <paramref name="anchor"/> at its fraction of the view.
		/// </summary>
		/// <param name="factor">How much to zoom by. Non-finite or non-positive factors are ignored.</param>
		/// <param name="anchor">The timeline position that stays put. One outside the view is pinned to the nearer edge.</param>
		/// <returns><see langword="true"/> when the view changed.</returns>
		public bool ZoomAt(float factor, float anchor)
		{
			if (!float.IsFinite(factor) || factor <= 0f)
			{
				return false;
			}

			float fraction = PositionToFraction(anchor);
			fraction = float.IsFinite(fraction) ? Math.Clamp(fraction, 0f, 1f) : 0f;
			float pinned = FractionToPosition(fraction);
			float length = Math.Clamp(ViewLength / factor, MinViewLength, Duration);
			return SetView(pinned - (fraction * length), length);
		}

		/// <summary>Moves the view by <paramref name="delta"/> timeline units, clamped to the timeline.</summary>
		/// <param name="delta">How far to move; positive moves later.</param>
		/// <returns><see langword="true"/> when the view changed.</returns>
		public bool ScrollBy(float delta) => SetView(ViewStart + delta, ViewLength);

		/// <summary>
		/// Called once per frame with the playhead; pages the view when a playhead that was inside it
		/// leaves it.
		/// </summary>
		/// <param name="playhead">Where the playhead is this frame. NaN is ignored.</param>
		/// <returns><see langword="true"/> when the view changed.</returns>
		/// <remarks>
		/// The playhead lands at the left edge whichever way it left, so a loop wrapping backwards
		/// shows the loop from its start. A playhead the user scrolled out of view is left alone until
		/// it comes back into view, rather than dragging the view back to it.
		/// </remarks>
		public bool Follow(float playhead)
		{
			bool wasInside = ViewStart <= previousPlayhead && previousPlayhead <= ViewEnd;
			previousPlayhead = playhead;

			if (!FollowPlayhead || !wasInside || !float.IsFinite(playhead))
			{
				return false;
			}

			return (playhead < ViewStart || playhead > ViewEnd) && SetView(playhead, ViewLength);
		}

		/// <summary>Scrolls while a drag is held beyond the view's edges, at two view lengths per second.</summary>
		/// <param name="pointerFraction">Where the pointer is, as a fraction of the view's width, unclamped.</param>
		/// <param name="deltaSeconds">The time since the last frame.</param>
		/// <returns><see langword="true"/> when the view changed.</returns>
		public bool EdgeScroll(float pointerFraction, float deltaSeconds)
		{
			if (!float.IsFinite(deltaSeconds) || deltaSeconds <= 0f || !float.IsFinite(pointerFraction))
			{
				return false;
			}

			if (pointerFraction is >= 0f and <= 1f)
			{
				return false;
			}

			float sign = pointerFraction < 0f ? -1f : 1f;
			return ScrollBy(sign * ViewLength * 2f * deltaSeconds);
		}

		/// <summary>Converts a timeline position to a fraction of the view: (position − ViewStart) / ViewLength, unclamped.</summary>
		/// <param name="position">The timeline position.</param>
		/// <returns>The fraction; 0 when the view is empty.</returns>
		public float PositionToFraction(float position) => ViewLength > 0f ? (position - ViewStart) / ViewLength : 0f;

		/// <summary>Converts a fraction of the view to a timeline position: ViewStart + fraction × ViewLength, unclamped.</summary>
		/// <param name="fraction">The fraction of the view.</param>
		/// <returns>The timeline position.</returns>
		public float FractionToPosition(float fraction) => ViewStart + (fraction * ViewLength);

		private void Clamp(float start, float length)
		{
			ViewLength = Math.Clamp(length, MinViewLength, Duration);
			ViewStart = Math.Clamp(start, 0f, Duration - ViewLength);
		}

		// Compared against float.Epsilon, the smallest denormal, so this is "exactly equal" and not a
		// tolerance, as TimelineGestureState compares positions.
		private static bool Same(float a, float b) => MathF.Abs(a - b) <= float.Epsilon;
	}
}
