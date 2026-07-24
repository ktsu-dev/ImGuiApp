// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown.Tests;

using System.Collections.Generic;

using ktsu.ImGui.Markdown;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class MarkdownSizingTests
{
	private static readonly IReadOnlyList<float> Scales = MarkdownConfig.DefaultHeadingScales;

	[TestMethod]
	public void HeadingPixelSize_H1_DoublesBody()
	{
		Assert.AreEqual(28.0f, MarkdownSizing.HeadingPixelSize(14.0f, 1, Scales));
	}

	[TestMethod]
	public void HeadingPixelSize_ClampsOutOfRangeLevel()
	{
		Assert.AreEqual(14.0f * 2.0f, MarkdownSizing.HeadingPixelSize(14.0f, 0, Scales));
		Assert.AreEqual(14.0f * 0.9f, MarkdownSizing.HeadingPixelSize(14.0f, 9, Scales));
	}

	[TestMethod]
	public void HeadingRole_MapsLevelToRole()
	{
		Assert.AreEqual(MarkdownFontRole.H1, MarkdownSizing.HeadingRole(1));
		Assert.AreEqual(MarkdownFontRole.H6, MarkdownSizing.HeadingRole(6));
		Assert.AreEqual(MarkdownFontRole.H1, MarkdownSizing.HeadingRole(-3));
	}

	[TestMethod]
	public void EmphasisRole_CombinesBoldAndItalic()
	{
		Assert.AreEqual(MarkdownFontRole.Body, MarkdownSizing.EmphasisRole(false, false));
		Assert.AreEqual(MarkdownFontRole.Bold, MarkdownSizing.EmphasisRole(true, false));
		Assert.AreEqual(MarkdownFontRole.Italic, MarkdownSizing.EmphasisRole(false, true));
		Assert.AreEqual(MarkdownFontRole.BoldItalic, MarkdownSizing.EmphasisRole(true, true));
	}
}
