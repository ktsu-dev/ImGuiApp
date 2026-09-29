// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using static ktsu.ImGui.Widgets.ImGuiWidgets;

/// <summary>
/// Tests the view onto a timeline that the zoomable timeline widgets share. All pure — no ImGui
/// context required.
/// </summary>
[TestClass]
public class TimelineViewTests
{
	[TestMethod]
	public void NewView_IsEmptyAndShowsAll()
	{
		TimelineView view = new();

		Assert.AreEqual(0f, view.Duration);
		Assert.AreEqual(0f, view.ViewStart);
		Assert.AreEqual(0f, view.ViewLength);
		Assert.IsTrue(view.IsShowingAll);
		Assert.IsTrue(view.FollowPlayhead);
	}

	[TestMethod]
	public void SetDuration_OnAViewShowingAll_KeepsShowingAll()
	{
		TimelineView view = new();

		Assert.IsTrue(view.SetDuration(10f));
		Assert.AreEqual(10f, view.ViewLength, 1e-6f, "A new view did not show the whole clip.");

		Assert.IsTrue(view.SetDuration(20f));
		Assert.AreEqual(0f, view.ViewStart, 1e-6f);
		Assert.AreEqual(20f, view.ViewLength, 1e-6f, "A view showing everything stopped showing everything when the clip grew.");
	}

	[TestMethod]
	public void SetDuration_OnAZoomedView_KeepsTheViewAndReclamps()
	{
		TimelineView view = ViewOf(100f, 90f, 10f);

		Assert.IsTrue(view.SetDuration(50f));

		Assert.AreEqual(10f, view.ViewLength, 1e-4f, "A zoomed view lost its zoom when the clip changed length.");
		Assert.AreEqual(40f, view.ViewStart, 1e-4f, "The view was left past the end of the shorter clip.");
	}

