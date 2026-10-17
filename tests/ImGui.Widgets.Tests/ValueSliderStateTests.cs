// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;
using System.Globalization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the arithmetic behind ValueSlider: formatting, parsing, dragging and the fill. All pure, with
/// no ImGui context.
/// </summary>
[TestClass]
public class ValueSliderStateTests
{
	private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

	// Built rather than looked up, so the tests also run where the runtime carries no culture data.
	private static readonly NumberFormatInfo DecimalComma = new() { NumberDecimalSeparator = ",", NumberGroupSeparator = "." };

	[TestMethod]
	[DataRow(0.0, 255.0, "0")]
	[DataRow(-100.0, 100.0, "0")]
	[DataRow(0.0, 50.0, "0.0")]
	[DataRow(-5.0, 5.0, "0.0")]
	[DataRow(0.0, 4.0, "0.00")]
	[DataRow(0.0, 1.0, "0.00")]
	[DataRow(0.0, 0.5, "0.000")]
	public void DefaultFormat_ShowsAsManyDecimalsAsTheRangeNeeds(double min, double max, string expected) =>
		Assert.AreEqual(expected, ImGuiWidgets.ValueSliderState.DefaultFormat(min, max));

	[TestMethod]
	public void Format_WritesTheUnitStraightAfterTheNumber()
	{
		Assert.AreEqual("75%", ImGuiWidgets.ValueSliderState.Format(75.0, "0", "%", Invariant));
		Assert.AreEqual("4.5 px", ImGuiWidgets.ValueSliderState.Format(4.5, "0.0", " px", Invariant));
		Assert.AreEqual("12", ImGuiWidgets.ValueSliderState.Format(12.0, "0", null, Invariant));
	}

	[TestMethod]
	public void Format_ShowsZeroRatherThanMinusZero()
	{
		Assert.AreEqual("0.0", ImGuiWidgets.ValueSliderState.Format(-0.01, "0.0", null, Invariant));
		Assert.AreEqual("0°", ImGuiWidgets.ValueSliderState.Format(-0.2, "0", "°", Invariant));
		Assert.AreEqual("-0.5", ImGuiWidgets.ValueSliderState.Format(-0.5, "0.0", null, Invariant));
	}

	[TestMethod]
	public void Display_WritesATrueMinusSign_AndTryParseReadsIt()
	{
		Assert.AreEqual("\u22121.51", ImGuiWidgets.ValueSliderState.Display(-1.51, "0.00", null, Invariant));
		Assert.AreEqual("0.0", ImGuiWidgets.ValueSliderState.Display(-0.01, "0.0", null, Invariant));
		Assert.AreEqual("1.5 px", ImGuiWidgets.ValueSliderState.Display(1.5, "0.0", " px", Invariant));
		Assert.IsTrue(ImGuiWidgets.ValueSliderState.TryParse("\u22121.5", null, Invariant, out double value));
		Assert.AreEqual(-1.5, value);
	}

	[TestMethod]
	public void Format_UsesTheCultureGiven() =>
		Assert.AreEqual("2,5", ImGuiWidgets.ValueSliderState.Format(2.5, "0.0", null, DecimalComma));

	[TestMethod]
	[DataRow("42", null, 42.0)]
	[DataRow("  -3.25 ", null, -3.25)]
	[DataRow("80%", "%", 80.0)]
	[DataRow("80 %", "%", 80.0)]
	[DataRow("12 PX", " px", 12.0)]
	[DataRow("1e2", null, 100.0)]
	public void TryParse_ReadsANumberAndIgnoresTheUnit(string text, string? unit, double expected)
	{
		Assert.IsTrue(ImGuiWidgets.ValueSliderState.TryParse(text, unit, Invariant, out double value));
		Assert.AreEqual(expected, value, 1e-9);
	}

	[TestMethod]
	public void TryParse_FallsBackToTheInvariantCulture()
	{
		Assert.IsTrue(ImGuiWidgets.ValueSliderState.TryParse("2,5", null, DecimalComma, out double comma));
		Assert.AreEqual(2.5, comma, 1e-9);

		Assert.IsTrue(ImGuiWidgets.ValueSliderState.TryParse("2.5", null, DecimalComma, out double point));
		Assert.AreEqual(2.5, point, 1e-9, "A point typed where the culture writes a comma was not read as a decimal mark.");
	}

	[TestMethod]
	[DataRow("")]
	[DataRow("abc")]
	[DataRow("%")]
	[DataRow("NaN")]
	[DataRow("Infinity")]
	public void TryParse_RejectsWhatIsNotAFiniteNumber(string text) =>
		Assert.IsFalse(ImGuiWidgets.ValueSliderState.TryParse(text, "%", Invariant, out _));

