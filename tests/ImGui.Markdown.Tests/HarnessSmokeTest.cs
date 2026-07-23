// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

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
