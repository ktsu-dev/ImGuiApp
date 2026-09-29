// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using EqBand = ImGuiWidgets.EqBand;
using EqBandType = ImGuiWidgets.EqBandType;
using EqResponse = ImGuiWidgets.EqResponse;
using ParametricEqState = ImGuiWidgets.ParametricEqState;

/// <summary>
/// Tests the node geometry, picking, dragging, Q wheel and normalisation behind ParametricEq, and the
/// RBJ biquad magnitudes in EqResponse. All pure — no ImGui context required.
/// </summary>
[TestClass]
public class ParametricEqStateTests
{
	private const float MinDb = -24f;
	private const float MaxDb = 24f;
	private const float GrabRadius = 6f;

	private static readonly ImGuiWidgets.LogFrequencyAxis Axis = new(20f, 20000f);
	private static readonly Vector2 Min = Vector2.Zero;
	private static readonly Vector2 Size = new(400f, 200f);

	private static Vector2 NodeOf(EqBand band) => ParametricEqState.NodePosition(band, Axis, MinDb, MaxDb, Min, Size);

	private static bool DragTo(ParametricEqState state, EqBand[] bands, Vector2 pointer) =>
		state.Drag(bands, pointer, Axis, MinDb, MaxDb, Min, Size);

	[TestMethod]
	public void NodePosition_PlacesFrequencyAndGainOnTheAxes()
	{
		Vector2 node = NodeOf(new EqBand(1000f, 12f, 1f));

		Assert.AreEqual(Axis.FrequencyToPosition(1000f) * 400f, node.X, 1e-3f);
		Assert.AreEqual(50f, node.Y, 1e-3f);
	}

	[TestMethod]
	public void NodePosition_GainlessBandSitsOnZeroDb()
	{
		Vector2 node = NodeOf(new EqBand(100f, 9f, 0.7f, EqBandType.LowCut));

		Assert.AreEqual(100f, node.Y);
	}

	[TestMethod]
	public void Pick_FindsTheNearestNodeWithinRadius()
	{
		Vector2[] nodes = [new(10f, 10f), new(30f, 10f)];

		Assert.AreEqual(1, ParametricEqState.Pick(nodes, new Vector2(27f, 10f), GrabRadius));
		Assert.AreEqual(-1, ParametricEqState.Pick(nodes, new Vector2(20f, 40f), GrabRadius));
	}

	[TestMethod]
	public void Pick_TieGoesToTheLaterBand()
	{
		Vector2[] nodes = [new(10f, 10f), new(10f, 10f)];

		Assert.AreEqual(1, ParametricEqState.Pick(nodes, new Vector2(10f, 10f), GrabRadius));
	}

	[TestMethod]
	public void Activate_RemembersTheGrabOffset()
	{
		ParametricEqState state = new();
		Vector2[] nodes = [new(100f, 50f)];

		Assert.IsTrue(state.Activate(nodes, new Vector2(103f, 52f), GrabRadius));
		Assert.AreEqual(0, state.ActiveBand);
		Assert.AreEqual(new Vector2(-3f, -2f), state.GrabOffset);
	}

	[TestMethod]
	public void Drag_WithoutMovingChangesNothing()
	{
		EqBand original = new(1000f, 3.3f, 1f);
		EqBand[] bands = [original];
		ParametricEqState state = new();
		Vector2 pointer = NodeOf(original) + new Vector2(3f, 0f);

		Assert.IsTrue(state.Activate([NodeOf(original)], pointer, GrabRadius));
		Assert.IsFalse(DragTo(state, bands, pointer));
		Assert.AreEqual(original, bands[0]);
	}

	[TestMethod]
	public void Drag_MovesFrequencyAndGain()
	{
		EqBand[] bands = [new EqBand(1000f, 0f, 1f)];
		ParametricEqState state = new();
		Vector2 node = NodeOf(bands[0]);

		state.Activate([node], node, GrabRadius);

		Assert.IsTrue(DragTo(state, bands, node - new Vector2(0f, 50f)));
		Assert.AreEqual(12f, bands[0].GainDb, 1e-3f);
		Assert.AreEqual(1000f, bands[0].Frequency, 1000f * 1e-4f);
	}

	[TestMethod]
	public void Drag_ClampsToTheAxes()
	{
		EqBand[] bands = [new EqBand(1000f, 0f, 1f)];
		ParametricEqState state = new();
		Vector2 node = NodeOf(bands[0]);

		state.Activate([node], node, GrabRadius);
		DragTo(state, bands, new Vector2(-500f, -500f));

		Assert.AreEqual(20f, bands[0].Frequency);
		Assert.AreEqual(24f, bands[0].GainDb);
	}

	[TestMethod]
	public void Drag_GainlessBandKeepsItsGain()
	{
		EqBand[] bands = [new EqBand(100f, 5f, 0.7f, EqBandType.LowCut)];
		ParametricEqState state = new();
		Vector2 node = NodeOf(bands[0]);

		state.Activate([node], node, GrabRadius);
		DragTo(state, bands, node - new Vector2(0f, 80f));

		Assert.AreEqual(5f, bands[0].GainDb);
		Assert.AreEqual(100f, bands[0].Frequency);
	}

