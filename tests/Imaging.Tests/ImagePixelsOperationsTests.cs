// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.Imaging.Tests;

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers the whole-image operations a consumer composes into a pipeline: filling, row access and
/// padding onto a larger canvas.
/// </summary>
[TestClass]
public class ImagePixelsOperationsTests
{
	[TestMethod]
	public void Fill_SetsEveryPixel()
	{
		ImagePixels image = new(3, 2);

		image.Fill(1, 2, 3, 4);

		for (int y = 0; y < 2; y++)
		{
			for (int x = 0; x < 3; x++)
			{
				Assert.AreEqual(((byte)1, (byte)2, (byte)3, (byte)4), image.GetPixel(x, y));
			}
		}
	}

	[TestMethod]
	public void GetRow_IsAViewOverThatRow()
	{
		ImagePixels image = new(2, 3);

		Span<byte> row = image.GetRow(1);
		row[4] = 9;
		row[7] = 8;

		Assert.AreEqual(8, row.Length);
		Assert.AreEqual(((byte)9, (byte)0, (byte)0, (byte)8), image.GetPixel(1, 1));
		Assert.AreEqual(((byte)0, (byte)0, (byte)0, (byte)0), image.GetPixel(1, 0));
	}

	[TestMethod]
	public void GetRow_OutsideTheImage_Throws()
	{
		ImagePixels image = new(2, 2);

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => image.GetRow(2));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => image.GetRow(-1));
	}

	[TestMethod]
	public void Pad_CentresTheImageUnscaled()
	{
		ImagePixels image = new(2, 1);
		image.SetPixel(0, 0, 10, 0, 0, 255);
		image.SetPixel(1, 0, 20, 0, 0, 255);

		ImagePixels padded = image.Pad(4, 3, 0, 0, 0, 0);

		Assert.AreEqual(4, padded.Width);
		Assert.AreEqual(3, padded.Height);
		Assert.AreEqual(((byte)10, (byte)0, (byte)0, (byte)255), padded.GetPixel(1, 1));
		Assert.AreEqual(((byte)20, (byte)0, (byte)0, (byte)255), padded.GetPixel(2, 1));
		Assert.AreEqual(((byte)0, (byte)0, (byte)0, (byte)0), padded.GetPixel(0, 1));
		Assert.AreEqual(((byte)0, (byte)0, (byte)0, (byte)0), padded.GetPixel(1, 0));
	}

	[TestMethod]
	public void Pad_WithAnOddMargin_PutsTheExtraPixelRightAndBelow()
	{
		ImagePixels image = new(1, 1);
		image.Fill(255, 255, 255, 255);

		ImagePixels padded = image.Pad(4, 4, 1, 2, 3, 4);

		Assert.AreEqual(((byte)255, (byte)255, (byte)255, (byte)255), padded.GetPixel(1, 1));
		Assert.AreEqual(((byte)1, (byte)2, (byte)3, (byte)4), padded.GetPixel(2, 2));
		Assert.AreEqual(((byte)1, (byte)2, (byte)3, (byte)4), padded.GetPixel(0, 0));
	}

	[TestMethod]
	public void Pad_ToTheSameSize_CopiesTheImage()
	{
		ImagePixels image = new(3, 2);
		new Random(5).NextBytes(image.Pixels);

		ImagePixels padded = image.Pad(3, 2, 0, 0, 0, 0);

		Assert.AreNotSame(image, padded);
		CollectionAssert.AreEqual(image.GetBuffer(), padded.GetBuffer());
	}

	[TestMethod]
	public void Pad_ToASmallerCanvas_Throws()
	{
		ImagePixels image = new(3, 3);

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => image.Pad(2, 3, 0, 0, 0, 0));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => image.Pad(3, 2, 0, 0, 0, 0));
	}
}
