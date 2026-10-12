// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Clipboard;

using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

using ktsu.ImGui.App.Images;

/// <summary>Writes RGBA8 pixels as a PNG file, the form a clipboard image travels in.</summary>
/// <remarks>
/// The smallest encoder that does the job: one <c>IHDR</c>, one <c>IDAT</c> and an <c>IEND</c>, eight-bit
/// RGBA, no interlacing, and the Sub filter on every row, which costs nothing to choose and compresses
/// photographs and flat colour alike far better than no filter at all. Nothing ancillary is written.
/// </remarks>
internal static class PngEncoder
{
	private const byte ColorTypeRgba = 6;
	private const byte FilterSub = 1;

	private static readonly byte[] Signature = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];

	private static readonly uint[] CrcTable = BuildCrcTable();

	/// <summary>Encodes an image.</summary>
	/// <param name="pixels">The image, straight-alpha RGBA8.</param>
	/// <returns>The PNG file's bytes.</returns>
	public static byte[] Encode(ImagePixels pixels)
	{
		Ensure.NotNull(pixels);
		int stride = pixels.Width * 4;
		ReadOnlySpan<byte> source = pixels.ReadOnlyPixels;

		using MemoryStream compressed = new();
		using (ZLibStream zlib = new(compressed, CompressionLevel.Optimal, leaveOpen: true))
		{
			byte[] row = new byte[stride + 1];
			row[0] = FilterSub;
			for (int y = 0; y < pixels.Height; y++)
			{
				ReadOnlySpan<byte> line = source.Slice(y * stride, stride);
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
		return file.ToArray();
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
