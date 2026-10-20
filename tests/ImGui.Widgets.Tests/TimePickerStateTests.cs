// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;
using System.Globalization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class TimePickerStateTests
{
	[TestMethod]
	public void ToWheels_TwentyFourHour_ShowsTheHourAsItIs()
	{
		Assert.AreEqual(new TimeWheels(17, 45, false), TimePickerState.ToWheels(new TimeOnly(17, 45), twelveHour: false, minuteStep: 1));
	}

	[TestMethod]
	public void ToWheels_TwelveHour_SplitsOffTheAfternoon()
	{
		Assert.AreEqual(new TimeWheels(5, 45, true), TimePickerState.ToWheels(new TimeOnly(17, 45), twelveHour: true, minuteStep: 1));
		Assert.AreEqual(new TimeWheels(0, 0, false), TimePickerState.ToWheels(new TimeOnly(0, 0), twelveHour: true, minuteStep: 1));
		Assert.AreEqual(new TimeWheels(0, 30, true), TimePickerState.ToWheels(new TimeOnly(12, 30), twelveHour: true, minuteStep: 1));
	}

	[TestMethod]
	public void ToWheels_MinutesBetweenSteps_RoundDown()
	{
		Assert.AreEqual(2, TimePickerState.ToWheels(new TimeOnly(9, 14), twelveHour: false, minuteStep: 5).Minute);
	}

	[TestMethod]
	public void FromWheels_RoundTripsEveryQuarterHour()
	{
		for (int hour = 0; hour < 24; hour++)
		{
			for (int minute = 0; minute < 60; minute += 15)
			{
				TimeOnly time = new(hour, minute);
				Assert.AreEqual(time, TimePickerState.FromWheels(TimePickerState.ToWheels(time, true, 15), true, 15));
				Assert.AreEqual(time, TimePickerState.FromWheels(TimePickerState.ToWheels(time, false, 15), false, 15));
			}
		}
	}

	[TestMethod]
	public void HourLabel_TwelveHour_ReadsTwelveForTheFirstItem()
	{
		CultureInfo previous = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
		try
		{
			Assert.AreEqual("12", TimePickerState.HourLabel(0, twelveHour: true));
			Assert.AreEqual("3", TimePickerState.HourLabel(3, twelveHour: true));
			Assert.AreEqual("03", TimePickerState.HourLabel(3, twelveHour: false));
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}

	[TestMethod]
	public void ValidStep_RefusesAStepThatDoesNotDivideAnHour()
	{
		Assert.AreEqual(15, TimePickerState.ValidStep(15));
		_ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => TimePickerState.ValidStep(7));
		_ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => TimePickerState.ValidStep(0));
	}
}
