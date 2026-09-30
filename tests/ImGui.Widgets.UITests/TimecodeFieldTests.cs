// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.TimecodeField"/> on its own.</summary>
[TestClass]
public sealed class TimecodeFieldTests : WidgetTest
{
	private const string Decrement = "tc/dec";
	private const string ValueBox = "tc/value";
	private const string Increment = "tc/inc";
	private const string Editor = "tc/edit";
	private const int Min = 0;

	private int frame;
	private bool changed;
	private TimecodeRate rate = TimecodeRate.Fps25;
	private int max = 100000;

	private void Draw() => changed |= ImGuiWidgets.TimecodeField("tc", ref frame, rate, Min, max);

	// Opens the value for typing, types, and presses the key that closes the editor.
	private void TypeAndPress(string text, ImGuiKey key)
	{
		Click(ValueBox);
		Step();
		Assert.IsTrue(IsVisible(Editor), "Clicking the value did not open it for typing.");

		Harness.Keyboard.Type(text);
		Harness.Keyboard.Press(key);
		Step();
	}

	[TestMethod]
	public void TimecodeField_MarksItsParts()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Decrement), "The field marked no decrement button.");
		Assert.IsTrue(IsVisible(ValueBox), "The field marked no value box.");
		Assert.IsTrue(IsVisible(Increment), "The field marked no increment button.");
		AssertSomethingWasDrawn("the timecode field");
	}

	[TestMethod]
	public void TimecodeField_IncrementStepsOneFrame()
	{
		Start(Draw);

		Click(Increment);

		Assert.AreEqual(1, frame);
		Assert.IsTrue(changed, "An increment that moved the value did not report a change.");
	}

	[TestMethod]
	public void TimecodeField_ShiftIncrementStepsOneSecond()
	{
		Start(Draw);

		HarnessKeyboard.KeyDown(ImGuiKey.ModShift);
		Step();
		Click(Increment);
		HarnessKeyboard.KeyUp(ImGuiKey.ModShift);
		Step();

		Assert.AreEqual(25, frame);
	}

	[TestMethod]
	public void TimecodeField_DecrementStopsAtTheMinimum()
	{
		Start(Draw);

		Click(Decrement);

		Assert.AreEqual(0, frame);
		Assert.IsFalse(changed, "A decrement at the minimum reported a change.");
	}

	[TestMethod]
	public void TimecodeField_DragScrubsOneFramePerPixel()
	{
		Start(Draw);

		Vector2 center = CenterOf(ValueBox);
		Harness.Mouse.Drag(center.X, center.Y, center.X + 40f, center.Y);
		Step();

		Assert.AreEqual(40, frame);
		Assert.IsFalse(IsVisible(Editor), "A scrub opened the value for typing.");
	}

	[TestMethod]
	public void TimecodeField_ClickEntersEditModeAndTypingCommits()
	{
		Start(Draw);

		TypeAndPress("2:10", ImGuiKey.Enter);

		Assert.AreEqual(60, frame);
		Assert.IsTrue(changed, "A committed edit did not report a change.");
		Assert.IsTrue(IsVisible(ValueBox), "The value box did not come back after the edit.");
	}

	[TestMethod]
	public void TimecodeField_InvalidTextReverts()
	{
		frame = 7;
		Start(Draw);

		TypeAndPress("99:99", ImGuiKey.Enter);

		Assert.AreEqual(7, frame);
		Assert.IsFalse(IsVisible(Editor), "Rejected text left the editor open.");
	}

	[TestMethod]
	public void TimecodeField_EscapeReverts()
	{
		frame = 7;
		Start(Draw);

		TypeAndPress("1:00", ImGuiKey.Escape);

		Assert.AreEqual(7, frame);
		Assert.IsFalse(IsVisible(Editor), "Escape left the editor open.");
	}

	[TestMethod]
	public void TimecodeField_TypedValueIsClamped()
	{
		max = 100;
		Start(Draw);

		TypeAndPress("10:00", ImGuiKey.Enter);

		Assert.AreEqual(100, frame);
	}

	[TestMethod]
	public void TimecodeField_ArrowKeysStepWhileHovered()
	{
		Start(Draw);

		Hover(ValueBox);
		Harness.Keyboard.Press(ImGuiKey.UpArrow);
		Assert.AreEqual(1, frame);

		Harness.Keyboard.Press(ImGuiKey.DownArrow, shift: true);
		Assert.AreEqual(0, frame);
	}

	[TestMethod]
	public void TimecodeField_DropFrameStepsAcrossTheSkippedLabels()
	{
		rate = TimecodeRate.Fps29_97Drop;
		frame = 1799;
		Start(Draw);

		Click(Increment);

		Assert.AreEqual(1800, frame);
		Assert.AreEqual("00:01:00;02", Timecode.Format(frame, rate));
	}
}
