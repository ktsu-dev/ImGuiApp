// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Imaging;

using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

/// <summary>Writes RGBA8 pixels as a PNG file.</summary>
/// <remarks>
/// The smallest encoder that does the job: one <c>IHDR</c>, one <c>IDAT</c> and an <c>IEND</c>, eight-bit
/// RGBA, no interlacing, and the Sub filter on every row, which costs nothing to choose and compresses
/// photographs and flat colour alike far better than no filter at all. Nothing ancillary is written.
/// </remarks>
public static class PngEncoder
{
	private const byte ColorTypeRgba = 6;
	private const byte FilterSub = 1;

	private static readonly byte[] Signature = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];

	private static readonly uint[] CrcTable = BuildCrcTable();

	/// <summary>Encodes an image.</summary>
	/// <param name="pixels">The image, straight-alpha RGBA8.</param>
	/// <param name="options">How to encode it, or null for <see cref="PngEncoderOptions.Default"/>.</param>
	/// <returns>The PNG file's bytes.</returns>
	public static byte[] Encode(ImagePixels pixels, PngEncoderOptions? options = null)
	{
		using MemoryStream file = new();
		Encode(pixels, file, options);
		return file.ToArray();
	}

	/// <summary>Encodes an image to a stream.</summary>
	/// <param name="pixels">The image, straight-alpha RGBA8.</param>
	/// <param name="destination">The stream to write the PNG file to. It is not disposed.</param>
	/// <param name="options">How to encode it, or null for <see cref="PngEncoderOptions.Default"/>.</param>
	public static void Encode(ImagePixels pixels, Stream destination, PngEncoderOptions? options = null)
	{
		Ensure.NotNull(pixels);
		Ensure.NotNull(destination);
		options ??= PngEncoderOptions.Default;

		int stride = pixels.Width * 4;
		ReadOnlySpan<byte> source = pixels.ReadOnlyPixels;

		using MemoryStream compressed = new();
		using (ZLibStream zlib = new(compressed, options.CompressionLevel, leaveOpen: true))
		{
			byte[] line = new byte[stride];
			byte[] row = new byte[stride + 1];
			row[0] = FilterSub;
			for (int y = 0; y < pixels.Height; y++)
			{
				source.Slice(y * stride, stride).CopyTo(line);
				if (options.ClearTransparentPixels)
				{
					ClearTransparent(line);
				}

				for (int x = 0; x < stride; x++)
				{
					row[x + 1] = x < 4 ? line[x] : (byte)(line[x] - line[x - 4]);
				}

				zlib.Write(row);
			}
		}

		using MemoryStream file = new();
		file.Write(Signature);

		byte[] header = new byte[13];
		BinaryPrimitives.WriteInt32BigEndian(header, pixels.Width);
		BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), pixels.Height);
		header[8] = 8;
		header[9] = ColorTypeRgba;
		WriteChunk(file, "IHDR", header);
		WriteChunk(file, "IDAT", compressed.GetBuffer().AsSpan(0, (int)compressed.Length));
		WriteChunk(file, "IEND", []);
		file.Position = 0;
		file.CopyTo(destination);
	}

	/// <summary>Encodes an image and writes it to a file, replacing any file already there.</summary>
	/// <param name="pixels">The image, straight-alpha RGBA8.</param>
	/// <param name="path">The file to write.</param>
	/// <param name="options">How to encode it, or null for <see cref="PngEncoderOptions.Default"/>.</param>
	public static void Save(ImagePixels pixels, string path, PngEncoderOptions? options = null)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
		byte[] bytes = Encode(pixels, options);
		File.WriteAllBytes(path, bytes);
	}

	private static void ClearTransparent(Span<byte> line)
	{
		for (int i = 0; i < line.Length; i += 4)
		{
			if (line[i + 3] == 0)
			{
				line[i] = 0;
				line[i + 1] = 0;
				line[i + 2] = 0;
			}
		}
	}

	private static void WriteChunk(MemoryStream file, string type, ReadOnlySpan<byte> data)
	{
		Span<byte> word = stackalloc byte[4];
		BinaryPrimitives.WriteInt32BigEndian(word, data.Length);
		file.Write(word);

		Span<byte> typeBytes = stackalloc byte[4];
		_ = Encoding.ASCII.GetBytes(type, typeBytes);
		file.Write(typeBytes);
		file.Write(data);

		uint crc = Crc(Crc(uint.MaxValue, typeBytes), data) ^ uint.MaxValue;
		BinaryPrimitives.WriteUInt32BigEndian(word, crc);
		file.Write(word);
	}

	private static uint Crc(uint crc, ReadOnlySpan<byte> data)
	{
		foreach (byte b in data)
		{
			crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
		}

		return crc;
	}

	private static uint[] BuildCrcTable()
	{
		uint[] table = new uint[256];
		for (uint n = 0; n < 256; n++)
		{
			uint c = n;
			for (int k = 0; k < 8; k++)
			{
				c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
			}

			table[n] = c;
		}

		return table;
	}
}
