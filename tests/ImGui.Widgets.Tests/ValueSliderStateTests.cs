// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

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
}
