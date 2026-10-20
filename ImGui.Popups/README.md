# ktsu.ImGui.Popups

[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGui.Popups?logo=nuget)](https://nuget.org/packages/ktsu.ImGui.Popups)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](https://github.com/ktsu-dev/ImGuiApp/blob/main/LICENSE.md)

A comprehensive library for custom popup windows and modal dialogs for Dear ImGui, built on the Hexa.NET.ImGui bindings, providing a rich set of UI components for interactive applications.

## Features

### 🪟 Modal Windows
- **Modal**: Base modal window with customizable content and size
- **MessageOK**: Simple message dialog with OK button
- **Prompt**: Customizable prompt with multiple button options

### 📝 Input Components
- **InputString**: Text input popup with validation
- **InputInt**: Integer input popup with numeric validation
- **InputFloat**: Floating-point input popup with numeric validation

### 🔍 Selection Components
- **SearchableList**: Searchable dropdown list with filtering capabilities
- **FilesystemBrowser**: Advanced file/directory browser with:
  - Open and Save modes
  - File and Directory targeting
  - Pattern filtering support
  - Back, forward and up, and a breadcrumb path you can click to type a path
  - A sidebar of your own places, the user's folders, recent folders and drives
  - Search, hidden files, and a new folder button
  - A sortable table (name, size, modified) or a grid of thumbnails you supply

### ✨ Key Features
- **Responsive Design**: All popups adapt to content and custom sizing
- **Keyboard Navigation**: Full keyboard support with proper focus management
- **Validation**: Built-in input validation and error handling
- **Customizable**: Flexible styling and layout options
- **Type-Safe**: Generic components with strong typing

## Installation

### Package Manager Console
```powershell
Install-Package ktsu.ImGui.Popups
```

### .NET CLI
```bash
dotnet add package ktsu.ImGui.Popups
```

### PackageReference
```xml
<PackageReference Include="ktsu.ImGui.Popups" Version="x.y.z" />
```

## Quick Start

```csharp
using ktsu.ImGui.Popups;

// Create popup instances (typically as class members)
private static readonly ImGuiPopups.MessageOK messageOK = new();
private static readonly ImGuiPopups.InputString inputString = new();
private static readonly ImGuiPopups.SearchableList<string> searchableList = new();

// In your ImGui render loop
private void OnRender()
{
    // Show a simple message
    if (ImGui.Button("Show Message"))
    {
        messageOK.Open("Information", "Hello, World!");
    }
    
    // Get text input from user
    if (ImGui.Button("Get Input"))
    {
        inputString.Open("Enter Name", "Name:", "Default Name", 
            result => Console.WriteLine($"User entered: {result}"));
    }
    
    // Show searchable selection
    if (ImGui.Button("Select Item"))
    {
        var items = new[] { "Apple", "Banana", "Cherry", "Date" };
        searchableList.Open("Select Fruit", "Choose:", items, null, 
            item => item, // Text converter
            selected => Console.WriteLine($"Selected: {selected}"),
            Vector2.Zero);
    }
    
    // Render all popups (call this once per frame)
    messageOK.ShowIfOpen();
    inputString.ShowIfOpen();
    searchableList.ShowIfOpen();
}
```

## Component Documentation

### MessageOK
Simple message dialog with an OK button.

```csharp
var messageOK = new ImGuiPopups.MessageOK();
messageOK.Open("Title", "Your message here");
```

### Input Components
Get validated input from users:

```csharp
// String input
var inputString = new ImGuiPopups.InputString();
inputString.Open("Enter Text", "Label:", "default", result => HandleString(result));

// Integer input
var inputInt = new ImGuiPopups.InputInt();
inputInt.Open("Enter Number", "Value:", 42, result => HandleInt(result));

// Float input
var inputFloat = new ImGuiPopups.InputFloat();
inputFloat.Open("Enter Float", "Value:", 3.14f, result => HandleFloat(result));
```

### SearchableList
Searchable selection from a list of items:

```csharp
var searchableList = new ImGuiPopups.SearchableList<MyClass>();
searchableList.Open(
    title: "Select Item",
    label: "Choose an item:",
    items: myItemList,
    defaultItem: null,
    getText: item => item.DisplayName, // How to display items
    onConfirm: selected => HandleSelection(selected),
    customSize: new Vector2(400, 300)
);
```

### FilesystemBrowser
Advanced file and directory browser:

```csharp
var browser = new ImGuiPopups.FilesystemBrowser();

// Open a file (the optional glob filters which files are shown)
browser.FileOpen("Open File", path => OpenFile(path), glob: "*.txt");

// Save a file
browser.FileSave("Save File", path => SaveFile(path), glob: "*.txt");

// Choose a directory
browser.ChooseDirectory("Select Folder", dir => UseDirectory(dir));

// Render the browser each frame in your ImGui loop
browser.ShowIfOpen();
```

Set `ConfirmOverwrite = true` and a save that picks a file that already exists asks "Replace it?"
first. Replace chooses the file and closes the browser; Cancel, Escape or closing the question leave
the browser open where it was. It is off by default, so an application that confirms replacements
itself does not ask twice.

The browser has a toolbar (back, forward, up, refresh, a breadcrumb path and a search box), a
sidebar of places, recent folders and drives, and a footer with New Folder, Hidden files, the view
choice and the confirm buttons. Clicking the empty part of the path, or Ctrl+L, turns it into a text
box: a folder typed there is opened, and a file is chosen. Alt+Left, Alt+Right, Alt+Up (or
Backspace), F5 and Enter do what they do in a file manager.

Thumbnails come from you, since this package uploads no textures. Set `ThumbnailResolver` and the
grid view, and a preview of the selected file in the sidebar, draw what it returns:

```csharp
browser.ThumbnailResolver = file =>
    thumbnailCache.TryGet(file, out Texture texture)
        ? new ImGuiPopups.FilesystemBrowserThumbnail(texture.Id, new Vector2(texture.Width, texture.Height))
        : null; // still being made, or not a picture: the tile shows the extension
```

It is called for each file tile on screen, every frame, so look the picture up rather than decoding
the file there. Other settings:

| Member | What it does |
|---|---|
| `View` | `FilesystemBrowserView.List` (the default) or `Thumbnails`; the browser keeps whichever the user picked |
| `ThumbnailSize` | The thumbnail edge in pixels at 100% scale, 96 by default; the user changes it with a slider |
| `ShowHiddenFiles` | Lists hidden files and folders; off by default |
| `Places` | Your own folders, listed above the user's home, desktop, documents, pictures and downloads |
| `RecentFolders` | Folders something was chosen in, most recent first; fill it on start and save it on exit to keep it |
| `NavigateTo(folder)` | Shows a folder, now or when the browser next opens |
| `Directory` | The folder being shown |

A folder chooser with no folder selected chooses the folder being shown.

> File callbacks receive an `AbsoluteFilePath` and directory callbacks an `AbsoluteDirectoryPath`
> (from `ktsu.Semantics.Paths`). Each `FileOpen`/`FileSave`/`ChooseDirectory` overload also
> accepts a `Vector2 customSize`.

### Custom Modal
Create custom modal dialogs:

```csharp
var customModal = new ImGuiPopups.Modal();
customModal.Open("Custom Dialog", () => {
    ImGui.Text("Custom content here");
    if (ImGui.Button("Close"))
    {
        ImGui.CloseCurrentPopup();
    }
}, new Vector2(300, 200));
```

## Advanced Usage

### Custom Sizing
All popups support custom sizing:

```csharp
// Fixed size
popup.Open("Title", "Content", new Vector2(400, 300));

// Auto-size (Vector2.Zero)
popup.Open("Title", "Content", Vector2.Zero);
```

### Text Layout Options
Prompts support different text layout modes:

```csharp
var prompt = new ImGuiPopups.Prompt();
prompt.Open("Title", "Long message text here...", 
    buttons: new() { { "OK", null }, { "Cancel", null } },
    textLayoutType: PromptTextLayoutType.Wrapped, // or Unformatted
    size: new Vector2(400, 200)
);
```

### Buttons with their own probe names, a checkbox, and a dismiss answer
`PromptButton` gives each button its own probe name, and marks at most one as the cancel answer. A
modal can close without any button being clicked: by Escape, by its close button, or because ImGui
closed it when another popup opened at the same level. Whoever asked is still waiting then, so the
cancel button's action runs, once, as if it had been clicked. Every action runs after the popup has
ended, so an answer can open the next prompt. A `PromptCheckbox` draws a checkbox above the buttons,
read through `Checked` from inside an action.

```csharp
var prompt = new ImGuiPopups.Prompt();
prompt.Open("Unsaved Changes", "Save changes before closing?",
[
    new ImGuiPopups.PromptButton("Save", () => Save(rememberChoice: prompt.Checked), "unsaved/save"),
    new ImGuiPopups.PromptButton("Don't Save", Close, "unsaved/discard"),
    new ImGuiPopups.PromptButton("Cancel", KeepOpen, "unsaved/cancel", IsCancel: true),
],
new ImGuiPopups.PromptCheckbox("Don't ask again", "unsaved/remember"));
```

### Validation and Error Handling
Input components provide built-in validation:

```csharp
inputInt.Open("Enter Age", "Age (1-120):", 25, result => {
    if (result < 1 || result > 120)
    {
        messageOK.Open("Error", "Age must be between 1 and 120");
        return;
    }
    ProcessAge(result);
});
```

## Demo Application

The repository includes a comprehensive demo application showcasing all components:

```bash
git clone https://github.com/ktsu-dev/ImGuiApp.git
cd ImGuiApp
dotnet run --project examples/ImGuiPopupsDemo
```

## Dependencies

- [Hexa.NET.ImGui](https://www.nuget.org/packages/Hexa.NET.ImGui/) - .NET bindings for Dear ImGui
- [ktsu.Extensions](https://www.nuget.org/packages/ktsu.Extensions/) - Utility extensions
- [ktsu.CaseConverter](https://www.nuget.org/packages/ktsu.CaseConverter/) - String case conversion
- [ktsu.ScopedAction](https://www.nuget.org/packages/ktsu.ScopedAction/) - RAII-style actions
- [ktsu.Semantics.Paths](https://www.nuget.org/packages/ktsu.Semantics.Paths/) - Type-safe path handling
- [ktsu.Semantics.Strings](https://www.nuget.org/packages/ktsu.Semantics.Strings/) - Type-safe string wrappers
- [ktsu.TextFilter](https://www.nuget.org/packages/ktsu.TextFilter/) - Text filtering utilities
- [Microsoft.Extensions.FileSystemGlobbing](https://www.nuget.org/packages/Microsoft.Extensions.FileSystemGlobbing/) - File pattern matching
- [System.Text.Json](https://www.nuget.org/packages/System.Text.Json/) - JSON serialization attributes used by the filesystem browser
- [Polyfill](https://www.nuget.org/packages/Polyfill/) - Build-time only; backports newer .NET APIs to the older target frameworks

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for a detailed history of changes.

---

**ktsu.dev** - Building tools for developers
## Acknowledgments

- [Dear ImGui](https://github.com/ocornut/imgui) - The immediate mode GUI library these popups draw into
- [Hexa.NET.ImGui](https://github.com/HexaEngine/Hexa.NET.ImGui) - The .NET bindings for Dear ImGui that this package is built on
- [ktsu.TextFilter](https://github.com/ktsu-dev/TextFilter) - Glob, regex and fuzzy filtering behind the searchable list
- [ktsu.Semantics](https://github.com/ktsu-dev/Semantics) - Type-safe path and string types used by the filesystem browser
- [ktsu.Extensions](https://github.com/ktsu-dev/Extensions) and [ktsu.CaseConverter](https://github.com/ktsu-dev/CaseConverter) - Collection and string helpers
- [ktsu.ScopedAction](https://github.com/ktsu-dev/ScopedAction) - The RAII scope type behind the scoped helpers

## Contributing

Contributions are welcome! For feature requests, bug reports, or questions, please open an issue on the GitHub repository. If you would like to contribute code, please open a pull request with your changes.

## License

ImGui.Popups is licensed under the MIT License. See [LICENSE.md](https://github.com/ktsu-dev/ImGuiApp/blob/main/LICENSE.md) for more information.

