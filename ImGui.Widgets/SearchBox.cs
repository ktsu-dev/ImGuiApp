// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGui.Widgets;

using System;
using System.Collections.Generic;
using System.Linq;

using Hexa.NET.ImGui;

using ktsu.TextFilter;

/// <summary>
/// Options for configuring a <see cref="ImGuiWidgets.SearchBox(ref SearchBoxOptions, ref string)"/>.
/// </summary>
/// <param name="Label">Label for display and id.</param>
/// <param name="Hint">Hint text to display; when <see langword="null"/> a hint is derived from <paramref name="FilterType"/>.</param>
/// <param name="FilterType">Type of filter to use.</param>
/// <param name="MatchOptions">How to match the filter text against each item.</param>
/// <param name="ShowHint">Whether to show the hint text when the input is empty and it fits.</param>
/// <param name="ShowTooltip">Whether to show a tooltip describing the filter when hovered.</param>
/// <param name="ShowContextMenu">Whether to show the right-click context menu for changing filter options.</param>
/// <param name="ReturnAllWhenEmpty">When the filter text is empty, whether the filtering overload returns all items (<see langword="true"/>) or none (<see langword="false"/>).</param>
/// <param name="FullWidth">Whether to stretch the text input to the full available content width.</param>
public record class SearchBoxOptions(
	string Label = "",
	string? Hint = null,
	TextFilterType FilterType = TextFilterType.Glob,
	TextFilterMatchOptions MatchOptions = TextFilterMatchOptions.ByWholeString,
	bool ShowHint = true,
	bool ShowTooltip = true,
	bool ShowContextMenu = true,
	bool ReturnAllWhenEmpty = false,
	bool FullWidth = false
);

/// <summary>
/// Options for configuring a <see cref="ImGuiWidgets.SearchBoxRanked{T}(ref SearchBoxRankedOptions, ref string, System.Collections.Generic.IEnumerable{T}, System.Func{T, string})"/>.
/// </summary>
/// <param name="Label">Label for display and id.</param>
/// <param name="Hint">Hint text to display; when <see langword="null"/> a hint is derived from the fuzzy filter.</param>
/// <param name="ShowHint">Whether to show the hint text when the input is empty and it fits.</param>
/// <param name="ShowTooltip">Whether to show a tooltip describing the filter when hovered.</param>
/// <param name="FullWidth">Whether to stretch the text input to the full available content width.</param>
public record class SearchBoxRankedOptions(
	string Label = "",
	string? Hint = null,
	bool ShowHint = true,
	bool ShowTooltip = true,
	bool FullWidth = false
) : SearchBoxOptions(Label, Hint, TextFilterType.Fuzzy, TextFilterMatchOptions.ByWholeString, ShowHint, ShowTooltip, ShowContextMenu: false, ReturnAllWhenEmpty: true, FullWidth: FullWidth);

