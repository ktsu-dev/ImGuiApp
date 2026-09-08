// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.SyntaxHighlighting;

using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Breaks the tokenizers' flat token stream into lines. Tokens for block comments and multi-line
/// strings span line breaks, so this is where they are cut, line endings normalized, and tabs
/// expanded against the column they start at.
/// </summary>
internal static class LineSplitter
{
	/// <summary>Splits tokens into lines, expanding tabs to spaces.</summary>
	/// <param name="tokens">The tokens produced by a tokenizer.</param>
	/// <param name="tabWidth">The tab stop width in characters; values below one are treated as one.</param>
	/// <returns>The lines, without trailing line-break tokens.</returns>
	public static IReadOnlyList<HighlightedLine> Split(IReadOnlyList<HighlightedToken> tokens, int tabWidth)
	{
		Ensure.NotNull(tokens);

		int stop = Math.Max(1, tabWidth);
		List<HighlightedLine> lines = [];
		List<HighlightedToken> current = [];
		int column = 0;
		bool sawLineBreak = false;

		void EndLine()
		{
			lines.Add(current.Count == 0 ? HighlightedLine.Empty : new HighlightedLine([.. current]));
			current = [];
			column = 0;
		}

		foreach (HighlightedToken token in tokens)
		{
			int segmentStart = 0;
			string text = token.Text;

			for (int i = 0; i <= text.Length; i++)
			{
				bool atEnd = i == text.Length;
				if (!atEnd && text[i] != '\n')
				{
					continue;
				}

				int segmentEnd = i;
				if (!atEnd && segmentEnd > segmentStart && text[segmentEnd - 1] == '\r')
				{
					segmentEnd--;
				}

				string segment = ExpandTabs(text[segmentStart..segmentEnd], stop, ref column);
				if (segment.Length > 0)
				{
					current.Add(new HighlightedToken(token.Kind, segment));
				}

				if (!atEnd)
				{
					EndLine();
					sawLineBreak = true;
					segmentStart = i + 1;
				}
			}
		}

		// The text after the last line break is always a line, even when empty; a source that ends
		// with a line break does not get a phantom line after it.
		if (current.Count > 0 || !sawLineBreak || lines.Count == 0)
		{
			EndLine();
		}

		return lines;
	}

	private static string ExpandTabs(string text, int tabStop, ref int column)
	{
		if (!text.Contains('\t', StringComparison.Ordinal))
		{
			column += text.Length;
			return text;
		}

		StringBuilder builder = new(text.Length + tabStop);
		foreach (char character in text)
		{
			if (character == '\t')
			{
				int spaces = tabStop - (column % tabStop);
				builder.Append(' ', spaces);
				column += spaces;
				continue;
			}

			builder.Append(character);
			column++;
		}

		return builder.ToString();
	}
}
