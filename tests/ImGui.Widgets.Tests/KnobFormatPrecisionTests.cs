// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// The knob reads its drag precision from its format on every draw, so a format the parser cannot read
/// must fall back to the default rather than throw. Before issue #661 <c>"%.f"</c> threw a
/// <see cref="System.FormatException"/>, and an empty or truncated format indexed past the end of the span.
/// </summary>
[TestClass]
public sealed class KnobFormatPrecisionTests
{
	private const int DefaultPrecision = 3;

	[TestMethod]
	[DataRow("%.3f", 3)]
	[DataRow("%.0f", 0)]
	[DataRow("%.f", 0)]
	[DataRow("Gain %.f dB", 0)]
	[DataRow("%8.2f", 2)]
	[DataRow("%.3", 3)]
	[DataRow("%.", 0)]
	[DataRow("%e", -1)]
	[DataRow("%g", -1)]
	[DataRow("%f", DefaultPrecision)]
	[DataRow("", DefaultPrecision)]
	[DataRow("%", DefaultPrecision)]
	[DataRow("%5", DefaultPrecision)]
	[DataRow("%%", DefaultPrecision)]
	[DataRow("%.100f", DefaultPrecision)]
	[DataRow("%.99999999999f", DefaultPrecision)]
	public void ParseFormatPrecision_ReadsPrecisionWithoutThrowing(string format, int expected) =>
		Assert.AreEqual(expected, ImGuiWidgets.KnobImpl.ParseFormatPrecision(format, DefaultPrecision));
}
