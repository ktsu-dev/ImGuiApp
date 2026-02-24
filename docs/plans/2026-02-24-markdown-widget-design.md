# Markdown Widget Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a markdown rendering widget to `ImGui.Widgets` that parses markdown strings via Markdig and renders them using ImGui primitives with color-based styling.

**Architecture:** Markdig parses markdown into an AST. A `MarkdownRenderer` walks the AST tree, dispatching to render methods per block/inline node type. Inline emphasis uses color-based styling via the existing `ScopedColor` system. Parsed documents are cached to avoid per-frame re-parsing. The public API follows the existing `ImGuiWidgets` partial class pattern.

**Tech Stack:** C#, .NET 8/9/10 multi-target, Hexa.NET.ImGui 2.2.9, Markdig 0.45.0, ktsu.ImGui.Styler (ScopedColor, Color.Palette)

---

### Task 1: Add Markdig Dependency

**Files:**
- Modify: `Directory.Packages.props` (add PackageVersion entry)
- Modify: `ImGui.Widgets/ImGui.Widgets.csproj` (add PackageReference)

**Step 1: Add Markdig version to central package management**

In `Directory.Packages.props`, add inside the `<ItemGroup>`:

```xml
<PackageVersion Include="Markdig" Version="0.45.0" />
```

**Step 2: Add PackageReference to ImGui.Widgets**

In `ImGui.Widgets/ImGui.Widgets.csproj`, add inside the `<ItemGroup>`:

```xml
<PackageReference Include="Markdig" />
```

**Step 3: Restore and build to verify**

Run: `dotnet restore && dotnet build ImGui.Widgets/ImGui.Widgets.csproj`
Expected: BUILD SUCCEEDED

**Step 4: Commit**

```bash
git add Directory.Packages.props ImGui.Widgets/ImGui.Widgets.csproj
git commit -m "[patch] Add Markdig NuGet dependency for markdown widget"
```

---

### Task 2: Create Public API (`Markdown.cs`)

**Files:**
- Create: `ImGui.Widgets/Markdown.cs`

**Step 1: Create the public API file**

This follows the existing pattern from `Text.cs` where `ImGuiWidgets` is a partial class with public static methods delegating to an internal impl class. Also defines `MarkdownOptions`.

```csharp
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
```

**Step 2: Build to verify compilation**

