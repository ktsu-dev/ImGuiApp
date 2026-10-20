// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.Stepper"/> on its own.</summary>
[TestClass]
public sealed class StepperTests : WidgetTest
{
	private const string Label = "Quantity";

	// The stepper marks its three parts under its label, and the whole widget under the label itself.
	private const string ValueBox = Label + "/value";

	private int value;
	private int step = 1;
	private int min = int.MinValue;
	private int max = int.MaxValue;

	private void Draw() => ImGuiWidgets.Stepper(Label, ref value, step, min, max);

	private void ClickDecrement() => Click(Label + "/decrement");

	private void ClickIncrement() => Click(Label + "/increment");

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
	public void Stepper_ReleasingAfterAHoldKeepsTheLastRepeatedValue()
	{
		value = 0;
		Start(Draw);

		// Half a second at the harness's 60 frames a second: past the 0.4 s delay, so the hold has
		// repeated at least once before the button is let go.
		Rectangle rect = RectOf(ValueBox);
		Harness.Mouse.MoveTo(rect.MaxX + (rect.Height / 2f), rect.MinY + (rect.Height / 2f));
		Step();
		HarnessMouse.Down(0);
		Step(30);
		int whileHeld = value;
		HarnessMouse.Up(0);
		Step(2);

		Assert.IsTrue(whileHeld >= 2, $"Holding the button for half a second repeated to {whileHeld}.");
		Assert.AreEqual(whileHeld, value, "Releasing the button after a hold took one more step.");
	}

	[TestMethod]
	public void Stepper_AClickAfterAHoldStillStepsOnce()
	{
		value = 0;
		Start(Draw);

		Rectangle rect = RectOf(ValueBox);
		Harness.Mouse.MoveTo(rect.MaxX + (rect.Height / 2f), rect.MinY + (rect.Height / 2f));
		Step();
		HarnessMouse.Down(0);
		Step(30);
		HarnessMouse.Up(0);
		Step(2);
		int afterHold = value;

		ClickIncrement();

		Assert.AreEqual(afterHold + 1, value, "A click following a hold did not step exactly once.");
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

	[TestMethod]
	public void Stepper_MarksItself()
	{
		Rectangle whole = AssertMarksItself(Label, Draw);

		foreach (string part in new[] { "decrement", "value", "increment" })
		{
			Rectangle rect = RectOf($"{Label}/{part}");
			Assert.IsTrue(
				rect.MinX >= whole.MinX && rect.MaxX <= whole.MaxX && rect.MinY >= whole.MinY && rect.MaxY <= whole.MaxY,
				$"The stepper's {part} at {rect} is outside the whole widget at {whole}.");
		}

		Assert.IsTrue(RectOf(Label + "/decrement").MaxX <= RectOf(ValueBox).MinX, "The decrement button is not left of the value.");
		Assert.IsTrue(RectOf(Label + "/increment").MinX >= RectOf(ValueBox).MaxX, "The increment button is not right of the value.");
	}

	[TestMethod]
	public void Stepper_TwoInOneWindowResolveSeparately()
	{
		int other = 0;
		Start(() =>
		{
			ImGuiWidgets.Stepper("Adults", ref value);
			ImGuiWidgets.Stepper("Children", ref other);
		});

		Click("Children/increment");

		Assert.AreEqual(1, other, "Clicking the second stepper's increment did not reach it.");
		Assert.AreEqual(0, value, "Clicking the second stepper's increment changed the first.");
		Assert.IsTrue(RectOf("Adults/value").MaxY <= RectOf("Children/value").MinY, "The two steppers' value boxes were not told apart.");
	}
}
