// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Markdown;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;

using Markdig.Syntax;

/// <summary>Draws a flattened inline run list using the pure layout engine, then paints each token.</summary>
internal static class InlineRenderer
{
	/// <summary>Lays out and draws the inline content of a block.</summary>
	/// <param name="block">The paragraph, heading or table cell paragraph whose inlines to render.</param>
	/// <param name="config">The active config.</param>
	/// <param name="baseRole">The block's own role, which plain text takes and emphasis combines with.</param>
	public static void Render(LeafBlock block, MarkdownConfig config, MarkdownFontRole baseRole = MarkdownFontRole.Body)
	{
		IReadOnlyList<InlineRun> runs = InlineBuilder.Build(block.Inline, baseRole);
		if (runs.Count == 0)
		{
			return;
		}

		// Links are numbered from zero in every block, so each block gets an ID scope of its own.
		// Without it the first link of every paragraph or table cell shares one ID, and ImGui reports a
		// conflict whenever one is hovered. The block's offset into the source is unique in a document.
		ImGui.PushID(block.Span.Start);
		try
		{
			RenderRuns(runs, config, baseRole);
		}
		finally
		{
			ImGui.PopID();
		}
	}

	private static void RenderRuns(IReadOnlyList<InlineRun> runs, MarkdownConfig config, MarkdownFontRole baseRole)
	{
		float wrapWidth = config.WrapWidth ?? ImGui.GetContentRegionAvail().X;
		if (wrapWidth <= 0.0f)
		{
			wrapWidth = 1.0f;
		}

		float bodySize = ImGui.GetFontSize();
		float lineSpacing = ImGui.GetTextLineHeightWithSpacing() - ImGui.GetTextLineHeight();

		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap(runs, wrapWidth, (text, role, isImage) => Measure(text, role, bodySize, config, isImage), baseRole);

		Vector2 origin = ImGui.GetCursorScreenPos();
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		ImGuiVector4 linkColor = MarkdownColors.Link(config);
		uint linkU32 = ImGui.GetColorU32(linkColor);
		uint textU32 = MarkdownColors.TextU32();

		float y = 0.0f;
		int linkIndex = 0;
		foreach (LaidOutLine line in lines)
		{
			foreach (LaidOutToken token in line.Tokens)
			{
				Vector2 pos = new(origin.X + token.X, origin.Y + y);
				DrawToken(token, pos, bodySize, config, drawList, token.LinkUrl is null ? textU32 : linkU32, ref linkIndex);
			}

			y += line.Height + lineSpacing;
		}

		// Reset the cursor to the captured origin before reserving layout space: link and image
		// tokens call SetCursorScreenPos (and images/InvisibleButton advance the ImGui cursor), so
		// when the last drawn token is a link or image the live cursor is no longer at origin.
		ImGui.SetCursorScreenPos(origin);

		// Reserve the space the text occupied so following blocks flow beneath it.
		ImGui.Dummy(new Vector2(wrapWidth, y));
	}

	private static Vector2 Measure(string text, MarkdownFontRole role, float bodySize, MarkdownConfig config, bool isImage)
	{
		if (isImage)
		{
			MarkdownImageResult? image = config.ImageResolver?.Invoke(text);
			float width = image?.Size.X ?? ImagePlaceholderWidth;
			float height = image?.Size.Y ?? bodySize;
			return new Vector2(width, height);
		}

		// Measure with the role's font pushed so the measured width matches what DrawToken draws
		// once a real Code/bold/italic font is registered via FontResolver.
		using ScopedMarkdownFont font = new(role, bodySize, config);
		float baseWidth = ImGui.CalcTextSize(text).X;
		return new Vector2(baseWidth, ImGui.GetTextLineHeight());
	}

	private const float ImagePlaceholderWidth = 120.0f;

	private const float ImagePlaceholderPadding = 2.0f;

	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required for native ImGui/OpenGL interop; pointers are scoped to the call and not retained.")]
	private static void DrawToken(LaidOutToken token, Vector2 pos, float bodySize, MarkdownConfig config, ImDrawListPtr drawList, uint color, ref int linkIndex)
	{
		MarkdownFontRole role = token.Role;
		float size = bodySize;

		if (token.IsImage)
		{
			MarkdownImageResult? image = config.ImageResolver?.Invoke(token.Text);
			Vector2 box;
			if (image.HasValue)
			{
				box = image.Value.Size;
				ImGui.SetCursorScreenPos(pos);
				unsafe
				{
					ImGui.Image(new ImTextureRef(texId: image.Value.TextureId), box);
				}
			}
			else
			{
				// Placeholder box for a remote or unresolved image, showing its description (or its src
				// when it has none). The layout reserved only the box, so the text is clipped to it rather
				// than running on over whatever follows.
				box = new Vector2(token.Width, size);
				Vector2 max = pos + box;
				drawList.AddRect(pos, max, MarkdownColors.Separator());
				string caption = string.IsNullOrEmpty(token.AltText) ? token.Text : token.AltText;
				drawList.PushClipRect(pos, max, true);
				drawList.AddText(pos + new Vector2(ImagePlaceholderPadding, 0.0f), color, caption);
				drawList.PopClipRect();
			}

			if (token.LinkUrl is not null)
			{
				LinkHitTest(token.LinkUrl, pos, box, config, ref linkIndex);
			}

			return;
		}

		using ScopedMarkdownFont font = new(role, size, config);

		if (role == MarkdownFontRole.Code)
		{
			// Subtle background behind inline code.
			Vector2 pad = new(2.0f, 1.0f);
			drawList.AddRectFilled(pos - pad, pos + new Vector2(token.Width, size) + pad, MarkdownColors.InlineCodeBackground(), 2.0f);
		}

		drawList.AddText(pos, color, token.Text);

		if (font.FauxBold)
		{
			// Second draw offset by one pixel thickens the glyphs.
			drawList.AddText(pos + new Vector2(1.0f, 0.0f), color, token.Text);
		}

		if (token.LinkUrl is not null)
		{
			// Underline and hit-test the link token.
			float underlineY = pos.Y + size;
			drawList.AddLine(new Vector2(pos.X, underlineY), new Vector2(pos.X + token.Width, underlineY), color, 1.0f);
			LinkHitTest(token.LinkUrl, pos, new Vector2(token.Width, size), config, ref linkIndex);
		}
	}

	/// <summary>Makes a link's area clickable: the hand cursor on hover, and the link activated on a click.</summary>
	private static void LinkHitTest(string url, Vector2 pos, Vector2 size, MarkdownConfig config, ref int linkIndex)
	{
		ImGui.SetCursorScreenPos(pos);
		ImGui.PushID(linkIndex++);
		ImGui.InvisibleButton("##mdlink", size);
		if (ImGui.IsItemHovered())
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		if (ImGui.IsItemClicked())
		{
			LinkPolicy.Activate(url, config.OnLinkClicked);
		}

		ImGui.PopID();
	}
}
