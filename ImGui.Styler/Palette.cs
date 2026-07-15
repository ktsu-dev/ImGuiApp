// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Styler;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;
using ktsu.Semantics.Color;
using ktsu.ThemeProvider;

/// <summary>
/// Theme-aware color palette. Colors are sourced from the current <see cref="Theme"/>: semantic
/// entries map to their semantic meaning, and the rest snap to the nearest theme color while
/// preserving the intended hue. Color construction and manipulation live in the
/// <c>ktsu.ImGui.Color</c> adapter (<see cref="ImColors"/> and <see cref="ImColorExtensions"/>).
/// </summary>
public static class Palette
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
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3398:Move this method inside 'Semantic'.", Justification = "Private helper pairs with GetThemeColor and reads shared theme state; kept at the palette level for cohesion.")]
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
				IReadOnlyDictionary<SemanticColorRequest, Color> colorMapping = SemanticColorMapper.MapColors([request], Theme.CurrentTheme.CreateInstance());

				if (colorMapping.TryGetValue(request, out Color semanticColor))
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
	/// <param name="fallbackColor">The default color (standard storage) to find a close match for.</param>
	/// <returns>An ImColor that's close to the fallback color within the current theme, or the fallback color itself.</returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3398:Move this method inside 'Basic'.", Justification = "Helper is shared across multiple nested types (Basic, Neutral, Natural, Vibrant, Pastel); moving it into a single nested type would break cohesion.")]
	private static ImColor GetThemeColor(Color fallbackColor)
	{
		// Check if a theme is currently applied and get its complete palette
		IReadOnlyDictionary<SemanticColorRequest, Color>? completePalette = Theme.GetCurrentThemeCompletePalette();
		if (completePalette is not null)
		{
			try
			{
				Color? closestColor = null;
				double closestDistance = double.MaxValue;

				// Search through the complete palette to find the closest match
				// This is much more efficient than nested loops through semantic mappings
				foreach (Color color in completePalette.Values)
				{
					double distance = fallbackColor.DistanceTo(color);
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

		// Fall back to the standard-storage color (converted at the ImGui seam) if no theme is applied or no close match found
		return fallbackColor.ToImColor();
	}

	#endregion

	/// <summary>
	/// Basic primary and secondary colors.
	/// These try to find close colors in the current theme while preserving the intended hue.
	/// </summary>
	public static class Basic
	{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static ImColor Red => GetThemeColor(Color.FromHex("#ff4a49"));
		public static ImColor Green => GetThemeColor(Color.FromHex("#49ff4a"));
		public static ImColor Blue => GetThemeColor(Color.FromHex("#49a3ff"));
		public static ImColor Yellow => GetThemeColor(Color.FromHex("#ecff49"));
		public static ImColor Cyan => GetThemeColor(Color.FromHex("#49feff"));
		public static ImColor Magenta => GetThemeColor(Color.FromHex("#ff49fe"));
		public static ImColor Orange => GetThemeColor(Color.FromHex("#ffa549"));
		public static ImColor Pink => GetThemeColor(Color.FromHex("#ff49a3"));
		public static ImColor Lime => GetThemeColor(Color.FromHex("#a3ff49"));
		public static ImColor Purple => GetThemeColor(Color.FromHex("#c949ff"));
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}

	/// <summary>
	/// Neutral colors for backgrounds, borders, and subtle elements.
	/// These try to find close colors in the current theme while preserving the intended lightness.
	/// </summary>
	public static class Neutral
	{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static ImColor White => GetThemeColor(NamedColors.White);
		public static ImColor Black => GetThemeColor(NamedColors.Black);
		public static ImColor Gray => GetThemeColor(NamedColors.Gray);
		public static ImColor LightGray => GetThemeColor(Color.FromHex("#c0c0c0"));
		public static ImColor DarkGray => GetThemeColor(Color.FromHex("#404040"));
		public static ImColor Transparent => NamedColors.Transparent.ToImColor(); // Always transparent
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
		public static ImColor Brown => GetThemeColor(Color.FromBytes(165, 42, 42));
		public static ImColor Olive => GetThemeColor(Color.FromBytes(128, 128, 0));
		public static ImColor Maroon => GetThemeColor(Color.FromBytes(128, 0, 0));
		public static ImColor Navy => GetThemeColor(Color.FromBytes(0, 0, 128));
		public static ImColor Teal => GetThemeColor(Color.FromBytes(0, 128, 128));
		public static ImColor Indigo => GetThemeColor(Color.FromBytes(75, 0, 130));
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}

	/// <summary>
	/// Vibrant and colorful shades.
	/// These try to find close colors in the current theme while preserving the intended vibrant character.
	/// </summary>
	public static class Vibrant
	{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static ImColor Coral => GetThemeColor(Color.FromBytes(255, 127, 80));
		public static ImColor Salmon => GetThemeColor(Color.FromBytes(250, 128, 114));
		public static ImColor Turquoise => GetThemeColor(Color.FromBytes(64, 224, 208));
		public static ImColor Violet => GetThemeColor(Color.FromBytes(238, 130, 238));
		public static ImColor Gold => GetThemeColor(Color.FromBytes(255, 215, 0));
		public static ImColor Silver => GetThemeColor(Color.FromBytes(192, 192, 192));
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}

	/// <summary>
	/// Soft, pastel colors for gentle UIs.
	/// These try to find close colors in the current theme while preserving the intended pastel softness.
	/// </summary>
	public static class Pastel
	{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static ImColor Beige => GetThemeColor(Color.FromBytes(245, 245, 220));
		public static ImColor Peach => GetThemeColor(Color.FromBytes(255, 218, 185));
		public static ImColor Mint => GetThemeColor(Color.FromBytes(189, 252, 201));
		public static ImColor Lavender => GetThemeColor(Color.FromBytes(230, 230, 250));
		public static ImColor Khaki => GetThemeColor(Color.FromBytes(240, 230, 140));
		public static ImColor Plum => GetThemeColor(Color.FromBytes(221, 160, 221));
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
