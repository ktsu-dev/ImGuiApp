// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <c>ImGuiWidgets.Badge</c> on its own.</summary>
[TestClass]
public sealed class BadgeTests : WidgetTest
{
	private const string Host = "Inbox";

	// A badge decorates the item submitted before it rather than submitting one of its own, so the
	// test draws a host button for it to hang off.
	private void DrawWithBadge(Action badge)
	{
		ImGui.Button(Host, new Vector2(120f, 32f));
		Mark(Host);
		badge();
	}

	[TestMethod]
	public void Badge_DrawsOverTheItemBeforeIt()
	{
		Start(() => DrawWithBadge(() => { }));
		MoveAway();
		byte[] bare = Snapshot();

		DisposeHarness();
		Start(() => DrawWithBadge(() => ImGuiWidgets.Badge(3)));
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(bare) > 0, "The badge drew nothing over its host item.");
	}

	[TestMethod]
	public void Badge_ReservesNoLayoutOfItsOwn()
	{
		Start(() => DrawWithBadge(() => { }));
		Rectangle bare = RectOf(Host);
		DisposeHarness();

		Start(() => DrawWithBadge(() => ImGuiWidgets.Badge(3)));
		Rectangle badged = RectOf(Host);

		Assert.AreEqual(bare, badged, "The badge moved or resized the item it decorates.");
	}

	[TestMethod]
	public void Badge_ZeroCount_DrawsNothing()
	{
		Start(() => DrawWithBadge(() => { }));
		MoveAway();
		byte[] bare = Snapshot();

		DisposeHarness();
		Start(() => DrawWithBadge(() => ImGuiWidgets.Badge(0)));
		MoveAway();

		Assert.AreEqual(0, PixelsChangedSince(bare), "A zero count still drew a badge.");
	}

	[TestMethod]
	public void Badge_OverflowsToTheMaximumForm()
	{
		Assert.AreEqual("", ImGuiWidgets.FormatBadgeCount(0, 99));
		Assert.AreEqual("7", ImGuiWidgets.FormatBadgeCount(7, 99));
		Assert.AreEqual("99+", ImGuiWidgets.FormatBadgeCount(1200, 99));
	}

	[TestMethod]
	public void Badge_LargeCountDrawsWiderThanASmallOne()
	{
		Start(() => DrawWithBadge(() => ImGuiWidgets.Badge(1)));
		MoveAway();
		byte[] single = Snapshot();

		DisposeHarness();
		Start(() => DrawWithBadge(() => ImGuiWidgets.Badge(1200)));
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(single) > 0, "A '99+' badge drew the same as a '1' badge.");
	}

	[TestMethod]
	public void Badge_TextOverload_Draws()
	{
		Start(() => DrawWithBadge(() => { }));
		MoveAway();
		byte[] bare = Snapshot();

		DisposeHarness();
		Start(() => DrawWithBadge(() => ImGuiWidgets.Badge("NEW")));
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(bare) > 0, "A text badge drew nothing.");
	}

	[TestMethod]
	public void BadgeDot_Draws()
	{
		Start(() => DrawWithBadge(() => { }));
		MoveAway();
		byte[] bare = Snapshot();

		DisposeHarness();
		Start(() => DrawWithBadge(ImGuiWidgets.BadgeDot));
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(bare) > 0, "The badge dot drew nothing.");
	}
}
