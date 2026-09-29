// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.UITests;

using System;
using System.Numerics;

using ktsu.ImGui.App.Testing;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using EqBand = ImGuiWidgets.EqBand;
using EqBandType = ImGuiWidgets.EqBandType;

/// <summary>Drives <see cref="ImGuiWidgets.ParametricEq"/> on its own.</summary>
[TestClass]
public sealed class ParametricEqTests : WidgetTest
{
	private const string Label = "EQ";
	private const string Band0 = "EQ/band0";
	private static readonly Vector2 Size = new(400f, 200f);

	private readonly ImGuiWidgets.LogFrequencyAxis axis = new();

	private EqBand[] bands = [new EqBand(1000f, 0f, 1f)];
	private int selected = -1;
	private bool changed;
	private Func<float, float>? response;

	private void Draw()
	{
		// No Mark call: the widget marks itself and each node under its label.
		Func<float, float> responseDb = response ?? (f => ImGuiWidgets.EqResponse.TotalDb(bands, f));
		changed |= ImGuiWidgets.ParametricEq(Label, bands, responseDb, axis, ref selected, Size);
	}

	private void DragBand0(Vector2 by)
	{
		Vector2 from = CenterOf(Band0);
		Harness.Mouse.Drag(from.X, from.Y, from.X + by.X, from.Y + by.Y);
		Step();
	}

	[TestMethod]
	public void ParametricEq_ReservesTheSizeItIsGiven()
	{
		Start(Draw);

		Rectangle rect = RectOf(Label);

		Assert.AreEqual(400, rect.Width);
		Assert.AreEqual(200, rect.Height);
	}

	[TestMethod]
	public void ParametricEq_PressingABandSelectsItWithoutMovingIt()
	{
		EqBand original = bands[0];
		Start(Draw);
		changed = false;

		Click(Band0);

		Assert.AreEqual(0, selected);
		Assert.AreEqual(original, bands[0]);
		Assert.IsFalse(changed, "A press that did not move reported a change.");
	}

	[TestMethod]
	public void ParametricEq_DraggingABandRightRaisesItsFrequency()
	{
		Start(Draw);
		changed = false;

		DragBand0(new Vector2(60f, 0f));

		Assert.IsGreaterThan(1000f, bands[0].Frequency);
		Assert.AreEqual(0f, bands[0].GainDb, 0.25f);
		Assert.IsTrue(changed);
	}

	[TestMethod]
	public void ParametricEq_DraggingABandUpRaisesItsGain()
	{
		Start(Draw);

		DragBand0(new Vector2(0f, -50f));

		Assert.AreEqual(12f, bands[0].GainDb, 1f);
		Assert.AreEqual(1000f, bands[0].Frequency, 1000f * 0.02f);
	}

	[TestMethod]
	public void ParametricEq_CutBandIgnoresVerticalDrag()
	{
		bands = [new EqBand(200f, 0f, EqBand.DefaultQ, EqBandType.LowCut)];
		Start(Draw);

		DragBand0(new Vector2(0f, -50f));

		Assert.AreEqual(0f, bands[0].GainDb);
	}

	[TestMethod]
	public void ParametricEq_WheelOverABandChangesItsQ()
	{
		Start(Draw);
		Vector2 center = CenterOf(Band0);

		Harness.Mouse.Wheel(center.X, center.Y, 1);
		Step();

		Assert.AreEqual(1.2f, bands[0].Q, 1e-4f);
	}

	[TestMethod]
	public void ParametricEq_PressingEmptySpaceClearsTheSelection()
	{
		selected = 0;
		Start(Draw);

		ClickFraction(Label, 0.05f, 0.95f);

		Assert.AreEqual(-1, selected);
	}

	[TestMethod]
	public void ParametricEq_PlotsTheCallersResponse()
	{
		response = _ => 0f;
		Start(Draw);
		MoveAway();
		byte[] flat = Snapshot();

		response = _ => 12f;
		Step(2);

		Assert.IsGreaterThan(0, PixelsChangedSince(flat), "A different response changed nothing on screen.");

		Rectangle rect = RectOf(Label);
		Rectangle? difference = BoundsOfDifference(flat);
		Assert.IsNotNull(difference);
		Assert.IsTrue(
			difference.Value.MinX >= rect.MinX && difference.Value.MaxX <= rect.MaxX
			&& difference.Value.MinY >= rect.MinY && difference.Value.MaxY <= rect.MaxY,
			$"The plot drew outside its rectangle: {difference} against {rect}.");
	}
}
