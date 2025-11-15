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
- **Performance Optimization**: Sleep-based throttled rendering with lowest-selection logic when unfocused, idle, or not visible to maximize resource savings
- **PID Frame Limiting**: Precision frame rate control using a PID controller with comprehensive auto-tuning capabilities for highly accurate target FPS achievement
- **DPI Awareness**: Built-in support for high-DPI displays and scaling
- **Font Management**: Flexible font loading system with customization options and dynamic scaling
- **Font Memory Guard**: Intelligent GPU memory management for font atlases with special handling for Intel & AMD integrated GPUs
- **Unicode & Emoji Support**: Built-in support for Unicode characters and emojis (enabled by default)
- **Texture Support**: Built-in texture management with caching and automatic cleanup for ImGui
- **Debug Logging**: Comprehensive debug logging system for troubleshooting crashes and performance issues
- **Context Handling**: Automatic OpenGL context change detection and texture reloading
- **Lifecycle Callbacks**: Customizable delegate callbacks for application events
- **Menu System**: Easy-to-use API for creating application menus
- **Positioning Guards**: Offscreen positioning checks to keep windows visible
- **Modern .NET**: Supports .NET 9 and newer
- **Active Development**: Open-source and actively maintained

## Getting Started

### Prerequisites

- .NET 9.0 or later

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

### Unicode and Emoji Support

ImGuiApp automatically includes support for Unicode characters and emojis. This feature is **enabled by default**, so you can use extended characters without any configuration:

```csharp
private static void OnRender(float deltaTime)
{
    ImGui.Text("Basic ASCII: Hello World!");
    ImGui.Text("Accented characters: café, naïve, résumé");
    ImGui.Text("Mathematical symbols: ∞ ≠ ≈ ≤ ≥ ± × ÷ ∂ ∑");
    ImGui.Text("Currency symbols: $ € £ ¥ ₹ ₿");
    ImGui.Text("Arrows: ← → ↑ ↓ ↔ ↕");
    ImGui.Text("Emojis (if font supports): 😀 🚀 🌟 💻 🎨 🌈");
}
```

**Note**: Character display depends on your font's Unicode support. Most modern fonts include extended Latin characters and symbols, but emojis require specialized fonts.

To disable Unicode support (ASCII only), set `EnableUnicodeSupport = false`:

```csharp
ImGuiApp.Start(new()
{
    Title = "ASCII Only App",
    EnableUnicodeSupport = false, // Disables Unicode support
    // ... other settings
});
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

### PID Frame Limiting

ImGuiApp features a sophisticated **PID (Proportional-Integral-Derivative) controller** for precise frame rate limiting. This system provides highly accurate target FPS control that learns and adapts to your system's characteristics.

#### Key Features

- **High-Precision Timing**: Hybrid sleep system combining `Thread.Sleep()` for coarse delays with spin-waiting for sub-millisecond accuracy
- **PID Controller**: Advanced control algorithm that learns from frame timing errors and dynamically adjusts sleep times
- **Comprehensive Auto-Tuning**: Multi-phase tuning procedure that automatically finds optimal PID parameters for your system
- **VSync Independence**: Works independently of monitor refresh rates for any target FPS
- **Real-Time Diagnostics**: Built-in performance monitoring and tuning visualization

#### Optimized Defaults

ImGuiApp comes pre-configured with optimal PID parameters derived from comprehensive auto-tuning:

- **Kp: 1.800** - Proportional gain for current error response
- **Ki: 0.048** - Integral gain for accumulated error correction  
- **Kd: 0.237** - Derivative gain for predictive adjustment

These defaults provide excellent frame timing accuracy out-of-the-box for most systems.

#### Configuration

Configure frame limiting through `ImGuiAppPerformanceSettings`:

```csharp
ImGuiApp.Start(new ImGuiAppConfig
{
    Title = "PID Frame Limited App",
    OnRender = OnRender,
    PerformanceSettings = new ImGuiAppPerformanceSettings
    {
        EnableThrottledRendering = true,
        FocusedFps = 30.0,           // Target 30 FPS when focused
        UnfocusedFps = 5.0,          // Target 5 FPS when unfocused
        IdleFps = 10.0,              // Target 10 FPS when idle
        NotVisibleFps = 2.0,         // Target 2 FPS when minimized
        EnableIdleDetection = true,
        IdleTimeoutSeconds = 30.0    // Idle after 30 seconds
    }
});
```

#### Auto-Tuning Procedure

For maximum accuracy, ImGuiApp includes a comprehensive **3-phase auto-tuning system**:

1. **Coarse Phase** (8s per test): Tests 24 parameter combinations to find the general optimal range
2. **Fine Phase** (12s per test): Tests 25 refined parameters around the best coarse result  
3. **Precision Phase** (15s per test): Final optimization with 9 precision-focused parameters

**Total tuning time**: ~12-15 minutes for maximum accuracy

Access auto-tuning through the **Debug > Show Performance Monitor** menu, which provides:
- Real-time tuning progress visualization
- Performance metrics (Average Error, Max Error, Stability, Score)
- Interactive tuning controls and results display
- Live FPS graphs showing PID controller performance

#### Technical Details

The PID controller works by:
- **Measuring** actual frame times vs. target frame times
- **Calculating** error using smoothed measurements to reduce noise
- **Adjusting** sleep duration using PID mathematics: `output = Kp×error + Ki×∫error + Kd×Δerror`
- **Learning** from past performance to minimize future timing errors

The system automatically:
- Disables VSync to prevent interference with custom frame limiting
- Pauses throttling during auto-tuning for accurate measurements  
- Uses integral windup prevention to maintain stability
- Applies high-precision sleep for sub-millisecond timing accuracy

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
            InitialWindowState = new ImGuiAppWindowState
            {
                Size = new Vector2(1280, 720),
                Pos = new Vector2(100, 100)
            },
            OnStart = OnStart,
            OnUpdate = OnUpdate,
            OnRender = OnRender,
            OnAppMenu = OnAppMenu,
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

    private static Vector3 _backgroundColor = new Vector3(0.45f, 0.55f, 0.60f);
}
```

