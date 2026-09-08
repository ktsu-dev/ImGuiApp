// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests.Images;

using ktsu.ImGui.App.Images;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives the BMP decoder over bit counts, row order and palette handling.
/// </summary>
[TestClass]
public class BmpDecoderTests
{
	[TestMethod]
	public void Decode_TwentyFourBit_ReadsBgrAsRgb()
	{
		// Rows are given top-down and the builder flips them into the file's bottom-up order.
		byte[][] rows =
		[
			[3, 2, 1, 30, 20, 10],
			[13, 12, 11, 130, 120, 110],
		];

		ImagePixels image = ImageDecoder.Decode(TestImageBuilder.Bmp(2, 2, 24, rows));

		Assert.AreEqual((1, 2, 3, (byte)255), image.GetPixel(0, 0));
		Assert.AreEqual((10, 20, 30, (byte)255), image.GetPixel(1, 0));
		Assert.AreEqual((11, 12, 13, (byte)255), image.GetPixel(0, 1));
		Assert.AreEqual((110, 120, 130, (byte)255), image.GetPixel(1, 1));
	}

	[TestMethod]
	public void Decode_TopDownRows_DoesNotFlipTheImage()
	{
		byte[][] rows =
		[
			[3, 2, 1, 30, 20, 10],
			[13, 12, 11, 130, 120, 110],
		];

		ImagePixels image = ImageDecoder.Decode(TestImageBuilder.Bmp(2, 2, 24, rows, topDown: true));

		Assert.AreEqual((1, 2, 3, (byte)255), image.GetPixel(0, 0));
		Assert.AreEqual((11, 12, 13, (byte)255), image.GetPixel(0, 1));
	}

	[TestMethod]
	public void Decode_ThirtyTwoBitWithAlpha_KeepsAlpha()
	{
		byte[][] rows = [[3, 2, 1, 128, 30, 20, 10, 255]];

		ImagePixels image = ImageDecoder.Decode(TestImageBuilder.Bmp(2, 1, 32, rows));

		Assert.AreEqual((1, 2, 3, (byte)128), image.GetPixel(0, 0));
		Assert.AreEqual((10, 20, 30, (byte)255), image.GetPixel(1, 0));
	}

	[TestMethod]
	public void Decode_ThirtyTwoBitWithEmptyAlphaChannel_TreatsImageAsOpaque()
	{
		// BI_RGB leaves the fourth byte undefined and many encoders write zero; taking that at face
		// value would make the whole image invisible.
		byte[][] rows = [[3, 2, 1, 0, 30, 20, 10, 0]];

		ImagePixels image = ImageDecoder.Decode(TestImageBuilder.Bmp(2, 1, 32, rows));

		Assert.AreEqual((1, 2, 3, (byte)255), image.GetPixel(0, 0));
		Assert.AreEqual((10, 20, 30, (byte)255), image.GetPixel(1, 0));
	}

	[TestMethod]
	public void Decode_EightBitPalette_ResolvesEntries()
	{
		byte[] palette = [3, 2, 1, 0, 30, 20, 10, 0];
		byte[][] rows = [[1, 0, 1]];

		ImagePixels image = ImageDecoder.Decode(TestImageBuilder.Bmp(3, 1, 8, rows, palette));

		Assert.AreEqual((10, 20, 30, (byte)255), image.GetPixel(0, 0));
		Assert.AreEqual((1, 2, 3, (byte)255), image.GetPixel(1, 0));
		Assert.AreEqual((10, 20, 30, (byte)255), image.GetPixel(2, 0));
	}

	[TestMethod]
	public void Decode_OneBitPalette_UnpacksBits()
	{
		byte[] palette = [0, 0, 0, 0, 255, 255, 255, 0];
		byte[][] rows = [[0b1010_0000]];

		ImagePixels image = ImageDecoder.Decode(TestImageBuilder.Bmp(4, 1, 1, rows, palette));

		Assert.AreEqual(255, image.GetPixel(0, 0).R);
		Assert.AreEqual(0, image.GetPixel(1, 0).R);
		Assert.AreEqual(255, image.GetPixel(2, 0).R);
		Assert.AreEqual(0, image.GetPixel(3, 0).R);
	}

	[TestMethod]
	public void Decode_RowsArePaddedToFourByteStride()
	{
		// Three 24-bit pixels are nine bytes, which the format pads to twelve.
		byte[][] rows =
		[
			[1, 1, 1, 2, 2, 2, 3, 3, 3, 0, 0, 0],
			[4, 4, 4, 5, 5, 5, 6, 6, 6, 0, 0, 0],
		];

		ImagePixels image = ImageDecoder.Decode(TestImageBuilder.Bmp(3, 2, 24, rows));

		Assert.AreEqual(3, image.GetPixel(2, 0).R);
		Assert.AreEqual(4, image.GetPixel(0, 1).R);
	}

	[TestMethod]
	public void Decode_NotABmp_Throws() =>
		Assert.ThrowsExactly<InvalidImageDataException>(() => ImageDecoder.Decode([0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]));
}
