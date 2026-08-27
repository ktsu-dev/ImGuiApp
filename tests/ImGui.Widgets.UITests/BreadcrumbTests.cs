// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System.Numerics;

using Hexa.NET.ImGui;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.Breadcrumb"/>, the Hexa-backed path trail, on its own.</summary>
[TestClass]
public sealed class BreadcrumbTests : WidgetTest
{
	private const string Id = "trail";
	private const string Span = "trail-span";

	private string path = "home/projects/imgui/widgets";
	private bool changed;

	private void Draw()
	{
		Vector2 origin = ImGui.GetCursorScreenPos();
		changed |= ImGuiWidgets.Breadcrumb(Id, ref path);
		MarkSpan(Span, origin);
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
		int shortWidth = RectOf(Span).Width;

		path = "home/projects/imgui/widgets";
		Step(2);
		int longWidth = RectOf(Span).Width;

		Assert.IsTrue(longWidth > shortWidth, $"A four-segment path ({longWidth}px) was no wider than a one-segment path ({shortWidth}px).");
	}

	[TestMethod]
	public void Breadcrumb_AcceptsBackslashSeparators()
	{
		path = @"C:\users\ktsu";
		Start(Draw);

		Assert.IsTrue(IsVisible(Span), "A backslash-separated path drew nothing.");
		AssertSomethingWasDrawn("a backslash-separated breadcrumb");
	}
}
