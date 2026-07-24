# ktsu.ImGui.Markdown — Design

Date: 2026-07-23
Status: Approved

## Summary

A new package, `ktsu.ImGui.Markdown`, that renders CommonMark markdown inside Dear
ImGui using the Hexa.NET.ImGui bindings. Markdig parses the source into an AST; this
package owns only the rendering layer that walks that AST and issues ImGui draw calls
every frame (immediate mode).

The package is deliberately layered like `ImGui.Widgets`: it does **not** depend on
`ImGui.App`. It respects the application's DPI scaling and accessibility scaling by
deriving sizes from the live font size rather than by referencing App internals.

## Decisions (locked during brainstorming)

- **Feature scope**: Full CommonMark, plus common extensions (pipe tables, task lists,
  autolinks). Rendered elements: headings, bold/italic emphasis, inline code, fenced and
  indented code blocks, links, ordered/unordered/task lists (nested), blockquotes,
  thematic breaks (horizontal rules), tables, images, and HTML (rendered as escaped
  literal text, not interpreted).
- **Parsing**: Use the Markdig dependency (de-facto .NET CommonMark parser, MIT,
  netstandard2.0+). We do not hand-roll a parser.
- **Packaging**: A new standalone package `ktsu.ImGui.Markdown` (not folded into
  `ImGui.Widgets`), keeping the Markdig dependency out of Widgets.
- **Bold/italic**: Named fonts with a faux fallback. Emphasis is resolved through a
  configurable font resolver; when a variant is unavailable it is synthesized (faux-bold
  by double-drawing with a 1px offset, faux-italic by shear).
- **Links**: Clickable and theme-styled. On click, invoke an optional `OnLinkClicked`
  callback; if none is set, open the URL with the OS default handler. Only `http`,
  `https`, and `mailto` schemes are auto-opened; other schemes route to the callback only.
- **Images**: Local `src` values resolve through a config resolver callback that returns a
  texture handle and size. Remote (`http(s)`) or unresolved images render as a placeholder
  box with the alt text. No async HTTP download in v1.
- **API shape**: Both a static convenience method (`ImGuiMarkdown.Render`) with an internal
  source-keyed parse cache, and an explicit `MarkdownDocument` instance (parse once, render
  many) for hot paths.
- **Layering / scaling**: Decoupled from `ImGui.App`. Heading sizes derive from the live
  `ImGui.GetFontSize()` (already scaled by DPI `ScaleFactor` and accessibility
  `GlobalScale`), so scaling is respected without an App reference. The demo wires a
  `FontAppearance`-backed resolver.
- **v1 exclusions**: No syntax highlighting in code blocks (monospace only). No async
  remote image download. HTML is not interpreted.

## Project & packaging

- New project directory: `ImGui.Markdown/`.
- Package id: `ktsu.ImGui.Markdown`. Root namespace: `ktsu.ImGui.Markdown`.
- SDK: `Microsoft.NET.Sdk` + `ktsu.Sdk` (matching sibling libraries).
- Target frameworks: `net10.0;net9.0;net8.0`.
- `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` (ImGui font pointer work may require it).
- Dependencies (all via central package management in `Directory.Packages.props`):
  - `Hexa.NET.ImGui`
  - `Markdig` (**new** `PackageVersion` entry to add)
  - `ktsu.ScopedAction`
  - `Polyfill`
  - `ProjectReference` → `ImGui.Color`
  - `ProjectReference` → `ImGui.Styler`
- `InternalsVisibleTo` → `ktsu.ImGui.Markdown.Tests`.
- Assembly attributes live in a dedicated `AssemblyInfo.cs` (project convention).
- Add the project (and its test + example projects) to `ImGui.sln`.

## Public API

### Static entry point

```csharp
public static partial class ImGuiMarkdown
{
    // Convenience. Internally parses (via a bounded source-string-keyed cache so
    // re-parsing only happens when the text changes) and renders in one call.
    public static void Render(string markdown, MarkdownConfig? config = null);
}
```

### Document instance

```csharp
// Parse once, render many. For hot paths where the source is stable across frames.
public sealed class MarkdownDocument
{
    public MarkdownDocument(string markdown);      // parses immediately
    public void Render(MarkdownConfig? config = null);
}
```

Markdig also exposes a type named `MarkdownDocument` (`Markdig.Syntax.MarkdownDocument`).
Internally alias it (e.g. `using MarkdigAst = Markdig.Syntax.MarkdownDocument;`) so the
public `ktsu.ImGui.Markdown.MarkdownDocument` name is unambiguous.

### Configuration

```csharp
public sealed record MarkdownConfig
{
    // (role, targetPixelSize) -> font, or null to fall back to base font + faux styling.
    public Func<MarkdownFontRole, float, ImFontPtr?>? FontResolver { get; init; }

    // Invoked on link click. When null, http/https/mailto open via the OS default handler.
    public Action<string>? OnLinkClicked { get; init; }

    // Local image src -> texture handle + size. null / remote -> placeholder box.
    public Func<string, MarkdownImageResult?>? ImageResolver { get; init; }

    // Heading size multipliers applied to the live body font size. Index 0 = H1.
    public IReadOnlyList<float> HeadingScales { get; init; } // default {2.0,1.6,1.35,1.15,1.0,0.9}

    // Optional wrap width override; when null, uses ImGui.GetContentRegionAvail().X.
    public float? WrapWidth { get; init; }

    // Color / spacing overrides. Defaults resolve from the active theme via Palette.
    // e.g. link color, inline-code background, blockquote accent bar, code-block background,
    // paragraph spacing, list indent width.
}

public enum MarkdownFontRole { Body, Bold, Italic, BoldItalic, Code, H1, H2, H3, H4, H5, H6 }

public readonly record struct MarkdownImageResult(nint TextureId, System.Numerics.Vector2 Size);
```

