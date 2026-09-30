// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the console taper and the drag behind <see cref="ImGuiWidgets.ChannelFader"/>. All pure — no ImGui context required.
/// </summary>
[TestClass]
public class ChannelFaderStateTests
{
	private const float Exact = 1e-5f;

	[TestMethod]
	public void PositionToDb_Breakpoints()
	{
		Assert.AreEqual(6f, ImGuiWidgets.FaderTaper.PositionToDb(1f), Exact);
		Assert.AreEqual(0f, ImGuiWidgets.FaderTaper.PositionToDb(0.75f), Exact);
		Assert.AreEqual(-10f, ImGuiWidgets.FaderTaper.PositionToDb(0.5f), Exact);
		Assert.AreEqual(-20f, ImGuiWidgets.FaderTaper.PositionToDb(0.3f), Exact);
		Assert.AreEqual(-40f, ImGuiWidgets.FaderTaper.PositionToDb(0.15f), Exact);
		Assert.AreEqual(-60f, ImGuiWidgets.FaderTaper.PositionToDb(0.05f), Exact);
	}

	[TestMethod]
	public void PositionToDb_InterpolatesLinearlyBetweenBreakpoints()
	{
		Assert.AreEqual(-5f, ImGuiWidgets.FaderTaper.PositionToDb(0.625f), Exact);
		Assert.AreEqual(-15f, ImGuiWidgets.FaderTaper.PositionToDb(0.4f), Exact);
	}

	[TestMethod]
	public void PositionToDb_ZeroIsNegativeInfinity() =>
		Assert.AreEqual(float.NegativeInfinity, ImGuiWidgets.FaderTaper.PositionToDb(0f));

	[TestMethod]
	public void PositionToDb_BelowTheFloorIsLogarithmic() =>
		Assert.AreEqual(-66.0206f, ImGuiWidgets.FaderTaper.PositionToDb(0.025f), 1e-3f);

	[TestMethod]
	public void PositionToDb_ClampsAndTreatsNaNAsUnity()
	{
		Assert.AreEqual(float.NegativeInfinity, ImGuiWidgets.FaderTaper.PositionToDb(-1f));
		Assert.AreEqual(6f, ImGuiWidgets.FaderTaper.PositionToDb(2f), Exact);
		Assert.AreEqual(0f, ImGuiWidgets.FaderTaper.PositionToDb(float.NaN), Exact);
	}

	[TestMethod]
	public void PositionToDb_MaxDbMovesOnlyTheTopSegment()
	{
		Assert.AreEqual(5f, ImGuiWidgets.FaderTaper.PositionToDb(0.875f, 10f), Exact);
		Assert.AreEqual(-10f, ImGuiWidgets.FaderTaper.PositionToDb(0.5f, 10f), Exact);
	}

	[TestMethod]
	public void DbToPosition_InvertsPositionToDb()
	{
		for (int i = 1; i <= 1000; i++)
		{
			float position = i / 1000f;
			float roundTrip = ImGuiWidgets.FaderTaper.DbToPosition(ImGuiWidgets.FaderTaper.PositionToDb(position));
			Assert.AreEqual(position, roundTrip, Exact, $"position {position}");
		}
	}

	[TestMethod]
	public void DbToPosition_IsStrictlyIncreasing()
	{
		float previous = ImGuiWidgets.FaderTaper.DbToPosition(-120f);
		for (int i = 1; i <= 1260; i++)
		{
			float db = -120f + (i * 0.1f);
			float position = ImGuiWidgets.FaderTaper.DbToPosition(db);
			Assert.IsGreaterThan(previous, position, $"{db} dB");
			previous = position;
		}
	}

	[TestMethod]
	public void DbToPosition_RoundTripsWithinATenThousandthOfADecibel()
	{
		for (int i = 0; i <= 1260; i++)
		{
			float db = -120f + (i * 0.1f);
			Assert.AreEqual(db, ImGuiWidgets.FaderTaper.PositionToDb(ImGuiWidgets.FaderTaper.DbToPosition(db)), 1e-4f, $"{db} dB");
		}
	}

	[TestMethod]
	public void DbToPosition_EndsAndNaN()
	{
		Assert.AreEqual(0f, ImGuiWidgets.FaderTaper.DbToPosition(float.NegativeInfinity));
		Assert.AreEqual(1f, ImGuiWidgets.FaderTaper.DbToPosition(20f));
		Assert.AreEqual(0.75f, ImGuiWidgets.FaderTaper.DbToPosition(float.NaN), Exact);
	}

