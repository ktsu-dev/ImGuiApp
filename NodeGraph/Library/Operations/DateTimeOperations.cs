// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

// Date and time operations as static method nodes.

namespace ktsu.NodeGraph.Library.Operations;
using System;
using System.ComponentModel;

/// <summary>
/// Date and time operations.
/// </summary>
public static class DateTimeOperations
{
	[Node("Add Days")]
	[Description("Adds days to a date")]
	public static DateTime AddDays(DateTime date, double days) => date.AddDays(days);

	[Node("Add Hours")]
	[Description("Adds hours to a date")]
	public static DateTime AddHours(DateTime date, double hours) => date.AddHours(hours);

	[Node("Add Minutes")]
	[Description("Adds minutes to a date")]
	public static DateTime AddMinutes(DateTime date, double minutes) => date.AddMinutes(minutes);

	[Node("Date Difference")]
	[Description("Calculates difference between two dates")]
	public static TimeSpan DateDifference(DateTime date1, DateTime date2) => date1 - date2;

	[Node("Format Date")]
	[Description("Formats a date as string")]
	public static string FormatDate(DateTime date, string format = "yyyy-MM-dd") => date.ToString(format);

	[Node("Parse Date")]
	[Description("Parses a string as date")]
	public static DateTime ParseDate(string dateString) =>
		DateTime.TryParse(dateString, out DateTime result) ? result : DateTime.MinValue;

	[Node("Is Weekend")]
	[Description("Checks if date falls on weekend")]
	public static bool IsWeekend(DateTime date) =>
		date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

	[Node("Days In Month")]
	[Description("Gets number of days in a month")]
	public static int DaysInMonth(int year, int month) => DateTime.DaysInMonth(year, month);
}
