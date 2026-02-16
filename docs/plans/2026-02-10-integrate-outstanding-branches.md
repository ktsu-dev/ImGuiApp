# Integrate Outstanding Feature Branches Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Integrate the outstanding work from `hexa-extras` and `node-graph` branches into main, and clean up stale branches.

**Architecture:** The `hexa-extras` branch adds ImGuizmo/ImNodes/ImPlot extension support via a reflection-based `ImGuiExtensionManager` and refactored demo app. The `node-graph` branch builds on top of `hexa-extras` adding a standalone `NodeGraph` attribute library and `ImGuiNodeEditor` UI component. Both branches assume a project restructure (`ImGui.App/` -> `ImGuiApp/`) that conflicts with main's current structure. Instead of merging the branches directly (which would cause massive conflicts due to diverged directory structures), we will cherry-pick the new features and adapt them to main's current project layout.

**Tech Stack:** C#/.NET, Hexa.NET.ImGui ecosystem (ImGuizmo, ImNodes, ImPlot), MSTest, ktsu.Sdk

---

## Branch Analysis Summary

| Branch | Status | Action |
|--------|--------|--------|
| `origin/font-memory-guard` | **Superseded** - All work merged via PRs #144, #154-159 | Delete branch |
| `origin/copilot/sub-pr-132` | **Stale** - Only contains "Initial plan" commit over hexa-extras | Delete branch |
| `origin/copilot/sub-pr-135` | **Superseded** - Subset of font-memory-guard, already merged | Delete branch |
| `origin/copilot/integrate-font-memory-guard` | **Superseded** - Integration attempt, work completed via PR #159 | Delete branch |
| `origin/copilot/integrate-hexa-extras-branch` | **Stale** - Failed integration attempt, "Awaiting Clarification" (PR #150) | Close PR, delete branch |
| `origin/hexa-extras` | **Has valuable work** - ImGuiExtensionManager + demo refactor | Cherry-pick features |
| `origin/node-graph` | **Has valuable work** - NodeGraph + ImGuiNodeEditor on top of hexa-extras | Cherry-pick features |

## Integration Strategy

The feature branches restructured the entire project (renamed dirs, removed sub-libraries). Main still uses the original structure. We will **port the new code into main's structure** rather than merge, because:

1. The restructuring conflicts with ~130 files
2. Main has diverged significantly since these branches were created
3. The new features (ExtensionManager, NodeGraph, demos) are additive and can be ported cleanly
4. Main's multi-library structure (ImGui.App, ImGui.Popups, ImGui.Styler, ImGui.Widgets) should be preserved per project conventions

---

## Task 1: Clean Up Stale Branches

**Goal:** Remove branches whose work has been fully merged or abandoned.

**Step 1: Close the stale draft PR**

Run:
```bash
gh pr close 150 --comment "Closing: hexa-extras integration will be done fresh from main with cherry-picked features."
gh pr close 135 --comment "Closing: FontMemoryGuard was fully integrated via PRs #144, #154-159."
gh pr close 132 --comment "Closing: hexa-extras features will be integrated via a fresh branch from main."
```

Expected: PRs #150, #135, #132 closed.

**Step 2: Delete stale remote branches**

Run:
```bash
git push origin --delete copilot/sub-pr-132
git push origin --delete copilot/sub-pr-135
git push origin --delete copilot/integrate-font-memory-guard
git push origin --delete copilot/integrate-hexa-extras-branch
git push origin --delete font-memory-guard
```

Expected: 5 branches deleted. These are fully superseded.

**Step 3: Commit checkpoint**

No code changes - just branch cleanup. No commit needed.

---

## Task 2: Add Hexa.NET Extension Package References

**Files:**
- Modify: `Directory.Packages.props`

**Step 1: Check current Hexa.NET.ImGui version**

Run:
```bash
grep -i "hexa" Directory.Packages.props
```

Expected: Find the current Hexa.NET.ImGui version to use matching extension versions.

**Step 2: Add package references**

Add to `Directory.Packages.props` in the dependencies section:
```xml
<PackageVersion Include="Hexa.NET.ImGuizmo" Version="<matching-version>" />
<PackageVersion Include="Hexa.NET.ImNodes" Version="<matching-version>" />
<PackageVersion Include="Hexa.NET.ImPlot" Version="<matching-version>" />
```

Use the same version as `Hexa.NET.ImGui` if available, otherwise check NuGet for latest compatible versions.

**Step 3: Verify restore**

Run:
```bash
dotnet restore
```

Expected: Restore succeeds with new packages.

**Step 4: Commit**

```bash
git add Directory.Packages.props
git commit -m "[minor] Add Hexa.NET.ImGuizmo, ImNodes, and ImPlot package references"
```

---

## Task 3: Port ImGuiExtensionManager to ImGui.App

**Files:**
- Create: `ImGui.App/ImGuiExtensionManager.cs`
- Modify: `ImGui.App/ImGui.App.csproj` (add package references if needed)

The `ImGuiExtensionManager` from `hexa-extras` is a self-contained static class that uses reflection to optionally load ImGuizmo, ImNodes, and ImPlot. It needs to be adapted to use the `ktsu.ImGui.App` namespace instead of the restructured namespace.

**Step 1: Extract ImGuiExtensionManager from hexa-extras**

Run:
```bash
git show origin/hexa-extras:ImGuiApp/ImGuiExtensionManager.cs > ImGui.App/ImGuiExtensionManager.cs
```

**Step 2: Fix namespace**

Change the namespace from whatever it uses to `ktsu.ImGui.App` to match the existing codebase.

**Step 3: Add package references to ImGui.App.csproj**

Add `PackageReference` entries for:
- `Hexa.NET.ImGuizmo`
- `Hexa.NET.ImNodes`
- `Hexa.NET.ImPlot`

(Version managed by Directory.Packages.props)

**Step 4: Build and verify**

Run:
```bash
dotnet build ImGui.App/
```

Expected: Build succeeds.

**Step 5: Commit**

```bash
git add ImGui.App/ImGuiExtensionManager.cs ImGui.App/ImGui.App.csproj
git commit -m "[minor] Add ImGuiExtensionManager for optional ImGuizmo, ImNodes, and ImPlot support"
```

---

## Task 4: Integrate ExtensionManager into ImGuiController

**Files:**
- Modify: `ImGui.App/ImGuiController/ImGuiController.cs`

The controller needs to call ExtensionManager at key lifecycle points:
1. `Init()` -> `ImGuiExtensionManager.Initialize()` + `CreateExtensionContexts()`
2. After `ImGui.NewFrame()` -> `ImGuiExtensionManager.BeginFrame()`
3. Context setup -> `ImGuiExtensionManager.SetImGuiContext()`

**Step 1: Read current ImGuiController.cs**

Read `ImGui.App/ImGuiController/ImGuiController.cs` to understand the current Init(), Update(), and context management flow.

**Step 2: Add initialization calls**

In the `Init()` method, after ImGui context creation, add:
```csharp
ImGuiExtensionManager.Initialize();
ImGuiExtensionManager.CreateExtensionContexts();
```

**Step 3: Add per-frame calls**

In the `Update()` method, after `ImGui.NewFrame()`, add:
```csharp
ImGuiExtensionManager.BeginFrame();
```

**Step 4: Add context management**

Where ImGui context is set, also call:
```csharp
ImGuiExtensionManager.SetImGuiContext(imGuiContext);
```

**Step 5: Build and verify**

Run:
```bash
dotnet build ImGui.App/
```

Expected: Build succeeds.

**Step 6: Commit**

```bash
git add ImGui.App/ImGuiController/ImGuiController.cs
git commit -m "[minor] Integrate ImGuiExtensionManager into ImGuiController lifecycle"
```

---

## Task 5: Add ExtensionManager Cleanup to ImGuiApp

**Files:**
- Modify: `ImGui.App/ImGuiApp.cs`

**Step 1: Read current ImGuiApp.cs**

Read `ImGui.App/ImGuiApp.cs` to find the shutdown/cleanup path.

**Step 2: Add cleanup call**

In the shutdown handler (where controller cleanup happens), add:
```csharp
ImGuiExtensionManager.Cleanup();
```

**Step 3: Build and run tests**

Run:
```bash
dotnet build ImGui.App/
dotnet test
```

Expected: Build succeeds, all 261 existing tests pass.

**Step 4: Commit**

```bash
git add ImGui.App/ImGuiApp.cs
git commit -m "[patch] Add ImGuiExtensionManager cleanup on app shutdown"
```

---

## Task 6: Write Tests for ImGuiExtensionManager

**Files:**
- Create: `tests/ImGui.App.Tests/ImGuiExtensionManagerTests.cs`

**Step 1: Write tests**

Test the reflection-based extension manager:
- Test `Initialize()` doesn't throw when extensions aren't loaded
- Test `IsImGuizmoAvailable`, `IsImNodesAvailable`, `IsImPlotAvailable` properties
- Test `Cleanup()` doesn't throw on uninitialized state
- Test `BeginFrame()` doesn't throw when no extensions loaded

**Step 2: Run tests to verify they pass**

Run:
```bash
dotnet test tests/ImGui.App.Tests/
```

Expected: All tests pass (existing + new).

**Step 3: Commit**

```bash
git add tests/ImGui.App.Tests/ImGuiExtensionManagerTests.cs
git commit -m "[minor] Add unit tests for ImGuiExtensionManager"
```

---

## Task 7: Port Demo App Refactor

**Files:**
- Modify: `examples/ImGuiAppDemo/ImGuiAppDemo.cs`
- Create: `examples/ImGuiAppDemo/Demos/IDemoTab.cs`
- Create: `examples/ImGuiAppDemo/Demos/BasicWidgetsDemo.cs`
- Create: `examples/ImGuiAppDemo/Demos/ImGuizmoDemo.cs`
- Create: `examples/ImGuiAppDemo/Demos/ImNodesDemo.cs`
- Create: `examples/ImGuiAppDemo/Demos/ImPlotDemo.cs`
- Create: remaining demo tab files
- Modify: `examples/ImGuiAppDemo/ImGuiAppDemo.csproj`

**Step 1: Extract demo files from hexa-extras**

Run for each demo file:
```bash
mkdir -p examples/ImGuiAppDemo/Demos
git show origin/hexa-extras:ImGuiAppDemo/Demos/IDemoTab.cs > examples/ImGuiAppDemo/Demos/IDemoTab.cs
git show origin/hexa-extras:ImGuiAppDemo/Demos/BasicWidgetsDemo.cs > examples/ImGuiAppDemo/Demos/BasicWidgetsDemo.cs
git show origin/hexa-extras:ImGuiAppDemo/Demos/AdvancedWidgetsDemo.cs > examples/ImGuiAppDemo/Demos/AdvancedWidgetsDemo.cs
git show origin/hexa-extras:ImGuiAppDemo/Demos/AnimationDemo.cs > examples/ImGuiAppDemo/Demos/AnimationDemo.cs
git show origin/hexa-extras:ImGuiAppDemo/Demos/DataVisualizationDemo.cs > examples/ImGuiAppDemo/Demos/DataVisualizationDemo.cs
git show origin/hexa-extras:ImGuiAppDemo/Demos/GraphicsDemo.cs > examples/ImGuiAppDemo/Demos/GraphicsDemo.cs
git show origin/hexa-extras:ImGuiAppDemo/Demos/ImGuizmoDemo.cs > examples/ImGuiAppDemo/Demos/ImGuizmoDemo.cs
git show origin/hexa-extras:ImGuiAppDemo/Demos/ImNodesDemo.cs > examples/ImGuiAppDemo/Demos/ImNodesDemo.cs
git show origin/hexa-extras:ImGuiAppDemo/Demos/ImPlotDemo.cs > examples/ImGuiAppDemo/Demos/ImPlotDemo.cs
git show origin/hexa-extras:ImGuiAppDemo/Demos/InputHandlingDemo.cs > examples/ImGuiAppDemo/Demos/InputHandlingDemo.cs
git show origin/hexa-extras:ImGuiAppDemo/Demos/LayoutDemo.cs > examples/ImGuiAppDemo/Demos/LayoutDemo.cs
git show origin/hexa-extras:ImGuiAppDemo/Demos/NerdFontDemo.cs > examples/ImGuiAppDemo/Demos/NerdFontDemo.cs
git show origin/hexa-extras:ImGuiAppDemo/Demos/UnicodeDemo.cs > examples/ImGuiAppDemo/Demos/UnicodeDemo.cs
git show origin/hexa-extras:ImGuiAppDemo/Demos/UtilityDemo.cs > examples/ImGuiAppDemo/Demos/UtilityDemo.cs
git show origin/hexa-extras:ImGuiAppDemo/ImGuiAppDemo.cs > examples/ImGuiAppDemo/ImGuiAppDemo.cs.new
```

**Step 2: Fix namespaces**

All extracted files will need namespace updates to match main's conventions. Change references from the restructured namespace to the existing `ktsu.ImGui.App` / demo namespace.

**Step 3: Add package references to demo csproj**

Add references to `Hexa.NET.ImGuizmo`, `Hexa.NET.ImNodes`, `Hexa.NET.ImPlot` in the demo project.

**Step 4: Update the main ImGuiAppDemo.cs**

Replace the current monolithic demo with the new tab-based orchestrator that loads all demo tabs.

**Step 5: Build the demo**

Run:
```bash
dotnet build examples/ImGuiAppDemo/
```

Expected: Build succeeds.

**Step 6: Commit**

```bash
git add examples/ImGuiAppDemo/
git commit -m "[minor] Refactor demo app with modular tab-based architecture and extension demos"
```

---

## Task 8: Port NodeGraph Library

**Files:**
- Create: `NodeGraph/` directory with all source files
- Create: `NodeGraph/NodeGraph.csproj`
- Create: `NodeGraph/Library/` subdirectories

The NodeGraph library is UI-agnostic (no ImGui dependency) and defines node graph attributes and types.

**Step 1: Extract NodeGraph from node-graph branch**

Run:
```bash
git show origin/node-graph:NodeGraph/NodeGraph.csproj > /dev/null && echo "exists"
```

Then extract all NodeGraph files:
```bash
mkdir -p NodeGraph/Library/Operations NodeGraph/Library/Primitives NodeGraph/Library/Utilities
git checkout origin/node-graph -- NodeGraph/
```

**Step 2: Fix namespaces if needed**

Verify namespace conventions match the ktsu ecosystem (`ktsu.NodeGraph` or similar).

**Step 3: Add NodeGraph project to solution**

Run:
```bash
dotnet sln ImGui.sln add NodeGraph/NodeGraph.csproj
```

**Step 4: Build**

Run:
```bash
dotnet build NodeGraph/
```

Expected: Build succeeds.

**Step 5: Commit**

```bash
git add NodeGraph/ ImGui.sln
git commit -m "[minor] Add NodeGraph library with attribute-based node definitions"
```

---

## Task 9: Port NodeGraph Tests

**Files:**
- Create: `NodeGraphTests/` directory with all test files
- Create: `NodeGraphTests/NodeGraphTests.csproj`

**Step 1: Extract test files from node-graph branch**

```bash
git checkout origin/node-graph -- NodeGraphTests/
```

**Step 2: Add test project to solution**

```bash
dotnet sln ImGui.sln add NodeGraphTests/NodeGraphTests.csproj
```

**Step 3: Run tests**

Run:
```bash
dotnet test NodeGraphTests/
```

Expected: All NodeGraph tests pass.

**Step 4: Commit**

```bash
git add NodeGraphTests/ ImGui.sln
git commit -m "[minor] Add NodeGraph test suite"
```

---

## Task 10: Port ImGuiNodeEditor

**Files:**
- Create: `ImGuiNodeEditor/` directory with all source files
- Create: `ImGuiNodeEditor/ImGuiNodeEditor.csproj`

**Step 1: Extract ImGuiNodeEditor from node-graph branch**

```bash
git checkout origin/node-graph -- ImGuiNodeEditor/
```

**Step 2: Fix project references**

The ImGuiNodeEditor references both NodeGraph and ImGui.App. Verify the project references point to the correct paths in main's structure.

**Step 3: Add to solution**

```bash
dotnet sln ImGui.sln add ImGuiNodeEditor/ImGuiNodeEditor.csproj
```

**Step 4: Build**

Run:
```bash
dotnet build ImGuiNodeEditor/
```

Expected: Build succeeds.

**Step 5: Commit**

```bash
git add ImGuiNodeEditor/ ImGui.sln
git commit -m "[minor] Add ImGuiNodeEditor with attribute-based node rendering"
```

---

## Task 11: Add CleanImNodesDemo to Demo App

**Files:**
- Create: `examples/ImGuiAppDemo/Demos/CleanImNodesDemo.cs`
- Modify: `examples/ImGuiAppDemo/ImGuiAppDemo.cs` (add tab registration)

**Step 1: Extract CleanImNodesDemo**

```bash
git show origin/node-graph:ImGuiAppDemo/Demos/CleanImNodesDemo.cs > examples/ImGuiAppDemo/Demos/CleanImNodesDemo.cs
```

**Step 2: Fix namespaces and project references**

Update namespace and ensure ImGuiNodeEditor project reference is added to the demo csproj.

**Step 3: Register in main demo orchestrator**

Add `new CleanImNodesDemo()` to the demo tab list in `ImGuiAppDemo.cs`.

**Step 4: Build**

Run:
```bash
dotnet build examples/ImGuiAppDemo/
```

Expected: Build succeeds.

**Step 5: Commit**

```bash
git add examples/ImGuiAppDemo/
git commit -m "[minor] Add CleanImNodesDemo showcasing NodeGraph integration"
```

---

## Task 12: Full Build and Test Verification

**Step 1: Clean build everything**

Run:
```bash
dotnet build --no-incremental
```

Expected: Full solution builds with 0 errors.

**Step 2: Run all tests**

Run:
```bash
dotnet test
```

Expected: All tests pass (existing 261 + new ExtensionManager tests + NodeGraph tests).

**Step 3: Run the demo app manually (if possible)**

Run:
```bash
dotnet run --project examples/ImGuiAppDemo/
```

Verify the app opens and all demo tabs render without crashing.

**Step 4: Commit any fixes**

If any issues found, fix and commit with appropriate messages.

---

## Task 13: Clean Up Remaining Stale Branches

**Step 1: Delete the original feature branches (now integrated)**

Run:
```bash
git push origin --delete hexa-extras
git push origin --delete node-graph
```

Expected: Branches deleted. All their work has been ported to main.

---

## Task 14: Create PR

**Step 1: Create feature branch and push**

```bash
git checkout -b integrate-hexa-extras-and-node-graph
git push -u origin integrate-hexa-extras-and-node-graph
```

**Step 2: Create PR**

```bash
gh pr create --title "Add ImGui extension support (ImGuizmo/ImNodes/ImPlot) and NodeGraph library" --body "$(cat <<'EOF'
## Summary

- Adds `ImGuiExtensionManager` for optional ImGuizmo, ImNodes, and ImPlot support via reflection-based loading
- Ports `NodeGraph` attribute library for UI-agnostic node graph definitions
- Ports `ImGuiNodeEditor` for ImGui-based node graph rendering
- Refactors demo app with modular tab-based architecture including extension demos
- Cleans up 5 stale/superseded remote branches

## Changes

- **ImGui.App**: Added `ImGuiExtensionManager.cs`, integrated into controller lifecycle
- **NodeGraph**: New library with attribute-based node definitions, pin system, and type-safe validation
- **ImGuiNodeEditor**: New library with rendering engine, input handling, and attribute-based node factory
- **Demo App**: Refactored to modular tab system with 14+ demo tabs including ImGuizmo, ImNodes, ImPlot, and CleanImNodes demos
- **Tests**: Added ExtensionManager tests and NodeGraph test suite

## Test plan

- [ ] All existing 261 tests pass
- [ ] New ImGuiExtensionManager tests pass
- [ ] New NodeGraph tests pass
- [ ] Demo app launches and all tabs render
- [ ] Build succeeds on all target frameworks (net8.0, net9.0, net10.0)
EOF
)"
```

Expected: PR created and URL returned.
