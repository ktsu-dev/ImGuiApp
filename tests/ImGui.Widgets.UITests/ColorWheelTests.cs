// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.ColorWheel"/> and <see cref="ImGuiWidgets.LiftGammaGain"/> on their own.</summary>
[TestClass]
public sealed class ColorWheelTests : WidgetTest
{
	private const string Label = "Balance";
	private const float Side = 160f;

	// Mirrors ColorWheelState, which is internal to the library and not visible from here.
	private const float RedAngle = 103f;
	private const float FineScale = 0.25f;

	private ColorWheelValue value = ColorWheelValue.Neutral;
	private ColorWheelValue lift = ColorWheelValue.Neutral;
	private ColorWheelValue gamma = ColorWheelValue.Neutral;
	private ColorWheelValue gain = ColorWheelValue.Neutral;

	private void Draw() => ImGuiWidgets.ColorWheel(Label, ref value, Side);

	private void DrawSet() => ImGuiWidgets.LiftGammaGain("Grade", ref lift, ref gamma, ref gain, 120f);

	private void DragBy(string name, float fromX, float fromY, float dx, float dy)
	{
		Rectangle rect = RectOf(name);
		float x = rect.MinX + (rect.Width * fromX);
		float y = rect.MinY + (rect.Height * fromY);
		Harness.Mouse.Drag(x, y, x + dx, y + dy);
		Step();
	}

