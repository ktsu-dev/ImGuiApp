# ktsu.ImGuiApp

A .NET library that provides application scaffolding for [Dear ImGui](https://github.com/ocornut/imgui), using [Silk.NET](https://github.com/dotnet/Silk.NET) and [ImGui.NET](https://github.com/mellinoe/ImGui.NET).

[![NuGet](https://img.shields.io/nuget/v/ktsu.ImGuiApp.svg)](https://www.nuget.org/packages/ktsu.ImGuiApp/)
[![License](https://img.shields.io/github/license/ktsu-dev/ImGuiApp.svg)](LICENSE.md)

## Features
- Provides a simple-to-use API for creating ImGui applications.
- Uses Silk.NET for OpenGL and input handling.
- Manages window state, rendering, and input handling.
- DPI-aware scaling and offscreen positioning checks for window management.
- Built-in font loading system and texture management for ImGui.
- Flexible delegate callbacks for application lifecycle and menu handling.
- Sample project that demonstrates how to use the library in a .NET application.
- Supports .NET 8 and newer.
- Open-source and actively maintained.

## Installation

To install ImGuiApp, you can use the .NET CLI:

```bash
dotnet add package ktsu.ImGuiApp
```

Or by adding the package reference to your project file:

```xml
<PackageReference Include="ktsu.ImGuiApp" Version="X.X.X" />
```

Or you can use the NuGet Package Manager in Visual Studio to search for and install the `ktsu.ImGuiApp` package.

## Quick Start

Create a new class and call `ImGuiApp.Start()` with your application config:

```csharp
using ktsu.ImGuiApp;
using ImGuiNET;

static class Program
{
    static void Main()
    {
        ImGuiApp.Start(new AppConfig()
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

## Demo Application

1. Clone or download the repository.
2. Open the solution in Visual Studio (or run dotnet build).
3. Start the ImGuiAppDemo project to see a basic ImGui application.

## Using `FontAppearance` to Customize Fonts

Use the resource designer to add font files to your project, then load the fonts using the `ImGuiApp.Start()` call:

```csharp
ImGuiApp.Start(new()
{
    Title = "ImGuiApp Demo",
    OnRender = OnRender,
    Fonts = Fonts = new Dictionary<string, byte[]>
    {
        { nameof(Resources.MY_FONT), Resources.MY_FONT }
    },
});
```

Or load the font data manually and pass the data to the `ImGuiApp.Start()` call:

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

## Texture Management

Load and manage textures using `ImGuiApp.GetOrLoadTexture`, `ImGuiApp.UploadTextureRGBA`, and `ImGuiApp.DeleteTexture`:

```csharp
var textureId = ImGuiApp.GetOrLoadTexture("path/to/texture.png");
ImGui.Image(textureId, new Vector2(128, 128));
```

## Contributing

Contributions are welcome! Please submit issues and pull requests to help improve the library.

## Versioning

Check the [CHANGELOG.md](CHANGELOG.md) for detailed release notes and version changes.

## License

ImGuiApp is licensed under the MIT License. See the [LICENSE.md](LICENSE.md) file for more information.
