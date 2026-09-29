// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>What a press on a timeline started.</summary>
	internal enum TimelineGesture
	{
		/// <summary>Nothing is held.</summary>
		None,

		/// <summary>The playhead follows the pointer.</summary>
		Scrub,

		/// <summary>One of the region's two handles follows the pointer.</summary>
		LoopHandle,

		/// <summary>A new region is drawn out from where the press landed.</summary>
		LoopCreate,

		/// <summary>The view follows the pointer, as if the timeline were dragged along under it.</summary>
		Pan,

		/// <summary>The scrollbar thumb follows the pointer.</summary>
		ScrollThumb,
	}

	/// <summary>What a timeline gesture changed.</summary>
	/// <remarks>
	/// Bit for bit the same as <see cref="WaveformChange"/>, so a widget converts one to the other by
	/// cast. <c>ChangeFlags_MatchWaveformChangeBitForBit</c> pins that.
	/// </remarks>
	[Flags]
	internal enum TimelineChange
	{
		/// <summary>Nothing moved.</summary>
		None = 0,

		/// <summary>The playhead moved.</summary>
		Playhead = 1,

		/// <summary>The region (a waveform's loop) moved, resized or was drawn out anew.</summary>
		Region = 2,

		/// <summary>The view was zoomed or scrolled.</summary>
		View = 4,
	}

	/// <summary>
	/// The interaction behind a timeline widget: what a press grabbed, and where a drag moves the
	/// playhead, the region or the view.
	/// </summary>
	/// <remarks>
	/// Deliberately free of ImGui, like <see cref="HandleTrackState"/>, which it delegates the region
	/// handles to. The region is a two-handle track, and reusing that state rather than re-deriving
	/// it is what keeps the handles ordered, inside the timeline and at least the minimum region
	/// length apart by the same rules a <see cref="HandleTrack"/> keeps. Callers convert the pointer
	/// to a position on the timeline and hand that in. <see cref="Waveform(string, ReadOnlySpan{float}, ReadOnlySpan{float}, float, ref float, ref float, ref float, Vector2, float)"/>
	/// uses it, and so does every widget that shares a <see cref="TimelineView"/> with one.
	/// </remarks>
	internal sealed class TimelineGestureState
	{
		private readonly HandleTrackState loopHandles = new();
		private float anchor;
		private float pressFraction;
		private float pressViewStart;
		private float grabOffset;
		private float thumbLength;

		/// <summary>Gets what the current press is doing.</summary>
		public TimelineGesture Gesture { get; private set; }

		/// <summary>Gets the index of the region handle being dragged, or -1 when none is.</summary>
		public int ActiveLoopHandle => Gesture == TimelineGesture.LoopHandle ? loopHandles.ActiveHandle : -1;

		/// <summary>Whether <paramref name="loop"/> describes a region that is drawn and can be grabbed.</summary>
		/// <remarks>
		/// An empty region — no handles, or both at the same position — is "no region". Treating it as
		/// a zero-width region with two grabbable handles would make a press at that spot drag an
		/// invisible handle instead of seeking, which reads as the widget ignoring the click.
		/// </remarks>
		public static bool HasLoop(ReadOnlySpan<float> loop) => loop.Length == 2 && !Same(loop[0], loop[1]);

		/// <summary>
		/// Returns the region handle within <paramref name="grabRadius"/> of <paramref name="value"/>,
		/// or -1 when the press missed both.
		/// </summary>
		/// <remarks>
		/// Nearest wins, and a tie resolves to the lower handle, matching
		/// <see cref="HandleTrackState.Activate"/>. Unlike that method this can miss, because a press
		/// away from the handles means something else here: it seeks.
		/// </remarks>
		public static int HitLoopHandle(ReadOnlySpan<float> loop, float value, float grabRadius)
		{
			if (!HasLoop(loop))
			{
				return -1;
			}

			float toStart = MathF.Abs(loop[0] - value);
			float toEnd = MathF.Abs(loop[1] - value);
			int nearest = toEnd < toStart ? 1 : 0;
			return MathF.Min(toStart, toEnd) <= grabRadius ? nearest : -1;
		}

		/// <summary>Decides what a press at <paramref name="value"/> grabs.</summary>
		/// <param name="loop">The region's two ends, or an empty span when the timeline has no region.</param>
		/// <param name="value">Where the press landed, on the timeline.</param>
		/// <param name="grabRadius">How close to a region handle counts as grabbing it, on the timeline.</param>
		/// <param name="createLoop">Whether the press should draw out a new region, rather than seek.</param>
		/// <remarks>
		/// Nothing moves here. The widget calls <see cref="Drag"/> on the activation frame too, so the
		/// press and every later frame of the drag go through one code path, and a plain click seeks
		/// exactly where a drag would have started.
		/// </remarks>
		public void Press(ReadOnlySpan<float> loop, float value, float grabRadius, bool createLoop)
		{
			anchor = value;

			if (loop.Length == 2 && createLoop)
			{
				Gesture = TimelineGesture.LoopCreate;
				return;
			}

			int hit = HitLoopHandle(loop, value, grabRadius);
			if (hit >= 0)
			{
				// Activate by the handle's own position rather than the pointer's: HandleTrackState
				// picks by nearest, and handing it the handle's value is what makes it pick the one
				// HitLoopHandle already chose, ties included.
				loopHandles.Activate(loop, loop[hit]);
				Gesture = TimelineGesture.LoopHandle;
				return;
			}

			Gesture = TimelineGesture.Scrub;
		}

		/// <summary>Moves whatever the press grabbed to <paramref name="value"/>.</summary>
		/// <param name="value">Where the pointer is, on the timeline.</param>
		/// <param name="playhead">The playhead, moved in place while scrubbing.</param>
		/// <param name="loop">The region's two ends, moved in place while a handle is dragged or a region is drawn.</param>
		/// <param name="duration">The length of the timeline.</param>
		/// <param name="minLoopLength">The narrowest a region may become.</param>
		/// <returns>What changed; <see cref="TimelineChange.None"/> when nothing moved.</returns>
		/// <remarks>
		/// A drag that resolves to where things already are reports no change, for the same reason
		/// <see cref="HandleTrackState.Drag"/> does: a held, stationary pointer must not mint an undo
		/// entry or a seek every frame. Pan and scrollbar gestures move the view, which
		/// <see cref="DragView"/> handles; here they change nothing.
		/// </remarks>
		public TimelineChange Drag(float value, ref float playhead, Span<float> loop, float duration, float minLoopLength)
		{
			float end = MathF.Max(duration, 0f);
			float position = Math.Clamp(value, 0f, end);

			switch (Gesture)
			{
				case TimelineGesture.Scrub:
					if (Same(position, playhead))
					{
						return TimelineChange.None;
					}

					playhead = position;
					return TimelineChange.Playhead;

				case TimelineGesture.LoopHandle:
					return loopHandles.Drag(loop, position, 0f, end, minLoopLength) ? TimelineChange.Region : TimelineChange.None;

				case TimelineGesture.LoopCreate:
					return DrawOutLoop(position, loop, end, minLoopLength);

				case TimelineGesture.None:
				case TimelineGesture.Pan:
				case TimelineGesture.ScrollThumb:
				default:
					return TimelineChange.None;
			}
		}

		/// <summary>Starts dragging the view along under the pointer.</summary>
		/// <param name="fraction">Where the press landed, as a fraction of the view's width.</param>
		/// <param name="view">The view to pan.</param>
		public void PressPan(float fraction, TimelineView view)
		{
			Ensure.NotNull(view);
			pressFraction = fraction;
			pressViewStart = view.ViewStart;
			Gesture = TimelineGesture.Pan;
		}

		/// <summary>Starts dragging the scrollbar thumb.</summary>
		/// <param name="fraction">Where the press landed, as a fraction of the scrollbar track's width.</param>
		/// <param name="view">The view the scrollbar scrolls.</param>
		/// <param name="minThumbFraction">The narrowest the thumb is drawn, as a fraction of the track.</param>
		/// <returns><see cref="TimelineChange.View"/> when a press on the track jumped the view there.</returns>
		/// <remarks>
		/// A press on the thumb keeps where on the thumb it landed, so the thumb does not jump under
		/// the pointer. A press on the track centres the thumb there straight away, which is how every
		/// scrollbar a user has met behaves when the track is clicked.
		/// </remarks>
		public TimelineChange PressScrollbar(float fraction, TimelineView view, float minThumbFraction)
		{
			Ensure.NotNull(view);
			Gesture = TimelineGesture.ScrollThumb;

			(float start, float length) = ScrollThumb(view, minThumbFraction);
			thumbLength = length;

			if (fraction >= start && fraction <= start + length)
			{
				grabOffset = fraction - start;
				return TimelineChange.None;
			}

			grabOffset = length / 2f;
			return DragView(fraction, view);
		}

		/// <summary>Where the scrollbar thumb sits on its track, as fractions of the track's width.</summary>
		/// <param name="view">The view the scrollbar scrolls.</param>
		/// <param name="minThumbFraction">The narrowest the thumb is drawn, as a fraction of the track.</param>
		/// <returns>The thumb's left edge and width.</returns>
		/// <remarks>
		/// A minimum width keeps the thumb grabbable when the view is a sliver of a long clip. The
		/// thumb's travel is then shorter than the track, so its position is a fraction of that travel
		/// rather than of the track, which is what lets it still reach both ends.
		/// </remarks>
		public static (float Start, float Length) ScrollThumb(TimelineView view, float minThumbFraction)
		{
			Ensure.NotNull(view);

			if (view.Duration <= 0f)
			{
				return (0f, 1f);
			}

			float minimum = float.IsFinite(minThumbFraction) ? Math.Clamp(minThumbFraction, 0f, 1f) : 0f;
			float length = Math.Clamp(view.ViewLength / view.Duration, minimum, 1f);
			float travel = view.Duration - view.ViewLength;
			float start = travel > 0f ? view.ViewStart / travel * (1f - length) : 0f;
			return (start, length);
		}

		/// <summary>Moves the view for a pan or scrollbar drag with the pointer at <paramref name="fraction"/>.</summary>
		/// <param name="fraction">
		/// Where the pointer is: a fraction of the view's width while panning, or of the scrollbar
		/// track's width while dragging the thumb.
		/// </param>
		/// <param name="view">The view to move.</param>
		/// <returns><see cref="TimelineChange.View"/> when the view moved; otherwise <see cref="TimelineChange.None"/>.</returns>
		public TimelineChange DragView(float fraction, TimelineView view)
		{
			Ensure.NotNull(view);

			switch (Gesture)
			{
				case TimelineGesture.Pan:
					return view.SetView(pressViewStart - ((fraction - pressFraction) * view.ViewLength), view.ViewLength)
						? TimelineChange.View
						: TimelineChange.None;

				case TimelineGesture.ScrollThumb:
					float travel = 1f - thumbLength;
					if (travel <= 0f)
					{
						return TimelineChange.None;
					}

					float start = Math.Clamp(fraction - grabOffset, 0f, travel);
					return view.SetView(start / travel * (view.Duration - view.ViewLength), view.ViewLength)
						? TimelineChange.View
						: TimelineChange.None;

				case TimelineGesture.None:
				case TimelineGesture.Scrub:
				case TimelineGesture.LoopHandle:
				case TimelineGesture.LoopCreate:
				default:
					return TimelineChange.None;
			}
		}

		// Held on the anchor until the pointer leaves it, so a modifier-click that never moves
		// leaves the existing region alone rather than replacing it with one of the minimum length.
		private TimelineChange DrawOutLoop(float position, Span<float> loop, float end, float minLoopLength)
		{
			float start = Math.Clamp(anchor, 0f, end);
			if (Same(position, start))
			{
				return TimelineChange.None;
			}

			float previousStart = loop[0];
			float previousEnd = loop[1];

			loop[0] = MathF.Min(start, position);
			loop[1] = MathF.Max(start, position);
			HandleTrackState.Normalize(loop, 0f, end, minLoopLength);

			return Same(loop[0], previousStart) && Same(loop[1], previousEnd) ? TimelineChange.None : TimelineChange.Region;
		}

		// Whether two positions are the same value. Compared against float.Epsilon, the smallest
		// denormal, so this is "exactly equal" and not a tolerance: a genuinely narrow region or a
		// one-pixel move on a long timeline must still register. Every position reaching here has
		// been clamped onto the timeline, so neither side is ever NaN or infinite.
		private static bool Same(float a, float b) => MathF.Abs(a - b) <= float.Epsilon;

		/// <summary>Ends the current press.</summary>
		public void Release()
		{
			Gesture = TimelineGesture.None;
			loopHandles.Release();
		}
	}
}
