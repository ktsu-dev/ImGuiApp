// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.examples.ImGuiWidgetsDemo.UITests;

using System;

using ktsu.ImGui.App.Testing;
using ktsu.ImGui.App.Testing.Support;
using ktsu.ImGui.Examples.Widgets;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives the mobile polish sections of the widgets demo: the accordion, the infinite list, the sticky
/// headers and the coach marks. A class of its own because <see cref="WidgetsDemoUITests"/> is at the
/// analyzers' class-coupling limit.
/// </summary>
[TestClass]
public sealed class MobilePolishDemoUITests
{
	private static readonly HarnessOptions DemoViewport = new() { Width = 1600, Height = 1400 };

	private const string WidgetDemosTab = "Widget Demos";

	private ImGuiAppHarness harness = null!;

	/// <summary>Gets or sets the context MSTest supplies for the running test.</summary>
	public TestContext TestContext { get; set; } = null!;

	[TestInitialize]
	public void SetUp()
	{
		ImGuiWidgetsDemo.ResetState();
		harness = ImGuiAppHarness.Start(ImGuiWidgetsDemo.BuildConfig(), DemoViewport);
		harness.Step(3);
	}

	[TestCleanup]
	public void TearDown()
	{
		TestContext.KeepFailureFrame(harness);
		harness?.Dispose();
	}

	private void OpenSection(string header)
	{
		harness.Click(WidgetDemosTab);
		harness.Step();
		harness.Click(header);
		harness.Step();
	}

	[TestMethod]
	public void Accordion_OpensOneQuestionAtATime()
	{
		OpenSection("Mobile - Accordion");

		harness.Click("How long does shipping take?");
		harness.Step(20);
		harness.Click("Can I return an item?");
		harness.Step(20);

		Assert.IsFalse(AccordionDemo.Faq.IsOpen("How long does shipping take?"));
		Assert.IsTrue(AccordionDemo.Faq.IsOpen("Can I return an item?"));
	}

	[TestMethod]
	public void InfiniteList_LoadsThePageAfterTheEnd()
	{
		OpenSection("Mobile - Infinite List");

		Rectangle feed = harness.Probe.Rect("demoFeed")
			?? throw new InvalidOperationException("The feed was never recorded by the probe.");
		for (int i = 0; i < 30 && InfiniteListDemo.Loads == 0; i++)
		{
			harness.Mouse.Wheel(feed.MinX + 40, feed.MinY + 40, -1);
		}

		harness.Step(60);

		Assert.AreEqual(1, InfiniteListDemo.Loads);
		Assert.AreEqual(40, InfiniteListDemo.Count);
	}

	[TestMethod]
	public void StickyHeaders_PinTheSectionScrolledInto()
	{
		OpenSection("Mobile - Sticky Headers");
		Assert.IsNull(StickyHeaderDemo.Pinned);

		Rectangle list = harness.Probe.Rect("demoContacts")
			?? throw new InvalidOperationException("The contacts were never recorded by the probe.");
		harness.Mouse.Wheel(list.MinX + 40, list.MinY + 40, -1);

		Assert.IsNotNull(StickyHeaderDemo.Pinned);
	}

	[TestMethod]
	public void CoachMarks_TourCanBeFinished()
	{
		OpenSection("Mobile - Coach Marks");

		harness.Click("Take the tour");
		harness.Step(20);
		for (int i = 0; i < 3; i++)
		{
			harness.Click("demoTour/next");
			harness.Step(20);
		}

		Assert.AreEqual("Finished", CoachMarkDemo.Outcome);
		Assert.IsFalse(CoachMarkDemo.Tour.IsActive);
	}
}
