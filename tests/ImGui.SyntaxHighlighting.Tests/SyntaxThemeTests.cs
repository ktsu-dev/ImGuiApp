// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.SyntaxHighlighting.Tests;

using ktsu.Semantics.Color;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class SyntaxThemeTests
{
	[TestMethod]
	public void ABrightBackgroundSelectsTheLightPalette()
	{
		Assert.AreEqual(SyntaxTheme.Light, SyntaxTheme.ForBackgroundLuminance(0.9));
		Assert.AreEqual(SyntaxTheme.Dark, SyntaxTheme.ForBackgroundLuminance(0.05));
	}

	[TestMethod]
	public void EveryClassifiedKindHasAColor()
	{
		foreach (TokenKind kind in System.Enum.GetValues<TokenKind>())
		{
			if (kind == TokenKind.Plain)
			{
				continue;
			}

			Assert.IsNotNull(SyntaxTheme.Dark.ColorFor(kind), $"{kind} has no color.");
			Assert.IsNotNull(SyntaxTheme.Light.ColorFor(kind), $"{kind} has no color.");
		}
	}

	[TestMethod]
	public void PlainIsUnsetByDefaultSoTheImGuiThemeSuppliesIt()
	{
		Assert.IsNull(SyntaxTheme.Dark.ColorFor(TokenKind.Plain));
		Assert.IsNull(SyntaxTheme.Dark.Background);
		Assert.IsNull(SyntaxTheme.Dark.LineNumber);
	}

	[TestMethod]
	public void AThemeCanBeDerivedWithASingleOverride()
	{
		SyntaxTheme theme = SyntaxTheme.Dark with { Comment = Color.FromHex("#ff00ff") };

		Assert.AreEqual(Color.FromHex("#ff00ff"), theme.ColorFor(TokenKind.Comment));
		Assert.AreEqual(SyntaxTheme.Dark.Keyword, theme.Keyword);
	}
}
