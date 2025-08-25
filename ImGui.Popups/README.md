# ImGuiPopups

[![Version](https://img.shields.io/badge/version-1.3.5-blue.svg)](VERSION.md)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE.md)

A comprehensive library for custom popup windows and modal dialogs using ImGui.NET, providing a rich set of UI components for interactive applications.

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
  - Navigation breadcrumbs

### ✨ Key Features
- **Responsive Design**: All popups adapt to content and custom sizing
- **Keyboard Navigation**: Full keyboard support with proper focus management
- **Validation**: Built-in input validation and error handling
- **Customizable**: Flexible styling and layout options
- **Type-Safe**: Generic components with strong typing

## Installation

### Package Manager Console
```powershell
Install-Package ktsu.ImGuiPopups
```

### .NET CLI
```bash
dotnet add package ktsu.ImGuiPopups
```

### PackageReference
```xml
<PackageReference Include="ktsu.ImGuiPopups" Version="1.3.5" />
```

## Quick Start

```csharp
using ktsu.ImGuiPopups;

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

// Open file
browser.Open(
    title: "Open File",
    mode: FilesystemBrowserMode.Open,
    target: FilesystemBrowserTarget.File,
    startPath: Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
    onConfirm: path => OpenFile(path),
    patterns: new[] { "*.txt", "*.md" } // Optional file filters
);

// Save file
browser.Open(
    title: "Save File",
    mode: FilesystemBrowserMode.Save,
    target: FilesystemBrowserTarget.File,
    startPath: currentDirectory,
    onConfirm: path => SaveFile(path)
);
```

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
git clone https://github.com/ktsu-dev/ImGuiPopups.git
cd ImGuiPopups
dotnet run --project ImGuiPopupsDemo
```

## Dependencies

- [Hexa.NET.ImGui](https://www.nuget.org/packages/Hexa.NET.ImGui/) - ImGui.NET bindings
- [ktsu.Extensions](https://www.nuget.org/packages/ktsu.Extensions/) - Utility extensions
- [ktsu.CaseConverter](https://www.nuget.org/packages/ktsu.CaseConverter/) - String case conversion
- [ktsu.ScopedAction](https://www.nuget.org/packages/ktsu.ScopedAction/) - RAII-style actions
- [ktsu.StrongPaths](https://www.nuget.org/packages/ktsu.StrongPaths/) - Type-safe path handling
- [ktsu.TextFilter](https://www.nuget.org/packages/ktsu.TextFilter/) - Text filtering utilities
- [Microsoft.Extensions.FileSystemGlobbing](https://www.nuget.org/packages/Microsoft.Extensions.FileSystemGlobbing/) - File pattern matching

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for a detailed history of changes.

---

**ktsu.dev** - Building tools for developers
