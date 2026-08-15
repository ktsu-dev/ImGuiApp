# Hexa.NET.ImGui.Widgets integration, Tier 1 — Design

Date: 2026-08-14
Status: Approved

## Summary

Bring the [Hexa.NET.ImGui.Widgets](https://github.com/HexaEngine/Hexa.NET.ImGui.Widgets)
widget collection into `ktsu.ImGui.Widgets` by exposing a ktsu-idiomatic API surface that
delegates to Hexa's implementation. We wrap; we do not reimplement, and we do not vendor
source.

The full Hexa surface (base package plus Extras) is roughly 45 public types spanning four
distinct integration problems. This spec covers **Tier 1 only**: the pure immediate-mode
widgets, which are stateless `static` calls over value parameters and therefore a
mechanical (if careful) wrapping exercise. Tiers 2–4 are named in
[Deferred tiers](#deferred-tiers) and each gets its own spec.

Tier 1 also delivers a side-by-side comparison demo tab, because seven of the Hexa widgets
duplicate widgets we already ship and we need to see them driven by the same state before
deciding which implementation to keep.

## Decisions (locked during brainstorming)

- **Scope of the overall effort**: Everything Hexa offers, including the Extras package.
  Delivered in tiers; this spec is Tier 1.
- **Packaging**: Everything lands in the existing `ImGui.Widgets` project as
  `ImGuiWidgets` partials. No new project, no separate opt-in package.
- **API shape**: Idiomatic ktsu conventions with flat names and no vendor marker —
  `ImGuiWidgets.Spinner()`, not `ImGuiWidgets.Hexa.Spinner()`. Semantic colour and path
  types, no `unsafe` in the public surface.
- **Sequencing**: Tier 1 now; Tiers 2–4 specced later, informed by what Tier 1 settles
  about house style and by which duplicate widgets survive the comparison.
- **Material Icons**: Add glyph-range support to `ImGui.App.FontHelper`. Do not embed a
  font file.

## Accepted trade-off: dependency weight

Putting everything in `ImGui.Widgets` means every consumer of `ktsu.ImGui.Widgets`
transitively acquires `Microsoft.CodeAnalysis.CSharp.Scripting` (~10 MB, required by a
single Extras file, `CSharpSyntaxHighlight`), plus `Hexa.NET.Math`, `Hexa.NET.Utilities`
and `Hexa.NET.ImGuizmo`. This was raised during brainstorming and accepted. Recording it
here so it is not rediscovered as a surprise at pack time.

Tier 1 uses nothing from Extras. The Extras reference is added now only because the
single-package decision means it has to land eventually, and adding it once avoids a
second dependency-graph change later.

## Project & packaging changes

### `Directory.Packages.props`

New `PackageVersion` entries:

- `Hexa.NET.ImGui.Widgets` version `1.2.18`
- `Hexa.NET.ImGui.Widgets.Extras` version `1.0.9`

### `ImGui.Widgets/ImGui.Widgets.csproj`

New `PackageReference` entries: `Hexa.NET.ImGui.Widgets`,
`Hexa.NET.ImGui.Widgets.Extras`, `ktsu.Semantics.Paths`.

Compatibility notes, all verified against the upstream project files:

- Hexa's base package targets `net9.0;net9.0-android;net8.0;net7.0;net6.0;netstandard2.1;netstandard2.0`;
  Extras targets `net9.0;net9.0-android;net8.0`. Our TFMs are `net10.0;net9.0;net8.0`, so
  `net10.0` resolves the `net9.0` asset and `net9.0`/`net8.0` resolve exactly. No blocker.
- Hexa depends on `Hexa.NET.ImGui 2.2.8.4`; central package management pins `2.2.9`, which
  wins. `Hexa.NET.ImGuizmo 2.2.9` is already pinned in this repo.
- `ImGui.Widgets` already sets `<AllowUnsafeBlocks>True</AllowUnsafeBlocks>`, which the
  flame-graph marshalling needs.

### `ImGui.App/FontHelper.cs`

Add `internal static void AddMaterialIconRanges(ImFontGlyphRangesBuilderPtr builder)`
covering the Material Icons private-use block, mirroring the existing
`AddNerdFontRanges`.

**The overlap matters.** Hexa's `MaterialIcons` constants are Material's PUA ligature-font
codepoints: `Home = \xe9b2`, `Computer = \xe31e`, `Folder = \xe2c7`,
`CalendarToday = \xe935`. Our Nerd Font ranges already claim parts of that block — notably
Weather Icons at `0xE300–0xE3EB`, which swallows `Computer`. The Nerd Font "Material Design
Icons" range at `0xF500–0xFD46` is a *different* mapping and does not help: `\xe9b2` and
`\xe2c7` are simply absent, so they render as tofu today.

A single merged font cannot own the same codepoint twice. Therefore
`AddMaterialIconRanges` is designed to be used for a **separate merged font** rather than
combined into the Nerd Font builder. Apps that want both must accept that Material wins the
contested `0xE300–0xE3EB` span for whichever font is merged last. `ImGuiAppDemo`
demonstrates the layering and documents the conflict inline.

A public `uint* GetMaterialIconRanges()` accompanies it, mirroring the existing public
`GetEmojiRanges()` including its build-once caching. This is necessary rather than
decorative: `ImGuiAppConfig.Fonts` routes every font it loads through
`GetExtendedUnicodeRanges`, so a Material TTF registered that way would receive the *Nerd
Font* mapping and still leave `\xe9b2` unmapped. The supported path is therefore

```csharp
FontHelper.AddCustomFont(io, ttfBytes, 16f, FontHelper.GetMaterialIconRanges(), mergeWithPrevious: true);
```

using the already-public `AddCustomFont`.

No font file is embedded: no package weight, no third-party binary in the repo, no
licensing question. Apps supply their own Material Icons TTF (Apache 2.0, from
google/material-design-icons).

## Public API

All members are added to the existing `public static partial class ImGuiWidgets` in
namespace `ktsu.ImGui.Widgets`. One file per widget family, matching current convention.

### Net-new widgets

| File | Public members |
|---|---|
| `Spinner.cs` | `void Spinner(float radius, float thickness, Srgb color)` |
| `BufferingBar.cs` | `void BufferingBar(float value, Vector2 size, Srgb background, Srgb foreground)` |
| `Breadcrumb.cs` | `bool Breadcrumb(string id, ref string path)` |
| `DatePicker.cs` | `bool DatePicker(string label, ref DateTime date)`<br>`bool YearPicker(string label, ref DateTime date)` |
| `FlameGraph.cs` | `void FlameGraph(string label, IReadOnlyList<FlameGraphSample> samples, ref int selected, FlameGraphOptions? options = null)` |
| `FileTreeView.cs` | `bool FileTreeView(string id, Vector2 size, ref AbsoluteDirectoryPath currentFolder, AbsoluteDirectoryPath homeFolder)` |
| `Splitter.cs` | `bool HorizontalSplitter(string id, ref float height, float minHeight = float.MinValue, float maxHeight = float.MaxValue, float width = 0f, float thickness = 0f, float tolerance = 0f)`<br>`bool VerticalSplitter(string id, ref float width, float minWidth = float.MinValue, float maxWidth = float.MaxValue, float height = 0f, float thickness = 0f, float tolerance = 0f)` |
| `IconTreeNode.cs` | `bool IconTreeNode(string label, string icon, Color iconColor, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)` |
| `HexaButtons.cs` | `bool ToggleSwitch(string label, ref bool selected)`<br>`bool ToggleButton(string label, ref bool selected, Vector2 size = default)`<br>`bool TransparentButton(string label, Vector2 size = default)`<br>`bool InlineButton(string label, Vector2 min, Vector2 max, Vector2 anchor, InlineButtonPlacement placement = InlineButtonPlacement.None)` |
| `EnumCombo.cs` | `bool EnumCombo<T>(string label, ref T value) where T : struct, Enum` |
| `TextAlign.cs` | `void TextCenteredV(string text)` · `void TextCenteredH(string text)` · `void TextCenteredVH(string text)` |
| `ImageAlign.cs` | `void ImageCenteredV(nint textureId, Vector2 size)` · `void ImageCenteredH(…)` · `void ImageCenteredVH(…)` · `void ImageScaleTo(nint textureId, Vector2 imageSize, Vector2 destinationSize)` |
| `Tooltip.cs` | `void Tooltip(string description)` |

### Supporting public types

```csharp
/// One bar in a flame graph.
public readonly record struct FlameGraphSample(float Start, float End, byte Level, string Caption);

/// Optional presentation settings for FlameGraph.
public sealed record FlameGraphOptions
{
    public bool Flip { get; init; }
    public string? OverlayText { get; init; }
    public float ScaleMin { get; init; } = float.MaxValue;
    public float ScaleMax { get; init; } = float.MaxValue;
    public Vector2 GraphSize { get; init; }
}

/// Placement flags for InlineButton. Mirrors Hexa's InlineButtonFlags.
[Flags]
public enum InlineButtonPlacement
{
    None = 0,
    NoRounding = 1 << 0,
    FillSpace = 1 << 1,
}
```

### Conversion rules

Applied uniformly across every wrapper:

1. **Colour.** Hexa's `uint` (ABGR-packed) and `Vector4` colour parameters become
   `ktsu.Semantics` `Srgb` or `Color`, converted at the call boundary using the existing
   `ImGui.Color` extensions. `Srgb` is used where Hexa takes a packed `uint`; `Color`
   where it takes a `Vector4`.

   We use `Srgb.ToImGuiU32()` — which applies the global style alpha, matching
   `ImGui.GetColorU32` — rather than the pure-pack `Color.ToImGuiU32()`. This is
   deliberate: Hexa passes these straight to draw-list calls, and going through the
   style alpha means Hexa-backed widgets fade consistently with the rest of the UI under
   `ScopedDisable` and other alpha changes. The consequence is that every colour-taking
   wrapper requires an active ImGui context, which is already true of all of them.
2. **Strings.** Only `string` overloads are exposed. Hexa's `byte*` and
   `ReadOnlySpan<byte>` overloads are not surfaced.
3. **Textures.** `ImageAlign` takes `nint textureId` rather than Hexa's `ImTextureRef`,
   matching our existing `ImGuiWidgets.Image` / `ImageCentered` API. The wrapper
   constructs `new ImTextureRef(texId: textureId)` internally, which is already the idiom
   used by `Icon.cs` and `Avatar.cs`.
4. **Paths.** `FileTreeView` takes `AbsoluteDirectoryPath` from `ktsu.Semantics.Paths`,
   since it enumerates the real filesystem. **Deliberate exception:** `Breadcrumb` keeps
   `ref string path`, because Hexa's implementation only tokenizes on `/` and `\` for
   display and the path need not exist on disk. Forcing a validated path type there would
   reject legitimate virtual paths.
5. **Return values.** Hexa's `DatePicker.Draw` returns `void`. Our wrapper returns `bool`
   by capturing the `ref DateTime` before the call and comparing after, matching
   `YearPicker`, which already returns `bool`.
6. **Sizing.** Default sizes derive from `ImGui.GetFrameHeight()` /
   `ImGui.GetTextLineHeight()`, matching the house convention set by `Switch`,
   `RadialProgressBar` and `Rating`. No widget in this repo multiplies by `GlobalScale`,
   and these will not either.

   Because C# optional parameters must be compile-time constants, size parameters that
   need a style-derived default use `0f` as a "derive from style" sentinel, substituted
   inside the wrapper. This applies to the splitters' `thickness` and `tolerance`, where
   Hexa hardcodes DPI-unaware `2` and `8` pixels; we substitute style-derived equivalents
   so the grab area scales with the UI. A zero-thickness splitter has no legitimate use,
   so the sentinel is unambiguous.

7. **Overloads.** Hexa's defaulting ladders (four `VerticalSplitter` overloads, and so on)
   collapse into a single method with optional parameters, since every Hexa default is a
   compile-time constant.
8. **No `unsafe` in signatures.** Any pointer work is confined to wrapper internals.

### Flame graph marshalling

The largest adaptation. Hexa exposes:

```csharp
delegate void ValuesGetter(float* start, float* end, byte* level, byte** caption, void* data, int idx);
void PlotFlame(string label, ValuesGetter valuesGetter, void* data, int valuesCount,
               ref int selected, bool flip, int valuesOffset, string? overlayText,
               float scaleMin, float scaleMax, Vector2 graphSize);
```

Our wrapper accepts `IReadOnlyList<FlameGraphSample>` and supplies the callback itself. The
callback reads from the caller's list via a pinned `GCHandle` passed as `data`, writing
`Start`, `End` and `Level` directly and marshalling `Caption` to a UTF-8 buffer whose
lifetime spans the `PlotFlame` call. Caption buffers are allocated once per call into a
pooled native block sized to the sum of the captions' UTF-8 byte lengths, not per-sample,
to keep the per-frame allocation to one.

### Naming: coexistence with existing widgets

Seven Hexa widgets duplicate something we already ship. Both implementations stay in the
API until the comparison demo settles which to keep, so names must not collide:

| Hexa-backed name | Existing ktsu name it must avoid |
|---|---|
| `EnumCombo` | `Combo` |
| `ToggleSwitch` | `Switch` |
| `IconTreeNode` | the `Tree` nested type |
| `HorizontalSplitter` / `VerticalSplitter` | `DividerContainer`, `DividerZone` |
| `BufferingBar` / `Spinner` | `RadialProgressBar`, `SkeletonLoader` |
| `TextCenteredV` / `H` / `VH` | `TextCentered`, `TextCenteredWithin`, `Centered` |
| `ImageCenteredV` / `H` / `VH` | `ImageCentered`, `ImageCenteredWithin` |

Retiring a loser after the comparison is a breaking change to a published package and is
explicitly out of scope for Tier 1.

## Native buffer lifecycle

`Hexa.NET.ImGui.Widgets.TextHelper` allocates a 4 KB native buffer in its static
constructor and exposes `Release()`. Any wrapper that routes through `TextHelper` inherits
that allocation. Tier 1 does not introduce a shutdown hook for it — the buffer is a single
fixed 4 KB allocation for the process lifetime, and adding a disposal contract to
`ImGuiWidgets` (which is otherwise entirely static and lifecycle-free) would be a larger
design change than it earns. This is noted for Tier 2, where `WidgetManager`,
`DialogManager` and `PopupManager` all introduce real per-frame lifecycle and a shutdown
story has to be designed anyway.

## Comparison demo

A new tab in `examples/ImGuiWidgetsDemo`, titled **"Hexa vs ktsu"**.

Layout: a two-column grid, ours on the left, Hexa's on the right, one row per overlapping
pair. Both columns of a row bind to the *same* backing state, so divergent behaviour
(different hit areas, different keyboard handling, different response to the same value)
is visible live rather than inferred from two independent demos.

Rows: splitter, tree node, toggle, progress/spinner, combo, text centering, image
centering.

The net-new widgets — breadcrumb, date picker, year picker, flame graph, file tree view,
transparent button, inline button — have no counterpart to compare against and go into the
existing gallery tabs instead.

`ImGuiAppDemo` gains the Material Icons font-loading example described above, since
`DatePicker` and `FileTreeView` are unusable without it.

## Testing

`tests/ImGui.Widgets.Tests/HexaWidgetTests.cs`, following the existing convention in that
project.

**The binding constraint:** the existing test project never creates an ImGui context — no
test in `tests/` calls `ImGui.CreateContext`. `IconTests` is the model: the draw path stays
untested and a *pure helper extracted from it* (`ImGuiWidgets.CalcTextBlockSize`) is what
gets asserted. Anything touching `ImGui.GetStyle()`, `ImGui.GetFontSize()` or a draw list
would null-deref without a context.

That rules out testing colour conversion directly, because the `Srgb.ToImGuiU32()` chosen
in the conversion rules applies the global style alpha and therefore requires a live
context. Colour output is verified visually in the demo instead.

So each wrapper's context-dependent logic is split into a pure `internal static` helper
that the wrapper calls, and only the helper is unit-tested:

- **`FlattenCaptions(IReadOnlyList<FlameGraphSample>)`** → the UTF-8 caption block plus
  per-sample byte offsets. Assert offsets and bytes for: an empty list, a single sample,
  multi-byte UTF-8 captions, and an empty-string caption.
- **`ResolveSplitterMetrics(float thickness, float tolerance, float grabMinSize)`** → the
  sentinel substitution. Assert that `0f` yields the style-derived value, that a non-zero
  value passes through untouched, and that both parameters resolve independently.
- **`ResolveFlameGraphSelection(int selected, int sampleCount)`** → clamping. Assert
  in-range passthrough, negative, past-the-end, and an empty sample list.
- **`EnumComboNames<T>()`** → name resolution, delegating to Hexa's
  `ComboEnumHelper<T>.GetName`, which is pure. Assert a plain enum, an enum with explicit
  values, and a `[Flags]` enum.
- **Argument guards** — `ArgumentNullException` / `ArgumentException` for null and empty
  labels, and `ArgumentOutOfRangeException` for `min > max` on the splitters. These must be
  thrown by the guard clause *before* any ImGui call, which is what makes them testable
  without a context; that ordering is itself part of what the tests pin down.

`DatePicker`/`YearPicker` change detection has no pure helper worth extracting — the
comparison is a single `!=` on a `DateTime` — and is verified in the demo.

## File layout

```
ImGui.Widgets/
  Breadcrumb.cs        BufferingBar.cs     DatePicker.cs
  EnumCombo.cs         FileTreeView.cs     FlameGraph.cs
  HexaButtons.cs       IconTreeNode.cs     ImageAlign.cs
  Splitter.cs          Spinner.cs          TextAlign.cs
  Tooltip.cs
ImGui.App/
  FontHelper.cs                            (AddMaterialIconRanges)
examples/ImGuiWidgetsDemo/
  HexaComparisonTab.cs                     (new tab)
examples/ImGuiAppDemo/
  (Material Icons font-loading example)
tests/ImGui.Widgets.Tests/
  HexaWidgetTests.cs
```

Every new file carries the standard ktsu header, uses tabs, file-scoped namespaces,
explicit types, and full XML documentation, per `CLAUDE.md`.

## Deferred tiers

Named here so the decomposition is recorded, not to design them:

- **Tier 2 — stateful objects needing a per-frame pump.** `OpenFileDialog`,
  `SaveFileDialog`, `OpenFolderDialog`, `RenameDialog`, `MessageBox`, `DialogMessageBox`,
  `ImWindow`. These only function if `DialogManager.Draw()`, `MessageBoxes.Draw()`,
  `PopupManager.Draw()` and/or `WidgetManager.Draw()` run every frame — a lifecycle
  contract this library has no concept of, needing reconciliation with `ImGuiApp`'s render
  loop. Overlaps `ImGui.Popups.FilesystemBrowser` and `MessageOK`.
- **Tier 3 — callback-driven abstract classes with unsafe signatures.**
  `ImCurveEdit`/`CurveContext` and `ImSequencer`/`SequenceInterface`. Honouring "no unsafe
  in the public surface" means designing managed `ICurveSource`/`ISequenceSource`
  interfaces plus unsafe shim subclasses that marshal to Hexa's abstract bases. Also covers
  Extras' `ImGuiBezierWidget` and `ImGuiCurveEditor`, which take `BezierCurve`/`Curve` from
  `Hexa.NET.Math`.
- **Tier 4 — the text editor.** `TextEditor`, `TextEditorTab`, `TextSource`,
  `SyntaxHighlight` — ~4,600 LOC whose public surface traffics in `StdWString*` from
  `Hexa.NET.Utilities`. An application component rather than a widget, and the sole reason
  Roslyn is in the dependency graph.

## Out of scope (Tier 1)

- Any Tier 2/3/4 type.
- Retiring or deprecating any existing ktsu widget that the comparison may supersede.
- Embedding a Material Icons font file.
- A disposal or shutdown contract for `ImGuiWidgets`.
- Re-exporting Hexa's 3,600 `MaterialIcons` constants; consumers reach
  `Hexa.NET.ImGui.Widgets.MaterialIcons` transitively.
