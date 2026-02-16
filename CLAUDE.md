# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is **ktsu ImGui Suite**, a collection of .NET libraries for building Dear ImGui applications:
- **ImGui.App** - Application foundation with windowing, rendering, font/texture management
- **ImGui.Widgets** - Custom UI components (TabPanel, Knob, RadialProgressBar with countdown/count-up timers, SearchBox with fuzzy matching, Grid, DividerContainer with resizable layouts, Combo for type-safe selections, ScopedDisable, ScopedId, Icons, ColorIndicator, Text, Image, Tree)
- **ImGui.Popups** - Modal dialogs (MessageOK, Prompt, InputString, FilesystemBrowser, SearchableList)
- **ImGui.Styler** - Theming system with 50+ built-in themes, scoped styling, Button.Alignment for text alignment, Text.Color for semantic colors (Error/Warning/Info/Success), Indent utilities, and Alignment helpers for centering

## Build Commands

```bash
dotnet restore                                    # Restore dependencies
dotnet build                                      # Build solution
dotnet test                                       # Run all tests
dotnet test --filter "FullyQualifiedName~Name"   # Run specific test
dotnet run --project examples/ImGuiAppDemo        # Run main demo
```

## Architecture

### Static Entry Points with Nested Classes
Each library exposes a static class as its main entry point, with nested public classes for components:
- `ImGuiApp.Start()` / `ImGuiApp.Stop()` - Application lifecycle
- `ImGuiWidgets.SearchBox()`, `ImGuiWidgets.Knob()`, `ImGuiWidgets.Combo()`, `ImGuiWidgets.RadialProgressBar()`, `ImGuiWidgets.RadialCountdown()`, `ImGuiWidgets.RadialCountUp()` - Widget methods
- `new ImGuiWidgets.TabPanel()`, `new ImGuiWidgets.DividerContainer()`, `new ScopedDisable()` - Widget instances
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
// Scoped colors
using (new ScopedColor(ImGuiCol.Text, Color.FromHex("#ff6b6b")))
{
    ImGui.Text("Styled text");  // Auto-restored after block
}

// Semantic text colors
using (Text.Color.Error())
{
    ImGui.Text("Error message");
}

// Scoped disable
using (new ScopedDisable(true))
{
    ImGui.Button("Disabled button");
}

// Button text alignment
using (Button.Alignment.Center())
{
    ImGui.Button("Centered text", new Vector2(200, 30));
}
```

### Key Technical Details
- **PID frame limiter** with auto-tuning (Coarse/Fine/Precision phases)
- **Throttled rendering**: Different FPS for focused/unfocused/idle/minimized states
- **Font memory management** via `FontMemoryGuard` with GCHandle pinning
- **Texture caching** with concurrent dictionary, auto-cleanup on context change
- **Dear ImGui paradigm**: Immediate mode - render every frame, no retained state

## Dependencies

All managed centrally in `Directory.Packages.props`:
- **Hexa.NET.ImGui** - Dear ImGui bindings
- **Silk.NET** - Cross-platform windowing (ImGui.App only)
- **SixLabors.ImageSharp** - Image loading
- **ktsu.ThemeProvider** - Semantic theming foundation

## Multi-Targeting

Projects target `net10.0;net9.0;net8.0`. Tests target `net10.0` only.

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

## Adding Components

### New Widget

1. Add class to `ImGui.Widgets/`
2. Follow existing widget patterns (static methods or instance classes)
3. Add demo to `examples/ImGuiWidgetsDemo/`
4. Update `ImGui.Widgets/README.md`

## Key Widget Features

### RadialProgressBar

The RadialProgressBar widget supports multiple display modes and use cases:

**Text Display Modes:**

- `Percentage` (default) - Shows 0-100% progress
- `Time` - Shows time in MM:SS or HH:MM:SS format
- `Custom` - Shows user-provided text

**Convenience Methods:**

- `RadialProgressBar(progress, ...)` - General-purpose with configurable text modes
- `RadialCountdown(currentTime, totalTime, ...)` - Countdown timer (e.g., "05:00" → "00:00")
- `RadialCountUp(elapsedTime, totalTime, ...)` - Count-up timer (e.g., "00:00" → "05:00")

**Common Use Cases:**

- Loading indicators with percentage
- Countdown timers for tasks/sessions
- Pomodoro timers (25min work, 5min break)
- Elapsed time tracking
- Combined progress + time display

**Example:**

```csharp
// Countdown timer
float timeRemaining = 300.0f;  // 5 minutes
ImGuiWidgets.RadialCountdown(timeRemaining, 300.0f);  // Shows "05:00"

// Progress with custom text
ImGuiWidgets.RadialProgressBar(0.75f, textMode: ImGuiRadialProgressBarTextMode.Custom, customText: "Loading...");
```

### New Theme
1. Add theme definition to `ImGui.Styler/`
2. Test in `examples/ImGuiStylerDemo/`
3. Update theme gallery in README

### Modifying ImGui.App
Changes affect all consumers. Test with all example applications. Update CHANGELOG.md for breaking changes.

## Version Control

- Commit message tags control versioning: `[major]`, `[minor]`, `[patch]`, `[pre]`
- Auto-generated files (VERSION.md, CHANGELOG.md, LICENSE.md) - do not manually edit
- CI runs on Windows, publishes to NuGet, uses SonarQube for analysis
