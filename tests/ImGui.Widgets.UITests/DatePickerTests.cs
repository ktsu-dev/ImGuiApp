// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives the Hexa-backed <see cref="ImGuiWidgets.DatePicker"/> and
/// <see cref="ImGuiWidgets.YearPicker"/> on their own.
/// </summary>
/// <remarks>
/// The date picker draws a Material Icons glyph and falls back to a placeholder box without that
/// font in the atlas. These tests deliberately register no font, so they cover the control's
/// layout and behavior rather than its glyphs — a missing icon is a placeholder, not a failure.
/// </remarks>
[TestClass]
public sealed class DatePickerTests : WidgetTest
{
	private const string DateLabel = "Due";
	private const string YearLabel = "Year";
	private const string Span = "picker";

	private DateTime date = new(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc);
	private bool changed;

	private void DrawDatePicker()
	{
		Vector2 origin = ImGui.GetCursorScreenPos();
		changed |= ImGuiWidgets.DatePicker(DateLabel, ref date);
		MarkSpan(Span, origin);
	}

	private void DrawYearPicker()
	{
		Vector2 origin = ImGui.GetCursorScreenPos();
		changed |= ImGuiWidgets.YearPicker(YearLabel, ref date);
		MarkSpan(Span, origin);
	}

	[TestMethod]
	public void DatePicker_DrawsACalendar()
	{
		Start(DrawDatePicker);

		Assert.IsTrue(IsVisible(Span), "The date picker drew nothing.");
		AssertSomethingWasDrawn("the date picker");
	}

	[TestMethod]
	public void DatePicker_ShowsTheDateItIsGiven()
	{
		date = new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc);
		Start(DrawDatePicker);
		MoveAway();
		byte[] march = Snapshot();

		date = new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc);
		Step(2);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(march) > 0, "Two different months drew the same calendar.");
	}

	[TestMethod]
	public void DatePicker_LeftAlone_ReportsNoChange()
	{
		Start(DrawDatePicker);
		Step(5);

		Assert.IsFalse(changed, "The date picker reported a change nobody made.");
		Assert.AreEqual(new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc), date);
	}

	[TestMethod]
	public void DatePicker_ClickingTheFieldOpensACalendar()
	{
		Start(DrawDatePicker);
		MoveAway();
		byte[] closed = Snapshot();

		Click(Span);

		Rectangle field = RectOf(Span);
		Rectangle opened = BoundsOfDifference(closed) ?? throw new InvalidOperationException("Clicking the field opened nothing.");

		Assert.IsTrue(
			opened.MaxY > field.MaxY + 40,
			$"What appeared ran from {opened.MinY} to {opened.MaxY}, no further down than the {field.MaxY}px field, so no calendar opened.");
	}

	// The calendar opens on the current month rather than the month of the date it holds, so which
	// day a given cell carries depends on when the suite runs. What is stable is the grid: a cell
	// in the middle of it always holds some day, whatever month is on screen.
	[TestMethod]
	public void DatePicker_ClickingADayInTheCalendarPicksIt()
	{
		DateTime original = new(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc);
		date = original;
		Start(DrawDatePicker);

		Click(Span);

		Rectangle field = RectOf(Span);
		Harness.Mouse.Click(field.MinX + 140f, field.MaxY + 105f);
		Step();

		Assert.IsTrue(changed, "Clicking a day in the calendar picked no date.");
		Assert.AreNotEqual(original, date, "The calendar reported a change but left the date alone.");
	}

	[TestMethod]
	public void YearPicker_DrawsAGridOfYears()
	{
		Start(DrawYearPicker);

		Assert.IsTrue(IsVisible(Span), "The year picker drew nothing.");
		AssertSomethingWasDrawn("the year picker");
	}

	[TestMethod]
	public void YearPicker_ClickingAYearPicksIt()
	{
		date = new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc);
		Start(DrawYearPicker);

		ClickFraction(Span, 0.5f, 0.5f);

		Assert.IsTrue(changed, "Clicking inside the year grid picked no year.");
		Assert.AreEqual(3, date.Month, "Picking a year moved the month.");
		Assert.AreEqual(14, date.Day, "Picking a year moved the day of the month.");
	}

	[TestMethod]
	public void YearPicker_NeedsNoIconFont()
	{
		Start(DrawYearPicker);

		// Unlike the date picker, the year grid draws no Material glyph, so it is fully rendered
		// in a harness with no icon font registered.
		Assert.IsTrue(IsVisible(Span), "The year picker drew nothing without an icon font.");
	}
}
