# ktsu.SyntaxHighlighting

SyntaxHighlighting turns source text into classified token runs. It is UI-agnostic: nothing in the package draws, references a graphics API, or knows that Dear ImGui exists. `ktsu.ImGui.SyntaxHighlighting` is one renderer over it; any host that can draw colored text can be another.

## Features

- **Fifteen built-in languages**: C#, C, C++, JavaScript, TypeScript, Python, JSON, YAML, XML, HTML, CSS, SQL, shell, Lua, and plain text, each reachable by name or alias (`cs`, `c#`, `js`, `py`, `bash`, `yml`, …)
- **Data-driven definitions**: a language is a `LanguageDefinition` record — comment, string, keyword, operator and embedded-language rules — so an application can register its own, or derive a variant of a built-in with a `with` expression
- **Embedded languages**: XML in a doc comment, JSON in a fixture string and SQL in a query string are found inside the host language and highlighted in place, escapes and all
- **Two tokenizers**: a general lexer driven by the definition, and a structural one for angle-bracket markup, which has no keywords
- **Colors without a renderer**: `SyntaxTheme` holds one `ktsu.Semantics.Color.Color` per `TokenKind`, with `Dark` and `Light` built in, and leaves background, plain text and gutter unset so a host can fill them from its own theme
- **Cached tokenization**: `SyntaxHighlighter.HighlightCached` keys a bounded cache by source, language and tab width, and `HighlightedCode` tokenizes once for hot render paths

## Installation

```bash
dotnet add package ktsu.SyntaxHighlighting
```

## Quick Start

```csharp
using ktsu.SyntaxHighlighting;

foreach (HighlightedLine line in SyntaxHighlighter.Highlight(source, "csharp"))
{
    foreach (HighlightedToken token in line.Tokens)
    {
        Draw(token.Text, SyntaxTheme.Dark.ColorFor(token.Kind));
    }
}
```

Tokens tile the source: concatenating every token's text, with a newline between lines, reproduces the input exactly (with tabs expanded). Unknown language names never throw — they fall back to plain text, since the name usually comes from a markdown fence or a user-selected file.

For a render loop, tokenize once:

```csharp
private static readonly HighlightedCode Snippet = new("SELECT * FROM users;", "sql");
```

## Embedded languages

XML, JSON and SQL are written inside other languages' comments and strings constantly, and a lexer that stops at the quote leaves them a single flat color. Each `LanguageDefinition` carries `EmbeddedLanguages`: rules saying which language to look for, and where.

```csharp
/// <summary>Posts an order.</summary>          // tags are XML, the prose stays a comment
string body = @"{""id"": 7, ""paid"": true}";   // keys, numbers and constants are JSON
string query = "SELECT id FROM receipts";       // keywords are SQL
```

Three things make this safe to leave on:

- **Recognition is strict.** JSON is parsed, not pattern-matched, so a fragment or a brace-heavy sentence is rejected. Markup must open with a tag and close with `>`. SQL must open with a statement keyword *and* go on to use a second one, which is what keeps "Update the cache" from turning into a query — and SQL is only looked for in strings, never in comments.
- **Unclassified text keeps its host's color.** Prose between doc comment tags still reads as a comment, so a false positive costs a few punctuation glyphs rather than a paragraph.
- **Escapes are resolved for the tokenizer, not for the output.** The inner tokenizer sees `{"id": 7}` where the source holds `{\"id\": 7}`, but every token is re-sliced from the original text, so the backslashes are still drawn.

When recognition cannot work — a fragment, a heavily interpolated string — name the language outright with a hint comment, which applies to the next string literal:

```csharp
// lang=sql
string tail = "ORDER BY total DESC";
```

Embedding is one level deep, and applies to `LanguageDefinition.EmbeddedLanguages` on the host language only. Turn it off, or change it, by deriving a definition:

```csharp
LanguageRegistry.Register(BuiltInLanguages.CSharp with { EmbeddedLanguages = [] });

LanguageRegistry.Register(BuiltInLanguages.Python with
{
    EmbeddedLanguages = [.. BuiltInEmbeddedRules.Default, new EmbeddedLanguageRule
    {
        Language = "graphql",
        Hosts = EmbeddedHosts.StringLiteral,
        Matches = body => body.TrimStart().StartsWith("query ", StringComparison.Ordinal),
    }],
});
```

## Registering a language

A language is plain data, so nothing needs to be subclassed:

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

## Limitations

- Highlighting is lexical, not semantic: user-defined type names are not distinguished from other identifiers, and a call is recognized by the `(` that follows it
- Embedding is one level deep and is not applied by the markup tokenizer, so `<script>` and `<style>` bodies inside HTML are still treated as markup text
- Doc comments written one `///` line at a time are tokenized a line at a time, so an XML construct split across lines is classified per line
- Lines are not wrapped and nothing is drawn; this classifies code, a renderer decides what to do with it

## Contributing

Contributions are welcome! Feel free to open issues or submit pull requests.

## License

This project is licensed under the MIT License. See the [LICENSE.md](LICENSE.md) file for details.
