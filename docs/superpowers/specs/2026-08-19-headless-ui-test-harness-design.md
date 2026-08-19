# Headless UI Test Harness Design

**Date:** 2026-08-19
**Status:** Approved in outline, pending review of the written spec
**Repository:** `ktsu-dev/ImGuiApp`, consumed by `ktsu-dev/ImageGui`

## Purpose

ImGui applications in this ecosystem have no way to test their user interface automatically. The
only verification available today drives the real desktop through `SendInput` and captures the
screen, which takes exclusive control of the machine, cannot run without a display, and cannot run
in continuous integration at all.

Two real defects in ImageGui Milestone 1 went undetected by unit tests and were found only by
driving the application and looking at screenshots. A file dialog listed no files because a
semicolon separated glob matched nothing, and fit to viewport sized the view to the previous
document because evaluation is asynchronous. Both were structural facts about application state
that an automated test could have asserted on every push.

This design adds a headless harness that renders without a display or GPU, injects input directly
into the ImGui event queue rather than through the operating system, advances frames under test
control, and captures rendered pixels for measurement and diagnosis.

## Goals and Non-Goals

**Goals**

- Run user interface tests with no visible window, no display, and no GPU.
- Produce identical results on a developer machine and on a hosted continuous integration runner.
- Advance frames deterministically, with no sleeps and no wall clock waits.
- Capture rendered frames as images, for measurement in assertions and as artifacts on failure.
- Serve both automated regression tests and ad hoc verification during development.
- Keep the harness reusable by every ktsu ImGui application, not specific to ImageGui.

**Non-Goals**

- Golden image comparison. Full frame baselines are not the assertion mechanism, for reasons given
  under Rendering Substrate.
- Integrating Dear ImGui's test engine. Named targeting is instead provided by item probes, for
  the reasons under Item Probes.
- Testing the OpenGL renderer. The harness rasterizes in software, so a defect specific to the GL
  backend will not be caught. See Deferred Work.
- Replacing unit tests. The harness targets integration behavior that only appears when the whole
  application runs.

## Architecture

The harness ships as a separate package, `ktsu.ImGui.App.Testing`, built from a new project in the
ImGuiApp repository. Keeping it out of `ktsu.ImGui.App` means shipping applications never carry a
software rasterizer they do not use.

The harness brings its own headless controller. `ImGuiController` cannot be reused because it is
both the ImGui context manager and the OpenGL renderer, and every constructor overload requires
Silk's `GL`, `IView`, and `IInputContext`. Splitting that class is the tidier long term structure
and is tracked as ImGuiApp issue #313, but restructuring a file that every downstream application
depends on, purely to enable tests, trades a real regression risk for elegance. The harness
therefore owns a small equivalent that creates the ImGui context, configures IO, builds the font
atlas, and runs `NewFrame` and `Render`.

The cost of that decision is duplicated setup logic that can drift from the real path. Issue #313
records the tradeoff so the duplication is deleted when the refactor happens.

### Package Layout

| Project | Package | Contents |
|---|---|---|
| `ImGui.Probes` | `ktsu.ImGui.Probes` | New. The item-marking registry, with no dependencies of its own. |
| `ImGui.App` | `ktsu.ImGui.App` | Existing. Gains a public frame-hosting API and forwards marking. |
| `ImGui.App.Testing` | `ktsu.ImGui.App.Testing` | New. Harness, headless controller, software rasterizer, capture. |
| `tests/ImGui.App.Testing.Tests` | not packaged | New. Tests for the harness itself. |

### The Frame Pump

The per frame body currently lives inline in the `window.Render` handler in `ImGuiApp.cs`. It
configures the frame wrapper, renders the application menu, calls `OnRender`, renders the
performance monitor, calls into the controller, and applies frame rate limiting.

Extract that body into an internal method, `RunFrame(double delta)`, which the existing handler
calls unchanged. The harness calls the same method. This matters because the alternative, a
reimplementation of the frame body inside the harness, would test a copy of the application loop
rather than the loop that ships.

The extraction is behavior preserving and changes no public API.

## Rendering Substrate

The harness implements `IRendererBackend` with a CPU rasterizer rather than using OpenGL.

Hosted continuous integration runners have no GPU and typically expose only a software OpenGL 1.1
path, which cannot run the GL3 renderer that `ImGuiController` uses. A harness that needs a GL
context risks failing on the exact machine it exists to protect. Shipping a software OpenGL
implementation such as Mesa on the runner is a known workaround, but it adds a moving part and
makes rendered output depend on which implementation the runner resolves.

