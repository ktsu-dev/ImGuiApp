// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Examples.Widgets;

using System.Numerics;
using Hexa.NET.ImGui;
using ktsu.ImGui.Widgets;
using ktsu.TextFilter;

/// <summary>
/// The search boxes: plain, filtered, ranked, glob and regular expression.
/// </summary>
internal static class SearchBoxDemo
{
	// Static fields for SearchBox filter persistence
	private static string BasicSearchTerm = string.Empty;
	private static SearchBoxOptions BasicSearchOptions = new(Label: "##BasicSearch");

	private static string FilteredSearchTerm = string.Empty;
	private static SearchBoxOptions FilteredSearchOptions = new(Label: "##FilteredSearch");

	private static string RankedSearchTerm = string.Empty;
	private static SearchBoxRankedOptions RankedSearchOptions = new(Label: "##RankedSearch");

	private static string GlobSearchTerm = string.Empty;
	private static SearchBoxOptions GlobSearchOptions = new(Label: "##GlobSearch", FilterType: TextFilterType.Glob);

	private static string RegexSearchTerm = string.Empty;
	private static SearchBoxOptions RegexSearchOptions = new(Label: "##RegexSearch", FilterType: TextFilterType.Regex);

	/// <summary>
	/// Returns this section's state to its starting values. The demo keeps its state in statics, which
	/// outlive a harness.
	/// </summary>
	internal static void ResetState()
	{
		BasicSearchTerm = string.Empty;
		FilteredSearchTerm = string.Empty;
		RankedSearchTerm = string.Empty;
		GlobSearchTerm = string.Empty;
		RegexSearchTerm = string.Empty;
	}

	/// <summary>Draws the demo section.</summary>
	internal static void Show()
	{
		if (DemoProbe.Header("SearchBox"))
		{
			ImGui.TextUnformatted("Powerful search functionality with multiple filter types:");
			ImGui.Separator();

			ImGui.TextUnformatted("Basic SearchBox (UI only):");
			ImGuiWidgets.SearchBox(ref BasicSearchOptions, ref BasicSearchTerm);
			ImGui.TextUnformatted($"Search term: '{BasicSearchTerm}' | Type: {BasicSearchOptions.FilterType} | Match: {BasicSearchOptions.MatchOptions}");

			ImGui.Separator();
			ImGui.TextUnformatted("SearchBox with Filtering:");

			// Toggle whether an empty filter returns all items or none
			bool returnAllWhenEmpty = FilteredSearchOptions.ReturnAllWhenEmpty;
			if (DemoProbe.Checkbox("Return all items when the filter is empty", ref returnAllWhenEmpty))
			{
				FilteredSearchOptions = FilteredSearchOptions with { ReturnAllWhenEmpty = returnAllWhenEmpty };
			}

			// Toggle whether the input stretches to the full available content width
			bool fullWidth = FilteredSearchOptions.FullWidth;
			if (DemoProbe.Checkbox("Stretch input to full content width", ref fullWidth))
			{
				FilteredSearchOptions = FilteredSearchOptions with { FullWidth = fullWidth };
			}

			// Using the SearchBox that returns filtered results
			List<string> filteredResults = [.. ImGuiWidgets.SearchBox(
				ref FilteredSearchOptions,
				ref FilteredSearchTerm,
				items: GridDemo.Strings,
				selector: s => s)];

			if (filteredResults.Count > 0)
			{
				string forText = string.IsNullOrEmpty(FilteredSearchTerm) ? "empty filter" : $"'{FilteredSearchTerm}'";
				ImGui.TextUnformatted($"Results: {filteredResults.Count} matches for {forText}");
				ImGui.BeginChild("FilteredResults", new Vector2(0, 100), ImGuiChildFlags.Borders);
				foreach (string item in filteredResults.Take(20))
				{
					ImGui.TextUnformatted($"• {item}");
				}
				if (filteredResults.Count > 20)
				{
					ImGui.TextUnformatted($"... and {filteredResults.Count - 20} more");
				}
				ImGui.EndChild();
			}

			ImGui.Separator();
			ImGui.TextUnformatted("Ranked SearchBox (Fuzzy Matching):");

			List<string> rankedResults = [.. ImGuiWidgets.SearchBoxRanked(
				ref RankedSearchOptions,
				ref RankedSearchTerm,
				items: GridDemo.Strings,
				selector: s => s)];

			if (!string.IsNullOrEmpty(RankedSearchTerm))
			{
				ImGui.TextUnformatted($"Fuzzy Results: {rankedResults.Count} matches for '{RankedSearchTerm}'");
				ImGui.BeginChild("RankedResults", new Vector2(0, 100), ImGuiChildFlags.Borders);
				foreach (string item in rankedResults.Take(20))
				{
					ImGui.TextUnformatted($"• {item}");
				}
				if (rankedResults.Count > 20)
				{
					ImGui.TextUnformatted($"... and {rankedResults.Count - 20} more");
				}
				ImGui.EndChild();
			}

			ImGui.Separator();
			ImGui.TextUnformatted("Filter Type Comparison:");

			ImGui.Columns(2, "SearchComparison");

			ImGui.TextUnformatted("Glob Pattern (*,?):");
			List<string> globResults = [.. ImGuiWidgets.SearchBox(
				ref GlobSearchOptions,
				ref GlobSearchTerm,
				items: GridDemo.Strings,
				selector: s => s)];

			if (!string.IsNullOrEmpty(GlobSearchTerm))
			{
				ImGui.TextUnformatted($"{globResults.Count} matches");
				ImGui.BeginChild("GlobResults", new Vector2(0, 80), ImGuiChildFlags.Borders);
				foreach (string item in globResults.Take(10))
				{
					ImGui.TextUnformatted($"• {item}");
				}
				ImGui.EndChild();
			}
			else
			{
				ImGui.TextUnformatted("Try: *1*, ?:*, [0-9]*");
			}

			ImGui.NextColumn();

			ImGui.TextUnformatted("Regex Pattern:");
			List<string> regexResults = [.. ImGuiWidgets.SearchBox(
				ref RegexSearchOptions,
				ref RegexSearchTerm,
				items: GridDemo.Strings,
				selector: s => s)];

			if (!string.IsNullOrEmpty(RegexSearchTerm))
			{
				ImGui.TextUnformatted($"{regexResults.Count} matches");
				ImGui.BeginChild("RegexResults", new Vector2(0, 80), ImGuiChildFlags.Borders);
				foreach (string item in regexResults.Take(10))
				{
					ImGui.TextUnformatted($"• {item}");
				}
				ImGui.EndChild();
			}
			else
			{
				ImGui.TextUnformatted("Try: ^\\d+, [A-Z]+, .*[aeiou].*");
			}

			ImGui.Columns(1);
		}
	}
}
