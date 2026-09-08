// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SyntaxHighlighting;

/// <summary>
/// Where a token's body sits inside its text, and the rule that escaped it. A line comment has no
/// closing delimiter, and an unterminated string or block comment never reached its own, so both
/// lengths are recorded by the scanner rather than derived afterwards.
/// </summary>
/// <param name="OpenLength">The opening delimiter's length.</param>
/// <param name="CloseLength">The closing delimiter's length, or zero when it was never reached.</param>
/// <param name="String">The string rule the token came from, when it is a literal.</param>
internal readonly record struct TokenBody(int OpenLength, int CloseLength, StringRule? String)
{
	/// <summary>A token with no body to look inside.</summary>
	public static TokenBody None { get; }
}
