// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Images;

using System;
using System.Buffers.Binary;
using System.Numerics;

/// <summary>
/// Decodes Windows BMP images to RGBA8.
/// </summary>
/// <remarks>
/// Handles the core header and every version of the info header, bit counts 1, 4, 8, 16, 24 and 32,
/// bottom-up and top-down row order, and both <c>BI_RGB</c> and <c>BI_BITFIELDS</c> channel layouts.
/// The run-length encoded variants (<c>BI_RLE4</c>, <c>BI_RLE8</c>) and embedded PNG/JPEG payloads
/// are rejected rather than guessed at; they do not occur in the icons and textures this library
/// loads.
/// </remarks>
internal static class BmpDecoder
{
	private const int FileHeaderSize = 14;

	/// <summary>Reports whether a buffer starts with the BMP magic.</summary>
	/// <param name="data">The candidate bytes.</param>
	/// <returns><see langword="true"/> when the buffer is a BMP.</returns>
	public static bool IsBmp(ReadOnlySpan<byte> data) => data.Length >= FileHeaderSize && data[0] == (byte)'B' && data[1] == (byte)'M';

	/// <summary>Decodes a BMP to RGBA8.</summary>
	/// <param name="data">The complete file contents.</param>
	/// <returns>The decoded image.</returns>
	/// <exception cref="InvalidImageDataException">The data is not a well formed BMP this decoder supports.</exception>
	public static ImagePixels Decode(ReadOnlySpan<byte> data)
	{
		if (!IsBmp(data))
		{
			throw new InvalidImageDataException("Not a BMP: the file does not start with 'BM'.");
		}

		if (data.Length < 18)
		{
			throw new InvalidImageDataException("BMP is truncated before its info header.");
		}

		int pixelOffset = (int)BinaryPrimitives.ReadUInt32LittleEndian(data[10..]);
		int headerSize = (int)BinaryPrimitives.ReadUInt32LittleEndian(data[FileHeaderSize..]);
		if (headerSize < 12 || FileHeaderSize + headerSize > data.Length)
		{
			throw new InvalidImageDataException($"BMP declares an info header of {headerSize} bytes, which does not fit the file.");
		}

		Info info = ReadInfo(data.Slice(FileHeaderSize, headerSize), headerSize);
		ChannelMasks masks = ReadMasks(data, headerSize, info);
		byte[]? palette = info.BitCount <= 8 ? ReadPalette(data, headerSize, info) : null;

		int stride = ((info.Width * info.BitCount) + 31) / 32 * 4;
		if (pixelOffset < 0 || pixelOffset + (stride * info.Height) > data.Length)
		{
			throw new InvalidImageDataException("BMP pixel data runs past the end of the file.");
		}

		ImagePixels image = new(info.Width, info.Height);
		Span<byte> pixels = image.Pixels;
		bool sawAlpha = false;

		for (int row = 0; row < info.Height; row++)
		{
			int sourceRow = info.TopDown ? row : info.Height - 1 - row;
			ReadOnlySpan<byte> line = data.Slice(pixelOffset + (sourceRow * stride), stride);
			sawAlpha |= ConvertRow(line, info, masks, palette, pixels.Slice(row * info.Width * 4, info.Width * 4));
		}

		// A BI_RGB image carries no alpha by definition, and plenty of encoders leave the fourth byte
		// of a 32-bit pixel at zero. Taking that literally would make the whole image invisible, so an
		// all-zero alpha channel is treated as opaque.
		if (info.BitCount is 16 or 32 && !sawAlpha)
		{
			for (int i = 3; i < pixels.Length; i += 4)
			{
				pixels[i] = 255;
			}
		}

		return image;
	}

	private static Info ReadInfo(ReadOnlySpan<byte> header, int headerSize)
	{
		bool core = headerSize == 12;

		int width;
		int rawHeight;
		int bitCount;
		int compression = 0;
		int paletteColors = 0;

		if (core)
		{
			width = BinaryPrimitives.ReadInt16LittleEndian(header[4..]);
			rawHeight = BinaryPrimitives.ReadInt16LittleEndian(header[6..]);
			bitCount = BinaryPrimitives.ReadUInt16LittleEndian(header[10..]);
		}
		else
		{
			width = BinaryPrimitives.ReadInt32LittleEndian(header[4..]);
			rawHeight = BinaryPrimitives.ReadInt32LittleEndian(header[8..]);
			bitCount = BinaryPrimitives.ReadUInt16LittleEndian(header[14..]);
			compression = (int)BinaryPrimitives.ReadUInt32LittleEndian(header[16..]);
			paletteColors = (int)BinaryPrimitives.ReadUInt32LittleEndian(header[32..]);
		}

		if (width <= 0 || rawHeight == 0)
		{
			throw new InvalidImageDataException($"BMP has unusable dimensions {width}x{rawHeight}.");
		}

		if (compression is 1 or 2)
		{
			throw new InvalidImageDataException("Run-length encoded BMP files are not supported.");
		}

		if (compression is 4 or 5)
		{
			throw new InvalidImageDataException("BMP files embedding a JPEG or PNG payload are not supported.");
		}

		if (compression is not (0 or 3 or 6))
		{
			throw new InvalidImageDataException($"BMP uses unsupported compression {compression}.");
		}

		if (bitCount is not (1 or 4 or 8 or 16 or 24 or 32))
		{
			throw new InvalidImageDataException($"BMP uses unsupported bit count {bitCount}.");
		}

		return new Info
		{
			Width = width,
			Height = Math.Abs(rawHeight),
			TopDown = rawHeight < 0,
			BitCount = bitCount,
			Compression = compression,
			PaletteColors = paletteColors,
			Core = core,
		};
	}