public static partial class ImGuiWidgets
{
	/// <summary>
	/// A search box that filters items using ktsu.TextFilter.
	/// </summary>
	/// <param name="options">Options for configuring the search box.</param>
	/// <param name="filterText">Current filter text.</param>
	/// <returns>True if the filter text changed, otherwise false.</returns>
	public static bool SearchBox(ref SearchBoxOptions options, ref string filterText)
	{
		Ensure.NotNull(options);

		string hint = options.Hint ?? (TextFilter.GetHint(options.FilterType) + (options.ShowContextMenu ? "\nRight-click for options" : ""));

		if (options.FullWidth)
		{
			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
		}

		// Only show hint if there's enough width to display it fully
		// The tooltip covers this case when the hint doesn't fit
		float availableWidth = ImGui.CalcItemWidth();
		float hintWidth = ImGui.CalcTextSize(hint).X;
		float framePadding = ImGui.GetStyle().FramePadding.X * 2;
		string displayHint = options.ShowHint && (hintWidth + framePadding) <= availableWidth ? hint : string.Empty;

		bool changed = ImGui.InputTextWithHint(options.Label, displayHint, ref filterText, 256);
		bool isHovered = ImGui.IsItemHovered();
		bool isRightMouseClicked = ImGui.IsMouseClicked(ImGuiMouseButton.Right);

		if (options.ShowTooltip && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
		{
			ImGui.SetTooltip(hint);
		}

		if (options.ShowContextMenu && isHovered && isRightMouseClicked)
		{
			ImGui.OpenPopup(options.Label + "##context");
		}

		if (options.ShowContextMenu && ImGui.BeginPopup(options.Label + "##context"))
		{
			bool isGlob = options.FilterType == TextFilterType.Glob;
			bool isRegex = options.FilterType == TextFilterType.Regex;
			bool isFuzzy = options.FilterType == TextFilterType.Fuzzy;

			bool isWholeString = options.MatchOptions == TextFilterMatchOptions.ByWholeString;
			bool isAllWord = options.MatchOptions == TextFilterMatchOptions.ByWordAll;
			bool isAnyWord = options.MatchOptions == TextFilterMatchOptions.ByWordAny;

			if (ImGui.MenuItem("Glob", "", ref isGlob))
			{
				options = options with { FilterType = TextFilterType.Glob };
			}

			if (ImGui.MenuItem("Regex", "", ref isRegex))
			{
				options = options with { FilterType = TextFilterType.Regex };
			}

			if (ImGui.MenuItem("Fuzzy", "", ref isFuzzy))
			{
				options = options with { FilterType = TextFilterType.Fuzzy };
			}

			ImGui.Separator();

			if (ImGui.MenuItem("Whole String", "", ref isWholeString))
			{
				options = options with { MatchOptions = TextFilterMatchOptions.ByWholeString };
			}

			if (ImGui.MenuItem("All Words", "", ref isAllWord))
			{
				options = options with { MatchOptions = TextFilterMatchOptions.ByWordAll };
			}

			if (ImGui.MenuItem("Any Word", "", ref isAnyWord))
			{
				options = options with { MatchOptions = TextFilterMatchOptions.ByWordAny };
			}

			ImGui.EndPopup();
		}

		return changed;
	}

	/// <summary>
	/// A search box that filters items using ktsu.TextFilter and returns the filtered results.
	/// </summary>
	/// <typeparam name="T">Type of items to filter.</typeparam>
	/// <param name="options">Search box options.</param>
	/// <param name="filterText">Current filter text.</param>
	/// <param name="items">Collection of items to filter.</param>
	/// <param name="selector">Function to extract the string to match against from each item.</param>
	/// <returns>Filtered collection of items. When the filter text is empty, returns all items or none depending on <see cref="SearchBoxOptions.ReturnAllWhenEmpty"/>.</returns>
	public static IEnumerable<T> SearchBox<T>(
		ref SearchBoxOptions options,
		ref string filterText,
		IEnumerable<T> items,
		Func<T, string> selector
	)
	{
		Ensure.NotNull(options);
		Ensure.NotNull(items);
		Ensure.NotNull(selector);

		SearchBox(ref options, ref filterText);

		if (string.IsNullOrWhiteSpace(filterText))
		{
			return options.ReturnAllWhenEmpty ? items : [];
		}

		Dictionary<string, T> keyedItems = items.ToDictionary(selector, item => item);

		return TextFilter
				.Filter(keyedItems.Keys, filterText, options.FilterType, options.MatchOptions)
				.Select(x => keyedItems[x]);
	}

	/// <summary>
	/// A search box that ranks items using a fuzzy filter.
	/// </summary>
	/// <typeparam name="T">Type of items to rank.</typeparam>
	/// <param name="options">Search box options.</param>
	/// <param name="filterText">Current filter text.</param>
	/// <param name="items">Collection of items to rank.</param>
	/// <param name="selector">Function to extract the string to match against from each item.</param>
	/// <returns>Ranked collection of items.</returns>
	public static IEnumerable<T> SearchBoxRanked<T>(
		ref SearchBoxRankedOptions options,
		ref string filterText,
		IEnumerable<T> items,
		Func<T, string> selector
	)
	{
		Ensure.NotNull(options);
		Ensure.NotNull(items);
		Ensure.NotNull(selector);

		// Ranked search has no context menu, so the options are not mutated by the widget.
		SearchBoxOptions baseOptions = options;
		SearchBox(ref baseOptions, ref filterText);

		if (string.IsNullOrWhiteSpace(filterText))
		{
			return items;
		}

		Dictionary<string, T> keyedItems = items.ToDictionary(selector, item => item);
		return TextFilter.Rank(keyedItems.Keys, filterText)
				.Select(x => keyedItems[x]);
	}
}
