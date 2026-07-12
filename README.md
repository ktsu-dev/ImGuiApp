# ktsu.ImGuiApp

> A comprehensive collection of .NET libraries for building modern, feature-rich desktop applications with Dear ImGui.

[![License](https://img.shields.io/github/license/ktsu-dev/ImGuiApp.svg?label=License&logo=nuget)](LICENSE.md)
[![NuGet Version](https://img.shields.io/nuget/v/ktsu.ImGui.App?label=Stable&logo=nuget)](https://nuget.org/packages/ktsu.ImGui.App)
[![NuGet Version](https://img.shields.io/nuget/vpre/ktsu.ImGui.App?label=Latest&logo=nuget)](https://nuget.org/packages/ktsu.ImGui.App)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ktsu.ImGui.App?label=Downloads&logo=nuget)](https://nuget.org/packages/ktsu.ImGui.App)
[![GitHub commit activity](https://img.shields.io/github/commit-activity/m/ktsu-dev/ImGuiApp?label=Commits&logo=github)](https://github.com/ktsu-dev/ImGuiApp/commits/main)
[![GitHub contributors](https://img.shields.io/github/contributors/ktsu-dev/ImGuiApp?label=Contributors&logo=github)](https://github.com/ktsu-dev/ImGuiApp/graphs/contributors)
[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/ktsu-dev/ImGuiApp/dotnet.yml?label=Build&logo=github)](https://github.com/ktsu-dev/ImGuiApp/actions)

## Introduction

`ktsu.ImGui.App` is a suite of .NET libraries that provides everything you need to build desktop applications with [Dear ImGui](https://github.com/ocornut/imgui). The suite includes application scaffolding, custom widgets, modal dialogs, a theming system, and a node graph editor framework. Built on Hexa.NET.ImGui bindings and Silk.NET for cross-platform windowing, it supports .NET 10, 9, and 8.

## Features

- **Application Foundation**: Complete application scaffolding with windowing, OpenGL rendering, font management, texture caching, and DPI awareness via `ktsu.ImGui.App`
- **PID Frame Limiting**: High-precision PID-controlled frame rate limiting with auto-tuning and adaptive throttling for focused, unfocused, idle, and minimized states
- **Custom Widgets**: Rich collection of UI components including TabPanel, Knob, SearchBox with fuzzy matching, RadialProgressBar with countdown/count-up timers, Grid layouts, DividerContainer with resizable sections, Combo, Tree, and Icons via `ktsu.ImGui.Widgets`
- **Modal Dialogs**: Professional popup system with MessageOK, Prompt, InputString/Int/Float, FilesystemBrowser, and SearchableList via `ktsu.ImGui.Popups`
- **Theming System**: 50+ built-in themes (Catppuccin, Tokyo Night, Gruvbox, Dracula, and more) with scoped styling, semantic text colors, button alignment, color palettes, and an interactive theme browser via `ktsu.ImGui.Styler`
- **Node Graph Framework**: Attribute-based node declaration system with UI-agnostic `ktsu.NodeGraph` metadata library and ImNodes-based visual editor `ktsu.ImGuiNodeEditor` with physics-based layout
- **Font Management**: Unicode, emoji, and Nerd Font support with GPU memory management via `FontMemoryGuard` and dynamic font scaling
- **Scoped Styling**: RAII-pattern disposable wrappers for colors, styles, fonts, themes, disable states, and UI scaling
- **Color Utilities**: HSL/HSLA color creation, accessibility-focused contrast calculations, color manipulation extensions, and semantic color palettes

## Libraries

### ImGui.App - Application Foundation

[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGui.App?label=ktsu.ImGui.App&logo=nuget)](https://nuget.org/packages/ktsu.ImGui.App)

Complete application scaffolding for Dear ImGui applications with windowing, rendering, font/texture management, and performance tuning.

### ImGui.Widgets - Custom UI Components

[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGui.Widgets?label=ktsu.ImGui.Widgets&logo=nuget)](https://nuget.org/packages/ktsu.ImGui.Widgets)

Rich collection of custom widgets: TabPanel, Knob, SearchBox, RadialProgressBar, Grid, DividerContainer, Combo, Tree, Icons, ColorIndicator, Text, Image, ScopedDisable, and ScopedId.

### ImGui.Popups - Modal Dialogs

[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGui.Popups?label=ktsu.ImGui.Popups&logo=nuget)](https://nuget.org/packages/ktsu.ImGui.Popups)

Professional modal dialogs: MessageOK, Prompt, InputString/Int/Float with validation, FilesystemBrowser with glob filtering, and SearchableList with type-safe generics.

### ImGui.Styler - Themes and Styling

[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGui.Styler?label=ktsu.ImGui.Styler&logo=nuget)](https://nuget.org/packages/ktsu.ImGui.Styler)

Advanced theming system with 50+ built-in themes, scoped styling, semantic text colors, button alignment, color palettes, and an interactive theme browser.

### NodeGraph - Node Metadata (UI-Agnostic)

[![NuGet](https://img.shields.io/nuget/v/ktsu.NodeGraph?label=ktsu.NodeGraph&logo=nuget)](https://nuget.org/packages/ktsu.NodeGraph)

Generic attribute-based system for declaring node graphs. Decorate classes, structs, and methods with node metadata (pins, execution modes, visibility, deprecation) without coupling to a specific editor implementation.

### ImGuiNodeEditor - Visual Node Editor

Attribute-driven visual node editor built on ImNodes. Includes `NodeEditorEngine` for business logic, `AttributeBasedNodeFactory` for node creation from decorated types, physics-based layout simulation, and `NodeEditorRenderer`/`NodeEditorInputHandler` for rendering and interaction.

## Installation

### Package Manager Console

```powershell
Install-Package ktsu.ImGui.App
Install-Package ktsu.ImGui.Widgets
Install-Package ktsu.ImGui.Popups
Install-Package ktsu.ImGui.Styler
Install-Package ktsu.NodeGraph
```

### .NET CLI

```bash
dotnet add package ktsu.ImGui.App
dotnet add package ktsu.ImGui.Widgets
dotnet add package ktsu.ImGui.Popups
dotnet add package ktsu.ImGui.Styler
dotnet add package ktsu.NodeGraph
```

### Package Reference

```xml
<PackageReference Include="ktsu.ImGui.App" Version="x.y.z" />
<PackageReference Include="ktsu.ImGui.Widgets" Version="x.y.z" />
<PackageReference Include="ktsu.ImGui.Popups" Version="x.y.z" />
<PackageReference Include="ktsu.ImGui.Styler" Version="x.y.z" />
<PackageReference Include="ktsu.NodeGraph" Version="x.y.z" />
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
using ktsu.ImGuiNodeEditor;

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

## API Reference

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
| `CalculateOptimalContrastingColor()` | Get best contrast text color |
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

## Demo Applications

The repository includes demo applications showcasing all features:

```bash
# Run the main demo
dotnet run --project examples/ImGuiAppDemo

# Run individual library demos
dotnet run --project examples/ImGuiWidgetsDemo
dotnet run --project examples/ImGuiPopupsDemo
dotnet run --project examples/ImGuiStylerDemo
```

## Contributing

Contributions are welcome! Feel free to open issues or submit pull requests.

## License

This project is licensed under the MIT License. See the [LICENSE.md](LICENSE.md) file for details.
