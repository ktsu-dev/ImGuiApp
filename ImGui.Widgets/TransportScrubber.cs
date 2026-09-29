// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Probes;

/// <summary>What a <see cref="ImGuiWidgets.TransportScrubber"/> call changed.</summary>
/// <remarks>
/// Bit for bit the same as the timeline gesture flags the widget is built on, and as
/// <see cref="WaveformChange"/>, so a scrubber and a waveform over one clip report changes alike.
/// </remarks>
[Flags]
public enum TransportScrubberChange
{
	/// <summary>Nothing moved.</summary>
	None = 0,

	/// <summary>The playhead was written. The caller should seek.</summary>
	Playhead = 1,

	/// <summary>The in point, the out point, or both were written.</summary>
	InOut = 2,

	/// <summary>The shared <see cref="ImGuiWidgets.TimelineView"/> was zoomed, scrolled or paged by this call.</summary>
	View = 4,
}

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>Options for <see cref="TransportScrubber"/>. Immutable; one instance may be reused every frame.</summary>
	public sealed class TransportScrubberOptions
	{
		/// <summary>
		/// Gets the callback returning a texture id for the frame at a time, or 0 for none, in which
		/// case an empty frame is drawn. Called once per visible slot per frame, so cache on the host
		/// side. Must not throw. Null draws no thumbnail strip.
		/// </summary>
		/// <remarks>
		/// Widgets cannot load textures, because texture upload lives in <c>ktsu.ImGui.App</c>, which
		/// this library deliberately does not reference; the host decodes and uploads, and owns the
		/// textures it hands out. The widget never deletes one.
		/// </remarks>
		public Func<float, nint>? ThumbnailResolver { get; init; }

		/// <summary>
		/// Gets the thumbnail width divided by its height. Default 16/9. Values that are not finite or
		/// not positive fall back to 16/9.
		/// </summary>
		public float ThumbnailAspect { get; init; } = 16f / 9f;

		/// <summary>
		/// Gets the length of one frame, e.g. <c>1/24f</c>. When above 0, every position the widget
		/// writes is a multiple of it and the arrow keys step one frame. 0 disables snapping, and so
		/// does a value that is not finite or is negative.
		/// </summary>
		public float FrameDuration { get; init; }

		/// <summary>Gets the minimum in-to-out length. 0 means one <see cref="FrameDuration"/> (or 0 without frames).</summary>
		public float MinInOutLength { get; init; }

		/// <summary>
		/// Gets the formatter for times on the ruler and in the tooltip. Null uses <c>m:ss.fff</c>
		/// (<c>h:mm:ss.fff</c> from an hour up), in the invariant culture.
		/// </summary>
		public Func<float, string>? FormatTime { get; init; }
	}

	/// <summary>A scrub bar with a ruler, optional thumbnails, a playhead, and an in/out range, over a zoomable timeline.</summary>
	/// <param name="label">A unique label, used for the ImGui ID and the probe name.</param>
	/// <param name="duration">The length of the clip. Passed to <see cref="TimelineView.SetDuration"/> every call.</param>
	/// <param name="view">The caller-owned view, shared with any other widget showing this clip.</param>
	/// <param name="playhead">The playhead's position; written only while seeking.</param>
	/// <param name="inPoint">The start of the range. Equal to <paramref name="outPoint"/> means "no range".</param>
	/// <param name="outPoint">The end of the range.</param>
	/// <param name="options">Null uses the defaults.</param>
	/// <param name="size">The box size, scrollbar included; 0 on an axis uses the default.</param>
	/// <returns>What this call wrote. A caller that changes the refs or the view itself gets no flag.</returns>
	/// <remarks>
	/// <para>
	/// Click or drag the ruler or track to seek; drag an in/out edge to move it; Shift-drag to draw
	/// out a new range; Ctrl+wheel to zoom, Shift+wheel or the scrollbar to scroll, middle-drag to
	/// pan. While the pointer is over the box, Left/Right step the playhead a frame (Shift ten), Home
	/// and End jump to either end, and I and O mark in and out. Right-click for a menu with the same
	/// marks, Clear in/out and Show all.
	/// </para>
	/// <para>
	/// The in/out pair is a two-handle region under exactly the grab, drag, minimum-gap and
	/// Shift-drag rules of a <see cref="Waveform(string, WaveformPeakSource, TimelineView, ref float, ref float, ref float, Vector2, float)"/>'s
	/// loop, because both are built on the same gesture state. Hand both widgets the same
	/// <paramref name="view"/> and zooming or scrolling either moves both.
	/// </para>
	/// <para>
	/// Every position the widget writes lands on a <see cref="TransportScrubberOptions.FrameDuration"/>
	/// boundary. In/out points outside the clip or reversed are drawn as given until the first drag
	/// or mark normalises them. A <paramref name="duration"/> that is not positive and finite draws
	/// the ruler and track backgrounds only, ignores the pointer and the keys, and never calls the
	/// thumbnail resolver. A NaN playhead is neither drawn nor followed.
	/// </para>
	/// </remarks>
	public static TransportScrubberChange TransportScrubber(
		string label,
		float duration,
		TimelineView view,
		ref float playhead,
		ref float inPoint,
		ref float outPoint,
		TransportScrubberOptions? options = null,
		Vector2 size = default)
	{
		Span<float> region = [inPoint, outPoint];
		TransportScrubberChange change = TransportScrubberImpl.Draw(label, duration, view, ref playhead, region, options ?? TransportScrubberImpl.Defaults, size);
		inPoint = region[0];
		outPoint = region[1];
		return change;
	}

	internal static class TransportScrubberImpl
	{
		internal static readonly TransportScrubberOptions Defaults = new();

		private const string ContextPopup = "##context";
		private const int MaxTicks = 4096;

		private static readonly Dictionary<uint, TimelineGestureState> States = [];

		public static TransportScrubberChange Draw(
			string label,
			float duration,
			TimelineView view,
			ref float playhead,
			Span<float> region,
			TransportScrubberOptions options,
			Vector2 size)
		{
			Ensure.NotNull(label);
			Ensure.NotNull(view);

			float frame = float.IsFinite(options.FrameDuration) && options.FrameDuration > 0f ? options.FrameDuration : 0f;
			Func<float, string> format = options.FormatTime ?? TransportScrubberState.DefaultFormat;

			Layout layout = Layout.At(ImGui.GetCursorScreenPos(), size, options.ThumbnailResolver != null);

			ImGui.InvisibleButton(label, Vector2.Max(layout.Max - layout.Min, Vector2.One), ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonMiddle | ImGuiButtonFlags.MouseButtonRight);
			ImGuiProbes.MarkItem(label);
			ImGuiProbes.MarkRegion($"{label}/ruler", layout.Min, layout.RulerMax);
			ImGuiProbes.MarkRegion($"{label}/track", layout.TrackMin, layout.TrackMax);
			ImGuiProbes.MarkRegion($"{label}/scrollbar", layout.StripMin, layout.Max);

			bool hovered = ImGui.IsItemHovered();
			bool active = ImGui.IsItemActive();
			bool activated = ImGui.IsItemActivated();

			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			Span<Vector4> colors = ImGui.GetStyle().Colors;
			drawList.AddRectFilled(layout.Min, layout.RulerMax, ImGui.GetColorU32(colors[(int)ImGuiCol.TableHeaderBg]));
			drawList.AddRectFilled(layout.TrackMin, layout.TrackMax, ImGui.GetColorU32(colors[(int)ImGuiCol.FrameBg]));

			uint id = ImGui.GetID(label);
			TimelineGestureState state = StateFor(id);
			TimelineChange change = view.SetDuration(duration) ? TimelineChange.View : TimelineChange.None;

			// An empty timeline, or a box with no room to draw one, has nothing to point at and
			// nothing to ask the resolver for.
			bool usable = view.Duration > 0f && layout.Width >= 1f && layout.TrackHeight >= 1f;
			if (!usable)
			{
				state.Release();
				DrawContextMenu(label, view, ref playhead, region, usable: false, 0f);
				return (TransportScrubberChange)change;
			}

			float minLength = TransportScrubberState.EffectiveMinLength(options.MinInOutLength, frame);
			Pointer pointer = Pointer.Read(layout, view, frame);

			if (hovered)
			{
				change |= TimelineViewChrome.HandleWheel(view, pointer.Position) ? TimelineChange.View : TimelineChange.None;
			}

			if (activated)
			{
				change |= Press(state, view, region, layout, pointer);
			}

			if (active)
			{
				change |= Drag(state, view, ref playhead, region, minLength, frame, pointer);
			}
			else
			{
				state.Release();
			}

			change |= HandleMenuAndKeys(label, view, ref playhead, region, frame, minLength, hovered);

			if (state.Gesture is not (TimelineGesture.Scrub or TimelineGesture.Pan or TimelineGesture.ScrollThumb) && view.Follow(playhead))
			{
				change |= TimelineChange.View;
			}

			int hoveredHandle = hovered && !active ? HoveredHandle(view, region, layout, pointer) : -1;
			ShowPointerFeedback(state, hoveredHandle, hovered, pointer, format);

			// Everything below is drawn for the view as it stands after this frame's input, so a
			// zoom, a scroll or a page shows on the frame it happens.
			DrawRuler(drawList, view, layout, frame, format, colors);
			DrawTrack(drawList, view, layout, frame, options, region, state.ActiveLoopHandle >= 0 ? state.ActiveLoopHandle : hoveredHandle, colors);

			if (float.IsFinite(playhead) && InView(view, playhead))
			{
				drawList.PushClipRect(layout.Min, layout.TrackMax, true);
				DrawPlayhead(drawList, layout.ToX(view, playhead), layout, colors);
				drawList.PopClipRect();
			}

			bool stripHovered = hovered && pointer.InStrip;
			TimelineViewChrome.DrawScrollbar(drawList, layout.StripMin, layout.Max, view, stripHovered, state.Gesture == TimelineGesture.ScrollThumb);

			return (TransportScrubberChange)change;
		}

		private static TimelineChange HandleMenuAndKeys(string label, TimelineView view, ref float playhead, Span<float> region, float frame, float minLength, bool hovered)
		{
			TimelineChange change = TimelineChange.None;

			if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
			{
				ImGui.PushID(label);
				ImGui.OpenPopup(ContextPopup);
				ImGui.PopID();
			}

			if (hovered && !ImGui.GetIO().WantTextInput)
			{
				change |= HandleKeys(view, ref playhead, region, frame, minLength);
			}

			return change | DrawContextMenu(label, view, ref playhead, region, usable: true, minLength);
		}

		private static void ShowPointerFeedback(TimelineGestureState state, int hoveredHandle, bool hovered, Pointer pointer, Func<float, string> format)
		{
			if (state.Gesture == TimelineGesture.Pan)
			{
				ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);
			}
			else if (hoveredHandle >= 0 || state.Gesture is TimelineGesture.LoopHandle or TimelineGesture.LoopCreate)
			{
				ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);
			}

			if (hovered && state.Gesture == TimelineGesture.None && !pointer.InStrip)
			{
				ImGui.SetTooltip(format(pointer.Snapped));
			}
		}

		private static TimelineChange Press(TimelineGestureState state, TimelineView view, ReadOnlySpan<float> region, Layout layout, Pointer pointer)
		{
			if (ImGui.IsMouseClicked(ImGuiMouseButton.Middle))
			{
				state.PressPan(pointer.Fraction, view);
				return TimelineChange.None;
			}

			// A right press opens the menu and grabs nothing.
			if (!ImGui.IsMouseClicked(ImGuiMouseButton.Left))
			{
				return TimelineChange.None;
			}

			if (pointer.InStrip)
			{
				return state.PressScrollbar(pointer.StripFraction, view, layout.MinThumbFraction);
			}

			bool createRange = ImGui.GetIO().KeyShift;
			float grab = layout.GrabOnTimeline(view);
			int hit = createRange ? -1 : TimelineGestureState.HitLoopHandle(region, pointer.Snapped, grab);

			// An edge the view has scrolled past is not drawn, so it cannot be grabbed either: the
			// press seeks, as it would anywhere else nothing is drawn.
			bool hitOutOfView = hit >= 0 && !InView(view, region[hit]);
			state.Press(hitOutOfView ? [] : region, pointer.Snapped, grab, createRange);
			return TimelineChange.None;
		}

		private static TimelineChange Drag(TimelineGestureState state, TimelineView view, ref float playhead, Span<float> region, float minLength, float frame, Pointer pointer)
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
					// user will see this frame, on a frame boundary.
					TimelineChange change = view.EdgeScroll(pointer.Fraction, ImGui.GetIO().DeltaTime) ? TimelineChange.View : TimelineChange.None;
					float value = TransportScrubberState.Snap(view.FractionToPosition(pointer.ClampedFraction), frame);
					return change | state.Drag(value, ref playhead, region, view.Duration, minLength);

				case TimelineGesture.None:
				default:
					return TimelineChange.None;
			}
		}

		private static TimelineChange HandleKeys(TimelineView view, ref float playhead, Span<float> region, float frame, float minLength)
		{
			ImGuiIOPtr io = ImGui.GetIO();
			float duration = view.Duration;
			float target = playhead;

			if (ImGui.IsKeyPressed(ImGuiKey.RightArrow, true))
			{
				target = TransportScrubberState.StepPlayhead(target, +1, io.KeyShift, frame, view.ViewLength, duration);
			}

			if (ImGui.IsKeyPressed(ImGuiKey.LeftArrow, true))
			{
				target = TransportScrubberState.StepPlayhead(target, -1, io.KeyShift, frame, view.ViewLength, duration);
			}

			if (ImGui.IsKeyPressed(ImGuiKey.Home, false))
			{
				target = 0f;
			}

			if (ImGui.IsKeyPressed(ImGuiKey.End, false))
			{
				target = duration;
			}

			TimelineChange change = TimelineChange.None;
			if (!target.Equals(playhead))
			{
				playhead = target;
				change |= TimelineChange.Playhead;
			}

			if (ImGui.IsKeyPressed(ImGuiKey.I, false) && TransportScrubberState.MarkIn(playhead, ref region[0], ref region[1], duration, minLength))
			{
				change |= TimelineChange.Region;
			}

			if (ImGui.IsKeyPressed(ImGuiKey.O, false) && TransportScrubberState.MarkOut(playhead, ref region[0], ref region[1], duration, minLength))
			{
				change |= TimelineChange.Region;
			}

			return change;
		}

		// Drawn every frame whether or not the timeline is usable, so an open menu closes itself
		// rather than being left open and undrawn. Pushed under the label so two scrubbers in one
		// window do not share a menu.
		private static TimelineChange DrawContextMenu(string label, TimelineView view, ref float playhead, Span<float> region, bool usable, float minLength)
		{
			TimelineChange change = TimelineChange.None;

			ImGui.PushID(label);
			if (ImGui.BeginPopup(ContextPopup))
			{
				float duration = view.Duration;

				if (ImGui.MenuItem("Mark in", "I", false, usable) && TransportScrubberState.MarkIn(playhead, ref region[0], ref region[1], duration, minLength))
				{
					change |= TimelineChange.Region;
				}

				ImGuiProbes.MarkItem("Mark in");

				if (ImGui.MenuItem("Mark out", "O", false, usable) && TransportScrubberState.MarkOut(playhead, ref region[0], ref region[1], duration, minLength))
				{
					change |= TimelineChange.Region;
				}

				ImGuiProbes.MarkItem("Mark out");

				if (ImGui.MenuItem("Clear in/out", string.Empty, false, usable && TransportScrubberState.HasRange(region[0], region[1])) && TransportScrubberState.ClearInOut(ref region[0], ref region[1]))
				{
					change |= TimelineChange.Region;
				}

				ImGuiProbes.MarkItem("Clear in/out");

				if (ImGui.MenuItem("Show all", string.Empty, false, usable) && view.ShowAll())
				{
					change |= TimelineChange.View;
				}

				ImGuiProbes.MarkItem("Show all");
				ImGui.EndPopup();
			}

			ImGui.PopID();
			return change;
		}

		private static int HoveredHandle(TimelineView view, ReadOnlySpan<float> region, Layout layout, Pointer pointer)
		{
			if (ImGui.GetIO().KeyShift || pointer.InStrip)
			{
				return -1;
			}

			int handle = TimelineGestureState.HitLoopHandle(region, pointer.Snapped, layout.GrabOnTimeline(view));
			return handle >= 0 && InView(view, region[handle]) ? handle : -1;
		}

		private static void DrawRuler(ImDrawListPtr drawList, TimelineView view, Layout layout, float frame, Func<float, string> format, ReadOnlySpan<Vector4> colors)
		{
			float minSpacing = ImGui.CalcTextSize(format(view.ViewEnd)).X + 8f;
			float step = TransportScrubberState.RulerStep(view.ViewLength, layout.Width, minSpacing, frame);
			if (step <= 0f)
			{
				return;
			}

			drawList.PushClipRect(layout.Min, layout.RulerMax, true);

			uint tickColor = ImGui.GetColorU32(colors[(int)ImGuiCol.Border]);
			uint labelColor = ImGui.GetColorU32(colors[(int)ImGuiCol.TextDisabled]);
			float rulerHeight = layout.RulerMax.Y - layout.Min.Y;

			float minor = step / 5f;
			if (minor / view.ViewLength * layout.Width >= 4f)
			{
				DrawTicks(drawList, view, layout, minor, rulerHeight * 0.25f, tickColor, format: null, labelColor);
			}

			DrawTicks(drawList, view, layout, step, rulerHeight * 0.5f, tickColor, format, labelColor);

			drawList.PopClipRect();
		}

		private static void DrawTicks(ImDrawListPtr drawList, TimelineView view, Layout layout, float step, float height, uint tickColor, Func<float, string>? format, uint labelColor)
		{
			// Indexed from the start of the timeline rather than accumulated, so a tick is drawn at
			// the same place however the view got there. The first one may sit left of the view, so
			// a label whose tick has just scrolled off is still partly shown.
			double first = Math.Floor(view.ViewStart / step);
			for (int index = 0; index < MaxTicks; index++)
			{
				float time = (float)((first + index) * step);
				if (time > view.ViewEnd || time > view.Duration)
				{
					break;
				}

				float x = layout.ToX(view, time);
				drawList.AddLine(new Vector2(x, layout.RulerMax.Y - height), new Vector2(x, layout.RulerMax.Y), tickColor);

				if (format != null)
				{
					drawList.AddText(new Vector2(x + 3f, layout.Min.Y + 2f), labelColor, format(time));
				}
			}
		}

		private static void DrawTrack(ImDrawListPtr drawList, TimelineView view, Layout layout, float frame, TransportScrubberOptions options, ReadOnlySpan<float> region, int hotHandle, ReadOnlySpan<Vector4> colors)
		{
			drawList.PushClipRect(layout.TrackMin, layout.TrackMax, true);

			if (options.ThumbnailResolver is { } resolver)
			{
				DrawThumbnails(drawList, view, layout, frame, options.ThumbnailAspect, resolver, colors);
			}

			if (TimelineGestureState.HasLoop(region))
			{
				drawList.AddRectFilled(
					new Vector2(layout.ToX(view, region[0]), layout.TrackMin.Y),
					new Vector2(layout.ToX(view, region[1]), layout.TrackMax.Y),
					ImGui.GetColorU32(colors[(int)ImGuiCol.TextSelectedBg]));

				for (int handle = 0; handle < 2; handle++)
				{
					if (InView(view, region[handle]))
					{
						DrawEdge(drawList, layout.ToX(view, region[handle]), layout, hotHandle == handle, colors);
					}
				}
			}

			drawList.PopClipRect();
		}

		[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here.", Justification = "Required for native ImGui interop; the texture reference is scoped to the call and not retained.")]
		private static void DrawThumbnails(ImDrawListPtr drawList, TimelineView view, Layout layout, float frame, float aspect, Func<float, nint> resolver, ReadOnlySpan<Vector4> colors)
		{
			float ratio = float.IsFinite(aspect) && aspect > 0f ? aspect : 16f / 9f;
			float thumbWidth = layout.TrackHeight * ratio;
			(int first, int count, float slotLength) = TransportScrubberState.ThumbnailSlots(view.ViewStart, view.ViewLength, view.Duration, layout.Width, thumbWidth);
			uint outline = ImGui.GetColorU32(colors[(int)ImGuiCol.Border]);

			for (int index = 0; index < count; index++)
			{
				int slot = first + index;
				float x = layout.ToX(view, slot * slotLength);
				Vector2 min = new(x, layout.TrackMin.Y);
				Vector2 max = new(x + thumbWidth, layout.TrackMax.Y);

				nint texture = resolver(TransportScrubberState.SlotTime(slot, slotLength, view.Duration, frame));
				if (texture != 0)
				{
					unsafe
					{
						drawList.AddImage(new ImTextureRef(texId: texture), min, max);
					}
				}
				else
				{
					drawList.AddRect(min, max, outline);
				}
			}
		}

		private static void DrawEdge(ImDrawListPtr drawList, float x, Layout layout, bool hot, ReadOnlySpan<Vector4> colors)
		{
			uint color = ImGui.GetColorU32(hot ? colors[(int)ImGuiCol.SliderGrabActive] : colors[(int)ImGuiCol.SliderGrab]);
			drawList.AddLine(new Vector2(x, layout.TrackMin.Y), new Vector2(x, layout.TrackMax.Y), color, 2f);
			drawList.AddRectFilled(new Vector2(x - layout.Radius, layout.TrackMax.Y - layout.Radius), new Vector2(x + layout.Radius, layout.TrackMax.Y), color);
		}

		private static void DrawPlayhead(ImDrawListPtr drawList, float x, Layout layout, ReadOnlySpan<Vector4> colors)
		{
			uint color = ImGui.GetColorU32(colors[(int)ImGuiCol.PlotLinesHovered]);
			drawList.AddLine(new Vector2(x, layout.Min.Y), new Vector2(x, layout.TrackMax.Y), color, 2f);

			float halfWidth = layout.LineHeight * 0.25f;
			float height = MathF.Min(layout.LineHeight * 0.5f, layout.RulerMax.Y - layout.Min.Y);
			drawList.AddTriangleFilled(
				new Vector2(x - halfWidth, layout.RulerMax.Y - height),
				new Vector2(x + halfWidth, layout.RulerMax.Y - height),
				new Vector2(x, layout.RulerMax.Y),
				color);
		}

		private static bool InView(TimelineView view, float position) => position >= view.ViewStart && position <= view.ViewEnd;

		private static TimelineGestureState StateFor(uint id)
		{
			if (!States.TryGetValue(id, out TimelineGestureState? state))
			{
				state = new TimelineGestureState();
				States[id] = state;
			}

			return state;
		}

		/// <summary>Where a scrubber's parts are on screen: the ruler on top, the track, then the scrollbar strip.</summary>
		private readonly record struct Layout(Vector2 Min, Vector2 RulerMax, Vector2 TrackMin, Vector2 TrackMax, Vector2 StripMin, Vector2 Max, float LineHeight, float Radius)
		{
			public float Width => Max.X - Min.X;

			public float TrackHeight => TrackMax.Y - TrackMin.Y;

			public float MinThumbFraction => TimelineViewChrome.MinThumbFraction(Width);

			public static Layout At(Vector2 min, Vector2 size, bool hasThumbnails)
			{
				ImGuiStylePtr style = ImGui.GetStyle();
				float lineHeight = ImGui.GetTextLineHeight();
				float width = size.X > 0 ? size.X : ImGui.CalcItemWidth();
				float rulerHeight = lineHeight + 4f;
				float stripHeight = style.ScrollbarSize;
				float trackHeight = size.Y > 0
					? MathF.Max(1f, size.Y - rulerHeight - stripHeight)
					: hasThumbnails ? lineHeight * 3f : lineHeight * 1.5f;

				float right = min.X + MathF.Max(width, 0f);
				Vector2 rulerMax = new(right, min.Y + rulerHeight);
				Vector2 trackMin = new(min.X, rulerMax.Y);
				Vector2 trackMax = new(right, trackMin.Y + trackHeight);
				Vector2 stripMin = new(min.X, trackMax.Y);
				Vector2 max = new(right, stripMin.Y + stripHeight);
				return new(min, rulerMax, trackMin, trackMax, stripMin, max, lineHeight, MathF.Max(lineHeight * 0.4f, 5.0f));
			}

			/// <summary>The grab radius on the timeline, so an edge's tab stays the size it is drawn at whatever the zoom.</summary>
			public float GrabOnTimeline(TimelineView view) => Radius / Width * view.ViewLength;

			public float ToX(TimelineView view, float position) => Min.X + (view.PositionToFraction(position) * Width);
		}

		/// <summary>Where the pointer is, in each of the coordinates a scrubber reads it in.</summary>
		private readonly record struct Pointer(Vector2 Screen, float Fraction, float ClampedFraction, float StripFraction, float Position, float Snapped, bool InStrip)
		{
			public static Pointer Read(Layout layout, TimelineView view, float frame)
			{
				Vector2 screen = ImGui.GetIO().MousePos;

				// Unclamped: a drag beyond either end is what edge scrolling and panning read.
				float fraction = (screen.X - layout.Min.X) / layout.Width;
				float clamped = float.IsFinite(fraction) ? Math.Clamp(fraction, 0f, 1f) : 0f;
				float position = view.FractionToPosition(clamped);
				return new(screen, fraction, clamped, fraction, position, TransportScrubberState.Snap(position, frame), screen.Y >= layout.StripMin.Y);
			}
		}
	}
}
