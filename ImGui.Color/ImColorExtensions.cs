// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Color;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.Semantics.Color;

/// <summary>
/// Adjustment and analysis operations on ImGui <see cref="ImColor"/> values. The cylindrical
/// adjustments (saturation, lightness, hue) delegate to <see cref="Srgb"/>'s HSL operations,
/// staying in the gamma-encoded sRGB space ImGui uses; luminance, contrast, and distance delegate to
/// the linear <c>ktsu.Semantics.Color.Color</c> so they use correct color science.
/// </summary>
public static class ImColorExtensions
{
	/// <summary>The WCAG AA contrast ratio for normal-size body text.</summary>
	public const float OptimalTextContrastRatio = 4.5f;

	// --- Cylindrical adjustments (HSL over sRGB) ---

	/// <summary>Returns a copy with HSL saturation replaced (clamped to 0-1).</summary>
	/// <param name="color">The color.</param>
	/// <param name="saturation">The new saturation.</param>
	/// <returns>The adjusted color.</returns>
	public static ImColor WithSaturation(this ImColor color, float saturation) =>
		Rebuild(color, ToSrgb(color).WithSaturation(saturation));

	/// <summary>Returns a copy with HSL saturation increased by <paramref name="amount"/> (clamped to 0-1).</summary>
	/// <param name="color">The color.</param>
	/// <param name="amount">The amount to add.</param>
	/// <returns>The adjusted color.</returns>
	public static ImColor SaturateBy(this ImColor color, float amount) =>
		Rebuild(color, ToSrgb(color).SaturateBy(amount));

	/// <summary>Returns a copy with HSL saturation decreased by <paramref name="amount"/> (clamped to 0-1).</summary>
	/// <param name="color">The color.</param>
	/// <param name="amount">The amount to subtract.</param>
	/// <returns>The adjusted color.</returns>
	public static ImColor DesaturateBy(this ImColor color, float amount) =>
		Rebuild(color, ToSrgb(color).DesaturateBy(amount));

	/// <summary>Returns a copy with HSL saturation multiplied by <paramref name="factor"/> (clamped to 0-1).</summary>
	/// <param name="color">The color.</param>
	/// <param name="factor">The multiplier.</param>
	/// <returns>The adjusted color.</returns>
	public static ImColor MultiplySaturation(this ImColor color, float factor) =>
		Rebuild(color, ToSrgb(color).MultiplySaturation(factor));

	/// <summary>Returns a fully desaturated (grayscale) copy, preserving lightness and alpha.</summary>
	/// <param name="color">The color.</param>
	/// <returns>The grayscale color.</returns>
	public static ImColor ToGrayscale(this ImColor color) => Rebuild(color, ToSrgb(color).ToGrayscale());

	/// <summary>Returns a copy with HSL lightness replaced (clamped to 0-1).</summary>
	/// <param name="color">The color.</param>
	/// <param name="lightness">The new lightness.</param>
	/// <returns>The adjusted color.</returns>
	public static ImColor WithLightness(this ImColor color, float lightness) =>
		Rebuild(color, ToSrgb(color).WithLightness(lightness));

	/// <summary>Returns a copy with HSL lightness increased by <paramref name="amount"/> (clamped to 0-1).</summary>
	/// <param name="color">The color.</param>
	/// <param name="amount">The amount to add.</param>
	/// <returns>The adjusted color.</returns>
	public static ImColor LightenBy(this ImColor color, float amount) =>
		Rebuild(color, ToSrgb(color).LightenBy(amount));

	/// <summary>Returns a copy with HSL lightness decreased by <paramref name="amount"/> (clamped to 0-1).</summary>
	/// <param name="color">The color.</param>
	/// <param name="amount">The amount to subtract.</param>
	/// <returns>The adjusted color.</returns>
	public static ImColor DarkenBy(this ImColor color, float amount) =>
		Rebuild(color, ToSrgb(color).DarkenBy(amount));

	/// <summary>Returns a copy with HSL lightness multiplied by <paramref name="factor"/> (clamped to 0-1).</summary>
	/// <param name="color">The color.</param>
	/// <param name="factor">The multiplier.</param>
	/// <returns>The adjusted color.</returns>
	public static ImColor MultiplyLightness(this ImColor color, float factor) =>
		Rebuild(color, ToSrgb(color).MultiplyLightness(factor));

	/// <summary>Returns a copy with its hue offset by <paramref name="degrees"/> around the wheel (wraps at 360).</summary>
	/// <param name="color">The color.</param>
	/// <param name="degrees">The hue offset in degrees.</param>
	/// <returns>The adjusted color.</returns>
	public static ImColor OffsetHue(this ImColor color, float degrees) =>
		Rebuild(color, ToSrgb(color).OffsetHue(degrees));

	/// <summary>Returns a copy with alpha replaced (clamped to 0-1).</summary>
	/// <param name="color">The color.</param>
	/// <param name="alpha">The new alpha.</param>
	/// <returns>The adjusted color.</returns>
	public static ImColor WithAlpha(this ImColor color, float alpha) =>
		new() { Value = new Vector4(color.Value.X, color.Value.Y, color.Value.Z, Math.Clamp(alpha, 0f, 1f)) };

