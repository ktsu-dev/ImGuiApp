// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Images;

using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;

/// <summary>
/// Decodes PNG (ISO/IEC 15948) images to RGBA8.
/// </summary>
/// <remarks>
/// Covers the whole non-extension format: all five colour types, bit depths 1, 2, 4, 8 and 16,
/// palettes, <c>tRNS</c> transparency in every form it takes, Adam7 interlacing, and all five
/// scanline filters. Ancillary chunks other than <c>tRNS</c> are skipped, which includes <c>gAMA</c>
/// and <c>iCCP</c> — pixels are taken at face value with no colour management, the same as the
/// texture pipeline that consumes them. Inflation uses <see cref="ZLibStream"/> from the base class
/// library, so nothing here needs a compression dependency.
/// </remarks>
internal static class PngDecoder
{
	private static ReadOnlySpan<byte> Signature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

	// Adam7: the origin and step of each of the seven interlace passes.
	private static ReadOnlySpan<int> PassOriginX => [0, 4, 0, 2, 0, 1, 0];
	private static ReadOnlySpan<int> PassOriginY => [0, 0, 4, 0, 2, 0, 1];
	private static ReadOnlySpan<int> PassStepX => [8, 8, 4, 4, 2, 2, 1];
	private static ReadOnlySpan<int> PassStepY => [8, 8, 8, 4, 4, 2, 2];

	/// <summary>Reports whether a buffer starts with the PNG signature.</summary>
	/// <param name="data">The candidate bytes.</param>
	/// <returns><see langword="true"/> when the buffer is a PNG.</returns>
	public static bool IsPng(ReadOnlySpan<byte> data) => data.Length >= 8 && data[..8].SequenceEqual(Signature);

	/// <summary>Decodes a PNG to RGBA8.</summary>
	/// <param name="data">The complete file contents.</param>
	/// <returns>The decoded image.</returns>
	/// <exception cref="InvalidImageDataException">The data is not a well formed PNG this decoder supports.</exception>
	public static ImagePixels Decode(ReadOnlySpan<byte> data)
	{
		if (!IsPng(data))
		{
			throw new InvalidImageDataException("Not a PNG: the file does not start with the PNG signature.");
		}

		Header header = default;
		bool haveHeader = false;
		byte[]? palette = null;
		byte[]? paletteAlpha = null;
		ushort[]? transparentKey = null;
		using MemoryStream compressed = new();

		int position = 8;
		while (position + 8 <= data.Length)
		{
			uint length = BinaryPrimitives.ReadUInt32BigEndian(data[position..]);
			if (length > int.MaxValue)
			{
				throw new InvalidImageDataException("PNG chunk length exceeds the addressable range.");
			}

			ReadOnlySpan<byte> type = data.Slice(position + 4, 4);
			int dataStart = position + 8;
			if (dataStart + (int)length + 4 > data.Length)
			{
				throw new InvalidImageDataException("PNG chunk runs past the end of the file.");
			}

			ReadOnlySpan<byte> chunk = data.Slice(dataStart, (int)length);

			if (type.SequenceEqual("IHDR"u8))
			{
				header = ReadHeader(chunk);
				haveHeader = true;
			}
			else if (type.SequenceEqual("PLTE"u8))
			{
				if (chunk.Length % 3 != 0)
				{
					throw new InvalidImageDataException("PNG palette length is not a multiple of three.");
				}

				palette = chunk.ToArray();
			}
			else if (type.SequenceEqual("tRNS"u8))
			{
				ReadTransparency(chunk, haveHeader ? header : throw new InvalidImageDataException("PNG tRNS chunk appears before IHDR."), ref paletteAlpha, ref transparentKey);
			}
			else if (type.SequenceEqual("IDAT"u8))
			{
				compressed.Write(chunk);
			}
			else if (type.SequenceEqual("IEND"u8))
			{
				break;
			}

			position = dataStart + (int)length + 4;
		}

		if (!haveHeader)
		{
			throw new InvalidImageDataException("PNG is missing its IHDR chunk.");
		}

		if (compressed.Length == 0)
		{
			throw new InvalidImageDataException("PNG is missing its image data.");
		}

		if (header.ColorType == 3 && palette is null)
		{
			throw new InvalidImageDataException("Palette-indexed PNG is missing its PLTE chunk.");
		}

		byte[] raw = Inflate(compressed, ExpectedRawLength(header));
		ImagePixels image = new(header.Width, header.Height);

		if (header.Interlace == 0)
		{
			ExpandPass(raw, 0, header, header.Width, header.Height, 0, 0, 1, 1, palette, paletteAlpha, transparentKey, image);
		}
		else
		{
			int offset = 0;
			for (int pass = 0; pass < 7; pass++)
			{
				int passWidth = CeilDiv(header.Width - PassOriginX[pass], PassStepX[pass]);
				int passHeight = CeilDiv(header.Height - PassOriginY[pass], PassStepY[pass]);
				if (passWidth <= 0 || passHeight <= 0)
				{
					continue;
				}

				ExpandPass(raw, offset, header, passWidth, passHeight, PassOriginX[pass], PassOriginY[pass], PassStepX[pass], PassStepY[pass], palette, paletteAlpha, transparentKey, image);
				offset += passHeight * (1 + BytesPerRow(header, passWidth));
			}
		}

		return image;
	}

