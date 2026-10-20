// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Markdown;

using System.Collections.Generic;
using System.Text;

using Markdig.Syntax.Inlines;

/// <summary>Flattens a Markdig inline tree into a flat list of styled runs for layout.</summary>
internal static class InlineBuilder
{
	/// <summary>Builds the run list for an inline container.</summary>
	/// <param name="container">The inline container, or <see langword="null"/>.</param>
	/// <param name="baseRole">
	/// The role of the block the inlines sit in: <see cref="MarkdownFontRole.Bold"/> for a table header
	/// row, a heading role for a heading. Plain text takes it, and emphasis combines with it.
	/// </param>
	/// <returns>The flattened styled runs.</returns>
	public static IReadOnlyList<InlineRun> Build(ContainerInline? container, MarkdownFontRole baseRole = MarkdownFontRole.Body)
	{
		List<InlineRun> runs = [];
		if (container is not null)
		{
			Walk(container, new Style(baseRole, Bold: false, Italic: false, LinkUrl: null), runs);
		}

		return runs;
	}

	private static void Walk(ContainerInline container, Style style, List<InlineRun> runs)
	{
		Inline? child = container.FirstChild;
		while (child is not null)
		{
			Append(child, style, runs);
			child = child.NextSibling;
		}
	}

	private static void Append(Inline inline, Style style, List<InlineRun> runs)
	{
		switch (inline)
		{
			case LiteralInline literal:
				runs.Add(new InlineRun(literal.Content.ToString(), style.Role, style.LinkUrl, IsImage: false));
				break;

			case CodeInline code:
				runs.Add(new InlineRun(code.Content, MarkdownFontRole.Code, style.LinkUrl, IsImage: false));
				break;

			case EmphasisInline emphasis:
				Walk(emphasis, style with { Bold = style.Bold || emphasis.DelimiterCount >= 2, Italic = style.Italic || emphasis.DelimiterCount == 1 }, runs);
				break;

			case LinkInline link when link.IsImage:
				// Text stays the src, which is what an ImageResolver is keyed on; the alt text is what a
				// placeholder shows when nothing resolves it.
				runs.Add(new InlineRun(link.Url ?? string.Empty, style.BaseRole, style.LinkUrl, IsImage: true, AltText: PlainText(link)));
				break;

			case LinkInline link:
				Walk(link, style with { LinkUrl = link.Url ?? style.LinkUrl }, runs);
				break;

			case AutolinkInline autolink:
				runs.Add(new InlineRun(autolink.Url, style.Role, autolink.Url, IsImage: false));
				break;

			case LineBreakInline { IsHard: true }:
				runs.Add(new InlineRun(string.Empty, style.BaseRole, style.LinkUrl, IsImage: false, IsHardBreak: true));
				break;

			case LineBreakInline:
				runs.Add(new InlineRun(" ", style.BaseRole, style.LinkUrl, IsImage: false));
				break;

			case HtmlEntityInline entity:
				runs.Add(new InlineRun(entity.Transcoded.ToString(), style.Role, style.LinkUrl, IsImage: false));
				break;

			case HtmlInline html:
				runs.Add(new InlineRun(html.Tag, style.Role, style.LinkUrl, IsImage: false));
				break;

			case Markdig.Extensions.TaskLists.TaskList:
				// The checkbox marker is rendered by ListMarker in BlockRenderer; drop the inline itself.
				break;

			case ContainerInline nested:
				Walk(nested, style, runs);
				break;

			default:
				// Unknown leaf inline: drop it. Markdig inlines don't override ToString(), so
				// emitting it would draw the CLR type name rather than any of the document's text.
				break;
		}
	}

	/// <summary>The text an image's description reads as, with its emphasis and links dropped.</summary>
	private static string PlainText(ContainerInline container)
	{
		StringBuilder builder = new();
		AppendPlainText(container, builder);
		return builder.ToString();
	}

	private static void AppendPlainText(ContainerInline container, StringBuilder builder)
	{
		for (Inline? child = container.FirstChild; child is not null; child = child.NextSibling)
		{
			switch (child)
			{
				case LiteralInline literal:
					builder.Append(literal.Content.ToString());
					break;

				case CodeInline code:
					builder.Append(code.Content);
					break;

				case HtmlEntityInline entity:
					builder.Append(entity.Transcoded.ToString());
					break;

				case LineBreakInline:
					builder.Append(' ');
					break;

				case ContainerInline nested:
					AppendPlainText(nested, builder);
					break;

				default:
					break;
			}
		}
	}

	/// <summary>The styling in force while walking: the block's role, emphasis, and enclosing link.</summary>
	private readonly record struct Style(MarkdownFontRole BaseRole, bool Bold, bool Italic, string? LinkUrl)
	{
		/// <summary>The role plain text takes under this style.</summary>
		public MarkdownFontRole Role => MarkdownSizing.CombineRole(BaseRole, Bold, Italic);
	}
}