	[TestMethod]
	public void AdjustQ_MultipliesPerDetentAndClamps()
	{
		EqBand[] bands = [new EqBand(1000f, 0f, 1f)];
		Assert.IsTrue(ParametricEqState.AdjustQ(bands, 0, 1f));
		Assert.AreEqual(1.2f, bands[0].Q, 1e-5f);

		bands[0] = bands[0] with { Q = 17f };
		Assert.IsTrue(ParametricEqState.AdjustQ(bands, 0, 3f));
		Assert.AreEqual(18f, bands[0].Q);

		bands[0] = bands[0] with { Q = 0.11f };
		Assert.IsTrue(ParametricEqState.AdjustQ(bands, 0, -2f));
		Assert.AreEqual(0.1f, bands[0].Q);

		Assert.IsFalse(ParametricEqState.AdjustQ(bands, 0, 0f));
	}

	[TestMethod]
	public void Normalize_ReplacesNaNAndClamps()
	{
		EqBand[] bands = [new EqBand(float.NaN, float.NaN, float.NaN), new EqBand(50000f, 99f, 40f)];

		Assert.IsTrue(ParametricEqState.Normalize(bands, Axis, MinDb, MaxDb));
		Assert.AreEqual(MathF.Sqrt(20f * 20000f), bands[0].Frequency);
		Assert.AreEqual(0f, bands[0].GainDb);
		Assert.AreEqual(EqBand.DefaultQ, bands[0].Q);
		Assert.AreEqual(20000f, bands[1].Frequency);
		Assert.AreEqual(24f, bands[1].GainDb);
		Assert.AreEqual(18f, bands[1].Q);

		Assert.IsFalse(ParametricEqState.Normalize(bands, Axis, MinDb, MaxDb));
	}

	[TestMethod]
	public void Normalize_EachBadBandReportsAChange()
	{
		EqBand[] nan = [new EqBand(float.NaN, float.NaN, float.NaN)];
		EqBand[] outOfRange = [new EqBand(50000f, 99f, 40f)];

		Assert.IsTrue(ParametricEqState.Normalize(nan, Axis, MinDb, MaxDb));
		Assert.IsTrue(ParametricEqState.Normalize(outOfRange, Axis, MinDb, MaxDb));
	}

	[TestMethod]
	public void BandDb_PeakHitsItsGainAtItsFrequency() =>
		Assert.AreEqual(6f, EqResponse.BandDb(new EqBand(1000f, 6f, 1f), 1000f), 0.01f);

	[TestMethod]
	public void BandDb_PeakIsFlatFarAway() =>
		Assert.IsLessThan(0.1f, MathF.Abs(EqResponse.BandDb(new EqBand(1000f, 6f, 4f), 20f)));

	[TestMethod]
	public void BandDb_ButterworthLowCutIsMinusThreeAtCutoff()
	{
		EqBand lowCut = new(1000f, 0f, EqBand.DefaultQ, EqBandType.LowCut);

		Assert.AreEqual(-3.01f, EqResponse.BandDb(lowCut, 1000f), 0.05f);
		Assert.IsLessThan(-30f, EqResponse.BandDb(lowCut, 20f));
	}

	[TestMethod]
	public void BandDb_ShelfReachesItsGainOnItsSide()
	{
		EqBand lowShelf = new(200f, -6f, EqBand.DefaultQ, EqBandType.LowShelf);
		Assert.AreEqual(-6f, EqResponse.BandDb(lowShelf, 20f), 0.1f);
		Assert.IsLessThan(0.1f, MathF.Abs(EqResponse.BandDb(lowShelf, 10000f)));

		EqBand highShelf = new(5000f, 6f, EqBand.DefaultQ, EqBandType.HighShelf);
		Assert.AreEqual(6f, EqResponse.BandDb(highShelf, 20000f), 0.1f);
		Assert.IsLessThan(0.1f, MathF.Abs(EqResponse.BandDb(highShelf, 100f)));
	}

	[TestMethod]
	public void BandDb_NotchIsDeepAtItsFrequency() =>
		Assert.IsLessThan(-40f, EqResponse.BandDb(new EqBand(1000f, 0f, 4f, EqBandType.Notch), 1000f));

	[TestMethod]
	public void BandDb_HighCutMirrorsLowCut()
	{
		EqBand highCut = new(1000f, 0f, EqBand.DefaultQ, EqBandType.HighCut);

		Assert.AreEqual(-3.01f, EqResponse.BandDb(highCut, 1000f), 0.05f);
		Assert.IsLessThan(-30f, EqResponse.BandDb(highCut, 20000f));
	}

	[TestMethod]
	public void BandDb_RejectsAnUnusableSampleRate()
	{
		EqBand band = new(1000f, 6f, 1f);

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => EqResponse.BandDb(band, 1000f, 0f));
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => EqResponse.BandDb(band, 1000f, float.NaN));
	}

	[TestMethod]
	public void TotalDb_IsTheSumOfBands()
	{
		EqBand a = new(300f, 4f, 1f);
		EqBand b = new(2000f, -5f, 2f);
		EqBand[] bands = [a, b];

		foreach (float frequency in new[] { 100f, 500f, 1500f, 8000f })
		{
			float expected = EqResponse.BandDb(a, frequency) + EqResponse.BandDb(b, frequency);
			Assert.AreEqual(expected, EqResponse.TotalDb(bands, frequency), 1e-4f, $"{frequency} Hz");
		}

		Assert.AreEqual(0f, EqResponse.TotalDb([], 1000f));
	}
}