	private static Header ReadHeader(ReadOnlySpan<byte> chunk)
	{
		if (chunk.Length < 13)
		{
			throw new InvalidImageDataException("PNG IHDR chunk is too short.");
		}

		uint width = BinaryPrimitives.ReadUInt32BigEndian(chunk);
		uint height = BinaryPrimitives.ReadUInt32BigEndian(chunk[4..]);
		if (width == 0 || height == 0 || width > int.MaxValue || height > int.MaxValue)
		{
			throw new InvalidImageDataException($"PNG has unusable dimensions {width}x{height}.");
		}

		Header header = new()
		{
			Width = (int)width,
			Height = (int)height,
			BitDepth = chunk[8],
			ColorType = chunk[9],
			Interlace = chunk[12],
		};

		if (chunk[10] != 0)
		{
			throw new InvalidImageDataException($"PNG uses unsupported compression method {chunk[10]}.");
		}

		if (chunk[11] != 0)
		{
			throw new InvalidImageDataException($"PNG uses unsupported filter method {chunk[11]}.");
		}

		if (header.Interlace > 1)
		{
			throw new InvalidImageDataException($"PNG uses unsupported interlace method {header.Interlace}.");
		}

		bool validDepth = header.ColorType switch
		{
			0 => header.BitDepth is 1 or 2 or 4 or 8 or 16,
			2 or 4 or 6 => header.BitDepth is 8 or 16,
			3 => header.BitDepth is 1 or 2 or 4 or 8,
			_ => throw new InvalidImageDataException($"PNG uses unsupported colour type {header.ColorType}."),
		};

		return validDepth
			? header
			: throw new InvalidImageDataException($"PNG colour type {header.ColorType} does not allow a bit depth of {header.BitDepth}.");
	}

	private static void ReadTransparency(ReadOnlySpan<byte> chunk, Header header, ref byte[]? paletteAlpha, ref ushort[]? transparentKey)
	{
		switch (header.ColorType)
		{
			case 3:
				paletteAlpha = chunk.ToArray();
				break;

			case 0 when chunk.Length >= 2:
				transparentKey = [BinaryPrimitives.ReadUInt16BigEndian(chunk)];
				break;

			case 2 when chunk.Length >= 6:
				transparentKey =
				[
					BinaryPrimitives.ReadUInt16BigEndian(chunk),
					BinaryPrimitives.ReadUInt16BigEndian(chunk[2..]),
					BinaryPrimitives.ReadUInt16BigEndian(chunk[4..]),
				];
				break;

			default:
				// tRNS is meaningless for colour types 4 and 6, and a short chunk is not usable; the
				// spec says decoders may ignore it, which is safer than failing the whole image.
				break;
		}
	}

	private static int Channels(byte colorType) => colorType switch
	{
		0 or 3 => 1,
		2 => 3,
		4 => 2,
		_ => 4,
	};

	private static int BytesPerRow(Header header, int width) =>
		((width * Channels(header.ColorType) * header.BitDepth) + 7) / 8;

	private static int ExpectedRawLength(Header header)
	{
		if (header.Interlace == 0)
		{
			return header.Height * (1 + BytesPerRow(header, header.Width));
		}

		int total = 0;
		for (int pass = 0; pass < 7; pass++)
		{
			int passWidth = CeilDiv(header.Width - PassOriginX[pass], PassStepX[pass]);
			int passHeight = CeilDiv(header.Height - PassOriginY[pass], PassStepY[pass]);
			if (passWidth > 0 && passHeight > 0)
			{
				total += passHeight * (1 + BytesPerRow(header, passWidth));
			}
		}

		return total;
	}

	private static int CeilDiv(int value, int divisor) => value <= 0 ? 0 : (value + divisor - 1) / divisor;

