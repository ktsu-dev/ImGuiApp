// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.App.Tests;

using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// <see cref="TouchScrollGesture"/>: one finger is the mouse, and a two-finger pan is the wheel.
/// The first half checks the rules against a recording sink; the second drives a real window
/// through the harness, which is what says the wheel steps add up to the distance panned.
/// </summary>
[TestClass]
public sealed class TouchScrollGestureTests
{
	private static readonly Vector2 Step = new(50f, 50f);

	private sealed class RecordingSink : ITouchPointerSink
	{
		internal List<string> Events { get; } = [];

		internal Vector2 Scrolled { get; private set; }

		public void MovePointer(Vector2 position) =>
			Events.Add(position == TouchScrollGesture.Offscreen ? "move offscreen" : $"move {position.X:0},{position.Y:0}");

		public void SetButton(bool down) => Events.Add(down ? "down" : "up");

		public void Scroll(float horizontal, float vertical)
		{
			Events.Add("scroll");
			Scrolled += new Vector2(horizontal, vertical);
		}
	}

	private static string Joined(RecordingSink sink) => string.Join(" | ", sink.Events);

	private readonly object first = new();
	private readonly object second = new();
	private readonly object third = new();

	[TestMethod]
	public void OneFinger_TapsAndDragsAsTheMouse()
	{
		RecordingSink sink = new();
		TouchScrollGesture gesture = new(sink);

		gesture.Began(first, new Vector2(10, 20));
		gesture.Moved(first, new Vector2(30, 40), Step);
		gesture.Ended(first, new Vector2(35, 45));

		Assert.AreEqual("move 10,20 | down | move 30,40 | move 35,45 | up", Joined(sink));
		Assert.IsFalse(gesture.IsPanning);
	}

	[TestMethod]
	public void SecondFinger_ReleasesTheFirstOffscreen_ThenParksTheMouseAtTheCentroid()
	{
		RecordingSink sink = new();
		TouchScrollGesture gesture = new(sink);

		gesture.Began(first, new Vector2(100, 100));
		gesture.Began(second, new Vector2(200, 100));

		// The release hovers nothing, so a button under the first finger is not clicked.
		Assert.AreEqual("move 100,100 | down | move offscreen | up | move 150,100", Joined(sink));
		Assert.IsTrue(gesture.IsPanning);
	}

	[TestMethod]
	public void Pan_ScrollsByTheCentroidsMovement_InWheelSteps_WithoutMovingTheMouse()
	{
		RecordingSink sink = new();
		TouchScrollGesture gesture = new(sink);
		gesture.Began(first, new Vector2(100, 100));
		gesture.Began(second, new Vector2(200, 100));
		sink.Events.Clear();

		// Both fingers move 100 points down and 50 left: the centroid follows.
		gesture.Moved(first, new Vector2(50, 200), Step);
		gesture.Moved(second, new Vector2(150, 200), Step);

		Assert.AreEqual(new Vector2(-1f, 2f), sink.Scrolled, "Fingers moving down reveal what is above: the wheel's positive direction.");
		CollectionAssert.DoesNotContain(sink.Events, "down");
		Assert.IsFalse(sink.Events.Exists(e => e.StartsWith("move", System.StringComparison.Ordinal)), "The mouse stays where the pan began.");
	}

	[TestMethod]
	public void FingerJoiningAPan_DoesNotScrollByTheCentroidsJump()
	{
		RecordingSink sink = new();
		TouchScrollGesture gesture = new(sink);
		gesture.Began(first, new Vector2(100, 100));
		gesture.Began(second, new Vector2(200, 100));
		gesture.Began(third, new Vector2(150, 400));

		Assert.AreEqual(Vector2.Zero, sink.Scrolled);

		gesture.Moved(third, new Vector2(150, 550), Step);
		Assert.AreEqual(new Vector2(0f, 1f), sink.Scrolled, "One of three fingers moving 150 moves the centroid 50.");
	}

