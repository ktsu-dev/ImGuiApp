// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Images;

using System;
using System.Buffers.Binary;

/// <summary>
/// Decodes Truevision TGA images to RGBA8.
/// </summary>
/// <remarks>
/// Handles colour-mapped, true-colour and greyscale images, both uncompressed and run-length
/// encoded, at 8, 15, 16, 24 and 32 bits per pixel, honouring the descriptor byte's origin flags.
/// TGA has no magic number at the front of the file, so <see cref="IsTga"/> is a plausibility check
/// rather than a certainty; <see cref="ImageDecoder"/> only falls back to it once every other format
/// has been ruled out.
/// </remarks>
internal static class TgaDecoder
{
	private const int HeaderSize = 18;

	/// <summary>Reports whether a buffer plausibly holds a TGA image.</summary>
	/// <param name="data">The candidate bytes.</param>
	/// <returns><see langword="true"/> when the header fields are self-consistent, or the v2 footer is present.</returns>
	public static bool IsTga(ReadOnlySpan<byte> data)
	{
		if (data.Length >= 26 && data[^18..^2].SequenceEqual("TRUEVISION-XFILE"u8))
		{
			return true;
		}

		if (data.Length < HeaderSize)
		{
			return false;
		}

		if (data[1] > 1 || data[2] is not (1 or 2 or 3 or 9 or 10 or 11))
		{
			return false;
		}

		int width = BinaryPrimitives.ReadUInt16LittleEndian(data[12..]);
		int height = BinaryPrimitives.ReadUInt16LittleEndian(data[14..]);
		return width > 0 && height > 0 && data[16] is 8 or 15 or 16 or 24 or 32;
	}

	/// <summary>Decodes a TGA to RGBA8.</summary>
	/// <param name="data">The complete file contents.</param>
	/// <returns>The decoded image.</returns>
	/// <exception cref="InvalidImageDataException">The data is not a well formed TGA this decoder supports.</exception>
	public static ImagePixels Decode(ReadOnlySpan<byte> data)
	{
		if (data.Length < HeaderSize)
		{
			throw new InvalidImageDataException("TGA is truncated before its header.");
		}

		int idLength = data[0];
		int colorMapType = data[1];
		int imageType = data[2];
		int colorMapLength = BinaryPrimitives.ReadUInt16LittleEndian(data[5..]);
		int colorMapEntryBits = data[7];
		int width = BinaryPrimitives.ReadUInt16LittleEndian(data[12..]);
		int height = BinaryPrimitives.ReadUInt16LittleEndian(data[14..]);
		int pixelBits = data[16];
		int descriptor = data[17];

		if (width <= 0 || height <= 0)
		{
			throw new InvalidImageDataException($"TGA has unusable dimensions {width}x{height}.");
		}

		bool runLengthEncoded = imageType >= 9;
		int baseType = imageType & 0x07;
		if (baseType is not (1 or 2 or 3))
		{
			throw new InvalidImageDataException($"TGA uses unsupported image type {imageType}.");
		}

		if (pixelBits is not (8 or 15 or 16 or 24 or 32))
		{
			throw new InvalidImageDataException($"TGA uses unsupported pixel depth {pixelBits}.");
		}

		int position = HeaderSize + idLength;
		byte[]? colorMap = null;
		if (colorMapType == 1)
		{
			int entryBytes = (colorMapEntryBits + 7) / 8;
			int mapBytes = colorMapLength * entryBytes;
			if (position + mapBytes > data.Length)
			{
				throw new InvalidImageDataException("TGA colour map runs past the end of the file.");
			}

			colorMap = new byte[colorMapLength * 4];
			for (int i = 0; i < colorMapLength; i++)
			{
				ReadPixel(data.Slice(position + (i * entryBytes), entryBytes), colorMapEntryBits, null, colorMap.AsSpan(i * 4), attributeAlpha: true);
			}

			position += mapBytes;
		}

		if (baseType == 1 && colorMap is null)
		{
			throw new InvalidImageDataException("Colour-mapped TGA is missing its colour map.");
		}

		int bytesPerPixel = (pixelBits + 7) / 8;
		ImagePixels image = new(width, height);
		Span<byte> pixels = image.Pixels;

		bool rightToLeft = (descriptor & 0x10) != 0;
		bool topToBottom = (descriptor & 0x20) != 0;

		// The low nibble counts attribute bits. Without any, the top bit of a 16-bit pixel is padding
		// rather than alpha, and reading it as alpha would make the whole image transparent.
		bool attributeAlpha = (descriptor & 0x0F) != 0;

		int pixelCount = width * height;
		int written = 0;
		Span<byte> rgba = stackalloc byte[4];

		while (written < pixelCount)
		{
			int runLength = 1;
			bool literal = true;

			if (runLengthEncoded)
			{
				if (position >= data.Length)
				{
					throw new InvalidImageDataException("TGA pixel data ended inside a run-length packet.");
				}

				byte packet = data[position++];
				runLength = (packet & 0x7F) + 1;
				literal = (packet & 0x80) == 0;
			}
			else
			{
				runLength = Math.Min(pixelCount - written, 0x4000);
			}

			if (written + runLength > pixelCount)
			{
				throw new InvalidImageDataException("TGA run-length packet overruns the image.");
			}

			for (int i = 0; i < runLength; i++)
			{
				// A repeated run reads its single source pixel once and replays it.
				if (literal || i == 0)
				{
					if (position + bytesPerPixel > data.Length)
					{
						throw new InvalidImageDataException("TGA pixel data ended before the image was complete.");
					}

					ReadPixel(data.Slice(position, bytesPerPixel), pixelBits, baseType == 1 ? colorMap : null, rgba, attributeAlpha);
					position += bytesPerPixel;
				}

				int index = written + i;
				int x = index % width;
				int y = index / width;
				int destinationX = rightToLeft ? width - 1 - x : x;
				int destinationY = topToBottom ? y : height - 1 - y;
				int destination = ((destinationY * width) + destinationX) * 4;
				rgba.CopyTo(pixels[destination..]);
			}

			written += runLength;
		}

		// Some writers store a 32-bit image with every alpha byte left at zero. Taken literally that
		// is an invisible image, so an entirely empty alpha channel is treated as opaque.
		if (pixelBits == 32 && !AnyAlpha(pixels))
		{
			for (int i = 3; i < pixels.Length; i += 4)
			{
				pixels[i] = 255;
			}
		}

		return image;
	}

