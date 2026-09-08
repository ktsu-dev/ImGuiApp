// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.SyntaxHighlighting.Tests;

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
			ImGuiSyntaxHighlighting.Highlight("""<Project Sdk="ktsu.Sdk">text</Project>""", "xml");

		TokenAssert.HasToken(lines, "Project", TokenKind.Tag);
		TokenAssert.HasToken(lines, "Sdk", TokenKind.AttributeName);
		TokenAssert.HasToken(lines, "\"ktsu.Sdk\"", TokenKind.StringLiteral);
		TokenAssert.HasToken(lines, "text", TokenKind.Plain);
	}

	[TestMethod]
	public void Xml_CommentsAndDeclarationsAreNotTags()
	{
		IReadOnlyList<HighlightedLine> lines =
			ImGuiSyntaxHighlighting.Highlight("<?xml version=\"1.0\"?>\n<!-- note -->\n<a/>", "xml");

		TokenAssert.HasToken(lines, "<?xml version=\"1.0\"?>", TokenKind.Preprocessor);
		TokenAssert.HasToken(lines, "<!-- note -->", TokenKind.Comment);
		TokenAssert.HasToken(lines, "a", TokenKind.Tag);
		TokenAssert.HasToken(lines, "/>", TokenKind.Punctuation);
	}

	[TestMethod]
	public void Html_EntitiesAreConstants()
	{
		IReadOnlyList<HighlightedLine> lines = ImGuiSyntaxHighlighting.Highlight("<p>a &amp; b</p>", "html");

		TokenAssert.HasToken(lines, "&amp;", TokenKind.Constant);
	}

	[TestMethod]
	public void Xml_BareLessThanStaysText()
	{
		IReadOnlyList<HighlightedLine> lines = ImGuiSyntaxHighlighting.Highlight("<p>1 < 2</p>", "xml");

		Assert.IsEmpty(
			TokenAssert.Flatten(lines).Where(token => token.Kind == TokenKind.Tag && token.Text == "2"),
			$"A comparison was read as a tag: {TokenAssert.Describe(lines)}");
	}

	[TestMethod]
	public void Xml_TokensReconstructTheSource()
	{
		string markup = "<root>\n  <item id=\"1\">value</item>\n</root>";
		IReadOnlyList<HighlightedLine> lines = ImGuiSyntaxHighlighting.Highlight(markup, "xml");

		TokenAssert.Reconstructs(lines, markup);
	}
}
