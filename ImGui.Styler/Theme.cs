// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Styler;

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ThemeProvider;
using ktsu.ThemeProvider.ImGui;

/// <summary>
/// Provides methods and properties to manage and apply themes for ImGui elements using ThemeProvider.
/// </summary>
public static class Theme
{
	private static readonly ImGuiPaletteMapper paletteMapper = new();
	private static string? currentThemeName;

	// Cache for complete palettes to avoid recalculating them every frame
	private static readonly Dictionary<string, IReadOnlyDictionary<SemanticColorRequest, PerceptualColor>> paletteCache = [];
	private static readonly Lock paletteCacheLock = new();

	// ThemeBrowser modal instance
	private static readonly ThemeBrowser themeBrowser = new();

	#region Theme Application

	/// <summary>
	/// Applies a semantic theme to ImGui using ThemeProvider's color mapping system.
	/// </summary>
	/// <param name="theme">The semantic theme to apply.</param>
	public static void Apply(ISemanticTheme theme)
	{
		Ensure.NotNull(theme);

		// Map the theme to ImGui colors
		IReadOnlyDictionary<ImGuiCol, Vector4> colorMapping = paletteMapper.MapTheme(theme);

		// Apply all colors to ImGui
		unsafe
		{
			ImGuiStylePtr style = ImGui.GetStyle();
			foreach ((ImGuiCol imguiCol, Vector4 color) in colorMapping)
			{
				style.Colors[(int)imguiCol] = color;
			}
		}
	}

	/// <summary>
	/// Gets the color mapping for a semantic theme without applying it.
	/// This is useful for temporary theme application via scoped actions.
	/// </summary>
	/// <param name="theme">The semantic theme to get the color mapping for.</param>
	/// <returns>A dictionary mapping ImGui colors to their theme-based values.</returns>
	public static IReadOnlyDictionary<ImGuiCol, Vector4> GetColorMapping(ISemanticTheme theme)
	{
		Ensure.NotNull(theme);
		return paletteMapper.MapTheme(theme);
	}

	/// <summary>
	/// Applies a theme by name using ThemeRegistry.
	/// </summary>
	/// <param name="themeName">The name of the theme to apply.</param>
	/// <returns>True if the theme was found and applied, false otherwise.</returns>
	public static bool Apply(string themeName)
	{
		ThemeRegistry.ThemeInfo? themeInfo = ThemeRegistry.FindTheme(themeName);
		if (themeInfo is null)
		{
			return false;
		}

		Apply(themeInfo.CreateInstance());
		currentThemeName = themeName;

		// Clear palette cache when theme changes
		ClearPaletteCache();
		return true;
	}

	/// <summary>
	/// Resets ImGui to default styling with no theme applied.
	/// </summary>
	public static void ResetToDefault()
	{
		// Use ImGui's built-in classic color scheme to restore proper defaults
		ImGui.StyleColorsDark();
		currentThemeName = null;

		// Clear palette cache when resetting theme
		ClearPaletteCache();
	}

	#endregion

	#region Current Theme Tracking

	/// <summary>
	/// Gets or sets the name of the currently selected theme.
	/// Setting this will apply the theme if it exists, or reset to default if set to null.
	/// </summary>
	public static string? CurrentThemeName
	{
		get => currentThemeName;
		set
		{
			if (value is null)
			{
				ResetToDefault();
			}
			else if (Apply(value))
			{
				currentThemeName = value;
			}
		}
	}

	/// <summary>
	/// Gets the ThemeInfo for the currently selected theme, if any.
	/// </summary>
	public static ThemeRegistry.ThemeInfo? CurrentTheme =>
		currentThemeName is not null ? FindTheme(currentThemeName) : null;

	#endregion

	#region Theme Menu Rendering

	/// <summary>
	/// Renders a theme selection submenu for use in an application's main menu bar.
	/// </summary>
	/// <param name="menuLabel">The label for the theme submenu (default: "Theme")</param>
	/// <returns>True if a theme was selected and changed, false otherwise.</returns>
	public static bool RenderMenu(string menuLabel = "Theme")
	{
		bool themeChanged = false;

		if (ImGui.BeginMenu(menuLabel))
		{
			// Reset option at the top
			bool isReset = currentThemeName is null;
			if (ImGui.MenuItem("Reset to Default", "", isReset))
			{
				if (!isReset)
				{
					ResetToDefault();
					themeChanged = true;
				}
			}

			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip("Reset to default ImGui styling with no theme applied");
			}

			ImGui.Separator();

			// Group themes by family for better organization
			IOrderedEnumerable<KeyValuePair<string, IReadOnlyList<ThemeRegistry.ThemeInfo>>> themesByFamily = ThemesByFamily.OrderBy(kvp => kvp.Key);

			foreach ((string family, IReadOnlyList<ThemeRegistry.ThemeInfo> themes) in themesByFamily)
			{
				if (themes.Count == 1)
				{
					// Single theme - render directly with color swatches
					ThemeRegistry.ThemeInfo theme = themes[0];
					bool isSelected = currentThemeName == theme.Name;

					if (RenderThemeMenuItemWithDialogSwatches(theme, theme.Name, isSelected))
					{
						if (!isSelected && Apply(theme.Name))
						{
							themeChanged = true;
						}
					}
				}
				else
				{
					themeChanged |= RenderFamilySubmenu(family, themes);
				}
			}

			ImGui.EndMenu();
		}

