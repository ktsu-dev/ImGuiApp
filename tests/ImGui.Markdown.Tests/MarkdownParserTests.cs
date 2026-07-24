// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown.Tests;

using ktsu.ImGui.Markdown;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class MarkdownParserTests
{
	[TestMethod]
	public void Parse_ArbitraryText_DoesNotThrow()
	{
		Markdig.Syntax.MarkdownDocument doc = MarkdownParser.Parse("*unbalanced [text](");
		Assert.IsNotNull(doc);
	}

	[TestMethod]
	public void Parse_PipeTable_IsRecognized()
	{
		string md = "| a | b |\n|---|---|\n| 1 | 2 |\n";
		Markdig.Syntax.MarkdownDocument doc = MarkdownParser.Parse(md);
		int tables = 0;
		foreach (Markdig.Syntax.Block block in doc)
		{
			if (block is Markdig.Extensions.Tables.Table)
			{
				tables++;
			}
		}

		Assert.AreEqual(1, tables);
	}

	[TestMethod]
	public void GetOrParse_SameSource_ReturnsCachedInstance()
	{
		string md = "# Cached";
		Markdig.Syntax.MarkdownDocument first = MarkdownParser.GetOrParse(md);
		Markdig.Syntax.MarkdownDocument second = MarkdownParser.GetOrParse(md);
		Assert.AreSame(first, second);
	}

	[TestMethod]
	public void GetOrParse_ChangedSource_ReturnsNewInstance()
	{
		Markdig.Syntax.MarkdownDocument first = MarkdownParser.GetOrParse("# One");
		Markdig.Syntax.MarkdownDocument second = MarkdownParser.GetOrParse("# Two");
		Assert.AreNotSame(first, second);
	}

	[TestMethod]
	public void Document_ExposesSource()
	{
		MarkdownDocument document = new("# Title");
		Assert.AreEqual("# Title", document.Source);
	}
}