// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.SyntaxHighlighting;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

using Hexa.NET.ImGui;

/// <summary>
/// Draws highlighted lines: a background panel, an optional line-number gutter, and one colored
/// draw-list run per token. Everything is painted into the window draw list and the total footprint
/// is reserved afterwards, so the block behaves like a single item to the surrounding layout.
/// </summary>
internal static class CodeRenderer
{
	/// <summary>Draws highlighted lines at the current cursor position.</summary>
	/// <param name="lines">The lines to draw.</param>
	/// <param name="config">The active config.</param>
	public static void Render(IReadOnlyList<HighlightedLine> lines, SyntaxHighlightConfig config)
	{
		Ensure.NotNull(lines);
		Ensure.NotNull(config);

		if (lines.Count == 0)
		{
			return;
		}

		float fontSize = config.FontSizePixels ?? ImGui.GetFontSize();
		using ScopedCodeFont font = new(fontSize, config);

		SyntaxTheme theme = SyntaxColors.Resolve(config);
		uint[] palette = SyntaxColors.BuildPalette(theme);
		uint lineNumberColor = SyntaxColors.LineNumber(theme);

		float lineHeight = ImGui.GetTextLineHeight() + config.LineSpacingPixels;
		float padding = config.PaddingPixels;
		float width = config.Width ?? ImGui.GetContentRegionAvail().X;
		float height = (lines.Count * lineHeight) - config.LineSpacingPixels + (padding * 2.0f);

		string widestNumber = (config.FirstLineNumber + lines.Count - 1).ToString(CultureInfo.InvariantCulture);
		float numberWidth = config.ShowLineNumbers ? ImGui.CalcTextSize(widestNumber).X : 0.0f;
		float gutter = config.ShowLineNumbers ? numberWidth + config.GutterSpacingPixels : 0.0f;

		Vector2 origin = ImGui.GetCursorScreenPos();
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();

		if (config.ShowBackground)
		{
			drawList.AddRectFilled(origin, origin + new Vector2(width, height), SyntaxColors.Background(theme), config.BackgroundRounding);
		}

		float textLeft = origin.X + padding + gutter;
		for (int index = 0; index < lines.Count; index++)
		{
			float y = origin.Y + padding + (index * lineHeight);

			if (config.ShowLineNumbers)
			{
				// Right-align the numbers so the code column does not shift when the digit count grows.
				string number = (config.FirstLineNumber + index).ToString(CultureInfo.InvariantCulture);
				float offset = numberWidth - ImGui.CalcTextSize(number).X;
				drawList.AddText(new Vector2(origin.X + padding + offset, y), lineNumberColor, number);
			}

			float x = textLeft;
			foreach (HighlightedToken token in lines[index].Tokens)
			{
				float advance = ImGui.CalcTextSize(token.Text).X;

				// Whitespace has nothing to paint, so only its advance matters.
				if (!IsBlank(token.Text))
				{
					drawList.AddText(new Vector2(x, y), palette[(int)token.Kind], token.Text);
				}

				x += advance;
			}
		}

		// Reserve the drawn footprint so following widgets flow beneath the block.
		ImGui.SetCursorScreenPos(origin);
		ImGui.Dummy(new Vector2(width, height));
	}

	// Tabs are already expanded by the line splitter, so a run is blank exactly when it is all spaces.
	// The span check reads the string in place rather than allocating on the per-token draw path.
	private static bool IsBlank(string text) => !text.AsSpan().ContainsAnyExcept(' ');
}