	private static bool AnyAlpha(ReadOnlySpan<byte> pixels)
	{
		for (int i = 3; i < pixels.Length; i += 4)
		{
			if (pixels[i] != 0)
			{
				return true;
			}
		}

		return false;
	}

	private static void ReadPixel(ReadOnlySpan<byte> source, int bits, byte[]? colorMap, Span<byte> rgba, bool attributeAlpha)
	{
		switch (bits)
		{
			case 8 when colorMap is not null:
			{
				int index = source[0];
				if ((index * 4) + 3 >= colorMap.Length)
				{
					throw new InvalidImageDataException($"TGA references colour map entry {index}, which is past the end of a {colorMap.Length / 4} entry map.");
				}

				colorMap.AsSpan(index * 4, 4).CopyTo(rgba);
				break;
			}

			case 8:
				rgba[0] = source[0];
				rgba[1] = source[0];
				rgba[2] = source[0];
				rgba[3] = 255;
				break;

			case 15:
			case 16:
			{
				ushort value = BinaryPrimitives.ReadUInt16LittleEndian(source);
				rgba[0] = Expand5((value >> 10) & 0x1F);
				rgba[1] = Expand5((value >> 5) & 0x1F);
				rgba[2] = Expand5(value & 0x1F);

				rgba[3] = bits == 16 && attributeAlpha && (value & 0x8000) == 0 ? (byte)0 : (byte)255;
				break;
			}

			case 24:
				rgba[0] = source[2];
				rgba[1] = source[1];
				rgba[2] = source[0];
				rgba[3] = 255;
				break;

			default:
				rgba[0] = source[2];
				rgba[1] = source[1];
				rgba[2] = source[0];
				rgba[3] = source[3];
				break;
		}
	}

	private static byte Expand5(int value) => (byte)(value * 255 / 31);
}
