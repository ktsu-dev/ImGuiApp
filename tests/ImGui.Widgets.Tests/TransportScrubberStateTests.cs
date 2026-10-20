// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using static ktsu.ImGui.Widgets.ImGuiWidgets;

/// <summary>
/// Tests the rules the transport scrubber adds over the shared timeline gestures. All pure — no ImGui
/// context required.
/// </summary>
[TestClass]
public class TransportScrubberStateTests
{
	[TestMethod]
	public void Snap_RoundsToTheNearestFrame()
	{
		Assert.AreEqual(1.04f, TransportScrubberState.Snap(1.02f, 0.04f), 1e-6f);
		Assert.AreEqual(1.0f, TransportScrubberState.Snap(1.019f, 0.04f), 1e-6f);
	}

	[TestMethod]
	public void Snap_WithNoStep_LeavesThePosition()
	{
		Assert.AreEqual(1.019f, TransportScrubberState.Snap(1.019f, 0f));
		Assert.AreEqual(1.019f, TransportScrubberState.Snap(1.019f, -1f));
		Assert.AreEqual(1.019f, TransportScrubberState.Snap(1.019f, float.NaN));
	}

	[TestMethod]
	public void EffectiveMinLength_FallsBackToOneFrame()
	{
		Assert.AreEqual(0.04f, TransportScrubberState.EffectiveMinLength(0f, 0.04f));
		Assert.AreEqual(0.5f, TransportScrubberState.EffectiveMinLength(0.5f, 0.04f));
		Assert.AreEqual(0f, TransportScrubberState.EffectiveMinLength(0f, 0f));
	}

	[TestMethod]
	public void StepPlayhead_StepsOneFrame_OrTenWithShift()
	{
		Assert.AreEqual(1.04f, TransportScrubberState.StepPlayhead(1f, +1, false, 0.04f, 10f, 10f), 1e-5f);
		Assert.AreEqual(0.6f, TransportScrubberState.StepPlayhead(1f, -1, true, 0.04f, 10f, 10f), 1e-5f);
	}

	[TestMethod]
	public void StepPlayhead_WithoutFrames_StepsOnePercentOfTheView() =>
		Assert.AreEqual(5.02f, TransportScrubberState.StepPlayhead(5f, +1, false, 0f, 2f, 10f), 1e-5f);

	[TestMethod]
	public void StepPlayhead_ClampsToTheClip()
	{
		Assert.AreEqual(10f, TransportScrubberState.StepPlayhead(9.99f, +1, true, 0.04f, 10f, 10f), 1e-6f);
		Assert.AreEqual(0f, TransportScrubberState.StepPlayhead(0f, -1, false, 0.04f, 10f, 10f), 1e-6f);
	}

	[TestMethod]
	public void MarkIn_WithNoRange_RunsToTheEnd()
	{
		float inPoint = 0f;
		float outPoint = 0f;

		bool changed = TransportScrubberState.MarkIn(3f, ref inPoint, ref outPoint, 10f, 0f);

		Assert.IsTrue(changed);
		Assert.AreEqual(3f, inPoint, 1e-6f);
		Assert.AreEqual(10f, outPoint, 1e-6f);
	}

	[TestMethod]
	public void MarkIn_AfterTheOut_KeepsTheMinimumLength()
	{
		float inPoint = 2f;
		float outPoint = 4f;

		TransportScrubberState.MarkIn(6f, ref inPoint, ref outPoint, 10f, 0.5f);

		Assert.AreEqual(6f, inPoint, 1e-6f);
		Assert.AreEqual(6.5f, outPoint, 1e-6f);
	}

	[TestMethod]
	public void MarkIn_AtTheVeryEnd_PushesTheRangeBack()
	{
		float inPoint = 2f;
		float outPoint = 4f;

		TransportScrubberState.MarkIn(10f, ref inPoint, ref outPoint, 10f, 0.5f);

		Assert.AreEqual(9.5f, inPoint, 1e-6f);
		Assert.AreEqual(10f, outPoint, 1e-6f);
	}

	[TestMethod]
	public void MarkOut_WithNoRange_RunsFromTheStart()
	{
		float inPoint = 0f;
		float outPoint = 0f;

		bool changed = TransportScrubberState.MarkOut(7f, ref inPoint, ref outPoint, 10f, 0f);

		Assert.IsTrue(changed);
		Assert.AreEqual(0f, inPoint, 1e-6f);
		Assert.AreEqual(7f, outPoint, 1e-6f);
	}

