// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Provides custom ImGui widgets.
/// </summary>
public static partial class ImGuiWidgets
{
	/// <summary>What one line of a diff does.</summary>
	public enum DiffLineKind
	{
		/// <summary>Present on both sides, shown for context.</summary>
		Context,

		/// <summary>Present only after the change.</summary>
		Added,

		/// <summary>Present only before the change.</summary>
		Removed,
	}

	/// <summary>One line of a diff.</summary>
	/// <remarks>
	/// <see cref="Text"/> carries the content without the leading marker. The widget draws the marker
	/// in its own column, so a caller mapping from a source that keeps it strips it once rather than
	/// leaving the widget to guess whether a line beginning with a hyphen is a removal or content.
	/// </remarks>
	public sealed record DiffLine
	{
		/// <summary>Gets what this line does.</summary>
		public required DiffLineKind Kind { get; init; }

		/// <summary>Gets the line's content, without a leading marker.</summary>
		public required string Text { get; init; }

		/// <summary>Gets the line's number on the old side, or <see langword="null"/> when it has none.</summary>
		public int? OldNumber { get; init; }

		/// <summary>Gets the line's number on the new side, or <see langword="null"/> when it has none.</summary>
		public int? NewNumber { get; init; }
	}

	/// <summary>A run of lines forming one change, with the context around it.</summary>
	public sealed record DiffHunk
	{
		/// <summary>Gets the hunk's lines, in the order they are shown.</summary>
		public required IReadOnlyList<DiffLine> Lines { get; init; }

		/// <summary>Gets what the source names this hunk, which for git is the enclosing function.</summary>
		public string Heading { get; init; } = string.Empty;
	}

	/// <summary>The selected hunk indices that still name a hunk, ascending.</summary>
	/// <remarks>
	/// A selection outlives the diff it was made against. A caller that stages something and re-reads
	/// gets a different set of hunks, and an index that pointed at the third of five means nothing
	/// about the third of two. This is what a caller reads its selection back through, so a stale
	/// index is dropped rather than turned into a hunk that is not there.
	/// </remarks>
	/// <param name="selection">The selected hunk indices.</param>
	/// <param name="hunkCount">How many hunks there are.</param>
	/// <returns>The indices within range, ascending.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="selection"/> is <see langword="null"/>.</exception>
	public static IReadOnlyList<int> SelectedHunks(ISet<int> selection, int hunkCount)
	{
		Ensure.NotNull(selection);

		return [.. selection.Where(index => index >= 0 && index < hunkCount).Order()];
	}
}
