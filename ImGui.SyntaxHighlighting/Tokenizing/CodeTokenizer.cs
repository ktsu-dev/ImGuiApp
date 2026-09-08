// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.SyntaxHighlighting;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

/// <summary>
/// A single-pass lexer for the curly-brace and scripting languages described by a
/// <see cref="LanguageDefinition"/>. Tokens are emitted over the whole source and may span lines;
/// <see cref="LineSplitter"/> breaks them into per-line runs afterwards.
/// </summary>
internal static class CodeTokenizer
{
	// Keyword lookups are derived from a definition's collections and cached against the definition
	// instance, so re-tokenizing (a cache miss, or a document rendered per frame) does not rebuild them.
	private static readonly ConditionalWeakTable<LanguageDefinition, KeywordLookup> Lookups = [];

	/// <summary>Tokenizes source text against a language definition.</summary>
	/// <param name="source">The source text.</param>
	/// <param name="language">The language definition.</param>
	/// <returns>The tokens, in source order; concatenating their text reproduces the source.</returns>
	public static IReadOnlyList<HighlightedToken> Tokenize(string source, LanguageDefinition language)
	{
		Ensure.NotNull(source);
		Ensure.NotNull(language);

		KeywordLookup lookup = Lookups.GetValue(language, KeywordLookup.Create);
		List<HighlightedToken> tokens = [];
		int index = 0;
		int plainStart = 0;

		void FlushPlain(int end)
		{
			if (end > plainStart)
			{
				tokens.Add(new HighlightedToken(TokenKind.Plain, source[plainStart..end]));
			}
		}

		while (index < source.Length)
		{
			int start = index;

			if (TryReadBlockComment(source, lookup, ref index, out TokenKind kind)
				|| TryReadLineComment(source, lookup, ref index, out kind)
				|| TryReadString(source, lookup, ref index, out kind)
				|| TryReadDirective(source, language, ref index, out kind)
				|| TryReadNumber(source, ref index, out kind)
				|| TryReadIdentifier(source, language, lookup, ref index, out kind)
				|| TryReadSymbol(source, language, ref index, out kind))
			{
				FlushPlain(start);
				string text = source[start..index];
				tokens.Add(new HighlightedToken(Reclassify(kind, text, source, index, language), text));
				plainStart = index;
				continue;
			}

			index++;
		}

		FlushPlain(source.Length);
		return tokens;
	}

	// A string or identifier that is immediately followed by ':' is a key in the object-shaped
	// languages (JSON, YAML, CSS), and one followed by '(' is a call. Both are decided from what
	// comes after the token, so they are applied once the token's extent is known.
	private static TokenKind Reclassify(TokenKind kind, string text, string source, int end, LanguageDefinition language)
	{
		bool nameLike = kind is TokenKind.Plain or TokenKind.StringLiteral;
		if (!nameLike || text.Length == 0)
		{
			return kind;
		}

		char next = PeekNonSpace(source, end);
		if (language.HighlightPropertyNames && next == ':')
		{
			return TokenKind.Property;
		}

		return language.HighlightFunctionCalls && next == '(' && kind == TokenKind.Plain
			? TokenKind.Function
			: kind;
	}

	private static char PeekNonSpace(string source, int index)
	{
		int scan = index;
		while (scan < source.Length && (source[scan] == ' ' || source[scan] == '\t'))
		{
			scan++;
		}

		return scan < source.Length ? source[scan] : '\0';
	}

	private static bool TryReadBlockComment(string source, KeywordLookup lookup, ref int index, out TokenKind kind)
	{
		kind = TokenKind.Comment;
		foreach (BlockCommentRule rule in lookup.BlockComments)
		{
			if (!Matches(source, index, rule.Open))
			{
				continue;
			}

			int scan = index + rule.Open.Length;
			int depth = 1;
			while (scan < source.Length && depth > 0)
			{
				if (rule.Nested && Matches(source, scan, rule.Open))
				{
					depth++;
					scan += rule.Open.Length;
				}
				else if (Matches(source, scan, rule.Close))
				{
					depth--;
					scan += rule.Close.Length;
				}
				else
				{
					scan++;
				}
			}

			index = scan;
			kind = rule.Kind;
			return true;
		}

		return false;
	}

	private static bool TryReadLineComment(string source, KeywordLookup lookup, ref int index, out TokenKind kind)
	{
		kind = TokenKind.Comment;
		foreach (LineCommentRule rule in lookup.LineComments)
		{
			if (!Matches(source, index, rule.Prefix))
			{
				continue;
			}

			int scan = index + rule.Prefix.Length;
			while (scan < source.Length && source[scan] != '\n' && source[scan] != '\r')
			{
				scan++;
			}

			index = scan;
			kind = rule.Kind;
			return true;
		}

		return false;
	}

	private static bool TryReadString(string source, KeywordLookup lookup, ref int index, out TokenKind kind)
	{
		kind = TokenKind.StringLiteral;
		foreach (StringRule rule in lookup.Strings)
		{
			if (!Matches(source, index, rule.Open))
			{
				continue;
			}

			index = ScanStringBody(source, index + rule.Open.Length, rule);
			kind = rule.Kind;
			return true;
		}

		return false;
	}

	private static int ScanStringBody(string source, int start, StringRule rule)
	{
		int scan = start;
		while (scan < source.Length)
		{
			char current = source[scan];

			if (!rule.AllowMultiline && current == '\n')
			{
				// An unterminated single-line literal ends at the newline rather than swallowing the
				// rest of the file, so one stray quote cannot recolor the whole document.
				return scan;
			}

			if (rule.Escape.HasValue && current == rule.Escape.Value && scan + 1 < source.Length)
			{
				scan += 2;
				continue;
			}

			if (Matches(source, scan, rule.Close))
			{
				int afterClose = scan + rule.Close.Length;
				if (rule.DoubledCloseEscapes && Matches(source, afterClose, rule.Close))
				{
					scan = afterClose + rule.Close.Length;
					continue;
				}

				return afterClose;
			}

			scan++;
		}

		return source.Length;
	}

