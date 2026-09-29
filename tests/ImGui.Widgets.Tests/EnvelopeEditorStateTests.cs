// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImGui.Widgets.Tests;

using System;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Envelope = ImGuiWidgets.Envelope;
using EnvelopeEditorState = ImGuiWidgets.EnvelopeEditorState;
using EnvelopeHandle = ImGuiWidgets.EnvelopeHandle;

/// <summary>
/// Tests the envelope evaluator and the interaction behind EnvelopeEditor. All pure — no ImGui context required.
/// </summary>
/// <remarks>
/// The fixture maps 5 seconds onto 500 pixels, so a pixel is a hundredth of a second and every
/// coordinate below can be checked by hand.
/// </remarks>
[TestClass]
public class EnvelopeEditorStateTests
{
	private const float TimeSpan = 5f;
	private const float Grab = 6f;

	private static readonly Envelope Fixture = new(0.2f, 0.3f, 0.1f, 0.4f, 0.6f, 0.5f);
	private static readonly Vector2 Min = Vector2.Zero;
	private static readonly Vector2 Size = new(500f, 200f);

	private static Vector2[] Positions(Envelope e)
	{
		Vector2[] positions = new Vector2[EnvelopeEditorState.HandleCount];
		EnvelopeEditorState.GetHandlePositions(e, TimeSpan, Min, Size, positions);
		return positions;
	}

	private static Vector2 PositionOf(Envelope e, EnvelopeHandle handle) => Positions(e)[EnvelopeEditorState.IndexOf(handle)];

	/// <summary>Grabs whatever is at <paramref name="from"/> and drags it to <paramref name="to"/>.</summary>
	private static (Envelope Result, bool Changed) DragFrom(Envelope e, Vector2 from, Vector2 to, EnvelopeHandle expected)
	{
		EnvelopeEditorState state = new();
		Assert.IsTrue(state.Activate(Positions(e), from, Grab, includeDelayAndHold: true), "Nothing was grabbed.");
		Assert.AreEqual(expected, state.Active);

		Envelope result = e;
		bool changed = state.Drag(ref result, to, TimeSpan, Min, Size);
		return (result, changed);
	}

	private static void AssertNear(Vector2 expected, Vector2 actual, float tolerance, string what)
	{
		Assert.AreEqual(expected.X, actual.X, tolerance, $"{what}: x");
		Assert.AreEqual(expected.Y, actual.Y, tolerance, $"{what}: y");
	}

	[TestMethod]
	public void Curve_IsLinearAtZeroTension() => Assert.AreEqual(0.25f, Envelope.Curve(0.25f, 0f));

	[TestMethod]
	public void Curve_PositiveTensionMovesFastFirst()
	{
		Assert.AreEqual(0.9526f, Envelope.Curve(0.5f, 1f), 1e-3f);
		Assert.AreEqual(0.0474f, Envelope.Curve(0.5f, -1f), 1e-3f);
	}

	[TestMethod]
	public void Curve_HitsItsEnds()
	{
		foreach (float k in (float[])[-1f, -0.3f, 0.5f, 1f])
		{
			Assert.AreEqual(0f, Envelope.Curve(0f, k), 1e-6f, $"Start at tension {k}");
			Assert.AreEqual(1f, Envelope.Curve(1f, k), 1e-6f, $"End at tension {k}");
		}
	}

	[TestMethod]
	public void Curve_ClampsItsInputs()
	{
		Assert.AreEqual(1f, Envelope.Curve(2f, 0f));
		Assert.AreEqual(0f, Envelope.Curve(-1f, 0f));
		Assert.AreEqual(Envelope.Curve(0.5f, 1f), Envelope.Curve(0.5f, 5f));
	}

