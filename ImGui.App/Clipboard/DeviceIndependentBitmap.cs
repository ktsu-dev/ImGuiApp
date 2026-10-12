// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Clipboard;

using System.Buffers.Binary;

/// <summary>Converts between the Windows clipboard's device-independent bitmaps and files the codec reads.</summary>
/// <remarks>
/// A clipboard DIB is a BMP file without its 14-byte file header, so reading one means putting the header
/// back, with the offset to the pixels worked out from what follows the info header: three colour masks
/// for an old-style header with bit fields, and a palette for eight bits or fewer.
/// </remarks>
internal static class DeviceIndependentBitmap
{
	/// <summary>The size of a <c>BITMAPINFOHEADER</c>.</summary>
	public const int InfoHeaderSize = 40;

	private const int FileHeaderSize = 14;
	private const uint Rgb = 0;
	private const uint BitFields = 3;
	private const uint AlphaBitFields = 6;

	/// <summary>Puts a BMP file header in front of a clipboard DIB.</summary>
	/// <param name="dib">The DIB: an info header of any version, then masks, palette and pixels.</param>
	/// <returns>The BMP file, or null when the DIB is too short to be one.</returns>
	public static byte[]? ToBitmapFile(ReadOnlySpan<byte> dib)
	{
		if (dib.Length < InfoHeaderSize)
		{
			return null;
		}

		int headerSize = BinaryPrimitives.ReadInt32LittleEndian(dib);
		if (headerSize < InfoHeaderSize || headerSize > dib.Length)
		{
			return null;
		}

		int bitCount = BinaryPrimitives.ReadUInt16LittleEndian(dib[14..]);
		uint compression = BinaryPrimitives.ReadUInt32LittleEndian(dib[16..]);
		uint colorsUsed = BinaryPrimitives.ReadUInt32LittleEndian(dib[32..]);

		// A version 4 or 5 header holds its masks itself; the original one is followed by them.
		int masks = headerSize == InfoHeaderSize ? compression switch
		{
			BitFields => 12,
			AlphaBitFields => 16,
			_ => 0,
		} : 0;
		long paletteEntries = colorsUsed != 0 ? colorsUsed : bitCount <= 8 ? 1L << bitCount : 0;
		long pixelOffset = headerSize + masks + (paletteEntries * 4);
		if (pixelOffset > dib.Length)
		{
			return null;
		}

		byte[] file = new byte[FileHeaderSize + dib.Length];
		file[0] = (byte)'B';
		file[1] = (byte)'M';
		BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(2), file.Length);
		BinaryPrimitives.WriteInt32LittleEndian(file.AsSpan(10), FileHeaderSize + (int)pixelOffset);
		dib.CopyTo(file.AsSpan(FileHeaderSize));

		return file;
	}

	/// <summary>Builds a 32-bit bottom-up DIB, with an original info header, from top-down BGRA rows.</summary>
	/// <param name="width">The width in pixels.</param>
	/// <param name="height">The height in pixels.</param>
	/// <param name="bgra">The pixels, top row first, four bytes each.</param>
	/// <returns>The DIB.</returns>
	public static byte[] FromBgra(int width, int height, ReadOnlySpan<byte> bgra)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
		ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
		int stride = width * 4;
		if (bgra.Length != stride * height)
		{
			throw new ArgumentException("The pixels don't fill the size given.", nameof(bgra));
		}

		byte[] dib = new byte[InfoHeaderSize + bgra.Length];
		Span<byte> header = dib.AsSpan(0, InfoHeaderSize);
		BinaryPrimitives.WriteInt32LittleEndian(header, InfoHeaderSize);
		BinaryPrimitives.WriteInt32LittleEndian(header[4..], width);

		// Bottom-up, a positive height, because some applications still misread a top-down clipboard DIB.
		BinaryPrimitives.WriteInt32LittleEndian(header[8..], height);
		BinaryPrimitives.WriteUInt16LittleEndian(header[12..], 1);
		BinaryPrimitives.WriteUInt16LittleEndian(header[14..], 32);
		BinaryPrimitives.WriteUInt32LittleEndian(header[16..], Rgb);
		BinaryPrimitives.WriteInt32LittleEndian(header[20..], bgra.Length);

		// 72 dots per inch, in dots per metre.
		BinaryPrimitives.WriteInt32LittleEndian(header[24..], 2835);
		BinaryPrimitives.WriteInt32LittleEndian(header[28..], 2835);

		for (int row = 0; row < height; row++)
		{
			bgra.Slice(row * stride, stride).CopyTo(dib.AsSpan(InfoHeaderSize + ((height - 1 - row) * stride)));
		}

		return dib;
	}
}
