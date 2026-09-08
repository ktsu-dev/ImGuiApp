// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.SyntaxHighlighting;

using System.Collections.Generic;

/// <summary>A classified run of text within a line.</summary>
/// <param name="Kind">The syntactic role, which selects the token's color.</param>
/// <param name="Text">The token text; never contains a line break.</param>
public readonly record struct HighlightedToken(TokenKind Kind, string Text);

/// <summary>One line of classified tokens.</summary>
public sealed class HighlightedLine
{
	/// <summary>An empty line.</summary>
	public static HighlightedLine Empty { get; } = new([]);

	/// <summary>The tokens on this line, in source order.</summary>
	public IReadOnlyList<HighlightedToken> Tokens { get; }

	/// <summary>Initializes a new instance from a token list.</summary>
	/// <param name="tokens">The tokens on the line, in source order.</param>
	public HighlightedLine(IReadOnlyList<HighlightedToken> tokens)
	{
		Ensure.NotNull(tokens);
		Tokens = tokens;
	}

	/// <summary>Reconstructs the line's text by concatenating its tokens.</summary>
	/// <returns>The line text, with tabs already expanded to spaces.</returns>
	public string ToText()
	{
		System.Text.StringBuilder builder = new();
		foreach (HighlightedToken token in Tokens)
		{
			builder.Append(token.Text);
		}

		return builder.ToString();
	}
}
