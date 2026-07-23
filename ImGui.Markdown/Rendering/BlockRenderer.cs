// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown;

using System.Numerics;
using System.Text;

using Hexa.NET.ImGui;

using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

/// <summary>Walks block-level AST nodes and issues ImGui draw calls for each.</summary>
internal static class BlockRenderer
{
	/// <summary>Renders all blocks in a container.</summary>
	/// <param name="blocks">The container block (document or nested container).</param>
	/// <param name="config">The active config.</param>
	public static void Render(ContainerBlock blocks, MarkdownConfig config)
	{
		foreach (Block block in blocks)
		{
			RenderBlock(block, config);
		}
	}

	private static void RenderBlock(Block block, MarkdownConfig config)
	{
		switch (block)
		{
			case HeadingBlock heading:
				RenderHeading(heading, config);
				break;

			case ParagraphBlock paragraph:
				InlineRenderer.Render(paragraph.Inline, config);
				ImGui.Dummy(new Vector2(0.0f, config.ParagraphSpacingPixels));
				break;

			case ListBlock list:
				RenderList(list, config);
				break;

			case QuoteBlock quote:
				RenderQuote(quote, config);
				break;

			case Markdig.Syntax.CodeBlock code:
				RenderCodeBlock(code, config);
				break;

			case ThematicBreakBlock:
				RenderThematicBreak();
				break;

			case Markdig.Extensions.Tables.Table table:
				RenderTable(table, config);
				break;

			case HtmlBlock html:
				RenderHtmlAsText(html, config);
				break;

			case ContainerBlock container:
				Render(container, config);
				break;

			default:
				break;
		}
	}

	private static void RenderHeading(HeadingBlock heading, MarkdownConfig config)
	{
		float body = ImGui.GetFontSize();
		float size = MarkdownSizing.HeadingPixelSize(body, heading.Level, config.HeadingScales);
		using (new ScopedMarkdownFont(MarkdownSizing.HeadingRole(heading.Level), size, config))
		{
			InlineRenderer.Render(heading.Inline, config);
		}

		ImGui.Dummy(new Vector2(0.0f, config.ParagraphSpacingPixels));
	}

	private static void RenderList(ListBlock list, MarkdownConfig config)
	{
		int index = 0;
		int start = 1;
		if (list.IsOrdered && int.TryParse(list.OrderedStart, out int parsed))
		{
			start = parsed;
		}

		foreach (Block item in list)
		{
			if (item is ListItemBlock listItem)
			{
				bool? taskChecked = TryGetTaskState(listItem);
				string marker = ListMarker.For(list.IsOrdered, index, start, taskChecked);

				ImGui.TextUnformatted(marker);
				ImGui.SameLine();
				ImGui.Indent(config.ListIndentPixels);
				Render(listItem, config);
				ImGui.Unindent(config.ListIndentPixels);
				index++;
			}
		}
	}

	private static bool? TryGetTaskState(ListItemBlock listItem)
	{
		foreach (Block child in listItem)
		{
			if (child is ParagraphBlock paragraph && paragraph.Inline is not null)
			{
				Inline? inline = paragraph.Inline.FirstChild;
				while (inline is not null)
				{
					if (inline is TaskList task)
					{
						return task.Checked;
					}

					inline = inline.NextSibling;
				}
			}
		}

		return null;
	}

	private static void RenderQuote(QuoteBlock quote, MarkdownConfig config)
	{
		Vector2 start = ImGui.GetCursorScreenPos();
		ImGui.Indent(config.ListIndentPixels);
		Render(quote, config);
		ImGui.Unindent(config.ListIndentPixels);

		Vector2 end = ImGui.GetCursorScreenPos();
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		float barX = start.X + (config.ListIndentPixels * 0.35f);
		drawList.AddLine(new Vector2(barX, start.Y), new Vector2(barX, end.Y), MarkdownColors.BlockquoteBar(), 2.0f);
	}

	private static void RenderCodeBlock(Markdig.Syntax.CodeBlock code, MarkdownConfig config)
	{
		string text = ExtractCodeText(code);
		float size = ImGui.GetFontSize();
		Vector2 start = ImGui.GetCursorScreenPos();
		Vector2 avail = ImGui.GetContentRegionAvail();

		using (new ScopedMarkdownFont(MarkdownFontRole.Code, size, config))
		{
			Vector2 textSize = ImGui.CalcTextSize(text);
			float height = textSize.Y + 8.0f;
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			drawList.AddRectFilled(start, start + new Vector2(avail.X, height), MarkdownColors.InlineCodeBackground(), 3.0f);
			ImGui.Dummy(new Vector2(0.0f, 4.0f));
			ImGui.Indent(4.0f);
			ImGui.TextUnformatted(text);
			ImGui.Unindent(4.0f);
			ImGui.Dummy(new Vector2(0.0f, 4.0f));
		}

		ImGui.Dummy(new Vector2(0.0f, config.ParagraphSpacingPixels));
	}

	// LeafBlock (not CodeBlock specifically) because HtmlBlock shares the same Lines field and
	// is rendered as plain text via this same extraction path.
	private static string ExtractCodeText(LeafBlock block)
	{
		StringBuilder builder = new();
		Markdig.Helpers.StringLineGroup lines = block.Lines;
		for (int i = 0; i < lines.Count; i++)
		{
			builder.AppendLine(lines.Lines[i].ToString());
		}

		return builder.ToString().TrimEnd('\n', '\r');
	}

	private static void RenderThematicBreak()
	{
		Vector2 start = ImGui.GetCursorScreenPos();
		float width = ImGui.GetContentRegionAvail().X;
		float y = start.Y + 4.0f;
		ImGui.GetWindowDrawList().AddLine(new Vector2(start.X, y), new Vector2(start.X + width, y), MarkdownColors.Separator(), 1.0f);
		ImGui.Dummy(new Vector2(width, 9.0f));
	}

	private static void RenderTable(Markdig.Extensions.Tables.Table table, MarkdownConfig config)
	{
		// v1: render each row's cells as inline content separated by a tab-like spacing.
		// A full column-aligned table can replace this later without touching callers.
		foreach (Block rowBlock in table)
		{
			if (rowBlock is Markdig.Extensions.Tables.TableRow row)
			{
				bool first = true;
				foreach (Block cellBlock in row)
				{
					if (cellBlock is Markdig.Extensions.Tables.TableCell cell)
					{
						if (!first)
						{
							ImGui.SameLine();
							ImGui.TextUnformatted("  |  ");
							ImGui.SameLine();
						}

						first = false;
						foreach (Block content in cell)
						{
							if (content is ParagraphBlock paragraph)
							{
								InlineRenderer.Render(paragraph.Inline, config);
							}
						}
					}
				}
			}
		}

		ImGui.Dummy(new Vector2(0.0f, config.ParagraphSpacingPixels));
	}

	private static void RenderHtmlAsText(HtmlBlock html, MarkdownConfig config)
	{
		string text = ExtractCodeText(html);
		using (new ScopedMarkdownFont(MarkdownFontRole.Code, ImGui.GetFontSize(), config))
		{
			ImGui.TextUnformatted(text);
		}
	}
}
