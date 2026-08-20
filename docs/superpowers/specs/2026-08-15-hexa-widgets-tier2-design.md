# Hexa widgets Tier 2 — deferred-drawing dialogs and windows

Status: proposed
Date: 2026-08-15
Supersedes nothing. Follows `2026-08-14-hexa-widgets-tier1-design.md`, which named this tier and deferred it.

## Summary

Tier 1 wrapped Hexa's immediate-mode widgets: call a function, get a result, no state between
frames. Tier 2 wraps the types that do not work that way. A Hexa dialog is an object that
registers itself with a static manager, survives across frames, and is drawn by a pump the
application must run every frame. Nothing in `ktsu.ImGui.Widgets` has ever had that shape.

This spec defines the pump, the lifecycle contract around it, and the wrappers for:

`OpenFileDialog`, `SaveFileDialog`, `OpenFolderDialog`, `RenameDialog`, `MessageBox`,
`DialogMessageBox`, and `ImWindow` (exposed as `DockedWindow`).

It also extends the "Hexa vs ktsu" comparison tab to cover the file dialogs and message boxes,
which overlap `ImGui.Popups`' `FilesystemBrowser` and `MessageOK`.

## Decisions (locked during brainstorming)

- **Scope**: wrap all seven types, including the ones that overlap `ImGui.Popups`, and extend the
  comparison tab so the overlap can be judged rather than argued.
- **Lifecycle model**: adopt Hexa's — a global manager plus an explicit per-frame pump. Rejected:
  forcing Hexa's dialogs into `ImGui.Popups`' per-instance `Open()`/`ShowIfOpen()` idiom.
- **Pump placement**: the consumer calls it from their own `OnRender`. Rejected: having `ImGuiApp`
  call it automatically.
- **Naming**: `DockedWindow` for Hexa's `ImWindow`.

## Why the pump is not optional

`Dialog.Show()` adds the dialog to `DialogManager`'s static list. `Dialog.Close()` enqueues it
onto a static removal queue and fires the result callback. Only `DialogManager.Draw()` drains
that queue.

So a consumer cannot simply not pump. Without it, both static collections grow for the life of
the process and no dialog is ever drawn. This is not a feature that degrades when unused; it is
a contract that must be honoured.

Tier 1 already proved this the hard way. `ToggleSwitch` registers state with `AnimationManager`
and reads it back, but nothing advanced the clock, so the switch rendered inverted from its first
click — shipped in v3.4.0 and fixed in #303 with a deliberately temporary frame-guarded tick. The
pump defined here is the permanent answer, and it retires that stopgap.

## Architecture

### Two pumps, mutually exclusive

`ImGuiWidgets.DrawDeferred()` — the default.

Calls `DialogManager.Draw()`, `MessageBoxes.Draw()`, `PopupManager.Draw()`,
`AnimationManager.Tick()`. Draws every dialog, message box and popup. Imposes no layout.

`ImGuiWidgets.DrawDeferredDocked()` — opt-in, required only for `DockedWindow`.

Calls Hexa's `WidgetManager.Draw()`, which creates a dockspace over the main viewport, draws
registered windows, and then does everything `DrawDeferred()` does.

They are mutually exclusive: `WidgetManager.Draw()` already calls the other three managers, so
running both in one frame draws every dialog twice.

The split exists because `ImWindow` cannot be separated from the dockspace. `ImWindow.Show()`
calls `WidgetManager.Register(this)`, `ImWindow` positions itself with
`ImGui.SetNextWindowDockID(WidgetManager.DockSpaceId)`, and only `WidgetManager.Draw()` iterates
the registered list. Folding that into the default pump would impose `DockSpaceOverViewport` on
every consumer of this library — a layout decision no widget library should make for an
application.

### Why not have `ImGuiApp` pump automatically

`ImGui.Widgets` does not reference `ImGui.App`, and `ImGui.App` is the foundation every other
library sits on. Making `ImGuiApp` drive the pump would invert that dependency.

It is also unnecessary: `ImGuiAppConfig.OnRender` already *is* the per-frame hook. The consumer
calling `ImGuiWidgets.DrawDeferred()` at the end of their render callback needs no new API.

### Diagnostics: make the failure loud

Forgetting the pump produces no error and no dialog. That is the same silent-failure class as
the `OnConfigureFonts` bug (a font registered too late, never rasterised) and the `FlameGraph`
null-pointer writes (invisible because no test exercises a draw path). Both cost real time this
tier should not repeat.

The wrappers know when something is open, so:

| Condition | Response |
|---|---|
| Neither pump has *ever* run, and a dialog is being shown | Throw `InvalidOperationException` naming the missing call |
| A pump has run before, but not this frame | `Debug`/`Trace` once |
| Both pumps ran in one frame | `Debug`/`Trace` once, naming the double-draw |

