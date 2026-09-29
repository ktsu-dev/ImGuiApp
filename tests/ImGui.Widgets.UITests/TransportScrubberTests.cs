// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Collections.Generic;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.TransportScrubber"/> on its own.</summary>
[TestClass]
public sealed class TransportScrubberTests : WidgetTest
{
	private const string Label = "scrubber";
	private const string Track = "scrubber/track";
	private const float Width = 400f;
	private const float Duration = 10f;

	// A twentieth of a second is two pixels at this width: wider than the rounding of a probe
	// rectangle to whole pixels, and far narrower than the distances the assertions tell apart.
	private const float Tolerance = 0.05f;

	private readonly ImGuiWidgets.TimelineView view = new();
	private readonly List<float> requested = [];
	private float duration = Duration;
	private float playhead;
	private float inPoint;
	private float outPoint;
	private nint thumbnail;
	private ImGuiWidgets.TransportScrubberOptions? options;
	private TransportScrubberChange changes;

	private void Draw() =>
		changes |= ImGuiWidgets.TransportScrubber(Label, duration, view, ref playhead, ref inPoint, ref outPoint, options, new Vector2(Width, 0f));

	// The first frame reports a view change of its own, because it is the one on which the widget
	// sizes the view to the clip. Tests of what the user changes start counting after it.
	private void StartScrubber(Action? draw = null, HarnessOptions? harnessOptions = null)
	{
		Start(draw ?? Draw, harnessOptions);
		changes = TransportScrubberChange.None;
	}

	private Vector2 TrackPointAt(float fractionX)
	{
		Rectangle rect = RectOf(Track);
		return new Vector2(rect.MinX + (rect.Width * fractionX), rect.MinY + (rect.Height / 2f));
	}

	private Vector2 TrackPointAtTime(float seconds) => TrackPointAt(seconds / Duration);

	private static bool Contains(Rectangle outer, Rectangle inner, int slack) =>
		inner.MinX >= outer.MinX - slack
		&& inner.MinY >= outer.MinY - slack
		&& inner.MaxX <= outer.MaxX + slack
		&& inner.MaxY <= outer.MaxY + slack;

	private static bool Intersects(Rectangle a, Rectangle b) =>
		a.MinX < b.MaxX && b.MinX < a.MaxX && a.MinY < b.MaxY && b.MinY < a.MaxY;

	[TestMethod]
	public void TransportScrubber_IsDrawnAndMarksItsParts()
	{
		StartScrubber();

		Assert.IsTrue(IsVisible(Label), "The scrubber marked no probe item.");
		Assert.IsTrue(IsVisible($"{Label}/ruler"), "The scrubber did not mark its ruler.");
		Assert.IsTrue(IsVisible(Track), "The scrubber did not mark its track.");
		Assert.IsTrue(IsVisible($"{Label}/scrollbar"), "The scrubber did not mark its scrollbar.");
		AssertSomethingWasDrawn("a scrubber draws a ruler, a track and a scrollbar");
	}

	[TestMethod]
	public void TransportScrubber_ClickSeeks()
	{
		StartScrubber();

		ClickFraction(Track, 0.5f);

		Assert.AreEqual(5f, playhead, Tolerance, "A click in the middle of the track did not seek to the middle.");
		Assert.IsTrue(changes.HasFlag(TransportScrubberChange.Playhead), "The seek was not reported.");
	}

	[TestMethod]
	public void TransportScrubber_SnapsToFrames()
	{
		options = new() { FrameDuration = 0.04f };
		StartScrubber();

		ClickFraction(Track, 0.3013f);

		float frames = playhead / 0.04f;
		Assert.AreEqual(MathF.Round(frames), frames, 1e-3f, $"The playhead {playhead} is not on a frame boundary.");
	}

	[TestMethod]
	public void TransportScrubber_DraggingTheInPointMovesIt()
	{
		inPoint = 2f;
		outPoint = 6f;
		StartScrubber();
		Vector2 from = TrackPointAtTime(2f);
		Vector2 to = TrackPointAtTime(3f);

		Harness.Mouse.Drag(from.X, from.Y, to.X, to.Y);
		Step();

		Assert.AreEqual(3f, inPoint, Tolerance, "The in point did not follow the drag.");
		Assert.AreEqual(6f, outPoint, 1e-4f, "Dragging the in point moved the out point.");
		Assert.IsTrue(changes.HasFlag(TransportScrubberChange.InOut), "The range change was not reported.");
	}

	[TestMethod]
	public void TransportScrubber_ShiftDragDrawsANewRange()
	{
		StartScrubber();
		Vector2 from = TrackPointAtTime(1f);
		Vector2 to = TrackPointAtTime(4f);

		HarnessKeyboard.KeyDown(ImGuiKey.ModShift);
		Step();
		Harness.Mouse.Drag(from.X, from.Y, to.X, to.Y);
		HarnessKeyboard.KeyUp(ImGuiKey.ModShift);
		Step();

		Assert.AreEqual(1f, inPoint, Tolerance, "The new range did not start where the drag did.");
		Assert.AreEqual(4f, outPoint, Tolerance, "The new range did not end where the drag did.");
	}

