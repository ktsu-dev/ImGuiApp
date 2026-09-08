// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SyntaxHighlighting.Tests;

using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class EmbeddedLanguageTests
{
	private static IReadOnlyList<HighlightedLine> Highlight(string code, string language) =>
		SyntaxHighlighter.Highlight(code, language);

	[TestMethod]
	public void DocCommentTagsAreXmlAndItsProseStaysAComment()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("/// <param name=\"x\">The x.</param>", "csharp");

		TokenAssert.HasToken(lines, "param", TokenKind.Tag);
		TokenAssert.HasToken(lines, "name", TokenKind.AttributeName);
		TokenAssert.HasToken(lines, "\"x\"", TokenKind.StringLiteral);
		TokenAssert.HasToken(lines, "The x.", TokenKind.DocComment);
		TokenAssert.Reconstructs(lines, "/// <param name=\"x\">The x.</param>");
	}

	[TestMethod]
	public void ADocCommentWithNoMarkupIsStillOneComment()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("/// Just prose, no tags.", "csharp");

		TokenAssert.HasToken(lines, "/// Just prose, no tags.", TokenKind.DocComment);
	}

	[TestMethod]
	public void JsonInAVerbatimStringIsHighlighted()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("var body = @\"{\"\"id\"\": 7, \"\"ok\"\": true}\";", "csharp");

		TokenAssert.HasToken(lines, "\"\"id\"\"", TokenKind.Property);
		TokenAssert.HasToken(lines, "7", TokenKind.Number);
		TokenAssert.HasToken(lines, "true", TokenKind.Constant);
		TokenAssert.Reconstructs(lines, "var body = @\"{\"\"id\"\": 7, \"\"ok\"\": true}\";");
	}

	[TestMethod]
	public void JsonInAnEscapedStringIsHighlightedAndKeepsItsEscapes()
	{
		// The tokenizer sees {"id": 7} but every token is re-sliced from the source, so the
		// backslashes are still drawn — they just travel with the token they escape.
		string code = "string body = \"{\\\"id\\\": 7}\";";
		IReadOnlyList<HighlightedLine> lines = Highlight(code, "csharp");

		TokenAssert.Reconstructs(lines, code);
		Assert.IsTrue(
			TokenAssert.Flatten(lines).Any(token => token.Kind == TokenKind.Property && token.Text.Contains("id")),
			$"The escaped key was not classified: {TokenAssert.Describe(lines)}");
		TokenAssert.HasToken(lines, "7", TokenKind.Number);
	}

	[TestMethod]
	public void SqlInAStringIsHighlighted()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("var query = \"SELECT id FROM users WHERE active = 1\";", "csharp");

		TokenAssert.HasToken(lines, "SELECT", TokenKind.Keyword);
		TokenAssert.HasToken(lines, "FROM", TokenKind.Keyword);
		TokenAssert.HasToken(lines, "WHERE", TokenKind.Keyword);
	}

	[TestMethod]
	public void ProseThatOpensWithAnSqlKeywordIsNotSql()
	{
		// "Update" opens the sentence but no second keyword follows, which is what stops the sniffer
		// from recoloring English.
		IReadOnlyList<HighlightedLine> lines = Highlight("var note = \"Update the cache and carry on\";", "csharp");

		TokenAssert.HasToken(lines, "\"Update the cache and carry on\"", TokenKind.StringLiteral);
	}

	[TestMethod]
	public void MarkupInAStringIsHighlighted()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("var markup = \"<div class='a'>hi</div>\";", "csharp");

		TokenAssert.HasToken(lines, "div", TokenKind.Tag);
		TokenAssert.HasToken(lines, "class", TokenKind.AttributeName);
		TokenAssert.HasToken(lines, "hi", TokenKind.StringLiteral);
	}

	[TestMethod]
	public void AnOrdinaryStringIsLeftAlone()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("var greeting = \"hello, world\";", "csharp");

		TokenAssert.HasToken(lines, "\"hello, world\"", TokenKind.StringLiteral);
	}

	[TestMethod]
	public void JsonInABlockCommentIsHighlighted()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("/* {\"a\": [1, 2]} */\nint x;", "csharp");

		TokenAssert.HasToken(lines, "\"a\"", TokenKind.Property);
		TokenAssert.HasToken(lines, "1", TokenKind.Number);
		TokenAssert.HasToken(lines, "int", TokenKind.Type);
		TokenAssert.Reconstructs(lines, "/* {\"a\": [1, 2]} */\nint x;");
	}

	[TestMethod]
	public void AHintCommentNamesTheLanguageOfTheNextString()
	{
		// The fragment is not recognizable on its own: the hint is what makes it SQL.
		IReadOnlyList<HighlightedLine> lines = Highlight("// lang=sql\nvar tail = \"ORDER BY id DESC\";", "csharp");

		TokenAssert.HasToken(lines, "ORDER", TokenKind.Keyword);
		TokenAssert.HasToken(lines, "BY", TokenKind.Keyword);
	}

	[TestMethod]
	public void AHintCommentIsSpentOnOneStringOnly()
	{
		IReadOnlyList<HighlightedLine> lines =
			Highlight("// language=sql\nvar a = \"ORDER BY id\";\nvar b = \"ORDER BY id\";", "csharp");

		Assert.AreEqual(
			1,
			TokenAssert.Flatten(lines).Count(token => token.Kind == TokenKind.Keyword && token.Text == "ORDER"),
			$"The hint outlived the string it named: {TokenAssert.Describe(lines)}");
	}

	[TestMethod]
	public void AnUnknownHintLeavesTheStringAlone()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("// lang=klingon\nvar a = \"ORDER BY id\";", "csharp");

		TokenAssert.HasToken(lines, "\"ORDER BY id\"", TokenKind.StringLiteral);
	}

	[TestMethod]
	public void EmbeddedJsonInsideJsonIsHighlighted()
	{
		string code = "{\"payload\": \"{\\\"n\\\": 1}\"}";
		IReadOnlyList<HighlightedLine> lines = Highlight(code, "json");

		TokenAssert.Reconstructs(lines, code);
		Assert.IsTrue(
			TokenAssert.Flatten(lines).Any(token => token.Kind == TokenKind.Number && token.Text.Contains('1')),
			$"The nested value was not classified: {TokenAssert.Describe(lines)}");
	}

	[TestMethod]
	public void EmbeddingIsOnlyOneLevelDeep()
	{
		// The C# string holds JSON, and that JSON's own string holds SQL. The SQL is not looked for:
		// stopping at one level is what bounds the work and keeps definitions from cycling.
		string code = "var a = @\"{\"\"q\"\": \"\"SELECT id FROM t\"\"}\";";
		IReadOnlyList<HighlightedLine> lines = Highlight(code, "csharp");

		TokenAssert.Reconstructs(lines, code);
		Assert.IsFalse(
			TokenAssert.Flatten(lines).Any(token => token.Text == "SELECT" && token.Kind == TokenKind.Keyword),
			$"The second level was expanded: {TokenAssert.Describe(lines)}");
	}

	[TestMethod]
	public void ALanguageWithNoRulesEmbedsNothing()
	{
		IReadOnlyList<HighlightedLine> lines = Highlight("a: \"<b>c</b>\"", "css");

		TokenAssert.HasToken(lines, "\"<b>c</b>\"", TokenKind.StringLiteral);
	}

	[TestMethod]
	public void EmbeddingCanBeTurnedOffByDerivingADefinition()
	{
		LanguageDefinition quiet = BuiltInLanguages.CSharp with
		{
			Name = "csharp-no-embedding",
			Aliases = [],
			EmbeddedLanguages = [],
		};
		LanguageRegistry.Register(quiet);

		IReadOnlyList<HighlightedLine> lines = Highlight("/// <summary>Doc.</summary>", "csharp-no-embedding");

		TokenAssert.HasToken(lines, "/// <summary>Doc.</summary>", TokenKind.DocComment);
	}

	[TestMethod]
	public void ARuleNamingAnUnregisteredLanguageFallsThroughToTheNext()
	{
		// The first rule claims every string but names nothing the registry knows. The search has to
		// carry on to the JSON rule rather than treating the claim as the end of it.
		LanguageDefinition host = BuiltInLanguages.CSharp with
		{
			Name = "csharp-with-a-dead-rule",
			Aliases = [],
			EmbeddedLanguages =
			[
				new EmbeddedLanguageRule { Language = "not-a-language", Hosts = EmbeddedHosts.StringLiteral },
				BuiltInEmbeddedRules.Json,
			],
		};
		LanguageRegistry.Register(host);

		IReadOnlyList<HighlightedLine> lines = Highlight("var a = @\"{\"\"id\"\": 7}\";", "csharp-with-a-dead-rule");

		TokenAssert.HasToken(lines, "\"\"id\"\"", TokenKind.Property);
		TokenAssert.HasToken(lines, "7", TokenKind.Number);
	}

	[TestMethod]
	public void AnUnterminatedStringStillReconstructs()
	{
		string code = "var a = \"{\"; var b = 1;";
		IReadOnlyList<HighlightedLine> lines = Highlight(code, "csharp");

		TokenAssert.Reconstructs(lines, code);
	}

	[TestMethod]
	public void JsonSnifferAcceptsWholeDocumentsOnly()
	{
		Assert.IsTrue(EmbeddedContent.LooksLikeJson("""{"a": [1, 2, {"b": null}]}"""));
		Assert.IsTrue(EmbeddedContent.LooksLikeJson("  []  "));
		Assert.IsFalse(EmbeddedContent.LooksLikeJson("""{"a": 1"""), "A truncated object was accepted.");
		Assert.IsFalse(EmbeddedContent.LooksLikeJson("{ see the note above }"), "Prose in braces was accepted.");
		Assert.IsFalse(EmbeddedContent.LooksLikeJson("\"a\""), "A bare string was accepted.");
		Assert.IsFalse(EmbeddedContent.LooksLikeJson("""{"a": 1} trailing"""), "Trailing text was accepted.");
	}

	[TestMethod]
	public void MarkupSnifferNeedsATagOnBothEnds()
	{
		Assert.IsTrue(EmbeddedContent.LooksLikeMarkup("<a href='x'>y</a>"));
		Assert.IsTrue(EmbeddedContent.LooksLikeMarkup("<!-- note -->"));
		Assert.IsFalse(EmbeddedContent.LooksLikeMarkup("a < b > c"), "An arithmetic comparison was accepted.");
		Assert.IsFalse(EmbeddedContent.LooksLikeMarkup("<a> trailing text"), "Trailing text was accepted.");
	}

	[TestMethod]
	public void SqlSnifferNeedsTwoKeywords()
	{
		Assert.IsTrue(EmbeddedContent.LooksLikeSql("select id from users"));
		Assert.IsTrue(EmbeddedContent.LooksLikeSql("INSERT INTO t (a) VALUES (1)"));
		Assert.IsFalse(EmbeddedContent.LooksLikeSql("Delete the temporary directory"), "Prose was accepted.");
		Assert.IsFalse(EmbeddedContent.LooksLikeSql("select"), "A lone keyword was accepted.");
	}
}