	[TestMethod]
	public void Drag_TheFullWidthCoversTheFullRange()
	{
		Assert.AreEqual(75.0, ImGuiWidgets.ValueSliderState.Drag(50.0, 50f, 200f, 0.0, 100.0, 1f), 1e-9);
		Assert.AreEqual(-50.0, ImGuiWidgets.ValueSliderState.Drag(0.0, -50f, 200f, -100.0, 100.0, 1f), 1e-9);
	}

	[TestMethod]
	public void Drag_ScalesByTheFineFactor() =>
		Assert.AreEqual(52.5, ImGuiWidgets.ValueSliderState.Drag(50.0, 50f, 200f, 0.0, 100.0, 0.1f), 1e-6);

	[TestMethod]
	public void Drag_StaysInsideTheRange()
	{
		Assert.AreEqual(100.0, ImGuiWidgets.ValueSliderState.Drag(90.0, 1000f, 200f, 0.0, 100.0, 1f));
		Assert.AreEqual(0.0, ImGuiWidgets.ValueSliderState.Drag(10.0, -1000f, 200f, 0.0, 100.0, 1f));
		Assert.AreEqual(100.0, ImGuiWidgets.ValueSliderState.Drag(150.0, 0f, 0f, 0.0, 100.0, 1f));
	}

	[TestMethod]
	public void FillSpan_FromTheMinimumRunsFromTheLeftEdge()
	{
		(float start, float end) = ImGuiWidgets.ValueSliderState.FillSpan(25.0, 0.0, 100.0, ValueSliderFill.FromMinimum);

		Assert.AreEqual(0f, start);
		Assert.AreEqual(0.25f, end, 1e-6f);
	}

	[TestMethod]
	public void FillSpan_FromZeroRunsEitherSideOfZero()
	{
		(float start, float end) = ImGuiWidgets.ValueSliderState.FillSpan(50.0, -100.0, 100.0, ValueSliderFill.FromZero);
		Assert.AreEqual(0.5f, start, 1e-6f);
		Assert.AreEqual(0.75f, end, 1e-6f);

		(start, end) = ImGuiWidgets.ValueSliderState.FillSpan(-50.0, -100.0, 100.0, ValueSliderFill.FromZero);
		Assert.AreEqual(0.25f, start, 1e-6f);
		Assert.AreEqual(0.5f, end, 1e-6f);
	}

	[TestMethod]
	public void FillSpan_FromZeroClampsAZeroOutsideTheRange()
	{
		(float start, float end) = ImGuiWidgets.ValueSliderState.FillSpan(15.0, 10.0, 20.0, ValueSliderFill.FromZero);

		Assert.AreEqual(0f, start);
		Assert.AreEqual(0.5f, end, 1e-6f);
	}

	[TestMethod]
	public void Fraction_IsZeroForAnEmptyRange() =>
		Assert.AreEqual(0f, ImGuiWidgets.ValueSliderState.Fraction(5.0, 3.0, 3.0));

	[TestMethod]
	public void Logarithmic_EqualDistancesMultiplyByEqualFactors()
	{
		// 1..1000 over 300 px: each 100 px is a factor of ten.
		Assert.AreEqual(10.0, ImGuiWidgets.ValueSliderState.Drag(1.0, 100f, 300f, 1.0, 1000.0, 1f, logarithmic: true), 1e-9);
		Assert.AreEqual(100.0, ImGuiWidgets.ValueSliderState.Drag(10.0, 100f, 300f, 1.0, 1000.0, 1f, logarithmic: true), 1e-9);
		Assert.AreEqual(10.0, ImGuiWidgets.ValueSliderState.Drag(100.0, -100f, 300f, 1.0, 1000.0, 1f, logarithmic: true), 1e-9);
	}

	[TestMethod]
	public void Logarithmic_ShiftDragsFinelyInTheSameProportion() =>
		Assert.AreEqual(Math.Pow(10.0, 0.1), ImGuiWidgets.ValueSliderState.Drag(1.0, 100f, 300f, 1.0, 1000.0, 0.1f, logarithmic: true), 1e-6);

	[TestMethod]
	public void Logarithmic_PositionAndValueAreInverses()
	{
		foreach (double v in new[] { 1.0, 2.0, 37.5, 999.0, 1000.0 })
		{
			float fraction = ImGuiWidgets.ValueSliderState.Fraction(v, 1.0, 1000.0, logarithmic: true);
			Assert.AreEqual(v, ImGuiWidgets.ValueSliderState.ValueAt(fraction, 1.0, 1000.0, logarithmic: true), v * 1e-5);
		}

		Assert.AreEqual(0.5f, ImGuiWidgets.ValueSliderState.Fraction(10.0, 1.0, 100.0, logarithmic: true), 1e-6f);
	}

