// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;
using ktsu.Semantics.Color;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives the loading indicators — <see cref="ImGuiWidgets.Spinner"/>,
/// <see cref="ImGuiWidgets.BufferingBar"/> and the skeleton placeholders — each on its own.
/// </summary>
[TestClass]
public sealed class LoaderTests : WidgetTest
{
	private const string Name = "loader";

	[TestMethod]
	public void Spinner_Draws()
	{
		Start(() =>
		{
			Vector2 origin = ImGui.GetCursorScreenPos();
			ImGuiWidgets.Spinner(16f, 4f, new Srgb(0.2f, 0.6f, 1f));
			MarkSpan(Name, origin);
		});

		Assert.IsTrue(IsVisible(Name), "The spinner drew nothing.");
		AssertSomethingWasDrawn("the spinner");
	}

	[TestMethod]
	public void Spinner_Animates()
	{
		Start(() =>
		{
			Vector2 origin = ImGui.GetCursorScreenPos();
			ImGuiWidgets.Spinner(16f, 4f, new Srgb(0.2f, 0.6f, 1f));
			MarkSpan(Name, origin);
		});

		MoveAway();
		byte[] first = Snapshot();
		Step(20);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(first) > 0, "The spinner drew the same frame twenty frames apart.");
	}

	[TestMethod]
	public void BufferingBar_FillsToItsValue()
	{
		float value = 0.2f;

		Start(() =>
		{
			Vector2 origin = ImGui.GetCursorScreenPos();
			ImGuiWidgets.BufferingBar(value, new Vector2(200f, 8f), new Srgb(0.1f, 0.1f, 0.1f), new Srgb(0.2f, 0.7f, 0.3f));
			MarkSpan(Name, origin);
		});

		MoveAway();
		byte[] nearlyEmpty = Snapshot();

		value = 0.9f;
		Step(2);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(nearlyEmpty) > 0, "A nearly full buffering bar drew the same as a nearly empty one.");
	}

	// The buffering bar paints straight into the draw list without submitting an ImGui item, so
	// there is no reported rectangle to read: where it drew is measured from the pixels.
	[TestMethod]
	public void BufferingBar_DrawsAtTheWidthItIsGiven()
	{
		bool show = false;

		Start(() =>
		{
			if (show)
			{
				ImGuiWidgets.BufferingBar(1f, new Vector2(200f, 8f), new Srgb(0.1f, 0.1f, 0.1f), new Srgb(0.2f, 0.7f, 0.3f));
			}
		});

		MoveAway();
		byte[] blank = Snapshot();
		show = true;
		Step(2);
		MoveAway();

		Rectangle drawn = BoundsOfDifference(blank) ?? throw new InvalidOperationException("The buffering bar drew nothing.");

		// Hexa draws rounded end caps beyond the bar's own width, so the painted region runs a
		// few pixels wider than the size requested.
		Assert.IsTrue(
			drawn.Width is >= 200 and <= 220,
			$"The bar drew {drawn.Width}px wide, not the 200px it was given plus its end caps.");
	}

	[TestMethod]
	public void SkeletonLine_ReservesALineAndShimmers()
	{
		Start(() =>
		{
			ImGuiWidgets.SkeletonLine("headline", 240f, 18f);
			Mark(Name);
		});

		Rectangle rect = RectOf(Name);
		byte[] first = Snapshot();
		Step(20);

		Assert.IsTrue(Math.Abs(rect.Width - 240) <= 2, $"The skeleton line reserved {rect.Width}px of width rather than 240.");
		Assert.IsTrue(PixelsChangedSince(first) > 0, "The skeleton line's shimmer never moved.");
	}

	[TestMethod]
	public void SkeletonRect_ReservesTheSizeItIsGiven()
	{
		Start(() =>
		{
			ImGuiWidgets.SkeletonRect("thumbnail", new Vector2(120f, 90f));
			Mark(Name);
		});

		Rectangle rect = RectOf(Name);

		Assert.IsTrue(Math.Abs(rect.Width - 120) <= 2, $"The skeleton rectangle reserved {rect.Width}px of width rather than 120.");
		Assert.IsTrue(Math.Abs(rect.Height - 90) <= 2, $"The skeleton rectangle reserved {rect.Height}px of height rather than 90.");
	}

	[TestMethod]
	public void SkeletonCircle_IsSquareAtItsDiameter()
	{
		Start(() =>
		{
			ImGuiWidgets.SkeletonCircle("avatar", 64f);
			Mark(Name);
		});

		Rectangle rect = RectOf(Name);

		Assert.IsTrue(Math.Abs(rect.Width - 64) <= 2, $"The skeleton circle reserved {rect.Width}px rather than 64.");
		Assert.AreEqual(rect.Width, rect.Height, "The skeleton circle's area was not square.");
	}

	[TestMethod]
	public void Skeleton_ZeroSize_DrawsNothing()
	{
		// A zero-sized placeholder returns before submitting anything, so there is no item to
		// measure and the question is whether any pixel changed at all.
		bool show = false;

		Start(() =>
		{
			if (show)
			{
				ImGuiWidgets.SkeletonRect("empty", Vector2.Zero);
			}
		});

		byte[] blank = Snapshot();
		show = true;
		Step(2);

		Assert.IsNull(BoundsOfDifference(blank), "A zero-sized skeleton drew something.");
	}
}
