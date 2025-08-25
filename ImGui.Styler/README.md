# ImGuiStyler 🎨

[![Version](https://img.shields.io/badge/version-1.3.10-blue.svg)](VERSION.md)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE.md)

**A powerful, expressive styling library for ImGui.NET interfaces** that simplifies theme management, provides scoped styling utilities, and offers advanced color manipulation with accessibility features.

## ✨ Features

### 🎨 **Advanced Theme System**
- **50+ Built-in Themes**: Comprehensive collection including Catppuccin, Dracula, Gruvbox, Tokyo Night, Nord, and many more
- **Interactive Theme Browser**: Visual theme selection with live preview and categorization
- **Semantic Theme Support**: Leverages `ktsu.ThemeProvider` for consistent, semantic color theming
- **Scoped Theme Application**: Apply themes to specific UI sections without affecting the global style

### 🎯 **Precise Alignment Tools**
- **Automatic Content Centering**: Center any content within containers or available regions
- **Flexible Container Alignment**: Align content within custom-sized containers
- **Layout Integration**: Seamlessly works with ImGui's existing layout system

### 🌈 **Advanced Color Management**
- **Hex Color Support**: Direct conversion from hex strings to ImGui colors
- **Accessibility-First**: Automatic contrast calculation and optimal text color selection
- **Color Manipulation**: Lighten, darken, and adjust colors programmatically
- **Scoped Color Application**: Apply colors to specific UI elements without side effects

### 🔧 **Scoped Styling System**
- **Style Variables**: Apply temporary style modifications with automatic cleanup
- **Text Colors**: Scoped text color changes with proper restoration
- **Theme Colors**: Apply theme-based colors to specific UI sections
- **Memory Safe**: Automatic resource management and style restoration

## 📦 Installation

Add ImGuiStyler to your project via NuGet:

```xml
<PackageReference Include="ktsu.ImGuiStyler" Version="1.3.10" />
```

Or via Package Manager Console:
```powershell
Install-Package ktsu.ImGuiStyler
```

## 🚀 Quick Start

```csharp
using ktsu.ImGuiStyler;
using Hexa.NET.ImGui;

// Apply a global theme
Theme.Apply("TokyoNight");

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

## 📚 Comprehensive Usage Guide

### 🎨 Theme Management

#### Applying Global Themes
```csharp
// Apply any of the 50+ built-in themes
Theme.Apply("Catppuccin.Mocha");
Theme.Apply("Gruvbox.Dark");
Theme.Apply("Tokyo Night");

// Get current theme information
string? currentTheme = Theme.CurrentThemeName;
bool isCurrentThemeDark = Theme.IsCurrentThemeDark;

// Reset to default ImGui theme
Theme.Reset();
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
using (new ScopedTheme("Dracula"))
{
    ImGui.Text("This text uses Dracula theme");
    ImGui.Button("Themed button");
    
    using (new ScopedTheme("Nord"))
    {
        ImGui.Text("Nested Nord theme");
    }
    // Automatically reverts to Dracula
}
// Automatically reverts to previous theme
```

### 🌈 Color Management

#### Creating Colors
```csharp
// From hex strings
ImColor red = Color.FromHex("#ff0000");
ImColor blueWithAlpha = Color.FromHex("#0066ffcc");

// From RGB values
ImColor green = Color.FromRGB(0, 255, 0);
ImColor customColor = Color.FromRGBA(255, 128, 64, 200);

// From HSV
ImColor rainbow = Color.FromHSV(0.83f, 1.0f, 1.0f); // Purple
```

#### Color Manipulation
```csharp
ImColor baseColor = Color.FromHex("#3498db");

// Adjust brightness
ImColor lighter = Color.Lighten(baseColor, 0.3f);
ImColor darker = Color.Darken(baseColor, 0.2f);

// Accessibility-focused text colors
ImColor optimalText = Color.GetOptimalTextColor(baseColor);
ImColor contrastText = Color.GetContrastingTextColor(baseColor);
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

### 🎯 Alignment and Layout

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

### 🔧 Advanced Styling

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

## 🎨 Available Themes

ImGuiStyler includes **50+ carefully crafted themes** across multiple families:

### 🌙 Dark Themes
- **Catppuccin**: Mocha, Macchiato, Frappe
- **Tokyo Night**: Classic, Storm
- **Gruvbox**: Dark, Dark Hard, Dark Soft
- **Dracula**: Classic vampire theme
- **Nord**: Arctic, frost-inspired theme
- **Nightfox**: Carbonfox, Nightfox, Terafox
- **OneDark**: Popular dark theme
- **Kanagawa**: Wave, Dragon variants
- **Everforest**: Dark, Dark Hard, Dark Soft

### ☀️ Light Themes
- **Catppuccin**: Latte
- **Tokyo Night**: Day
- **Gruvbox**: Light, Light Hard, Light Soft
- **Nord**: Light variant
- **Nightfox**: Dawnfox, Dayfox
- **PaperColor**: Light
- **Everforest**: Light, Light Hard, Light Soft
- **VSCode**: Light theme

### 🎨 Specialty Themes
- **Monokai**: Classic editor theme
- **Nightfly**: Smooth dark theme
- **VSCode**: Dark theme recreation

## 🛠️ API Reference

### Theme Class
- `Theme.Apply(string themeName)` - Apply a global theme
- `Theme.Apply(ISemanticTheme theme)` - Apply a semantic theme
- `Theme.Reset()` - Reset to default ImGui theme
- `Theme.ShowThemeSelector(string title)` - Show theme browser modal
- `Theme.RenderThemeSelector()` - Render theme browser (returns true if theme changed)
- `Theme.AllThemes` - Get all available themes
- `Theme.Families` - Get all theme families
- `Theme.CurrentThemeName` - Get current theme name
- `Theme.IsCurrentThemeDark` - Check if current theme is dark

### Color Class
- `Color.FromHex(string hex)` - Create color from hex string
- `Color.FromRGB(int r, int g, int b)` - Create color from RGB
- `Color.FromRGBA(int r, int g, int b, int a)` - Create color from RGBA
- `Color.GetOptimalTextColor(ImColor background)` - Get accessible text color
- `Color.Lighten(ImColor color, float amount)` - Lighten color
- `Color.Darken(ImColor color, float amount)` - Darken color

### Alignment Classes
- `new Alignment.Center(Vector2 contentSize)` - Center in available region
- `new Alignment.CenterWithin(Vector2 contentSize, Vector2 containerSize)` - Center in container

### Scoped Classes
- `new ScopedColor(ImGuiCol col, ImColor color)` - Scoped color application
- `new ScopedTextColor(ImColor color)` - Scoped text color
- `new ScopedStyleVar(ImGuiStyleVar var, float value)` - Scoped style variable
- `new ScopedTheme(string themeName)` - Scoped theme application
- `new ScopedThemeColor(Color semanticColor)` - Scoped semantic color

## 🎯 Demo Application

The included demo application showcases all features:

```bash
cd ImGuiStylerDemo
dotnet run
```

Features demonstrated:
- Interactive theme browser with live preview
- All 50+ themes with family categorization
- Scoped styling examples
- Color manipulation demos
- Alignment showcases
- Accessibility features

## 🤝 Contributing

We welcome contributions! Please see our contributing guidelines:

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Commit** your changes (`git commit -m 'Add amazing feature'`)
4. **Push** to the branch (`git push origin feature/amazing-feature`)
5. **Open** a Pull Request

### Development Setup
```bash
git clone https://github.com/ktsu-dev/ImGuiStyler.git
cd ImGuiStyler
dotnet restore
dotnet build
```

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE.md](LICENSE.md) file for details.

## 🙏 Acknowledgments

- **[ImGui.NET](https://github.com/mellinoe/ImGui.NET)** - .NET bindings for Dear ImGui
- **[Hexa.NET.ImGui](https://github.com/HexaEngine/Hexa.NET.ImGui)** - Modern ImGui bindings
- **Theme Inspirations**: Catppuccin, Tokyo Night, Gruvbox, and other amazing color schemes
- **Community Contributors** - Thank you for your themes, bug reports, and improvements!

## 🔗 Related Projects

- **[ktsu.ThemeProvider](https://github.com/ktsu-dev/ThemeProvider)** - Semantic theming foundation
- **[ktsu.ImGuiPopups](https://github.com/ktsu-dev/ImGuiPopups)** - Modal and popup utilities
- **[ktsu.Extensions](https://github.com/ktsu-dev/Extensions)** - Utility extensions

---

**Made with ❤️ by the ktsu.dev team**
