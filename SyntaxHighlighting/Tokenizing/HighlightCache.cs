// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SyntaxHighlighting;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// Caches highlighted lines keyed by language, tab width and source text, so the immediate-mode
/// render loop does not re-tokenize unchanged code every frame. The cache is bounded and evicts in
/// insertion order.
/// </summary>
internal static class HighlightCache
{
	private const int MaxCacheEntries = 32;

	private static readonly Dictionary<CacheKey, IReadOnlyList<HighlightedLine>> Cache = [];
	private static readonly Queue<CacheKey> InsertionOrder = new();

#if NET9_0_OR_GREATER
	private static readonly Lock Gate = new();
#else
	private static readonly object Gate = new();
#endif

	/// <summary>Returns cached lines for the source, tokenizing and caching them on first use.</summary>
	/// <param name="source">The source text.</param>
	/// <param name="language">The resolved language definition.</param>
	/// <param name="tabWidth">The tab stop width used when expanding tabs.</param>
	/// <returns>The highlighted lines.</returns>
	public static IReadOnlyList<HighlightedLine> GetOrTokenize(string source, LanguageDefinition language, int tabWidth)
	{
		CacheKey key = new(source ?? string.Empty, language, tabWidth);
		lock (Gate)
		{
			if (Cache.TryGetValue(key, out IReadOnlyList<HighlightedLine>? cached))
			{
				return cached;
			}

			IReadOnlyList<HighlightedLine> lines = Tokenize(key.Source, language, tabWidth);
			Cache[key] = lines;
			InsertionOrder.Enqueue(key);

			while (InsertionOrder.Count > MaxCacheEntries)
			{
				Cache.Remove(InsertionOrder.Dequeue());
			}

			return lines;
		}
	}

	/// <summary>Tokenizes source text without consulting or populating the cache.</summary>
	/// <param name="source">The source text.</param>
	/// <param name="language">The resolved language definition.</param>
	/// <param name="tabWidth">The tab stop width used when expanding tabs.</param>
	/// <returns>The highlighted lines.</returns>
	public static IReadOnlyList<HighlightedLine> Tokenize(string source, LanguageDefinition language, int tabWidth)
	{
		Ensure.NotNull(language);
		string text = source ?? string.Empty;
		if (text.Length == 0)
		{
			return [];
		}

		IReadOnlyList<HighlightedToken> tokens = language.IsMarkup
			? MarkupTokenizer.Tokenize(text)
			: CodeTokenizer.Tokenize(text, language);

		return LineSplitter.Split(tokens, tabWidth);
	}

	// The definition is compared by reference, not by name or by value: re-registering a language
	// hands out a new instance, which must miss rather than serve lines tokenized by the old rules,
	// and a record's structural equality would walk every keyword collection on every lookup.
	private readonly record struct CacheKey(string Source, LanguageDefinition Language, int TabWidth)
	{
		public bool Equals(CacheKey other) =>
			TabWidth == other.TabWidth
			&& ReferenceEquals(Language, other.Language)
			&& string.Equals(Source, other.Source, StringComparison.Ordinal);

		public override int GetHashCode() =>
			HashCode.Combine(Source, RuntimeHelpers.GetHashCode(Language), TabWidth);
	}
}
