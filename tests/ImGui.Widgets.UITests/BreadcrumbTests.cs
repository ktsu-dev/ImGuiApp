// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.Breadcrumb"/>, the Hexa-backed path trail, on its own.</summary>
[TestClass]
public sealed class BreadcrumbTests : WidgetTest
{
	private const string Id = "trail";
	private const string Span = Id;

	private string path = "home/projects/imgui/widgets";
	private bool changed;

	private void Draw()
	{
		changed |= ImGuiWidgets.Breadcrumb(Id, ref path);
	}

	[TestMethod]
	public void Breadcrumb_DrawsTheTrail()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Span), "The breadcrumb drew nothing.");
		AssertSomethingWasDrawn("the breadcrumb");
	}

	[TestMethod]
	public void Breadcrumb_ClickingAnEarlierSegmentTruncatesThePath()
	{
		path = "home/projects/imgui/widgets";
		Start(Draw);

		// The first segment sits at the left-hand end of the trail.
		ClickFraction(Span, 0.05f);

		Assert.IsTrue(changed, "Clicking a segment reported no change.");
		// The trailing separator is Hexa's: it truncates to the end of the clicked segment,
		// separator included, rather than stripping it.
		Assert.AreEqual("home/", path, $"Clicking the first segment left the path as '{path}'.");
	}

	[TestMethod]
	public void Breadcrumb_LeftAlone_KeepsThePath()
	{
		path = "home/projects/imgui/widgets";
		Start(Draw);

		Step(3);

		Assert.AreEqual("home/projects/imgui/widgets", path);
		Assert.IsFalse(changed, "The breadcrumb reported a change nobody made.");
	}

	[TestMethod]
	public void Breadcrumb_LongerPathDrawsWider()
	{
		path = "home";
		Start(Draw);
		MoveAway();
		byte[] oneSegment = Snapshot();

		path = "home/projects/imgui/widgets";
		Step(2);
		MoveAway();

		// The trail draws a frame of its own width whatever the path, so it is the segments drawn
		// inside it that have to reach further along. "home" alone is about forty pixels wide.
		Rectangle trail = RectOf(Span);
		Rectangle grew = BoundsOfDifference(oneSegment) ?? throw new AssertFailedException("A four-segment path drew the same as a one-segment path.");
		Assert.IsTrue(grew.MaxX - trail.MinX > 100, $"A four-segment path changed the trail only up to x={grew.MaxX}, from its left edge at x={trail.MinX}.");
	}

	[TestMethod]
	public void Breadcrumb_AcceptsBackslashSeparators()
	{
		path = @"C:\users\ktsu";
		Start(Draw);

		Assert.IsTrue(IsVisible(Span), "A backslash-separated path drew nothing.");
		AssertSomethingWasDrawn("a backslash-separated breadcrumb");
	}

	[TestMethod]
	public void Breadcrumb_MarksItself() => _ = AssertMarksItself(Id, Draw);
}
