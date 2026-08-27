# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
dotnet restore                                    # Restore dependencies
dotnet build                                      # Build solution
dotnet test                                       # Run all tests
dotnet test --filter "FullyQualifiedName~Name"   # Run specific test
dotnet run --project examples/ImGuiAppDemo        # Run main demo
dotnet run --project examples/ImGuiWidgetsDemo    # Run widgets demo
dotnet run --project examples/ImGuiStylerDemo     # Run styler demo
dotnet run --project examples/ImGuiPopupsDemo     # Run popups demo
dotnet build -c Release                           # Build release configuration
```

## Project Structure

This is the **ktsu ImGui Suite**, a collection of .NET libraries for building Dear ImGui applications. The solution (`ImGui.sln`) uses:

- **ktsu.Sdk** - Custom SDK providing shared build configuration
- **MSTest.Sdk** - Test project SDK with Microsoft Testing Platform
- Multi-targeting: `net10.0;net9.0;net8.0` for libraries, `net10.0` for tests

### Libraries

- **ImGui.App** (`ktsu.ImGui.App`) - Application foundation with windowing, rendering, font/texture management, PID frame limiting, DPI awareness
- **ImGui.Widgets** (`ktsu.ImGui.Widgets`) - Custom UI components: TabPanel, Knob, SearchBox, RadialProgressBar, Grid, DividerContainer, Combo, Tree, Icons, ColorIndicator, Text, Image, ScopedDisable, ScopedId. Also thin adapters delegating to `Hexa.NET.ImGui.Widgets`: `Spinner`, `BufferingBar`, `HorizontalSplitter`/`VerticalSplitter`, `ToggleSwitch`/`ToggleButton`/`TransparentButton`/`InlineButton`, `IconTreeNode`, `EnumCombo`, `TextCenteredV`/`TextCenteredH`/`TextCenteredVH`, `ImageCenteredV`/`ImageCenteredH`/`ImageCenteredVH`/`ImageScaleTo`, `Tooltip`, `Breadcrumb`, `DatePicker`/`YearPicker`, `FlameGraph`, `FileTreeView`, `OpenFileDialog`/`SaveFileDialog`/`OpenFolderDialog`, `RenameDialog`, `DialogMessageBox`/`ShowMessageBox`, `DockedWindow`. Several of these deliberately duplicate an existing ktsu widget (`HorizontalSplitter`/`VerticalSplitter` vs `DividerContainer`, `IconTreeNode` vs `Tree`, `ToggleSwitch` vs `Switch`, `BufferingBar`/`Spinner` vs `RadialProgressBar`/`SkeletonLoader`, `EnumCombo` vs `Combo`, `TextCenteredV/H/VH` vs `TextCentered`, `ImageCenteredV/H/VH` vs `ImageCentered`) — both survive on purpose until the "Hexa vs ktsu" comparison tab in `examples/ImGuiWidgetsDemo` settles which to keep. `DatePicker` and `FileTreeView` need a Material Icons font registered via `FontHelper.AddCustomFont(io, data, size, FontHelper.GetMaterialIconRanges(), mergeWithPrevious: true)` (not `ImGuiAppConfig.Fonts`, which applies the Nerd Font mapping); see `examples/ImGuiAppDemo`. `YearPicker` needs no icon font. `OpenFileDialog`, `SaveFileDialog` and `OpenFolderDialog` need the same Material Icons font, for their toolbar, breadcrumb and file-tree glyphs; `RenameDialog`, `DialogMessageBox` and `ShowMessageBox` need none. `DockedWindow` composes Hexa's `ImWindow` internally rather than inheriting it — subclass it, override `Title` and `DrawContent()`, then call `Show()`/`Close()`. All of the dialogs and `DockedWindow` require a per-frame deferred-drawing pump; see [Deferred Drawing](#deferred-drawing-dialogs-and-docked-windows) below. Also includes callback-driven editors: `Sequencer`, `SequenceSource`, `CurveEditor`, `CurveSource`, `CurveData`, `BezierEditor`. Unlike the dialogs above, none of these need a deferred-drawing pump; see [Callback-driven editors](#callback-driven-editors) below.
- **ImGui.Popups** (`ktsu.ImGui.Popups`) - Modal dialogs: MessageOK, Prompt, InputString/Int/Float, FilesystemBrowser, SearchableList
- **ImGui.Color** (`ktsu.ImGui.Color`) - Bridge between `ktsu.Semantics.Color` and ImGui. Colors are held as the semantic `Color` (linear) and `Srgb` types and converted only at the ImGui seam: `ColorImGuiExtensions` (`ToImColor`/`FromImColor`, `ToImGuiVector4`, `ToImGuiU32`) and `SrgbImGuiExtensions` (`Srgb` → `ImColor`/`ImGuiVector4`/`ImU32`, packed directly with no linear round-trip). The `ImColor` and `Srgb` `ToImGuiU32` apply the global style alpha like `ImGui.GetColorU32`; the linear `Color.ToImGuiU32` is a pure pack matching `ColorConvertFloat4ToU32`. `ImColor` extension operations: adjustments (lighten/darken, saturate/desaturate, hue offset, grayscale, invert, alpha), analysis (relative luminance, contrast ratio, perceptual distance), and contrast heuristics (`MostReadableTextColor`, `AdjustForSufficientContrast`). All color math delegates to `ktsu.Semantics.Color`. (There is no `ImColor` factory class — construct via `Color`/`Srgb` and convert.)
- **ImGui.Styler** (`ktsu.ImGui.Styler`) - Theming system with 50+ built-in themes, scoped styling, Button.Alignment, Text.Color semantic colors, Indent utilities, Alignment helpers, theme-aware color palette (`Palette`, e.g. `Palette.Basic.Red`, `Palette.Semantic.Error`), and interactive theme browser. Color construction and manipulation live in `ImGui.Color`.
- **NodeGraph** (`ktsu.NodeGraph`) - UI-agnostic attribute-based node graph metadata: `[Node]`, `[InputPin]`, `[OutputPin]`, `[NodeExecute]`, `[NodeBehavior]`, pin type utilities
- **ImGuiNodeEditor** (`ktsu.ImGuiNodeEditor`) - ImNodes-based visual node editor with `NodeEditorEngine`, `AttributeBasedNodeFactory`, physics-based layout, `NodeEditorRenderer`, `NodeEditorInputHandler`
- **ImGui.Markdown** (`ktsu.ImGui.Markdown`) - CommonMark markdown renderer built on Markdig (pipe tables, task lists, autolinks), layered on `ImGui.Color` only, with no dependency on `ImGui.App`. Static `ImGuiMarkdown.Render(string, MarkdownConfig?)` parses with an internal source-keyed cache; `MarkdownDocument` parses once for hot render paths. `MarkdownConfig` exposes `FontResolver`, `OnLinkClicked`, `ImageResolver`, `HeadingScales`, `WrapWidth`, `ListIndentPixels`, `ParagraphSpacingPixels`, and `LinkColor`. Heading sizes derive from the live font size, so DPI and `ImGuiApp.GlobalScale` are respected automatically. Bold/italic use real glyphs when the host app registers named font variants via `FontResolver`, otherwise faux styling (faux-bold double-draw, faux-italic renders upright). v1 has no code-block syntax highlighting, no async remote image download, and renders HTML as escaped text.

### Examples

- `examples/ImGuiAppDemo/` - Main application demo
- `examples/ImGuiWidgetsDemo/` - Widget showcase
- `examples/ImGuiStylerDemo/` - Theme gallery
- `examples/ImGuiPopupsDemo/` - Popup demonstrations
- `examples/ImGuiMarkdownDemo/` - Markdown rendering demo

### Tests

- `tests/ImGui.App.Tests/` - App framework tests with mock OpenGL provider
- `tests/NodeGraph.Tests/` - Node graph attribute and type utility tests
- `tests/<Demo>.UITests/` - One headless UI test project per example, driving the demo's real
  `BuildConfig()` through `ImGuiAppHarness`: `ImGuiAppDemo.UITests`, `ImGuiWidgetsDemo.UITests`,
  `ImGuiStylerDemo.UITests`, `ImGuiPopupsDemo.UITests`, `ImGuiMarkdownDemo.UITests`. See
  [Demo UI tests](#demo-ui-tests) below.
- `tests/ImGui.Widgets.UITests/` - One headless UI test class per widget, each driving that widget
  alone with nothing else on screen. See [Widget UI tests](#widget-ui-tests) below.

### Key Files

- `ImGui.App/ImGuiApp.cs` - Main static entry point (`ImGuiApp.Start()`, `ImGuiApp.Stop()`)
- `ImGui.App/ImGuiAppConfig.cs` - Application configuration record
- `ImGui.App/PidFrameLimiter.cs` - PID-controlled frame rate limiter with auto-tuning
- `ImGui.App/FontMemoryGuard.cs` - GPU memory management for font atlases
- `ImGui.App/FontHelper.cs` - Unicode, emoji, and Nerd Font character range support
- `ImGui.App/ForceDpiAware.cs` - Multi-platform DPI detection
- `ImGui.App/WindowingEnvironment.cs` - Wayland / tiling window manager detection driving `ImGuiAppConfig.WindowGeometry`
- `ImGui.App/ImGuiExtensionManager.cs` - Auto-detection of ImGuizmo, ImNodes, ImPlot
- `ImGui.Widgets/DividerZone.cs` - Resizable split pane layout
- `ImGui.Widgets/TabPanel.cs` - Tabbed interface with drag-and-drop
- `ImGui.Widgets/FlameGraph.cs` - Hexa-backed flame graph with managed sample marshaling
- `ImGui.Widgets/Splitter.cs` - Hexa-backed horizontal/vertical draggable splitters
- `ImGui.Widgets/DeferredDrawing.cs` - `DrawDeferred()`/`DrawDeferredDocked()` per-frame pumps, and the `ToggleSwitch` animation-clock fallback used when neither has ever run
- `ImGui.Widgets/DockedWindow.cs` - Abstract base for windows drawn by `DrawDeferredDocked()`; composes Hexa's `ImWindow` via a private adapter instead of inheriting it
- `ImGui.Widgets/Dialogs/` - Hexa-backed dialog wrappers: `FileDialogs.cs` (`OpenFileDialog`/`SaveFileDialog`/`OpenFolderDialog`), `RenameDialog.cs`, `MessageDialogs.cs` (`DialogMessageBox`/`ShowMessageBox`), `DialogOutcome.cs` (shared `DialogOutcome` enum and result mapping)
- `ImGui.Color/ColorImGuiExtensions.cs` - `Color` ↔ ImColor/ImU32/Vector4 conversions (`ImColor.ToImGuiU32` applies global alpha; `Color.ToImGuiU32` is pure)
- `ImGui.Color/SrgbImGuiExtensions.cs` - Direct `Srgb` → ImColor/ImGuiVector4/ImU32 conversions (no linear round-trip)
- `ImGui.Color/ImColorExtensions.cs` - ImColor adjustment, analysis, and contrast operations
- `ImGui.Styler/Palette.cs` - Theme-aware color palette (`Palette.Basic`, `Palette.Semantic`, `Palette.Neutral`, …) and theme color lookups
- `ImGui.Styler/Theme.cs` - Theme management, browser, and selector
- `ImGui.Styler/ScopedColor.cs` - RAII-pattern color styling (`ImColor`/`Color`/`Srgb` overloads; `ScopedTextColor` too)
- `NodeGraph/NodeAttribute.cs` - Core node attributes
- `NodeGraph/PinAttribute.cs` - Pin declaration attributes
- `ImGuiNodeEditor/NodeEditorEngine.cs` - Node graph business logic

### Dependencies

- **Hexa.NET.ImGui** (2.2.9) - Dear ImGui .NET bindings
- **Hexa.NET.ImGuizmo** (2.2.9) - ImGuizmo gizmo extension
- **Hexa.NET.ImNodes** (2.2.9) - ImNodes node editor extension
- **Hexa.NET.ImPlot** (2.2.9) - ImPlot charting extension
- **Silk.NET** (2.23.0) - Cross-platform windowing and OpenGL
- **SixLabors.ImageSharp** (3.1.12) - Image loading
- **ktsu.ThemeProvider** (1.0.11) - Semantic theming foundation
- **ktsu.ThemeProvider.ImGui** (1.0.11) - ImGui theming integration
- **ktsu.TextFilter** (1.5.4) - Text filtering (Glob/Regex/Fuzzy)
- **ktsu.FuzzySearch** (1.2.2) - Fuzzy search matching
- **ktsu.Extensions** (1.5.9) - Collection extension methods
- **ktsu.CaseConverter** (1.3.6) - String case conversion
- **ktsu.Semantics.Color** (2.7.0) - Physically-grounded color type (linear RGB, Oklab, HSL/HSV, WCAG accessibility, adjustment operations); backs `ImGui.Color`
- **ktsu.Semantics.Paths** (1.0.28) - Type-safe path handling
- **ktsu.Semantics.Strings** (1.0.28) - Type-safe string wrappers
- **ktsu.Semantics.Quantities** (1.0.29) - Typed quantity calculations
- **Hexa.NET.ImGui.Widgets** (1.2.18) - Upstream widget collection backing the Hexa-delegated widgets in `ImGui.Widgets`
- **Hexa.NET.ImGui.Widgets.Extras** (1.0.9) - Curve editor, bezier and text editor extras; referenced for a future tier but not yet used by any Tier 1 widget. Pulls `Microsoft.CodeAnalysis.CSharp.Scripting` and `Hexa.NET.Math` into the dependency graph.
- **ktsu.Invoker** (1.1.2) - Delegate invocation utilities
- **ktsu.ScopedAction** (1.1.6) - RAII-pattern scoped actions
- **Polyfill** (9.7.7) - Backport newer .NET APIs
- **Markdig** - CommonMark markdown parser backing `ImGui.Markdown`

## Architecture

### Static Entry Points with Nested Classes

Each library exposes a static class as its main entry point, with nested public classes for components:

- `ImGuiApp.Start()` / `ImGuiApp.Stop()` - Application lifecycle
- `ImGuiWidgets.SearchBox()`, `ImGuiWidgets.Knob()`, `ImGuiWidgets.Combo()`, `ImGuiWidgets.RadialProgressBar()` - Widget methods
- `new ImGuiWidgets.TabPanel()`, `new ImGuiWidgets.DividerContainer()` - Widget instances
- `new ImGuiPopups.InputString()`, `new ImGuiPopups.FilesystemBrowser()` - Popup instances
- `Theme.Apply()`, `Theme.ShowThemeSelector()` - Theme management
- `Button.Alignment.Center()`, `Text.Color.Error()`, `Indent.ByDefault()` - Styling utilities

### Configuration Pattern

```csharp
ImGuiApp.Start(new ImGuiAppConfig
{
    Title = "App",
    OnRender = delta => { /* render code */ },
    OnStart = () => { /* init code */ },
    PerformanceSettings = new() { FocusedFps = 60.0 }
});
```

### Font Configuration

The `ImGuiAppConfig.OnConfigureFonts` callback fires during font atlas initialization, after fonts configured via the `Fonts` property have been added but immediately before the atlas is built. This is the correct — and only — place to register custom fonts with custom glyph ranges, using `FontHelper.AddCustomFont(io, fontData, size, glyphRange, mergeWithPrevious: true)`. For example, Material Icons requires this:

```csharp
OnConfigureFonts = () =>
{
    ImGuiIOPtr io = ImGui.GetIO();
    byte[] fontData = File.ReadAllBytes("MaterialIcons-Regular.ttf");
    unsafe
    {
        FontHelper.AddCustomFont(io, fontData, 16f, FontHelper.GetMaterialIconRanges(), mergeWithPrevious: true);
    }
}
```

Do not register custom fonts from `OnStart` — that runs after the atlas has already built, so glyphs silently fail to rasterize. Routing via `ImGuiAppConfig.Fonts` also does not work for fonts with custom glyph ranges, since that path applies the Nerd Font ranges instead. The callback fires on every atlas rebuild — that means startup plus any DPI change greater than 5%, but *not* `SetGlobalScale`, which does not rebuild the atlas — so handlers must be safe to run repeatedly and re-register fonts each time. `InitFonts` releases the previous run's pinned font data before invoking the callback, so re-registering does not accumulate pinned memory.

Note that `FontHelper.GetMaterialIconRanges()` claims the entire Private Use Area (U+E000–U+F8FF), which subsumes every Nerd Font range. Merging Material Icons last therefore replaces Nerd Font glyphs across Powerline, Font Awesome, Devicons, Octicons and the rest — not just one narrow span.

### Deferred Drawing (dialogs and docked windows)

Hexa's dialogs and `DockedWindow` are stateful: `Show()` registers the instance with a static
manager, and it is only drawn by a per-frame pump. Call one of these once per frame, at the end of
`OnRender`:

- `ImGuiWidgets.DrawDeferred()` — draws every open dialog, message box and popup, and advances
  Hexa's animation clock. No layout opinion.
- `ImGuiWidgets.DrawDeferredDocked()` — additionally creates a dockspace over the main viewport and
  draws every registered `DockedWindow`. It **requires** `ImGuiConfigFlags.DockingEnable`, which is
  set by `ImGuiAppConfig.EnableDocking = true`; without it the pump throws
  `InvalidOperationException`. The flag cannot be turned on from the pump: ImGui only accepts it
  before the first `NewFrame()`, and changing it mid-frame aborts the process on the *next* frame
  with `"Please set DockingEnable before the first call to NewFrame()!"`. Hexa's
  `WidgetManager.Draw()` calls `DockSpaceOverViewport` without checking the flag, so without it the
  dockspace would silently do nothing anyway. Internally it does
  everything `DrawDeferred()` does (via Hexa's `WidgetManager.Draw()`, which itself calls Hexa's
  `DialogManager.Draw()`, `MessageBoxes.Draw()`, `PopupManager.Draw()` and `AnimationManager.Tick()`),
  so `DrawDeferred()` must not also be called. `DockedWindow` only renders under this pump — under
  `DrawDeferred()` a `DockedWindow` is registered but never drawn, because Hexa's widget manager is
  only driven by that pump. A `DockedWindow` is dockable but **not** auto-docked: it opens floating
  and stays there until the user drags it into the dockspace.

They are mutually exclusive: calling both in the same frame draws every dialog twice.
`ImGuiWidgets` detects this (via the ImGui frame counter) and logs a `Trace.TraceWarning`, but does
not throw.

Showing a dialog (`OpenFileDialog`, `SaveFileDialog`, `OpenFolderDialog`, `RenameDialog`,
`DialogMessageBox`, or `ShowMessageBox`) before any pump has ever run throws
`InvalidOperationException` — otherwise it would never appear, and the manager's internal
collections would grow for the life of the process.

Calling `Show()` on a dialog instance that is already shown also throws `InvalidOperationException`.
Hexa's `Dialog.Show()` adds the instance to its manager unconditionally, so a second `Show()`
registers the same instance twice; closing removes one entry and the survivor is never drawn, never
closed and never removed, which latches `WidgetManager.BlockInput` on for the life of the process.
Wait for the close callback, or create a new instance per showing. The guard lives in
`ImGuiWidgets.DialogShowGuard` and is cleared from the close callback, which Hexa invokes on every
close path including the window's X button.

A pump is not required just to keep animated Hexa-backed widgets correct: `ToggleSwitch` ticks
Hexa's animation clock itself once per frame whenever no pump has ever run, so it animates
correctly even in an application that never calls `DrawDeferred()` or `DrawDeferredDocked()`. Once
either pump runs for the first time, it owns the clock exclusively and the fallback stops ticking.
The pump is required for dialogs, not for animation.

The file dialogs' underlying `Close()` briefly blocks the UI thread while its async directory-scan
task unwinds (`refreshTask?.Wait()`).

### Callback-driven editors

`ImGuiWidgets.Sequencer` and the multi-curve `ImGuiWidgets.CurveEditor(CurveSource, Vector2, string)`
take a source object they interrogate while drawing, rather than a value:

- Subclass `ImGuiWidgets.SequenceSource` for a timeline: implement `FrameMin`, `FrameMax`, `ItemCount`,
  `GetItem(int)`, and `SetItemRange(int index, int start, int endFrame)` (the third parameter is the
  clip's new *end* frame, not a generic "end" value), which receives drag edits. `GetItemLabel`,
  `ItemTypeNames`, `AddItem`, `DeleteItem`, `DuplicateItem`, `Copy`, `Paste`, `GetCustomHeight`,
  `DoubleClick`, `BeginEdit` and `EndEdit` are virtual with no-op/empty defaults.
- Subclass `ImGuiWidgets.CurveSource` for a multi-curve graph: implement `CurveCount`, `ViewMin`,
  `ViewMax`, `GetPointCount(int)`, `GetPoints(int)`, `GetCurveColor(int)`, `EditPoint(int, int, Vector2)`
  and `AddPoint(int, Vector2)`. `IsVisible`, `GetInterpolation`, `BackgroundColor`, `BeginEdit` and
  `EndEdit` are virtual with sensible defaults.

Neither entry point calls or requires `DrawDeferred()`/`DrawDeferredDocked()` — they are immediate-mode
calls that happen to take a callback object, drawn inline wherever you call them. `SequenceSource` and
`CurveSource` themselves are plain abstract classes with managed members only; no vendor type and no
`unsafe` appears in either class's public surface (the `unsafe` in `Sequencer`'s and the multi-curve
`CurveEditor`'s own signatures is confined to the adapters that marshal to Hexa internally).

`ImGuiWidgets.CurveEditor` also has a single-curve overload — distinguished from the `CurveSource`
overload by its first parameter's type and the overload's arity — that edits a `CurveData` value:
`CurveEditor(CurveData curve, Vector2 size, Vector2 rangeMin, Vector2 rangeMax, ref int selection,
string label)`. `ImGuiWidgets.BezierEditor(string label, ref BezierControlPoints points, float size =
128f)` edits a `BezierControlPoints` pair (`First`/`Second`) directly, with no source object.

`CurveData` wraps the curve representation the widget expects, exposing `PointCount`, a mutable `Shape`
(`CurveShape.Smooth` or `.Freehand`), and `GetPoint`/`AddPoint`/`SetPoint`/`RemovePoint`/`Clear` over
`CurveKnot` values (a `Position` plus a `CurvePointKind` of `.Smooth` or `.Corner`). It tracks a dirty
flag internally: every one of those mutators, the `Shape` setter, and an edit made through the
`CurveEditor(CurveData, ...)` overload all mark it dirty, so `Sample(float t)` recomputes the
underlying sample cache on its next call rather than returning a stale value.

### Scoped Styling (RAII Pattern)

```csharp
// ScopedColor accepts a semantic Color, an Srgb, or an ImColor
using (new ScopedColor(ImGuiCol.Text, Color.FromHex("#ff6b6b")))
{
    ImGui.Text("Styled text");  // Auto-restored after block
}