## API Reference

### `ImGuiApp` Static Class

The main entry point for creating and managing ImGui applications.

#### Properties

| Name | Type | Description |
|------|------|-------------|
| `WindowState` | `ImGuiAppWindowState` | Gets the current state of the application window |
| `Invoker` | `Invoker` | Gets an instance to delegate tasks to the window thread |
| `IsFocused` | `bool` | Gets whether the application window is focused |
| `IsVisible` | `bool` | Gets whether the application window is visible |
| `IsIdle` | `bool` | Gets whether the application is currently idle |
| `ScaleFactor` | `float` | Gets the current DPI scale factor |

#### Methods

| Name | Parameters | Return Type | Description |
|------|------------|-------------|-------------|
| `Start` | `ImGuiAppConfig config` | `void` | Starts the ImGui application with the provided configuration |
| `Stop` | | `void` | Stops the running application |
| `GetOrLoadTexture` | `AbsoluteFilePath path` | `ImGuiAppTextureInfo` | Loads a texture from file or returns cached texture info if already loaded |
| `TryGetTexture` | `AbsoluteFilePath path, out ImGuiAppTextureInfo textureInfo` | `bool` | Attempts to get a cached texture by path |
| `DeleteTexture` | `uint textureId` | `void` | Deletes a texture and frees its resources |
| `DeleteTexture` | `ImGuiAppTextureInfo textureInfo` | `void` | Deletes a texture and frees its resources (convenience overload) |
| `CleanupAllTextures` | | `void` | Cleans up all loaded textures |
| `SetWindowIcon` | `string iconPath` | `void` | Sets the window icon using the specified icon file path |
| `EmsToPx` | `float ems` | `int` | Converts a value in ems to pixels based on current font size |
| `PtsToPx` | `int pts` | `int` | Converts a value in points to pixels based on current scale factor |
| `UseImageBytes` | `Image<Rgba32> image, Action<byte[]> action` | `void` | Executes an action with temporary access to image bytes using pooled memory |

### `ImGuiAppConfig` Class

Configuration for the ImGui application.

#### Properties

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `TestMode` | `bool` | `false` | Whether the application is running in test mode |
| `Title` | `string` | `"ImGuiApp"` | The window title |
| `IconPath` | `string` | `""` | The file path to the application window icon |
| `InitialWindowState` | `ImGuiAppWindowState` | `new()` | The initial state of the application window |
| `Fonts` | `Dictionary<string, byte[]>` | `[]` | Font name to font data mapping |
| `EnableUnicodeSupport` | `bool` | `true` | Whether to enable Unicode and emoji support |
| `SaveIniSettings` | `bool` | `true` | Whether ImGui should save window settings to imgui.ini |
| `PerformanceSettings` | `ImGuiAppPerformanceSettings` | `new()` | Performance settings for throttled rendering |
| `OnStart` | `Action` | `() => { }` | Called when the application starts |
| `FrameWrapperFactory` | `Func<ScopedAction?>` | `() => null` | Factory for creating frame wrappers |
| `OnUpdate` | `Action<float>` | `(delta) => { }` | Called each frame before rendering (param: delta time) |
| `OnRender` | `Action<float>` | `(delta) => { }` | Called each frame for rendering (param: delta time) |
| `OnAppMenu` | `Action` | `() => { }` | Called each frame for rendering the application menu |
| `OnMoveOrResize` | `Action` | `() => { }` | Called when the application window is moved or resized |

