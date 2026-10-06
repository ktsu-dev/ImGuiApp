// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.Waveform(string, ReadOnlySpan{float}, ReadOnlySpan{float}, float, ref float, ref float, ref float, Vector2, float)"/> on its own.</summary>
[TestClass]
public sealed class WaveformTests : WidgetTest
{
	private const string Label = "overview";
	private const float Width = 400f;
	private const float Height = 80f;
	private const float Duration = 10f;

	// Half a unit is twenty pixels at this width: far wider than any rounding in where a click
	// lands, and far narrower than the distances the assertions tell apart.
	private const float Tolerance = 0.5f;

	private float[] minimums = BuildOverview(-1f);
	private float[] maximums = BuildOverview(1f);
	private float duration = Duration;
	private float playhead;
	private float loopStart = 2f;
	private float loopEnd = 6f;
	private WaveformChange changes;

	private static float[] BuildOverview(float sign)
	{
		float[] columns = new float[200];
		for (int i = 0; i < columns.Length; i++)
		{
			columns[i] = sign * (0.2f + (0.7f * MathF.Abs(MathF.Sin(i * 0.1f))));
		}

		return columns;
	}

	private void Draw() =>
		changes |= ImGuiWidgets.Waveform(Label, minimums, maximums, duration, ref playhead, ref loopStart, ref loopEnd, new Vector2(Width, Height), minLoopLength: 0.5f);

	// The zoomable overloads: a ten-second clip at 4.8 kHz, drawn into a box whose bottom strip is
	// the scrollbar. Clicks aim at a third of the way down, well clear of that strip.
	private const float ZoomHeight = 96f;
	private const int ZoomSamples = 48000;
	private const float ClickRow = 0.3f;

	private ImGuiWidgets.WaveformPeakCache cache = BuildCache(spikeAt: -1);
	private readonly ImGuiWidgets.TimelineView view = new();

	private static ImGuiWidgets.WaveformPeakCache BuildCache(int spikeAt)
	{
		float[] samples = new float[ZoomSamples];
		if (spikeAt >= 0)
		{
			samples[spikeAt] = 0.9f;
		}

		return new ImGuiWidgets.WaveformPeakCache(samples, Duration);
	}

	private void DrawZoomable() =>
		changes |= ImGuiWidgets.Waveform(Label, cache, view, ref playhead, ref loopStart, ref loopEnd, new Vector2(Width, ZoomHeight), minLoopLength: 0.5f);

	private void ZoomTo(float start, float length)
	{
		view.SetDuration(Duration);
		view.SetView(start, length);
	}

	private Vector2 PointAt(float fractionX, float fractionY = ClickRow)
	{
		Rectangle rect = RectOf(Label);
		return new Vector2(rect.MinX + (rect.Width * fractionX), rect.MinY + (rect.Height * fractionY));
	}

	private Vector2 ScrollbarPointAt(float fractionX)
	{
		Rectangle strip = RectOf($"{Label}/scrollbar");
		return new Vector2(strip.MinX + (strip.Width * fractionX), strip.MinY + (strip.Height / 2f));
	}

	// The first frame reports a view change of its own, because it is the one on which the widget
	// sizes the view to the clip. Tests of what the user changes start counting after it.
	private void StartZoomable()
	{
		Start(DrawZoomable);
		changes = WaveformChange.None;
	}

	// The modifier gets frames of its own on both sides of the notch: ImGui trickles input events,
	// so a modifier change queued behind a wheel event can be applied before the wheel is.
	private void WheelWith(ImGuiKey modifier, Vector2 point, int clicks)
	{
		HarnessKeyboard.KeyDown(modifier);
		Step();
		Harness.Mouse.Wheel(point.X, point.Y, clicks);
		Step();
		HarnessKeyboard.KeyUp(modifier);
		Step();
	}

