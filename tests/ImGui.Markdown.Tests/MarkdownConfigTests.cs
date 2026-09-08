// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Markdown.Tests;

using ktsu.ImGui.Markdown;

[TestClass]
public sealed class MarkdownConfigTests
{
	[TestMethod]
	public void Defaults_HeadingScales_AreDescendingSixEntries()
	{
		MarkdownConfig config = new();
		Assert.AreEqual(6, config.HeadingScales.Count);
		Assert.AreEqual(2.0f, config.HeadingScales[0]);
		Assert.AreEqual(0.9f, config.HeadingScales[5]);
	}

	[TestMethod]
	public void Defaults_SpacingAndIndent_ArePositive()
	{
		MarkdownConfig config = new();
		Assert.IsTrue(config.ListIndentPixels > 0f);
		Assert.IsTrue(config.ParagraphSpacingPixels > 0f);
	}

	[TestMethod]
	public void Defaults_ResolversAreNull()
	{
		MarkdownConfig config = new();
		Assert.IsNull(config.FontResolver);
		Assert.IsNull(config.OnLinkClicked);
		Assert.IsNull(config.ImageResolver);
		Assert.IsNull(config.CodeBlockRenderer);
	}

	[TestMethod]
	public void CodeBlockRenderer_TakesTheLanguageAndTheCode()
	{
		// The hook's parameters are what an outside highlighter binds to, so their order and content
		// are part of the contract rather than an implementation detail.
		string? seenLanguage = null;
		string? seenCode = null;
		MarkdownConfig config = new()
		{
			CodeBlockRenderer = (language, code) =>
			{
				seenLanguage = language;
				seenCode = code;
			},
		};

		config.CodeBlockRenderer!("csharp", "int x = 1;");

		Assert.AreEqual("csharp", seenLanguage);
		Assert.AreEqual("int x = 1;", seenCode);
	}
}
