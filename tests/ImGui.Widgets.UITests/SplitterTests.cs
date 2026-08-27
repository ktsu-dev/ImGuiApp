// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives the Hexa-backed <see cref="ImGuiWidgets.VerticalSplitter"/> and
/// <see cref="ImGuiWidgets.HorizontalSplitter"/> on their own.
/// </summary>
[TestClass]
public sealed class SplitterTests : WidgetTest
{
	private const string Span = "splitter";

	private float width = 200f;
	private float height = 150f;
	private bool dragging;

	private void DrawVertical()
	{
		Vector2 origin = ImGui.GetCursorScreenPos();
		dragging |= ImGuiWidgets.VerticalSplitter("v-split", ref width, 80f, 400f, 200f);
		MarkSpan(Span, origin);
	}

	private void DrawHorizontal()
	{
		Vector2 origin = ImGui.GetCursorScreenPos();
		dragging |= ImGuiWidgets.HorizontalSplitter("h-split", ref height, 60f, 300f, 240f);
		MarkSpan(Span, origin);
	}

	[TestMethod]
	public void VerticalSplitter_DrawsABar()
	{
		Start(DrawVertical);

		Assert.IsTrue(IsVisible(Span), "The splitter drew nothing.");
		AssertSomethingWasDrawn("the vertical splitter");
	}

	[TestMethod]
	public void VerticalSplitter_DraggingRightWidensTheZone()
	{
		width = 200f;
		Start(DrawVertical);

		Vector2 center = CenterOf(Span);
		Harness.Mouse.Drag(center.X, center.Y, center.X + 80f, center.Y);
		Step();

		Assert.IsTrue(width > 200f, $"Dragging right left the width at {width}.");
		Assert.IsTrue(dragging, "The splitter never reported being grabbed.");
	}

	[TestMethod]
	public void VerticalSplitter_StopsAtItsMaximum()
	{
		width = 200f;
		Start(DrawVertical);

		Vector2 center = CenterOf(Span);
		Harness.Mouse.Drag(center.X, center.Y, center.X + 600f, center.Y);
		Step();

		Assert.IsTrue(width <= 400f, $"The splitter ran past its 400px maximum to {width}.");
	}

	[TestMethod]
	public void VerticalSplitter_StopsAtItsMinimum()
	{
		width = 200f;
		Start(DrawVertical);

		Vector2 center = CenterOf(Span);
		Harness.Mouse.Drag(center.X, center.Y, center.X - 600f, center.Y);
		Step();

		Assert.IsTrue(width >= 80f, $"The splitter ran past its 80px minimum to {width}.");
	}

	[TestMethod]
	public void VerticalSplitter_RejectsAMinimumAboveItsMaximum()
	{
		float ignored = 100f;

		Start(() => { });

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
		{
			float local = ignored;
			ImGuiWidgets.VerticalSplitter("bad", ref local, 300f, 100f);
		});
	}

	// Hexa's horizontal splitter tracks the region below the bar, so dragging the bar down makes
	// that region smaller and dragging it up makes it larger -- the opposite sign from the
	// vertical splitter, which tracks the region to its left.
	[TestMethod]
	public void HorizontalSplitter_DraggingDownShrinksTheZone()
	{
		height = 150f;
		Start(DrawHorizontal);

		Vector2 center = CenterOf(Span);
		Harness.Mouse.Drag(center.X, center.Y, center.X, center.Y + 60f);
		Step();

		Assert.IsTrue(height < 150f, $"Dragging down left the height at {height}.");
		Assert.IsTrue(dragging, "The splitter never reported being grabbed.");
	}

	[TestMethod]
	public void HorizontalSplitter_DraggingUpGrowsTheZone()
	{
		height = 150f;
		Start(DrawHorizontal);

		Vector2 center = CenterOf(Span);
		Harness.Mouse.Drag(center.X, center.Y, center.X, center.Y - 60f);
		Step();

		Assert.IsTrue(height > 150f, $"Dragging up left the height at {height}.");
	}

	[TestMethod]
	public void HorizontalSplitter_StopsAtItsMinimum()
	{
		height = 150f;
		Start(DrawHorizontal);

		Vector2 center = CenterOf(Span);
		Harness.Mouse.Drag(center.X, center.Y, center.X, center.Y + 600f);
		Step();

		Assert.IsTrue(height >= 60f, $"The splitter ran past its 60px minimum to {height}.");
	}

	[TestMethod]
	public void HorizontalSplitter_LeftAlone_KeepsItsSize()
	{
		height = 150f;
		Start(DrawHorizontal);

		Step(5);

		Assert.AreEqual(150f, height, "The splitter moved on its own.");
		Assert.IsFalse(dragging, "The splitter reported a drag nobody made.");
	}
}
