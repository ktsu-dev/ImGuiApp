## v1.0.12 (unknown)

Changes since v1.0.12-pre.6:

- Add automation scripts for metadata and version management ([@Matthew Edmondson](https://github.com/Matthew Edmondson))

## v1.0.12 (patch)

Changes since v1.0.11-pre.1:

- Add Silk.NET packages for enhanced graphics and input support ([@Matthew Edmondson](https://github.com/Matthew Edmondson))

## v1.0.10-pre.1 (patch)

Changes since v1.0.9:

- Renamed metadata files ([@Matthew Edmondson](https://github.com/Matthew Edmondson))

## v1.0.8 (patch)

Changes since v1.0.7:

- Replace LICENSE file with LICENSE.md and update copyright information ([@Matthew Edmondson](https://github.com/Matthew Edmondson))

## v1.0.4 (patch)

Changes since v1.0.3:

- Add conditional compilation for contextLock in ImGuiController ([@Matthew Edmondson](https://github.com/Matthew Edmondson))

## v1.0.0 (major)

Changes since 0.0.0.0:

- #1 Track the state of the window when its in the Normal state ([@matt-edmondson](https://github.com/matt-edmondson))
- Add a method to convert ems to pixels based on the current font size ([@Matt Edmondson](https://github.com/Matt Edmondson))
- Add a property for getting the current window state from the app and correctly set the position and layout from the initial window state ([@Matthew Edmondson](https://github.com/Matthew Edmondson))
- Add action to publish to nuget ([@Matt Edmondson](https://github.com/Matt Edmondson))
- Add an onStart delegate ([@matt-edmondson](https://github.com/matt-edmondson))
- Add divider widgets to ImGuiApp ([@matt-edmondson](https://github.com/matt-edmondson))
- Add dpi dependent content scaling ([@Matt Edmondson](https://github.com/Matt Edmondson))
- Add frame limiting ([@Matt Edmondson](https://github.com/Matt Edmondson))
- Add github package support ([@matt-edmondson](https://github.com/matt-edmondson))
- Add medium font ([@Matt Edmondson](https://github.com/Matt Edmondson))
- Add methods to allow saving and restoring divider states in bulk and add a delegate that gets called when a zone is resized ([@matt-edmondson](https://github.com/matt-edmondson))
- Add more locks for GL ([@Matt Edmondson](https://github.com/Matt Edmondson))
- Add Stop() method to quit the application ([@matt-edmondson](https://github.com/matt-edmondson))
- Add the ability to set the window icon ([@Matt Edmondson](https://github.com/Matt Edmondson))
- Added methods for loading textures ([@Matt Edmondson](https://github.com/Matt Edmondson))
- Also dont render if the window is not visible ([@Matt Edmondson](https://github.com/Matt Edmondson))
- Assign dependabot PRs to matt ([@matt-edmondson](https://github.com/matt-edmondson))
- Avoid double upload of symbols package ([@matt-edmondson](https://github.com/matt-edmondson))
- Bump ktsu.StrongPaths from 1.1.29 to 1.1.30 ([@Matthew Edmondson](https://github.com/Matthew Edmondson))
- Cleanup and force demo window open for debug ([@Matthew Edmondson](https://github.com/Matthew Edmondson))
- Cleanup and reduce code duplication ([@Matthew Edmondson](https://github.com/Matthew Edmondson))
- Consume the resize and move events from silk to actually call the resize delegate ([@Matthew Edmondson](https://github.com/Matthew Edmondson))
- Copy ImGuiController from Silk.NET ([@Matt Edmondson](https://github.com/Matt Edmondson))
- Create dependabot.yml ([@Matthew Edmondson](https://github.com/Matthew Edmondson))
- Create VERSION ([@matt-edmondson](https://github.com/matt-edmondson))
- Destroy and clear fonts on shutdown before the imgui context gets destroyed ([@Matt Edmondson](https://github.com/Matt Edmondson))
- Documentation and warning fixes ([@Matthew Edmondson](https://github.com/Matthew Edmondson))
- Dont try to push packages when building pull requests ([@matt-edmondson](https://github.com/matt-edmondson))
- Enable sourcelink ([@matt-edmondson](https://github.com/matt-edmondson))
- Expose the Scale Factor to the public api so that clients can scale their custom things accordingly ([@Matt Edmondson](https://github.com/Matt Edmondson))
- Fix a crash calling an ImGui style too early in the initialization ([@matt-edmondson](https://github.com/matt-edmondson))
- Fix build and remove obsolete files and settings ([@matt-edmondson](https://github.com/matt-edmondson))
- Fix issue with OnStart being invoked too early ([@matt-edmondson](https://github.com/matt-edmondson))
- Fix namespacing to include ImGuiApp and make it filescoped and apply code style rules ([@matt-edmondson](https://github.com/matt-edmondson))
- Implement net6 DllImports ([@matt-edmondson](https://github.com/matt-edmondson))
- Initial commit ([@matt-edmondson](https://github.com/matt-edmondson))
- Migrate demo to net8 ([@matt-edmondson](https://github.com/matt-edmondson))
- Migrate ktsu.io to ktsu namespace ([@Matt Edmondson](https://github.com/Matt Edmondson))
- Migrate to Silk.NET for windowing/graphics backend ([@matt-edmondson](https://github.com/matt-edmondson))
- Minor code style changes ([@matt-edmondson](https://github.com/matt-edmondson))
- More detailed exception message in TranslateInputKeyToImGuiKey ([@Damon Lewis](https://github.com/Damon Lewis))
- Move OnStart callsite to a place where font loading works with Silk.NET, and add a demo project ([@matt-edmondson](https://github.com/matt-edmondson))
- Prevent drawing an extra border around the main window ([@Matthew Edmondson](https://github.com/Matthew Edmondson))
- Read from AUTHORS file during build ([@matt-edmondson](https://github.com/matt-edmondson))
- Read from VERSION when building ([@matt-edmondson](https://github.com/matt-edmondson))
- Read PackageDescription from DESCRIPTION file ([@matt-edmondson](https://github.com/matt-edmondson))
- Reduce background tick rates ([@Matt Edmondson](https://github.com/Matt Edmondson))
- Reformat ImGuiController instantiation for readability ([@Matthew Edmondson](https://github.com/Matthew Edmondson))
- Remove DividerContainers and zones and Extensions which have been moved to their own respective libraries ([@matt-edmondson](https://github.com/matt-edmondson))
- Remove windows only flags and migrate to nested Directory.Build.props ([@matt-edmondson](https://github.com/matt-edmondson))
- Reposition the window if it becomes completely offscreen ([@Matt Edmondson](https://github.com/Matt Edmondson))
- Roll back code style changes to a working version ([@Matthew Edmondson](https://github.com/Matthew Edmondson))
- Rollback net6 imports ([@matt-edmondson](https://github.com/matt-edmondson))
- Separate application update and render so we dont render when the application is minimized ([@Matt Edmondson](https://github.com/Matt Edmondson))
- Set System.Text.Json to 8.0.5 ([@Damon Lewis](https://github.com/Damon Lewis))
- Speculative fix for crash on shutdown ([@Matt Edmondson](https://github.com/Matt Edmondson))
- Sync workflows ([@Matt Edmondson](https://github.com/Matt Edmondson))
- Take a lock during the closing delegate ([@Matt Edmondson](https://github.com/Matt Edmondson))
- Tell imgui were responsible for owning the font atlas data and then free it ourselves on shutdown ([@Matt Edmondson](https://github.com/Matt Edmondson))
- Update build actions ([@matt-edmondson](https://github.com/matt-edmondson))
- Update build config ([@matt-edmondson](https://github.com/matt-edmondson))
- Update Directory.Build.props ([@matt-edmondson](https://github.com/matt-edmondson))
- Update Directory.Build.targets ([@matt-edmondson](https://github.com/matt-edmondson))
- Update dotnet.yml ([@matt-edmondson](https://github.com/matt-edmondson))
- Update ImGui.NET ([@Matt Edmondson](https://github.com/Matt Edmondson))
- Update LICENSE ([@matt-edmondson](https://github.com/matt-edmondson))
- Update nuget.config ([@matt-edmondson](https://github.com/matt-edmondson))
- Update project properties to enable windows targeting to allow building on linux build servers ([@Matt Edmondson](https://github.com/Matt Edmondson))
- Update VERSION ([@Matt Edmondson](https://github.com/Matt Edmondson))
- Use a concurrent dictionary for textures ([@Matt Edmondson](https://github.com/Matt Edmondson))
- v1.0.0-alpha.9 ([@Matthew Edmondson](https://github.com/Matthew Edmondson))


