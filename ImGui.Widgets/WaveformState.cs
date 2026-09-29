// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Numerics;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>What a press on a waveform started.</summary>
	internal enum WaveformGesture
	{
		/// <summary>Nothing is held.</summary>
		None,

		/// <summary>The playhead follows the pointer.</summary>
		Scrub,

		/// <summary>One of the loop region's two handles follows the pointer.</summary>
		LoopHandle,

		/// <summary>A new loop region is drawn out from where the press landed.</summary>
		LoopCreate,
	}

	/// <summary>
	/// The interaction behind <see cref="Waveform(string, ReadOnlySpan{float}, ReadOnlySpan{float}, float, ref float, ref float, ref float, Vector2, float)"/>:
	/// what a press grabbed, and where a drag moves the playhead or the loop region.
	/// </summary>
	/// <remarks>
	/// Deliberately free of ImGui, like <see cref="HandleTrackState"/>, which it delegates the loop
	/// handles to. The loop region is a two-handle track, and reusing that state rather than
	/// re-deriving it is what keeps the handles ordered, inside the timeline and at least the minimum
	/// loop length apart by the same rules a <see cref="HandleTrack"/> keeps. Callers convert the
	/// pointer to a position on the timeline and hand that in.
	/// </remarks>
	internal sealed class WaveformState
	{
		private readonly HandleTrackState loopHandles = new();
		private float anchor;

		/// <summary>Gets what the current press is doing.</summary>
		public WaveformGesture Gesture { get; private set; }

		/// <summary>Gets the index of the loop handle being dragged, or -1 when none is.</summary>
		public int ActiveLoopHandle => Gesture == WaveformGesture.LoopHandle ? loopHandles.ActiveHandle : -1;

		/// <summary>Whether <paramref name="loop"/> describes a region that is drawn and can be grabbed.</summary>
		/// <remarks>
		/// An empty loop — no handles, or both at the same position — is "no loop". Treating it as a
		/// zero-width region with two grabbable handles would make a press at that spot drag an
		/// invisible handle instead of seeking, which reads as the widget ignoring the click.
		/// </remarks>
		public static bool HasLoop(ReadOnlySpan<float> loop) => loop.Length == 2 && !Same(loop[0], loop[1]);

		/// <summary>
		/// Returns the loop handle within <paramref name="grabRadius"/> of <paramref name="value"/>,
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
		/// <param name="loop">The loop region's two ends, or an empty span when the waveform has no loop.</param>
		/// <param name="value">Where the press landed, on the timeline.</param>
		/// <param name="grabRadius">How close to a loop handle counts as grabbing it, on the timeline.</param>
		/// <param name="createLoop">Whether the press should draw out a new loop region, rather than seek.</param>
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
				Gesture = WaveformGesture.LoopCreate;
				return;
			}

			int hit = HitLoopHandle(loop, value, grabRadius);
			if (hit >= 0)
			{
				// Activate by the handle's own position rather than the pointer's: HandleTrackState
				// picks by nearest, and handing it the handle's value is what makes it pick the one
				// HitLoopHandle already chose, ties included.
				loopHandles.Activate(loop, loop[hit]);
				Gesture = WaveformGesture.LoopHandle;
				return;
			}

			Gesture = WaveformGesture.Scrub;
		}

		/// <summary>Moves whatever the press grabbed to <paramref name="value"/>.</summary>
		/// <param name="value">Where the pointer is, on the timeline.</param>
		/// <param name="playhead">The playhead, moved in place while scrubbing.</param>
		/// <param name="loop">The loop region's two ends, moved in place while a handle is dragged or a region is drawn.</param>
		/// <param name="duration">The length of the timeline.</param>
		/// <param name="minLoopLength">The narrowest a loop region may become.</param>
		/// <returns>What changed; <see cref="WaveformChange.None"/> when nothing moved.</returns>
		/// <remarks>
		/// A drag that resolves to where things already are reports no change, for the same reason
		/// <see cref="HandleTrackState.Drag"/> does: a held, stationary pointer must not mint an undo
		/// entry or a seek every frame.
		/// </remarks>
		public WaveformChange Drag(float value, ref float playhead, Span<float> loop, float duration, float minLoopLength)
		{
			float end = MathF.Max(duration, 0f);
			float position = Math.Clamp(value, 0f, end);

			switch (Gesture)
			{
				case WaveformGesture.Scrub:
					if (Same(position, playhead))
					{
						return WaveformChange.None;
					}

					playhead = position;
					return WaveformChange.Playhead;

				case WaveformGesture.LoopHandle:
					return loopHandles.Drag(loop, position, 0f, end, minLoopLength) ? WaveformChange.Loop : WaveformChange.None;

				case WaveformGesture.LoopCreate:
					return DrawOutLoop(position, loop, end, minLoopLength);

				case WaveformGesture.None:
				default:
					return WaveformChange.None;
			}
		}

		// Held on the anchor until the pointer leaves it, so a modifier-click that never moves
		// leaves the existing loop alone rather than replacing it with one of the minimum length.
		private WaveformChange DrawOutLoop(float position, Span<float> loop, float end, float minLoopLength)
		{
			float start = Math.Clamp(anchor, 0f, end);
			if (Same(position, start))
			{
				return WaveformChange.None;
			}

			float previousStart = loop[0];
			float previousEnd = loop[1];

			loop[0] = MathF.Min(start, position);
			loop[1] = MathF.Max(start, position);
			HandleTrackState.Normalize(loop, 0f, end, minLoopLength);

			return Same(loop[0], previousStart) && Same(loop[1], previousEnd) ? WaveformChange.None : WaveformChange.Loop;
		}

		// Whether two positions are the same value. Compared against float.Epsilon, the smallest
		// denormal, so this is "exactly equal" and not a tolerance: a genuinely narrow loop or a
		// one-pixel move on a long timeline must still register. Every position reaching here has
		// been clamped onto the timeline, so neither side is ever NaN or infinite.
		private static bool Same(float a, float b) => MathF.Abs(a - b) <= float.Epsilon;

		/// <summary>Ends the current press.</summary>
		public void Release()
		{
			Gesture = WaveformGesture.None;
			loopHandles.Release();
		}
	}
}
