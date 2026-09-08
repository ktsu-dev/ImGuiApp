// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SyntaxHighlighting;

using System.Collections.Generic;

/// <summary>
/// The renderer-agnostic entry point: turns source text into classified token runs that any UI can
/// draw. Nothing here knows about a graphics API — <c>ktsu.ImGui.SyntaxHighlighting</c> is one such
/// renderer, and another host only needs a way to draw colored text.
/// </summary>
public static class SyntaxHighlighter
{
	/// <summary>The tab stop width used when a caller does not choose one.</summary>
	public const int DefaultTabWidth = 4;

	/// <summary>
	/// Tokenizes code, without consulting the shared cache. Use this for one-off work; a render loop
	/// wants <see cref="HighlightCached(string, string, int)"/> or a <see cref="HighlightedCode"/>.
	/// </summary>
	/// <param name="code">The source text.</param>
	/// <param name="language">The language name or alias; unknown names classify everything as plain.</param>
	/// <param name="tabWidth">The tab stop width used when expanding tabs to spaces.</param>
	/// <returns>The tokenized lines.</returns>
	public static IReadOnlyList<HighlightedLine> Highlight(string code, string language, int tabWidth = DefaultTabWidth) =>
		HighlightCache.Tokenize(code, LanguageRegistry.Resolve(language), tabWidth);

	/// <summary>
	/// Tokenizes code through a bounded cache keyed by source text, language and tab width, so an
	/// immediate-mode loop rendering the same source every frame tokenizes it once.
	/// </summary>
	/// <param name="code">The source text.</param>
	/// <param name="language">The language name or alias; unknown names classify everything as plain.</param>
	/// <param name="tabWidth">The tab stop width used when expanding tabs to spaces.</param>
	/// <returns>The tokenized lines.</returns>
	public static IReadOnlyList<HighlightedLine> HighlightCached(string code, string language, int tabWidth = DefaultTabWidth) =>
		HighlightCache.GetOrTokenize(code ?? string.Empty, LanguageRegistry.Resolve(language), tabWidth);
}
