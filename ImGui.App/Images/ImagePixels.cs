// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Images;

using System;

/// <summary>
/// A decoded image held as tightly packed, straight-alpha RGBA8 pixels: four bytes per pixel, row
/// major from the top left, with no row padding.
/// </summary>
/// <remarks>
/// This is the currency type of <see cref="ImageDecoder"/> and the replacement for the third-party
/// image type the texture cache used to be written against. It is deliberately minimal — it owns a
/// buffer and knows its dimensions — because the only things this library does with a decoded image
/// are upload it to the GPU and, for window icons, crop and scale it.
/// </remarks>
public sealed class ImagePixels
{
	private readonly byte[] pixels;

	/// <summary>Initializes a new fully transparent image.</summary>
	/// <param name="width">Width in pixels. Must be positive.</param>
	/// <param name="height">Height in pixels. Must be positive.</param>
	public ImagePixels(int width, int height)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

		Width = width;
		Height = height;
		pixels = new byte[checked(width * height * 4)];
	}

	/// <summary>Initializes an image that takes ownership of an existing RGBA8 buffer.</summary>
	/// <param name="width">Width in pixels. Must be positive.</param>
	/// <param name="height">Height in pixels. Must be positive.</param>
	/// <param name="pixels">The RGBA8 buffer. Must be exactly <paramref name="width"/> * <paramref name="height"/> * 4 bytes long.</param>
	/// <exception cref="ArgumentException">The buffer length does not match the dimensions.</exception>
	public ImagePixels(int width, int height, byte[] pixels)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
		ArgumentNullException.ThrowIfNull(pixels);

		int expected = checked(width * height * 4);
		if (pixels.Length != expected)
		{
			throw new ArgumentException($"Expected a buffer of {expected} bytes for a {width}x{height} RGBA8 image, but got {pixels.Length}.", nameof(pixels));
		}

		Width = width;
		Height = height;
		this.pixels = pixels;
	}

	/// <summary>Gets the width in pixels.</summary>
	public int Width { get; }

	/// <summary>Gets the height in pixels.</summary>
	public int Height { get; }

	/// <summary>Gets the number of bytes of pixel data, which is <see cref="Width"/> * <see cref="Height"/> * 4.</summary>
	public int ByteLength => pixels.Length;

	/// <summary>Gets the raw RGBA8 bytes, four per pixel, row major from the top left.</summary>
	public Span<byte> Pixels => pixels;

	/// <summary>Gets the raw RGBA8 bytes as a read-only span.</summary>
	public ReadOnlySpan<byte> ReadOnlyPixels => pixels;

	/// <summary>Gets the underlying buffer without copying.</summary>
	/// <returns>The buffer this image owns.</returns>
	/// <remarks>Exposed for zero-copy uploads; mutating it mutates the image.</remarks>
	public byte[] GetBuffer() => pixels;

	/// <summary>Copies the pixel data into a destination span.</summary>
	/// <param name="destination">The destination, which must be at least <see cref="ByteLength"/> bytes.</param>
	/// <exception cref="ArgumentException">The destination is too small.</exception>
	public void CopyPixelDataTo(Span<byte> destination)
	{
		if (destination.Length < pixels.Length)
		{
			throw new ArgumentException($"Destination is {destination.Length} bytes but the image needs {pixels.Length}.", nameof(destination));
		}

		pixels.AsSpan().CopyTo(destination);
	}

	/// <summary>Creates an independent copy of this image.</summary>
	/// <returns>A new image with its own copy of the pixel data.</returns>
	public ImagePixels Clone() => new(Width, Height, (byte[])pixels.Clone());

	/// <summary>Reads one pixel.</summary>
	/// <param name="x">Column, from the left.</param>
	/// <param name="y">Row, from the top.</param>
	/// <returns>The red, green, blue and alpha channels at that position.</returns>
	public (byte R, byte G, byte B, byte A) GetPixel(int x, int y)
	{
		int i = Offset(x, y);
		return (pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3]);
	}

	/// <summary>Writes one pixel.</summary>
	/// <param name="x">Column, from the left.</param>
	/// <param name="y">Row, from the top.</param>
	/// <param name="r">Red channel.</param>
	/// <param name="g">Green channel.</param>
	/// <param name="b">Blue channel.</param>
	/// <param name="a">Alpha channel.</param>
	public void SetPixel(int x, int y, byte r, byte g, byte b, byte a)
	{
		int i = Offset(x, y);
		pixels[i] = r;
		pixels[i + 1] = g;
		pixels[i + 2] = b;
		pixels[i + 3] = a;
	}

	/// <summary>Crops a rectangle out of this image.</summary>
	/// <param name="x">Left edge of the rectangle.</param>
	/// <param name="y">Top edge of the rectangle.</param>
	/// <param name="width">Width of the rectangle. Must be positive.</param>
	/// <param name="height">Height of the rectangle. Must be positive.</param>
	/// <returns>A new image holding the cropped region.</returns>
	/// <exception cref="ArgumentOutOfRangeException">The rectangle falls outside this image.</exception>
	public ImagePixels Crop(int x, int y, int width, int height)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(x);
		ArgumentOutOfRangeException.ThrowIfNegative(y);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(x + width, Width, nameof(width));
		ArgumentOutOfRangeException.ThrowIfGreaterThan(y + height, Height, nameof(height));

		ImagePixels result = new(width, height);
		Span<byte> destination = result.Pixels;
		for (int row = 0; row < height; row++)
		{
			ReadOnlySpan<byte> source = pixels.AsSpan((((y + row) * Width) + x) * 4, width * 4);
			source.CopyTo(destination[(row * width * 4)..]);
		}

		return result;
	}

	/// <summary>Crops the largest centred square out of this image.</summary>
	/// <returns>A new square image, or a copy of this one when it is already square.</returns>
	public ImagePixels CropToSquare()
	{
		int size = Math.Min(Width, Height);
		return Crop((Width - size) / 2, (Height - size) / 2, size, size);
	}

	/// <summary>Scales this image to a new size.</summary>
	/// <param name="width">Target width in pixels. Must be positive.</param>
	/// <param name="height">Target height in pixels. Must be positive.</param>
	/// <returns>A new image at the requested size.</returns>
	/// <remarks>Uses the Lanczos-3 filter from <see cref="ImageResampler"/>, which is area weighted when downscaling.</remarks>
	public ImagePixels Resize(int width, int height) => ImageResampler.Resize(this, width, height);

	private int Offset(int x, int y)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(x);
		ArgumentOutOfRangeException.ThrowIfNegative(y);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, Width);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, Height);
		return ((y * Width) + x) * 4;
	}
}
