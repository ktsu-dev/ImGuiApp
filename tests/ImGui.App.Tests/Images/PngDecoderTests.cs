// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests.Images;

using System;
using ktsu.ImGui.App.Images;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives the PNG decoder across colour types, bit depths, filters, transparency and interlacing.
/// </summary>
[TestClass]
public class PngDecoderTests
{
	[TestMethod]
	public void Decode_Rgba8_ReturnsExactPixels()
	{
		byte[] scanlines =
		[
			1, 2, 3, 4, 5, 6, 7, 8,
			9, 10, 11, 12, 13, 14, 15, 16,
		];

		ImagePixels image = ImageDecoder.Decode(TestImageBuilder.Png(2, 2, colorType: 6, bitDepth: 8, scanlines));

		Assert.AreEqual(2, image.Width);
		Assert.AreEqual(2, image.Height);
		CollectionAssert.AreEqual(scanlines, image.GetBuffer());
	}

	[TestMethod]
	public void Decode_Rgb8_FillsAlphaOpaque()
	{
		byte[] scanlines = [10, 20, 30, 40, 50, 60];

		ImagePixels image = ImageDecoder.Decode(TestImageBuilder.Png(2, 1, colorType: 2, bitDepth: 8, scanlines));

		Assert.AreEqual((10, 20, 30, (byte)255), image.GetPixel(0, 0));
		Assert.AreEqual((40, 50, 60, (byte)255), image.GetPixel(1, 0));
	}

	[TestMethod]
	public void Decode_Greyscale8_ReplicatesAcrossChannels()
	{
		ImagePixels image = ImageDecoder.Decode(TestImageBuilder.Png(3, 1, colorType: 0, bitDepth: 8, [0, 128, 255]));

		Assert.AreEqual((0, 0, 0, (byte)255), image.GetPixel(0, 0));
		Assert.AreEqual((128, 128, 128, (byte)255), image.GetPixel(1, 0));
		Assert.AreEqual((255, 255, 255, (byte)255), image.GetPixel(2, 0));
	}

	[TestMethod]
	public void Decode_GreyscaleAlpha8_KeepsAlpha()
	{
		ImagePixels image = ImageDecoder.Decode(TestImageBuilder.Png(2, 1, colorType: 4, bitDepth: 8, [90, 12, 200, 240]));

		Assert.AreEqual((90, 90, 90, (byte)12), image.GetPixel(0, 0));
		Assert.AreEqual((200, 200, 200, (byte)240), image.GetPixel(1, 0));
	}

	[TestMethod]
	public void Decode_Greyscale16_TruncatesToHighByte()
	{
		// Two 16-bit samples, big endian: 0x1234 and 0xABCD.
		ImagePixels image = ImageDecoder.Decode(TestImageBuilder.Png(2, 1, colorType: 0, bitDepth: 16, [0x12, 0x34, 0xAB, 0xCD]));

		Assert.AreEqual((0x12, 0x12, 0x12, (byte)255), image.GetPixel(0, 0));
		Assert.AreEqual((0xAB, 0xAB, 0xAB, (byte)255), image.GetPixel(1, 0));
	}

	[TestMethod]
	[DataRow((byte)1, 0b1010_0000, new int[] { 255, 0, 255, 0 })]
	[DataRow((byte)2, 0b00_01_10_11, new int[] { 0, 85, 170, 255 })]
	[DataRow((byte)4, 0b0000_1111, new int[] { 0, 255 })]
	public void Decode_SubByteGreyscale_ScalesToFullRange(byte bitDepth, int packed, int[] expected)
	{
		ArgumentNullException.ThrowIfNull(expected);

		ImagePixels image = ImageDecoder.Decode(TestImageBuilder.Png(expected.Length, 1, colorType: 0, bitDepth, [(byte)packed]));

		for (int x = 0; x < expected.Length; x++)
		{
			Assert.AreEqual((byte)expected[x], image.GetPixel(x, 0).R, $"pixel {x}");
		}
	}

	[TestMethod]
	public void Decode_Palette_ResolvesEntries()
	{
		byte[] palette = [255, 0, 0, 0, 255, 0, 0, 0, 255];

		ImagePixels image = ImageDecoder.Decode(TestImageBuilder.Png(3, 1, colorType: 3, bitDepth: 8, [2, 0, 1], palette));

		Assert.AreEqual((0, 0, 255, (byte)255), image.GetPixel(0, 0));
		Assert.AreEqual((255, 0, 0, (byte)255), image.GetPixel(1, 0));
		Assert.AreEqual((0, 255, 0, (byte)255), image.GetPixel(2, 0));
	}

