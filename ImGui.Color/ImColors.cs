// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Color;

using System;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using SemanticHsl = ktsu.Semantics.Color.Hsl;
using SemanticSrgb = ktsu.Semantics.Color.Srgb;

/// <summary>
/// Factories that build ImGui <see cref="ImColor"/> values from common inputs. ImGui works in
/// gamma-encoded sRGB, so these construct sRGB channels directly (no linear round-trip). For
/// conversions from a semantic color, use <see cref="ColorImGuiExtensions.ToImColor"/>.
/// </summary>
public static class ImColors
{
	/// <summary>Parses a hex string (<c>#RGB</c>, <c>#RRGGBB</c>, or <c>#RRGGBBAA</c>; the <c>#</c> is optional).</summary>
	/// <param name="hex">The hex color string.</param>
	/// <returns>The ImGui color.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="hex"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="hex"/> is not a supported length.</exception>
	public static ImColor FromHex(string hex)
	{
		Ensure.NotNull(hex);

		if (hex.StartsWith('#'))
		{
			hex = hex[1..];
		}

		if (hex.Length == 3)
		{
			hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
		}

		if (hex.Length == 6)
		{
			hex += "FF";
		}

		if (hex.Length != 8)
		{
			throw new ArgumentException("Hex color must be in the format #RGB, #RRGGBB, or #RRGGBBAA", nameof(hex));
		}

		byte r = byte.Parse(hex.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
		byte g = byte.Parse(hex.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
		byte b = byte.Parse(hex.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
		byte a = byte.Parse(hex.AsSpan(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

		return FromRgba(r, g, b, a);
	}

	/// <summary>Creates an opaque color from 8-bit sRGB channels.</summary>
	/// <param name="r">Red (0-255).</param>
	/// <param name="g">Green (0-255).</param>
	/// <param name="b">Blue (0-255).</param>
	/// <returns>The ImGui color.</returns>
	public static ImColor FromRgb(byte r, byte g, byte b) => FromRgba(r, g, b, 255);

	/// <summary>Creates a color from 8-bit sRGB channels plus alpha.</summary>
	/// <param name="r">Red (0-255).</param>
	/// <param name="g">Green (0-255).</param>
	/// <param name="b">Blue (0-255).</param>
	/// <param name="a">Alpha (0-255).</param>
	/// <returns>The ImGui color.</returns>
	public static ImColor FromRgba(byte r, byte g, byte b, byte a) =>
		new() { Value = new Vector4(r / 255f, g / 255f, b / 255f, a / 255f) };

	/// <summary>Creates an opaque color from normalized sRGB channels (0-1).</summary>
	/// <param name="r">Red (0-1).</param>
	/// <param name="g">Green (0-1).</param>
	/// <param name="b">Blue (0-1).</param>
	/// <returns>The ImGui color.</returns>
	public static ImColor FromRgb(float r, float g, float b) => new() { Value = new Vector4(r, g, b, 1f) };

	/// <summary>Creates a color from normalized sRGB channels plus alpha (0-1).</summary>
	/// <param name="r">Red (0-1).</param>
	/// <param name="g">Green (0-1).</param>
	/// <param name="b">Blue (0-1).</param>
	/// <param name="a">Alpha (0-1).</param>
	/// <returns>The ImGui color.</returns>
	public static ImColor FromRgba(float r, float g, float b, float a) => new() { Value = new Vector4(r, g, b, a) };

	/// <summary>Creates an opaque color from an RGB <see cref="Vector3"/> (sRGB channels, 0-1).</summary>
	/// <param name="rgb">The RGB vector.</param>
	/// <returns>The ImGui color.</returns>
	public static ImColor FromVector(Vector3 rgb) => new() { Value = new Vector4(rgb, 1f) };

	/// <summary>Creates a color from an RGBA <see cref="Vector4"/> (sRGB channels, 0-1).</summary>
	/// <param name="rgba">The RGBA vector.</param>
	/// <returns>The ImGui color.</returns>
	public static ImColor FromVector(Vector4 rgba) => new() { Value = rgba };

	/// <summary>Creates a color from HSL. Hue is in degrees (0-360); saturation, lightness, and alpha are 0-1.</summary>
	/// <param name="hueDegrees">Hue angle in degrees.</param>
	/// <param name="saturation">Saturation (0-1).</param>
	/// <param name="lightness">Lightness (0-1).</param>
	/// <param name="alpha">Alpha (0-1, default 1).</param>
	/// <returns>The ImGui color.</returns>
	public static ImColor FromHsl(float hueDegrees, float saturation, float lightness, float alpha = 1f)
	{
		SemanticSrgb srgb = new SemanticHsl(hueDegrees, saturation, lightness).ToSrgb();
		return new() { Value = new Vector4((float)srgb.R, (float)srgb.G, (float)srgb.B, alpha) };
	}
}
