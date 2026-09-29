// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests the geometry and drag behind ColorWheel, and the value it edits. All pure — no ImGui
/// context required.
/// </summary>
[TestClass]
public class ColorWheelStateTests
{
	private const float Tolerance = 1e-4f;

	private static float AngleOf(Vector2 disk) => MathF.Atan2(disk.Y, disk.X) * (180.0f / MathF.PI);

	private static float HueDistance(float a, float b)
	{
		float d = MathF.Abs(ColorWheelValue.WrapHue(a) - ColorWheelValue.WrapHue(b));
		return MathF.Min(d, 360.0f - d);
	}

	[TestMethod]
	public void ToDisk_PutsRedWhereAVectorscopeDoes()
	{
		Vector2 red = ImGuiWidgets.ColorWheelState.ToDisk(new ColorWheelValue(0.0f, 1.0f));

		Assert.AreEqual(ImGuiWidgets.ColorWheelState.RedAngle, AngleOf(red), Tolerance);
		Assert.AreEqual(1.0f, red.Length(), Tolerance);
	}

	[TestMethod]
	public void ToDisk_RunsTheHuesCounterClockwise()
	{
		// Red, then yellow a sixth of a turn further round, as on a vectorscope.
		float red = AngleOf(ImGuiWidgets.ColorWheelState.ToDisk(new ColorWheelValue(0.0f, 1.0f)));
		float yellow = AngleOf(ImGuiWidgets.ColorWheelState.ToDisk(new ColorWheelValue(60.0f, 1.0f)));

		Assert.AreEqual(60.0f, yellow - red, Tolerance);
	}

	[TestMethod]
	public void ToDisk_PlacesStrengthAsDistanceFromTheCentre()
	{
		Vector2 half = ImGuiWidgets.ColorWheelState.ToDisk(new ColorWheelValue(200.0f, 0.5f));

		Assert.AreEqual(0.5f, half.Length(), Tolerance);
	}

	[TestMethod]
	public void ToDisk_ClampsStrengthToTheRim()
	{
		Assert.AreEqual(1.0f, ImGuiWidgets.ColorWheelState.ToDisk(new ColorWheelValue(10.0f, 4.0f)).Length(), Tolerance);
		Assert.AreEqual(0.0f, ImGuiWidgets.ColorWheelState.ToDisk(new ColorWheelValue(10.0f, -1.0f)).Length(), Tolerance);
		Assert.AreEqual(0.0f, ImGuiWidgets.ColorWheelState.ToDisk(new ColorWheelValue(10.0f, float.NaN)).Length(), Tolerance);
	}

	[TestMethod]
	public void FromDisk_RoundTripsEveryHue()
	{
		for (float hue = 0.0f; hue < 360.0f; hue += 15.0f)
		{
			ColorWheelValue value = new(hue, 0.75f);

			ColorWheelValue back = ImGuiWidgets.ColorWheelState.FromDisk(ImGuiWidgets.ColorWheelState.ToDisk(value), 0.0f);

			Assert.IsTrue(HueDistance(hue, back.Hue) < 1e-2f, $"Hue {hue} came back as {back.Hue}.");
			Assert.AreEqual(0.75f, back.Strength, Tolerance);
		}
	}

	[TestMethod]
	public void FromDisk_ReadsPastTheRimAsTheRim()
	{
		ColorWheelValue value = ImGuiWidgets.ColorWheelState.FromDisk(new Vector2(3.0f, 4.0f), 0.0f);

		Assert.AreEqual(1.0f, value.Strength, Tolerance);
	}

	[TestMethod]
	public void FromDisk_KeepsThePreviousHueAtTheCentre()
	{
		// The centre has no direction. Reporting red there would swing the hue of a handle dragged
		// through the middle.
		ColorWheelValue value = ImGuiWidgets.ColorWheelState.FromDisk(Vector2.Zero, 217.0f);

		Assert.AreEqual(217.0f, value.Hue, Tolerance);
		Assert.AreEqual(0.0f, value.Strength, Tolerance);
	}

	[TestMethod]
	public void FromDisk_ReturnsHuesInsideTheWrappedRange()
	{
		for (int i = 0; i < 64; i++)
		{
			float angle = i / 64.0f * MathF.PI * 2.0f;
			ColorWheelValue value = ImGuiWidgets.ColorWheelState.FromDisk(new Vector2(MathF.Cos(angle), MathF.Sin(angle)), 0.0f);

			Assert.IsTrue(value.Hue is >= 0.0f and < 360.0f, $"Angle {angle} gave hue {value.Hue}.");
		}
	}

