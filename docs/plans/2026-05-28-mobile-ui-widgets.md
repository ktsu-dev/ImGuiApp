# Plan: Mobile UI Paradigms for ImGui.Widgets

Date: 2026-05-28

## Goals

Expand `ImGui.Widgets` with common mobile UI patterns that Dear ImGui does not ship natively, while staying true to ImGui's immediate-mode philosophy and the library's existing conventions (static entry points, scoped RAII, primary constructors, tabs for indentation, file-scoped namespaces).

## Progress

- **Phase 0 — Foundational Infrastructure**: ✅ complete (`GestureDetector`, `Tween`/`Spring`/`Easing`, `InertialScroll`, `OverlayHost`).
- **Phase 1 — High-value, low-risk widgets**: ✅ complete (`Switch`, `SegmentedControl`, `Chip`/`ChipGroup`, `Stepper`, `Avatar`, `Badge`, `Rating`, `RangeSlider`, `PageIndicator`, `Card`, `SkeletonLoader`, `PinInput`).
- **Phase 2 — Gesture-dependent widgets**: ⬜ not started. Next up — start with `SwipeableListItem` (already have `GestureDetector`).
- **Phase 3 — Overlays & navigation**: ⬜ not started (`OverlayHost` is ready; begin with `Toast`).
- **Phase 4 — Polish**: ⬜ not started.

## Phase 0 — Foundational Infrastructure

Most mobile widgets need shared plumbing. Build these first; everything else depends on them.

1. **Gesture detector** — `ImGui.Widgets/Gestures/GestureDetector.cs`
   - Per-ID state machine tracking pointer-down position, velocity, duration
   - Emits: `Tap`, `DoubleTap`, `LongPress`, `SwipeLeft/Right/Up/Down`, `Pan`, `Pinch` (if multi-touch is available)
   - Mouse-friendly fallback (drag distance + timing) so it works on desktop builds
2. **Animation primitives** — `ImGui.Widgets/Animation/Tween.cs`, `Spring.cs`
   - Frame-rate-independent easing (`EaseOutCubic`, `Spring(stiffness, damping)`)
   - Driven by `ImGui.GetIO().DeltaTime`; values keyed by ImGui ID
   - Integrates with `ImGuiApp`'s PID frame limiter (request "active" framerate while a tween runs)
3. **Inertial scroll helper** — `ImGui.Widgets/Scroll/InertialScroll.cs`
   - Wraps `ImGui.SetScrollY` with velocity decay; consumed by carousels, pickers, lists
4. **Overlay / layer manager** — `ImGui.Widgets/Overlays/OverlayHost.cs`
   - Z-ordered host for toasts, bottom sheets, action sheets, drawers
   - Single `OverlayHost.Render()` call near the end of the user's frame

## Phase 1 — High-value, low-risk widgets

Pure layout / draw work, no gesture state required.

| Widget | File | Notes |
|---|---|---|
| `Switch` (iOS-style toggle) | `Switch.cs` | Animated thumb via Tween |
| `SegmentedControl` | `SegmentedControl.cs` | Like radio buttons, pill-shaped |
| `Chip` / `ChipGroup` | `Chip.cs` | Filter / input / choice variants |
| `Stepper` | `Stepper.cs` | `[-] 3 [+]` with hold-to-repeat |
| `Avatar` | `Avatar.cs` | Circular image, initials fallback, status dot |
| `Badge` | `Badge.cs` | Overlay decorator (count / dot) on any prior item |
| `Rating` | `Rating.cs` | Star / heart rating, half-step support |
| `RangeSlider` | `RangeSlider.cs` | Dual-handle, min / max gap |
| `PageIndicator` | `PageIndicator.cs` | Dots for carousels |
| `Card` | `Card.cs` | Shadowed container; `using (new Card(...))` RAII |
| `SkeletonLoader` | `SkeletonLoader.cs` | Shimmer placeholder driven by Tween |
| `PinInput` / `OtpInput` | `PinInput.cs` | N-digit boxed entry, auto-advance |

## Phase 2 — Gesture-dependent widgets

Depend on Phase 0's `GestureDetector`.

- **`SwipeableListItem`** — leading / trailing reveal actions, snap or commit on threshold
- **`PullToRefresh`** — wraps a scroll region, fires callback past threshold; spinner via animation primitives
- **`Carousel`** — paged horizontal scroller with snap + momentum + dots
- **`PickerWheel`** — iOS-style rotating cylinder picker (numbers, dates)
- **`DatePicker` / `TimePicker`** — built on `PickerWheel`
- **`LongPressMenu`** — context menu triggered by long press, with haptic-style scale animation

## Phase 3 — Overlays & navigation

Depend on `OverlayHost` (Phase 0).

- **`BottomSheet`** — slide-up modal with snap points (peek / half / full), drag handle
- **`ActionSheet`** — bottom-anchored button stack (iOS) with cancel
- **`Toast` / `Snackbar`** — auto-dismissing, queueable, optional action button
- **`NavigationDrawer`** — side panel with edge-swipe-to-open
- **`FloatingActionButton`** — anchored circular button; `SpeedDial` variant with expanding sub-actions
- **`BottomNavigationBar`** — fixed-bottom tab bar, integrates with existing `TabPanel`
- **`AppBar`** — top bar with title, leading / trailing actions, collapse-on-scroll variant
- **`SearchOverlay`** — fullscreen search with result list (composes `SearchBox`)

## Phase 4 — Polish

- **`CoachMark` / `Tutorial`** — spotlight overlay highlighting a previously-rendered widget (uses ImGui's last-item rect)
- **`Accordion` / `ExpandableListItem`** — animated height collapse
- **`StickyHeader`** — list section header that pins to top while scrolling
- **`InfiniteList`** — calls a `LoadMore` callback when scrolled near the bottom

## Cross-cutting concerns

- **Theming**: every new widget reads colors from the existing `ImGuiStyler` palette + the `ThemeProvider` semantic system. No hard-coded hex.
- **DPI**: sizes expressed in DIPs, scaled via `ImGuiApp.ScaleToPx()` (already used elsewhere).
- **Frame limiter**: animated widgets must signal the app to bump the active framerate while animating and release when done — otherwise idle FPS will stutter animations.
- **Tests**: each gesture / animation helper gets unit tests under `tests/ImGui.App.Tests/` using the existing mock GL provider; widget visual behavior verified via `ImGuiWidgetsDemo`.
- **Demo**: extend `examples/ImGuiWidgetsDemo/ImGuiWidgetsDemo.cs` with a "Mobile" tab grouping all new widgets.

## Suggested rollout order

1. Phase 0 infrastructure (1 PR per: gestures, animation, overlay host, inertial scroll).
2. Phase 1 batch — split into 2–3 PRs grouped by theme (form controls / decorators / loaders).
3. Phase 2 widgets — one PR per widget, each landing alongside its demo section.
4. Phase 3 overlays — start with `Toast` (easiest), then `BottomSheet`, then drawer / FAB / nav.
5. Phase 4 polish.

## Open questions to resolve before starting

- Multi-touch: does `Silk.NET` surface touch events on desktop, or do we go mouse-only for now and add touch later when a mobile backend (e.g. the iOS port) lands?
- Haptics: stub out an `IHapticFeedback` interface in `ImGuiApp` config so widgets can request feedback even if the desktop impl is a no-op.
- Where do shared animation / gesture helpers live — `ImGui.Widgets` itself, or a new `ImGui.Widgets.Foundation` sub-library to avoid bloating consumers who don't want mobile widgets?