	[TestMethod]
	public void LiftingToOneFinger_EndsThePan_AndTheLastFingerPressesNothing()
	{
		RecordingSink sink = new();
		TouchScrollGesture gesture = new(sink);
		gesture.Began(first, new Vector2(100, 100));
		gesture.Began(second, new Vector2(200, 100));
		gesture.Ended(second, new Vector2(200, 100));
		sink.Events.Clear();

		gesture.Moved(first, new Vector2(100, 300), Step);
		gesture.Ended(first, new Vector2(100, 300));

		Assert.IsEmpty(sink.Events, "A spent touch neither drags, scrolls nor clicks.");
		Assert.IsFalse(gesture.IsPanning);

		gesture.Began(first, new Vector2(5, 5));
		Assert.AreEqual("move 5,5 | down", Joined(sink), "Once every finger is up, the next touch is the mouse again.");
	}

	[TestMethod]
	public void SecondFingerReturning_ResumesThePan()
	{
		RecordingSink sink = new();
		TouchScrollGesture gesture = new(sink);
		gesture.Began(first, new Vector2(100, 100));
		gesture.Began(second, new Vector2(200, 100));
		gesture.Ended(second, new Vector2(200, 100));

		gesture.Began(second, new Vector2(300, 100));

		Assert.IsTrue(gesture.IsPanning);
		CollectionAssert.DoesNotContain(sink.Events.GetRange(5, sink.Events.Count - 5), "down");
	}

	[TestMethod]
	public void UnknownTouches_AreIgnored()
	{
		RecordingSink sink = new();
		TouchScrollGesture gesture = new(sink);

		gesture.Moved(first, new Vector2(1, 1), Step);
		gesture.Ended(first, new Vector2(1, 1));

		Assert.IsEmpty(sink.Events);
	}

	[TestMethod]
	public void PointsPerWheelStep_IsTwoLinesAcrossAndFiveDown_LimitedByTheWindow()
	{
		Assert.AreEqual(new Vector2(26f, 65f), TouchScrollGesture.PointsPerWheelStep(13f, new Vector2(1000, 1000)));
		Assert.AreEqual(new Vector2(26f, 33f), TouchScrollGesture.PointsPerWheelStep(13f, new Vector2(1000, 50)), "A short window scrolls two thirds of itself.");
	}

	[TestMethod]
	public void Pan_ScalesEachAxisByItsOwnStep()
	{
		RecordingSink sink = new();
		TouchScrollGesture gesture = new(sink);
		gesture.Began(first, new Vector2(0, 0));
		gesture.Began(second, new Vector2(0, 0));

		gesture.Moved(first, new Vector2(40, 100), new Vector2(10f, 25f));

		Assert.AreEqual(new Vector2(2f, 2f), sink.Scrolled);
	}

	/// <summary>The bridge the iOS app uses, writing into whatever ImGui context is current.</summary>
	private sealed class ImGuiSink : ITouchPointerSink
	{
		public void MovePointer(Vector2 position) => ImGui.GetIO().AddMousePosEvent(position.X, position.Y);

		public void SetButton(bool down) => ImGui.GetIO().AddMouseButtonEvent(0, down);

		public void Scroll(float horizontal, float vertical) => ImGui.GetIO().AddMouseWheelEvent(horizontal, vertical);
	}

	private static HarnessOptions Window() => new() { Width = 400, Height = 300 };

	private static void ListWindow(Vector2 size, ImGuiWindowFlags flags, ref Vector2 scroll, ref int clicks)
	{
		ImGui.SetNextWindowPos(Vector2.Zero);
		ImGui.SetNextWindowSize(size);
		ImGui.Begin("list", ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | flags);
		if (ImGui.Button("Press", new Vector2(120, 40)))
		{
			clicks++;
		}

		for (int i = 0; i < 200; i++)
		{
			ImGui.TextUnformatted($"Row {i} with enough text to run wider than the window it is drawn in, so it scrolls across");
		}

		scroll = new Vector2(ImGui.GetScrollX(), ImGui.GetScrollY());
		ImGui.End();
	}