Run: `dotnet build ImGui.Widgets/ImGui.Widgets.csproj`
Expected: Error (MarkdownRenderer doesn't exist yet) - that's fine, we'll fix in next task.

---

### Task 3: Create MarkdownRenderer - Core Infrastructure

**Files:**
- Create: `ImGui.Widgets/MarkdownRenderer.cs`

This is the main rendering engine. Build it incrementally - start with the infrastructure (caching, pipeline, AST walking skeleton, paragraph/plain text rendering).

**Step 1: Create the renderer with caching and basic paragraph support**

```csharp
// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets;

using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

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

	private static readonly ConditionalWeakTable<string, MarkdownDocument> ParseCache = new();

	/// <summary>
	/// Renders a markdown string using ImGui.
	/// </summary>
	/// <param name="markdown">The raw markdown text.</param>
	/// <param name="options">Rendering options.</param>
	public static void Render(string markdown, MarkdownOptions options)
	{
		Ensure.NotNull(markdown);
		Ensure.NotNull(options);

		MarkdownDocument document = ParseCache.GetValue(markdown, key => Markdig.Markdown.Parse(key, Pipeline));

		float wrapWidth = options.WrapWidth > 0 ? options.WrapWidth : ImGui.GetContentRegionAvail().X;

		foreach (Block block in document)
		{
			RenderBlock(block, wrapWidth, 0);
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
					RenderInlines(childContainer, wrapWidth);
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
		bool isItalic = emphasis.DelimiterCount == 1 || emphasis.DelimiterCount >= 3;
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

		Vector2 textSize = ImGui.CalcTextSize(text);
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
		string linkText = string.Empty;
		foreach (Inline child in link)
		{
			if (child is LiteralInline literal)
			{
				linkText += literal.Content.ToString();
			}
		}

		if (string.IsNullOrEmpty(linkText))
		{
			linkText = link.Url ?? string.Empty;
		}

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

		ImGui.SetWindowFontScale(scale);

		if (heading.Inline is not null)
		{
			bool firstInline = true;
			foreach (Inline inline in heading.Inline)
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
					default:
						break;
				}
			}
		}

		ImGui.SetWindowFontScale(1.0f);

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

			float indent = (indentLevel + 1) * 20.0f;
			ImGui.Indent(indent);

			string bullet = list.IsOrdered
				? $"{itemIndex + 1}. "
				: "\u2022 ";

			bool firstInline = true;
			RenderLiteralText(bullet, wrapWidth - indent, ref firstInline);

			foreach (Block subBlock in listItem)
			{
				if (subBlock is ParagraphBlock paragraph && paragraph.Inline is not null)
				{
					RenderInlines(paragraph.Inline, wrapWidth - indent);
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

			ImGui.Unindent(indent);
			ImGui.NewLine();
			itemIndex++;
		}
	}

	private static void RenderCodeBlock(CodeBlock codeBlock, float wrapWidth)
	{
		string code = string.Empty;

		for (int i = 0; i < codeBlock.Lines.Count; i++)
		{
			Markdig.Helpers.StringLine line = codeBlock.Lines.Lines[i];
			if (i > 0)
			{
				code += "\n";
			}

			code += line.Slice.ToString();
		}

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
				RenderBlock(child, wrapWidth - indent, 0);
			}
		}

		ImGui.Unindent(indent);

		Vector2 endPos = ImGui.GetCursorScreenPos();

		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		drawList.AddLine(
			new Vector2(startPos.X + 4.0f, startPos.Y),
			new Vector2(startPos.X + 4.0f, endPos.Y),
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

		string text = string.Empty;
		foreach (Block block in tableCell)
		{
			if (block is ParagraphBlock paragraph && paragraph.Inline is not null)
			{
				foreach (Inline inline in paragraph.Inline)
				{
					if (inline is LiteralInline literal)
					{
						text += literal.Content.ToString();
					}
				}
			}
		}

		return text;
	}
}
```

**Step 2: Build to verify compilation**

Run: `dotnet build ImGui.Widgets/ImGui.Widgets.csproj`
Expected: BUILD SUCCEEDED

**Step 3: Commit**

```bash
git add ImGui.Widgets/Markdown.cs ImGui.Widgets/MarkdownRenderer.cs
git commit -m "[minor] Add markdown rendering widget with Markdig AST walker"
```

---

### Task 4: Build Full Solution and Verify

**Step 1: Build the entire solution**

Run: `dotnet build`
Expected: BUILD SUCCEEDED for all projects

**Step 2: Run existing tests to ensure no regressions**

Run: `dotnet test`
Expected: All existing tests pass

**Step 3: Commit if any fixes were needed**

---

### Task 5: Add Demo to ImGuiWidgetsDemo

**Files:**
- Modify: `examples/ImGuiWidgetsDemo/` (main demo file)

**Step 1: Find and read the widgets demo entry point**

Look at the existing demo to understand how widgets are showcased.

**Step 2: Add a markdown demo section**

Add a collapsing header section with sample markdown showcasing all supported features:
- Headers of different levels
- Bold, italic, bold+italic text
- Bullet and numbered lists with nesting
- Inline code and code blocks
- Links
- Blockquotes
- Tables
- Strikethrough
- Horizontal rules

Use `ImGui.CollapsingHeader("Markdown")` to wrap the demo, then call `ImGuiWidgets.Markdown(sampleText)`.

**Step 3: Run the demo to visually verify**

Run: `dotnet run --project examples/ImGuiWidgetsDemo`
Expected: Markdown renders correctly with styled text, lists, code blocks, tables, etc.

**Step 4: Commit**

```bash
git add examples/ImGuiWidgetsDemo/
git commit -m "[patch] Add markdown widget demo to ImGuiWidgetsDemo"
```

---

### Task 6: Iterate and Polish

**Step 1: Test edge cases visually**

- Empty string input
- Very long paragraphs (text wrapping)
- Deeply nested lists (3+ levels)
- Large tables
- Mixed inline styles (bold inside italic inside link)
- Code blocks with long lines

**Step 2: Fix any rendering issues found**

Adjust spacing, colors, or layout as needed based on visual testing.

**Step 3: Final build and test**

Run: `dotnet build && dotnet test`
Expected: All pass

**Step 4: Commit any fixes**

```bash
git add -A
git commit -m "[patch] Polish markdown widget rendering"
```
