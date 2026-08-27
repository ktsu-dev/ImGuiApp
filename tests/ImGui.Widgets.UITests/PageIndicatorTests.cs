// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.PageIndicator"/> on its own.</summary>
[TestClass]
public sealed class PageIndicatorTests : WidgetTest
{
	private const string Id = "carousel";
	private const string Span = "pages";
	private const int PageCount = 4;

	private int currentPage;
	private bool interactive;

	private void Draw()
	{
		Vector2 origin = ImGui.GetCursorScreenPos();
		currentPage = ImGuiWidgets.PageIndicator(Id, currentPage, PageCount, interactive);
		MarkSpan(Span, origin);
	}

	[TestMethod]
	public void PageIndicator_DrawsARowOfDots()
	{
		Start(Draw);

		Rectangle rect = RectOf(Span);

		Assert.IsTrue(IsVisible(Span), "The page indicator drew nothing.");
		Assert.IsTrue(rect.Width > rect.Height, $"A row of {PageCount} dots was {rect.Width}x{rect.Height}, which is not a row.");
		AssertSomethingWasDrawn("the page indicator");
	}

	[TestMethod]
	public void PageIndicator_NotInteractive_IgnoresClicks()
	{
		currentPage = 0;
		Start(Draw);

		ClickFraction(Span, 0.85f);

		Assert.AreEqual(0, currentPage, "A non-interactive page indicator changed page when clicked.");
	}

	// The dots are spaced with gaps between their hit areas and the active one is drawn larger, so
	// the row is not divided into equal clickable columns. What does hold is that the first dot
	// starts at the left edge of the row and the last one ends at the right edge, which is what
	// these two aim at.
	[TestMethod]
	public void PageIndicator_Interactive_SelectsTheLastDot()
	{
		currentPage = 0;
		interactive = true;
		Start(Draw);

		ClickFraction(Span, 0.98f);

		Assert.AreEqual(PageCount - 1, currentPage, "Clicking the last dot did not select the last page.");
	}

	[TestMethod]
	public void PageIndicator_Interactive_SelectsTheFirstDot()
	{
		currentPage = PageCount - 1;
		interactive = true;
		Start(Draw);

		ClickFraction(Span, 0.02f);

		Assert.AreEqual(0, currentPage, "Clicking the first dot did not select the first page.");
	}

	[TestMethod]
	public void PageIndicator_HighlightsTheCurrentPage()
	{
		currentPage = 0;
		Start(Draw);
		MoveAway();
		byte[] onFirst = Snapshot();

		currentPage = PageCount - 1;
		Step(2);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(onFirst) > 0, "The highlighted dot did not move with the current page.");
	}

	[TestMethod]
	public void PageIndicator_ClampsAnOutOfRangePage()
	{
		currentPage = 99;
		Start(Draw);

		Assert.AreEqual(PageCount - 1, currentPage, "An out-of-range page was not clamped.");
	}
}
