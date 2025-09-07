// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Styler;

using System.Numerics;
using Hexa.NET.ImGui;

/// <summary>
/// Extension methods for color manipulation and analysis.
/// </summary>
public static class ColorExtensions
{
	/// <summary>
	/// Desaturates the color by a specified amount.
	/// </summary>
	/// <param name="color">The original color.</param>
	/// <param name="amount">The amount to desaturate (0-1).</param>
	/// <returns>A new <see cref="ImColor"/> object with the adjusted saturation.</returns>
	public static ImColor DesaturateBy(this ImColor color, float amount)
	{
		Vector4 hsla = color.ToHSLA();
		hsla.Y = Math.Clamp(hsla.Y - amount, 0, 1);
		return Color.FromHSLA(hsla);
	}

	/// <summary>
	/// Saturates the color by a specified amount.
	/// </summary>
	/// <param name="color">The original color.</param>
	/// <param name="amount">The amount to saturate (0-1).</param>
	/// <returns>A new <see cref="ImColor"/> object with the adjusted saturation.</returns>
	public static ImColor SaturateBy(this ImColor color, float amount)
	{
		Vector4 hsla = color.ToHSLA();
		hsla.Y = Math.Clamp(hsla.Y + amount, 0, 1);
		return Color.FromHSLA(hsla);
	}

	/// <summary>
	/// Sets the saturation of the color to a specified amount.
	/// </summary>
	/// <param name="color">The original color.</param>
	/// <param name="amount">The new saturation value (0-1).</param>
	/// <returns>A new <see cref="ImColor"/> object with the adjusted saturation.</returns>
	public static ImColor WithSaturation(this ImColor color, float amount)
	{
		Vector4 hsla = color.ToHSLA();
		hsla.Y = Math.Clamp(amount, 0, 1);
		return Color.FromHSLA(hsla);
	}

	/// <summary>
	/// Multiplies the saturation of the color by a specified amount.
	/// </summary>
	/// <param name="color">The original color.</param>
	/// <param name="amount">The factor to multiply the saturation by.</param>
	/// <returns>A new <see cref="ImColor"/> object with the adjusted saturation.</returns>
	public static ImColor MultiplySaturation(this ImColor color, float amount)
	{
		Vector4 hsla = color.ToHSLA();
		hsla.Y = Math.Clamp(hsla.Y * amount, 0, 1);
		return Color.FromHSLA(hsla);
	}

	/// <summary>
	/// Offsets the hue of the color by a specified amount.
	/// </summary>
	/// <param name="color">The original color.</param>
	/// <param name="amount">The amount to offset the hue by (0-1).</param>
	/// <returns>A new <see cref="ImColor"/> object with the adjusted hue.</returns>
	public static ImColor OffsetHue(this ImColor color, float amount)
	{
		Vector4 hsla = color.ToHSLA();
		hsla.X = (1f + (hsla.X + amount)) % 1f;
		return Color.FromHSLA(hsla);
	}

	/// <summary>
	/// Lightens the color by a specified amount.
	/// </summary>
	/// <param name="color">The original color.</param>
	/// <param name="amount">The amount to lighten the color by (0-1).</param>
	/// <returns>A new <see cref="ImColor"/> object with the adjusted lightness.</returns>
	public static ImColor LightenBy(this ImColor color, float amount)
	{
		Vector4 hsla = color.ToHSLA();
		hsla.Z = Math.Clamp(hsla.Z + amount, 0, 1);
		return Color.FromHSLA(hsla);
	}

	/// <summary>
	/// Darkens the color by a specified amount.
	/// </summary>
	/// <param name="color">The original color.</param>
	/// <param name="amount">The amount to darken the color by (0-1).</param>
	/// <returns>A new <see cref="ImColor"/> object with the adjusted lightness.</returns>
	public static ImColor DarkenBy(this ImColor color, float amount)
	{
		Vector4 hsla = color.ToHSLA();
		hsla.Z = Math.Clamp(hsla.Z - amount, 0, 1);
		return Color.FromHSLA(hsla);
	}

