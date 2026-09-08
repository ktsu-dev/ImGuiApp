// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests.Images;

using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;

/// <summary>
/// Builds small image files in memory so the decoders can be driven over their whole feature matrix
/// without checking binary fixtures into the repository.
/// </summary>
internal static class TestImageBuilder
{
	/// <summary>Encodes a PNG from raw, unfiltered scanline bytes.</summary>
	/// <param name="width">Image width in pixels.</param>
	/// <param name="height">Image height in pixels.</param>
	/// <param name="colorType">PNG colour type: 0 greyscale, 2 truecolour, 3 palette, 4 greyscale+alpha, 6 truecolour+alpha.</param>
	/// <param name="bitDepth">Bits per sample.</param>
	/// <param name="scanlines">The concatenated unfiltered scanlines, each of the natural byte length for the format.</param>
	/// <param name="palette">Optional PLTE contents, three bytes per entry.</param>
	/// <param name="transparency">Optional tRNS contents.</param>
	/// <param name="filter">The filter type to apply to every scanline, 0 through 4.</param>
	/// <returns>The complete PNG file.</returns>
	public static byte[] Png(
		int width,
		int height,
		byte colorType,
		byte bitDepth,
		byte[] scanlines,
		byte[]? palette = null,
		byte[]? transparency = null,
		byte filter = 0)
	{
		int channels = colorType switch { 0 or 3 => 1, 2 => 3, 4 => 2, _ => 4 };
		int bytesPerRow = ((width * channels * bitDepth) + 7) / 8;
		int filterUnit = Math.Max(1, channels * bitDepth / 8);

		using MemoryStream raw = new();
		byte[] previous = new byte[bytesPerRow];
		for (int row = 0; row < height; row++)
		{
			ReadOnlySpan<byte> current = scanlines.AsSpan(row * bytesPerRow, bytesPerRow);
			raw.WriteByte(filter);
			raw.Write(ApplyFilter(filter, current, previous, filterUnit));
			current.CopyTo(previous);
		}

		using MemoryStream compressed = new();
		using (ZLibStream deflate = new(compressed, CompressionLevel.Optimal, leaveOpen: true))
		{
			raw.Position = 0;
			raw.CopyTo(deflate);
		}

		byte[] header = new byte[13];
		BinaryPrimitives.WriteUInt32BigEndian(header, (uint)width);
		BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), (uint)height);
		header[8] = bitDepth;
		header[9] = colorType;

		using MemoryStream file = new();
		file.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
		WriteChunk(file, "IHDR"u8, header);
		if (palette is not null)
		{
			WriteChunk(file, "PLTE"u8, palette);
		}

		if (transparency is not null)
		{
			WriteChunk(file, "tRNS"u8, transparency);
		}

		WriteChunk(file, "IDAT"u8, compressed.ToArray());
		WriteChunk(file, "IEND"u8, []);
		return file.ToArray();
	}

	/// <summary>Encodes an uncompressed BMP.</summary>
	/// <param name="width">Image width in pixels.</param>
	/// <param name="height">Image height in pixels.</param>
	/// <param name="bitCount">Bits per pixel: 1, 4, 8, 24 or 32.</param>
	/// <param name="rows">The pixel rows in top-down order, each already packed for <paramref name="bitCount"/> but without stride padding.</param>
	/// <param name="palette">Optional palette, four bytes per entry in blue, green, red, reserved order.</param>
	/// <param name="topDown">Whether to store the rows top-down, which a BMP signals with a negative height.</param>
	/// <returns>The complete BMP file.</returns>
	public static byte[] Bmp(int width, int height, int bitCount, byte[][] rows, byte[]? palette = null, bool topDown = false)
	{
		int stride = ((width * bitCount) + 31) / 32 * 4;
		int paletteBytes = palette?.Length ?? 0;
		int pixelOffset = 14 + 40 + paletteBytes;

		byte[] file = new byte[pixelOffset + (stride * height)];
		file[0] = (byte)'B';
		file[1] = (byte)'M';
		BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(2), (uint)file.Length);
		BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(10), (uint)pixelOffset);

		BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(14), 40);
		BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(18), width);
		BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(22), topDown ? -height : height);
		BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan(26), 1);
		BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan(28), (ushort)bitCount);
		BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(46), (uint)(paletteBytes / 4));

		palette?.CopyTo(file.AsSpan(54));

		for (int row = 0; row < height; row++)
		{
			int target = topDown ? row : height - 1 - row;
			rows[row].CopyTo(file.AsSpan(pixelOffset + (target * stride)));
		}

		return file;
	}

	/// <summary>Encodes a TGA.</summary>
	/// <param name="width">Image width in pixels.</param>
	/// <param name="height">Image height in pixels.</param>
	/// <param name="imageType">TGA image type: 1 colour-mapped, 2 true-colour, 3 greyscale, or those plus 8 for the run-length encoded variants.</param>
	/// <param name="pixelBits">Bits per pixel.</param>
	/// <param name="body">The colour map, if any, followed by the image data.</param>
	/// <param name="colorMapLength">The number of colour map entries.</param>
	/// <param name="colorMapEntryBits">Bits per colour map entry.</param>
	/// <param name="descriptor">The descriptor byte: attribute bit count in the low nibble, origin flags in bits four and five.</param>
	/// <returns>The complete TGA file.</returns>
	public static byte[] Tga(int width, int height, int imageType, int pixelBits, byte[] body, int colorMapLength = 0, int colorMapEntryBits = 0, byte descriptor = 0)
	{
		byte[] file = new byte[18 + body.Length];
		file[1] = (byte)(colorMapLength > 0 ? 1 : 0);
		file[2] = (byte)imageType;
		BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan(5), (ushort)colorMapLength);
		file[7] = (byte)colorMapEntryBits;
		BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan(12), (ushort)width);
		BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan(14), (ushort)height);
		file[16] = (byte)pixelBits;
		file[17] = descriptor;
		body.CopyTo(file.AsSpan(18));
		return file;
	}

	private static byte[] ApplyFilter(byte filter, ReadOnlySpan<byte> current, ReadOnlySpan<byte> previous, int filterUnit)
	{
		byte[] result = current.ToArray();
		for (int i = current.Length - 1; i >= 0; i--)
		{
			int left = i >= filterUnit ? current[i - filterUnit] : 0;
			int up = previous[i];
			int upLeft = i >= filterUnit ? previous[i - filterUnit] : 0;

			int predictor = filter switch
			{
				1 => left,
				2 => up,
				3 => (left + up) / 2,
				4 => Paeth(left, up, upLeft),
				_ => 0,
			};

			result[i] = (byte)(current[i] - predictor);
		}

		return result;
	}

	private static int Paeth(int a, int b, int c)
	{
		int p = a + b - c;
		int pa = Math.Abs(p - a);
		int pb = Math.Abs(p - b);
		int pc = Math.Abs(p - c);
		return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
	}

	private static void WriteChunk(Stream target, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
	{
		Span<byte> length = stackalloc byte[4];
		BinaryPrimitives.WriteUInt32BigEndian(length, (uint)data.Length);
		target.Write(length);

		byte[] typeAndData = new byte[type.Length + data.Length];
		type.CopyTo(typeAndData);
		data.CopyTo(typeAndData.AsSpan(type.Length));
		target.Write(typeAndData);

		Span<byte> crc = stackalloc byte[4];
		BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32(typeAndData));
		target.Write(crc);
	}

	private static uint Crc32(ReadOnlySpan<byte> data)
	{
		uint crc = 0xFFFFFFFF;
		foreach (byte value in data)
		{
			crc ^= value;
			for (int bit = 0; bit < 8; bit++)
			{
				crc = (crc >> 1) ^ (0xEDB88320 & (uint)-(int)(crc & 1));
			}
		}

		return crc ^ 0xFFFFFFFF;
	}
}
