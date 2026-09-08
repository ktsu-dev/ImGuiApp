// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SyntaxHighlighting;

using System;

/// <summary>
/// Recognizes the languages that are commonly written inside another language's comments and
/// strings. Each test is deliberately strict: a body that is only probably JSON is left alone,
/// because coloring prose as code is worse than leaving code uncolored.
/// </summary>
public static class EmbeddedContent
{
	// A nesting limit keeps a pathological body ("[[[[[[…") from recursing through the stack. Real
	// documents nest a handful of levels; anything past this is not what the sniffer is here for.
	private const int MaxJsonDepth = 64;

	private static readonly string[] JsonLiterals = ["true", "false", "null"];

	private static readonly string[] SqlOpeningKeywords =
	[
		"select", "insert", "update", "delete", "create", "alter", "drop", "with", "merge", "truncate",
	];

	private static readonly string[] SqlFollowingKeywords =
	[
		"from", "into", "values", "set", "where", "table", "join", "index", "view", "column", "returning",
	];

	/// <summary>
	/// Whether the content is a complete JSON object or array. It is parsed rather than pattern
	/// matched, so a fragment or a brace-heavy sentence is rejected.
	/// </summary>
	/// <param name="content">The candidate text.</param>
	/// <returns><see langword="true"/> when the whole content parses as one JSON object or array.</returns>
	public static bool LooksLikeJson(string content)
	{
		if (content is null)
		{
			return false;
		}

		ReadOnlySpan<char> text = content.AsSpan().Trim();
		if (text.Length < 2 || (text[0] != '{' && text[0] != '['))
		{
			return false;
		}

		int index = 0;
		if (!TryReadValue(text, ref index, 0))
		{
			return false;
		}

		SkipWhitespace(text, ref index);
		return index == text.Length;
	}

	/// <summary>
	/// Whether the content is angle-bracket markup: it opens with a tag, a comment, a declaration or
	/// a processing instruction, and closes with <c>&gt;</c>.
	/// </summary>
	/// <param name="content">The candidate text.</param>
	/// <returns><see langword="true"/> when the content should be highlighted as markup.</returns>
	public static bool LooksLikeMarkup(string content)
	{
		if (content is null)
		{
			return false;
		}

		ReadOnlySpan<char> text = content.AsSpan().Trim();
		if (text.Length < 3 || text[0] != '<' || text[^1] != '>')
		{
			return false;
		}

		char second = text[1];
		return char.IsLetter(second) || second is '_' or '/' or '!' or '?';
	}

	/// <summary>
	/// Whether the content is an SQL statement: it opens with a statement keyword and goes on to use
	/// a second one. Both halves are required, so a sentence that happens to start with "Update" is
	/// not mistaken for a query.
	/// </summary>
	/// <param name="content">The candidate text.</param>
	/// <returns><see langword="true"/> when the content should be highlighted as SQL.</returns>
	public static bool LooksLikeSql(string content)
	{
		if (content is null)
		{
			return false;
		}

		ReadOnlySpan<char> text = content.AsSpan().Trim();
		if (text.Length < 10)
		{
			return false;
		}

		int index = 0;
		if (!TryReadWord(text, ref index, out ReadOnlySpan<char> first) || !Contains(SqlOpeningKeywords, first))
		{
			return false;
		}

		while (TryReadWord(text, ref index, out ReadOnlySpan<char> word))
		{
			if (Contains(SqlFollowingKeywords, word))
			{
				return true;
			}
		}

		return false;
	}

	private static bool Contains(string[] keywords, ReadOnlySpan<char> word)
	{
		foreach (string keyword in keywords)
		{
			if (word.Equals(keyword, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	// Words are runs of letters; everything else is a separator, so "SELECT * FROM t" yields
	// "SELECT", "FROM" and "t".
	private static bool TryReadWord(ReadOnlySpan<char> text, ref int index, out ReadOnlySpan<char> word)
	{
		while (index < text.Length && !char.IsLetter(text[index]))
		{
			index++;
		}

		int start = index;
		while (index < text.Length && char.IsLetter(text[index]))
		{
			index++;
		}

		word = text[start..index];
		return word.Length > 0;
	}

	private static bool TryReadValue(ReadOnlySpan<char> text, ref int index, int depth)
	{
		if (depth > MaxJsonDepth)
		{
			return false;
		}

		SkipWhitespace(text, ref index);
		if (index >= text.Length)
		{
			return false;
		}

		return text[index] switch
		{
			'{' => TryReadObject(text, ref index, depth),
			'[' => TryReadArray(text, ref index, depth),
			'"' => TryReadString(text, ref index),
			_ => TryReadAtom(text, ref index),
		};
	}

	private static bool TryReadObject(ReadOnlySpan<char> text, ref int index, int depth)
	{
		index++;
		SkipWhitespace(text, ref index);
		if (index < text.Length && text[index] == '}')
		{
			index++;
			return true;
		}

		while (true)
		{
			SkipWhitespace(text, ref index);
			if (index >= text.Length || text[index] != '"' || !TryReadString(text, ref index))
			{
				return false;
			}

			SkipWhitespace(text, ref index);
			if (index >= text.Length || text[index] != ':')
			{
				return false;
			}

			index++;
			if (!TryReadValue(text, ref index, depth + 1))
			{
				return false;
			}

			SkipWhitespace(text, ref index);
			if (index >= text.Length)
			{
				return false;
			}

			if (text[index] == ',')
			{
				index++;
				continue;
			}

			if (text[index] == '}')
			{
				index++;
				return true;
			}

			return false;
		}
	}

	private static bool TryReadArray(ReadOnlySpan<char> text, ref int index, int depth)
	{
		index++;
		SkipWhitespace(text, ref index);
		if (index < text.Length && text[index] == ']')
		{
			index++;
			return true;
		}

		while (true)
		{
			if (!TryReadValue(text, ref index, depth + 1))
			{
				return false;
			}

			SkipWhitespace(text, ref index);
			if (index >= text.Length)
			{
				return false;
			}

			if (text[index] == ',')
			{
				index++;
				continue;
			}

			if (text[index] == ']')
			{
				index++;
				return true;
			}

			return false;
		}
	}

	private static bool TryReadString(ReadOnlySpan<char> text, ref int index)
	{
		index++;
		while (index < text.Length)
		{
			char current = text[index];
			if (current == '\\')
			{
				index += 2;
				continue;
			}

			index++;
			if (current == '"')
			{
				return true;
			}
		}

		return false;
	}

	// true, false, null and numbers. Numbers are checked loosely — a run of numeric characters that
	// holds at least one digit — because a malformed number is not what tells prose from JSON.
	private static bool TryReadAtom(ReadOnlySpan<char> text, ref int index)
	{
		foreach (string literal in JsonLiterals)
		{
			if (text[index..].StartsWith(literal, StringComparison.Ordinal))
			{
				index += literal.Length;
				return true;
			}
		}

		int start = index;
		bool digit = false;
		while (index < text.Length && (char.IsAsciiDigit(text[index]) || text[index] is '-' or '+' or '.' or 'e' or 'E'))
		{
			digit |= char.IsAsciiDigit(text[index]);
			index++;
		}

		return digit && index > start;
	}

	private static void SkipWhitespace(ReadOnlySpan<char> text, ref int index)
	{
		while (index < text.Length && char.IsWhiteSpace(text[index]))
		{
			index++;
		}
	}
}
