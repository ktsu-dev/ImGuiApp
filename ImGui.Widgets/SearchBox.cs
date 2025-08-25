// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImGuiWidgets;

using System;
using System.Collections.Generic;
using System.Linq;

using Hexa.NET.ImGui;

using ktsu.TextFilter;

public static partial class ImGuiWidgets
{
	/// <summary>
	/// A search box that filters items using ktsu.TextFilter.
	/// </summary>
	/// <param name="label">Label for display and id.</param>
	/// <param name="filterText">Current filter text.</param>
	/// <param name="matchOptions">How to match the filter text (default: WholeString).</param>
	/// <param name="filterType">Type of filter to use (default: Glob).</param>
	/// <returns>True if the filter text changed, otherwise false.</returns>
	public static bool SearchBox(
		string label,
		ref string filterText,
		ref TextFilterType filterType,
		ref TextFilterMatchOptions matchOptions
	)
	{
		string hint = TextFilter.GetHint(filterType) + "\nRight-click for options";

		bool changed = ImGui.InputTextWithHint(label, hint, ref filterText, 256);
		bool isHovered = ImGui.IsItemHovered();
		bool isRightMouseClicked = ImGui.IsMouseClicked(ImGuiMouseButton.Right);

		if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
		{
			ImGui.SetTooltip(hint);
		}

		if (isHovered && isRightMouseClicked)
		{
			ImGui.OpenPopup(label + "##context");
		}

		if (ImGui.BeginPopup(label + "##context"))
		{
			bool isGlob = filterType == TextFilterType.Glob;
			bool isRegex = filterType == TextFilterType.Regex;
			bool isFuzzy = filterType == TextFilterType.Fuzzy;

			bool isWholeString = matchOptions == TextFilterMatchOptions.ByWholeString;
			bool isAllWord = matchOptions == TextFilterMatchOptions.ByWordAll;
			bool isAnyWord = matchOptions == TextFilterMatchOptions.ByWordAny;

			if (ImGui.MenuItem("Glob", "", ref isGlob))
			{
				filterType = TextFilterType.Glob;
			}

			if (ImGui.MenuItem("Regex", "", ref isRegex))
			{
				filterType = TextFilterType.Regex;
			}

			if (ImGui.MenuItem("Fuzzy", "", ref isFuzzy))
			{
				filterType = TextFilterType.Fuzzy;
			}

			ImGui.Separator();

			if (ImGui.MenuItem("Whole String", "", ref isWholeString))
			{
				matchOptions = TextFilterMatchOptions.ByWholeString;
			}

			if (ImGui.MenuItem("All Words", "", ref isAllWord))
			{
				matchOptions = TextFilterMatchOptions.ByWordAll;
			}

			if (ImGui.MenuItem("Any Word", "", ref isAnyWord))
			{
				matchOptions = TextFilterMatchOptions.ByWordAny;
			}

			ImGui.EndPopup();
		}

		return changed;
	}

	/// <summary>
	/// A search box that filters items using ktsu.TextFilter and returns the filtered results.
	/// </summary>
	/// <typeparam name="T">Type of items to filter.</typeparam>
	/// <param name="label">Label for display and id.</param>
	/// <param name="filterText">Current filter text.</param>
	/// <param name="items">Collection of items to filter.</param>
	/// <param name="selector">Function to extract the string to match against from each item.</param>
	/// <param name="matchOptions">How to match the filter text (default: WholeString).</param>
	/// <param name="filterType">Type of filter to use (default: Glob).</param>
	/// <returns>Filtered collection of items.</returns>
	public static IEnumerable<T> SearchBox<T>(
		string label,
		ref string filterText,
		IEnumerable<T> items,
		Func<T, string> selector,
		ref TextFilterType filterType,
		ref TextFilterMatchOptions matchOptions
	)
	{
		ArgumentNullException.ThrowIfNull(items);
		ArgumentNullException.ThrowIfNull(selector);

		SearchBox(label, ref filterText, ref filterType, ref matchOptions);

		if (string.IsNullOrWhiteSpace(filterText))
		{
			return [];
		}

		Dictionary<string, T> keyedItems = items.ToDictionary(selector, item => item);

		return TextFilter.Filter(keyedItems.Keys, filterText, filterType, matchOptions)
				.Select(x => keyedItems[x]);
	}

	/// <summary>
	/// A search box that ranks items using a fuzzy filter.
	/// </summary>
	/// <typeparam name="T">Type of items to rank.</typeparam>
	/// <param name="label">Label for display and id.</param>
	/// <param name="filterText">Current filter text.</param>
	/// <param name="items">Collection of items to rank.</param>
	/// <param name="selector">Function to extract the string to match against from each item.</param>
	/// <returns>Ranked collection of items.</returns>
	public static IEnumerable<T> SearchBoxRanked<T>(
		string label,
		ref string filterText,
		IEnumerable<T> items,
		Func<T, string> selector
	)
	{
		ArgumentNullException.ThrowIfNull(items);
		ArgumentNullException.ThrowIfNull(selector);

		TextFilterType filterType = TextFilterType.Fuzzy;
		TextFilterMatchOptions matchOptions = TextFilterMatchOptions.ByWholeString;
		SearchBox(label, ref filterText, ref filterType, ref matchOptions);

		if (string.IsNullOrWhiteSpace(filterText))
		{
			return items;
		}

		Dictionary<string, T> keyedItems = items.ToDictionary(selector, item => item);
		return TextFilter.Rank(keyedItems.Keys, filterText)
				.Select(x => keyedItems[x]);
	}
}
