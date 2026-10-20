// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>How a time picker looks and behaves.</summary>
public sealed record TimePickerOptions
{
	/// <summary>Gets a value indicating whether hours run from 1 to 12 with an AM/PM wheel, rather than from 0 to 23. Off by default.</summary>
	public bool TwelveHour { get; init; }

	/// <summary>Gets the minutes between neighbouring items on the minute wheel, such as 5 or 15. Must divide 60; the default is 1.</summary>
	public int MinuteStep { get; init; } = 1;

	/// <summary>Gets how many rows each wheel shows. The default is five.</summary>
	public int VisibleRows { get; init; } = 5;
}

/// <summary>The wheels a time picker shows for one time.</summary>
/// <param name="Hour">The hour wheel's item: 0 to 23, or 0 to 11 for 12 to 11 on a twelve-hour clock.</param>
/// <param name="Minute">The minute wheel's item: the minute divided by the step.</param>
/// <param name="Afternoon">Whether the AM/PM wheel shows PM. Unused on a 24-hour clock.</param>
internal readonly record struct TimeWheels(int Hour, int Minute, bool Afternoon);

/// <summary>Converts between a time and the items its wheels show, with no ImGui calls.</summary>
internal static class TimePickerState
{
	private static string[]? hourLabels12;
	private static string[]? hourLabels24;
	private static CultureInfo? labelCulture12;
	private static CultureInfo? labelCulture24;

	/// <summary>Finds the items the wheels show for a time.</summary>
	/// <param name="time">The time. Seconds are ignored, and minutes between steps round down.</param>
	/// <param name="twelveHour">Whether the clock runs to twelve.</param>
	/// <param name="minuteStep">The minutes between neighbouring minute items.</param>
	/// <returns>The wheels' items.</returns>
	internal static TimeWheels ToWheels(TimeOnly time, bool twelveHour, int minuteStep)
	{
		int step = ValidStep(minuteStep);
		int minute = time.Minute / step;
		return twelveHour
			? new TimeWheels(time.Hour % 12, minute, time.Hour >= 12)
			: new TimeWheels(time.Hour, minute, false);
	}

	/// <summary>Builds the time the wheels show.</summary>
	/// <param name="wheels">The wheels' items.</param>
	/// <param name="twelveHour">Whether the clock runs to twelve.</param>
	/// <param name="minuteStep">The minutes between neighbouring minute items.</param>
	/// <returns>The time, with no seconds.</returns>
	internal static TimeOnly FromWheels(TimeWheels wheels, bool twelveHour, int minuteStep)
	{
		int step = ValidStep(minuteStep);
		int hour = twelveHour ? (wheels.Hour % 12) + (wheels.Afternoon ? 12 : 0) : wheels.Hour % 24;
		int minute = Math.Clamp(wheels.Minute * step, 0, 59);
		return new TimeOnly(hour, minute);
	}

	/// <summary>Writes the hour wheel's label for an item.</summary>
	/// <param name="item">The item.</param>
	/// <param name="twelveHour">Whether the clock runs to twelve, where item 0 reads 12.</param>
	/// <returns>The label.</returns>
	internal static string HourLabel(int item, bool twelveHour) =>
		twelveHour ? (item == 0 ? 12 : item).ToString(CultureInfo.CurrentCulture) : item.ToString("00", CultureInfo.CurrentCulture);

	/// <summary>Gets the hour wheel's labels, built once per clock for the culture in use then.</summary>
	/// <param name="twelveHour">Whether the clock runs to twelve.</param>
	/// <returns>The labels.</returns>
	internal static IReadOnlyList<string> HourLabels(bool twelveHour)
	{
		CultureInfo culture = CultureInfo.CurrentCulture;
		if (twelveHour)
		{
			if (hourLabels12 is null || !ReferenceEquals(labelCulture12, culture))
			{
				hourLabels12 = BuildHourLabels(twelveHour: true);
				labelCulture12 = culture;
			}

			return hourLabels12;
		}

		if (hourLabels24 is null || !ReferenceEquals(labelCulture24, culture))
		{
			hourLabels24 = BuildHourLabels(twelveHour: false);
			labelCulture24 = culture;
		}

		return hourLabels24;
	}

	/// <summary>Gets the AM/PM wheel's labels in the culture in use, falling back to AM and PM where it names none.</summary>
	/// <returns>The two labels, morning first.</returns>
	internal static IReadOnlyList<string> PeriodLabels()
	{
		DateTimeFormatInfo format = CultureInfo.CurrentCulture.DateTimeFormat;
		return [format.AMDesignator is { Length: > 0 } am ? am : "AM", format.PMDesignator is { Length: > 0 } pm ? pm : "PM"];
	}

	/// <summary>Checks a minute step.</summary>
	/// <param name="minuteStep">The step.</param>
	/// <returns>The step.</returns>
	/// <exception cref="ArgumentOutOfRangeException">The step does not divide an hour.</exception>
	internal static int ValidStep(int minuteStep) =>
		minuteStep is > 0 and <= 60 && 60 % minuteStep == 0
			? minuteStep
			: throw new ArgumentOutOfRangeException(nameof(minuteStep), minuteStep, "The minute step must divide 60.");

	private static string[] BuildHourLabels(bool twelveHour)
	{
		string[] labels = new string[twelveHour ? 12 : 24];
		for (int i = 0; i < labels.Length; i++)
		{
			labels[i] = HourLabel(i, twelveHour);
		}

		return labels;
	}
}
