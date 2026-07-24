// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown.Tests;

using System.Collections.Generic;
using System.Numerics;
using ktsu.ImGui.Markdown;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class InlineLayoutTests
{
	private static Vector2 Measure(string text, MarkdownFontRole role, bool isImage) => new(text.Length, 1f);

	[TestMethod]
	public void Wrap_ShortRun_ProducesSingleLine()
	{
		List<InlineRun> runs = [new("hello world", MarkdownFontRole.Body, null, false)];
		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap(runs, 100f, Measure);
		Assert.AreEqual(1, lines.Count);
		Assert.AreEqual(2, lines[0].Tokens.Count);
	}

	[TestMethod]
	public void Wrap_BreaksAtWordBoundaryWhenExceedingWidth()
	{
		List<InlineRun> runs = [new("aaa bbb ccc", MarkdownFontRole.Body, null, false)];
		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap(runs, 7f, Measure);
		Assert.AreEqual(2, lines.Count);
		Assert.AreEqual(2, lines[0].Tokens.Count);
		Assert.AreEqual(1, lines[1].Tokens.Count);
		Assert.AreEqual("ccc", lines[1].Tokens[0].Text);
	}

	[TestMethod]
	public void Wrap_TokenWiderThanWidth_GetsOwnLine()
	{
		List<InlineRun> runs = [new("supercalifragilistic", MarkdownFontRole.Body, null, false)];
		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap(runs, 5f, Measure);
		Assert.AreEqual(1, lines.Count);
		Assert.AreEqual(1, lines[0].Tokens.Count);
	}

	[TestMethod]
	public void Wrap_TokenXPositions_AccountForSpaces()
	{
		List<InlineRun> runs = [new("ab cd", MarkdownFontRole.Body, null, false)];
		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap(runs, 100f, Measure);
		Assert.AreEqual(0f, lines[0].Tokens[0].X);
		Assert.AreEqual(3f, lines[0].Tokens[1].X);
	}

	[TestMethod]
	public void Wrap_ImageRun_IsSingleUnsplitToken()
	{
		List<InlineRun> runs = [new("logo.png", MarkdownFontRole.Body, null, true)];
		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap(runs, 3f, Measure);
		Assert.AreEqual(1, lines[0].Tokens.Count);
		Assert.AreEqual("logo.png", lines[0].Tokens[0].Text);
		Assert.IsTrue(lines[0].Tokens[0].IsImage);
	}

	[TestMethod]
	public void Wrap_EmptyRuns_ProducesNoLines()
	{
		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap([], 100f, Measure);
		Assert.AreEqual(0, lines.Count);
	}

	[TestMethod]
	public void Wrap_LineHeight_TracksTallestToken()
	{
		static Vector2 MeasureWithTallImage(string text, MarkdownFontRole role, bool isImage) => new(text.Length, isImage ? 10f : 1f);

		List<InlineRun> runs =
		[
			new("hi", MarkdownFontRole.Body, null, false),
			new("logo.png", MarkdownFontRole.Body, null, true),
		];
		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap(runs, 100f, MeasureWithTallImage);
		Assert.AreEqual(1, lines.Count);
		Assert.AreEqual(10f, lines[0].Height);
	}
}
