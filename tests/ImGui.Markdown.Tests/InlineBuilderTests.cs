// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Markdown.Tests;

using System.Collections.Generic;
using System.Linq;

using ktsu.ImGui.Markdown;

using Markdig.Syntax;
using Markdig.Syntax.Inlines;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class InlineBuilderTests
{
	private static ContainerInline? FirstParagraphInline(string md)
	{
		Markdown.MarkdownDocument document = new(md);
		ParagraphBlock paragraph = document.Ast.Descendants<ParagraphBlock>().First();
		return paragraph.Inline;
	}

	[TestMethod]
	public void Build_BoldText_ProducesBoldRun()
	{
		IReadOnlyList<InlineRun> runs = InlineBuilder.Build(FirstParagraphInline("**strong**"));
		Assert.IsTrue(runs.Any(r => r.Role == MarkdownFontRole.Bold && r.Text.Contains("strong")));
	}

	[TestMethod]
	public void Build_ItalicText_ProducesItalicRun()
	{
		IReadOnlyList<InlineRun> runs = InlineBuilder.Build(FirstParagraphInline("*soft*"));
		Assert.IsTrue(runs.Any(r => r.Role == MarkdownFontRole.Italic));
	}

	[TestMethod]
	public void Build_InlineCode_ProducesCodeRun()
	{
		IReadOnlyList<InlineRun> runs = InlineBuilder.Build(FirstParagraphInline("`code`"));
		Assert.IsTrue(runs.Any(r => r.Role == MarkdownFontRole.Code && r.Text == "code"));
	}

	[TestMethod]
	public void Build_Link_AttachesUrlToChildRuns()
	{
		IReadOnlyList<InlineRun> runs = InlineBuilder.Build(FirstParagraphInline("[text](https://x.com)"));
		Assert.IsTrue(runs.Any(r => r.LinkUrl == "https://x.com" && !r.IsImage));
	}

	[TestMethod]
	public void Build_Image_ProducesImageRun()
	{
		IReadOnlyList<InlineRun> runs = InlineBuilder.Build(FirstParagraphInline("![alt](logo.png)"));
		Assert.IsTrue(runs.Any(r => r.IsImage && r.Text == "logo.png"));
	}

	[TestMethod]
	public void Build_NamedEntity_ProducesItsCharacter()
	{
		IReadOnlyList<InlineRun> runs = InlineBuilder.Build(FirstParagraphInline("Tom &amp; Jerry"));
		Assert.AreEqual("Tom & Jerry", string.Concat(runs.Select(r => r.Text)));
	}

	[TestMethod]
	public void Build_NumericEntity_ProducesItsCharacter()
	{
		IReadOnlyList<InlineRun> runs = InlineBuilder.Build(FirstParagraphInline("&#169; 2026"));
		Assert.AreEqual("\u00A9 2026", string.Concat(runs.Select(r => r.Text)));
	}

	[TestMethod]
	public void Build_EntityInsideEmphasis_KeepsTheEmphasisRole()
	{
		IReadOnlyList<InlineRun> runs = InlineBuilder.Build(FirstParagraphInline("**a &lt; b**"));
		Assert.IsTrue(runs.Any(r => r.Role == MarkdownFontRole.Bold && r.Text == "<"));
		Assert.IsFalse(runs.Any(r => r.Text.Contains("Markdig", System.StringComparison.Ordinal)));
	}

	[TestMethod]
	public void Build_Null_ReturnsEmpty()
	{
		IReadOnlyList<InlineRun> runs = InlineBuilder.Build(null);
		Assert.AreEqual(0, runs.Count);
	}

	[TestMethod]
	public void Build_TrailingSpacesLineBreak_ProducesHardBreakRun()
	{
		IReadOnlyList<InlineRun> runs = InlineBuilder.Build(FirstParagraphInline("line one  \nline two"));
		Assert.AreEqual(1, runs.Count(r => r.IsHardBreak));
	}

	[TestMethod]
	public void Build_BackslashLineBreak_ProducesHardBreakRun()
	{
		IReadOnlyList<InlineRun> runs = InlineBuilder.Build(FirstParagraphInline("line one\\\nline two"));
		Assert.AreEqual(1, runs.Count(r => r.IsHardBreak));
	}

	[TestMethod]
	public void Build_SoftLineBreak_ProducesSpaceRun()
	{
		IReadOnlyList<InlineRun> runs = InlineBuilder.Build(FirstParagraphInline("line one\nline two"));
		Assert.IsFalse(runs.Any(r => r.IsHardBreak));
		Assert.IsTrue(runs.Any(r => r.Text == " "));
	}

	[TestMethod]
	public void Build_WithABoldBase_GivesPlainTextBoldAndItalicBoldItalic()
	{
		// A table header row's cells: the header's weight reaches the text instead of being replaced by Body.
		IReadOnlyList<InlineRun> runs = InlineBuilder.Build(FirstParagraphInline("plain *slanted* `code`"), MarkdownFontRole.Bold);
		Assert.AreEqual(MarkdownFontRole.Bold, runs.First(r => r.Text.Contains("plain")).Role);
		Assert.AreEqual(MarkdownFontRole.BoldItalic, runs.First(r => r.Text == "slanted").Role);
		Assert.AreEqual(MarkdownFontRole.Code, runs.First(r => r.Text == "code").Role);
	}

	[TestMethod]
	public void Build_WithAHeadingBase_KeepsTheHeadingRole()
	{
		IReadOnlyList<InlineRun> runs = InlineBuilder.Build(FirstParagraphInline("plain **strong**"), MarkdownFontRole.H1);
		Assert.IsTrue(runs.Where(r => r.Text.Trim().Length > 0).All(r => r.Role == MarkdownFontRole.H1));
	}

	[TestMethod]
	public void Build_Image_CarriesItsAltTextAndKeepsItsSrc()
	{
		IReadOnlyList<InlineRun> runs = InlineBuilder.Build(FirstParagraphInline("[![Build *status*](https://img.example/b.svg)](https://example.com)"));
		InlineRun image = runs.Single(r => r.IsImage);
		Assert.AreEqual("https://img.example/b.svg", image.Text);
		Assert.AreEqual("Build status", image.AltText);
		Assert.AreEqual("https://example.com", image.LinkUrl);
	}

	[TestMethod]
	public void Build_ImageWithNoDescription_HasEmptyAltText()
	{
		IReadOnlyList<InlineRun> runs = InlineBuilder.Build(FirstParagraphInline("![](logo.png)"));
		Assert.AreEqual(string.Empty, runs.Single(r => r.IsImage).AltText);
	}
}