### `ImGuiAppPerformanceSettings` Class

Configuration for performance optimization and throttled rendering. Uses a sophisticated **PID controller with high-precision timing** to achieve accurate target frame rates while maintaining system resource efficiency. The system combines Thread.Sleep for coarse delays with spin-waiting for sub-millisecond precision, and automatically disables VSync to prevent interference with custom frame limiting.

#### Properties

| Name | Type | Default | Description |
|------|------|---------|-------------|
| `EnableThrottledRendering` | `bool` | `true` | Enables/disables throttled rendering feature |
| `FocusedFps` | `double` | `30.0` | Target frame rate when the window is focused and active |
| `UnfocusedFps` | `double` | `5.0` | Target frame rate when the window is unfocused |
| `IdleFps` | `double` | `10.0` | Target frame rate when the application is idle (no user input) |
| `NotVisibleFps` | `double` | `2.0` | Target frame rate when the window is not visible (minimized or hidden) |
| `EnableIdleDetection` | `bool` | `true` | Enables/disables idle detection based on user input |
| `IdleTimeoutSeconds` | `double` | `30.0` | Time in seconds without user input before considering the app idle |

#### Example Usage

```csharp
ImGuiApp.Start(new ImGuiAppConfig
{
    Title = "My Application",
    OnRender = OnRender,
    PerformanceSettings = new ImGuiAppPerformanceSettings
    {
        EnableThrottledRendering = true,
        FocusedFps = 60.0,           // Custom higher rate when focused
        UnfocusedFps = 15.0,         // Custom rate when unfocused
        IdleFps = 2.0,               // Custom very low rate when idle
        NotVisibleFps = 1.0,         // Custom ultra-low rate when minimized
        EnableIdleDetection = true,
        IdleTimeoutSeconds = 10.0    // Custom idle timeout
    }
    // PID controller uses optimized defaults: Kp=1.8, Ki=0.048, Kd=0.237
    // For fine-tuning, use Debug > Show Performance Monitor > Start Auto-Tuning
});
```

This feature automatically:
- Uses a **PID controller** with optimized defaults for highly accurate frame rate targeting
- Combines **Thread.Sleep** with **spin-waiting** for sub-millisecond timing precision
- Disables **VSync** automatically to prevent interference with custom frame limiting
- Detects when the window loses/gains focus and visibility state (minimized/hidden)
- Tracks user input (keyboard, mouse movement, clicks, scrolling) for idle detection
- Evaluates all applicable throttling conditions and selects the lowest frame rate
- Saves significant CPU and GPU resources without affecting user experience
- Provides instant transitions between different performance states
- Uses conservative defaults: 30 FPS focused, 5 FPS unfocused, 10 FPS idle, 2 FPS not visible

The **PID controller** learns from timing errors and adapts to your system's characteristics, providing much more accurate frame rate control than simple sleep-based methods. The throttling system uses a "lowest wins" approach - if multiple conditions apply (e.g., unfocused + idle), the lowest frame rate is automatically selected for maximum resource savings.

### `FontAppearance` Class

A utility class for applying font styles using a using statement.

#### Constructors

| Constructor | Parameters | Description |
|-------------|------------|-------------|
| `FontAppearance` | `string fontName` | Creates a font appearance with the named font at default size |
| `FontAppearance` | `float fontSize` | Creates a font appearance with the default font at the specified size |
| `FontAppearance` | `string fontName, float fontSize` | Creates a font appearance with the named font at the specified size |

### `FontMemoryGuard` Static Class

Provides intelligent memory management for font atlas textures, preventing excessive memory allocation on systems with integrated GPUs or high-resolution displays.

#### Key Features

- **Automatic GPU Detection**: Identifies Intel and AMD integrated GPUs using renderer string analysis
- **Smart Memory Limits**: Conservative 16-96MB limits for integrated GPUs vs 64-128MB for discrete GPUs
- **Generation-Aware**: Newer integrated GPUs (Intel Xe, AMD RDNA2+) get higher memory limits
- **Fallback Strategies**: Automatically reduces font sizes, disables emojis, or limits Unicode ranges when memory constrained
- **Memory Estimation**: Calculates expected memory usage before font loading

