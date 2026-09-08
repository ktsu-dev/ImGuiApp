// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.SyntaxHighlighting;

using System;
using System.Collections.Generic;

/// <summary>
/// A lexer for angle-bracket markup (XML, HTML). Markup has no keywords: meaning comes from
/// position, so element names, attribute names and attribute values are classified structurally.
/// Embedded script and style bodies are treated as text.
/// </summary>
internal static class MarkupTokenizer
{
	/// <summary>Tokenizes markup source.</summary>
	/// <param name="source">The source text.</param>
	/// <returns>The tokens, in source order; concatenating their text reproduces the source.</returns>
	public static IReadOnlyList<HighlightedToken> Tokenize(string source)
	{
		Ensure.NotNull(source);

		List<HighlightedToken> tokens = [];
		int index = 0;

		while (index < source.Length)
		{
			if (source[index] == '<')
			{
				index = ReadMarkup(source, index, tokens);
				continue;
			}

			index = ReadText(source, index, tokens);
		}

		return tokens;
	}

	private static int ReadMarkup(string source, int index, List<HighlightedToken> tokens)
	{
		if (Matches(source, index, "<!--"))
		{
			return ReadDelimited(source, index, "-->", TokenKind.Comment, tokens);
		}

		if (Matches(source, index, "<![CDATA["))
		{
			return ReadDelimited(source, index, "]]>", TokenKind.StringLiteral, tokens);
		}

		if (Matches(source, index, "<?"))
		{
			return ReadDelimited(source, index, "?>", TokenKind.Preprocessor, tokens);
		}

		if (Matches(source, index, "<!"))
		{
			return ReadDelimited(source, index, ">", TokenKind.Preprocessor, tokens);
		}

		int scan = index + 1;
		bool closing = scan < source.Length && source[scan] == '/';
		if (closing)
		{
			scan++;
		}

		if (scan >= source.Length || !IsNameStart(source[scan]))
		{
			// A bare '<' that opens nothing (a less-than in text) is just text.
			tokens.Add(new HighlightedToken(TokenKind.Plain, source[index..scan]));
			return scan;
		}

		tokens.Add(new HighlightedToken(TokenKind.Punctuation, source[index..scan]));

		int nameEnd = ReadName(source, scan);
		tokens.Add(new HighlightedToken(TokenKind.Tag, source[scan..nameEnd]));
		return ReadAttributes(source, nameEnd, tokens);
	}

	private static int ReadAttributes(string source, int index, List<HighlightedToken> tokens)
	{
		int scan = index;
		while (scan < source.Length)
		{
			char current = source[scan];

			if (current == '>')
			{
				tokens.Add(new HighlightedToken(TokenKind.Punctuation, ">"));
				return scan + 1;
			}

			if (Matches(source, scan, "/>"))
			{
				tokens.Add(new HighlightedToken(TokenKind.Punctuation, "/>"));
				return scan + 2;
			}

			if (current is '"' or '\'')
			{
				int end = scan + 1;
				while (end < source.Length && source[end] != current)
				{
					end++;
				}

				end = Math.Min(end + 1, source.Length);
				tokens.Add(new HighlightedToken(TokenKind.StringLiteral, source[scan..end]));
				scan = end;
				continue;
			}

			if (current == '=')
			{
				tokens.Add(new HighlightedToken(TokenKind.Operator, "="));
				scan++;
				continue;
			}

			if (IsNameStart(current))
			{
				int end = ReadName(source, scan);
				tokens.Add(new HighlightedToken(TokenKind.AttributeName, source[scan..end]));
				scan = end;
				continue;
			}

			int plainEnd = scan + 1;
			while (plainEnd < source.Length && char.IsWhiteSpace(source[plainEnd]))
			{
				plainEnd++;
			}

			tokens.Add(new HighlightedToken(TokenKind.Plain, source[scan..plainEnd]));
			scan = plainEnd;
		}

		return scan;
	}

	private static int ReadText(string source, int index, List<HighlightedToken> tokens)
	{
		if (source[index] == '&')
		{
			int entityEnd = index + 1;
			while (entityEnd < source.Length && entityEnd - index <= 10 && source[entityEnd] != ';' && !char.IsWhiteSpace(source[entityEnd]))
			{
				entityEnd++;
			}

			if (entityEnd < source.Length && source[entityEnd] == ';')
			{
				tokens.Add(new HighlightedToken(TokenKind.Constant, source[index..(entityEnd + 1)]));
				return entityEnd + 1;
			}
		}

		int scan = index + 1;
		while (scan < source.Length && source[scan] != '<' && source[scan] != '&')
		{
			scan++;
		}

		tokens.Add(new HighlightedToken(TokenKind.Plain, source[index..scan]));
		return scan;
	}

	private static int ReadDelimited(string source, int index, string close, TokenKind kind, List<HighlightedToken> tokens)
	{
		int scan = index;
		while (scan < source.Length && !Matches(source, scan, close))
		{
			scan++;
		}

		int end = Math.Min(scan + close.Length, source.Length);
		tokens.Add(new HighlightedToken(kind, source[index..end]));
		return end;
	}

	private static int ReadName(string source, int index)
	{
		int scan = index;
		while (scan < source.Length && IsNameChar(source[scan]))
		{
			scan++;
		}

		return scan;
	}

	private static bool IsNameStart(char value) => char.IsLetter(value) || value == '_' || value == ':';

	private static bool IsNameChar(char value) =>
		char.IsLetterOrDigit(value) || value == '_' || value == '-' || value == '.' || value == ':';

	private static bool Matches(string source, int index, string value) =>
		index + value.Length <= source.Length
		&& string.CompareOrdinal(source, index, value, 0, value.Length) == 0;
}
