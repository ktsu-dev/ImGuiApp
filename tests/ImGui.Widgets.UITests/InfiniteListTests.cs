// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.InfiniteList"/> on its own.</summary>
[TestClass]
public sealed class InfiniteListTests : WidgetTest
{
	private const string Id = "feed";

	private int count;
	private int loads;
	private bool loading;
	private bool hasMore = true;
	private bool loadAtOnce = true;

	private void Draw() =>
		ImGuiWidgets.InfiniteList(Id, new Vector2(300f, 300f), count, i => ImGui.TextUnformatted($"Post {i}"), LoadMore, loading, hasMore);

	private void LoadMore()
	{
		loads++;
		if (loadAtOnce)
		{
			count += 10;
		}
		else
		{
			loading = true;
		}
	}

	[TestMethod]
	public void AnEmptyList_FillsItsView_ThenStops()
	{
		Start(Draw);
		Step(10);

		Assert.IsTrue(count * ImGui.GetTextLineHeightWithSpacing() > 300f, $"Only {count} items were loaded.");
		int filled = loads;
		Step(10);

		Assert.AreEqual(filled, loads);
	}

	[TestMethod]
	public void ScrollingToTheEnd_AsksForMoreOnce()
	{
		count = 60;
		Start(Draw);
		Assert.AreEqual(0, loads);

		loadAtOnce = false;
		Rectangle list = RectOf(Id);
		for (int i = 0; i < 40 && loads == 0; i++)
		{
			Harness.Mouse.Wheel(list.MinX + 50, list.MinY + 50, -1);
		}

		Assert.AreEqual(1, loads);
		Step(10);
		Assert.AreEqual(1, loads);
	}

	[TestMethod]
	public void WhileLoading_ShowsTheLoadingRow_AndAsksNoMore()
	{
		loadAtOnce = false;
		Start(Draw);
		Step(10);

		Assert.AreEqual(1, loads);
		Assert.IsTrue(IsVisible($"{Id}/loading"));
	}

	[TestMethod]
	public void WithNothingMore_NeverAsks()
	{
		hasMore = false;
		Start(Draw);
		Step(10);

		Assert.AreEqual(0, loads);
	}
}
