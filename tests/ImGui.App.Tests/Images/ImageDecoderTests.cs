// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests.Images;

using System;
using System.IO;
using ktsu.ImGui.App.Images;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers format detection and the failure modes of the decoder front door.
/// </summary>
[TestClass]
public class ImageDecoderTests
{
	[TestMethod]
	public void Identify_Png_ReturnsPng() =>
		Assert.AreEqual(ImageFormat.Png, ImageDecoder.Identify(TestImageBuilder.Png(1, 1, colorType: 6, bitDepth: 8, [1, 2, 3, 4])));

	[TestMethod]
	public void Identify_Bmp_ReturnsBmp() =>
		Assert.AreEqual(ImageFormat.Bmp, ImageDecoder.Identify(TestImageBuilder.Bmp(1, 1, 24, [[1, 2, 3, 0]])));

	[TestMethod]
	public void Identify_Tga_ReturnsTga() =>
		Assert.AreEqual(ImageFormat.Tga, ImageDecoder.Identify(TestImageBuilder.Tga(1, 1, imageType: 2, pixelBits: 24, [1, 2, 3])));

	[TestMethod]
	public void Identify_Jpeg_ReturnsJpeg() =>
		Assert.AreEqual(ImageFormat.Jpeg, ImageDecoder.Identify([0xFF, 0xD8, 0xFF, 0xE0]));

	[TestMethod]
	public void Identify_Garbage_ReturnsUnknown() =>
		Assert.AreEqual(ImageFormat.Unknown, ImageDecoder.Identify("version ht"u8));

	[TestMethod]
	public void Identify_EmptyBuffer_ReturnsUnknown() =>
		Assert.AreEqual(ImageFormat.Unknown, ImageDecoder.Identify([]));

	[TestMethod]
	public void Decode_UnrecognisedFormat_ReportsTheLeadingBytes()
	{
		InvalidImageDataException exception = Assert.ThrowsExactly<InvalidImageDataException>(
			() => ImageDecoder.Decode("version ht"u8));

		Assert.IsTrue(exception.Message.Contains("76 65 72 73", StringComparison.Ordinal), exception.Message);
	}

	[TestMethod]
	public void Decode_EmptyBuffer_Throws() =>
		Assert.ThrowsExactly<InvalidImageDataException>(() => ImageDecoder.Decode([]));

	[TestMethod]
	public void Decode_IgnoresTheFileExtension()
	{
		// A PNG named as a JPEG still decodes: the format comes from the bytes, not the name.
		string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.jpg");
		File.WriteAllBytes(path, TestImageBuilder.Png(1, 1, colorType: 6, bitDepth: 8, [9, 8, 7, 6]));

		try
		{
			ImagePixels image = ImageDecoder.Load(path);
			Assert.AreEqual((9, 8, 7, (byte)6), image.GetPixel(0, 0));
		}
		finally
		{
			File.Delete(path);
		}
	}

	[TestMethod]
	public void Load_Stream_ReadsToTheEnd()
	{
		using MemoryStream stream = new(TestImageBuilder.Png(2, 1, colorType: 2, bitDepth: 8, [1, 2, 3, 4, 5, 6]));

		ImagePixels image = ImageDecoder.Load(stream);

		Assert.AreEqual(2, image.Width);
		Assert.AreEqual((4, 5, 6, (byte)255), image.GetPixel(1, 0));
	}

	[TestMethod]
	public void Load_NullPath_Throws() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => ImageDecoder.Load((string)null!));

	[TestMethod]
	public void Load_NullStream_Throws() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => ImageDecoder.Load((Stream)null!));
}