The distinction is deliberate. "Never wired up" has no legitimate reading, so it fails hard at
the first `Show()`. "Skipped one frame" can happen legitimately (a branch that returned early),
so it must not take down the application mid-render.

## Public API

All dialog types are nested in `ImGuiWidgets`, matching `ImGuiWidgets.TabPanel` and
`ImGuiPopups.InputString`.

| Type | Shape |
|---|---|
| `ImGuiWidgets.OpenFileDialog` | instance; `Show(Action<FileDialogOutcome>)` |
| `ImGuiWidgets.SaveFileDialog` | instance; `Show(Action<FileDialogOutcome>)` |
| `ImGuiWidgets.OpenFolderDialog` | instance; `Show(Action<FolderDialogOutcome>)` |
| `ImGuiWidgets.RenameDialog` | instance; `Show(Action<RenameOutcome>)` |
| `ImGuiWidgets.DialogMessageBox` | instance; `Show(Action<DialogOutcome>)` |
| `ImGuiWidgets.ShowMessageBox(...)` | static; `MessageBox` is a static factory upstream |
| `ImGuiWidgets.DockedWindow` | abstract; subclass and override `Name` / `DrawContent()` |

Conversion rules carried forward from Tier 1: no `unsafe` in public signatures, no Hexa type in
the public surface, semantic types at the boundary. Paths are returned as `AbsoluteFilePath` /
`AbsoluteDirectoryPath`, not strings, so a result is usable without re-parsing — the same choice
`FileTreeView` already made.

### Outcome types

Each callback receives one record rather than a bare enum, so the answer and the value it
produced arrive together and cannot be read out of order:

```csharp
public sealed record FileDialogOutcome(
    DialogOutcome Outcome,
    AbsoluteFilePath? Path,                        // Hexa's SelectedFile
    IReadOnlyList<AbsoluteFilePath> Selection);    // Hexa's Selection; empty unless AllowMultipleSelection

public sealed record FolderDialogOutcome(
    DialogOutcome Outcome,
    AbsoluteDirectoryPath? Path);

public sealed record RenameOutcome(
    DialogOutcome Outcome,
    AbsoluteFilePath? Destination,                 // Hexa's DestinationPath
    Exception? Error);                             // Hexa's Exception
```

`RenameOutcome.Error` exists because `RenameDialog` is not just a text prompt: unless
`NoAutomaticMove` is set it performs the move itself, and captures any failure into an
`Exception` property instead of throwing. A wrapper that returned only an outcome enum would
silently discard the reason a rename failed. Surfacing it in the record makes that unmissable.

### The result enums do not agree, and the wrapper must not pretend they do

Hexa has two result types with incompatible precision:

```csharp
// Dialogs and DialogMessageBox
enum DialogResult { None = -1, Ok = 0, Cancel = 1, Failed = 2, Yes = 0, No = 3 }
//                                                             ^^^^^^^ aliases Ok

// MessageBox
enum MessageBoxResult { None, Ok, Cancel, Yes, No }   // all distinct
```

`DialogResult.Yes` and `DialogResult.Ok` are the same value. They cannot be distinguished at
runtime and cannot both appear in one `switch`. So a `DialogMessageBox` configured as `YesNo`
reports its affirmative answer as `0`, which is indistinguishable from `Ok`. A `MessageBox`
configured as `YesNo` reports a genuinely distinct `Yes`.

We expose one enum with distinct members:

```csharp
public enum DialogOutcome { None, Ok, Cancel, Failed, Yes, No }
```

and recover the lost distinction from context rather than from the value alone:

- From `MessageBoxResult`: a direct 1:1 mapping; nothing is lost.
- From `DialogResult`: `0` maps to `Yes` when the dialog was configured as `YesNo`,
  `YesNoCancel` or `YesCancel`, and to `Ok` otherwise. The dialog's own configuration is the only
  place that information survives.

This mapping is a pure function of `(rawResult, dialogType)`. That matters more than it looks:
it makes the single most error-prone part of this tier fully unit-testable, without an ImGui
context. Tier 1 had no such seam and shipped two bugs because of it.

Rejected alternative: collapsing to `Affirmative`/`Negative` so no distinction is ever claimed.
Honest, but it degrades `MessageBox`, which does carry the distinction, and it reads poorly for
an `Ok`-only dialog.

## Upstream behavior we surface rather than hide

These are Hexa's characteristics. The comparison tab exists to judge Hexa's widgets on their
merits, so the wrappers must not quietly paper over them — and must not quietly inherit blame for
them either. Both get documented on the wrapper.

- **The file dialogs require a Material Icons font.** `FileDialogBase.DrawMenuBar` draws
  `MaterialIcons.Home`. Same requirement as `FileTreeView`, same answer:
  `ImGuiAppConfig.OnConfigureFonts` with `FontHelper.GetMaterialIconRanges()`. Without it the
  navigation bar renders placeholder boxes.
