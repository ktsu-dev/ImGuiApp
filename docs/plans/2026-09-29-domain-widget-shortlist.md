# Plan: Domain Widget Shortlist for ImGui.Widgets

Date: 2026-09-29

## Goals

Add widgets to `ktsu.ImGui.Widgets` for audio, image, video and game engine tools, ranked by value against effort. Most candidates compose pieces that already exist (`Scope`, `DbMeter`, `Histogram`, `HandleTrack`, `CurveTrack`, `ImageCanvas`), so the early work is small.

Every widget here follows the library's existing conventions:

- Interaction logic lives in a state class tested without an ImGui context, as `HandleTrackState` and `CurveTrackState` are.
- The widget calls `ImGuiProbes.MarkItem` and gets its own isolation suite in `tests/ImGui.Widgets.UITests/`.
- Textures come from the host through a resolver (as `PropertyGridOptions.ThumbnailResolver` does), because Widgets does not reference `ImGui.App`.
- No new package dependencies, to stay clear of the dependency weight tracked in #384.

The audio and image rankings come from an earlier assessment of this repo. The video and game engine rows were added afterwards from reading the repo, and their value calls are judgement rather than measurement.

## Progress

- **Tier 1 — Build first**: ⬜ not started. The spectrum analyzer is already in progress.
- **Tier 2 — Build next**: ⬜ not started. The waveform and colour wheels are already in progress.
- **Tier 3 — Cheap extras**: ⬜ not started (#512).
- **Deferred**: no issues filed.

## Tier 1 — Build first: high value, small effort

These are independent of each other and can land in any order.

| Rank | Widget | Domain | Reuses | Effort | Issue |
|---|---|---|---|---|---|
| 1 | Levels control | Image | `Histogram` + `HandleTrack` | S | #499 |
| 2 | Gradient editor | Image, game VFX, heatmaps | `HandleTrack` for the stops | S–M | #500 |
| 3 | Spectrum analyzer | Audio | `Histogram` styling, `DbMeter`'s dB mapping | S | #501 |
| 4 | Before/after compare | Image, video | `ImageCanvas` / `ImageCanvasState`, `Splitter` | S | #502 |
| 5 | Piano keyboard | Audio | None (draw list) | S | #503 |

Notes:

- **Levels control** nearly exists already. `HandleTrack` keeps handles sorted with a minimum gap, which is exactly black < grey < white.
- **Gradient editor** is useful in three domains. Because `HandleTrack` never lets handles cross, a stop's index stays stable while dragging, so its colour can live in a parallel array.
- **Spectrum analyzer**: the caller supplies the FFT. `Histogram` auto-scales and has a linear x axis, so the log-frequency axis and dB scaling are the new work.
- **Before/after compare**: two texture ids sharing one `ImageCanvasState`, clipped at a split line.
- **Piano keyboard**: keep the key layout in a state class so a piano roll can reuse it later.

## Tier 2 — Build next: high value, medium effort

| Rank | Widget | Domain | Reuses | Effort | Issue |
|---|---|---|---|---|---|
| 6 | Waveform (long clip, playhead, loop region) | Audio, video | `Scope` drawing, `HandleTrack` for loop handles | M | #504 |
| 7 | Parametric EQ graph | Audio | `CurveTrack` pattern, `CurveTrackState`-style picking, `DbMeter` dB axis | M | #505 |
| 8 | Crop / transform overlay | Image | `ImageCanvasState.ViewportToImage`, `ImageRectInViewport` | M | #506 |
| 9 | Transport scrubber | Video | `HandleTrack` for in/out points, host thumbnail resolver | M | #507 |
| 10 | Log / console view | Game engine, tooling | `VirtualTable`, `ktsu.TextFilter` | M | #508 |
| 11 | Asset browser grid | Game engine | `Grid`, host thumbnail resolver | M | #509 |
| 12 | Envelope editor (ADSR/DAHDSR) | Audio | `CurveTrack` pattern | M | #510 |
| 13 | Colour wheels (lift/gamma/gain) | Image, video | `XYPad`-style state, `ktsu.Semantics.Color` | M | #511 |

Notes:

- **Waveform**: `Scope` plots one short block. This needs a min/max peak cache per zoom level and a view state class, which the transport scrubber should share.
- **Parametric EQ**: the caller passes the response as a function, so the drawn curve and the applied filter are the same function. The log-frequency axis can be shared with the spectrum analyzer.
- **Crop overlay** overlays `ImageCanvas` the way `HandleTrack` overlays `Histogram`.

## Tier 3 — Cheap extras: small effort, moderate value

Tracked together in #512. Split one out into its own issue when someone picks it up.

| Widget | Reuses | Effort |
|---|---|---|
| Timecode field (HH:MM:SS:FF, drag to scrub) | `Stepper`-style input | S |
| Frame-time graph with budget line | `Histogram` | S |
| Gain-reduction, correlation and goniometer meters | `DbMeter`, `Scope` | S |
| Step sequencer / drum grid | `Grid` | S |
| Channel fader with dB taper and meter | `DbMeter` + `RangeSlider` | S–M |
| Pixel loupe with RGBA readout | `ImageCanvas` zoom; host supplies pixels | S |
| Swatch palette with drag-reorder | Draw list | S |

## Deferred

No issues are filed for these yet.

| Widget | Reuses | Why wait |
|---|---|---|
| Spectrogram / waterfall | Host texture resolver | Needs a streaming texture path through the host, which nothing in Widgets does yet. |
| Video scopes (waveform monitor, vectorscope, RGB parade) | `Histogram`, `Scope` | Specialist; the heavy lifting is analysis the host must do. |
| Piano roll | `SequenceSource` idea with a pitch axis | Large. Build the piano keyboard first and reuse it as the roll's key column. |
| Mask / brush overlay | `ImageCanvas` | Needs a pixel write-back path through the host. |
| Sprite-sheet / tilemap slicer | `ImageCanvas` + a grid overlay | Niche; easy once the crop overlay exists. |

## Already covered or tracked elsewhere

- **3D viewport**: #417, blocked on the OpenGL backend in #414.
- **Transform gizmos**: ImGuizmo, which `ImGuiExtensionManager` already detects.
- **Inspectors**: `PropertyGrid`.
- **Dope sheets and keyframe curves**: `Sequencer` and `CurveEditor`.
- **Profilers**: `FlameGraph`.

## Suggested rollout order

1. Tier 1 as one batch, since each widget is small and none depends on another.
2. The waveform, then the transport scrubber on its shared view state.
3. The parametric EQ, reusing the spectrum analyzer's log-frequency axis.
4. The crop overlay, then the rest of Tier 2.
5. Tier 3 items as filler between larger pieces.
