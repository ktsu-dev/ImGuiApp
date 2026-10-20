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

	[TestMethod]
	public void Spinner_Draws()
	{
		Start(() => ImGuiWidgets.Spinner(16f, 4f, new Srgb(0.2f, 0.6f, 1f)));

		Assert.IsTrue(IsVisible("spinner"), "The spinner drew nothing.");
		AssertSomethingWasDrawn("the spinner");
	}

	[TestMethod]
	public void Spinner_Animates()
	{
		Start(() => ImGuiWidgets.Spinner(16f, 4f, new Srgb(0.2f, 0.6f, 1f)));

		MoveAway();
		byte[] first = Snapshot();
		Step(20);
		MoveAway();

		Assert.IsTrue(PixelsChangedSince(first) > 0, "The spinner drew the same frame twenty frames apart.");
	}

	[TestMethod]
	public void Spinner_DrawsItsWholeStrokeInsideItsItem()
	{
		bool show = false;

		Start(() =>
		{
			ImGui.SetCursorPosX(40f);
			if (show)
			{
				ImGuiWidgets.Spinner(16f, 6f, new Srgb(0.2f, 0.6f, 1f));
			}
			else
			{
				ImGui.Dummy(Vector2.One);
			}
		});

		MoveAway();
		byte[] blank = Snapshot();
		show = true;

		// The arc turns, so any one frame may leave a side undrawn. Over a run of frames it sweeps
		// every side, and a stroke drawn past the item shows up on whichever frame reaches it.
		int minX = int.MaxValue;
		int maxX = int.MinValue;
		for (int frame = 0; frame < 30; frame++)
		{
			Step(1);
			MoveAway();
			Rectangle drawn = BoundsOfDifference(blank) ?? throw new InvalidOperationException("The spinner drew nothing.");
			minX = Math.Min(minX, drawn.MinX);
			maxX = Math.Max(maxX, drawn.MaxX);
		}

		// Anti-aliasing feathers a stroke by about a pixel either side of its geometry.
		const int Feather = 1;
		Rectangle item = RectOf("spinner");
		Assert.IsTrue(minX >= item.MinX - Feather, $"The spinner drew at x={minX}, left of its item at x={item.MinX}.");
		Assert.IsTrue(maxX <= item.MaxX + Feather, $"The spinner drew to x={maxX}, past its item's right edge at x={item.MaxX}.");
	}

	[TestMethod]
	public void BufferingBar_FillsToItsValue()
	{
		float value = 0.2f;

		Start(() => ImGuiWidgets.BufferingBar(value, new Vector2(200f, 8f), new Srgb(0.1f, 0.1f, 0.1f), new Srgb(0.2f, 0.7f, 0.3f)));

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
		Start(() => ImGuiWidgets.SkeletonLine("headline", 240f, 18f));

		Rectangle rect = RectOf("headline");
		byte[] first = Snapshot();
		Step(20);

		Assert.IsTrue(Math.Abs(rect.Width - 240) <= 2, $"The skeleton line reserved {rect.Width}px of width rather than 240.");
		Assert.IsTrue(PixelsChangedSince(first) > 0, "The skeleton line's shimmer never moved.");
	}

	[TestMethod]
	public void SkeletonRect_ReservesTheSizeItIsGiven()
	{
		Start(() => ImGuiWidgets.SkeletonRect("thumbnail", new Vector2(120f, 90f)));

		Rectangle rect = RectOf("thumbnail");

		Assert.IsTrue(Math.Abs(rect.Width - 120) <= 2, $"The skeleton rectangle reserved {rect.Width}px of width rather than 120.");
		Assert.IsTrue(Math.Abs(rect.Height - 90) <= 2, $"The skeleton rectangle reserved {rect.Height}px of height rather than 90.");
	}

	[TestMethod]
	public void SkeletonCircle_IsSquareAtItsDiameter()
	{
		Start(() => ImGuiWidgets.SkeletonCircle("avatar", 64f));

		Rectangle rect = RectOf("avatar");

		Assert.IsTrue(Math.Abs(rect.Width - 64) <= 2, $"The skeleton circle reserved {rect.Width}px rather than 64.");
		Assert.AreEqual(rect.Width, rect.Height, "The skeleton circle's area was not square.");
	}

	[TestMethod]
	public void SkeletonCircle_ShimmerStaysInsideTheCircle()
	{
		// The band used to be clipped to the circle's bounding square rather than to the circle,
		// so as it passed it painted the square's corners as a bar beside the placeholder.
		const float Diameter = 64f;
		bool show = false;
		Vector2 origin = Vector2.Zero;

		Start(() =>
		{
			origin = ImGui.GetCursorScreenPos();
			if (show)
			{
				ImGuiWidgets.SkeletonCircle("avatar", Diameter);
			}
		});

		byte[] blank = Snapshot();
		show = true;
		Vector2 center = origin + new Vector2(Diameter * 0.5f);

		// A little over one full sweep of the band, checked every few frames.
		for (int frame = 0; frame < 100; frame += 4)
		{
			Step(4);
			Span<byte> now = Harness.Target.Pixels;
			for (int y = (int)origin.Y; y < (int)(origin.Y + Diameter); y++)
			{
				for (int x = (int)origin.X; x < (int)(origin.X + Diameter); x++)
				{
					if (Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) < (Diameter * 0.5f) + 1.5f)
					{
						continue;
					}

					int i = ((y * Harness.Options.Width) + x) * 4;
					Assert.IsTrue(
						now[i] == blank[i] && now[i + 1] == blank[i + 1] && now[i + 2] == blank[i + 2],
						$"The shimmer painted ({x},{y}), outside the circle, on frame {frame}.");
				}
			}
		}
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

	[TestMethod]
	public void Spinner_MarksItself() => _ = AssertMarksItself("spinner", () => ImGuiWidgets.Spinner(16f, 4f, new Srgb(0.2f, 0.6f, 1f)));

	[TestMethod]
	public void BufferingBar_MarksItself() =>
		_ = AssertMarksItself("buffering-bar", () => ImGuiWidgets.BufferingBar(0.5f, new Vector2(200f, 8f), new Srgb(0.1f, 0.1f, 0.1f), new Srgb(0.2f, 0.7f, 0.3f)));

	[TestMethod]
	public void SkeletonLine_MarksItself() => _ = AssertMarksItself("headline", () => ImGuiWidgets.SkeletonLine("headline", 240f, 18f));

	[TestMethod]
	public void SkeletonRect_MarksItself() => _ = AssertMarksItself("thumbnail", () => ImGuiWidgets.SkeletonRect("thumbnail", new Vector2(120f, 90f)));

	[TestMethod]
	public void SkeletonCircle_MarksItself() => _ = AssertMarksItself("avatar", () => ImGuiWidgets.SkeletonCircle("avatar", 64f));

	[TestMethod]
	public void SkeletonLine_OfNoSize_MarksNothing()
	{
		Start(() => ImGuiWidgets.SkeletonRect("nothing", new Vector2(0f, 20f)));

		Assert.IsFalse(IsVisible("nothing"), "A skeleton with no width marked an item it never submitted.");
	}

	[TestMethod]
	public void Spinners_InTheirOwnScopes_ResolveSeparately()
	{
		Start(() =>
		{
			using (new ImGuiWidgets.ScopedId("first"))
			{
				ImGuiWidgets.Spinner(16f, 4f, new Srgb(0.2f, 0.6f, 1f));
			}

			using (new ImGuiWidgets.ScopedId("second"))
			{
				ImGuiWidgets.Spinner(16f, 4f, new Srgb(0.2f, 0.6f, 1f));
			}
		});

		Assert.IsTrue(RectOf("second/spinner").MinY > RectOf("first/spinner").MinY, "Two scoped spinners were not told apart.");
	}

	[TestMethod]
	public void Spinners_WithoutScopes_AreAmbiguous()
	{
		Start(() =>
		{
			ImGuiWidgets.Spinner(16f, 4f, new Srgb(0.2f, 0.6f, 1f));
			ImGuiWidgets.Spinner(16f, 4f, new Srgb(0.2f, 0.6f, 1f));
		});

		Assert.IsTrue(IsVisible("spinner"), "The spinners were not marked.");
		Assert.IsTrue(Harness.Probe.IsAmbiguous("spinner"), "Two unscoped spinners resolved to one.");
	}
}
