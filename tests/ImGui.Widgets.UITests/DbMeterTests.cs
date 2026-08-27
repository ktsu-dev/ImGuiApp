// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Numerics;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>Drives <see cref="ImGuiWidgets.DbMeter"/> on its own.</summary>
[TestClass]
public sealed class DbMeterTests : WidgetTest
{
	private const string Label = "output";
	private const string Name = "meter";
	private static readonly Vector2 Size = new(24f, 200f);

	private float db = -20f;
	private float peakDb = float.NegativeInfinity;

	private void Draw()
	{
		ImGuiWidgets.DbMeter(Label, db, Size, peakDb: peakDb);
		Mark(Name);
	}

	[TestMethod]
	public void DbMeter_ReservesTheSizeItIsGiven()
	{
		Start(Draw);

		Rectangle rect = RectOf(Name);

		Assert.IsTrue(Math.Abs(rect.Width - Size.X) <= 2, $"The meter reserved {rect.Width}px of width rather than {Size.X}.");
		Assert.IsTrue(Math.Abs(rect.Height - Size.Y) <= 2, $"The meter reserved {rect.Height}px of height rather than {Size.Y}.");
	}

	[TestMethod]
	public void DbMeter_FillsFromTheBottomUp()
	{
		db = -60f;
		Start(Draw);
		byte[] silent = Snapshot();

		db = 0f;
		Step(2);

		Assert.IsTrue(PixelsChangedSince(silent) > 0, "Raising the level changed nothing on screen.");
	}

	[TestMethod]
	public void DbMeter_LoudLevelsDrawARedZone()
	{
		db = -30f;
		Start(Draw);
		MoveAway();

		Rectangle rect = RectOf(Name);
		Rgba32 nominal = Harness.Capture().GetPixel(rect.MinX + (rect.Width / 2), rect.MaxY - 4);

		db = 3f;
		Step(2);
		// +3 dB on a -60..+6 scale fills all but the top few pixels, so the sample is taken just
		// inside the fill rather than right against the frame.
		Rgba32 hot = Harness.Capture().GetPixel(rect.MinX + (rect.Width / 2), rect.MinY + 20);

		Assert.IsTrue(nominal.G > nominal.R, $"A -30 dB level drew ({nominal.R}, {nominal.G}, {nominal.B}) at the bottom rather than green.");
		Assert.IsTrue(hot.R > hot.G, $"A +3 dB level drew ({hot.R}, {hot.G}, {hot.B}) at the top rather than red.");
	}

	[TestMethod]
	public void DbMeter_PeakMarkerIsDrawnWhenGiven()
	{
		db = -30f;
		peakDb = float.NegativeInfinity;
		Start(Draw);
		byte[] withoutPeak = Snapshot();

		peakDb = -6f;
		Step(2);

		Assert.IsTrue(PixelsChangedSince(withoutPeak) > 0, "A peak-hold level drew no marker.");
	}

	[TestMethod]
	public void DbMeter_LevelsBelowTheFloorDrawNoFill()
	{
		db = float.NegativeInfinity;
		Start(Draw);

		Assert.IsTrue(IsVisible(Name), "A silent meter drew nothing at all, not even its frame.");
	}
}
