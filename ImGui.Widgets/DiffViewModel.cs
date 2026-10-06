// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets;

using System.Collections.Generic;

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
	/// <remarks>
	/// The widget treats a hunk instance as immutable. It lays a hunk out once and keeps that layout,
	/// its side-by-side rows and its change counts, until it is handed a different instance at the
	/// same position, so a caller whose diff has changed builds new hunks rather than editing the
	/// lines of one it already handed over. <see cref="IReadOnlyList{T}"/> does not stop a caller
	/// mutating the list behind it, and a change made that way is not seen.
	/// </remarks>
	public sealed record DiffHunk
	{
		/// <summary>Gets the hunk's lines, in the order they are shown.</summary>
		public required IReadOnlyList<DiffLine> Lines { get; init; }

		/// <summary>Gets what the source names this hunk, which for git is the enclosing function.</summary>
		public string Heading { get; init; } = string.Empty;
	}
}
