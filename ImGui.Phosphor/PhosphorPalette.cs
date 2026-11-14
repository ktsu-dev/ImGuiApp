// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Phosphor;

using System.Numerics;

/// <summary>
/// The intensities of one phosphor. A monochrome tube has a single hue, so every colour on the
/// screen is that hue at some brightness; these are the steps the theme and the widgets draw with.
/// </summary>
/// <param name="Tint">Which phosphor this palette describes.</param>
/// <param name="Hot">The over-driven beam: highlights, the cursor, an active alert.</param>
/// <param name="Bright">Ordinary text and lit lines.</param>
/// <param name="Normal">Borders, frames and filled segments.</param>
/// <param name="Dim">Secondary text, grid lines and unlit segments.</param>
/// <param name="Faint">Barely excited phosphor: panel fills and hover washes.</param>
/// <param name="Screen">The unlit glass behind everything.</param>
public sealed record PhosphorPalette(
	PhosphorTint Tint,
	Vector4 Hot,
	Vector4 Bright,
	Vector4 Normal,
	Vector4 Dim,
	Vector4 Faint,
	Vector4 Screen)
{
	/// <summary>Gets the green phosphor palette.</summary>
	public static PhosphorPalette Green { get; } = new(
		PhosphorTint.Green,
		Hot: Rgb(0xD8, 0xFF, 0xDC),
		Bright: Rgb(0x6C, 0xFF, 0x86),
		Normal: Rgb(0x2E, 0xD0, 0x58),
		Dim: Rgb(0x16, 0x7A, 0x34),
		Faint: Rgb(0x0A, 0x33, 0x17),
		Screen: Rgb(0x02, 0x0B, 0x05));

	/// <summary>Gets the amber phosphor palette.</summary>
	public static PhosphorPalette Amber { get; } = new(
		PhosphorTint.Amber,
		Hot: Rgb(0xFF, 0xF0, 0xC8),
		Bright: Rgb(0xFF, 0xC2, 0x3A),
		Normal: Rgb(0xE0, 0x96, 0x10),
		Dim: Rgb(0x86, 0x55, 0x06),
		Faint: Rgb(0x38, 0x22, 0x03),
		Screen: Rgb(0x0B, 0x07, 0x01));

	/// <summary>Gets the palette for a phosphor.</summary>
	/// <param name="tint">The phosphor.</param>
	/// <returns>Its palette.</returns>
	public static PhosphorPalette For(PhosphorTint tint) => tint == PhosphorTint.Amber ? Amber : Green;

	/// <summary>Returns a colour with its alpha replaced.</summary>
	/// <param name="color">The colour.</param>
	/// <param name="alpha">The new alpha, from 0 to 1.</param>
	/// <returns>The colour at that alpha.</returns>
	public static Vector4 WithAlpha(Vector4 color, float alpha) => color with { W = alpha };

	private static Vector4 Rgb(byte r, byte g, byte b) => new(r / 255f, g / 255f, b / 255f, 1f);

	/// <summary>Gets the colour of one brightness level.</summary>
	/// <param name="shade">The level wanted.</param>
	/// <returns>The colour of that level in this palette.</returns>
	public Vector4 Of(PhosphorShade shade) => shade switch
	{
		PhosphorShade.Hot => Hot,
		PhosphorShade.Bright => Bright,
		PhosphorShade.Normal => Normal,
		PhosphorShade.Dim => Dim,
		_ => Faint,
	};
}
