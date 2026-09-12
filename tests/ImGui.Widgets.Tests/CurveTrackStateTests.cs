// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System.Collections.Generic;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the interaction behind CurveTrack. All pure — no ImGui context required.
/// </summary>
[TestClass]
public class CurveTrackStateTests
{
	private static readonly Vector2 Low = Vector2.Zero;
	private static readonly Vector2 High = Vector2.One;

	private static List<Vector2> Identity() => [new Vector2(0f, 0f), new Vector2(1f, 1f)];

	[TestMethod]
	public void Normalize_OrdersPointsByX()
	{
		List<Vector2> points = [new Vector2(0.9f, 0.2f), new Vector2(0.1f, 0.8f), new Vector2(0.5f, 0.5f)];

		ImGuiWidgets.CurveTrackState.Normalize(points, Low, High, 0f, pinEnds: false);

		Assert.AreEqual(0.1f, points[0].X, 1e-6f);
		Assert.AreEqual(0.5f, points[1].X, 1e-6f);
		Assert.AreEqual(0.9f, points[2].X, 1e-6f);
	}

	[TestMethod]
	public void Normalize_CarriesEachPointsYWithIt()
	{
		// Sorting by x and leaving y where it was would silently rewrite the curve. The y that
		// travelled with 0.1 has to still be on the point at 0.1 afterwards.
		List<Vector2> points = [new Vector2(0.9f, 0.2f), new Vector2(0.1f, 0.8f)];

		ImGuiWidgets.CurveTrackState.Normalize(points, Low, High, 0f, pinEnds: false);

		Assert.AreEqual(0.8f, points[0].Y, 1e-6f);
		Assert.AreEqual(0.2f, points[1].Y, 1e-6f);
	}

	[TestMethod]
	public void Normalize_ClampsPointsIntoTheBounds()
	{
		List<Vector2> points = [new Vector2(-3f, -2f), new Vector2(9f, 7f)];

		ImGuiWidgets.CurveTrackState.Normalize(points, Low, High, 0f, pinEnds: false);

		Assert.AreEqual(0f, points[0].X, 1e-6f);
		Assert.AreEqual(0f, points[0].Y, 1e-6f);
		Assert.AreEqual(1f, points[1].X, 1e-6f);
		Assert.AreEqual(1f, points[1].Y, 1e-6f);
	}

	[TestMethod]
	public void Normalize_OpensNeighborsToTheMinimumGap()
	{
		List<Vector2> points = [new Vector2(0.5f, 0f), new Vector2(0.5f, 1f)];

		ImGuiWidgets.CurveTrackState.Normalize(points, Low, High, 0.2f, pinEnds: false);

		Assert.IsGreaterThanOrEqualTo(0.2f - 1e-6f, points[1].X - points[0].X);
	}

	[TestMethod]
	public void Normalize_PinsTheEndsToTheDomain()
	{
		List<Vector2> points = [new Vector2(0.3f, 0.1f), new Vector2(0.5f, 0.5f), new Vector2(0.7f, 0.9f)];

		ImGuiWidgets.CurveTrackState.Normalize(points, Low, High, 0f, pinEnds: true);

		Assert.AreEqual(0f, points[0].X, 1e-6f);
		Assert.AreEqual(1f, points[^1].X, 1e-6f);

		// Pinned in x only. The ends are where clipping is expressed, so their y must stay free.
		Assert.AreEqual(0.1f, points[0].Y, 1e-6f);
		Assert.AreEqual(0.9f, points[^1].Y, 1e-6f);
	}

	[TestMethod]
	public void Normalize_LeavesAnEmptyListAlone()
	{
		List<Vector2> points = [];

		ImGuiWidgets.CurveTrackState.Normalize(points, Low, High, 0.1f, pinEnds: true);

		Assert.AreEqual(0, points.Count);
	}

	[TestMethod]
	public void Activate_GrabsThePointUnderThePointer()
	{
		List<Vector2> points = [new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(1f, 1f)];
		ImGuiWidgets.CurveTrackState state = new();

		bool grabbed = state.Activate(points, new Vector2(0.52f, 0.48f), 0.1f);

		Assert.IsTrue(grabbed);
		Assert.AreEqual(1, state.ActivePoint);
	}