	private static byte[] Inflate(MemoryStream compressed, int expectedLength)
	{
		compressed.Position = 0;
		byte[] result = new byte[expectedLength];
		try
		{
			using ZLibStream inflater = new(compressed, CompressionMode.Decompress, leaveOpen: true);
			int read = 0;
			while (read < expectedLength)
			{
				int chunk = inflater.Read(result, read, expectedLength - read);
				if (chunk == 0)
				{
					throw new InvalidImageDataException($"PNG image data ended after {read} of {expectedLength} expected bytes.");
				}

				read += chunk;
			}
		}
		catch (InvalidDataException ex)
		{
			throw new InvalidImageDataException("PNG image data is not a valid zlib stream.", ex);
		}

		return result;
	}

	/// <summary>
	/// Unfilters one interlace pass in place and expands it into the destination image. A
	/// non-interlaced image is the degenerate pass covering every pixel with a step of one.
	/// </summary>
	private static void ExpandPass(
		byte[] raw,
		int offset,
		Header header,
		int passWidth,
		int passHeight,
		int originX,
		int originY,
		int stepX,
		int stepY,
		byte[]? palette,
		byte[]? paletteAlpha,
		ushort[]? transparentKey,
		ImagePixels destination)
	{
		int channels = Channels(header.ColorType);
		int bytesPerRow = BytesPerRow(header, passWidth);
		int filterUnit = Math.Max(1, channels * header.BitDepth / 8);

		// The common case is eight-bit truecolour with no interlacing, where a whole scanline lands in
		// the destination as one span operation.
		bool contiguous = stepX == 1 && originX == 0;
		bool copyable = contiguous && header.BitDepth == 8 && header.ColorType == 6;
		bool widenable = contiguous && header.BitDepth == 8 && header.ColorType == 2 && transparentKey is null;

		Span<byte> pixels = destination.Pixels;
		int destinationWidth = destination.Width;

		Span<byte> rgba = stackalloc byte[4];
		byte[] previous = new byte[bytesPerRow];

		for (int row = 0; row < passHeight; row++)
		{
			int rowStart = offset + (row * (1 + bytesPerRow));
			byte filter = raw[rowStart];
			Span<byte> current = raw.AsSpan(rowStart + 1, bytesPerRow);
			Unfilter(filter, current, previous, filterUnit);

			int y = originY + (row * stepY);
			int rowBase = y * destinationWidth * 4;

			if (copyable)
			{
				current.CopyTo(pixels.Slice(rowBase, passWidth * 4));
			}
			else if (widenable)
			{
				for (int column = 0; column < passWidth; column++)
				{
					int source = column * 3;
					int target = rowBase + (column * 4);
					pixels[target] = current[source];
					pixels[target + 1] = current[source + 1];
					pixels[target + 2] = current[source + 2];
					pixels[target + 3] = 255;
				}
			}
			else
			{
				for (int column = 0; column < passWidth; column++)
				{
					ExpandPixel(current, column, header, palette, paletteAlpha, transparentKey, rgba);
					int target = rowBase + ((originX + (column * stepX)) * 4);
					pixels[target] = rgba[0];
					pixels[target + 1] = rgba[1];
					pixels[target + 2] = rgba[2];
					pixels[target + 3] = rgba[3];
				}
			}

			current.CopyTo(previous);
		}
	}

	private static void Unfilter(byte filter, Span<byte> current, ReadOnlySpan<byte> previous, int filterUnit)
	{
		switch (filter)
		{
			case 0:
				break;

			case 1:
				for (int i = filterUnit; i < current.Length; i++)
				{
					current[i] = (byte)(current[i] + current[i - filterUnit]);
				}

				break;

			case 2:
				for (int i = 0; i < current.Length; i++)
				{
					current[i] = (byte)(current[i] + previous[i]);
				}

				break;

			case 3:
				for (int i = 0; i < current.Length; i++)
				{
					int left = i >= filterUnit ? current[i - filterUnit] : 0;
					current[i] = (byte)(current[i] + ((left + previous[i]) / 2));
				}

				break;

			case 4:
				for (int i = 0; i < current.Length; i++)
				{
					int left = i >= filterUnit ? current[i - filterUnit] : 0;
					int up = previous[i];
					int upLeft = i >= filterUnit ? previous[i - filterUnit] : 0;
					current[i] = (byte)(current[i] + Paeth(left, up, upLeft));
				}

				break;

			default:
				throw new InvalidImageDataException($"PNG scanline uses unknown filter type {filter}.");
		}
	}

