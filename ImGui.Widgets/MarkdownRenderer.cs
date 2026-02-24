// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets;

using System.Collections.Concurrent;
using System.Numerics;
using System.Text;

using Hexa.NET.ImGui;

using ktsu.ImGui.Styler;

using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

/// <summary>
/// Internal renderer that walks a Markdig AST and renders markdown using ImGui primitives.
/// </summary>
internal static class MarkdownRenderer
{
	private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
		.UseAdvancedExtensions()
		.Build();

	private static readonly ConcurrentDictionary<string, MarkdownDocument> ParseCache = new();

	/// <summary>
	/// Renders a markdown string using ImGui.
	/// </summary>
	/// <param name="markdown">The raw markdown text.</param>
	/// <param name="options">Rendering options.</param>
	public static void Render(string markdown, MarkdownOptions options)
	{
		Ensure.NotNull(markdown);
		Ensure.NotNull(options);

		MarkdownDocument document = ParseCache.GetOrAdd(markdown, key => Markdig.Markdown.Parse(key, Pipeline));

		float wrapWidth = options.WrapWidth > 0 ? options.WrapWidth : ImGui.GetContentRegionAvail().X;

		int blockIndex = 0;
		foreach (Block block in document)
		{
			ImGui.PushID(blockIndex++);
			RenderBlock(block, wrapWidth, 0);
			ImGui.PopID();
		}
	}

	private static void RenderBlock(Block block, float wrapWidth, int indentLevel)
	{
		switch (block)
		{
			case HeadingBlock heading:
				RenderHeading(heading, wrapWidth);
				break;
			case ParagraphBlock paragraph:
				RenderParagraph(paragraph, wrapWidth);
				break;
			case ListBlock list:
				RenderList(list, wrapWidth, indentLevel);
				break;
			case FencedCodeBlock fencedCode:
				RenderCodeBlock(fencedCode, wrapWidth);
				break;
			case CodeBlock code:
				RenderCodeBlock(code, wrapWidth);
				break;
			case QuoteBlock quote:
				RenderBlockquote(quote, wrapWidth);
				break;
			case ThematicBreakBlock:
				ImGui.Separator();
				break;
			case Table table:
				RenderTable(table, wrapWidth);
				break;
			case ContainerBlock container:
				foreach (Block child in container)
				{
					RenderBlock(child, wrapWidth, indentLevel);
				}
				break;
			default:
				break;
		}
	}

	private static void RenderParagraph(LeafBlock leaf, float wrapWidth)
	{
		if (leaf.Inline is not null)
		{
			RenderInlines(leaf.Inline, wrapWidth);
		}

		ImGui.NewLine();
	}

	private static void RenderInlines(ContainerInline container, float wrapWidth)
	{
		bool firstInline = true;
		RenderInlines(container, wrapWidth, ref firstInline);
	}

	private static void RenderInlines(ContainerInline container, float wrapWidth, ref bool firstInline)
	{
		foreach (Inline inline in container)
		{
			switch (inline)
			{
				case LiteralInline literal:
					RenderLiteralText(literal.Content.ToString(), wrapWidth, ref firstInline);
					break;
				case EmphasisInline emphasis:
					RenderEmphasis(emphasis, wrapWidth, ref firstInline);
					break;
				case CodeInline code:
					RenderInlineCode(code.Content, wrapWidth, ref firstInline);
					break;
				case LinkInline link:
					RenderLink(link, wrapWidth, ref firstInline);
					break;
				case LineBreakInline lineBreak:
					if (lineBreak.IsHard)
					{
						ImGui.NewLine();
					}
					else
					{
						RenderLiteralText(" ", wrapWidth, ref firstInline);
					}
					break;
				case ContainerInline childContainer:
					RenderInlines(childContainer, wrapWidth, ref firstInline);
					break;
				default:
					break;
			}
		}
	}

