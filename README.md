# ktsu.ImGuiApp

> A .NET library that provides application scaffolding for Dear ImGui, using Silk.NET and Hexa.NET.ImGui.

[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGuiApp.svg)](https://www.nuget.org/packages/ktsu.ImGuiApp/)
[![License](https://img.shields.io/github/license/ktsu-dev/ImGuiApp.svg)](LICENSE.md)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ktsu.ImGuiApp.svg)](https://www.nuget.org/packages/ktsu.ImGuiApp/)
[![GitHub Stars](https://img.shields.io/github/stars/ktsu-dev/ImGuiApp?style=social)](https://github.com/ktsu-dev/ImGuiApp/stargazers)

## Introduction

ImGuiApp is a .NET library that provides application scaffolding for [Dear ImGui](https://github.com/ocornut/imgui), using [Silk.NET](https://github.com/dotnet/Silk.NET) for OpenGL and window management and [Hexa.NET.ImGui](https://github.com/HexaEngine/Hexa.NET.ImGui) for the ImGui bindings. It simplifies the creation of ImGui-based applications by abstracting away the complexities of window management, rendering, and input handling.

## Features

- **Simple API**: Create ImGui applications with minimal boilerplate code
- **Full Integration**: Seamless integration with Silk.NET for OpenGL and input handling
- **Window Management**: Automatic window state, rendering, and input handling
- **DPI Awareness**: Built-in support for high-DPI displays and scaling
- **Font Management**: Flexible font loading system with customization options
- **Texture Support**: Built-in texture management for ImGui
- **Lifecycle Callbacks**: Customizable delegate callbacks for application events
- **Menu System**: Easy-to-use API for creating application menus
- **Positioning Guards**: Offscreen positioning checks to keep windows visible
- **Modern .NET**: Supports .NET 8 and newer
- **Active Development**: Open-source and actively maintained

## Getting Started

### Prerequisites

- .NET 8.0 or later
- Windows OS (for DPI awareness features)

## Installation

### Package Manager Console

```powershell
Install-Package ktsu.ImGuiApp
```

### .NET CLI

```bash
dotnet add package ktsu.ImGuiApp
```

### Package Reference

```xml
<PackageReference Include="ktsu.ImGuiApp" Version="x.y.z" />
```

## Usage Examples

### Basic Application

Create a new class and call `ImGuiApp.Start()` with your application config:

```csharp
using ktsu.ImGuiApp;
using Hexa.NET.ImGui;

static class Program
{
    static void Main()
    {
        ImGuiApp.Start(new ImGuiAppConfig()
        {
            Title = "ImGuiApp Demo",
            OnStart = () => { /* Initialization code */ },
            OnUpdate = delta => { /* Logic updates */ },
            OnRender = delta => { ImGui.Text("Hello, ImGuiApp!"); },
            OnAppMenu = () =>
            {
                if (ImGui.BeginMenu("File"))
                {
                    // Menu items
                    if (ImGui.MenuItem("Exit"))
                    {
                        ImGuiApp.Stop();
                    }
                    ImGui.EndMenu();
                }
            }
        });
    }
}
```

### Custom Font Management

Use the resource designer to add font files to your project, then load the fonts:

```csharp
ImGuiApp.Start(new()
{
    Title = "ImGuiApp Demo",
    OnRender = OnRender,
    Fonts = new Dictionary<string, byte[]>
    {
        { nameof(Resources.MY_FONT), Resources.MY_FONT }
    },
});
```

Or load the font data manually:

```csharp
var fontData = File.ReadAllBytes("path/to/font.ttf");
ImGuiApp.Start(new()
{
    Title = "ImGuiApp Demo",
    OnRender = OnRender,
    Fonts = new Dictionary<string, byte[]>
    {
        { "MyFont", fontData }
    },
});
```

Then apply the font to ImGui using the `FontAppearance` class:

```csharp
private static void OnRender(float deltaTime)
{
    ImGui.Text("Hello, I am normal text!");

    using (new FontAppearance("MyFont", 24))
    {
        ImGui.Text("Hello, I am BIG fancy text!");
    }

    using (new FontAppearance(32))
    {
        ImGui.Text("Hello, I am just huge text!");
    }

    using (new FontAppearance("MyFont"))
    {
        ImGui.Text("Hello, I am somewhat fancy!");
    }
}
```

### Texture Management

Load and manage textures with the built-in texture management system:

```csharp
private static void OnRender(float deltaTime)
{
    // Load texture from file path
    var textureInfo = ImGuiApp.GetOrLoadTexture("path/to/texture.png");

    // Use the texture in ImGui (using the new TextureRef API for Hexa.NET.ImGui)
    ImGui.Image(textureInfo.TextureRef, new Vector2(128, 128));

    // Clean up when done (optional - textures are cached and managed automatically)
    ImGuiApp.DeleteTexture(textureInfo);
}
```

### Full Application with Multiple Windows

```csharp
using ktsu.ImGuiApp;
using Hexa.NET.ImGui;
using System.Numerics;

class Program
{
    private static bool _showDemoWindow = true;
    private static bool _showCustomWindow = true;

    static void Main()
    {
        ImGuiApp.Start(new ImGuiAppConfig
        {
            Title = "Advanced ImGuiApp Demo",
            Width = 1280,
            Height = 720,
            OnStart = OnStart,
            OnUpdate = OnUpdate,
            OnRender = OnRender,
            OnAppMenu = OnAppMenu,
            OnShutdown = OnShutdown
        });
    }

    private static void OnStart()
    {
        // Initialize your application state
        Console.WriteLine("Application started");
    }

    private static void OnUpdate(float deltaTime)
    {
        // Update your application state
        // This runs before rendering each frame
    }

    private static void OnRender(float deltaTime)
    {
        // ImGui demo window
        if (_showDemoWindow)
            ImGui.ShowDemoWindow(ref _showDemoWindow);

        // Custom window
        if (_showCustomWindow)
        {
            ImGui.Begin("Custom Window", ref _showCustomWindow);

            ImGui.Text($"Frame time: {deltaTime * 1000:F2} ms");
            ImGui.Text($"FPS: {1.0f / deltaTime:F1}");

            if (ImGui.Button("Click Me"))
                Console.WriteLine("Button clicked!");

            ImGui.ColorEdit3("Background Color", ref _backgroundColor);

            ImGui.End();
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

        if (ImGui.BeginMenu("Windows"))
        {
            ImGui.MenuItem("Demo Window", string.Empty, ref _showDemoWindow);
            ImGui.MenuItem("Custom Window", string.Empty, ref _showCustomWindow);
            ImGui.EndMenu();
        }
    }

    private static void OnShutdown()
    {
        // Clean up resources
        Console.WriteLine("Application shutting down");
    }

    private static Vector3 _backgroundColor = new Vector3(0.45f, 0.55f, 0.60f);
}
```

## API Reference

### `ImGuiApp` Static Class

The main entry point for creating and managing ImGui applications.

#### Methods

| Name | Parameters | Return Type | Description |
|------|------------|-------------|-------------|
| `Start` | `ImGuiAppConfig config` | `void` | Starts the ImGui application with the provided configuration |
| `Stop` | | `void` | Stops the running application |
| `GetOrLoadTexture` | `string path` | `ImGuiAppTextureInfo` | Loads a texture from file or returns cached texture info if already loaded |
| `TryGetTexture` | `string path, out ImGuiAppTextureInfo textureInfo` | `bool` | Attempts to get a cached texture by path |
| `DeleteTexture` | `uint textureId` | `void` | Deletes a texture and frees its resources |
| `DeleteTexture` | `ImGuiAppTextureInfo textureInfo` | `void` | Deletes a texture and frees its resources (convenience overload) |
| `GetWindowSize` | | `Vector2` | Returns the current window size |
| `SetClipboardText` | `string text` | `void` | Sets the clipboard text |
| `GetClipboardText` | | `string` | Gets the clipboard text |

### `ImGuiAppConfig` Class

Configuration for the ImGui application.

#### Properties

| Name | Type | Description |
|------|------|-------------|
| `Title` | `string` | The window title |
| `IconPath` | `string` | The file path to the application window icon |
| `InitialWindowState` | `ImGuiAppWindowState` | The initial state of the application window |
| `TestMode` | `bool` | Whether the application is running in test mode |
| `Fonts` | `Dictionary<string, byte[]>` | Font name to font data mapping |
| `OnStart` | `Action` | Called when the application starts |
| `OnUpdate` | `Action<float>` | Called each frame before rendering (param: delta time) |
| `OnRender` | `Action<float>` | Called each frame for rendering (param: delta time) |
| `OnAppMenu` | `Action` | Called each frame for rendering the application menu |
| `OnMoveOrResize` | `Action` | Called when the application window is moved or resized |
| `SaveIniSettings` | `bool` | Whether ImGui should save window settings to imgui.ini |

### `FontAppearance` Class

A utility class for applying font styles using a using statement.

#### Constructors

| Constructor | Parameters | Description |
|-------------|------------|-------------|
| `FontAppearance` | `string fontName` | Creates a font appearance with the named font at default size |
| `FontAppearance` | `float fontSize` | Creates a font appearance with the default font at the specified size |
| `FontAppearance` | `string fontName, float fontSize` | Creates a font appearance with the named font at the specified size |

## Demo Application

Check out the included demo project to see a working example:

1. Clone or download the repository
2. Open the solution in Visual Studio (or run dotnet build)
3. Start the ImGuiAppDemo project to see a basic ImGui application

## Contributing

Contributions are welcome! Here's how you can help:

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

Please make sure to update tests as appropriate and adhere to the existing coding style.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## Versioning

Check the [CHANGELOG.md](CHANGELOG.md) for detailed release notes and version changes.

## Acknowledgements

- [Dear ImGui](https://github.com/ocornut/imgui) - The immediate mode GUI library
- [Hexa.NET.ImGui](https://github.com/HexaEngine/Hexa.NET.ImGui) - .NET bindings for Dear ImGui
- [Silk.NET](https://github.com/dotnet/Silk.NET) - .NET bindings for OpenGL and windowing
- All contributors and the .NET community for their support

## Support

If you encounter any issues or have questions, please [open an issue](https://github.com/ktsu-dev/ImGuiApp/issues).
