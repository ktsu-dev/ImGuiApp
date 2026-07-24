// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

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

		foreach (InlineRun run in runs)
		{
			IEnumerable<string> tokens = run.IsImage
				? [run.Text]
				: SplitWords(run.Text);

			foreach (string token in tokens)
			{
				Vector2 tokenSize = measure(token, run.Role, run.IsImage);
				float tokenWidth = tokenSize.X;
				float advance = current.Count == 0 ? 0.0f : spaceWidth;

				if (current.Count > 0 && cursorX + advance + tokenWidth > maxWidth)
				{
					FlushLine();
					advance = 0.0f;
				}

				float x = cursorX + advance;
				current.Add(new LaidOutToken(token, run.Role, run.LinkUrl, run.IsImage, x, tokenWidth));
				cursorX = x + tokenWidth;
				lineHeight = Math.Max(lineHeight, tokenSize.Y);
			}
		}

		FlushLine();
		return lines;
	}

	private static IEnumerable<string> SplitWords(string text)
	{
		foreach (string part in text.Split(' '))
		{
			if (part.Length > 0)
			{
				yield return part;
			}
		}
	}
}