	[TestMethod]
	public void Activate_MissesWhenNoPointIsWithinTheGrabRadius()
	{
		// The miss is the gesture that adds a point, so it has to be reported rather than rounded
		// to the nearest point the way a handle track does.
		List<Vector2> points = Identity();
		ImGuiWidgets.CurveTrackState state = new();

		bool grabbed = state.Activate(points, new Vector2(0.5f, 0.9f), 0.05f);

		Assert.IsFalse(grabbed);
		Assert.AreEqual(-1, state.ActivePoint);
	}

	[TestMethod]
	public void Activate_MeasuresInBothAxes()
	{
		// Two points at nearly the same x and very different y. Picking by x alone would grab the
		// wrong one, and it would look like the widget ignored where the pointer was.
		List<Vector2> points = [new Vector2(0.50f, 0.05f), new Vector2(0.51f, 0.95f)];
		ImGuiWidgets.CurveTrackState state = new();

		state.Activate(points, new Vector2(0.505f, 0.93f), 0.2f);

		Assert.AreEqual(1, state.ActivePoint);
	}

	[TestMethod]
	public void Drag_MovesTheActivePoint()
	{
		List<Vector2> points = [new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(1f, 1f)];
		ImGuiWidgets.CurveTrackState state = new();
		state.Activate(points, new Vector2(0.5f, 0.5f), 0.1f);

		bool moved = state.Drag(points, new Vector2(0.6f, 0.8f), Low, High, 0f, pinEnds: true);

		Assert.IsTrue(moved);
		Assert.AreEqual(0.6f, points[1].X, 1e-6f);
		Assert.AreEqual(0.8f, points[1].Y, 1e-6f);
	}

	[TestMethod]
	public void Drag_ToWhereThePointAlreadyIs_ReportsNoChange()
	{
		// A stationary pointer held down must not mint an undo entry per frame.
		List<Vector2> points = [new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(1f, 1f)];
		ImGuiWidgets.CurveTrackState state = new();
		state.Activate(points, new Vector2(0.5f, 0.5f), 0.1f);

		Assert.IsFalse(state.Drag(points, new Vector2(0.5f, 0.5f), Low, High, 0f, pinEnds: true));
	}

	[TestMethod]
	public void Drag_HoldsAPinnedEndAtItsX()
	{
		List<Vector2> points = Identity();
		ImGuiWidgets.CurveTrackState state = new();
		state.Activate(points, new Vector2(0f, 0f), 0.1f);

		state.Drag(points, new Vector2(0.4f, 0.3f), Low, High, 0f, pinEnds: true);

		Assert.AreEqual(0f, points[0].X, 1e-6f);
		Assert.AreEqual(0.3f, points[0].Y, 1e-6f);
	}

	[TestMethod]
	public void Drag_FreesTheEndsWhenTheyAreNotPinned()
	{
		List<Vector2> points = Identity();
		ImGuiWidgets.CurveTrackState state = new();
		state.Activate(points, new Vector2(0f, 0f), 0.1f);

		state.Drag(points, new Vector2(0.4f, 0.3f), Low, High, 0f, pinEnds: false);

		Assert.AreEqual(0.4f, points[0].X, 1e-6f);
	}

	[TestMethod]
	public void Drag_KeepsAPointBetweenItsNeighbors()
	{
		// Two points sharing an x make the curve vertical there, which is not a function of x and
		// which no interpolation through them is defined for. Order is the one constraint that
		// cannot yield.
		List<Vector2> points = [new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(1f, 1f)];
		ImGuiWidgets.CurveTrackState state = new();
		state.Activate(points, new Vector2(0.5f, 0.5f), 0.1f);

		state.Drag(points, new Vector2(5f, 0.5f), Low, High, 0.1f, pinEnds: true);

		Assert.IsLessThanOrEqualTo(points[2].X - 0.1f + 1e-6f, points[1].X);
		Assert.IsGreaterThan(points[0].X, points[1].X);
	}

	[TestMethod]
	public void Drag_ClampsYIntoTheBounds()
	{
		List<Vector2> points = [new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(1f, 1f)];
		ImGuiWidgets.CurveTrackState state = new();
		state.Activate(points, new Vector2(0.5f, 0.5f), 0.1f);

		state.Drag(points, new Vector2(0.5f, 9f), Low, High, 0f, pinEnds: true);

		Assert.AreEqual(1f, points[1].Y, 1e-6f);
	}