	[TestMethod]
	public void TransportScrubber_KeysMarkInAndOut()
	{
		StartScrubber();
		Hover(Track);

		playhead = 3f;
		Harness.Keyboard.Press(ImGuiKey.I);

		Assert.AreEqual(3f, inPoint, 1e-4f, "I did not mark the in point at the playhead.");
		Assert.AreEqual(10f, outPoint, 1e-4f, "Marking in with no range did not run the range to the end.");

		playhead = 7f;
		Harness.Keyboard.Press(ImGuiKey.O);

		Assert.AreEqual(7f, outPoint, 1e-4f, "O did not mark the out point at the playhead.");
		Assert.AreEqual(3f, inPoint, 1e-4f, "Marking out moved the in point.");
	}

	[TestMethod]
	public void TransportScrubber_ArrowKeysStepByAFrame()
	{
		options = new() { FrameDuration = 0.04f };
		playhead = 1f;
		StartScrubber();
		Hover(Track);

		Harness.Keyboard.Press(ImGuiKey.RightArrow);
		Assert.AreEqual(1.04f, playhead, 1e-4f, "Right arrow did not step one frame.");

		Harness.Keyboard.Press(ImGuiKey.LeftArrow, shift: true);
		Assert.AreEqual(0.64f, playhead, 1e-4f, "Shift+Left did not step ten frames back.");
	}

	[TestMethod]
	public void TransportScrubber_ContextMenuClearsTheRange()
	{
		inPoint = 2f;
		outPoint = 6f;
		StartScrubber();
		Vector2 point = TrackPointAtTime(8f);

		Harness.Mouse.Click(point.X, point.Y, 1);
		Step(2);
		Click("Clear in/out");

		Assert.AreEqual(inPoint, outPoint, "Clear in/out left a range.");
		Assert.IsTrue(changes.HasFlag(TransportScrubberChange.InOut), "Clearing the range was not reported.");
	}

	[TestMethod]
	public void TransportScrubber_AsksForAThumbnailPerVisibleSlot()
	{
		options = new()
		{
			ThumbnailResolver = time =>
			{
				requested.Add(time);
				return 0;
			},
		};

		StartScrubber(() =>
		{
			requested.Clear();
			Draw();
		});

		float trackHeight = RectOf(Track).Height;
		int expected = ImGuiWidgets.TransportScrubberState.ThumbnailSlots(0f, 10f, 10f, Width, trackHeight * 16f / 9f).Count;

		Assert.IsTrue(expected > 0, "The fixture expects no slots, so it proves nothing.");
		Assert.HasCount(expected, requested, "The resolver was not asked once per visible slot.");
		foreach (float time in requested)
		{
			Assert.IsTrue(time is >= 0f and <= Duration, $"The resolver was asked for {time}, outside the clip.");
		}
	}

	[TestMethod]
	public void TransportScrubber_DrawsTheThumbnailItIsGiven()
	{
		options = new() { ThumbnailResolver = _ => thumbnail };
		StartScrubber();
		byte[] before = Snapshot();

		thumbnail = CreateTestTexture().TextureId;
		Step();

		Assert.IsTrue(PixelsChangedSince(before) > 0, "Handing the scrubber a texture changed nothing on screen.");
		Rectangle? changed = BoundsOfDifference(before);
		Assert.IsNotNull(changed);
		Assert.IsTrue(Contains(RectOf(Track), changed.Value, slack: 1), $"The thumbnail drew at {changed.Value}, outside the track {RectOf(Track)}.");
	}

	[TestMethod]
	public void TransportScrubber_CtrlWheelZoomsTheSharedView()
	{
		float[] samples = new float[48000];
		for (int i = 0; i < samples.Length; i++)
		{
			samples[i] = 0.8f * MathF.Sin(i * 0.002f) * MathF.Sin(i * 0.0003f);
		}

		ImGuiWidgets.WaveformPeakCache cache = new(samples, Duration);
		float wavePlayhead = 0f;

		StartScrubber(() =>
		{
			Draw();
			ImGuiWidgets.Waveform("wave", cache, view, ref wavePlayhead, new Vector2(Width, 80f));
		});

		Vector2 point = TrackPointAt(0.25f);
		Harness.Mouse.MoveTo(point.X, point.Y);
		Step(2);
		byte[] before = Snapshot();

		HarnessKeyboard.KeyDown(ImGuiKey.ModCtrl);
		Step();
		Harness.Mouse.Wheel(point.X, point.Y, 1);
		Step();
		HarnessKeyboard.KeyUp(ImGuiKey.ModCtrl);
		Step();

		Assert.AreEqual(8f, view.ViewLength, 0.01f, "One notch did not zoom in by a quarter.");
		Assert.IsTrue(changes.HasFlag(TransportScrubberChange.View), "The zoom was not reported.");
		Rectangle? changed = BoundsOfDifference(before);
		Assert.IsNotNull(changed, "Zooming changed nothing on screen.");
		Assert.IsTrue(Intersects(changed.Value, RectOf("wave")), "The waveform sharing the view did not redraw at the new zoom.");
	}

	[TestMethod]
	public void TransportScrubber_WithNoDuration_IgnoresThePointer()
	{
		duration = 0f;
		playhead = 1f;
		StartScrubber();

		ClickFraction(Track, 0.8f);

		Assert.AreEqual(1f, playhead, 1e-6f, "A scrubber with no clip seeked.");
		Assert.AreEqual(TransportScrubberChange.None, changes);
		Assert.IsTrue(IsVisible(Label), "A scrubber with no clip stopped reserving its box.");
	}
}
