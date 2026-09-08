// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SyntaxHighlighting;

using System.Collections.Generic;

/// <summary>A line comment: everything from <see cref="Prefix"/> to the end of the line.</summary>
public sealed record LineCommentRule
{
	/// <summary>The text that opens the comment, for example <c>//</c>.</summary>
	public required string Prefix { get; init; }

	/// <summary>The kind emitted for the comment; use <see cref="TokenKind.DocComment"/> for doc comments.</summary>
	public TokenKind Kind { get; init; } = TokenKind.Comment;
}

/// <summary>A delimited comment that may span lines.</summary>
public sealed record BlockCommentRule
{
	/// <summary>The text that opens the comment, for example <c>/*</c>.</summary>
	public required string Open { get; init; }

	/// <summary>The text that closes the comment, for example <c>*/</c>.</summary>
	public required string Close { get; init; }

	/// <summary>Whether the comment may nest, as in Rust or Lua long comments.</summary>
	public bool Nested { get; init; }

	/// <summary>The kind emitted for the comment; use <see cref="TokenKind.DocComment"/> for doc comments.</summary>
	public TokenKind Kind { get; init; } = TokenKind.Comment;
}

/// <summary>A delimited literal such as a string, character, or raw string.</summary>
public sealed record StringRule
{
	/// <summary>The text that opens the literal, for example <c>"</c>, <c>@"</c>, or <c>"""</c>.</summary>
	public required string Open { get; init; }

	/// <summary>The text that closes the literal.</summary>
	public required string Close { get; init; }

	/// <summary>The escape character that neutralizes the next character, or <see langword="null"/> when the literal has none.</summary>
	public char? Escape { get; init; } = '\\';

	/// <summary>Whether a doubled closing delimiter escapes it, as in a C# verbatim string.</summary>
	public bool DoubledCloseEscapes { get; init; }

	/// <summary>Whether the literal may span lines; when <see langword="false"/> an unterminated literal ends at the newline.</summary>
	public bool AllowMultiline { get; init; }

	/// <summary>The kind emitted for the literal.</summary>
	public TokenKind Kind { get; init; } = TokenKind.StringLiteral;
}

/// <summary>
/// The data a tokenizer needs to highlight one language. Definitions are plain data, so an
/// application can register its own language without subclassing anything — see
/// <see cref="LanguageRegistry.Register(LanguageDefinition)"/>.
/// </summary>
public sealed record LanguageDefinition
{
	/// <summary>The canonical language name, for example <c>csharp</c>. Matching is case-insensitive.</summary>
	public required string Name { get; init; }

	/// <summary>Alternative names that resolve to this definition, for example <c>cs</c> and <c>c#</c>.</summary>
	public IReadOnlyCollection<string> Aliases { get; init; } = [];

	/// <summary>Whether keyword matching is case-sensitive.</summary>
	public bool CaseSensitive { get; init; } = true;

	/// <summary>The line comment rules, for example <c>//</c> and <c>///</c>.</summary>
	public IReadOnlyCollection<LineCommentRule> LineComments { get; init; } = [];

	/// <summary>The block comment rules, for example <c>/* */</c>.</summary>
	public IReadOnlyCollection<BlockCommentRule> BlockComments { get; init; } = [];

	/// <summary>The string and character literal rules.</summary>
	public IReadOnlyCollection<StringRule> Strings { get; init; } = [];

	/// <summary>Declaration and modifier keywords, emitted as <see cref="TokenKind.Keyword"/>.</summary>
	public IReadOnlyCollection<string> Keywords { get; init; } = [];

	/// <summary>Control-flow keywords, emitted as <see cref="TokenKind.ControlKeyword"/>.</summary>
	public IReadOnlyCollection<string> ControlKeywords { get; init; } = [];

	/// <summary>Built-in type names, emitted as <see cref="TokenKind.Type"/>.</summary>
	public IReadOnlyCollection<string> Types { get; init; } = [];

	/// <summary>Literal constants such as <c>true</c> and <c>null</c>, emitted as <see cref="TokenKind.Constant"/>.</summary>
	public IReadOnlyCollection<string> Constants { get; init; } = [];

	/// <summary>
	/// The character that opens a directive line (<c>#</c> for C-family preprocessors, <c>@</c> for CSS
	/// at-rules). A directive is only recognized when nothing but whitespace precedes it on the line.
	/// </summary>
	public char? DirectivePrefix { get; init; }

	/// <summary>Whether an identifier immediately followed by <c>(</c> is emitted as <see cref="TokenKind.Function"/>.</summary>
	public bool HighlightFunctionCalls { get; init; } = true;

	/// <summary>Whether an identifier or string immediately followed by <c>:</c> is emitted as <see cref="TokenKind.Property"/>.</summary>
	public bool HighlightPropertyNames { get; init; }

	/// <summary>Extra characters, beyond letters and digits, that may appear inside an identifier.</summary>
	public string IdentifierCharacters { get; init; } = "_";

	/// <summary>Extra characters, beyond letters, that may start an identifier.</summary>
	public string IdentifierStartCharacters { get; init; } = "_";

	/// <summary>Characters treated as operators; runs of them form a single token.</summary>
	public string OperatorCharacters { get; init; } = "+-*/%=<>!&|^~?:";

	/// <summary>Characters treated as structural punctuation; each forms its own token.</summary>
	public string PunctuationCharacters { get; init; } = "(){}[];,.";

	/// <summary>
	/// Whether the language is angle-bracket markup (XML, HTML) and should use the markup tokenizer
	/// rather than the general one. Markup definitions ignore the keyword, operator, identifier, and
	/// embedded-language members.
	/// </summary>
	public bool IsMarkup { get; init; }

	/// <summary>
	/// Languages to look for inside this one's comments and strings — XML in a doc comment, JSON in a
	/// fixture, SQL in a query string. Rules are tried in order and the first match wins; an empty
	/// list (the default) turns the feature off for this language.
	/// </summary>
	/// <remarks>
	/// A non-empty list also enables hint comments: <c>// lang=json</c> or <c>// language=sql</c> names
	/// the language of the next string literal outright, for a snippet the rules cannot recognize.
	/// Text the embedded language does not classify keeps its host's color, so prose in a doc comment
	/// still reads as a comment.
	/// </remarks>
	public IReadOnlyCollection<EmbeddedLanguageRule> EmbeddedLanguages { get; init; } = [];
}
