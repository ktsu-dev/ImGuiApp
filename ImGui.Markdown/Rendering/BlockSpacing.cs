// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Markdown;

using Markdig.Syntax;

/// <summary>Pure decisions about where the block renderer emits paragraph spacing.</summary>
internal static class BlockSpacing
{
	/// <summary>Determines whether a paragraph is followed by paragraph spacing.</summary>
	/// <remarks>
	/// CommonMark calls a list tight when no blank line separates its items. Markdig still wraps
	/// each item's text in a <see cref="ParagraphBlock"/>, so spacing those the way a standalone
	/// paragraph is spaced would leave a blank line between every item of a tight list.
	/// </remarks>
	/// <param name="paragraph">The paragraph being rendered.</param>
	/// <returns><see langword="true"/> when trailing spacing should be emitted.</returns>
	public static bool ShouldSpaceAfter(ParagraphBlock paragraph)
	{
		Ensure.NotNull(paragraph);
		return paragraph.Parent is not ListItemBlock item
			|| item.Parent is not ListBlock list
			|| list.IsLoose;
	}

	/// <summary>Determines whether a list is followed by paragraph spacing.</summary>
	/// <remarks>
	/// A loose list already ends with the trailing spacing of its last item's paragraph, and a
	/// nested list is followed by more items of the list containing it, which supply their own.
	/// </remarks>
	/// <param name="list">The list being rendered.</param>
	/// <returns><see langword="true"/> when trailing spacing should be emitted.</returns>
	public static bool ShouldSpaceAfter(ListBlock list)
	{
		Ensure.NotNull(list);
		return !list.IsLoose && list.Parent is not ListItemBlock;
	}
}
