// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown;

using MarkdigAst = Markdig.Syntax.MarkdownDocument;

/// <summary>
/// Renders CommonMark markdown inside Dear ImGui. The static <see cref="Render(string, MarkdownConfig?)"/>
/// caches parses by source string; for hot paths, construct a <see cref="MarkdownDocument"/> once and
/// render it each frame.
/// </summary>
public static partial class ImGuiMarkdown
{
	private static readonly MarkdownConfig DefaultConfig = new();

	/// <summary>Parses (cached) and renders markdown at the current cursor position.</summary>
	/// <param name="markdown">The markdown source.</param>
	/// <param name="config">Optional rendering config; defaults are used when omitted.</param>
	public static void Render(string markdown, MarkdownConfig? config = null)
	{
		if (string.IsNullOrEmpty(markdown))
		{
			return;
		}

		MarkdigAst ast = MarkdownParser.GetOrParse(markdown);
		BlockRenderer.Render(ast, config ?? DefaultConfig);
	}

	/// <summary>Renders a pre-parsed document at the current cursor position.</summary>
	/// <param name="document">The parsed document.</param>
	/// <param name="config">Optional rendering config; defaults are used when omitted.</param>
	public static void Render(MarkdownDocument document, MarkdownConfig? config = null)
	{
		Ensure.NotNull(document);
		BlockRenderer.Render(document.Ast, config ?? DefaultConfig);
	}
}
