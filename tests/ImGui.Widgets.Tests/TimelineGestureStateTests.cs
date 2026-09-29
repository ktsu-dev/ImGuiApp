// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using static ktsu.ImGui.Widgets.ImGuiWidgets;

/// <summary>
/// Tests the pointer gestures shared by the timeline widgets. All pure — no ImGui context required.
/// </summary>
[TestClass]
public class TimelineGestureStateTests
{
	private const float Duration = 10f;
	private const float Grab = 0.2f;

	[TestMethod]
	public void PressAwayFromTheLoop_Scrubs()
	{
		TimelineGestureState state = new();
		float[] loop = [2f, 6f];
		float playhead = 0f;

		state.Press(loop, 4f, Grab, createLoop: false);
		TimelineChange change = state.Drag(4f, ref playhead, loop, Duration, 0f);

		Assert.AreEqual(TimelineGesture.Scrub, state.Gesture);
		Assert.AreEqual(TimelineChange.Playhead, change);
		Assert.AreEqual(4f, playhead, 1e-6f);
		Assert.AreEqual(2f, loop[0], 1e-6f, "Seeking moved the loop.");
		Assert.AreEqual(6f, loop[1], 1e-6f, "Seeking moved the loop.");
	}

	[TestMethod]
	public void Scrub_ClampsToTheTimeline()
	{
		TimelineGestureState state = new();
		float playhead = 5f;

		state.Press([], 5f, Grab, createLoop: false);
		state.Drag(25f, ref playhead, [], Duration, 0f);

		Assert.AreEqual(Duration, playhead, 1e-6f);
	}

	[TestMethod]
	public void Scrub_ThatDoesNotMove_ReportsNoChange()
	{
		TimelineGestureState state = new();
		float playhead = 3f;

		state.Press([], 3f, Grab, createLoop: false);

		Assert.AreEqual(TimelineChange.None, state.Drag(3f, ref playhead, [], Duration, 0f));
	}

	[TestMethod]
	public void PressNearALoopEdge_GrabsThatEdge()
	{
		TimelineGestureState state = new();
		float[] loop = [2f, 6f];
		float playhead = 0f;

		state.Press(loop, 6.1f, Grab, createLoop: false);
		TimelineChange change = state.Drag(8f, ref playhead, loop, Duration, 0f);

		Assert.AreEqual(1, state.ActiveLoopHandle);
		Assert.AreEqual(TimelineChange.Region, change);
		Assert.AreEqual(8f, loop[1], 1e-6f);
		Assert.AreEqual(0f, playhead, 1e-6f, "Dragging a loop edge seeked.");
	}

	[TestMethod]
	public void LoopEdges_StayOrderedAndKeepTheMinimumLength()
	{
		TimelineGestureState state = new();
		float[] loop = [2f, 6f];
		float playhead = 0f;

		state.Press(loop, 2f, Grab, createLoop: false);
		state.Drag(9f, ref playhead, loop, Duration, 1f);

		Assert.AreEqual(5f, loop[0], 1e-6f, "The start ran past the end less the minimum loop length.");
		Assert.AreEqual(6f, loop[1], 1e-6f);
	}

	[TestMethod]
	public void AnEmptyLoop_CannotBeGrabbed()
	{
		float[] loop = [3f, 3f];

		Assert.IsFalse(TimelineGestureState.HasLoop(loop));
		Assert.AreEqual(-1, TimelineGestureState.HitLoopHandle(loop, 3f, Grab));

		TimelineGestureState state = new();
		state.Press(loop, 3f, Grab, createLoop: false);
		Assert.AreEqual(TimelineGesture.Scrub, state.Gesture, "A press on an empty loop did not seek.");
	}

	[TestMethod]
	public void HitLoopHandle_PicksTheNearerEdge_AndTiesGoLow()
	{
		// Exactly representable values, so the tie at 4.25 is a real tie and not a rounding.
		float[] loop = [4f, 4.5f];

		Assert.AreEqual(1, TimelineGestureState.HitLoopHandle(loop, 4.375f, 0.3f));
		Assert.AreEqual(0, TimelineGestureState.HitLoopHandle(loop, 4.25f, 0.3f));
		Assert.AreEqual(-1, TimelineGestureState.HitLoopHandle(loop, 5.5f, 0.3f));
	}

	[TestMethod]
	public void CreateLoop_DrawsARegionOutFromThePress_InEitherDirection()
	{
		TimelineGestureState state = new();
		float[] loop = [0f, 0f];
		float playhead = 0f;

		state.Press(loop, 5f, Grab, createLoop: true);
		Assert.AreEqual(TimelineChange.Region, state.Drag(7f, ref playhead, loop, Duration, 0f));
		Assert.AreEqual(5f, loop[0], 1e-6f);
		Assert.AreEqual(7f, loop[1], 1e-6f);

		// Crossing back over the anchor flips which end follows the pointer.
		state.Drag(3f, ref playhead, loop, Duration, 0f);
		Assert.AreEqual(3f, loop[0], 1e-6f);
		Assert.AreEqual(5f, loop[1], 1e-6f);
		Assert.AreEqual(0f, playhead, 1e-6f, "Drawing a loop seeked.");
	}

