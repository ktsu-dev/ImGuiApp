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
}