	private static bool TryReadDirective(string source, LanguageDefinition language, ref int index, out TokenKind kind)
	{
		kind = TokenKind.Preprocessor;
		if (!language.DirectivePrefix.HasValue || source[index] != language.DirectivePrefix.Value || !AtLineStart(source, index))
		{
			return false;
		}

		int scan = index + 1;
		while (scan < source.Length && source[scan] != '\n' && source[scan] != '\r')
		{
			scan++;
		}

		index = scan;
		return true;
	}

	private static bool AtLineStart(string source, int index)
	{
		int scan = index - 1;
		while (scan >= 0 && (source[scan] == ' ' || source[scan] == '\t'))
		{
			scan--;
		}

		return scan < 0 || source[scan] == '\n';
	}

	private static bool TryReadNumber(string source, ref int index, out TokenKind kind)
	{
		kind = TokenKind.Number;
		char current = source[index];
		bool leadingDot = current == '.' && index + 1 < source.Length && char.IsAsciiDigit(source[index + 1]);
		if (!char.IsAsciiDigit(current) && !leadingDot)
		{
			return false;
		}

		// In hex the letters 'e' and 'E' are digits, so '+'/'-' after one is an operator, not an exponent sign.
		bool hex = current == '0' && index + 1 < source.Length && (source[index + 1] == 'x' || source[index + 1] == 'X');
		int scan = leadingDot ? index + 2 : index + 1;
		while (scan < source.Length)
		{
			char c = source[scan];
			bool exponentSign = !hex
				&& (c == '+' || c == '-')
				&& (source[scan - 1] == 'e' || source[scan - 1] == 'E');
			bool fraction = c == '.' && scan + 1 < source.Length && char.IsAsciiDigit(source[scan + 1]);

			if (char.IsAsciiLetterOrDigit(c) || c == '_' || fraction || exponentSign)
			{
				scan++;
				continue;
			}

			break;
		}

		index = scan;
		return true;
	}

	private static bool TryReadIdentifier(string source, LanguageDefinition language, KeywordLookup lookup, ref int index, out TokenKind kind)
	{
		kind = TokenKind.Plain;
		char current = source[index];
		if (!char.IsLetter(current) && !language.IdentifierStartCharacters.Contains(current, StringComparison.Ordinal))
		{
			return false;
		}

		int scan = index + 1;
		while (scan < source.Length)
		{
			char c = source[scan];
			if (char.IsLetterOrDigit(c) || language.IdentifierCharacters.Contains(c, StringComparison.Ordinal))
			{
				scan++;
				continue;
			}

			break;
		}

		string word = source[index..scan];
		index = scan;
		kind = lookup.Classify(word);
		return true;
	}

	private static bool TryReadSymbol(string source, LanguageDefinition language, ref int index, out TokenKind kind)
	{
		char current = source[index];

		if (language.OperatorCharacters.Contains(current, StringComparison.Ordinal))
		{
			kind = TokenKind.Operator;
			int scan = index;
			while (scan < source.Length && language.OperatorCharacters.Contains(source[scan], StringComparison.Ordinal))
			{
				scan++;
			}

			index = scan;
			return true;
		}

		if (language.PunctuationCharacters.Contains(current, StringComparison.Ordinal))
		{
			kind = TokenKind.Punctuation;
			index++;
			return true;
		}

		kind = TokenKind.Plain;
		return false;
	}

	private static bool Matches(string source, int index, string value) =>
		value.Length > 0
		&& index + value.Length <= source.Length
		&& string.CompareOrdinal(source, index, value, 0, value.Length) == 0;

	/// <summary>
	/// The keyword sets and delimiter rules of one language, prepared for lookup: rules are ordered
	/// longest-opener-first so <c>"""</c> wins over <c>"</c> and <c>///</c> over <c>//</c>.
	/// </summary>
	private sealed class KeywordLookup
	{
		private readonly HashSet<string> keywords;
		private readonly HashSet<string> controlKeywords;
		private readonly HashSet<string> types;
		private readonly HashSet<string> constants;

		public IReadOnlyList<LineCommentRule> LineComments { get; }

		public IReadOnlyList<BlockCommentRule> BlockComments { get; }

		public IReadOnlyList<StringRule> Strings { get; }

		private KeywordLookup(LanguageDefinition language)
		{
			StringComparer comparer = language.CaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
			keywords = new HashSet<string>(language.Keywords, comparer);
			controlKeywords = new HashSet<string>(language.ControlKeywords, comparer);
			types = new HashSet<string>(language.Types, comparer);
			constants = new HashSet<string>(language.Constants, comparer);

			LineComments = [.. language.LineComments.OrderByDescending(rule => rule.Prefix.Length)];
			BlockComments = [.. language.BlockComments.OrderByDescending(rule => rule.Open.Length)];
			Strings = [.. language.Strings.OrderByDescending(rule => rule.Open.Length)];
		}

		public static KeywordLookup Create(LanguageDefinition language) => new(language);

		public TokenKind Classify(string word)
		{
			if (constants.Contains(word))
			{
				return TokenKind.Constant;
			}

			if (types.Contains(word))
			{
				return TokenKind.Type;
			}

			if (controlKeywords.Contains(word))
			{
				return TokenKind.ControlKeyword;
			}

			return keywords.Contains(word) ? TokenKind.Keyword : TokenKind.Plain;
		}
	}
}