- **`FileDialogBase.Close()` blocks the UI thread.** It calls `refreshTask?.Wait()` to unwind the
  async directory scan started by `Show()`. On a slow or network directory this stalls a frame.
  `ImGui.Popups`' `FilesystemBrowser` has no equivalent stall, which is a real difference the
  comparison should show.
- **`PopupManager.Remove` is broken upstream.** It reads
  `int idx = popups.IndexOf(popup); if (idx != -1) { return; }` — returning when the popup *is*
  found, and otherwise falling through to `RemoveAt(-1)`, which throws. It never removes anything.
  Tier 2 must not route through it; `IPopup.Close()` plus the manager's own draw-time cleanup is
  the working path. Worth an upstream issue.

## Comparison demo

The existing "Hexa vs ktsu" tab gains a dialogs section. Dialogs are modal windows, so they
cannot sit in the two-column table Tier 1 used. The binding contract is preserved instead: each
row is a pair of buttons writing to **one shared backing field**, so opening ktsu's and then
Hexa's shows the same state round-tripping through both.

| Row | ktsu | Hexa |
|---|---|---|
| Open file | `ImGuiPopups.FilesystemBrowser` | `OpenFileDialog` |
| Pick folder | `ImGuiPopups.FilesystemBrowser` | `OpenFolderDialog` |
| Save file | `ImGuiPopups.FilesystemBrowser` | `SaveFileDialog` |
| Message | `ImGuiPopups.MessageOK` | `ShowMessageBox` |

Net-new gallery: `RenameDialog`, `DialogMessageBox`.

`DockedWindow` is demoed in `ImGuiAppDemo`, not `ImGuiWidgetsDemo`. The two pumps cannot both run
in one application, and a dockspace is an app-shell concern, which is what `ImGuiAppDemo`
demonstrates. This avoids a runtime pump toggle whose only purpose would be to work around the
constraint.

`ImGuiWidgetsDemo` must call `ImGuiWidgets.DrawDeferred()` in its render loop, and must register
a Material Icons font via `OnConfigureFonts` — it already does the latter as of #299.

## Testing

The draw paths need a live ImGui context and remain untestable, consistent with the Tier 1
constraint. What is pure, and therefore gets real tests:

- `DialogOutcome` mapping from `MessageBoxResult` — every member, so a new upstream value cannot
  silently map to a wrong one.
- `DialogOutcome` mapping from `(DialogResult, DialogMessageBoxType)` — in particular that `0`
  resolves to `Yes` for the three Yes-flavoured types and `Ok` for the rest.
- Path round-tripping through `AbsoluteFilePath` / `AbsoluteDirectoryPath`, including drive roots
  and UNC paths.
- Pump state: never-ran detection, same-frame double-pump detection, frame-advance behavior.

Standing rule carried from the Tier 1 `FlameGraph` Critical: for any callback- or
marshaling-shaped upstream API, read **every call site of the callback**, not just its
signature. That bug was a guaranteed process crash and was invisible to both the compiler and the
test suite.

## File layout

```
ImGui.Widgets/DeferredDrawing.cs        the two pumps + diagnostics
ImGui.Widgets/Dialogs/DialogOutcome.cs  result enum + mapping (pure, tested)
ImGui.Widgets/Dialogs/FileDialogs.cs    Open/Save/Folder
ImGui.Widgets/Dialogs/RenameDialog.cs
ImGui.Widgets/Dialogs/MessageDialogs.cs DialogMessageBox + ShowMessageBox
                                        (not "MessageBoxes.cs" — Hexa has a type by that name)
ImGui.Widgets/DockedWindow.cs
```

Matches the existing `Animation/`, `Gestures/`, `Overlays/`, `Scroll/` subfolder convention.

`HexaAnimationPump.cs` is **deleted** — both pumps tick the animation clock, and its documented
exit condition ("when a real per-frame pump lands") is met. Leaving it would double animation
speed.

## Out of scope

- Tier 3 (`ImCurveEdit`, `ImSequencer`, Extras' bezier and curve editors) and Tier 4
  (`TextEditor`).
- Retiring `ImGui.Popups`' `FilesystemBrowser` or `MessageOK`. The comparison informs that
  decision; it does not pre-empt it.
- Fixing `PopupManager.Remove` upstream. We route around it.
- An `async`/`Task`-based dialog API. Callbacks match both libraries' existing shape; an awaitable
  layer can be added later without breaking it.
- A disposal contract for `ImGuiWidgets` as a whole, still deferred from Tier 1. `DockedWindow`
  does surface `Dispose` from `IImGuiWindow`, which is handled per-instance.
