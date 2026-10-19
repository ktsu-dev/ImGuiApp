// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests.Images;

using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using ktsu.ImGui.App.Images;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Headers whose dimensions or lengths overflow the decoders' size arithmetic must fail as
/// <see cref="InvalidImageDataException"/>, the documented failure, and before allocating a buffer
/// sized from them. Callers such as <c>ImGuiApp.ReloadAllTextures</c> catch only that exception.
/// </summary>
[TestClass]
public class OversizedHeaderTests
{
	[TestMethod]
	public void Png_16BitRgbaWhoseScanlinesOverflowAnInt_Throws()
	{
		byte[] file = Png(16384, 16384, colorType: 6, bitDepth: 16, Deflate(new byte[16]));

		Assert.ThrowsExactly<InvalidImageDataException>(() => ImageDecoder.Decode(file));
	}

	[TestMethod]
	public void Png_WideRowTheImageDataCannotInflateTo_ThrowsBeforeAllocating()
	{
		byte[] file = Png((1 << 26) + 1, 1, colorType: 6, bitDepth: 16, Deflate(new byte[16]));

		InvalidImageDataException exception = Assert.ThrowsExactly<InvalidImageDataException>(() => ImageDecoder.Decode(file));

		Assert.IsTrue(exception.Message.Contains("too short", StringComparison.Ordinal), exception.Message);
	}

	[TestMethod]
	public void Png_ChunkLengthNearIntMaxValue_Throws()
	{
		using MemoryStream file = new();
		file.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
		WriteChunk(file, "IHDR"u8, Header(1, 1, colorType: 6, bitDepth: 8));
		byte[] length = new byte[4];
		BinaryPrimitives.WriteUInt32BigEndian(length, 0x7FFFFFFF);
		file.Write(length);
		file.Write("IDAT"u8);
		file.Write(new byte[16]);

		InvalidImageDataException exception = Assert.ThrowsExactly<InvalidImageDataException>(() => ImageDecoder.Decode(file.ToArray()));

		Assert.IsTrue(exception.Message.Contains("past the end", StringComparison.Ordinal), exception.Message);
	}

	[TestMethod]
	public void Png_HighlyCompressibleImage_StillDecodes()
	{
		// Guards the inflate-ratio check against rejecting a real file: a blank image compresses about
		// as well as deflate allows.
		byte[] file = TestImageBuilder.Png(512, 512, colorType: 6, bitDepth: 8, new byte[512 * 512 * 4]);

		ImagePixels image = ImageDecoder.Decode(file);

		Assert.AreEqual(512, image.Width);
		Assert.AreEqual(512, image.Height);
	}

	[TestMethod]
	public void Jpeg_MaximumFrameDimensions_Throws()
	{
		byte[] quantization = new byte[64];
		Array.Fill(quantization, (byte)1);

		using MemoryStream file = new();
		file.Write([0xFF, 0xD8]);
		file.Write([0xFF, 0xDB, 0x00, 0x43, 0x00]);
		file.Write(quantization);
		file.Write([0xFF, 0xC0, 0x00, 0x0B, 0x08, 0xFF, 0xFF, 0xFF, 0xFF, 0x01, 0x01, 0x11, 0x00]);
		file.Write([0xFF, 0xD9]);

		InvalidImageDataException exception = Assert.ThrowsExactly<InvalidImageDataException>(() => ImageDecoder.Decode(file.ToArray()));

		Assert.IsTrue(exception.Message.Contains("too large", StringComparison.Ordinal), exception.Message);
	}

	[TestMethod]
	public void Jpeg_FrameWithNoScan_ThrowsBeforeAllocating()
	{
		// SOI, SOF0 for an 8000 high by 65535 wide three-component image, EOI: 23 bytes that used to
		// allocate about 3 GB of coefficients before failing.
		byte[] file = [0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x11, 8, 0x1F, 0x40, 0xFF, 0xFF, 3, 1, 0x11, 0, 2, 0x11, 0, 3, 0x11, 0, 0xFF, 0xD9];

		AssertRejectedWithoutLargeAllocation(file);
	}

	[TestMethod]
	public void Jpeg_ScanTooShortForItsBlocks_ThrowsBeforeAllocating()
	{
		using MemoryStream file = new();
		file.Write([0xFF, 0xD8]);
		file.Write([0xFF, 0xC0, 0x00, 0x11, 8, 0x1F, 0x40, 0xFF, 0xFF, 3, 1, 0x11, 0, 2, 0x11, 0, 3, 0x11, 0]);
		file.Write([0xFF, 0xDA, 0x00, 0x0C, 3, 1, 0x00, 2, 0x11, 3, 0x11, 0, 63, 0]);
		file.Write(new byte[16]);
		file.Write([0xFF, 0xD9]);

		InvalidImageDataException exception = AssertRejectedWithoutLargeAllocation(file.ToArray());

		Assert.IsTrue(exception.Message.Contains("too short", StringComparison.Ordinal), exception.Message);
	}

	[TestMethod]
	public void Bmp_HeightOfIntMinValue_Throws()
	{
		byte[] file = TestImageBuilder.Bmp(1, 1, 24, [[0, 0, 0]]);
		BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(22), int.MinValue);

		Assert.ThrowsExactly<InvalidImageDataException>(() => ImageDecoder.Decode(file));
	}

	[TestMethod]
	public void Tga_MaximumDimensions_Throws()
	{
		byte[] file = TestImageBuilder.Tga(ushort.MaxValue, ushort.MaxValue, imageType: 2, pixelBits: 32, new byte[4]);

		InvalidImageDataException exception = Assert.ThrowsExactly<InvalidImageDataException>(() => ImageDecoder.Decode(file));

		Assert.IsTrue(exception.Message.Contains("too large", StringComparison.Ordinal), exception.Message);
	}

	private static byte[] Png(int width, int height, byte colorType, byte bitDepth, byte[] imageData)
	{
		using MemoryStream file = new();
		file.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
		WriteChunk(file, "IHDR"u8, Header(width, height, colorType, bitDepth));
		WriteChunk(file, "IDAT"u8, imageData);
		WriteChunk(file, "IEND"u8, []);
		return file.ToArray();
	}

	private static InvalidImageDataException AssertRejectedWithoutLargeAllocation(byte[] file)
	{
		long before = GC.GetAllocatedBytesForCurrentThread();
		InvalidImageDataException exception = Assert.ThrowsExactly<InvalidImageDataException>(() => ImageDecoder.Decode(file));
		long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

		Assert.IsLessThan(4L * 1024 * 1024, allocated, $"Allocated {allocated} bytes before rejecting the file.");
		return exception;
	}

	private static byte[] Header(int width, int height, byte colorType, byte bitDepth)
	{
		byte[] header = new byte[13];
		BinaryPrimitives.WriteUInt32BigEndian(header, (uint)width);
		BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), (uint)height);
		header[8] = bitDepth;
		header[9] = colorType;
		return header;
	}

	private static byte[] Deflate(byte[] raw)
	{
		using MemoryStream compressed = new();
		using (ZLibStream deflate = new(compressed, CompressionLevel.Optimal, leaveOpen: true))
		{
			deflate.Write(raw);
		}

		return compressed.ToArray();
	}

	// The decoder does not verify chunk CRCs, so these are written as zero.
	private static void WriteChunk(Stream target, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
	{
		byte[] length = new byte[4];
		BinaryPrimitives.WriteUInt32BigEndian(length, (uint)data.Length);
		target.Write(length);
		target.Write(type);
		target.Write(data);
		target.Write(new byte[4]);
	}
}
