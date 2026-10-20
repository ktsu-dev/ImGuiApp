// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.AppBar"/> on its own.</summary>
[TestClass]
public sealed class AppBarTests : WidgetTest
{
	private const string Id = "bar";

	private static readonly AppBarOptions Options = new()
	{
		Leading = new AppBarAction("<", "Back"),
		Actions = [new AppBarAction("S", "Search"), new AppBarAction("M", "More")],
	};

	private AppBarResult last = AppBarResult.None;
	private int leading;
	private int action = -1;
	private float scroll;

	private void Draw()
	{
		last = ImGuiWidgets.AppBar(Id, "Inbox", Options);
		Record();
	}

	private void Record()
	{
		if (last.LeadingClicked)
		{
			leading++;
		}

		if (last.ActionClicked >= 0)
		{
			action = last.ActionClicked;
		}
	}

	private void DrawCollapsing()
	{
		last = ImGuiWidgets.AppBar(Id, "Feed", Options with { CollapseOnScroll = true }, scroll);
		Record();
		if (ImGui.BeginChild("##feed", Vector2.Zero))
		{
			for (int i = 0; i < 100; i++)
			{
				ImGui.TextUnformatted($"Post {i}");
			}

			scroll = ImGui.GetScrollY();
		}

		ImGui.EndChild();
		Mark("feed");
	}

	[TestMethod]
	public void LeadingButton_IsReported()
	{
		Start(Draw);

		Click($"{Id}/Back");

		Assert.AreEqual(1, leading);
		Assert.AreEqual(-1, action);
	}

	[TestMethod]
	public void TrailingButtons_AreReportedByIndex()
	{
		Start(Draw);

		Click($"{Id}/More");

		Assert.AreEqual(1, action);
		Assert.AreEqual(0, leading);
	}

	[TestMethod]
	public void Bar_SpansTheWindow_WithTheActionsAtTheEnd()
	{
		Start(Draw);

		Rectangle bar = RectOf(Id);
		Assert.AreEqual(56f, bar.Height, 1f);
		Assert.IsTrue(RectOf($"{Id}/Back").MinX < RectOf($"{Id}/Search").MinX);
		Assert.IsTrue(RectOf($"{Id}/More").MaxX > bar.MaxX - 10f);
	}

	[TestMethod]
	public void CollapsingBar_SlidesAwayAsTheContentScrollsDown_AndBack()
	{
		Start(DrawCollapsing);
		Vector2 feed = CenterOf("feed");

		Harness.Mouse.Wheel(feed.X, feed.Y, -10);
		Step(30);
		Assert.IsTrue(RectOf(Id).Height < 1f, $"The bar was still {RectOf(Id).Height} tall.");

		Harness.Mouse.Wheel(feed.X, feed.Y, 1);
		Step(30);
		Assert.IsTrue(RectOf(Id).Height > 10f, "Scrolling back up should bring the bar back.");
	}

	[TestMethod]
	public void CollapsingBar_IsFullyShownAtTheTop()
	{
		Start(DrawCollapsing);
		Vector2 feed = CenterOf("feed");

		Harness.Mouse.Wheel(feed.X, feed.Y, -10);
		Step(30);
		Harness.Mouse.Wheel(feed.X, feed.Y, 20);
		Step(30);

		Assert.AreEqual(56f, RectOf(Id).Height, 1f);
	}
}
