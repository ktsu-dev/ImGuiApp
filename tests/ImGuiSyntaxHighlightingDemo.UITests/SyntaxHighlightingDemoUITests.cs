// Copyright (c) 2023-2026 ktsu-dev contributors

// ImGui contexts are global and the harness refuses to start while another is live, so every test
// in this assembly must have the process to itself.
[assembly: Microsoft.VisualStudio.TestTools.UnitTesting.DoNotParallelize]

namespace ktsu.examples.ImGuiSyntaxHighlightingDemo.UITests;

using System.Collections.Generic;
using System.Linq;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.Examples.SyntaxHighlighting;
using ktsu.ImGui.Markdown;
using ktsu.ImGui.SyntaxHighlighting;
using ktsu.SyntaxHighlighting;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives ImGuiSyntaxHighlightingDemo through the headless harness: that the code block is drawn,
/// that the demo's own controls change what it draws, and that the markdown tab's hook renders.
/// </summary>
[TestClass]
public sealed class SyntaxHighlightingDemoUITests
{
	private static readonly HarnessOptions DemoViewport = new() { Width = 1280, Height = 900 };

	private ImGuiAppHarness harness = null!;

	[TestInitialize]
	public void SetUp()
	{
		ImGuiSyntaxHighlightingDemo.ResetState();
		harness = ImGuiAppHarness.Start(ImGuiSyntaxHighlightingDemo.BuildConfig(), DemoViewport);
		harness.Step(3);
	}

	[TestCleanup]
	public void TearDown() => harness?.Dispose();

	private bool IsVisible(string name) => harness.Probe.WasSeenInFrame(name, harness.FrameCount - 1);

	[TestMethod]
	public void Config_UsesTheDemoTitleAndRenderCallback()
	{
		ImGuiAppConfig config = ImGuiSyntaxHighlightingDemo.BuildConfig();

		Assert.AreEqual("ImGui.SyntaxHighlighting - Demo", config.Title);
		Assert.IsNotNull(config.OnRender, "The demo must render something.");
	}

	[TestMethod]
	public void Demo_RendersACodeBlock()
	{
		if (harness.Probe.Rect("code") is not Rectangle code)
		{
			Assert.Fail("The demo never rendered a code block.");
			return;
		}

		Assert.IsGreaterThan(code.MinY, code.MaxY, "The code block occupied no vertical space.");
	}

	[TestMethod]
	public void Demo_DrawsVisiblePixels()
	{
		CapturedFrame frame = harness.Capture();

		Assert.IsNotNull(frame.FindBounds(pixel => pixel.A > 0), "The frame was entirely blank.");
	}

	[TestMethod]
	public void SelectingALanguageSwitchesTheSnippet()
	{
		harness.Click("SQL");
		harness.Step(2);

		Assert.AreEqual("SQL", ImGuiSyntaxHighlightingDemo.Snippets[ImGuiSyntaxHighlightingDemo.SelectedSnippet].Label);
	}

	[TestMethod]
	public void TogglingLineNumbersChangesWhatIsDrawn()
	{
		byte[] withNumbers = harness.Target.Pixels.ToArray();

		harness.Click("Line numbers");
		harness.Step(2);

		Assert.IsFalse(ImGuiSyntaxHighlightingDemo.ShowLineNumbers, "The checkbox did not toggle.");
		CollectionAssert.AreNotEqual(withNumbers, harness.Target.Pixels.ToArray(), "Hiding the gutter changed nothing on screen.");
	}

	[TestMethod]
	public void ForcingTheLightPaletteChangesWhatIsDrawn()
	{
		byte[] beforeSwitch = harness.Target.Pixels.ToArray();

		harness.Click("Light");
		harness.Step(2);

		Assert.AreEqual(2, ImGuiSyntaxHighlightingDemo.SelectedPalette);
		CollectionAssert.AreNotEqual(beforeSwitch, harness.Target.Pixels.ToArray(), "Switching palette changed nothing on screen.");
	}

