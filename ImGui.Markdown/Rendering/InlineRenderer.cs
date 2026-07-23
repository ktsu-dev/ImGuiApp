// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.Color;

using Markdig.Syntax.Inlines;

/// <summary>Draws a flattened inline run list using the pure layout engine, then paints each token.</summary>
internal static class InlineRenderer
{
	/// <summary>Lays out and draws the inline content of a block.</summary>
	/// <param name="inline">The inline container to render.</param>
	/// <param name="config">The active config.</param>
	public static void Render(ContainerInline? inline, MarkdownConfig config)
	{
		IReadOnlyList<InlineRun> runs = InlineBuilder.Build(inline);
		if (runs.Count == 0)
		{
			return;
		}

		float wrapWidth = config.WrapWidth ?? ImGui.GetContentRegionAvail().X;
		if (wrapWidth <= 0.0f)
		{
			wrapWidth = 1.0f;
		}

		float bodySize = ImGui.GetFontSize();
		float lineHeight = ImGui.GetTextLineHeightWithSpacing();

		IReadOnlyList<LaidOutLine> lines = InlineLayout.Wrap(runs, wrapWidth, (text, role, isImage) => Measure(text, role, bodySize, config, isImage));

		Vector2 origin = ImGui.GetCursorScreenPos();
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		ImGuiVector4 linkColor = MarkdownColors.Link(config);
		uint linkU32 = ImGui.GetColorU32(linkColor);
		uint textU32 = MarkdownColors.TextU32();

		float y = 0.0f;
		foreach (LaidOutLine line in lines)
		{
			foreach (LaidOutToken token in line.Tokens)
			{
				Vector2 pos = new(origin.X + token.X, origin.Y + y);
				DrawToken(token, pos, bodySize, config, drawList, token.LinkUrl is null ? textU32 : linkU32);
			}

			y += lineHeight;
		}

		// Reserve the space the text occupied so following blocks flow beneath it.
		ImGui.Dummy(new Vector2(wrapWidth, y));
	}

	private static float Measure(string text, MarkdownFontRole role, float bodySize, MarkdownConfig config, bool isImage)
	{
		if (isImage)
		{
			MarkdownImageResult? image = config.ImageResolver?.Invoke(text);
			return image?.Size.X ?? ImagePlaceholderWidth;
		}

		float size = role is >= MarkdownFontRole.H1 and <= MarkdownFontRole.H6
			? MarkdownSizing.HeadingPixelSize(bodySize, (int)role - (int)MarkdownFontRole.H1 + 1, config.HeadingScales)
			: bodySize;

		// CalcTextSize measures at the current font size; scale by the role's target size ratio.
		float baseWidth = ImGui.CalcTextSize(text).X;
		return baseWidth * (size / bodySize);
	}

	private const float ImagePlaceholderWidth = 120.0f;

	[SuppressMessage("Major Code Smell", "S6640:Make sure that using \"unsafe\" is safe here", Justification = "Required for native ImGui/OpenGL interop; pointers are scoped to the call and not retained.")]
	private static void DrawToken(LaidOutToken token, Vector2 pos, float bodySize, MarkdownConfig config, ImDrawListPtr drawList, uint color)
	{
		MarkdownFontRole role = token.Role;
		float size = role is >= MarkdownFontRole.H1 and <= MarkdownFontRole.H6
			? MarkdownSizing.HeadingPixelSize(bodySize, (int)role - (int)MarkdownFontRole.H1 + 1, config.HeadingScales)
			: bodySize;

		if (token.IsImage)
		{
			MarkdownImageResult? image = config.ImageResolver?.Invoke(token.Text);
			if (image.HasValue)
			{
				ImGui.SetCursorScreenPos(pos);
				unsafe
				{
					ImGui.Image(new ImTextureRef(texId: image.Value.TextureId), image.Value.Size);
				}
			}
			else
			{
				// Placeholder box with the src/alt text for remote or unresolved images.
				drawList.AddRect(pos, pos + new Vector2(token.Width, size), MarkdownColors.Separator());
				drawList.AddText(pos + new Vector2(2.0f, 0.0f), color, token.Text);
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

			ImGui.SetCursorScreenPos(pos);
			ImGui.InvisibleButton("##mdlink_" + token.LinkUrl + "_" + pos.X.ToString(System.Globalization.CultureInfo.InvariantCulture) + "_" + pos.Y.ToString(System.Globalization.CultureInfo.InvariantCulture), new Vector2(token.Width, size));
			if (ImGui.IsItemHovered())
			{
				ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			}

			if (ImGui.IsItemClicked())
			{
				LinkPolicy.Activate(token.LinkUrl, config.OnLinkClicked);
			}
		}
	}
}
