// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.Carousel"/> on its own.</summary>
[TestClass]
public sealed class CarouselTests : WidgetTest
{
	private const string Id = "photos";
	private const int PageCount = 4;
	private static readonly Vector2 PageSize = new(300f, 160f);
	private static readonly int[] FirstPage = [0];
	private static readonly int[] SecondPage = [1];
	private static readonly int[] ThirdPage = [2];
	private static readonly int[] LastPage = [PageCount - 1];
	private static readonly int[] FirstTwoPages = [0, 1];

	private readonly HashSet<int> drawnThisFrame = [];
	private int page;
	private bool changed;

	private void Draw()
	{
		drawnThisFrame.Clear();
		changed |= ImGuiWidgets.Carousel(Id, ref page, PageCount, PageSize, (index, size) =>
		{
			drawnThisFrame.Add(index);
			ImGui.TextUnformatted($"Page {index + 1}");
		});
	}

	// Drags across the pages. Many small steps make a slow release, judged by distance alone.
	private void DragPages(float pixels, int steps)
	{
		Rectangle rect = RectOf(Id);
		float x = rect.MinX + (rect.Width * 0.5f);
		float y = rect.MinY + (rect.Height * 0.5f);
		Harness.Mouse.Drag(x, y, x + pixels, y, steps);
		Step(60);
	}

	[TestMethod]
	public void Carousel_DrawsOnlyThePageInView()
	{
		Start(Draw);

		Assert.AreEqual(PageSize.X, RectOf(Id).Width, 1f);
		CollectionAssert.AreEquivalent(FirstPage, new List<int>(drawnThisFrame));
		Assert.IsTrue(IsVisible("photos/dots"), "The page dots were not drawn.");
	}

	[TestMethod]
	public void Carousel_SlowDragPastHalfAPage_TurnsThePage()
	{
		Start(Draw);

		DragPages(-180f, 60);

		Assert.AreEqual(1, page);
		Assert.IsTrue(changed);
		CollectionAssert.AreEquivalent(SecondPage, new List<int>(drawnThisFrame), "The carousel did not settle on the new page.");
	}

	[TestMethod]
	public void Carousel_ShortSlowDrag_SpringsBack()
	{
		Start(Draw);

		DragPages(-80f, 60);

		Assert.AreEqual(0, page);
		Assert.IsFalse(changed);
		CollectionAssert.AreEquivalent(FirstPage, new List<int>(drawnThisFrame));
	}

	[TestMethod]
	public void Carousel_QuickFlick_TurnsThePage()
	{
		Start(Draw);

		DragPages(-90f, 4);

		Assert.AreEqual(1, page, "A quick flick shorter than half a page did not turn it.");
	}

	[TestMethod]
	public void Carousel_DraggingBeforeTheFirstPage_SpringsBack()
	{
		Start(Draw);

		DragPages(200f, 60);

		Assert.AreEqual(0, page);
		CollectionAssert.AreEquivalent(FirstPage, new List<int>(drawnThisFrame));
	}

	[TestMethod]
	public void Carousel_ClickingTheLastDot_SlidesToTheLastPage()
	{
		Start(Draw);

		ClickFraction("photos/dots", 0.98f);
		Step(60);

		Assert.AreEqual(PageCount - 1, page);
		Assert.IsTrue(changed);
		CollectionAssert.AreEquivalent(LastPage, new List<int>(drawnThisFrame));
	}

	[TestMethod]
	public void Carousel_PageChangedByTheCaller_SlidesThere()
	{
		Start(Draw);

		page = 2;
		Step(60);

		Assert.IsFalse(changed, "A page set by the caller was reported as a user change.");
		CollectionAssert.AreEquivalent(ThirdPage, new List<int>(drawnThisFrame));
	}

	[TestMethod]
	public void Carousel_MidSlide_DrawsBothPages()
	{
		Start(Draw);
		Rectangle rect = RectOf(Id);
		float y = rect.MinY + (rect.Height * 0.5f);

		Harness.Mouse.MoveTo(rect.MinX + 250f, y);
		Step();
		HarnessMouse.Down(0);
		Step();
		for (int i = 1; i <= 20; i++)
		{
			Harness.Mouse.MoveTo(rect.MinX + 250f - (i * 5f), y);
			Step();
		}

		CollectionAssert.AreEquivalent(FirstTwoPages, new List<int>(drawnThisFrame));
		HarnessMouse.Up(0);
		Step();
	}
}