	/// <summary>Returns the per-channel inverse (photographic negative) in sRGB, preserving alpha.</summary>
	/// <param name="color">The color.</param>
	/// <returns>The inverted color.</returns>
	public static ImColor Invert(this ImColor color) => Rebuild(color, ToSrgb(color).Invert());

	// --- Analysis (linear-space color science) ---
	/// <summary>The WCAG relative luminance (computed on linear channels).</summary>
	/// <param name="color">The color.</param>
	/// <returns>The relative luminance.</returns>
	public static float GetRelativeLuminance(this ImColor color) => (float)color.FromImColor().RelativeLuminance;

	/// <summary>The WCAG contrast ratio (1-21) of this color over a background.</summary>
	/// <param name="color">The color.</param>
	/// <param name="background">The background color.</param>
	/// <returns>The contrast ratio.</returns>
	public static float GetContrastRatioOver(this ImColor color, ImColor background) =>
		(float)color.FromImColor().ContrastRatio(background.FromImColor());

	/// <summary>The perceptual (Oklab) distance between two colors.</summary>
	/// <param name="color">The first color.</param>
	/// <param name="other">The second color.</param>
	/// <returns>The perceptual distance.</returns>
	public static float GetColorDistance(this ImColor color, ImColor other) =>
		(float)color.FromImColor().DistanceTo(other.FromImColor());

	// --- Contrast heuristics (UI policy over the primitives above) ---

	/// <summary>
	/// Finds the most readable text color to draw over this background: pure white or black when either
	/// meets <see cref="OptimalTextContrastRatio"/>, otherwise the same-hue lightness with the highest contrast.
	/// </summary>
	/// <param name="background">The background color.</param>
	/// <returns>The text color.</returns>
	public static ImColor MostReadableTextColor(this ImColor background)
	{
		ImColor white = new Srgb(1, 1, 1).ToImColor();
		ImColor black = new Srgb(0, 0, 0).ToImColor();

		float whiteContrast = white.GetContrastRatioOver(background);
		float blackContrast = black.GetContrastRatioOver(background);

		if (whiteContrast >= OptimalTextContrastRatio || blackContrast >= OptimalTextContrastRatio)
		{
			return whiteContrast > blackContrast ? white : black;
		}

		float bestLightness = 0f;
		float bestContrast = 0f;
		const int steps = 256;
		for (int i = 0; i < steps; i++)
		{
			float l = i / (steps - 1f);
			float contrast = background.WithLightness(l).GetContrastRatioOver(background);
			if (contrast > bestContrast)
			{
				bestContrast = contrast;
				bestLightness = l;
			}
		}

		return background.WithLightness(bestLightness);
	}

	/// <summary>
	/// Adjusts this background's lightness until <paramref name="textColor"/> reads against it at the target
	/// contrast ratio, preferring the lightness closest to the original. Returns the background unchanged when
	/// contrast is already sufficient or no lightness reaches the target.
	/// </summary>
	/// <param name="background">The background color to adjust.</param>
	/// <param name="textColor">The text color that must remain readable.</param>
	/// <param name="targetContrastRatio">The target ratio (defaults to <see cref="OptimalTextContrastRatio"/>).</param>
	/// <returns>The adjusted background color.</returns>
	public static ImColor AdjustForSufficientContrast(this ImColor background, ImColor textColor, float? targetContrastRatio = null)
	{
		float targetRatio = targetContrastRatio ?? OptimalTextContrastRatio;
		float currentContrast = textColor.GetContrastRatioOver(background);
		if (currentContrast >= targetRatio)
		{
			return background;
		}

		float originalLightness = (float)background.ToHsl().L;
		float bestLightness = originalLightness;
		float bestContrast = currentContrast;
		const int steps = 256;
		for (int i = 0; i < steps; i++)
		{
			float l = i / (steps - 1f);
			float contrast = textColor.GetContrastRatioOver(background.WithLightness(l));
			if (contrast >= targetRatio)
			{
				float difference = Math.Abs(l - originalLightness);
				float bestDifference = Math.Abs(bestLightness - originalLightness);
				if (contrast > bestContrast || difference < bestDifference)
				{
					bestContrast = contrast;
					bestLightness = l;
				}
			}
		}

		return background.WithLightness(bestLightness);
	}

	/// <summary>
	/// Converts an ImColor to an Srgb color.
	/// </summary>
	/// <param name="color"></param>
	/// <returns>The Srgb color.</returns>
	public static Srgb ToSrgb(ImColor color) => new(color.Value.X, color.Value.Y, color.Value.Z);

	/// <summary>Converts to HSL. Hue is in degrees (0-360); saturation and lightness are 0-1.</summary>
	/// <param name="color">The color.</param>
	/// <returns>The HSL value.</returns>
	public static Hsl ToHsl(this ImColor color) => ToSrgb(color).ToHsl();

	/// <summary>
	/// Converts an ImColor to an ImGuiVector4
	/// </summary>
	/// <param name="color"></param>
	/// <returns>The ImGuiVector4 representation of the color.</returns>
	public static ImGuiVector4 ToImGuiVector4(this ImColor color) =>
		new(color.Value.X, color.Value.Y, color.Value.Z, color.Value.W);

	private static ImColor Rebuild(ImColor original, Srgb adjusted) =>
		new() { Value = new Vector4((float)adjusted.R, (float)adjusted.G, (float)adjusted.B, original.Value.W) };
}
