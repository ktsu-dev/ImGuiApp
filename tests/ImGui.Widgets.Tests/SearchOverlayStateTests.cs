// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class SearchOverlayStateTests
{
	[TestMethod]
	public void Open_StartsWithAnEmptyQueryAndNoHighlight()
	{
		SearchOverlayState state = new() { Query = "old" };
		state.Open();

		Assert.AreEqual(string.Empty, state.Query);
		Assert.AreEqual(-1, state.Highlighted);
		Assert.IsTrue(state.IsOpen);
	}

	[TestMethod]
	public void ResultsChanged_HighlightsTheBestMatchOnceSomethingIsTyped()
	{
		SearchOverlayState state = new();
		state.Open();
		state.ResultsChanged(5);
		Assert.AreEqual(-1, state.Highlighted);

		state.Query = "a";
		state.ResultsChanged(3);
		Assert.AreEqual(0, state.Highlighted);
	}

	[TestMethod]
	public void ResultsChanged_KeepsTheHighlightInRange()
	{
		SearchOverlayState state = new();
		state.Open();
		state.MoveHighlight(4, 5);
		state.MoveHighlight(4, 5);

		state.ResultsChanged(2);
		Assert.AreEqual(1, state.Highlighted);

		state.ResultsChanged(0);
		Assert.AreEqual(-1, state.Highlighted);
	}

	[TestMethod]
	public void MoveHighlight_StopsAtEitherEnd()
	{
		SearchOverlayState state = new();
		state.Open();

		state.MoveHighlight(1, 3);
		Assert.AreEqual(0, state.Highlighted);
		state.MoveHighlight(10, 3);
		Assert.AreEqual(2, state.Highlighted);
		state.MoveHighlight(-10, 3);
		Assert.AreEqual(0, state.Highlighted);
	}

	[TestMethod]
	public void MoveHighlight_Up_FromNothing_StartsAtTheBottom()
	{
		SearchOverlayState state = new();
		state.Open();

		state.MoveHighlight(-1, 4);

		Assert.AreEqual(3, state.Highlighted);
	}
}