	[TestMethod]
	public void Curve_IsMonotone()
	{
		foreach (float k in (float[])[-1f, -0.5f, 0f, 0.5f, 1f])
		{
			float previous = 0f;
			for (int i = 1; i <= 100; i++)
			{
				float value = Envelope.Curve(i / 100f, k);
				Assert.IsGreaterThanOrEqualTo(previous, value, $"Tension {k} fell at {i / 100f}");
				previous = value;
			}
		}
	}

	[TestMethod]
	public void LevelAt_FollowsEachSegment()
	{
		Assert.AreEqual(0f, Fixture.LevelAt(0.1f), "In the delay");
		Assert.AreEqual(0.5f, Fixture.LevelAt(0.35f), 1e-5f, "Halfway through the attack");
		Assert.AreEqual(1f, Fixture.LevelAt(0.55f), "In the hold");
		Assert.AreEqual(0.8f, Fixture.LevelAt(0.8f), 1e-5f, "Halfway through the decay");
		Assert.AreEqual(0.6f, Fixture.LevelAt(10f), 1e-6f, "Sustaining");
	}

	[TestMethod]
	public void LevelAt_ReleaseStartsFromTheLevelAtNoteOff() =>
		Assert.AreEqual(0.4f, Fixture.LevelAt(0.45f, noteOffTime: 0.35f), 1e-5f);

	[TestMethod]
	public void LevelAt_ReleaseEndsAtZero()
	{
		Assert.AreEqual(0f, Fixture.LevelAt(2f, noteOffTime: 1f));
		Assert.AreEqual(0.6f, Fixture.LevelAt(1f, noteOffTime: 1f), 1e-6f);
	}

	[TestMethod]
	public void LevelAt_ZeroAttackJumpsToFull() => Assert.AreEqual(1f, Envelope.Adsr(0f, 0.1f, 0.5f, 0.1f).LevelAt(0f));

	[TestMethod]
	public void LevelAt_NegativeOrNaNTimeIsSilent()
	{
		Assert.AreEqual(0f, Fixture.LevelAt(-1f));
		Assert.AreEqual(0f, Fixture.LevelAt(float.NaN));
	}

	[TestMethod]
	public void LevelAt_StaysInRangeWhateverTheFieldsHold()
	{
		Envelope wild = new(float.NaN, -3f, float.PositiveInfinity, 0.2f, 7f, float.NaN, 9f, float.NaN, -4f);

		for (float t = 0f; t < 2f; t += 0.01f)
		{
			float held = wild.LevelAt(t);
			float released = wild.LevelAt(t, noteOffTime: 0.1f);
			Assert.IsTrue(held is >= 0f and <= 1f, $"Held level {held} at {t}");
			Assert.IsTrue(released is >= 0f and <= 1f, $"Released level {released} at {t}");
		}
	}

	[TestMethod]
	public void GetHandlePositions_PlacesBreakpointsOnTheTimeline()
	{
		AssertNear(new Vector2(50f, 0f), PositionOf(Fixture, EnvelopeHandle.AttackPeak), 1e-3f, "Attack peak");
		AssertNear(new Vector2(60f, 0f), PositionOf(Fixture, EnvelopeHandle.HoldEnd), 1e-3f, "Hold end");
		AssertNear(new Vector2(100f, 80f), PositionOf(Fixture, EnvelopeHandle.DecayEnd), 1e-3f, "Decay end");
		AssertNear(new Vector2(175f, 80f), PositionOf(Fixture, EnvelopeHandle.SustainEnd), 1e-3f, "Sustain end");
		AssertNear(new Vector2(225f, 200f), PositionOf(Fixture, EnvelopeHandle.ReleaseEnd), 1e-3f, "Release end");
	}

	[TestMethod]
	public void GetHandlePositions_TensionHandleSitsOnItsCurve() =>
		AssertNear(new Vector2(35f, 100f), PositionOf(Fixture, EnvelopeHandle.AttackTension), 1e-3f, "Attack tension");