	[TestMethod]
	public void MaxDb_IsClampedToOneThroughTwentyFour()
	{
		Assert.AreEqual(1f, ImGuiWidgets.FaderTaper.PositionToDb(1f, 0.2f), Exact);
		Assert.AreEqual(24f, ImGuiWidgets.FaderTaper.PositionToDb(1f, 100f), Exact);
		Assert.AreEqual(6f, ImGuiWidgets.FaderTaper.PositionToDb(1f, float.NaN), Exact);
	}

	[TestMethod]
	public void Nudge_StepsByOneDb()
	{
		Assert.AreEqual(1f, ImGuiWidgets.FaderTaper.Nudge(0f, 1f, 1f));
		Assert.AreEqual(-3f, ImGuiWidgets.FaderTaper.Nudge(0f, -3f, 1f));
	}

	[TestMethod]
	public void Nudge_ClampsToMaxDb() =>
		Assert.AreEqual(6f, ImGuiWidgets.FaderTaper.Nudge(6f, 1f, 1f));

	[TestMethod]
	public void Nudge_FallsToNegativeInfinityBelowTheFloor() =>
		Assert.AreEqual(float.NegativeInfinity, ImGuiWidgets.FaderTaper.Nudge(-59.5f, -1f, 1f));

	[TestMethod]
	public void Nudge_UpFromNegativeInfinityLandsOnTheFloor()
	{
		Assert.AreEqual(ImGuiWidgets.FaderTaper.FloorDb, ImGuiWidgets.FaderTaper.Nudge(float.NegativeInfinity, 1f, 1f));
		Assert.AreEqual(float.NegativeInfinity, ImGuiWidgets.FaderTaper.Nudge(float.NegativeInfinity, -1f, 1f));
	}

	[TestMethod]
	public void Nudge_ZeroOrNaNNotchesLeaveTheValue()
	{
		Assert.AreEqual(-3.1f, ImGuiWidgets.FaderTaper.Nudge(-3.1f, 0f, 1f));
		Assert.AreEqual(-3.1f, ImGuiWidgets.FaderTaper.Nudge(-3.1f, float.NaN, 1f));
	}

	[TestMethod]
	public void Press_OnTheCapKeepsTheGrabOffset()
	{
		ImGuiWidgets.ChannelFaderState state = new();
		float[] position = [0.75f];

		state.Press(0.75f, 0.77f, 0.05f);

		Assert.IsTrue(state.IsDragging);
		Assert.IsFalse(state.Drag(position, 0.77f));
		Assert.AreEqual(0.75f, position[0]);

		Assert.IsTrue(state.Drag(position, 0.67f));
		Assert.AreEqual(0.65f, position[0], 1e-6f);
	}

	[TestMethod]
	public void Press_OffTheCapJumps()
	{
		ImGuiWidgets.ChannelFaderState state = new();
		float[] position = [0.75f];

		state.Press(0.75f, 0.2f, 0.05f);

		Assert.AreEqual(0f, state.GrabOffset);
		Assert.IsTrue(state.Drag(position, 0.2f));
		Assert.AreEqual(0.2f, position[0]);
	}

	[TestMethod]
	public void Drag_ClampsToTheTrack()
	{
		ImGuiWidgets.ChannelFaderState state = new();
		float[] position = [0.95f];

		state.Press(0.95f, 0.93f, 0.05f);
		Assert.AreEqual(0.02f, state.GrabOffset, 1e-6f);

		state.Drag(position, 1.2f);
		Assert.AreEqual(1f, position[0]);

		state.Drag(position, -3f);
		Assert.AreEqual(0f, position[0]);
	}

	[TestMethod]
	public void Drag_WithoutAPressIsFalse()
	{
		ImGuiWidgets.ChannelFaderState state = new();
		float[] position = [0.5f];

		Assert.IsFalse(state.IsDragging);
		Assert.IsFalse(state.Drag(position, 0.2f));
		Assert.AreEqual(0.5f, position[0]);
	}

	[TestMethod]
	public void Release_StopsTheDrag()
	{
		ImGuiWidgets.ChannelFaderState state = new();
		float[] position = [0.75f];

		state.Press(0.75f, 0.77f, 0.05f);
		state.Release();

		Assert.IsFalse(state.IsDragging);
		Assert.AreEqual(0f, state.GrabOffset);
		Assert.IsFalse(state.Drag(position, 0.2f));
		Assert.AreEqual(0.75f, position[0]);
	}
}
