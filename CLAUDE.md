# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ImGuiApp is a comprehensive .NET library that provides complete application scaffolding for Dear ImGui applications. It features advanced window management, DPI-aware rendering, performance optimization with throttling, advanced font handling with Unicode/emoji support, texture management with caching, debug tooling, and cross-platform compatibility. Built on Silk.NET for OpenGL and windowing, with Hexa.NET.ImGui for modern Dear ImGui bindings.

## Architecture

### Core Components
- **ImGuiApp.cs**: Main static class providing the public API, application lifecycle management, debug logging, and performance optimization
- **ImGuiAppConfig.cs**: Configuration object with comprehensive settings including performance tuning and lifecycle callbacks
- **ImGuiAppPerformanceSettings.cs**: Advanced performance settings for throttled rendering with sleep-based frame rate control
- **ImGuiController/**: Contains OpenGL abstraction layer with IGL interface, texture management, font handling, and shader utilities
- **FontAppearance.cs**: RAII wrapper for applying font styles using `using` statements with dynamic scaling support
- **FontHelper.cs**: Utility for advanced Unicode, emoji, and extended character range handling
- **UIScaler.cs**: Handles DPI-aware scaling calculations with pixel-perfect rendering
- **ForceDpiAware.cs**: Platform-specific DPI detection and awareness enforcement across Windows, Linux, and WSL
- **GdiPlusHelper.cs**: Graphics utilities for enhanced rendering capabilities

### Key Design Patterns
- Static facade pattern for the main ImGuiApp class
- OpenGL abstraction through IGL interface for testability
- Resource management through IDisposable patterns with automatic cleanup
- Configuration-driven architecture with extensive delegate callbacks
- Performance optimization through sleep-based throttling with multi-condition evaluation
- Debug logging with automatic crash diagnostics

### Recent Enhancements
- **Debug Logging**: Comprehensive logging system that creates debug files for troubleshooting crashes and performance issues
- **Performance Throttling**: Multi-condition frame rate throttling (focused/unfocused/idle/not visible) using sleep-based timing
- **Context Handling**: Automatic OpenGL context change detection with texture reloading
- **Memory Optimization**: Pool-based memory management for textures and improved font memory handling
- **Unicode & Emoji Support**: Built-in support enabled by default with configurable character ranges
- **Dynamic Font Scaling**: Improved font rendering with crisp scaling at multiple DPI levels

## Build and Development Commands

### Build
```bash
dotnet build ImGuiApp.sln
```

### Test
```bash
dotnet test ImGuiApp.Test/
```

### Run Demo
```bash
dotnet run --project ImGuiAppDemo/
```

### Package
```bash
dotnet pack ImGuiApp/ -o staging/
```

### Run Single Test
```bash
dotnet test ImGuiApp.Test/ --filter "TestMethodName"
```

### Build in Release Mode
```bash
dotnet build ImGuiApp.sln -c Release
```

### PowerShell Build System
The project uses a comprehensive PowerShell build system located in `scripts/`:
- Use `Import-Module ./scripts/PSBuild.psm1` to access build functions
- `Invoke-CIPipeline` runs the complete CI pipeline with versioning, build, test, and packaging
- Supports semantic versioning based on git history and commit message tags ([major], [minor], [patch], [pre])

## Project Structure

### Main Library (`ImGuiApp/`)
- Uses ktsu.Sdk.Lib SDK with .NET 9+ multi-targeting
- Dependencies: Hexa.NET.ImGui, Silk.NET, SixLabors.ImageSharp, ktsu utilities (Invoker, StrongPaths, Extensions, ScopedAction)
- Allows unsafe blocks for OpenGL operations and memory management
- Embedded resources for fonts including Nerd Font and NotoEmoji in `Resources/`

### Test Project (`ImGuiApp.Test/`)
- Uses ktsu.Sdk.Test SDK with MSTest framework
- Comprehensive test coverage across multiple test classes:
  - `ImGuiAppCoreTests.cs`: Core functionality tests
  - `ImGuiAppDataStructureTests.cs`: Configuration and data structure tests
  - `FontAndUITests.cs`: Font management and UI scaling tests
  - `ErrorHandlingAndEdgeCaseTests.cs`: Exception handling and edge cases
  - `PlatformSpecificTests.cs`: Cross-platform compatibility tests
  - `AdvancedCoverageTests.cs`: Advanced feature coverage
- Mock implementations for OpenGL testing (MockGL.cs, TestOpenGLProvider.cs)
- Tests for DPI awareness, font management, performance throttling, and debug features

### Demo Application (`ImGuiAppDemo/`)
- Comprehensive demonstration with tabbed interface:
  - Unicode & Emoji showcase
  - Widgets & Layout demonstrations
  - Graphics & Plotting examples
  - Nerd Font icons gallery
- Performance monitoring accessible through Debug menu (Debug > Show Performance Monitor)

## Key Implementation Details

### Performance Optimization
- Sleep-based frame rate throttling with Thread.Sleep for precise timing control
- Multi-condition evaluation (focused/unfocused/idle/not visible) with "lowest wins" logic
- Automatic user input detection for idle state management
- Resource conservation with ultra-low frame rates when minimized

### Debug Features
- Automatic debug log creation (`ImGuiApp_Debug.log` on desktop)
- Comprehensive logging during initialization, font loading, and error conditions
- Built-in debug menu with ImGui Demo, Metrics, and Performance Monitor windows
- Real-time performance monitoring with FPS graphs and throttling visualization

### DPI Handling
- Cross-platform DPI detection in ForceDpiAware.cs with Windows, Linux, and WSL support
- Automatic scaling calculations through UIScaler with pixel-perfect rendering
- Dynamic font scaling based on DPI changes
- Platform-specific implementations with Wayland support

### Font Management
- Embedded font resources (Nerd Font and NotoEmoji)
- Dynamic font loading system with multiple size support (10, 12, 14, 16, 18, 20, 24, 32, 48 pt)
- Unicode and emoji support enabled by default
- FontHelper utility for extended character ranges and glyph management
- Memory-efficient font loading with pre-allocated handles

### Texture Management
- Concurrent dictionary-based texture caching
- Automatic texture reloading on OpenGL context changes
- Pool-based memory management for improved performance
- Support for various image formats through SixLabors.ImageSharp

### OpenGL Abstraction
- IGL interface abstracts OpenGL calls for testing
- WindowOpenGLFactory creates platform-appropriate GL contexts
- Automatic context change detection and handling
- Cross-platform compatibility (Windows, Linux)

## Testing Approach

Tests use MSTest framework with comprehensive mock OpenGL implementations. Key test categories:
- Core application lifecycle and configuration validation
- DPI detection across multiple platforms
- Font loading, scaling, and Unicode support
- Performance throttling and idle detection
- OpenGL abstraction layer and context handling
- Error handling and edge cases
- Memory management and cleanup

When writing tests, use the existing mock patterns in TestOpenGLProvider.cs and MockGL.cs. The test suite is organized into focused test classes for better maintainability.

## Version Management

The project uses automated semantic versioning with comprehensive changelog generation:
- Version tags in commit messages control increments
- Public API changes automatically trigger minor version bumps
- VERSION.md, CHANGELOG.md, and other metadata files are auto-generated
- Uses git history analysis for version calculation with detailed release notes

## Key File Locations and Patterns

### Main Components
- `ImGuiApp/ImGuiApp.cs`: Main static API class with debug logging and performance optimization (ImGuiApp:32)
- `ImGuiApp/ImGuiController/IGL.cs`: OpenGL abstraction interface
- `ImGuiApp/ImGuiController/ImGuiController.cs`: Core ImGui controller implementation
- `ImGuiApp/FontAppearance.cs`: RAII font management with dynamic scaling (FontAppearance:14)
- `ImGuiApp/FontHelper.cs`: Unicode, emoji, and glyph range utilities
- `ImGuiApp/UIScaler.cs`: DPI-aware scaling utility with pixel-perfect rendering (UIScaler:16)
- `ImGuiApp/ForceDpiAware.cs`: Cross-platform DPI detection with Wayland support (ForceDpiAware:17)

### Configuration and Data
- `ImGuiApp/ImGuiAppConfig.cs`: Application configuration with performance settings (ImGuiAppConfig:12)
- `ImGuiApp/ImGuiAppPerformanceSettings.cs`: Advanced performance tuning configuration
- `ImGuiApp/ImGuiAppWindowState.cs`: Window state management
- `ImGuiApp/ImGuiAppTextureInfo.cs`: Texture information and management
- `ImGuiApp/Resources/`: Embedded font resources (Nerd Font and NotoEmoji)

### Testing Infrastructure
- `ImGuiApp.Test/MockGL.cs`: Mock OpenGL implementation for testing
- `ImGuiApp.Test/TestOpenGLProvider.cs`: Test OpenGL provider
- `ImGuiApp.Test/TestHelpers.cs`: Common test utilities
- `ImGuiApp.Test/ImGuiAppCoreTests.cs`: Core application functionality tests
- `ImGuiApp.Test/FontAndUITests.cs`: Font and UI scaling tests
- `ImGuiApp.Test/ErrorHandlingAndEdgeCaseTests.cs`: Exception handling tests
- `ImGuiApp.Test/AdvancedCoverageTests.cs`: Advanced feature coverage

### Build System
- `scripts/PSBuild.psm1`: PowerShell build automation module
- Uses ktsu.Sdk.Lib, ktsu.Sdk.Test, and ktsu.Sdk.App SDKs
- Supports automatic font updates via `scripts/Update-NerdFont.ps1`
