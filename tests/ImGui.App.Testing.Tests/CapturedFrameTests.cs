// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing.Tests;

using System;
using System.IO;

using ktsu.ImGui.App.Images;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Every UI suite measures through <see cref="CapturedFrame"/>, so these pin its arithmetic exactly
/// rather than through a "the frame is not blank" check that an off-by-one would still pass.
/// </summary>
[TestClass]
public sealed class CapturedFrameTests
{
	private static readonly Rgba32 Background = new(0, 0, 0, 255);
	private static readonly Rgba32 Ink = new(255, 0, 0, 255);

	/// <summary>A 16x12 frame with ink at the given pixels and background everywhere else.</summary>
	private static CapturedFrame FrameWithInkAt(params (int X, int Y)[] points)
	{
		Bitmap32 bitmap = new(16, 12);
		bitmap.Clear(Background);
		foreach ((int x, int y) in points)
		{
			bitmap.SetPixel(x, y, Ink);
		}

		return new CapturedFrame(bitmap);
	}

	[TestMethod]
	public void FindBounds_ReturnsTheTightBoxAroundMatchingPixels()
	{
		CapturedFrame frame = FrameWithInkAt((3, 2), (9, 7), (5, 4));

		Rectangle? bounds = frame.FindBounds(p => p == Ink);

		// The maximum edges are exclusive: the box holds columns 3..9 and rows 2..7.
		Assert.AreEqual(new Rectangle(3, 2, 10, 8), bounds);
	}

	[TestMethod]
	public void FindBounds_SinglePixel_IsOnePixelWideAndTall()
	{
		CapturedFrame frame = FrameWithInkAt((15, 11));

		Assert.AreEqual(new Rectangle(15, 11, 16, 12), frame.FindBounds(p => p == Ink));
	}

	[TestMethod]
	public void FindBounds_NoMatch_ReturnsNull()
	{
		CapturedFrame frame = FrameWithInkAt();

		Assert.IsNull(frame.FindBounds(p => p == Ink));
	}

	[TestMethod]
	public void CountPixels_CountsEveryMatch()
	{
		CapturedFrame frame = FrameWithInkAt((0, 0), (1, 0), (15, 11), (7, 6));

		Assert.AreEqual(4, frame.CountPixels(p => p == Ink));
		Assert.AreEqual((16 * 12) - 4, frame.CountPixels(p => p == Background));
	}

	[TestMethod]
	public void SavePng_WritesAFileThatDecodesToTheSamePixels()
	{
		CapturedFrame frame = FrameWithInkAt((3, 2), (9, 7));
		string path = Path.Combine(Path.GetTempPath(), $"captured-frame-{Guid.NewGuid():N}.png");

		try
		{
			frame.SavePng(path);

			byte[] file = File.ReadAllBytes(path);
			Assert.IsNotEmpty(file, "SavePng wrote an empty file.");

			ImagePixels decoded = ImageDecoder.Decode(file);
			Assert.AreEqual(frame.Width, decoded.Width);
			Assert.AreEqual(frame.Height, decoded.Height);

			ReadOnlySpan<byte> pixels = decoded.ReadOnlyPixels;
			for (int y = 0; y < frame.Height; y++)
			{
				for (int x = 0; x < frame.Width; x++)
				{
					int i = ((y * frame.Width) + x) * 4;
					Rgba32 read = new(pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3]);
					Assert.AreEqual(frame.GetPixel(x, y), read, $"Pixel ({x}, {y}) changed on the way through the file.");
				}
			}
		}
		finally
		{
			File.Delete(path);
		}
	}
}
