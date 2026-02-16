# ktsu ImGui Suite 🎨🖥️

[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE.md)
[![.NET](https://img.shields.io/badge/.NET-10.0%20%7C%209.0%20%7C%208.0-blue.svg)](https://dotnet.microsoft.com)](https://dotnet.microsoft.com/download/dotnet/9.0)

A comprehensive collection of .NET libraries for building modern, beautiful, and feature-rich applications with [Dear ImGui](https://github.com/ocornut/imgui). This suite provides everything you need from application scaffolding to advanced UI components and styling systems.

## 📦 Libraries Overview

### 🖥️ [ImGui.App](ImGui.App/README.md) - Application Foundation
[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGuiApp.svg)](https://www.nuget.org/packages/ktsu.ImGuiApp/)

**Complete application scaffolding for Dear ImGui applications**

- **Simple API**: Create ImGui applications with minimal boilerplate
- **Advanced Performance**: PID-controlled frame limiting with auto-tuning
- **Font Management**: Unicode/emoji support with dynamic scaling
- **Texture System**: Built-in texture management with caching
- **DPI Awareness**: Full high-DPI display support
- **Debug Tools**: Comprehensive logging and performance monitoring

```csharp
ImGuiApp.Start(new ImGuiAppConfig()
{
    Title = "My Application",
    OnRender = delta => { ImGui.Text("Hello, ImGui!"); }
});
```

### 🧩 [ImGui.Widgets](ImGui.Widgets/README.md) - Custom UI Components
[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGuiWidgets.svg)](https://www.nuget.org/packages/ktsu.ImGuiWidgets/)

**Rich collection of custom widgets and layout tools**

- **Advanced Controls**: Knobs, SearchBox, TabPanel with drag-and-drop
- **Layout Systems**: Resizable dividers, flexible Grid, Tree views
- **Interactive Elements**: Icons with events, Color indicators
- **Utilities**: Scoped IDs, alignment helpers, text formatting

```csharp
// Tabbed interface with closable, reorderable tabs
var tabPanel = new TabPanel("MyTabs", closable: true, reorderable: true);
tabPanel.AddTab("tab1", "First Tab", () => ImGui.Text("Content 1"));

// Powerful search with multiple filter types
ImGuiWidgets.SearchBox("##Search", ref searchTerm, ref filterType, ref matchOptions);
```

### 🪟 [ImGui.Popups](ImGui.Popups/README.md) - Modal Dialogs & Popups
[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGuiPopups.svg)](https://www.nuget.org/packages/ktsu.ImGuiPopups/)

**Professional modal dialogs and popup components**

- **Input Components**: String, Int, Float inputs with validation
- **File Management**: Advanced filesystem browser with filtering
- **Selection Tools**: Searchable lists with type-safe generics
- **User Interaction**: Message dialogs, prompts, custom modals

```csharp
// Get user input with validation
var inputString = new ImGuiPopups.InputString();
inputString.Open("Enter Name", "Name:", "Default", result => ProcessName(result));

// File browser with pattern filtering
var browser = new ImGuiPopups.FilesystemBrowser();
browser.Open("Open File", FilesystemBrowserMode.Open, 
    FilesystemBrowserTarget.File, startPath, OpenFile, new[] { "*.txt", "*.md" });
```

### 🎨 [ImGui.Styler](ImGui.Styler/README.md) - Themes & Styling
[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGuiStyler.svg)](https://www.nuget.org/packages/ktsu.ImGuiStyler/)

**Advanced theming system with 50+ built-in themes**

- **Theme Library**: Catppuccin, Tokyo Night, Gruvbox, Dracula, and more
- **Interactive Browser**: Visual theme selection with live preview
- **Color Tools**: Hex support, accessibility-focused contrast
- **Scoped Styling**: Apply styles to specific UI sections safely

```csharp
// Apply global theme
Theme.Apply("Catppuccin.Mocha");

// Scoped color styling
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

## 🚀 Quick Start

### Installation

Add the libraries you need via NuGet Package Manager or CLI:

```bash
# Complete application foundation
dotnet add package ktsu.ImGuiApp

# Custom widgets and controls
dotnet add package ktsu.ImGuiWidgets

# Modal dialogs and popups
dotnet add package ktsu.ImGuiPopups

# Theming and styling system
dotnet add package ktsu.ImGuiStyler
```

### Basic Application

Here's a complete example using multiple libraries together:

```csharp
using ktsu.ImGuiApp;
using ktsu.ImGuiStyler;
using ktsu.ImGuiPopups;
using ktsu.ImGuiWidgets;
using Hexa.NET.ImGui;

class Program
{
    private static readonly ImGuiPopups.MessageOK messageOK = new();
    private static readonly TabPanel tabPanel = new("MainTabs", true, true);
    private static string searchTerm = "";
    private static TextFilterType filterType = TextFilterType.Glob;
    private static TextFilterMatchOptions matchOptions = TextFilterMatchOptions.ByWholeString;

    static void Main()
    {
        ImGuiApp.Start(new ImGuiAppConfig
        {
            Title = "ImGui Suite Demo",
            OnStart = OnStart,
            OnRender = OnRender,
            OnAppMenu = OnAppMenu,
            PerformanceSettings = new()
            {
                FocusedFps = 60.0,
                UnfocusedFps = 10.0
            }
        });
    }

    private static void OnStart()
    {
        // Apply a beautiful theme
        Theme.Apply("Tokyo Night");
        
        // Setup tabs
        tabPanel.AddTab("widgets", "Widgets", RenderWidgetsTab);
        tabPanel.AddTab("styling", "Styling", RenderStylingTab);
    }

    private static void OnRender(float deltaTime)
    {
        // Main tabbed interface
        tabPanel.Draw();
        
        // Render popups
        messageOK.ShowIfOpen();
    }

    private static void RenderWidgetsTab()
    {
        ImGui.Text("Search Example:");
        ImGuiWidgets.SearchBox("##Search", ref searchTerm, ref filterType, ref matchOptions);
        
        if (ImGui.Button("Show Message"))
        {
            messageOK.Open("Hello!", "This is a popup message from ImGuiPopups!");
        }
    }

    private static void RenderStylingTab()
    {
        ImGui.Text("Theme Demo:");
        
        if (ImGui.Button("Choose Theme"))
        {
            Theme.ShowThemeSelector("Select Theme");
        }
        
        using (new ScopedColor(ImGuiCol.Text, Color.FromHex("#ff6b6b")))
        {
            ImGui.Text("This text is styled red!");
        }
        
        using (new Alignment.Center(ImGui.CalcTextSize("Centered Text")))
        {
            ImGui.Text("Centered Text");
        }
    }

    private static void OnAppMenu()
    {
        if (ImGui.BeginMenu("File"))
        {
            if (ImGui.MenuItem("Exit"))
                ImGuiApp.Stop();
            ImGui.EndMenu();
        }
        
        if (ImGui.BeginMenu("View"))
        {
            if (ImGui.MenuItem("Change Theme"))
                Theme.ShowThemeSelector("Select Theme");
            ImGui.EndMenu();
        }
    }
}
```

## 🎯 Key Features

- **🖥️ Complete Application Framework**: Everything needed for production ImGui applications
- **🎨 Professional Theming**: 50+ themes with interactive browser and accessibility features  
- **🧩 Rich Widget Library**: Advanced controls like tabbed interfaces, search boxes, and knobs
- **🪟 Modal System**: Type-safe popups, file browsers, and input validation
- **⚡ High Performance**: PID-controlled frame limiting with auto-tuning capabilities
- **🎯 Developer Friendly**: Clean APIs, comprehensive documentation, and extensive examples
- **🔧 Production Ready**: Debug logging, error handling, and resource management
- **🌐 Modern .NET**: Multi-targeted for .NET 10, 9, 8, 7, 6, 5, and netstandard2.0/2.1 with latest language features

## 📚 Documentation

Each library has comprehensive documentation with examples:

- **[📖 ImGui.App Documentation](ImGui.App/README.md)** - Application scaffolding, performance tuning, font management
- **[📖 ImGui.Widgets Documentation](ImGui.Widgets/README.md)** - Widget gallery, layout systems, interactive controls  
- **[📖 ImGui.Popups Documentation](ImGui.Popups/README.md)** - Modal dialogs, file browsers, input validation
- **[📖 ImGui.Styler Documentation](ImGui.Styler/README.md)** - Theme gallery, color tools, styling utilities

## 🎮 Demo Applications

The repository includes comprehensive demo applications showcasing all features:

```bash
# Clone the repository
git clone https://github.com/ktsu-dev/ImGui.git
cd ImGui

# Run the main demo (showcases all libraries)
dotnet run --project examples/ImGuiAppDemo

# Run individual library demos
dotnet run --project examples/ImGuiWidgetsDemo
dotnet run --project examples/ImGuiPopupsDemo  
dotnet run --project examples/ImGuiStylerDemo
```

Each demo includes:
- **Interactive Examples**: Try all features with live code
- **Performance Testing**: See PID frame limiting and throttling in action
- **Theme Gallery**: Browse and apply all 50+ built-in themes
- **Widget Showcase**: Complete widget and layout demonstrations
- **Integration Examples**: How libraries work together

## 🛠️ Requirements

- **.NET 10.0, 9.0, or 8.0** (multi-targeted libraries support .NET 10.0, 9.0, 8.0, 7.0, 6.0, 5.0, and netstandard2.0/2.1)
- **Windows, macOS, or Linux** (cross-platform support via Silk.NET)
- **OpenGL 3.3** or higher (handled automatically)

## 🤝 Contributing

We welcome contributions! Here's how to get started:

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)  
3. **Make** your changes with tests
4. **Commit** your changes (`git commit -m 'Add amazing feature'`)
5. **Push** to the branch (`git push origin feature/amazing-feature`)
6. **Open** a Pull Request

### Development Setup

```bash
git clone https://github.com/ktsu-dev/ImGui.git
cd ImGui
dotnet restore
dotnet build
```

Please ensure:
- Code follows existing style conventions
- All tests pass (`dotnet test`)  
- Documentation is updated for new features
- Examples demonstrate new functionality

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE.md](LICENSE.md) file for details.

## 🙏 Acknowledgments

- **[Dear ImGui](https://github.com/ocornut/imgui)** - The amazing immediate mode GUI library
- **[Hexa.NET.ImGui](https://github.com/HexaEngine/Hexa.NET.ImGui)** - Excellent .NET bindings for Dear ImGui  
- **[Silk.NET](https://github.com/dotnet/Silk.NET)** - Cross-platform .NET OpenGL and windowing
- **Theme Communities** - Catppuccin, Tokyo Night, Gruvbox creators and communities
- **Contributors** - Everyone who has contributed code, themes, bug reports, and feedback

## 🔗 Related Projects

- **[ktsu.ThemeProvider](https://github.com/ktsu-dev/ThemeProvider)** - Semantic theming foundation
- **[ktsu.Extensions](https://github.com/ktsu-dev/Extensions)** - Utility extension methods
- **[ktsu.StrongPaths](https://github.com/ktsu-dev/StrongPaths)** - Type-safe path handling
- **[ktsu.TextFilter](https://github.com/ktsu-dev/TextFilter)** - Advanced text filtering utilities

---

**Made with ❤️ by the ktsu.dev team**

*Build beautiful, performant desktop applications with the power of Dear ImGui and .NET*