	private static int Paeth(int a, int b, int c)
	{
		int p = a + b - c;
		int pa = Math.Abs(p - a);
		int pb = Math.Abs(p - b);
		int pc = Math.Abs(p - c);
		return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
	}

	private static void ExpandPixel(
		ReadOnlySpan<byte> row,
		int column,
		Header header,
		byte[]? palette,
		byte[]? paletteAlpha,
		ushort[]? transparentKey,
		Span<byte> rgba)
	{
		int channels = Channels(header.ColorType);

		if (header.BitDepth < 8)
		{
			// Only greyscale and palette reach here, so there is exactly one sample per pixel.
			int sample = ReadPackedSample(row, column, header.BitDepth);
			if (header.ColorType == 3)
			{
				WritePaletteEntry(sample, palette!, paletteAlpha, rgba);
				return;
			}

			byte grey = ScaleToByte(sample, header.BitDepth);
			byte alpha = transparentKey is not null && transparentKey[0] == sample ? (byte)0 : (byte)255;
			rgba[0] = grey;
			rgba[1] = grey;
			rgba[2] = grey;
			rgba[3] = alpha;
			return;
		}

		int sampleBytes = header.BitDepth / 8;
		int baseIndex = column * channels * sampleBytes;

		Span<ushort> samples = stackalloc ushort[4];
		for (int channel = 0; channel < channels; channel++)
		{
			int index = baseIndex + (channel * sampleBytes);
			samples[channel] = sampleBytes == 1 ? row[index] : BinaryPrimitives.ReadUInt16BigEndian(row[index..]);
		}

		// A 16-bit sample is truncated to its high byte, which is what the GPU texture format holds.
		int shift = sampleBytes == 1 ? 0 : 8;

		switch (header.ColorType)
		{
			case 0:
			{
				byte grey = (byte)(samples[0] >> shift);
				rgba[0] = grey;
				rgba[1] = grey;
				rgba[2] = grey;
				rgba[3] = transparentKey is not null && transparentKey[0] == samples[0] ? (byte)0 : (byte)255;
				break;
			}

			case 2:
			{
				rgba[0] = (byte)(samples[0] >> shift);
				rgba[1] = (byte)(samples[1] >> shift);
				rgba[2] = (byte)(samples[2] >> shift);
				bool keyed = transparentKey is not null
					&& transparentKey[0] == samples[0]
					&& transparentKey[1] == samples[1]
					&& transparentKey[2] == samples[2];
				rgba[3] = keyed ? (byte)0 : (byte)255;
				break;
			}

			case 3:
				WritePaletteEntry(samples[0], palette!, paletteAlpha, rgba);
				break;

			case 4:
			{
				byte grey = (byte)(samples[0] >> shift);
				rgba[0] = grey;
				rgba[1] = grey;
				rgba[2] = grey;
				rgba[3] = (byte)(samples[1] >> shift);
				break;
			}

			default:
				rgba[0] = (byte)(samples[0] >> shift);
				rgba[1] = (byte)(samples[1] >> shift);
				rgba[2] = (byte)(samples[2] >> shift);
				rgba[3] = (byte)(samples[3] >> shift);
				break;
		}
	}

	private static void WritePaletteEntry(int index, byte[] palette, byte[]? paletteAlpha, Span<byte> rgba)
	{
		int entry = index * 3;
		if (entry + 2 >= palette.Length)
		{
			throw new InvalidImageDataException($"PNG references palette entry {index}, which is past the end of a {palette.Length / 3} entry palette.");
		}

		rgba[0] = palette[entry];
		rgba[1] = palette[entry + 1];
		rgba[2] = palette[entry + 2];

		// tRNS may be shorter than the palette; entries it does not cover are fully opaque.
		rgba[3] = paletteAlpha is not null && index < paletteAlpha.Length ? paletteAlpha[index] : (byte)255;
	}

	private static int ReadPackedSample(ReadOnlySpan<byte> row, int column, int bitDepth)
	{
		int perByte = 8 / bitDepth;
		byte packed = row[column / perByte];
		int shift = 8 - bitDepth - (column % perByte * bitDepth);
		return (packed >> shift) & ((1 << bitDepth) - 1);
	}

	/// <summary>Expands a sub-byte greyscale sample to the full 0-255 range.</summary>
	private static byte ScaleToByte(int sample, int bitDepth) => (byte)(sample * 255 / ((1 << bitDepth) - 1));

	private readonly record struct Header
	{
		public int Width { get; init; }
		public int Height { get; init; }
		public byte BitDepth { get; init; }
		public byte ColorType { get; init; }
		public byte Interlace { get; init; }
	}
}
