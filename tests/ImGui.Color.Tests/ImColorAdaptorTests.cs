// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Color.Tests;

using Hexa.NET.ImGui;

using ktsu.Semantics.Color;

[TestClass]
public class ImColorAdaptorTests
{
	[TestMethod]
	public void FromHex_ParsesRgbAndAlpha()
	{
		ImColor c = Color.FromHex("#ff8000").ToImColor();
		Assert.AreEqual(1.0f, c.Value.X, 1e-6f);
		Assert.AreEqual(128f / 255f, c.Value.Y, 1e-6f);
		Assert.AreEqual(0.0f, c.Value.Z, 1e-6f);
		Assert.AreEqual(1.0f, c.Value.W, 1e-6f);

		Assert.AreEqual(0.5f, Color.FromHex("#00000080").ToImColor().Value.W, 2f / 255f);
	}

	[TestMethod]
	public void FromHex_ExpandsShorthand()
	{
		ImColor full = Color.FromHex("#f00").ToImColor();
		Assert.AreEqual(1.0f, full.Value.X, 1e-6f);
		Assert.AreEqual(0.0f, full.Value.Y, 1e-6f);
	}

	[TestMethod]
	public void FromRgb_NormalizesBytes()
	{
		ImColor c = Color.FromBytes(255, 0, 128).ToImColor();
		Assert.AreEqual(1.0f, c.Value.X, 1e-6f);
		Assert.AreEqual(128f / 255f, c.Value.Z, 1e-6f);
		Assert.AreEqual(1.0f, c.Value.W, 1e-6f);
	}

	[TestMethod]
	public void FromHsl_RedAtZeroDegrees()
	{
		ImColor c = new Hsl(0f, 1f, 0.5f).ToSrgb().ToImColor();
		Assert.AreEqual(1.0f, c.Value.X, 1e-4f);
		Assert.AreEqual(0.0f, c.Value.Y, 1e-4f);
		Assert.AreEqual(0.0f, c.Value.Z, 1e-4f);
	}

	[TestMethod]
	public void LightenAndDarken_MoveTowardWhiteAndBlack()
	{
		ImColor mid = new Srgb(0.4f, 0.4f, 0.4f).ToImColor();
		Assert.IsTrue(mid.LightenBy(0.2f).Value.X > mid.Value.X);
		Assert.IsTrue(mid.DarkenBy(0.2f).Value.X < mid.Value.X);
	}

	[TestMethod]
	public void Invert_IsPerChannelComplementAndPreservesAlpha()
	{
		ImColor c = new Srgb(0.8f, 0.2f, 0.1f).ToImColor(0.5f);
		ImColor inv = c.Invert();
		Assert.AreEqual(0.2f, inv.Value.X, 1e-5f);
		Assert.AreEqual(0.8f, inv.Value.Y, 1e-5f);
		Assert.AreEqual(0.9f, inv.Value.Z, 1e-5f);
		Assert.AreEqual(0.5f, inv.Value.W, 1e-6f);
	}

	[TestMethod]
	public void WithAlpha_SetsAlphaOnly()
	{
		ImColor c = new Srgb(0.3f, 0.6f, 0.9f).ToImColor(1f).WithAlpha(0.25f);
		Assert.AreEqual(0.3f, c.Value.X, 1e-6f);
		Assert.AreEqual(0.25f, c.Value.W, 1e-6f);
	}

	[TestMethod]
	public void ToGrayscale_EqualizesChannels()
	{
		ImColor g = new Srgb(0.8f, 0.2f, 0.1f).ToImColor().ToGrayscale();
		Assert.AreEqual(g.Value.X, g.Value.Y, 1e-5f);
		Assert.AreEqual(g.Value.Y, g.Value.Z, 1e-5f);
	}

	[TestMethod]
	public void ContrastRatio_BlackOverWhiteIsTwentyOne()
	{
		ImColor black = new Srgb(0f, 0f, 0f).ToImColor(1f);
		ImColor white = new Srgb(1f, 1f, 1f).ToImColor(1f);
		Assert.AreEqual(21f, black.GetContrastRatioOver(white), 0.05f);
		Assert.AreEqual(1f, white.GetRelativeLuminance(), 1e-4f);
		Assert.AreEqual(0f, black.GetRelativeLuminance(), 1e-4f);
	}

	[TestMethod]
	public void MostReadableTextColor_PicksDarkOnLightAndLightOnDark()
	{
		ImColor onWhite = new Srgb(1f, 1f, 1f).ToImColor(1f).MostReadableTextColor();
		ImColor onBlack = new Srgb(0f, 0f, 0f).ToImColor(1f).MostReadableTextColor();
		Assert.IsTrue(onWhite.GetRelativeLuminance() < 0.5f, "text on white should be dark");
		Assert.IsTrue(onBlack.GetRelativeLuminance() > 0.5f, "text on black should be light");
	}

	[TestMethod]
	public void ColorDistance_ZeroForIdenticalGrowsWithDifference()
	{
		ImColor red = new Srgb(1f, 0f, 0f).ToImColor();
		ImColor red2 = new Srgb(1f, 0f, 0f).ToImColor();
		ImColor blue = new Srgb(0f, 0f, 1f).ToImColor();
		Assert.AreEqual(0f, red.GetColorDistance(red2), 1e-5f);
		Assert.IsTrue(red.GetColorDistance(blue) > 0.1f);
	}
}