	[TestMethod]
	public void MarkOut_BeforeTheIn_KeepsTheMinimumLength()
	{
		float inPoint = 5f;
		float outPoint = 8f;

		TransportScrubberState.MarkOut(3f, ref inPoint, ref outPoint, 10f, 0.5f);

		Assert.AreEqual(2.5f, inPoint, 1e-6f);
		Assert.AreEqual(3f, outPoint, 1e-6f);
	}

	private const float Frame = 1f / 24f;

	private static void AssertOnFrameGrid(float position, string what)
	{
		float frames = position / Frame;
		Assert.AreEqual(MathF.Round(frames), frames, 1e-3f, $"The {what} ({position}) is not on a frame boundary.");
	}

	[TestMethod]
	public void MarkIn_WithAMidFramePlayhead_LandsOnTheNearestFrame()
	{
		float inPoint = 0f;
		float outPoint = 0f;

		TransportScrubberState.MarkIn(12.3456f, ref inPoint, ref outPoint, 30f, Frame, Frame);

		Assert.AreEqual(296f * Frame, inPoint, 1e-5f);
		Assert.AreEqual(30f, outPoint, 1e-6f);
		AssertOnFrameGrid(inPoint, "in point");
	}

	[TestMethod]
	public void MarkOut_WithAMidFramePlayhead_PushesTheInPointOnTheGrid()
	{
		float inPoint = 20f;
		float outPoint = 25f;

		TransportScrubberState.MarkOut(20.01f, ref inPoint, ref outPoint, 30f, Frame, Frame);

		Assert.AreEqual(480f * Frame, outPoint, 1e-5f);
		Assert.AreEqual(479f * Frame, inPoint, 1e-5f);
		AssertOnFrameGrid(inPoint, "in point");
		AssertOnFrameGrid(outPoint, "out point");
	}

	[TestMethod]
	public void MarkIn_RoundsAMinimumLengthUpToWholeFrames()
	{
		float inPoint = 2f;
		float outPoint = 4f;

		// 0.05 s is 1.2 frames at 24 fps, so the range keeps two.
		TransportScrubberState.MarkIn(6.01f, ref inPoint, ref outPoint, 10f, 0.05f, Frame);

		Assert.AreEqual(6f, inPoint, 1e-5f);
		Assert.AreEqual(6f + (2f * Frame), outPoint, 1e-5f);
		AssertOnFrameGrid(inPoint, "in point");
		AssertOnFrameGrid(outPoint, "out point");
	}

	[TestMethod]
	public void MarkOut_RoundsAMinimumLengthUpToWholeFrames()
	{
		float inPoint = 5f;
		float outPoint = 8f;

		TransportScrubberState.MarkOut(3.01f, ref inPoint, ref outPoint, 10f, 0.05f, Frame);

		Assert.AreEqual(3f, outPoint, 1e-5f);
		Assert.AreEqual(3f - (2f * Frame), inPoint, 1e-5f);
		AssertOnFrameGrid(inPoint, "in point");
		AssertOnFrameGrid(outPoint, "out point");
	}

	[TestMethod]
	public void MarkIn_AtTheEndOfAClipThatIsNotWholeFrames_PushesTheInPointBackOntoTheGrid()
	{
		float inPoint = 2f;
		float outPoint = 4f;

		TransportScrubberState.MarkIn(10.01f, ref inPoint, ref outPoint, 10.01f, Frame, Frame);

		Assert.AreEqual(10.01f, outPoint, 1e-6f, "The end of the clip is the one point written off the grid.");
		AssertOnFrameGrid(inPoint, "in point");
		Assert.IsTrue(outPoint - inPoint >= Frame - 1e-6f, "The range is narrower than the minimum.");
	}

	[TestMethod]
	public void MarkInAndOut_OneFrameMinimum_IsNotRoundedUpToTwo()
	{
		float inPoint = 2f;
		float outPoint = 4f;

		TransportScrubberState.MarkIn(5f, ref inPoint, ref outPoint, 10f, Frame, Frame);

		Assert.AreEqual(5f + Frame, outPoint, 1e-5f);
	}

	[TestMethod]
	public void ClearInOut_ReportsWhetherThereWasARange()
	{
		float inPoint = 2f;
		float outPoint = 6f;

		Assert.IsTrue(TransportScrubberState.ClearInOut(ref inPoint, ref outPoint));
		Assert.AreEqual(0f, inPoint);
		Assert.AreEqual(0f, outPoint);
		Assert.IsFalse(TransportScrubberState.ClearInOut(ref inPoint, ref outPoint));
	}

