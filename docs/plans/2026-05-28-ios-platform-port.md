# iOS Platform Port for ImGui.App

> **Status:** In progress. Each task below is a separate PR-sized chunk.
> **Owner:** TBD (Mac required for runtime verification — Linux container cannot build `net10.0-ios`).
> **Predecessor:** PR #180 landed the `net10.0-ios` TFM and a throwing stub at `ImGui.App/Platform/iOS/ImGuiApp.iOS.cs`.
>
> **Progress log:**
> - ✅ **Task 1** — `IRendererBackend` seam introduced; desktop routes through it (`IRendererBackend.cs`).
> - ✅ **Task 2** — `macos-14` CI job compile-checks `net10.0-ios` (`.github/workflows/dotnet.yml`).
> - 🚧 **Task 3 groundwork** — the platform-neutral public surface (`ImGuiAppConfig`,
>   `ImGuiAppWindowState`, `FontMemoryGuard.FontMemoryConfig`) now compiles for `net10.0-ios`
>   (Silk.NET-coupled members gated behind `#if !IOS`), and the iOS entry point exposes the
>   cross-platform `Start(ImGuiAppConfig)` signature. The `UIApplicationDelegate` + `CADisplayLink`
>   lifecycle (the rest of Task 3) is the next chunk; `Start` still throws until it lands.

**Goal:** Make `ImGuiApp.Start(config)` actually run a Dear ImGui application on iOS (iPhone + iPad, iOS 15+) with parity for the OnStart / OnUpdate / OnRender / OnAppMenu lifecycle, fonts, textures, and DPI scaling.

**Non-goals (this port):** AppStore submission tooling, MAUI/Xamarin compatibility, push notifications, background execution, in-app purchase plumbing, multi-window/Stage Manager, Apple Pencil pressure curves, ARKit overlay. Those are downstream of "the renderer works."

---

## 1. Constraints That Shape Every Decision

