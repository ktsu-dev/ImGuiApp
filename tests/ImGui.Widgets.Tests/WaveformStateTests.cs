// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the interaction and peak reduction behind Waveform. All pure — no ImGui context required.
/// </summary>
[TestClass]
public class WaveformStateTests
{
	private const float Duration = 10f;
	private const float Grab = 0.2f;

	[TestMethod]
	public void PressAwayFromTheLoop_Scrubs()
	{
		ImGuiWidgets.WaveformState state = new();
		float[] loop = [2f, 6f];
		float playhead = 0f;

		state.Press(loop, 4f, Grab, createLoop: false);
		WaveformChange change = state.Drag(4f, ref playhead, loop, Duration, 0f);

		Assert.AreEqual(ImGuiWidgets.WaveformGesture.Scrub, state.Gesture);
		Assert.AreEqual(WaveformChange.Playhead, change);
		Assert.AreEqual(4f, playhead, 1e-6f);
		Assert.AreEqual(2f, loop[0], 1e-6f, "Seeking moved the loop.");
		Assert.AreEqual(6f, loop[1], 1e-6f, "Seeking moved the loop.");
	}

	[TestMethod]
	public void Scrub_ClampsToTheTimeline()
	{
		ImGuiWidgets.WaveformState state = new();
		float playhead = 5f;

		state.Press([], 5f, Grab, createLoop: false);
		state.Drag(25f, ref playhead, [], Duration, 0f);

		Assert.AreEqual(Duration, playhead, 1e-6f);
	}

	[TestMethod]
	public void Scrub_ThatDoesNotMove_ReportsNoChange()
	{
		ImGuiWidgets.WaveformState state = new();
		float playhead = 3f;

		state.Press([], 3f, Grab, createLoop: false);

		Assert.AreEqual(WaveformChange.None, state.Drag(3f, ref playhead, [], Duration, 0f));
	}

	[TestMethod]
	public void PressNearALoopEdge_GrabsThatEdge()
	{
		ImGuiWidgets.WaveformState state = new();
		float[] loop = [2f, 6f];
		float playhead = 0f;

		state.Press(loop, 6.1f, Grab, createLoop: false);
		WaveformChange change = state.Drag(8f, ref playhead, loop, Duration, 0f);

		Assert.AreEqual(1, state.ActiveLoopHandle);
		Assert.AreEqual(WaveformChange.Loop, change);
		Assert.AreEqual(8f, loop[1], 1e-6f);
		Assert.AreEqual(0f, playhead, 1e-6f, "Dragging a loop edge seeked.");
	}

	[TestMethod]
	public void LoopEdges_StayOrderedAndKeepTheMinimumLength()
	{
		ImGuiWidgets.WaveformState state = new();
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

		Assert.IsFalse(ImGuiWidgets.WaveformState.HasLoop(loop));
		Assert.AreEqual(-1, ImGuiWidgets.WaveformState.HitLoopHandle(loop, 3f, Grab));

		ImGuiWidgets.WaveformState state = new();
		state.Press(loop, 3f, Grab, createLoop: false);
		Assert.AreEqual(ImGuiWidgets.WaveformGesture.Scrub, state.Gesture, "A press on an empty loop did not seek.");
	}

	[TestMethod]
	public void HitLoopHandle_PicksTheNearerEdge_AndTiesGoLow()
	{
		// Exactly representable values, so the tie at 4.25 is a real tie and not a rounding.
		float[] loop = [4f, 4.5f];

		Assert.AreEqual(1, ImGuiWidgets.WaveformState.HitLoopHandle(loop, 4.375f, 0.3f));
		Assert.AreEqual(0, ImGuiWidgets.WaveformState.HitLoopHandle(loop, 4.25f, 0.3f));
		Assert.AreEqual(-1, ImGuiWidgets.WaveformState.HitLoopHandle(loop, 5.5f, 0.3f));
	}

	[TestMethod]
	public void CreateLoop_DrawsARegionOutFromThePress_InEitherDirection()
	{
		ImGuiWidgets.WaveformState state = new();
		float[] loop = [0f, 0f];
		float playhead = 0f;

		state.Press(loop, 5f, Grab, createLoop: true);
		Assert.AreEqual(WaveformChange.Loop, state.Drag(7f, ref playhead, loop, Duration, 0f));
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
		ImGuiWidgets.WaveformState state = new();
		float[] loop = [2f, 6f];
		float playhead = 0f;

		state.Press(loop, 4f, Grab, createLoop: true);

		Assert.AreEqual(WaveformChange.None, state.Drag(4f, ref playhead, loop, Duration, 1f));
		Assert.AreEqual(2f, loop[0], 1e-6f);
		Assert.AreEqual(6f, loop[1], 1e-6f);
	}

