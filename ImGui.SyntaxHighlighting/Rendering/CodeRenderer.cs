// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.SyntaxHighlighting;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

using Hexa.NET.ImGui;

using ktsu.SyntaxHighlighting;

/// <summary>
/// Draws highlighted lines: a background panel, an optional line-number gutter, and one colored
/// draw-list run per token. Everything is painted into the window draw list and the total footprint
/// is reserved afterwards, so the block behaves like a single item to the surrounding layout.
/// </summary>
/// <remarks>
/// Only the lines inside the window's clip rectangle are measured and drawn, so a frame costs what
/// is on screen rather than what is in the file. The footprint is still the whole block, as wide as
/// its widest line, which is what lets a horizontally scrolling parent reach the end of a long line.
/// </remarks>
internal static class CodeRenderer
{
	/// <summary>
	/// The widest line of each block, measured once per font rather than once per frame.
	/// </summary>
	/// <remarks>
	/// Keyed by the line list itself, which the tokenizing cache and <see cref="HighlightedCode"/>
	/// both hand back unchanged from frame to frame, and held weakly so a block nobody draws any more
	/// takes its measurement with it.
	/// </remarks>
	private static readonly ConditionalWeakTable<IReadOnlyList<HighlightedLine>, WidestLine> WidestLines = [];

	/// <summary>A block's widest line and the font it was measured in.</summary>
	/// <param name="FontKey">What the font measured as, so a different font or size measures again.</param>
	/// <param name="Width">The widest line's width, in pixels.</param>
	private sealed record WidestLine(Vector2 FontKey, float Width);

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
		float available = config.Width ?? ImGui.GetContentRegionAvail().X;
		float height = (lines.Count * lineHeight) - config.LineSpacingPixels + (padding * 2.0f);

		string widestNumber = (config.FirstLineNumber + lines.Count - 1).ToString(CultureInfo.InvariantCulture);
		float numberWidth = config.ShowLineNumbers ? ImGui.CalcTextSize(widestNumber).X : 0.0f;
		float gutter = config.ShowLineNumbers ? numberWidth + config.GutterSpacingPixels : 0.0f;

		// Never narrower than the space it was given, so a short block still fills its row, and never
		// narrower than its widest line, so the line's end is inside the reserved footprint.
		float width = MathF.Max(available, (padding * 2.0f) + gutter + MeasureWidestLine(lines));

		Vector2 origin = ImGui.GetCursorScreenPos();
		ImDrawListPtr drawList = ImGui.GetWindowDrawList();
		(int first, int last) = VisibleLines(drawList, origin.Y + padding, lineHeight, lines.Count);

		if (config.ShowBackground)
		{
			drawList.AddRectFilled(origin, origin + new Vector2(width, height), SyntaxColors.Background(theme), config.BackgroundRounding);
		}

		float textLeft = origin.X + padding + gutter;
		for (int index = first; index < last; index++)
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

	/// <summary>The range of lines the clip rectangle can show any part of.</summary>
	/// <remarks>
	/// Lines are a uniform height, so the range is arithmetic rather than a search. It is widened by a
	/// line at either end, which covers a glyph whose ink reaches past its own line box.
	/// </remarks>
	/// <param name="drawList">The draw list being painted, whose clip rectangle bounds the range.</param>
	/// <param name="top">The screen y of the first line.</param>
	/// <param name="lineHeight">The distance from one line to the next.</param>
	/// <param name="count">How many lines the block has.</param>
	/// <returns>The first line to draw and the line after the last.</returns>
	internal static (int First, int Last) VisibleLines(ImDrawListPtr drawList, float top, float lineHeight, int count)
	{
		float clipTop = ImGui.GetClipRectMin(drawList).Y;
		float clipBottom = ImGui.GetClipRectMax(drawList).Y;

		return VisibleLines(clipTop, clipBottom, top, lineHeight, count);
	}

	/// <summary>The range of lines between two screen heights.</summary>
	/// <param name="clipTop">The top of the visible region, in screen pixels.</param>
	/// <param name="clipBottom">The bottom of the visible region, in screen pixels.</param>
	/// <param name="top">The screen y of the first line.</param>
	/// <param name="lineHeight">The distance from one line to the next.</param>
	/// <param name="count">How many lines the block has.</param>
	/// <returns>The first line to draw and the line after the last.</returns>
	internal static (int First, int Last) VisibleLines(float clipTop, float clipBottom, float top, float lineHeight, int count)
	{
		if (lineHeight <= 0.0f)
		{
			return (0, count);
		}

		int first = (int)MathF.Floor((clipTop - top) / lineHeight) - 1;
		int last = (int)MathF.Ceiling((clipBottom - top) / lineHeight) + 1;

		first = Math.Clamp(first, 0, count);
		last = Math.Clamp(last, first, count);

		return (first, last);
	}

	/// <summary>The width of the block's widest line in the current font.</summary>
	/// <remarks>
	/// Measured over every line, which is the one pass here that cannot be limited to what is on
	/// screen, so it is done once per block and font and kept. A line is measured whole rather than
	/// token by token: the width is the same, and it is one call rather than one per token.
	/// </remarks>
	/// <param name="lines">The block's lines.</param>
	/// <returns>The widest line's width, in pixels.</returns>
	private static float MeasureWidestLine(IReadOnlyList<HighlightedLine> lines)
	{
		// Two glyphs of different widths, so a change of face is caught as well as a change of size.
		Vector2 fontKey = new(ImGui.GetFontSize(), ImGui.CalcTextSize("Mi").X);

		if (WidestLines.TryGetValue(lines, out WidestLine? cached) && cached.FontKey == fontKey)
		{
			return cached.Width;
		}

		float widest = 0.0f;
		foreach (HighlightedLine line in lines)
		{
			float lineWidth = 0.0f;
			foreach (HighlightedToken token in line.Tokens)
			{
				lineWidth += ImGui.CalcTextSize(token.Text).X;
			}

			widest = MathF.Max(widest, lineWidth);
		}

		WidestLines.AddOrUpdate(lines, new WidestLine(fontKey, widest));

		return widest;
	}

	// Tabs are already expanded by the line splitter, so a run is blank exactly when it is all spaces.
	// The span check reads the string in place rather than allocating on the per-token draw path.
	private static bool IsBlank(string text) => !text.AsSpan().ContainsAnyExcept(' ');
}