// Theme-aware palette entries are ImColor values
using (new ScopedColor(ImGuiCol.Button, Palette.Semantic.Success))
{
    ImGui.Button("Themed button");
}

using (Text.Color.Error())
{
    ImGui.Text("Error message");
}

using (new ScopedDisable(true))
{
    ImGui.Button("Disabled button");
}

using (Button.Alignment.Center())
{
    ImGui.Button("Centered text", new Vector2(200, 30));
}
```

### Node Graph Architecture

The node graph system follows a clean separation of concerns:

- **NodeGraph** (UI-agnostic): Attribute-based metadata for declaring nodes, pins, execution modes, and type compatibility. No dependency on any rendering library.
- **ImGuiNodeEditor**: Renders and interacts with the graph using ImNodes. Split into:
  - `NodeEditorEngine` - Business logic (nodes, links, physics)
  - `AttributeBasedNodeFactory` - Creates nodes from attribute-decorated types
  - `NodeEditorRenderer` - Pure ImNodes rendering
  - `NodeEditorInputHandler` - Input event processing

### Key Technical Details

- **PID frame limiter** with auto-tuning (Coarse/Fine/Precision phases)
- **Throttled rendering**: Different FPS for focused/unfocused/idle/minimized states
- **Font memory management** via `FontMemoryGuard` with GCHandle pinning and GPU detection
- **Texture caching** with concurrent dictionary, auto-cleanup on context change
- **ImGui extension auto-detection** via reflection for ImGuizmo, ImNodes, ImPlot
- **Physics-based node layout** with force-directed simulation, spring links, and stability detection
- **Dear ImGui paradigm**: Immediate mode - render every frame, no retained state

## Testing

Tests use **MSTest.Sdk** with the Microsoft Testing Platform. The ImGui.App tests use a mock OpenGL provider (`MockGL`, `TestOpenGLProvider`) to test rendering logic without a real GPU. NodeGraph tests validate attribute scanning, pin type utilities, and node factory behavior.

```bash
dotnet test                                          # Run all tests
dotnet test --filter "FullyQualifiedName~TestGL"    # Run specific test class
```

Do **not** pass `--nologo` to `dotnet test`. On Microsoft Testing Platform projects it reports
`Zero tests ran` and exit code 5 instead of running anything, which looks exactly like a broken test
project (dotnet/sdk#55309). Running the produced test executable directly is the way to confirm.

### Demo UI tests

Each example has a headless UI test project under `tests/<Demo>.UITests/`, built on
`ktsu.ImGui.App.Testing`. They render through the CPU rasterizer with no window and no GPU, inject
input straight into ImGui, and advance frames under the test's control, so they neither steal focus
nor need a display.

The pattern each suite follows:

```csharp
[assembly: DoNotParallelize]   // ImGui contexts are global; only one harness may be live

