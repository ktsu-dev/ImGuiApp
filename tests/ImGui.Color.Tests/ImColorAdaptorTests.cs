// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Color.Tests;

using Hexa.NET.ImGui;

[TestClass]
public class ImColorAdaptorTests
{
	[TestMethod]
	public void FromHex_ParsesRgbAndAlpha()
	{
		ImColor c = ImColors.FromHex("#ff8000");
		Assert.AreEqual(1.0f, c.Value.X, 1e-6f);
		Assert.AreEqual(128f / 255f, c.Value.Y, 1e-6f);
		Assert.AreEqual(0.0f, c.Value.Z, 1e-6f);
		Assert.AreEqual(1.0f, c.Value.W, 1e-6f);

		Assert.AreEqual(0.5f, ImColors.FromHex("#00000080").Value.W, 2f / 255f);
	}

	[TestMethod]
	public void FromHex_ExpandsShorthand()
	{
		ImColor full = ImColors.FromHex("#f00");
		Assert.AreEqual(1.0f, full.Value.X, 1e-6f);
		Assert.AreEqual(0.0f, full.Value.Y, 1e-6f);
	}

	[TestMethod]
	public void FromRgb_NormalizesBytes()
	{
		ImColor c = ImColors.FromRgb(255, 0, 128);
		Assert.AreEqual(1.0f, c.Value.X, 1e-6f);
		Assert.AreEqual(128f / 255f, c.Value.Z, 1e-6f);
		Assert.AreEqual(1.0f, c.Value.W, 1e-6f);
	}

	[TestMethod]
	public void FromHsl_RedAtZeroDegrees()
	{
		ImColor c = ImColors.FromHsl(0f, 1f, 0.5f);
		Assert.AreEqual(1.0f, c.Value.X, 1e-4f);
		Assert.AreEqual(0.0f, c.Value.Y, 1e-4f);
		Assert.AreEqual(0.0f, c.Value.Z, 1e-4f);
	}

	[TestMethod]
	public void LightenAndDarken_MoveTowardWhiteAndBlack()
	{
		ImColor mid = ImColors.FromRgb(0.4f, 0.4f, 0.4f);
		Assert.IsTrue(mid.LightenBy(0.2f).Value.X > mid.Value.X);
		Assert.IsTrue(mid.DarkenBy(0.2f).Value.X < mid.Value.X);
	}

	[TestMethod]
	public void Invert_IsPerChannelComplementAndPreservesAlpha()
	{
		ImColor c = ImColors.FromRgba(0.8f, 0.2f, 0.1f, 0.5f);
		ImColor inv = c.Invert();
		Assert.AreEqual(0.2f, inv.Value.X, 1e-5f);
		Assert.AreEqual(0.8f, inv.Value.Y, 1e-5f);
		Assert.AreEqual(0.9f, inv.Value.Z, 1e-5f);
		Assert.AreEqual(0.5f, inv.Value.W, 1e-6f);
	}

	[TestMethod]
	public void WithAlpha_SetsAlphaOnly()
	{
		ImColor c = ImColors.FromRgba(0.3f, 0.6f, 0.9f, 1f).WithAlpha(0.25f);
		Assert.AreEqual(0.3f, c.Value.X, 1e-6f);
		Assert.AreEqual(0.25f, c.Value.W, 1e-6f);
	}

	[TestMethod]
	public void ToGrayscale_EqualizesChannels()
	{
		ImColor g = ImColors.FromRgb(0.8f, 0.2f, 0.1f).ToGrayscale();
		Assert.AreEqual(g.Value.X, g.Value.Y, 1e-5f);
		Assert.AreEqual(g.Value.Y, g.Value.Z, 1e-5f);
	}

	[TestMethod]
	public void ContrastRatio_BlackOverWhiteIsTwentyOne()
	{
		ImColor black = ImColors.FromRgba(0f, 0f, 0f, 1f);
		ImColor white = ImColors.FromRgba(1f, 1f, 1f, 1f);
		Assert.AreEqual(21f, black.GetContrastRatioOver(white), 0.05f);
		Assert.AreEqual(1f, white.GetRelativeLuminance(), 1e-4f);
		Assert.AreEqual(0f, black.GetRelativeLuminance(), 1e-4f);
	}

	[TestMethod]
	public void MostReadableTextColor_PicksDarkOnLightAndLightOnDark()
	{
		ImColor onWhite = ImColors.FromRgba(1f, 1f, 1f, 1f).MostReadableTextColor();
		ImColor onBlack = ImColors.FromRgba(0f, 0f, 0f, 1f).MostReadableTextColor();
		Assert.IsTrue(onWhite.GetRelativeLuminance() < 0.5f, "text on white should be dark");
		Assert.IsTrue(onBlack.GetRelativeLuminance() > 0.5f, "text on black should be light");
	}

	[TestMethod]
	public void ColorDistance_ZeroForIdenticalGrowsWithDifference()
	{
		ImColor red = ImColors.FromRgb(1f, 0f, 0f);
		ImColor red2 = ImColors.FromRgb(1f, 0f, 0f);
		ImColor blue = ImColors.FromRgb(0f, 0f, 1f);
		Assert.AreEqual(0f, red.GetColorDistance(red2), 1e-5f);
		Assert.IsTrue(red.GetColorDistance(blue) > 0.1f);
	}
}