		return themeChanged;
	}

	/// <summary>
	/// Renders a submenu for a theme family.
	/// </summary>
	/// <param name="family">The theme family name.</param>
	/// <param name="themes">The themes in the family.</param>
	/// <returns>True if a theme was selected and changed, false otherwise.</returns>
	private static bool RenderFamilySubmenu(string family, IReadOnlyList<ThemeRegistry.ThemeInfo> themes)
	{
		bool themeChanged = false;

		// Use dialog window style for the family header using colors from the first theme
		bool anyFamilyThemeSelected = themes.Any(t => t.Name == currentThemeName);

		if (RenderFamilyMenuHeader(family, themes.Count > 0 ? themes[0] : null, anyFamilyThemeSelected))
		{
			try
			{
				// Group by dark/light for families with many variants
				if (themes.Count > 4)
				{
					ThemeRegistry.ThemeInfo[] darkThemes = [.. themes.Where(t => t.IsDark)];
					ThemeRegistry.ThemeInfo[] lightThemes = [.. themes.Where(t => !t.IsDark)];

					themeChanged |= RenderThemeGroup("Dark", darkThemes);

					if (darkThemes.Length > 0 && lightThemes.Length > 0)
					{
						ImGui.Separator();
					}

					themeChanged |= RenderThemeGroup("Light", lightThemes);
				}
				else
				{
					// Few themes - render directly with color swatches
					foreach (ThemeRegistry.ThemeInfo theme in themes)
					{
						bool isSelected = currentThemeName == theme.Name;
						string displayName = theme.Variant ?? theme.Name;

						if (RenderThemeMenuItemWithDialogSwatches(theme, displayName, isSelected))
						{
							if (!isSelected && Apply(theme.Name))
							{
								themeChanged = true;
							}
						}
					}
				}

				ImGui.EndMenu();
			}
			finally
			{
				// Pop the ID that was pushed in RenderFamilyMenuHeader when menu was opened
				ImGui.PopID();
			}
		}

		return themeChanged;
	}

	/// <summary>
	/// Renders a group of themes (e.g., "Dark", "Light").
	/// </summary>
	/// <param name="groupName">The group name.</param>
	/// <param name="themes">The themes to render.</param>
	/// <returns>True if a theme was selected and changed, false otherwise.</returns>
	private static bool RenderThemeGroup(string groupName, ThemeRegistry.ThemeInfo[] themes)
	{
		bool themeChanged = false;

		if (themes.Length > 0)
		{
			if (!string.IsNullOrEmpty(groupName))
			{
				// Use dialog window style for the group header using colors from the first theme
				bool anyGroupThemeSelected = themes.Any(t => t.Name == currentThemeName);
				RenderGroupHeader(groupName, themes[0], anyGroupThemeSelected);
				ImGui.Separator();
			}

			foreach (ThemeRegistry.ThemeInfo theme in themes)
			{
				bool isSelected = currentThemeName == theme.Name;
				string displayName = theme.Variant ?? theme.Name;

				if (RenderThemeMenuItemWithDialogSwatches(theme, displayName, isSelected))
				{
					if (!isSelected && Apply(theme.Name))
					{
						themeChanged = true;
					}
				}
			}
		}

		return themeChanged;
	}

	/// <summary>
	/// Renders a simple theme selection menu without family grouping.
	/// </summary>
	/// <param name="menuLabel">The label for the theme submenu (default: "Theme")</param>
	/// <returns>True if a theme was selected and changed, false otherwise.</returns>
	public static bool RenderSimpleMenu(string menuLabel = "Theme")
	{
		bool themeChanged = false;

		if (ImGui.BeginMenu(menuLabel))
		{
			// Reset option at the top
			bool isReset = currentThemeName is null;
			if (ImGui.MenuItem("Reset to Default", "", isReset))
			{
				if (!isReset)
				{
					ResetToDefault();
					themeChanged = true;
				}
			}

			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip("Reset to default ImGui styling with no theme applied");
			}

			ImGui.Separator();

			// Group by dark/light
			ThemeRegistry.ThemeInfo[] darkThemes = [.. DarkThemes.OrderBy(t => t.Name)];
			ThemeRegistry.ThemeInfo[] lightThemes = [.. LightThemes.OrderBy(t => t.Name)];

			if (darkThemes.Length > 0)
			{
				ImGui.TextDisabled("Dark Themes");
				ImGui.Separator();

				foreach (ThemeRegistry.ThemeInfo theme in darkThemes)
				{
					bool isSelected = currentThemeName == theme.Name;

					if (RenderThemeMenuItemWithDialogSwatches(theme, theme.Name, isSelected))
					{
						if (!isSelected && Apply(theme.Name))
						{
							themeChanged = true;
						}
					}
				}

				if (lightThemes.Length > 0)
				{
					ImGui.Separator();
				}
			}

			if (lightThemes.Length > 0)
			{
				if (darkThemes.Length > 0)
				{
					ImGui.TextDisabled("Light Themes");
					ImGui.Separator();
				}

				foreach (ThemeRegistry.ThemeInfo theme in lightThemes)
				{
					bool isSelected = currentThemeName == theme.Name;

					if (RenderThemeMenuItemWithDialogSwatches(theme, theme.Name, isSelected))
					{
						if (!isSelected && Apply(theme.Name))
						{
							themeChanged = true;
						}
					}
				}
			}

			ImGui.EndMenu();
		}

		return themeChanged;
	}

	#endregion

	#region Theme Menu Helpers

	/// <summary>
	/// Renders a theme menu item with color preview swatches
	/// </summary>
	/// <param name="theme">The theme to render.</param>
	/// <param name="displayName">The display name for the theme.</param>
	/// <param name="isSelected">Whether this theme is currently selected.</param>
	/// <returns>True if the theme was clicked, false otherwise.</returns>
