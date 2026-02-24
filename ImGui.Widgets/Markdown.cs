// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets;

/// <summary>
/// Options for customizing the appearance and behavior of the markdown widget.
/// </summary>
public record MarkdownOptions
{
	/// <summary>
	/// Gets or sets the maximum width for text wrapping. If zero or negative, uses available content width.
	/// </summary>
	public float WrapWidth { get; init; }
}

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>
	/// Renders formatted text from a markdown string.
	/// </summary>
	/// <param name="markdown">The markdown text to render.</param>
	public static void Markdown(string markdown) => MarkdownRenderer.Render(markdown, new MarkdownOptions());

	/// <summary>
	/// Renders formatted text from a markdown string with the specified options.
	/// </summary>
	/// <param name="markdown">The markdown text to render.</param>
	/// <param name="options">Options for customizing the rendering.</param>
	public static void Markdown(string markdown, MarkdownOptions options) => MarkdownRenderer.Render(markdown, options);
}
