# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
dotnet restore                                    # Restore dependencies
dotnet build                                      # Build solution
dotnet test                                       # Run all tests
dotnet test --filter "FullyQualifiedName~Name"   # Run specific test
dotnet run --project examples/ImGuiAppDemo        # Run main demo
dotnet run --project examples/ImGuiWidgetsDemo    # Run widgets demo
dotnet run --project examples/ImGuiStylerDemo     # Run styler demo
dotnet run --project examples/ImGuiPopupsDemo     # Run popups demo
dotnet build -c Release                           # Build release configuration
```

## Project Structure

This is the **ktsu ImGui Suite**, a collection of .NET libraries for building Dear ImGui applications. The solution (`ImGui.sln`) uses:

- **ktsu.Sdk** - Custom SDK providing shared build configuration
- **MSTest.Sdk** - Test project SDK with Microsoft Testing Platform
- Multi-targeting: `net10.0;net9.0;net8.0` for libraries, `net10.0` for tests

### Libraries

- **ImGui.App** (`ktsu.ImGuiApp`) - Application foundation with windowing, rendering, font/texture management, PID frame limiting, DPI awareness
- **ImGui.Widgets** (`ktsu.ImGuiWidgets`) - Custom UI components: TabPanel, Knob, SearchBox, RadialProgressBar, Grid, DividerContainer, Combo, Tree, Icons, ColorIndicator, Text, Image, ScopedDisable, ScopedId
- **ImGui.Popups** (`ktsu.ImGuiPopups`) - Modal dialogs: MessageOK, Prompt, InputString/Int/Float, FilesystemBrowser, SearchableList
- **ImGui.Styler** (`ktsu.ImGuiStyler`) - Theming system with 50+ built-in themes, scoped styling, Button.Alignment, Text.Color semantic colors, Indent utilities, Alignment helpers, color palettes, and interactive theme browser
- **NodeGraph** (`ktsu.NodeGraph`) - UI-agnostic attribute-based node graph metadata: `[Node]`, `[InputPin]`, `[OutputPin]`, `[NodeExecute]`, `[NodeBehavior]`, pin type utilities
- **ImGuiNodeEditor** (`ktsu.ImGuiNodeEditor`) - ImNodes-based visual node editor with `NodeEditorEngine`, `AttributeBasedNodeFactory`, physics-based layout, `NodeEditorRenderer`, `NodeEditorInputHandler`

### Examples

- `examples/ImGuiAppDemo/` - Main application demo
- `examples/ImGuiWidgetsDemo/` - Widget showcase
- `examples/ImGuiStylerDemo/` - Theme gallery
- `examples/ImGuiPopupsDemo/` - Popup demonstrations

### Tests

- `tests/ImGui.App.Tests/` - App framework tests with mock OpenGL provider
- `tests/NodeGraph.Tests/` - Node graph attribute and type utility tests

### Key Files

- `ImGui.App/ImGuiApp.cs` - Main static entry point (`ImGuiApp.Start()`, `ImGuiApp.Stop()`)
- `ImGui.App/ImGuiAppConfig.cs` - Application configuration record
- `ImGui.App/PidFrameLimiter.cs` - PID-controlled frame rate limiter with auto-tuning
- `ImGui.App/FontMemoryGuard.cs` - GPU memory management for font atlases
- `ImGui.App/FontHelper.cs` - Unicode, emoji, and Nerd Font character range support
- `ImGui.App/ForceDpiAware.cs` - Multi-platform DPI detection
- `ImGui.App/ImGuiExtensionManager.cs` - Auto-detection of ImGuizmo, ImNodes, ImPlot
- `ImGui.Widgets/DividerZone.cs` - Resizable split pane layout
- `ImGui.Widgets/TabPanel.cs` - Tabbed interface with drag-and-drop
- `ImGui.Styler/Theme.cs` - Theme management, browser, and selector
- `ImGui.Styler/ScopedColor.cs` - RAII-pattern color styling
- `NodeGraph/NodeAttribute.cs` - Core node attributes
- `NodeGraph/PinAttribute.cs` - Pin declaration attributes
- `ImGuiNodeEditor/NodeEditorEngine.cs` - Node graph business logic

### Dependencies

- **Hexa.NET.ImGui** (2.2.9) - Dear ImGui .NET bindings
- **Hexa.NET.ImGuizmo** (2.2.9) - ImGuizmo gizmo extension
- **Hexa.NET.ImNodes** (2.2.9) - ImNodes node editor extension
- **Hexa.NET.ImPlot** (2.2.9) - ImPlot charting extension
- **Silk.NET** (2.23.0) - Cross-platform windowing and OpenGL
- **SixLabors.ImageSharp** (3.1.12) - Image loading
- **ktsu.ThemeProvider** (1.0.11) - Semantic theming foundation
- **ktsu.ThemeProvider.ImGui** (1.0.11) - ImGui theming integration
- **ktsu.TextFilter** (1.5.4) - Text filtering (Glob/Regex/Fuzzy)
- **ktsu.FuzzySearch** (1.2.2) - Fuzzy search matching
- **ktsu.Extensions** (1.5.9) - Collection extension methods
- **ktsu.CaseConverter** (1.3.6) - String case conversion
- **ktsu.Semantics.Paths** (1.0.28) - Type-safe path handling
- **ktsu.Semantics.Strings** (1.0.28) - Type-safe string wrappers
- **ktsu.Semantics.Quantities** (1.0.29) - Typed quantity calculations
- **ktsu.Invoker** (1.1.2) - Delegate invocation utilities
- **ktsu.ScopedAction** (1.1.6) - RAII-pattern scoped actions
- **Polyfill** (9.7.7) - Backport newer .NET APIs

## Architecture

### Static Entry Points with Nested Classes

Each library exposes a static class as its main entry point, with nested public classes for components:

- `ImGuiApp.Start()` / `ImGuiApp.Stop()` - Application lifecycle
- `ImGuiWidgets.SearchBox()`, `ImGuiWidgets.Knob()`, `ImGuiWidgets.Combo()`, `ImGuiWidgets.RadialProgressBar()` - Widget methods
- `new ImGuiWidgets.TabPanel()`, `new ImGuiWidgets.DividerContainer()` - Widget instances
- `new ImGuiPopups.InputString()`, `new ImGuiPopups.FilesystemBrowser()` - Popup instances
- `Theme.Apply()`, `Theme.ShowThemeSelector()` - Theme management
- `Button.Alignment.Center()`, `Text.Color.Error()`, `Indent.ByDefault()` - Styling utilities

### Configuration Pattern

```csharp
ImGuiApp.Start(new ImGuiAppConfig
{
    Title = "App",
    OnRender = delta => { /* render code */ },
    OnStart = () => { /* init code */ },
    PerformanceSettings = new() { FocusedFps = 60.0 }
});
```

### Scoped Styling (RAII Pattern)

```csharp
using (new ScopedColor(ImGuiCol.Text, Color.FromHex("#ff6b6b")))
{
    ImGui.Text("Styled text");  // Auto-restored after block
}

