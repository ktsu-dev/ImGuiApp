// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;
using ktsu.Semantics.Color;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives both curve editors on their own: the single-curve overload that edits a
/// <see cref="CurveData"/> value, and the multi-curve overload driven by a
/// <see cref="ImGuiWidgets.CurveSource"/>.
/// </summary>
[TestClass]
public sealed class CurveEditorTests : WidgetTest
{
	private const string Label = "falloff";
	private static readonly Vector2 Size = new(320f, 200f);

	/// <summary>A source holding two straight lines, so an edit is visible as a moved point.</summary>
	private sealed class TwoLineSource : ImGuiWidgets.CurveSource
	{
		private readonly Vector2[][] curves =
		[
			[new Vector2(0f, 0f), new Vector2(1f, 1f)],
			[new Vector2(0f, 1f), new Vector2(1f, 0f)],
		];

		public List<(int Curve, int Point, Vector2 Value)> Edits { get; } = [];

		public int BeginEditCalls { get; private set; }

		public int EndEditCalls { get; private set; }

		public override int CurveCount => curves.Length;

		public override Vector2 ViewMin => Vector2.Zero;

		public override Vector2 ViewMax => Vector2.One;

		public override int GetPointCount(int curveIndex) => curves[curveIndex].Length;

		public override Span<Vector2> GetPoints(int curveIndex) => curves[curveIndex];

		public override Srgb GetCurveColor(int curveIndex) =>
			curveIndex == 0 ? new Srgb(1f, 0.4f, 0.2f) : new Srgb(0.2f, 0.6f, 1f);

		public override int EditPoint(int curveIndex, int pointIndex, Vector2 value)
		{
			Edits.Add((curveIndex, pointIndex, value));
			curves[curveIndex][pointIndex] = value;
			return pointIndex;
		}

		public override void AddPoint(int curveIndex, Vector2 value)
		{
			// The two-point curves here are enough to drive the editor; adding is not exercised.
		}

		public override void BeginEdit(int curveIndex) => BeginEditCalls++;

		public override void EndEdit() => EndEditCalls++;
	}

	private CurveData curve = null!;
	private TwoLineSource source = null!;
	private int selection = -1;
	private bool changed;

	private static CurveData BuildCurve() =>
		new(
		[
			new CurveKnot(new Vector2(0f, 0f), CurvePointKind.Smooth),
			new CurveKnot(new Vector2(0.5f, 0.8f), CurvePointKind.Smooth),
			new CurveKnot(new Vector2(1f, 0.2f), CurvePointKind.Smooth),
		]);

	private void DrawSingleCurve()
	{
		Vector2 origin = ImGui.GetCursorScreenPos();
		changed |= ImGuiWidgets.CurveEditor(curve, Size, Vector2.Zero, Vector2.One, ref selection, Label);
		MarkSpan(Label, origin);
	}

	private void DrawMultiCurve()
	{
		Vector2 origin = ImGui.GetCursorScreenPos();
		changed |= ImGuiWidgets.CurveEditor(source, Size, "curves");
		MarkSpan("curves", origin);
	}

	[TestMethod]
	public void CurveEditor_SingleCurve_DrawsTheCurve()
	{
		curve = BuildCurve();
		Start(DrawSingleCurve);

		Rectangle rect = RectOf(Label);

		Assert.IsTrue(Math.Abs(rect.Width - Size.X) <= 6, $"The editor reserved {rect.Width}px of width rather than {Size.X}.");
		AssertSomethingWasDrawn("the curve editor");
	}