#### Why This Matters

Integrated GPUs share system RAM and have limited memory bandwidth. A 4K display with full Unicode font support can easily create 200MB+ font atlases, potentially causing:
- Application crashes from GPU memory exhaustion
- Severe performance degradation from memory pressure
- System-wide slowdowns as GPU competes with CPU for RAM bandwidth

#### Configuration

```csharp
// Configure font memory limits
FontMemoryGuard.CurrentConfig = new FontMemoryGuard.FontMemoryConfig
{
    MaxAtlasMemoryBytes = 64 * 1024 * 1024, // 64MB default
    EnableGpuMemoryDetection = true,
    MaxGpuMemoryPercentage = 0.1f, // 10% for discrete GPUs
    EnableIntelGpuHeuristics = true,
    EnableAmdApuHeuristics = true,
    EnableFallbackStrategies = true,
    MinFontSizesToLoad = 3,
    DisableEmojisOnLowMemory = true,
    ReduceUnicodeRangesOnLowMemory = true
};
```

#### Memory Estimation

```csharp
// Estimate memory usage before loading fonts
var estimate = FontMemoryGuard.EstimateMemoryUsage(
    fontCount: 2,
    fontSizes: new[] { 12, 16, 20, 24 },
    includeEmojis: true,
    includeExtendedUnicode: true,
    scaleFactor: 1.5f
);

if (estimate.ExceedsLimits)
{
    Console.WriteLine($"Estimated memory: {estimate.EstimatedBytes / 1024 / 1024}MB");
    Console.WriteLine($"Recommended max sizes: {estimate.RecommendedMaxSizes}");
    Console.WriteLine($"Should disable emojis: {estimate.ShouldDisableEmojis}");
}
```

### `ImGuiAppWindowState` Class

Represents the state of the application window.

#### Properties

| Name | Type | Description |
|------|------|-------------|
| `Size` | `Vector2` | The size of the window |
| `Pos` | `Vector2` | The position of the window |
| `LayoutState` | `WindowState` | The layout state of the window (Normal, Maximized, etc.) |

## Debug Features

ImGuiApp includes comprehensive debug logging capabilities to help troubleshoot crashes and performance issues:

### Debug Logging

The application automatically creates debug logs on the desktop (`ImGuiApp_Debug.log`) when issues occur. These logs include:
- Window initialization steps
- OpenGL context creation
- Font loading progress  
- Error conditions and exceptions

### Debug Menu

When using the `OnAppMenu` callback, ImGuiApp automatically adds a Debug menu with options to:
- Show ImGui Demo Window
- Show ImGui Metrics Window
- Show Performance Monitor (real-time FPS graphs and throttling visualization)

### Performance Monitoring

The core library includes a built-in performance monitor accessible via the debug menu. It provides:
- Real-time FPS tracking and visualization
- Throttling state monitoring (focused/unfocused/idle/not visible)
- Performance testing tips and interactive guidance
- Historical performance data graphing

Access it through: **Debug > Show Performance Monitor**

## Demo Application

Check out the included demo project to see a comprehensive working example:

1. Clone or download the repository
2. Open the solution in Visual Studio (or run dotnet build)
3. Start the ImGuiAppDemo project to see a feature-rich ImGui application
4. Explore the different tabs:
   - **Unicode & Emojis**: Test character rendering with extended Unicode support
   - **Widgets & Layout**: Comprehensive ImGui widget demonstrations
   - **Graphics & Plotting**: Custom drawing and data visualization examples
   - **Nerd Font Icons**: Browse and test various icon sets and glyphs
5. Use the debug menu to access additional features:
   - **Debug > Show Performance Monitor**: Real-time FPS graph showing PID controller performance with comprehensive auto-tuning capabilities
   - **Debug > Show ImGui Demo**: Official ImGui demo window
   - **Debug > Show ImGui Metrics**: ImGui internal metrics and debugging info

The **Performance Monitor** includes:
- **Live FPS graphs** that visualize frame rate changes as you focus/unfocus the window, let it go idle, or minimize it
- **PID Controller diagnostics** showing real-time proportional, integral, and derivative values
- **Comprehensive Auto-Tuning** with 3-phase optimization (Coarse, Fine, Precision phases)
- **Performance metrics** including Average Error, Max Error, Stability, and composite Score
- **Interactive tuning controls** to start/stop optimization and view detailed results

Perfect for seeing both the throttling system and PID controller work in real-time!

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
