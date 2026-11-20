// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Sortie;

using System.Numerics;

/// <summary>
/// The colours of a command display: a near-black field, one leading warning colour at three strengths,
/// and the fixed signal colours every display uses whatever its condition.
/// </summary>
/// <param name="Condition">Which condition this palette describes.</param>
/// <param name="Lead">The leading colour: panel rules, caption blocks, filled segments.</param>
/// <param name="LeadBright">The leading colour pushed hot: hovered controls, the active cell.</param>
/// <param name="LeadDim">The leading colour held back: grid lines, unlit segments, secondary text.</param>
/// <param name="LeadFaint">A wash of the leading colour for panel fills.</param>
/// <param name="Text">Ordinary text.</param>
/// <param name="Field">The black field behind everything.</param>
/// <param name="Alert">Red, for failures and the emergency banner.</param>
/// <param name="Caution">Amber, for systems that need watching.</param>
/// <param name="Signal">Green, for systems within limits and live traces.</param>
public sealed record SortiePalette(
	SortieCondition Condition,
	Vector4 Lead,
	Vector4 LeadBright,
	Vector4 LeadDim,
	Vector4 LeadFaint,
	Vector4 Text,
	Vector4 Field,
	Vector4 Alert,
	Vector4 Caution,
	Vector4 Signal)
{
	/// <summary>Gets the standby palette, led by orange.</summary>
	public static SortiePalette Standby { get; } = new(
		SortieCondition.Standby,
		Lead: Rgb(0xFF, 0x7A, 0x12),
		LeadBright: Rgb(0xFF, 0xB2, 0x4A),
		LeadDim: Rgb(0x8A, 0x3C, 0x08),
		LeadFaint: Rgb(0x2A, 0x10, 0x03),
		Text: Rgb(0xFF, 0xC0, 0x7A),
		Field: Rgb(0x07, 0x04, 0x03),
		Alert: Rgb(0xF0, 0x1E, 0x24),
		Caution: Rgb(0xFF, 0xD0, 0x2A),
		Signal: Rgb(0x48, 0xF0, 0x6A));

	/// <summary>Gets the emergency palette, led by red.</summary>
	public static SortiePalette Emergency { get; } = new(
		SortieCondition.Emergency,
		Lead: Rgb(0xF0, 0x1E, 0x24),
		LeadBright: Rgb(0xFF, 0x6A, 0x5A),
		LeadDim: Rgb(0x80, 0x0C, 0x10),
		LeadFaint: Rgb(0x2C, 0x04, 0x05),
		Text: Rgb(0xFF, 0x9A, 0x82),
		Field: Rgb(0x08, 0x02, 0x02),
		Alert: Rgb(0xFF, 0x2A, 0x2A),
		Caution: Rgb(0xFF, 0x8A, 0x12),
		Signal: Rgb(0x48, 0xF0, 0x6A));

	/// <summary>Gets the palette for a condition.</summary>
	/// <param name="condition">The condition.</param>
	/// <returns>Its palette.</returns>
	public static SortiePalette For(SortieCondition condition) =>
		condition == SortieCondition.Emergency ? Emergency : Standby;

	/// <summary>Gets the colour a lamp shows in a state.</summary>
	/// <param name="lamp">The lamp state.</param>
	/// <returns>The lamp's colour; the dim lead colour when it is off.</returns>
	public Vector4 LampColor(SortieLamp lamp) => lamp switch
	{
		SortieLamp.Nominal => Signal,
		SortieLamp.Caution => Caution,
		SortieLamp.Alert => Alert,
		_ => LeadDim,
	};

	/// <summary>Returns a colour with a different alpha.</summary>
	/// <param name="color">The colour.</param>
	/// <param name="alpha">The new alpha.</param>
	/// <returns>The colour at that alpha.</returns>
	public static Vector4 WithAlpha(Vector4 color, float alpha) => color with { W = alpha };

	private static Vector4 Rgb(byte r, byte g, byte b) => new(r / 255f, g / 255f, b / 255f, 1f);
}
