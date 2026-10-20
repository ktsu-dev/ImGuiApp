// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Markdown;

using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>A styled run of inline content produced from the markdown AST.</summary>
/// <param name="Text">The run text (for images, the resolved src used as the image key).</param>
/// <param name="Role">The typographic role controlling font selection.</param>
/// <param name="LinkUrl">The enclosing link URL, or <see langword="null"/> when not a link.</param>
/// <param name="IsImage">Whether this run is an inline image rather than text.</param>
/// <param name="IsHardBreak">Whether this run is a hard line break, which ends the current line and carries no text.</param>
/// <param name="AltText">For an image, its description, which a placeholder shows when nothing resolves the image.</param>
internal readonly record struct InlineRun(string Text, MarkdownFontRole Role, string? LinkUrl, bool IsImage, bool IsHardBreak = false, string? AltText = null);

/// <summary>A single positioned token within a laid-out line.</summary>
/// <param name="Text">The token text.</param>
/// <param name="Role">The typographic role.</param>
/// <param name="LinkUrl">The enclosing link URL, or <see langword="null"/>.</param>
/// <param name="IsImage">Whether this token is an inline image (its <paramref name="Text"/> is the image key).</param>
/// <param name="X">The token's horizontal offset from the line start, in pixels.</param>
/// <param name="Width">The measured token width, in pixels.</param>
/// <param name="AltText">For an image, its description.</param>
internal readonly record struct LaidOutToken(string Text, MarkdownFontRole Role, string? LinkUrl, bool IsImage, float X, float Width, string? AltText = null);

/// <summary>A wrapped line of tokens.</summary>
internal sealed class LaidOutLine
{
	/// <summary>The positioned tokens on this line, in order.</summary>
	public required IReadOnlyList<LaidOutToken> Tokens { get; init; }

	/// <summary>The total line width in pixels (end of the last token).</summary>
	public required float Width { get; init; }

	/// <summary>The maximum token height on this line, in pixels (zero when the line is empty).</summary>
	public required float Height { get; init; }
}

/// <summary>How the lines of a block of inline content sit within the width they were wrapped to.</summary>
internal enum InlineAlignment
{
	/// <summary>Lines start at the left edge. The default, and what a table column with no marker gets.</summary>
	Left,

	/// <summary>Lines are centered, as a <c>|:-:|</c> table column asks.</summary>
	Center,

	/// <summary>Lines end at the right edge, as a <c>|--:|</c> table column asks.</summary>
	Right,
}

/// <summary>
/// Pure word-wrap engine. Splits styled runs into word tokens and greedily packs them into
/// lines that fit within a maximum width, using an injected measurement function.
/// </summary>
internal static class InlineLayout
{
	/// <summary>
	/// Wraps the given runs into lines no wider than <paramref name="maxWidth"/>.
	/// </summary>
	/// <param name="runs">The styled runs to lay out.</param>
	/// <param name="maxWidth">The maximum line width in pixels.</param>
	/// <param name="measure">Measures the pixel size (width in X, height in Y) of text for a given role.</param>
	/// <param name="spaceRole">The role the space between words is measured in: the block's own role.</param>
	/// <returns>The wrapped lines.</returns>
	public static IReadOnlyList<LaidOutLine> Wrap(
		IReadOnlyList<InlineRun> runs,
		float maxWidth,
		Func<string, MarkdownFontRole, bool, Vector2> measure,
		MarkdownFontRole spaceRole = MarkdownFontRole.Body)
	{
		Ensure.NotNull(runs);
		Ensure.NotNull(measure);

		float spaceWidth = measure(" ", spaceRole, false).X;

		List<LaidOutLine> lines = [];
		List<LaidOutToken> current = [];
		float cursorX = 0.0f;
		float lineHeight = 0.0f;

		void FlushLine()
		{
			if (current.Count > 0)
			{
				LaidOutToken[] tokens = [.. current];
				lines.Add(new LaidOutLine { Tokens = tokens, Width = cursorX, Height = lineHeight });
			}

			current = [];
			cursorX = 0.0f;
			lineHeight = 0.0f;
		}

		// A token only gets a space before it, and a line may only break before it, where the source
		// had whitespace. Runs split at every emphasis, code and link boundary, so a token that does not
		// follow whitespace is glued to the one before it even across runs, and a glued group wraps as
		// one word.
		foreach (List<(string Text, InlineRun Run)> word in SplitGluedWords(runs))
		{
			// A hard break ends the line even when the next word would fit. Flushing an empty line is
			// a no-op, so a hard break at the end of a paragraph adds no blank line.
			if (word[0].Run.IsHardBreak)
			{
				FlushLine();
				continue;
			}

			float wordWidth = 0.0f;
			Vector2[] sizes = new Vector2[word.Count];
			for (int i = 0; i < word.Count; i++)
			{
				sizes[i] = measure(word[i].Text, word[i].Run.Role, word[i].Run.IsImage);
				wordWidth += sizes[i].X;
			}

			float advance = current.Count == 0 ? 0.0f : spaceWidth;

			if (current.Count > 0 && cursorX + advance + wordWidth > maxWidth)
			{
				FlushLine();
				advance = 0.0f;
			}

			cursorX += advance;

			for (int i = 0; i < word.Count; i++)
			{
				(string text, InlineRun run) = word[i];
				current.Add(new LaidOutToken(text, run.Role, run.LinkUrl, run.IsImage, cursorX, sizes[i].X, run.AltText));
				cursorX += sizes[i].X;
				lineHeight = Math.Max(lineHeight, sizes[i].Y);
			}
		}

		FlushLine();
		return lines;
	}

