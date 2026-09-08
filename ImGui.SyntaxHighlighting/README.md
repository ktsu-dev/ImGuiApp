# ktsu.ImGui.SyntaxHighlighting

ImGui.SyntaxHighlighting draws syntax-highlighted source code directly inside Dear ImGui. Like its sibling `ktsu.ImGui.Markdown`, it is a standalone package layered on `ktsu.ImGui.Color` only, with no dependency on `ktsu.ImGui.App`, so it can be dropped into any Hexa.NET.ImGui application.

Tokenizing lives one layer down, in the renderer-agnostic [`ktsu.SyntaxHighlighting`](../SyntaxHighlighting/README.md): languages, themes, the token kinds and the embedded-language rules are all defined there and know nothing about ImGui. This package is the drawing half — a background panel, a line-number gutter, and colored token runs painted into the window draw list.

## Features

- **Fifteen built-in languages**: C#, C, C++, JavaScript, TypeScript, Python, JSON, YAML, XML, HTML, CSS, SQL, shell, Lua, and plain text, each reachable by name or alias (`cs`, `c#`, `js`, `py`, `bash`, `yml`, …)
- **Data-driven definitions**: a language is a `LanguageDefinition` record — comment, string, keyword and operator rules — so an application can register its own, or derive a variant of a built-in with a `with` expression
- **Embedded languages**: XML in a doc comment, JSON in a fixture string and SQL in a query string are found inside the host language and highlighted in place, or named outright with a `// lang=json` hint comment
- **Theme-aware colors**: by default the palette is picked per frame from the luminance of the ImGui window background, and unset palette entries (background, plain text, gutter) come from the active ImGui theme, so code keeps matching the surrounding UI
- **Line numbers**: an optional right-aligned gutter that does not shift the code column as the digit count grows
- **Cached tokenization**: the static `Render` caches by source text, and `HighlightedCode` tokenizes once for hot render paths
- **Tokens without rendering**: `Highlight` returns the classified runs for callers that want to draw, export, or test them themselves
- **Markdown integration**: pairs with `ktsu.ImGui.Markdown`'s `CodeBlockRenderer` hook to highlight fenced code blocks, with neither library depending on the other

## Installation

```bash
dotnet add package ktsu.ImGui.SyntaxHighlighting
```

## Quick Start

### Static render (cached by source)

`ImGuiSyntaxHighlighting.Render` tokenizes the given code and caches the result keyed by the source text, language and tab width, so calling it every frame with the same code does not re-tokenize it.

```csharp
using ktsu.ImGui.SyntaxHighlighting;
using Hexa.NET.ImGui;

ImGui.Begin("Code");
ImGuiSyntaxHighlighting.Render("""
    /// <summary>Greets the world.</summary>
    public static void Main()
    {
        Console.WriteLine("Hello, ImGui");
    }
    """, "csharp");
ImGui.End();
```

The doc comment's tags in that snippet are highlighted as XML while its prose stays comment-colored — see [embedded languages](../SyntaxHighlighting/README.md#embedded-languages) for what is recognized and how to change it.

### `HighlightedCode` for hot paths

When the same source is rendered every frame, tokenize it once and render that instance instead of relying on the source-keyed cache. `HighlightedCode` comes from `ktsu.SyntaxHighlighting`; `Render` is an extension this package adds to it.

```csharp
private static readonly HighlightedCode Snippet = new("SELECT * FROM users;", "sql");

// In the render loop:
Snippet.Render();
```

### Tokens without rendering

`Highlight` forwards to `SyntaxHighlighter.Highlight`, so a caller with no ImGui context can reference `ktsu.SyntaxHighlighting` alone and skip this package entirely.

```csharp
foreach (HighlightedLine line in ImGuiSyntaxHighlighting.Highlight(source, "python"))
{
    foreach (HighlightedToken token in line.Tokens)
    {
        Console.WriteLine($"{token.Kind}: {token.Text}");
    }
}
```