[TestInitialize]
public void SetUp()
{
    ImGuiPopupsDemo.ResetState();   // demo state lives in statics that outlive a harness
    harness = ImGuiAppHarness.Start(ImGuiPopupsDemo.BuildConfig(), DemoViewport);
    harness.Step(2);
}

harness.Click("Show Custom Prompt");
harness.Step(2);
harness.Click("prompt/Maybe");
Assert.AreEqual("User clicked Maybe", ImGuiPopupsDemo.lastPromptResult);
```

Things that bite when writing these:

- **Each demo exposes `BuildConfig()`** (internal, with `InternalsVisibleTo` for its UITests
  assembly) so a test drives the real configuration rather than a copy, plus `ResetState()` because
  demo state is static and outlives the harness.
- **Use `WasSeenInFrame`, not `Rect`, to ask "is this on screen now".** `ItemProbe.Rect` returns the
  last position an item *ever* occupied and never expires, so it cannot tell a closed popup from an
  open one. Every suite wraps this as
  `IsVisible(name) => harness.Probe.WasSeenInFrame(name, harness.FrameCount - 1)`.
- **Give the harness a viewport tall enough for the whole demo.** Items below the fold are still
  recorded by the probe, but clicking their recorded position lands on whatever is actually there,
  so the click silently does nothing. The tabbed demos use 1600x1200 or larger.
- **Run in Release.** The software rasterizer is roughly 17x faster there — about 180 ms per frame
  versus 3 s in Debug — so keep frame counts modest either way.
- **The demos mark their own controls.** `ktsu.ImGui.Widgets` and `ktsu.ImGui.Popups` mark items
  themselves; the plain ImGui buttons, headers, tabs and sliders the demos draw go through a small
  `DemoProbe` helper (or a local `DemoButton`/`DemoHeader`/`DemoTab`) that calls
  `ImGuiProbes.MarkItem` right after submitting the widget.
- **Names are qualified by window and scope**, and lookups match trailing segments. A leaf name that
  appears in two windows is ambiguous and `Rect` throws rather than guessing, so qualify it
  (`harness.Probe.Matches("Username").Single(n => n.Contains("FormExample"))`) or pick a distinct one.

### Widget UI tests

`tests/ImGui.Widgets.UITests/` covers every widget in `ktsu.ImGui.Widgets` **in isolation**: one
test class per widget, whose harness renders that widget and nothing else. The per-demo suites above
prove the examples work; these prove each widget works whether or not any example happens to use it,
and a failure names the widget rather than a demo page containing it.

Tests derive from `WidgetTest`, which starts a harness around a bare `ImGuiAppConfig` whose
`OnRender` is the widget under test, and disposes it on cleanup:

```csharp
[TestClass]
public sealed class SwitchTests : WidgetTest
{
    private bool value;

