// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests.Images;

using System;
using ktsu.ImGui.App.Images;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Covers the scaling used for window icons.
/// </summary>
[TestClass]
public class ImageResamplerTests
{
	[TestMethod]
	public void Resize_ProducesTheRequestedSize()
	{
		ImagePixels resized = ImageResampler.Resize(Solid(37, 23, 10, 20, 30, 255), 16, 16);

		Assert.AreEqual(16, resized.Width);
		Assert.AreEqual(16, resized.Height);
	}

	[TestMethod]
	public void Resize_ToTheSameSize_ReturnsAnEquivalentCopy()
	{
		ImagePixels source = Solid(8, 8, 1, 2, 3, 4);

		ImagePixels resized = source.Resize(8, 8);

		Assert.AreNotSame(source, resized);
		CollectionAssert.AreEqual(source.GetBuffer(), resized.GetBuffer());
	}

	[TestMethod]
	[DataRow(64, 16)]
	[DataRow(64, 128)]
	[DataRow(17, 5)]
	public void Resize_OfASolidColour_KeepsThatColour(int from, int to)
	{
		ImagePixels resized = Solid(from, from, 70, 140, 210, 255).Resize(to, to);

		for (int y = 0; y < to; y++)
		{
			for (int x = 0; x < to; x++)
			{
				(byte r, byte g, byte b, byte a) = resized.GetPixel(x, y);
				Assert.AreEqual(70, r, $"red at {x},{y}");
				Assert.AreEqual(140, g, $"green at {x},{y}");
				Assert.AreEqual(210, b, $"blue at {x},{y}");
				Assert.AreEqual(255, a, $"alpha at {x},{y}");
			}
		}
	}

	[TestMethod]
	public void Resize_DownscalingAverages_RatherThanPointSampling()
	{
		// Alternating black and white columns. Point sampling would land on one colour or the other;
		// an area-weighted filter has to land near the mean.
		ImagePixels source = new(64, 4);
		for (int y = 0; y < 4; y++)
		{
			for (int x = 0; x < 64; x++)
			{
				byte value = (byte)(x % 2 == 0 ? 0 : 255);
				source.SetPixel(x, y, value, value, value, 255);
			}
		}

		ImagePixels resized = source.Resize(4, 4);

		for (int x = 0; x < 4; x++)
		{
			int red = resized.GetPixel(x, 1).R;
			Assert.IsTrue(Math.Abs(red - 128) <= 12, $"column {x} came out at {red}, which is not the mean of the two columns");
		}
	}

	[TestMethod]
	public void Resize_DoesNotBleedColourOutOfTransparentPixels()
	{
		// The left half is opaque green; the right half is fully transparent but carries red in its
		// colour channels. Resampling straight alpha would drag that red into the visible pixels.
		ImagePixels source = new(32, 8);
		for (int y = 0; y < 8; y++)
		{
			for (int x = 0; x < 32; x++)
			{
				if (x < 16)
				{
					source.SetPixel(x, y, 0, 255, 0, 255);
				}
				else
				{
					source.SetPixel(x, y, 255, 0, 0, 0);
				}
			}
		}

		ImagePixels resized = source.Resize(8, 4);

		for (int y = 0; y < 4; y++)
		{
			for (int x = 0; x < 4; x++)
			{
				(byte r, byte g, _, byte a) = resized.GetPixel(x, y);
				if (a > 0)
				{
					Assert.IsTrue(r < 40, $"red bled to {r} at {x},{y}");
					Assert.IsTrue(g > 200, $"green fell to {g} at {x},{y}");
				}
			}
		}
	}

	[TestMethod]
	public void Resize_KeepsAFullyTransparentImageTransparent()
	{
		ImagePixels resized = Solid(16, 16, 200, 100, 50, 0).Resize(4, 4);

		for (int i = 3; i < resized.ByteLength; i += 4)
		{
			Assert.AreEqual(0, resized.ReadOnlyPixels[i]);
		}
	}

	[TestMethod]
	public void Resize_ToAnInvalidSize_Throws()
	{
		ImagePixels source = Solid(4, 4, 0, 0, 0, 255);

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => source.Resize(0, 4));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => source.Resize(4, -1));
	}

	[TestMethod]
	public void Resize_NullSource_Throws() =>
		Assert.ThrowsExactly<ArgumentNullException>(() => ImageResampler.Resize(null!, 4, 4));

	private static ImagePixels Solid(int width, int height, byte r, byte g, byte b, byte a)
	{
		ImagePixels image = new(width, height);
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				image.SetPixel(x, y, r, g, b, a);
			}
		}

		return image;
	}
}
