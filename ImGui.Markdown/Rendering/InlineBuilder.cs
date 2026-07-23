// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown;

using System.Collections.Generic;

using Markdig.Syntax.Inlines;

/// <summary>Flattens a Markdig inline tree into a flat list of styled runs for layout.</summary>
internal static class InlineBuilder
{
	/// <summary>Builds the run list for an inline container.</summary>
	/// <param name="container">The inline container, or <see langword="null"/>.</param>
	/// <returns>The flattened styled runs.</returns>
	public static IReadOnlyList<InlineRun> Build(ContainerInline? container)
	{
		List<InlineRun> runs = [];
		if (container is not null)
		{
			Walk(container, runs, bold: false, italic: false, linkUrl: null);
		}

		return runs;
	}

	private static void Walk(ContainerInline container, List<InlineRun> runs, bool bold, bool italic, string? linkUrl)
	{
		Inline? child = container.FirstChild;
		while (child is not null)
		{
			Append(child, runs, bold, italic, linkUrl);
			child = child.NextSibling;
		}
	}

	private static void Append(Inline inline, List<InlineRun> runs, bool bold, bool italic, string? linkUrl)
	{
		switch (inline)
		{
			case LiteralInline literal:
				runs.Add(new InlineRun(literal.Content.ToString(), MarkdownSizing.EmphasisRole(bold, italic), linkUrl, IsImage: false));
				break;

			case CodeInline code:
				runs.Add(new InlineRun(code.Content, MarkdownFontRole.Code, linkUrl, IsImage: false));
				break;

			case EmphasisInline emphasis:
				bool nowBold = bold || emphasis.DelimiterCount >= 2;
				bool nowItalic = italic || emphasis.DelimiterCount == 1;
				Walk(emphasis, runs, nowBold, nowItalic, linkUrl);
				break;

			case LinkInline link when link.IsImage:
				runs.Add(new InlineRun(link.Url ?? string.Empty, MarkdownFontRole.Body, linkUrl, IsImage: true));
				break;

			case LinkInline link:
				Walk(link, runs, bold, italic, link.Url ?? linkUrl);
				break;

			case AutolinkInline autolink:
				runs.Add(new InlineRun(autolink.Url, MarkdownSizing.EmphasisRole(bold, italic), autolink.Url, IsImage: false));
				break;

			case LineBreakInline:
				runs.Add(new InlineRun(" ", MarkdownFontRole.Body, linkUrl, IsImage: false));
				break;

			case HtmlInline html:
				runs.Add(new InlineRun(html.Tag, MarkdownSizing.EmphasisRole(bold, italic), linkUrl, IsImage: false));
				break;

			case ContainerInline nested:
				Walk(nested, runs, bold, italic, linkUrl);
				break;

			default:
				// Unknown inline: emit its text form so nothing is silently dropped.
				runs.Add(new InlineRun(inline.ToString() ?? string.Empty, MarkdownSizing.EmphasisRole(bold, italic), linkUrl, IsImage: false));
				break;
		}
	}
}
