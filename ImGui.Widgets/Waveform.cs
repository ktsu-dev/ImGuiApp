// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

/// <summary>What a <see cref="ImGuiWidgets.Waveform(string, ReadOnlySpan{float}, ReadOnlySpan{float}, float, ref float, Vector2)"/> call changed.</summary>
[Flags]
public enum WaveformChange
{
	/// <summary>Nothing moved.</summary>
	None = 0,

	/// <summary>The playhead was moved, by a click or a scrub. The caller should seek.</summary>
	Playhead = 1,

	/// <summary>The loop region was moved, resized or drawn out anew.</summary>
	Loop = 2,

	/// <summary>The view (zoom or scroll) of the <see cref="ImGuiWidgets.TimelineView"/> changed this frame.</summary>
	View = 4,
}

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Draws a waveform overview with a playhead the user can click or scrub to seek.
	/// </summary>
	/// <param name="label">A unique label, used for the ImGui ID and the probe name.</param>
	/// <param name="minimums">The lowest sample in each column of the overview, in -1..1.</param>
	/// <param name="maximums">The highest sample in each column of the overview, in -1..1.</param>
	/// <param name="duration">The length of the timeline, in whatever unit the caller measures position in.</param>
	/// <param name="playhead">The playhead's position on the timeline, updated in place while the user seeks.</param>
	/// <param name="size">The box to draw into. Non-positive components fall back to sensible defaults.</param>
	/// <returns>What the user changed this frame.</returns>
	/// <remarks>
	/// See the overload taking a loop region for how the overview is drawn and how the timeline is
	/// mapped; this one is that overload with no loop.
	/// </remarks>
	public static WaveformChange Waveform(
		string label,
		ReadOnlySpan<float> minimums,
		ReadOnlySpan<float> maximums,
		float duration,
		ref float playhead,
		Vector2 size = default) =>
		WaveformImpl.Draw(label, minimums, maximums, duration, ref playhead, [], size, 0f);

	/// <summary>
	/// Draws a waveform overview with a playhead and a loop region: click or drag to seek, drag a
	/// loop edge to move it, and Shift-drag to draw out a new loop region.
	/// </summary>
	/// <param name="label">A unique label, used for the ImGui ID and the probe name.</param>
	/// <param name="minimums">The lowest sample in each column of the overview, in -1..1.</param>
	/// <param name="maximums">The highest sample in each column of the overview, in -1..1.</param>
	/// <param name="duration">The length of the timeline, in whatever unit the caller measures position in.</param>
	/// <param name="playhead">The playhead's position on the timeline, updated in place while the user seeks.</param>
	/// <param name="loopStart">The start of the loop region, updated in place.</param>
	/// <param name="loopEnd">The end of the loop region, updated in place.</param>
	/// <param name="size">The box to draw into. Non-positive components fall back to sensible defaults.</param>
	/// <param name="minLoopLength">The narrowest a loop region may be dragged or drawn to, on the timeline.</param>
	/// <returns>What the user changed this frame.</returns>
	/// <remarks>
	/// <para>
	/// <b>It takes an overview, not the audio.</b> <paramref name="minimums"/> and
	/// <paramref name="maximums"/> hold one column each of the lowest and highest sample, the way
	/// <see cref="Histogram"/> takes bins rather than the data they came from — so a full scan of
	/// the source never lands on the render thread. <see cref="ComputeWaveformPeaks"/> builds one
	/// from samples. The overview can have any number of columns: it is stretched or reduced to the
	/// box's pixel width, and a reduction keeps every peak rather than skipping columns, so a
	/// transient narrower than a pixel still shows. Values outside -1..1 are clipped to the box and
	/// non-finite ones are drawn as silence. The two spans are read up to the shorter one's length.
	/// </para>
	/// <para>
	/// <b>The loop region is a two-handle <see cref="HandleTrack"/>.</b> Its ends are kept ordered,
	/// inside 0..<paramref name="duration"/> and at least <paramref name="minLoopLength"/> apart by
	/// the same state, so they behave exactly as a handle track's do. A loop whose start equals its
	/// end is no loop at all: it is not drawn, its edges cannot be grabbed, and a press there seeks.
	/// Shift-drag is how one is created, and a Shift-click that never moves leaves the current loop
	/// alone.
	/// </para>
	/// <para>
	/// The playhead is only ever written while the user seeks, and is drawn clamped to the timeline
	/// otherwise, so a caller advancing it during playback owns it outright. A
	/// <paramref name="duration"/> that is not positive and finite draws the overview with no
	/// playhead or loop, and nothing responds to the pointer.
	/// </para>
	/// </remarks>
	public static WaveformChange Waveform(
		string label,
		ReadOnlySpan<float> minimums,
		ReadOnlySpan<float> maximums,
		float duration,
		ref float playhead,
		ref float loopStart,
		ref float loopEnd,
		Vector2 size = default,
		float minLoopLength = 0f)
	{
		Span<float> loop = [loopStart, loopEnd];
		WaveformChange change = WaveformImpl.Draw(label, minimums, maximums, duration, ref playhead, loop, size, minLoopLength);
		loopStart = loop[0];
		loopEnd = loop[1];
		return change;
	}

	/// <summary>
	/// Draws a zoomable, scrollable waveform with a playhead the user can click or scrub to seek.
	/// </summary>
	/// <param name="label">A unique label, used for the ImGui ID and the probe name.</param>
	/// <param name="peaks">Where the peaks come from; asked for the span in view, one column per pixel, every frame.</param>
	/// <param name="view">The part of the clip in view. Caller-owned, so other widgets can share it.</param>
	/// <param name="playhead">The playhead's position on the timeline, updated in place while the user seeks.</param>
	/// <param name="size">The box to draw into, scrollbar included. Non-positive components fall back to sensible defaults.</param>
	/// <returns>What the user changed this frame.</returns>
	/// <remarks>
	/// See the overload taking a loop region for how zooming and scrolling work; this one is that
	/// overload with no loop.
	/// </remarks>
	public static WaveformChange Waveform(
		string label,
		WaveformPeakSource peaks,
		TimelineView view,
		ref float playhead,
		Vector2 size = default) =>
		WaveformImpl.DrawZoomable(label, peaks, view, ref playhead, [], size, 0f);

	/// <summary>
	/// Draws a zoomable, scrollable waveform with a playhead and a loop region: click or drag to seek,
	/// drag a loop edge to move it, Shift-drag to draw out a new loop, Ctrl+wheel to zoom, Shift+wheel
	/// or the scrollbar to scroll, and middle-drag to pan.
	/// </summary>
	/// <param name="label">A unique label, used for the ImGui ID and the probe name.</param>
	/// <param name="peaks">Where the peaks come from; asked for the span in view, one column per pixel, every frame.</param>
	/// <param name="view">The part of the clip in view. Caller-owned, so other widgets can share it.</param>
	/// <param name="playhead">The playhead's position on the timeline, updated in place while the user seeks.</param>
	/// <param name="loopStart">The start of the loop region, updated in place.</param>
	/// <param name="loopEnd">The end of the loop region, updated in place.</param>
	/// <param name="size">The box to draw into, scrollbar included. Non-positive components fall back to sensible defaults.</param>
	/// <param name="minLoopLength">The narrowest a loop region may be dragged or drawn to, on the timeline.</param>
	/// <returns>What the user changed this frame; <see cref="WaveformChange.View"/> when the view moved.</returns>
	/// <remarks>
	/// <para>
	/// <b>The view belongs to the caller.</b> <paramref name="view"/> is a plain object rather than
	/// state hidden behind the label, so a caller can zoom it from a button, persist it, or hand the
	/// same instance to another timeline widget over the same clip. Each call sets its duration to
	/// <paramref name="peaks"/>' own, which keeps a view that was showing the whole clip showing
	/// the whole clip when the clip changes length.
	/// </para>
	/// <para>
	/// <b>The peaks are asked for, not handed in.</b> What a pixel covers changes with the zoom, so
	/// a fixed overview cannot serve every zoom. <see cref="WaveformPeakCache"/> answers from a
	/// multi-resolution summary and keeps a single-sample transient visible however far out the view
	/// is; derive from <see cref="WaveformPeakSource"/> to answer from anything else.
	/// </para>
	/// <para>
	/// The view follows the playhead: when a playhead that was in view leaves it, the view pages so
	/// the playhead is at its left edge. A playhead the user scrolled away from is left alone until
	/// it is back in view, and <see cref="TimelineView.FollowPlayhead"/> turns this off. Dragging the
	/// playhead or a loop edge past either end of the waveform scrolls the view, two views a second.
	/// </para>
	/// <para>
	/// The loop behaves as in the overview overloads. A playhead or loop edge outside the view is
	/// neither drawn nor grabbable. A <see cref="WaveformPeakSource.Duration"/> that is not positive
	/// and finite draws an empty waveform, nothing responds to the pointer, and the peaks are never
	/// asked for.
	/// </para>
	/// </remarks>
	public static WaveformChange Waveform(
		string label,
		WaveformPeakSource peaks,
		TimelineView view,
		ref float playhead,
		ref float loopStart,
		ref float loopEnd,
		Vector2 size = default,
		float minLoopLength = 0f)
	{
		Span<float> loop = [loopStart, loopEnd];
		WaveformChange change = WaveformImpl.DrawZoomable(label, peaks, view, ref playhead, loop, size, minLoopLength);
		loopStart = loop[0];
		loopEnd = loop[1];
		return change;
	}

	/// <summary>
	/// Reduces <paramref name="samples"/> to a waveform overview of as many columns as the output spans hold.
	/// </summary>
	/// <param name="samples">The samples, typically -1..1.</param>
	/// <param name="minimums">Receives the lowest sample in each column.</param>
	/// <param name="maximums">Receives the highest sample in each column.</param>
	/// <remarks>
	/// The column count is the shorter output's length. Samples are divided among the columns as
	/// evenly as whole samples allow; when there are fewer samples than columns, neighboring columns
	/// share a sample rather than any being left empty. Non-finite samples are skipped, and a column
	/// with nothing finite in it, or no samples at all, reads as silence.
	/// </remarks>
	public static void ComputeWaveformPeaks(ReadOnlySpan<float> samples, Span<float> minimums, Span<float> maximums)
	{
		int columns = Math.Min(minimums.Length, maximums.Length);

		for (int column = 0; column < columns; column++)
		{
			float low = 0f;
			float high = 0f;

			if (samples.Length > 0)
			{
				int start = (int)((long)column * samples.Length / columns);
				int end = Math.Max(start + 1, (int)((long)(column + 1) * samples.Length / columns));
				WaveformImpl.Range(samples[start..Math.Min(end, samples.Length)], out low, out high);
			}

			minimums[column] = low;
			maximums[column] = high;
		}
	}

	internal static class WaveformImpl
	{
		private static readonly Dictionary<uint, TimelineGestureState> States = [];
		private static readonly Dictionary<uint, (float[] Minimums, float[] Maximums)> PeakBuffers = [];

		public static WaveformChange Draw(
			string label,
			ReadOnlySpan<float> minimums,
			ReadOnlySpan<float> maximums,
			float duration,
			ref float playhead,
			Span<float> loop,
			Vector2 size,
			float minLoopLength)
		{
			Ensure.NotNull(label);

			float lineHeight = ImGui.GetTextLineHeight();
			Vector2 boxSize = new(
				size.X > 0 ? size.X : ImGui.CalcItemWidth(),
				size.Y > 0 ? size.Y : lineHeight * 4.0f);
			boxSize = Vector2.Max(boxSize, Vector2.One);

			Vector2 min = ImGui.GetCursorScreenPos();
			Vector2 max = min + boxSize;

			ImGui.InvisibleButton(label, boxSize);
			ImGuiProbes.MarkItem(label);

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			Span<Vector4> colors = ImGui.GetStyle().Colors;

			drawList.AddRectFilled(min, max, ImGui.GetColorU32(colors[(int)ImGuiCol.FrameBg]));

			bool hasTimeline = float.IsFinite(duration) && duration > 0f;
			if (!hasTimeline)
			{
				DrawPeaks(drawList, minimums, maximums, min, max, colors);
				return WaveformChange.None;
			}

			bool hasLoop = TimelineGestureState.HasLoop(loop);
			if (hasLoop)
			{
				HandleTrackState.Normalize(loop, 0f, duration, minLoopLength);
			}

			// Drawn before the peaks so the region reads as a highlight behind the signal rather
			// than a tint laid over it.
			if (hasLoop)
			{
				drawList.AddRectFilled(
					new Vector2(ToX(loop[0], duration, min, max), min.Y),
					new Vector2(ToX(loop[1], duration, min, max), max.Y),
					ImGui.GetColorU32(colors[(int)ImGuiCol.TextSelectedBg]));
			}

			DrawPeaks(drawList, minimums, maximums, min, max, colors);

			TimelineGestureState state = StateFor(ImGui.GetID(label));

			// The grab radius is how big a loop edge's tab is drawn, so what can be grabbed is what
			// can be seen. It is given in pixels and compared on the timeline, so it crosses the same
			// mapping the pointer does.
			float radius = MathF.Max(lineHeight * 0.4f, 5.0f);
			float grabOnTimeline = radius / boxSize.X * duration;
			float pointer = Math.Clamp((ImGui.GetIO().MousePos.X - min.X) / boxSize.X, 0f, 1f) * duration;

			WaveformChange change = WaveformChange.None;

			if (ImGui.IsItemActivated())
			{
				state.Press(loop, pointer, grabOnTimeline, ImGui.GetIO().KeyShift);
			}

			if (ImGui.IsItemActive())
			{
				change = (WaveformChange)state.Drag(pointer, ref playhead, loop, duration, minLoopLength);
			}
			else
			{
				state.Release();
			}

			int hoveredHandle = -1;
			if (ImGui.IsItemHovered() && !ImGui.IsItemActive() && !ImGui.GetIO().KeyShift)
			{
				hoveredHandle = TimelineGestureState.HitLoopHandle(loop, pointer, grabOnTimeline);
			}

			if (hoveredHandle >= 0 || state.Gesture is TimelineGesture.LoopHandle or TimelineGesture.LoopCreate)
			{
				ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);
			}

			// Re-read after the drag: a region drawn out on this very frame is drawn on this frame.
			if (TimelineGestureState.HasLoop(loop))
			{
				int hotHandle = state.ActiveLoopHandle >= 0 ? state.ActiveLoopHandle : hoveredHandle;
				DrawLoopEdge(drawList, ToX(loop[0], duration, min, max), min, max, radius, hotHandle == 0, colors);
				DrawLoopEdge(drawList, ToX(loop[1], duration, min, max), min, max, radius, hotHandle == 1, colors);
			}

			DrawPlayhead(drawList, ToX(Math.Clamp(playhead, 0f, duration), duration, min, max), min, max, radius, colors);

			return change;
		}

		public static WaveformChange DrawZoomable(
			string label,
			WaveformPeakSource peaks,
			TimelineView view,
			ref float playhead,
			Span<float> loop,
			Vector2 size,
			float minLoopLength)
		{
			Ensure.NotNull(label);
			Ensure.NotNull(peaks);
			Ensure.NotNull(view);

			ZoomLayout layout = ZoomLayout.At(ImGui.GetCursorScreenPos(), size);

			ImGui.InvisibleButton(label, layout.Max - layout.Min, ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonMiddle);
			ImGuiProbes.MarkItem(label);
			ImGuiProbes.MarkRegion($"{label}/scrollbar", layout.StripMin, layout.Max);

			bool hovered = ImGui.IsItemHovered();
			bool active = ImGui.IsItemActive();
			bool activated = ImGui.IsItemActivated();

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			Span<Vector4> colors = ImGui.GetStyle().Colors;
			drawList.AddRectFilled(layout.Min, layout.AreaMax, ImGui.GetColorU32(colors[(int)ImGuiCol.FrameBg]));

			TimelineChange change = view.SetDuration(peaks.Duration) ? TimelineChange.View : TimelineChange.None;

			if (layout.AreaWidth < 1f || layout.AreaMax.Y - layout.Min.Y <= 0f)
			{
				return (WaveformChange)change;
			}

			uint id = ImGui.GetID(label);
			TimelineGestureState state = StateFor(id);

			// An empty timeline has nothing to point at and nothing to ask the peaks for.
			if (view.Duration <= 0f)
			{
				state.Release();
				DrawCentreLine(drawList, layout.Min, layout.AreaMax, colors);
				TimelineViewChrome.DrawScrollbar(drawList, layout.StripMin, layout.Max, view, hovered: false, active: false);
				return (WaveformChange)change;
			}

			if (TimelineGestureState.HasLoop(loop))
			{
				HandleTrackState.Normalize(loop, 0f, view.Duration, minLoopLength);
			}

			ZoomPointer pointer = ZoomPointer.Read(layout, view);

			if (hovered)
			{
				change |= TimelineViewChrome.HandleWheel(view, pointer.Position) ? TimelineChange.View : TimelineChange.None;
			}

			if (activated)
			{
				change |= Press(state, view, loop, layout, pointer);
			}

			change |= active
				? DragZoomable(state, view, ref playhead, loop, minLoopLength, pointer)
				: Release(state);

			if (state.Gesture is not (TimelineGesture.Scrub or TimelineGesture.Pan or TimelineGesture.ScrollThumb) && view.Follow(playhead))
			{
				change |= TimelineChange.View;
			}

			int hoveredHandle = hovered && !active ? HoveredHandle(view, loop, layout, pointer) : -1;
			SetZoomableCursor(state, hoveredHandle);

			// Everything below is drawn for the view as it stands after this frame's input, so a
			// zoom, a scroll or a page shows on the frame it happens.
			drawList.PushClipRect(layout.Min, layout.AreaMax, true);
			DrawZoomableTimeline(drawList, id, peaks, view, playhead, loop, layout, state.ActiveLoopHandle >= 0 ? state.ActiveLoopHandle : hoveredHandle, colors);
			drawList.PopClipRect();

			bool stripHovered = hovered && pointer.Screen.Y >= layout.StripMin.Y;
			TimelineViewChrome.DrawScrollbar(drawList, layout.StripMin, layout.Max, view, stripHovered, state.Gesture == TimelineGesture.ScrollThumb);

			return (WaveformChange)change;
		}

		private static TimelineChange Press(TimelineGestureState state, TimelineView view, ReadOnlySpan<float> loop, ZoomLayout layout, ZoomPointer pointer)
		{
			if (ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
			{
				state.PressPan(pointer.Fraction, view);
				return TimelineChange.None;
			}

			if (pointer.Screen.Y >= layout.StripMin.Y)
			{
				return state.PressScrollbar(pointer.StripFraction, view, layout.MinThumbFraction);
			}

			bool createLoop = ImGui.GetIO().KeyShift;
			float grab = layout.GrabOnTimeline(view);
			int hit = createLoop ? -1 : TimelineGestureState.HitLoopHandle(loop, pointer.Position, grab);

			// A handle the view has scrolled past is not drawn, so it cannot be grabbed either: the
			// press seeks, as it would anywhere else nothing is drawn.
			bool hitOutOfView = hit >= 0 && !InView(view, loop[hit]);
			state.Press(hitOutOfView ? [] : loop, pointer.Position, grab, createLoop);
			return TimelineChange.None;
		}

		private static TimelineChange DragZoomable(TimelineGestureState state, TimelineView view, ref float playhead, Span<float> loop, float minLoopLength, ZoomPointer pointer)
		{
			switch (state.Gesture)
			{
				case TimelineGesture.Pan:
					return state.DragView(pointer.Fraction, view);

				case TimelineGesture.ScrollThumb:
					return state.DragView(pointer.StripFraction, view);

				case TimelineGesture.Scrub:
				case TimelineGesture.LoopHandle:
				case TimelineGesture.LoopCreate:
					// Scrolled first, then read: the drag lands where the pointer is in the view the
					// user will see this frame.
					TimelineChange change = view.EdgeScroll(pointer.Fraction, ImGui.GetIO().DeltaTime) ? TimelineChange.View : TimelineChange.None;
					return change | state.Drag(view.FractionToPosition(pointer.ClampedFraction), ref playhead, loop, view.Duration, minLoopLength);

				case TimelineGesture.None:
				default:
					return TimelineChange.None;
			}
		}

		private static TimelineChange Release(TimelineGestureState state)
		{
			state.Release();
			return TimelineChange.None;
		}

		private static int HoveredHandle(TimelineView view, ReadOnlySpan<float> loop, ZoomLayout layout, ZoomPointer pointer)
		{
			if (ImGui.GetIO().KeyShift || pointer.Screen.Y >= layout.StripMin.Y)
			{
				return -1;
			}

			int handle = TimelineGestureState.HitLoopHandle(loop, pointer.Position, layout.GrabOnTimeline(view));
			return handle >= 0 && InView(view, loop[handle]) ? handle : -1;
		}

		private static void SetZoomableCursor(TimelineGestureState state, int hoveredHandle)
		{
			if (state.Gesture == TimelineGesture.Pan)
			{
				ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
			}
			else if (hoveredHandle >= 0 || state.Gesture is TimelineGesture.LoopHandle or TimelineGesture.LoopCreate)
			{
				ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);
			}
		}

		private static void DrawZoomableTimeline(ImDrawListPtr drawList, uint id, WaveformPeakSource peaks, TimelineView view, float playhead, ReadOnlySpan<float> loop, ZoomLayout layout, int hotHandle, ReadOnlySpan<Vector4> colors)
		{
			bool hasLoop = TimelineGestureState.HasLoop(loop);
			if (hasLoop)
			{
				drawList.AddRectFilled(
					new Vector2(layout.ToX(view, loop[0]), layout.Min.Y),
					new Vector2(layout.ToX(view, loop[1]), layout.AreaMax.Y),
					ImGui.GetColorU32(colors[(int)ImGuiCol.TextSelectedBg]));
			}

			DrawCentreLine(drawList, layout.Min, layout.AreaMax, colors);
			DrawViewPeaks(drawList, id, peaks, view, layout.Min, layout.AreaMax, colors);

			for (int handle = 0; hasLoop && handle < 2; handle++)
			{
				if (InView(view, loop[handle]))
				{
					DrawLoopEdge(drawList, layout.ToX(view, loop[handle]), layout.Min, layout.AreaMax, layout.Radius, hotHandle == handle, colors);
				}
			}

			if (float.IsFinite(playhead) && InView(view, playhead))
			{
				DrawPlayhead(drawList, layout.ToX(view, playhead), layout.Min, layout.AreaMax, layout.Radius, colors);
			}
		}

		/// <summary>Where a zoomable waveform's parts are on screen: the waveform area above, the scrollbar strip below.</summary>
		private readonly record struct ZoomLayout(Vector2 Min, Vector2 AreaMax, Vector2 StripMin, Vector2 Max, float Radius)
		{
			public float AreaWidth => AreaMax.X - Min.X;

			public float MinThumbFraction => TimelineViewChrome.MinThumbFraction(Max.X - StripMin.X);

			public static ZoomLayout At(Vector2 min, Vector2 size)
			{
				ImGuiStylePtr style = ImGui.GetStyle();
				float lineHeight = ImGui.GetTextLineHeight();
				Vector2 boxSize = new(
					size.X > 0 ? size.X : ImGui.CalcItemWidth(),
					size.Y > 0 ? size.Y : (lineHeight * 4.0f) + style.ScrollbarSize);
				boxSize = Vector2.Max(boxSize, Vector2.One);

				// The strip never takes more than half the box, so a short waveform keeps something to show.
				float stripHeight = MathF.Min(style.ScrollbarSize, boxSize.Y * 0.5f);
				Vector2 max = min + boxSize;
				return new(min, new Vector2(max.X, max.Y - stripHeight), new Vector2(min.X, max.Y - stripHeight), max, MathF.Max(lineHeight * 0.4f, 5.0f));
			}

			/// <summary>The grab radius on the timeline, so a loop edge's tab stays the size it is drawn at whatever the zoom.</summary>
			public float GrabOnTimeline(TimelineView view) => Radius / AreaWidth * view.ViewLength;

			public float ToX(TimelineView view, float position) => Min.X + (view.PositionToFraction(position) * AreaWidth);
		}

		/// <summary>Where the pointer is, in each of the coordinates a zoomable waveform reads it in.</summary>
		private readonly record struct ZoomPointer(Vector2 Screen, float Fraction, float ClampedFraction, float StripFraction, float Position)
		{
			public static ZoomPointer Read(ZoomLayout layout, TimelineView view)
			{
				Vector2 screen = ImGui.GetIO().MousePos;

				// Unclamped: a drag beyond either end is what edge scrolling and panning read.
				float fraction = (screen.X - layout.Min.X) / layout.AreaWidth;
				float clamped = float.IsFinite(fraction) ? Math.Clamp(fraction, 0f, 1f) : 0f;
				float strip = (screen.X - layout.StripMin.X) / (layout.Max.X - layout.StripMin.X);
				return new(screen, fraction, clamped, strip, view.FractionToPosition(clamped));
			}
		}

		private static bool InView(TimelineView view, float position) => position >= view.ViewStart && position <= view.ViewEnd;

		private static void DrawCentreLine(ImDrawListPtr drawList, Vector2 min, Vector2 max, ReadOnlySpan<Vector4> colors)
		{
			float middle = (min.Y + max.Y) * 0.5f;
			drawList.AddLine(new Vector2(min.X, middle), new Vector2(max.X, middle), ImGui.GetColorU32(colors[(int)ImGuiCol.Border]));
		}

		private static void DrawViewPeaks(ImDrawListPtr drawList, uint id, WaveformPeakSource peaks, TimelineView view, Vector2 min, Vector2 max, ReadOnlySpan<Vector4> colors)
		{
			int width = (int)(max.X - min.X);
			if (!PeakBuffers.TryGetValue(id, out (float[] Minimums, float[] Maximums) buffers) || buffers.Minimums.Length != width)
			{
				buffers = (new float[width], new float[width]);
				PeakBuffers[id] = buffers;
			}

			Array.Clear(buffers.Minimums);
			Array.Clear(buffers.Maximums);
			peaks.GetPeaks(view.ViewStart, view.ViewEnd, buffers.Minimums, buffers.Maximums);

			float middle = (min.Y + max.Y) * 0.5f;
			float halfHeight = (max.Y - min.Y) * 0.5f;
			uint color = ImGui.GetColorU32(colors[(int)ImGuiCol.PlotLines]);

			for (int column = 0; column < width; column++)
			{
				float low = buffers.Minimums[column];
				float high = buffers.Maximums[column];
				low = float.IsFinite(low) ? Math.Clamp(low, -1f, 1f) : 0f;
				high = float.IsFinite(high) ? Math.Clamp(high, -1f, 1f) : 0f;
				if (low > high)
				{
					(low, high) = (high, low);
				}

				float top = middle - (high * halfHeight);
				float bottom = MathF.Max(middle - (low * halfHeight), top + 1f);
				float x = min.X + column;
				drawList.AddRectFilled(new Vector2(x, top), new Vector2(x + 1f, bottom), color);
			}
		}

		private static TimelineGestureState StateFor(uint id)
		{
			if (!States.TryGetValue(id, out TimelineGestureState? state))
			{
				state = new TimelineGestureState();
				States[id] = state;
			}

			return state;
		}

		internal static float ToX(float position, float duration, Vector2 min, Vector2 max) =>
			min.X + (Math.Clamp(position / duration, 0f, 1f) * (max.X - min.X));

		/// <summary>The lowest and highest finite value in <paramref name="values"/>, or silence if there is none.</summary>
		internal static void Range(ReadOnlySpan<float> values, out float low, out float high)
		{
			low = float.PositiveInfinity;
			high = float.NegativeInfinity;

			foreach (float value in values)
			{
				if (float.IsFinite(value))
				{
					low = MathF.Min(low, value);
					high = MathF.Max(high, value);
				}
			}

			if (low > high)
			{
				low = 0f;
				high = 0f;
			}
		}

		/// <summary>
		/// The extent of the overview across the fraction <paramref name="from"/>..<paramref name="to"/>
		/// of its width: the lowest minimum and highest maximum of every column that span touches.
		/// </summary>
		/// <returns><see langword="false"/> when the overview is empty; otherwise <see langword="true"/>.</returns>
		/// <remarks>
		/// Taking every column the span touches, rather than the one at its centre, is what keeps a
		/// one-column spike visible when the overview has more columns than the box has pixels.
		/// </remarks>
		internal static bool Reduce(ReadOnlySpan<float> minimums, ReadOnlySpan<float> maximums, float from, float to, out float low, out float high)
		{
			int count = Math.Min(minimums.Length, maximums.Length);
			if (count == 0)
			{
				low = 0f;
				high = 0f;
				return false;
			}

			int first = Math.Clamp((int)MathF.Floor(from * count), 0, count - 1);
			int last = Math.Clamp((int)MathF.Ceiling(to * count), first + 1, count);

			Range(minimums[first..last], out low, out _);
			Range(maximums[first..last], out _, out high);

			low = Math.Clamp(low, -1f, 1f);
			high = Math.Clamp(high, -1f, 1f);
			if (low > high)
			{
				(low, high) = (high, low);
			}

			return true;
		}

		private static void DrawPeaks(ImDrawListPtr drawList, ReadOnlySpan<float> minimums, ReadOnlySpan<float> maximums, Vector2 min, Vector2 max, ReadOnlySpan<Vector4> colors)
		{
			float width = max.X - min.X;
			float middle = (min.Y + max.Y) * 0.5f;
			float halfHeight = (max.Y - min.Y) * 0.5f;

			drawList.AddLine(new Vector2(min.X, middle), new Vector2(max.X, middle), ImGui.GetColorU32(colors[(int)ImGuiCol.Border]));

			uint color = ImGui.GetColorU32(colors[(int)ImGuiCol.PlotLines]);
			int pixels = (int)MathF.Ceiling(width);

			for (int column = 0; column < pixels; column++)
			{
				if (!Reduce(minimums, maximums, column / width, (column + 1) / width, out float low, out float high))
				{
					return;
				}

				// At least a pixel tall, so silence still reads as a flat line along the centre
				// rather than a gap in the signal.
				float top = middle - (high * halfHeight);
				float bottom = MathF.Max(middle - (low * halfHeight), top + 1f);
				float x = min.X + column;
				drawList.AddRectFilled(new Vector2(x, top), new Vector2(MathF.Min(x + 1f, max.X), bottom), color);
			}
		}

		private static void DrawLoopEdge(ImDrawListPtr drawList, float x, Vector2 min, Vector2 max, float radius, bool hot, ReadOnlySpan<Vector4> colors)
		{
			uint color = ImGui.GetColorU32(hot ? colors[(int)ImGuiCol.SliderGrabActive] : colors[(int)ImGuiCol.SliderGrab]);
			drawList.AddLine(new Vector2(x, min.Y), new Vector2(x, max.Y), color, hot ? 2f : 1f);

			// The tab is what a user reaches for, so it spans the grab radius either side of the edge.
			drawList.AddRectFilled(new Vector2(x - radius, min.Y), new Vector2(x + radius, min.Y + radius), color);
		}

		private static void DrawPlayhead(ImDrawListPtr drawList, float x, Vector2 min, Vector2 max, float radius, ReadOnlySpan<Vector4> colors)
		{
			uint color = ImGui.GetColorU32(colors[(int)ImGuiCol.PlotLinesHovered]);
			drawList.AddLine(new Vector2(x, min.Y), new Vector2(x, max.Y), color, 2f);
			drawList.AddTriangleFilled(
				new Vector2(x - radius, max.Y),
				new Vector2(x + radius, max.Y),
				new Vector2(x, max.Y - radius),
				color);
		}
	}
}
