// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Markdown.Tests;

using System.Linq;

using ktsu.ImGui.Markdown;

using Markdig.Syntax;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class BlockSpacingTests
{
	private const string TightList = "- one\n- two\n- three\n";
	private const string LooseList = "- one\n\n- two\n\n- three\n";
	private const string NestedTightList = "- one\n  - nested\n- two\n";

	private static ListBlock FirstList(string markdown) =>
		MarkdownParser.Parse(markdown).Descendants().OfType<ListBlock>().First();

	private static ParagraphBlock FirstItemParagraph(string markdown) =>
		FirstList(markdown).Descendants().OfType<ParagraphBlock>().First();

	[TestMethod]
	public void ShouldSpaceAfterParagraph_StandalonePara_IsSpaced()
	{
		ParagraphBlock paragraph = MarkdownParser.Parse("a paragraph\n").Descendants().OfType<ParagraphBlock>().First();

		Assert.IsTrue(BlockSpacing.ShouldSpaceAfter(paragraph));
	}

	[TestMethod]
	public void ShouldSpaceAfterParagraph_TightListItem_IsNotSpaced()
	{
		Assert.IsFalse(BlockSpacing.ShouldSpaceAfter(FirstItemParagraph(TightList)));
	}

	[TestMethod]
	public void ShouldSpaceAfterParagraph_LooseListItem_IsSpaced()
	{
		Assert.IsTrue(BlockSpacing.ShouldSpaceAfter(FirstItemParagraph(LooseList)));
	}

	[TestMethod]
	public void ShouldSpaceAfterList_TopLevelTightList_IsSpaced()
	{
		Assert.IsTrue(BlockSpacing.ShouldSpaceAfter(FirstList(TightList)));
	}

	[TestMethod]
	public void ShouldSpaceAfterList_LooseList_IsNotSpacedTwice()
	{
		Assert.IsFalse(BlockSpacing.ShouldSpaceAfter(FirstList(LooseList)));
	}

	[TestMethod]
	public void ShouldSpaceAfterList_NestedList_IsNotSpaced()
	{
		ListBlock nested = FirstList(NestedTightList).Descendants().OfType<ListBlock>().First();

		Assert.IsFalse(BlockSpacing.ShouldSpaceAfter(nested));
	}
}
