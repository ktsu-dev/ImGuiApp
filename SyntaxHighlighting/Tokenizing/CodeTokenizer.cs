// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SyntaxHighlighting;

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

	// The keywords after which a JavaScript '/' opens a regular expression rather than dividing:
	// each is followed by an expression, never by a value a '/' could divide.
	private static readonly HashSet<string> RegexPrecedingKeywords =
	[
		"await", "case", "delete", "do", "else", "in", "instanceof", "new", "of", "return", "throw",
		"typeof", "void", "yield",
	];

	/// <summary>Tokenizes source text against a language definition.</summary>
	/// <param name="source">The source text.</param>
	/// <param name="language">The language definition.</param>
	/// <param name="expandEmbedded">
	/// Whether comments and strings are searched for an embedded language. Expansion tokenizes the
	/// snippet with the embedded language and this flag off, which is what keeps embedding one level
	/// deep however the definitions refer to each other.
	/// </param>
	/// <returns>The tokens, in source order; concatenating their text reproduces the source.</returns>
	public static IReadOnlyList<HighlightedToken> Tokenize(string source, LanguageDefinition language, bool expandEmbedded = true)
	{
		Ensure.NotNull(source);
		Ensure.NotNull(language);

		KeywordLookup lookup = Lookups.GetValue(language, KeywordLookup.Create);
		bool embeds = expandEmbedded && language.EmbeddedLanguages.Count > 0;
		List<HighlightedToken> tokens = [];
		string? pendingHint = null;
		int index = 0;
		int plainStart = 0;

		// The last token that was not a comment, which decides whether a '/' opens a regular
		// expression or divides.
		TokenKind? previousKind = null;
		string previousText = string.Empty;

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

			if (language.RegexLiterals && TryReadRegex(source, previousKind, previousText, ref index))
			{
				FlushPlain(start);
				tokens.Add(new HighlightedToken(TokenKind.StringLiteral, source[start..index]));
				previousKind = TokenKind.StringLiteral;
				previousText = source[start..index];
				plainStart = index;
				continue;
			}

			if (TryReadBlockComment(source, lookup, ref index, out TokenKind kind, out TokenBody body)
				|| TryReadLineComment(source, lookup, ref index, out kind, out body)
				|| TryReadString(source, lookup, ref index, out kind, out body)
				|| TryReadDirective(source, language, ref index, out kind, out body)
				|| TryReadNumber(source, ref index, out kind, out body)
				|| TryReadIdentifier(source, language, lookup, ref index, out kind, out body)
				|| TryReadSymbol(source, language, ref index, out kind, out body))
			{
				FlushPlain(start);
				string text = source[start..index];
				TokenKind classified = Reclassify(kind, text, source, index, language);

				if (embeds)
				{
					pendingHint = Embed(tokens, text, classified, body, language, pendingHint);
				}
				else
				{
					tokens.Add(new HighlightedToken(classified, text));
				}

				if (kind is not TokenKind.Comment and not TokenKind.DocComment)
				{
					previousKind = classified;
					previousText = text;
				}

				plainStart = index;
				continue;
			}

			index++;
		}

		FlushPlain(source.Length);
		return tokens;
	}

	/// <summary>
	/// Appends one token, expanded into an embedded language when one is found inside it, and
	/// carries the language hint comments leave behind.
	/// </summary>
	/// <returns>The hint still waiting for a string literal to attach to.</returns>
	private static string? Embed(
		List<HighlightedToken> tokens,
		string text,
		TokenKind kind,
		in TokenBody body,
		LanguageDefinition language,
		string? pendingHint)
	{
		// A hint names the language of the *next* string literal, so a comment carrying one sets the
		// hint and the first literal after it spends it. Nothing in between clears it: the hint is
		// written immediately above the literal it describes.
		bool isComment = kind is TokenKind.Comment or TokenKind.DocComment;
		bool isString = kind == TokenKind.StringLiteral;
		string? hint = isComment
			? EmbeddedExpander.ReadLanguageHint(text[body.OpenLength..(text.Length - body.CloseLength)]) ?? pendingHint
			: pendingHint;

		EmbeddedExpander.Append(tokens, text, kind, body, language, isString ? pendingHint : null);
		return isString ? null : hint;
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

	// A '/' opens a regular expression where an expression is expected, and divides after a value.
	// It cannot open one before '*' or '/', which start comments. The literal runs to the next '/'
	// outside a [...] class that no backslash escapes, then takes its flag letters; with no such '/'
	// on the line it is not a literal at all.
	private static bool TryReadRegex(string source, TokenKind? previousKind, string previousText, ref int index)
	{
		if (source[index] != '/'
			|| index + 1 >= source.Length
			|| source[index + 1] is '*' or '/'
			|| !ExpectsExpression(previousKind, previousText))
		{
			return false;
		}

		bool inClass = false;
		int scan = index + 1;
		while (scan < source.Length)
		{
			char c = source[scan];
			if (c is '\n' or '\r')
			{
				return false;
			}

			if (c == '\\')
			{
				if (scan + 1 >= source.Length || source[scan + 1] is '\n' or '\r')
				{
					return false;
				}

				scan += 2;
				continue;
			}

			if (c == '[')
			{
				inClass = true;
			}
			else if (c == ']')
			{
				inClass = false;
			}
			else if (c == '/' && !inClass)
			{
				break;
			}

			scan++;
		}

		if (scan >= source.Length)
		{
			return false;
		}

		scan++;
		while (scan < source.Length && char.IsAsciiLetter(source[scan]))
		{
			scan++;
		}

		index = scan;
		return true;
	}

	private static bool ExpectsExpression(TokenKind? previousKind, string previousText) => previousKind switch
	{
		null => true,
		TokenKind.Operator => true,
		TokenKind.Punctuation => previousText is not (")" or "]" or "}"),
		TokenKind.Keyword or TokenKind.ControlKeyword => RegexPrecedingKeywords.Contains(previousText),
		_ => false,
	};

	private static bool TryReadBlockComment(string source, KeywordLookup lookup, ref int index, out TokenKind kind, out TokenBody body)
	{
		kind = TokenKind.Comment;
		body = TokenBody.None;

		// In /**/ the second star belongs both to the doc opener /** and to the close */, so the doc
		// rule would scan past its own close and run on to the next */ in the file. A rule whose close
		// overlaps its opener at this position gives way to a shorter one (/* here, which sees the */);
		// only when nothing else matches does it apply after all, as /* does to /*/, which C opens.
		BlockCommentRule? overlapping = null;
		foreach (BlockCommentRule rule in lookup.BlockComments)
		{
			if (!Matches(source, index, rule.Open))
			{
				continue;
			}

			if (CloseOverlapsOpener(source, index, rule))
			{
				overlapping ??= rule;
				continue;
			}

			ReadBlockComment(source, rule, ref index, out kind, out body);
			return true;
		}

		if (overlapping is not null)
		{
			ReadBlockComment(source, overlapping, ref index, out kind, out body);
			return true;
		}

		return false;
	}

	private static bool CloseOverlapsOpener(string source, int index, BlockCommentRule rule)
	{
		int limit = Math.Min(rule.Open.Length - 1, rule.Close.Length);
		for (int shared = 1; shared <= limit; shared++)
		{
			if (Matches(source, index + rule.Open.Length - shared, rule.Close))
			{
				return true;
			}
		}

		return false;
	}

	private static void ReadBlockComment(string source, BlockCommentRule rule, ref int index, out TokenKind kind, out TokenBody body)
	{
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

		// An unterminated block comment ran to the end of the source, so it has no closing
		// delimiter to hold back from the body.
		bool closed = depth == 0;
		body = new TokenBody(rule.Open.Length, closed ? rule.Close.Length : 0, null);
	}

	private static bool TryReadLineComment(string source, KeywordLookup lookup, ref int index, out TokenKind kind, out TokenBody body)
	{
		kind = TokenKind.Comment;
		body = TokenBody.None;
		foreach (LineCommentRule rule in lookup.LineComments)
		{
			if (!Matches(source, index, rule.Prefix)
				|| (rule.RequiresWhitespaceBefore && index > 0 && !char.IsWhiteSpace(source[index - 1])))
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
			body = new TokenBody(rule.Prefix.Length, 0, null);
			return true;
		}

		return false;
	}

	private static bool TryReadString(string source, KeywordLookup lookup, ref int index, out TokenKind kind, out TokenBody body)
	{
		kind = TokenKind.StringLiteral;
		body = TokenBody.None;
		foreach (StringRule rule in lookup.StringsStartingWith(source[index]))
		{
			if (!Matches(source, index, rule.Open))
			{
				continue;
			}

			int openLength = rule.Open.Length;
			string close = rule.Close;
			if (rule.CloseRepeatsDelimiter && !TryExtendByRawDelimiter(source, index, rule, ref openLength, ref close))
			{
				continue;
			}

			if (rule.CloseMatchesOpeningRun)
			{
				// A raw string opened with more quotes than the rule names closes only on as many.
				char repeated = rule.Open[^1];
				int extra = 0;
				while (index + openLength + extra < source.Length && source[index + openLength + extra] == repeated)
				{
					extra++;
				}

				openLength += extra;
				close = string.Concat(rule.Close, new string(repeated, extra));
			}

			index = ScanLiteral(source, index + openLength, rule, close, out bool closed);

			kind = rule.Kind;
			body = new TokenBody(openLength, closed ? close.Length : 0, rule);
			return true;
		}

		return false;
	}

	// Scans a literal's body with its escape. A scan-only escape that runs the literal off its line or
	// the source most likely means the literal ends in that character (r'C:\dir\'), so the body is
	// read again without it, and that reading is kept when it closes.
	private static int ScanLiteral(string source, int start, StringRule rule, string close, out bool closed)
	{
		int end = ScanStringBody(source, start, rule, close, rule.Escape ?? rule.ScanEscape, out closed);
		if (closed || rule.Escape.HasValue || !rule.ScanEscape.HasValue)
		{
			return end;
		}

		int plainEnd = ScanStringBody(source, start, rule, close, null, out bool plainClosed);
		if (!plainClosed)
		{
			return end;
		}

		closed = true;
		return plainEnd;
	}

	private static int ScanStringBody(string source, int start, StringRule rule, string close, char? escape, out bool closed)
	{
		closed = false;
		int scan = start;
		while (scan < source.Length)
		{
			char current = source[scan];

			if (!rule.AllowMultiline && (current == '\n' || current == '\r'))
			{
				// An unterminated single-line literal ends at the newline rather than swallowing the
				// rest of the file, so one stray quote cannot recolor the whole document.
				return scan;
			}

			if (escape.HasValue && current == escape.Value && scan + 1 < source.Length)
			{
				scan += 2;
				continue;
			}

			if (Matches(source, scan, close))
			{
				int afterClose = scan + close.Length;
				if (rule.DoubledCloseEscapes && Matches(source, afterClose, close))
				{
					scan = afterClose + close.Length;
					continue;
				}

				closed = true;
				return afterClose;
			}

			scan++;
		}

		return source.Length;
	}

	// A C++ raw string opens with R"delim( and closes only on )delim", so the delimiter and its '('
	// join the opener and the close gains the same delimiter. False when no valid delimiter follows.
	private static bool TryExtendByRawDelimiter(string source, int index, StringRule rule, ref int openLength, ref string close)
	{
		if (!TryReadRawDelimiter(source, index + openLength, out string delimiter))
		{
			return false;
		}

		openLength += delimiter.Length + 1;
		close = string.Concat(")", delimiter, rule.Close);
		return true;
	}

	// A C++ raw-string delimiter: at most 16 characters, none of them a space, quote, parenthesis,
	// backslash or control character, ended by '('.
	private static bool TryReadRawDelimiter(string source, int start, out string delimiter)
	{
		const int MaxLength = 16;
		delimiter = string.Empty;
		int scan = start;
		while (scan < source.Length && scan - start <= MaxLength)
		{
			char current = source[scan];
			if (current == '(')
			{
				delimiter = source[start..scan];
				return true;
			}

			if (current is ' ' or ')' or '\\' or '"' || char.IsControl(current))
			{
				return false;
			}

			scan++;
		}

		return false;
	}

	private static bool TryReadDirective(string source, LanguageDefinition language, ref int index, out TokenKind kind, out TokenBody body)
	{
		kind = TokenKind.Preprocessor;
		body = TokenBody.None;
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

	private static bool TryReadNumber(string source, ref int index, out TokenKind kind, out TokenBody body)
	{
		kind = TokenKind.Number;
		body = TokenBody.None;
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

	private static bool TryReadIdentifier(string source, LanguageDefinition language, KeywordLookup lookup, ref int index, out TokenKind kind, out TokenBody body)
	{
		kind = TokenKind.Plain;
		body = TokenBody.None;
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

	private static bool TryReadSymbol(string source, LanguageDefinition language, ref int index, out TokenKind kind, out TokenBody body)
	{
		body = TokenBody.None;
		char current = source[index];

		if (language.OperatorCharacters.Contains(current, StringComparison.Ordinal))
		{
			kind = TokenKind.Operator;
			int scan = index + 1;
			while (scan < source.Length
				&& language.OperatorCharacters.Contains(source[scan], StringComparison.Ordinal)
				&& !(language.RegexLiterals && source[scan] == '/'))
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
		private readonly Dictionary<char, StringRule[]> stringsByFirstCharacter;

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

			// Python spells a literal with any of two dozen prefixes in any case, which is a hundred
			// rules; grouping them by their first character keeps every position from trying them all.
			stringsByFirstCharacter = Strings
				.Where(rule => rule.Open.Length > 0)
				.GroupBy(rule => rule.Open[0])
				.ToDictionary(group => group.Key, group => group.ToArray());
		}

		public StringRule[] StringsStartingWith(char first) =>
			stringsByFirstCharacter.TryGetValue(first, out StringRule[]? rules) ? rules : [];

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
