// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.examples.ImGuiSyntaxHighlightingDemo.UITests;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.SyntaxHighlighting;
using ktsu.Semantics.Color;
using ktsu.SyntaxHighlighting;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Themed code dims inside <c>ImGui.BeginDisabled</c> like the widgets around it, rather than keeping its
/// token colors at full strength.
/// </summary>
[TestClass]
public sealed class DisabledCodeTests
{
	private static readonly Color Red = Color.FromBytes(255, 0, 0, 255);

	// Every token kind in one color, so any glyph the code draws is that color wherever it is fully covered.
	private static readonly SyntaxTheme AllRed = new()
	{
		Plain = Red,
		Comment = Red,
		DocComment = Red,
		Keyword = Red,
		ControlKeyword = Red,
		Type = Red,
		Function = Red,
		Number = Red,
		StringLiteral = Red,
		Constant = Red,
		Operator = Red,
		Punctuation = Red,
		Preprocessor = Red,
		Tag = Red,
		AttributeName = Red,
		Property = Red,
	};

	private const string Code = "public static class Example { int value = 42; }";

	private ImGuiAppHarness? harness;

	[TestCleanup]
	public void TearDown() => harness?.Dispose();

	private int CountRedPixels(bool disabled, SyntaxTheme theme, bool showBackground)
	{
		harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig
			{
				Title = nameof(DisabledCodeTests),
				SaveIniSettings = false,
				OnRender = _ =>
				{
					ImGui.BeginDisabled(disabled);
					ImGuiSyntaxHighlighting.Render(Code, "csharp", new SyntaxHighlightConfig { Theme = theme, ShowBackground = showBackground, FontSizePixels = 32f });
					ImGui.EndDisabled();
				},
			},
			new HarnessOptions { Width = 1200, Height = 200 });
		harness.Step(3);

		Bitmap32 frame = harness.Target;
		int count = 0;
		for (int y = 0; y < frame.Height; y++)
		{
			for (int x = 0; x < frame.Width; x++)
			{
				Rgba32 pixel = frame.GetPixel(x, y);
				if (pixel.R > 220 && pixel.G < 40 && pixel.B < 40)
				{
					count++;
				}
			}
		}

		harness.Dispose();
		harness = null;
		return count;
	}

	[TestMethod]
	public void ThemedTokens_AreFullStrengthWhenEnabled() =>
		Assert.IsGreaterThan(0, CountRedPixels(disabled: false, AllRed, showBackground: false), "No glyph drew at the theme's full color.");

	[TestMethod]
	public void ThemedTokens_DimInsideADisabledScope() =>
		Assert.AreEqual(0, CountRedPixels(disabled: true, AllRed, showBackground: false), "Themed tokens kept their full color inside a disabled scope.");

	[TestMethod]
	public void ThemedBackground_DimsInsideADisabledScope()
	{
		SyntaxTheme redBackground = SyntaxTheme.Dark with { Background = Red };

		Assert.IsGreaterThan(1000, CountRedPixels(disabled: false, redBackground, showBackground: true), "The theme's background did not draw at full color.");
		Assert.AreEqual(0, CountRedPixels(disabled: true, redBackground, showBackground: true), "The theme's background kept its full color inside a disabled scope.");
	}
}
