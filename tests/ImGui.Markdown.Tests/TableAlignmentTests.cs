// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Markdown.Tests;

using System.Linq;

using ktsu.ImGui.Markdown;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class TableAlignmentTests
{
	private static Markdig.Extensions.Tables.Table ParseTable(string markdown) =>
		MarkdownParser.Parse(markdown).OfType<Markdig.Extensions.Tables.Table>().Single();

	[TestMethod]
	public void AlignmentOffset_Left_IsZero() =>
		Assert.AreEqual(0.0f, InlineLayout.AlignmentOffset(30.0f, 100.0f, InlineAlignment.Left));

	[TestMethod]
	public void AlignmentOffset_Right_PutsTheLineEndAtTheRightEdge() =>
		Assert.AreEqual(70.0f, InlineLayout.AlignmentOffset(30.0f, 100.0f, InlineAlignment.Right));

	[TestMethod]
	public void AlignmentOffset_Center_SplitsTheSlackEvenly() =>
		Assert.AreEqual(35.0f, InlineLayout.AlignmentOffset(30.0f, 100.0f, InlineAlignment.Center));

	[TestMethod]
	public void AlignmentOffset_LineWiderThanItsCell_StartsAtTheLeftEdge()
	{
		Assert.AreEqual(0.0f, InlineLayout.AlignmentOffset(130.0f, 100.0f, InlineAlignment.Right));
		Assert.AreEqual(0.0f, InlineLayout.AlignmentOffset(130.0f, 100.0f, InlineAlignment.Center));
	}

	[TestMethod]
	public void ColumnAlignment_ReadsEachDelimiterMarker()
	{
		Markdig.Extensions.Tables.Table table = ParseTable("| a | b | c | d |\n|:--|:-:|--:|---|\n| 1 | 2 | 3 | 4 |\n");

		Assert.AreEqual(InlineAlignment.Left, BlockRenderer.ColumnAlignment(table, 0), "|:--|");
		Assert.AreEqual(InlineAlignment.Center, BlockRenderer.ColumnAlignment(table, 1), "|:-:|");
		Assert.AreEqual(InlineAlignment.Right, BlockRenderer.ColumnAlignment(table, 2), "|--:|");
		Assert.AreEqual(InlineAlignment.Left, BlockRenderer.ColumnAlignment(table, 3), "a column with no marker stays left-aligned");
	}

	[TestMethod]
	public void ColumnAlignment_ColumnWithNoDefinition_IsLeft()
	{
		Markdig.Extensions.Tables.Table table = ParseTable("| a | b |\n|---|--:|\n| 1 | 2 |\n");

		Assert.AreEqual(InlineAlignment.Left, BlockRenderer.ColumnAlignment(table, 50));
	}
}
