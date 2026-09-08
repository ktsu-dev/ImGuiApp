// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.SyntaxHighlighting.Tests;

using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class LineSplitterTests
{
	[TestMethod]
	public void TabsExpandToTheNextTabStop()
	{
		IReadOnlyList<HighlightedLine> lines = SyntaxHighlighter.Highlight("ab\tc", "text", tabWidth: 4);

		Assert.AreEqual("ab  c", lines[0].ToText());
	}

	[TestMethod]
	public void TabWidthIsHonored()
	{
		IReadOnlyList<HighlightedLine> lines = SyntaxHighlighter.Highlight("\tx", "text", tabWidth: 2);

		Assert.AreEqual("  x", lines[0].ToText());
	}

	[TestMethod]
	public void TabStopsAreColumnRelativeAcrossTokens()
	{
		// "int" is a token of its own in C#, so the tab that follows it must still expand against the
		// column reached so far rather than restarting at zero.
		IReadOnlyList<HighlightedLine> lines = SyntaxHighlighter.Highlight("int\tx;", "csharp", tabWidth: 4);

		Assert.AreEqual("int x;", lines[0].ToText());
	}

	[TestMethod]
	public void WindowsLineEndingsAreNormalized()
	{
		IReadOnlyList<HighlightedLine> lines = SyntaxHighlighter.Highlight("a\r\nb", "text");

		Assert.AreEqual(2, lines.Count);
		Assert.AreEqual("a", lines[0].ToText());
		Assert.AreEqual("b", lines[1].ToText());
	}

	[TestMethod]
	public void ATrailingNewlineDoesNotAddAPhantomLine()
	{
		IReadOnlyList<HighlightedLine> lines = SyntaxHighlighter.Highlight("a\nb\n", "text");

		Assert.AreEqual(2, lines.Count);
	}

	[TestMethod]
	public void BlankLinesInsideTheSourceAreKept()
	{
		IReadOnlyList<HighlightedLine> lines = SyntaxHighlighter.Highlight("a\n\nb\n", "text");

		Assert.AreEqual(3, lines.Count);
		Assert.IsEmpty(lines[1].Tokens);
	}

	[TestMethod]
	public void EmptySourceProducesNoLines()
	{
		Assert.IsEmpty(SyntaxHighlighter.Highlight(string.Empty, "csharp"));
	}

	[TestMethod]
	public void MultiLineCommentsAreCutAtLineBoundaries()
	{
		IReadOnlyList<HighlightedLine> lines = SyntaxHighlighter.Highlight("/* a\nb */", "csharp");

		Assert.AreEqual(2, lines.Count);
		foreach (HighlightedLine line in lines)
		{
			foreach (HighlightedToken token in line.Tokens)
			{
				Assert.DoesNotContain("\n", token.Text, "A token still spans a line break.");
				Assert.AreEqual(TokenKind.Comment, token.Kind);
			}
		}
	}
}
