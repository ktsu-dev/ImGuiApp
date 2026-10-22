// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App;

using System;

/// <summary>
/// The encoding of a half-float texture: the sRGB curve, extended past 1 and mirrored below 0, so a
/// value of 1 is SDR white and a value above it is brighter.
/// </summary>
/// <remarks>
/// Half-float textures hold encoded values for the same reason RGBA8 ones do: ImGui blends and filters
/// in encoded space, so a texture drawn over an SDR one looks the same as it does in an SDR window.
/// The window decodes what it drew just before presenting it. Negative values carry colours outside
/// sRGB's gamut in Rec.709 primaries, as scRGB does.
/// </remarks>
public static class HdrEncoding
{
	/// <summary>Encodes a linear value, 1 being SDR white.</summary>
	/// <param name="linear">The linear value.</param>
	/// <returns>The encoded value.</returns>
	public static float Encode(float linear)
	{
		float magnitude = Math.Abs(linear);
		float encoded = magnitude <= 0.0031308f ? magnitude * 12.92f : (1.055f * MathF.Pow(magnitude, 1 / 2.4f)) - 0.055f;
		return linear < 0 ? -encoded : encoded;
	}

	/// <summary>Decodes an encoded value back to linear, 1 being SDR white.</summary>
	/// <param name="encoded">The encoded value.</param>
	/// <returns>The linear value.</returns>
	public static float Decode(float encoded)
	{
		float magnitude = Math.Abs(encoded);
		float linear = magnitude <= 0.04045f ? magnitude / 12.92f : MathF.Pow((magnitude + 0.055f) / 1.055f, 2.4f);
		return encoded < 0 ? -linear : linear;
	}

	/// <summary>Converts encoded half-float pixels to RGBA8, clamping everything above white to white, as an SDR window shows them.</summary>
	/// <param name="rgba">The half-float pixels, four values each.</param>
	/// <param name="destination">The RGBA8 pixels, as many as <paramref name="rgba"/> holds.</param>
	/// <exception cref="ArgumentException"><paramref name="destination"/> is not the same length as <paramref name="rgba"/>.</exception>
	public static void ToRgba8(ReadOnlySpan<Half> rgba, Span<byte> destination)
	{
		if (destination.Length != rgba.Length)
		{
			throw new ArgumentException("The destination must hold one byte for each half-float value.", nameof(destination));
		}

		for (int i = 0; i < rgba.Length; i++)
		{
			float value = (float)rgba[i];
			destination[i] = value >= 1 ? (byte)255 : value > 0 ? (byte)((value * 255) + 0.5f) : (byte)0;
		}
	}
}
