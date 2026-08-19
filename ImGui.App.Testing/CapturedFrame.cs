// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Testing;

using System;

/// <summary>
/// An immutable snapshot of one rendered frame. Measurements taken here go into assertions, and the
/// whole frame can be written to disk as a diagnostic artifact when a test fails.
/// </summary>
public sealed class CapturedFrame
{
	private readonly Bitmap32 pixels;

	/// <summary>Initializes a snapshot by copying the supplied bitmap.</summary>
	/// <param name="source">The bitmap to copy.</param>
	public CapturedFrame(Bitmap32 source)
	{
		Ensure.NotNull(source);

		pixels = new Bitmap32(source.Width, source.Height);
		source.Pixels.CopyTo(pixels.Pixels);
	}

	/// <summary>Gets the frame width in pixels.</summary>
	public int Width => pixels.Width;

	/// <summary>Gets the frame height in pixels.</summary>
	public int Height => pixels.Height;

	/// <summary>Reads one pixel.</summary>
	/// <param name="x">Column.</param>
	/// <param name="y">Row.</param>
	/// <returns>The color at that position.</returns>
	public Rgba32 GetPixel(int x, int y) => pixels.GetPixel(x, y);

	/// <summary>Writes the frame to disk as a PNG.</summary>
	/// <param name="path">Destination file path.</param>
	public void SavePng(string path) => pixels.SavePng(path);

	/// <summary>
	/// Finds the tight rectangle containing every pixel matching a predicate, which is how a test
	/// measures where something was drawn without resorting to a golden image.
	/// </summary>
	/// <param name="predicate">Chooses which pixels count.</param>
	/// <returns>The bounding rectangle, or null when nothing matched.</returns>
	public Rectangle? FindBounds(Func<Rgba32, bool> predicate)
	{
		Ensure.NotNull(predicate);

		int minX = int.MaxValue;
		int minY = int.MaxValue;
		int maxX = int.MinValue;
		int maxY = int.MinValue;

		for (int y = 0; y < Height; y++)
		{
			for (int x = 0; x < Width; x++)
			{
				if (!predicate(pixels.GetPixel(x, y)))
				{
					continue;
				}

				minX = Math.Min(minX, x);
				minY = Math.Min(minY, y);
				maxX = Math.Max(maxX, x);
				maxY = Math.Max(maxY, y);
			}
		}

		return minX == int.MaxValue ? null : new Rectangle(minX, minY, maxX + 1, maxY + 1);
	}

	/// <summary>Counts pixels matching a predicate.</summary>
	/// <param name="predicate">Chooses which pixels count.</param>
	/// <returns>How many pixels matched.</returns>
	public int CountPixels(Func<Rgba32, bool> predicate)
	{
		Ensure.NotNull(predicate);

		int count = 0;
		for (int y = 0; y < Height; y++)
		{
			for (int x = 0; x < Width; x++)
			{
				if (predicate(pixels.GetPixel(x, y)))
				{
					count++;
				}
			}
		}

		return count;
	}
}