	/// <summary>Pans two fingers from <paramref name="from"/> by <paramref name="by"/>, one finger per frame.</summary>
	private static void Pan(ImGuiAppHarness harness, TouchScrollGesture gesture, Vector2 from, Vector2 by)
	{
		object a = new();
		object b = new();
		Vector2 spread = new(30, 0);

		gesture.Began(a, from - spread);
		harness.Step(2);
		gesture.Began(b, from + spread);
		harness.Step(4);

		const int Moves = 4;
		for (int i = 1; i <= Moves; i++)
		{
			Vector2 step = TouchScrollWheel.PointsPerWheelStep();
			Vector2 offset = by * i / Moves;
			gesture.Moved(a, from - spread + offset, step);
			gesture.Moved(b, from + spread + offset, step);
			harness.Step();
		}

		gesture.Ended(a, from - spread + by);
		gesture.Ended(b, from + spread + by);
		harness.Step(2);
	}

	[TestMethod]
	public void TwoFingerPan_ScrollsAWindow_ByTheDistancePanned()
	{
		Vector2 scroll = Vector2.Zero;
		int clicks = 0;
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig { OnRender = _ => ListWindow(new Vector2(380, 280), ImGuiWindowFlags.None, ref scroll, ref clicks) },
			Window());
		harness.Step(2);
		TouchScrollGesture gesture = new(new ImGuiSink());

		Pan(harness, gesture, new Vector2(200, 200), new Vector2(0, -120));

		Assert.AreEqual(120f, scroll.Y, 2f, "Fingers moving up 120 points scroll the content up with them.");

		Pan(harness, gesture, new Vector2(200, 100), new Vector2(0, 40));
		Assert.AreEqual(80f, scroll.Y, 2f, "Panning down brings it back.");
	}

	[TestMethod]
	public void TwoFingerPan_ScrollsAcross_WhereTheWindowScrollsHorizontally()
	{
		Vector2 scroll = Vector2.Zero;
		int clicks = 0;
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig { OnRender = _ => ListWindow(new Vector2(380, 280), ImGuiWindowFlags.HorizontalScrollbar, ref scroll, ref clicks) },
			Window());
		harness.Step(2);
		TouchScrollGesture gesture = new(new ImGuiSink());

		Pan(harness, gesture, new Vector2(200, 200), new Vector2(-90, 0));

		Assert.AreEqual(90f, scroll.X, 2f, "Fingers moving left 90 points reveal what is to the right.");
		Assert.AreEqual(0f, scroll.Y, 0.5f);
	}

	[TestMethod]
	public void SecondFingerLandingOnAButton_DoesNotClickIt_AndOneFingerStillDoes()
	{
		Vector2 scroll = Vector2.Zero;
		int clicks = 0;
		using ImGuiAppHarness harness = ImGuiAppHarness.Start(
			new ImGuiAppConfig { OnRender = _ => ListWindow(new Vector2(380, 280), ImGuiWindowFlags.None, ref scroll, ref clicks) },
			Window());
		harness.Step(2);
		Vector2 button = ImGuiCenterOfPress();
		TouchScrollGesture gesture = new(new ImGuiSink());

		// The pan starts with both fingers on the button and moves nowhere.
		object a = new();
		object b = new();
		gesture.Began(a, button);
		harness.Step(2);
		gesture.Began(b, button + new Vector2(4, 0));
		harness.Step(4);
		gesture.Ended(a, button);
		gesture.Ended(b, button + new Vector2(4, 0));
		harness.Step(4);

		Assert.AreEqual(0, clicks, "The first finger's press is released over nothing.");

		object tap = new();
		gesture.Began(tap, button);
		harness.Step(2);
		gesture.Ended(tap, button);
		harness.Step(2);

		Assert.AreEqual(1, clicks, "A one-finger tap is a click, as before.");
	}

	/// <summary>The button sits at the window's top-left padding; its centre is a fixed offset in.</summary>
	private static Vector2 ImGuiCenterOfPress()
	{
		Vector2 padding = ImGui.GetStyle().WindowPadding;
		return padding + new Vector2(60, 20);
	}
}
