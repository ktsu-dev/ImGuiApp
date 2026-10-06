// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the selection and grid arithmetic behind the asset browser. All pure — no ImGui context required.
/// </summary>
[TestClass]
public class AssetBrowserStateTests
{
	[TestMethod]
	public void Click_SelectsOnlyThatItem()
	{
		ImGuiWidgets.AssetBrowserState state = new();
		_ = state.Click(2, ctrl: false, shift: false);

		Assert.IsTrue(state.Click(5, ctrl: false, shift: false));

		AssertSelected(state, 5);
		Assert.AreEqual(5, state.FocusIndex);
		Assert.AreEqual(5, state.AnchorIndex);
		Assert.IsFalse(state.Click(5, ctrl: false, shift: false), "Clicking the only selected item again changes nothing.");
	}

	[TestMethod]
	public void CtrlClick_Toggles()
	{
		ImGuiWidgets.AssetBrowserState state = new();
		_ = state.Click(1, ctrl: false, shift: false);

		Assert.IsTrue(state.Click(4, ctrl: true, shift: false));
		AssertSelected(state, 1, 4);

		Assert.IsTrue(state.Click(1, ctrl: true, shift: false));
		AssertSelected(state, 4);
		Assert.AreEqual(1, state.AnchorIndex, "A Ctrl+click moves the anchor even when it deselects.");
	}

	[TestMethod]
	public void ShiftClick_SelectsTheRangeFromTheAnchor()
	{
		ImGuiWidgets.AssetBrowserState state = new();
		_ = state.Click(3, ctrl: false, shift: false);

		Assert.IsTrue(state.Click(6, ctrl: false, shift: true));
		AssertSelected(state, 3, 4, 5, 6);
		Assert.AreEqual(6, state.FocusIndex);
		Assert.AreEqual(3, state.AnchorIndex);

		// The anchor stays put, so a second Shift+click redraws the range rather than growing it.
		Assert.IsTrue(state.Click(1, ctrl: false, shift: true));
		AssertSelected(state, 1, 2, 3);
	}

	[TestMethod]
	public void ShiftClick_WithNoAnchor_ActsAsAPlainClick()
	{
		ImGuiWidgets.AssetBrowserState state = new();

		Assert.IsTrue(state.Click(4, ctrl: false, shift: true));

		AssertSelected(state, 4);
		Assert.AreEqual(4, state.AnchorIndex);
	}

	[TestMethod]
	public void CtrlShiftClick_AddsTheRange()
	{
		ImGuiWidgets.AssetBrowserState state = new();
		_ = state.Click(0, ctrl: false, shift: false);
		_ = state.Click(5, ctrl: true, shift: false);

		Assert.IsTrue(state.Click(7, ctrl: true, shift: true));

		AssertSelected(state, 0, 5, 6, 7);
	}

	[TestMethod]
	public void ClickOnNothing_Clears()
	{
		ImGuiWidgets.AssetBrowserState state = new();
		_ = state.Click(2, ctrl: false, shift: false);

		Assert.IsTrue(state.Click(-1, ctrl: false, shift: false));

		Assert.IsEmpty(state.SelectedIndices);
		Assert.AreEqual(2, state.FocusIndex, "Clearing keeps the focus, so the arrow keys carry on from it.");
		Assert.IsFalse(state.Click(-1, ctrl: false, shift: false), "Clearing an empty selection changes nothing.");
	}

	[TestMethod]
	public void Move_WithNoFocus_StartsAtTheFirstItem()
	{
		ImGuiWidgets.AssetBrowserState state = new();

		Assert.IsTrue(state.Move(5, 10, shift: false));

		Assert.AreEqual(0, state.FocusIndex);
		AssertSelected(state, 0);
	}

	[TestMethod]
	public void Move_ClampsAndSelectsTheNewFocus()
	{
		ImGuiWidgets.AssetBrowserState state = new();
		_ = state.Click(8, ctrl: false, shift: false);

		Assert.IsTrue(state.Move(4, 10, shift: false));
		Assert.AreEqual(9, state.FocusIndex);
		AssertSelected(state, 9);

		Assert.IsFalse(state.Move(4, 10, shift: false), "A move the clamp cancels out changes nothing.");
	}