	[TestMethod]
	public void SetDuration_NonFiniteOrNonPositive_EmptiesTheView()
	{
		foreach (float duration in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
		{
			TimelineView view = ViewOf(100f, 20f, 10f);

			Assert.IsTrue(view.SetDuration(duration), $"Setting a duration of {duration} reported no change.");
			Assert.AreEqual(0f, view.Duration, $"A duration of {duration} was kept.");
			Assert.AreEqual(0f, view.ViewStart);
			Assert.AreEqual(0f, view.ViewLength);
			Assert.IsTrue(view.IsShowingAll);
		}
	}

	[TestMethod]
	public void SetView_ClampsLengthAndStart()
	{
		TimelineView view = new();
		view.SetDuration(10f);

		view.SetView(8f, 5f);
		Assert.AreEqual(5f, view.ViewStart, 1e-6f, "The view ran past the end of the clip.");
		Assert.AreEqual(5f, view.ViewLength, 1e-6f);

		view.SetView(-3f, 20f);
		Assert.AreEqual(0f, view.ViewStart, 1e-6f);
		Assert.AreEqual(10f, view.ViewLength, 1e-6f, "The view was longer than the clip.");

		view.SetView(2f, 0f);
		Assert.AreEqual(1e-4f, view.ViewLength, 1e-9f, "The view shrank below the minimum length.");
	}

	[TestMethod]
	public void SetView_ThatChangesNothing_ReturnsFalse()
	{
		TimelineView view = ViewOf(10f, 2f, 3f);

		Assert.IsFalse(view.SetView(2f, 3f));
		Assert.IsFalse(view.SetView(100f, 3f) && view.SetView(100f, 3f), "Clamping to the same place twice reported a change the second time.");
	}

	[TestMethod]
	public void SetView_IgnoresNonFiniteArguments()
	{
		TimelineView view = ViewOf(10f, 2f, 3f);

		Assert.IsFalse(view.SetView(float.NaN, 3f));
		Assert.IsFalse(view.SetView(2f, float.PositiveInfinity));
		Assert.AreEqual(2f, view.ViewStart, 1e-6f);
		Assert.AreEqual(3f, view.ViewLength, 1e-6f);
	}

	[TestMethod]
	public void ZoomAt_KeepsTheAnchorAtItsFraction()
	{
		TimelineView view = ViewOf(100f, 20f, 40f);

		Assert.IsTrue(view.ZoomAt(2f, 30f));

		Assert.AreEqual(20f, view.ViewLength, 1e-4f);
		Assert.AreEqual(25f, view.ViewStart, 1e-4f);
		Assert.AreEqual(0.25f, view.PositionToFraction(30f), 1e-5f, "The anchor moved under the pointer.");
	}

	[TestMethod]
	public void ZoomAt_ClampsAtTheClipEnds()
	{
		TimelineView view = ViewOf(100f, 0f, 10f);

		view.ZoomAt(0.5f, 2f);
		Assert.AreEqual(0f, view.ViewStart, 1e-4f, "Zooming out at the start pushed the view before the clip.");
		Assert.AreEqual(20f, view.ViewLength, 1e-4f);

		view.ShowAll();
		Assert.IsFalse(view.ZoomAt(0.5f, 50f), "Zooming out of a view already showing everything reported a change.");
	}

	[TestMethod]
	public void ZoomAt_StopsAtTheMinimumLength()
	{
		TimelineView view = new();
		view.SetDuration(10f);

		view.ZoomAt(1e9f, 5f);

		Assert.AreEqual(1e-4f, view.ViewLength, 1e-9f);
		Assert.AreEqual(view.MinViewLength, view.ViewLength, 1e-9f);
	}

	[TestMethod]
	public void ZoomAt_RejectsNonPositiveOrNonFiniteFactors()
	{
		foreach (float factor in new[] { 0f, -2f, float.NaN, float.PositiveInfinity })
		{
			TimelineView view = ViewOf(100f, 20f, 10f);

			Assert.IsFalse(view.ZoomAt(factor, 25f), $"A factor of {factor} changed the view.");
			Assert.AreEqual(10f, view.ViewLength, 1e-6f);
		}
	}

	[TestMethod]
	public void ScrollBy_ClampsToTheClip()
	{
		TimelineView view = ViewOf(10f, 0f, 2f);

		view.ScrollBy(100f);
		Assert.AreEqual(8f, view.ViewStart, 1e-6f);

		view.ScrollBy(-100f);
		Assert.AreEqual(0f, view.ViewStart, 1e-6f);
	}

	[TestMethod]
	public void Follow_PagesWhenThePlayheadLeavesTheView()
	{
		TimelineView view = ViewOf(100f, 0f, 10f);

		Assert.IsFalse(view.Follow(5f));
		Assert.IsTrue(view.Follow(10.5f));

		Assert.AreEqual(10.5f, view.ViewStart, 1e-4f, "The playhead did not land at the left edge of the new page.");
		Assert.AreEqual(10f, view.ViewLength, 1e-4f, "Paging changed the zoom.");
	}

	[TestMethod]
	public void Follow_DoesNotChaseAPlayheadTheUserScrolledAwayFrom()
	{
		TimelineView view = ViewOf(100f, 0f, 10f);
		view.Follow(5f);

		view.SetView(50f, 10f);

		Assert.IsFalse(view.Follow(5.1f), "The view was dragged back to a playhead the user scrolled away from.");
		Assert.IsFalse(view.Follow(5.2f));
		Assert.AreEqual(50f, view.ViewStart, 1e-4f);
	}

	[TestMethod]
	public void Follow_PagesBackToALoopStart()
	{
		TimelineView view = ViewOf(100f, 20f, 10f);

		view.Follow(29f);
		Assert.IsTrue(view.Follow(2f), "A loop wrapping back out of view did not page.");

		Assert.AreEqual(2f, view.ViewStart, 1e-4f);
	}

	[TestMethod]
	public void Follow_NearTheEnd_ClampsTheView()
	{
		TimelineView view = ViewOf(100f, 85f, 10f);

		view.Follow(94f);
		view.Follow(96f);

		Assert.AreEqual(90f, view.ViewStart, 1e-4f);
	}

	[TestMethod]
	public void Follow_IsOffWhenFollowPlayheadIsFalse()
	{
		TimelineView view = ViewOf(100f, 0f, 10f);
		view.FollowPlayhead = false;

		view.Follow(5f);

		Assert.IsFalse(view.Follow(20f));
		Assert.AreEqual(0f, view.ViewStart, 1e-6f);
	}

	[TestMethod]
	public void EdgeScroll_ScrollsTwoViewsPerSecondBeyondAnEdge()
	{
		TimelineView view = ViewOf(100f, 10f, 10f);

		Assert.IsTrue(view.EdgeScroll(1.2f, 0.1f));
		Assert.AreEqual(12f, view.ViewStart, 1e-4f);

		Assert.IsTrue(view.EdgeScroll(-0.5f, 0.1f));
		Assert.AreEqual(10f, view.ViewStart, 1e-4f);

		Assert.IsFalse(view.EdgeScroll(0.5f, 0.1f), "A drag inside the view scrolled it.");
	}

	[TestMethod]
	public void PositionAndFraction_AreInverse()
	{
		TimelineView view = ViewOf(100f, 20f, 40f);

		Assert.AreEqual(0.5f, view.PositionToFraction(40f), 1e-6f);
		Assert.AreEqual(40f, view.FractionToPosition(0.5f), 1e-5f);
		Assert.AreEqual(-0.5f, view.PositionToFraction(0f), 1e-6f, "A position before the view was clamped.");
		Assert.AreEqual(0f, new TimelineView().PositionToFraction(5f), "An empty view did not map everything to its left edge.");
	}

	[TestMethod]
	public void MinViewFraction_OutsideZeroToOne_Throws()
	{
		foreach (float fraction in new[] { 0f, -1f, 1.5f, float.NaN })
		{
			Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TimelineView { MinViewFraction = fraction }, $"A minimum view fraction of {fraction} was accepted.");
		}

		Assert.AreEqual(1f, new TimelineView { MinViewFraction = 1f }.MinViewFraction);
	}

	private static TimelineView ViewOf(float duration, float start, float length)
	{
		TimelineView view = new();
		view.SetDuration(duration);
		view.SetView(start, length);
		return view;
	}
}
