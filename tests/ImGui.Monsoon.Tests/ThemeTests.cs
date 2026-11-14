// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Monsoon.Tests;

using System.Numerics;

using Hexa.NET.ImGui;

[TestClass]
public sealed class ThemeTests
{
	[TestMethod]
	public void ThemeSetsEveryColor()
	{
		IReadOnlyDictionary<ImGuiCol, Vector4> colors = MonsoonTheme.Colors();
		foreach (ImGuiCol col in Enum.GetValues<ImGuiCol>().Where(c => c != ImGuiCol.Count))
		{
			Assert.IsTrue(colors.ContainsKey(col), $"{col} is not set by the theme.");
		}
	}

	[TestMethod]
	public void TextReadsAgainstTheBackground()
	{
		IReadOnlyDictionary<ImGuiCol, Vector4> colors = MonsoonTheme.Colors();
		Assert.IsGreaterThanOrEqualTo(7.0, Contrast(colors[ImGuiCol.Text], colors[ImGuiCol.WindowBg]));
		Assert.IsGreaterThanOrEqualTo(3.0, Contrast(MonsoonPalette.TextDim, MonsoonPalette.Background));
		foreach (Vector4 neon in new[] { MonsoonPalette.Cyan, MonsoonPalette.Magenta, MonsoonPalette.Amber })
		{
			Assert.IsGreaterThanOrEqualTo(4.5, Contrast(neon, MonsoonPalette.Background));
		}
	}

	[TestMethod]
	public void FromHex_ReadsSixAndEightDigits()
	{
		Assert.AreEqual(new Vector4(1f, 0f, 0f, 1f), MonsoonPalette.FromHex("#FF0000"));
		Assert.AreEqual(new Vector4(0f, 0f, 1f, 0f), MonsoonPalette.FromHex("0000FF00"));
	}

	[TestMethod]
	[DataRow("")]
	[DataRow("#FFF")]
	[DataRow("#GGGGGG")]
	public void FromHex_RejectsMalformedText(string hex) =>
		Assert.ThrowsExactly<FormatException>(() => MonsoonPalette.FromHex(hex));

	private static double Contrast(Vector4 a, Vector4 b)
	{
		double la = Luminance(a);
		double lb = Luminance(b);
		return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
	}

	private static double Luminance(Vector4 c) => (0.2126 * Linear(c.X)) + (0.7152 * Linear(c.Y)) + (0.0722 * Linear(c.Z));

	private static double Linear(float v) => v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
}
