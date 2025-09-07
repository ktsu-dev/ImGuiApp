// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Styler;

using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ThemeProvider;

/// <summary>
/// Provides methods for creating and manipulating colors in ImGui.
/// </summary>
public static class Color
{
	/// <summary>
	/// Represents the optimal text contrast ratio for accessibility.
	/// </summary>
	public const float OptimalTextContrastRatio = 4.5f;

	#region Color Creation Methods

	/// <summary>
	/// Converts a hexadecimal color string to an <see cref="ImColor"/> object.
	/// </summary>
	/// <param name="hex">The hexadecimal color string in the format #RRGGBB or #RRGGBBAA.</param>
	/// <returns>An <see cref="ImColor"/> object representing the color.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the <paramref name="hex"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when the <paramref name="hex"/> is not in the correct format.</exception>
	public static ImColor FromHex(string hex)
	{
		ArgumentNullException.ThrowIfNull(hex, nameof(hex));

		if (hex.StartsWith('#'))
		{
			hex = hex[1..];
		}

		if (hex.Length == 6)
		{
			hex += "FF";
		}

		if (hex.Length != 8)
		{
			throw new ArgumentException("Hex color must be in the format #RRGGBB or #RRGGBBAA", nameof(hex));
		}

		byte r = byte.Parse(hex.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
		byte g = byte.Parse(hex.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
		byte b = byte.Parse(hex.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
		byte a = byte.Parse(hex.AsSpan(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

		return FromRGBA(r, g, b, a);
	}

	/// <summary>
	/// Creates an <see cref="ImColor"/> object from RGB byte values.
	/// </summary>
	/// <param name="r">The red component value (0-255).</param>
	/// <param name="g">The green component value (0-255).</param>
	/// <param name="b">The blue component value (0-255).</param>
	/// <returns>An <see cref="ImColor"/> object representing the color.</returns>
	public static ImColor FromRGB(byte r, byte g, byte b) => new()
	{
		Value = new Vector4(r / 255f, g / 255f, b / 255f, 1f)
	};

	/// <summary>
	/// Creates an <see cref="ImColor"/> object from RGBA byte values.
	/// </summary>
	/// <param name="r">The red component value (0-255).</param>
	/// <param name="g">The green component value (0-255).</param>
	/// <param name="b">The blue component value (0-255).</param>
	/// <param name="a">The alpha component value (0-255).</param>
	/// <returns>An <see cref="ImColor"/> object representing the color.</returns>
	public static ImColor FromRGBA(byte r, byte g, byte b, byte a) => new()
	{
		Value = new Vector4(r / 255f, g / 255f, b / 255f, a / 255f)
	};

	/// <summary>
	/// Creates an <see cref="ImColor"/> object from RGB float values.
	/// </summary>
	/// <param name="r">The red component value (0-1).</param>
	/// <param name="g">The green component value (0-1).</param>
	/// <param name="b">The blue component value (0-1).</param>
	/// <returns>An <see cref="ImColor"/> object representing the color.</returns>
	public static ImColor FromRGB(float r, float g, float b) => new()
	{
		Value = new Vector4(r, g, b, 1f)
	};

	/// <summary>
	/// Creates an <see cref="ImColor"/> object from RGBA float values.
	/// </summary>
	/// <param name="r">The red component value (0-1).</param>
	/// <param name="g">The green component value (0-1).</param>
	/// <param name="b">The blue component value (0-1).</param>
	/// <param name="a">The alpha component value (0-1).</param>
	/// <returns>An <see cref="ImColor"/> object representing the color.</returns>
	public static ImColor FromRGBA(float r, float g, float b, float a) => new()
	{
		Value = new Vector4(r, g, b, a)
	};

	/// <summary>
	/// Creates an <see cref="ImColor"/> object from a <see cref="Vector3"/>.
	/// </summary>
	/// <param name="vector">The vector containing RGB values.</param>
	/// <returns>An <see cref="ImColor"/> object representing the color.</returns>
	public static ImColor FromVector(Vector3 vector) => new()
	{
		Value = new Vector4(vector.X, vector.Y, vector.Z, 1f)
	};

	/// <summary>
	/// Creates an <see cref="ImColor"/> object from a <see cref="Vector4"/>.
	/// </summary>
	/// <param name="vector">The vector containing RGBA values.</param>
	/// <returns>An <see cref="ImColor"/> object representing the color.</returns>
	public static ImColor FromVector(Vector4 vector) => new()
	{
		Value = vector
	};

	/// <summary>
	/// Creates an <see cref="ImColor"/> object from HSL values.
	/// </summary>
	/// <param name="vector">The vector containing HSL values.</param>
	/// <returns>An <see cref="ImColor"/> object representing the color.</returns>
	public static ImColor FromHSL(Vector3 vector) => FromHSLA(vector.X, vector.Y, vector.Z, 1);

	/// <summary>
	/// Creates an <see cref="ImColor"/> object from HSLA values.
	/// </summary>
	/// <param name="vector">The vector containing HSLA values.</param>
	/// <returns>An <see cref="ImColor"/> object representing the color.</returns>
	public static ImColor FromHSLA(Vector4 vector) => FromHSLA(vector.X, vector.Y, vector.Z, vector.W);

	/// <summary>
	/// Creates an <see cref="ImColor"/> object from HSL values.
	/// </summary>
	/// <param name="h">The hue component value (0-1).</param>
	/// <param name="s">The saturation component value (0-1).</param>
	/// <param name="l">The lightness component value (0-1).</param>
	/// <returns>An <see cref="ImColor"/> object representing the color.</returns>
	public static ImColor FromHSL(float h, float s, float l) => FromHSLA(h, s, l, 1);

	/// <summary>
	/// Creates an <see cref="ImColor"/> object from HSLA values.
	/// </summary>
	/// <param name="h">The hue component value (0-1).</param>
	/// <param name="s">The saturation component value (0-1).</param>
	/// <param name="l">The lightness component value (0-1).</param>
	/// <param name="a">The alpha component value (0-1).</param>
	/// <returns>An <see cref="ImColor"/> object representing the color.</returns>
	public static ImColor FromHSLA(float h, float s, float l, float a)
	{
		float r, g, b;

		if (s == 0)
		{
			r = g = b = l;
		}
		else
		{
			float q = l < 0.5f ? l * (1f + s) : l + s - (l * s);
			float p = (2f * l) - q;
			r = HueToRGB(p, q, h + (1f / 3f));
			g = HueToRGB(p, q, h);
			b = HueToRGB(p, q, h - (1f / 3f));
		}

		return FromRGBA(r, g, b, a);
	}

	/// <summary>
	/// Converts a PerceptualColor from ThemeProvider to an ImColor.
	/// </summary>
	/// <param name="color">The PerceptualColor to convert.</param>
	/// <returns>An ImColor representing the same color.</returns>
	public static ImColor FromPerceptualColor(PerceptualColor color)
	{
		RgbColor rgb = color.RgbValue;
		return FromRGBA(rgb.R, rgb.G, rgb.B, 1f);
	}

	#endregion

	#region Private Helper Methods

	/// <summary>
	/// Converts a hue to an RGB component.
	/// </summary>
	/// <param name="p">The first parameter for the conversion.</param>
	/// <param name="q">The second parameter for the conversion.</param>
	/// <param name="t">The hue value.</param>
	/// <returns>The RGB component value.</returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Clarity over brevity")]
	private static float HueToRGB(float p, float q, float t)
	{
		if (t < 0)
		{
			t += 1;
		}

		if (t > 1)
		{
			t -= 1;
		}

		if (t < 1f / 6f)
		{
			return p + ((q - p) * 6f * t);
		}

		if (t < 1f / 2f)
		{
			return q;
		}

		if (t < 2f / 3f)
		{
			return p + ((q - p) * ((2f / 3f) - t) * 6f);
		}

		return p;
	}

	/// <summary>
	/// Gets a semantic color from the current theme, or a fallback color if no theme is applied.
	/// This should only be used for semantic UI meanings.
	/// </summary>
	/// <param name="meaning">The semantic meaning of the color.</param>
	/// <param name="priority">The priority level for the color.</param>
	/// <param name="fallbackColor">The fallback color to use if no theme is applied.</param>
	/// <returns>An ImColor from the current theme or the fallback color.</returns>
	private static ImColor GetSemanticColor(SemanticMeaning meaning, Priority priority, ImColor fallbackColor)
	{
		// Check if a theme is currently applied
		if (Theme.CurrentTheme is not null)
		{
			try
			{
				// Create a semantic color request
				SemanticColorRequest request = new(meaning, priority);

				// Use SemanticColorMapper to get the color from the current theme
				IReadOnlyDictionary<SemanticColorRequest, PerceptualColor> colorMapping = SemanticColorMapper.MapColors([request], Theme.CurrentTheme.CreateInstance());

				if (colorMapping.TryGetValue(request, out PerceptualColor perceptualColor))
				{
					return FromPerceptualColor(perceptualColor);
				}
			}
			catch (ArgumentException)
			{
				// Invalid arguments for theme mapping
			}
			catch (InvalidOperationException)
			{
				// Theme operation failed
			}
		}

		// Fall back to hardcoded color if no theme is applied or mapping fails
		return fallbackColor;
	}

	/// <summary>
	/// Gets a color from the current theme that is closest to the desired default color,
	/// or returns the fallback color if no theme is applied.
	/// This preserves the intended hue while adapting to the theme's color scheme.
	/// </summary>
	/// <param name="fallbackColor">The default hardcoded color to find a close match for.</param>
	/// <returns>An ImColor that's close to the fallback color within the current theme, or the fallback color itself.</returns>
	private static ImColor GetThemeColor(ImColor fallbackColor)
	{
		// Check if a theme is currently applied and get its complete palette
		IReadOnlyDictionary<SemanticColorRequest, PerceptualColor>? completePalette = Theme.GetCurrentThemeCompletePalette();
		if (completePalette is not null)
		{
			try
			{
				// Convert the fallback color to PerceptualColor for comparison
				RgbColor fallbackRgb = new(fallbackColor.Value.X, fallbackColor.Value.Y, fallbackColor.Value.Z);
				PerceptualColor targetColor = new(fallbackRgb);

				PerceptualColor? closestColor = null;
				float closestDistance = float.MaxValue;

				// Search through the complete palette to find the closest match
				// This is much more efficient than nested loops through semantic mappings
				foreach (PerceptualColor color in completePalette.Values)
				{
					float distance = targetColor.SemanticDistanceTo(color);
					if (distance < closestDistance)
					{
						closestDistance = distance;
						closestColor = color;
					}
				}

				// If we found a reasonably close color, use it
				if (closestColor.HasValue && closestDistance < 0.3f) // Reasonable similarity threshold
				{
					return FromPerceptualColor(closestColor.Value);
				}
			}
			catch (ArgumentException)
			{
				// Invalid arguments for theme color matching
			}
			catch (InvalidOperationException)
			{
				// Theme operation failed
			}
		}

		// Fall back to hardcoded color if no theme is applied or no close match found
		return fallbackColor;
	}

	#endregion

	/// <summary>
	/// Comprehensive color palette with organized categories.
	/// Semantic colors are sourced from the current theme's semantic meanings.
	/// Other colors try to find close matches in the theme while preserving intended hues.
	/// </summary>
	public static class Palette
	{
		/// <summary>
		/// Basic primary and secondary colors.
		/// These try to find close colors in the current theme while preserving the intended hue.
		/// </summary>
		public static class Basic
		{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
			public static ImColor Red => GetThemeColor(FromHex("#ff4a49"));
			public static ImColor Green => GetThemeColor(FromHex("#49ff4a"));
			public static ImColor Blue => GetThemeColor(FromHex("#49a3ff"));
			public static ImColor Yellow => GetThemeColor(FromHex("#ecff49"));
			public static ImColor Cyan => GetThemeColor(FromHex("#49feff"));
			public static ImColor Magenta => GetThemeColor(FromHex("#ff49fe"));
			public static ImColor Orange => GetThemeColor(FromHex("#ffa549"));
			public static ImColor Pink => GetThemeColor(FromHex("#ff49a3"));
			public static ImColor Lime => GetThemeColor(FromHex("#a3ff49"));
			public static ImColor Purple => GetThemeColor(FromHex("#c949ff"));
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
		}

		/// <summary>
		/// Neutral colors for backgrounds, borders, and subtle elements.
		/// These try to find close colors in the current theme while preserving the intended lightness.
		/// </summary>
		public static class Neutral
		{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
			public static ImColor White => GetThemeColor(FromHex("#ffffff"));
			public static ImColor Black => GetThemeColor(FromHex("#000000"));
			public static ImColor Gray => GetThemeColor(FromHex("#808080"));
			public static ImColor LightGray => GetThemeColor(FromHex("#c0c0c0"));
			public static ImColor DarkGray => GetThemeColor(FromHex("#404040"));
			public static ImColor Transparent => FromHex("#00000000"); // Always transparent
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
		}

		/// <summary>
		/// Semantic colors for UI states and meanings.
		/// These are mapped directly to their semantic meanings in the current theme.
		/// </summary>
		public static class Semantic
		{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
			public static ImColor Error => GetSemanticColor(SemanticMeaning.Error, Priority.High, Basic.Red);
			public static ImColor Warning => GetSemanticColor(SemanticMeaning.Warning, Priority.High, Basic.Orange);
			public static ImColor Success => GetSemanticColor(SemanticMeaning.Success, Priority.High, Basic.Green);
			public static ImColor Info => GetSemanticColor(SemanticMeaning.Information, Priority.High, Basic.Cyan);
			public static ImColor Primary => GetSemanticColor(SemanticMeaning.Primary, Priority.High, Basic.Blue);
			public static ImColor Secondary => GetSemanticColor(SemanticMeaning.Alternate, Priority.High, Basic.Purple);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
		}

		/// <summary>
		/// Natural and earthy colors.
		/// These try to find close colors in the current theme while preserving the intended natural hue.
		/// </summary>
		public static class Natural
		{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
			public static ImColor Brown => GetThemeColor(FromRGB(165, 42, 42));
			public static ImColor Olive => GetThemeColor(FromRGB(128, 128, 0));
			public static ImColor Maroon => GetThemeColor(FromRGB(128, 0, 0));
			public static ImColor Navy => GetThemeColor(FromRGB(0, 0, 128));
			public static ImColor Teal => GetThemeColor(FromRGB(0, 128, 128));
			public static ImColor Indigo => GetThemeColor(FromRGB(75, 0, 130));
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
		}

		/// <summary>
		/// Vibrant and colorful shades.
		/// These try to find close colors in the current theme while preserving the intended vibrant character.
		/// </summary>
		public static class Vibrant
		{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
			public static ImColor Coral => GetThemeColor(FromRGB(255, 127, 80));
			public static ImColor Salmon => GetThemeColor(FromRGB(250, 128, 114));
			public static ImColor Turquoise => GetThemeColor(FromRGB(64, 224, 208));
			public static ImColor Violet => GetThemeColor(FromRGB(238, 130, 238));
			public static ImColor Gold => GetThemeColor(FromRGB(255, 215, 0));
			public static ImColor Silver => GetThemeColor(FromRGB(192, 192, 192));
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
		}

		/// <summary>
		/// Soft, pastel colors for gentle UIs.
		/// These try to find close colors in the current theme while preserving the intended pastel softness.
		/// </summary>
		public static class Pastel
		{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
			public static ImColor Beige => GetThemeColor(FromRGB(245, 245, 220));
			public static ImColor Peach => GetThemeColor(FromRGB(255, 218, 185));
			public static ImColor Mint => GetThemeColor(FromRGB(189, 252, 201));
			public static ImColor Lavender => GetThemeColor(FromRGB(230, 230, 250));
			public static ImColor Khaki => GetThemeColor(FromRGB(240, 230, 140));
			public static ImColor Plum => GetThemeColor(FromRGB(221, 160, 221));
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
		}
	}
}
