// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class Bitmap32Tests
{
	[TestMethod]
	public void SetPixel_ThenGetPixel_RoundTrips()
	{
		Bitmap32 bitmap = new(4, 3);

		bitmap.SetPixel(2, 1, new Rgba32(10, 20, 30, 40));

		Assert.AreEqual(new Rgba32(10, 20, 30, 40), bitmap.GetPixel(2, 1));
	}

	[TestMethod]
	public void Clear_FillsEveryPixel()
	{
		Bitmap32 bitmap = new(3, 2);

		bitmap.Clear(new Rgba32(1, 2, 3, 255));

		for (int y = 0; y < bitmap.Height; y++)
		{
			for (int x = 0; x < bitmap.Width; x++)
			{
				Assert.AreEqual(new Rgba32(1, 2, 3, 255), bitmap.GetPixel(x, y), $"Pixel {x},{y} was not cleared.");
			}
		}
	}

	[TestMethod]
	public void EncodePng_StartsWithTheSignatureAndEndsWithIend()
	{
		Bitmap32 bitmap = new(2, 2);
		bitmap.Clear(new Rgba32(255, 0, 0, 255));

		byte[] png = bitmap.EncodePng();

		byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
		CollectionAssert.AreEqual(signature, png[..8], "PNG signature is wrong.");

		// An IEND chunk is twelve bytes: a four byte length of zero, the four byte type, then a
		// four byte CRC. The type therefore sits eight to four bytes from the end.
		byte[] type = png[^8..^4];
		CollectionAssert.AreEqual("IEND"u8.ToArray(), type, "The stream should end with an IEND chunk.");
	}

	[TestMethod]
	public void GetPixel_OutsideBounds_Throws() =>
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Bitmap32(2, 2).GetPixel(2, 0));

	[TestMethod]
	public void Constructor_NonPositiveSize_Throws() =>
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Bitmap32(0, 4));
}
