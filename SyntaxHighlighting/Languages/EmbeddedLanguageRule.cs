// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SyntaxHighlighting;

using System;

/// <summary>
/// The token kinds an embedded snippet may be found in. A rule is only considered for the hosts it
/// names, which is what keeps a rule for SQL from recoloring English prose in a comment.
/// </summary>
[Flags]
public enum EmbeddedHosts
{
	/// <summary>No host: the rule never applies.</summary>
	None = 0,

	/// <summary>The body of a string or character literal.</summary>
	StringLiteral = 1,

	/// <summary>The body of an ordinary line or block comment.</summary>
	Comment = 2,

	/// <summary>The body of a documentation comment.</summary>
	DocComment = 4,

	/// <summary>Either kind of comment.</summary>
	Comments = Comment | DocComment,

	/// <summary>Every host.</summary>
	All = StringLiteral | Comment | DocComment,
}

/// <summary>
/// Highlights another language found inside a comment or string. XML in a doc comment, JSON in a
/// test fixture and SQL in a query string are all written inside a host language's literals, and a
/// rule says which of them to look for and where.
/// </summary>
/// <remarks>
/// Rules are tried in order and the first match wins, so put the specific ones first. Embedding is
/// one level deep: a snippet found by a rule is tokenized with the embedded language's own comment,
/// string and keyword rules, but that language's <see cref="LanguageDefinition.EmbeddedLanguages"/>
/// are not consulted again.
/// </remarks>
public sealed record EmbeddedLanguageRule
{
	/// <summary>The language name or alias the matched content is highlighted as.</summary>
	public required string Language { get; init; }

	/// <summary>The token kinds this rule looks inside.</summary>
	public EmbeddedHosts Hosts { get; init; } = EmbeddedHosts.All;

	/// <summary>
	/// Decides whether a body is written in <see cref="Language"/>, given the body with the host's
	/// delimiters removed and its escape sequences resolved. When <see langword="null"/> every body
	/// in a matching host is accepted, which is what makes a rule like "doc comments are XML"
	/// unconditional.
	/// </summary>
	public Func<string, bool>? Matches { get; init; }

	/// <summary>Whether this rule claims a body found in the given host.</summary>
	/// <param name="host">The host the body was found in.</param>
	/// <param name="body">The body, unescaped.</param>
	/// <returns><see langword="true"/> when the body should be highlighted as <see cref="Language"/>.</returns>
	public bool AppliesTo(EmbeddedHosts host, string body) =>
		(Hosts & host) != EmbeddedHosts.None && (Matches is null || Matches(body));
}