## Configuration

`SyntaxHighlightConfig` is a record; every member is optional.

```csharp
SyntaxHighlightConfig config = new()
{
    ShowLineNumbers = true,
    FirstLineNumber = 1,
    TabWidth = 4,
    Theme = null,                  // null follows the ImGui theme; or SyntaxTheme.Dark / .Light
    FontResolver = size => monoFont,
    ShowBackground = true,
    BackgroundRounding = 3.0f,
    PaddingPixels = 6.0f,
    LineSpacingPixels = 2.0f,
    GutterSpacingPixels = 10.0f,
    FontSizePixels = null,         // null keeps the current size, so DPI scaling is followed
    Width = null,                  // null uses the available content width
};
```

Code is never wrapped: a line longer than the block is clipped by the surrounding window, so wrap the call in a horizontally scrolling child window when long lines must stay reachable.

### Themes

`SyntaxTheme` holds one color per `TokenKind` as a semantic `ktsu.Semantics.Color.Color`. `SyntaxTheme.Dark` and `SyntaxTheme.Light` are the built-ins, and any theme can be derived with a `with` expression:

```csharp
SyntaxTheme theme = SyntaxTheme.Dark with
{
    Comment = Color.FromHex("#7f848e"),
    Keyword = Color.FromHex("#c678dd"),
};
```

Leaving `Background`, `Plain` or `LineNumber` unset makes the renderer take them from the ImGui theme (`FrameBg`, `Text` and `TextDisabled`).

### Registering a language

Definitions live in `ktsu.SyntaxHighlighting`, and a language is plain data, so nothing needs to be subclassed:

```csharp
LanguageRegistry.Register(new LanguageDefinition
{
    Name = "ini",
    Aliases = ["conf", "cfg"],
    LineComments = [new LineCommentRule { Prefix = ";" }],
    Strings = [new StringRule { Open = "\"", Close = "\"" }],
    Constants = ["true", "false", "yes", "no"],
    HighlightPropertyNames = true,
    HighlightFunctionCalls = false,
});
```

Unknown language names never throw — they fall back to plain text, since the name usually comes from a markdown fence or a user-selected file.

## Highlighting markdown code blocks

`ktsu.ImGui.Markdown` exposes a `CodeBlockRenderer` hook that receives a fence's info string and its text. Pointing it at this package highlights fenced code without either library depending on the other:

```csharp
MarkdownConfig markdown = new()
{
    CodeBlockRenderer = (language, code) =>
        ImGuiSyntaxHighlighting.Render(code, language ?? "text", highlightConfig),
};
```

## Limitations

- Highlighting is lexical, not semantic: user-defined type names are not distinguished from other identifiers, and a call is recognized by the `(` that follows it
- Embedded languages are found in comments and strings only, one level deep — script and style bodies inside HTML are still treated as markup text
- Lines are not wrapped, and there is no built-in scrolling, selection, or editing; this renders code, it is not a text editor

## Demo

See `examples/ImGuiSyntaxHighlightingDemo/` for a runnable demo covering the language samples, the line-number gutter, palette switching, and markdown code blocks.

```bash
dotnet run --project examples/ImGuiSyntaxHighlightingDemo
```

## Acknowledgments

- [ktsu.SyntaxHighlighting](../SyntaxHighlighting/README.md) - The renderer-agnostic tokenizer, languages and themes this draws
- [Dear ImGui](https://github.com/ocornut/imgui) - The immediate mode GUI library this draws into
- [Hexa.NET.ImGui](https://github.com/HexaEngine/Hexa.NET.ImGui) - The .NET bindings for Dear ImGui that this package is built on

## Contributing

Contributions are welcome! For feature requests, bug reports, or questions, please open an issue on the GitHub repository. If you would like to contribute code, please open a pull request with your changes.

## License

ImGui.SyntaxHighlighting is licensed under the MIT License. See [LICENSE](LICENSE) for more information.
