// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Numerics;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Drives <see cref="ImGuiWidgets.ChannelFader"/> on its own. No test calls <c>Mark</c>: the fader
/// marks itself and its track, handle and meter.
/// </summary>
[TestClass]
public sealed class ChannelFaderTests : WidgetTest
{
	private const string Fader = "fader";
	private const string Track = "fader/track";
	private const string Handle = "fader/handle";
	private const string Meter = "fader/meter";

	private static readonly Vector2 FaderSize = new(60f, 240f);

	private float gainDb;
	private float meterDb = float.NegativeInfinity;
	private bool changed;

	private void Draw() => changed |= ImGuiWidgets.ChannelFader(Fader, ref gainDb, meterDb, size: FaderSize);

	[TestMethod]
	public void ChannelFader_MarksItselfAndItsParts()
	{
		Start(Draw);

		Assert.IsTrue(IsVisible(Fader));
		Assert.IsTrue(IsVisible(Track));
		Assert.IsTrue(IsVisible(Handle));
		Assert.IsTrue(IsVisible(Meter));
	}

	[TestMethod]
	public void ChannelFader_ReservesTheBodyAndAReadoutLine()
	{
		float lineHeight = 0f;
		Start(() =>
		{
			lineHeight = Hexa.NET.ImGui.ImGui.GetTextLineHeight();
			Draw();
		});

		Rectangle rect = RectOf(Fader);
		Assert.AreEqual(FaderSize.X, rect.Width, 2f);
		Assert.IsGreaterThan(FaderSize.Y, rect.Height);
		Assert.IsLessThanOrEqualTo(FaderSize.Y + (2f * lineHeight), rect.Height);
	}

	[TestMethod]
	public void ChannelFader_HandleSitsAtUnityForZeroDb()
	{
		Start(Draw);

		Rectangle meter = RectOf(Meter);
		Vector2 handle = CenterOf(Handle);
		Assert.AreEqual(meter.MaxY - (0.75f * meter.Height), handle.Y, 2f);
	}

	[TestMethod]
	public void ChannelFader_PressOnTheCapDoesNotMoveIt()
	{
		Start(Draw);

		Click(Handle);

		Assert.IsFalse(changed);
		Assert.AreEqual(0f, gainDb);
	}

	[TestMethod]
	public void ChannelFader_DragDownLowersGain()
	{
		Start(Draw);

		Rectangle meter = RectOf(Meter);
		Vector2 from = CenterOf(Handle);
		Harness.Mouse.Drag(from.X, from.Y, from.X, from.Y + 40f);
		Step();

		Assert.IsTrue(changed);
		float expected = ImGuiWidgets.FaderTaper.PositionToDb(0.75f - (40f / meter.Height));
		Assert.AreEqual(expected, gainDb, 0.5f);
	}

	[TestMethod]
	public void ChannelFader_ClickLowOnTheTrackJumps()
	{
		Start(Draw);

		ClickFraction(Track, 0.5f, 0.97f);

		Assert.IsTrue(changed);
		Assert.IsLessThan(-40f, gainDb);
	}

	[TestMethod]
	public void ChannelFader_DragBelowTheBottomReachesNegativeInfinity()
	{
		Start(Draw);

		Vector2 from = CenterOf(Handle);
		Harness.Mouse.Drag(from.X, from.Y, from.X, RectOf(Fader).MaxY + 100f);
		Step();

		Assert.IsTrue(changed);
		Assert.AreEqual(float.NegativeInfinity, gainDb);
	}

	[TestMethod]
	public void ChannelFader_RightClickResetsToUnity()
	{
		gainDb = -12f;
		Start(Draw);

		Vector2 centre = CenterOf(Track);
		Harness.Mouse.Click(centre.X, centre.Y, 1);
		Step();

		Assert.IsTrue(changed);
		Assert.AreEqual(0f, gainDb);
	}

	[TestMethod]
	public void ChannelFader_RightClickAtUnityReportsNoChange()
	{
		Start(Draw);

		Vector2 centre = CenterOf(Track);
		Harness.Mouse.Click(centre.X, centre.Y, 1);
		Step();

		Assert.IsFalse(changed);
		Assert.AreEqual(0f, gainDb);
	}

	[TestMethod]
	public void ChannelFader_WheelNudgesByOneDb()
	{
		Start(Draw);

		Vector2 centre = CenterOf(Handle);
		Harness.Mouse.Wheel(centre.X, centre.Y, 1);
		Step();

		Assert.IsTrue(changed);
		Assert.AreEqual(1f, gainDb);
	}

	[TestMethod]
	public void ChannelFader_MeterFillsToTheTaperedHeight()
	{
		Start(Draw);
		byte[] baseline = Snapshot();

		meterDb = -10f;
		Step(2);

		Rectangle meter = RectOf(Meter);
		Rectangle difference = BoundsOfDifference(baseline) ?? throw new InvalidOperationException("A -10 dB meter level drew nothing.");
		Assert.IsGreaterThanOrEqualTo(meter.MinX, difference.MinX);
		Assert.IsLessThanOrEqualTo(meter.MaxX, difference.MaxX);
		Assert.IsGreaterThanOrEqualTo(meter.MinY, difference.MinY);
		Assert.IsLessThanOrEqualTo(meter.MaxY, difference.MaxY);
		Assert.AreEqual(meter.MaxY - (0.5f * meter.Height), difference.MinY, 2f);
	}

	[TestMethod]
	public void ChannelFader_LeavesAnUntouchedGainBitIdentical()
	{
		gainDb = -3.1f;
		Start(Draw);
		Step(3);

		Assert.IsFalse(changed);
		Assert.AreEqual(-3.1f, gainDb);
	}
}