	/// <summary>
	/// Sets the luminance of the color to a specified amount.
	/// </summary>
	/// <param name="color">The original color.</param>
	/// <param name="amount">The new luminance value (0-1).</param>
	/// <returns>A new <see cref="ImColor"/> object with the adjusted luminance.</returns>
	public static ImColor WithLuminance(this ImColor color, float amount)
	{
		Vector4 hsla = color.ToHSLA();
		hsla.Z = Math.Clamp(amount, 0, 1);
		return Color.FromHSLA(hsla);
	}

	/// <summary>
	/// Multiplies the luminance of the color by a specified amount.
	/// </summary>
	/// <param name="color">The original color.</param>
	/// <param name="amount">The factor to multiply the luminance by.</param>
	/// <returns>A new <see cref="ImColor"/> object with the adjusted luminance.</returns>
	public static ImColor MultiplyLuminance(this ImColor color, float amount)
	{
		Vector4 hsla = color.ToHSLA();
		hsla.Z = Math.Clamp(hsla.Z * amount, 0, 1);
		return Color.FromHSLA(hsla);
	}

	/// <summary>
	/// Sets the alpha of the color to a specified amount.
	/// </summary>
	/// <param name="color">The original color.</param>
	/// <param name="amount">The new alpha value (0-1).</param>
	/// <returns>A new <see cref="ImColor"/> object with the adjusted alpha.</returns>
	public static ImColor WithAlpha(this ImColor color, float amount)
	{
		Vector4 hsla = color.ToHSLA();
		hsla.W = Math.Clamp(amount, 0, 1);
		return Color.FromHSLA(hsla);
	}

	/// <summary>
	/// Converts the color to grayscale.
	/// </summary>
	/// <param name="color">The original color.</param>
	/// <returns>A new <see cref="ImColor"/> object in grayscale.</returns>
	public static ImColor ToGrayscale(this ImColor color) => color.WithSaturation(0);

	/// <summary>
	/// Converts the color to HSLA (Hue, Saturation, Lightness, Alpha) format.
	/// </summary>
	/// <param name="color">The original color.</param>
	/// <returns>A <see cref="Vector4"/> representing the color in HSLA format.</returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0045:Convert to conditional expression", Justification = "Clarity over brevity")]
	public static Vector4 ToHSLA(this ImColor color)
	{
		float r = color.Value.X;
		float g = color.Value.Y;
		float b = color.Value.Z;
		float a = color.Value.W;

		float max = Math.Max(r, Math.Max(g, b));
		float min = Math.Min(r, Math.Min(g, b));
		float h, s, l = (max + min) / 2f;

		if (max == min)
		{
			h = s = 0;
		}
		else
		{
			float d = max - min;
			s = l > 0.5f ? d / (2f - max - min) : d / (max + min);
			if (max == r)
			{
				h = (g - b) / d;
			}
			else if (max == g)
			{
				h = ((b - r) / d) + 2;
			}
			else
			{
				h = ((r - g) / d) + 4;
			}

			h /= 6;
			if (h < 0)
			{
				h += 1;
			}
		}

		return new Vector4(h, s, l, a);
	}

	/// <summary>
	/// Gets the relative luminance of the color.
	/// </summary>
	/// <param name="color">The original color.</param>
	/// <returns>The relative luminance of the color.</returns>
	public static float GetRelativeLuminance(this ImColor color) =>
		(color.Value.X * 0.2126f) + (color.Value.Y * 0.7152f) + (color.Value.Z * 0.0722f);

	/// <summary>
	/// Calculates the contrast ratio of the color over a background color.
	/// </summary>
	/// <param name="color">The original color.</param>
	/// <param name="background">The background color.</param>
	/// <returns>The contrast ratio of the color over the background color.</returns>
	public static float GetContrastRatioOver(this ImColor color, ImColor background)
	{
		float relativeLuminance = color.GetRelativeLuminance();
		float backgroundRelativeLuminance = background.GetRelativeLuminance();

		// Ensure lighter color is in numerator for proper contrast ratio calculation
		float lighter = Math.Max(relativeLuminance, backgroundRelativeLuminance);
		float darker = Math.Min(relativeLuminance, backgroundRelativeLuminance);

		return (lighter + 0.05f) / (darker + 0.05f);
	}