	[TestMethod]
	public void Drag_WithNothingActive_ReportsNoChange()
	{
		List<Vector2> points = Identity();
		ImGuiWidgets.CurveTrackState state = new();

		Assert.IsFalse(state.Drag(points, new Vector2(0.5f, 0.5f), Low, High, 0f, pinEnds: true));
	}

	[TestMethod]
	public void Release_ClearsTheActivePoint()
	{
		List<Vector2> points = Identity();
		ImGuiWidgets.CurveTrackState state = new();
		state.Activate(points, new Vector2(0f, 0f), 0.1f);

		state.Release();

		Assert.AreEqual(-1, state.ActivePoint);
	}

	[TestMethod]
	public void Add_InsertsInSortedPosition()
	{
		List<Vector2> points = Identity();

		int index = ImGuiWidgets.CurveTrackState.Add(points, new Vector2(0.5f, 0.7f), Low, High, 0f);

		Assert.AreEqual(1, index);
		Assert.AreEqual(3, points.Count);
		Assert.AreEqual(0.5f, points[1].X, 1e-6f);
		Assert.AreEqual(0.7f, points[1].Y, 1e-6f);
	}

	[TestMethod]
	public void Add_ClampsIntoTheBounds()
	{
		List<Vector2> points = Identity();

		int index = ImGuiWidgets.CurveTrackState.Add(points, new Vector2(0.5f, 4f), Low, High, 0f);

		Assert.AreEqual(1f, points[index].Y, 1e-6f);
	}

	[TestMethod]
	public void Add_RefusesWhenThereIsNoRoomForTheGap()
	{
		// Adding anyway would put two points at effectively the same x, which is the state Drag
		// exists to prevent — so it is refused here rather than created and then fought over.
		List<Vector2> points = [new Vector2(0f, 0f), new Vector2(0.05f, 0.5f), new Vector2(1f, 1f)];

		int index = ImGuiWidgets.CurveTrackState.Add(points, new Vector2(0.03f, 0.4f), Low, High, 0.1f);

		Assert.AreEqual(-1, index);
		Assert.AreEqual(3, points.Count);
	}

	[TestMethod]
	public void Add_PopulatesAnEmptyCurve()
	{
		// The vendor editor this one replaces cannot do it at all: its only add gesture indexes the
		// first point before checking there is one.
		List<Vector2> points = [];

		int index = ImGuiWidgets.CurveTrackState.Add(points, new Vector2(0.5f, 0.5f), Low, High, 0.05f);

		Assert.AreEqual(0, index);
		Assert.AreEqual(1, points.Count);
	}

	[TestMethod]
	public void Remove_TakesAnInteriorPoint()
	{
		List<Vector2> points = [new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(1f, 1f)];

		Assert.IsTrue(ImGuiWidgets.CurveTrackState.Remove(points, 1, pinEnds: true));
		Assert.AreEqual(2, points.Count);
	}

	[TestMethod]
	public void Remove_RefusesAPinnedEnd()
	{
		// Pinning an end only to let it be deleted would leave the curve with no value at that edge
		// of its domain, which is the thing pinning is for.
		List<Vector2> points = [new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(1f, 1f)];

		Assert.IsFalse(ImGuiWidgets.CurveTrackState.Remove(points, 0, pinEnds: true));
		Assert.IsFalse(ImGuiWidgets.CurveTrackState.Remove(points, 2, pinEnds: true));
		Assert.AreEqual(3, points.Count);
	}

	[TestMethod]
	public void Remove_KeepsTwoPointsEvenWithFreeEnds()
	{
		List<Vector2> points = Identity();

		Assert.IsFalse(ImGuiWidgets.CurveTrackState.Remove(points, 0, pinEnds: false));
		Assert.AreEqual(2, points.Count);
	}

	[TestMethod]
	public void Remove_RefusesAnIndexOutsideTheCurve()
	{
		List<Vector2> points = [new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(1f, 1f)];

		Assert.IsFalse(ImGuiWidgets.CurveTrackState.Remove(points, -1, pinEnds: true));
		Assert.IsFalse(ImGuiWidgets.CurveTrackState.Remove(points, 3, pinEnds: true));
		Assert.AreEqual(3, points.Count);
	}
}
