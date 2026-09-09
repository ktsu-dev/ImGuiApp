# ktsu.ImGuiApp

> A comprehensive collection of .NET libraries for building modern, feature-rich desktop applications with Dear ImGui.

[![License](https://img.shields.io/github/license/ktsu-dev/ImGuiApp.svg?label=License&logo=nuget)](LICENSE.md)
[![NuGet Version](https://img.shields.io/nuget/v/ktsu.ImGui.App?label=Stable&logo=nuget)](https://nuget.org/packages/ktsu.ImGui.App)
[![NuGet Version](https://img.shields.io/nuget/vpre/ktsu.ImGui.App?label=Latest&logo=nuget)](https://nuget.org/packages/ktsu.ImGui.App)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ktsu.ImGui.App?label=Downloads&logo=nuget)](https://nuget.org/packages/ktsu.ImGui.App)
[![GitHub commit activity](https://img.shields.io/github/commit-activity/m/ktsu-dev/ImGuiApp?label=Commits&logo=github)](https://github.com/ktsu-dev/ImGuiApp/commits/main)
[![GitHub contributors](https://img.shields.io/github/contributors/ktsu-dev/ImGuiApp?label=Contributors&logo=github)](https://github.com/ktsu-dev/ImGuiApp/graphs/contributors)
[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/ktsu-dev/ImGuiApp/dotnet.yml?branch=main&label=Build&logo=github)](https://github.com/ktsu-dev/ImGuiApp/actions)

## Introduction

`ktsu.ImGui.App` is a suite of .NET libraries that provides everything you need to build desktop applications with [Dear ImGui](https://github.com/ocornut/imgui). The suite includes application scaffolding, custom widgets, modal dialogs, a theming system, and a node graph editor framework. Built on Hexa.NET.ImGui bindings and Silk.NET for cross-platform windowing, it supports .NET 10, 9, and 8.

## Features

- **Application Foundation**: Complete application scaffolding with windowing, OpenGL rendering, font management, texture caching, dependency-free PNG/JPEG/BMP/TGA decoding, and DPI awareness via `ktsu.ImGui.App`
- **PID Frame Limiting**: High-precision PID-controlled frame rate limiting with auto-tuning and adaptive throttling for focused, unfocused, idle, and minimized states
- **Custom Widgets**: Around sixty UI components via `ktsu.ImGui.Widgets` — controls (Switch, SegmentedControl, Stepper, RangeSlider, XYPad, Knob, Rating, Chip, PinInput, SearchBox with fuzzy matching), layout (DividerContainer, Grid, TabPanel, Card, Tree, ImageCanvas, overlays), feedback (RadialProgressBar with countdown/count-up timers, spinners, skeleton placeholders, badges), signal views (Histogram, FlameGraph, DbMeter, oscilloscope), motion (tweens, springs, inertial scrolling, gesture detection), and callback-driven Sequencer, CurveEditor and BezierEditor
- **Modal Dialogs**: Professional popup system with MessageOK, Prompt, InputString/Int/Float, FilesystemBrowser, and SearchableList via `ktsu.ImGui.Popups`
- **Theming System**: 50+ built-in themes (Catppuccin, Tokyo Night, Gruvbox, Dracula, and more) with scoped styling, semantic text colors, button alignment, color palettes, and an interactive theme browser via `ktsu.ImGui.Styler`
- **Node Graph Framework**: Attribute-based node declaration system with UI-agnostic `ktsu.NodeGraph` metadata library and ImNodes-based visual editor `ktsu.ImGui.NodeEditor` with physics-based layout
- **Font Management**: Unicode, emoji, and Nerd Font support with GPU memory management via `FontMemoryGuard` and dynamic font scaling
- **Scoped Styling**: RAII-pattern disposable wrappers for colors, styles, fonts, themes, disable states, and UI scaling
- **Color Utilities**: HSL/HSLA color creation, accessibility-focused contrast calculations, color manipulation extensions, and semantic color palettes
- **Markdown Rendering**: CommonMark rendering (headings, emphasis, lists, tables, links, images) built on Markdig via `ktsu.ImGui.Markdown`, standalone and independent of `ktsu.ImGui.App`
- **Syntax Highlighting**: Themed code rendering for fifteen languages, with an optional line-number gutter and cached tokenization via `ktsu.ImGui.SyntaxHighlighting`, standalone and independent of `ktsu.ImGui.App`. The data-driven tokenizer itself lives in the renderer-agnostic `ktsu.SyntaxHighlighting`, which knows nothing about ImGui
- **Embedded Languages**: XML in a doc comment, JSON in a fixture string and SQL in a query string are recognized inside the host language and highlighted in place, or named outright with a `// lang=json` hint comment

## Libraries

### ImGui.App - Application Foundation

[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGui.App?label=ktsu.ImGui.App&logo=nuget)](https://nuget.org/packages/ktsu.ImGui.App)

Complete application scaffolding for Dear ImGui applications with windowing, rendering, font/texture management, and performance tuning. Image decoding for PNG, JPEG, BMP and TGA is built in, so the package carries no imaging dependency.

### ImGui.Widgets - Custom UI Components

[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGui.Widgets?label=ktsu.ImGui.Widgets&logo=nuget)](https://nuget.org/packages/ktsu.ImGui.Widgets)

Rich collection of custom widgets, grouped by what they are for: input and controls (Switch, SegmentedControl, Stepper, RangeSlider, XYPad, Knob, Rating, Chip, PinInput, SearchBox, Combo), display and status (Avatar, Badge, ColorIndicator, Icon, Text, Image, PageIndicator, Tooltip, Breadcrumb), progress and loading (RadialProgressBar, BufferingBar, Spinner, skeleton placeholders), data and signals (Histogram, HandleTrack, FlameGraph, DbMeter, Scope), layout and containers (DividerContainer, Grid, TabPanel, Card, Tree, ImageCanvas, OverlayHost, ScopedId, ScopedDisable), motion and gestures (Tween, Spring, Easing, InertialScroll, GestureDetector), callback-driven editors (Sequencer, CurveEditor, BezierEditor), and stateful dialogs.

### ImGui.Popups - Modal Dialogs

[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGui.Popups?label=ktsu.ImGui.Popups&logo=nuget)](https://nuget.org/packages/ktsu.ImGui.Popups)

Professional modal dialogs: MessageOK, Prompt, InputString/Int/Float with validation, FilesystemBrowser with glob filtering, and SearchableList with type-safe generics.

### ImGui.Color - Color Adapter

[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGui.Color?label=ktsu.ImGui.Color&logo=nuget)](https://nuget.org/packages/ktsu.ImGui.Color)

Bridges the physically-grounded [`ktsu.Semantics.Color`](https://github.com/ktsu-dev/Semantics) type and ImGui's `ImColor`/`ImU32`/`Vector4`. Colors are constructed from the semantic `Color`/`Srgb` types and converted at the ImGui seam — `ColorImGuiExtensions` for the linear `Color`, and `SrgbImGuiExtensions` for direct `Srgb` packing with no linear round-trip — plus `ImColor` extension operations for adjustment (lighten/darken, saturate, hue, invert), analysis (relative luminance, contrast ratio, perceptual distance), and readable text-color selection. All color math delegates to `ktsu.Semantics.Color`, so results are correct across the sRGB/linear boundary.

### ImGui.Styler - Themes and Styling

[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGui.Styler?label=ktsu.ImGui.Styler&logo=nuget)](https://nuget.org/packages/ktsu.ImGui.Styler)

Advanced theming system with 50+ built-in themes, scoped styling, semantic text colors, button alignment, a theme-aware color palette, and an interactive theme browser. Color construction and manipulation are provided by `ImGui.Color`.

### NodeGraph - Node Metadata (UI-Agnostic)

[![NuGet](https://img.shields.io/nuget/v/ktsu.NodeGraph?label=ktsu.NodeGraph&logo=nuget)](https://nuget.org/packages/ktsu.NodeGraph)

Generic attribute-based system for declaring node graphs. Decorate classes, structs, and methods with node metadata (pins, execution modes, visibility, deprecation) without coupling to a specific editor implementation.

### ImGui.NodeEditor - Visual Node Editor

[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGui.NodeEditor?label=ktsu.ImGui.NodeEditor&logo=nuget)](https://nuget.org/packages/ktsu.ImGui.NodeEditor)

Attribute-driven visual node editor built on ImNodes. Includes `NodeEditorEngine` for business logic, `AttributeBasedNodeFactory` for node creation from decorated types, physics-based layout simulation, and `NodeEditorRenderer`/`NodeEditorInputHandler` for rendering and interaction.

### ImGui.Markdown - Markdown Rendering

[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGui.Markdown?label=ktsu.ImGui.Markdown&logo=nuget)](https://nuget.org/packages/ktsu.ImGui.Markdown)

CommonMark markdown renderer built on Markdig, with pipe tables, task lists, and autolinks. Renders headings, emphasis, inline and block code, lists, blockquotes, tables, links, and images directly inside Dear ImGui. Standalone, with no dependency on `ktsu.ImGui.App`.

### SyntaxHighlighting - Tokenizing (UI-Agnostic)

[![NuGet](https://img.shields.io/nuget/v/ktsu.SyntaxHighlighting?label=ktsu.SyntaxHighlighting&logo=nuget)](https://nuget.org/packages/ktsu.SyntaxHighlighting)

Turns source text into classified token runs, with built-in definitions for C#, C, C++, JavaScript, TypeScript, Python, JSON, YAML, XML, HTML, CSS, SQL, shell, Lua, and plain text. Languages are plain data, so applications can register their own, and comments and strings are searched for an embedded language — XML doc comments, JSON payloads, SQL queries. Nothing here draws anything: no ImGui, no graphics API, just `SyntaxHighlighter.Highlight` and a `SyntaxTheme` of semantic colors.

### ImGui.SyntaxHighlighting - Code Highlighting

[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGui.SyntaxHighlighting?label=ktsu.ImGui.SyntaxHighlighting&logo=nuget)](https://nuget.org/packages/ktsu.ImGui.SyntaxHighlighting)

Draws what `ktsu.SyntaxHighlighting` classifies, inside Dear ImGui: a background panel, an optional line-number gutter, and colored token runs whose palette follows the host's light or dark theme. Standalone, with no dependency on `ktsu.ImGui.App`, and it doubles as the code-block renderer for `ktsu.ImGui.Markdown`.

### ForceDirectedLayout - Graph Layout Simulation

[![NuGet](https://img.shields.io/nuget/v/ktsu.ForceDirectedLayout?label=ktsu.ForceDirectedLayout&logo=nuget)](https://nuget.org/packages/ktsu.ForceDirectedLayout)

Renderer-agnostic force-directed layout: bodies repel across the clear space between their bounding boxes, edges pull like springs, gravity holds the graph together, and overlapping boxes are pushed apart. Double precision, AOT- and trim-clean, with no runtime dependencies, and also published as a native shared library with a C ABI. `ktsu.ImGui.NodeEditor` uses it to lay out node graphs.

### ImGui.Probes - Item Recording for Tests

[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGui.Probes?label=ktsu.ImGui.Probes&logo=nuget)](https://nuget.org/packages/ktsu.ImGui.Probes)

Lets a UI library record where it drew named items so a test can address a widget by name instead of by pixel position. Tiny and dependency free beyond the ImGui binding, so any widget library can mark its items without depending on an application host or on test infrastructure.

### ImGui.App.Testing - Headless Test Harness

[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGui.App.Testing?label=ktsu.ImGui.App.Testing&logo=nuget)](https://nuget.org/packages/ktsu.ImGui.App.Testing)

Runs a `ktsu.ImGui.App` application headlessly: a CPU rasterizer with no window and no GPU, input injected straight into ImGui, and frames advanced under the test's control. Widgets are addressed by name through `ktsu.ImGui.Probes`, and frames are captured for assertions and failure artifacts.

## Installation

### Package Manager Console

```powershell
Install-Package ktsu.ImGui.App
Install-Package ktsu.ImGui.Widgets
Install-Package ktsu.ImGui.Popups
Install-Package ktsu.ImGui.Styler
Install-Package ktsu.NodeGraph
Install-Package ktsu.ImGui.Markdown
Install-Package ktsu.SyntaxHighlighting
Install-Package ktsu.ImGui.SyntaxHighlighting
```

### .NET CLI

```bash
dotnet add package ktsu.ImGui.App
dotnet add package ktsu.ImGui.Widgets
dotnet add package ktsu.ImGui.Popups
dotnet add package ktsu.ImGui.Styler
dotnet add package ktsu.NodeGraph
dotnet add package ktsu.ImGui.Markdown
dotnet add package ktsu.SyntaxHighlighting
dotnet add package ktsu.ImGui.SyntaxHighlighting
```

### Package Reference

```xml
<PackageReference Include="ktsu.ImGui.App" Version="x.y.z" />
<PackageReference Include="ktsu.ImGui.Widgets" Version="x.y.z" />
<PackageReference Include="ktsu.ImGui.Popups" Version="x.y.z" />
<PackageReference Include="ktsu.ImGui.Styler" Version="x.y.z" />
<PackageReference Include="ktsu.NodeGraph" Version="x.y.z" />
<PackageReference Include="ktsu.ImGui.Markdown" Version="x.y.z" />
<PackageReference Include="ktsu.SyntaxHighlighting" Version="x.y.z" />
<PackageReference Include="ktsu.ImGui.SyntaxHighlighting" Version="x.y.z" />
```

## Usage Examples

### Basic Application

```csharp
using ktsu.ImGui.App;
using Hexa.NET.ImGui;

ImGuiApp.Start(new ImGuiAppConfig
{
    Title = "My Application",
    OnRender = delta =>
    {
        ImGui.Text("Hello, ImGui!");
    }
});
```

### Application with Menu and Performance Settings

```csharp
using ktsu.ImGui.App;
using ktsu.ImGui.Styler;
using Hexa.NET.ImGui;

ImGuiApp.Start(new ImGuiAppConfig
{
    Title = "Full Application",
    OnStart = () =>
    {
        Theme.Apply("Tokyo Night");
    },
    OnRender = delta =>
    {
        ImGui.Text("Content goes here");
    },
    OnAppMenu = () =>
    {
        if (ImGui.BeginMenu("File"))
        {
            if (ImGui.MenuItem("Exit"))
            {
                ImGuiApp.Stop();
            }
            ImGui.EndMenu();
        }
    },
    PerformanceSettings = new ImGuiAppPerformanceSettings
    {
        FocusedFps = 60.0,
        UnfocusedFps = 10.0,
        IdleTimeoutSeconds = 30.0
    }
});
```

### Overlay Mode (Always-On-Top HUD)

Overlay mode turns the application window into a borderless, always-on-top, translucent HUD
with optional click-through — ideal for live status displays that sit over other apps. Drive
it from your render callback; the calls are cheap to make every frame (the native window is
only restyled when something actually changes). A dedicated `OverlayFps` keeps the overlay
animating smoothly even while unfocused, bypassing the normal focus/idle/visibility throttling.

```csharp
bool overlayEnabled = true;

ImGuiApp.Start(new ImGuiAppConfig
{
    Title = "Status HUD",
    PerformanceSettings = new ImGuiAppPerformanceSettings { OverlayFps = 60.0 },
    OnRender = delta =>
    {
        if (overlayEnabled)
        {
            // opacity 0.2–1.0, plus optional click-through (mouse passes through to apps behind it).
            ImGuiApp.EnableOverlay(opacity: 0.9f, clickThrough: false);
            // Optional: lock it to a corner of the monitor work area (Windows).
            ImGuiApp.SetOverlayGeometry(OverlayCorner.TopRight, offsetX: 24, offsetY: 24, width: 360, height: 280);
        }
        else
        {
            ImGuiApp.DisableOverlay(); // restores the decorated window
        }

        ImGui.Text("Live status…");
    }
});
```

Overlay window styling (borderless / topmost / translucency / click-through) is implemented on
Windows. On other platforms `IsOverlayActive` and `OverlayFps` still apply, but the window is
not restyled.

### Widgets

```csharp
using ktsu.ImGui.Widgets;
using Hexa.NET.ImGui;

// Tabbed interface
ImGuiWidgets.TabPanel tabPanel = new("MyTabs", closable: true, reorderable: true);
tabPanel.AddTab("tab1", "First Tab", () => ImGui.Text("Content 1"));
tabPanel.Draw();

// Search box with filtering
string searchTerm = "";
SearchBoxOptions searchOptions = new(Label: "##Search", FilterType: TextFilterType.Glob);
ImGuiWidgets.SearchBox(ref searchOptions, ref searchTerm);

// Radial countdown timer
float timeRemaining = 300.0f;
ImGuiWidgets.RadialCountdown(timeRemaining, 300.0f);

// Resizable divider layout
ImGuiWidgets.DividerContainer divider = new("MySplit", ImGuiWidgets.DividerLayout.Columns);
divider.Add("left", 200f, true, dt => ImGui.Text("Left pane"));
divider.Add("right", 400f, true, dt => ImGui.Text("Right pane"));
divider.Tick(deltaTime);
```

### Popups and Dialogs

```csharp
using ktsu.ImGui.Popups;

// Message dialog
ImGuiPopups.MessageOK messageOK = new();
messageOK.Open("Hello!", "This is a message.");
messageOK.ShowIfOpen();

// String input dialog
ImGuiPopups.InputString inputString = new();
inputString.Open("Enter Name", "Name:", "Default", result => ProcessName(result));
inputString.ShowIfOpen();

// File browser
ImGuiPopups.FilesystemBrowser browser = new();
browser.FileOpen("Open File", path => LoadFile(path), "*.txt");
browser.ShowIfOpen();

// Searchable list
ImGuiPopups.SearchableList<string> list = new();
list.Open("Select Item", "Choose:", items, item => OnSelected(item));
list.ShowIfOpen();
```

### Theming and Styling

```csharp
using ktsu.ImGui.Styler;
using Hexa.NET.ImGui;

// Apply a built-in theme
Theme.Apply("Catppuccin Mocha");

// Show interactive theme browser
Theme.ShowThemeSelector("Select Theme");

// Scoped color styling (auto-restored after block)
using (new ScopedColor(ImGuiCol.Text, Color.FromHex("#ff6b6b")))
{
    ImGui.Text("This text is red!");
}

// Semantic text colors
using (Text.Color.Error())
{
    ImGui.Text("Error message");
}

using (Text.Color.Success())
{
    ImGui.Text("Success message");
}

// Center content
using (new Alignment.Center(ImGui.CalcTextSize("Centered!")))
{
    ImGui.Text("Centered!");
}

// Button text alignment
using (Button.Alignment.Center())
{
    ImGui.Button("Centered text", new Vector2(200, 30));
}

// Scoped theme for a section
using (new ScopedTheme(myTheme))
{
    ImGui.Text("This section uses a different theme");
}
```

### Node Graph (Attribute-Based Declaration)

```csharp
using ktsu.NodeGraph;

[Node("Math Add")]
[NodeBehavior(NodeExecutionMode.OnInputChange, IsDeterministic = true)]
public class AddNode
{
    [InputPin("A")]
    public float A { get; set; }

    [InputPin("B")]
    public float B { get; set; }

    [OutputPin("Result")]
    public float Result { get; set; }

    [NodeExecute]
    public void Execute()
    {
        Result = A + B;
    }
}
```

### Visual Node Editor

```csharp
using ktsu.ImGui.NodeEditor;

// Create engine and factory
NodeEditorEngine engine = new();
AttributeBasedNodeFactory factory = new(engine);

// Register node types
factory.RegisterNodeType<AddNode>();
factory.RegisterNodeTypesFromAssembly(typeof(AddNode).Assembly);

// Create nodes
Node nodeA = factory.CreateNode<AddNode>(new Vector2(100, 100));

// Render in ImGui loop
NodeEditorRenderer renderer = new();
NodeEditorInputHandler inputHandler = new();
renderer.Render(engine, editorSize);
```

### Markdown Rendering

```csharp
using ktsu.ImGui.Markdown;
using Hexa.NET.ImGui;

ImGui.Begin("Markdown");
ImGuiMarkdown.Render("""
    # Hello, ImGui

    A **CommonMark** renderer for *Dear ImGui*, with `inline code` and [links](https://github.com/ktsu-dev).
    """);
ImGui.End();
```

## API Reference

### Syntax Highlighting

```csharp
using ktsu.ImGui.SyntaxHighlighting;
using Hexa.NET.ImGui;

ImGui.Begin("Code");
ImGuiSyntaxHighlighting.Render("""
    /// <summary>Greets the world.</summary>
    public static void Main()
    {
        Console.WriteLine("Hello, ImGui");

        // The JSON below is highlighted as JSON, inside the string.
        Post(@"{""greeting"": ""hello"", ""count"": 1}");
    }
    """, "csharp", new SyntaxHighlightConfig { ShowLineNumbers = true });
ImGui.End();
```

Tokenizing is available on its own, with no ImGui in sight, from `ktsu.SyntaxHighlighting`:

```csharp
using ktsu.SyntaxHighlighting;

foreach (HighlightedLine line in SyntaxHighlighter.Highlight(source, "python"))
{
    foreach (HighlightedToken token in line.Tokens)
    {
        Console.WriteLine($"{token.Kind}: {token.Text}");
    }
}
```

### `ImGuiApp` (Static)

Application lifecycle and utilities.

#### Methods

| Name | Return Type | Description |
| ---- | ----------- | ----------- |
| `Start(ImGuiAppConfig)` | `void` | Initialize and run the application |
| `Stop()` | `void` | Close the application window |
| `Show()` / `Hide()` | `void` | Show or hide the window without stopping the render loop |
| `EnableOverlay(float, bool)` | `void` | Enter overlay mode: borderless, always-on-top, translucent, optional click-through |
| `SetOverlayGeometry(OverlayCorner, int, int, int, int)` | `void` | Lock the overlay to a work-area corner at an offset and size |
| `DisableOverlay()` | `void` | Restore the decorated, non-topmost, opaque window |
| `SetGlobalScale(float)` | `void` | Set accessibility UI scale (0.5-3.0) |
| `SetWindowIcon(string)` | `void` | Set window icon from image file |
| `GetOrLoadTexture(AbsoluteFilePath)` | `ImGuiAppTextureInfo` | Load or retrieve cached GPU texture |
| `DeleteTexture(uint)` | `void` | Remove texture from GPU |
| `EmsToPx(float)` | `int` | Convert EMs to pixels |
| `PtsToPx(int)` | `int` | Convert points to pixels |

#### Properties

| Name | Type | Description |
| ---- | ---- | ----------- |
| `IsFocused` | `bool` | Whether the window has focus |
| `IsVisible` | `bool` | Whether the window is visible |
| `IsIdle` | `bool` | Whether the app is idle |
| `IsOverlayActive` | `bool` | Whether the window is in overlay mode |
| `ScaleFactor` | `float` | DPI-based scale factor |
| `GlobalScale` | `float` | User-adjustable UI scale |
| `Invoker` | `Invoker` | Delegate invocation for window thread |

### `ImGuiAppConfig`

Configuration for `ImGuiApp.Start()`.

#### Configuration Properties

| Name | Type | Description |
| ---- | ---- | ----------- |
| `Title` | `string` | Window title (default: "ImGuiApp") |
| `IconPath` | `string` | Path to window icon |
| `OnStart` | `Action` | Initialization callback |
| `OnUpdate` | `Action<float>` | Per-frame update callback |
| `OnRender` | `Action<float>` | Per-frame render callback |
| `OnAppMenu` | `Action` | Menu bar rendering callback |
| `OnMoveOrResize` | `Action` | Window moved/resized callback |
| `OnGlobalScaleChanged` | `Action<float>` | Scale changed callback |
| `Fonts` | `Dictionary<string, byte[]>` | Custom fonts to load |
| `EnableUnicodeSupport` | `bool` | Include extended Unicode ranges (default: true) |
| `PerformanceSettings` | `ImGuiAppPerformanceSettings` | Throttling configuration |
| `FontMemoryConfig` | `FontMemoryGuard.FontMemoryConfig` | Font memory limits |
| `InitialWindowState` | `ImGuiAppWindowState` | Initial window size/position |

### `ImGuiAppPerformanceSettings`

Frame rate throttling configuration.

| Name | Type | Description |
| ---- | ---- | ----------- |
| `EnableThrottledRendering` | `bool` | Enable adaptive frame limiting (default: true) |
| `FocusedFps` | `double` | Target FPS when focused (default: 30) |
| `UnfocusedFps` | `double` | Target FPS when unfocused (default: 5) |
| `IdleFps` | `double` | Target FPS when idle (default: 10) |
| `NotVisibleFps` | `double` | Target FPS when minimized (default: 2) |
| `OverlayFps` | `double` | Target FPS in overlay mode, bypassing focus/idle/visibility throttling (default: 30) |
| `IdleTimeoutSeconds` | `double` | Seconds before idle state (default: 30) |

### `ImGuiWidgets` (Static)

Custom UI components.

| Name | Return Type | Description |
| ---- | ----------- | ----------- |
| `SearchBox(...)` | `bool` | Search box with Glob/Regex/Fuzzy filtering |
| `SearchBox<T>(...)` | `IEnumerable<T>` | Filtered item selection |
| `SearchBoxRanked<T>(...)` | `IEnumerable<T>` | Fuzzy-ranked item selection |
| `Knob(...)` | `bool` | Rotary knob control (float or int) |
| `RadialProgressBar(...)` | `void` | Radial progress indicator |
| `RadialCountdown(...)` | `void` | Countdown timer display |
| `RadialCountUp(...)` | `void` | Count-up timer display |
| `Combo<TEnum>(...)` | `bool` | Enum selection combo |
| `Combo<TString>(...)` | `bool` | String selection combo |
| `Icon(...)` | `bool` | Icon with label and events |
| `Image(...)` | `bool` | Clickable image display |
| `ColorIndicator(...)` | `void` | Colored square indicator |
| `RowMajorGrid<T>(...)` | `void` | Row-major grid layout |
| `ColumnMajorGrid<T>(...)` | `void` | Column-major grid layout |

### Widget Instance Classes

| Class | Description |
| ----- | ----------- |
| `TabPanel` | Tabbed interface with closable, reorderable tabs |
| `DividerContainer` | Resizable split pane layout (columns or rows) |
| `ScopedDisable` | RAII wrapper to disable UI elements |
| `ScopedId` | RAII wrapper to push ImGui IDs |
| `Tree` | Tree view with nested children |

### `ImGuiPopups` Classes

| Class | Description |
| ----- | ----------- |
| `Modal` | Generic modal dialog |
| `MessageOK` | Simple message with OK button |
| `Prompt` | Multi-button prompt dialog |
| `InputString` | String input with confirmation |
| `InputInt` | Integer input with confirmation |
| `InputFloat` | Float input with confirmation |
| `FilesystemBrowser` | File/directory browser with glob filtering |
| `SearchableList<T>` | Searchable item selection list |

### `Theme` (Static)

Theme management from ImGui.Styler.

| Name | Return Type | Description |
| ---- | ----------- | ----------- |
| `Apply(string)` | `bool` | Apply a named theme |
| `Apply(ISemanticTheme)` | `void` | Apply a theme instance |
| `ResetToDefault()` | `void` | Reset to default ImGui theme |
| `ShowThemeSelector(...)` | `void` | Show interactive theme browser |
| `RenderMenu(...)` | `bool` | Render theme selection menu |
| `FindTheme(string)` | `ThemeInfo?` | Look up theme by name |
| `AllThemes` | `IReadOnlyList<ThemeInfo>` | All available themes |
| `DarkThemes` | `IReadOnlyList<ThemeInfo>` | Dark themes only |
| `LightThemes` | `IReadOnlyList<ThemeInfo>` | Light themes only |
| `Families` | `IReadOnlyList<string>` | Theme family names |

### Styling Utilities

| Class | Description |
| ----- | ----------- |
| `ScopedColor` | RAII scoped ImGui color override |
| `ScopedTextColor` | RAII scoped text color |
| `ScopedStyleVar` | RAII scoped style variable |
| `ScopedThemeColor` | RAII scoped theme-aware color |
| `ScopedTheme` | RAII scoped full theme |
| `FontAppearance` | RAII scoped font styling |
| `UIScaler` | RAII scoped UI scaling |
| `Alignment.Center` | RAII scoped content centering |
| `Button.Alignment` | RAII scoped button text alignment |
| `Text.Color` | Semantic text colors (Error, Warning, Info, Success) |
| `Indent` | Scoped indentation utilities |
| `Color` | Color creation (Hex, RGB, HSL) with palettes |

### Color Extensions (on `ImColor`)

| Name | Description |
| ---- | ----------- |
| `DesaturateBy(float)` | Reduce saturation |
| `SaturateBy(float)` | Increase saturation |
| `LightenBy(float)` | Increase luminance |
| `DarkenBy(float)` | Decrease luminance |
| `WithAlpha(float)` | Set alpha channel |
| `ToGrayscale()` | Convert to grayscale |
| `MostReadableTextColor()` | Get best contrast text color |
| `GetContrastRatioOver(ImColor)` | WCAG contrast ratio |

### `NodeEditorEngine`

Core node graph business logic.

| Name | Return Type | Description |
| ---- | ----------- | ----------- |
| `CreateNode(...)` | `Node` | Create a node with pins |
| `RemoveNode(int)` | `bool` | Remove a node |
| `TryCreateLink(int, int)` | `LinkCreationResult` | Create a link between pins |
| `RemoveLink(int)` | `bool` | Remove a link |
| `UpdatePhysics(float)` | `void` | Run physics simulation step |
| `Clear()` | `void` | Remove all nodes and links |
| `Nodes` | `IReadOnlyList<Node>` | All nodes |
| `Links` | `IReadOnlyList<Link>` | All links |
| `IsStable` | `bool` | Whether physics is stable |

### Node Graph Attributes

| Attribute | Target | Description |
| --------- | ------ | ----------- |
| `[Node]` | Class/Struct/Method | Marks type as a node |
| `[NodeBehavior]` | Class/Struct | Specifies execution mode |
| `[NodeExecute]` | Method | Marks execution method |
| `[InputPin]` | Property/Field/Parameter | Declares an input pin |
| `[OutputPin]` | Property/Field/Method | Declares an output pin |
| `[ExecutionInput]` | Property/Field | Declares execution input flow |
| `[ExecutionOutput]` | Property/Field | Declares execution output flow |
| `[NodeDeprecated]` | Class/Struct | Marks node as deprecated |
| `[NodeVisibility]` | Class/Struct | Controls menu visibility |
| `[WildcardPin]` | Property/Field | Accepts wildcard connections |

### `ImGuiMarkdown` (Static)

CommonMark markdown rendering.

| Name | Return Type | Description |
| ---- | ----------- | ----------- |
| `Render(string, MarkdownConfig?)` | `void` | Parses (cached by source) and renders markdown at the current cursor position |
| `Render(MarkdownDocument, MarkdownConfig?)` | `void` | Renders a pre-parsed document at the current cursor position |

| Class | Description |
| ----- | ----------- |
| `MarkdownDocument` | Parses markdown once in its constructor; render the same instance every frame for hot paths |
| `MarkdownConfig` | Rendering options: `FontResolver`, `OnLinkClicked`, `ImageResolver`, `HeadingScales`, `WrapWidth`, `ListIndentPixels`, `ParagraphSpacingPixels`, `LinkColor` |

### `ImGuiSyntaxHighlighting` (Static)

Syntax-highlighted code rendering.

| Name | Return Type | Description |
| ---- | ----------- | ----------- |
| `Render(string, string, SyntaxHighlightConfig?)` | `void` | Tokenizes (cached by source) and renders code at the current cursor position |
| `Render(HighlightedCode, SyntaxHighlightConfig?)` | `void` | Renders pre-tokenized code at the current cursor position |
| `Highlight(string, string, int)` | `IReadOnlyList<HighlightedLine>` | Tokenizes code without drawing anything; forwards to `SyntaxHighlighter` |

| Class | Description |
| ----- | ----------- |
| `SyntaxHighlightConfig` | Rendering options: `FontResolver`, `Theme`, `ShowLineNumbers`, `FirstLineNumber`, `TabWidth`, `ShowBackground`, `BackgroundRounding`, `PaddingPixels`, `LineSpacingPixels`, `GutterSpacingPixels`, `FontSizePixels`, `Width` |

### `SyntaxHighlighter` (Static, UI-agnostic)

Tokenizing, from `ktsu.SyntaxHighlighting`. Nothing in this package draws.

| Name | Return Type | Description |
| ---- | ----------- | ----------- |
| `Highlight(string, string, int)` | `IReadOnlyList<HighlightedLine>` | Tokenizes code, without consulting the cache |
| `HighlightCached(string, string, int)` | `IReadOnlyList<HighlightedLine>` | Tokenizes through a bounded cache keyed by source, language and tab width |

| Class | Description |
| ----- | ----------- |
| `HighlightedCode` | Tokenizes once in its constructor; render the same instance every frame for hot paths |
| `SyntaxTheme` | One color per `TokenKind`, with `Dark` and `Light` built in and unset entries taken from the host UI's theme |
| `LanguageDefinition` | The comment, string, keyword, operator and embedded-language rules of one language |
| `LanguageRegistry` | Resolves language names and aliases, and registers custom definitions |
| `EmbeddedLanguageRule` | Says which language to look for inside a comment or string, and how to recognize it |
| `BuiltInEmbeddedRules` | The rules the built-in languages use: `Json`, `Markup`, `Sql`, `XmlDocComments` |
| `EmbeddedContent` | The recognizers behind those rules: `LooksLikeJson`, `LooksLikeMarkup`, `LooksLikeSql` |

## Demo Applications

The repository includes demo applications showcasing all features:

```bash
# Run the main demo
dotnet run --project examples/ImGuiAppDemo

# Run individual library demos
dotnet run --project examples/ImGuiWidgetsDemo
dotnet run --project examples/ImGuiPopupsDemo
dotnet run --project examples/ImGuiStylerDemo
dotnet run --project examples/ImGuiMarkdownDemo
dotnet run --project examples/ImGuiSyntaxHighlightingDemo
```

## Acknowledgments

The suite renders through the [Hexa.NET](https://github.com/HexaEngine/Hexa.NET.ImGui) bindings:

- [Hexa.NET.ImGui](https://github.com/HexaEngine/Hexa.NET.ImGui) - .NET bindings for Dear ImGui, and for the `Hexa.NET.ImGuizmo`, `Hexa.NET.ImNodes` and `Hexa.NET.ImPlot` extensions that `ktsu.ImGui.App` auto-detects
- [Hexa.NET.ImGui.Widgets](https://github.com/HexaEngine/Hexa.NET.ImGui.Widgets) - The upstream widget collection, with `Hexa.NET.ImGui.Widgets.Extras`, behind the Hexa-backed widgets, dialogs and editors in `ktsu.ImGui.Widgets`
- [Hexa.NET.Math](https://github.com/HexaEngine/Hexa.NET.Math) - The math types those widgets marshal through
- [HexaGen](https://github.com/JunaMeinhold/HexaGen) - The binding generator and the `HexaGen.Runtime` the wrappers are built on

and on the libraries they wrap or sit beside:

- [Dear ImGui](https://github.com/ocornut/imgui) - The immediate mode GUI library everything here draws into
- [Silk.NET](https://github.com/dotnet/Silk.NET) - Cross-platform windowing and OpenGL
- [Markdig](https://github.com/xoofx/markdig) - The CommonMark parser behind `ktsu.ImGui.Markdown`
- [Polyfill](https://github.com/SimonCropp/Polyfill) - Backports newer .NET APIs to the older target frameworks, at build time only

and on the ktsu libraries the suite shares with the rest of the ecosystem:

- [ktsu.Semantics](https://github.com/ktsu-dev/Semantics) - The `Color`, path, string and quantity types
- [ktsu.ThemeProvider](https://github.com/ktsu-dev/ThemeProvider) - The semantic theming foundation behind `ktsu.ImGui.Styler`
- [ktsu.TextFilter](https://github.com/ktsu-dev/TextFilter) - Glob, regex and fuzzy filtering
- [ktsu.Extensions](https://github.com/ktsu-dev/Extensions), [ktsu.CaseConverter](https://github.com/ktsu-dev/CaseConverter), [ktsu.ScopedAction](https://github.com/ktsu-dev/ScopedAction) and [ktsu.Invoker](https://github.com/ktsu-dev/Invoker) - Collection, string, RAII and invocation helpers

## Contributing

Contributions are welcome! For feature requests, bug reports, or questions, please open an issue on the GitHub repository. If you would like to contribute code, please open a pull request with your changes.

## License

This project is licensed under the MIT License. See [LICENSE.md](LICENSE.md) for more information.
