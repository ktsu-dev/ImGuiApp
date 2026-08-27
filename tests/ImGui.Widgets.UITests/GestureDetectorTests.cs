// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;
using ktsu.ImGui.Widgets.Gestures;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.GestureDetector"/> on its own.</summary>
[TestClass]
public sealed class GestureDetectorTests : WidgetTest
{
	private const string Label = "surface";
	private static readonly Vector2 Size = new(300f, 200f);

	private GestureResult latest;
	private GestureFlags seen;
	private bool resetRequested = true;

	// The detector keys its state by the ImGui identifier of the label, and every test here draws
	// into the same window under the same label, so the state survives from one test to the next.
	// Resetting on the first frame is what keeps a press left over from an earlier test out of
	// this one. It has to happen from inside the frame: ResetGestureDetector asks ImGui for the
	// identifier, which reads the current window and is only valid while one is being drawn.
	private void Draw()
	{
		if (resetRequested)
		{
			ImGuiWidgets.ResetGestureDetector(Label);
			resetRequested = false;
		}

		Vector2 origin = ImGui.GetCursorScreenPos();
		latest = ImGuiWidgets.GestureDetector(Label, Size);
		seen |= latest.Gestures;
		MarkSpan(Label, origin);
	}

	[TestMethod]
	public void GestureDetector_ClaimsTheRegionItIsGiven()
	{
		Start(Draw);

		Rectangle rect = RectOf(Label);

		Assert.IsTrue(Math.Abs(rect.Width - Size.X) <= 2, $"The region claimed {rect.Width}px of width rather than {Size.X}.");
		Assert.IsTrue(Math.Abs(rect.Height - Size.Y) <= 2, $"The region claimed {rect.Height}px of height rather than {Size.Y}.");
	}

	[TestMethod]
	public void GestureDetector_ReportsNothingWhileUntouched()
	{
		Start(Draw);
		Step(5);

		Assert.AreEqual(GestureFlags.None, seen, $"An untouched region reported {seen}.");
		Assert.IsFalse(latest.HasGesture, "An untouched region reported a gesture.");
	}

	[TestMethod]
	public void GestureDetector_ReportsATap()
	{
		Start(Draw);

		Vector2 center = CenterOf(Label);
		Harness.Mouse.Click(center.X, center.Y);
		Step(2);

		Assert.IsTrue((seen & GestureFlags.Tap) != 0, $"A quick click reported {seen} rather than a tap.");
	}

	[TestMethod]
	public void GestureDetector_ReportsADoubleTap()
	{
		Start(Draw);

		Vector2 center = CenterOf(Label);
		Harness.Mouse.Click(center.X, center.Y);
		Harness.Mouse.Click(center.X, center.Y);
		Step(2);

		Assert.IsTrue((seen & GestureFlags.DoubleTap) != 0, $"Two quick clicks reported {seen} rather than a double tap.");
	}

	[TestMethod]
	public void GestureDetector_ReportsALongPress()
	{
		Start(Draw);

		Vector2 center = CenterOf(Label);
		Harness.Mouse.MoveTo(center.X, center.Y);
		Step();
		HarnessMouse.Down(0);

		// The default long-press threshold is half a second, which is thirty frames at the
		// harness's fixed frame delta.
		Step(45);
		HarnessMouse.Up(0);
		Step();

		Assert.IsTrue((seen & GestureFlags.LongPress) != 0, $"Holding for 45 frames reported {seen} rather than a long press.");
	}

	[TestMethod]
	public void GestureDetector_ReportsAPan()
	{
		Start(Draw);

		Rectangle rect = RectOf(Label);
		float y = rect.MinY + (rect.Height / 2f);
		Harness.Mouse.Drag(rect.MinX + 40f, y, rect.MinX + 200f, y);
		Step();

		Assert.IsTrue((seen & GestureFlags.Pan) != 0, $"Dragging across the region reported {seen} rather than a pan.");
	}

	[TestMethod]
	public void GestureDetector_Reset_RestartsAPressInProgress()
	{
		Start(Draw);

		Vector2 center = CenterOf(Label);
		Harness.Mouse.MoveTo(center.X, center.Y);
		Step();
		HarnessMouse.Down(0);

		// Twenty frames is a third of a second, short of the half-second long-press threshold.
		Step(20);
		resetRequested = true;
		Step(20);
		seen = GestureFlags.None;
		Step(5);

		Assert.AreEqual(
			GestureFlags.None,
			seen,
			"The press was not restarted by the reset: the long press fired on time counted from before it.");

		// The detector is not merely dead afterwards -- holding on past the threshold, measured
		// from the reset, still reports the long press.
		Step(30);

		Assert.IsTrue((seen & GestureFlags.LongPress) != 0, $"After the reset the detector reported {seen} rather than a long press.");

		HarnessMouse.Up(0);
		Step();
	}
}