	[TestMethod]
	public void ShiftMove_ExtendsFromTheAnchor()
	{
		ImGuiWidgets.AssetBrowserState state = new();
		_ = state.Click(2, ctrl: false, shift: false);

		Assert.IsTrue(state.Move(5, 20, shift: true));

		AssertSelected(state, 2, 3, 4, 5, 6, 7);
		Assert.AreEqual(7, state.FocusIndex);
		Assert.AreEqual(2, state.AnchorIndex);
	}

	[TestMethod]
	public void Move_WithNoItems_DoesNothing()
	{
		ImGuiWidgets.AssetBrowserState state = new();

		Assert.IsFalse(state.Move(1, 0, shift: false));
		Assert.AreEqual(-1, state.FocusIndex);
	}

	[TestMethod]
	public void SelectAll_SelectsEveryItem()
	{
		ImGuiWidgets.AssetBrowserState state = new();

		Assert.IsTrue(state.SelectAll(4));

		AssertSelected(state, 0, 1, 2, 3);
		Assert.AreEqual(0, state.FocusIndex);
		Assert.IsFalse(state.SelectAll(4), "Selecting everything twice changes nothing the second time.");
	}

	[TestMethod]
	public void ItemCountChanged_DropsIndicesPastTheEnd()
	{
		ImGuiWidgets.AssetBrowserState state = new();
		_ = state.Click(1, ctrl: true, shift: false);
		_ = state.Click(5, ctrl: true, shift: false);
		_ = state.Click(8, ctrl: true, shift: false);

		Assert.IsTrue(state.ItemCountChanged(6));

		AssertSelected(state, 1, 5);
		Assert.AreEqual(5, state.FocusIndex);
		Assert.AreEqual(5, state.AnchorIndex);
		Assert.IsFalse(state.ItemCountChanged(6));
	}

	[TestMethod]
	public void ItemCountChanged_ToZero_ClearsTheFocus()
	{
		ImGuiWidgets.AssetBrowserState state = new();
		_ = state.Click(3, ctrl: false, shift: false);

		Assert.IsTrue(state.ItemCountChanged(0));

		Assert.IsEmpty(state.SelectedIndices);
		Assert.AreEqual(-1, state.FocusIndex);
		Assert.AreEqual(-1, state.AnchorIndex);
	}

	[TestMethod]
	public void ColumnCount_FitsWholeTilesWithSpacing()
	{
		Assert.AreEqual(3, ImGuiWidgets.AssetBrowserState.ColumnCount(400f, 96f, 8f));
		Assert.AreEqual(4, ImGuiWidgets.AssetBrowserState.ColumnCount(416f, 96f, 8f), "The last tile needs no gap after it.");
		Assert.AreEqual(1, ImGuiWidgets.AssetBrowserState.ColumnCount(10f, 96f, 8f), "A tile wider than the space still gets a column.");
		Assert.AreEqual(1, ImGuiWidgets.AssetBrowserState.ColumnCount(float.NaN, 96f, 8f));
		Assert.AreEqual(1, ImGuiWidgets.AssetBrowserState.ColumnCount(400f, 0f, 8f));
	}

	[TestMethod]
	public void RowCount_RoundsUp()
	{
		Assert.AreEqual(0, ImGuiWidgets.AssetBrowserState.RowCount(0, 4));
		Assert.AreEqual(1, ImGuiWidgets.AssetBrowserState.RowCount(4, 4));
		Assert.AreEqual(2, ImGuiWidgets.AssetBrowserState.RowCount(5, 4));
		Assert.AreEqual(3, ImGuiWidgets.AssetBrowserState.RowOf(13, 4));
	}

	[TestMethod]
	public void ScrollToReveal_ScrollsOnlyWhenNeeded()
	{
		Assert.AreEqual(0f, ImGuiWidgets.AssetBrowserState.ScrollToReveal(0, 100f, 250f, 300f), "A row above the view scrolls up to its top.");
		Assert.AreEqual(300f, ImGuiWidgets.AssetBrowserState.ScrollToReveal(5, 100f, 0f, 300f), "A row below the view scrolls down to its bottom.");
		Assert.AreEqual(50f, ImGuiWidgets.AssetBrowserState.ScrollToReveal(1, 100f, 50f, 300f), "A visible row does not scroll.");
		Assert.AreEqual(1000f, ImGuiWidgets.AssetBrowserState.ScrollToReveal(2, 500f, 0f, 300f), "A row taller than the view aligns to its top.");
	}

	private static void AssertSelected(ImGuiWidgets.AssetBrowserState state, params int[] expected) =>
		Assert.AreSequenceEqual(expected, state.SelectedIndices);
}
