// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown;

using MarkdigAst = Markdig.Syntax.MarkdownDocument;

/// <summary>
/// A parsed markdown document. Construct once from a source string and render it each frame;
/// the parse happens only in the constructor. Use this for hot render paths where the source
/// is stable across frames.
/// </summary>
public sealed class MarkdownDocument
{
	/// <summary>The original markdown source.</summary>
	public string Source { get; }

	/// <summary>The parsed Markdig AST.</summary>
	internal MarkdigAst Ast { get; }

	/// <summary>Initializes a new instance parsed from the given markdown source.</summary>
	/// <param name="markdown">The markdown source to parse.</param>
	public MarkdownDocument(string markdown)
	{
		Source = markdown ?? string.Empty;
		Ast = MarkdownParser.Parse(Source);
	}

	/// <summary>Renders this document at the current cursor position.</summary>
	/// <param name="config">Optional rendering config; defaults are used when omitted.</param>
	public void Render(MarkdownConfig? config = null) => ImGuiMarkdown.Render(this, config);
}