	[TestMethod]
	public void Logarithmic_ARangeFromZeroKeepsZeroAtItsLeftEnd()
	{
		// Zero sits at the left edge, and the rest runs from the epsilon to the maximum.
		Assert.AreEqual(0f, ImGuiWidgets.ValueSliderState.Fraction(0.0, 0.0, 100.0, logarithmic: true, epsilon: 1.0));
		Assert.AreEqual(0.0, ImGuiWidgets.ValueSliderState.ValueAt(0.0, 0.0, 100.0, logarithmic: true, epsilon: 1.0));
		Assert.AreEqual(10.0, ImGuiWidgets.ValueSliderState.ValueAt(0.5, 0.0, 100.0, logarithmic: true, epsilon: 1.0), 1e-9);

		// Dragging off zero lands on the epsilon at the least, and dragging back below it returns to zero.
		double moved = ImGuiWidgets.ValueSliderState.Drag(0.0, 1f, 200f, 0.0, 100.0, 1f, logarithmic: true, epsilon: 1.0);
		Assert.IsTrue(moved is >= 1.0 and < 1.1, $"A one-pixel drag off zero reached {moved}.");
		Assert.AreEqual(0.0, ImGuiWidgets.ValueSliderState.Drag(moved, -2f, 200f, 0.0, 100.0, 1f, logarithmic: true, epsilon: 1.0));
	}

	[TestMethod]
	public void Logarithmic_ARangeBelowZeroMirrorsOneAboveIt()
	{
		Assert.AreEqual(0.5f, ImGuiWidgets.ValueSliderState.Fraction(-10.0, -100.0, -1.0, logarithmic: true), 1e-6f);
		Assert.AreEqual(-10.0, ImGuiWidgets.ValueSliderState.ValueAt(0.5, -100.0, -1.0, logarithmic: true), 1e-9);
	}

	[TestMethod]
	public void Logarithmic_ARangeCrossingZeroIsLinear()
	{
		Assert.IsFalse(ImGuiWidgets.ValueSliderState.IsLogarithmic(-10.0, 10.0, 0.1));
		Assert.AreEqual(0.75f, ImGuiWidgets.ValueSliderState.Fraction(5.0, -10.0, 10.0, logarithmic: true, epsilon: 0.1), 1e-6f);
	}

	[TestMethod]
	public void Logarithmic_TheFillFollowsTheScale()
	{
		(float start, float end) = ImGuiWidgets.ValueSliderState.FillSpan(10.0, 1.0, 100.0, ValueSliderFill.FromMinimum, logarithmic: true);

		Assert.AreEqual(0f, start);
		Assert.AreEqual(0.5f, end, 1e-6f);
	}

	[TestMethod]
	[DataRow("0", false, 1.0)]
	[DataRow("0.0", false, 0.1)]
	[DataRow("0.00", false, 0.01)]
	[DataRow("#,##0.###", false, 0.001)]
	[DataRow("F2", false, 0.01)]
	[DataRow("0.0", true, 1.0)]
	public void LogarithmicEpsilon_IsTheSmallestStepTheFormatShows(string format, bool isInteger, double expected) =>
		Assert.AreEqual(expected, ImGuiWidgets.ValueSliderState.LogarithmicEpsilon(format, isInteger), expected * 1e-9);

	[TestMethod]
	public void FillAvoidsText_WheneverAnEdgeOfTheFillFallsInsideTheNumber()
	{
		// A sliver from zero in the middle of the number.
		Assert.IsTrue(ImGuiWidgets.ValueSliderState.FillAvoidsText(100f, 103f, 90f, 110f));

		// A fill from zero ending inside the number, or one from the left ending there: either would split it.
		Assert.IsTrue(ImGuiWidgets.ValueSliderState.FillAvoidsText(100f, 120f, 90f, 110f));
		Assert.IsTrue(ImGuiWidgets.ValueSliderState.FillAvoidsText(0f, 105f, 90f, 110f));

		// A fill covering the whole number, or ending clear of it, leaves the number in one color.
		Assert.IsFalse(ImGuiWidgets.ValueSliderState.FillAvoidsText(0f, 120f, 90f, 110f));
		Assert.IsFalse(ImGuiWidgets.ValueSliderState.FillAvoidsText(90f, 110f, 90f, 110f));
		Assert.IsFalse(ImGuiWidgets.ValueSliderState.FillAvoidsText(0f, 80f, 90f, 110f));
		Assert.IsFalse(ImGuiWidgets.ValueSliderState.FillAvoidsText(115f, 200f, 90f, 110f));

		// No fill at all.
		Assert.IsFalse(ImGuiWidgets.ValueSliderState.FillAvoidsText(100f, 100f, 90f, 110f));
	}

	[TestMethod]
	[DataRow("25", 25.0)]
	[DataRow("Size 25", 25.0)]
	[DataRow("size 25 px", 25.0)]
	public void TryParse_IgnoresThePrefixWhenItIsTypedToo(string text, double expected)
	{
		Assert.IsTrue(ImGuiWidgets.ValueSliderState.TryParse(text, " px", Invariant, out double value, "Size "));
		Assert.AreEqual(expected, value);
	}
}
