// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SyntaxHighlighting;

using System.Collections.Generic;

/// <summary>
/// The embedded-language rules the built-in languages use. Each is exposed individually so an
/// application can assemble its own list — or drop one it dislikes — with a <c>with</c> expression
/// on a <see cref="LanguageDefinition"/>.
/// </summary>
public static class BuiltInEmbeddedRules
{
	/// <summary>Highlights JSON found in a string or comment.</summary>
	public static EmbeddedLanguageRule Json { get; } = new()
	{
		Language = "json",
		Hosts = EmbeddedHosts.All,
		Matches = EmbeddedContent.LooksLikeJson,
	};

	/// <summary>Highlights XML or HTML found in a string or comment.</summary>
	public static EmbeddedLanguageRule Markup { get; } = new()
	{
		Language = "xml",
		Hosts = EmbeddedHosts.All,
		Matches = EmbeddedContent.LooksLikeMarkup,
	};

	/// <summary>
	/// Highlights SQL found in a string. Comments are excluded: a comment is prose often enough that
	/// the opening-keyword test would fire on English sentences.
	/// </summary>
	public static EmbeddedLanguageRule Sql { get; } = new()
	{
		Language = "sql",
		Hosts = EmbeddedHosts.StringLiteral,
		Matches = EmbeddedContent.LooksLikeSql,
	};

	/// <summary>
	/// Highlights every documentation comment as XML, whatever it contains. This is the C# doc
	/// comment convention (<c>&lt;summary&gt;</c>, <c>&lt;param&gt;</c>); prose between the tags stays
	/// comment-colored because unclassified embedded text keeps its host's color.
	/// </summary>
	public static EmbeddedLanguageRule XmlDocComments { get; } = new()
	{
		Language = "xml",
		Hosts = EmbeddedHosts.DocComment,
	};

	/// <summary>The rules a general-purpose programming language gets by default.</summary>
	public static IReadOnlyList<EmbeddedLanguageRule> Default { get; } = [Json, Markup, Sql];

	/// <summary>The rules for a data language, which has strings but no queries written in them.</summary>
	public static IReadOnlyList<EmbeddedLanguageRule> Data { get; } = [Json, Markup];
}
