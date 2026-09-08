// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests.Images;

using System;
using ktsu.ImGui.App.Images;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers the pixel buffer's own behaviour: construction, bounds, copying and cropping.
/// </summary>
[TestClass]
public class ImagePixelsTests
{
	[TestMethod]
	public void Constructor_AllocatesFourBytesPerPixel()
	{
		ImagePixels image = new(3, 5);

		Assert.AreEqual(3, image.Width);
		Assert.AreEqual(5, image.Height);
		Assert.AreEqual(60, image.ByteLength);
	}

	[TestMethod]
	public void Constructor_WithMismatchedBuffer_Throws() =>
		Assert.ThrowsExactly<ArgumentException>(() => new ImagePixels(2, 2, new byte[15]));

	[TestMethod]
	public void Constructor_WithZeroDimension_Throws() =>
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ImagePixels(0, 4));

	[TestMethod]
	public void GetPixel_OutsideTheImage_Throws()
	{
		ImagePixels image = new(2, 2);

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => image.GetPixel(2, 0));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => image.GetPixel(0, 2));
	}

	[TestMethod]
	public void SetPixel_ThenGetPixel_RoundTrips()
	{
		ImagePixels image = new(2, 2);

		image.SetPixel(1, 1, 10, 20, 30, 40);

		Assert.AreEqual((10, 20, 30, (byte)40), image.GetPixel(1, 1));
	}

	[TestMethod]
	public void Clone_ProducesAnIndependentCopy()
	{
		ImagePixels image = new(2, 2);
		image.SetPixel(0, 0, 1, 2, 3, 4);

		ImagePixels copy = image.Clone();
		image.SetPixel(0, 0, 9, 9, 9, 9);

		Assert.AreEqual((1, 2, 3, (byte)4), copy.GetPixel(0, 0));
	}

	[TestMethod]
	public void CopyPixelDataTo_ShorterDestination_Throws()
	{
		ImagePixels image = new(2, 2);

		Assert.ThrowsExactly<ArgumentException>(() => image.CopyPixelDataTo(new byte[15]));
	}

	[TestMethod]
	public void CopyPixelDataTo_LongerDestination_CopiesTheImage()
	{
		ImagePixels image = new(1, 1);
		image.SetPixel(0, 0, 1, 2, 3, 4);
		byte[] destination = new byte[8];

		image.CopyPixelDataTo(destination);

		CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 0, 0, 0, 0 }, destination);
	}

	[TestMethod]
	public void Crop_TakesTheRequestedRectangle()
	{
		ImagePixels image = new(4, 4);
		for (int y = 0; y < 4; y++)
		{
			for (int x = 0; x < 4; x++)
			{
				image.SetPixel(x, y, (byte)x, (byte)y, 0, 255);
			}
		}

		ImagePixels cropped = image.Crop(1, 2, 2, 2);

		Assert.AreEqual(2, cropped.Width);
		Assert.AreEqual(2, cropped.Height);
		Assert.AreEqual((1, 2, 0, (byte)255), cropped.GetPixel(0, 0));
		Assert.AreEqual((2, 3, 0, (byte)255), cropped.GetPixel(1, 1));
	}

	[TestMethod]
	public void Crop_PastTheEdge_Throws()
	{
		ImagePixels image = new(4, 4);

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => image.Crop(2, 0, 3, 1));
	}

	[TestMethod]
	public void CropToSquare_TakesTheCentredSquare()
	{
		ImagePixels image = new(9, 5);
		for (int x = 0; x < 9; x++)
		{
			image.SetPixel(x, 0, (byte)x, 0, 0, 255);
		}

		ImagePixels square = image.CropToSquare();

		Assert.AreEqual(5, square.Width);
		Assert.AreEqual(5, square.Height);
		Assert.AreEqual(2, square.GetPixel(0, 0).R);
	}

	[TestMethod]
	public void CropToSquare_OnASquareImage_ReturnsAnEquivalentCopy()
	{
		ImagePixels image = new(4, 4);
		image.SetPixel(3, 3, 7, 7, 7, 7);

		ImagePixels square = image.CropToSquare();

		Assert.AreEqual(4, square.Width);
		Assert.AreEqual((7, 7, 7, (byte)7), square.GetPixel(3, 3));
	}
}
