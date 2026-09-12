// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.CurveTrack"/> on its own.</summary>
[TestClass]
public sealed class CurveTrackTests : WidgetTest
{
	private const string Label = "tone";
	private const float TrackWidth = 300f;
	private const float TrackHeight = 300f;

	private List<Vector2> points = [];
	private Func<float, float> sample = static x => x;
	private int sampleCalls;
	private bool changed;

	[TestInitialize]
	public void SetUp()
	{
		points = [new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(1f, 1f)];
		sample = static x => x;
		sampleCalls = 0;
		changed = false;
	}

	// The track overlays a rectangle the caller owns rather than reserving layout of its own, so the
	// test reserves one — with the degenerate Histogram call that is defined to draw just the empty
	// frame, which is how a caller with nothing underneath is meant to do it.
	private void Draw()
	{
		ImGuiWidgets.Histogram("##frame", default, 0, new Vector2(TrackWidth, TrackHeight));
		Vector2 min = ImGui.GetItemRectMin();
		Vector2 max = ImGui.GetItemRectMax();

		changed |= ImGuiWidgets.CurveTrack(
			Label,
			points,
			Count,
			min,
			max,
			Vector2.Zero,
			Vector2.One,
			pinEnds: true,
			minGap: 0.02f);

		// The track restores the cursor, and ImGui asserts if the last thing a window does is move
		// the cursor without submitting anything. A real caller has more rows below; this stands in
		// for them.
		ImGui.Dummy(Vector2.Zero);
	}

	private float Count(float x)
	{
		sampleCalls++;
		return sample(x);
	}

	/// <summary>
	/// Drags from one point of the rectangle to another, both as fractions of it.
	/// </summary>
	/// <remarks>
	/// The y fraction runs down the screen while the curve's y runs up it, so a fraction of 0.25 is
	/// three quarters of the way up in value space. Every assertion below reads in value space.
	/// </remarks>
	private void DragWithin(Vector2 fromFraction, Vector2 toFraction)
	{
		Rectangle rect = RectOf(Label);
		Harness.Mouse.Drag(
			rect.MinX + (rect.Width * fromFraction.X),
			rect.MinY + (rect.Height * fromFraction.Y),
			rect.MinX + (rect.Width * toFraction.X),
			rect.MinY + (rect.Height * toFraction.Y),
			steps: 12,
			button: 0);
		Step(2);
	}

	private void ClickWithinFraction(Vector2 fraction, int button)
	{
		Rectangle rect = RectOf(Label);
		Harness.Mouse.Click(
			rect.MinX + (rect.Width * fraction.X),
			rect.MinY + (rect.Height * fraction.Y),
			button);
		Step(2);
	}

