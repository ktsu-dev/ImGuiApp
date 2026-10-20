// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.TimePicker"/> on its own.</summary>
[TestClass]
public sealed class TimePickerTests : WidgetTest
{
	private TimeOnly time = new(9, 30);
	private bool changed;
	private TimePickerOptions options = new();

	private void Draw() => changed |= ImGuiWidgets.TimePicker("alarm", ref time, options);

	private void Roll(string wheel, int clicks)
	{
		Rectangle rect = RectOf(wheel);
		Harness.Mouse.Wheel(rect.MinX + (rect.Width * 0.5f), rect.MinY + (rect.Height * 0.5f), clicks);
		Step();
	}

	[TestMethod]
	public void TimePicker_TwentyFourHour_DrawsTwoWheels()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible("alarm/hour"));
		Assert.IsTrue(IsVisible("alarm/minute"));
		Assert.IsFalse(IsVisible("alarm/period"));
		Assert.IsTrue(RectOf("alarm/minute").MinX > RectOf("alarm/hour").MaxX, "The minute wheel is not beside the hour wheel.");
	}

	[TestMethod]
	public void TimePicker_RollingTheHour_ChangesOnlyTheHour()
	{
		Start(Draw);

		Roll("alarm/hour", -1);

		Assert.AreEqual(new TimeOnly(10, 30), time);
		Assert.IsTrue(changed);
	}

	[TestMethod]
	public void TimePicker_MinutesWrapWithoutCarrying()
	{
		time = new TimeOnly(9, 59);
		Start(Draw);

		Roll("alarm/minute", -1);

		Assert.AreEqual(new TimeOnly(9, 0), time);
	}

	[TestMethod]
	public void TimePicker_TwelveHour_TheAfternoonWheelMovesTwelveHours()
	{
		options = new TimePickerOptions { TwelveHour = true };
		Start(Draw);
		Assert.IsTrue(IsVisible("alarm/period"));

		Roll("alarm/period", -1);

		Assert.AreEqual(new TimeOnly(21, 30), time);
	}

	[TestMethod]
	public void TimePicker_MinuteStep_RollsByTheStep()
	{
		options = new TimePickerOptions { MinuteStep = 15 };
		Start(Draw);

		Roll("alarm/minute", -1);

		Assert.AreEqual(new TimeOnly(9, 45), time);
	}

	[TestMethod]
	public void TimePicker_Untouched_KeepsATimeBetweenSteps()
	{
		time = new TimeOnly(9, 37, 12);
		options = new TimePickerOptions { MinuteStep = 15 };
		Start(Draw);
		Step(10);

		Assert.AreEqual(new TimeOnly(9, 37, 12), time);
		Assert.IsFalse(changed);
	}
}
