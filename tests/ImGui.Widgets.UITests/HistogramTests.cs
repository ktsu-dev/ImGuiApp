// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Numerics;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.Histogram"/> on its own.</summary>
[TestClass]
public sealed class HistogramTests : WidgetTest
{
	private const string Label = "levels";
	private static readonly Vector2 Size = new(280f, 120f);

	private float[] bins = [1f, 4f, 9f, 3f, 6f];
	private int seriesCount = 1;

	private void Draw() => ImGuiWidgets.Histogram(Label, bins, seriesCount, Size);

	[TestMethod]
	public void Histogram_IsDrawnAndMarksItself()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Label), "The histogram marked no probe item.");
		AssertSomethingWasDrawn("the histogram");
	}

	[TestMethod]
	public void Histogram_ReservesTheSizeItIsGiven()
	{
		Start(Draw);

		Rectangle rect = RectOf(Label);

		Assert.IsTrue(Math.Abs(rect.Width - Size.X) <= 2, $"The histogram reserved {rect.Width}px of width rather than {Size.X}.");
		Assert.IsTrue(Math.Abs(rect.Height - Size.Y) <= 2, $"The histogram reserved {rect.Height}px of height rather than {Size.Y}.");
	}

	[TestMethod]
	public void Histogram_ChangingTheBinsRedrawsTheBars()
	{
		bins = [1f, 4f, 9f, 3f, 6f];
		Start(Draw);
		byte[] before = Snapshot();

		bins = [9f, 1f, 2f, 8f, 1f];
		Step(2);

		Assert.IsTrue(PixelsChangedSince(before) > 0, "A different distribution drew the same bars.");
	}

	[TestMethod]
	public void Histogram_MultipleSeriesStackDifferently()
	{
		bins = [1f, 4f, 9f, 3f, 6f, 2f];
		seriesCount = 1;
		Start(Draw);
		byte[] single = Snapshot();

		seriesCount = 2;
		Step(2);

		Assert.IsTrue(PixelsChangedSince(single) > 0, "Two series drew the same as one.");
	}

	[TestMethod]
	public void Histogram_EmptyBins_StillDrawsItsFrame()
	{
		bins = [];
		Start(Draw);

		Assert.IsTrue(IsVisible(Label), "An empty histogram reserved no layout.");
		AssertSomethingWasDrawn("an empty histogram");
	}

	[TestMethod]
	public void Histogram_NonFiniteBins_DrawNothingForThatBar()
	{
		bins = [1f, 4f, 9f, 3f, 6f];
		Start(Draw);
		byte[] finite = Snapshot();

		bins = [1f, 4f, float.NaN, 3f, 6f];
		Step(2);

		Assert.IsTrue(PixelsChangedSince(finite) > 0, "Replacing a bin with NaN drew the same bar as before.");
	}
}
