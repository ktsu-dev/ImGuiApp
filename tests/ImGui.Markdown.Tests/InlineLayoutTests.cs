// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Markdown.Tests;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

	[TestMethod]
	public void Wrap_HardBreak_EndsLineEvenWhenNextTokenFits()
	{
		List<InlineRun> runs =
		[
			new("line one", MarkdownFontRole.Body, null, false),
			new(string.Empty, MarkdownFontRole.Body, null, false, IsHardBreak: true),
			new("line two", MarkdownFontRole.Body, null, false),
		];
		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap(runs, 100f, Measure);
		Assert.AreEqual(2, lines.Count);
		Assert.AreEqual("one", lines[0].Tokens[^1].Text);
		Assert.AreEqual("line", lines[1].Tokens[0].Text);
		Assert.AreEqual(0f, lines[1].Tokens[0].X);
	}

	[TestMethod]
	public void Wrap_SoftBreak_StaysOnOneLineWhenItFits()
	{
		List<InlineRun> runs =
		[
			new("line one", MarkdownFontRole.Body, null, false),
			new(" ", MarkdownFontRole.Body, null, false),
			new("line two", MarkdownFontRole.Body, null, false),
		];
		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap(runs, 100f, Measure);
		Assert.AreEqual(1, lines.Count);
		Assert.AreEqual(4, lines[0].Tokens.Count);
	}

	[TestMethod]
	public void Wrap_HardBreakBetweenTouchingRuns_StillEndsTheLine()
	{
		// Nothing but the break separates "a" from "b", so without it they would glue into one word.
		List<InlineRun> runs =
		[
			new("a", MarkdownFontRole.Body, null, false),
			new(string.Empty, MarkdownFontRole.Body, null, false, IsHardBreak: true),
			new("b", MarkdownFontRole.Body, null, false),
		];
		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap(runs, 100f, Measure);
		Assert.AreEqual(2, lines.Count);
		Assert.AreEqual("a", TokenTexts(lines[0]));
		Assert.AreEqual("b", TokenTexts(lines[1]));
		Assert.AreEqual("0", TokenXs(lines[1]));
	}

	[TestMethod]
	public void Wrap_HardBreakAsLastRun_AddsNoEmptyLine()
	{
		List<InlineRun> runs =
		[
			new("line one", MarkdownFontRole.Body, null, false),
			new(string.Empty, MarkdownFontRole.Body, null, false, IsHardBreak: true),
		];
		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap(runs, 100f, Measure);
		Assert.AreEqual(1, lines.Count);
	}

	[TestMethod]
	public void Wrap_BoldFollowedByPunctuation_IsNotSeparatedBySpace()
	{
		// "**Note**: text" — the colon touches the bold run in the source.
		List<InlineRun> runs =
		[
			new("Note", MarkdownFontRole.Bold, null, false),
			new(": text", MarkdownFontRole.Body, null, false),
		];
		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap(runs, 100f, Measure);
		Assert.AreEqual(1, lines.Count);
		Assert.AreEqual("Note|:|text", TokenTexts(lines[0]));
		Assert.AreEqual("0 4 6", TokenXs(lines[0]));
		Assert.AreEqual(10f, lines[0].Width);
	}

	[TestMethod]
	public void Wrap_CodeFollowedByLetter_IsNotSeparatedBySpace()
	{
		// "`a`b"
		List<InlineRun> runs =
		[
			new("a", MarkdownFontRole.Code, null, false),
			new("b", MarkdownFontRole.Body, null, false),
		];
		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap(runs, 100f, Measure);
		Assert.AreEqual("0 1", TokenXs(lines[0]));
	}

	[TestMethod]
	public void Wrap_LinkFollowedByPeriod_IsNotSeparatedBySpace()
	{
		// "[l](u)."
		List<InlineRun> runs =
		[
			new("l", MarkdownFontRole.Body, "u", false),
			new(".", MarkdownFontRole.Body, null, false),
		];
		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap(runs, 100f, Measure);
		Assert.AreEqual("0 1", TokenXs(lines[0]));
		Assert.AreEqual("u", lines[0].Tokens[0].LinkUrl);
		Assert.IsNull(lines[0].Tokens[1].LinkUrl);
	}

	[TestMethod]
	public void Wrap_BoldInsideAWord_IsNotSeparatedBySpace()
	{
		// "a**b**c"
		List<InlineRun> runs =
		[
			new("a", MarkdownFontRole.Body, null, false),
			new("b", MarkdownFontRole.Bold, null, false),
			new("c", MarkdownFontRole.Body, null, false),
		];
		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap(runs, 100f, Measure);
		Assert.AreEqual("0 1 2", TokenXs(lines[0]));
		Assert.AreEqual(3f, lines[0].Width);
	}

	[TestMethod]
	public void Wrap_WhitespaceAtRunBoundary_StillSeparatesTokens()
	{
		// "plain **bold** plain": the spaces live at the ends of the plain runs.
		List<InlineRun> runs =
		[
			new("ab ", MarkdownFontRole.Body, null, false),
			new("cd", MarkdownFontRole.Bold, null, false),
			new(" ef", MarkdownFontRole.Body, null, false),
		];
		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap(runs, 100f, Measure);
		Assert.AreEqual("0 3 6", TokenXs(lines[0]));
	}

	[TestMethod]
	public void Wrap_NeverBreaksInsideAGluedSequence()
	{
		// "aaa [docs](u)." at a width that fits "aaa docs" but not "aaa docs.": the period must
		// not be stranded on its own line, so "docs." moves down together.
		List<InlineRun> runs =
		[
			new("aaa ", MarkdownFontRole.Body, null, false),
			new("docs", MarkdownFontRole.Body, "u", false),
			new(".", MarkdownFontRole.Body, null, false),
		];
		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap(runs, 8f, Measure);
		Assert.AreEqual(2, lines.Count);
		Assert.AreEqual("aaa", TokenTexts(lines[0]));
		Assert.AreEqual("docs|.", TokenTexts(lines[1]));
		Assert.AreEqual("0 4", TokenXs(lines[1]));
	}

	[TestMethod]
	public void Wrap_EmptyRunBetweenTouchingRuns_KeepsThemGlued()
	{
		List<InlineRun> runs =
		[
			new("a", MarkdownFontRole.Body, null, false),
			new(string.Empty, MarkdownFontRole.Bold, null, false),
			new("b", MarkdownFontRole.Body, null, false),
		];
		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap(runs, 100f, Measure);
		Assert.AreEqual("0 1", TokenXs(lines[0]));
	}

	private static string TokenTexts(LaidOutLine line) => string.Join('|', line.Tokens.Select(t => t.Text));

	private static string TokenXs(LaidOutLine line) =>
		string.Join(' ', line.Tokens.Select(t => t.X.ToString(CultureInfo.InvariantCulture)));
}
