// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class TextureAndBlendingTests
{
	private static TextureSource TwoByTwo()
	{
		Bitmap32 pixels = new(2, 2);
		pixels.SetPixel(0, 0, new Rgba32(255, 0, 0, 255));
		pixels.SetPixel(1, 0, new Rgba32(0, 255, 0, 255));
		pixels.SetPixel(0, 1, new Rgba32(0, 0, 255, 255));
		pixels.SetPixel(1, 1, new Rgba32(255, 255, 0, 255));
		return new TextureSource(pixels);
	}

	[TestMethod]
	public void Sample_TopLeft_ReturnsFirstTexel() =>
		Assert.AreEqual(new Rgba32(255, 0, 0, 255), TwoByTwo().Sample(0.1f, 0.1f));

	[TestMethod]
	public void Sample_BottomRight_ReturnsLastTexel() =>
		Assert.AreEqual(new Rgba32(255, 255, 0, 255), TwoByTwo().Sample(0.9f, 0.9f));

	[TestMethod]
	public void Sample_BeyondRange_ClampsRatherThanWrapping() =>
		Assert.AreEqual(new Rgba32(255, 255, 0, 255), TwoByTwo().Sample(5f, 5f));

	[TestMethod]
	public void Sample_NegativeCoordinates_ClampToFirstTexel() =>
		Assert.AreEqual(new Rgba32(255, 0, 0, 255), TwoByTwo().Sample(-3f, -3f));

	[TestMethod]
	public void BlendOver_OpaqueSource_ReplacesDestination() =>
		Assert.AreEqual(
			new Rgba32(255, 0, 0, 255),
			SoftwareRasterizer.BlendOver(new Rgba32(255, 0, 0, 255), new Rgba32(0, 0, 255, 255)));

	[TestMethod]
	public void BlendOver_TransparentSource_LeavesDestination() =>
		Assert.AreEqual(
			new Rgba32(0, 0, 255, 255),
			SoftwareRasterizer.BlendOver(new Rgba32(255, 0, 0, 0), new Rgba32(0, 0, 255, 255)));

	[TestMethod]
	public void BlendOver_HalfAlpha_MixesTowardTheSource()
	{
		Rgba32 result = SoftwareRasterizer.BlendOver(new Rgba32(255, 255, 255, 128), new Rgba32(0, 0, 0, 255));

		Assert.IsTrue(result.R is >= 126 and <= 130, $"Expected roughly half way between black and white, got {result.R}.");
		Assert.AreEqual(255, result.A, "Blending onto an opaque destination stays opaque.");
	}

	[TestMethod]
	public void BlendOver_OntoTransparent_KeepsSourceColor()
	{
		// Compositing onto nothing must not darken the source toward the transparent pixel's
		// color, which is the classic premultiplied-versus-straight alpha mistake.
		Rgba32 result = SoftwareRasterizer.BlendOver(new Rgba32(255, 120, 0, 128), new Rgba32(0, 0, 0, 0));

		Assert.AreEqual(255, result.R, "Red should survive compositing over a fully transparent pixel.");
		Assert.AreEqual(120, result.G, "Green should survive compositing over a fully transparent pixel.");
		Assert.AreEqual(128, result.A);
	}

	[TestMethod]
	public void Modulate_WhiteTexel_LeavesVertexColorUnchanged() =>
		Assert.AreEqual(
			new Rgba32(10, 20, 30, 40),
			SoftwareRasterizer.Modulate(new Rgba32(10, 20, 30, 40), new Rgba32(255, 255, 255, 255)));

	[TestMethod]
	public void Constructor_NullPixels_Throws() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => new TextureSource(null!));
}