	private static void RenderLiteralText(string text, float wrapWidth, ref bool firstInline)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		if (!firstInline)
		{
			ImGui.SameLine(0, 0);
		}

		ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + wrapWidth);
		ImGui.TextUnformatted(text);
		ImGui.PopTextWrapPos();
		firstInline = false;
	}

	private static void RenderEmphasis(EmphasisInline emphasis, float wrapWidth, ref bool firstInline)
	{
		bool isBold = emphasis.DelimiterCount >= 2;
		bool isItalic = emphasis.DelimiterCount is 1 or >= 3;
		bool isStrikethrough = emphasis.DelimiterChar == '~';

		ImColor color;
		if (isStrikethrough)
		{
			color = Color.Palette.Neutral.Gray;
		}
		else if (isBold && isItalic)
		{
			color = Color.Palette.Semantic.Secondary;
		}
		else if (isBold)
		{
			color = Color.Palette.Semantic.Primary;
		}
		else
		{
			color = Color.Palette.Neutral.LightGray;
		}

		using (new ScopedColor(ImGuiCol.Text, color))
		{
			foreach (Inline child in emphasis)
			{
				switch (child)
				{
					case LiteralInline literal:
						if (isStrikethrough)
						{
							RenderStrikethroughText(literal.Content.ToString(), wrapWidth, ref firstInline);
						}
						else
						{
							RenderLiteralText(literal.Content.ToString(), wrapWidth, ref firstInline);
						}
						break;
					case EmphasisInline nestedEmphasis:
						RenderEmphasis(nestedEmphasis, wrapWidth, ref firstInline);
						break;
					case CodeInline code:
						RenderInlineCode(code.Content, wrapWidth, ref firstInline);
						break;
					case LinkInline link:
						RenderLink(link, wrapWidth, ref firstInline);
						break;
					default:
						break;
				}
			}
		}
	}

	private static void RenderStrikethroughText(string text, float wrapWidth, ref bool firstInline)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		if (!firstInline)
		{
			ImGui.SameLine(0, 0);
		}

		Vector2 startPos = ImGui.GetCursorScreenPos();
		ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + wrapWidth);
		ImGui.TextUnformatted(text);
		ImGui.PopTextWrapPos();

		Vector2 textSize = ImGui.CalcTextSize(text, false, wrapWidth);
		float strikeY = startPos.Y + (textSize.Y * 0.5f);
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		drawList.AddLine(
			new Vector2(startPos.X, strikeY),
			new Vector2(startPos.X + textSize.X, strikeY),
			ImGui.GetColorU32(ImGuiCol.Text),
			1.0f);

		firstInline = false;
	}

	private static void RenderInlineCode(string code, float wrapWidth, ref bool firstInline)
	{
		if (!firstInline)
		{
			ImGui.SameLine(0, 0);
		}

		Vector2 textSize = ImGui.CalcTextSize(code);
		Vector2 cursorPos = ImGui.GetCursorScreenPos();
		float padding = 2.0f;

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		Vector2 bgMin = new(cursorPos.X - padding, cursorPos.Y - padding);
		Vector2 bgMax = new(cursorPos.X + textSize.X + padding, cursorPos.Y + textSize.Y + padding);
		drawList.AddRectFilled(bgMin, bgMax, ImGui.GetColorU32(new Vector4(0.3f, 0.3f, 0.3f, 0.5f)), 3.0f);

		using (new ScopedColor(ImGuiCol.Text, Color.Palette.Semantic.Success))
		{
			ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + wrapWidth);
			ImGui.TextUnformatted(code);
			ImGui.PopTextWrapPos();
		}

		firstInline = false;
	}

	private static void RenderLink(LinkInline link, float wrapWidth, ref bool firstInline)
	{
		StringBuilder linkTextBuilder = new();
		foreach (Inline child in link)
		{
			if (child is LiteralInline literal)
			{
				linkTextBuilder.Append(literal.Content.ToString());
			}
		}

		string linkText = linkTextBuilder.Length > 0
			? linkTextBuilder.ToString()
			: link.Url ?? string.Empty;

		if (!firstInline)
		{
			ImGui.SameLine(0, 0);
		}

		Vector2 startPos = ImGui.GetCursorScreenPos();

		using (new ScopedColor(ImGuiCol.Text, Color.Palette.Semantic.Info))
		{
			ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + wrapWidth);
			ImGui.TextUnformatted(linkText);
			ImGui.PopTextWrapPos();
		}

		if (ImGui.IsItemHovered())
		{
			Vector2 textSize = ImGui.CalcTextSize(linkText);
			ImDrawListPtr drawList = ImGui.GetWindowDrawList();
			float underlineY = startPos.Y + textSize.Y;
			drawList.AddLine(
				new Vector2(startPos.X, underlineY),
				new Vector2(startPos.X + textSize.X, underlineY),
				ImGui.GetColorU32(Color.Palette.Semantic.Info.Value),
				1.0f);

			if (!string.IsNullOrEmpty(link.Url))
			{
				ImGui.SetTooltip(link.Url);
			}
		}

		firstInline = false;
	}

	private static void RenderHeading(HeadingBlock heading, float wrapWidth)
	{
		float scale = heading.Level switch
		{
			1 => 1.8f,
			2 => 1.5f,
			3 => 1.3f,
			4 => 1.1f,
			5 => 1.0f,
			_ => 0.9f,
		};

		int scaledPointSize = (int)(14 * scale);
		using (new FontAppearance(scaledPointSize))
		{
			if (heading.Inline is not null)
			{
				RenderInlines(heading.Inline, wrapWidth);
			}
		}

		if (heading.Level <= 2)
		{
			ImGui.Separator();
		}

		ImGui.NewLine();
	}

	private static void RenderList(ListBlock list, float wrapWidth, int indentLevel)
	{
		int itemIndex = 0;

		foreach (Block item in list)
		{
			if (item is not ListItemBlock listItem)
			{
				continue;
			}

			float indent = indentLevel * 20.0f;
			ImGui.Indent(indent);

			if (list.IsOrdered)
			{
				bool firstInline = true;
				RenderLiteralText($"{itemIndex + 1}. ", wrapWidth - indent, ref firstInline);

				foreach (Block subBlock in listItem)
				{
					if (subBlock is ParagraphBlock paragraph && paragraph.Inline is not null)
					{
						RenderInlines(paragraph.Inline, wrapWidth - indent, ref firstInline);
					}
					else if (subBlock is ListBlock nestedList)
					{
						ImGui.NewLine();
						RenderList(nestedList, wrapWidth, indentLevel + 1);
					}
					else
					{
						RenderBlock(subBlock, wrapWidth - indent, indentLevel + 1);
					}
				}
			}
			else
			{
				ImGui.Bullet();
				ImGui.SameLine();

				bool firstInline = true;
				foreach (Block subBlock in listItem)
				{
					if (subBlock is ParagraphBlock paragraph && paragraph.Inline is not null)
					{
						RenderInlines(paragraph.Inline, wrapWidth - indent, ref firstInline);
					}
					else if (subBlock is ListBlock nestedList)
					{
						ImGui.NewLine();
						RenderList(nestedList, wrapWidth, indentLevel + 1);
					}
					else
					{
						RenderBlock(subBlock, wrapWidth - indent, indentLevel + 1);
					}
				}
			}

			ImGui.Unindent(indent);
			itemIndex++;
		}
	}

	private static void RenderCodeBlock(CodeBlock codeBlock, float wrapWidth)
	{
		StringBuilder codeBuilder = new();

		for (int i = 0; i < codeBlock.Lines.Count; i++)
		{
			Markdig.Helpers.StringLine line = codeBlock.Lines.Lines[i];
			if (i > 0)
			{
				codeBuilder.Append('\n');
			}

			codeBuilder.Append(line.Slice.ToString());
		}

		string code = codeBuilder.ToString();
		Vector2 textSize = ImGui.CalcTextSize(code);
		float padding = 8.0f;
		Vector2 cursorPos = ImGui.GetCursorScreenPos();

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		Vector2 bgMin = new(cursorPos.X, cursorPos.Y);
		Vector2 bgMax = new(cursorPos.X + Math.Max(textSize.X + (padding * 2), wrapWidth), cursorPos.Y + textSize.Y + (padding * 2));
		drawList.AddRectFilled(bgMin, bgMax, ImGui.GetColorU32(new Vector4(0.2f, 0.2f, 0.2f, 0.6f)), 4.0f);

		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + padding);
		ImGui.SetCursorPosY(ImGui.GetCursorPosY() + padding);

		using (new ScopedColor(ImGuiCol.Text, Color.Palette.Semantic.Success))
		{
			ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + wrapWidth - (padding * 2));
			ImGui.TextUnformatted(code);
			ImGui.PopTextWrapPos();
		}

		ImGui.SetCursorPosY(ImGui.GetCursorPosY() + padding);
	}

	private static void RenderBlockquote(QuoteBlock quote, float wrapWidth)
	{
		float indent = 20.0f;
		float borderThickness = 3.0f;

		Vector2 startPos = ImGui.GetCursorScreenPos();

		ImGui.Indent(indent);

		using (new ScopedColor(ImGuiCol.Text, Color.Palette.Neutral.Gray))
		{
			foreach (Block child in quote)
			{
				if (child is ParagraphBlock paragraph && paragraph.Inline is not null)
				{
					RenderInlines(paragraph.Inline, wrapWidth - indent);
				}
				else
				{
					RenderBlock(child, wrapWidth - indent, 0);
				}
			}
		}

		// GetItemRectMax gives the bottom of the last rendered text item,
		// excluding any trailing NewLine spacing.
		float borderBottom = ImGui.GetItemRectMax().Y;
		ImGui.NewLine();
		ImGui.Unindent(indent);

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		drawList.AddLine(
			new Vector2(startPos.X + 4.0f, startPos.Y),
			new Vector2(startPos.X + 4.0f, borderBottom),
			ImGui.GetColorU32(Color.Palette.Neutral.Gray.Value),
			borderThickness);
	}

	private static void RenderTable(Table table, float wrapWidth)
	{
		int columnCount = 0;
		foreach (Block row in table)
		{
			if (row is TableRow tableRow)
			{
				columnCount = Math.Max(columnCount, tableRow.Count);
			}
		}

		if (columnCount == 0)
		{
			return;
		}

		if (ImGui.BeginTable("##md_table", columnCount, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable))
		{
			bool isHeader = true;

			foreach (Block row in table)
			{
				if (row is not TableRow tableRow)
				{
					continue;
				}

				if (isHeader && tableRow.IsHeader)
				{
					foreach (Block cell in tableRow)
					{
						ImGui.TableSetupColumn(GetCellText(cell));
					}

					ImGui.TableHeadersRow();
					isHeader = false;
					continue;
				}

				ImGui.TableNextRow();
				int columnIndex = 0;

				foreach (Block cell in tableRow)
				{
					ImGui.TableSetColumnIndex(columnIndex);

					if (cell is TableCell tableCell)
					{
						foreach (Block cellBlock in tableCell)
						{
							if (cellBlock is ParagraphBlock paragraph && paragraph.Inline is not null)
							{
								RenderInlines(paragraph.Inline, wrapWidth / columnCount);
							}
						}
					}

					columnIndex++;
				}
			}

			ImGui.EndTable();
		}
	}

	private static string GetCellText(Block cell)
	{
		if (cell is not TableCell tableCell)
		{
			return string.Empty;
		}

		StringBuilder textBuilder = new();
		foreach (Block block in tableCell)
		{
			if (block is ParagraphBlock paragraph && paragraph.Inline is not null)
			{
				foreach (Inline inline in paragraph.Inline)
				{
					if (inline is LiteralInline literal)
					{
						textBuilder.Append(literal.Content.ToString());
					}
				}
			}
		}

		return textBuilder.ToString();
	}
}