	/// <summary>
	/// The horizontal offset that places a line of <paramref name="lineWidth"/> pixels within
	/// <paramref name="availableWidth"/> under <paramref name="alignment"/>. A line wider than the space
	/// it was wrapped to (one word too long to break) starts at the left edge rather than before it.
	/// </summary>
	/// <param name="lineWidth">The laid-out width of the line, in pixels.</param>
	/// <param name="availableWidth">The width the line was wrapped to, in pixels.</param>
	/// <param name="alignment">The alignment to apply.</param>
	/// <returns>The offset from the left edge, in pixels, never negative.</returns>
	public static float AlignmentOffset(float lineWidth, float availableWidth, InlineAlignment alignment)
	{
		float slack = Math.Max(0.0f, availableWidth - lineWidth);
		return alignment switch
		{
			InlineAlignment.Right => slack,
			InlineAlignment.Center => slack * 0.5f,
			_ => 0.0f,
		};
	}

	/// <summary>
	/// Splits the runs into words: groups of tokens with no whitespace between them in the source.
	/// </summary>
	private static IEnumerable<List<(string Text, InlineRun Run)>> SplitGluedWords(IReadOnlyList<InlineRun> runs)
	{
		List<(string Text, InlineRun Run)> word = [];

		foreach ((string text, InlineRun run, bool spaceBefore) in Tokenize(runs))
		{
			if (spaceBefore && word.Count > 0)
			{
				yield return word;
				word = [];
			}

			word.Add((text, run));
		}

		if (word.Count > 0)
		{
			yield return word;
		}
	}

	/// <summary>
	/// Splits the runs into tokens, flagging each one the source had whitespace before.
	/// </summary>
	private static IEnumerable<(string Text, InlineRun Run, bool SpaceBefore)> Tokenize(IReadOnlyList<InlineRun> runs)
	{
		bool spaceBefore = false;

		foreach (InlineRun run in runs)
		{
			// A hard break is a word of its own: whitespace on both sides keeps it from gluing to either neighbour.
			if (run.IsHardBreak)
			{
				yield return (string.Empty, run, true);
				spaceBefore = true;
				continue;
			}

			if (run.IsImage)
			{
				yield return (run.Text, run, spaceBefore);
				spaceBefore = false;
				continue;
			}

			if (run.Text.Length == 0)
			{
				continue;
			}

			// A code span is one token with its spaces kept: CommonMark keeps interior spaces in code,
			// and splitting it would draw each word as a chip of its own with body-font gaps between.
			// Markdig has already stripped the one space a span may be padded with at either end.
			if (run.Role == MarkdownFontRole.Code)
			{
				yield return (run.Text, run, spaceBefore);
				spaceBefore = false;
				continue;
			}

			foreach (string part in run.Text.Split(' '))
			{
				// An empty part is a space at the start or end of the run, or one of several in a row.
				if (part.Length > 0)
				{
					yield return (part, run, spaceBefore);
				}

				spaceBefore = true;
			}

			// The next run's first token follows whitespace only if this run ended in some.
			spaceBefore = run.Text[^1] == ' ';
		}
	}
}
