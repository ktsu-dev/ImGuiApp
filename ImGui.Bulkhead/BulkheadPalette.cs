// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Bulkhead;

using System.Numerics;

/// <summary>
/// The named colours the Bulkhead look is built from: warm pastels laid on black.
/// </summary>
/// <remarks>
/// Each colour is straight sRGB with full alpha, the form ImGui's style and draw lists take. Widgets
/// pass them through <c>ImGui.GetColorU32</c>, so they dim with the style alpha inside a disabled
/// block like anything else ImGui draws.
/// </remarks>
public static class BulkheadPalette
{
	/// <summary>The backdrop every surface sits on.</summary>
	public static Vector4 Background { get; } = FromHex(0x000000);

	/// <summary>The lead accent: frames, elbows and primary buttons.</summary>
	public static Vector4 Amber { get; } = FromHex(0xF5A04A);

	/// <summary>A paler amber, for secondary bars and hovered surfaces.</summary>
	public static Vector4 Apricot { get; } = FromHex(0xF7C08A);

	/// <summary>A soft violet, for navigation and alternate actions.</summary>
	public static Vector4 Lilac { get; } = FromHex(0xC29BE0);

	/// <summary>A blue-violet, for information and selection.</summary>
	public static Vector4 Periwinkle { get; } = FromHex(0x8E9CF0);

	/// <summary>A dusty rose, for calls to action.</summary>
	public static Vector4 Mauve { get; } = FromHex(0xC9799A);

	/// <summary>A warm yellow, for caution.</summary>
	public static Vector4 Sunflower { get; } = FromHex(0xF2CC55);

	/// <summary>A red, for alerts and destructive actions.</summary>
	public static Vector4 Ember { get; } = FromHex(0xE5553F);

	/// <summary>A pale blue, for readouts and secondary text.</summary>
	public static Vector4 Ice { get; } = FromHex(0x9CCBEA);

	/// <summary>A green-teal, for success and active states.</summary>
	public static Vector4 Seafoam { get; } = FromHex(0x7FC8A0);

	/// <summary>A muted slate, for idle segments and inactive controls.</summary>
	public static Vector4 Steel { get; } = FromHex(0x5C6A8A);

	/// <summary>Text drawn on a coloured surface.</summary>
	public static Vector4 Ink { get; } = FromHex(0x000000);

	/// <summary>Text drawn on the black backdrop.</summary>
	public static Vector4 Bone { get; } = FromHex(0xF0E6D2);

	/// <summary>Builds an opaque colour from a 0xRRGGBB value.</summary>
	/// <param name="rgb">The colour, red in the high byte.</param>
	/// <returns>The colour as ImGui takes it.</returns>
	public static Vector4 FromHex(uint rgb) => new(
		((rgb >> 16) & 0xFF) / 255f,
		((rgb >> 8) & 0xFF) / 255f,
		(rgb & 0xFF) / 255f,
		1f);

	/// <summary>Moves a colour towards white (positive) or black (negative), keeping its alpha.</summary>
	/// <param name="color">The colour to adjust.</param>
	/// <param name="amount">How far to move, from -1 (black) to 1 (white).</param>
	/// <returns>The adjusted colour.</returns>
	public static Vector4 Shade(Vector4 color, float amount)
	{
		Vector3 rgb = new(color.X, color.Y, color.Z);
		Vector3 target = amount >= 0 ? Vector3.One : Vector3.Zero;
		Vector3 result = Vector3.Lerp(rgb, target, System.Math.Clamp(System.Math.Abs(amount), 0f, 1f));
		return new Vector4(result, color.W);
	}

	/// <summary>Returns a colour with its alpha scaled.</summary>
	/// <param name="color">The colour.</param>
	/// <param name="alpha">The factor to multiply the alpha by.</param>
	/// <returns>The colour at the new alpha.</returns>
	public static Vector4 Fade(Vector4 color, float alpha) => color with { W = color.W * alpha };
}
