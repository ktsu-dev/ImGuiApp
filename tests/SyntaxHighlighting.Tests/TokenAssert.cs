// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SyntaxHighlighting.Tests;

using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Helpers for asserting against tokenized lines.</summary>
internal static class TokenAssert
{
	/// <summary>Flattens every line's tokens into one sequence.</summary>
	/// <param name="lines">The tokenized lines.</param>
	/// <returns>Every token, in order.</returns>
	public static IReadOnlyList<HighlightedToken> Flatten(IReadOnlyList<HighlightedLine> lines) =>
		[.. lines.SelectMany(line => line.Tokens)];

	/// <summary>Asserts that the given text was classified as the expected kind, exactly once.</summary>
	/// <param name="lines">The tokenized lines.</param>
	/// <param name="text">The token text to look for.</param>
	/// <param name="expected">The expected kind.</param>
	public static void HasToken(IReadOnlyList<HighlightedLine> lines, string text, TokenKind expected)
	{
		List<HighlightedToken> matches = [.. Flatten(lines).Where(token => token.Text == text)];
		Assert.IsNotEmpty(matches, $"No token with the text '{text}' was produced. Tokens: {Describe(lines)}");
		Assert.AreEqual(expected, matches[0].Kind, $"'{text}' was classified as {matches[0].Kind}. Tokens: {Describe(lines)}");
	}

	/// <summary>Asserts that concatenating every token reproduces the expected text.</summary>
	/// <param name="lines">The tokenized lines.</param>
	/// <param name="expected">The expected reconstruction, with lines joined by a newline.</param>
	public static void Reconstructs(IReadOnlyList<HighlightedLine> lines, string expected) =>
		Assert.AreEqual(expected, string.Join("\n", lines.Select(line => line.ToText())));

	/// <summary>Renders the tokens as a readable string for assertion messages.</summary>
	/// <param name="lines">The tokenized lines.</param>
	/// <returns>A description of every token.</returns>
	public static string Describe(IReadOnlyList<HighlightedLine> lines)
	{
		StringBuilder builder = new();
		foreach (HighlightedToken token in Flatten(lines))
		{
			builder.Append('[').Append(token.Kind).Append(' ').Append('\'').Append(token.Text).Append("'] ");
		}

		return builder.ToString();
	}
}