using (Text.Color.Error())
{
    ImGui.Text("Error message");
}

using (new ScopedDisable(true))
{
    ImGui.Button("Disabled button");
}

using (Button.Alignment.Center())
{
    ImGui.Button("Centered text", new Vector2(200, 30));
}
```

### Node Graph Architecture

The node graph system follows a clean separation of concerns:

- **NodeGraph** (UI-agnostic): Attribute-based metadata for declaring nodes, pins, execution modes, and type compatibility. No dependency on any rendering library.
- **ImGuiNodeEditor**: Renders and interacts with the graph using ImNodes. Split into:
  - `NodeEditorEngine` - Business logic (nodes, links, physics)
  - `AttributeBasedNodeFactory` - Creates nodes from attribute-decorated types
  - `NodeEditorRenderer` - Pure ImNodes rendering
  - `NodeEditorInputHandler` - Input event processing

### Key Technical Details

- **PID frame limiter** with auto-tuning (Coarse/Fine/Precision phases)
- **Throttled rendering**: Different FPS for focused/unfocused/idle/minimized states
- **Font memory management** via `FontMemoryGuard` with GCHandle pinning and GPU detection
- **Texture caching** with concurrent dictionary, auto-cleanup on context change
- **ImGui extension auto-detection** via reflection for ImGuizmo, ImNodes, ImPlot
- **Physics-based node layout** with force-directed simulation, spring links, and stability detection
- **Dear ImGui paradigm**: Immediate mode - render every frame, no retained state

## Testing

Tests use **MSTest.Sdk** with the Microsoft Testing Platform. The ImGui.App tests use a mock OpenGL provider (`MockGL`, `TestOpenGLProvider`) to test rendering logic without a real GPU. NodeGraph tests validate attribute scanning, pin type utilities, and node factory behavior.

```bash
dotnet test                                          # Run all tests
dotnet test --filter "FullyQualifiedName~TestGL"    # Run specific test class
```

## Adding Components

### New Widget

1. Add class to `ImGui.Widgets/`
2. Follow existing widget patterns (static methods or instance classes)
3. Add demo to `examples/ImGuiWidgetsDemo/`

### New Theme

1. Add theme definition to `ImGui.Styler/`
2. Test in `examples/ImGuiStylerDemo/`

### New Node Type

1. Decorate a class/struct with `[Node]` attribute
2. Add `[InputPin]` / `[OutputPin]` to properties/fields
3. Add `[NodeExecute]` to the execution method
4. Register with `AttributeBasedNodeFactory.RegisterNodeType<T>()`

### Modifying ImGui.App

Changes affect all consumers. Test with all example applications.

## Code Style

- **Tabs** for indentation (not spaces)
- **File-scoped namespaces** with using directives inside
- **Explicit types** - no `var`
- **No `this.` qualifier**
- **Always use braces** for control flow
- **Primary constructors** when appropriate

All C# files require this header:

```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.
```

## CI/CD

Uses `scripts/PSBuild.psm1` PowerShell module for CI pipeline. Version increments are controlled by commit message tags: `[major]`, `[minor]`, `[patch]`, `[pre]`. Auto-generated files (VERSION.md, CHANGELOG.md, LICENSE.md) should not be manually edited. CI runs on Windows, publishes to NuGet, uses SonarQube for analysis.

## Code Quality

Do not add global suppressions for warnings. Use explicit suppression attributes with justifications when needed, with preprocessor defines only as fallback. Make the smallest, most targeted suppressions possible.
