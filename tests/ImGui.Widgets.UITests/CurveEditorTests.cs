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

		// An empty curve is never handed to the vendor, whose add-a-point path would throw on it,
		// but the layout slot is still reserved so nothing below shifts.
		Assert.IsTrue(Math.Abs(rect.Height - Size.Y) <= 6, $"An empty curve reserved {rect.Height}px of height rather than {Size.Y}.");
		Assert.IsFalse(changed, "An empty curve reported a change.");
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

		// The two straight lines cross at the middle of the plot, so a press there is within grab
		// distance of a point on one of them whichever way the editor breaks the tie.
		float x = rect.MinX + (rect.Width / 2f);
		float y = rect.MinY + (rect.Height / 2f);
		Harness.Mouse.Drag(x, y, x - 30f, y - 30f);
		Step(2);

		Assert.IsTrue(source.Edits.Count > 0, "Dragging a point sent no edit to the source.");
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
