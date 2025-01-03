## v1.0.12 (patch)

Changes since v1.0.11-pre.1:

- Add Silk.NET packages for enhanced graphics and input support ([@matt-edmondson](https://github.com/matt-edmondson))

## v1.0.10-pre.1 (patch)

Changes since v1.0.9:

- Renamed metadata files ([@matt-edmondson](https://github.com/matt-edmondson))

## v1.0.8 (patch)

Changes since v1.0.7:

- Replace LICENSE file with LICENSE.md and update copyright information ([@matt-edmondson](https://github.com/matt-edmondson))

## v1.0.4 (patch)

Changes since v1.0.3:

- Add conditional compilation for contextLock in ImGuiController ([@matt-edmondson](https://github.com/matt-edmondson))

## v1.0.0 (major)

Changes since 0.0.0.0:

- #1 Track the state of the window when its in the Normal state ([@matt-edmondson](https://github.com/matt-edmondson))
- Add a method to convert ems to pixels based on the current font size ([@matt-edmondson](https://github.com/matt-edmondson))
- Add a property for getting the current window state from the app and correctly set the position and layout from the initial window state ([@matt-edmondson](https://github.com/matt-edmondson))
- Add action to publish to nuget ([@matt-edmondson](https://github.com/matt-edmondson))
- Add an onStart delegate ([@matt-edmondson](https://github.com/matt-edmondson))
- Add divider widgets to ImGuiApp ([@matt-edmondson](https://github.com/matt-edmondson))
- Add dpi dependent content scaling ([@matt-edmondson](https://github.com/matt-edmondson))
- Add frame limiting ([@matt-edmondson](https://github.com/matt-edmondson))
- Add github package support ([@matt-edmondson](https://github.com/matt-edmondson))
- Add medium font ([@matt-edmondson](https://github.com/matt-edmondson))
- Add methods to allow saving and restoring divider states in bulk and add a delegate that gets called when a zone is resized ([@matt-edmondson](https://github.com/matt-edmondson))
- Add more locks for GL ([@matt-edmondson](https://github.com/matt-edmondson))
- Add Stop() method to quit the application ([@matt-edmondson](https://github.com/matt-edmondson))
- Add the ability to set the window icon ([@matt-edmondson](https://github.com/matt-edmondson))
- Added methods for loading textures ([@matt-edmondson](https://github.com/matt-edmondson))
- Also dont render if the window is not visible ([@matt-edmondson](https://github.com/matt-edmondson))
- Assign dependabot PRs to matt ([@matt-edmondson](https://github.com/matt-edmondson))
- Avoid double upload of symbols package ([@matt-edmondson](https://github.com/matt-edmondson))
- Bump ktsu.StrongPaths from 1.1.29 to 1.1.30 ([@matt-edmondson](https://github.com/matt-edmondson))
- Cleanup and force demo window open for debug ([@matt-edmondson](https://github.com/matt-edmondson))
- Cleanup and reduce code duplication ([@matt-edmondson](https://github.com/matt-edmondson))
- Consume the resize and move events from silk to actually call the resize delegate ([@matt-edmondson](https://github.com/matt-edmondson))
- Copy ImGuiController from Silk.NET ([@matt-edmondson](https://github.com/matt-edmondson))
- Create dependabot.yml ([@matt-edmondson](https://github.com/matt-edmondson))
- Create VERSION ([@matt-edmondson](https://github.com/matt-edmondson))
- Destroy and clear fonts on shutdown before the imgui context gets destroyed ([@matt-edmondson](https://github.com/matt-edmondson))
- Documentation and warning fixes ([@matt-edmondson](https://github.com/matt-edmondson))
- Dont try to push packages when building pull requests ([@matt-edmondson](https://github.com/matt-edmondson))
- Enable sourcelink ([@matt-edmondson](https://github.com/matt-edmondson))
- Expose the Scale Factor to the public api so that clients can scale their custom things accordingly ([@matt-edmondson](https://github.com/matt-edmondson))
- Fix a crash calling an ImGui style too early in the initialization ([@matt-edmondson](https://github.com/matt-edmondson))
- Fix build and remove obsolete files and settings ([@matt-edmondson](https://github.com/matt-edmondson))
- Fix issue with OnStart being invoked too early ([@matt-edmondson](https://github.com/matt-edmondson))
- Fix namespacing to include ImGuiApp and make it filescoped and apply code style rules ([@matt-edmondson](https://github.com/matt-edmondson))
- Implement net6 DllImports ([@matt-edmondson](https://github.com/matt-edmondson))
- Initial commit ([@matt-edmondson](https://github.com/matt-edmondson))
- Migrate demo to net8 ([@matt-edmondson](https://github.com/matt-edmondson))
- Migrate ktsu.io to ktsu namespace ([@matt-edmondson](https://github.com/matt-edmondson))
- Migrate to Silk.NET for windowing/graphics backend ([@matt-edmondson](https://github.com/matt-edmondson))
- Minor code style changes ([@matt-edmondson](https://github.com/matt-edmondson))
- More detailed exception message in TranslateInputKeyToImGuiKey ([@Damon3000s](https://github.com/Damon3000s))
- Move OnStart callsite to a place where font loading works with Silk.NET, and add a demo project ([@matt-edmondson](https://github.com/matt-edmondson))
- Prevent drawing an extra border around the main window ([@matt-edmondson](https://github.com/matt-edmondson))
- Read from AUTHORS file during build ([@matt-edmondson](https://github.com/matt-edmondson))
- Read from VERSION when building ([@matt-edmondson](https://github.com/matt-edmondson))
- Read PackageDescription from DESCRIPTION file ([@matt-edmondson](https://github.com/matt-edmondson))
- Reduce background tick rates ([@matt-edmondson](https://github.com/matt-edmondson))
- Reformat ImGuiController instantiation for readability ([@matt-edmondson](https://github.com/matt-edmondson))
- Remove DividerContainers and zones and Extensions which have been moved to their own respective libraries ([@matt-edmondson](https://github.com/matt-edmondson))
- Remove windows only flags and migrate to nested Directory.Build.props ([@matt-edmondson](https://github.com/matt-edmondson))
- Reposition the window if it becomes completely offscreen ([@matt-edmondson](https://github.com/matt-edmondson))
- Roll back code style changes to a working version ([@matt-edmondson](https://github.com/matt-edmondson))
- Rollback net6 imports ([@matt-edmondson](https://github.com/matt-edmondson))
- Separate application update and render so we dont render when the application is minimized ([@matt-edmondson](https://github.com/matt-edmondson))
- Set System.Text.Json to 8.0.5 ([@Damon3000s](https://github.com/Damon3000s))
- Speculative fix for crash on shutdown ([@matt-edmondson](https://github.com/matt-edmondson))
- Sync workflows ([@matt-edmondson](https://github.com/matt-edmondson))
- Take a lock during the closing delegate ([@matt-edmondson](https://github.com/matt-edmondson))
- Tell imgui were responsible for owning the font atlas data and then free it ourselves on shutdown ([@matt-edmondson](https://github.com/matt-edmondson))
- Update build actions ([@matt-edmondson](https://github.com/matt-edmondson))
- Update build config ([@matt-edmondson](https://github.com/matt-edmondson))
- Update Directory.Build.props ([@matt-edmondson](https://github.com/matt-edmondson))
- Update Directory.Build.targets ([@matt-edmondson](https://github.com/matt-edmondson))
- Update dotnet.yml ([@matt-edmondson](https://github.com/matt-edmondson))
- Update ImGui.NET ([@matt-edmondson](https://github.com/matt-edmondson))
- Update LICENSE ([@matt-edmondson](https://github.com/matt-edmondson))
- Update nuget.config ([@matt-edmondson](https://github.com/matt-edmondson))
- Update project properties to enable windows targeting to allow building on linux build servers ([@matt-edmondson](https://github.com/matt-edmondson))
- Update VERSION ([@matt-edmondson](https://github.com/matt-edmondson))
- Use a concurrent dictionary for textures ([@matt-edmondson](https://github.com/matt-edmondson))
- v1.0.0-alpha.9 ([@matt-edmondson](https://github.com/matt-edmondson))


