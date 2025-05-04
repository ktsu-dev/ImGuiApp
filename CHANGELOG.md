## v1.12.6-pre.1 (prerelease)

Changes since v1.12.5:

- Sync scripts\PSBuild.psm1 ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v1.12.5 (patch)

Changes since v1.12.4:

- Update DESCRIPTION.md to clarify the purpose of the library as a .NET application scaffolding tool for Dear ImGui, utilizing Silk.NET and ImGui.NET. ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor variable declarations in ImGuiApp and related files to use 'var' for improved readability and consistency. Update .editorconfig to change the suggestion level for 'dotnet_style_prefer_auto_properties' from silent to suggestion. ([@matt-edmondson](https://github.com/matt-edmondson))
- Implement texture caching and memory optimization in ImGuiApp. Introduced a concurrent dictionary for texture management, added a method to load textures with pooled memory usage, and implemented a cleanup function for unused textures. Refactored the texture upload process for improved performance and reliability. ([@matt-edmondson](https://github.com/matt-edmondson))
- Add Moq package and enhance ImGuiAppTests with new unit tests ([@matt-edmondson](https://github.com/matt-edmondson))
- Enhance README.md with improved structure and content. Added new sections for Introduction, API Reference, and Acknowledgements. Updated features list for clarity and detail. Included usage examples for application setup and texture management. ([@matt-edmondson](https://github.com/matt-edmondson))
- Update project SDK references to ktsu.Sdk version 1.8.0 ([@matt-edmondson](https://github.com/matt-edmondson))
- Style conformance ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor font management in ImGuiApp to improve memory handling and performance. Added logic to prevent unnecessary font reloads based on scale factor changes. Implemented cleanup for pinned font data during window closing and enhanced font initialization process. ([@matt-edmondson](https://github.com/matt-edmondson))
- Enhance README.md with a new "Getting Started" section detailing prerequisites for .NET 8.0 and Windows OS. Clean up code formatting in the Program class by removing unnecessary blank lines for improved readability. ([@matt-edmondson](https://github.com/matt-edmondson))
- Add additional demo features to ImGuiApp, including a Style Editor, Metrics window, and About section. Enhanced the main demo window with new widgets and improved layout options. Implemented real-time plotting functionality and updated menu items for better navigation. ([@matt-edmondson](https://github.com/matt-edmondson))
- Add VSCode configuration files for .NET Core development ([@matt-edmondson](https://github.com/matt-edmondson))
- Add unit tests for ForceDpiAware and ImGuiApp functionality ([@matt-edmondson](https://github.com/matt-edmondson))
- Remove obsolete build configuration files and scripts, including Directory.Build.props, Directory.Build.targets, and various PowerShell scripts for metadata and version management. Introduce a new PowerShell module, PSBuild, for automating the build, test, package, and release processes for .NET applications. ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor ImGuiApp startup process by consolidating window initialization and event handler setup into dedicated methods. This improves code organization and readability while maintaining functionality. ([@matt-edmondson](https://github.com/matt-edmondson))
- Update copyright headers in ImGuiApp files to reflect ownership and licensing. Ensure consistent formatting across multiple files, enhancing clarity and compliance with licensing requirements. ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.12.5-pre.13 (prerelease)

Changes since v1.12.5-pre.12:

- Bump Moq from 4.20.70 to 4.20.72 ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Bump SixLabors.ImageSharp from 3.1.7 to 3.1.8 ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.12.5-pre.12 (prerelease)

Changes since v1.12.5-pre.11:

- Update buffer configuration in ImGuiApp class ([@github-actions[bot]](https://github.com/github-actions[bot]))
## v1.12.5-pre.11 (prerelease)

Changes since v1.12.5-pre.10:

- Add OpenGL context change handling and texture reloading in ImGuiApp ([@github-actions[bot]](https://github.com/github-actions[bot]))
- Add texture reloading test to ImGuiAppTests ([@github-actions[bot]](https://github.com/github-actions[bot]))
## v1.12.5-pre.10 (prerelease)

Changes since v1.12.5-pre.9:

- Refactor image handling in ImGuiApp for improved memory management ([@github-actions[bot]](https://github.com/github-actions[bot]))
## v1.12.5-pre.9 (prerelease)

Changes since v1.12.5-pre.8:

- Add TryGetTexture methods to ImGuiApp for texture retrieval ([@github-actions[bot]](https://github.com/github-actions[bot]))
## v1.12.5-pre.8 (prerelease)

Changes since v1.12.5-pre.7:

- Enhance ImGuiApp configuration and pixel conversion methods ([@github-actions[bot]](https://github.com/github-actions[bot]))
- Enhance ImGuiApp configuration validation and testing ([@github-actions[bot]](https://github.com/github-actions[bot]))
## v1.12.5-pre.7 (prerelease)

Changes since v1.12.5-pre.6:

- Enhance EnsureWindowPositionIsValid test to validate window positioning ([@github-actions[bot]](https://github.com/github-actions[bot]))
## v1.12.5-pre.6 (prerelease)

Changes since v1.12.5-pre.5:

- Update DESCRIPTION.md to clarify the purpose of the library as a .NET application scaffolding tool for Dear ImGui, utilizing Silk.NET and ImGui.NET. ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor variable declarations in ImGuiApp and related files to use 'var' for improved readability and consistency. Update .editorconfig to change the suggestion level for 'dotnet_style_prefer_auto_properties' from silent to suggestion. ([@matt-edmondson](https://github.com/matt-edmondson))
- Implement texture caching and memory optimization in ImGuiApp. Introduced a concurrent dictionary for texture management, added a method to load textures with pooled memory usage, and implemented a cleanup function for unused textures. Refactored the texture upload process for improved performance and reliability. ([@matt-edmondson](https://github.com/matt-edmondson))
- Add Moq package and enhance ImGuiAppTests with new unit tests ([@matt-edmondson](https://github.com/matt-edmondson))
- Enhance README.md with improved structure and content. Added new sections for Introduction, API Reference, and Acknowledgements. Updated features list for clarity and detail. Included usage examples for application setup and texture management. ([@matt-edmondson](https://github.com/matt-edmondson))
- Style conformance ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor font management in ImGuiApp to improve memory handling and performance. Added logic to prevent unnecessary font reloads based on scale factor changes. Implemented cleanup for pinned font data during window closing and enhanced font initialization process. ([@matt-edmondson](https://github.com/matt-edmondson))
- Enhance README.md with a new "Getting Started" section detailing prerequisites for .NET 8.0 and Windows OS. Clean up code formatting in the Program class by removing unnecessary blank lines for improved readability. ([@matt-edmondson](https://github.com/matt-edmondson))
- Add additional demo features to ImGuiApp, including a Style Editor, Metrics window, and About section. Enhanced the main demo window with new widgets and improved layout options. Implemented real-time plotting functionality and updated menu items for better navigation. ([@matt-edmondson](https://github.com/matt-edmondson))
- Add VSCode configuration files for .NET Core development ([@matt-edmondson](https://github.com/matt-edmondson))
- Add unit tests for ForceDpiAware and ImGuiApp functionality ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor ImGuiApp startup process by consolidating window initialization and event handler setup into dedicated methods. This improves code organization and readability while maintaining functionality. ([@matt-edmondson](https://github.com/matt-edmondson))
- Update copyright headers in ImGuiApp files to reflect ownership and licensing. Ensure consistent formatting across multiple files, enhancing clarity and compliance with licensing requirements. ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.12.5-pre.5 (prerelease)

Changes since v1.12.5-pre.4:

- Sync .runsettings ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync .editorconfig ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync .github\workflows\dotnet.yml ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v1.12.5-pre.4 (prerelease)

Changes since v1.12.5-pre.3:

- Bump ktsu.ScopedAction from 1.1.0 to 1.1.1 in the ktsu group ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.12.5-pre.3 (prerelease)

Changes since v1.12.5-pre.2:

- Bump System.Text.Json from 9.0.3 to 9.0.4 in the system group ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Bump Microsoft.DotNet.ILCompiler in the microsoft group ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.12.5-pre.2 (prerelease)

Changes since v1.12.5-pre.1:

- Sync .editorconfig ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v1.12.5-pre.1 (prerelease)

Incremental prerelease update.
## v1.12.4 (patch)

Changes since v1.12.3:

- Update .gitignore to include additional IDE and OS-specific files ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.12.3 (patch)

Changes since v1.12.2:

- Update packages ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.12.2 (patch)

Changes since v1.12.1:

- Update build scripts ([@matt-edmondson](https://github.com/matt-edmondson))
- Add LICENSE template ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.12.1 (patch)

Changes since v1.12.0:

- Update packages ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.12.0 (minor)

Changes since v1.11.0:

- Try a different method of memory marshaling and reorder some thigns to try fix the font crash ([@matt-edmondson](https://github.com/matt-edmondson))
- Throw an exception if the font is not ready yet for some reason ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.11.1-pre.1 (prerelease)

Changes since v1.11.0:

- Update README.md ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.11.0 (minor)

Changes since v1.10.0:

- Replace the Silk.NET window invoker with ktsu.Invoker ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.10.0 (minor)

Changes since v1.9.0:

- Font system cleanup ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.9.0 (minor)

Changes since v1.8.0:

- Add an overload to ImGuiController.Texture to allow specifying the pixel format ([@matt-edmondson](https://github.com/matt-edmondson))
- Re-add icon to fix LFS ([@matt-edmondson](https://github.com/matt-edmondson))
- Add an image to the demo app to test the texture upload ([@matt-edmondson](https://github.com/matt-edmondson))
- Reuse the texture upload from ImGuiController to remove code duplication ([@matt-edmondson](https://github.com/matt-edmondson))
- Remove icon to fix LFS ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.8.1 (patch)

Changes since v1.8.0:

- Re-add icon to fix LFS ([@matt-edmondson](https://github.com/matt-edmondson))
- Remove icon to fix LFS ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.8.0 (minor)

Changes since v1.7.0:

- Enhance documentation ([@matt-edmondson](https://github.com/matt-edmondson))
- Add constructors and documentation to FontAppearance class ([@matt-edmondson](https://github.com/matt-edmondson))
- Renamed several classes and moved them to their own source files ([@matt-edmondson](https://github.com/matt-edmondson))
- Update license links in README.md ([@matt-edmondson](https://github.com/matt-edmondson))
- Refactor font loading to use unmanaged memory ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.7.1 (patch)

Changes since v1.7.0:

- Enhance documentation ([@matt-edmondson](https://github.com/matt-edmondson))
- Add constructors and documentation to FontAppearance class ([@matt-edmondson](https://github.com/matt-edmondson))
- Renamed several classes and moved them to their own source files ([@matt-edmondson](https://github.com/matt-edmondson))
- Update license links in README.md ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.7.0 (minor)

Changes since v1.6.0:

- Add better font management to ImGuiApp and icon to demo project ([@matt-edmondson](https://github.com/matt-edmondson))
- Fix crash on shutdown when imgui would try to free memory owned by dotnet ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.6.1-pre.1 (prerelease)

Changes since v1.6.0:

- Fix crash on shutdown when imgui would try to free memory owned by dotnet ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.6.0 (minor)

Changes since v1.5.0:

- Remove unnesscessary threading protections that were speculative fixes for the font ownership crash ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.5.1-pre.1 (prerelease)

Changes since v1.5.0:

- Bump System.Text.Json from 9.0.2 to 9.0.3 in the system group ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Bump Microsoft.DotNet.ILCompiler in the microsoft group ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.5.0 (minor)

Changes since v1.4.0:

- Refactor ImGuiApp and related classes for thread safety ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.4.1-pre.3 (prerelease)

Changes since v1.4.1-pre.2:

- Bump SixLabors.ImageSharp from 3.1.6 to 3.1.7 ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.4.1-pre.2 (prerelease)

Changes since v1.4.1-pre.1:

- Bump ktsu.StrongPaths from 1.1.50 to 1.2.0 in the ktsu group ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.4.1-pre.1 (prerelease)

Changes since v1.4.0:

- Bump ktsu.ScopedAction from 1.0.14 to 1.0.15 in the ktsu group ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.4.0 (minor)

Changes since v1.3.0:

- Apply new editorconfig ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.3.1-pre.1 (prerelease)

Changes since v1.3.0:

- Bump System.Text.Json from 9.0.1 to 9.0.2 in the system group ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Bump Microsoft.DotNet.ILCompiler in the microsoft group ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.3.0 (minor)

Changes since v1.2.0:

- Always call ImGui.End for ImGui.Begin ([@Damon3000s](https://github.com/Damon3000s))
## v1.2.1-pre.1 (prerelease)

Changes since v1.2.0:

- Sync scripts\make-version.ps1 ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync scripts\make-changelog.ps1 ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v1.2.0 (minor)

Changes since v1.1.0:

- Enable debug tracing in versioning and changelog scripts; handle empty tag scenarios ([@matt-edmondson](https://github.com/matt-edmondson))
- Replace deprecated ImGui enum value ([@Damon3000s](https://github.com/Damon3000s))
- Call EndChild instead of just End ([@Damon3000s](https://github.com/Damon3000s))
- Update files from Resources.resx ([@Damon3000s](https://github.com/Damon3000s))
## v1.1.0 (minor)

Changes since v1.0.0:

- Fix version script to exclude merge commits and order logs correctly ([@matt-edmondson](https://github.com/matt-edmondson))
- Replace LICENSE file with LICENSE.md and update copyright information ([@matt-edmondson](https://github.com/matt-edmondson))
- Renamed metadata files ([@matt-edmondson](https://github.com/matt-edmondson))
- Fix mouse wheel scrolling and improve API usage ([@Damon3000s](https://github.com/Damon3000s))
- Remove spelling check ignore comments in ImGuiApp files ([@matt-edmondson](https://github.com/matt-edmondson))
- Add Silk.NET packages for enhanced graphics and input support ([@matt-edmondson](https://github.com/matt-edmondson))
- Add conditional compilation for contextLock in ImGuiController ([@matt-edmondson](https://github.com/matt-edmondson))
- Add automation scripts for metadata and version management ([@matt-edmondson](https://github.com/matt-edmondson))
- Review feedback ([@Damon3000s](https://github.com/Damon3000s))

## v1.0.12-pre.11 (prerelease)

Changes since v1.0.12-pre.10:

- Bump ktsu.StrongPaths from 1.1.49 to 1.1.50 in the ktsu group ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.0.12-pre.10 (prerelease)

Changes since v1.0.12-pre.9:

- Fix version script to exclude merge commits and order logs correctly ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.12-pre.9 (prerelease)

Changes since v1.0.12-pre.8:

- Fix mouse wheel scrolling and improve API usage ([@Damon3000s](https://github.com/Damon3000s))
- Review feedback ([@Damon3000s](https://github.com/Damon3000s))
## v1.0.12-pre.8 (prerelease)

Changes since v1.0.12-pre.7:

- Sync scripts\make-changelog.ps1 ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v1.0.12-pre.7 (prerelease)

Changes since v1.0.12-pre.6:

- Add automation scripts for metadata and version management ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.12-pre.6 (prerelease)

Changes since v1.0.12-pre.5:

- Bump coverlet.collector from 6.0.2 to 6.0.3 ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.0.12-pre.5 (prerelease)

Changes since v1.0.12-pre.4:
## v1.0.12-pre.4 (prerelease)

Changes since v1.0.12-pre.3:
## v1.0.12-pre.3 (prerelease)

Changes since v1.0.12-pre.2:

- Add Silk.NET packages for enhanced graphics and input support ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.12-pre.2 (prerelease)

Changes since v1.0.12-pre.1:

- Sync .github\workflows\dotnet.yml ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v1.0.12-pre.1 (prerelease)

Incremental prerelease update.
## v1.0.11-pre.1 (prerelease)

Changes since v1.0.10-pre.1:

- Sync .github\workflows\dotnet.yml ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync .github\workflows\dotnet.yml ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v1.0.10-pre.1 (prerelease)

Changes since v1.0.9:

- Renamed metadata files ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.9 (patch)

Changes since v1.0.8:

- Sync icon.png ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v1.0.8 (patch)

Changes since v1.0.7:

- Replace LICENSE file with LICENSE.md and update copyright information ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.7 (patch)

Changes since v1.0.6:

- Bump ktsu.StrongPaths from 1.1.40 to 1.1.41 in the ktsu group ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.0.6 (patch)

Changes since v1.0.5:

- Bump the ktsu group with 2 updates ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.0.5 (patch)

Changes since v1.0.4:

- Sync .github\workflows\dotnet.yml ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v1.0.4 (patch)

Changes since v1.0.3:

- Add conditional compilation for contextLock in ImGuiController ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.3 (patch)

Changes since v1.0.2:
## v1.0.2 (patch)

Changes since v1.0.1:

- Bump ktsu.StrongPaths from 1.1.35 to 1.1.36 in the ktsu group ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.0.1 (patch)

Changes since v1.0.0:

- Bump ktsu.ScopedAction from 1.0.0 to 1.0.1 in the ktsu group ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.0.0-alpha.76 (prerelease)

Changes since v1.0.0-alpha.75:

- Bump ktsu.StrongPaths from 1.1.34 to 1.1.35 in the ktsu group ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Update VERSION to 1.0.0-alpha.76 ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.0-alpha.75 (prerelease)

Changes since v1.0.0-alpha.74:

- Bump ktsu.StrongPaths from 1.1.33 to 1.1.34 in the ktsu group ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Update VERSION to 1.0.0-alpha.75 ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.0-alpha.74 (prerelease)

Changes since v1.0.0-alpha.73:

- Bump MSTest.TestFramework from 3.6.3 to 3.6.4 ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Update VERSION to 1.0.0-alpha.74 ([@matt-edmondson](https://github.com/matt-edmondson))
- Bump ktsu.ScopedAction from 1.0.0-alpha.22 to 1.0.0 in the ktsu group ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Bump MSTest.TestAdapter from 3.6.3 to 3.6.4 ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.0.0-alpha.73 (prerelease)

Changes since v1.0.0-alpha.72:

- Update VERSION to 1.0.0-alpha.73 ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.0-alpha.72 (prerelease)

Changes since v1.0.0-alpha.71:

- Update VERSION to 1.0.0-alpha.72 ([@matt-edmondson](https://github.com/matt-edmondson))
- Sync Directory.Build.targets ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v1.0.0-alpha.71 (prerelease)

Changes since v1.0.0-alpha.70:

- Bump ktsu.StrongPaths from 1.1.29 to 1.1.30 ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.0-alpha.70 (prerelease)

Changes since v1.0.0-alpha.69:

- Bump ktsu.StrongPaths from 1.1.25 to 1.1.26 in the ktsu group ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Update VERSION to 1.0.0-alpha.70 ([@matt-edmondson](https://github.com/matt-edmondson))
- Sync Directory.Build.props ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v1.0.0-alpha.69 (prerelease)

Changes since v1.0.0-alpha.68:

- Update VERSION to 1.0.0-alpha.69 ([@matt-edmondson](https://github.com/matt-edmondson))
- Bump ktsu.StrongPaths from 1.1.24 to 1.1.25 in the ktsu group ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.0.0-alpha.68 (prerelease)

Changes since v1.0.0-alpha.67:

- Bump the ktsu group with 2 updates ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Update VERSION to 1.0.0-alpha.68 ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.0-alpha.67 (prerelease)

Changes since v1.0.0-alpha.66:

- Sync Directory.Build.props ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Update VERSION to 1.0.0-alpha.67 ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.0-alpha.66 (prerelease)

Changes since v1.0.0-alpha.65:

- Update VERSION to 1.0.0-alpha.66 ([@matt-edmondson](https://github.com/matt-edmondson))
- Bump ktsu.ScopedAction in the ktsu group ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Bump System.Text.Json from 8.0.5 to 9.0.0 in the system group ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.0.0-alpha.65 (prerelease)

Changes since v1.0.0-alpha.64:

- More detailed exception message in TranslateInputKeyToImGuiKey ([@Damon3000s](https://github.com/Damon3000s))
- Set System.Text.Json to 8.0.5 ([@Damon3000s](https://github.com/Damon3000s))
## v1.0.0-alpha.64 (prerelease)

Changes since v1.0.0-alpha.63:

- Bump ktsu.StrongPaths from 1.1.22 to 1.1.23 in the ktsu group ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Update VERSION to 1.0.0-alpha.64 ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.0-alpha.63 (prerelease)

Changes since v1.0.0-alpha.62:

- Update VERSION to 1.0.0-alpha.63 ([@matt-edmondson](https://github.com/matt-edmondson))
- Bump ktsu.StrongPaths from 1.1.21 to 1.1.22 in the ktsu group ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.0.0-alpha.62 (prerelease)

Changes since v1.0.0-alpha.61:

- Bump the ktsu group with 2 updates ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Update VERSION to 1.0.0-alpha.62 ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.0-alpha.61 (prerelease)

Changes since v1.0.0-alpha.60:

- Update VERSION to 1.0.0-alpha.61 ([@matt-edmondson](https://github.com/matt-edmondson))
- Bump MSTest.TestFramework from 3.6.2 to 3.6.3 ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Bump MSTest.TestAdapter from 3.6.2 to 3.6.3 ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.0.0-alpha.60 (prerelease)

Changes since v1.0.0-alpha.59:

- Bump Silk.NET from 2.21.0 to 2.22.0 ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Update VERSION to 1.0.0-alpha.60 ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.0-alpha.59 (prerelease)

Changes since v1.0.0-alpha.58:

- Bump Silk.NET.Input.Extensions from 2.21.0 to 2.22.0 in the silk group ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Update VERSION to 1.0.0-alpha.59 ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.0-alpha.58 (prerelease)

Changes since v1.0.0-alpha.57:

- Update VERSION to 1.0.0-alpha.58 ([@matt-edmondson](https://github.com/matt-edmondson))
- Bump ktsu.StrongPaths from 1.1.19 to 1.1.20 in the ktsu group ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.0.0-alpha.57 (prerelease)

Changes since v1.0.0-alpha.56:

- Update VERSION to 1.0.0-alpha.57 ([@matt-edmondson](https://github.com/matt-edmondson))
- Bump ktsu.StrongPaths from 1.1.18 to 1.1.19 in the ktsu group ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.0.0-alpha.56 (prerelease)

Changes since v1.0.0-alpha.55:

- Bump the ktsu group with 2 updates ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Update VERSION to 1.0.0-alpha.56 ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.0-alpha.55 (prerelease)

Changes since v1.0.0-alpha.54:

- Bump MSTest.TestAdapter from 3.6.1 to 3.6.2 ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Bump MSTest.TestFramework from 3.6.1 to 3.6.2 ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Update VERSION to 1.0.0-alpha.55 ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.0-alpha.54 (prerelease)

Changes since v1.0.0-alpha.53:

- Update VERSION to 1.0.0-alpha.54 ([@matt-edmondson](https://github.com/matt-edmondson))
- Bump ktsu.StrongPaths from 1.1.16 to 1.1.17 in the ktsu group ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.0.0-alpha.53 (prerelease)

Changes since v1.0.0-alpha.52:

- Update VERSION to 1.0.0-alpha.53 ([@matt-edmondson](https://github.com/matt-edmondson))
- Bump ktsu.StrongPaths from 1.1.15 to 1.1.16 in the ktsu group ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.0.0-alpha.52 (prerelease)

Changes since v1.0.0-alpha.51:

- Update VERSION to 1.0.0-alpha.52 ([@matt-edmondson](https://github.com/matt-edmondson))
- Bump ktsu.StrongPaths from 1.1.14 to 1.1.15 in the ktsu group ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.0.0-alpha.51 (prerelease)

Changes since v1.0.0-alpha.50:

- Bump the ktsu group with 2 updates ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Bump MSTest.TestAdapter from 3.6.0 to 3.6.1 ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Update VERSION to 1.0.0-alpha.51 ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.0-alpha.50 (prerelease)

Changes since v1.0.0-alpha.49:

- Bump MSTest.TestFramework from 3.6.0 to 3.6.1 ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Update VERSION to 1.0.0-alpha.50 ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.0-alpha.49 (prerelease)

Changes since v1.0.0-alpha.48:

- Bump ktsu.StrongPaths from 1.1.12 to 1.1.13 in the ktsu group ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Update VERSION to 1.0.0-alpha.49 ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.0-alpha.48 (prerelease)

Changes since v1.0.0-alpha.47:

- Update VERSION to 1.0.0-alpha.48 ([@matt-edmondson](https://github.com/matt-edmondson))
- Bump ktsu.StrongPaths from 1.1.11 to 1.1.12 in the ktsu group ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.0.0-alpha.47 (prerelease)

Changes since v1.0.0-alpha.46:

- Bump the ktsu group with 2 updates ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Update VERSION to 1.0.0-alpha.47 ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.0-alpha.46 (prerelease)

Changes since v1.0.0-alpha.45:

- Sync .github\workflows\dotnet.yml ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Update VERSION to 1.0.0-alpha.46 ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.0-alpha.45 (prerelease)

Changes since v1.0.0-alpha.44:

- Update VERSION to 1.0.0-alpha.45 ([@matt-edmondson](https://github.com/matt-edmondson))
- Bump the ktsu group with 2 updates ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.0.0-alpha.44 (prerelease)

Changes since v1.0.0-alpha.43:

- Update VERSION to 1.0.0-alpha.44 ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.0-alpha.43 (prerelease)

Changes since v1.0.0-alpha.42:

- Update VERSION to 1.0.0-alpha.43 ([@matt-edmondson](https://github.com/matt-edmondson))
- Bump the all group with 2 updates ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Bump ktsu.StrongPaths from 1.1.3 to 1.1.6 ([@dependabot[bot]](https://github.com/dependabot[bot]))
- Sync .github\workflows\dotnet.yml ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync .github\dependabot.yml ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Sync .github\dependabot.yml ([@ktsu[bot]](https://github.com/ktsu[bot]))
- Bump ktsu.ScopedAction from 1.0.0-alpha.6 to 1.0.0-alpha.9 ([@dependabot[bot]](https://github.com/dependabot[bot]))
## v1.0.0-alpha.42 (prerelease)

Changes since v1.0.0-alpha.41:

- Sync .github\workflows\dependabot-merge.yml ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v1.0.0-alpha.41 (prerelease)

Changes since v1.0.0-alpha.40:

- Sync .github\workflows\dependabot-merge.yml ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v1.0.0-alpha.40 (prerelease)

Changes since v1.0.0-alpha.39:

- Sync .github\workflows\dependabot-merge.yml ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v1.0.0-alpha.39 (prerelease)

Changes since v1.0.0-alpha.38:

- Sync .github\workflows\dotnet.yml ([@ktsu[bot]](https://github.com/ktsu[bot]))
## v1.0.0-alpha.38 (prerelease)

Changes since v1.0.0-alpha.37:

- Copy ImGuiController from Silk.NET ([@matt-edmondson](https://github.com/matt-edmondson))
- Update ImGui.NET ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.0-alpha.37 (prerelease)

Changes since v1.0.0-alpha.36:

- Sync workflows ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.0-alpha.36 (prerelease)

Changes since v1.0.0-alpha.15:

- Destroy and clear fonts on shutdown before the imgui context gets destroyed ([@matt-edmondson](https://github.com/matt-edmondson))
- Implement net6 DllImports ([@matt-edmondson](https://github.com/matt-edmondson))
- Separate application update and render so we dont render when the application is minimized ([@matt-edmondson](https://github.com/matt-edmondson))
- Reduce background tick rates ([@matt-edmondson](https://github.com/matt-edmondson))
- Add medium font ([@matt-edmondson](https://github.com/matt-edmondson))
- Take a lock during the closing delegate ([@matt-edmondson](https://github.com/matt-edmondson))
- Minor code style changes ([@matt-edmondson](https://github.com/matt-edmondson))
- Add frame limiting ([@matt-edmondson](https://github.com/matt-edmondson))
- Update VERSION ([@matt-edmondson](https://github.com/matt-edmondson))
- Expose the Scale Factor to the public api so that clients can scale their custom things accordingly ([@matt-edmondson](https://github.com/matt-edmondson))
- Migrate ktsu.io to ktsu namespace ([@matt-edmondson](https://github.com/matt-edmondson))
- Add the ability to set the window icon ([@matt-edmondson](https://github.com/matt-edmondson))
- Added methods for loading textures ([@matt-edmondson](https://github.com/matt-edmondson))
- Add dpi dependent content scaling ([@matt-edmondson](https://github.com/matt-edmondson))
- Reposition the window if it becomes completely offscreen ([@matt-edmondson](https://github.com/matt-edmondson))
- Update VERSION ([@matt-edmondson](https://github.com/matt-edmondson))
- Add a method to convert ems to pixels based on the current font size ([@matt-edmondson](https://github.com/matt-edmondson))
- Use a concurrent dictionary for textures ([@matt-edmondson](https://github.com/matt-edmondson))
- Migrate ktsu.io to ktsu namespace ([@matt-edmondson](https://github.com/matt-edmondson))
- Also dont render if the window is not visible ([@matt-edmondson](https://github.com/matt-edmondson))
- Rollback net6 imports ([@matt-edmondson](https://github.com/matt-edmondson))
- Add more locks for GL ([@matt-edmondson](https://github.com/matt-edmondson))
- Tell imgui were responsible for owning the font atlas data and then free it ourselves on shutdown ([@matt-edmondson](https://github.com/matt-edmondson))
- Speculative fix for crash on shutdown ([@matt-edmondson](https://github.com/matt-edmondson))
## v1.0.0-alpha.15 (prerelease)

Incremental prerelease update.
## v1.0.0 (major)

- Add Stop() method to quit the application ([@matt-edmondson](https://github.com/matt-edmondson))
- Destroy and clear fonts on shutdown before the imgui context gets destroyed ([@matt-edmondson](https://github.com/matt-edmondson))
- Implement net6 DllImports ([@matt-edmondson](https://github.com/matt-edmondson))
- Separate application update and render so we dont render when the application is minimized ([@matt-edmondson](https://github.com/matt-edmondson))
- Reduce background tick rates ([@matt-edmondson](https://github.com/matt-edmondson))
- Update LICENSE ([@matt-edmondson](https://github.com/matt-edmondson))
- Update nuget.config ([@matt-edmondson](https://github.com/matt-edmondson))
- Update build config ([@matt-edmondson](https://github.com/matt-edmondson))
- Avoid double upload of symbols package ([@matt-edmondson](https://github.com/matt-edmondson))
- Remove windows only flags and migrate to nested Directory.Build.props ([@matt-edmondson](https://github.com/matt-edmondson))
- Add medium font ([@matt-edmondson](https://github.com/matt-edmondson))
- Take a lock during the closing delegate ([@matt-edmondson](https://github.com/matt-edmondson))
- Fix namespacing to include ImGuiApp and make it filescoped and apply code style rules ([@matt-edmondson](https://github.com/matt-edmondson))
- Add an onStart delegate ([@matt-edmondson](https://github.com/matt-edmondson))
- Update LICENSE ([@matt-edmondson](https://github.com/matt-edmondson))
- Copy ImGuiController from Silk.NET ([@matt-edmondson](https://github.com/matt-edmondson))
- Update Directory.Build.props ([@matt-edmondson](https://github.com/matt-edmondson))
- Initial commit ([@matt-edmondson](https://github.com/matt-edmondson))
- Sync workflows ([@matt-edmondson](https://github.com/matt-edmondson))
- Dont try to push packages when building pull requests ([@matt-edmondson](https://github.com/matt-edmondson))
- Update build actions ([@matt-edmondson](https://github.com/matt-edmondson))
- Minor code style changes ([@matt-edmondson](https://github.com/matt-edmondson))
- Reformat ImGuiController instantiation for readability ([@matt-edmondson](https://github.com/matt-edmondson))
- Update Directory.Build.targets ([@matt-edmondson](https://github.com/matt-edmondson))
- Create VERSION ([@matt-edmondson](https://github.com/matt-edmondson))
- Update Directory.Build.targets ([@matt-edmondson](https://github.com/matt-edmondson))
- Add frame limiting ([@matt-edmondson](https://github.com/matt-edmondson))
- Update VERSION ([@matt-edmondson](https://github.com/matt-edmondson))
- Expose the Scale Factor to the public api so that clients can scale their custom things accordingly ([@matt-edmondson](https://github.com/matt-edmondson))
- Migrate ktsu.io to ktsu namespace ([@matt-edmondson](https://github.com/matt-edmondson))
- Roll back code style changes to a working version ([@matt-edmondson](https://github.com/matt-edmondson))
- Add the ability to set the window icon ([@matt-edmondson](https://github.com/matt-edmondson))
- Added methods for loading textures ([@matt-edmondson](https://github.com/matt-edmondson))
- Add dpi dependent content scaling ([@matt-edmondson](https://github.com/matt-edmondson))
- Reposition the window if it becomes completely offscreen ([@matt-edmondson](https://github.com/matt-edmondson))
- Consume the resize and move events from silk to actually call the resize delegate ([@matt-edmondson](https://github.com/matt-edmondson))
- More detailed exception message in TranslateInputKeyToImGuiKey ([@Damon3000s](https://github.com/Damon3000s))
- Cleanup and reduce code duplication ([@matt-edmondson](https://github.com/matt-edmondson))
- Update project properties to enable windows targeting to allow building on linux build servers ([@matt-edmondson](https://github.com/matt-edmondson))
- Update VERSION ([@matt-edmondson](https://github.com/matt-edmondson))
- Update ImGui.NET ([@matt-edmondson](https://github.com/matt-edmondson))
- Migrate to Silk.NET for windowing/graphics backend ([@matt-edmondson](https://github.com/matt-edmondson))
- Set System.Text.Json to 8.0.5 ([@Damon3000s](https://github.com/Damon3000s))
- Add action to publish to nuget ([@matt-edmondson](https://github.com/matt-edmondson))
- Bump ktsu.StrongPaths from 1.1.29 to 1.1.30 ([@matt-edmondson](https://github.com/matt-edmondson))
- Remove DividerContainers and zones and Extensions which have been moved to their own respective libraries ([@matt-edmondson](https://github.com/matt-edmondson))
- Enable sourcelink ([@matt-edmondson](https://github.com/matt-edmondson))
- Create dependabot.yml ([@matt-edmondson](https://github.com/matt-edmondson))
- Fix issue with OnStart being invoked too early ([@matt-edmondson](https://github.com/matt-edmondson))
- Prevent drawing an extra border around the main window ([@matt-edmondson](https://github.com/matt-edmondson))
- Add a method to convert ems to pixels based on the current font size ([@matt-edmondson](https://github.com/matt-edmondson))
- Update nuget.config ([@matt-edmondson](https://github.com/matt-edmondson))
- Read from VERSION when building ([@matt-edmondson](https://github.com/matt-edmondson))
- Use a concurrent dictionary for textures ([@matt-edmondson](https://github.com/matt-edmondson))
- Migrate ktsu.io to ktsu namespace ([@matt-edmondson](https://github.com/matt-edmondson))
- #1 Track the state of the window when its in the Normal state ([@matt-edmondson](https://github.com/matt-edmondson))
- Fix a crash calling an ImGui style too early in the initialization ([@matt-edmondson](https://github.com/matt-edmondson))
- Update dotnet.yml ([@matt-edmondson](https://github.com/matt-edmondson))
- Add methods to allow saving and restoring divider states in bulk and add a delegate that gets called when a zone is resized ([@matt-edmondson](https://github.com/matt-edmondson))
- Also dont render if the window is not visible ([@matt-edmondson](https://github.com/matt-edmondson))
- Move OnStart callsite to a place where font loading works with Silk.NET, and add a demo project ([@matt-edmondson](https://github.com/matt-edmondson))
- Read PackageDescription from DESCRIPTION file ([@matt-edmondson](https://github.com/matt-edmondson))
- Assign dependabot PRs to matt ([@matt-edmondson](https://github.com/matt-edmondson))
- Rollback net6 imports ([@matt-edmondson](https://github.com/matt-edmondson))
- Cleanup and force demo window open for debug ([@matt-edmondson](https://github.com/matt-edmondson))
- Add github package support ([@matt-edmondson](https://github.com/matt-edmondson))
- Migrate demo to net8 ([@matt-edmondson](https://github.com/matt-edmondson))
- Update dotnet.yml ([@matt-edmondson](https://github.com/matt-edmondson))
- Add more locks for GL ([@matt-edmondson](https://github.com/matt-edmondson))
- Add a property for getting the current window state from the app and correctly set the position and layout from the initial window state ([@matt-edmondson](https://github.com/matt-edmondson))
- Update dotnet.yml ([@matt-edmondson](https://github.com/matt-edmondson))
- v1.0.0-alpha.9 ([@matt-edmondson](https://github.com/matt-edmondson))
- Documentation and warning fixes ([@matt-edmondson](https://github.com/matt-edmondson))
- Fix build and remove obsolete files and settings ([@matt-edmondson](https://github.com/matt-edmondson))
- Add divider widgets to ImGuiApp ([@matt-edmondson](https://github.com/matt-edmondson))
- Read from AUTHORS file during build ([@matt-edmondson](https://github.com/matt-edmondson))
- Tell imgui were responsible for owning the font atlas data and then free it ourselves on shutdown ([@matt-edmondson](https://github.com/matt-edmondson))
- Speculative fix for crash on shutdown ([@matt-edmondson](https://github.com/matt-edmondson))
