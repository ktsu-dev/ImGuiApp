// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Numerics;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives <see cref="ImGuiWidgets.FrameTimeGraph(string, ReadOnlySpan{float}, FrameTimeGraphOptions?)"/>
/// on its own. No test calls <c>Mark</c>: the graph marks its box and its statistics row itself.
/// </summary>
[TestClass]
public sealed class FrameTimeGraphTests : WidgetTest
{
	private const string Graph = "ftg";
	private const string Stats = "ftg/stats";

	private static readonly Vector2 GraphSize = new(300f, 100f);

	private float[] values = [];
	private FrameTimeHistory? history;
	private FrameTimeGraphOptions options = new() { Size = GraphSize };

	private void Draw()
	{
		if (history is not null)
		{
			ImGuiWidgets.FrameTimeGraph(Graph, history, options);
		}
		else
		{
			ImGuiWidgets.FrameTimeGraph(Graph, values, options);
		}
	}

	private static float[] Repeat(int count, float value)
	{
		float[] result = new float[count];
		Array.Fill(result, value);
		return result;
	}

	private static FrameTimeGraphOptions Plain(float budget, float scale) =>
		new() { Size = GraphSize, BudgetMilliseconds = budget, ScaleMilliseconds = scale, ShowStatistics = false };

	[TestMethod]
	public void FrameTimeGraph_MarksItsBoxAndStats()
	{
		values = Repeat(60, 10f);
		Start(Draw);

		Assert.IsTrue(IsVisible(Graph), "The graph did not mark its box.");
		Assert.IsTrue(IsVisible(Stats), "The graph did not mark its statistics row.");

		Rectangle rect = RectOf(Graph);
		Assert.IsTrue(Math.Abs(rect.Width - GraphSize.X) <= 1, $"The graph reserved {rect.Width}px of width rather than {GraphSize.X}.");
		Assert.IsTrue(Math.Abs(rect.Height - GraphSize.Y) <= 1, $"The graph reserved {rect.Height}px of height rather than {GraphSize.Y}.");
		AssertSomethingWasDrawn("a frame-time graph should draw its box and bars");
	}

	[TestMethod]
	public void FrameTimeGraph_NoStatistics_OmitsTheStatsRow()
	{
		values = Repeat(60, 10f);
		options = options with { ShowStatistics = false };
		Start(Draw);

		Assert.IsTrue(IsVisible(Graph), "The graph did not mark its box.");
		Assert.IsFalse(IsVisible(Stats), "The statistics row was drawn with ShowStatistics off.");
	}

	[TestMethod]
	public void FrameTimeGraph_BudgetLineSitsAtItsFraction()
	{
		values = Repeat(60, 5f);
		options = Plain(budget: 0f, scale: 40f);
		Start(Draw);
		MoveAway();
		byte[] withoutBudget = Snapshot();
		Rectangle rect = RectOf(Graph);

		options = Plain(budget: 20f, scale: 40f);
		Step();

		Rectangle line = BoundsOfDifference(withoutBudget) ?? throw new InvalidOperationException("A 20 ms budget on a 40 ms scale drew no line.");
		Assert.IsTrue(Math.Abs(line.MaxY - (rect.MinY + 50)) <= 2, $"The budget line ended at y={line.MaxY}, not half way down at y={rect.MinY + 50}.");
		Assert.IsTrue(Math.Abs(line.MaxX - rect.MaxX) <= 2, $"The budget line ended at x={line.MaxX}, not at the right edge x={rect.MaxX}.");
	}

	[TestMethod]
	public void FrameTimeGraph_OverBudgetBarChangesColour()
	{
		values = [.. Repeat(59, 5f), 19.9f];
		options = Plain(budget: 20f, scale: 40f);
		Start(Draw);
		MoveAway();
		byte[] underBudget = Snapshot();
		Rectangle rect = RectOf(Graph);

		values[^1] = 20.1f;
		Step();

		Rectangle bar = BoundsOfDifference(underBudget) ?? throw new InvalidOperationException("Crossing the budget changed nothing.");
		Assert.IsGreaterThanOrEqualTo(rect.MaxX - 6, bar.MinX, $"The change started at x={bar.MinX}, left of the last bar.");
		Assert.IsLessThanOrEqualTo(rect.MaxX + 1, bar.MaxX, $"The change ended at x={bar.MaxX}, right of the last bar.");
		Assert.IsGreaterThanOrEqualTo(45, bar.Height, $"Only {bar.Height}px changed, so the bar was not recoloured along its height.");
	}

