// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Imaging.Tests;

using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// The encoder is checked against the decoder beside it, and against the header bytes a PNG reader
/// sees, so a change to either side that loses a pixel or a format promise fails here.
/// </summary>
[TestClass]
public class PngEncoderTests
{
	private static ImagePixels Corners()
	{
		ImagePixels image = new(2, 2);
		image.SetPixel(0, 0, 255, 0, 0, 255);
		image.SetPixel(1, 0, 0, 255, 0, 128);
		image.SetPixel(0, 1, 0, 0, 255, 0);
		image.SetPixel(1, 1, 10, 20, 30, 40);
		return image;
	}

	[TestMethod]
	public void Encode_DecodesToTheSamePixels()
	{
		ImagePixels source = Corners();

		ImagePixels decoded = ImageDecoder.Decode(PngEncoder.Encode(source));

		Assert.AreEqual(source.Width, decoded.Width);
		Assert.AreEqual(source.Height, decoded.Height);
		CollectionAssert.AreEqual(source.GetBuffer(), decoded.GetBuffer());
	}

	[TestMethod]
	public void Encode_OfAWideImage_DecodesToTheSamePixels()
	{
		ImagePixels source = new(37, 5);
		Random random = new(7);
		random.NextBytes(source.Pixels);

		ImagePixels decoded = ImageDecoder.Decode(PngEncoder.Encode(source));

		CollectionAssert.AreEqual(source.GetBuffer(), decoded.GetBuffer());
	}

	[TestMethod]
	public void Encode_WritesEightBitRgbaWithAlpha()
	{
		byte[] png = PngEncoder.Encode(new ImagePixels(3, 7));

		Assert.AreEqual(ImageFormat.Png, ImageDecoder.Identify(png));
		Assert.AreEqual(3, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16)));
		Assert.AreEqual(7, BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20)));
		Assert.AreEqual(8, png[24], "bit depth");
		Assert.AreEqual(6, png[25], "colour type: truecolour with alpha");
	}

	[TestMethod]
	public void Encode_ByDefault_KeepsTheColourOfTransparentPixels()
	{
		ImagePixels decoded = ImageDecoder.Decode(PngEncoder.Encode(Corners()));

		Assert.AreEqual(((byte)0, (byte)0, (byte)255, (byte)0), decoded.GetPixel(0, 1));
	}

	[TestMethod]
	public void Encode_ClearingTransparentPixels_ZeroesOnlyTheirColour()
	{
		ImagePixels source = Corners();
		byte[] before = (byte[])source.GetBuffer().Clone();

		ImagePixels decoded = ImageDecoder.Decode(PngEncoder.Encode(source, new PngEncoderOptions { ClearTransparentPixels = true }));

		Assert.AreEqual(((byte)0, (byte)0, (byte)0, (byte)0), decoded.GetPixel(0, 1));
		Assert.AreEqual(((byte)0, (byte)255, (byte)0, (byte)128), decoded.GetPixel(1, 0), "a translucent pixel keeps its colour");
		Assert.AreEqual(((byte)10, (byte)20, (byte)30, (byte)40), decoded.GetPixel(1, 1));
		CollectionAssert.AreEqual(before, source.GetBuffer(), "the image itself is not changed");
	}

	[TestMethod]
	public void Encode_AtEveryCompressionLevel_DecodesToTheSamePixels()
	{
		ImagePixels source = new(16, 16);
		new Random(3).NextBytes(source.Pixels);

		foreach (CompressionLevel level in Enum.GetValues<CompressionLevel>())
		{
			byte[] png = PngEncoder.Encode(source, new PngEncoderOptions { CompressionLevel = level });

			CollectionAssert.AreEqual(source.GetBuffer(), ImageDecoder.Decode(png).GetBuffer(), level.ToString());
		}
	}

	[TestMethod]
	public void Encode_ToAStream_WritesTheSameBytesAndLeavesItOpen()
	{
		ImagePixels source = Corners();
		using MemoryStream stream = new();
		stream.WriteByte(0xAA);

		PngEncoder.Encode(source, stream);

		Assert.IsTrue(stream.CanWrite);
		CollectionAssert.AreEqual(PngEncoder.Encode(source), stream.ToArray().AsSpan(1).ToArray());
	}

	[TestMethod]
	public void Save_WritesAFileTheDecoderLoads()
	{
		string path = Path.Join(Path.GetTempPath(), $"ktsu-imaging-{Guid.NewGuid():N}.png");
		try
		{
			PngEncoder.Save(Corners(), path);

			CollectionAssert.AreEqual(Corners().GetBuffer(), ImageDecoder.Load(path).GetBuffer());
		}
		finally
		{
			File.Delete(path);
		}
	}

	[TestMethod]
	public void Encode_WithNoImage_Throws() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => PngEncoder.Encode(null!));
}
