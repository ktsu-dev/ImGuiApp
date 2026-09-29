// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Numerics;

using Hexa.NET.ImGui;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.Waveform(string, ReadOnlySpan{float}, ReadOnlySpan{float}, float, ref float, ref float, ref float, Vector2, float)"/> on its own.</summary>
[TestClass]
public sealed class WaveformTests : WidgetTest
{
	private const string Label = "overview";
	private const float Width = 400f;
	private const float Height = 80f;
	private const float Duration = 10f;

	// Half a unit is twenty pixels at this width: far wider than any rounding in where a click
	// lands, and far narrower than the distances the assertions tell apart.
	private const float Tolerance = 0.5f;

	private float[] minimums = BuildOverview(-1f);
	private float[] maximums = BuildOverview(1f);
	private float duration = Duration;
	private float playhead;
	private float loopStart = 2f;
	private float loopEnd = 6f;
	private WaveformChange changes;

	private static float[] BuildOverview(float sign)
	{
		float[] columns = new float[200];
		for (int i = 0; i < columns.Length; i++)
		{
			columns[i] = sign * (0.2f + (0.7f * MathF.Abs(MathF.Sin(i * 0.1f))));
		}

		return columns;
	}

	private void Draw() =>
		changes |= ImGuiWidgets.Waveform(Label, minimums, maximums, duration, ref playhead, ref loopStart, ref loopEnd, new Vector2(Width, Height), minLoopLength: 0.5f);

	[TestMethod]
	public void Waveform_IsDrawnAndMarksItself()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Label), "The waveform marked no probe item.");
		AssertSomethingWasDrawn("the waveform");
	}

	[TestMethod]
	public void Waveform_ReservesTheSizeItWasGiven()
	{
		Start(Draw);

		Rectangle rect = RectOf(Label);

		Assert.IsTrue(Math.Abs(rect.Width - Width) <= 2, $"The waveform claimed {rect.Width}px of width rather than {Width}.");
		Assert.IsTrue(Math.Abs(rect.Height - Height) <= 2, $"The waveform claimed {rect.Height}px of height rather than {Height}.");
	}

	[TestMethod]
	public void Waveform_DrawsTheOverviewItIsGiven()
	{
		minimums = new float[200];
		maximums = new float[200];
		Start(Draw);
		byte[] silent = Snapshot();

		minimums = BuildOverview(-1f);
		maximums = BuildOverview(1f);
		Step();

		Assert.IsTrue(PixelsChangedSince(silent) > 1000, "A loud overview drew no differently from silence.");
	}

	[TestMethod]
	public void Waveform_ClickSeeks()
	{
		Start(Draw);

		ClickFraction(Label, 0.8f);

		Assert.AreEqual(8f, playhead, Tolerance, "The click did not move the playhead to where it landed.");
		Assert.IsTrue(changes.HasFlag(WaveformChange.Playhead), "The waveform did not report the seek.");
		Assert.AreEqual(2f, loopStart, 1e-4f, "Seeking moved the loop.");
		Assert.AreEqual(6f, loopEnd, 1e-4f, "Seeking moved the loop.");
	}

	[TestMethod]
	public void Waveform_DragScrubsThePlayhead()
	{
		Start(Draw);

		DragAcross(Label, 0.1f, 0.9f);

		Assert.AreEqual(9f, playhead, Tolerance, "The playhead did not follow the drag.");
	}

	[TestMethod]
	public void Waveform_DraggingALoopEdgeMovesIt()
	{
		Start(Draw);

		DragAcross(Label, 0.6f, 0.8f);

		Assert.AreEqual(8f, loopEnd, Tolerance, "The loop end did not follow the drag.");
		Assert.AreEqual(2f, loopStart, 1e-4f, "Dragging the end moved the start.");
		Assert.AreEqual(0f, playhead, 1e-4f, "Dragging a loop edge seeked.");
		Assert.IsTrue(changes.HasFlag(WaveformChange.Loop), "The waveform did not report the loop change.");
	}

	[TestMethod]
	public void Waveform_LoopKeepsItsMinimumLength()
	{
		Start(Draw);

		DragAcross(Label, 0.2f, 0.9f);

		Assert.IsTrue(loopEnd - loopStart >= 0.5f - 1e-4f, $"The loop closed to {loopEnd - loopStart}, inside its 0.5 minimum.");
		Assert.IsTrue(loopStart <= loopEnd, "The loop edges crossed.");
	}

	[TestMethod]
	public void Waveform_ShiftDragDrawsANewLoop()
	{
		loopStart = 0f;
		loopEnd = 0f;
		Start(Draw);

		HarnessKeyboard.KeyDown(ImGuiKey.ModShift);
		DragAcross(Label, 0.3f, 0.5f);
		HarnessKeyboard.KeyUp(ImGuiKey.ModShift);
		Step();

		Assert.AreEqual(3f, loopStart, Tolerance, "The new loop did not start where the drag did.");
		Assert.AreEqual(5f, loopEnd, Tolerance, "The new loop did not end where the drag did.");
		Assert.AreEqual(0f, playhead, 1e-4f, "Drawing a loop seeked.");
	}

	[TestMethod]
	public void Waveform_WithNoTimeline_IgnoresThePointer()
	{
		duration = 0f;
		Start(Draw);

		ClickFraction(Label, 0.8f);

		Assert.AreEqual(WaveformChange.None, changes);
		Assert.AreEqual(0f, playhead, 1e-4f);
		Assert.IsTrue(IsVisible(Label), "A waveform with no timeline stopped reserving its box.");
	}
}