	[TestMethod]
	public void CreateLoop_ThatNeverMoves_LeavesTheLoopAlone()
	{
		TimelineGestureState state = new();
		float[] loop = [2f, 6f];
		float playhead = 0f;

		state.Press(loop, 4f, Grab, createLoop: true);

		Assert.AreEqual(TimelineChange.None, state.Drag(4f, ref playhead, loop, Duration, 1f));
		Assert.AreEqual(2f, loop[0], 1e-6f);
		Assert.AreEqual(6f, loop[1], 1e-6f);
	}

	[TestMethod]
	public void CreateLoop_OnAWaveformWithNoLoop_Scrubs()
	{
		TimelineGestureState state = new();

		state.Press([], 4f, Grab, createLoop: true);

		Assert.AreEqual(TimelineGesture.Scrub, state.Gesture);
	}

	[TestMethod]
	public void Release_EndsTheGesture()
	{
		TimelineGestureState state = new();
		float[] loop = [2f, 6f];
		float playhead = 0f;

		state.Press(loop, 2f, Grab, createLoop: false);
		state.Release();

		Assert.AreEqual(TimelineGesture.None, state.Gesture);
		Assert.AreEqual(-1, state.ActiveLoopHandle);
		Assert.AreEqual(TimelineChange.None, state.Drag(8f, ref playhead, loop, Duration, 0f));
	}

	[TestMethod]
	public void ChangeFlags_MatchWaveformChangeBitForBit()
	{
		// The waveform returns TimelineGestureState's changes cast straight to its own enum.
		(TimelineChange Timeline, WaveformChange Waveform)[] pairs =
		[
			(TimelineChange.None, WaveformChange.None),
			(TimelineChange.Playhead, WaveformChange.Playhead),
			(TimelineChange.Region, WaveformChange.Loop),
			(TimelineChange.View, WaveformChange.View),
		];

		foreach ((TimelineChange timeline, WaveformChange waveform) in pairs)
		{
			Assert.AreEqual((int)waveform, (int)timeline, $"{timeline} and {waveform} are different bits.");
		}
	}

	[TestMethod]
	public void Pan_MovesTheViewWithThePointer()
	{
		TimelineView view = ViewOf(100f, 20f, 10f);
		TimelineGestureState state = new();

		state.PressPan(0.5f, view);
		TimelineChange change = state.DragView(0.3f, view);

		Assert.AreEqual(TimelineGesture.Pan, state.Gesture);
		Assert.AreEqual(TimelineChange.View, change);
		Assert.AreEqual(22f, view.ViewStart, 1e-4f, "Dragging left by a fifth of the view did not move it later by a fifth.");
	}

	[TestMethod]
	public void PressScrollbar_OnTheThumb_KeepsTheGrabOffset()
	{
		TimelineView view = ViewOf(100f, 0f, 10f);
		TimelineGestureState state = new();

		Assert.AreEqual(TimelineChange.None, state.PressScrollbar(0.05f, view, 0f));
		Assert.AreEqual(0f, view.ViewStart, 1e-4f, "Pressing on the thumb moved it.");

		// The thumb is 0.1 of the track and was grabbed 0.05 into it, so a pointer at 0.55 puts the
		// thumb's start at 0.5 — halfway along the 0.9 it can travel, which is 50 of the 90 the view can.
		Assert.AreEqual(TimelineChange.View, state.DragView(0.55f, view));
		Assert.AreEqual(50f, view.ViewStart, 1e-3f, "The thumb jumped rather than keeping the point it was grabbed by under the pointer.");
	}

	[TestMethod]
	public void PressScrollbar_OnTheTrack_CentresTheThumbThere()
	{
		TimelineView view = ViewOf(100f, 0f, 10f);
		TimelineGestureState state = new();

		Assert.AreEqual(TimelineChange.View, state.PressScrollbar(0.8f, view, 0f));
		Assert.AreEqual(TimelineGesture.ScrollThumb, state.Gesture);
		Assert.AreEqual(75f, view.ViewStart, 1e-3f);
	}

	[TestMethod]
	public void ScrollThumb_WithAMinimumSize_StillReachesTheEnd()
	{
		TimelineView view = ViewOf(100f, 0f, 1f);
		TimelineGestureState state = new();

		state.PressScrollbar(0.1f, view, 0.2f);
		state.DragView(1f, view);

		Assert.AreEqual(99f, view.ViewStart, 1e-3f, "A thumb drawn larger than its share of the track could not reach the end of the clip.");
	}

	[TestMethod]
	public void DragView_WhenShowingAll_ReportsNoChange()
	{
		TimelineView view = new();
		view.SetDuration(100f);
		TimelineGestureState state = new();

		state.PressPan(0.5f, view);

		Assert.AreEqual(TimelineChange.None, state.DragView(0.1f, view));
		Assert.IsTrue(view.IsShowingAll);
	}

	private static TimelineView ViewOf(float duration, float start, float length)
	{
		TimelineView view = new();
		view.SetDuration(duration);
		view.SetView(start, length);
		return view;
	}
}