	[TestMethod]
	public void CreateLoop_OnAWaveformWithNoLoop_Scrubs()
	{
		ImGuiWidgets.WaveformState state = new();

		state.Press([], 4f, Grab, createLoop: true);

		Assert.AreEqual(ImGuiWidgets.WaveformGesture.Scrub, state.Gesture);
	}

	[TestMethod]
	public void Release_EndsTheGesture()
	{
		ImGuiWidgets.WaveformState state = new();
		float[] loop = [2f, 6f];
		float playhead = 0f;

		state.Press(loop, 2f, Grab, createLoop: false);
		state.Release();

		Assert.AreEqual(ImGuiWidgets.WaveformGesture.None, state.Gesture);
		Assert.AreEqual(-1, state.ActiveLoopHandle);
		Assert.AreEqual(WaveformChange.None, state.Drag(8f, ref playhead, loop, Duration, 0f));
	}

	[TestMethod]
	public void ComputeWaveformPeaks_TakesTheExtremesOfEachColumn()
	{
		float[] samples = [0.1f, -0.5f, 0.9f, 0.2f, -0.2f, 0.3f];
		float[] minimums = new float[3];
		float[] maximums = new float[3];

		ImGuiWidgets.ComputeWaveformPeaks(samples, minimums, maximums);

		float[] expectedMinimums = [-0.5f, 0.2f, -0.2f];
		CollectionAssert.AreEqual(expectedMinimums, minimums);
		float[] expectedMaximums = [0.1f, 0.9f, 0.3f];
		CollectionAssert.AreEqual(expectedMaximums, maximums);
	}

	[TestMethod]
	public void ComputeWaveformPeaks_WithFewerSamplesThanColumns_LeavesNoColumnEmpty()
	{
		float[] samples = [0.5f, -0.5f];
		float[] minimums = new float[4];
		float[] maximums = new float[4];

		ImGuiWidgets.ComputeWaveformPeaks(samples, minimums, maximums);

		float[] expectedMaximums = [0.5f, 0.5f, -0.5f, -0.5f];
		CollectionAssert.AreEqual(expectedMaximums, maximums);
	}

	[TestMethod]
	public void ComputeWaveformPeaks_SkipsNonFiniteSamples()
	{
		float[] samples = [float.NaN, 0.4f, float.PositiveInfinity, float.NaN];
		float[] minimums = new float[2];
		float[] maximums = new float[2];

		ImGuiWidgets.ComputeWaveformPeaks(samples, minimums, maximums);

		Assert.AreEqual(0.4f, maximums[0], 1e-6f);
		Assert.AreEqual(0f, maximums[1], 1e-6f, "A column of nothing finite did not read as silence.");
		Assert.AreEqual(0f, minimums[1], 1e-6f, "A column of nothing finite did not read as silence.");
	}

	[TestMethod]
	public void Reduce_KeepsASpikeNarrowerThanAPixel()
	{
		float[] minimums = new float[1000];
		float[] maximums = new float[1000];
		maximums[503] = 0.8f;

		// Ten columns per pixel: the pixel covering 500..510 must still show the spike at 503.
		Assert.IsTrue(ImGuiWidgets.WaveformImpl.Reduce(minimums, maximums, 0.5f, 0.51f, out _, out float high));
		Assert.AreEqual(0.8f, high, 1e-6f);
	}

	[TestMethod]
	public void Reduce_ClipsToTheBox()
	{
		float[] minimums = [-3f];
		float[] maximums = [3f];

		ImGuiWidgets.WaveformImpl.Reduce(minimums, maximums, 0f, 1f, out float low, out float high);

		Assert.AreEqual(-1f, low, 1e-6f);
		Assert.AreEqual(1f, high, 1e-6f);
	}

	[TestMethod]
	public void Reduce_OfAnEmptyOverview_ReportsNothing()
	{
		Assert.IsFalse(ImGuiWidgets.WaveformImpl.Reduce([], [], 0f, 1f, out _, out _));
	}
}