    [TestMethod]
    public void Switch_ClickTogglesOn()
    {
        Start(() => ImGuiWidgets.Switch("Wi-Fi", ref value));
        Click("Wi-Fi");
        Assert.IsTrue(value);
    }
}
```

`WidgetTest` offers `Start`/`Step`, `Click`/`ClickFraction`/`DragAcross`/`Hover`, `IsVisible`,
`RectOf`/`CenterOf`, `Mark`/`MarkSpan`, `Snapshot`/`PixelsChangedSince`/`BoundsOfDifference`,
`CreateTestTexture`, and the dialog helpers `FindDialogButtons`/`ClickDialogButton`/
`DismissOpenDialogs`.

Things that bite here, beyond the demo-suite list above:

- **A widget's probe mark is not always the thing it drew.** The alignment helpers (`TextCentered`,
  `ImageCenteredWithin`, `TextCenteredH`, …) position the cursor and leave a zero-width spacer as
  the last submitted item, so a mark taken after the call measures the spacer. Measure those from
  the pixels with `BoundsOfDifference` against a frame drawn without the widget.
- **Widgets that submit several items, or none, need `MarkSpan`.** `PinInput` is a row of separate
  text boxes; `BufferingBar` paints straight into the draw list. `MarkSpan(name, cursorBefore)`
  records from the cursor position to the last item's rectangle so a test can aim inside it.
- **Latch return values.** A widget reports a click for the single frame it happens on, and `Click`
  renders a further frame after the release, so capture with `clicked |= ImGuiWidgets.Chip(...)`
  rather than a plain assignment.
- **Hexa's dialog managers are process-static.** A dialog left unanswered outlives the harness that
  showed it and is drawn again over the next test. Any suite that opens one calls
  `DismissOpenDialogs()` from a `[TestCleanup]`.
- **Some state is process-global and cannot be tested from here.** The pumpless animation-clock
  fallback (`TickAnimationClockIfUnpumped`) latches off as soon as any test in the assembly runs a
  pump, so it is covered by the unit tests around `EvaluateFallbackTick` instead.
- **Vendor widgets are reached by geometry, not by name.** Hexa's dialogs mark nothing:
  `FindDialogButtons()` locates a message box's buttons by finding the lowest run of theme-blue
  pixels, and the file pickers — whose action row is not blue enough to find that way — are
  cancelled at a fixed offset from the picker window's corner, which is stable because the harness
  pins the viewport.
- **Where a click has to land was measured, not assumed.** The flame graph's bars sit in a band near
  the top of the graph rather than filling it; the sequencer's clips sit just under its header; the
  page indicator's dots do not divide its row into equal columns. Those coordinates are commented
  where they appear.

## Adding Components

### New Widget

1. Add class to `ImGui.Widgets/`
2. Follow existing widget patterns (static methods or instance classes)
3. Add demo to `examples/ImGuiWidgetsDemo/`
4. Add an isolation suite to `tests/ImGui.Widgets.UITests/`

### New Theme

1. Add theme definition to `ImGui.Styler/`
2. Test in `examples/ImGuiStylerDemo/`

### New Node Type

1. Decorate a class/struct with `[Node]` attribute
2. Add `[InputPin]` / `[OutputPin]` to properties/fields
3. Add `[NodeExecute]` to the execution method
4. Register with `AttributeBasedNodeFactory.RegisterNodeType<T>()`

### Modifying ImGui.App

Changes affect all consumers. Test with all example applications.

## Code Style

- **Tabs** for indentation (not spaces)
- **File-scoped namespaces** with using directives inside
- **Explicit types** - no `var`
- **No `this.` qualifier**
- **Always use braces** for control flow
- **Primary constructors** when appropriate

All C# files require this header:

```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.
```

## CI/CD

Uses `scripts/PSBuild.psm1` PowerShell module for CI pipeline. Version increments are controlled by commit message tags: `[major]`, `[minor]`, `[patch]`, `[pre]`. Auto-generated files (VERSION.md, CHANGELOG.md, LICENSE.md) should not be manually edited. CI runs on Windows, publishes to NuGet, uses SonarQube for analysis.

## Code Quality

Do not add global suppressions for warnings. Use explicit suppression attributes with justifications when needed, with preprocessor defines only as fallback. Make the smallest, most targeted suppressions possible.
