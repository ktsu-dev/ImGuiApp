# Copilot Instructions for ktsu ImGui Suite

## Project Overview

This is a comprehensive C#/.NET suite of libraries for building ImGui applications. The project consists of four main libraries:
- **ImGui.App** - Application foundation and scaffolding
- **ImGui.Widgets** - Custom UI components and controls  
- **ImGui.Popups** - Modal dialogs and popup systems
- **ImGui.Styler** - Theming and styling utilities

## Technology Stack

- **.NET 9.0** - Target framework
- **C#** with latest language features
- **Dear ImGui** via Hexa.NET.ImGui bindings
- **Silk.NET** for cross-platform windowing
- **MSTest** for unit testing
- **PowerShell** for build automation (PSBuild.psm1)

## Code Style and Conventions

### C# Coding Standards

Use tabs for indentation in C# files (not spaces).

Follow these naming conventions:
- PascalCase for types, methods, properties, and public members
- Interfaces must start with `I` prefix
- Do not use `this.` qualifier
- Use language keywords instead of framework types (e.g., `int` not `Int32`)

Expression preferences:
- Use object and collection initializers
- Use explicit tuple names
- Prefer auto-properties
- Use pattern matching over `is` with cast
- Use switch expressions where appropriate
- No `var` - always use explicit types

File organization:
- Use file-scoped namespaces
- Place using directives inside namespace
- One class per file

Code structure:
- Always use braces for control flow statements
- No top-level statements - use traditional program structure
- Use primary constructors when appropriate
- Expression-bodied members only when on single line
- New line before opening braces for all constructs

### Code Quality Rules

All analyzer diagnostics are treated as errors. Key rules:
- Validate all public method arguments (CA1062)
- Implement IDisposable correctly (CA1063)  
- Avoid catching general exception types (CA1031)
- Use StringComparison explicitly (CA1307)
- Avoid excessive complexity (CA1502)
- All code must pass static analysis

### File Headers

All C# source files must include this header:
```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.
```

## Build and Testing

### Building

Use the PSBuild pipeline for builds:
```powershell
Import-Module ./scripts/PSBuild.psm1
```

Or use standard .NET commands:
```bash
dotnet restore
dotnet build
```

### Testing

Run tests using:
```bash
dotnet test
```

Tests use MSTest framework and are located in the `tests/` directory.

All code changes should include appropriate unit tests unless there is no existing test infrastructure for that component.

### CI/CD

The project uses GitHub Actions with a custom PowerShell build pipeline (`.github/workflows/dotnet.yml`):
- Builds, tests, and releases automatically
- Uses SonarQube for code analysis
- Generates code coverage reports
- Publishes to NuGet
- Runs on Windows

## Project Structure

```
ImGui.App/          - Core application framework
ImGui.Widgets/      - UI widgets and components
ImGui.Popups/       - Modal and popup systems
ImGui.Styler/       - Theme and styling utilities
examples/           - Demo applications
tests/              - Unit tests
scripts/            - Build scripts (PowerShell)
.github/            - GitHub Actions workflows
```

## Package Management

Dependencies are managed centrally via `Directory.Packages.props` for version consistency across all projects.

Use NuGet for package management:
```bash
dotnet add package <PackageName>
```

## Documentation

Update README.md files when making changes to APIs or adding new features.

Each library has its own README with:
- Feature documentation
- API examples
- Usage patterns

## Dependencies and Security

Before adding new NuGet packages, verify they are secure and well-maintained.

The project has scheduled security scans via GitHub Actions.

## Git Workflow

- Main development happens on `develop` branch
- Releases are made to `main` branch
- Follow conventional commit messages
- All commits must pass CI checks
- CI runs on push to main/develop and on all PRs

## Common Tasks

### Adding a new widget
1. Add class to `ImGui.Widgets/` 
2. Follow existing widget patterns
3. Add demo usage to `examples/ImGuiWidgetsDemo/`
4. Update `ImGui.Widgets/README.md`
5. Add unit tests if applicable

### Adding a new theme
1. Add theme definition to `ImGui.Styler/`
2. Follow existing theme structure
3. Test in `examples/ImGuiStylerDemo/`
4. Update theme gallery documentation

### Modifying application framework
1. Changes to `ImGui.App/` affect all consumers
2. Ensure backward compatibility when possible
3. Update breaking changes in CHANGELOG.md
4. Test with all example applications

## Notes

- This is a cross-platform library targeting Windows, macOS, and Linux
- Performance is critical - use efficient patterns
- The project uses Dear ImGui's immediate mode paradigm
- Font and texture management requires careful resource handling
