// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SyntaxHighlighting;

using System.Collections.Generic;

/// <summary>
/// Code that has been tokenized once. Construct it from a source string and draw it each frame;
/// tokenizing happens only in the constructor. Use this for hot render paths, and
/// <see cref="SyntaxHighlighter.HighlightCached(string, string, int)"/> for convenience elsewhere.
/// </summary>
public sealed class HighlightedCode
{
	/// <summary>The original source text.</summary>
	public string Source { get; }

	/// <summary>The language the source was tokenized as.</summary>
	public LanguageDefinition Language { get; }

	/// <summary>The tab stop width the source was tokenized with.</summary>
	public int TabWidth { get; }

	/// <summary>The tokenized lines.</summary>
	public IReadOnlyList<HighlightedLine> Lines { get; }

	/// <summary>Tokenizes source text for a language.</summary>
	/// <param name="source">The source text.</param>
	/// <param name="language">The language name or alias; unknown names fall back to plain text.</param>
	/// <param name="tabWidth">The tab stop width used when expanding tabs to spaces.</param>
	public HighlightedCode(string source, string language, int tabWidth = SyntaxHighlighter.DefaultTabWidth)
	{
		Source = source ?? string.Empty;
		Language = LanguageRegistry.Resolve(language);
		TabWidth = tabWidth;
		Lines = HighlightCache.Tokenize(Source, Language, tabWidth);
	}
}