1. **No CI coverage.** `.github/workflows/dotnet.yml` is `windows-latest` only; the `net10.0-ios` TFM in `ImGui.App.csproj` is gated on `IsOSPlatform('OSX')`, so even running CI on Mac would need a new workflow. Until that exists, iOS code is verified only by hand on a developer's Mac.
2. **No Linux build.** The `ios-workload` requires Xcode, which only runs on macOS. Anyone iterating from Linux/Windows is writing blind — the symbol must be in the .NET SDK lookups, but the codegen, AOT, and linker run only on Mac.
3. **AOT-only runtime.** iOS forbids JIT. Reflection-heavy paths (e.g. `ImGuiExtensionManager`'s extension auto-detection) need `DynamicallyAccessedMembers` / trimming roots, or feature-flagged off.
4. **No file system writes outside the app sandbox.** `imgui.ini` and any user state must go to the Documents/Library directories, not CWD.
5. **OpenGL ES is deprecated.** Apple has not removed it, but Metal is the supported path. We do not want to ship on a deprecated API for a new port.
6. **The desktop `ImGuiApp` static class is 1861 lines and tightly coupled to `Silk.NET.Windowing` + `Silk.NET.OpenGL`.** Sharing implementation requires extracting platform-neutral pieces first; doing this badly will regress desktop.

---

## 2. Architecture Decisions

### 2.1 Renderer: Metal via a C# port of `imgui_impl_metal.mm` — recommended

| Option | Pros | Cons | Verdict |
|---|---|---|---|
| **Metal (native)** | Supported by Apple indefinitely; matches platform direction; ImGui has an upstream reference impl (`imgui_impl_metal.mm`, ~600 LoC) to port. | No Hexa.NET binding exists — must P/Invoke `Metal.framework` or use `Apple.Metal` NuGet (or hand-rolled `[DllImport]`s). Substantial new code. | **Chosen.** |
| ANGLE (GL→Metal) | Reuses existing `ImGuiController` (OpenGL) unchanged. | Not officially supported on iOS; ships native dylib; another moving part to maintain; cuts off Metal-only features (MetalFX, ProMotion control). | Rejected. |
| MoltenVK / Vulkan | Hexa.NET has a Vulkan backend story. | Adds full Vulkan stack on iOS; far more native deps than the problem warrants. | Rejected. |
| OpenGL ES via Silk.NET + SDL2 | Smallest C# delta — `Silk.NET.Windowing.Sdl` already supports iOS. | Deprecated API, future-hostile, performance ceiling lower than Metal, brings SDL2 native dep into the iOS bundle. | Rejected for shipping; **kept as a "Plan B" prototype** if Metal port stalls. |
| SDL3 renderer | Modern, has iOS support, ImGui ships `imgui_impl_sdlrenderer3`. | Replaces Silk.NET entirely on iOS — divergent code path; SDL3 still maturing; bundle size. | Rejected. |

**Decision: implement a minimal Metal backend in C#.** Scope the port to what ImGui actually exercises: a `MTLDevice`, `CAMetalLayer`, command queue, per-frame `MTLBuffer` for vertex/index, `MTLTexture` for the font atlas plus user textures, and a single `MTLRenderPipelineState` matching ImGui's shader. The upstream `.mm` file is the spec we port from. Shader source lives next to the C# in `Platform/iOS/Shaders/ImGui.metal`, precompiled to a `.metallib` resource at build time.

### 2.2 Windowing & Lifecycle: hand-rolled `UIApplicationDelegate`

Silk.NET.Windowing's iOS path goes through SDL2 — useful as a Plan B (§2.1), but pulling SDL into the bundle for `Start()` is unjustified once we have Metal. Instead:

- `Platform/iOS/ImGuiAppDelegate.cs` — `[Register("ImGuiAppDelegate")]`, conforms to `IUIApplicationDelegate`. Owns the `UIWindow`, root `UIViewController`, and `CADisplayLink`.
- `Platform/iOS/ImGuiAppViewController.cs` — hosts an `MTKView` (or a bare `UIView` backed by `CAMetalLayer`); routes `viewDidLayoutSubviews` → `OnMoveOrResize`, `viewWillAppear` → focus on, `viewDidDisappear` → focus off.
- `ImGuiApp.Start(config)` on iOS does **not** block. It calls `UIApplication.Main(args, null, typeof(ImGuiAppDelegate))`, which never returns until the OS kills the process. `Stop()` calls `UIApplication.SharedApplication.PerformSelector(new Selector("terminateWithSuccess"), null, 0)` (with the standard "iOS apps shouldn't terminate" caveat documented on the API).
- Frame pacing: `CADisplayLink` at `preferredFramesPerSecond = config.PerformanceSettings.FocusedFps` (clamped to display capability — ProMotion up to 120, standard 60). The existing `PidFrameLimiter` becomes a no-op on iOS; the OS owns vsync.

### 2.3 Input: touch → ImGui mouse + multi-touch sidecar

ImGui's input model is single-mouse. We map:

- Single finger → `ImGuiIO.AddMousePosEvent` / `AddMouseButtonEvent(0)`.
- Second finger during a single-finger drag → no-op (avoid jitter); future multi-touch goes through a sidecar `ImGuiApp.Touches` API exposed on iOS only.
- Soft keyboard → triggered by `ImGuiIO.WantTextInput`; we present a hidden `UITextField` first-responder that forwards `UITextInput` deltas to `ImGuiIO.AddInputCharacter` and arrow/delete to `AddKeyEvent`.
- Hardware keyboard (iPad with Magic Keyboard) → `UIKey` events through `pressesBegan` / `pressesEnded` → `AddKeyEvent`.
- Pencil → treated as mouse for v1; pressure exposed via the same sidecar as multi-touch.

### 2.4 DPI & scaling: drop `ForceDpiAware`, use `UIScreen.Scale`

`ForceDpiAware.cs` (761 lines) detects DPI by interrogating X11/Win32/macOS-window APIs that don't apply on iOS. On iOS the answer is `UIScreen.MainScreen.Scale` (typically 2 or 3) for pixel density, and `UIScreen.MainScreen.NativeBounds` for resolution. `ImGuiApp.ScaleFactor` initialises from that; `GlobalScale` stays user-controlled. The `ForceDpiAware` type is excluded from the `net10.0-ios` compile via `<Compile Remove>` in the csproj rather than gated with `#if`s — keeps the desktop file readable.

### 2.5 Fonts & textures: shared atlas, Metal upload

`FontHelper.cs` and `FontMemoryGuard.cs` are platform-neutral and stay shared. The atlas-upload path moves behind an `IRendererBackend` interface:

```csharp
internal interface IRendererBackend
{
    nint CreateTexture(ReadOnlySpan<byte> rgba, int width, int height);
    void DeleteTexture(nint id);
    void RenderDrawData(ImDrawDataPtr drawData);
}
```

Desktop GL implementation wraps the existing `ImGuiController`; iOS implementation calls Metal. `ImGuiApp.GetOrLoadTexture` stays on the public surface; the `uint TextureId` field on `ImGuiAppTextureInfo` becomes `nint` (Metal returns pointer-sized handles) — **breaking change**, but only for callers who poke at the raw id, which existing code doesn't.

### 2.6 File paths: redirect `imgui.ini` and resources

`ImGui.IO.IniFilename` defaults to `imgui.ini` (CWD). On iOS, CWD is the bundle (read-only). Override in iOS `Start`:

```csharp
io.IniFilename = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "imgui.ini");
```

Honour `config.SaveIniSettings = false` by setting `IniFilename = null` (ImGui native API).

### 2.7 Extension manager: opt-out on iOS

`ImGuiExtensionManager` scans loaded assemblies via reflection. Under AOT this is brittle. For v1, iOS `Start` skips extension auto-detection entirely; ImGuizmo/ImNodes/ImPlot users on iOS must call `ImGuiExtensionManager.Register<T>()` manually. Add a `config.AutoDiscoverExtensions` flag (default true on desktop, false on iOS) so the choice is explicit.

---

## 3. Public API Contract on iOS

| Member | iOS behaviour |
|---|---|
| `Start(ImGuiAppConfig)` | Bootstraps `UIApplication.Main`. Does not return until OS kills the process. |
| `Stop()` | Calls `terminateWithSuccess` selector. Logs a warning that iOS apps should not self-terminate. |
| `WindowState` | Returns a synthetic state: size = `UIScreen.MainScreen.NativeBounds`, pos = (0,0), `LayoutState = Normal`. Setters are no-ops. |
| `IsFocused` | Driven by `applicationDidBecomeActive` / `applicationWillResignActive`. |
| `IsVisible` | Driven by `viewWillAppear` / `viewDidDisappear`. |
| `IsIdle` | Same input-time heuristic as desktop; touch events update `lastInputTime`. |
| `ScaleFactor` | `UIScreen.MainScreen.Scale`. |
| `GlobalScale` | User-controlled, identical semantics. |
| `Invoker` | Marshals to the main UI thread via `NSObject.InvokeOnMainThread`. |
| `SetWindowIcon` | No-op + warning (icons are bundle metadata, set via Info.plist). |
| `GetOrLoadTexture` / `TryGetTexture` / `DeleteTexture` | Same surface, Metal handles under the covers. |
| `EmsToPx` / `PtsToPx` | Unchanged. |
| `OnAppMenu` | Renders into a top toolbar `UIView` on iPad (where a menu bar makes sense); on iPhone, an overflow button. Optional — v1 renders nothing if the iPhone idiom is detected. |
| `config.IconPath` | Ignored with warning. |
| `config.InitialWindowState` | Ignored — iOS controls window sizing. |
| `config.TestMode` | Honoured: skips display link, runs N frames synchronously, then exits via `Stop()`. Needed for any future iOS test target. |

---

## 4. Code Organisation

```
ImGui.App/
  Platform/
    Desktop/                  (new — moved from root)
      ImGuiApp.Desktop.cs     (extracted from ImGuiApp.cs)
      ImGuiController/        (existing GL impl)
    iOS/
      ImGuiApp.iOS.cs         (existing stub — replaced)
      ImGuiAppDelegate.cs
      ImGuiAppViewController.cs
      MetalRendererBackend.cs
      MetalTextureCache.cs
      TouchInputBridge.cs
      Shaders/
        ImGui.metal           (compiled to .metallib at build)
  ImGuiApp.cs                 (platform-neutral state: WindowState, GlobalScale, fonts, textures, frame pacing config)
  ImGuiApp.Shared.cs          (frame loop body; calls into IRendererBackend; new)
  IRendererBackend.cs         (new abstraction)
```

The csproj uses `<Compile Remove>` to scope desktop-only files out of `net10.0-ios` and iOS files out of `net8.0/9.0/10.0`. We avoid `#if IOS` blocks inside files where possible — the stub is the last one to use that pattern.

---

## 5. Chunked Implementation Plan

Each task is a separate PR. Each PR includes a manual verification recipe because CI can't validate iOS.

### Task 1 — Extract `IRendererBackend` on desktop, no behavioural change

**Scope:** Introduce the interface (§2.5), make `ImGuiController` implement it, route `ImGuiApp` through it. Desktop only — iOS stub unchanged.

**Verify:** `dotnet test` green; all four example apps launch and render unchanged. No new public types except the interface (internal).

**Risk:** Low. Pure refactor. Worth doing before any iOS code lands so the iOS port plugs into a stable seam.

**Est:** 1 PR, ~400 LoC moved, no net new logic.

### Task 2 — Add `windows-latest` + `macos-14` matrix to CI; build (not run) `net10.0-ios`

**Scope:** Edit `.github/workflows/dotnet.yml` to add a `macos-14` job that installs `ios` workload (`dotnet workload install ios`) and runs `dotnet build -f net10.0-ios ImGui.App/ImGui.App.csproj`. No test execution — Apple Simulator boot from CI is a separate problem.

**Verify:** PR shows green Mac build; the existing stub compiles end-to-end on Mac CI.

**Risk:** Low–medium. CI cost goes up; workload install can be flaky and slow. Cache the workload.

**Est:** 1 PR. Mostly YAML.

### Task 3 — Replace the stub with `UIApplicationDelegate` + black screen

**Scope:** Implement §2.2 minus rendering. Launching the app shows a `UIWindow` with the configured `Title` in a `UILabel`, runs a `CADisplayLink` that calls `OnUpdate(delta)` and `OnRender(delta)` but does **not** call any ImGui drawing yet. Proves the lifecycle plumbing in isolation from Metal.

**Verify:** On a developer Mac, `dotnet build -f net10.0-ios -t:Run` (or open in Xcode after `dotnet publish`) — app launches in simulator, `OnStart` fires, `OnUpdate` ticks at 60Hz, `OnRender` ticks at 60Hz. Background the app → ticks pause; foreground → ticks resume. `IsFocused` matches.

**Risk:** Medium. First real iOS code. The `[Register]`/AOT path needs `<TrimmerRootDescriptor>` entries to keep the delegate from being trimmed. Likely 1–2 round-trips on a Mac before it actually launches.

**Est:** 1 PR. ~300 LoC across delegate/VC/Start.

### Task 4 — Metal renderer backend (the real work)

**Scope:** Port `imgui_impl_metal.mm` to `MetalRendererBackend.cs`. Implement `IRendererBackend` from Task 1. Compile `ImGui.metal` to `.metallib` as a build target (XML `<Target>` running `xcrun metal` + `metallib`). Wire it into `ImGuiAppViewController` so the display link renders an ImGui frame containing whatever `config.OnRender` draws.

**Verify:** Demo (`examples/ImGuiAppDemo`) needs a tiny iOS-aware tweak — ImGuiAppDemo.csproj gets a `net10.0-ios` TFM and `[Register]`s its own `Main`. Launching shows the demo window with all default ImGui demo widgets interactive via touch.

**Risk:** High. The Metal port is the bulk of the project's novelty. Plan for two PRs if it gets too big: (4a) atlas + flat-coloured draw lists, (4b) textured draw lists + user textures via `GetOrLoadTexture`. Triple-buffered command buffers and `MTLBuffer` ring allocation must be correct or you get flicker/GPU stalls; budget time for a frame-debugger session in Xcode.

**Est:** 1–2 PRs. ~700 LoC of C# + ~150 LoC of Metal shader.

### Task 5 — Touch & keyboard input

**Scope:** §2.3. `TouchInputBridge` wires `UITouch` → `ImGuiIO`; hidden `UITextField` for soft keyboard. Hardware keyboard via `pressesBegan`. Multi-touch sidecar deferred.

**Verify:** Demo's input fields accept text; buttons respond to taps; drag widgets work; scroll works via two-finger pan.

**Risk:** Medium. Soft-keyboard reveal/hide animation interacts with `viewDidLayoutSubviews` — easy to introduce layout thrash.

**Est:** 1 PR. ~250 LoC.

### Task 6 — DPI, fonts, textures, ini redirect

**Scope:** §2.4 + §2.5 + §2.6. Verify the existing font atlas math produces crisp text at `UIScreen.Scale = 3`. Confirm `Resources.NerdFont` and `Resources.NotoEmoji` load (the resource path resolution should "just work" but is worth testing). Redirect `imgui.ini` to `Library/Application Support/<bundleId>/imgui.ini`.

**Verify:** Text is crisp on a 3x device; emoji renders; relaunching the app preserves any ImGui window positions the demo creates.

**Risk:** Low–medium. Mostly applying decisions already made above.

**Est:** 1 PR. ~200 LoC.

### Task 7 — `OnAppMenu` strategy + `Stop()` semantics

**Scope:** §2.3 OnAppMenu rendering (iPad toolbar, iPhone no-op). `Stop()` warning. Audit `SetWindowIcon` / `InitialWindowState` / etc. to log iOS-no-op warnings consistently.

**Verify:** iPad demo shows the app menu; iPhone demo runs without it; `Stop()` logs but still terminates.

**Risk:** Low.

**Est:** 1 PR. ~100 LoC.

### Task 8 — Extension manager AOT path + ImGuiAppDemo iOS target

**Scope:** §2.7. Add `config.AutoDiscoverExtensions`. Make `ImGuiAppDemo` build for `net10.0-ios`. Document the manual registration pattern.

**Verify:** Demo runs on iOS without ImGuizmo/ImNodes/ImPlot (or with, when registered manually).

**Risk:** Low–medium. AOT linker may strip something unexpected; expect one round of `<TrimmerRootDescriptor>` additions.

**Est:** 1 PR. ~150 LoC.

### Out-of-scope follow-ups (post-port)

- ImGuiWidgets / ImGuiPopups / ImGuiStyler iOS validation passes (they should "just work" since they sit on top of `ImGui.App`, but each needs a once-over).
- iPad multi-window via `UISceneDelegate`.
- Apple Pencil pressure / multi-touch sidecar.
- iOS test project (`tests/ImGui.App.iOS.Tests/`) running a small headless `ImGuiApp` in `TestMode` on a CI simulator.

---

## 6. Verification Strategy Without CI

Until Task 2 lands, every iOS-touching PR needs a Mac smoke test in the PR description with:

1. `dotnet workload list` output (proves the workload is installed).
2. `dotnet build -f net10.0-ios ImGui.App/ImGui.App.csproj` log tail.
3. For Task 3+: a screen recording or screenshot of the simulator showing the demo running.

After Task 2 lands, the Mac job catches compile regressions automatically; manual screenshots still required for behavioural changes.

---

## 7. Open Questions

1. **`uint TextureId` → `nint TextureId`** is a public-API breaking change for anyone storing the raw handle. Acceptable in a feature release, but flag in CHANGELOG. Alternative: keep `uint` and lose the top 32 bits of the Metal handle (risky on 64-bit pointer comparison).
2. **Do we ship `.metallib` source-compiled at consumer build time, or precompiled in our NuGet?** Precompiled is simpler but locks shader to one Metal version. Source-compiled requires consumers to have the iOS workload, which they already need to consume the iOS TFM, so probably fine.
3. **AOT trimming budget.** Hexa.NET.ImGui's P/Invoke surface is large; we may need `<IsTrimmable>false</IsTrimmable>` for v1 and tighten later. Costs binary size.
4. **iPad Stage Manager / multi-window.** Single-scene is fine for v1; document the limitation.

Answer these before starting Task 4 (Metal renderer); the others can wait.