	[TestMethod]
	public void Drag_WithoutBeginChangesNothing()
	{
		ImGuiWidgets.ColorWheelState state = new();
		ColorWheelValue value = new(40.0f, 0.3f);

		Assert.AreEqual(value, state.Drag(value, new Vector2(0.9f, 0.1f), fine: false));
	}

	[TestMethod]
	public void Drag_PressDoesNotJumpTheHandle()
	{
		// A trackball grabs the handle where it is. Pressing on the far side of the wheel and
		// holding still must leave the value exactly as it was.
		ImGuiWidgets.ColorWheelState state = new();
		ColorWheelValue value = new(123.0f, 0.4f);

		state.Begin(value, new Vector2(-0.8f, -0.2f), fine: false);
		ColorWheelValue held = state.Drag(value, new Vector2(-0.8f, -0.2f), fine: false);

		Assert.AreEqual(value, held);
	}

	[TestMethod]
	public void Drag_MovesTheHandleByThePointersMovement()
	{
		ImGuiWidgets.ColorWheelState state = new();
		ColorWheelValue value = ColorWheelValue.Neutral;

		state.Begin(value, new Vector2(0.5f, 0.5f), fine: false);
		ColorWheelValue moved = state.Drag(value, new Vector2(0.5f, 0.8f), fine: false);

		Vector2 disk = ImGuiWidgets.ColorWheelState.ToDisk(moved);
		Assert.AreEqual(0.0f, disk.X, Tolerance);
		Assert.AreEqual(0.3f, disk.Y, Tolerance);
	}

	[TestMethod]
	public void Drag_FineAdjustmentScalesTheMovement()
	{
		ImGuiWidgets.ColorWheelState state = new();
		ColorWheelValue value = ColorWheelValue.Neutral;

		state.Begin(value, Vector2.Zero, fine: true);
		ColorWheelValue moved = state.Drag(value, new Vector2(0.4f, 0.0f), fine: true);

		Assert.AreEqual(0.4f * ImGuiWidgets.ColorWheelState.FineScale, moved.Strength, Tolerance);
	}

	[TestMethod]
	public void Drag_TogglingFineMidDragDoesNotJumpTheHandle()
	{
		ImGuiWidgets.ColorWheelState state = new();
		ColorWheelValue value = ColorWheelValue.Neutral;

		state.Begin(value, Vector2.Zero, fine: false);
		value = state.Drag(value, new Vector2(0.4f, 0.0f), fine: false);
		Vector2 before = ImGuiWidgets.ColorWheelState.ToDisk(value);

		// Shift goes down with the pointer where it is: the handle must stay put.
		ColorWheelValue after = state.Drag(value, new Vector2(0.4f, 0.0f), fine: true);
		Assert.AreEqual(value, after);

		// And from there it moves at the fine rate.
		after = state.Drag(after, new Vector2(0.4f, 0.4f), fine: true);
		Vector2 disk = ImGuiWidgets.ColorWheelState.ToDisk(after);
		Assert.AreEqual(before.X, disk.X, Tolerance);
		Assert.AreEqual(before.Y + (0.4f * ImGuiWidgets.ColorWheelState.FineScale), disk.Y, Tolerance);
	}

	[TestMethod]
	public void Drag_StopsAtTheRim()
	{
		ImGuiWidgets.ColorWheelState state = new();
		ColorWheelValue value = ColorWheelValue.Neutral;

		state.Begin(value, Vector2.Zero, fine: false);
		value = state.Drag(value, new Vector2(5.0f, 0.0f), fine: false);

		Assert.AreEqual(1.0f, value.Strength, Tolerance);
	}

	[TestMethod]
	public void Drag_BackFromPastTheRimRespondsAtOnce()
	{
		// Overshooting the rim must not be remembered: turning back inward moves the handle
		// straight away rather than after the pointer retraces the overshoot.
		ImGuiWidgets.ColorWheelState state = new();
		ColorWheelValue value = ColorWheelValue.Neutral;

		state.Begin(value, Vector2.Zero, fine: false);
		value = state.Drag(value, new Vector2(5.0f, 0.0f), fine: false);
		value = state.Drag(value, new Vector2(4.5f, 0.0f), fine: false);

		Assert.AreEqual(0.5f, value.Strength, Tolerance);
	}