A CPU rasterizer removes both problems. Output is identical on every machine, which makes pixel
measurements reliable enough to assert on. The rasterizer handles the subset of drawing that ImGui
emits: indexed triangle lists, a single texture per draw command, vertex color modulation, alpha
blending, and scissor rectangles.

The tradeoff is explicit. The harness measures what ImGui asked to be drawn, not what a graphics
driver actually drew. A regression confined to the GL backend passes these tests. Deferred Work
covers closing that gap.

## Harness API

### Entry Point

The harness accepts the same `ImGuiAppConfig` the application passes to `ImGuiApp.Start`, so tests
exercise the real configuration rather than a parallel one.

```csharp
using ImGuiAppHarness harness = ImGuiAppHarness.Start(app.BuildConfig(), new HarnessOptions
{
	Width = 1280,
	Height = 720,
	DpiScale = 1.0f,
});
```

The harness also installs its rasterizer as the application host's renderer backend for the
duration of the session. That is what lets an application upload its own textures through
`ImGuiApp.CreateTexture` and have them reach the renderer actually drawing the frame. Without it,
every such call fails on a backend that was never installed, which rules out testing any
application that shows an image it generated rather than one loaded from a file — ImageGui's entire
preview path, among others. The seam is `IRendererBackend`, made public for this reason.

### Determinism Controls

`HarnessOptions` pins everything that would otherwise vary between runs:

- Fixed display size and DPI scale, so layout is identical everywhere.
- A fixed frame delta, defaulting to one sixtieth of a second, independent of real elapsed time.
- Frame rate limiting disabled, because the harness controls pacing.
- `SaveIniSettings` forced off. ImGui otherwise writes window layout to an ini file in the working
  directory, which would make a test depend on the state left by whatever ran before it.

### Frame Stepping

```csharp
harness.Step();
harness.Step(frames: 5);
bool settled = harness.StepUntil(() => session.IsSettled, maxFrames: 300);
```

`StepUntil` returns `false` when the frame budget is exhausted rather than throwing, so the caller
decides whether a timeout is a failure. The budget counts frames rather than milliseconds, so a
loaded runner takes longer in wall clock time without changing the outcome.

### Input Injection

Input is posted directly into the ImGui event queue, the same mechanism the iOS platform already
uses through `io.AddMousePosEvent` and its siblings. Nothing reaches the operating system, so no
window needs focus and no other application is disturbed.

```csharp
harness.Mouse.MoveTo(x, y);
harness.Mouse.Click(x, y);
harness.Mouse.Drag(from, to, steps: 24);
harness.Mouse.Wheel(x, y, clicks: 4);
harness.Keyboard.Press(ImGuiKey.Z, ctrl: true);
harness.Keyboard.Type("export.png");
```

Low level posts inject an event without advancing a frame. The high level helpers advance frames
where the interaction requires it, because ImGui activates a button on release, so a press and
release inside a single frame does nothing.

### Frame Capture

```csharp
CapturedFrame frame = harness.LastFrame;
frame.GetPixel(x, y);
frame.FindBounds(p => p.A > 0);
frame.SavePng(path);
```

Captured frames serve two purposes. Measurements taken from them appear in assertions, for example
the position of a known landmark or the extent of a rendered image. Whole frames are written as
artifacts when a test fails, so a failure can be diagnosed without reproducing it locally.

## Item Probes

Coordinate-based interaction is brittle. A layout change moves what a test clicks, and the test
either fails confusingly or, worse, clicks something else and passes. Item probes remove the
coordinates from tests without taking on Dear ImGui's test engine.

### Why Not the Test Engine

Dear ImGui ships a test engine that provides exactly this, addressing widgets by name. It was
evaluated and rejected on two grounds.

The first is licensing. The test engine is not under the MIT license that covers Dear ImGui itself.
It uses the Dear ImGui Test Engine License, which is free for individuals, not-for-profits,
educational use, open-source derivative work, and entities under two million dollars of turnover,
and requires a paid license otherwise. ktsu-dev qualifies for the free license, but
`ktsu.ImGui.App.Testing` is a package other people consume, and embedding the engine would pass
that obligation to every downstream user. A commercial consumer above the threshold would need to
buy a license in order to use a ktsu test package. This repository already carries issue #230 about
a comparable constraint.

