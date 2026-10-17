// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Images;

using System;
using System.Globalization;
using System.IO;
using System.Text;

/// <summary>
/// Decodes image files to <see cref="ImagePixels"/>, choosing a decoder from the data itself.
/// </summary>
/// <remarks>
/// <para>
/// This is the whole of the imaging this library needs: turn a file on disk into RGBA8 bytes to hand
/// the GPU, and scale those bytes for window icons. It is implemented here rather than taken from a
/// package so that the licence terms of an imaging library cannot constrain consumers of
/// <c>ktsu.ImGui.App</c>; see the discussion in issue #354 and the earlier breakage in issue #230.
/// </para>
/// <para>
/// PNG, JPEG, BMP and TGA are supported. The format is identified from the file's own bytes, not its
/// extension, so a mislabelled file still loads. Anything else raises
/// <see cref="InvalidImageDataException"/> naming what was found.
/// </para>
/// </remarks>
public static class ImageDecoder
{
	/// <summary>Identifies the format of an image from its leading bytes.</summary>
	/// <param name="data">The file contents, or at least its first few dozen bytes.</param>
	/// <returns>The detected format, or <see cref="ImageFormat.Unknown"/>.</returns>
	public static ImageFormat Identify(ReadOnlySpan<byte> data) =>
		PngDecoder.IsPng(data) ? ImageFormat.Png
		: JpegDecoder.IsJpeg(data) ? ImageFormat.Jpeg
		: BmpDecoder.IsBmp(data) ? ImageFormat.Bmp

		// TGA has no signature at the front of the file, so it is only reached once the formats that
		// do have one are ruled out.
		: TgaDecoder.IsTga(data) ? ImageFormat.Tga
		: ImageFormat.Unknown;

	/// <summary>Decodes image data to RGBA8.</summary>
	/// <param name="data">The complete file contents.</param>
	/// <returns>The decoded image.</returns>
	/// <exception cref="InvalidImageDataException">The data is empty, of an unrecognised format, or malformed.</exception>
	public static ImagePixels Decode(ReadOnlySpan<byte> data)
	{
		if (data.IsEmpty)
		{
			throw new InvalidImageDataException("Cannot decode an empty buffer.");
		}

		ImageFormat format = Identify(data);
		try
		{
			return format switch
			{
				ImageFormat.Png => PngDecoder.Decode(data),
				ImageFormat.Jpeg => JpegDecoder.Decode(data),
				ImageFormat.Bmp => BmpDecoder.Decode(data),
				ImageFormat.Tga => TgaDecoder.Decode(data),
				_ => throw new InvalidImageDataException($"Unrecognised image format; the file starts with {Describe(data)}. Supported formats are PNG, JPEG, BMP and TGA."),
			};
		}
		catch (Exception ex) when (ex is OverflowException or ArgumentOutOfRangeException or IndexOutOfRangeException)
		{
			// A backstop, not the plan: the decoders validate sizes before using them, but an arithmetic
			// or bounds failure that slips past them is still malformed input, and callers rely on the
			// documented exception to skip one bad file rather than abort.
			throw new InvalidImageDataException($"{format} data is malformed: {ex.Message}", ex);
		}
	}

	/// <summary>
	/// Throws unless a <paramref name="width"/> by <paramref name="height"/> RGBA8 image fits in one array.
	/// Decoders call this with header dimensions before allocating anything sized from them, so a corrupt
	/// or crafted header fails here rather than as an overflow partway through a huge allocation.
	/// </summary>
	/// <param name="width">The declared width. Must be positive.</param>
	/// <param name="height">The declared height. Must be positive.</param>
	/// <param name="format">The format name for the message.</param>
	/// <exception cref="InvalidImageDataException">The decoded image would not fit in memory.</exception>
	internal static void EnsurePixelBufferFits(long width, long height, string format)
	{
		if (width > Array.MaxLength / 4 / height)
		{
			throw new InvalidImageDataException($"{format} dimensions {width}x{height} are too large to decode.");
		}
	}

	/// <summary>Reads and decodes an image file.</summary>
	/// <param name="path">Path to the image file.</param>
	/// <returns>The decoded image.</returns>
	/// <exception cref="InvalidImageDataException">The file is of an unrecognised format, or malformed.</exception>
	public static ImagePixels Load(string path)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
		return Decode(File.ReadAllBytes(path));
	}

	/// <summary>Reads a stream to its end and decodes it as an image.</summary>
	/// <param name="stream">The stream to read. It is read to the end but not disposed.</param>
	/// <returns>The decoded image.</returns>
	/// <exception cref="InvalidImageDataException">The stream is of an unrecognised format, or malformed.</exception>
	public static ImagePixels Load(Stream stream)
	{
		Ensure.NotNull(stream);

		using MemoryStream buffer = new();
		stream.CopyTo(buffer);
		return Decode(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
	}

	private static string Describe(ReadOnlySpan<byte> data)
	{
		int count = Math.Min(4, data.Length);
		StringBuilder description = new(count * 3);
		for (int i = 0; i < count; i++)
		{
			if (i > 0)
			{
				description.Append(' ');
			}

			description.Append(data[i].ToString("X2", CultureInfo.InvariantCulture));
		}

		return description.ToString();
	}
}
