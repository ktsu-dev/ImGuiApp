// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Markdown.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class HarnessSmokeTest
{
	[TestMethod]
	public void MarkdigResolvesAndParses()
	{
		Markdig.Syntax.MarkdownDocument document = Markdig.Markdown.Parse("# Hello");
		Assert.AreEqual(1, document.Count);
	}
}