	[TestMethod]
	public void Waveform_IsDrawnAndMarksItself()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Label), "The waveform marked no probe item.");
		AssertSomethingWasDrawn("the waveform");
	}

	[TestMethod]
	public void Waveform_ReservesTheSizeItWasGiven()
	{
		Start(Draw);

		Rectangle rect = RectOf(Label);

		Assert.IsTrue(Math.Abs(rect.Width - Width) <= 2, $"The waveform claimed {rect.Width}px of width rather than {Width}.");
		Assert.IsTrue(Math.Abs(rect.Height - Height) <= 2, $"The waveform claimed {rect.Height}px of height rather than {Height}.");
	}

	[TestMethod]
	public void Waveform_DrawsTheOverviewItIsGiven()
	{
		minimums = new float[200];
		maximums = new float[200];
		Start(Draw);
		byte[] silent = Snapshot();

		minimums = BuildOverview(-1f);
		maximums = BuildOverview(1f);
		Step();

		Assert.IsTrue(PixelsChangedSince(silent) > 1000, "A loud overview drew no differently from silence.");
	}

	[TestMethod]
	public void Waveform_ClickSeeks()
	{
		Start(Draw);

		ClickFraction(Label, 0.8f);

		Assert.AreEqual(8f, playhead, Tolerance, "The click did not move the playhead to where it landed.");
		Assert.IsTrue(changes.HasFlag(WaveformChange.Playhead), "The waveform did not report the seek.");
		Assert.AreEqual(2f, loopStart, 1e-4f, "Seeking moved the loop.");
		Assert.AreEqual(6f, loopEnd, 1e-4f, "Seeking moved the loop.");
	}

	[TestMethod]
	public void Waveform_DragScrubsThePlayhead()
	{
		Start(Draw);

		DragAcross(Label, 0.1f, 0.9f);

		Assert.AreEqual(9f, playhead, Tolerance, "The playhead did not follow the drag.");
	}

	[TestMethod]
	public void Waveform_DraggingALoopEdgeMovesIt()
	{
		Start(Draw);

		DragAcross(Label, 0.6f, 0.8f);

		Assert.AreEqual(8f, loopEnd, Tolerance, "The loop end did not follow the drag.");
		Assert.AreEqual(2f, loopStart, 1e-4f, "Dragging the end moved the start.");
		Assert.AreEqual(0f, playhead, 1e-4f, "Dragging a loop edge seeked.");
		Assert.IsTrue(changes.HasFlag(WaveformChange.Loop), "The waveform did not report the loop change.");
	}

	[TestMethod]
	public void Waveform_LoopKeepsItsMinimumLength()
	{
		Start(Draw);

		DragAcross(Label, 0.2f, 0.9f);

		Assert.IsTrue(loopEnd - loopStart >= 0.5f - 1e-4f, $"The loop closed to {loopEnd - loopStart}, inside its 0.5 minimum.");
		Assert.IsTrue(loopStart <= loopEnd, "The loop edges crossed.");
	}

	[TestMethod]
	public void Waveform_ShiftDragDrawsANewLoop()
	{
		loopStart = 0f;
		loopEnd = 0f;
		Start(Draw);

		HarnessKeyboard.KeyDown(ImGuiKey.ModShift);
		DragAcross(Label, 0.3f, 0.5f);
		HarnessKeyboard.KeyUp(ImGuiKey.ModShift);
		Step();

		Assert.AreEqual(3f, loopStart, Tolerance, "The new loop did not start where the drag did.");
		Assert.AreEqual(5f, loopEnd, Tolerance, "The new loop did not end where the drag did.");
		Assert.AreEqual(0f, playhead, 1e-4f, "Drawing a loop seeked.");
	}

	[TestMethod]
	public void Waveform_WithNoTimeline_IgnoresThePointer()
	{
		duration = 0f;
		Start(Draw);

		ClickFraction(Label, 0.8f);

		Assert.AreEqual(WaveformChange.None, changes);
		Assert.AreEqual(0f, playhead, 1e-4f);
		Assert.IsTrue(IsVisible(Label), "A waveform with no timeline stopped reserving its box.");
	}

	[TestMethod]
	public void WaveformZoom_IsDrawnAndMarksItsScrollbar()
	{
		Start(DrawZoomable);

		Assert.IsTrue(IsVisible(Label), "The zoomable waveform marked no probe item.");
		Assert.IsTrue(IsVisible($"{Label}/scrollbar"), "The zoomable waveform did not mark its scrollbar.");
	}

	[TestMethod]
	public void WaveformZoom_CtrlWheelZoomsAboutThePointer()
	{
		StartZoomable();
		Vector2 point = PointAt(0.25f);

		WheelWith(ImGuiKey.ModCtrl, point, 1);

		Assert.AreEqual(8f, view.ViewLength, 0.01f, "One notch did not zoom in by a quarter.");
		Assert.AreEqual(0.25f, view.PositionToFraction(2.5f), 0.01f, "The point under the pointer moved.");
		Assert.IsTrue(changes.HasFlag(WaveformChange.View), "The waveform did not report the zoom.");
	}

	[TestMethod]
	public void WaveformZoom_PlainWheelLeavesTheViewAlone()
	{
		StartZoomable();
		Vector2 point = PointAt(0.25f);

		Harness.Mouse.Wheel(point.X, point.Y, 1);
		Step();

		Assert.IsTrue(view.IsShowingAll, "An unmodified wheel zoomed rather than being left to the window.");
		Assert.IsFalse(changes.HasFlag(WaveformChange.View), "An unmodified wheel reported a view change.");
	}

	[TestMethod]
	public void WaveformZoom_ShiftWheelScrolls()
	{
		ZoomTo(0f, 2f);
		Start(DrawZoomable);
		Vector2 point = PointAt(0.5f);

		WheelWith(ImGuiKey.ModShift, point, -1);

		Assert.AreEqual(0.2f, view.ViewStart, 0.01f, "A notch did not scroll a tenth of the view.");
		Assert.AreEqual(2f, view.ViewLength, 1e-4f, "Scrolling changed the zoom.");
	}

	[TestMethod]
	public void WaveformZoom_MiddleDragPans()
	{
		ZoomTo(4f, 2f);
		Start(DrawZoomable);
		Vector2 from = PointAt(0.5f);
		Vector2 to = PointAt(0.25f);

		Harness.Mouse.Drag(from.X, from.Y, to.X, to.Y, button: 2);
		Step();

		Assert.AreEqual(4.5f, view.ViewStart, 0.05f, "The view did not move with the pointer.");
		Assert.AreEqual(0f, playhead, 1e-4f, "Panning seeked.");
	}

	[TestMethod]
	public void WaveformZoom_ScrollbarDragMovesTheView()
	{
		ZoomTo(0f, 2f);
		Start(DrawZoomable);
		Vector2 from = ScrollbarPointAt(0.1f);
		Vector2 to = ScrollbarPointAt(0.6f);

		Harness.Mouse.Drag(from.X, from.Y, to.X, to.Y);
		Step();

		Assert.AreEqual(5f, view.ViewStart, 0.1f, "The thumb did not carry the view with it.");
		Assert.AreEqual(0f, playhead, 1e-4f, "Dragging the scrollbar seeked.");
	}

	[TestMethod]
	public void WaveformZoom_ClickSeeksInViewCoordinates()
	{
		ZoomTo(4f, 2f);
		StartZoomable();

		ClickFraction(Label, 0.5f, ClickRow);

		Assert.AreEqual(5f, playhead, 0.05f, "The click seeked as if the whole clip were showing.");
		Assert.IsTrue(changes.HasFlag(WaveformChange.Playhead), "The waveform did not report the seek.");
	}

	[TestMethod]
	public void WaveformZoom_FollowsThePlayheadPastTheViewEnd()
	{
		ZoomTo(0f, 2f);
		playhead = 1f;
		StartZoomable();

		playhead = 2.5f;
		Step();

		Assert.AreEqual(2.5f, view.ViewStart, 1e-3f, "The view did not page to a playhead that left it.");
		Assert.IsTrue(changes.HasFlag(WaveformChange.View), "The waveform did not report following the playhead.");
	}

	[TestMethod]
	public void WaveformZoom_DraggingPastTheEdgeScrollsTheView()
	{
		ZoomTo(0f, 2f);
		Start(DrawZoomable);
		Rectangle rect = RectOf(Label);
		Vector2 press = PointAt(0.5f);

		Harness.Mouse.MoveTo(press.X, press.Y);
		Step();
		HarnessMouse.Down(0);
		Step();
		Harness.Mouse.MoveTo(rect.MaxX + 20f, press.Y);
		Step(10);
		HarnessMouse.Up(0);
		Step();

		Assert.IsTrue(view.ViewStart > 0f, "Holding a scrub past the right edge did not scroll the view.");
		Assert.AreEqual(view.ViewEnd, playhead, 0.05f, "The playhead did not stay at the visible edge while the view scrolled.");
	}

	[TestMethod]
	public void WaveformZoom_DrawsTheSpikeAtEveryZoom()
	{
		ZoomTo(4f, 2f);
		Start(DrawZoomable);
		byte[] silent = Snapshot();

		// Sample 24,000 of 48,000 over ten seconds is at 5.0 s, the middle of a (4, 2) view.
		cache = BuildCache(spikeAt: ZoomSamples / 2);
		Step();

		Rectangle rect = RectOf(Label);
		Rectangle? difference = BoundsOfDifference(silent);
		Assert.IsNotNull(difference, "A one-sample spike drew nothing when zoomed in.");
		float centre = difference.Value.MinX + (difference.Value.Width / 2f);
		Assert.AreEqual(rect.MinX + 200f, centre, 3f, "The spike was drawn somewhere other than where it is in the view.");
	}
}