	[TestMethod]
	public void Drag_ThroughTheCentreKeepsTheHue()
	{
		ImGuiWidgets.ColorWheelState state = new();
		ColorWheelValue value = new(250.0f, 0.2f);
		Vector2 start = ImGuiWidgets.ColorWheelState.ToDisk(value);

		state.Begin(value, start, fine: false);
		value = state.Drag(value, Vector2.Zero, fine: false);

		Assert.AreEqual(0.0f, value.Strength, Tolerance);
		Assert.AreEqual(250.0f, value.Hue, 1e-2f);
	}

	[TestMethod]
	public void End_StopsTheDrag()
	{
		ImGuiWidgets.ColorWheelState state = new();
		ColorWheelValue value = ColorWheelValue.Neutral;

		state.Begin(value, Vector2.Zero, fine: false);
		state.End();

		Assert.IsFalse(state.IsDragging);
		Assert.AreEqual(value, state.Drag(value, new Vector2(0.5f, 0.0f), fine: false));
	}

	[TestMethod]
	public void Drag_KeepsTheMaster()
	{
		ImGuiWidgets.ColorWheelState state = new();
		ColorWheelValue value = new(0.0f, 0.0f, 0.4f);

		state.Begin(value, Vector2.Zero, fine: false);
		value = state.Drag(value, new Vector2(0.3f, 0.0f), fine: false);

		Assert.AreEqual(0.4f, value.Master, Tolerance);
	}

	[TestMethod]
	public void DragMaster_PressDoesNotJumpTheSlider()
	{
		ImGuiWidgets.ColorWheelState state = new();

		state.BeginMaster(0.2f, 0.9f, fine: false);

		Assert.AreEqual(0.2f, state.DragMaster(0.2f, 0.9f, fine: false));
	}

	[TestMethod]
	public void DragMaster_MovesByThePointersMovement()
	{
		ImGuiWidgets.ColorWheelState state = new();

		state.BeginMaster(0.0f, -0.5f, fine: false);

		Assert.AreEqual(0.3f, state.DragMaster(0.0f, -0.2f, fine: false), Tolerance);
	}

	[TestMethod]
	public void DragMaster_FineAdjustmentScalesTheMovement()
	{
		ImGuiWidgets.ColorWheelState state = new();

		state.BeginMaster(0.0f, 0.0f, fine: true);

		Assert.AreEqual(-0.4f * ImGuiWidgets.ColorWheelState.FineScale, state.DragMaster(0.0f, -0.4f, fine: true), Tolerance);
	}

	[TestMethod]
	public void DragMaster_TogglingFineMidDragDoesNotJump()
	{
		ImGuiWidgets.ColorWheelState state = new();

		state.BeginMaster(0.0f, 0.0f, fine: false);
		float master = state.DragMaster(0.0f, 0.5f, fine: false);

		Assert.AreEqual(master, state.DragMaster(master, 0.5f, fine: true));
		Assert.AreEqual(master + (0.4f * ImGuiWidgets.ColorWheelState.FineScale), state.DragMaster(master, 0.9f, fine: true), Tolerance);
	}

	[TestMethod]
	public void DragMaster_StopsAtTheEndsAndTurnsBackAtOnce()
	{
		ImGuiWidgets.ColorWheelState state = new();

		state.BeginMaster(0.0f, 0.0f, fine: false);
		float master = state.DragMaster(0.0f, 3.0f, fine: false);
		Assert.AreEqual(1.0f, master, Tolerance);

		master = state.DragMaster(master, 2.75f, fine: false);
		Assert.AreEqual(0.75f, master, Tolerance);
	}

	[TestMethod]
	public void DragMaster_WithoutBeginOrAfterEndChangesNothing()
	{
		ImGuiWidgets.ColorWheelState state = new();
		Assert.AreEqual(0.1f, state.DragMaster(0.1f, 0.8f, fine: false));

		state.BeginMaster(0.1f, 0.0f, fine: false);
		state.EndMaster();

		Assert.IsFalse(state.IsDraggingMaster);
		Assert.AreEqual(0.1f, state.DragMaster(0.1f, 0.8f, fine: false));
	}

