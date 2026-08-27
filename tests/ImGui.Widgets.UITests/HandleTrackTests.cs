// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Linq;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.HandleTrack"/> on its own.</summary>
[TestClass]
public sealed class HandleTrackTests : WidgetTest
{
	private const string Label = "thresholds";
	private const float TrackWidth = 300f;
	private const float TrackHeight = 60f;

	private float[] handles = [25f, 75f];
	private float minGap;
	private bool moved;

	// The track overlays a rectangle the caller owns rather than reserving layout of its own, so
	// the test supplies that rectangle from the current cursor position.
	private void Draw()
	{
		Vector2 origin = ImGui.GetCursorScreenPos();
		Vector2 max = origin + new Vector2(TrackWidth, TrackHeight);
		moved |= ImGuiWidgets.HandleTrack(Label, handles, origin, max, 0f, 100f, minGap);
	}

	[TestMethod]
	public void HandleTrack_IsDrawnAndMarksItself()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Label), "The handle track marked no probe item.");
		AssertSomethingWasDrawn("the handle track");
	}

	[TestMethod]
	public void HandleTrack_CoversTheRectangleItWasGiven()
	{
		Start(Draw);

		Rectangle rect = RectOf(Label);

		Assert.IsTrue(Math.Abs(rect.Width - TrackWidth) <= 2, $"The track claimed {rect.Width}px of width rather than {TrackWidth}.");
		Assert.IsTrue(Math.Abs(rect.Height - TrackHeight) <= 2, $"The track claimed {rect.Height}px of height rather than {TrackHeight}.");
	}

	[TestMethod]
	public void HandleTrack_DraggingAHandleMovesIt()
	{
		handles = [25f, 75f];
		Start(Draw);

		DragAcross(Label, 0.25f, 0.4f);

		Assert.IsTrue(handles[0] > 25f, $"The first handle stayed at {handles[0]}.");
		Assert.IsTrue(moved, "The track reported no movement while a handle was being dragged.");
	}

	[TestMethod]
	public void HandleTrack_HandlesStayOrdered()
	{
		handles = [25f, 75f];
		Start(Draw);

		// Drag the lower handle past the upper one; ordering is what stops them swapping.
		DragAcross(Label, 0.25f, 0.95f);

		Assert.IsTrue(handles[0] <= handles[1], $"The handles crossed: {handles[0]} > {handles[1]}.");
	}

	[TestMethod]
	public void HandleTrack_KeepsTheMinimumGap()
	{
		handles = [25f, 75f];
		minGap = 20f;
		Start(Draw);

		DragAcross(Label, 0.25f, 0.9f);

		Assert.IsTrue(
			handles[1] - handles[0] >= minGap - 0.5f,
			$"The handles closed to {handles[1] - handles[0]}, inside the {minGap} minimum gap.");
	}

	[TestMethod]
	public void HandleTrack_StaysInsideItsBounds()
	{
		handles = [25f, 75f];
		Start(Draw);

		DragAcross(Label, 0.75f, 1.5f);

		Assert.IsTrue(handles[1] <= 100f, $"A handle ran past the upper bound to {handles[1]}.");
	}

	[TestMethod]
	public void HandleTrack_WithNoHandles_ReservesNothingAndMarksNothing()
	{
		handles = [];
		Start(Draw);

		Assert.IsFalse(
			Harness.Probe.KnownNames.Any(name => name.EndsWith("/" + Label, StringComparison.Ordinal)),
			"An empty handle track still submitted an item, so it consumed layout it does not own.");
	}
}
