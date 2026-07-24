// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Markdown;

using System;
using System.Collections.Generic;

/// <summary>Pure size and role computations shared by the renderers.</summary>
internal static class MarkdownSizing
{
	/// <summary>Computes the pixel size for a heading level from the live body size and scales.</summary>
	/// <param name="bodyPixelSize">The current body font pixel size (already DPI/accessibility scaled).</param>
	/// <param name="level">The heading level (clamped to 1..6).</param>
	/// <param name="scales">The heading size multipliers, H1 first.</param>
	/// <returns>The heading pixel size.</returns>
	public static float HeadingPixelSize(float bodyPixelSize, int level, IReadOnlyList<float> scales)
	{
		Ensure.NotNull(scales);
		int clamped = Math.Clamp(level, 1, 6);
		float scale = clamped - 1 < scales.Count ? scales[clamped - 1] : 1.0f;
		return bodyPixelSize * scale;
	}

	/// <summary>Maps a heading level (clamped 1..6) to its font role.</summary>
	/// <param name="level">The heading level.</param>
	/// <returns>The matching <see cref="MarkdownFontRole"/>.</returns>
	public static MarkdownFontRole HeadingRole(int level) => Math.Clamp(level, 1, 6) switch
	{
		1 => MarkdownFontRole.H1,
		2 => MarkdownFontRole.H2,
		3 => MarkdownFontRole.H3,
		4 => MarkdownFontRole.H4,
		5 => MarkdownFontRole.H5,
		_ => MarkdownFontRole.H6,
	};

	/// <summary>Combines bold/italic flags into the corresponding emphasis role.</summary>
	/// <param name="bold">Whether the run is bold.</param>
	/// <param name="italic">Whether the run is italic.</param>
	/// <returns>The matching <see cref="MarkdownFontRole"/>.</returns>
	public static MarkdownFontRole EmphasisRole(bool bold, bool italic) => (bold, italic) switch
	{
		(true, true) => MarkdownFontRole.BoldItalic,
		(true, false) => MarkdownFontRole.Bold,
		(false, true) => MarkdownFontRole.Italic,
		_ => MarkdownFontRole.Body,
	};
}