	[TestMethod]
	public void DragMaster_AndTheBallDragIndependently()
	{
		ImGuiWidgets.ColorWheelState state = new();

		state.BeginMaster(0.0f, 0.0f, fine: false);

		Assert.IsFalse(state.IsDragging, "Starting the slider started the ball.");
	}

	[TestMethod]
	public void ToRgbOffset_AddsTheMasterToEveryChannel()
	{
		Vector3 offset = new ColorWheelValue(0.0f, 0.0f, -0.25f).ToRgbOffset();

		Assert.AreEqual(new Vector3(-0.25f), offset);
	}

	[TestMethod]
	public void ToRgbOffset_BalancePlusMasterSumsToThreeMasters()
	{
		Vector3 offset = new ColorWheelValue(75.0f, 0.6f, 0.2f).ToRgbOffset();

		Assert.AreEqual(0.6f, offset.X + offset.Y + offset.Z, Tolerance);
	}

	[TestMethod]
	public void IsNeutral_NeedsBothTheBallAndTheMasterAtRest()
	{
		Assert.IsTrue(ColorWheelValue.Neutral.IsNeutral);
		Assert.IsTrue(new ColorWheelValue(123.0f, 0.0f).IsNeutral, "A hue alone does nothing.");
		Assert.IsFalse(new ColorWheelValue(0.0f, 0.1f).IsNeutral);
		Assert.IsFalse(new ColorWheelValue(0.0f, 0.0f, 0.1f).IsNeutral);
	}

	[TestMethod]
	public void ToRgbOffset_IsZeroWhenNeutral()
	{
		Assert.AreEqual(Vector3.Zero, new ColorWheelValue(90.0f, 0.0f).ToRgbOffset());
	}

	[TestMethod]
	public void ToRgbOffset_KeepsTheChannelAverage()
	{
		for (float hue = 0.0f; hue < 360.0f; hue += 20.0f)
		{
			Vector3 offset = new ColorWheelValue(hue, 1.0f).ToRgbOffset();

			Assert.AreEqual(0.0f, offset.X + offset.Y + offset.Z, Tolerance, $"Hue {hue} moved the average.");
		}
	}

	[TestMethod]
	public void ToRgbOffset_PushesTowardsTheHue()
	{
		Vector3 red = new ColorWheelValue(0.0f, 1.0f).ToRgbOffset();
		Vector3 blue = new ColorWheelValue(240.0f, 0.5f).ToRgbOffset();

		Assert.AreEqual(2.0f / 3.0f, red.X, Tolerance);
		Assert.AreEqual(-1.0f / 3.0f, red.Y, Tolerance);
		Assert.AreEqual(1.0f / 3.0f, blue.Z, Tolerance);
		Assert.IsTrue(blue.X < 0.0f && blue.Y < 0.0f);
	}

	[TestMethod]
	public void HueToRgb_HitsThePrimariesAndSecondaries()
	{
		Assert.AreEqual(new Vector3(1, 0, 0), ColorWheelValue.HueToRgb(0.0f));
		Assert.AreEqual(new Vector3(1, 1, 0), ColorWheelValue.HueToRgb(60.0f));
		Assert.AreEqual(new Vector3(0, 1, 0), ColorWheelValue.HueToRgb(120.0f));
		Assert.AreEqual(new Vector3(0, 1, 1), ColorWheelValue.HueToRgb(180.0f));
		Assert.AreEqual(new Vector3(0, 0, 1), ColorWheelValue.HueToRgb(240.0f));
		Assert.AreEqual(new Vector3(1, 0, 1), ColorWheelValue.HueToRgb(300.0f));
		Assert.AreEqual(new Vector3(1, 0, 0), ColorWheelValue.HueToRgb(360.0f));
	}

	[TestMethod]
	public void WrapHue_WrapsIntoRange()
	{
		Assert.AreEqual(350.0f, ColorWheelValue.WrapHue(-10.0f), Tolerance);
		Assert.AreEqual(10.0f, ColorWheelValue.WrapHue(730.0f), Tolerance);
		Assert.AreEqual(0.0f, ColorWheelValue.WrapHue(-1e-8f), Tolerance);
		Assert.AreEqual(0.0f, ColorWheelValue.WrapHue(float.NaN));
	}
}
