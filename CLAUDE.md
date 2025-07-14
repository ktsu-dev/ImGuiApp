# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ImGuiApp is a .NET library that provides application scaffolding for Dear ImGui using Silk.NET and ImGui.NET. It simplifies creating ImGui applications by abstracting window management, rendering, input handling, DPI scaling, and font management.

## Architecture

### Core Components
- **ImGuiApp.cs**: Main static class providing the public API and application lifecycle management
- **ImGuiController/**: Contains OpenGL abstraction layer with IGL interface, texture management, font handling, and shader utilities
- **FontAppearance.cs**: RAII wrapper for applying font styles using `using` statements
- **UIScaler.cs**: Handles DPI-aware scaling calculations
- **ForceDpiAware.cs**: Platform-specific DPI detection and awareness enforcement
- **ImGuiAppConfig.cs**: Configuration object for application setup with lifecycle callbacks

### Key Design Patterns
- Static facade pattern for the main ImGuiApp class
- OpenGL abstraction through IGL interface for testability
- Resource management through IDisposable patterns
- Configuration-driven architecture with delegate callbacks

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

### PowerShell Build System
The project uses a comprehensive PowerShell build system located in `scripts/`:
- Use `Import-Module ./scripts/PSBuild.psm1` to access build functions
- `Invoke-CIPipeline` runs the complete CI pipeline with versioning, build, test, and packaging
- Supports semantic versioning based on git history and commit message tags ([major], [minor], [patch], [pre])

## Project Structure

### Main Library (`ImGuiApp/`)
- Uses ktsu.Sdk.Lib SDK with .NET 8+ multi-targeting
- Dependencies: ImGui.NET, Silk.NET, SixLabors.ImageSharp, ktsu utilities
- Allows unsafe blocks for OpenGL operations
- Embedded resources for fonts in `Resources/`

### Test Project (`ImGuiApp.Test/`)
- Uses ktsu.Sdk.Test SDK with MSTest framework
- Mock implementations for OpenGL testing (MockGL.cs, TestOpenGLProvider.cs)
- Tests for DPI awareness, font management, and core functionality

### Demo Application (`ImGuiAppDemo/`)
- Simple demonstration of library usage
- Shows basic window setup, font loading, and ImGui rendering

## Key Implementation Details

### DPI Handling
- Cross-platform DPI detection in ForceDpiAware.cs
- Automatic scaling calculations through UIScaler
- Platform-specific implementations for Windows, Linux, and WSL detection

### Font Management
- Embedded font resources (Roboto Mono Nerd Font variants)
- Dictionary-based font loading system in AppConfig
- FontAppearance wrapper for temporary font application

### OpenGL Abstraction
- IGL interface abstracts OpenGL calls for testing
- WindowOpenGLFactory creates platform-appropriate GL contexts
- Texture management with automatic cleanup

## Testing Approach

Tests use MSTest framework with mock OpenGL implementations. Key test categories:
- DPI detection across platforms
- Font loading and management
- OpenGL abstraction layer
- Application lifecycle management

When writing tests, use the existing mock patterns in TestOpenGLProvider.cs and MockGL.cs.

## Version Management

The project uses automated semantic versioning:
- Version tags in commit messages control increments
- Public API changes automatically trigger minor version bumps
- VERSION.md, CHANGELOG.md, and other metadata files are auto-generated
- Uses git history analysis for version calculation