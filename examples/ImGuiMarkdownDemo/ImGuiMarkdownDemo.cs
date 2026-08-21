// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Markdown;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.Markdown;
using ktsu.ImGui.Probes;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

internal static class ImGuiMarkdownDemo
{
	/// <summary>The markdown exercised by the demo. Internal so a UI test can assert against the
	/// same source the demo renders rather than a copy that could drift from it.</summary>
	internal const string Sample = """
		# ImGui.Markdown

		A **CommonMark** renderer for *Dear ImGui*, with `inline code`, [links](https://github.com/ktsu-dev), and more.

		![logo](ktsu.png)

		## Lists

		- First item
		- Second item with **bold**
		  - Nested item
		- [x] Completed task
		- [ ] Pending task

		1. One
		2. Two
		3. Three

		## Quote

		> The best way to predict the future is to invent it.

		## Code

		```
		var greeting = "hello";
		Console.WriteLine(greeting);
		```

		## Table

		| Feature | Status |
		|---------|--------|
		| Headings | Yes |
		| Tables | Yes |

		---

		That's the tour.
		""";

	/// <summary>The name of the ImGui window the markdown is rendered into.</summary>
	internal const string WindowTitle = "Markdown";

	/// <summary>
	/// Builds the configuration the demo runs on. Extracted from <c>Main</c> so a UI test drives the
	/// real configuration rather than a parallel one written for testing.
	/// </summary>
	/// <returns>The application configuration.</returns>
	internal static ImGuiAppConfig BuildConfig() => new()
	{
		Title = "ImGui.Markdown - Demo",
		OnRender = _ => RenderMarkdownWindow(),
	};

	/// <summary>The markdown configuration the demo renders with.</summary>
	internal static MarkdownConfig BuildMarkdownConfig() => new()
	{
		FontResolver = ResolveFont,
		OnLinkClicked = null, // fall back to OS open for http/https/mailto
		ImageResolver = ResolveImage,
	};

	private static readonly MarkdownConfig Config = BuildMarkdownConfig();

	private static void Main() => ImGuiApp.Start(BuildConfig());

	private static void RenderMarkdownWindow()
	{
		// Without a starting size the window opens at ImGui's 32px default, which leaves ~16px of
		// content width. The renderer wraps to that width, the window auto-fits to the resulting sliver,
		// and the layout never recovers on later frames. A first-use size breaks that feedback loop.
		ImGui.SetNextWindowSize(new Vector2(760, 640), ImGuiCond.FirstUseEver);
		ImGui.Begin(WindowTitle);

		// Width has to be sampled before rendering: afterwards the cursor sits at the end of the
		// document and the remaining region no longer describes the span the document was laid out in.
		Vector2 contentMin = ImGui.GetCursorScreenPos();
		float contentWidth = ImGui.GetContentRegionAvail().X;
		ImGuiMarkdown.Render(Sample, Config);
		Vector2 contentMax = new(contentMin.X + contentWidth, ImGui.GetCursorScreenPos().Y);

		// The markdown renderer submits draw commands rather than named ImGui items, so nothing
		// inside it is addressable on its own. Recording the span the document occupied gives a
		// test a stable handle on "the rendered document" without reaching into the renderer.
		ImGuiProbes.MarkRegion("markdown", contentMin, contentMax);

		ImGui.End();
	}

	// Map markdown roles to app fonts. Body/emphasis reuse the default font (faux bold applies);
	// headings request the default font at the target pixel size so DPI + GlobalScale are honored.
	// Returning null lets the renderer keep the current font at pixelSize with faux styling.
	// This repo has no bundled bold/italic font assets; a real app would register named font
	// variants via ImGuiAppConfig.Fonts and resolve them here per role, e.g. mapping
	// MarkdownFontRole.Bold/Italic/BoldItalic to distinct ImFontPtr instances loaded at startup.
	private static ImFontPtr? ResolveFont(MarkdownFontRole role, float pixelSize) => null;

	// Resolves image sources referenced from markdown (e.g. "![logo](ktsu.png)") to a loaded
	// GPU texture via ImGuiApp's texture cache, demonstrating the real ImageResolver extension
	// point rather than falling back to a placeholder box.
	internal static MarkdownImageResult? ResolveImage(string source)
	{
		AbsoluteFilePath imagePath = AppContext.BaseDirectory.As<AbsoluteDirectoryPath>() / source.As<FileName>();
		if (!File.Exists(imagePath))
		{
			return null;
		}

		ImGuiAppTextureInfo texture = ImGuiApp.GetOrLoadTexture(imagePath);
		return new MarkdownImageResult(texture.TextureId, new Vector2(64, 64));
	}
}
