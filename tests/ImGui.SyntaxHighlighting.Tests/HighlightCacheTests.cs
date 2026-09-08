// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.SyntaxHighlighting.Tests;

using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class HighlightCacheTests
{
	[TestMethod]
	public void TheSameSourceAndLanguageReturnTheSameLines()
	{
		LanguageDefinition csharp = LanguageRegistry.Resolve("csharp");

		IReadOnlyList<HighlightedLine> first = HighlightCache.GetOrTokenize("int x = 1;", csharp, 4);
		IReadOnlyList<HighlightedLine> second = HighlightCache.GetOrTokenize("int x = 1;", csharp, 4);

		Assert.AreSame(first, second, "The cache re-tokenized identical source.");
	}

	[TestMethod]
	public void TabWidthIsPartOfTheCacheKey()
	{
		LanguageDefinition text = LanguageRegistry.Resolve("text");

		IReadOnlyList<HighlightedLine> wide = HighlightCache.GetOrTokenize("\tx", text, 8);
		IReadOnlyList<HighlightedLine> narrow = HighlightCache.GetOrTokenize("\tx", text, 2);

		Assert.AreEqual("        x", wide[0].ToText());
		Assert.AreEqual("  x", narrow[0].ToText());
	}

	[TestMethod]
	public void LanguageIsPartOfTheCacheKey()
	{
		IReadOnlyList<HighlightedLine> asCode = HighlightCache.GetOrTokenize("class x", LanguageRegistry.Resolve("csharp"), 4);
		IReadOnlyList<HighlightedLine> asText = HighlightCache.GetOrTokenize("class x", LanguageRegistry.Resolve("text"), 4);

		Assert.AreEqual(TokenKind.Keyword, asCode[0].Tokens[0].Kind);
		Assert.AreEqual(TokenKind.Plain, asText[0].Tokens[0].Kind);
	}

	[TestMethod]
	public void TheCacheEvictsRatherThanGrowingWithoutBound()
	{
		LanguageDefinition text = LanguageRegistry.Resolve("text");
		IReadOnlyList<HighlightedLine> first = HighlightCache.GetOrTokenize("eviction probe", text, 4);

		for (int index = 0; index < 64; index++)
		{
			HighlightCache.GetOrTokenize($"filler {index}", text, 4);
		}

		Assert.AreNotSame(first, HighlightCache.GetOrTokenize("eviction probe", text, 4));
	}

	[TestMethod]
	public void HighlightedCodeTokenizesOnceUpFront()
	{
		HighlightedCode code = new("int x = 1;", "csharp");

		Assert.AreEqual("csharp", code.Language.Name);
		Assert.AreEqual(1, code.Lines.Count);
		TokenAssert.HasToken(code.Lines, "int", TokenKind.Type);
	}
}
