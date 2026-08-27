// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.Stepper"/> on its own.</summary>
[TestClass]
public sealed class StepperTests : WidgetTest
{
	private const string Label = "Quantity";

	// The stepper marks only its value box. The buttons either side are ordinary ImGui buttons one
	// frame height wide, laid out with the style's inner spacing between them and the box, so
	// aiming half a frame height outside the box lands inside the button whatever that spacing is.
	private const string ValueBox = "##value";

	private int value;
	private int step = 1;
	private int min = int.MinValue;
	private int max = int.MaxValue;

	private void Draw() => ImGuiWidgets.Stepper(Label, ref value, step, min, max);

	private void ClickDecrement()
	{
		Rectangle rect = RectOf(ValueBox);
		Harness.Mouse.Click(rect.MinX - (rect.Height / 2f), rect.MinY + (rect.Height / 2f));
		Step();
	}

	private void ClickIncrement()
	{
		Rectangle rect = RectOf(ValueBox);
		Harness.Mouse.Click(rect.MaxX + (rect.Height / 2f), rect.MinY + (rect.Height / 2f));
		Step();
	}

	[TestMethod]
	public void Stepper_IsDrawnAndMarksItsValueBox()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(ValueBox), "The stepper marked no value box.");
		AssertSomethingWasDrawn("the stepper");
	}

	[TestMethod]
	public void Stepper_IncrementButtonAddsOneStep()
	{
		value = 4;
		step = 3;
		Start(Draw);

		ClickIncrement();

		Assert.AreEqual(7, value);
	}

	[TestMethod]
	public void Stepper_DecrementButtonSubtractsOneStep()
	{
		value = 4;
		step = 3;
		Start(Draw);

		ClickDecrement();

		Assert.AreEqual(1, value);
	}

	[TestMethod]
	public void Stepper_StopsAtItsUpperBound()
	{
		value = 10;
		max = 10;
		Start(Draw);

		ClickIncrement();

		Assert.AreEqual(10, value, "The stepper stepped past its maximum.");
	}

	[TestMethod]
	public void Stepper_StopsAtItsLowerBound()
	{
		value = 0;
		min = 0;
		Start(Draw);

		ClickDecrement();

		Assert.AreEqual(0, value, "The stepper stepped past its minimum.");
	}

	[TestMethod]
	public void Stepper_RedrawsTheNewValue()
	{
		value = 0;
		Start(Draw);

		byte[] before = Snapshot();
		ClickIncrement();
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(before) > 0, "The value box drew identically after stepping.");
	}
}
