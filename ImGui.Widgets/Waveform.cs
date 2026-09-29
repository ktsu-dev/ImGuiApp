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
		private static readonly Dictionary<uint, WaveformState> States = [];

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

			bool hasLoop = WaveformState.HasLoop(loop);
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

			WaveformState state = StateFor(ImGui.GetID(label));

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
				change = state.Drag(pointer, ref playhead, loop, duration, minLoopLength);
			}
			else
			{
				state.Release();
			}

			int hoveredHandle = -1;
			if (ImGui.IsItemHovered() && !ImGui.IsItemActive() && !ImGui.GetIO().KeyShift)
			{
				hoveredHandle = WaveformState.HitLoopHandle(loop, pointer, grabOnTimeline);
			}

			if (hoveredHandle >= 0 || state.Gesture is WaveformGesture.LoopHandle or WaveformGesture.LoopCreate)
			{
				ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);
			}

			// Re-read after the drag: a region drawn out on this very frame is drawn on this frame.
			if (WaveformState.HasLoop(loop))
			{
				int hotHandle = state.ActiveLoopHandle >= 0 ? state.ActiveLoopHandle : hoveredHandle;
				DrawLoopEdge(drawList, ToX(loop[0], duration, min, max), min, max, radius, hotHandle == 0, colors);
				DrawLoopEdge(drawList, ToX(loop[1], duration, min, max), min, max, radius, hotHandle == 1, colors);
			}

			DrawPlayhead(drawList, ToX(Math.Clamp(playhead, 0f, duration), duration, min, max), min, max, radius, colors);

			return change;
		}

		private static WaveformState StateFor(uint id)
		{
			if (!States.TryGetValue(id, out WaveformState? state))
			{
				state = new WaveformState();
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
