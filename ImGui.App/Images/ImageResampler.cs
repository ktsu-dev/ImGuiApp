// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Images;

using System;

/// <summary>
/// Scales <see cref="ImagePixels"/> with a separable Lanczos-3 filter.
/// </summary>
/// <remarks>
/// <para>
/// The filter support is widened by the reduction factor when downscaling, so shrinking an image
/// averages every source pixel that lands in an output pixel rather than point-sampling a few of
/// them. That is what keeps the small window-icon sizes free of the aliasing a naive bilinear
/// shrink produces.
/// </para>
/// <para>
/// Resampling runs on premultiplied alpha and the result is unpremultiplied afterwards, so the
/// colour of fully transparent pixels does not bleed into their visible neighbours.
/// </para>
/// </remarks>
public static class ImageResampler
{
	private const double Support = 3.0;

	/// <summary>Scales an image to a new size.</summary>
	/// <param name="source">The image to scale.</param>
	/// <param name="width">Target width in pixels. Must be positive.</param>
	/// <param name="height">Target height in pixels. Must be positive.</param>
	/// <returns>A new image at the requested size, or a copy of the source when the size is unchanged.</returns>
	public static ImagePixels Resize(ImagePixels source, int width, int height)
	{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

		if (width == source.Width && height == source.Height)
		{
			return source.Clone();
		}

		// Premultiply into floats once; both passes then work on the same representation.
		float[] premultiplied = Premultiply(source);

		float[] horizontal = ResampleAxis(premultiplied, source.Width, source.Height, width, horizontally: true);
		float[] vertical = ResampleAxis(horizontal, width, source.Height, height, horizontally: false);

		return Unpremultiply(vertical, width, height);
	}

	private static float[] Premultiply(ImagePixels source)
	{
		ReadOnlySpan<byte> pixels = source.ReadOnlyPixels;
		float[] result = new float[pixels.Length];
		for (int i = 0; i < pixels.Length; i += 4)
		{
			float a = pixels[i + 3];
			float scale = a * (1f / 255f);
			result[i] = pixels[i] * scale;
			result[i + 1] = pixels[i + 1] * scale;
			result[i + 2] = pixels[i + 2] * scale;
			result[i + 3] = a;
		}

		return result;
	}

	private static ImagePixels Unpremultiply(float[] source, int width, int height)
	{
		ImagePixels result = new(width, height);
		Span<byte> pixels = result.Pixels;
		for (int i = 0; i < pixels.Length; i += 4)
		{
			float a = source[i + 3];
			byte alpha = ClampToByte(a);
			if (alpha == 0)
			{
				continue;
			}

			float scale = 255f / a;
			pixels[i] = ClampToByte(source[i] * scale);
			pixels[i + 1] = ClampToByte(source[i + 1] * scale);
			pixels[i + 2] = ClampToByte(source[i + 2] * scale);
			pixels[i + 3] = alpha;
		}

		return result;
	}

	/// <summary>
	/// Resamples one axis of a planar RGBA float buffer, leaving the other axis untouched.
	/// </summary>
	private static float[] ResampleAxis(float[] source, int sourceWidth, int sourceHeight, int destinationLength, bool horizontally)
	{
		int sourceLength = horizontally ? sourceWidth : sourceHeight;
		int destinationWidth = horizontally ? destinationLength : sourceWidth;
		int destinationHeight = horizontally ? sourceHeight : destinationLength;

		if (sourceLength == destinationLength)
		{
			return source;
		}

		Kernel kernel = Kernel.Build(sourceLength, destinationLength);
		float[] destination = new float[destinationWidth * destinationHeight * 4];

		// The outer loop walks the axis that is not being resampled, so each inner iteration reads a
		// contiguous run of taps along the axis that is.
		int outerCount = horizontally ? sourceHeight : destinationWidth;
		for (int outer = 0; outer < outerCount; outer++)
		{
			for (int i = 0; i < destinationLength; i++)
			{
				float r = 0f;
				float g = 0f;
				float b = 0f;
				float a = 0f;

				int start = kernel.Start[i];
				int count = kernel.Count[i];
				int weightOffset = kernel.Offset[i];
				for (int tap = 0; tap < count; tap++)
				{
					float weight = kernel.Weights[weightOffset + tap];
					int sourceIndex = horizontally
						? ((outer * sourceWidth) + start + tap) * 4
						: (((start + tap) * sourceWidth) + outer) * 4;

					r += source[sourceIndex] * weight;
					g += source[sourceIndex + 1] * weight;
					b += source[sourceIndex + 2] * weight;
					a += source[sourceIndex + 3] * weight;
				}

				int destinationIndex = horizontally
					? ((outer * destinationWidth) + i) * 4
					: ((i * destinationWidth) + outer) * 4;

				destination[destinationIndex] = r;
				destination[destinationIndex + 1] = g;
				destination[destinationIndex + 2] = b;
				destination[destinationIndex + 3] = a;
			}
		}

		return destination;
	}