	[TestMethod]
	public void Decode_PaletteWithShortTransparency_LeavesUncoveredEntriesOpaque()
	{
		byte[] palette = [1, 2, 3, 4, 5, 6, 7, 8, 9];

		// tRNS only covers the first two entries; the third must stay opaque.
		ImagePixels image = ImageDecoder.Decode(TestImageBuilder.Png(3, 1, colorType: 3, bitDepth: 8, [0, 1, 2], palette, transparency: [0, 128]));

		Assert.AreEqual(0, image.GetPixel(0, 0).A);
		Assert.AreEqual(128, image.GetPixel(1, 0).A);
		Assert.AreEqual(255, image.GetPixel(2, 0).A);
	}

	[TestMethod]
	public void Decode_GreyscaleColorKey_MakesMatchingSamplesTransparent()
	{
		// tRNS for colour type 0 is a two-byte sample value; here the key is 128.
		ImagePixels image = ImageDecoder.Decode(TestImageBuilder.Png(2, 1, colorType: 0, bitDepth: 8, [128, 129], transparency: [0, 128]));

		Assert.AreEqual(0, image.GetPixel(0, 0).A);
		Assert.AreEqual(255, image.GetPixel(1, 0).A);
	}

	[TestMethod]
	public void Decode_TruecolourColorKey_MakesMatchingPixelsTransparent()
	{
		byte[] scanlines = [10, 20, 30, 10, 20, 31];

		ImagePixels image = ImageDecoder.Decode(TestImageBuilder.Png(2, 1, colorType: 2, bitDepth: 8, scanlines, transparency: [0, 10, 0, 20, 0, 30]));

		Assert.AreEqual(0, image.GetPixel(0, 0).A);
		Assert.AreEqual(255, image.GetPixel(1, 0).A);
	}

	[TestMethod]
	[DataRow((byte)0)]
	[DataRow((byte)1)]
	[DataRow((byte)2)]
	[DataRow((byte)3)]
	[DataRow((byte)4)]
	public void Decode_EveryScanlineFilter_RoundTrips(byte filter)
	{
		byte[] scanlines = new byte[4 * 4 * 4];
		for (int i = 0; i < scanlines.Length; i++)
		{
			scanlines[i] = (byte)(i * 7 % 251);
		}

		ImagePixels image = ImageDecoder.Decode(TestImageBuilder.Png(4, 4, colorType: 6, bitDepth: 8, scanlines, filter: filter));

		CollectionAssert.AreEqual(scanlines, image.GetBuffer(), $"filter {filter}");
	}

	[TestMethod]
	public void Decode_Adam7Interlaced_ReconstructsEveryPass()
	{
		// A 9x7 image touches all seven Adam7 passes, including the ones a smaller image would skip.
		ImagePixels image = ImageDecoder.Decode(Convert.FromBase64String(InterlacedNineBySeven));

		Assert.AreEqual(9, image.Width);
		Assert.AreEqual(7, image.Height);

		for (int y = 0; y < 7; y++)
		{
			for (int x = 0; x < 9; x++)
			{
				(byte r, byte g, byte b, byte a) = image.GetPixel(x, y);
				Assert.AreEqual((byte)(x * 28), r, $"red at {x},{y}");
				Assert.AreEqual((byte)(y * 36), g, $"green at {x},{y}");
				Assert.AreEqual((byte)(x * y * 5 % 256), b, $"blue at {x},{y}");
				Assert.AreEqual((byte)(255 - (x * 10)), a, $"alpha at {x},{y}");
			}
		}
	}

	[TestMethod]
	public void Decode_TruncatedImageData_Throws()
	{
		byte[] png = TestImageBuilder.Png(4, 4, colorType: 6, bitDepth: 8, new byte[4 * 4 * 4]);

		InvalidImageDataException exception = Assert.ThrowsExactly<InvalidImageDataException>(
			() => ImageDecoder.Decode(png.AsSpan(0, png.Length - 20).ToArray()));

		Assert.IsTrue(exception.Message.Contains("PNG", StringComparison.Ordinal), exception.Message);
	}

	[TestMethod]
	public void Decode_UnsupportedBitDepthForColourType_Throws() =>
		Assert.ThrowsExactly<InvalidImageDataException>(
			() => ImageDecoder.Decode(TestImageBuilder.Png(2, 1, colorType: 2, bitDepth: 4, [0, 0, 0])));

	[TestMethod]
	public void Decode_PaletteIndexPastEndOfPalette_Throws() =>
		Assert.ThrowsExactly<InvalidImageDataException>(
			() => ImageDecoder.Decode(TestImageBuilder.Png(1, 1, colorType: 3, bitDepth: 8, [5], palette: [1, 2, 3])));

	/// <summary>A 9x7 Adam7-interlaced RGBA PNG whose pixels follow the formula asserted above.</summary>
	private const string InterlacedNineBySeven =
		"iVBORw0KGgoAAAANSUhEUgAAAAkAAAAHCAYAAADam2dgAAAANUlEQVR4nGNkYGD4L8PA8A0fZmFQ" +
		"YWBgYGDFi5EUceHEaIr4sWIsikQwMA5FkigYjyI5OAYAPAwNGgsi7I4AAAAASUVORK5CYII=";
}