	/// <summary>
	/// Calculates the optimal contrasting color for the given color.
	/// </summary>
	/// <param name="color">The original color.</param>
	/// <returns>A new <see cref="ImColor"/> object representing the optimal contrasting color.</returns>
	public static ImColor CalculateOptimalContrastingColor(this ImColor color)
	{
		// Try pure white and pure black first as they often provide the best contrast
		ImColor white = Color.FromRGBA(1f, 1f, 1f, 1f);
		ImColor black = Color.FromRGBA(0f, 0f, 0f, 1f);

		float whiteContrast = white.GetContrastRatioOver(color);
		float blackContrast = black.GetContrastRatioOver(color);

		// If either pure white or black provides sufficient contrast, use the better one
		if (whiteContrast >= Color.OptimalTextContrastRatio || blackContrast >= Color.OptimalTextContrastRatio)
		{
			return whiteContrast > blackContrast ? white : black;
		}

		// Otherwise, search for optimal luminance
		float bestLuminance = 0;
		float bestContrast = 0;
		int steps = 256;

		for (int i = 0; i < steps; i++)
		{
			float l = i / (steps - 1f);
			ImColor candidateColor = color.WithLuminance(l);
			float contrast = candidateColor.GetContrastRatioOver(color);

			if (contrast > bestContrast)
			{
				bestContrast = contrast;
				bestLuminance = l;
			}
		}

		return color.WithLuminance(bestLuminance);
	}

	/// <summary>
	/// Adjusts the background color to provide sufficient contrast for the given text color.
	/// </summary>
	/// <param name="backgroundColor">The background color to adjust.</param>
	/// <param name="textColor">The text color that needs to be readable.</param>
	/// <param name="targetContrastRatio">The target contrast ratio. If not specified, uses the optimal text contrast ratio.</param>
	/// <returns>A new <see cref="ImColor"/> object representing the adjusted background color.</returns>
	public static ImColor AdjustForSufficientContrast(this ImColor backgroundColor, ImColor textColor, float? targetContrastRatio = null)
	{
		float targetRatio = targetContrastRatio ?? Color.OptimalTextContrastRatio;
		float currentContrast = textColor.GetContrastRatioOver(backgroundColor);

		// If contrast is already sufficient, return the original color
		if (currentContrast >= targetRatio)
		{
			return backgroundColor;
		}

		// Search for optimal luminance that provides the target contrast
		float bestLuminance = backgroundColor.ToHSLA().Z;
		float bestContrast = currentContrast;
		int steps = 256;

		// Try different luminance values to find one that provides sufficient contrast
		for (int i = 0; i < steps; i++)
		{
			float l = i / (steps - 1f);
			ImColor candidateBackground = backgroundColor.WithLuminance(l);
			float contrast = textColor.GetContrastRatioOver(candidateBackground);

			// If we found sufficient contrast, prefer the luminance closest to original
			if (contrast >= targetRatio)
			{
				float luminanceDifference = Math.Abs(l - backgroundColor.ToHSLA().Z);
				float currentBestDifference = Math.Abs(bestLuminance - backgroundColor.ToHSLA().Z);

				if (contrast > bestContrast ||
					(contrast >= targetRatio && luminanceDifference < currentBestDifference))
				{
					bestContrast = contrast;
					bestLuminance = l;
				}
			}
		}

		return backgroundColor.WithLuminance(bestLuminance);
	}

	/// <summary>
	/// Calculates the color distance between two colors using Euclidean distance in RGB space.
	/// </summary>
	/// <param name="color1">The first color.</param>
	/// <param name="color2">The second color.</param>
	/// <returns>The color distance between the two colors (0.0 to ~1.73).</returns>
	public static float GetColorDistance(this ImColor color1, ImColor color2)
	{
		float deltaR = color1.Value.X - color2.Value.X;
		float deltaG = color1.Value.Y - color2.Value.Y;
		float deltaB = color1.Value.Z - color2.Value.Z;

		return (float)Math.Sqrt((deltaR * deltaR) + (deltaG * deltaG) + (deltaB * deltaB));
	}
}
