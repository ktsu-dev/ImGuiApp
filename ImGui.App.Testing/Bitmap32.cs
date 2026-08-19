// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;

/// <summary>A straight-alpha RGBA8 color.</summary>
/// <param name="R">Red channel.</param>
/// <param name="G">Green channel.</param>
/// <param name="B">Blue channel.</param>
/// <param name="A">Alpha channel, where zero is fully transparent.</param>
public readonly record struct Rgba32(byte R, byte G, byte B, byte A);

/// <summary>
/// A tightly packed RGBA8 pixel buffer. Serves as the render target for the software rasterizer and
/// as the surface a test measures. Encodes to PNG through <see cref="ZLibStream"/>, so the package
/// needs no imaging dependency and avoids the licensing constraint recorded in issue #230.
/// </summary>
public sealed class Bitmap32
{
	private readonly byte[] pixels;

	/// <summary>Initializes a new instance of the <see cref="Bitmap32"/> class.</summary>
	/// <param name="width">Width in pixels. Must be positive.</param>
	/// <param name="height">Height in pixels. Must be positive.</param>
	public Bitmap32(int width, int height)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

		Width = width;
		Height = height;
		pixels = new byte[width * height * 4];
	}

	/// <summary>Gets the width in pixels.</summary>
	public int Width { get; }

	/// <summary>Gets the height in pixels.</summary>
	public int Height { get; }

	/// <summary>Gets the raw RGBA8 bytes, four per pixel, row major from the top left.</summary>
	public Span<byte> Pixels => pixels;

	/// <summary>Reads one pixel.</summary>
	/// <param name="x">Column.</param>
	/// <param name="y">Row.</param>
	/// <returns>The color at that position.</returns>
	public Rgba32 GetPixel(int x, int y)
	{
		int i = Offset(x, y);
		return new Rgba32(pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3]);
	}

	/// <summary>Writes one pixel, replacing whatever was there.</summary>
	/// <param name="x">Column.</param>
	/// <param name="y">Row.</param>
	/// <param name="color">The color to write.</param>
	public void SetPixel(int x, int y, Rgba32 color)
	{
		int i = Offset(x, y);
		pixels[i] = color.R;
		pixels[i + 1] = color.G;
		pixels[i + 2] = color.B;
		pixels[i + 3] = color.A;
	}

	/// <summary>Fills every pixel with one color.</summary>
	/// <param name="color">The fill color.</param>
	public void Clear(Rgba32 color)
	{
		for (int i = 0; i < pixels.Length; i += 4)
		{
			pixels[i] = color.R;
			pixels[i + 1] = color.G;
			pixels[i + 2] = color.B;
			pixels[i + 3] = color.A;
		}
	}

	/// <summary>Writes the buffer to disk as a PNG.</summary>
	/// <param name="path">Destination file path.</param>
	public void SavePng(string path) => File.WriteAllBytes(path, EncodePng());

	/// <summary>Encodes the buffer as a PNG byte stream.</summary>
	/// <returns>A complete PNG file in memory.</returns>
	public byte[] EncodePng()
	{
		// Every scanline is prefixed with filter type zero, which keeps the encoder small at the
		// cost of compression ratio. Diagnostic artifacts do not need to be small.
		int stride = Width * 4;
		byte[] raw = new byte[Height * (stride + 1)];
		for (int y = 0; y < Height; y++)
		{
			int destination = y * (stride + 1);
			raw[destination] = 0;
			Array.Copy(pixels, y * stride, raw, destination + 1, stride);
		}

		using MemoryStream compressed = new();
		using (ZLibStream deflate = new(compressed, CompressionLevel.Fastest, leaveOpen: true))
		{
			deflate.Write(raw, 0, raw.Length);
		}

		using MemoryStream png = new();
		png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

		byte[] header = new byte[13];
		BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0), Width);
		BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), Height);
		header[8] = 8;  // bit depth
		header[9] = 6;  // color type: truecolor with alpha
		header[10] = 0; // compression: deflate
		header[11] = 0; // filter: adaptive
		header[12] = 0; // interlace: none

		WriteChunk(png, "IHDR", header);
		WriteChunk(png, "IDAT", compressed.ToArray());
		WriteChunk(png, "IEND", []);

		return png.ToArray();
	}

	private int Offset(int x, int y)
	{
		if ((uint)x >= (uint)Width)
		{
			throw new ArgumentOutOfRangeException(nameof(x));
		}

		if ((uint)y >= (uint)Height)
		{
			throw new ArgumentOutOfRangeException(nameof(y));
		}

		return ((y * Width) + x) * 4;
	}

	private static void WriteChunk(Stream stream, string type, byte[] data)
	{
		Span<byte> length = stackalloc byte[4];
		BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
		stream.Write(length);

		// The CRC covers the type and the data together, so they are laid out contiguously.
		byte[] typeAndData = new byte[4 + data.Length];
		for (int i = 0; i < 4; i++)
		{
			typeAndData[i] = (byte)type[i];
		}

		Array.Copy(data, 0, typeAndData, 4, data.Length);
		stream.Write(typeAndData, 0, typeAndData.Length);

		Span<byte> crc = stackalloc byte[4];
		BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32(typeAndData));
		stream.Write(crc);
	}

	private static uint Crc32(ReadOnlySpan<byte> data)
	{
		uint crc = 0xFFFFFFFFu;
		foreach (byte value in data)
		{
			crc ^= value;
			for (int bit = 0; bit < 8; bit++)
			{
				uint mask = (uint)-(int)(crc & 1);
				crc = (crc >> 1) ^ (0xEDB88320u & mask);
			}
		}

		return crc ^ 0xFFFFFFFFu;
	}
}