	private static byte[] ReadPalette(ReadOnlySpan<byte> data, int headerSize, Info info)
	{
		int entrySize = info.Core ? 3 : 4;
		int entries = info.PaletteColors > 0 ? info.PaletteColors : 1 << info.BitCount;
		int start = FileHeaderSize + headerSize + InlineMaskBytes(headerSize, info.Compression);

		if (start + (entries * entrySize) > data.Length)
		{
			throw new InvalidImageDataException("BMP palette runs past the end of the file.");
		}

		byte[] palette = new byte[entries * 4];
		for (int i = 0; i < entries; i++)
		{
			int source = start + (i * entrySize);
			palette[(i * 4) + 0] = data[source + 2];
			palette[(i * 4) + 1] = data[source + 1];
			palette[(i * 4) + 2] = data[source];
			palette[(i * 4) + 3] = 255;
		}

		return palette;
	}

	/// <summary>
	/// The number of mask bytes sitting between a 40-byte info header and the palette. Later header
	/// versions carry the masks inside the header itself, so there are none.
	/// </summary>
	private static int InlineMaskBytes(int headerSize, int compression) =>
		headerSize == 40 && compression is 3 or 6 ? (compression == 6 ? 16 : 12) : 0;

	private static ChannelMasks ReadMasks(ReadOnlySpan<byte> data, int headerSize, Info info)
	{
		if (info.Compression is 3 or 6)
		{
			ReadOnlySpan<byte> maskSource = headerSize >= 52
				? data.Slice(FileHeaderSize + 40, headerSize - 40)
				: data[(FileHeaderSize + headerSize)..];

			uint alpha = info.Compression == 6 || headerSize >= 56
				? BinaryPrimitives.ReadUInt32LittleEndian(maskSource[12..])
				: 0;

			return new ChannelMasks(
				BinaryPrimitives.ReadUInt32LittleEndian(maskSource),
				BinaryPrimitives.ReadUInt32LittleEndian(maskSource[4..]),
				BinaryPrimitives.ReadUInt32LittleEndian(maskSource[8..]),
				alpha);
		}

		// BI_RGB defaults: 5-5-5 at 16 bits, 8-8-8 in the low three bytes at 32 bits.
		return info.BitCount == 16
			? new ChannelMasks(0x7C00, 0x03E0, 0x001F, 0)
			: new ChannelMasks(0x00FF0000, 0x0000FF00, 0x000000FF, 0xFF000000);
	}

	/// <summary>Converts one source scanline into RGBA8, reporting whether any pixel carried alpha.</summary>
	private static bool ConvertRow(ReadOnlySpan<byte> line, Info info, ChannelMasks masks, byte[]? palette, Span<byte> destination)
	{
		bool sawAlpha = false;

		for (int column = 0; column < info.Width; column++)
		{
			Span<byte> rgba = destination[(column * 4)..];
			switch (info.BitCount)
			{
				case 1:
				case 4:
				case 8:
					WritePaletteEntry(ReadPackedIndex(line, column, info.BitCount), palette!, rgba);
					break;

				case 16:
					WriteMasked(BinaryPrimitives.ReadUInt16LittleEndian(line[(column * 2)..]), masks, rgba);
					sawAlpha |= rgba[3] != 0;
					break;

				case 24:
					rgba[0] = line[(column * 3) + 2];
					rgba[1] = line[(column * 3) + 1];
					rgba[2] = line[column * 3];
					rgba[3] = 255;
					break;

				default:
					WriteMasked(BinaryPrimitives.ReadUInt32LittleEndian(line[(column * 4)..]), masks, rgba);
					sawAlpha |= rgba[3] != 0;
					break;
			}
		}

		return sawAlpha;
	}

	private static void WritePaletteEntry(int index, byte[] palette, Span<byte> rgba)
	{
		if ((index * 4) + 3 >= palette.Length)
		{
			throw new InvalidImageDataException($"BMP references palette entry {index}, which is past the end of a {palette.Length / 4} entry palette.");
		}

		palette.AsSpan(index * 4, 4).CopyTo(rgba);
	}

	private static void WriteMasked(uint value, ChannelMasks masks, Span<byte> rgba)
	{
		rgba[0] = ExtractChannel(value, masks.Red);
		rgba[1] = ExtractChannel(value, masks.Green);
		rgba[2] = ExtractChannel(value, masks.Blue);
		rgba[3] = ExtractChannel(value, masks.Alpha);
	}

	private static byte ExtractChannel(uint value, uint mask)
	{
		if (mask == 0)
		{
			return 0;
		}

		int shift = BitOperations.TrailingZeroCount(mask);
		uint raw = (value & mask) >> shift;
		int bits = BitOperations.PopCount(mask);

		// Scale to 8 bits so an all-ones field maps to 255 rather than to 2^bits - 1.
		return bits >= 8
			? (byte)(raw >> (bits - 8))
			: (byte)(raw * 255 / ((1u << bits) - 1));
	}

	private static int ReadPackedIndex(ReadOnlySpan<byte> line, int column, int bitCount)
	{
		int perByte = 8 / bitCount;
		byte packed = line[column / perByte];
		int shift = 8 - bitCount - (column % perByte * bitCount);
		return (packed >> shift) & ((1 << bitCount) - 1);
	}

	private readonly record struct ChannelMasks(uint Red, uint Green, uint Blue, uint Alpha);

	private readonly record struct Info
	{
		public int Width { get; init; }
		public int Height { get; init; }
		public bool TopDown { get; init; }
		public int BitCount { get; init; }
		public int Compression { get; init; }
		public int PaletteColors { get; init; }
		public bool Core { get; init; }
	}
}
