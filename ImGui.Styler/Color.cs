// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Styler;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.ThemeProvider;

using SemanticColor = ktsu.Semantics.Color.Color;

/// <summary>
/// Theme-aware color palette. Colors are sourced from the current <see cref="Theme"/>: semantic
/// entries map to their semantic meaning, and the rest snap to the nearest theme color while
/// preserving the intended hue. Color construction and manipulation live in the
/// <c>ktsu.ImGui.Color</c> adapter (<see cref="ImColors"/> and <see cref="ImColorExtensions"/>).
/// </summary>
public static class Color
{
	#region Private Helper Methods

	/// <summary>
	/// Gets a semantic color from the current theme, or a fallback color if no theme is applied.
	/// This should only be used for semantic UI meanings.
	/// </summary>
	/// <param name="meaning">The semantic meaning of the color.</param>
	/// <param name="priority">The priority level for the color.</param>
	/// <param name="fallbackColor">The fallback color to use if no theme is applied.</param>
	/// <returns>An ImColor from the current theme or the fallback color.</returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3398:Move this method inside 'Semantic'.", Justification = "Helper is shared across multiple nested types (Semantic, Palette); moving it into a single nested type would break cohesion.")]
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
				IReadOnlyDictionary<SemanticColorRequest, SemanticColor> colorMapping = SemanticColorMapper.MapColors([request], Theme.CurrentTheme.CreateInstance());

				if (colorMapping.TryGetValue(request, out SemanticColor semanticColor))
				{
					return semanticColor.ToImColor();
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
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3398:Move this method inside 'Palette'.", Justification = "Helper is shared across multiple nested types (Basic, Neutral, Natural, Vibrant, Pastel); moving it into a single nested type would break cohesion.")]
	private static ImColor GetThemeColor(ImColor fallbackColor)
	{
		// Check if a theme is currently applied and get its complete palette
		IReadOnlyDictionary<SemanticColorRequest, SemanticColor>? completePalette = Theme.GetCurrentThemeCompletePalette();
		if (completePalette is not null)
		{
			try
			{
				// Convert the fallback color (sRGB, as shown in ImGui) to a semantic color for comparison
				Vector4 fallbackVec = fallbackColor.Value;
				SemanticColor targetColor = SemanticColor.FromSrgb(fallbackVec.X, fallbackVec.Y, fallbackVec.Z, fallbackVec.W);

				SemanticColor? closestColor = null;
				double closestDistance = double.MaxValue;

				// Search through the complete palette to find the closest match
				// This is much more efficient than nested loops through semantic mappings
				foreach (SemanticColor color in completePalette.Values)
				{
					double distance = targetColor.DistanceTo(color);
					if (distance < closestDistance)
					{
						closestDistance = distance;
						closestColor = color;
					}
				}

				// If we found a reasonably close color, use it
				if (closestColor.HasValue && closestDistance < 0.3) // Reasonable similarity threshold
				{
					return closestColor.Value.ToImColor();
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
			public static ImColor Red => GetThemeColor(ImColors.FromHex("#ff4a49"));
			public static ImColor Green => GetThemeColor(ImColors.FromHex("#49ff4a"));
			public static ImColor Blue => GetThemeColor(ImColors.FromHex("#49a3ff"));
			public static ImColor Yellow => GetThemeColor(ImColors.FromHex("#ecff49"));
			public static ImColor Cyan => GetThemeColor(ImColors.FromHex("#49feff"));
			public static ImColor Magenta => GetThemeColor(ImColors.FromHex("#ff49fe"));
			public static ImColor Orange => GetThemeColor(ImColors.FromHex("#ffa549"));
			public static ImColor Pink => GetThemeColor(ImColors.FromHex("#ff49a3"));
			public static ImColor Lime => GetThemeColor(ImColors.FromHex("#a3ff49"));
			public static ImColor Purple => GetThemeColor(ImColors.FromHex("#c949ff"));
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
		}

		/// <summary>
		/// Neutral colors for backgrounds, borders, and subtle elements.
		/// These try to find close colors in the current theme while preserving the intended lightness.
		/// </summary>
		public static class Neutral
		{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
			public static ImColor White => GetThemeColor(ImColors.FromHex("#ffffff"));
			public static ImColor Black => GetThemeColor(ImColors.FromHex("#000000"));
			public static ImColor Gray => GetThemeColor(ImColors.FromHex("#808080"));
			public static ImColor LightGray => GetThemeColor(ImColors.FromHex("#c0c0c0"));
			public static ImColor DarkGray => GetThemeColor(ImColors.FromHex("#404040"));
			public static ImColor Transparent => ImColors.FromHex("#00000000"); // Always transparent
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
			public static ImColor Brown => GetThemeColor(ImColors.FromRgb(165, 42, 42));
			public static ImColor Olive => GetThemeColor(ImColors.FromRgb(128, 128, 0));
			public static ImColor Maroon => GetThemeColor(ImColors.FromRgb(128, 0, 0));
			public static ImColor Navy => GetThemeColor(ImColors.FromRgb(0, 0, 128));
			public static ImColor Teal => GetThemeColor(ImColors.FromRgb(0, 128, 128));
			public static ImColor Indigo => GetThemeColor(ImColors.FromRgb(75, 0, 130));
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
		}

		/// <summary>
		/// Vibrant and colorful shades.
		/// These try to find close colors in the current theme while preserving the intended vibrant character.
		/// </summary>
		public static class Vibrant
		{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
			public static ImColor Coral => GetThemeColor(ImColors.FromRgb(255, 127, 80));
			public static ImColor Salmon => GetThemeColor(ImColors.FromRgb(250, 128, 114));
			public static ImColor Turquoise => GetThemeColor(ImColors.FromRgb(64, 224, 208));
			public static ImColor Violet => GetThemeColor(ImColors.FromRgb(238, 130, 238));
			public static ImColor Gold => GetThemeColor(ImColors.FromRgb(255, 215, 0));
			public static ImColor Silver => GetThemeColor(ImColors.FromRgb(192, 192, 192));
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
		}

		/// <summary>
		/// Soft, pastel colors for gentle UIs.
		/// These try to find close colors in the current theme while preserving the intended pastel softness.
		/// </summary>
		public static class Pastel
		{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
			public static ImColor Beige => GetThemeColor(ImColors.FromRgb(245, 245, 220));
			public static ImColor Peach => GetThemeColor(ImColors.FromRgb(255, 218, 185));
			public static ImColor Mint => GetThemeColor(ImColors.FromRgb(189, 252, 201));
			public static ImColor Lavender => GetThemeColor(ImColors.FromRgb(230, 230, 250));
			public static ImColor Khaki => GetThemeColor(ImColors.FromRgb(240, 230, 140));
			public static ImColor Plum => GetThemeColor(ImColors.FromRgb(221, 160, 221));
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
		}
	}
}
