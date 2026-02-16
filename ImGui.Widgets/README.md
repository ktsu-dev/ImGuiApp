# ktsu.ImGuiWidgets

ImGuiWidgets is a library of custom widgets using ImGui.NET. This library provides a variety of widgets and utilities to enhance your ImGui-based applications.

## Features

- **Knobs**: Ported to .NET from [ImGui-works/ImGui-knobs-dial-gauge-meter](https://github.com/imgui-works/imgui-knobs-dial-gauge-meter)
- **Radial Progress Bar**: Circular progress indicators for visualizing loading and progress with countdown/count-up timers
- **Resizable Layout Dividers**: Draggable layout dividers for resizable layouts (DividerContainer)
- **TabPanel**: Tabbed interface with closable, reorderable tabs and dirty indicator support
- **Combo**: Type-safe combo boxes for enums, strings, and strong strings
- **Icons**: Customizable icons with various alignment options and event delegates
- **Grid**: Flexible grid layout for displaying items
- **Color Indicator**: An indicator that displays a color when enabled
- **Image**: An image widget with alignment options
- **Text**: A text widget with alignment options
- **Tree**: A tree widget for displaying hierarchical data
- **Scoped Id**: A utility class for creating scoped IDs
- **Scoped Disable**: Temporarily disable UI elements within a scope
- **SearchBox**: A powerful search box with support for various filter types (Glob, Regex, Fuzzy) and matching options

## Installation

To install ImGuiWidgets, you can add the library to your .NET project using the following command:

```bash
dotnet add package ktsu.ImGuiWidgets
```

## Usage

To use ImGuiWidgets, you need to include the `ktsu.ImGuiWidgets` namespace in your code:

```csharp
using ktsu.ImGuiWidgets;
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
// Static fields to maintain filter state between renders
private static string searchTerm = string.Empty;
private static TextFilterType filterType = TextFilterType.Glob;
private static TextFilterMatchOptions matchOptions = TextFilterMatchOptions.ByWholeString;

// List of items to search
var items = new List<string> { "Apple", "Banana", "Cherry", "Date", "Elderberry" };

// Basic search box with right-click context menu for filter options
ImGuiWidgets.SearchBox("##BasicSearch", ref searchTerm, ref filterType, ref matchOptions);

// Display results
if (!string.IsNullOrEmpty(searchTerm))
{
    ImGui.TextUnformatted($"Search results for: {searchTerm}");
}

// Search box that returns filtered results directly
var filteredResults = ImGuiWidgets.SearchBox(
    "##FilteredSearch",
    ref searchTerm,
    items,                  // Collection to filter
    item => item,           // Selector function to extract string from each item
    ref filterType,
    ref matchOptions).ToList();

// Ranked search box for fuzzy matching and ranked results
var rankedResults = ImGuiWidgets.SearchBoxRanked(
    "##RankedSearch",
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

uint textureId = ImGuiApp.GetOrLoadTexture("icon.png");

ImGuiWidgets.Icon("Click Me", textureId, iconWidthPx, Color.White.Value, ImGuiWidgets.IconAlignment.Vertical, new ImGuiWidgets.IconDelegates()
{
    OnClick = () => MessageOK.Open("Click", "You clicked")
});

ImGui.SameLine();
ImGuiWidgets.Icon("Double Click Me", textureId, iconWidthPx, Color.White.Value, ImGuiWidgets.IconAlignment.Vertical, new ImGuiWidgets.IconDelegates()
{
    OnDoubleClick = () => MessageOK.Open("Double Click", "You clicked twice")
});

ImGui.SameLine();
ImGuiWidgets.Icon("Right Click Me", textureId, iconWidthPx, Color.White.Value, ImGuiWidgets.IconAlignment.Vertical, new ImGuiWidgets.IconDelegates()
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

uint textureId = ImGuiApp.GetOrLoadTexture("icon.png");

ImGuiWidgets.Grid(items, i => ImGuiWidgets.CalcIconSize(i, iconSizePx), (item, cellSize, itemSize) =>
{
    ImGuiWidgets.Icon(item, textureId, iconSizePx, Color.White.Value);
});
```

### Color Indicator

The color indicator widget displays a color when enabled:

```csharp
bool enabled = true;
Color color = Color.Red;

ImGuiWidgets.ColorIndicator("Color Indicator", enabled, color);
```

### Image

The image widget allows you to display images with alignment options:

```csharp
uint textureId = ImGuiApp.GetOrLoadTexture("image.png");

ImGuiWidgets.Image(textureId, new Vector2(100, 100));
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

// Strong string combo box (using ktsu.StrongStrings)
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
// Create a horizontal divider container
var dividerContainer = new ImGuiWidgets.DividerContainer(
    "MyContainer",
    ImGuiWidgets.DividerLayout.Horizontal
);

// Add content regions
dividerContainer.AddZone("Left Panel", () =>
{
    ImGui.Text("Left side content");
    ImGui.Button("Left Button");
});

dividerContainer.AddZone("Right Panel", () =>
{
    ImGui.Text("Right side content");
    ImGui.Button("Right Button");
});

// Draw the container in your render loop
dividerContainer.Draw();

// For vertical layout:
var verticalContainer = new ImGuiWidgets.DividerContainer(
    "VerticalContainer",
    ImGuiWidgets.DividerLayout.Vertical
);

verticalContainer.AddZone("Top Panel", () =>
{
    ImGui.Text("Top content");
});

verticalContainer.AddZone("Bottom Panel", () =>
{
    ImGui.Text("Bottom content");
});

verticalContainer.Draw();
```

The dividers can be dragged by the user to resize the content regions dynamically.

## Contributing

Contributions are welcome! For feature requests, bug reports, or questions, please open an issue on the GitHub repository. If you would like to contribute code, please open a pull request with your changes.

## Acknowledgements

ImGuiWidgets is inspired by the following projects:

- [ocornut/ImGui](https://github.com/ocornut/imgui)
- [ImGui.NET](https://github.com/ImGuiNET/ImGui.NET)
- [ImGui-works/ImGui-knobs-dial-gauge-meter](https://github.com/imgui-works/imgui-knobs-dial-gauge-meter)

## License

ImGuiWidgets is licensed under the MIT License. See [LICENSE](LICENSE) for more information.