	[TestMethod]
	public void GetHandlePositions_ZeroLengthSegmentPutsItsTensionOnItsBreakpoint()
	{
		Envelope noAttack = Fixture with { Attack = 0f };

		Assert.AreEqual(PositionOf(noAttack, EnvelopeHandle.AttackPeak), PositionOf(noAttack, EnvelopeHandle.AttackTension));
	}

	[TestMethod]
	public void Pick_BreakpointBeatsTensionHandleOnATie()
	{
		Vector2[] positions = new Vector2[EnvelopeEditorState.HandleCount];
		Array.Fill(positions, new Vector2(float.NaN, float.NaN));
		positions[EnvelopeEditorState.IndexOf(EnvelopeHandle.AttackPeak)] = new Vector2(10f, 10f);
		positions[EnvelopeEditorState.IndexOf(EnvelopeHandle.AttackTension)] = new Vector2(10f, 10f);

		Assert.AreEqual(EnvelopeHandle.AttackPeak, EnvelopeEditorState.Pick(positions, new Vector2(10f, 10f), Grab, includeDelayAndHold: true));
	}

	[TestMethod]
	public void Pick_AllZerosGrabsTheFirstVisibleBreakpoint()
	{
		Vector2[] positions = Positions(new Envelope(0f, 0f, 0f, 0f, 0f, 0f));

		Assert.AreEqual(EnvelopeHandle.AttackPeak, EnvelopeEditorState.Pick(positions, new Vector2(0f, 0f), Grab, includeDelayAndHold: false));
	}

	[TestMethod]
	public void Pick_SkipsDelayAndHoldWhenHidden()
	{
		Vector2 delayEnd = PositionOf(Fixture, EnvelopeHandle.DelayEnd);

		Assert.AreEqual(EnvelopeHandle.DelayEnd, EnvelopeEditorState.Pick(Positions(Fixture), delayEnd, Grab, includeDelayAndHold: true));
		Assert.AreEqual(EnvelopeHandle.None, EnvelopeEditorState.Pick(Positions(Fixture), delayEnd, Grab, includeDelayAndHold: false));
	}

	[TestMethod]
	public void Pick_NothingInReachIsNone() =>
		Assert.AreEqual(EnvelopeHandle.None, EnvelopeEditorState.Pick(Positions(Fixture), new Vector2(400f, 20f), Grab, includeDelayAndHold: true));

	[TestMethod]
	public void Drag_AttackPeakLengthensOnlyTheAttack()
	{
		(Envelope result, bool changed) = DragFrom(Fixture, new Vector2(50f, 0f), new Vector2(80f, 0f), EnvelopeHandle.AttackPeak);

		Assert.IsTrue(changed);
		Assert.AreEqual(0.6f, result.Attack, 1e-4f);
		Assert.AreEqual(Fixture.Delay, result.Delay);
		Assert.AreEqual(Fixture.Hold, result.Hold);
		Assert.AreEqual(Fixture.Decay, result.Decay);
		Assert.AreEqual(Fixture.Release, result.Release);
	}

	[TestMethod]
	public void Drag_AttackIsClampedToTheBudget()
	{
		(Envelope result, _) = DragFrom(Fixture, new Vector2(50f, 0f), new Vector2(5000f, 0f), EnvelopeHandle.AttackPeak);

		Assert.AreEqual(4.25f - (0.2f + 0.1f + 0.4f + 0.5f), result.Attack, 1e-4f);
	}

	[TestMethod]
	public void Drag_TimeCannotGoNegative()
	{
		(Envelope result, _) = DragFrom(Fixture, new Vector2(50f, 0f), new Vector2(-100f, 0f), EnvelopeHandle.AttackPeak);

		Assert.AreEqual(0f, result.Attack);
	}