## Rendering pipeline

Data flow:

```
string
  -> Markdig parse (pipeline: CommonMark + pipe tables + task lists + autolinks)
  -> AST (cached: keyed by source string for static Render; held directly by MarkdownDocument)
  -> walk AST every frame
  -> ImGui draw calls
```

### Block renderers

- Heading (H1–H6): push resolved heading font/size, render inline content, add spacing.
- Paragraph: inline content through the inline layout engine, paragraph spacing after.
- Lists (ordered / unordered / task): bullet or number or checkbox marker, nested via
  indent; supports tight and loose lists.
- Blockquote: left indent plus a vertical accent bar; nestable.
- Code block (fenced + indented): monospace font, background rectangle, horizontal scroll
  for long lines; no syntax highlighting in v1.
- Thematic break: a horizontal rule line.
- Table (pipe table extension): column layout with header row emphasis, cell alignment.
- HTML block: rendered as escaped literal text (not interpreted).

### Inline renderers

- Literal text.
- Emphasis / strong (italic / bold / bold-italic) via `FontResolver`; faux fallback.
- Inline code: monospace with a subtle background rect.
- Link: clickable, theme-colored, underline on hover; click handling per link rules above.
- Image: inline placement through `ImageResolver`, else placeholder with alt text.
- Autolink: treated as a link.
- Line break (hard/soft) and HTML inline (escaped literal).

### InlineLayout (core, unit-testable)

ImGui does not wrap runs that switch font or color mid-line, so this component builds line
flow manually. It accepts a sequence of runs (text + role + color + optional link/image
metadata), an available width, and an **injected** `Func<string, float> measure`. It emits
wrapped lines broken at word boundaries. Because width and measurement are injected, the
wrapping logic is pure and testable without a GPU. The renderer then draws each emitted
line with `ImGui.SameLine` between runs, applying per-run fonts, colors, code backgrounds,
and link hit-testing.

## Scaling, links, images, error handling

- **Scaling**: The base body pixel size is read live from `ImGui.GetFontSize()`, which the
  host application has already scaled by DPI (`ScaleFactor`) and accessibility
  (`GlobalScale`). Heading sizes are `body * HeadingScales[level]`. The `FontResolver`
  receives the computed target pixel size. No dependency on `ImGui.App` is required and
  scaling is honored automatically.
- **Links**: On click, `OnLinkClicked` is invoked when set. Otherwise, `http`/`https`/
  `mailto` URLs open with the OS default handler inside a try/catch that swallows failures.
  Other schemes never auto-open; they route to the callback only.
- **Images**: `ImageResolver` returns a texture id + size for local paths. A null result,
  or a remote/unsupported src, renders a labeled placeholder box showing the alt text.
- **Robustness**: null or empty input is a no-op. Markdig parsing is lenient and does not
  throw on arbitrary text. Missing `FontResolver` / `ImageResolver` degrade gracefully
  (faux styling / placeholder). Unknown or unsupported nodes render as escaped literal text.

## Testing

MSTest with semantic asserts (project convention). Tests focus on logic that does not need
a live ImGui/GPU context:

- `InlineLayout` word-wrapping given an injected measure function and a fixed width
  (boundary cases: single long word, exact-fit, multi-run lines, hard vs soft breaks).
- Parse-cache behavior for static `Render` (same source reuses AST; changed source
  re-parses; bounded size).
- Link-scheme filtering (which schemes auto-open vs callback-only).
- Image placeholder decisions (local resolved vs remote vs unresolved).
- `MarkdownConfig` defaults (heading scales, default colors resolving from theme).

ImGui-coupled draw code follows the existing repo test harness pattern.

## Example

New `examples/ImGuiMarkdownDemo/` (consistent with the per-library demos). It renders a
sample document exercising every supported element and wires:

- a `FontResolver` backed by `ImGui.App`'s `FontAppearance` (real bold/italic + heading
  sizes, demonstrating DPI and accessibility scaling), and
- an `ImageResolver` using `ImGui.App`'s texture loading for local images.

## File layout

```
ImGui.Markdown/
  ImGui.Markdown.csproj
  AssemblyInfo.cs
  ImGuiMarkdown.cs            // static entry point + Render + parse cache
  MarkdownDocument.cs         // instance: parse once, render many
  MarkdownConfig.cs           // config record + MarkdownFontRole + MarkdownImageResult
  Rendering/
    BlockRenderer.cs          // block-level AST walk
    InlineRenderer.cs         // inline-level AST walk + draw
    InlineLayout.cs           // pure word-wrap / line-flow builder
    MarkdownColors.cs         // theme color resolution + defaults
tests/ImGui.Markdown.Tests/
  (unit tests per Testing section)
examples/ImGuiMarkdownDemo/
  (showcase document + FontAppearance/texture wiring)
```

Files are split by responsibility to keep each focused and independently reasoned about.

## Out of scope (v1)

- Syntax highlighting in code blocks.
- Asynchronous remote image download and caching.
- Interpreting/rendering embedded HTML.
- Editing (this is a read-only renderer).