The second is build ownership. The engine's hooks only exist when Dear ImGui itself is compiled
with `IMGUI_ENABLE_TEST_ENGINE`, and Hexa.NET.ImGui ships prebuilt native binaries for nine runtime
identifiers. Using the engine would mean producing a test-engine-enabled native build for each of
them, compiling roughly 660 KB of additional C++, writing a C shim because the engine's API is C++
classes while Hexa.NET binds the C wrapper, and supplying a coroutine implementation. That is a
native build pipeline to own and keep aligned with Hexa.NET's ImGui version, in perpetuity.

### Where the Registry Lives

Marking is a cross-cutting concern. Widget libraries, dialog libraries and the application host all
want to mark items, and none of them should have to depend on another to do it. The registry
therefore lives in its own package, `ktsu.ImGui.Probes`, which depends on nothing but the ImGui
binding.

It could not live in `ktsu.ImGui.App`. Nothing depends on that package, so `ImGui.Widgets` and
`ImGui.Popups` would have had to take a dependency on it, which would drag windowing and OpenGL into
every consumer of a button. Putting the registry in a package with no dependencies also means a
library outside this repository can mark its items without adopting any of this repository's
opinions.

`ImGuiProbes` carries an `Enabled` flag, a master switch independent of whether a probe is installed.
Setting it false suppresses marking without disturbing the installed callback.

### How Probes Work

ImGui already reports the rectangle of the most recently submitted item through `GetItemRectMin` and
`GetItemRectMax`, in the public API. A library or application marks an item immediately after
submitting it:

```csharp
if (ImGui.MenuItem("Open..."))
{
	OpenRequested();
}

ImGuiProbes.MarkItem("file.open");
```

In production no probe is installed and the call costs one null check, so a library can mark
unconditionally. Applications call `ImGuiProbes.MarkItem` directly and reference the package
explicitly. `ktsu.ImGui.App` deliberately does not forward to it: a host that re-exposed the API
would have to depend on the registry package, and two ktsu packages in one publish currently collide
over the `_PackageData` files the SDK copies to output.

A test then addresses the item by name and never states a coordinate:

```csharp
harness.Click("file.open");
Rectangle? rect = harness.Probe.Rect("canvas.image");
```

### Marking in the Libraries

`ktsu.ImGui.Widgets` and `ktsu.ImGui.Popups` mark their own interactive items, so an application gets
addressable widgets without marking anything itself.

The filesystem browser is the case that matters most. It marks its rows by filename, along with its
drives, its save filename field, and its confirm and cancel buttons. That makes a dialog assertable:
a test can state that the browser lists a given image and excludes a file that does not match the
codec filter, which is exactly the defect that shipped in ImGuiApp#312 and was noticed only because
somebody looked at a screenshot.

Widgets mark the region they claim for interaction, which for most of them is the invisible button
they already submit, so the recorded rectangle is exactly the clickable area. `DividerContainer` draws
its handle manually rather than as an ImGui item, so it marks an explicit region instead.

### Name Qualification

A bare label does not identify an item. Two widgets can share a label, and ImGui keeps them apart
through its identifier stack rather than through the label alone. A probe name that ignored that
context would collide exactly where ImGui does not.

Names are therefore recorded fully qualified, as the ImGui window followed by any pushed scopes and
then the item's own name. `ScopedId` pushes a probe scope alongside its identifier, so anything
already scoped for ImGui is scoped for probes without further work.

Tests do not write the whole path. A lookup matches on trailing path segments, so `a.png` resolves
`Open Image/filesystem-browser/a.png` as long as nothing else ends the same way. Qualification is
applied always rather than only on collision, because a name that changed meaning the day a second
widget appeared would break tests for reasons unrelated to the change that caused it.

### Ambiguity and Staleness

Two conditions make a name refuse to resolve, and both fail loudly rather than guessing.

A query matching several recorded names is ambiguous, and the failure lists the candidates so the
author can qualify further. A single fully qualified name marked twice within one frame is also
ambiguous: qualification could not separate those two items, and clicking whichever was drawn second
would be a test that passes while doing the wrong thing.

Probes are recorded per frame. A name last seen in an earlier frame belongs to something no longer on
screen, such as a closed popup, so acting on it would click empty space or whatever has since moved
there. `Click` fails when a name was not marked during the most recent frame, rather than clicking a
stale rectangle and reporting a pass.

### What Probes Do Not Cover

Only marked items are addressable. Two identically labelled widgets in the same window with no scope
between them still collide, and are refused rather than guessed at. ImGui has the same limitation and
the same remedy: give them distinct identifiers.

## ImageGui Integration

### Application Changes

