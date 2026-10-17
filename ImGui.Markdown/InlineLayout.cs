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
internal readonly record struct InlineRun(string Text, MarkdownFontRole Role, string? LinkUrl, bool IsImage);

/// <summary>A single positioned token within a laid-out line.</summary>
/// <param name="Text">The token text.</param>
/// <param name="Role">The typographic role.</param>
/// <param name="LinkUrl">The enclosing link URL, or <see langword="null"/>.</param>
/// <param name="IsImage">Whether this token is an inline image (its <paramref name="Text"/> is the image key).</param>
/// <param name="X">The token's horizontal offset from the line start, in pixels.</param>
/// <param name="Width">The measured token width, in pixels.</param>
internal readonly record struct LaidOutToken(string Text, MarkdownFontRole Role, string? LinkUrl, bool IsImage, float X, float Width);

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
	/// <returns>The wrapped lines.</returns>
	public static IReadOnlyList<LaidOutLine> Wrap(
		IReadOnlyList<InlineRun> runs,
		float maxWidth,
		Func<string, MarkdownFontRole, bool, Vector2> measure)
	{
		Ensure.NotNull(runs);
		Ensure.NotNull(measure);

		float spaceWidth = measure(" ", MarkdownFontRole.Body, false).X;

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
				current.Add(new LaidOutToken(text, run.Role, run.LinkUrl, run.IsImage, cursorX, sizes[i].X));
				cursorX += sizes[i].X;
				lineHeight = Math.Max(lineHeight, sizes[i].Y);
			}
		}

		FlushLine();
		return lines;
	}

	/// <summary>
	/// Splits the runs into words: groups of tokens with no whitespace between them in the source.
	/// </summary>
	private static IEnumerable<List<(string Text, InlineRun Run)>> SplitGluedWords(IReadOnlyList<InlineRun> runs)
	{
		List<(string Text, InlineRun Run)> word = [];
		bool spaceBefore = false;

		foreach (InlineRun run in runs)
		{
			if (run.IsImage)
			{
				if (spaceBefore && word.Count > 0)
				{
					yield return word;
					word = [];
				}

				word.Add((run.Text, run));
				spaceBefore = false;
				continue;
			}

			foreach (string part in run.Text.Split(' '))
			{
				if (part.Length == 0)
				{
					// An empty part is a space at the start or end of the run, or one of several in a row.
					spaceBefore = true;
					continue;
				}

				if (spaceBefore && word.Count > 0)
				{
					yield return word;
					word = [];
				}

				word.Add((part, run));
				spaceBefore = true;
			}

			// The next run's first token follows whitespace only if this run ended in some.
			spaceBefore = run.Text.Length == 0 ? spaceBefore : run.Text[^1] == ' ';
		}

		if (word.Count > 0)
		{
			yield return word;
		}
	}
}
