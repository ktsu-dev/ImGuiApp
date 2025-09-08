# ktsu.ImGuiWidgets

ImGuiWidgets is a library of custom widgets using ImGui.NET. This library provides a variety of widgets and utilities to enhance your ImGui-based applications.

## Features

- **Knobs**: Ported to .NET from [ImGui-works/ImGui-knobs-dial-gauge-meter](https://github.com/imgui-works/imgui-knobs-dial-gauge-meter)
- **Resizable Layout Dividers**: Draggable layout dividers for resizable layouts
- **TabPanel**: Tabbed interface with closable, reorderable tabs and dirty indicator support
- **Icons**: Customizable icons with various alignment options and event delegates
- **Grid**: Flexible grid layout for displaying items
- **Color Indicator**: An indicator that displays a color when enabled
- **Image**: An image widget with alignment options
- **Text**: A text widget with alignment options
- **Tree**: A tree widget for displaying hierarchical data
- **Scoped Id**: A utility class for creating scoped IDs
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

ImGuiWidgets.Knob("Knob", ref value, minValue);
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

The scoped ID utility class helps in creating scoped IDs for ImGui elements and ensuring they get popped appropriatley:

```csharp
using (new ImGuiWidgets.ScopedId())
{
    ImGui.Button("Hello, Scoped ID!");
}
```

## Contributing

Contributions are welcome! For feature requests, bug reports, or questions, please open an issue on the GitHub repository. If you would like to contribute code, please open a pull request with your changes.

## Acknowledgements

ImGuiWidgets is inspired by the following projects:

- [ocornut/ImGui](https://github.com/ocornut/imgui)
- [ImGui.NET](https://github.com/ImGuiNET/ImGui.NET)
- [ImGui-works/ImGui-knobs-dial-gauge-meter](https://github.com/imgui-works/imgui-knobs-dial-gauge-meter)

## License

ImGuiWidgets is licensed under the MIT License. See [LICENSE](LICENSE) for more information.