	private static byte ClampToByte(float value)
	{
		int rounded = (int)MathF.Round(value);
		return rounded <= 0 ? (byte)0 : rounded >= 255 ? (byte)255 : (byte)rounded;
	}

	private static double Lanczos3(double x)
	{
		x = Math.Abs(x);
		if (x < 1e-6)
		{
			return 1.0;
		}

		if (x >= Support)
		{
			return 0.0;
		}

		double pix = Math.PI * x;
		return Support * Math.Sin(pix) * Math.Sin(pix / Support) / (pix * pix);
	}

	/// <summary>
	/// Precomputed filter taps for one axis: for each destination sample, the first source sample it
	/// reads, how many it reads, and where its normalized weights start in <see cref="Weights"/>.
	/// </summary>
	private sealed class Kernel
	{
		public required int[] Start { get; init; }
		public required int[] Count { get; init; }
		public required int[] Offset { get; init; }
		public required float[] Weights { get; init; }

		public static Kernel Build(int sourceLength, int destinationLength)
		{
			double scale = (double)destinationLength / sourceLength;

			// Downscaling widens the filter so every source sample that maps into the destination
			// sample contributes; upscaling leaves it at its natural width.
			double filterScale = scale < 1.0 ? 1.0 / scale : 1.0;
			double support = Support * filterScale;

			int[] start = new int[destinationLength];
			int[] count = new int[destinationLength];
			int[] offset = new int[destinationLength];

			int maxTaps = (int)Math.Ceiling(support * 2) + 2;
			float[] weights = new float[destinationLength * maxTaps];

			int cursor = 0;
			double[] scratch = new double[maxTaps];
			for (int i = 0; i < destinationLength; i++)
			{
				double center = ((i + 0.5) / scale) - 0.5;
				int left = (int)Math.Ceiling(center - support);
				int right = (int)Math.Floor(center + support);

				left = Math.Max(left, 0);
				right = Math.Min(right, sourceLength - 1);

				int taps = right - left + 1;
				double sum = 0.0;
				for (int tap = 0; tap < taps; tap++)
				{
					double weight = Lanczos3((left + tap - center) / filterScale);
					scratch[tap] = weight;
					sum += weight;
				}

				// A clamped kernel at the edges, or a pathological one, can sum to zero; fall back to
				// the nearest source sample rather than emitting a black pixel.
				if (Math.Abs(sum) < 1e-9)
				{
					scratch[0] = 1.0;
					sum = 1.0;
					taps = 1;
					left = Math.Clamp((int)Math.Round(center), 0, sourceLength - 1);
				}

				start[i] = left;
				count[i] = taps;
				offset[i] = cursor;
				for (int tap = 0; tap < taps; tap++)
				{
					weights[cursor + tap] = (float)(scratch[tap] / sum);
				}

				cursor += taps;
			}

			return new Kernel { Start = start, Count = count, Offset = offset, Weights = weights };
		}
	}
}