	[TestMethod]
	public void ColorWheel_IsDrawnAndMarksItself()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Label), "The wheel marked no probe item.");
		AssertSomethingWasDrawn("the wheel");
	}

	[TestMethod]
	public void ColorWheel_IsSquareAtTheRequestedSize()
	{
		Start(Draw);

		Rectangle rect = RectOf(Label);

		Assert.IsTrue(Math.Abs(rect.Width - Side) <= 2, $"The wheel reserved {rect.Width}px of width rather than {Side}.");
		Assert.IsTrue(Math.Abs(rect.Height - Side) <= 2, $"The wheel reserved {rect.Height}px of height rather than {Side}.");
	}

	[TestMethod]
	public void ColorWheel_AClickDoesNotJumpTheHandle()
	{
		// A trackball grabs the handle where it is: pressing near the rim is not a push to the rim.
		value = new ColorWheelValue(30f, 0.25f);
		Start(Draw);

		ClickFraction(Label, 0.9f, 0.5f);

		Assert.AreEqual(new ColorWheelValue(30f, 0.25f), value);
	}

	[TestMethod]
	public void ColorWheel_DraggingPushesTheHandleTheWayThePointerWent()
	{
		Start(Draw);

		// Right, from anywhere on the wheel. Right is 103 degrees clockwise of red, which is a blue.
		DragBy(Label, 0.3f, 0.3f, 40f, 0f);

		Assert.IsTrue(value.Strength > 0.2f, $"Strength stayed at {value.Strength} after a drag of a quarter of the wheel.");
		float expected = ColorWheelValue.WrapHue(-RedAngle);
		Assert.IsTrue(Math.Abs(value.Hue - expected) < 3f, $"A drag to the right gave hue {value.Hue}, not about {expected}.");
	}

	[TestMethod]
	public void ColorWheel_StrengthStopsAtTheRim()
	{
		Start(Draw);

		DragBy(Label, 0.5f, 0.5f, 0f, -400f);

		Assert.IsTrue(value.Strength is > 0.99f and <= 1f, $"Strength ran to {value.Strength} on a drag far past the rim.");
	}

	[TestMethod]
	public void ColorWheel_ShiftDragsFinely()
	{
		Start(Draw);
		DragBy(Label, 0.5f, 0.5f, 40f, 0f);
		float coarse = value.Strength;

		value = ColorWheelValue.Neutral;
		HarnessKeyboard.KeyDown(ImGuiKey.ModShift);
		Step();
		DragBy(Label, 0.5f, 0.5f, 40f, 0f);
		HarnessKeyboard.KeyUp(ImGuiKey.ModShift);
		Step();

		Assert.IsTrue(value.Strength > 0f, "A fine drag did not move the handle at all.");
		Assert.AreEqual(coarse * FineScale, value.Strength, 0.02f, "A fine drag did not move the handle a quarter as far.");
	}

	[TestMethod]
	public void ColorWheel_DoubleClickReturnsItToNeutral()
	{
		value = new ColorWheelValue(200f, 0.6f, 0.2f);
		Start(Draw);

		Vector2 center = CenterOf(Label);
		Harness.Mouse.Click(center.X, center.Y);
		Harness.Mouse.Click(center.X, center.Y);
		Step();

		Assert.AreEqual(0f, value.Strength, $"Strength stayed at {value.Strength} after a double-click.");
		Assert.AreEqual(200f, value.Hue, 1e-3f, "A reset threw away the direction the wheel was last pushed.");
		Assert.AreEqual(0.2f, value.Master, 1e-4f, "Resetting the ball reset the master too.");
	}

	[TestMethod]
	public void ColorWheel_MasterSliderSitsUnderTheWheelAndMarksItself()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible($"{Label}/master"), "The master slider marked no probe item.");
		Rectangle wheel = RectOf(Label);
		Rectangle master = RectOf($"{Label}/master");
		Assert.IsTrue(master.MinY >= wheel.MaxY, "The master slider is not below the wheel.");
		Assert.IsTrue(Math.Abs(master.Width - wheel.Width) <= 2, $"The slider is {master.Width}px wide under a {wheel.Width}px wheel.");
	}

	[TestMethod]
	public void ColorWheel_DraggingTheMasterRaisesItAndLeavesTheBall()
	{
		value = new ColorWheelValue(60f, 0.3f);
		Start(Draw);

		// A quarter of the bar's width is a quarter of the way along a range two wide.
		DragBy($"{Label}/master", 0.2f, 0.5f, Side / 4f, 0f);

		Assert.AreEqual(0.5f, value.Master, 0.05f, "A quarter-width drag did not move the master by a half.");
		Assert.AreEqual(0.3f, value.Strength, 1e-4f, "Dragging the master moved the ball.");
	}

	[TestMethod]
	public void ColorWheel_DoubleClickingTheMasterZeroesOnlyIt()
	{
		value = new ColorWheelValue(60f, 0.3f, -0.4f);
		Start(Draw);

		Vector2 center = CenterOf($"{Label}/master");
		Harness.Mouse.Click(center.X, center.Y);
		Harness.Mouse.Click(center.X, center.Y);
		Step();

		Assert.AreEqual(0f, value.Master, $"A double-click left the master at {value.Master}.");
		Assert.AreEqual(0.3f, value.Strength, 1e-4f, "Resetting the master reset the ball too.");
	}

	[TestMethod]
	public void ColorWheel_RedrawsTheHandleWhereItWasMoved()
	{
		Start(Draw);
		MoveAway();
		byte[] neutral = Snapshot();

		value = new ColorWheelValue(120f, 0.8f);
		Step(2);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(neutral) > 0, "The handle drew in the same place after the value moved.");
	}

	[TestMethod]
	public void LiftGammaGain_MarksAllThreeWheelsUnderItsLabel()
	{
		Start(DrawSet);

		Assert.IsTrue(IsVisible("Grade/Lift"), "The lift wheel marked no probe item.");
		Assert.IsTrue(IsVisible("Grade/Gamma"), "The gamma wheel marked no probe item.");
		Assert.IsTrue(IsVisible("Grade/Gain"), "The gain wheel marked no probe item.");
		Assert.IsTrue(IsVisible("Grade/Gain/master"), "The gain wheel's master slider marked no probe item.");
	}

	[TestMethod]
	public void LiftGammaGain_LaysTheWheelsOutLeftToRight()
	{
		Start(DrawSet);

		Rectangle liftRect = RectOf("Grade/Lift");
		Rectangle gammaRect = RectOf("Grade/Gamma");
		Rectangle gainRect = RectOf("Grade/Gain");

		Assert.IsTrue(liftRect.MaxX <= gammaRect.MinX && gammaRect.MaxX <= gainRect.MinX, "The three wheels overlap or are out of order.");
		Assert.AreEqual(liftRect.MinY, gainRect.MinY, 1f, "The wheels are not on one row.");
	}

	[TestMethod]
	public void LiftGammaGain_DraggingOneWheelLeavesTheOthersAlone()
	{
		Start(DrawSet);

		DragBy("Grade/Gain", 0.5f, 0.5f, 0f, -30f);

		Assert.IsTrue(gain.Strength > 0f, "Dragging the gain wheel did not move it.");
		Assert.IsTrue(lift.IsNeutral, $"Dragging gain moved lift to {lift}.");
		Assert.IsTrue(gamma.IsNeutral, $"Dragging gain moved gamma to {gamma}.");
	}

	[TestMethod]
	public void ColorWheel_ShowsAMoveCursorOverTheWheel()
	{
		Start(Draw);

		Hover(Label);

		Assert.AreEqual(ImGuiMouseCursor.ResizeAll, Harness.MouseCursor, "Hovering the wheel did not show a move cursor.");
	}

	[TestMethod]
	public void ColorWheel_ShowsAHorizontalResizeCursorOverTheMaster()
	{
		Start(Draw);

		Hover($"{Label}/master");

		Assert.AreEqual(ImGuiMouseCursor.ResizeEw, Harness.MouseCursor, "Hovering the master slider did not show a horizontal resize cursor.");
	}
}
