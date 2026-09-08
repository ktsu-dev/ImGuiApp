// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests.Images;

using ktsu.ImGui.App.Images;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives the TGA decoder over its image types, pixel depths and origin flags.
/// </summary>
[TestClass]
public class TgaDecoderTests
{
	private const byte TopLeftOrigin = 0x20;
	private const byte TopLeftWithAlpha = 0x28;

	[TestMethod]
	public void Decode_TwentyFourBitTrueColour_ReadsBgrAsRgb()
	{
		byte[] body = [3, 2, 1, 30, 20, 10];

		ImagePixels image = ImageDecoder.Decode(TestImageBuilder.Tga(2, 1, imageType: 2, pixelBits: 24, body, descriptor: TopLeftOrigin));

		Assert.AreEqual((1, 2, 3, (byte)255), image.GetPixel(0, 0));
		Assert.AreEqual((10, 20, 30, (byte)255), image.GetPixel(1, 0));
	}

	[TestMethod]
	public void Decode_BottomLeftOrigin_FlipsRows()
	{
		byte[] body = [1, 1, 1, 2, 2, 2];

		ImagePixels image = ImageDecoder.Decode(TestImageBuilder.Tga(1, 2, imageType: 2, pixelBits: 24, body));

		// With no origin flag the first stored row is the bottom of the image.
		Assert.AreEqual(2, image.GetPixel(0, 0).R);
		Assert.AreEqual(1, image.GetPixel(0, 1).R);
	}

	[TestMethod]
	public void Decode_ThirtyTwoBit_KeepsAlpha()
	{
		byte[] body = [3, 2, 1, 64, 30, 20, 10, 255];

		ImagePixels image = ImageDecoder.Decode(TestImageBuilder.Tga(2, 1, imageType: 2, pixelBits: 32, body, descriptor: TopLeftWithAlpha));

		Assert.AreEqual((1, 2, 3, (byte)64), image.GetPixel(0, 0));
		Assert.AreEqual((10, 20, 30, (byte)255), image.GetPixel(1, 0));
	}

	[TestMethod]
	public void Decode_SixteenBitWithoutAttributeBits_StaysOpaque()
	{
		// 0x7C1F is red 31, green 0, blue 31 with the attribute bit clear. Without attribute bits in
		// the descriptor that bit is padding, not a transparency flag.
		byte[] body = [0x1F, 0x7C];

		ImagePixels image = ImageDecoder.Decode(TestImageBuilder.Tga(1, 1, imageType: 2, pixelBits: 16, body, descriptor: TopLeftOrigin));

		Assert.AreEqual((255, 0, 255, (byte)255), image.GetPixel(0, 0));
	}

	[TestMethod]
	public void Decode_RunLengthEncoded_ExpandsBothPacketKinds()
	{
		// A repeat packet of three red pixels, then a literal packet of two distinct pixels.
		byte[] body =
		[
			0x82, 3, 2, 1,
			0x01, 30, 20, 10, 60, 50, 40,
		];

		ImagePixels image = ImageDecoder.Decode(TestImageBuilder.Tga(5, 1, imageType: 10, pixelBits: 24, body, descriptor: TopLeftOrigin));

		Assert.AreEqual((1, 2, 3, (byte)255), image.GetPixel(0, 0));
		Assert.AreEqual((1, 2, 3, (byte)255), image.GetPixel(1, 0));
		Assert.AreEqual((1, 2, 3, (byte)255), image.GetPixel(2, 0));
		Assert.AreEqual((10, 20, 30, (byte)255), image.GetPixel(3, 0));
		Assert.AreEqual((40, 50, 60, (byte)255), image.GetPixel(4, 0));
	}

	[TestMethod]
	public void Decode_Greyscale_ReplicatesAcrossChannels()
	{
		byte[] body = [0, 128, 255];

		ImagePixels image = ImageDecoder.Decode(TestImageBuilder.Tga(3, 1, imageType: 3, pixelBits: 8, body, descriptor: TopLeftOrigin));

		Assert.AreEqual((128, 128, 128, (byte)255), image.GetPixel(1, 0));
	}

	[TestMethod]
	public void Decode_ColourMapped_ResolvesEntries()
	{
		// Two 24-bit colour map entries followed by three indices.
		byte[] body = [3, 2, 1, 30, 20, 10, 1, 0, 1];

		ImagePixels image = ImageDecoder.Decode(TestImageBuilder.Tga(3, 1, imageType: 1, pixelBits: 8, body, colorMapLength: 2, colorMapEntryBits: 24, descriptor: TopLeftOrigin));

		Assert.AreEqual((10, 20, 30, (byte)255), image.GetPixel(0, 0));
		Assert.AreEqual((1, 2, 3, (byte)255), image.GetPixel(1, 0));
		Assert.AreEqual((10, 20, 30, (byte)255), image.GetPixel(2, 0));
	}

	[TestMethod]
	public void Decode_TruncatedPixelData_Throws() =>
		Assert.ThrowsExactly<InvalidImageDataException>(
			() => ImageDecoder.Decode(TestImageBuilder.Tga(4, 4, imageType: 2, pixelBits: 24, [1, 2, 3], descriptor: TopLeftOrigin)));
}
