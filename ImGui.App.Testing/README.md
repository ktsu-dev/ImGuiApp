# ktsu.ImGui.App.Testing

Headless test harness for `ktsu.ImGui.App` applications. Renders through a CPU rasterizer with no
window, no GPU and no graphics driver, injects input directly into ImGui rather than through the
operating system, and advances frames under the test's control.

Because nothing reaches the operating system, tests neither steal focus nor disturb anything else on
the machine, and the same suite runs on a busy desktop and on a continuous integration runner with no
display attached.

## A Worked Example

```csharp
using ImGuiAppHarness harness = ImGuiAppHarness.Start(app.BuildConfig(), new HarnessOptions
{
	Width = 1280,
	Height = 720,
});

harness.Click("file.open");
bool ready = harness.StepUntil(() => session.IsSettled, maxFrames: 300);
Assert.IsTrue(ready, "The application never settled.");

CapturedFrame frame = harness.Capture();
Rectangle? image = frame.FindBounds(p => p.A > 0);
Assert.IsNotNull(image, "Something should have been drawn.");
frame.SavePng("artifact.png");
```

Pass the same `ImGuiAppConfig` the application gives `ImGuiApp.Start`, so a test exercises the real
configuration rather than one written for testing.

## Driving the Application

`Step()` advances exactly one frame. `StepUntil(predicate, maxFrames)` advances until a condition
holds or a frame budget runs out, returning false rather than throwing so the caller decides whether a
timeout is a failure. The budget counts frames rather than milliseconds, so a loaded machine takes
longer in real time without changing the outcome.

Input is injected into ImGui's event queue:

```csharp
harness.Mouse.MoveTo(x, y);
harness.Mouse.Click(x, y);
harness.Mouse.Drag(fromX, fromY, toX, toY, steps: 24);
harness.Mouse.Wheel(x, y, clicks: 4);
harness.Keyboard.Press(ImGuiKey.Z, ctrl: true);
harness.Keyboard.Type("export.png");
```

The high-level helpers advance frames where the interaction requires it. ImGui activates a button on
release and only notices a press that was visible during a completed frame, so a press and release
inside one frame would do nothing.

## Addressing Widgets by Name

Prefer names over coordinates. `ktsu.ImGui.Widgets` and `ktsu.ImGui.Popups` mark their interactive
items automatically, and an application marks anything else through `ImGuiProbes.MarkItem`:

```csharp
harness.Click("filesystem-browser/a.png");
Assert.IsNull(harness.Probe.Rect("filesystem-browser/notes.txt"), "The codec filter should exclude it.");
```

Names are recorded fully qualified, as the ImGui window followed by any pushed scopes and then the
item's own name. Lookups match trailing segments, so a test writes the shortest name that identifies
one item. A name matching several items, or one marked twice in a single frame, is reported as
ambiguous with its candidates listed rather than resolving to whichever was drawn last. Clicking an
item that was not drawn in the most recent frame fails as well, since its recorded position is stale
and clicking it would hit whatever has since moved there.

## Determinism

`HarnessOptions` pins everything that would otherwise vary between runs: display size, DPI scale, and
a fixed frame delta independent of real elapsed time. Frame rate limiting is off and ImGui's layout
file is never read or written, so one test cannot inherit state from another.

Two runs of the same scenario produce byte-identical frames. That property is what makes pixel
measurements worth asserting on, and it is covered by a test.

## Known Limitations

Rendering is a CPU rasterizer, not the OpenGL backend the application ships with. That is what makes
results identical on every machine, and it is also why a defect confined to the GL renderer will not
be caught here.

Only marked items can be addressed by name. Two identically labelled widgets in the same window with
no scope between them collide, and are refused rather than guessed at. ImGui has the same limitation
and the same remedy, which is to give them distinct identifiers.
