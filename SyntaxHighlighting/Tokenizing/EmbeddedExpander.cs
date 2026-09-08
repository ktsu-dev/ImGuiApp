// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SyntaxHighlighting;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Replaces a comment or string token with the token run of the language written inside it. The
/// delimiters keep the host's color, and so does any embedded text the inner language does not
/// classify, so prose in a doc comment still reads as a comment.
/// </summary>
internal static class EmbeddedExpander
{
	private static readonly string[] HintKeys = ["language", "lang"];

	/// <summary>
	/// Appends a host token, expanded into embedded tokens when a rule claims its body.
	/// </summary>
	/// <param name="tokens">The token list being built.</param>
	/// <param name="text">The whole host token text, delimiters included.</param>
	/// <param name="kind">The host token's kind, after reclassification.</param>
	/// <param name="body">Where the body sits inside <paramref name="text"/>, and how it is escaped.</param>
	/// <param name="language">The host language definition.</param>
	/// <param name="forcedLanguage">
	/// A language named by a preceding hint comment, which overrides the rules; <see langword="null"/>
	/// when there is none.
	/// </param>
	public static void Append(
		List<HighlightedToken> tokens,
		string text,
		TokenKind kind,
		in TokenBody body,
		LanguageDefinition language,
		string? forcedLanguage)
	{
		EmbeddedHosts host = HostFor(kind);
		int bodyStart = body.OpenLength;
		int bodyEnd = text.Length - body.CloseLength;

		if (host == EmbeddedHosts.None || bodyEnd <= bodyStart || (language.EmbeddedLanguages.Count == 0 && forcedLanguage is null))
		{
			tokens.Add(new HighlightedToken(kind, text));
			return;
		}

		UnescapedBody unescaped = UnescapedBody.Of(text[bodyStart..bodyEnd], body.String);
		if (!TryResolve(language, host, unescaped.Content, forcedLanguage, out LanguageDefinition embedded))
		{
			tokens.Add(new HighlightedToken(kind, text));
			return;
		}

		IReadOnlyList<HighlightedToken> inner = embedded.IsMarkup
			? MarkupTokenizer.Tokenize(unescaped.Content)
			: CodeTokenizer.Tokenize(unescaped.Content, embedded, expandEmbedded: false);

		int before = tokens.Count;
		AppendMerged(tokens, before, kind, text[..bodyStart]);

		int position = 0;
		foreach (HighlightedToken token in inner)
		{
			int next = position + token.Text.Length;
			string slice = text[(bodyStart + unescaped.Origin(position))..(bodyStart + unescaped.Origin(next))];
			AppendMerged(tokens, before, token.Kind == TokenKind.Plain ? kind : token.Kind, slice);
			position = next;
		}

		AppendMerged(tokens, before, kind, text[bodyEnd..]);

		// Nothing above can drop or reorder characters, but a silent mismatch would corrupt the
		// rendered text rather than merely miscolor it, so the reconstruction is checked.
		if (!Reconstructs(tokens, before, text))
		{
			tokens.RemoveRange(before, tokens.Count - before);
			tokens.Add(new HighlightedToken(kind, text));
		}
	}

