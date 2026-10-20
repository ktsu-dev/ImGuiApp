// Copyright (c) 2023-2026 ktsu-dev contributors

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

	/// <summary>
	/// Combines a block's base role with a run's emphasis. Body text takes the emphasis as it is, a
	/// bold block (a table header row) adds its weight to any italic, and a heading keeps its own
	/// role, since a heading's typeface and size come from the heading and there is no emphasised
	/// heading role to resolve.
	/// </summary>
	/// <param name="baseRole">The role of the block the run sits in.</param>
	/// <param name="bold">Whether the run is bold.</param>
	/// <param name="italic">Whether the run is italic.</param>
	/// <returns>The role to measure and draw the run in.</returns>
	public static MarkdownFontRole CombineRole(MarkdownFontRole baseRole, bool bold, bool italic) => baseRole switch
	{
		MarkdownFontRole.Bold => EmphasisRole(true, italic),
		MarkdownFontRole.Italic => EmphasisRole(bold, true),
		MarkdownFontRole.BoldItalic => MarkdownFontRole.BoldItalic,
		MarkdownFontRole.Body => EmphasisRole(bold, italic),
		_ => baseRole,
	};
}