	[TestMethod]
	public void CurveEditor_SingleCurve_ShowsTheShapeItIsGiven()
	{
		curve = BuildCurve();
		Start(DrawSingleCurve);
		MoveAway();
		byte[] original = Snapshot();

		curve.SetPoint(1, new CurveKnot(new Vector2(0.5f, 0.1f), CurvePointKind.Smooth));
		Step(2);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(original) > 0, "Moving a point redrew the same curve.");
	}

	[TestMethod]
	public void CurveEditor_SingleCurve_DraggingAPointEditsTheCurve()
	{
		curve = BuildCurve();
		Start(DrawSingleCurve);

		Vector2 before = curve.GetPoint(1).Position;

		// The middle point sits at the middle of the horizontal range, four fifths of the way up.
		Rectangle rect = RectOf(Label);
		float x = rect.MinX + (rect.Width * 0.5f);
		float y = rect.MaxY - (rect.Height * 0.8f);
		Harness.Mouse.Drag(x, y, x, y + 40f);
		Step(2);

		Assert.AreNotEqual(before, curve.GetPoint(1).Position, "Dragging the middle point did not move it.");
		Assert.IsTrue(changed, "The editor reported no change while a point was being dragged.");
	}

	[TestMethod]
	public void CurveEditor_SingleCurve_EmptyCurve_ReservesItsSlotAndDoesNothing()
	{
		curve = new CurveData();
		Start(DrawSingleCurve);

		Rectangle rect = RectOf(Label);

		// An empty curve still reserves its layout slot, so nothing below shifts.
		Assert.IsTrue(Math.Abs(rect.Height - Size.Y) <= 6, $"An empty curve reserved {rect.Height}px of height rather than {Size.Y}.");
		Assert.IsFalse(changed, "An empty curve reported a change.");
	}

	[TestMethod]
	public void CurveEditor_SingleCurve_DoubleClickingEmptySpace_AddsAPoint_EvenToAnEmptyCurve()
	{
		curve = new CurveData();
		Start(DrawSingleCurve);

		Rectangle rect = RectOf(Label);
		float x = rect.MinX + (rect.Width * 0.5f);
		float y = rect.MaxY - (rect.Height * 0.25f);
		Harness.Mouse.Click(x, y);
		Harness.Mouse.Click(x, y);
		Step(2);

		Assert.AreEqual(1, curve.PointCount, "Double-clicking an empty curve added no point.");
		Assert.AreEqual(0.5f, curve.GetPoint(0).Position.X, 0.02f);
		Assert.AreEqual(0.25f, curve.GetPoint(0).Position.Y, 0.02f);
		Assert.AreEqual(0, selection, "The new point was not selected.");
		Assert.IsTrue(changed, "Adding a point reported no change.");
	}

	[TestMethod]
	public void CurveEditor_SingleCurve_DoubleClickingAPoint_RemovesIt()
	{
		curve = BuildCurve();
		Start(DrawSingleCurve);

		Rectangle rect = RectOf(Label);
		float x = rect.MinX + (rect.Width * 0.5f);
		float y = rect.MaxY - (rect.Height * 0.8f);
		Harness.Mouse.Click(x, y);
		Harness.Mouse.Click(x, y);
		Step(2);

		Assert.AreEqual(2, curve.PointCount, "Double-clicking the middle point did not remove it.");
		Assert.AreEqual(new Vector2(1f, 0.2f), curve.GetPoint(1).Position, "A different point was removed.");
		Assert.AreEqual(-1, selection, "The selection still addresses a point after its point was removed.");
	}

	[TestMethod]
	public void CurveEditor_SingleCurve_WideRange_DrawsTheLineInTheSameRangeAsThePoints()
	{
		// #609: the line used to run across 0..1 in X whatever the range, so with a wider range it
		// no longer met its own points. Here X runs -1..2 and Y -1..2, so value 0 sits a third of the
		// way in from the left and two thirds of the way down, and left of the first point the line
		// is held flat at its value there.
		curve = new CurveData(
		[
			new CurveKnot(new Vector2(0f, 0f), CurvePointKind.Smooth),
			new CurveKnot(new Vector2(1f, 1f), CurvePointKind.Smooth),
		]);
		Start(() =>
		{
			Vector2 origin = ImGui.GetCursorScreenPos();
			changed |= ImGuiWidgets.CurveEditor(curve, Size, new Vector2(-1f), new Vector2(2f), ref selection, Label);
			MarkSpan(Label, origin);
		});
		MoveAway();
		Step();

		Rectangle rect = RectOf(Label);
		int x = (int)(rect.MinX + (rect.Width / 6f));
		int lineY = (int)(rect.MinY + (rect.Height * 2f / 3f));
		Bitmap32 target = Harness.Target;

		Assert.AreNotEqual(target.GetPixel(x, lineY - 12), target.GetPixel(x, lineY), "No line was drawn at value 0 left of the first point.");
	}

	[TestMethod]
	public void CurveEditor_SingleCurve_DraggingAPointOnAWiderRange_LandsWhereItWasDropped()
	{
		curve = BuildCurve();
		Start(() =>
		{
			Vector2 origin = ImGui.GetCursorScreenPos();
			changed |= ImGuiWidgets.CurveEditor(curve, Size, new Vector2(-0.5f, 0f), new Vector2(1.5f, 1f), ref selection, Label);
			MarkSpan(Label, origin);
		});

		// The middle point (0.5, 0.8) sits halfway across X -0.5..1.5; a fifth of the width left is X 0.1.
		Rectangle rect = RectOf(Label);
		float x = rect.MinX + (rect.Width * 0.5f);
		float y = rect.MaxY - (rect.Height * 0.8f);
		Harness.Mouse.Drag(x, y, x - (rect.Width * 0.2f), y);
		Step(2);

		Assert.IsTrue(changed, "The editor reported no change while a point was being dragged.");
		Assert.AreEqual(0.1f, curve.GetPoint(1).Position.X, 0.03f, $"The point should have landed at X 0.1, but is at {curve.GetPoint(1).Position}.");
		Assert.AreEqual(new Vector2(0f, 0f), curve.GetPoint(0).Position, "Dragging the middle point moved the first.");
		Assert.AreEqual(new Vector2(1f, 0.2f), curve.GetPoint(2).Position, "Dragging the middle point moved the last.");
	}

	[TestMethod]
	public void CurveEditor_SingleCurve_OutOfRangeSelection_IsNormalized()
	{
		curve = BuildCurve();
		selection = 99;
		Start(DrawSingleCurve);

		Assert.IsTrue(selection < curve.PointCount, $"A selection of 99 survived on a curve with {curve.PointCount} points.");
	}

	[TestMethod]
	public void CurveEditor_MultiCurve_DrawsEveryCurve()
	{
		source = new TwoLineSource();
		Start(DrawMultiCurve);

		Rectangle rect = RectOf("curves");

		Assert.IsTrue(Math.Abs(rect.Width - Size.X) <= 6, $"The editor reserved {rect.Width}px of width rather than {Size.X}.");
		AssertSomethingWasDrawn("the multi-curve editor");
	}

	[TestMethod]
	public void CurveEditor_MultiCurve_HidingACurveChangesWhatIsDrawn()
	{
		source = new TwoLineSource();
		Start(DrawMultiCurve);
		MoveAway();
		byte[] bothVisible = Snapshot();

		// Moving a point on one of the two lines is the cheapest visible difference that comes
		// from the source rather than from the editor's own state.
		source.EditPoint(0, 1, new Vector2(1f, 0.2f));
		Step(2);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(bothVisible) > 0, "Changing a curve's points redrew the same picture.");
	}

	[TestMethod]
	public void CurveEditor_MultiCurve_DraggingAPointReachesTheSource()
	{
		source = new TwoLineSource();
		Start(DrawMultiCurve);

		Rectangle rect = RectOf("curves");

		// The first line starts at (0, 0), the bottom-left corner of the plot.
		float x = rect.MinX + 1f;
		float y = rect.MaxY - 1f;
		Harness.Mouse.Drag(x, y, x + 30f, y - 30f);
		Step(2);

		Assert.IsTrue(source.Edits.Count > 0, "Dragging a point sent no edit to the source.");
	}

	[TestMethod]
	public void CurveEditor_MultiCurve_DrawsInsideItsFrame()
	{
		// Hexa reads points in a canvas centred on zero, not in the source's value space. Handed
		// value-space points, the editor put each curve's origin at its centre and ran the rest off
		// its right and bottom edges.
		source = new TwoLineSource();
		AssertDrawsWithinFrame(DrawMultiCurve, "curves");
	}

	[TestMethod]
	public void CurveEditor_MultiCurve_EditsArriveInTheSourcesValueSpace()
	{
		source = new TwoLineSource();
		Start(DrawMultiCurve);

		// The first line's end point is (1, 1): the top-right corner, since larger values draw
		// higher. Dragging it down a quarter of the height should land it at (1, 0.75).
		Rectangle rect = RectOf("curves");
		float x = rect.MaxX - 1f;
		float y = rect.MinY + 1f;
		Harness.Mouse.Drag(x, y, x, y + (rect.Height * 0.25f));
		Step(2);

		Assert.IsTrue(source.Edits.Count > 0, "Dragging the corner point sent no edit to the source.");
		(int curveIndex, int pointIndex, Vector2 value) = source.Edits[^1];
		Assert.AreEqual(0, curveIndex, "The drag edited a different curve.");
		Assert.AreEqual(1, pointIndex, "The drag edited a different point.");
		Assert.AreEqual(1f, value.X, 0.05f, $"The point should have stayed at x = 1, but arrived at {value}.");
		Assert.AreEqual(0.75f, value.Y, 0.05f, $"Dragging down a quarter of the height should give y = 0.75, but gave {value}.");
	}

	[TestMethod]
	public void CurveEditor_SingleCurve_DrawsInsideItsFrame()
	{
		// Upstream spaces its grid by the wrong axis, so on an editor that is not square the
		// horizontal lines used to run on below the frame.
		curve = BuildCurve();
		AssertDrawsWithinFrame(DrawSingleCurve, Label);
	}

	private void AssertDrawsWithinFrame(Action draw, string name)
	{
		bool show = false;
		Start(() =>
		{
			if (show)
			{
				draw();
			}
		});
		MoveAway();
		byte[] blank = Snapshot();

		show = true;
		Step(2);
		MoveAway();

		Rectangle frame = RectOf(name);
		if (BoundsOfDifference(blank) is not Rectangle drawn)
		{
			Assert.Fail("The editor drew nothing.");
			return;
		}

		// Point markers sit on the frame's edges, so allow them half their width outside it.
		const int Margin = 7;
		Assert.IsTrue(
			drawn.MinX >= frame.MinX - Margin && drawn.MinY >= frame.MinY - Margin
				&& drawn.MaxX <= frame.MaxX + Margin && drawn.MaxY <= frame.MaxY + Margin,
			$"The editor's frame is {frame}, but it drew over {drawn}.");
	}

	[TestMethod]
	public void CurveEditor_MultiCurve_LeftAlone_SendsNoEdits()
	{
		source = new TwoLineSource();
		Start(DrawMultiCurve);
		Step(5);

		Assert.AreEqual(0, source.Edits.Count, "The editor edited the source with no input.");
		Assert.IsFalse(changed, "The editor reported a change with no input.");
	}
}
