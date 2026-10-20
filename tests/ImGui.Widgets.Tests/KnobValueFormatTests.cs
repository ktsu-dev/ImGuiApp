// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Hexa.NET.ImGui;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// The knob's value tooltip formats the value with the knob's printf format, as its input does. Before issue
/// #667 the tooltip passed that format to <see cref="string.Format(System.IFormatProvider, string, object)"/>,
/// which left it unchanged, and the int default format was <c>"%if"</c>, which shows a stray <c>f</c>.
/// </summary>
[TestClass]
public sealed class KnobValueFormatTests
{
	[TestMethod]
	[DataRow(0.5f, "%.3f", "0.500")]
	[DataRow(0.5f, "%.1f dB", "0.5 dB")]
	[DataRow(0.5f, "%.0f%%", "0%")]
	public void FormatValue_FormatsAFloatAsImGuiDoes(float value, string format, string expected) =>
		Assert.AreEqual(expected, ImGuiWidgets.KnobImpl.FormatValue(ImGuiDataType.Float, value, format));

	[TestMethod]
	public void FormatValue_FormatsAnInt() =>
		Assert.AreEqual("5 steps", ImGuiWidgets.KnobImpl.FormatValue(ImGuiDataType.S32, 5, "%d steps"));

	[TestMethod]
	public void DefaultFloatFormat_ShowsThreeDecimals() =>
		Assert.AreEqual("0.500", ImGuiWidgets.KnobImpl.FormatValue(ImGuiDataType.Float, 0.5f, ImGuiWidgets.KnobImpl.DefaultFloatFormat));

	[TestMethod]
	public void DefaultIntFormat_ShowsTheValueAlone() =>
		Assert.AreEqual("5", ImGuiWidgets.KnobImpl.FormatValue(ImGuiDataType.S32, 5, ImGuiWidgets.KnobImpl.DefaultIntFormat));
}