`ImageGuiApplication.Run` currently constructs its `ImGuiAppConfig` inline and passes it straight to
`ImGuiApp.Start`. Extract the construction into an internal `BuildConfig` method so a test can hand
the same configuration to the harness. `Run` keeps its current behavior.

`DocumentSession` gains an `IsSettled` property, true when no evaluation is pending and no
re-evaluation is queued. `StepUntil` waits on this, which removes the guesswork about how many
frames an asynchronous preview needs.

A new MSTest project, `tests/ImageGui.App.UiTests`, references the harness package. `ImageGui.App`
grants it `InternalsVisibleTo`, since the application's types are internal.

### Coordinate Helpers

Because widgets cannot be addressed by name, every coordinate lives behind a named helper in one
file rather than being scattered through tests:

```csharp
internal static class Shell
{
	internal static void OpenFileMenu(ImGuiAppHarness h) => h.Mouse.Click(40, 17);
	internal static void ChooseOpen(ImGuiAppHarness h) => h.Mouse.Click(56, 60);
}
```

A layout change then costs one edit in one file instead of a scattered hunt.

### Initial Test Scenarios

The first scenarios cover the behavior Milestone 1 verified by hand, prioritizing the two defects
that reached a release candidate undetected:

1. Opening an image displays it, and the view fits the canvas.
2. Opening a second image of different dimensions fits that image, not the previous one. This is
   the regression test for the fit defect.
3. The file dialog lists files matching the codec filter and excludes others. This is the
   regression test for the glob defect.
4. Dragging the canvas pans the image, and the pan distance matches the drag distance.
5. Scrolling the wheel zooms toward the cursor, holding a landmark in place.
6. A file that is not a decodable image produces an error popup and leaves the open document intact.
7. A slider drag followed by one undo returns to the pre-drag value, and redo reapplies it.
8. Exporting after an exposure change writes a file whose pixels match the exposure applied
   independently.

Test images are generated in code at test time rather than committed as binaries, so the inputs are
reproducible, reviewable in the diff, and carry deliberate landmarks such as corner markers and a
known grid pitch that make measurement possible.

## Testing the Harness Itself

The harness is test infrastructure, so a defect in it produces false confidence rather than a
visible failure. `tests/ImGui.App.Testing.Tests` covers it directly:

- The rasterizer fills a known triangle, respects a scissor rectangle, blends alpha correctly, and
  samples a texture at the expected coordinates.
- Injected input reaches ImGui, verified by a test application whose `OnRender` records what ImGui
  reported for mouse position, button state, and typed characters.
- A button rendered by a test application responds to `Click`, which proves the press and release
  frame handling.
- `StepUntil` returns `false` and stops at the budget when its predicate never becomes true.
- Two runs of the same scenario produce byte identical captured frames.

## Error Handling

- A harness started twice in one process fails immediately, because ImGui contexts are global and a
  second context would corrupt the first.
- An exception thrown inside the application's `OnRender` propagates out of `Step` with the frame
  number attached, rather than being swallowed by the loop.
- Capturing before the first frame returns a clearly failing result rather than an empty image.
- Disposing the harness destroys the ImGui context and releases rasterizer buffers, so a test class
  running many scenarios does not accumulate contexts.

## Continuous Integration

ImageGui user interface tests run in the existing .NET workflow alongside unit tests. No display,
GPU, or extra runner configuration is required, which is the direct benefit of software
rasterization. Captured frames from failing tests upload as build artifacts using the coverage
report upload already present in the workflow as a model.

Note that `dotnet test` reports "Zero tests ran" in the ImageGui repository regardless of the real
result, so the workflow must invoke the test executable directly, as the existing suite does.

## Known Weaknesses

**Only marked items can be addressed by name.** The widget and dialog libraries mark their own
interactive items, and an application can mark anything else it wants reachable, but an unmarked item
still has to be clicked at a position, where a layout change moves the target and the test either
fails confusingly or clicks something else and passes. The coordinate helper convention limits the
blast radius for those cases.

**The harness does not test the shipping renderer.** Software rasterization is what makes the tests
trustworthy in continuous integration, and it is also what stops them from covering the GL path.

**Duplicated context setup can drift.** The headless controller repeats work that `ImGuiController`
does. If the real path changes and the harness does not follow, tests continue passing while
covering something the application no longer does. Issue #313 removes this once acted on.

## Deferred Work

- **An opt in OpenGL suite.** The same scenarios run against a real GL context with framebuffer
  readback, for local runs and any environment with a working driver, covering the GL specific gap.
- **The `ImGuiController` split**, tracked as issue #313.