	[TestMethod]
	public void Drag_OverBudgetEnvelopeCanShortenButNotLengthen()
	{
		// The segments sum to 6 s against a 4.25 s budget. Nothing snaps on the press, dragging
		// further right changes nothing, and dragging left shortens.
		Envelope tooLong = new(0f, 0.2f, 0f, 0.3f, 0.5f, 5.5f);
		Vector2 releaseEnd = PositionOf(tooLong, EnvelopeHandle.ReleaseEnd);

		EnvelopeEditorState state = new();
		Vector2[] positions = Positions(tooLong);
		Assert.IsTrue(state.Activate(positions, releaseEnd, Grab, includeDelayAndHold: true));

		Envelope result = tooLong;
		Assert.IsFalse(state.Drag(ref result, releaseEnd, TimeSpan, Min, Size), "The press alone changed it.");
		Assert.IsFalse(state.Drag(ref result, releaseEnd + new Vector2(100f, 0f), TimeSpan, Min, Size), "It grew past the budget.");
		Assert.IsTrue(state.Drag(ref result, releaseEnd - new Vector2(100f, 0f), TimeSpan, Min, Size));
		Assert.AreEqual(4.5f, result.Release, 1e-4f);
	}

	[TestMethod]
	public void Drag_DecayEndMovesDecayAndSustain()
	{
		(Envelope result, bool changed) = DragFrom(Fixture, new Vector2(100f, 80f), new Vector2(120f, 120f), EnvelopeHandle.DecayEnd);

		Assert.IsTrue(changed);
		Assert.AreEqual(0.6f, result.Decay, 1e-4f);
		Assert.AreEqual(0.4f, result.Sustain, 1e-4f);
	}

	[TestMethod]
	public void Drag_SustainEndMovesOnlySustain()
	{
		(Envelope result, bool changed) = DragFrom(Fixture, new Vector2(175f, 80f), new Vector2(400f, 40f), EnvelopeHandle.SustainEnd);

		Assert.IsTrue(changed);
		Assert.AreEqual(0.8f, result.Sustain, 1e-4f);
		Assert.AreEqual(Fixture.Decay, result.Decay);
		Assert.AreEqual(Fixture.Release, result.Release);
	}

	[TestMethod]
	public void Drag_AttackTensionFollowsThePointerUp()
	{
		Vector2 handle = PositionOf(Fixture, EnvelopeHandle.AttackTension);
		(Envelope result, bool changed) = DragFrom(Fixture, handle, handle - new Vector2(0f, 50f), EnvelopeHandle.AttackTension);

		Assert.IsTrue(changed);
		Assert.AreEqual(0.5f, result.AttackTension, 1e-4f);
	}

	[TestMethod]
	public void Drag_ReleaseTensionIsReversedBecauseTheSegmentFalls()
	{
		Vector2 handle = PositionOf(Fixture, EnvelopeHandle.ReleaseTension);
		(Envelope result, bool changed) = DragFrom(Fixture, handle, handle - new Vector2(0f, 50f), EnvelopeHandle.ReleaseTension);

		Assert.IsTrue(changed);
		Assert.AreEqual(-0.5f, result.ReleaseTension, 1e-4f);
	}

	[TestMethod]
	public void Drag_TensionIgnoresHorizontalMovementAndClamps()
	{
		Vector2 handle = PositionOf(Fixture, EnvelopeHandle.DecayTension);

		(Envelope sideways, bool sidewaysChanged) = DragFrom(Fixture, handle, handle + new Vector2(80f, 0f), EnvelopeHandle.DecayTension);
		Assert.IsFalse(sidewaysChanged);
		Assert.AreEqual(Fixture, sideways);

		(Envelope far, _) = DragFrom(Fixture, handle, handle + new Vector2(0f, 1000f), EnvelopeHandle.DecayTension);
		Assert.AreEqual(1f, far.DecayTension);
	}

	[TestMethod]
	public void Drag_WithoutMovingChangesNothing()
	{
		Vector2 offHandle = new(53f, 3f);
		(Envelope result, bool changed) = DragFrom(Fixture, offHandle, offHandle, EnvelopeHandle.AttackPeak);

		Assert.IsFalse(changed);
		Assert.AreEqual(Fixture, result);
	}