	[TestMethod]
	public void TheMarkdownTabRendersItsFencedBlocks()
	{
		harness.Click("In markdown");
		harness.Step(3);

		Assert.IsTrue(IsVisible("In markdown"), "The markdown tab was not on screen.");
		Assert.IsNotNull(harness.Capture().FindBounds(pixel => pixel.A > 0), "The markdown tab drew nothing.");
	}

	[TestMethod]
	public void TheMarkdownHookRoutesFencesThroughTheHighlighter()
	{
		// The hook is what makes the markdown tab worth having, so it is asserted directly rather than
		// only through the pixels it produces: the config supplies one, and the language it forwards
		// reaches a highlighter that actually classifies the fence's contents.
		MarkdownConfig config = ImGuiSyntaxHighlightingDemo.BuildMarkdownConfig();

		Assert.IsNotNull(config.CodeBlockRenderer, "The demo stopped routing code blocks through the highlighter.");
		Assert.Contains("```csharp", ImGuiSyntaxHighlightingDemo.MarkdownSample, "The sample no longer has a language-tagged fence.");

		IReadOnlyList<HighlightedLine> highlighted = ImGuiSyntaxHighlighting.Highlight("public static void Render()", "csharp");
		Assert.AreEqual(TokenKind.Keyword, highlighted[0].Tokens[0].Kind, "The fence's language reached an unstyled highlighter.");
	}

	[TestMethod]
	public void TheEmbeddedTabDrawsEverySnippet()
	{
		harness.Click("Embedded");
		harness.Step(3);

		Assert.IsTrue(IsVisible("Embedded"), "The embedded-language tab was not on screen.");
		foreach (ImGuiSyntaxHighlightingDemo.Snippet snippet in ImGuiSyntaxHighlightingDemo.EmbeddedSnippets)
		{
			if (harness.Probe.Rect($"embedded/{snippet.Label}") is not Rectangle block)
			{
				Assert.Fail($"'{snippet.Label}' was never rendered.");
				return;
			}

			Assert.IsGreaterThan(block.MinY, block.MaxY, $"'{snippet.Label}' occupied no vertical space.");
		}
	}

	[TestMethod]
	public void TheEmbeddedSamplesActuallyCarryAnEmbeddedLanguage()
	{
		// The samples are the point of the tab, so a rewrite that dropped the embedded snippet would
		// still draw a code block and still pass every pixel assertion above.
		IReadOnlyList<HighlightedLine> lines =
			ImGuiSyntaxHighlighting.Highlight(ImGuiSyntaxHighlightingDemo.EmbeddedSnippets[0].Sample, "csharp");

		List<HighlightedToken> tokens = [.. lines.SelectMany(line => line.Tokens)];
		Assert.IsTrue(tokens.Any(token => token.Kind == TokenKind.Tag), "No doc comment tag was classified as XML.");
		Assert.IsTrue(tokens.Any(token => token.Kind == TokenKind.Property), "No JSON key was classified.");
	}

	[TestMethod]
	public void EverySnippetNamesALanguageTheHighlighterKnows()
	{
		// Guards the samples themselves: a typo in a language name would still render, silently
		// unstyled, and every pixel assertion above would keep passing.
		List<ImGuiSyntaxHighlightingDemo.Snippet> samples =
			[.. ImGuiSyntaxHighlightingDemo.Snippets, .. ImGuiSyntaxHighlightingDemo.EmbeddedSnippets];

		foreach (ImGuiSyntaxHighlightingDemo.Snippet snippet in samples)
		{
			Assert.IsTrue(
				LanguageRegistry.TryGet(snippet.Language, out LanguageDefinition _),
				$"'{snippet.Label}' names the unknown language '{snippet.Language}'.");
			Assert.IsGreaterThan(0, snippet.Sample.Length, $"'{snippet.Label}' has no sample.");
		}
	}

	[TestMethod]
	public void Demo_SurvivesSustainedRendering()
	{
		// Tokenization is cached by source text. Stepping well past the first frame proves the cached
		// path is re-entered cleanly rather than only working on the frame that tokenized.
		harness.Step(30);

		Assert.IsNotNull(harness.Probe.Rect("code"), "The code block stopped rendering.");
	}
}
