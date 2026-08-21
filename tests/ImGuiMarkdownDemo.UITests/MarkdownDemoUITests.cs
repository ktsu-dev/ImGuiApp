// Copyright (c) 2023-2026 ktsu-dev contributors

// ImGui contexts are global and the harness refuses to start while another is live, so every test
// in this assembly must have the process to itself.
[assembly: Microsoft.VisualStudio.TestTools.UnitTesting.DoNotParallelize]

namespace ktsu.examples.ImGuiMarkdownDemo.UITests;

using ktsu.ImGui.App;
using ktsu.ImGui.App.Testing;
using ktsu.ImGui.Examples.Markdown;
using ktsu.ImGui.Markdown;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives ImGuiMarkdownDemo through the headless harness. The demo has no interactive controls, so
/// coverage is about what it renders: that the document is drawn, that every construct in the
/// sample survives a frame, and that the image extension point resolves against real files.
/// </summary>
[TestClass]
public sealed class MarkdownDemoUITests
{
	private static ImGuiAppHarness StartDemo(HarnessOptions? options = null) =>
		ImGuiAppHarness.Start(ImGuiMarkdownDemo.BuildConfig(), options ?? new HarnessOptions());

	[TestMethod]
	public void Config_UsesTheDemoTitleAndRenderCallback()
	{
		ImGuiAppConfig config = ImGuiMarkdownDemo.BuildConfig();

		Assert.AreEqual("ImGui.Markdown - Demo", config.Title);
		Assert.IsNotNull(config.OnRender, "The demo must render something.");
	}

	[TestMethod]
	public void Demo_RendersItsDocument()
	{
		using ImGuiAppHarness harness = StartDemo();
		harness.Step(3);

		Rectangle? document = harness.Probe.Rect("markdown");
		Assert.IsNotNull(document, "The demo never rendered its markdown document.");
		Assert.IsGreaterThan(document.Value.MinY, document.Value.MaxY, "The document occupied no vertical space.");
	}

	[TestMethod]
	public void Demo_DrawsVisiblePixels()
	{
		using ImGuiAppHarness harness = StartDemo();
		harness.Step(3);

		CapturedFrame frame = harness.Capture();
		Assert.IsNotNull(frame.FindBounds(p => p.A > 0), "The frame was entirely blank.");
	}

	[TestMethod]
	public void Demo_RendersEveryConstructInTheSample()
	{
		// Guards the sample itself: these are the constructs the demo exists to show off, and a
		// silent edit that dropped one would otherwise still pass every rendering assertion.
		string sample = ImGuiMarkdownDemo.Sample;

		Assert.Contains("# ImGui.Markdown", sample, "heading");
		Assert.Contains("**CommonMark**", sample, "bold");
		Assert.Contains("*Dear ImGui*", sample, "italic");
		Assert.Contains("`inline code`", sample, "inline code");
		Assert.Contains("[links](https://github.com/ktsu-dev)", sample, "link");
		Assert.Contains("![logo](ktsu.png)", sample, "image");
		Assert.Contains("- [x] Completed task", sample, "task list");
		Assert.Contains("1. One", sample, "ordered list");
		Assert.Contains("> The best way", sample, "block quote");
		Assert.Contains("| Feature | Status |", sample, "table");
		Assert.Contains("---", sample, "thematic break");
	}

	[TestMethod]
	public void Demo_SurvivesSustainedRendering()
	{
		// The renderer caches parsed documents by source. Stepping well past the first frame proves
		// the cached path is re-entered cleanly rather than only working on the parse frame.
		using ImGuiAppHarness harness = StartDemo();
		harness.Step(30);

		Assert.AreEqual(30, harness.FrameCount);
		Assert.IsNotNull(harness.Probe.Rect("markdown"), "The document stopped rendering.");
	}

	[TestMethod]
	public void ImageResolver_ResolvesAFileThatShipsBesideTheDemo()
	{
		// GetOrLoadTexture needs a live session, so this runs inside the harness rather than as a
		// plain unit test.
		using ImGuiAppHarness harness = StartDemo();
		harness.Step();

		MarkdownImageResult? resolved = ImGuiMarkdownDemo.ResolveImage("ktsu.png");

		Assert.IsNotNull(resolved, "ktsu.png ships beside the demo and should resolve to a texture.");
		Assert.AreEqual(64, resolved.Value.Size.X);
		Assert.AreEqual(64, resolved.Value.Size.Y);
	}

	[TestMethod]
	public void ImageResolver_ReturnsNullForAFileThatIsNotThere()
	{
		using ImGuiAppHarness harness = StartDemo();
		harness.Step();

		Assert.IsNull(ImGuiMarkdownDemo.ResolveImage("no-such-image.png"));
	}

	[TestMethod]
	public void Demo_RendersIdenticallyAcrossRuns()
	{
		byte[] first = CaptureOnce();
		byte[] second = CaptureOnce();

		CollectionAssert.AreEqual(first, second, "Two runs of the same scenario should be byte-identical.");

		static byte[] CaptureOnce()
		{
			using ImGuiAppHarness harness = StartDemo();
			harness.Step(5);
			return harness.Target.Pixels.ToArray();
		}
	}

	[TestMethod]
	public void Demo_LaysTheDocumentOutInsideTheDisplay()
	{
		// The demo draws a floating ImGui window, so the document tracks the window rather than the
		// display. What is worth pinning is that it lands on screen with a real extent: a document
		// laid out at zero width, or off the viewport, still satisfies "something was drawn".
		HarnessOptions options = new() { Width = 1280, Height = 720 };
		using ImGuiAppHarness harness = StartDemo(options);
		harness.Step(3);

		Rectangle? measured = harness.Probe.Rect("markdown");
		Assert.IsNotNull(measured, "No document rendered.");
		Rectangle document = measured.Value;

		Assert.IsGreaterThan(100, document.MaxX - document.MinX, "The document was laid out unreasonably narrow.");
		Assert.IsGreaterThan(100, document.MaxY - document.MinY, "A multi-block document should be taller than this.");
		Assert.IsGreaterThanOrEqualTo(0, document.MinX, "The document started left of the display.");
		Assert.IsGreaterThanOrEqualTo(0, document.MinY, "The document started above the display.");
		Assert.IsLessThanOrEqualTo(options.Width, document.MinX, "The document started right of the display.");
	}
}
