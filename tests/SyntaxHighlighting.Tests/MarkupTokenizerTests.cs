// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SyntaxHighlighting.Tests;

using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class MarkupTokenizerTests
{
	[TestMethod]
	public void Xml_ClassifiesTagsAttributesAndValues()
	{
		IReadOnlyList<HighlightedLine> lines =
			SyntaxHighlighter.Highlight("""<Project Sdk="ktsu.Sdk">text</Project>""", "xml");

		TokenAssert.HasToken(lines, "Project", TokenKind.Tag);
		TokenAssert.HasToken(lines, "Sdk", TokenKind.AttributeName);
		TokenAssert.HasToken(lines, "\"ktsu.Sdk\"", TokenKind.StringLiteral);
		TokenAssert.HasToken(lines, "text", TokenKind.Plain);
	}

	[TestMethod]
	public void Xml_CommentsAndDeclarationsAreNotTags()
	{
		IReadOnlyList<HighlightedLine> lines =
			SyntaxHighlighter.Highlight("<?xml version=\"1.0\"?>\n<!-- note -->\n<a/>", "xml");

		TokenAssert.HasToken(lines, "<?xml version=\"1.0\"?>", TokenKind.Preprocessor);
		TokenAssert.HasToken(lines, "<!-- note -->", TokenKind.Comment);
		TokenAssert.HasToken(lines, "a", TokenKind.Tag);
		TokenAssert.HasToken(lines, "/>", TokenKind.Punctuation);
	}

	[TestMethod]
	public void Html_EntitiesAreConstants()
	{
		IReadOnlyList<HighlightedLine> lines = SyntaxHighlighter.Highlight("<p>a &amp; b</p>", "html");

		TokenAssert.HasToken(lines, "&amp;", TokenKind.Constant);
	}

	[TestMethod]
	public void Xml_BareLessThanStaysText()
	{
		IReadOnlyList<HighlightedLine> lines = SyntaxHighlighter.Highlight("<p>1 < 2</p>", "xml");

		Assert.IsEmpty(
			TokenAssert.Flatten(lines).Where(token => token.Kind == TokenKind.Tag && token.Text == "2"),
			$"A comparison was read as a tag: {TokenAssert.Describe(lines)}");
	}

	[TestMethod]
	public void Html_ScriptBodyIsTextUpToItsClosingTag()
	{
		const string Markup = "<script>\n  if (count<limit) start(); // don't wait\n</script>\n<p class=\"intro\">Hello</p>\n<a href=\"/docs\">Docs</a>";
		IReadOnlyList<HighlightedLine> lines = SyntaxHighlighter.Highlight(Markup, "html");

		TokenAssert.Reconstructs(lines, Markup);
		Assert.IsTrue(
			lines[1].Tokens.All(token => token.Kind == TokenKind.Plain),
			$"The script body was read as markup: {TokenAssert.Describe(lines)}");
		Assert.AreEqual(
			"script script p p a a",
			string.Join(' ', TokenAssert.Flatten(lines).Where(token => token.Kind == TokenKind.Tag).Select(token => token.Text)),
			$"The tags around the script were not read as tags: {TokenAssert.Describe(lines)}");
		TokenAssert.HasToken(lines, "class", TokenKind.AttributeName);
		TokenAssert.HasToken(lines, "\"intro\"", TokenKind.StringLiteral);
		TokenAssert.HasToken(lines, "href", TokenKind.AttributeName);
		TokenAssert.HasToken(lines, "\"/docs\"", TokenKind.StringLiteral);
	}

	[TestMethod]
	public void Html_StyleBodyIsTextAndClosesOnlyOnItsOwnTag()
	{
		const string Markup = "<STYLE type=\"text/css\">a::after { content: '</b>'; }</styles></Style><b>x</b>";
		IReadOnlyList<HighlightedLine> lines = SyntaxHighlighter.Highlight(Markup, "html");

		TokenAssert.Reconstructs(lines, Markup);
		TokenAssert.HasToken(lines, "a::after { content: '</b>'; }</styles>", TokenKind.Plain);
		TokenAssert.HasToken(lines, "type", TokenKind.AttributeName);
		Assert.AreEqual(
			"STYLE Style b b",
			string.Join(' ', TokenAssert.Flatten(lines).Where(token => token.Kind == TokenKind.Tag).Select(token => token.Text)),
			$"Unexpected tags: {TokenAssert.Describe(lines)}");
	}

	[TestMethod]
	public void Html_UnclosedScriptBodyStaysText()
	{
		IReadOnlyList<HighlightedLine> lines = SyntaxHighlighter.Highlight("<script>if (a<b) go('x</scripts>')", "html");

		TokenAssert.HasToken(lines, "if (a<b) go('x</scripts>')", TokenKind.Plain);
	}

	[TestMethod]
	public void Xml_ScriptElementIsStillMarkup()
	{
		IReadOnlyList<HighlightedLine> lines = SyntaxHighlighter.Highlight("<script><item/></script>", "xml");

		TokenAssert.HasToken(lines, "item", TokenKind.Tag);
	}

	[TestMethod]
	public void Xml_TokensReconstructTheSource()
	{
		string markup = "<root>\n  <item id=\"1\">value</item>\n</root>";
		IReadOnlyList<HighlightedLine> lines = SyntaxHighlighter.Highlight(markup, "xml");

		TokenAssert.Reconstructs(lines, markup);
	}
}