	[TestMethod]
	public void CurveTrack_IsDrawnAndMarksItself()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Label), "The curve track marked no probe item.");
		AssertSomethingWasDrawn("the curve track");
	}

	[TestMethod]
	public void CurveTrack_CoversTheRectangleItWasGiven()
	{
		Start(Draw);

		Rectangle rect = RectOf(Label);

		Assert.IsLessThanOrEqualTo(2f, Math.Abs(rect.Width - TrackWidth), $"The track claimed {rect.Width}px of width rather than {TrackWidth}.");
		Assert.IsLessThanOrEqualTo(2f, Math.Abs(rect.Height - TrackHeight), $"The track claimed {rect.Height}px of height rather than {TrackHeight}.");
	}

	[TestMethod]
	public void CurveTrack_PlotsTheSamplerItWasGiven()
	{
		// The claim the whole widget rests on: it draws the caller's function rather than one of its
		// own. If it ignored the sampler, these two frames would be identical.
		Start(Draw);
		byte[] straight = Snapshot();

		sample = static x => x * x;
		Step(2);

		Assert.IsGreaterThan(0, PixelsChangedSince(straight), "Changing the sampler changed nothing on screen.");
	}

	[TestMethod]
	public void CurveTrack_AsksTheSamplerAcrossTheDomain()
	{
		Start(Draw);
		int before = sampleCalls;
		float lowest = float.MaxValue;
		float highest = float.MinValue;
		sample = x =>
		{
			lowest = MathF.Min(lowest, x);
			highest = MathF.Max(highest, x);
			return x;
		};

		Step(1);

		Assert.IsGreaterThan(before, sampleCalls, "The sampler was never called.");
		Assert.AreEqual(0f, lowest, 1e-3f, "The plot did not start at the low end of the domain.");
		Assert.AreEqual(1f, highest, 1e-3f, "The plot did not reach the high end of the domain.");
	}

	[TestMethod]
	public void CurveTrack_DraggingAPointMovesIt()
	{
		Start(Draw);

		// The middle point sits at the middle of the box. Drag it upward in value space.
		DragWithin(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.25f));

		Assert.IsGreaterThan(0.5f, points[1].Y, $"The middle point stayed at y {points[1].Y}.");
		Assert.IsTrue(changed, "The track reported no change while a point was being dragged.");
	}

	[TestMethod]
	public void CurveTrack_HoldsAPinnedEndAtItsX()
	{
		Start(Draw);

		// Just inside the bottom-left corner, where the left end sits. A press exactly on the corner
		// lands on the rectangle's boundary rather than inside the item, so it never reaches the
		// widget at all — which is a property of the press, not of the pinning being tested here.
		DragWithin(new Vector2(0.01f, 0.99f), new Vector2(0.3f, 0.5f));

		Assert.AreEqual(0f, points[0].X, 1e-4f, "A pinned end moved along x.");
		Assert.IsGreaterThan(0f, points[0].Y, "A pinned end would not move in y, so clipping cannot be expressed.");
	}

	[TestMethod]
	public void CurveTrack_PointsStayOrdered()
	{
		Start(Draw);

		// Drag the middle point past the right end.
		DragWithin(new Vector2(0.5f, 0.5f), new Vector2(1.4f, 0.5f));

		Assert.IsLessThan(points[2].X, points[1].X, $"The points crossed: {points[1].X} is not below {points[2].X}.");
		Assert.IsGreaterThan(points[0].X, points[1].X, $"The points crossed: {points[1].X} is not above {points[0].X}.");
	}

	[TestMethod]
	public void CurveTrack_PressingEmptyTrackAddsAPoint()
	{
		Start(Draw);
		int before = points.Count;

		// Away from all three points: high above the diagonal, a quarter of the way along.
		ClickWithinFraction(new Vector2(0.25f, 0.15f), button: 0);

		Assert.AreEqual(before + 1, points.Count, "Pressing empty track did not add a point.");
		Assert.IsTrue(changed, "Adding a point reported no change.");
	}

	[TestMethod]
	public void CurveTrack_PressingNearAPointGrabsItRatherThanAddingOne()
	{
		Start(Draw);
		int before = points.Count;

		ClickWithinFraction(new Vector2(0.5f, 0.5f), button: 0);

		Assert.AreEqual(before, points.Count, "Pressing on a point added another one instead of grabbing it.");
	}

	[TestMethod]
	public void CurveTrack_RightClickingAPointRemovesIt()
	{
		Start(Draw);

		ClickWithinFraction(new Vector2(0.5f, 0.5f), button: 1);

		Assert.AreEqual(2, points.Count, "Right-clicking the middle point did not remove it.");
	}

	[TestMethod]
	public void CurveTrack_RightClickingAPinnedEndKeepsIt()
	{
		Start(Draw);

		ClickWithinFraction(new Vector2(0.01f, 0.99f), button: 1);

		Assert.AreEqual(3, points.Count, "A pinned end was removed, leaving the curve with no value at that edge.");
	}

	[TestMethod]
	public void CurveTrack_WithNoPoints_StillDrawsAndCanBePopulated()
	{
		// The vendor editor this one exists beside cannot do either: its add gesture indexes the
		// first point before checking there is one, so an empty curve throws on the only gesture
		// that could fill it.
		points = [];
		Start(Draw);

		AssertSomethingWasDrawn("the empty curve track");

		ClickWithinFraction(new Vector2(0.5f, 0.5f), button: 0);

		Assert.AreEqual(1, points.Count, "An empty curve could not be populated by pressing on it.");
	}

	[TestMethod]
	public void CurveTrack_WithANonFiniteSampler_StillDraws()
	{
		// A caller's function is a caller's function. NaN must break the line rather than put a
		// segment through the whole draw list.
		sample = static x => x < 0.5f ? float.NaN : x;
		Start(Draw);

		AssertSomethingWasDrawn("the curve track with a partly undefined sampler");
	}
}
