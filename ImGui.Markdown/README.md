# ktsu.ImGui.Markdown

ImGui.Markdown renders CommonMark markdown directly inside Dear ImGui, using [Markdig](https://github.com/xoofx/markdig) for parsing. It is a standalone package with no dependency on `ktsu.ImGui.App`, so it can be dropped into any Hexa.NET.ImGui application.

## Features

- **CommonMark parsing**: Full CommonMark syntax plus pipe tables, task lists, and autolinks, via a configured Markdig pipeline
- **Headings, emphasis, and code**: Headings scale from the live font size, so DPI and accessibility scaling are respected automatically; bold and italic fall back to faux styling when no matching font is registered
- **Lists**: Nested bullet, ordered, and task lists
- **Blockquotes and thematic breaks**: Rendered with an indent bar and a horizontal rule respectively
- **Tables**: Rendered with ImGui's native table API
- **Links**: Clickable, with an optional callback or automatic OS-open for http, https, and mailto schemes
- **Images**: Local images via a resolver callback; remote or unresolved images fall back to a placeholder box with the alt text
- **Two APIs**: A cached static `Render` for convenience, and a `MarkdownDocument` instance for hot render paths where the source is parsed once

## Installation

```bash
dotnet add package ktsu.ImGui.Markdown
```

## Quick Start

### Static render (cached by source)

`ImGuiMarkdown.Render` parses the given markdown and caches the result keyed by the source string, so calling it every frame with the same text does not re-parse it.

```csharp
using ktsu.ImGui.Markdown;
using Hexa.NET.ImGui;

ImGui.Begin("Markdown");
ImGuiMarkdown.Render("""
    # Hello, ImGui

    A **CommonMark** renderer for *Dear ImGui*, with `inline code` and [links](https://github.com/ktsu-dev).
    """);
ImGui.End();
```

### `MarkdownDocument` for hot paths

When the same markdown source is rendered every frame, parse it once into a `MarkdownDocument` and render that instance instead of relying on the source-keyed cache.

```csharp
using ktsu.ImGui.Markdown;

private static readonly MarkdownDocument ReadmeDocument = new("""
    # Changelog

    - Fixed a bug
    - Added a feature
    """);

// In the render loop:
ReadmeDocument.Render();
```

## Configuration

Pass a `MarkdownConfig` to either API to control fonts, links, images, and spacing. All members are optional. Without any configuration, the renderer uses faux emphasis styling and image placeholder boxes.

```csharp
MarkdownConfig config = new()
{
    FontResolver = ResolveFont,
    OnLinkClicked = url => Log.Info($"Clicked: {url}"),
    ImageResolver = ResolveImage,
};

ImGuiMarkdown.Render(markdown, config);
```

| Option | Type | Description |
| ------ | ---- | ----------- |
| `FontResolver` | `Func<MarkdownFontRole, float, ImFontPtr?>?` | Resolves a font for a typographic role (`Body`, `Bold`, `Italic`, `BoldItalic`, `Code`, `H1`-`H6`) at a target pixel size. Return `null` for a role to fall back to the current font at that size, with faux bold/italic styling applied for emphasis roles. |
| `OnLinkClicked` | `Action<string>?` | Invoked when a link is clicked. When `null`, http, https, and mailto links open with the OS default handler; other schemes are ignored. |
| `ImageResolver` | `Func<string, MarkdownImageResult?>?` | Resolves an image source string to a `MarkdownImageResult` (an ImGui texture ID and a draw size). Return `null`, or omit the resolver, to draw a placeholder box with the alt text instead. |
| `HeadingScales` | `IReadOnlyList<float>` | Size multipliers applied to the live body font size, H1 first. Defaults to `[2.0, 1.6, 1.35, 1.15, 1.0, 0.9]`. |
| `WrapWidth` | `float?` | Explicit wrap width in pixels. When `null`, the available content region width is used. |
| `ListIndentPixels` | `float` | Indentation applied per list nesting level, in pixels. Defaults to `20.0`. |
| `ParagraphSpacingPixels` | `float` | Vertical spacing added after paragraphs and blocks, in pixels. Defaults to `6.0`. |
| `LinkColor` | `ImGuiVector4?` | Explicit link color. When `null`, a theme-appropriate color is used. |

### Registering real bold and italic fonts

By default, bold text is drawn with a faux technique (a second offset draw call that thickens the glyphs), and italic text renders upright because no shear is applied. For crisper output, register named font variants at startup (for example through `ImGuiAppConfig.Fonts`) and resolve them per role:

```csharp
private static ImFontPtr? ResolveFont(MarkdownFontRole role, float pixelSize) => role switch
{
    MarkdownFontRole.Bold => boldFont,
    MarkdownFontRole.Italic => italicFont,
    MarkdownFontRole.BoldItalic => boldItalicFont,
    MarkdownFontRole.Code => monoFont,
    _ => null, // headings and body text keep the current font at pixelSize
};
```

### Resolving local images

`ImageResolver` receives the raw source string from the markdown image syntax (for example `![logo](ktsu.png)`) and returns the loaded texture ID and draw size:

```csharp
private static MarkdownImageResult? ResolveImage(string source)
{
    AbsoluteFilePath imagePath = AppContext.BaseDirectory.As<AbsoluteDirectoryPath>() / source.As<FileName>();
    if (!File.Exists(imagePath))
    {
        return null; // falls back to a placeholder box with the alt text
    }

    ImGuiAppTextureInfo texture = ImGuiApp.GetOrLoadTexture(imagePath);
    return new MarkdownImageResult(texture.TextureId, new Vector2(64, 64));
}
```

## Supported CommonMark Elements

- Headings (`#` through `######`)
- Paragraphs, with bold (`**`) and italic (`*`) emphasis
- Inline code (`` `code` ``) and fenced or indented code blocks
- Bullet lists, ordered lists, and task lists (`- [ ]` / `- [x]`), all with nesting
- Blockquotes
- Thematic breaks (`---`)
- Pipe tables
- Links, including autolinks
- Images

## v1 Limitations

- No syntax highlighting in code blocks; code is rendered in a plain monospace style
- No asynchronous remote image download; remote or unresolved image sources always show a placeholder box with the alt text
- Raw HTML blocks and inline HTML are rendered as escaped, literal text, not interpreted
- Faux italic renders upright when no italic font is supplied through `FontResolver`, since no glyph shear is applied

## Demo

See `examples/ImGuiMarkdownDemo/` for a runnable demo covering headings, emphasis, lists, quotes, code, tables, and images.

```bash
dotnet run --project examples/ImGuiMarkdownDemo
```

## Contributing

Contributions are welcome! For feature requests, bug reports, or questions, please open an issue on the GitHub repository. If you would like to contribute code, please open a pull request with your changes.

## License

ImGui.Markdown is licensed under the MIT License. See [LICENSE](LICENSE) for more information.