#pragma warning disable IDE0051 // Remove unused private members - preserved for reference
	private static bool RenderThemeMenuItemWithSwatches(ThemeRegistry.ThemeInfo theme, string displayName, bool isSelected)
	{
		bool clicked = false;

		// Create a unique ID for this menu item
		ImGui.PushID(theme.Name);

		try
		{
			// Get the theme's complete palette for color preview
			IReadOnlyDictionary<SemanticColorRequest, PerceptualColor> completePalette = GetCompletePalette(theme.CreateInstance());

			// Define the colors we want to show: primary, alternate, medium neutral, low neutral
			SemanticColorRequest[] colorRequests = [
				new SemanticColorRequest(SemanticMeaning.Primary, Priority.High),
				new SemanticColorRequest(SemanticMeaning.Alternate, Priority.High),
				new SemanticColorRequest(SemanticMeaning.Neutral, Priority.Medium),
				new SemanticColorRequest(SemanticMeaning.Neutral, Priority.Low)
			];

			// Use Selectable instead of MenuItem so we can draw custom content
			if (ImGui.Selectable($"##theme_{theme.Name}", isSelected, ImGuiSelectableFlags.None))
			{
				clicked = true;
			}

			// Draw color swatches and theme name on top of the selectable
			Vector2 itemMin = ImGui.GetItemRectMin();
			Vector2 itemMax = ImGui.GetItemRectMax();
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();

			float swatchSize = 12.0f;
			float swatchSpacing = 2.0f;
			float textOffset = (colorRequests.Length * (swatchSize + swatchSpacing)) + 8.0f;

			// Draw color swatches
			for (int i = 0; i < colorRequests.Length; i++)
			{
				if (completePalette.TryGetValue(colorRequests[i], out PerceptualColor color))
				{
					ImColor imColor = Color.FromPerceptualColor(color);
					Vector2 swatchPos = new(
						itemMin.X + 4.0f + (i * (swatchSize + swatchSpacing)),
						itemMin.Y + ((itemMax.Y - itemMin.Y - swatchSize) * 0.5f)
					);

					// Draw swatch background (slightly larger for border effect)
					drawList.AddRectFilled(
						swatchPos - Vector2.One,
						swatchPos + new Vector2(swatchSize + 1, swatchSize + 1),
						ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.2f, 0.2f, 1.0f))
					);

					// Draw color swatch
					drawList.AddRectFilled(
						swatchPos,
						swatchPos + new Vector2(swatchSize, swatchSize),
						ImGui.ColorConvertFloat4ToU32(imColor.Value)
					);
				}
			}

			// Draw theme name text
			Vector2 textPos = new(itemMin.X + textOffset, itemMin.Y + ((itemMax.Y - itemMin.Y - ImGui.GetTextLineHeight()) * 0.5f));
			uint textColor = isSelected ?
				ImGui.ColorConvertFloat4ToU32(ImGui.GetStyle().Colors[(int)ImGuiCol.Text]) :
				ImGui.ColorConvertFloat4ToU32(ImGui.GetStyle().Colors[(int)ImGuiCol.Text]);

			drawList.AddText(textPos, textColor, displayName);

			// Add tooltip with theme description if hovered
			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip($"{theme.Description}\n\nColors shown: Primary, Alternate, Neutral (Med), Neutral (Low)");
			}
		}
		catch (ArgumentException)
		{
			// Fallback to simple menu item if color extraction fails
			clicked = ImGui.MenuItem(displayName, "", isSelected);
			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip(theme.Description);
			}
		}
		catch (InvalidOperationException)
		{
			// Fallback to simple menu item if color extraction fails
			clicked = ImGui.MenuItem(displayName, "", isSelected);
			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip(theme.Description);
			}
		}
		catch (System.Reflection.TargetInvocationException)
		{
			// Fallback to simple menu item if reflection call fails
			clicked = ImGui.MenuItem(displayName, "", isSelected);
			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip(theme.Description);
			}
		}
		finally
		{
			ImGui.PopID();
		}

		return clicked;
	}
