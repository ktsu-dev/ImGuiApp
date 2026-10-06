// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Images;

using System;

/// <summary>
/// Converts caller-owned pixel buffers in any <see cref="PixelLayout"/>, with or without row padding,
/// into the tightly packed RGBA8 that every renderer backend uploads.
/// </summary>
internal static class PixelConverter
{
	/// <summary>Gets the number of bytes one pixel occupies in <paramref name="layout"/>.</summary>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="layout"/> is not a defined value.</exception>
	internal static int BytesPerPixel(PixelLayout layout) => layout switch
	{
		PixelLayout.Rgba8 or PixelLayout.Bgra8 => 4,
		PixelLayout.Rgb8 or PixelLayout.Bgr8 => 3,
		PixelLayout.Gray8 => 1,
		_ => throw new ArgumentOutOfRangeException(nameof(layout), layout, "Unknown pixel layout."),
	};

	/// <summary>
	/// Checks a source buffer against its declared geometry and resolves its row stride.
	/// </summary>
	/// <param name="source">The source pixels.</param>
	/// <param name="width">Width in pixels.</param>
	/// <param name="height">Height in pixels.</param>
	/// <param name="layout">The source's pixel layout.</param>
	/// <param name="rowStride">Bytes from the start of one row to the start of the next, or 0 for tightly packed rows.</param>
	/// <returns>The effective row stride in bytes.</returns>
	/// <remarks>
	/// The last row need not be padded out to the full stride: a buffer is long enough when it reaches
	/// the end of the last row's pixels. That is how a sub-region of a larger image arrives, where the
	/// padding after the last row belongs to pixels outside the region, or to nothing at all.
	/// </remarks>
	/// <exception cref="ArgumentOutOfRangeException">
	/// A dimension is not positive, the layout is unknown, or the stride is negative or shorter than a row of pixels.
	/// </exception>
	/// <exception cref="ArgumentException"><paramref name="source"/> is too short for the geometry.</exception>
	internal static int Validate(ReadOnlySpan<byte> source, int width, int height, PixelLayout layout, int rowStride)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
		ArgumentOutOfRangeException.ThrowIfNegative(rowStride);

		int rowBytes = checked(width * BytesPerPixel(layout));
		int stride = rowStride == 0 ? rowBytes : rowStride;
		if (stride < rowBytes)
		{
			throw new ArgumentOutOfRangeException(nameof(rowStride), rowStride, $"A row of {width} {layout} pixels needs at least {rowBytes} bytes.");
		}

		long required = ((long)(height - 1) * stride) + rowBytes;
		if (source.Length < required)
		{
			throw new ArgumentException($"Expected at least {required} bytes for {width}x{height} {layout} pixels with a {stride}-byte row stride but got {source.Length}.", nameof(source));
		}

		return stride;
	}

	/// <summary>
	/// Writes <paramref name="source"/> into <paramref name="destination"/> as tightly packed RGBA8.
	/// </summary>
	/// <param name="source">The source pixels, already checked by <see cref="Validate"/>.</param>
	/// <param name="width">Width in pixels.</param>
	/// <param name="height">Height in pixels.</param>
	/// <param name="layout">The source's pixel layout.</param>
	/// <param name="stride">The effective row stride returned by <see cref="Validate"/>.</param>
	/// <param name="destination">At least <paramref name="width"/> * <paramref name="height"/> * 4 bytes.</param>
	internal static void ToRgba8(ReadOnlySpan<byte> source, int width, int height, PixelLayout layout, int stride, Span<byte> destination)
	{
		int bytesPerPixel = BytesPerPixel(layout);
		int destinationRowBytes = width * 4;
		for (int y = 0; y < height; y++)
		{
			ReadOnlySpan<byte> sourceRow = source.Slice(y * stride, width * bytesPerPixel);
			Span<byte> destinationRow = destination.Slice(y * destinationRowBytes, destinationRowBytes);
			ConvertRow(sourceRow, destinationRow, layout);
		}
	}

	private static void ConvertRow(ReadOnlySpan<byte> source, Span<byte> destination, PixelLayout layout)
	{
		switch (layout)
		{
			case PixelLayout.Rgba8:
				source.CopyTo(destination);
				break;

			case PixelLayout.Bgra8:
				for (int s = 0; s < source.Length; s += 4)
				{
					destination[s] = source[s + 2];
					destination[s + 1] = source[s + 1];
					destination[s + 2] = source[s];
					destination[s + 3] = source[s + 3];
				}

				break;

			case PixelLayout.Rgb8:
				for (int s = 0, d = 0; s < source.Length; s += 3, d += 4)
				{
					destination[d] = source[s];
					destination[d + 1] = source[s + 1];
					destination[d + 2] = source[s + 2];
					destination[d + 3] = byte.MaxValue;
				}

				break;

			case PixelLayout.Bgr8:
				for (int s = 0, d = 0; s < source.Length; s += 3, d += 4)
				{
					destination[d] = source[s + 2];
					destination[d + 1] = source[s + 1];
					destination[d + 2] = source[s];
					destination[d + 3] = byte.MaxValue;
				}

				break;

			case PixelLayout.Gray8:
				for (int s = 0, d = 0; s < source.Length; s++, d += 4)
				{
					byte luminance = source[s];
					destination[d] = luminance;
					destination[d + 1] = luminance;
					destination[d + 2] = luminance;
					destination[d + 3] = byte.MaxValue;
				}

				break;

			default:
				throw new ArgumentOutOfRangeException(nameof(layout), layout, "Unknown pixel layout.");
		}
	}
}