	/// <summary>
	/// Reads a language hint from a comment body, as in <c>// lang=json</c> or
	/// <c>/* language=sql */</c>. The hint names the language of the next string literal, which is
	/// how a snippet too short or too escaped to recognize is highlighted anyway.
	/// </summary>
	/// <param name="body">The comment body, delimiters removed.</param>
	/// <returns>The language named by the hint, or <see langword="null"/> when the comment has none.</returns>
	public static string? ReadLanguageHint(string body)
	{
		ReadOnlySpan<char> text = body.AsSpan().Trim();
		foreach (string key in HintKeys)
		{
			if (!text.StartsWith(key, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			ReadOnlySpan<char> rest = text[key.Length..].TrimStart();
			if (rest.Length < 2 || rest[0] != '=')
			{
				continue;
			}

			rest = rest[1..].TrimStart();
			int end = 0;
			while (end < rest.Length && !char.IsWhiteSpace(rest[end]))
			{
				end++;
			}

			// A block comment's trailing delimiter is not part of the body, but a hint written as
			// "lang=json */" would still be trimmed here by the whitespace scan above.
			return end > 0 ? rest[..end].ToString() : null;
		}

		return null;
	}

	private static bool TryResolve(
		LanguageDefinition language,
		EmbeddedHosts host,
		string body,
		string? forcedLanguage,
		out LanguageDefinition embedded)
	{
		if (forcedLanguage is not null)
		{
			return LanguageRegistry.TryGet(forcedLanguage, out embedded);
		}

		// The first rule that both claims the body and names a language the registry knows. A rule
		// that claims the body but names an unregistered language does not stop the search: the next
		// rule still gets its turn, the way it would if the first had not claimed the body at all.
		LanguageDefinition? match = language.EmbeddedLanguages
			.Where(rule => rule.AppliesTo(host, body))
			.Select(rule => LanguageRegistry.TryGet(rule.Language, out LanguageDefinition definition) ? definition : null)
			.FirstOrDefault(definition => definition is not null);

		embedded = match ?? BuiltInLanguages.PlainText;
		return match is not null;
	}

	private static EmbeddedHosts HostFor(TokenKind kind) => kind switch
	{
		TokenKind.StringLiteral => EmbeddedHosts.StringLiteral,
		TokenKind.Comment => EmbeddedHosts.Comment,
		TokenKind.DocComment => EmbeddedHosts.DocComment,
		_ => EmbeddedHosts.None,
	};

	// Runs of one kind are joined as they are appended: an inner tokenizer splits text far more
	// finely than the renderer needs, and the seams between host and embedded text are invisible.
	private static void AppendMerged(List<HighlightedToken> tokens, int floor, TokenKind kind, string text)
	{
		if (text.Length == 0)
		{
			return;
		}

		if (tokens.Count > floor && tokens[^1].Kind == kind)
		{
			tokens[^1] = new HighlightedToken(kind, tokens[^1].Text + text);
			return;
		}

		tokens.Add(new HighlightedToken(kind, text));
	}

	private static bool Reconstructs(List<HighlightedToken> tokens, int from, string text)
	{
		int length = 0;
		for (int index = from; index < tokens.Count; index++)
		{
			length += tokens[index].Text.Length;
		}

		if (length != text.Length)
		{
			return false;
		}

		int offset = 0;
		for (int index = from; index < tokens.Count; index++)
		{
			string part = tokens[index].Text;
			if (string.CompareOrdinal(text, offset, part, 0, part.Length) != 0)
			{
				return false;
			}

			offset += part.Length;
		}

		return true;
	}

	/// <summary>
	/// A body with its escape sequences resolved, plus the map back to the original text. The inner
	/// tokenizer sees <c>{"a": 1}</c> where the source holds <c>{\"a\": 1}</c>, but every token it
	/// produces is re-sliced from the original, so the rendered text is never rewritten.
	/// </summary>
	private sealed class UnescapedBody
	{
		private readonly int[]? origins;

		public string Content { get; }

		private UnescapedBody(string content, int[]? map)
		{
			Content = content;
			origins = map;
		}

		public static UnescapedBody Of(string body, StringRule? rule)
		{
			// NeedsUnescaping already answers false for a rule with neither escape mechanism, so
			// past here the rule is non-null and one of the two branches below can fire.
			if (rule is null || !NeedsUnescaping(body, rule))
			{
				return new UnescapedBody(body, null);
			}

			char? escape = rule.Escape;
			bool doubledCloseEscapes = rule.DoubledCloseEscapes;
			string close = rule.Close;

			StringBuilder content = new(body.Length);
			List<int> map = new(body.Length + 1);
			int index = 0;
			while (index < body.Length)
			{
				char current = body[index];

				if (escape is char escapeCharacter && current == escapeCharacter && index + 1 < body.Length)
				{
					char next = body[index + 1];
					map.Add(index);
					content.Append(next switch
					{
						'n' => '\n',
						'r' => '\r',
						't' => '\t',
						_ => next,
					});
					index += 2;
					continue;
				}

				if (doubledCloseEscapes && Doubled(body, index, close))
				{
					for (int offset = 0; offset < close.Length; offset++)
					{
						map.Add(index + offset);
						content.Append(close[offset]);
					}

					index += close.Length * 2;
					continue;
				}

				map.Add(index);
				content.Append(current);
				index++;
			}

			map.Add(body.Length);
			return new UnescapedBody(content.ToString(), [.. map]);
		}

		/// <summary>Maps an index in <see cref="Content"/> back to one in the original body.</summary>
		/// <param name="index">An index into the unescaped content, its length included.</param>
		/// <returns>The matching index in the original body.</returns>
		public int Origin(int index) => origins is null ? index : origins[index];

		private static bool NeedsUnescaping(string body, StringRule rule) =>
			(rule.Escape.HasValue && body.Contains(rule.Escape.Value))
			|| (rule.DoubledCloseEscapes && body.Contains(rule.Close, StringComparison.Ordinal));

		private static bool Doubled(string body, int index, string close) =>
			close.Length > 0
			&& index + (close.Length * 2) <= body.Length
			&& string.CompareOrdinal(body, index, close, 0, close.Length) == 0
			&& string.CompareOrdinal(body, index + close.Length, close, 0, close.Length) == 0;
	}
}
