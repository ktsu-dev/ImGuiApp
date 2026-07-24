// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Examples.Markdown;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App;
using ktsu.ImGui.Markdown;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

internal static class ImGuiMarkdownDemo
{
	private const string Sample = """
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

	private static void Main()
	{
		MarkdownConfig config = new()
		{
			FontResolver = ResolveFont,
			OnLinkClicked = null, // fall back to OS open for http/https/mailto
			ImageResolver = ResolveImage,
		};

		ImGuiApp.Start(new ImGuiAppConfig
		{
			Title = "ImGui.Markdown - Demo",
			OnRender = _ =>
			{
				ImGui.Begin("Markdown");
				ImGuiMarkdown.Render(Sample, config);
				ImGui.End();
			},
		});
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
	private static MarkdownImageResult? ResolveImage(string source)
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