#pragma warning restore IDE0051 // Remove unused private members

	/// <summary>
	/// Renders a theme menu item styled like a mini dialog window with title bar and content area.
	/// </summary>
	/// <param name="theme">The theme to render.</param>
	/// <param name="displayName">The display name for the theme.</param>
	/// <param name="isSelected">Whether this theme is currently selected.</param>
	/// <returns>True if the theme was clicked, false otherwise.</returns>
	private static bool RenderThemeMenuItemWithDialogSwatches(ThemeRegistry.ThemeInfo theme, string displayName, bool isSelected)
	{
		bool clicked = false;

		// Create a unique ID for this menu item
		ImGui.PushID(theme.Name);

		try
		{
			// Get the theme's complete palette for color preview
			IReadOnlyDictionary<SemanticColorRequest, PerceptualColor> completePalette = GetCompletePalette(theme.CreateInstance());

			// Get primary color for title bar and surface color for background
			ImColor primaryColor = Color.Palette.Basic.Blue; // Fallback
			ImColor surfaceColor = Color.Palette.Neutral.Gray; // Fallback

			if (completePalette.TryGetValue(new SemanticColorRequest(SemanticMeaning.Primary, Priority.High), out PerceptualColor primary))
			{
				primaryColor = Color.FromPerceptualColor(primary);
			}

			if (completePalette.TryGetValue(new SemanticColorRequest(SemanticMeaning.Neutral, Priority.Low), out PerceptualColor surface))
			{
				surfaceColor = Color.FromPerceptualColor(surface);
			}
			else if (completePalette.TryGetValue(new SemanticColorRequest(SemanticMeaning.Neutral, Priority.Medium), out PerceptualColor surfaceMed))
			{
				surfaceColor = Color.FromPerceptualColor(surfaceMed);
			}

			// Calculate required width for the dialog window
			// This ensures the menu expands wide enough to show our custom dialog rendering
			Vector2 textSize = ImGui.CalcTextSize(displayName);
			float minDialogWidth = Math.Max(textSize.X + 16.0f, 140.0f); // Text width + padding, minimum 140px
			float dialogHeight = 32.0f; // Height for the dialog window
			Vector2 selectableSize = new(minDialogWidth, dialogHeight);

			// Use invisible selectable for interaction with proper sizing
			if (ImGui.Selectable($"##theme_{theme.Name}", isSelected, ImGuiSelectableFlags.None, selectableSize))
			{
				clicked = true;
			}

			// Get item bounds for custom drawing
			Vector2 itemMin = ImGui.GetItemRectMin();
			Vector2 itemMax = ImGui.GetItemRectMax();
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();

			float titleBarHeight = 8.0f; // Height of the dialog title bar
			float margin = 2.0f;

			// Calculate dialog window bounds using the full selectable area
			Vector2 dialogMin = new(itemMin.X + margin, itemMin.Y + margin);
			Vector2 dialogMax = new(itemMax.X - margin, itemMax.Y - margin);
			Vector2 titleBarMax = new(dialogMax.X, dialogMin.Y + titleBarHeight);

			// Draw dialog window shadow/border (slightly offset and darker)
			Vector2 shadowOffset = new(1.0f, 1.0f);
			drawList.AddRectFilled(
				dialogMin + shadowOffset,
				dialogMax + shadowOffset,
				ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 0.0f, 0.0f, 0.3f)),
				2.0f
			);

			// Draw main surface background
			drawList.AddRectFilled(
				dialogMin,
				dialogMax,
				ImGui.ColorConvertFloat4ToU32(surfaceColor.Value),
				2.0f
			);

			// Draw primary color title bar
			drawList.AddRectFilled(
				dialogMin,
				titleBarMax,
				ImGui.ColorConvertFloat4ToU32(primaryColor.Value),
				2.0f,
				ImDrawFlags.RoundCornersTop
			);

			// Add subtle inner glow for selected themes
			if (isSelected)
			{
				// Inner glow - subtle light glow inside the dialog
				drawList.AddRect(
					dialogMin + Vector2.One,
					dialogMax - Vector2.One,
					ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 0.4f)),
					2.0f,
					ImDrawFlags.None,
					1.0f
				);
			}

			// Add hover effect
			if (ImGui.IsItemHovered())
			{
				drawList.AddRect(
					dialogMin,
					dialogMax,
					ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 0.4f)),
					2.0f,
					ImDrawFlags.None,
					1.0f
				);
			}

			// Calculate contrasting text color for the surface
			Vector4 surfaceVec = surfaceColor.Value;
			float luminance = (0.299f * surfaceVec.X) + (0.587f * surfaceVec.Y) + (0.114f * surfaceVec.Z);
			uint textColor = luminance > 0.5f ?
				ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 0.0f, 0.0f, 1.0f)) : // Dark text on light surface
				ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f));   // Light text on dark surface

			// Draw theme name text over the surface area (below title bar)
			Vector2 textPos = new(
				dialogMin.X + 4.0f,
				titleBarMax.Y + 2.0f
			);

			drawList.AddText(textPos, textColor, displayName);

			// Add tooltip with theme description if hovered
			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip($"{theme.Description}\n\nDialog style: Primary title bar, surface background");
			}
		}
		catch (ArgumentException)
		{
			// Fallback to simple menu item if color extraction fails
			clicked = ImGui.MenuItem(displayName, "", isSelected);
			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip(theme.Description);
			}
		}
		catch (InvalidOperationException)
		{
			// Fallback to simple menu item if color extraction fails
			clicked = ImGui.MenuItem(displayName, "", isSelected);
			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip(theme.Description);
			}
		}
		finally
		{
			ImGui.PopID();
		}

		return clicked;
	}

	/// <summary>
	/// Renders a family menu header styled like a mini dialog window with title bar and content area.
	/// </summary>
	/// <param name="familyName">The theme family name.</param>
	/// <param name="representativeTheme">The theme to use for color extraction (typically first theme in family).</param>
	/// <param name="anyFamilyThemeSelected">Whether any theme in this family is currently selected.</param>
	/// <returns>True if the menu should be opened, false otherwise.</returns>
	private static bool RenderFamilyMenuHeader(string familyName, ThemeRegistry.ThemeInfo? representativeTheme, bool anyFamilyThemeSelected)
	{
		// Create a unique ID for this family header
		ImGui.PushID($"Family_{familyName}");

		bool menuOpened = false;

		try
		{
			// Get colors from the representative theme if available
			ImColor primaryColor = Color.Palette.Basic.Blue; // Fallback
			ImColor surfaceColor = Color.Palette.Neutral.Gray; // Fallback

			if (representativeTheme != null)
			{
				try
				{
					// Use the complete palette for efficient color extraction
					IReadOnlyDictionary<SemanticColorRequest, PerceptualColor> completePalette = GetCompletePalette(representativeTheme.CreateInstance());

					// Get primary color for title bar
					if (completePalette.TryGetValue(new SemanticColorRequest(SemanticMeaning.Primary, Priority.High), out PerceptualColor primary))
					{
						primaryColor = Color.FromPerceptualColor(primary);
					}

					// Get surface color for background
					if (completePalette.TryGetValue(new SemanticColorRequest(SemanticMeaning.Neutral, Priority.Low), out PerceptualColor surface))
					{
						surfaceColor = Color.FromPerceptualColor(surface);
					}
					else if (completePalette.TryGetValue(new SemanticColorRequest(SemanticMeaning.Neutral, Priority.Medium), out PerceptualColor surfaceMed))
					{
						surfaceColor = Color.FromPerceptualColor(surfaceMed);
					}
				}
				catch (ArgumentException)
				{
					// Use fallback colors if extraction fails
				}
				catch (InvalidOperationException)
				{
					// Use fallback colors if extraction fails
				}
			}

			// Use BeginMenu with transparent styling and draw our custom dialog over it
			string displayText = familyName; // Don't add arrow, BeginMenu will handle it

			// Calculate proper width to match other menu items and account for arrow space
			Vector2 textSize = ImGui.CalcTextSize(displayText);
			float arrowWidth = ImGui.CalcTextSize(" ►").X; // Space needed for the arrow
			float desiredWidth = Math.Max(textSize.X + arrowWidth + 16.0f, 180.0f); // Text + arrow + padding, minimum 180px

			// Push style to control menu item size
			ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(ImGui.GetStyle().ItemSpacing.X, 4.0f)); // Ensure proper vertical spacing

			// Now use the standard BeginMenu with transparent styling to handle menu behavior
			ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0, 0, 0, 0)); // Transparent
			ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(1, 1, 1, 0.1f)); // Subtle hover
			ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(1, 1, 1, 0.2f)); // Subtle active
			ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 0, 0, 0)); // Hide text (we drew our own)

			// Use a dummy selectable to reserve the exact space we want, then use BeginMenu
			Vector2 desiredSize = new(desiredWidth, 34.0f); // 34px height to match our dialog design
			ImGui.Selectable($"##dummy_{familyName}", false, ImGuiSelectableFlags.Disabled, desiredSize);
			Vector2 itemMin = ImGui.GetItemRectMin();
			Vector2 itemMax = ImGui.GetItemRectMax();

			// Move cursor back to draw BeginMenu over the same space
			ImGui.SetCursorScreenPos(itemMin);
			menuOpened = ImGui.BeginMenu($"##menu_{familyName}");

			// Draw our custom dialog using the reserved space
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();

			float titleBarHeight = 8.0f; // Height of the dialog title bar
			float margin = 2.0f;
			float arrowSpace = 20.0f; // Reserve space on right for arrow

			// Calculate dialog window bounds using reserved space, leaving room for arrow
			Vector2 dialogMin = new(itemMin.X + margin, itemMin.Y + margin);
			Vector2 dialogMax = new(itemMax.X - margin - arrowSpace, itemMax.Y - margin);
			Vector2 titleBarMax = new(dialogMax.X, dialogMin.Y + titleBarHeight);

			// Draw dialog window shadow/border (slightly offset and darker)
			Vector2 shadowOffset = new(1.0f, 1.0f);
			drawList.AddRectFilled(
				dialogMin + shadowOffset,
				dialogMax + shadowOffset,
				ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 0.0f, 0.0f, 0.3f)),
				2.0f
			);

			// Draw main surface background
			drawList.AddRectFilled(
				dialogMin,
				dialogMax,
				ImGui.ColorConvertFloat4ToU32(surfaceColor.Value),
				2.0f
			);

			// Draw primary color title bar
			drawList.AddRectFilled(
				dialogMin,
				titleBarMax,
				ImGui.ColorConvertFloat4ToU32(primaryColor.Value),
				2.0f,
				ImDrawFlags.RoundCornersTop
			);

			// Add subtle inner glow if any family theme is selected
			if (anyFamilyThemeSelected)
			{
				// Inner glow - subtle light glow inside the dialog
				drawList.AddRect(
					dialogMin + Vector2.One,
					dialogMax - Vector2.One,
					ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 0.3f)),
					2.0f,
					ImDrawFlags.None,
					1.0f
				);
			}

			// Calculate contrasting text color for the surface
			Vector4 surfaceVec = surfaceColor.Value;
			float luminance = (0.299f * surfaceVec.X) + (0.587f * surfaceVec.Y) + (0.114f * surfaceVec.Z);
			uint textColor = luminance > 0.5f ?
				ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 0.0f, 0.0f, 1.0f)) : // Dark text on light surface
				ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f));   // Light text on dark surface

			// Draw family name text over the surface area (below title bar)
			Vector2 textPos = new(
				dialogMin.X + 4.0f,
				titleBarMax.Y + 2.0f
			);

			drawList.AddText(textPos, textColor, displayText);

			ImGui.PopStyleColor(4);
			ImGui.PopStyleVar(); // Pop the ItemSpacing style
		}
		catch
		{
			// If there's an exception and menu wasn't opened, clean up the ID
			if (!menuOpened)
			{
				ImGui.PopID();
			}
			throw;
		}

		// Only pop ID if menu is not opened - if opened, RenderFamilySubmenu will handle it
		if (!menuOpened)
		{
			ImGui.PopID();
		}

		return menuOpened;
	}

	/// <summary>
	/// Renders a group header styled like a mini dialog window with title bar and content area.
	/// </summary>
	/// <param name="groupName">The group name (e.g., "Dark", "Light").</param>
	/// <param name="representativeTheme">The theme to use for color extraction (typically first theme in group).</param>
	/// <param name="anyGroupThemeSelected">Whether any theme in this group is currently selected.</param>
	private static void RenderGroupHeader(string groupName, ThemeRegistry.ThemeInfo representativeTheme, bool anyGroupThemeSelected)
	{
		// Create a unique ID for this group header
		ImGui.PushID($"Group_{groupName}");

		try
		{
			// Get colors from the representative theme
			ImColor primaryColor = Color.Palette.Basic.Blue; // Fallback
			ImColor surfaceColor = Color.Palette.Neutral.Gray; // Fallback

			try
			{
				// Use the complete palette for efficient color extraction
				IReadOnlyDictionary<SemanticColorRequest, PerceptualColor> completePalette = GetCompletePalette(representativeTheme.CreateInstance());

				// Get primary color for title bar
				if (completePalette.TryGetValue(new SemanticColorRequest(SemanticMeaning.Primary, Priority.High), out PerceptualColor primary))
				{
					primaryColor = Color.FromPerceptualColor(primary);
				}

				// Get surface color for background
				if (completePalette.TryGetValue(new SemanticColorRequest(SemanticMeaning.Neutral, Priority.Low), out PerceptualColor surface))
				{
					surfaceColor = Color.FromPerceptualColor(surface);
				}
				else if (completePalette.TryGetValue(new SemanticColorRequest(SemanticMeaning.Neutral, Priority.Medium), out PerceptualColor surfaceMed))
				{
					surfaceColor = Color.FromPerceptualColor(surfaceMed);
				}
			}
			catch (ArgumentException)
			{
				// Use fallback colors if extraction fails
			}
			catch (InvalidOperationException)
			{
				// Use fallback colors if extraction fails
			}

			// Calculate required width for the dialog window
			Vector2 textSize = ImGui.CalcTextSize(groupName);
			float minDialogWidth = Math.Max(textSize.X + 16.0f, 120.0f); // Text width + padding, minimum 120px for groups
			float dialogHeight = 24.0f; // Smaller height for group headers
			Vector2 selectableSize = new(minDialogWidth, dialogHeight);

			// Use invisible selectable for proper sizing (non-interactive)
			ImGui.Selectable($"##group_{groupName}", false, ImGuiSelectableFlags.Disabled, selectableSize);

			// Get item bounds for custom drawing
			Vector2 itemMin = ImGui.GetItemRectMin();
			Vector2 itemMax = ImGui.GetItemRectMax();
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();

			float titleBarHeight = 6.0f; // Smaller title bar for group headers
			float margin = 1.5f;

			// Calculate dialog window bounds using the full selectable area
			Vector2 dialogMin = new(itemMin.X + margin, itemMin.Y + margin);
			Vector2 dialogMax = new(itemMax.X - margin, itemMax.Y - margin);
			Vector2 titleBarMax = new(dialogMax.X, dialogMin.Y + titleBarHeight);

			// Draw dialog window shadow/border (slightly offset and darker)
			Vector2 shadowOffset = new(0.5f, 0.5f);
			drawList.AddRectFilled(
				dialogMin + shadowOffset,
				dialogMax + shadowOffset,
				ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 0.0f, 0.0f, 0.2f)),
				1.5f
			);

			// Draw main surface background
			drawList.AddRectFilled(
				dialogMin,
				dialogMax,
				ImGui.ColorConvertFloat4ToU32(surfaceColor.Value),
				1.5f
			);

			// Draw primary color title bar
			drawList.AddRectFilled(
				dialogMin,
				titleBarMax,
				ImGui.ColorConvertFloat4ToU32(primaryColor.Value),
				1.5f,
				ImDrawFlags.RoundCornersTop
			);

			// Add subtle inner glow if any group theme is selected
			if (anyGroupThemeSelected)
			{
				// Inner glow - subtle light glow inside the dialog
				drawList.AddRect(
					dialogMin + new Vector2(0.5f, 0.5f),
					dialogMax - new Vector2(0.5f, 0.5f),
					ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 0.25f)),
					1.5f,
					ImDrawFlags.None,
					0.8f
				);
			}

			// Calculate contrasting text color for the surface
			Vector4 surfaceVec = surfaceColor.Value;
			float luminance = (0.299f * surfaceVec.X) + (0.587f * surfaceVec.Y) + (0.114f * surfaceVec.Z);
			uint textColor = luminance > 0.5f ?
				ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 0.0f, 0.0f, 1.0f)) : // Dark text on light surface
				ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f));   // Light text on dark surface

			// Draw group name text centered over the surface area (below title bar)
			Vector2 textPos = new(
				dialogMin.X + ((dialogMax.X - dialogMin.X - textSize.X) * 0.5f), // Centered horizontally
				titleBarMax.Y + 1.0f // Small padding below title bar
			);

			drawList.AddText(textPos, textColor, groupName);
		}
		finally
		{
			ImGui.PopID();
		}
	}

	#endregion

	#region Theme Discovery

	/// <summary>
	/// Gets all available themes with their metadata.
	/// </summary>
	public static IReadOnlyList<ThemeRegistry.ThemeInfo> AllThemes => ThemeRegistry.AllThemes;

	/// <summary>
	/// Gets all dark themes.
	/// </summary>
	public static IReadOnlyList<ThemeRegistry.ThemeInfo> DarkThemes => ThemeRegistry.DarkThemes;

	/// <summary>
	/// Gets all light themes.
	/// </summary>
	public static IReadOnlyList<ThemeRegistry.ThemeInfo> LightThemes => ThemeRegistry.LightThemes;

	/// <summary>
	/// Gets themes grouped by family.
	/// </summary>
	public static IReadOnlyDictionary<string, IReadOnlyList<ThemeRegistry.ThemeInfo>> ThemesByFamily => ThemeRegistry.ThemesByFamily;

	/// <summary>
	/// Gets all theme families.
	/// </summary>
	public static IReadOnlyList<string> Families => ThemeRegistry.Families;

	/// <summary>
	/// Finds a theme by name (case-insensitive).
	/// </summary>
	/// <param name="name">The theme name to search for.</param>
	/// <returns>The theme info if found, null otherwise.</returns>
	public static ThemeRegistry.ThemeInfo? FindTheme(string name) => ThemeRegistry.FindTheme(name);

	/// <summary>
	/// Gets all themes in a specific family.
	/// </summary>
	/// <param name="family">The family name.</param>
	/// <returns>Array of themes in the family.</returns>
	public static IReadOnlyList<ThemeRegistry.ThemeInfo> GetThemesInFamily(string family) => ThemeRegistry.GetThemesInFamily(family);

	/// <summary>
	/// Creates instances of all themes.
	/// </summary>
	/// <returns>Array of all theme instances.</returns>
	public static IReadOnlyList<ISemanticTheme> CreateAllThemeInstances() => ThemeRegistry.CreateAllThemeInstances();

	/// <summary>
	/// Creates theme instances for a specific family.
	/// </summary>
	/// <param name="family">The family name.</param>
	/// <returns>Array of theme instances in the family.</returns>
	public static IReadOnlyList<ISemanticTheme> CreateThemeInstancesInFamily(string family) => ThemeRegistry.CreateThemeInstancesInFamily(family);

	#endregion

	#region Complete Palette Access

	/// <summary>
	/// Gets the complete palette for the current theme containing all possible semantic color combinations.
	/// This provides every color that can be requested from the theme, useful for theme exploration and previews.
	/// </summary>
	/// <returns>A dictionary mapping every possible semantic color request to its assigned color, or null if no theme is active.</returns>
	public static IReadOnlyDictionary<SemanticColorRequest, PerceptualColor>? GetCurrentThemeCompletePalette()
	{
		ThemeRegistry.ThemeInfo? currentTheme = CurrentTheme;
		if (currentTheme is null)
		{
			return null;
		}

		return GetCompletePalette(currentTheme.CreateInstance());
	}

	/// <summary>
	/// Gets the complete palette for a specific theme containing all possible semantic color combinations.
	/// This provides every color that can be requested from the theme, useful for theme exploration and previews.
	/// Uses the MakeCompletePalette API with efficient caching to avoid expensive recalculation.
	/// </summary>
	/// <param name="theme">The semantic theme to generate the complete palette from.</param>
	/// <returns>A dictionary mapping every possible semantic color request to its assigned color.</returns>
	public static IReadOnlyDictionary<SemanticColorRequest, PerceptualColor> GetCompletePalette(ISemanticTheme theme)
	{
		Ensure.NotNull(theme);

		// Generate a cache key based on the theme
		string cacheKey = GenerateThemeCacheKey(theme);

		// Check cache first
		using (paletteCacheLock.EnterScope())
		{
			if (paletteCache.TryGetValue(cacheKey, out IReadOnlyDictionary<SemanticColorRequest, PerceptualColor>? cachedPalette))
			{
				return cachedPalette;
			}
		}

		// Generate the palette
		IReadOnlyDictionary<SemanticColorRequest, PerceptualColor> palette = GeneratePaletteUncached(theme);

		// Cache the result
		using (paletteCacheLock.EnterScope())
		{
			// Limit cache size to prevent memory issues
			if (paletteCache.Count >= 50) // Reasonable limit for theme count
			{
				// Remove oldest entries (simple FIFO)
				string firstKey = paletteCache.Keys.First();
				paletteCache.Remove(firstKey);
			}

			paletteCache[cacheKey] = palette;
		}

		return palette;
	}

	/// <summary>
	/// Generates the complete palette without caching using the MakeCompletePalette API.
	/// </summary>
	/// <param name="theme">The theme to generate the palette from.</param>
	/// <returns>The complete palette dictionary.</returns>
	private static IReadOnlyDictionary<SemanticColorRequest, PerceptualColor> GeneratePaletteUncached(ISemanticTheme theme) =>
		SemanticColorMapper.MakeCompletePalette(theme);

	/// <summary>
	/// Generates a cache key for a theme based on its semantic mapping.
	/// </summary>
	/// <param name="theme">The theme to generate a cache key for.</param>
	/// <returns>A unique cache key string.</returns>
	private static string GenerateThemeCacheKey(ISemanticTheme theme)
	{
		// Create a hash based on the theme's semantic mapping content
		// This ensures we get a new cache entry if the theme definition changes
		System.Text.StringBuilder keyBuilder = new();

		keyBuilder.Append(theme.GetType().FullName);
		keyBuilder.Append('_');

		// Add a simple hash of the semantic mappings
		foreach (KeyValuePair<SemanticMeaning, Collection<PerceptualColor>> mapping in theme.SemanticMapping.OrderBy(kvp => kvp.Key))
		{
			keyBuilder.Append(mapping.Key);
			keyBuilder.Append(':');
			keyBuilder.Append(mapping.Value.Count);
			keyBuilder.Append(';');
		}

		return keyBuilder.ToString();
	}

	/// <summary>
	/// Clears the palette cache. Called when themes change.
	/// </summary>
	private static void ClearPaletteCache()
	{
		using (paletteCacheLock.EnterScope())
		{
			paletteCache.Clear();
		}
	}

	/// <summary>
	/// Gets the complete palette for a theme by name.
	/// </summary>
	/// <param name="themeName">The name of the theme to get the palette for.</param>
	/// <returns>A dictionary mapping every possible semantic color request to its assigned color, or null if theme not found.</returns>
	public static IReadOnlyDictionary<SemanticColorRequest, PerceptualColor>? GetCompletePalette(string themeName)
	{
		ThemeRegistry.ThemeInfo? themeInfo = FindTheme(themeName);
		if (themeInfo is null)
		{
			return null;
		}

		return GetCompletePalette(themeInfo.CreateInstance());
	}

	/// <summary>
	/// Gets all available semantic color requests for the current theme.
	/// This is useful for discovering what colors are available without needing the actual color values.
	/// </summary>
	/// <returns>An array of all available semantic color requests, or empty array if no theme is active.</returns>
	public static ImmutableArray<SemanticColorRequest> GetCurrentThemeAvailableColorRequests()
	{
		IReadOnlyDictionary<SemanticColorRequest, PerceptualColor>? palette = GetCurrentThemeCompletePalette();
		return palette?.Keys.ToImmutableArray() ?? [];
	}

	/// <summary>
	/// Tries to get a specific color from the current theme's complete palette.
	/// This is more efficient than manually navigating semantic mappings.
	/// </summary>
	/// <param name="request">The semantic color request specifying the color to retrieve.</param>
	/// <param name="color">The retrieved color if found.</param>
	/// <returns>True if the color was found, false otherwise.</returns>
	public static bool TryGetColor(SemanticColorRequest request, out PerceptualColor color)
	{
		IReadOnlyDictionary<SemanticColorRequest, PerceptualColor>? palette = GetCurrentThemeCompletePalette();
		if (palette is not null && palette.TryGetValue(request, out color))
		{
			return true;
		}

		color = default;
		return false;
	}

	/// <summary>
	/// Gets a specific color from the current theme's complete palette.
	/// This is more efficient than manually navigating semantic mappings.
	/// </summary>
	/// <param name="request">The semantic color request specifying the color to retrieve.</param>
	/// <returns>The color if found, null otherwise.</returns>
	public static PerceptualColor? GetColor(SemanticColorRequest request) =>
		TryGetColor(request, out PerceptualColor color) ? color : null;

	#endregion

	#region Scoped Theme Colors

	/// <summary>
	/// Creates a scoped theme color that automatically reverts when disposed.
	/// </summary>
	/// <param name="color">The color to apply to the theme.</param>
	/// <returns>A scoped theme color instance.</returns>
	public static ScopedThemeColor FromColor(ImColor color) => new(color, enabled: true);

	/// <summary>
	/// Creates a scoped disabled theme color that automatically reverts when disposed.
	/// </summary>
	/// <param name="color">The color to apply to the theme.</param>
	/// <returns>A scoped theme color instance with disabled state.</returns>
	public static ScopedThemeColor DisabledFromColor(ImColor color) => new(color, enabled: false);

	#endregion

	#region Theme Selector Dialog

	/// <summary>
	/// Shows the theme browser modal. Call this to programmatically open the theme selector.
	/// </summary>
	/// <param name="title">The title for the theme browser modal.</param>
	/// <param name="customSize">Custom size for the modal window. Default is 900x650 to accommodate wider theme cards.</param>
	public static void ShowThemeSelector(string title = "🎨 Theme Browser", Vector2? customSize = null)
	{
		themeBrowser.Open(
			title,
			onThemeSelected: themeName => { /* Theme was already applied in ThemeBrowser */ },
			onDefaultRequested: () => { /* Default was already applied in ThemeBrowser */ },
			customSize
		);
	}

	/// <summary>
	/// Hides the theme selector dialog. This is kept for API compatibility but the modal handles its own state.
	/// </summary>
	public static void HideThemeSelector() { /* Modal handles its own close state, so this is mainly for API compatibility */ }

	/// <summary>
	/// Renders the theme browser modal if it's currently open.
	/// This should be called in your main render loop to display the theme browser.
	/// </summary>
	/// <returns>True if a theme was changed during modal interaction, false otherwise.</returns>
	public static bool RenderThemeSelector() => themeBrowser.ShowIfOpen();

	/// <summary>
	/// Renders the theme browser modal if it's currently open.
	/// This overload is kept for backward compatibility.
	/// </summary>
	/// <param name="windowTitle">The title for the theme browser modal (ignored - kept for compatibility).</param>
	/// <param name="windowSize">The size for the theme browser modal (ignored - kept for compatibility).</param>
	/// <returns>True if a theme was changed during modal interaction, false otherwise.</returns>
	public static bool RenderThemeSelector(string windowTitle, Vector2? windowSize = null)
	{
		// Parameters are intentionally unused - kept for backward compatibility
		_ = windowTitle;
		_ = windowSize;
		return themeBrowser.ShowIfOpen();
	}

	/// <summary>
	/// Renders a theme menu that opens the theme browser modal instead of using dropdown menus.
	/// This provides a better user experience than the traditional RenderMenu() method.
	/// </summary>
	/// <param name="menuLabel">The label for the theme submenu (default: "Theme")</param>
	/// <returns>True if a theme was selected and changed, false otherwise.</returns>
	public static bool RenderThemeSelectorMenu(string menuLabel = "Theme")
	{
		bool themeChanged = false;

		if (ImGui.BeginMenu(menuLabel))
		{
			if (ImGui.MenuItem("Browse Themes..."))
			{
				ShowThemeSelector();
			}

			ImGui.Separator();

			// Quick reset option
			if (ImGui.MenuItem("Reset to Default", "", currentThemeName is null))
			{
				if (currentThemeName is not null)
				{
					ResetToDefault();
					themeChanged = true;
				}
			}

			if (ImGui.IsItemHovered())
			{
				ImGui.SetTooltip("Reset to default ImGui styling with no theme applied");
			}

			// Show current theme info if any
			if (currentThemeName is not null)
			{
				ImGui.Separator();
				ImGui.TextDisabled($"Current: {currentThemeName}");
				ThemeRegistry.ThemeInfo? currentTheme = FindTheme(currentThemeName);
				if (currentTheme is not null)
				{
					ImGui.TextDisabled($"({(currentTheme.IsDark ? "Dark" : "Light")})");
				}
			}

			ImGui.EndMenu();
		}

		// Check if any theme changes occurred through the modal browser
		// The modal handles theme application internally, so we check for changes here
		bool modalThemeChanged = RenderThemeSelector();

		return themeChanged || modalThemeChanged;
	}

	#endregion
}
