# ktsu.ImGui.Styler

[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGui.Styler?logo=nuget)](https://nuget.org/packages/ktsu.ImGui.Styler)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](https://github.com/ktsu-dev/ImGuiApp/blob/main/LICENSE.md)

**A powerful, expressive styling library for Dear ImGui interfaces** that simplifies theme management, provides scoped styling utilities, and offers advanced color manipulation with accessibility features.

## Features

### Advanced Theme System
- **50+ Built-in Themes**: Comprehensive collection including Catppuccin, Dracula, Gruvbox, Tokyo Night, Nord, and many more
- **Interactive Theme Browser**: Visual theme selection with live preview and categorization
- **Semantic Theme Support**: Leverages `ktsu.ThemeProvider` for consistent, semantic color theming
- **Scoped Theme Application**: Apply themes to specific UI sections without affecting the global style

### Precise Alignment Tools
- **Automatic Content Centering**: Center any content within containers or available regions
- **Flexible Container Alignment**: Align content within custom-sized containers
- **Layout Integration**: Seamlessly works with ImGui's existing layout system

### Advanced Color Management
- **Hex Color Support**: Direct conversion from hex strings to ImGui colors
- **Accessibility-First**: Automatic contrast calculation and optimal text color selection
- **Color Manipulation**: Lighten, darken, and adjust colors programmatically
- **Scoped Color Application**: Apply colors to specific UI elements without side effects

### Scoped Styling System
- **Style Variables**: Apply temporary style modifications with automatic cleanup
- **Text Colors**: Scoped text color changes with proper restoration
- **Theme Colors**: Apply theme-based colors to specific UI sections
- **Memory Safe**: Automatic resource management and style restoration

## Installation

Add ImGuiStyler to your project via NuGet:

```xml
<PackageReference Include="ktsu.ImGui.Styler" Version="x.y.z" />
```

Or via Package Manager Console:
```powershell
Install-Package ktsu.ImGui.Styler
```

## Quick Start

```csharp
using ktsu.ImGui.Styler;
using Hexa.NET.ImGui;

// Apply a global theme
Theme.Apply("Tokyo Night");

// Use scoped styling for specific elements
using (new ScopedColor(ImGuiCol.Text, Color.FromHex("#ff6b6b")))
{
    ImGui.Text("This text is red!");
}

// Center content automatically
using (new Alignment.Center(ImGui.CalcTextSize("Centered!")))
{
    ImGui.Text("Centered!");
}
```

## Comprehensive Usage Guide

### Theme Management

#### Applying Global Themes
```csharp
// Apply any of the 50+ built-in themes
Theme.Apply("Catppuccin Mocha");
Theme.Apply("Gruvbox Dark");
Theme.Apply("Tokyo Night");

// Get the name of the currently applied theme
string? currentTheme = Theme.CurrentThemeName;

// Reset to default ImGui theme
Theme.ResetToDefault();
```

#### Interactive Theme Browser
```csharp
// Show the theme browser modal
if (ImGui.Button("Choose Theme"))
{
    Theme.ShowThemeSelector("Select a Theme");
}

// Render the theme selector (call this in your main render loop)
if (Theme.RenderThemeSelector())
{
    Console.WriteLine($"Theme changed to: {Theme.CurrentThemeName}");
}
```

#### Scoped Theme Application
```csharp
// ScopedTheme takes an ISemanticTheme instance; resolve one by name from the registry
ISemanticTheme dracula = Theme.FindTheme("Dracula")!.CreateInstance();
ISemanticTheme nord = Theme.FindTheme("Nord")!.CreateInstance();

using (new ScopedTheme(dracula))
{
    ImGui.Text("This text uses Dracula theme");
    ImGui.Button("Themed button");

    using (new ScopedTheme(nord))
    {
        ImGui.Text("Nested Nord theme");
    }
    // Automatically reverts to Dracula
}
// Automatically reverts to previous theme
```

### Color Management

#### Creating Colors

Colors are constructed as the semantic `Color` (from `ktsu.Semantics.Color`) and converted at the ImGui seam by `ktsu.ImGui.Color`. Most styling APIs here accept the semantic `Color` directly, so the conversion is usually only needed when you want an `ImColor` in hand.

```csharp
using ktsu.ImGui.Color;
using ktsu.Semantics.Color;

// From hex strings
Color red = Color.FromHex("#ff0000");
Color blueWithAlpha = Color.FromHex("#0066ffcc");

// From 8-bit sRGB components
Color green = Color.FromBytes(0, 255, 0);
Color custom = Color.FromBytes(255, 128, 64, 200);

// From HSL (hue in degrees 0..360, saturation and lightness 0..1)
Color purple = Color.FromHsl(new Hsl(280, 1.0, 0.5));

// Convert when an ImColor is what you need
ImColor imRed = red.ToImColor();
```

#### Color Manipulation

Color manipulation is provided as extension methods on `ImColor`:

```csharp
ImColor baseColor = Color.FromHex("#3498db").ToImColor();

// Adjust brightness
ImColor lighter = baseColor.LightenBy(0.3f);
ImColor darker = baseColor.DarkenBy(0.2f);

// Accessibility-focused text color (best contrast over baseColor)
ImColor readableText = baseColor.MostReadableTextColor();

// WCAG contrast ratio between two colors
float ratio = readableText.GetContrastRatioOver(baseColor);
```

#### Scoped Color Application
```csharp
// Scoped text color
using (new ScopedTextColor(Color.FromHex("#e74c3c")))
{
    ImGui.Text("Red text");
}

// Scoped UI element color
using (new ScopedColor(ImGuiCol.Button, Color.FromHex("#2ecc71")))
{
    ImGui.Button("Green button");
}

// Multiple scoped colors
using (new ScopedColor(ImGuiCol.Button, Color.FromHex("#9b59b6")))
using (new ScopedColor(ImGuiCol.ButtonHovered, Color.FromHex("#8e44ad")))
using (new ScopedColor(ImGuiCol.ButtonActive, Color.FromHex("#71368a")))
{
    ImGui.Button("Fully styled button");
}
```

### Alignment and Layout

#### Content Centering
```csharp
// Center text
string text = "Perfectly centered!";
using (new Alignment.Center(ImGui.CalcTextSize(text)))
{
    ImGui.Text(text);
}

// Center buttons
using (new Alignment.Center(new Vector2(120, 30)))
{
    ImGui.Button("Centered Button", new Vector2(120, 30));
}
```

#### Custom Container Alignment
```csharp
Vector2 containerSize = new(400, 200);
Vector2 contentSize = new(100, 50);

// Center content within a specific container
using (new Alignment.CenterWithin(contentSize, containerSize))
{
    ImGui.Button("Centered in Container", contentSize);
}
```

### Advanced Styling

#### Button Alignment

Align button text within buttons:

```csharp
// Left-aligned button text
using (Button.Alignment.Left())
{
    ImGui.Button("Left Aligned", new Vector2(200, 30));
}

// Center-aligned button text (default in most themes)
using (Button.Alignment.Center())
{
    ImGui.Button("Center Aligned", new Vector2(200, 30));
}
```

#### Text Colors

Apply semantic text colors for consistent messaging:

```csharp
// Normal text
using (Text.Color.Normal())
{
    ImGui.Text("This is normal text");
}

// Error messages
using (Text.Color.Error())
{
    ImGui.Text("Error: Something went wrong!");
}

// Warning messages
using (Text.Color.Warning())
{
    ImGui.Text("Warning: Please be careful");
}

// Info messages
using (Text.Color.Info())
{
    ImGui.Text("Info: Here's some information");
}

// Success messages
using (Text.Color.Success())
{
    ImGui.Text("Success: Operation completed!");
}

// Customize the color definitions globally
Text.Color.Definitions.Error = Color.FromHex("#e74c3c");
Text.Color.Definitions.Success = Color.FromHex("#2ecc71");
```

#### Indentation

Create indented content blocks:

```csharp
// Default indent
ImGui.Text("Normal text");
using (Indent.ByDefault())
{
    ImGui.Text("Indented text");
    using (Indent.ByDefault())
    {
        ImGui.Text("Double indented");
    }
}

// Custom indent width
ImGui.Text("Normal text");
using (Indent.By(40.0f))
{
    ImGui.Text("Indented by 40 pixels");
}
```

#### Scoped Style Variables
```csharp
// Rounded buttons
using (new ScopedStyleVar(ImGuiStyleVar.FrameRounding, 8.0f))
{
    ImGui.Button("Rounded Button");
}

// Multiple style modifications
using (new ScopedStyleVar(ImGuiStyleVar.FrameRounding, 12.0f))
using (new ScopedStyleVar(ImGuiStyleVar.FramePadding, new Vector2(20, 10)))
using (new ScopedStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(10, 8)))
{
    ImGui.Button("Highly Styled Button");
    ImGui.Button("Another Styled Button");
}
```

#### Theme-Based Styling
```csharp
// Use semantic colors from current theme
using (new ScopedThemeColor(Color.Primary))
{
    ImGui.Text("Primary theme color");
}

using (new ScopedThemeColor(Color.Secondary))
{
    ImGui.Button("Secondary theme button");
}
```

## Available Themes

ImGuiStyler includes **50+ carefully crafted themes** across multiple families:

### Dark Themes
- **Catppuccin**: Mocha, Macchiato, Frappe
- **Tokyo Night**: Classic, Storm
- **Gruvbox**: Dark, Dark Hard, Dark Soft
- **Dracula**: Classic vampire theme
- **Nord**: Arctic, frost-inspired theme
- **Nightfox**: Carbonfox, Nightfox, Terafox
- **OneDark**: Popular dark theme
- **Kanagawa**: Wave, Dragon variants
- **Everforest**: Dark, Dark Hard, Dark Soft

### Light Themes
- **Catppuccin**: Latte
- **Tokyo Night**: Day
- **Gruvbox**: Light, Light Hard, Light Soft
- **Nord**: Light variant
- **Nightfox**: Dawnfox, Dayfox
- **PaperColor**: Light
- **Everforest**: Light, Light Hard, Light Soft
- **VSCode**: Light theme

### Specialty Themes
- **Monokai**: Classic editor theme
- **Nightfly**: Smooth dark theme
- **VSCode**: Dark theme recreation

## API Reference

### Theme Class
- `Theme.Apply(string themeName)` - Apply a global theme (returns `false` if not found)
- `Theme.Apply(ISemanticTheme theme)` - Apply a semantic theme
- `Theme.ResetToDefault()` - Reset to default ImGui theme
- `Theme.ShowThemeSelector(string title)` - Show theme browser modal
- `Theme.RenderThemeSelector()` - Render theme browser (returns true if theme changed)
- `Theme.RenderMenu(string menuLabel)` - Render a theme selection menu
- `Theme.FindTheme(string name)` - Look up a theme by name (returns `ThemeInfo?`)
- `Theme.AllThemes` / `Theme.DarkThemes` / `Theme.LightThemes` - Available themes
- `Theme.Families` - Get all theme families
- `Theme.CurrentThemeName` - Get current theme name

### Colors

Color construction and manipulation live in `ktsu.Semantics.Color` and the `ktsu.ImGui.Color` adapter; Styler consumes them rather than defining its own color type.

- `Color.FromHex(string hex)` - Create a color from a hex string
- `Color.FromBytes(byte r, byte g, byte b, byte a = 255)` - Create a color from 8-bit sRGB components
- `Color.FromHsl(Hsl hsl)` - Create a color from hue (degrees), saturation and lightness
- `color.ToImColor()` / `color.ToImGuiVector4()` / `color.ToImGuiU32()` - Convert at the ImGui seam

### Color Extension Methods (on `ImColor`)
- `color.LightenBy(float amount)` - Lighten color
- `color.DarkenBy(float amount)` - Darken color
- `color.WithAlpha(float amount)` - Set alpha channel
- `color.MostReadableTextColor()` - Get accessible (max-contrast) text color
- `color.AdjustForSufficientContrast(ImColor textColor, float? targetRatio = null)` - Nudge a color until it meets a contrast target
- `color.GetContrastRatioOver(ImColor background)` - WCAG contrast ratio

### Alignment Classes
- `new Alignment.Center(Vector2 contentSize)` - Center in available region
- `new Alignment.CenterWithin(Vector2 contentSize, Vector2 containerSize)` - Center in container

### Scoped Classes
- `new ScopedColor(ImGuiCol col, ImColor color)` - Scoped color application (also accepts a semantic `Color` or `Srgb`)
- `new ScopedTextColor(ImColor color)` - Scoped text color (also accepts a semantic `Color` or `Srgb`)
- `new ScopedStyleVar(ImGuiStyleVar var, float value)` - Scoped style variable
- `new ScopedTheme(ISemanticTheme theme)` - Scoped theme application
- `new ScopedThemeColor(Color semanticColor)` - Scoped semantic color

### Button Class
- `Button.Alignment.Left()` - Left-align button text
- `Button.Alignment.Center()` - Center-align button text

### Text Class
- `Text.Color.Normal()` - Apply normal text color
- `Text.Color.Error()` - Apply error text color (red)
- `Text.Color.Warning()` - Apply warning text color (yellow)
- `Text.Color.Info()` - Apply info text color (cyan)
- `Text.Color.Success()` - Apply success text color (green)
- `Text.Color.Definitions` - Customize default colors

### Indent Class
- `Indent.ByDefault()` - Create default indent
- `Indent.By(float width)` - Create indent with custom width

## Demo Application

The included demo application showcases all features:

```bash
dotnet run --project examples/ImGuiStylerDemo
```

Features demonstrated:
- Interactive theme browser with live preview
- All 50+ themes with family categorization
- Scoped styling examples
- Color manipulation demos
- Alignment showcases
- Accessibility features

## Related Projects

- **[ktsu.ThemeProvider](https://github.com/ktsu-dev/ThemeProvider)** - Semantic theming foundation
- **[ktsu.ImGui.Popups](https://nuget.org/packages/ktsu.ImGui.Popups)** - Modal and popup utilities (part of this suite)
- **[ktsu.ImGui.Widgets](https://nuget.org/packages/ktsu.ImGui.Widgets)** - Custom widgets (part of this suite)
## Acknowledgments

- **[Dear ImGui](https://github.com/ocornut/imgui)** - The immediate mode GUI library these themes style
- **[Hexa.NET.ImGui](https://github.com/HexaEngine/Hexa.NET.ImGui)** - The .NET bindings for Dear ImGui that this package is built on
- **[ktsu.ThemeProvider](https://github.com/ktsu-dev/ThemeProvider)** - The semantic theming foundation, with `ktsu.ThemeProvider.ImGui` mapping it onto ImGui's style
- **[ktsu.Semantics](https://github.com/ktsu-dev/Semantics)** - The `Color` type and the color math behind the palette
- **[ktsu.ScopedAction](https://github.com/ktsu-dev/ScopedAction)** - The RAII scope type the scoped styling helpers are built on
- **Theme Inspirations**: Catppuccin, Tokyo Night, Gruvbox, and other amazing color schemes
- **Community Contributors** - Thank you for your themes, bug reports, and improvements!

## Contributing

Contributions are welcome! For feature requests, bug reports, or questions, please open an issue on the GitHub repository. If you would like to contribute code, please open a pull request with your changes.

## License

ImGui.Styler is licensed under the MIT License. See [LICENSE.md](https://github.com/ktsu-dev/ImGuiApp/blob/main/LICENSE.md) for more information.

