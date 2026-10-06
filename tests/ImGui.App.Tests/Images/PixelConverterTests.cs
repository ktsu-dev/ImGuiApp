// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests.Images;

using System;
using ktsu.ImGui.App.Images;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers the conversion from caller-owned pixel buffers to the RGBA8 every backend uploads.
/// </summary>
[TestClass]
public class PixelConverterTests
{
	// Two pixels, (10, 20, 30, 40) then (50, 60, 70, 80), written in each layout's own byte order.
	private static readonly byte[] ExpectedRgba = [10, 20, 30, 40, 50, 60, 70, 80];

	private static byte[] Convert(byte[] source, int width, int height, PixelLayout layout, int rowStride = 0)
	{
		int stride = PixelConverter.Validate(source, width, height, layout, rowStride);
		byte[] destination = new byte[width * height * 4];
		PixelConverter.ToRgba8(source, width, height, layout, stride, destination);
		return destination;
	}

	[TestMethod]
	public void Rgba8_IsCopiedUnchanged()
	{
		Assert.AreSequenceEqual(ExpectedRgba, Convert([10, 20, 30, 40, 50, 60, 70, 80], 2, 1, PixelLayout.Rgba8));
	}

	[TestMethod]
	public void Bgra8_SwapsRedAndBlueAndKeepsAlpha()
	{
		Assert.AreSequenceEqual(ExpectedRgba, Convert([30, 20, 10, 40, 70, 60, 50, 80], 2, 1, PixelLayout.Bgra8));
	}

	[TestMethod]
	public void Rgb8_GainsOpaqueAlpha()
	{
		Assert.AreSequenceEqual(new byte[] { 10, 20, 30, 255, 50, 60, 70, 255 }, Convert([10, 20, 30, 50, 60, 70], 2, 1, PixelLayout.Rgb8));
	}

	[TestMethod]
	public void Bgr8_SwapsRedAndBlueAndGainsOpaqueAlpha()
	{
		Assert.AreSequenceEqual(new byte[] { 10, 20, 30, 255, 50, 60, 70, 255 }, Convert([30, 20, 10, 70, 60, 50], 2, 1, PixelLayout.Bgr8));
	}

	[TestMethod]
	public void Gray8_FillsEveryColourChannelAndGainsOpaqueAlpha()
	{
		Assert.AreSequenceEqual(new byte[] { 7, 7, 7, 255, 200, 200, 200, 255 }, Convert([7, 200], 2, 1, PixelLayout.Gray8));
	}

	[TestMethod]
	public void RowStride_SkipsThePaddingAfterEachRow()
	{
		// Two rows of one Bgr8 pixel, each padded to four bytes as OpenCV and Windows DIBs pad rows.
		byte[] source = [3, 2, 1, 0xEE, 6, 5, 4, 0xEE];

		byte[] rgba = Convert(source, 1, 2, PixelLayout.Bgr8, rowStride: 4);

		Assert.AreSequenceEqual(new byte[] { 1, 2, 3, 255, 4, 5, 6, 255 }, rgba);
	}

	[TestMethod]
	public void RowStride_DoesNotRequirePaddingAfterTheLastRow()
	{
		// A sub-region of a larger image ends at its last pixel, not at the end of a padded row.
		byte[] source = [1, 0xEE, 0xEE, 2];

		byte[] rgba = Convert(source, 1, 2, PixelLayout.Gray8, rowStride: 3);

		Assert.AreSequenceEqual(new byte[] { 1, 1, 1, 255, 2, 2, 2, 255 }, rgba);
	}

	[TestMethod]
	public void Validate_ResolvesZeroStrideToATightRow()
	{
		Assert.AreEqual(9, PixelConverter.Validate(new byte[18], 3, 2, PixelLayout.Rgb8, rowStride: 0));
	}

	[TestMethod]
	public void Validate_RejectsAStrideShorterThanARow()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => PixelConverter.Validate(new byte[64], 4, 2, PixelLayout.Rgba8, rowStride: 15));
	}

	[TestMethod]
	public void Validate_RejectsANegativeStride()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => PixelConverter.Validate(new byte[64], 4, 2, PixelLayout.Rgba8, rowStride: -16));
	}

	[TestMethod]
	public void Validate_RejectsABufferShorterThanTheGeometry()
	{
		// Two padded rows need 8 + 5 bytes: the full stride for the first, the pixels of the second.
		Assert.ThrowsExactly<ArgumentException>(() => PixelConverter.Validate(new byte[12], 5, 2, PixelLayout.Gray8, rowStride: 8));
	}

	[TestMethod]
	public void Validate_RejectsNonPositiveDimensions()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => PixelConverter.Validate(new byte[4], 0, 1, PixelLayout.Rgba8, 0));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => PixelConverter.Validate(new byte[4], 1, -1, PixelLayout.Rgba8, 0));
	}

	[TestMethod]
	public void Validate_RejectsAnUndefinedLayout()
	{
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => PixelConverter.Validate(new byte[4], 1, 1, (PixelLayout)99, 0));
	}
}
