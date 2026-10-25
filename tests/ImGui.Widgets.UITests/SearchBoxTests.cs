// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <c>ImGuiWidgets.SearchBox</c> on its own.</summary>
[TestClass]
public sealed class SearchBoxTests : WidgetTest
{
	private const string Label = "Filter";

	private static readonly string[] Items = ["apple", "apricot", "banana", "blueberry"];

	private static readonly string[] ItemsStartingWithAp = ["apple", "apricot"];

	private SearchBoxOptions options = new(Label);
	private string filterText = string.Empty;
	private List<string> matches = [];

	private void DrawPlain() => ImGuiWidgets.SearchBox(ref options, ref filterText);

	private void DrawFiltering() =>
		matches = [.. ImGuiWidgets.SearchBox(ref options, ref filterText, Items, item => item)];

	private void DrawRanked()
	{
		SearchBoxRankedOptions ranked = new(Label);
		matches = [.. ImGuiWidgets.SearchBoxRanked(ref ranked, ref filterText, Items, item => item)];
	}

	[TestMethod]
	public void SearchBox_IsDrawnAndMarksItself()
	{
		Start(DrawPlain);

		Assert.IsTrue(IsVisible(Label), "The search box marked no probe item.");
		AssertSomethingWasDrawn("the search box");
	}

	[TestMethod]
	public void SearchBox_TypingUpdatesTheFilterText()
	{
		Start(DrawPlain);

		Click(Label);
		Harness.Keyboard.Type("ap");
		Step(2);

		Assert.AreEqual("ap", filterText, "Typing did not reach the search box.");
	}

	// Issue #553: the box had a fixed 256-byte buffer, so activating it over a longer filter aborted
	// the process, and a filter of 255 bytes could not be typed into.
	[TestMethod]
	[DataRow(600, 'a', DisplayName = "600 ASCII characters")]
	[DataRow(100, '\u6F22', DisplayName = "100 CJK characters")]
	[DataRow(255, 'a', DisplayName = "Exactly the old capacity")]
	public void SearchBox_EditsAFilterLongerThanTheOldBuffer(int length, char filler)
	{
		string original = new(filler, length);
		filterText = original;
		Start(DrawPlain);

		Click(Label);
		Harness.Keyboard.Press(ImGuiKey.End);
		Harness.Keyboard.Type("z");
		Step(2);

		Assert.AreEqual(original + "z", filterText);
	}

	[TestMethod]
	public void SearchBox_TakesAPasteLargerThanItsBuffer()
	{
		Start(DrawPlain);
		Click(Label);

		// Many characters in one frame, as a paste delivers them, so the buffer has to grow mid-edit.
		string pasted = new('p', 5000);
		ImGui.GetIO().AddInputCharactersUTF8(pasted);
		Step(2);

		Assert.AreEqual(pasted, filterText);
	}

	[TestMethod]
	public void SearchBox_EmptyFilter_ReturnsNothingByDefault()
	{
		Start(DrawFiltering);

		Assert.AreEqual(0, matches.Count, "An empty filter returned items, but this box does not return all when empty.");
	}

	[TestMethod]
	public void SearchBox_EmptyFilter_ReturnsEverythingWhenAsked()
	{
		options = new SearchBoxOptions(Label, ReturnAllWhenEmpty: true);
		Start(DrawFiltering);

		Assert.AreEqual(Items.Length, matches.Count, "An empty filter did not return every item.");
	}

	[TestMethod]
	public void SearchBox_GlobFilter_NarrowsTheResults()
	{
		Start(DrawFiltering);

		Click(Label);
		Harness.Keyboard.Type("ap*");
		Step(2);

		CollectionAssert.AreEquivalent(
			ItemsStartingWithAp,
			matches,
			$"The glob 'ap*' matched [{string.Join(", ", matches)}].");
	}

	[TestMethod]
	public void SearchBox_Ranked_OrdersFuzzyMatchesFirst()
	{
		Start(DrawRanked);

		Click(Label);
		Harness.Keyboard.Type("bl");
		Step(2);

		Assert.IsTrue(matches.Count > 0, "A fuzzy search for 'bl' matched nothing.");
		Assert.AreEqual("blueberry", matches.First(), $"The best fuzzy match for 'bl' was '{matches.First()}'.");
	}

	[TestMethod]
	public void SearchBox_FullWidth_StretchesAcrossTheWindow()
	{
		options = new SearchBoxOptions(Label);
		Start(DrawPlain);
		int naturalWidth = RectOf(Label).Width;
		DisposeHarness();

		options = new SearchBoxOptions(Label, FullWidth: true);
		Start(DrawPlain);
		int fullWidth = RectOf(Label).Width;

		Assert.IsTrue(
			fullWidth > naturalWidth,
			$"A full-width search box was {fullWidth}px, no wider than the default {naturalWidth}px.");
	}

	// The tooltip draws the hint as it is: "%%" is not collapsed to "%" and "%s" reads no vararg. A frame that
	// draws the hint in a tooltip by hand, with the pointer in the same place, must match it pixel for pixel.
	[TestMethod]
	[DataRow("100%% of rows")]
	[DataRow("Name: %s")]
	public void SearchBox_TooltipShowsTheHintAsItIs(string hint)
	{
		options = new SearchBoxOptions(Label, hint, ShowHint: false, ShowTooltip: false);
		Start(() =>
		{
			ImGuiWidgets.SearchBox(ref options, ref filterText);
			ImGui.BeginTooltip();
			ImGui.TextUnformatted(hint);
			ImGui.EndTooltip();
		});
		Vector2 pointer = CenterOf(Label);
		Harness.Mouse.MoveTo(pointer.X, pointer.Y);
		Step(4);
		byte[] expected = Snapshot();
		DisposeHarness();

		options = new SearchBoxOptions(Label, hint, ShowHint: false);
		Start(DrawPlain);
		Harness.Mouse.MoveTo(pointer.X, pointer.Y);
		Step(4);

		Assert.AreEqual(0, PixelsChangedSince(expected), "The tooltip did not show the hint exactly as given.");
	}
}
