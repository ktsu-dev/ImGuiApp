# ktsu.ImGui.Widgets

[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGui.Widgets?logo=nuget)](https://nuget.org/packages/ktsu.ImGui.Widgets)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](https://github.com/ktsu-dev/ImGuiApp/blob/main/LICENSE.md)

`ktsu.ImGui.Widgets` is a library of custom widgets for Dear ImGui, built on the Hexa.NET.ImGui bindings. It provides a variety of widgets and utilities to enhance your ImGui-based applications.

## Features

The widgets below are grouped by what they are for. Everything is a static method on `ImGuiWidgets` unless it is named as a type.

### Input and Controls

- **`Switch`**: iOS-style toggle with an animated thumb, whose track interpolates between the frame background and the accent color
- **`SegmentedControl`**: A row of mutually exclusive options with a sliding, animated highlight behind the selected one
- **`Stepper`**: A `[-] value [+]` integer stepper with hold-to-repeat after a short delay
- **`RangeSlider`**: Dual-handle slider for a span within a range; the handles cannot cross and stay a minimum distance apart
- **`XYPad`**: Edits two normalized parameters at once from one pad
- **`Knob`** / **`KnobWithDrag`**: Dial-style knobs in several variants, ported to .NET from [ImGui-works/ImGui-knobs-dial-gauge-meter](https://github.com/imgui-works/imgui-knobs-dial-gauge-meter)
- **`Rating`**: Interactive star rating that previews the value under the cursor before it is committed
- **`Chip`** / **`ChipGroup`**: Pill-shaped filter or choice tags, filled when selected, and a wrapping single-select group of them
- **`PinInput`**: An N-box PIN or one-time-passcode entry that auto-advances, and steps back on backspace
- **`SearchBox`** / **`SearchBoxRanked`**: Filters a collection with `ktsu.TextFilter` (glob, regex, fuzzy) or ranks it with a fuzzy match
- **`Combo`**: Type-safe combo boxes for enums, strings, and semantic strings

### Display and Status

- **`Avatar`**: Circular initials avatar on a color derived deterministically from the name, with an optional presence dot
- **`Badge`** / **`BadgeDot`**: A count badge (with a `maxCount+` cap) or a plain dot, overlaid on the corner of the item just submitted
- **`ColorIndicator`**: A colored square that shows a state
- **`Icon`**: Icons with alignment options and click, double-click and context-menu delegates
- **`Text`**: Text with alignment (`TextCentered`, `TextCenteredWithin`) and ellipsis clipping
- **`Image`**: Images with alignment (`ImageCentered`, `ImageCenteredWithin`), returning whether they were clicked
- **`PageIndicator`**: A row of carousel dots, optionally clickable to jump to a page
- **`Tooltip`** / **`Breadcrumb`**: A hover tooltip and a path-style breadcrumb trail

### Progress and Loading

- **`RadialProgressBar`**: Circular determinate progress, with `RadialCountdown` and `RadialCountUp` timer variants
- **`BufferingBar`** / **`Spinner`**: Determinate linear progress, and an indeterminate spinner
- **`SkeletonLine`** / **`SkeletonRect`** / **`SkeletonCircle`**: Shimmering placeholders for content that has not loaded, animated from `ImGui.GetTime()` so they need no per-widget state

### Data and Signals

- **`Histogram`**: One or more binned distributions as overlaid bars, scaled to the tallest bin; it takes pre-computed bins, so the binning scan stays off the render thread
- **`HandleTrack`**: Draggable handles over a rectangle you supply — a histogram plot, say — kept ordered and a minimum distance apart
- **`FlameGraph`**: A flame graph over managed sample data
- **`DbMeter`**: A vertical audio level meter in decibels, with an optional peak-hold marker
- **`Scope`**: An oscilloscope-style waveform over a block of audio samples

### Layout and Containers

- **`DividerContainer`** / **`DividerZone`**: A retained container divided into draggable zones, with persistable sizes; containers nest
- **`Grid`**: `RowMajorGrid` and `ColumnMajorGrid` layouts with measured, delegate-drawn cells
- **`TabPanel`**: Tabbed interface with closable, reorderable tabs and dirty indicators
- **`Card`**: A scoped elevated panel that draws its shadow and rounded background behind whatever the `using` block renders
- **`Tree`**: Connector lines drawn around whatever is nested inside it
- **`ImageCanvas`**: A pannable, zoomable image canvas with a checkerboard backing for transparency
- **`OverlayHost`** / **`OverlayLayer`**: A z-ordered registry for retained overlays — toasts, sheets, drawers — that must paint above the rest of the frame in a predictable order
- **`PropertyGrid`**: A two-column grid of labelled editors — name on the left, editor on the right — covering every scalar, vector, color, path and list type, with collapsible sections. See [Property Grid](#property-grid) below
- **`ScopedId`** / **`ScopedDisable`**: RAII scopes for the ID stack and for disabling a block of UI

### Motion and Gestures

- **`Tween`** / **`Spring`** / **`Easing`**: Frame-rate independent time-based interpolation, a damped harmonic oscillator that chases a target, and the easing curves to shape them
- **`InertialScroll`**: A one-dimensional scroll offset that coasts after release, for carousels, pickers and long lists
- **`GestureDetector`** / **`GestureMachine`**: Claims a region and reports tap, double-tap, long-press, swipe and pan over it

- **Hexa-backed widgets**: Thin adapters over [`Hexa.NET.ImGui.Widgets`](https://github.com/HexaEngine/Hexa.NET.ImGui.Widgets) — spinners, buffering bars, splitters, toggle/transparent/inline buttons, an icon tree node, an enum combo, text/image alignment helpers, tooltips, breadcrumbs, a date/year picker, a flame graph, a file tree view, stateful file/rename/message dialogs, and a docked-window base class. See [Hexa-backed Widgets](#hexa-backed-widgets) below.
- **Callback-driven editors**: `Sequencer` (an editable clip timeline), `CurveEditor` (a multi-curve graph, or a single `CurveData` curve), and `BezierEditor` (a cubic easing curve) — driven by a `SequenceSource`/`CurveSource` you subclass, or by a `CurveData`/`BezierControlPoints` value. See [Callback-driven Editors](#callback-driven-editors) below.

## Installation

To install ImGuiWidgets, you can add the library to your .NET project using the following command:

```bash
dotnet add package ktsu.ImGui.Widgets
```

## Usage

To use ImGuiWidgets, you need to include the `ktsu.ImGui.Widgets` namespace in your code:

```csharp
using ktsu.ImGui.Widgets;
```

Then, you can start using the widgets provided by ImGuiWidgets in your ImGui-based applications.

## Examples

Here are some examples of using ImGuiWidgets:

### Knobs

Knobs are useful for creating dial-like controls:

```csharp
float value = 0.5f;
float minValue = 0.0f;
float maxValue = 1.0f;

ImGuiWidgets.Knob("Knob", ref value, minValue, maxValue);
```

### Radial Progress Bar

The RadialProgressBar widget displays circular progress indicators perfect for loading states, progress tracking, countdowns, and timers:

```csharp
float progress = 0.65f; // Progress from 0.0 to 1.0

// Basic usage - displays with default size and settings (clockwise from top, percentage text)
ImGuiWidgets.RadialProgressBar(progress);

// Custom size (radius in pixels)
ImGuiWidgets.RadialProgressBar(progress, radius: 50);

// Custom thickness
ImGuiWidgets.RadialProgressBar(progress, radius: 50, thickness: 10);

// Without text in center
ImGuiWidgets.RadialProgressBar(progress, 50, 0, 32, ImGuiRadialProgressBarOptions.NoText);

// Counter-clockwise direction (default is clockwise)
ImGuiWidgets.RadialProgressBar(progress, 50, 0, 32, ImGuiRadialProgressBarOptions.CounterClockwise);

// Start at bottom instead of top
ImGuiWidgets.RadialProgressBar(progress, 50, 0, 32, ImGuiRadialProgressBarOptions.StartAtBottom);

// Combine options: counter-clockwise starting at bottom
ImGuiWidgets.RadialProgressBar(progress, 50, 0, 32,
    ImGuiRadialProgressBarOptions.CounterClockwise | ImGuiRadialProgressBarOptions.StartAtBottom);

// Animated progress example
float animatedProgress = 0.0f;
void UpdateProgress(float deltaTime)
{
    animatedProgress += deltaTime * 0.2f;
    if (animatedProgress > 1.0f) animatedProgress = 0.0f;

    ImGuiWidgets.RadialProgressBar(animatedProgress);
}
```

#### Text Display Modes

The RadialProgressBar supports three text display modes:

```csharp
// Percentage mode (default) - displays "65%"
ImGuiWidgets.RadialProgressBar(0.65f);

// Time mode - displays time in MM:SS or HH:MM:SS format
ImGuiWidgets.RadialProgressBar(
    progress: 0.5f,
    textMode: ImGuiRadialProgressBarTextMode.Time,
    timeValue: 150.0f  // Displays "02:30"
);

// Time mode with hours - displays "01:05:30"
ImGuiWidgets.RadialProgressBar(
    progress: 0.7f,
    textMode: ImGuiRadialProgressBarTextMode.Time,
    timeValue: 3930.0f  // 1 hour, 5 minutes, 30 seconds
);

// Custom text mode - displays any string you provide
ImGuiWidgets.RadialProgressBar(
    progress: 0.3f,
    textMode: ImGuiRadialProgressBarTextMode.Custom,
    customText: "Loading..."
);
```

#### Countdown Timer

Use `RadialCountdown` for countdown timers showing time remaining:

```csharp
float countdownTime = 300.0f;  // 5 minutes in seconds
const float CountdownTotal = 300.0f;
bool isRunning = false;

// Update countdown
if (isRunning && countdownTime > 0.0f)
{
    countdownTime -= deltaTime;
    if (countdownTime < 0.0f)
    {
        countdownTime = 0.0f;
        isRunning = false;
    }
}

// Display countdown - shows time remaining (e.g., "05:00", "04:30", etc.)
ImGuiWidgets.RadialCountdown(countdownTime, CountdownTotal);

// With custom options
ImGuiWidgets.RadialCountdown(
    countdownTime,
    CountdownTotal,
    radius: 60,
    thickness: 12,
    segments: 64,
    options: ImGuiRadialProgressBarOptions.CounterClockwise
);

// Reset button
if (ImGui.Button("Reset"))
{
    countdownTime = CountdownTotal;
    isRunning = false;
}
```

#### Count-Up Timer

Use `RadialCountUp` for timers showing elapsed time:

```csharp
float elapsedTime = 0.0f;
const float TotalTime = 180.0f;  // 3 minutes
bool isRunning = false;

// Update timer
if (isRunning && elapsedTime < TotalTime)
{
    elapsedTime += deltaTime;
    if (elapsedTime > TotalTime)
    {
        elapsedTime = TotalTime;
        isRunning = false;
    }
}

// Display count-up timer - shows elapsed time (e.g., "00:00", "00:15", etc.)
ImGuiWidgets.RadialCountUp(elapsedTime, TotalTime);

// With custom size and options
ImGuiWidgets.RadialCountUp(
    elapsedTime,
    TotalTime,
    radius: 70,
    options: ImGuiRadialProgressBarOptions.StartAtBottom
);

// Start/stop controls
if (ImGui.Button(isRunning ? "Stop" : "Start"))
{
    isRunning = !isRunning;
}

if (ImGui.Button("Reset"))
{
    elapsedTime = 0.0f;
    isRunning = false;
}
```

#### Advanced Timer Examples

```csharp
// Pomodoro timer (25 minutes work, 5 minutes break)
const float WorkDuration = 1500.0f;  // 25 minutes
const float BreakDuration = 300.0f;  // 5 minutes
float currentTime = WorkDuration;
bool isWorkSession = true;

if (isWorkSession)
{
    ImGuiWidgets.RadialCountdown(currentTime, WorkDuration, radius: 80);
}
else
{
    ImGuiWidgets.RadialCountdown(currentTime, BreakDuration, radius: 80);
}

// Stopwatch with custom display
float stopwatchTime = 0.0f;
ImGuiWidgets.RadialProgressBar(
    progress: 0.0f,  // No progress bar fill
    radius: 60,
    textMode: ImGuiRadialProgressBarTextMode.Time,
    timeValue: stopwatchTime,
    options: ImGuiRadialProgressBarOptions.NoText  // Hide text if desired
);
ImGui.Text($"Elapsed: {stopwatchTime:F2}s");

// Combined progress and time display
float taskProgress = 0.35f;
float taskTimeRemaining = 120.0f;  // 2 minutes remaining

ImGui.Columns(2);
ImGuiWidgets.RadialProgressBar(taskProgress);
ImGui.TextUnformatted("Progress");
ImGui.NextColumn();

ImGuiWidgets.RadialProgressBar(
    taskProgress,
    textMode: ImGuiRadialProgressBarTextMode.Time,
    timeValue: taskTimeRemaining
);
ImGui.TextUnformatted("Time Remaining");
ImGui.Columns(1);
```

### SearchBox

The SearchBox widget provides a powerful search interface with multiple filter type options:

```csharp
// Static fields to maintain filter state between renders.
// The options record carries the label, filter type, match options,
// and hint/tooltip/context-menu toggles. Right-click updates it in place.
private static string searchTerm = string.Empty;
private static SearchBoxOptions searchOptions = new(Label: "##BasicSearch", FilterType: TextFilterType.Glob);
private static SearchBoxRankedOptions rankedOptions = new(Label: "##RankedSearch");

// List of items to search
var items = new List<string> { "Apple", "Banana", "Cherry", "Date", "Elderberry" };

// Basic search box with right-click context menu for filter options
ImGuiWidgets.SearchBox(ref searchOptions, ref searchTerm);

// Display results
if (!string.IsNullOrEmpty(searchTerm))
{
    ImGui.TextUnformatted($"Search results for: {searchTerm}");
}

// Search box that returns filtered results directly
var filteredResults = ImGuiWidgets.SearchBox(
    ref searchOptions,
    ref searchTerm,
    items,                  // Collection to filter
    item => item).ToList();  // Selector function to extract string from each item

// Ranked search box for fuzzy matching and ranked results
var rankedResults = ImGuiWidgets.SearchBoxRanked(
    ref rankedOptions,
    ref searchTerm,
    items,
    item => item).ToList();
```

### TabPanel

TabPanel creates a tabbed interface with support for closable tabs, reordering, and dirty state indication:

```csharp
// Create a tab panel with closable and reorderable tabs
var tabPanel = new ImGuiWidgets.TabPanel("MyTabPanel", true, true);

// Add tabs with explicit IDs (recommended for stability when tabs are reordered)
string tab1Id = tabPanel.AddTab("tab1", "First Tab", RenderTab1Content);
string tab2Id = tabPanel.AddTab("tab2", "Second Tab", RenderTab2Content);
string tab3Id = tabPanel.AddTab("tab3", "Third Tab", RenderTab3Content);

// Draw the tab panel in your render loop
tabPanel.Draw();

// Methods to render tab content
void RenderTab1Content()
{
    ImGui.Text("Tab 1 Content");

    // Mark tab as dirty when content changes
    if (ImGui.Button("Edit"))
    {
        tabPanel.MarkTabDirty(tab1Id);
    }

    // Mark tab as clean when content is saved
    if (ImGui.Button("Save"))
    {
        tabPanel.MarkTabClean(tab1Id);
    }
}

void RenderTab2Content()
{
    ImGui.Text("Tab 2 Content");
}

void RenderTab3Content()
{
    ImGui.Text("Tab 3 Content");
}
```

### Icons

Icons can be used to display images with various alignment options and event delegates:

```csharp
float iconWidthEms = 7.5f;
float iconWidthPx = ImGuiApp.EmsToPx(iconWidthEms);

// GetOrLoadTexture returns an ImGuiAppTextureInfo; use its TextureId
ImGuiAppTextureInfo texture = ImGuiApp.GetOrLoadTexture("icon.png");

ImGuiWidgets.Icon("Click Me", texture.TextureId, iconWidthPx, ImGuiWidgets.IconAlignment.Vertical, new ImGuiWidgets.IconOptions()
{
    OnClick = () => Console.WriteLine("You clicked")
});

ImGui.SameLine();
ImGuiWidgets.Icon("Double Click Me", texture.TextureId, iconWidthPx, ImGuiWidgets.IconAlignment.Vertical, new ImGuiWidgets.IconOptions()
{
    OnDoubleClick = () => Console.WriteLine("You clicked twice")
});

ImGui.SameLine();
ImGuiWidgets.Icon("Right Click Me", texture.TextureId, iconWidthPx, ImGuiWidgets.IconAlignment.Vertical, new ImGuiWidgets.IconOptions()
{
    OnContextMenu = () =>
    {
        ImGui.MenuItem("Context Menu Item 1");
        ImGui.MenuItem("Context Menu Item 2");
        ImGui.MenuItem("Context Menu Item 3");
    },
});
```

### Grid

The grid layout allows you to display items in a flexible grid:

```csharp
float iconSizeEms = 7.5f;
float iconSizePx = ImGuiApp.EmsToPx(iconSizeEms);

ImGuiAppTextureInfo texture = ImGuiApp.GetOrLoadTexture("icon.png");

// RowMajorGrid (or ColumnMajorGrid) takes an id, the items, a measure delegate, and a draw delegate
ImGuiWidgets.RowMajorGrid(
    "MyGrid",
    items,
    item => ImGuiWidgets.CalcIconSize(item, iconSizePx, ImGuiWidgets.IconAlignment.Vertical),
    (item, cellSize, itemSize) =>
    {
        ImGuiWidgets.Icon(item, texture.TextureId, iconSizePx, ImGuiWidgets.IconAlignment.Vertical);
    });
```

### Color Indicator

The color indicator widget displays a color when enabled:

```csharp
// color is a Hexa.NET.ImGui.ImColor (for example, from ktsu.ImGui.Styler's Color helpers)
ImColor color = Color.FromHex("#ff0000");
bool enabled = true;

ImGuiWidgets.ColorIndicator(color, enabled);
```

### Image

The image widget allows you to display images with alignment options:

```csharp
ImGuiAppTextureInfo texture = ImGuiApp.GetOrLoadTexture("image.png");

ImGuiWidgets.Image(texture.TextureId, new Vector2(100, 100));
```

### Text

The text widget allows you to display text with alignment options:

```csharp
ImGuiWidgets.Text("Hello, ImGuiWidgets!");
ImGuiWidgets.TextCentered("Hello, ImGuiWidgets!");
ImGuiWidgets.TextCenteredWithin("Hello, ImGuiWidgets!", new Vector2(100, 100));
```

### Tree

The tree widget allows you to display hierarchical data:

```csharp
using (var tree = new ImGuiWidgets.Tree())
{
    for (int i = 0; i < 5; i++)
    {
        using (tree.Child)
        {
            ImGui.Button($"Hello, Child {i}!");
            using (var subtree = new ImGuiWidgets.Tree())
            {
                using (subtree.Child)
                {
                    ImGui.Button($"Hello, Grandchild!");
                }
            }
        }
    }
}
```

### Scoped Id

The scoped ID utility class helps in creating scoped IDs for ImGui elements and ensuring they get popped appropriately:

```csharp
using (new ImGuiWidgets.ScopedId())
{
    ImGui.Button("Hello, Scoped ID!");
}
```

### Scoped Disable

Temporarily disable UI elements within a scope. Disabled elements are visually grayed out and non-interactive:

```csharp
bool shouldDisable = true;

// Disable buttons within this scope
using (new ScopedDisable(shouldDisable))
{
    ImGui.Button("I'm disabled!");
    ImGui.InputText("Disabled Input", ref someText, 256);
}

// Elements outside the scope are enabled normally
ImGui.Button("I'm enabled!");

// Nested disables work as expected (per Dear ImGui rules)
using (new ScopedDisable(false))
{
    ImGui.Text("Enabled section");

    using (new ScopedDisable(true))
    {
        ImGui.Button("Disabled button");
    }
}
```

**Note**: As per Dear ImGui documentation, nested BeginDisabled calls cannot re-enable an already disabled section - a single `BeginDisabled(true)` in the stack is enough to keep everything disabled.

### Combo

Type-safe combo box widgets for enums, strings, and strong strings:

```csharp
// Enum combo box
enum Season { Spring, Summer, Fall, Winter }
Season selectedSeason = Season.Summer;

if (ImGuiWidgets.Combo("Season", ref selectedSeason))
{
    Console.WriteLine($"Selected: {selectedSeason}");
}

// String combo box
string selectedFruit = "Apple";
var fruits = new Collection<string> { "Apple", "Banana", "Cherry", "Date" };

if (ImGuiWidgets.Combo("Fruit", ref selectedFruit, fruits))
{
    Console.WriteLine($"Selected: {selectedFruit}");
}

// Strong string combo box (using ktsu.Semantics.Strings)
using ktsu.Semantics.Strings;

MyStrongString selected = new("Value1");
var options = new Collection<MyStrongString>
{
    new("Value1"),
    new("Value2"),
    new("Value3")
};

if (ImGuiWidgets.Combo("Option", ref selected, options))
{
    Console.WriteLine($"Selected: {selected}");
}
```

### DividerContainer

Create resizable layouts with draggable dividers between content regions:

```csharp
// Create a column-based divider container (side-by-side zones)
var dividerContainer = new ImGuiWidgets.DividerContainer(
    "MyContainer",
    ImGuiWidgets.DividerLayout.Columns
);

// Add zones with: id, initial size (relative weight), resizable, and a tick delegate (receives delta time)
dividerContainer.Add("Left Panel", 0.33f, true, dt =>
{
    ImGui.Text("Left side content");
    ImGui.Button("Left Button");
});

dividerContainer.Add("Right Panel", 0.67f, true, dt =>
{
    ImGui.Text("Right side content");
    ImGui.Button("Right Button");
});

// Tick the container each frame in your render loop
dividerContainer.Tick(deltaTime);

// For a stacked (top/bottom) layout, use DividerLayout.Rows:
var stackedContainer = new ImGuiWidgets.DividerContainer(
    "StackedContainer",
    ImGuiWidgets.DividerLayout.Rows
);

stackedContainer.Add("Top Panel", 0.5f, true, dt =>
{
    ImGui.Text("Top content");
});

stackedContainer.Add("Bottom Panel", 0.5f, true, dt =>
{
    ImGui.Text("Bottom content");
});

stackedContainer.Tick(deltaTime);
```

The dividers can be dragged by the user to resize the content regions dynamically.

### Property Grid

`ImGuiWidgets.PropertyGrid` lays out one labelled editor per property in a two-column, resizable
table. It is immediate mode like the rest of the library: it holds no model, and each row edits a
variable passed by reference.

```csharp
using (ImGuiWidgets.PropertyGrid grid = new("Settings"))
{
    using (grid.Section("Basics"))
    {
        grid.Value("Visible", ref visible);            // bool
        grid.Value("Quantity", ref quantity, 0, 100);  // int, clamped to a range
        grid.Value("Serial", ref serial);              // long
        grid.Value("Weight", ref weight);              // float
        grid.Value("Tolerance", ref tolerance);        // double
        grid.Value("Name", ref name);                  // string
        grid.Enum("Mode", ref mode);                   // any enum
    }

    using (grid.Section("Geometry"))
    {
        grid.Value("Offset", ref offset);   // System.Numerics.Vector2
        grid.Value("Scale", ref scale);     // System.Numerics.Vector3
        grid.Value("Origin", ref origin);   // ImGuiWidgets.DoubleVector2
        grid.Value("Extent", ref extent);   // ImGuiWidgets.DoubleVector3
    }

    grid.Value("Tint", ref tint);                        // ktsu.Semantics.Color.Color
    grid.FilePath("Config", ref configPath);
    grid.DirectoryPath("Output", ref outputFolder);
    grid.ImagePath("Icon", ref iconPath);                // with a thumbnail beneath it
    grid.List("Tags", tags);                             // a list of any of the above

    if (grid.Changed)
    {
        Save();
    }
}
```

One overloaded `Value` row covers every scalar, vector and color type; `Enum`, the three path rows
and `List` cover the rest. Every row returns whether it changed this frame, and `Changed`
accumulates those answers so the whole grid can be tested once.

`Section` returns a scope that closes the section when it is disposed, so there is nothing to
remember to call and nothing to test: a collapsed section holds its rows back itself, and the rows
inside it are written exactly as they are anywhere else. Sections nest, and a collapsed one holds
back everything inside it however deeply nested. `SectionScope.IsOpen` says whether the section is
expanded, which is worth reading only to skip work that costs something to prepare before a row can
be called.

A `List` row draws one editor per element, a `+` to append and an `x` on each element to remove it.
Any row method can be an element editor, so `List` has an overload per supported element type, plus
`FilePathList`, `DirectoryPathList` and `ImagePathList` for the ones whose element type is also a
string. A list of some other type takes an editor and a factory:

```csharp
grid.List("Points", points, (string label, ref Vector2 value) => grid.Value(label, ref value), () => Vector2.Zero);
```

#### Paths and thumbnails

This library does not open file dialogs or upload textures, so the path rows delegate both:

```csharp
ImGuiWidgets.PropertyGridOptions options = new()
{
    OnBrowse = request =>
    {
        // Hexa's dialogs are asynchronous, so the request is completed from the close callback.
        ImGuiWidgets.OpenFileDialog dialog = new();
        dialog.Show(outcome => request.Complete(outcome.Path?.ToString()));
    },
    ThumbnailResolver = path => ImGuiApp.GetOrLoadTexture(path.As<AbsoluteFilePath>()).TextureId,
};
```

`OnBrowse` receives the row's label, what it is asking for (`File`, `Directory` or `Image`) and the
path it currently holds. Browsing is asynchronous by nature — a dialog outlives the frame that
opened it — so the request carries no reference to the value: whenever `Complete` is called, the row
adopts the answer the next time it is drawn. Completing with `null` leaves the value alone, which is
what a cancelled dialog should do. Without an `OnBrowse` the browse button is disabled and paths can
still be typed; without a `ThumbnailResolver` an image row draws an empty preview frame, so its
height does not change once a picture appears.

The other options are `ReadOnly` (every row drawn disabled), `LabelColumnWeight` /
`LabelColumnWidth`, `ListsStartExpanded`, `ThumbnailSize`, and the printf-style `FloatFormat` /
`DoubleFormat`.

### Hexa-backed Widgets

These widgets are thin adapters that delegate to [`Hexa.NET.ImGui.Widgets`](https://github.com/HexaEngine/Hexa.NET.ImGui.Widgets) rather than reimplementing rendering logic:

- **`Spinner`**: Indeterminate loading spinner animated from the ImGui frame time
- **`BufferingBar`**: Horizontal bar filled left-to-right in proportion to a value
- **`HorizontalSplitter` / `VerticalSplitter`**: Draggable splitters that adjust a bound height/width within min/max limits
- **`ToggleSwitch`**: Sliding on/off switch
- **`ToggleButton`**: Button that shows a highlight ring while selected
- **`TransparentButton`**: Button with no background until hovered
- **`InlineButton`**: Compact button anchored inside an existing rectangle, for rows and headers
- **`IconTreeNode`**: Tree node with a colored icon glyph before its label
- **`EnumCombo<T>`**: Combo box listing every member of an enum type
- **`TextCenteredV` / `TextCenteredH` / `TextCenteredVH`**: Text centered vertically, horizontally, or both
- **`ImageCenteredV` / `ImageCenteredH` / `ImageCenteredVH`**: Image centered vertically, horizontally, or both
- **`ImageScaleTo`**: Image scaled to fit inside a destination box while preserving aspect ratio
- **`Tooltip`**: Shows a tooltip for the preceding item while it is hovered
- **`Breadcrumb`**: Clickable breadcrumb trail from a separator-delimited path
- **`DatePicker`**: Calendar control for picking a date
- **`YearPicker`**: Grid control for picking a year
- **`FlameGraph`**: Flame graph of hierarchical timing samples
- **`FileTreeView`**: Navigable tree of the filesystem rooted at the machine's drives
- **`OpenFileDialog`** / **`SaveFileDialog`** / **`OpenFolderDialog`**: Stateful dialogs for choosing existing files, a save destination, or a folder
- **`RenameDialog`**: Renames or moves a file, reporting success or failure without throwing
- **`DialogMessageBox`** / **`ShowMessageBox`**: A movable-window-style and a popup-style message box, respectively
- **`DockedWindow`**: Abstract base for a floating window the user can drag into the dockspace `DrawDeferredDocked()` creates — subclass it, override `Title` and `DrawContent()`, then call `Show()`/`Close()`. It is dockable, not auto-docked: it opens floating and stays there until the user drags it in

**Material Icons font**: `DatePicker` (Material `CalendarToday`, U+E935) and `FileTreeView` (`Home` U+E9B2, `Computer` U+E31E) render placeholder boxes unless a Material Icons font is registered in the atlas. `OpenFileDialog`, `SaveFileDialog` and `OpenFolderDialog` need the same font for their toolbar, breadcrumb and file-tree glyphs. Register it via `FontHelper.AddCustomFont(io, fontData, size, FontHelper.GetMaterialIconRanges(), mergeWithPrevious: true)` — not via `ImGuiAppConfig.Fonts`, which applies the Nerd Font mapping and leaves the glyphs unmapped. See `examples/ImGuiAppDemo` for a worked example. `YearPicker`, `RenameDialog`, `DialogMessageBox` and `ShowMessageBox` require no icon font.

**Overlapping widgets**: Seven Hexa-backed widgets look like duplicates of an existing ktsu widget. Five are not: `HorizontalSplitter`/`VerticalSplitter` is a single drag handle where `DividerContainer` is a retained layout container; `IconTreeNode` is a collapsible node where `Tree` only draws connector lines around whatever is nested inside it; `BufferingBar`/`Spinner` are determinate-linear and indeterminate where `RadialProgressBar`/`SkeletonLine` are determinate-radial and a shimmering placeholder; and the `TextCentered*`/`ImageCentered*` families each cover axes and overloads the other does not. Two do overlap: prefer **`Switch`** over `ToggleSwitch` (it marks itself for probes, animates from `ImGui.GetIO().DeltaTime` rather than Hexa's animation clock, and draws its own label), and prefer **`Combo`** over `EnumCombo` unless you need Hexa's display-name overrides. Nothing is obsoleted — that is a breaking change — but new code should reach for the preferred one, and the "Hexa vs ktsu" comparison tab in `examples/ImGuiWidgetsDemo` shows the pairs side by side. The through-line: the ktsu originals call `ImGuiProbes.MarkItem`, so a UI test can address them by name; the Hexa adapters have to be marked by the test itself.

**Deferred drawing**: The dialogs above and `DockedWindow` only draw when a per-frame pump runs. Call `ImGuiWidgets.DrawDeferred()` once per frame (at the end of `OnRender`) to draw every open dialog, message box and popup and advance Hexa's animation clock; call `ImGuiWidgets.DrawDeferredDocked()` instead if you use `DockedWindow` — it creates a dockspace over the main viewport and draws every registered docked window, and it already does everything `DrawDeferred()` does, so call only one of the two per frame (calling both draws every dialog twice). It *requires* `ImGuiConfigFlags.DockingEnable`, which `ImGuiAppConfig.EnableDocking = true` sets, and throws `InvalidOperationException` when the flag is off: ImGui only accepts that flag before the first frame, so the pump cannot turn it on itself, and Hexa's dockspace would silently do nothing without it. Showing a dialog before either pump has ever run throws `InvalidOperationException`, as does calling `Show()` on a dialog instance that is already shown (Hexa would register the same instance twice and permanently block input) — wait for the close callback, or create a new instance per showing. A pump is not needed just to keep animated widgets like `ToggleSwitch` correct — it self-ticks when unpumped — only to show dialogs or docked windows.

### Callback-driven Editors

`Sequencer` and the multi-curve `CurveEditor` overload take a source object they interrogate while drawing, instead of a value:

- Subclass **`SequenceSource`** for a timeline: `FrameMin`, `FrameMax`, `ItemCount`, `GetItem(int)`, and `SetItemRange(int index, int start, int endFrame)`, which receives drag edits.
- Subclass **`CurveSource`** for a multi-curve graph: `CurveCount`, `ViewMin`, `ViewMax`, `GetPointCount(int)`, `GetPoints(int)`, `GetCurveColor(int)`, `EditPoint(int, int, Vector2)`, `AddPoint(int, Vector2)`.

Neither needs a deferred-drawing pump — both are immediate-mode calls that happen to take a callback object, and neither `Sequencer` nor `CurveEditor` calls `DrawDeferred()`/`DrawDeferredDocked()`.

```csharp
public static bool ImGuiWidgets.Sequencer(SequenceSource source, ref int currentFrame, ref bool expanded,
    ref int selectedEntry, ref int firstFrame, SequencerFeatures features = SequencerFeatures.EditAll);

public static bool ImGuiWidgets.CurveEditor(CurveSource source, Vector2 size, string id);
```

`CurveEditor` also has a single-curve overload taking a **`CurveData`** value instead of a `CurveSource`:

```csharp
public static bool ImGuiWidgets.CurveEditor(CurveData curve, Vector2 size, Vector2 rangeMin,
    Vector2 rangeMax, ref int selection, string label);
```

`CurveData` wraps the curve representation the widget expects — points are `CurveKnot` (`Position` plus a `CurvePointKind` of `.Smooth` or `.Corner`), shaped by `CurveShape.Smooth`/`.Freehand` — and tracks a dirty flag so `Sample(float t)` recomputes its cache automatically after `AddPoint`/`SetPoint`/`RemovePoint`/`Clear`, a `Shape` change, or an edit made through the widget.

**`BezierEditor`** edits a `BezierControlPoints` pair (`First`/`Second`) directly, with no source object:

```csharp
public static bool ImGuiWidgets.BezierEditor(string label, ref BezierControlPoints points, float size = 128f);
```

## Acknowledgments

ImGuiWidgets is built on:

- [Dear ImGui](https://github.com/ocornut/imgui) - The immediate mode GUI library these widgets draw into
- [Hexa.NET.ImGui](https://github.com/HexaEngine/Hexa.NET.ImGui) - The .NET bindings for Dear ImGui
- [Hexa.NET.ImGui.Widgets](https://github.com/HexaEngine/Hexa.NET.ImGui.Widgets) - The upstream widget collection behind the Hexa-backed widgets, dialogs and editors here, with `Hexa.NET.ImGui.Widgets.Extras` supplying the curve and bezier editors
- [Hexa.NET.Math](https://github.com/HexaEngine/Hexa.NET.Math) - The math types those widgets marshal through
- [ktsu.Semantics](https://github.com/ktsu-dev/Semantics) - `Color`, path and string types used across the widget surface
- [ktsu.TextFilter](https://github.com/ktsu-dev/TextFilter) - Glob, regex and fuzzy filtering behind `SearchBox`
- [ktsu.Extensions](https://github.com/ktsu-dev/Extensions) - Collection extension methods
- [ktsu.ScopedAction](https://github.com/ktsu-dev/ScopedAction) - The RAII scope type behind `ScopedId`, `ScopedDisable` and `Tree`

and inspired by the following projects:

- [ImGui.NET](https://github.com/ImGuiNET/ImGui.NET)
- [ImGui-works/ImGui-knobs-dial-gauge-meter](https://github.com/imgui-works/imgui-knobs-dial-gauge-meter)

## Contributing

Contributions are welcome! For feature requests, bug reports, or questions, please open an issue on the GitHub repository. If you would like to contribute code, please open a pull request with your changes.

## License

ImGui.Widgets is licensed under the MIT License. See [LICENSE.md](https://github.com/ktsu-dev/ImGuiApp/blob/main/LICENSE.md) for more information.
