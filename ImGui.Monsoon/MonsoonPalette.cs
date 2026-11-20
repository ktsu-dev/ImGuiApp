// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Monsoon;

using System.Globalization;
using System.Numerics;

/// <summary>
/// The colours of the Monsoon look: neon tubes of magenta, cyan and amber burning against a wet,
/// blue-black night.
/// </summary>
/// <remarks>
/// Every value is straight-alpha sRGB, the form <c>ImGuiStyle.Colors</c> and
/// <c>ImGui.GetColorU32</c> take. The theme is built from these and nothing else, so a widget that
/// asks for <see cref="Cyan"/> matches the theme's own cyan exactly.
/// </remarks>
public static class MonsoonPalette
{
	/// <summary>Gets the night: the window background, nearly black with a blue cast.</summary>
	public static Vector4 Background { get; } = FromHex("#05070F");

	/// <summary>Gets the glass of a panel, one step up from the night.</summary>
	public static Vector4 Panel { get; } = FromHex("#0A1020");

	/// <summary>Gets the face of a control: a frame, a button at rest, a header.</summary>
	public static Vector4 Surface { get; } = FromHex("#111A2E");

	/// <summary>Gets the hot pink-magenta neon, the accent for anything active.</summary>
	public static Vector4 Magenta { get; } = FromHex("#FF2E88");

	/// <summary>Gets the cold cyan neon, the accent for borders, grids and readouts.</summary>
	public static Vector4 Cyan { get; } = FromHex("#2DE2E6");

	/// <summary>Gets the sodium-lamp amber, for warnings and values worth reading twice.</summary>
	public static Vector4 Amber { get; } = FromHex("#FFB547");

	/// <summary>Gets the violet haze, for selection and the space between other lights.</summary>
	public static Vector4 Violet { get; } = FromHex("#7B4DFF");

	/// <summary>Gets ordinary text: a pale, slightly cyan white.</summary>
	public static Vector4 Text { get; } = FromHex("#CFE8F2");

	/// <summary>Gets labels and disabled text: a blue-grey that recedes.</summary>
	public static Vector4 TextDim { get; } = FromHex("#5A7489");

	/// <summary>Gets the alarm red, kept apart from the magenta so the two are never confused.</summary>
	public static Vector4 Alarm { get; } = FromHex("#FF3B3B");

	/// <summary>
	/// Returns <paramref name="color"/> with its alpha replaced by <paramref name="alpha"/>.
	/// </summary>
	/// <param name="color">The colour whose channels to keep.</param>
	/// <param name="alpha">The new alpha, from 0 to 1.</param>
	/// <returns>The colour at the new alpha.</returns>
	public static Vector4 WithAlpha(Vector4 color, float alpha) => new(color.X, color.Y, color.Z, alpha);

	/// <summary>
	/// Returns <paramref name="color"/> with its red, green and blue scaled by
	/// <paramref name="brightness"/> and its alpha left alone.
	/// </summary>
	/// <param name="color">The colour to dim or brighten.</param>
	/// <param name="brightness">The factor; each channel is clamped to 1 afterwards.</param>
	/// <returns>The scaled colour.</returns>
	public static Vector4 Scale(Vector4 color, float brightness) => new(
		Math.Min(color.X * brightness, 1f),
		Math.Min(color.Y * brightness, 1f),
		Math.Min(color.Z * brightness, 1f),
		color.W);

	/// <summary>
	/// Returns the colour a straight blend puts <paramref name="amount"/> of the way from
	/// <paramref name="from"/> to <paramref name="to"/>.
	/// </summary>
	/// <param name="from">The colour at 0.</param>
	/// <param name="to">The colour at 1.</param>
	/// <param name="amount">How far to go, clamped to 0..1.</param>
	/// <returns>The blended colour.</returns>
	public static Vector4 Mix(Vector4 from, Vector4 to, float amount) =>
		Vector4.Lerp(from, to, Math.Clamp(amount, 0f, 1f));

	/// <summary>
	/// Parses <c>#RRGGBB</c> or <c>#RRGGBBAA</c> into a straight-alpha colour.
	/// </summary>
	/// <param name="hex">The colour, with or without the leading <c>#</c>.</param>
	/// <returns>The colour, opaque unless an alpha byte was given.</returns>
	/// <exception cref="FormatException">The text is not six or eight hex digits.</exception>
	public static Vector4 FromHex(string hex)
	{
		Ensure.NotNull(hex);
		string digits = hex.StartsWith('#') ? hex[1..] : hex;
		if (digits.Length is not (6 or 8) || !uint.TryParse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint packed))
		{
			throw new FormatException($"'{hex}' is not a #RRGGBB or #RRGGBBAA colour.");
		}

		if (digits.Length == 6)
		{
			packed = (packed << 8) | 0xFF;
		}

		return new Vector4(
			((packed >> 24) & 0xFF) / 255f,
			((packed >> 16) & 0xFF) / 255f,
			((packed >> 8) & 0xFF) / 255f,
			(packed & 0xFF) / 255f);
	}
}