	[TestMethod]
	public void FrameTimeGraph_ClippedSpikeGetsACap()
	{
		values = [.. Repeat(59, 5f), 39f];
		options = Plain(budget: 0f, scale: 40f);
		Start(Draw);
		MoveAway();
		byte[] unclipped = Snapshot();
		Rectangle rect = RectOf(Graph);

		values[^1] = 400f;
		Step();

		Rectangle cap = BoundsOfDifference(unclipped) ?? throw new InvalidOperationException("A spike past the scale changed nothing.");
		Assert.IsGreaterThanOrEqualTo(rect.MaxX - 6, cap.MinX, $"The change started at x={cap.MinX}, left of the last bar.");
		Assert.IsLessThanOrEqualTo(rect.MaxX + 1, cap.MaxX, $"The change ended at x={cap.MaxX}, right of the last bar.");
		Assert.IsTrue(Math.Abs(cap.MinY - rect.MinY) <= 1, $"The cap sat at y={cap.MinY}, not at the top edge y={rect.MinY}.");
		Assert.IsLessThanOrEqualTo(6, cap.Height, $"{cap.Height}px changed, more than a cap and a pixel of growth.");
	}

	[TestMethod]
	public void FrameTimeGraph_HoverShowsATooltip()
	{
		values = Repeat(60, 5f);
		options = Plain(budget: 20f, scale: 40f);
		Start(Draw);
		MoveAway();
		byte[] unhovered = Snapshot();
		Rectangle rect = RectOf(Graph);

		Harness.Mouse.MoveTo(rect.MaxX - 2, rect.MinY + (rect.Height / 2f));
		Step(2);

		Rectangle tooltip = BoundsOfDifference(unhovered) ?? throw new InvalidOperationException("Hovering a bar changed nothing.");
		Assert.IsGreaterThanOrEqualTo(40, tooltip.Width, $"Only {tooltip.Width}px changed, so no tooltip wider than the bar appeared.");
	}

	[TestMethod]
	public void FrameTimeGraph_EmptyStillDrawsTheFrame()
	{
		values = [];
		Start(Draw);

		Assert.IsTrue(IsVisible(Graph), "An empty graph did not mark its box.");
		AssertSomethingWasDrawn("an empty frame-time graph should still draw its frame");
	}

	[TestMethod]
	public void FrameTimeGraph_HistoryOverloadMatchesTheSpan()
	{
		float[] mix = new float[60];
		FrameTimeHistory frames = new(60);
		for (int i = 0; i < mix.Length; i++)
		{
			mix[i] = (i % 3) switch
			{
				0 => 5f,
				1 => 18f,
				_ => 36f,
			};
			frames.Add(mix[i]);
		}

		values = mix;
		options = Plain(budget: 1000f / 60f, scale: 0f);
		Start(Draw);
		MoveAway();
		byte[] fromSpan = Snapshot();

		history = frames;
		Step();

		Assert.AreEqual(0, PixelsChangedSince(fromSpan), "The history overload drew differently from the same frames as a span.");
	}

	[TestMethod]
	public void FrameTimeGraph_TooManyFrames_DrawsOnlyTheNewest()
	{
		values = [.. Repeat(700, 30f), .. Repeat(300, 5f)];
		options = Plain(budget: 20f, scale: 40f);
		Start(Draw);
		MoveAway();
		byte[] withHiddenFrames = Snapshot();

		values = Repeat(1000, 5f);
		Step();

		Assert.AreEqual(0, PixelsChangedSince(withHiddenFrames), "Frames older than the box is wide still changed what was drawn.");
	}
}