	[TestMethod]
	public void ThumbnailSlots_AreAnchoredToTheTimeline()
	{
		(int first, int count, float slotLength) = TransportScrubberState.ThumbnailSlots(0f, 10f, 10f, 400f, 40f);
		Assert.AreEqual(0, first);
		Assert.AreEqual(10, count);
		Assert.AreEqual(1f, slotLength, 1e-6f);

		(int scrolledFirst, int scrolledCount, _) = TransportScrubberState.ThumbnailSlots(0.5f, 10f, 20f, 400f, 40f);
		Assert.AreEqual(0, scrolledFirst, "Scrolling half a slot re-sliced the strip instead of sliding it.");
		Assert.AreEqual(11, scrolledCount);
	}

	[TestMethod]
	public void ThumbnailSlots_StopAtTheClipEnd() =>
		Assert.AreEqual(5, TransportScrubberState.ThumbnailSlots(0f, 10f, 5f, 400f, 40f).Count);

	[TestMethod]
	public void ThumbnailSlots_OfADegenerateView_AreEmpty()
	{
		Assert.AreEqual(0, TransportScrubberState.ThumbnailSlots(0f, 10f, 10f, 0f, 40f).Count);
		Assert.AreEqual(0, TransportScrubberState.ThumbnailSlots(0f, 0f, 10f, 400f, 40f).Count);
		Assert.AreEqual(0, TransportScrubberState.ThumbnailSlots(float.NaN, 10f, 10f, 400f, 40f).Count);
	}

	[TestMethod]
	public void SlotTime_IsTheSnappedMidpoint()
	{
		Assert.AreEqual(3.52f, TransportScrubberState.SlotTime(3, 1f, 10f, 0.04f), 1e-5f);
		Assert.AreEqual(10f, TransportScrubberState.SlotTime(12, 1f, 10f, 0f), 1e-6f);
	}

	[TestMethod]
	public void RulerStep_PicksTheSmallestStepThatFits()
	{
		Assert.AreEqual(1f, TransportScrubberState.RulerStep(10f, 400f, 40f, 0f), 1e-6f);
		Assert.AreEqual(5f, TransportScrubberState.RulerStep(10f, 400f, 100f, 0f), 1e-6f);
		Assert.AreEqual(120f, TransportScrubberState.RulerStep(600f, 400f, 60f, 0f), 1e-6f);
	}

	[TestMethod]
	public void RulerStep_BelowASecond_UsesWholeFrames() =>
		Assert.AreEqual(2f / 24f, TransportScrubberState.RulerStep(1f, 400f, 30f, 1f / 24f), 1e-6f);

	[TestMethod]
	public void RulerStep_OfADegenerateView_IsZero()
	{
		Assert.AreEqual(0f, TransportScrubberState.RulerStep(0f, 400f, 40f, 0f));
		Assert.AreEqual(0f, TransportScrubberState.RulerStep(10f, 0f, 40f, 0f));
		Assert.AreEqual(0f, TransportScrubberState.RulerStep(10f, 400f, 0f, 0f));
		Assert.AreEqual(0f, TransportScrubberState.RulerStep(float.NaN, 400f, 40f, 0f));
		Assert.AreEqual(0f, TransportScrubberState.RulerStep(10f, float.PositiveInfinity, 40f, 0f));
	}

	[TestMethod]
	public void DefaultFormat_UsesMinutesThenHours()
	{
		Assert.AreEqual("1:05.500", TransportScrubberState.DefaultFormat(65.5f));
		Assert.AreEqual("1:02:05.250", TransportScrubberState.DefaultFormat(3725.25f));
		Assert.AreEqual("-0:02.000", TransportScrubberState.DefaultFormat(-2f));
		Assert.AreEqual("--:--", TransportScrubberState.DefaultFormat(float.NaN));
	}

	[TestMethod]
	public void ChangeFlags_MatchTimelineChangeBitForBit()
	{
		// The scrubber returns TimelineGestureState's changes cast straight to its own enum.
		(TimelineChange Timeline, TransportScrubberChange Scrubber)[] pairs =
		[
			(TimelineChange.None, TransportScrubberChange.None),
			(TimelineChange.Playhead, TransportScrubberChange.Playhead),
			(TimelineChange.Region, TransportScrubberChange.InOut),
			(TimelineChange.View, TransportScrubberChange.View),
		];

		foreach ((TimelineChange timeline, TransportScrubberChange scrubber) in pairs)
		{
			Assert.AreEqual((int)scrubber, (int)timeline, $"{timeline} and {scrubber} are different bits.");
		}
	}
}
