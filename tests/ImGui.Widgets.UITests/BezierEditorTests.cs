// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.BezierEditor"/> on its own.</summary>
[TestClass]
public sealed class BezierEditorTests : WidgetTest
{
	private const string Label = "easing";
	private const float Size = 160f;

	private BezierControlPoints points = new(new Vector2(0.25f, 0.1f), new Vector2(0.25f, 1f));
	private bool changed;

	private void Draw()
	{
		Vector2 origin = ImGui.GetCursorScreenPos();
		changed |= ImGuiWidgets.BezierEditor(Label, ref points, Size);
		MarkSpan(Label, origin);
	}

	[TestMethod]
	public void BezierEditor_DrawsAtTheSizeItIsGiven()
	{
		Start(Draw);

		Rectangle rect = RectOf(Label);

		Assert.IsTrue(rect.Width >= Size, $"The editor reserved {rect.Width}px of width, narrower than the {Size} requested.");
		AssertSomethingWasDrawn("the bezier editor");
	}

	[TestMethod]
	public void BezierEditor_ShowsTheCurveItIsGiven()
	{
		points = new BezierControlPoints(new Vector2(0.25f, 0.1f), new Vector2(0.25f, 1f));
		Start(Draw);
		MoveAway();
		byte[] easeOut = Snapshot();

		points = new BezierControlPoints(new Vector2(0.9f, 0.05f), new Vector2(0.1f, 0.95f));
		Step(2);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(easeOut) > 0, "Two different control-point pairs drew the same curve.");
	}

	[TestMethod]
	public void BezierEditor_DraggingAHandleMovesAControlPoint()
	{
		points = new BezierControlPoints(new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.75f));
		Start(Draw);

		BezierControlPoints before = points;
		Rectangle rect = RectOf(Label);

		// The first control point sits a quarter of the way across and, with Y growing upward, a
		// quarter of the way up from the bottom of the plot.
		float x = rect.MinX + (rect.Width * 0.25f);
		float y = rect.MaxY - (rect.Height * 0.25f);
		Harness.Mouse.Drag(x, y, x + 30f, y - 30f);
		Step(2);

		Assert.AreNotEqual(before, points, "Dragging the first handle did not move a control point.");
		Assert.IsTrue(changed, "The editor reported no change while a handle was being dragged.");
	}

	[TestMethod]
	public void BezierEditor_LeftAlone_KeepsItsControlPoints()
	{
		BezierControlPoints original = new(new Vector2(0.25f, 0.1f), new Vector2(0.25f, 1f));
		points = original;
		Start(Draw);
		Step(5);

		Assert.AreEqual(original, points, "The control points moved on their own.");
		Assert.IsFalse(changed, "The editor reported a change nobody made.");
	}
}