	[TestMethod]
	public void Drag_WithNothingGrabbedDoesNothing()
	{
		EnvelopeEditorState state = new();
		Envelope result = Fixture;

		Assert.IsFalse(state.Drag(ref result, new Vector2(300f, 100f), TimeSpan, Min, Size));
		Assert.AreEqual(Fixture, result);
	}

	[TestMethod]
	public void Release_LetsGo()
	{
		EnvelopeEditorState state = new();
		state.Activate(Positions(Fixture), new Vector2(50f, 0f), Grab, includeDelayAndHold: true);

		state.Release();

		Assert.AreEqual(EnvelopeHandle.None, state.Active);
	}

	[TestMethod]
	public void ResetTension_SetsLinear()
	{
		Envelope e = Fixture with { DecayTension = 0.7f };

		Assert.IsTrue(EnvelopeEditorState.ResetTension(ref e, EnvelopeHandle.DecayTension));
		Assert.AreEqual(0f, e.DecayTension);
		Assert.IsFalse(EnvelopeEditorState.ResetTension(ref e, EnvelopeHandle.DecayTension));
	}

	[TestMethod]
	public void ResetTension_IgnoresBreakpoints()
	{
		Envelope e = Fixture with { AttackTension = 0.7f };

		Assert.IsFalse(EnvelopeEditorState.ResetTension(ref e, EnvelopeHandle.AttackPeak));
		Assert.AreEqual(0.7f, e.AttackTension);
	}

	[TestMethod]
	public void Normalize_ReplacesInvalidValues()
	{
		Envelope e = Fixture with
		{
			Delay = float.NaN,
			Attack = -1f,
			Sustain = float.NaN,
			Release = float.PositiveInfinity,
			AttackTension = 3f,
		};

		Assert.IsTrue(EnvelopeEditorState.Normalize(ref e));
		Assert.AreEqual(0f, e.Delay);
		Assert.AreEqual(0f, e.Attack);
		Assert.AreEqual(1f, e.Sustain);
		Assert.AreEqual(0f, e.Release);
		Assert.AreEqual(1f, e.AttackTension);
		Assert.IsFalse(EnvelopeEditorState.Normalize(ref e));
	}

	[TestMethod]
	public void Normalize_LeavesAnOverBudgetEnvelopeAlone()
	{
		Envelope e = new(1f, 2f, 3f, 4f, 0.5f, 5f);

		Assert.IsFalse(EnvelopeEditorState.Normalize(ref e));
		Assert.AreEqual(new Envelope(1f, 2f, 3f, 4f, 0.5f, 5f), e);
	}

	[TestMethod]
	public void Tooltip_FormatsTimesLevelsAndTensions()
	{
		Envelope e = new(0f, 0.12f, 0f, 0.3f, 0.7f, 1.5f, 0f, 0f, 0.4f);

		Assert.AreEqual("Attack 120 ms", ImGuiWidgets.EnvelopeEditorImpl.Describe(e, EnvelopeHandle.AttackPeak));
		Assert.AreEqual("Decay 300 ms · Sustain 70%", ImGuiWidgets.EnvelopeEditorImpl.Describe(e, EnvelopeHandle.DecayEnd));
		Assert.AreEqual("Sustain 70%", ImGuiWidgets.EnvelopeEditorImpl.Describe(e, EnvelopeHandle.SustainEnd));
		Assert.AreEqual("Release 1.50 s", ImGuiWidgets.EnvelopeEditorImpl.Describe(e, EnvelopeHandle.ReleaseEnd));
		Assert.AreEqual("Release tension +0.40", ImGuiWidgets.EnvelopeEditorImpl.Describe(e, EnvelopeHandle.ReleaseTension));
		Assert.AreEqual("Attack tension +0.00", ImGuiWidgets.EnvelopeEditorImpl.Describe(e, EnvelopeHandle.AttackTension));
	}
}
